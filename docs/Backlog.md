# Backlog

Tracked work for this project. Items are added via `/bug`, `/todo`, `/idea` skills and closed via `/done`.

ID convention: `B-N` for bugs, `T-N` for todos, `I-N` for ideas. Counter is monotonic per section.

## Todo
<!-- next ID: T-2 -->

- [ ] **T-1** (2026-05-04) Server: enforce per-UTC-day new-card cap (replace per-fetch).
      Add `released_on TEXT` to `card_schedules` (or equivalent per-day release tracking);
      update `GET /decks/:id/schedule` to release at most `new_cards_per_day` `stage='new'`
      cards per UTC day and let leftovers carry over. Aligns server behavior with GDD §5 + §13
      design intent. See [server/README.md §Known limitations](../server/README.md).
      **Acceptance:** integration test in `server/server.test.js` proving the cap holds
      across two consecutive `GET /decks/:id/schedule` within the same UTC day; second test
      proving leftover from yesterday surfaces today plus today's fresh cards (≤ `new_cards_per_day` new).

- [ ] **T-2** (2026-05-04) Eliminate openapi.yaml ↔ zod schema duplication via `zod-to-openapi`. Currently `server/openapi.yaml` and `server/schemas.js` describe the same DTOs (CardReview, SessionResult, etc.) in parallel and can drift silently. Generate the OpenAPI spec from zod schemas as single source of truth; keep `openapi.yaml` as a generated artifact.

## Bugs
<!-- next ID: B-1 -->

## Ideas
<!-- next ID: I-1 -->

## Archive

- [x] **T-0** (2026-05-02 → 2026-05-02) Set up first system
