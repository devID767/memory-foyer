# Game Design Document — Memory Foyer

## 1. Overview

**Memory Foyer** is a single-player desktop spaced-repetition trainer. The user opens a calm 3D-rendered library scene, picks a deck from an overlay menu, reviews cards that are due today, and grades each one. Schedules are updated using a modified SM-2 algorithm with learning steps, a soft-lapse policy, and a daily new-card limit.

It is built as a portfolio piece: the visible game is intentionally small so the architecture can be the showcase.

- **Genre:** productivity / educational micro-game
- **Platforms:** macOS Apple Silicon (primary), Windows x64 (compatibility)
- **Session length:** 2–10 minutes per day
- **Players:** single, local

## 2. Goals & Success Criteria

**Player goals**
- Learn or retain factual material with minimum daily time.
- Calm, low-friction surface — no streaks, no notifications, no gamification.

**Project (portfolio) goals — both must be met:**
- **Craft signal.** 100% test coverage of `Domain`, 0 compiler warnings, no engine references in `Domain`, all DI registrations in `Composition`, no third-party plugin modifications.
- **Visible artifact.** Public repo with a readable README, a 30-second demo video, and a screenshot suitable for HH/LinkedIn.

**Non-goals for v1:** retention mechanics, multi-user, mobile/web, persistence beyond mock backend, real analytics, custom shaders, particle systems.

## 3. Core Loop

```
Open app
  ↓
3D backdrop loads → slow Cinemachine dolly through library scene
Canvas overlay shows 3 deck buttons, each: name + "N due / M total"
  ↓
Player clicks a deck
  ↓
Review overlay opens (foyer 3D dims behind it)
  ↓
For each due card:
    Show front (TMP rich text) → tap/Space to flip → back appears
    Player grades: Again / Hard / Good / Easy (mouse or keys 1–4)
    SM-2 reschedules (with learning steps and soft lapse)
  ↓
Session ends (queue empty or Esc)
  ↓
SessionResultDto POSTed with sessionId; results upload (or queue if offline)
Backdrop returns; deck buttons refresh stats from /decks/:id/stats
  ↓
Player picks another deck or quits
```

## 4. Mechanics — Modified SM-2

The algorithm lives in `Domain/Scheduling/Sm2Algorithm.cs`. **Pure C#, zero `UnityEngine` references, fully unit-tested.**

### 4.1 Card State

- `Repetitions` (int, ≥ 0)
- `EaseFactor` (float, ≥ 1.3)
- `IntervalDays` (int, 0–365)
- `DueAt` (UTC)
- `LearningStage` (enum: `New`, `Learning`, `Review`)
- `LearningStepIndex` (int, used only when stage is `Learning`)

**New card initial state:** `Stage=New`, `Reps=0`, `EF=2.5`, `Interval=0`, `DueAt=now`.

### 4.2 Grades

| Button | Key | Grade | Meaning |
|---|---|---|---|
| Again | 1 | 0 | Forgot |
| Hard | 2 | 3 | Recalled with effort |
| Good | 3 | 4 | Recalled correctly |
| Easy | 4 | 5 | Trivial |

### 4.3 Learning Stage (new + lapsed cards)

Two steps before a card enters `Review` long-term schedule:

| Step | Interval after Good/Easy | On Again |
|---|---|---|
| 0 | 10 minutes → step 1 | Stay at step 0, reschedule +10 min |
| 1 | 1 day → graduate to `Review` (Interval=1, Reps=1) | Drop to step 0 |

Easy on any learning step graduates to `Review` immediately with `Interval=4, Reps=1`.

### 4.4 Review Stage (graduated cards)

**On Hard / Good / Easy (grade ≥ 3):**
- `Repetitions += 1`
- Next interval (clamped to ≤ 365 days):
  - `Reps == 1` → 1 day
  - `Reps == 2` → 6 days
  - else → `round(previousInterval × EaseFactor)`
- `EaseFactor := EaseFactor + (0.1 − (5 − grade) × (0.08 + (5 − grade) × 0.02))`, clamped to ≥ 1.3
- `DueAt := reviewedAt + IntervalDays`

