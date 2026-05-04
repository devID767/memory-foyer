# Backlog

Tracked work for this project. Items are added via `/bug`, `/todo`, `/idea` skills and closed via `/done`.

ID convention: `B-N` for bugs, `T-N` for todos, `I-N` for ideas. Counter is monotonic per section.

## Todo
<!-- next ID: T-4 -->

- [ ] **T-2** (2026-05-04) Eliminate openapi.yaml ↔ zod schema duplication via `zod-to-openapi`. Currently `server/openapi.yaml` and `server/schemas.js` describe the same DTOs (CardReview, SessionResult, etc.) in parallel and can drift silently. Generate the OpenAPI spec from zod schemas as single source of truth; keep `openapi.yaml` as a generated artifact.

## Bugs
<!-- next ID: B-2 -->

- [ ] **B-1** (2026-05-04) Daily new-card cap can be exceeded if a client POSTs a review for a
      `stage='new'` card whose `released_on IS NULL` (i.e. the card was never returned by
      `GET /:id/schedule`). After SM-2 the card transitions stage but `released_on` stays NULL,
      so `decks.js` `releasedToday` (which counts `released_on = today`) does not see it; a
      subsequent `GET /:id/schedule` on the same UTC day can release another full
      `new_cards_per_day` quota — total new cards introduced today exceeds the cap. **Dormant in
      production:** the Unity client always calls `GET /:id/schedule` before any POST
      (`Assets/Scripts/Application/Sessions/ReviewSessionService.cs:93`), so the leak is only
      reachable via direct curl / future client. **Acceptance:** integration test that constructs
      the leak (POST without prior GET on a fresh deck → second GET releases > cap cards) and
      proves the chosen fix — either server stamps `released_on = today` on grading any
      `stage='new'` card, or POST rejects reviews referencing an unreleased new card with 400.

## Ideas
<!-- next ID: I-1 -->

## Archive

- [x] **T-0** (2026-05-02 → 2026-05-02) Set up first system
- [x] **T-1** (2026-05-04 → 2026-05-04) Server: enforce per-UTC-day new-card cap (replace per-fetch)
- [x] **T-3** (2026-05-04 → 2026-05-04) Align `POST /sessions` `updatedSchedule` with `GET /decks/:id/schedule` filter semantics
