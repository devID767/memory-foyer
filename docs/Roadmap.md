# Roadmap

High-level project phases. The phase marked `[CURRENT]` is the active focus. Sub-task IDs (e.g. `0.3`) are atomic units of work — a sub-task closes when its commit lands.

## Phase 0: Project scaffolding

- [x] **0.1** Create GitHub repo & local structure _(done — repo initialized, structure in place)_
- [x] **0.2** Create Unity 6 URP project _(done in commit 0d89b19)_
- [x] **0.3** Install Unity packages (VContainer, UniTask, MessagePipe, DOTween, Cinemachine) _(+ MessagePipe.VContainer integration)_
- [x] **0.4** Create folder structure & assembly definitions for the five layers _(2026-05-01)_
- [x] **0.5** Wire up Node.js Express server skeleton _(endpoints come in Phase 3.5)_
- [x] **0.6** README skeleton at repo root

## Phase 0.7: Documentation reconciliation

Single pass to align GDD / architecture.md / Roadmap.md / CLAUDE.md after the persistence/backend redesign locked in `IScheduleStore` + authoritative server.

- [x] **0.7.1** Sweep 9 doc inconsistencies (event names, `ISessionUploader` vs `ISessionResultsUploader`, `DeckAsset` vs `DeckScriptableObject`, stray `IRandomProvider`, `Resources/Data` vs `ScriptableObjects/`, stale `memory-foyer-plan.md` reference, Phase 1.2 date format, commit-hash check, Card/Deck-as-records confirmation) _(2026-05-02)_
- [x] **0.7.2** Rewrite GDD §8 (5 endpoints, SQLite, idempotency) _(2026-05-02)_
- [x] **0.7.3** Rewrite GDD §12 Architectural Mapping (replace uploader/stats with `IScheduleStore` triple; canon event names) _(2026-05-02)_
- [x] **0.7.4** Rewrite GDD §15 Limitations (authoritative server, degraded offline, last-write-wins) _(2026-05-02)_
- [x] **0.7.5** GDD §4.5 — drop `IRandomProvider`, keep only `IClock` _(2026-05-02)_
- [x] **0.7.6** architecture.md — Composition root + Project-specific conventions _(2026-05-02)_
- [x] **0.7.7** CLAUDE.md — drop `Resources/Data/`, drop `IRandomProvider`, refresh Backend section _(2026-05-02)_
- [x] **0.7.8** Roadmap.md — Phase 2/3/3.5 restructure (this file) _(2026-05-02)_

Tag: `v0.0.7-docs`.

## Phase 1: Domain layer & SM-2 algorithm [CURRENT]

- [x] **1.1** `Card`, `Deck` models + `CardId`, `DeckId` record structs _(2026-05-01)_
- [x] **1.2** `ReviewGrade` enum (Again=0, Hard=3, Good=4, Easy=5) _(2026-05-01)_
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
- [ ] **2.2** `IScheduleStore` interface + `SessionResult` / `CardReview` records (`Application/Persistence/`)
- [ ] **2.3** `ServerConfig` record (`Application/Configuration/`)
- [ ] **2.4** `CachingScheduleStore` composite (HTTP primary + JSON cache fallback)
- [ ] **2.5** `IAnalyticsService`
- [ ] **2.6** `IReviewSessionService` interface + `SessionState` enum
- [ ] **2.7** `ReviewSessionService` implementation (depends on `IScheduleStore`)
- [ ] **2.8** Session events (`SessionStartedEvent`, `CardReviewedEvent`, `SessionFinishedEvent`, `DeckSelectedEvent`)
- [ ] **2.9** `ReviewSessionService` tests with fakes (~7 cases)

Tag: `v0.2-application`.

## Phase 3: Infrastructure layer

- [ ] **3.1** Schedule DTOs (`Sm2StateDto`, `CardScheduleDto`, `DeckScheduleDto`, `SessionResultDto`, `CardReviewDto`)
- [ ] **3.2** Domain ↔ DTO mappers (`Infrastructure/Dtos/ScheduleMappers.cs`)
- [ ] **3.3** `IHttpClient` (Application) + `UnityWebRequestHttpClient` (Infrastructure)
- [ ] **3.4** `HttpScheduleStore` (`Infrastructure/Persistence/`)
- [ ] **3.5** `JsonFileScheduleCache` (`Infrastructure/Persistence/`, `Application.persistentDataPath`)
- [ ] **3.6** `ServerConfigAsset` ScriptableObject + `Assets/Resources/Config/ServerConfig.asset`
- [ ] **3.7** `DeckAsset` + `ScriptableObjectDeckRepository`
- [ ] **3.8** `ConsoleAnalyticsService`, `NoOpAnalyticsService`
- [ ] **3.9** Mapper tests (~6 cases)

Tag: `v0.3-infrastructure`.

## Phase 3.5: Backend (authoritative, local)

May proceed in parallel with Phase 1 — no shared files with the Unity client.

- [ ] **3.5.1** `server/package.json` + deps (`express`, `better-sqlite3`, `zod`; dev: `vitest` or `node:test`)
- [ ] **3.5.2** `server/schema.sql` + migration loader in `server.js`
- [ ] **3.5.3** `server/seed.js` — three decks from GDD §7
- [ ] **3.5.4** `server/sm2.js` (port of GDD §4) + `server/sm2.test.js`
- [ ] **3.5.5** `server/server.js` — Express scaffold, `zod` validation, error middleware
- [ ] **3.5.6** `GET /health`, `GET /decks`, `GET /decks/:id/schedule`
- [ ] **3.5.7** `POST /sessions` (server-side SM-2 + idempotency on `sessionId`)
- [ ] **3.5.8** `GET /sessions/:id`
- [ ] **3.5.9** `server/server.test.js` — integration tests (~7 cases)
- [ ] **3.5.10** `server/openapi.yaml`
- [ ] **3.5.11** `server/README.md` — run, curl, schema, why-authoritative

Tag: `v0.3.5-backend`.

## Phase 4: VContainer wiring

- [ ] **4.1** `ProjectLifetimeScope` with all bindings (incl. `IScheduleStore` triple, `ServerConfig`) + MessagePipe registration
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
- [ ] **6.6** End-to-end smoke (server + Unity Play; verify offline-cache fallback by stopping server mid-session)

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