**On Again (grade 0) — soft lapse:**
- `IntervalDays := max(1, round(previousInterval × 0.5))`, clamped to ≤ 365
- `EaseFactor := max(1.3, EaseFactor − 0.20)`
- `Repetitions` is **not** reset
- `Stage := Learning`, `LearningStepIndex := 0` — card re-enters learning queue
- `DueAt := reviewedAt + 10 minutes`

### 4.5 Determinism

- Time flows through `IClock`; randomness through `IRandomProvider`.
- The algorithm is a pure function: `(CardState, Grade, Now) → CardState`.
- All operations are unit-tested across ~25 cases including learning, graduation, lapse-from-long-interval, EF floor, interval cap.

## 5. Session Rules

- A session pulls cards from the chosen deck where `DueAt ≤ now`, in deterministic order: oldest `DueAt` first, ties broken by stable card id.
- **New-card budget:** at most `NewCardsPerDay` (default 10, per-deck) cards with `Stage == New` enter today's queue. The rest stay hidden.
- A card graded **Again** is re-queued at the end of the **current session** in addition to its scheduled `DueAt`. If the player grades it Again twice in one session, the second grading still applies SM-2 and re-queues — there is no per-session cap.
- Session ends when the queue is empty or the player presses Esc.
- On end, a `SessionResultDto` is sent: `{ sessionId, deckId, reviews: [{ cardId, grade, reviewedAt }, ...] }`. `sessionId` is a client-generated GUID set when the session starts. The server uses it to dedup retries.
- **Quit mid-session:** results so far are uploaded silently — no confirm dialog, no "discard" path.

## 6. Scenes & UI

### 6.1 Foyer (3D backdrop)

A small library scene rendered in URP: bookshelf, desk lamp, dust motes, warm directional light (~2700 K), gentle bloom + vignette, slight color grading. Primitives + free assets only.

**Camera:** one Cinemachine virtual camera with a slow dolly path (~6 s loop) — cinematic, never interactive.

**No interactive 3D objects** — the backdrop exists for atmosphere and to justify URP/Cinemachine in the stack.

### 6.2 Deck Selection (2D overlay over backdrop)

A `Canvas` (Screen Space – Overlay) in front of the rendered backdrop:

- Title: "Memory Foyer".
- Three deck buttons stacked vertically. Each button: deck name + "N due / M total".
- Empty deck (`due == 0`): button is disabled, sub-label reads **"All caught up"**.
- Offline indicator: thin top banner "Server offline — stats may be stale" when last `/decks/:id/stats` call failed.

### 6.3 Review Overlay (2D, dims foyer behind)

Single Canvas, screen-space overlay:
- Top: deck name + progress (`3 / 12`).
- Center: card front (TextMeshPro, supports built-in `<b>`, `<i>`, `<color>` tags). Tap or Space → DOTween flip → back.
- Bottom: four buttons — Again / Hard / Good / Easy — also bound to keys 1–4.
- Esc closes the session.
- Backdrop fades from full to ~30% brightness while overlay is open.

## 7. Content — Decks

Decks are `ScriptableObject` assets under `Assets/ScriptableObjects/Decks/`.

```csharp
[CreateAssetMenu(...)] class DeckAsset : ScriptableObject
{
    public string DeckId;          // stable, matches server hardcode
    public string DisplayName;
    public string Description;
    public int    NewCardsPerDay;  // default 10
    public CardData[] Cards;
}

[Serializable] class CardData { public string CardId; public string Front; public string Back; }
```

**Three seed decks ship in the build:**
1. **Capitals of Europe** — 44 cards, `NewCardsPerDay = 10`
2. **Periodic Table 1–20** — 20 cards, `NewCardsPerDay = 5`
3. **English Idioms** — 30 cards, `NewCardsPerDay = 8`

Authoring is via a custom Editor window (`Editor/DeckAuthor/DeckAuthorWindow.cs`, UI Toolkit). CSV import is a Phase 7 stretch.

## 8. Backend (Mock)

Node.js + Express, single file `server/server.js`.

