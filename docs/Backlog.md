# Backlog

Tracked work for this project. Items are added via `/bug`, `/todo`, `/idea` skills and closed via `/done`.

ID convention: `B-N` for bugs, `T-N` for todos, `I-N` for ideas. Counter is monotonic per section.

## Todo
<!-- next ID: T-4 -->

- [ ] **T-1** (2026-05-04) Server: enforce per-UTC-day new-card cap (replace per-fetch).
      Add `released_on TEXT` to `card_schedules` (or equivalent per-day release tracking);
      update `GET /decks/:id/schedule` to release at most `new_cards_per_day` `stage='new'`
      cards per UTC day and let leftovers carry over. Aligns server behavior with GDD §5 + §13
      design intent. See [server/README.md §Known limitations](../server/README.md).
      **Acceptance:** integration test in `server/server.test.js` proving the cap holds
      across two consecutive `GET /decks/:id/schedule` within the same UTC day; second test
      proving leftover from yesterday surfaces today plus today's fresh cards (≤ `new_cards_per_day` new).

- [ ] **T-2** (2026-05-04) Eliminate openapi.yaml ↔ zod schema duplication via `zod-to-openapi`. Currently `server/openapi.yaml` and `server/schemas.js` describe the same DTOs (CardReview, SessionResult, etc.) in parallel and can drift silently. Generate the OpenAPI spec from zod schemas as single source of truth; keep `openapi.yaml` as a generated artifact.

- [ ] **T-3** (2026-05-04) Align `POST /sessions` `updatedSchedule` with `GET /decks/:id/schedule`
      filter semantics. Currently `buildDeckSchedule` in `server/sessions.js` returns the *full*
      deck (all cards, including `stage='new'` rows that haven't been released to the learner),
      while `GET /decks/:id/schedule` returns only the released subset (post T-1: per-UTC-day
      release tracking via `released_on`). The Unity client overwrites its cache atomically with
      the POST response, so unreleased cards leak into the cache and can surface as "due" in the
      client's local queue computation, contradicting GDD §5: *"the client trusts that filter and
      does not re-cap"*. Fix: filter `buildDeckSchedule` identically to `GET /:id/schedule`, OR
      rename the wire field to clarify it's a full snapshot and have the client re-apply the
      filter. Decide and document. **Acceptance:** integration test in `server/server.test.js`
      proving `POST /sessions` response and a subsequent `GET /decks/:id/schedule` for the same
      deck return identical card sets; dedup-retry returns the same filtered snapshot even if
      the daily quota has shifted between original and retry.

## Bugs
<!-- next ID: B-1 -->

## Ideas
<!-- next ID: I-1 -->

## Archive

- [x] **T-0** (2026-05-02 → 2026-05-02) Set up first system
