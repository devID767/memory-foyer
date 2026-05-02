---
name: prototype
description: Quick throwaway prototype to test game feel — ignores architecture standards
effort: medium
argument-hint: "<what to prototype>"
allowed-tools: Read, Grep, Glob, Write, Edit
---

# /prototype — Quick Prototype

Create a fast prototype for: $ARGUMENTS

**This is a speed-first task.** Architecture standards, interfaces, and configs are intentionally skipped. Do not ask clarifying questions about ambiguous design details — make a reasonable choice and move on. The goal is to test game feel, not build production code.

## Process

1. Read any relevant docs (CLAUDE.md, docs/architecture.md, or design notes) for values/mechanics — skim, don't memorize
2. Write a quick MonoBehaviour prototype — **architecture standards are relaxed**:
   - No need for Service/Config/Installer
   - Can hardcode values (but use comments with intended config values)
   - Can use MonoBehaviour directly with Update/FixedUpdate
   - Can skip interfaces
3. Place in `Assets/Scripts/Prototypes/` (create the folder if it doesn't exist)
4. Mark all prototype code with `// TODO(cleanup) — prototype, refactor before merge`

## Rules
- Goal is **speed** — test the feel, not build architecture
- Keep it minimal — only what's needed to test the idea
- Do NOT stop to ask about ambiguities — make a reasonable default and note it in a comment
- Always mark with TODO(cleanup)
- Tell the user what needs to happen to promote this to production code (or use `/refactor`)