- **State:** in-memory Map, mirrored to `server/data.json` on every mutation. On boot, file is loaded if present.
- **Idempotency:** server keeps a `Set<sessionId>` of processed session uploads. Duplicates return `200 { ok: true, dedup: true }` without mutating.

| Method | Path | Purpose |
|---|---|---|
| GET | `/decks/:id/stats` | `{ deckId, dueCount, totalCount }` |
| POST | `/sessions` | Accepts `{ sessionId, deckId, reviews }`; updates `dueCount`; returns `{ ok: true, dedup?: bool }` |

The Unity client uses `UnityWebRequestHttpClient` wrapped with UniTask. The mock is intentionally dumb — its purpose is to prove the wire works, not to be a real service.

## 9. Copy & UI Text

All UI strings collected here so tone stays consistent. Tone: terse, calm, no exclamation marks.

**Foyer / deck selection**
- Header: `Memory Foyer`
- Deck button stat (has due cards): `{N} due · {M} total`
- Deck button stat (caught up): `All caught up`
- Empty state — all three decks caught up: `All decks caught up. See you tomorrow.`
- Offline banner (top of foyer): `Server offline — stats may be stale`
- Server unreachable on first load: `Couldn't reach server. Click to retry.`

**Review overlay**
- Progress: `{current} / {total}`
- Flip prompt (faint, under card): `Tap or press Space`
- Grade buttons: `Again`, `Hard`, `Good`, `Easy`
- Key hints under each button: `1`, `2`, `3`, `4`
- Quit hint (small, top-right): `Esc`

**Session end (brief overlay before returning to foyer)**
- Line 1: `{N} reviewed`
- Line 2: `See you tomorrow.`

**Card content**
- Front and Back are author-supplied via `DeckAsset`. TMP rich text supported (`<b>`, `<i>`, `<color>`).

**Intentionally absent**
- Streaks, daily goals, XP, badges.
- Tutorials, tooltips, onboarding modals.
- Confirmation dialogs.
- "Loading…" spinners (operations are too fast to need them).

## 10. Visual Style

- **Palette:** library-at-night — dark walnut floor, off-white walls, warm key light (~2700 K).
- **Motion:** all UI transitions use DOTween. No instant snaps.
- **Audio:** none — no music, no SFX. Decision: avoid ruining first impression on muted reviewers.
- **Typography:** TextMeshPro, one serif for card content, one sans-serif for UI.

## 11. Accessibility

- Grades are color-independent: each button shows text label.
- Full keyboard support: 1–4 to grade, Space to flip, Esc to quit.
- Default UI scale aimed at 1080p readability without zoom.

## 12. Architectural Mapping

A cross-reference between mechanics in this document and the layer that owns them. Doubles as an interview talking point: *"X lives in Domain because it has no Unity dependency."*

| Concern | Layer | Owning file |
|---|---|---|
| SM-2 algorithm (pure function) | Domain | `Domain/Scheduling/Sm2Algorithm.cs` |
| Card / Deck records | Domain | `Domain/Models/Card.cs`, `Deck.cs` |
| `IClock`, `IRandomProvider` | Domain | `Domain/Time/IClock.cs`, `Domain/Random/IRandomProvider.cs` |
| Session orchestration / state machine | Application | `Application/Sessions/ReviewSessionService.cs` |
| New-cards-per-day budget logic | Application | `Application/Sessions/NewCardBudget.cs` |
| Result DTO assembly | Application | `Application/Sessions/SessionResultBuilder.cs` |
| Repository / uploader interfaces | Application | `Application/Repositories/IDeckRepository.cs`, `Application/Sessions/ISessionUploader.cs` |
| HTTP client (UnityWebRequest + UniTask) | Infrastructure | `Infrastructure/Http/UnityWebRequestHttpClient.cs` |
| Stats fetch / session upload | Infrastructure | `Infrastructure/Http/HttpStatsClient.cs`, `HttpSessionUploader.cs` |
| ScriptableObject deck loading | Infrastructure | `Infrastructure/Repositories/ScriptableObjectDeckRepository.cs` |
| Foyer scene + Cinemachine vcam | Presentation | `Presentation/Foyer/FoyerView.cs` |
| Deck-selection Canvas | Presentation | `Presentation/Foyer/DeckSelectionView.cs` |
| Review overlay UI + DOTween | Presentation | `Presentation/Review/ReviewView.cs` |
| Offline banner | Presentation | `Presentation/Foyer/OfflineBannerView.cs` |
| DI registrations | Composition | `Composition/ProjectLifetimeScope.cs`, `FoyerLifetimeScope.cs` |
| MessagePipe events on the bus | App → Pres | `DeckSelectedEvent`, `SessionCompletedEvent`, `CardGradedEvent` |

