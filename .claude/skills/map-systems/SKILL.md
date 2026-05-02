---
name: map-systems
description: Visualizes dependencies between systems
effort: medium
allowed-tools: Read, Grep, Glob
agent: architect
---

# /map-systems — System Dependency Map

## Process

1. **Scan** `Assets/Scripts/` — find all system folders / asmdefs
2. **Analyze dependencies** — for each system:
   - What interfaces does it depend on?
   - What does it inject via the DI container?
   - What events/signals (MessagePipe / C# events / UniRx) does it send/receive?
3. **Build map**:

```
[SystemA] ← [SystemB]
[SystemA] ← [SystemC]
[SystemB] → [SomeEvent] → [SystemD]
[SystemD] ← [SystemA, SystemB]
```

4. **Flag issues**:
   - Circular dependencies
   - Systems with too many dependencies (coupling)
   - Orphan systems (nothing depends on them, they depend on nothing)
   - Cross-layer violations (e.g. Domain reaching into UnityEngine, Application reaching into Infrastructure-only types) — only relevant if the project uses Layered architecture

## Rules
- Read-only — do not modify files
- Report in **Russian**
- Update the map every time this skill is called (don't cache old results)
