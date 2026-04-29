---
name: commit
model: sonnet
description: Analyzes changes, writes commit message in Conventional Commits style, and commits. Suggests splitting if too many unrelated changes.
argument-hint: "[optional context hint for the commit message]"
allowed-tools: Bash
---

# /commit — Smart Commit (Conventional Commits)

Analyze changes, write a Conventional-Commits message, commit. $ARGUMENTS

## Process

### Step 1: Analyze Changes
- Run `git status` to see staged, unstaged, and untracked files
- Run `git diff HEAD` to read the actual diff
- If there is nothing to commit — say so and stop

### Step 2: Evaluate Scope
- If changes span multiple unrelated systems (e.g. Movement + UI + Docs) — suggest splitting into separate commits, show grouping, and ask the user which group to commit first
- If changes are cohesive — proceed

### Step 3: Pick the Conventional Commits prefix

Always use one of these prefixes (no exceptions):

| Prefix | Use when |
|---|---|
| `feat:` | New feature or capability |
| `fix:` | Bug fix |
| `test:` | Adding or fixing tests, no production code change |
| `refactor:` | Code change that neither fixes a bug nor adds a feature |
| `perf:` | Performance improvement |
| `docs:` | Documentation only (README, comments, /docs) |
| `style:` | Formatting, whitespace, semicolons — no logic change |
| `chore:` | Maintenance: config tweaks, file renames, dep bumps |
| `build:` | Build system, package install / removal, asmdef changes |
| `ci:` | CI/CD workflow changes |

Optional scope: use the project's layer/area names — `domain`, `application`, `infra`, `presentation`, `composition`, `editor`, `server`. Examples: `feat(domain): add card and deck models`, `feat(infra): implement unity webrequest http client`, `feat(server): add deck stats endpoint`. Use when the change is clearly within one subsystem.

### Step 4: Write Commit Message
- If the change closes a roadmap sub-task in `docs/Roadmap.md`, prefer the wording suggested for that task (the roadmap mirrors the project's build plan)
- Run `git log --oneline -5` to verify existing project style and match capitalization conventions
- English, lowercase imperative subject (`feat: add deck stats endpoint`, not `Feat: Added`)
- Max 72 chars on subject line
- No body unless the change is non-obvious or spans multiple files for one logical reason

### Step 5: Commit
- Stage specific files by name from `git status` output (e.g. `git add Assets/Scripts/Domain/Card.cs Assets/Scripts/Domain/Card.cs.meta`) — never `git add .` or `git add -A`, to avoid sweeping in stray local files
- `git commit -m "message"`
- The PreToolUse hook will run automatically — follow its warnings if any

### Step 6: Confirm
- Show one line: commit hash + subject

## Rules
- Keep output minimal — no summaries of what you analyzed
- Never force-push, never amend, never --no-verify
- If $ARGUMENTS is provided — use it as context hint for the message, not as the message itself
- If the hook prints warnings — show them to the user and ask whether to proceed
- One commit = one logical change. If you can't pick a single prefix, the commit should probably be split
