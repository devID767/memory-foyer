---
name: roadmap-done
model: haiku
description: Mark a roadmap item as completed (in-place, no move)
argument-hint: "<ID like M1.2 or a keyword from the description>"
allowed-tools: Read, Edit
---

# /roadmap-done — close a roadmap item

Finds an entry in `docs/Roadmap.md` by ID or keyword, flips `[ ]` to `[x]`, and appends the closure date. The item **stays in place** within its phase section (no move to Archive — preserves the phase structure).

Search input: $ARGUMENTS

## Process

1. If `$ARGUMENTS` is empty, ask: "Which item to close? (ID like M1.2 or part of the description)". Stop.
2. Read `docs/Roadmap.md`. If the file is missing, report "Roadmap.md is missing" and stop.
3. Pick the search mode:
   - If `$ARGUMENTS` matches `M\d+\.\d+` in any case (or the project's phase IDs like `0.3`, `1.5`), use **ID mode** — exact match against `**<ID>**` in open items (`- [ ]`).
   - Otherwise use **substring mode** — case-insensitive substring match against the description text of open items.
4. Search all phase sections (heading `## Phase <N>:` or `## Current: M<N> — *` etc.).
5. Based on the result:
   - **0 matches** — print: "No open items matched. Open items in the current phase: <ID list>" and stop.
   - **>1 matches** — print the matched items as `[ID] description` and ask for a precise ID. Stop.
   - **1 match** — continue to step 6.
6. Extract the original creation date from the line (pattern `(YYYY-MM-DD)`). Replace the line with:
   ```
   - [x] **ID** (YYYY-MM-DD → YYYY-MM-DD) description
   ```
   First date is the original creation date, second is today. Item order in the file is preserved.
7. After writing, re-read the current phase section. If no `- [ ]` items remain, note "phase complete".
8. Print:
   - Main line: `ID closed in Roadmap.`
   - If the phase is complete: an extra line `Phase M<N> is complete. Run /roadmap-next when you're ready to advance.`

## Rules

- Use **Edit** (not Write) — surgical replacement of one line.
- Do not touch `docs/Backlog.md` — that's `/done`'s job.
- Do not move the item to `## Archive` — roadmap items stay in place.
- Do not run `/roadmap-next` automatically — just hint, the user decides.
- If the line format is unexpected (e.g. no date in parentheses), print the line and ask the user to fix it manually.
