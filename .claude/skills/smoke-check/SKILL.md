---
name: smoke-check
model: sonnet
description: Quick check after manual code changes — conventions, compilation, common issues
argument-hint: "[file path or 'all' for recent changes]"
allowed-tools: Read, Grep, Glob, Bash
agent: qa
---

# /smoke-check — Post-Manual-Work Check

Quick verification after manual code changes: $ARGUMENTS

## Process

1. **Find changes**: run `git diff` to see what changed (or read specific files if path given)
2. **Read changed files** in full for context
3. **Check conventions**:
   - Naming (_camelCase, PascalCase, namespaces)
   - Allman braces, indentation
   - No hardcoded gameplay values
4. **Check Unity safety**:
   - No Find/FindObjectOfType
   - GetComponent not in Update
   - CompareTag usage
   - Null handling on Unity objects
5. **Check critical rules**:
   - No System.Threading.Tasks.Task
   - No modifications to Plugins/ or TextMesh Pro/
6. **Report** — short summary:
   - Issues found (with file:line)
   - All clear (if nothing found)

## Rules
- Keep it fast — this is a smoke check, not a full review
- Only check changed files, not entire codebase
- Don't suggest refactors or improvements — only flag violations
