# Backlog

Tracked work for this project. Items are added via `/bug`, `/todo`, `/idea` skills and closed via `/done`.

ID convention: `B-N` for bugs, `T-N` for todos, `I-N` for ideas. Counter is monotonic per section.

## Todo
<!-- next ID: T-9 -->

- [ ] **T-2** (2026-05-04) Eliminate openapi.yaml ↔ zod schema duplication via `zod-to-openapi`. Currently `server/openapi.yaml` and `server/schemas.js` describe the same DTOs (CardReview, SessionResult, etc.) in parallel and can drift silently. Generate the OpenAPI spec from zod schemas as single source of truth; keep `openapi.yaml` as a generated artifact.
- [ ] **T-6** (2026-05-12) Progress label may exceed Total on ReviewGrade.Again — numerator (_session.ReviewsCompleted + 1) can exceed denominator (_session.Total, initial queue size) when user grades Again repeatedly because the same card re-enters the queue. Cosmetic only; denominator semantics are "initial unique cards", numerator is "card I'm currently looking at by order of appearance".

## Bugs
<!-- next ID: B-2 -->

## Ideas
<!-- next ID: I-2 -->

- [ ] **I-1** (2026-05-12) Visual canvas transition between Foyer and Review screens — currently the canvas swap (FoyerScreen.Hide() / ReviewScreen.Show() and reverse) is instantaneous SetActive flip. A fade or slide transition would improve perceived polish. Roadmap Phase 10 polish — no urgency.

## Archive

- [x] **T-0** (2026-05-02 → 2026-05-02) Set up first system
- [x] **T-1** (2026-05-04 → 2026-05-04) Server: enforce per-UTC-day new-card cap (replace per-fetch)
- [x] **T-3** (2026-05-04 → 2026-05-04) Align `POST /sessions` `updatedSchedule` with `GET /decks/:id/schedule` filter semantics
- [x] **B-1** (2026-05-04 → 2026-05-04) Daily new-card cap leak via POST without prior GET — server stamps `released_on=today` when grading unreleased `stage='new'` card
- [x] **T-8** (2026-05-12 → 2026-05-13) Specific handling of ScheduleStoreContractException in ReviewPresenter.RunGradeAsync — currently a 400/409 from server during GradeAsync falls into the generic catch (Exception), is logged, and the user is silently stuck on the back face with grade buttons hidden. Should catch ScheduleStoreContractException specifically, surface a friendly error to the user, and likely publish BackToFoyerRequested to recover. Source: Assets/Scripts/Presentation/Review/ReviewPresenter.cs:204.
- [x] **T-4** (2026-05-12 → 2026-05-14) Re-probe server reachability on BackToFoyerRequested in FoyerPresenter — currently the offline banner state stays frozen from initial scope start; if server availability changes during a review session, banner won't reflect it until app restart. TODO marker already present at Assets/Scripts/Presentation/Foyer/FoyerPresenter.cs:56.
- [x] **T-5** (2026-05-12 → 2026-05-14) Stale-flash of previous deck models on BackToFoyer — FoyerPresenter calls _screen.Show() immediately before RefreshAsync begins, so for ~50ms while the first I/O await is in flight, foyer canvas is visible with old deck stats before Bind(models) refreshes them. Acceptable today (symmetric with first-launch behavior), but could defer Show() or add a loading state.
- [x] **T-7** (2026-05-12 → 2026-05-14) Loading-state UX gap on session start — between ReviewPresenter calling _screen.Show() and _session.StartAsync completing, the review canvas is visible with deck name but empty card area (network round-trip to fetch schedule). Add a loading indicator or defer Show() until first card is ready. Source: Assets/Scripts/Presentation/Review/ReviewPresenter.cs RunOnDeckSelectedAsync.
- [x] **T-9** (2026-05-13 → 2026-05-15) fix: polish "All caught up" empty-state card visuals — currently the card layout looks broken (title/icon/counter areas left blank with only the bottom label visible). Needs a proper empty-state design instead of just hiding everything.
