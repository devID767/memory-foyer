---
name: roadmap
model: haiku
description: Show the current roadmap phase and the next 3 open items in it
allowed-tools: Read
---

# /roadmap — strategic "what's next"

Reads `docs/Roadmap.md`, shows the current phase and the next 3 open items in it. Read-only.

## Process

1. Read `docs/Roadmap.md`. If the file is missing, report: "Roadmap.md is missing. Define the phases with a single prompt to Claude (format: headings `## Current: M1 — <name>`, items `- [ ] **M1.1** (date) description`)".
2. Find the section whose heading matches `## Current: M<N> — <name>`. If none, report: "Roadmap.md has no `## Current:` section — please mark the current phase". (Note: this project uses a `[CURRENT]` tag instead of a `Current:` prefix; treat both forms as equivalent.)
3. Extract from the section:
   - The `M<N>` number and name from the heading.
   - The `**Goal:**` line, if present, immediately below the heading.
   - All `- [ ]` (open) and `- [x]` (closed) items up to the next `## ...` heading.
4. Compute progress: `closed / total * 100 = XX%`, rounded to an integer.
5. Take the first **3** open items in file order.
6. Print:

   ```
   Phase: M<N> — <name> (<closed>/<total> = <XX>%)
   Goal: <goal>

   Next 3 items:
   1. [M<N>.X] <description>
   2. [M<N>.Y] <description>
   3. [M<N>.Z] <description>
   ```

7. If fewer than 3 open items remain, print whatever is left. If zero, print: "Phase M<N> is complete — advance with `/roadmap-next`".
8. If the file accidentally contains multiple `## Current:` sections, use the first one and add `⚠ Multiple Current sections in Roadmap.md, using the first one`.

## Rules

- Read-only — no edits.
- Do not editorialize ("why pick this") — the user decides.
- If `**Goal:**` is absent, omit the `Goal:` line.
- Keep output short, ≤ 15 lines.
