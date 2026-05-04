# Memory Foyer — Backend

Local-only authoritative backend for the Memory Foyer Unity client. Stores per-card SM-2 state in SQLite (`better-sqlite3`), runs server-side SM-2 on every `POST /sessions`, and returns the full deck snapshot so the client can overwrite its cache atomically. Contract: [docs/GDD.md §8](../docs/GDD.md). API schema: [openapi.yaml](openapi.yaml).

## Prerequisites

- Node.js ≥ 20
- C toolchain (Xcode Command Line Tools on macOS, build-essential on Linux). `better-sqlite3` builds native bindings on `npm install`.

## Run

```bash
cd server
npm install
npm start
# Memory Foyer server listening on http://localhost:3000
```

The first start creates `server/data.sqlite` and seeds the decks declared in [`decks.json`](decks.json). That file is generated from Unity DeckAssets via the `Tools/Memory Foyer/Export Decks → server-decks.json` Editor menu and committed to git — the server does not embed any card content. To reset all SR history: stop the server, delete `data.sqlite`, restart.

### Adding or editing a deck

1. Open the `DeckAsset` under `Assets/Resources/Decks/` in Unity (or create a new one via `Create → MemoryFoyer/Deck`).
2. Edit cards, `_displayName`, `_newCardsPerDay`, etc. in the Inspector.
3. Run `Tools/Memory Foyer/Export Decks → server-decks.json` from the Unity menu bar.
4. Commit the changed `server/decks.json` (and the `.asset` files).
5. Restart the server. Existing schedules are preserved; new cards default to `stage='new'`. Metadata changes (`displayName`, `description`, `newCardsPerDay`) propagate via upsert.

### Known limitation: deletion

Deleting a deck or card from `decks.json` does **not** remove the corresponding rows from the DB — the seed flow only inserts/updates. Server logs an orphan WARN at boot listing `card_id`s present in DB but absent from the registry. To physically remove rows, run manual SQL.

```bash
npm test
```

Runs the full test suite (`sm2.test.js` algorithm parity + `server.test.js` integration via `supertest`).

## Endpoints

| Method | Path                       | Purpose                                             |
| ------ | -------------------------- | --------------------------------------------------- |
| GET    | `/health`                  | Liveness probe                                      |
| GET    | `/decks`                   | Deck summaries with `dueCount`, `newCount`, `totalCount` |
| GET    | `/decks/:id/schedule`      | Per-card schedule (filtered by new-card cap)        |
| POST   | `/sessions`                | Submit completed session; runs SM-2; returns snapshot |
| GET    | `/sessions/:id`            | Fetch a previously processed session                |

### curl examples

```bash
# Liveness
curl localhost:3000/health

# Decks
curl localhost:3000/decks

# Schedule for one deck
curl localhost:3000/decks/capitals/schedule

# Submit a session (UUID and reviews from the client)
curl -X POST localhost:3000/sessions \
  -H 'content-type: application/json' \
  -d '{
        "sessionId": "11111111-1111-4111-8111-111111111111",
        "deckId": "capitals",
        "reviews": [
          { "cardId": "capitals:1", "grade": 4, "reviewedAt": "2026-05-03T11:00:00.000Z" }
        ]
      }'

# Same POST again → { ok:true, dedup:true, ... }. Different reviews with the same
# sessionId → 409 session-payload-mismatch.

# Fetch processed session
curl localhost:3000/sessions/11111111-1111-4111-8111-111111111111
```

## Schema

`schema.sql` (idempotent, applied on every boot):

- `decks` — id, display name, description, `new_cards_per_day`.
- `cards` — registry only: `card_id`, `deck_id`, `ord`. Card text (front/back) lives in DeckAsset SOs on the client; the server never stores it.
- `card_schedules` — one row per card holding `Sm2State` (reps, ease, interval, due, stage, learning step) plus `released_on` (UTC date `'YYYY-MM-DD'` the card first entered the learner's queue; NULL until released). All times stored as ISO-8601 UTC strings with `Z` suffix.
- `processed_sessions` — `session_id UNIQUE`, `payload_hash`, full `snapshot_json` of the deck schedule at first processing time. Used for idempotent retries.
- `review_log` — append-only log of every grade submitted, joined by `session_id`.

## Why authoritative

The server is the source of truth for `Sm2State`. The Unity client caches the latest `DeckSchedule` locally (`JsonFileScheduleCache`) for offline use, but every `POST /sessions` reapplies SM-2 server-side and the response overwrites that cache atomically. Multi-device conflict resolution is last-write-wins on the server (single-device by design — local-only deployment, no auth, no cloud). See [GDD §15](../docs/GDD.md).

## Known limitations

1. **Removing a deck or card requires manual SQL.** The `decks.json` seed flow inserts and updates but never deletes — protects user progress. Boot-time orphan WARN identifies stale rows; cleanup is operator-driven.

## New-card release semantics

`GET /decks/:id/schedule` is a mutating read: it sets `released_on = today (UTC)` on up to `new_cards_per_day` previously-unreleased `stage='new'` cards (ordered by `cards.ord`), then returns every `learning|review|relearning` card plus every `stage='new'` card with `released_on IS NOT NULL`. The release step is wrapped in a SQLite transaction and is idempotent on retry — `released_on` persists across stage transitions, so a graded-then-graduated card still consumes its slot for that UTC day.

Concretely:

- Two consecutive fetches in the same UTC day return the same cards. The second fetch releases nothing further.
- Cards left ungraded across a UTC midnight stay visible (still `stage='new'`, `released_on` from a prior day) and do **not** count against the new day's quota — the new day releases up to `new_cards_per_day` additional fresh cards on top of leftovers.
- A player who never opens the schedule on day N does **not** accumulate that day's quota for day N+1: release is lazy, triggered only by `GET /:id/schedule`.
- `GET /decks` projects the next-fetch outcome (`newCount = released_new_count + min(unreleased_new_count, max(0, new_cards_per_day - released_today_count))`) without mutating state — calling `/decks` is always safe.

## Idempotency contract

- First `POST /sessions` with a new `sessionId` → `200 { ok: true, updatedSchedule }`. The released-subset snapshot (same filter as `GET /:id/schedule`) is stored in `processed_sessions.snapshot_json`.
- Repeat with same `sessionId` and same canonical `payload_hash` (sha-256 of `[{cardId, grade, reviewedAt}, ...]` with array order preserved) → `200 { ok: true, dedup: true, updatedSchedule }` — the stored snapshot is returned without re-running SM-2.
- Repeat with same `sessionId` but a different `payload_hash` → `409 { error: 'session-payload-mismatch', sessionId }`.

The session row is inserted with `INSERT OR IGNORE` *before* any schedule mutations, so concurrent retries cannot double-apply SM-2.

## Validation

`zod` schemas in `schemas.js`. On parse failure: `400 { error: 'validation', details: zodIssues }`. Domain errors share the same envelope: `400 { error: 'unknown-card', cardId }`, `404 { error: 'unknown-deck', deckId }`, `404 { error: 'unknown-session', sessionId }`, `409 { error: 'session-payload-mismatch', sessionId }`.

## Local-only by design

No cloud deployment, no auth, no multi-device sync. `data.sqlite` lives next to the running server and is git-ignored. If the file is deleted, all SR history is lost — that's the expected reset path.
