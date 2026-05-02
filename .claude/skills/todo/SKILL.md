---
name: todo
model: haiku
description: Add a planned task to Backlog.md (Todo section)
effort: low
argument-hint: "[task description]"
allowed-tools: Read, Edit, Write
---

# /todo — add a task to backlog

Appends an entry to `docs/Backlog.md`, section `## Todo`.

Description: $ARGUMENTS

## Process

1. If `$ARGUMENTS` is empty, ask the user one question: "Describe the task".
2. Read `docs/Backlog.md`.
   - If the file is missing, create it with a template (heading `# Backlog`, version `1.0`, today's date, and four empty sections: `## Bugs`, `## Ideas`, `## Todo`, `## Archive`).
3. Find the highest `T-NNN` across the **entire file** (active sections + archive). If none exists, start with `T-001`. Otherwise use `T-{max+1}` zero-padded to 3 digits.
4. Append to the end of the `## Todo` section:
   ```
   - [ ] **T-NNN** (YYYY-MM-DD) description
   ```
   where `YYYY-MM-DD` is today's date.
5. Report the new ID and description back to the user in one line.

## Rules

- Keep the description in the user's wording — do not paraphrase.
- Touch only the `## Todo` section.
- Never remove existing entries.
