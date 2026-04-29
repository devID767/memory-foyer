---
paths:
  - "Assets/Scripts/**/*.cs"
---

# Code Style

- Update patterns: see architecture.md (`ITickable` for pure services, `Update`/`FixedUpdate` for MonoBehaviour)
- Async: see CLAUDE.md Critical Rules (UniTask preferred, coroutines for simple loops)
- Use delta time for all time-dependent calculations (`Time.deltaTime` in `Update`, `Time.fixedDeltaTime` in `FixedUpdate`)
- State machines should have clear transition logic (explicit table, enum + switch, or similar — pick what fits the complexity)
- No direct references to UI code from gameplay — use C# events, MessagePipe, or UniRx for cross-system communication
- Auto-properties with `[field: SerializeField]`: all other Unity PropertyAttributes (`[Header]`, `[Tooltip]`, `[Min]`, `[Range]`, etc.) on the same property must also use the `[field: ]` target specifier — otherwise Unity Inspector ignores them. Example: `[field: Header("Section")]` / `[field: Tooltip("...")]`

## Nullable reference types

Enable nullable reference types project-wide. Recommended setup:

1. Create `Assets/csc.rsp` with:
   ```
   -nullable:enable
   -warnaserror+:nullable
   ```
2. Mark fields/properties/parameters that may be null with `?`: `string? name`, `Player? target`
3. Use null-forgiving `!` only when you've genuinely guaranteed non-null and the compiler can't see it (rare — usually means you should restructure)

Caveat: `_camelCase` MonoBehaviour fields populated by Unity (`[SerializeField]`) are technically nullable from the compiler's perspective even when set in Inspector. Either initialize with `= null!` (with a comment explaining "set in Inspector") or use a property that asserts non-null at access time.
