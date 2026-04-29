---
paths:
  - "Assets/Scripts/**/*.cs"
---

# Architecture — Project rules

This project uses a **Layered** architecture: Domain → Application → Infrastructure → Presentation, with a Composition root holding VContainer LifetimeScopes. Layer boundaries are enforced by .asmdef references; Domain has `noEngineReferences: true`. Full description: [docs/architecture.md](../../docs/architecture.md).

The principles below are the universal ones, with project-specific notes inline where relevant.

## Core principles

### Dependency injection
- VContainer is the DI container (chosen for Unity 6 native support)
- Constructor injection for plain C# classes
- `[Inject]` attribute injection for MonoBehaviours where unavoidable
- No service locators, no static singletons, no `Find()`/`FindObjectOfType()` for inter-system wiring

### Testability
- All Domain code is pure C# (no MonoBehaviour, no UnityEngine) — testable directly via NUnit
- Application code has no UnityEngine refs either; tests use fake implementations of Application interfaces
- `IClock` and `IRandomProvider` are mandatory in scheduling/balance code — never call `DateTime.UtcNow` or `UnityEngine.Random` directly. SM-2 is time-sensitive; flakiness from real time is exactly the failure mode this rule prevents.

### Configs over hardcoded values
- Gameplay values (speeds, cooldowns, damage, timings) belong in configs (ScriptableObject or JSON)
- Math constants (π, conversion factors) and buffer sizes can stay in code
- Configs are assigned in Inspector or loaded at composition root, not hardcoded paths

### Update patterns
- Pure logic without scene refs: implement DI container's tick interface (e.g. `ITickable`/`IFixedTickable`)
- Logic that needs Transform/Rigidbody: MonoBehaviour `Update`/`FixedUpdate` calling into a service
- For physics — always FixedUpdate or `IFixedTickable`

### Async
- Prefer UniTask over `System.Threading.Tasks.Task` for Unity-touching code
- Coroutines acceptable for simple controlled loops
- `System.Threading.Tasks.Task` only in pure C# code that doesn't touch Unity (editor tools, external integrations)

## Folder & namespace conventions

- One layer = one assembly definition. Sub-folders inside a layer share that layer's asmdef.
- Namespace mirrors folder path: `MemoryFoyer.Domain.Models`, `MemoryFoyer.Application.Sessions`, `MemoryFoyer.Infrastructure.Http`, etc.
- `MemoryFoyer.Domain.asmdef` sets `noEngineReferences: true` — this is the architectural guarantee, not a suggestion.

## Composition root

- `Assets/Scripts/Composition/ProjectLifetimeScope.cs` holds long-lived services (SM-2 algorithm, repositories, HTTP client, analytics, session service, MessagePipe brokers).
- `Assets/Scripts/Composition/FoyerLifetimeScope.cs` holds scene-specific bindings (presenters as `IAsyncStartable` entry points, MonoBehaviour view references).
- Cross-scope data passes through long-lived services, not through static fields or `DontDestroyOnLoad` singletons.
