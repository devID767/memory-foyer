---
name: idea
model: haiku
description: Add an idea/improvement to Backlog.md (Ideas section)
effort: low
argument-hint: "[idea description]"
allowed-tools: Read, Edit, Write
---

# /idea — add an idea to backlog

Appends an entry to `docs/Backlog.md`, section `## Ideas`.

Description: $ARGUMENTS

## Process

1. If `$ARGUMENTS` is empty, ask the user one question: "Describe the idea".
2. Read `docs/Backlog.md`.
   - If the file is missing, create it with a template (heading `# Backlog`, version `1.0`, today's date, and four empty sections: `## Bugs`, `## Ideas`, `## Todo`, `## Archive`).
3. Find the highest `I-NNN` across the **entire file** (active sections + archive). If none exists, start with `I-001`. Otherwise use `I-{max+1}` zero-padded to 3 digits.
4. Append to the end of the `## Ideas` section:
   ```
   - [ ] **I-NNN** (YYYY-MM-DD) description
   ```
   where `YYYY-MM-DD` is today's date.
5. Report the new ID and description back to the user in one line.

## Rules

- Keep the description in the user's wording — do not paraphrase.
- Touch only the `## Ideas` section.
- Never remove existing entries.