**Boundary checks (enforced by `.asmdef`):**
- `Domain` does not reference `UnityEngine` (asmdef `noEngineReferences: true`).
- `Application` does not reference `UnityEngine` — no `MonoBehaviour`, no `UnityEngine.Time`, no `UnityEngine.Random`.
- `Presentation` does not reference `Infrastructure` directly — it consumes `Application` interfaces.
- `Composition` is the only assembly referencing all five layers.

## 13. User Journey — Day 1, 7, 30

This section makes the SR effect concrete. All times assume a 10-card daily new budget on the Capitals deck.

- **Day 1.** Player opens the app for the first time. Capitals deck shows "10 due · 44 total" (new-card budget caps the 44). Player reviews all 10. Two are graded Again — they re-appear at end of session, then schedule for 10 min from now (which in practice is end of session). After session: queue is empty, all graded. Player closes app.
- **Day 2.** First 10 are due (Reps=1 → 1 day intervals). 10 new cards are released. Total: 20 due. Player reviews. Some Goods, some Hards, two Agains.
- **Day 7.** Mix: Reps=1 cards from Day 6 (1d interval), Reps=2 cards from Day 1 (6d interval), plus 10 new. ~30 due. Some cards are deep into long intervals already.
- **Day 30.** Most original cards are at Reps≥3 with 15–60 day intervals. Daily load is small (5–15 cards) — the SR effect is paying off. New cards trickle in until deck is exhausted (~Day 5 for 44-card deck with budget of 10).

This journey is also the **demo video script:** advance the `IClock` between recordings to capture all four states in 30 seconds.

## 14. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| 2-day budget overrun on Phase 5 (foyer) | Medium | High | Budget capped at 1.5 h. If exceeded, fall back to plain Canvas (no 3D backdrop). |
| VContainer / UniTask / MessagePipe version conflicts on Unity 6 | Low | High | Pin versions in `manifest.json`; verify in Phase 0 before any logic code. |
| Soft-lapse + learning-steps add complexity → tests blow out | Medium | Medium | Algorithm under `Domain/`; if Phase 1 (algorithm) > 4 h, drop learning steps and document. |
| URP post-FX makes backdrop look "off" on Windows | Low | Medium | Use only stock Volume profiles; no custom render passes. |
| Server JSON file grows / corrupts | Low | Low | File ignored in git; on parse failure server starts fresh and logs warning. |

## 15. Limitations (Honest)

- **Persistence is local to the dev machine.** `server/data.json` lives next to the running server. There is no cloud sync, no multi-device. If the file is deleted, all SR history is lost.
- **No retention mechanics.** No notifications, streaks, goals — by design. The app is a tool, not a service.
- **Mock backend is "dumb."** Server trusts client-computed schedules; in a real system the algorithm would run server-side and validate.
- **Three fixed decks.** Adding a fourth requires Inspector work + a server-side hardcoded entry.
- **No localization.** UI is English-only.
- **Mid-session force-quit during offline mode loses ungraded reviews.** Pending grades live in client memory until upload — closing the app before the server is reachable drops them. Acceptable scope for the portfolio; in production this would need a local write-ahead log.

## 16. Open Design Questions

Resolved during planning. Section retained intentionally small to flag what is **not** decided:

1. CSV import format for `DeckAuthorWindow` (Phase 7 stretch — defer).
2. Specific bookshelf/lamp art assets for foyer backdrop (decide during Phase 5 with whatever is on the Unity Asset Store under permissive license).
3. Demo video shot list (lock during Phase 8).
