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

Strict nullable is enforced **per asmdef**, not project-wide. Unity applies a single `Assets/csc.rsp` to all predefined assemblies, including `Assembly-CSharp-firstpass` (where `Assets/Plugins/` lives) — so a global flag breaks third-party code we cannot edit. Setup:

1. Leave `Assets/csc.rsp` empty. Predefined assemblies and firstpass compile with defaults; plugins are unaffected. Do **not** add comments — Unity passes every non-empty line of `csc.rsp` straight to the compiler as an argument, so `# ...` lines become bogus source-file arguments and break the build (`error CS2001`).
2. For every asmdef we own (Domain, Application, Infrastructure, Presentation, Composition, Editor, Tests.EditMode), place a `csc.rsp` file next to the `.asmdef` containing:
   ```
   -nullable:enable
   -warnaserror+:nullable
   ```
3. Mark fields/properties/parameters that may be null with `?`: `string? name`, `Player? target`
4. Use null-forgiving `!` only when you've genuinely guaranteed non-null and the compiler can't see it (rare — usually means you should restructure)

Caveat: `_camelCase` MonoBehaviour fields populated by Unity (`[SerializeField]`) are technically nullable from the compiler's perspective even when set in Inspector. Either initialize with `= null!` (with a comment explaining "set in Inspector") or use a property that asserts non-null at access time.
