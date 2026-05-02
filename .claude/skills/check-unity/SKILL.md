---
name: check-unity
model: sonnet
description: Check Unity console for errors and warnings via MCP
effort: low
argument-hint: "[recompile | tests | all]"
allowed-tools: Read, Grep, Glob, mcp__mcp-unity__get_console_logs, mcp__mcp-unity__recompile_scripts, mcp__mcp-unity__run_tests
---

# /check-unity — Unity Console Check

Check Unity console: $ARGUMENTS

## Before Starting

Try calling `get_console_logs` first. If the MCP call fails or returns a connection error:
1. Tell the user: **"Unity MCP is not connected. Make sure Unity is open and the MCP server is running."**
2. Stop — do not attempt further MCP calls

## Modes

### Default (no argument) — errors + warnings
1. Get errors: `get_console_logs` with logType "error"
2. Get warnings: `get_console_logs` with logType "warning"
3. Summarize: total count, grouped by type

### `recompile` — recompile + check
1. Call `recompile_scripts` with returnWithLogs true
2. Report compilation result
3. If errors — show them grouped by file

### `tests` — run tests
1. Call `run_tests` with testMode "EditMode" and returnOnlyFailures true
2. Report pass/fail summary
3. For failures — show test name and reason

### `all` — everything
1. Recompile
2. Check errors/warnings
3. Run tests

## After Reporting Errors

If errors reference specific files (e.g. `NullReferenceException in PlayerController.cs:42`):
- Read the file and show the relevant code snippet around the error line
- This helps the user understand the problem without switching to the editor

## Output Format
```
Unity Console:
  Errors:   N
  Warnings: N

[Details if any]

[Code snippets for errors, if file paths are available]

Status: ALL CLEAR / ACTION REQUIRED
```
