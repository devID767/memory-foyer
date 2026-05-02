---
name: dev-story
description: Full development cycle — reads spec, plans architecture, writes code, runs tests
effort: high
argument-hint: "<feature description>"
allowed-tools: Read, Grep, Glob, Write, Edit, Bash, Agent, mcp__mcp-unity__recompile_scripts, mcp__mcp-unity__get_console_logs
---

# /dev-story — Full Development Cycle

Implement: $ARGUMENTS

## Process

### Step 1: Understand
- Read `CLAUDE.md` and `docs/architecture.md` to know the project's conventions
- Read any relevant docs in `docs/`
- Read existing code in `Assets/Scripts/` for patterns and dependencies
- Clarify any ambiguities with the user

### Step 2: Plan (architect agent)
- Spawn the **architect** agent to plan the implementation:
  - File structure (across Domain / Application / Infrastructure / Presentation / Composition layers), interfaces, DI registrations, data flow
  - Follow the project's layered pattern documented in `docs/architecture.md`
- Present the architecture plan to the user for approval
- Do NOT proceed to Step 3 until the user approves

### Step 3: Implement (gameplay-programmer agent)
- Spawn the **gameplay-programmer** agent with the approved architecture plan
- The agent writes code following all project conventions (CLAUDE.md + rules)
- Creates Config ScriptableObject if applicable
- Wires up DI registrations in the appropriate scope/installer

### Step 4: Verify
Two checks, done sequentially by the **orchestrator** (not delegated to a single agent):

**4a. Compilation check** (orchestrator, MCP):
- Try `recompile_scripts` via MCP. If MCP unavailable — skip, note in report
- If compilation errors — fix them before proceeding to 4b

**4b. Code review** (qa agent):
- Spawn the **qa** agent to review the implementation:
  - Convention compliance check
  - Hardcoded values detection
  - Unity safety checks
- Fix any issues found by QA before proceeding

### Step 5: Report
- List all created/modified files
- Summarize what was implemented
- QA verdict (pass/fail with details)
- **Manual steps required** — list explicitly:
  - ScriptableObject assets to create in Unity (path + values)
  - Installers/LifetimeScopes to add to scenes or root scope
  - Input Actions to configure
  - Any prefab/scene setup

## Rules
- Follow the architecture plan from Step 2 exactly
- All gameplay values from Config, never hardcode
- Ask user before making architecture or product decisions not covered by the spec
- Each agent runs independently — pass context explicitly in the agent prompt
