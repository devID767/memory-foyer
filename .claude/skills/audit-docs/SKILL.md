---
name: audit-docs
model: sonnet
description: Audit documentation for inconsistencies — cross-references docs against each other and against the codebase. Finds stale paths, broken contracts, design-vs-code drift.
effort: high
argument-hint: "[scope: file path, topic, or empty for full audit]"
allowed-tools: Read, Grep, Glob, Bash, Write, Edit
---

# /audit-docs — Documentation Consistency Audit

Audit documentation for inconsistencies: $ARGUMENTS

The goal is to find drift between what the docs claim and what is true —
both across docs and against the actual codebase. Outputs a plan file with
concrete fixes; does NOT modify project files.

## Process

### Step 1: Identify the canon doc set

If $ARGUMENTS is empty, treat the full canonical set as scope. Typical canon:
`README.md`, `CLAUDE.md`, `docs/*.md` (architecture, GDD, Roadmap, Backlog),
`server/README.md`, OpenAPI/contract files, `.claude/rules/*.md`.

If $ARGUMENTS names a file or topic, narrow scope but still include any doc
that semantically depends on it (e.g. auditing `server/openapi.yaml`
includes GDD §8 because §8 documents the same contract).

### Step 2: Read each in-scope doc fully

Not partial. Inconsistencies live in implications and cross-references —
half-read docs miss them. Use parallel Read calls. For very large files or
broad scopes, dispatch Explore subagents to keep main context lean.

### Step 3: Cross-reference (term-agnostic categories)

For every doc, scan for and verify:

1. **Path/file claims.** Doc names `Foo/Bar.cs`, `assets/x.png`, etc. →
   confirm the path exists (`ls`, Glob).
2. **Code identifier claims.** Doc names a class/method/field/enum value →
   `grep` the codebase. Flag if missing or renamed.
3. **State claims.** "Current state…", "Phase X in progress",
   `[CURRENT]` markers, "TODO", "planned in Phase Y" → check `git log`,
   roadmap checkboxes, actual file presence.
4. **Behavior claims.** Doc describes how code behaves (e.g. "persists
   after every grade", "filters per UTC day") → read the implementation
   and verify. This is where the highest-value findings hide.
5. **Design intent vs implementation.** Doc declares a design decision
   (e.g. "no interactive 3D objects", "server is authoritative") → verify
   code obeys. **If they disagree, do NOT silently propose to "fix the
   doc" — surface the conflict as a decision for the user**, with
   trade-offs of changing code vs changing the doc.
6. **Cross-doc consistency.** Same concept described in N docs → compare
   wording, find drift.
7. **Internal contradictions.** Two sections of the same doc disagreeing
   (e.g. one section claims X, another claims ¬X).
8. **Stale references.** Commits/files/tools that have been removed,
   renamed, or replaced.

### Step 4: Verify findings before reporting

Every finding must have a concrete line:file pointer on both sides
(doc claim AND ground truth). If you can't pin down the ground truth,
mark it as "needs investigation" rather than asserting drift.

### Step 5: Write the plan file

Path: `~/.claude/plans/audit-docs-<slug>.md` (slug from scope or date).

Structure:

- **Context** — what was audited, why, files in scope.
- **Numbered findings** — one section per finding:
  - Where (line:file refs).
  - Reality (line:file refs to ground truth).
  - Fix proposal (concrete edit, not "consider updating").
  - Severity tag: `broken` / `stale` / `cosmetic` / `decision-needed`.
  - For category-5 findings: explicitly ask the user to choose
    code-fix vs doc-fix, with trade-offs.
- **Plan of fixes** — ordered action list.
- **Verification** — `grep` commands the user can run after fixes to
  confirm they stuck.
- **Notes** — out-of-scope observations, files NOT touched and why.

### Step 6: Hand off

Print a one-paragraph summary in chat with the count of findings grouped
by category and severity, plus the path to the plan file. Do NOT start
applying fixes — the user reviews the plan first.

## Rules

- **No hardcoded terms.** Don't look for any specific stale word; look for
  the categories above. The audit must surface new drift the next time too.
- **Read fully, not partially.** Skimming misses the implications layer.
- **Pin down ground truth.** Every claim of drift must have a verifiable
  source. No "this might be inconsistent" without evidence.
- **Code-vs-doc conflicts are decisions, not chores.** Surface them; don't
  unilaterally pick which side to "fix".
- **Don't modify project files.** The deliverable is the plan; user
  decides what to apply.
- **Don't try to be exhaustive on first pass.** If scope is large, batch
  Read calls in parallel via Explore subagents to keep main context lean.
- **Distinguish severity in findings:** broken contract / stale text /
  cosmetic / design decision needed. The user reads severity first.
