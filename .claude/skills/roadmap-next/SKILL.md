---
name: roadmap-next
model: haiku
description: Move the "current phase" marker in roadmap to the next phase
allowed-tools: Read, Edit
---

# /roadmap-next — advance to the next roadmap phase

Archives the current phase heading (`## Current: M<N> — ...` → `## M<N> — ...`) and promotes the next one (`## M<N+1> — ...` → `## Current: M<N+1> — ...`). Items inside sections are not touched.

Note: this project's `docs/Roadmap.md` uses a `[CURRENT]` tag in the heading instead of a `Current:` prefix (e.g. `## Phase 0: Project scaffolding [CURRENT]`). Treat both forms equivalently — flip the tag's location, not its surrounding text.

## Process

1. Read `docs/Roadmap.md`. If the file is missing, report "Roadmap.md is missing" and stop.
2. Find the section with `## Current: M<N> — <name>` (or `## Phase <N>: <name> [CURRENT]`).
   - If none, print "Roadmap.md has no current-phase marker — nothing to advance" and stop.
3. Verify that the current section has no open items (`- [ ]`). Scan up to the next `## ...` heading.
   - If open items remain, print:
     ```
     Phase M<N> is not complete. Open items:
       - M<N>.X <description>
       - M<N>.Y <description>
     Close them with /roadmap-done, or add a --force flag in a future version to override.
     ```
     Stop without moving the marker.
4. Find the next phase section (`## M<N+1> — <name>` or `## Phase <N+1>: <name>`).
   - If missing, print "Next phase (M<N+1>) is not declared in Roadmap.md. Add a `## M<N+1> — <name>` section before advancing" and stop.
5. Make two surgical Edits:
   - `## Current: M<N> — <name>` → `## M<N> — <name>` (or remove `[CURRENT]` tag from current).
   - `## M<N+1> — <name>` → `## Current: M<N+1> — <name>` (or add `[CURRENT]` tag to next).
6. Print: `Current phase: M<N+1> — <name>. Run /roadmap to see its items.`

## Rules

- Use **Edit** (not Write) — two surgical heading swaps. Order doesn't matter; section bodies are untouched.
- If the current phase has open items, refuse (do not move the marker). Protects against accidental advancement.
- If the next phase is absent, say what to do — don't stay silent.
- Do not touch `## Archive` — completed phases remain in the file as regular `## M<N>` sections.
