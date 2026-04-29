# Memory Foyer — Context for Claude Code

## Project Overview
Spaced-repetition trainer (SM-2) with a minimal 3D foyer for deck selection. Built as a portfolio project to demonstrate strict layered architecture in Unity 6.

## Tech Stack

- **Unity:** 6000.4.1f1 (Unity 6 LTS)
- **Render Pipeline:** URP
- **Color Space:** Linear
- **DI:** VContainer
- **Async:** UniTask
- **Events:** MessagePipe (in-process pub/sub, integrated with VContainer)
- **Tweening:** DOTween (free)
- **Camera:** Cinemachine
- **Input:** Unity Input System
- **Tests:** Unity Test Framework (NUnit)

## Backend

Node.js + Express mock in `server/server.js`. In-memory state, two endpoints:
- `GET /decks/:id/stats` — returns `{ deckId, dueCount, totalCount }`
- `POST /sessions` — accepts `SessionResultDto`, decrements `dueCount`

Single file, no build step. Run with `cd server && node server.js`.

## Project Structure

```
.claude/                # Agent configs, skills, rules, hooks
.vscode/                # Unity-owned VS Code config (cherry-picked extras)
docs/                   # All project documentation
  architecture.md       # Layered architecture reference
  Backlog.md            # Workflow: bugs / todos / ideas (managed via /backlog skills)
  Roadmap.md            # Workflow: project phases (managed via /roadmap skills)
server/                 # Node.js + Express mock backend
Assets/
  Plugins/              # Third-party — DO NOT MODIFY
  Resources/Data/       # ScriptableObject configs (created via Unity Inspector)
  Scenes/               # Foyer.unity
  Scripts/
    Domain/             # Pure C#, no UnityEngine — Card, Deck, Sm2Algorithm, IClock
    Application/        # Use cases — IReviewSessionService, repositories interfaces
    Infrastructure/     # I/O — HTTP client, ScriptableObject deck repo, analytics
    Presentation/       # Unity-facing — Foyer scene presenters, Review overlay
    Composition/        # VContainer LifetimeScopes (composition root)
  Editor/               # Editor tools (Deck Author UI Toolkit window)
  Tests/EditMode/       # Logic tests (NUnit)
  ScriptableObjects/    # Runtime data assets (decks)
  Prefabs/              # Pedestal.prefab etc.
  Settings/             # URP render pipeline settings
Packages/
ProjectSettings/
```

## Architecture

Layered: Domain → Application → Infrastructure → Presentation, with a Composition root.
Strict separation enforced via assembly definitions; Domain has `noEngineReferences: true`.
Time and randomness live behind `IClock` / `IRandomProvider` interfaces — never call `DateTime.UtcNow` or `UnityEngine.Random` from scheduling code.

Full detail: see [docs/architecture.md](docs/architecture.md).

## Code Conventions

### Naming
- **Private fields:** `_camelCase` with underscore prefix (`[SerializeField] private float _speed`)
- **Inspector fields:** prefer `[SerializeField] private` over `public`
- **Public fields/properties:** `PascalCase`
- **Interfaces:** `I` prefix (`IDeckRepository`)
- **Namespaces:** mirror folder path — `MemoryFoyer.Domain.Models`, `MemoryFoyer.Application.Sessions`, `MemoryFoyer.Infrastructure.Http`, etc.

### C# Style
- Allman braces (opening brace on new line)
- 4 spaces indentation
- `csharp_prefer_braces = true` — always use braces even for single-line if/for
- `var` only when type is obvious from right-hand side; never for built-in types
- No `this.` prefix
- Nullable reference types enabled (see `.claude/rules/code-style.md`)
- One class per file; file name == class name
- Properties (not public fields) on models; records for DTOs and value-objects

### DI (VContainer)
- Constructor injection for plain C# classes
- Attribute injection (`[Inject]`) for MonoBehaviour where unavoidable
- All registrations live in `Assets/Scripts/Composition/*LifetimeScope.cs`

## Critical Rules
- **Domain layer has zero `UnityEngine` references.** The .asmdef enforces this — adding `using UnityEngine;` to any file under `Assets/Scripts/Domain/` will fail to compile.
- **Prefer UniTask** for async operations. `System.Threading.Tasks.Task` acceptable in pure-Domain code that doesn't touch Unity. Coroutines acceptable for simple controlled loops.
- **DO NOT modify** `Assets/Plugins/`, `Assets/Art/Plugins/` or `Assets/TextMesh Pro/`
- **DO NOT modify** auto-generated input action C# files
- **Configs** assignable in Inspector — never construct ScriptableObject instances at runtime for gameplay values
- **Pre-commit hook** checks open backlog items vs. staged code changes — see `.claude/hooks/pre-commit-check.sh`

## Git Workflow

- **Conventional Commits:** `feat:` / `fix:` / `test:` / `refactor:` / `perf:` / `docs:` / `style:` / `chore:` / `build:` / `ci:`
- **Atomic commits:** one commit = one logical change from the project plan
- **English** commit messages, lowercase imperative subject, max 72 chars
- **Branch:** `main` (single branch, no merge commits, no squash)

## Target Platforms

- Mac Standalone (Apple Silicon) — primary development target
- Windows Standalone x64 — compatibility target for screenshots/video

iOS / Android / WebGL are explicitly out of scope.
