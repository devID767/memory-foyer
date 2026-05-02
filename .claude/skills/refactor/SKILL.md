---
name: refactor
description: Promotes prototype code to production quality — adds architecture, configs, tests
effort: high
argument-hint: "<path to prototype file or system name>"
allowed-tools: Read, Grep, Glob, Write, Edit, Bash, Agent, mcp__mcp-unity__recompile_scripts, mcp__mcp-unity__get_console_logs
---

# /refactor — Prototype → Production

Promote prototype code to production quality: $ARGUMENTS

## Process

### Step 1: Analyze the Prototype
- Read the prototype file(s) — look for `// TODO(cleanup)` markers
- Identify what the code does: mechanics, values, dependencies
- List all hardcoded values that should move to Config

### Step 2: Plan the Architecture
- Spawn the **architect** agent to plan the production structure following the project's layered pattern — see `docs/architecture.md`
- Place new files in the correct layer (Domain / Application / Infrastructure / Presentation / Composition)
- Add interfaces for testability where it matters
- Present the plan to the user for approval

### Step 3: Rewrite
- Spawn the **gameplay-programmer** agent with the architecture plan
- Move all hardcoded values to Config ScriptableObject (or other config mechanism the project uses)
- Apply proper namespacing (`MemoryFoyer.<Layer>.<Folder>`)
- Wire up VContainer registrations in `Assets/Scripts/Composition/*LifetimeScope.cs`
- Follow all project conventions (CLAUDE.md + rules)

### Step 4: Verify
- Spawn the **qa** agent to review the new code
- Ensure no prototype patterns remain (no `TODO(cleanup)`, no hardcoded values)
- Check convention compliance

### Step 5: Clean Up
- Ask the user whether to **delete** or **keep** the original prototype files
- Report all created/modified files
- List manual setup steps (SO assets, Installers/LifetimeScopes, scene setup)

## Rules
- Never delete prototype files without user approval
- All gameplay values must move from hardcoded → Config
- The refactored code must do exactly what the prototype did — no feature additions
