# Roadmap

High-level project phases, mirroring the execution plan in `memory-foyer-plan.md` §4. The phase marked `[CURRENT]` is the active focus. Sub-task IDs (e.g. `0.3`) reference the same numbering as the plan — a sub-task closes when its commit lands.

## Phase 0: Project scaffolding [CURRENT]

- [x] **0.1** Create GitHub repo & local structure _(done — repo initialized, structure in place)_
- [x] **0.2** Create Unity 6 URP project _(done in commit 0d89b19)_
- [x] **0.3** Install Unity packages (VContainer, UniTask, MessagePipe, DOTween, Cinemachine) _(+ MessagePipe.VContainer integration)_
- [x] **0.4** Create folder structure & assembly definitions for the five layers _(2026-05-01)_
- [ ] **0.5** Wire up Node.js Express server skeleton _(done — endpoints come in Phase 3.5)_
- [ ] **0.6** README skeleton at repo root

## Phase 1: Domain layer & SM-2 algorithm

- [ ] **1.1** `Card`, `Deck` models + `CardId`, `DeckId` record structs
- [ ] **1.2** `ReviewGrade` enum (Again=0, Hard=3, Good=4, Easy=5)
- [ ] **1.3** `Sm2State` value object
- [ ] **1.4** `IClock` interface + `SystemClock` implementation
- [ ] **1.5** `Sm2Algorithm.Schedule(state, grade, reviewedAt)` — pure SM-2
- [ ] **1.6** EditMode test asmdef setup
- [ ] **1.7** SM-2 tests: NewCard & first review (~9 cases)
- [ ] **1.8** SM-2 tests: subsequent reviews & EF bounds (~5 cases)
- [ ] **1.9** SM-2 tests: failure & reset paths (~4 cases)

Tag: `v0.1-domain`.

## Phase 2: Application layer

- [ ] **2.1** `IDeckRepository` interface
- [ ] **2.2** `ISessionResultsUploader` + `SessionResult` / `CardReview` records
- [ ] **2.3** `ISessionStatsProvider` + `DeckStats`
- [ ] **2.4** `IAnalyticsService`
- [ ] **2.5** `IReviewSessionService` interface + `SessionState` enum
- [ ] **2.6** `ReviewSessionService` implementation
- [ ] **2.7** Session events (`SessionStartedEvent`, `CardReviewedEvent`, `SessionFinishedEvent`)
- [ ] **2.8** `ReviewSessionService` tests with fakes (~7 cases)

Tag: `v0.2-application`.

## Phase 3: Infrastructure layer

- [ ] **3.1** DTOs (`DeckStatsDto`, `SessionResultDto`, `CardReviewDto`)
- [ ] **3.2** Domain ↔ DTO mappers
- [ ] **3.3** `IHttpClient` (in Application) + `UnityWebRequestHttpClient` (in Infrastructure)
- [ ] **3.4** `HttpSessionResultsUploader`, `HttpSessionStatsProvider`
- [ ] **3.5** `DeckScriptableObject` + `ScriptableObjectDeckRepository`
- [ ] **3.6** `ConsoleAnalyticsService`, `NoOpAnalyticsService`
- [ ] **3.7** Mapper tests (~4 cases)

Tag: `v0.3-infrastructure`.

## Phase 3.5: Backend mock endpoints

- [ ] **3.5.1** `GET /decks/:id/stats` with three default decks
- [ ] **3.5.2** `POST /sessions` (decrements due-count)
- [ ] **3.5.3** `server/README.md` with run + curl examples

## Phase 4: VContainer wiring

- [ ] **4.1** `ProjectLifetimeScope` with all bindings + MessagePipe registration
- [ ] **4.2** `FoyerLifetimeScope` (initially empty, populated in Phase 5/6)
- [ ] **4.3** Composition smoke-test `IStartable`

## Phase 5: Foyer 3D scene

- [ ] **5.1** Scene setup (floor, walls, lighting)
- [ ] **5.2** URP post-processing (bloom, vignette)
- [ ] **5.3** `Pedestal.prefab`
- [ ] **5.4** `PedestalView` MonoBehaviour (hover + click via `IPointerHandler`)
- [ ] **5.5** Three pedestals placed + DI binding via `FoyerPresenter`
- [ ] **5.6** Cinemachine virtual camera with slow orbit
- [ ] **5.7** EventSystem + PhysicsRaycaster sanity check

Tag: `v0.4-foyer-mvp`.

## Phase 6: Review UI overlay

- [ ] **6.1** Canvas layout with card display + 4 grade buttons
- [ ] **6.2** `ReviewView` MonoBehaviour
- [ ] **6.3** `ReviewPresenter` wired to `IReviewSessionService`
- [ ] **6.4** Register both presenters in `FoyerLifetimeScope`
- [ ] **6.5** Pedestal stats refresh on `SessionFinishedEvent`
- [ ] **6.6** End-to-end smoke (server + Unity Play)

Tag: `v0.5-mvp`.

## Phase 7: Editor tool (UI Toolkit)

- [ ] **7.1** `DeckAuthorWindow` scaffold (`Tools → Memory Foyer → Deck Author`)
- [ ] **7.2** Deck list pane (ListView from `AssetDatabase.FindAssets`)
- [ ] **7.3** Card editor pane
- [ ] **7.4** CSV import button _(stretch)_

## Phase 8: README, GIFs, polish

- [ ] **8.1** Capture `docs/foyer.gif` and `docs/review.gif`
- [x] **8.2** Architecture mermaid diagram in `docs/architecture.md`
- [ ] **8.3** Full README with highlights, trade-offs, what's next
- [ ] **8.4** Repo settings (description, topics, pin)

Tag: `v1.0`.

## Phase 9: GitHub Actions CI _(stretch)_

- [ ] **9.1** `.github/workflows/tests.yml` with `game-ci/unity-test-runner`
- [ ] **9.2** Unity license activation flow
- [ ] **9.3** README badge

## Phase 10: Polish _(stretch)_

- [ ] **10.1** DOTween animations (card flip, pedestal scale punch)
- [ ] **10.2** Graphy in development builds only
- [ ] **10.3** HDRI skybox from Polyhaven
