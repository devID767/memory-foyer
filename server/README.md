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

The first start creates `server/data.sqlite` and seeds three decks (Capitals of Europe, Periodic Table 1–20, English Idioms — see [GDD §7](../docs/GDD.md)). To reset all SR history: stop the server, delete `data.sqlite`, restart.

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
curl localhost:3000/decks/capitals-eu/schedule

# Submit a session (UUID and reviews from the client)
curl -X POST localhost:3000/sessions \
  -H 'content-type: application/json' \
  -d '{
        "sessionId": "11111111-1111-4111-8111-111111111111",
        "deckId": "capitals-eu",
        "reviews": [
          { "cardId": "capitals-eu:1", "grade": 4, "reviewedAt": "2026-05-03T11:00:00.000Z" }
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
- `cards` — per-deck card content, with stable `ord` for new-card ordering.
- `card_schedules` — one row per card holding `Sm2State` (reps, ease, interval, due, stage, learning step). All times stored as ISO-8601 UTC strings with `Z` suffix.
- `processed_sessions` — `session_id UNIQUE`, `payload_hash`, full `snapshot_json` of the deck schedule at first processing time. Used for idempotent retries.
- `review_log` — append-only log of every grade submitted, joined by `session_id`.

## Why authoritative

The server is the source of truth for `Sm2State`. The Unity client caches the latest `DeckSchedule` locally (`JsonFileScheduleCache`) for offline use, but every `POST /sessions` reapplies SM-2 server-side and the response overwrites that cache atomically. Multi-device conflict resolution is last-write-wins on the server (single-device by design — local-only deployment, no auth, no cloud). See [GDD §15](../docs/GDD.md).

## Known limitations

1. **New-card cap is per-fetch, not per-UTC-day.** `GET /decks/:id/schedule` returns at most `new_cards_per_day` cards in `stage='new'` (ordered by `cards.ord`). The same N cards surface on every fetch until the player grades them — this diverges from a literal reading of GDD §5 ("per UTC day") in favor of a simpler implementation. Adding per-day release tracking would require a `released_on` column and is intentionally deferred.
2. **Wire collapses `relearning` → `learning`.** The C# domain has four stages (`New`/`Learning`/`Review`/`Relearning`); the wire format and server schema use only three (`new`/`learning`/`review`), matching `ScheduleMappers.StageToWire`. The server SM-2 distinguishes the two cases internally via `reps > 0` (collapsed-relearning preserves reps on graduate; plain learning starts at `reps = 1`). The C# client cannot make this distinction when reading back, so an offline-graded card in the relearning queue computes `repetitions = 1` on graduate locally vs preserved-reps on the server. This is healed on next sync — the server response overwrites the cache. Acceptable per [GDD §15](../docs/GDD.md).

## Idempotency contract

- First `POST /sessions` with a new `sessionId` → `200 { ok: true, updatedSchedule }`. The full deck snapshot is stored in `processed_sessions.snapshot_json`.
- Repeat with same `sessionId` and same canonical `payload_hash` (sha-256 of `[{cardId, grade, reviewedAt}, ...]` with array order preserved) → `200 { ok: true, dedup: true, updatedSchedule }` — the stored snapshot is returned without re-running SM-2.
- Repeat with same `sessionId` but a different `payload_hash` → `409 { error: 'session-payload-mismatch', sessionId }`.

The session row is inserted with `INSERT OR IGNORE` *before* any schedule mutations, so concurrent retries cannot double-apply SM-2.

## Validation

`zod` schemas in `schemas.js`. On parse failure: `400 { error: 'validation', details: zodIssues }`. Domain errors share the same envelope: `400 { error: 'unknown-card', cardId }`, `404 { error: 'unknown-deck', deckId }`, `404 { error: 'unknown-session', sessionId }`, `409 { error: 'session-payload-mismatch', sessionId }`.

## Local-only by design

No cloud deployment, no auth, no multi-device sync. `data.sqlite` lives next to the running server and is git-ignored. If the file is deleted, all SR history is lost — that's the expected reset path.
