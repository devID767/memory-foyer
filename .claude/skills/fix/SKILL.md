---
name: fix
description: Structured bug investigation and fix — reads logs, finds root cause, fixes, verifies
argument-hint: "<bug description or error message>"
allowed-tools: Read, Grep, Glob, Write, Edit, Bash, mcp__mcp-unity__get_console_logs, mcp__mcp-unity__recompile_scripts
---

# /fix — Bug Investigation & Fix

Investigate and fix: $ARGUMENTS

## Process

### Step 1: Gather Evidence
- If bug description mentions an error — search for the exact message in code with Grep
- Try to get Unity console logs via MCP (`get_console_logs` with logType "error"). If MCP is unavailable — skip, rely on user description
- Search for relevant code: class names, method names, variable names from the error
- Read the file(s) where the issue likely lives

### Step 2: Identify Root Cause
- Read surrounding code for context — understand the full flow
- Check recent changes: `git diff` and `git log --oneline -10` for recently modified files
- Form a hypothesis about what's wrong and why
- Present the hypothesis to the user before fixing:
  - **Symptom**: what the bug looks like
  - **Root cause**: why it happens
  - **Proposed fix**: what you'd change

### Step 3: Fix
- Apply the minimal fix that addresses the root cause
- Do NOT refactor, clean up, or improve surrounding code
- Follow all project conventions

### Step 4: Verify
- If MCP available: call `recompile_scripts` to verify compilation
- If the fix is in testable logic: suggest a regression test (but don't write it unless asked)
- Review the fix against the original bug description — does it actually address the problem?

### Step 5: Report
- What was broken and why (one sentence)
- What was changed (file:line)
- What to test manually in Unity

## Rules
- **Minimal fix** — fix the bug, nothing else
- Always explain the root cause before applying the fix
- If the bug is in generated code (`PlayerInput_Actions.cs`) or Plugins — tell the user, don't modify
- If the root cause is unclear after investigation — say so honestly, suggest debugging steps
