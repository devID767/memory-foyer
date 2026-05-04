# Backlog

Tracked work for this project. Items are added via `/bug`, `/todo`, `/idea` skills and closed via `/done`.

ID convention: `B-N` for bugs, `T-N` for todos, `I-N` for ideas. Counter is monotonic per section.

## Todo
<!-- next ID: T-4 -->

- [ ] **T-2** (2026-05-04) Eliminate openapi.yaml ↔ zod schema duplication via `zod-to-openapi`. Currently `server/openapi.yaml` and `server/schemas.js` describe the same DTOs (CardReview, SessionResult, etc.) in parallel and can drift silently. Generate the OpenAPI spec from zod schemas as single source of truth; keep `openapi.yaml` as a generated artifact.

## Bugs
<!-- next ID: B-2 -->

## Ideas
<!-- next ID: I-1 -->

## Archive

- [x] **T-0** (2026-05-02 → 2026-05-02) Set up first system
- [x] **T-1** (2026-05-04 → 2026-05-04) Server: enforce per-UTC-day new-card cap (replace per-fetch)
- [x] **T-3** (2026-05-04 → 2026-05-04) Align `POST /sessions` `updatedSchedule` with `GET /decks/:id/schedule` filter semantics
- [x] **B-1** (2026-05-04 → 2026-05-04) Daily new-card cap leak via POST without prior GET — server stamps `released_on=today` when grading unreleased `stage='new'` card
