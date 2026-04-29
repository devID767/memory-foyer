---
name: done
model: haiku
description: Mark a backlog item as completed (moves to Archive)
argument-hint: "<ID or keyword from the description>"
allowed-tools: Read, Edit
---

# /done — close a backlog item

Finds an entry in `docs/Backlog.md` by ID or keyword, removes it from its active section, and appends it to `## Archive` with the closure date.

Search input: $ARGUMENTS

## Process

1. If `$ARGUMENTS` is empty, ask: "Which item to close? (ID or part of the description)".
2. Read `docs/Backlog.md`. If the file is missing, report "Backlog is empty".
3. Match **case-insensitively** across the three active sections: `## Bugs`, `## Ideas`, `## Todo`.
   - If `$ARGUMENTS` looks like an ID (pattern `[BIT]-\d+`, any case), match the ID exactly.
   - Otherwise, treat `$ARGUMENTS` as a substring of the description text after the ID.
4. Based on the result:
   - **0 matches** — print: "Not found. Active items: ..." (briefly list all active IDs).
   - **>1 matches** — print the matched items as `[ID] description` and ask for a precise ID.
   - **1 match** — extract the original creation date from the line, remove the line from its active section, and append to the end of `## Archive`:
     ```
     - [x] **ID** (YYYY-MM-DD → YYYY-MM-DD) description
     ```
     where the first date is the original creation date and the second is today.
5. Report in one line: `ID moved to Archive`.

## Rules

- Use Edit (not Write) — surgical replacement of two regions of the file.
- The ID counter is never decremented — archive entries still count when allocating new IDs.
- Do not reorder other entries.
