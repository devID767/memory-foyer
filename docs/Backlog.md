# Backlog

Tracked work for this project. Items are added via `/bug`, `/todo`, `/idea` skills and closed via `/done`.

ID convention: `B-N` for bugs, `T-N` for todos, `I-N` for ideas. Counter is monotonic per section.

## Todo
<!-- next ID: T-4 -->

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
- [x] **T-1** (2026-05-04 → 2026-05-04) Server: enforce per-UTC-day new-card cap (replace per-fetch)
