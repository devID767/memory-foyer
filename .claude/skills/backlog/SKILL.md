---
name: backlog
model: haiku
description: Show a summary of Backlog.md — counts and recent items by section
effort: low
allowed-tools: Read
---

# /backlog — backlog summary

Shows a brief summary of `docs/Backlog.md`. Read-only, no edits.

## Process

1. Read `docs/Backlog.md`. If the file is missing or every active section is empty, report "Backlog is empty".
2. Count active items (`- [ ]` lines) in `## Bugs`, `## Ideas`, `## Todo`. Also count items in `## Archive`.
3. Print in this format:

```
Backlog — summary

Bugs:  N open
  recent:
    - B-XXX description
    ...

Ideas: N open
  recent:
    - I-XXX description
    ...

Todo:  N open
  recent:
    - T-XXX description
    ...

Archive: N closed
```

## Rules

- Show the **last 3** items of each active section (newest IDs are at the bottom of the file → take the last 3 lines of the section).
- If a section is empty, print `Bugs: 0 open` with no list under it.
- Do not echo the whole file — summary only.
- Keep output short, ≤ 30 lines.
