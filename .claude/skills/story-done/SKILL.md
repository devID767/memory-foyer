---
name: story-done
model: sonnet
description: Checklist after agent/dev-story work — verifies everything is complete
effort: medium
argument-hint: "[system or feature name]"
allowed-tools: Read, Grep, Glob, Bash
agent: qa
---

# /story-done — Post-Agent Checklist

Verify completeness after agent work on: $ARGUMENTS

## Checklist

### Code
- [ ] All planned files created
- [ ] Code follows naming conventions (`_camelCase`, `PascalCase`, `MemoryFoyer.<Layer>.<Folder>`)
- [ ] Files placed in the correct layer (Domain / Application / Infrastructure / Presentation / Composition); Domain stays UnityEngine-free
- [ ] Allman braces, 4 spaces indentation
- [ ] No hardcoded gameplay values — all in Config
- [ ] Interfaces defined where the implementation crosses a layer boundary
- [ ] VContainer registrations added in `Assets/Scripts/Composition/*LifetimeScope.cs`

### Unity Safety
- [ ] No Find/FindObjectOfType at runtime
- [ ] GetComponent cached in Awake
- [ ] NonAlloc physics APIs (or List<T> overloads in Unity 6) used
- [ ] No ?. or ?? on Unity objects

### Tests
- [ ] Unit tests written for core logic (if applicable)
- [ ] Tests follow Arrange/Act/Assert
- [ ] Tests live under `Assets/Tests/EditMode/` (or PlayMode/ for integration)

### Documentation
- [ ] Code is self-documenting (no unexplained magic numbers)
- [ ] Public interfaces have xmldoc where behavior isn't obvious from the signature

### Manual Steps Required
- [ ] List any ScriptableObject assets to create
- [ ] List any Installers/LifetimeScopes to add to scene
- [ ] List any Input Actions to configure

## Output
Report with pass/fail for each item and a summary verdict.
