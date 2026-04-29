#!/bin/bash
# Claude Code PreToolUse hook: pre-commit check against Backlog coverage.
# Exit 0 = allow commit; Exit 2 = block with stderr shown to Claude.
# Always exit 0 on unexpected conditions — do not block commits due to hook bugs.

INPUT=$(cat)

if ! command -v jq >/dev/null 2>&1; then
  exit 0
fi

COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty' 2>/dev/null)

# Must be git commit (strict)
echo "$COMMAND" | grep -qE '^git[[:space:]]+commit([[:space:]]|$)' || exit 0

# Skip non-standard commit flavours
echo "$COMMAND" | grep -qE '(\-\-amend|\-\-dry-run|\-\-no-edit)' && exit 0
echo "$COMMAND" | grep -qE '[[:space:]]\-C[[:space:]]' && exit 0

# Staged state (filter noise)
STAGED_FILES=$(git diff --cached --name-only 2>/dev/null | grep -vE '\.meta$|^Assets/Plugins/|^Assets/Art/Plugins/|^Assets/TextMesh Pro/')
[ -z "$STAGED_FILES" ] && exit 0

# Size check — skip trivial diffs
SHORTSTAT=$(git diff --cached --shortstat 2>/dev/null)
INS=$(echo "$SHORTSTAT" | grep -oE '[0-9]+ insertion' | grep -oE '[0-9]+')
DEL=$(echo "$SHORTSTAT" | grep -oE '[0-9]+ deletion' | grep -oE '[0-9]+')
TOTAL=$(( ${INS:-0} + ${DEL:-0} ))
[ "$TOTAL" -lt 10 ] && exit 0

# At least one .cs file must be staged for backlog suggestions to be relevant
HAS_CODE=0
while IFS= read -r f; do
  case "$f" in
    *.cs)
      HAS_CODE=1
      break
      ;;
  esac
done <<< "$STAGED_FILES"
[ "$HAS_CODE" -eq 0 ] && exit 0

# Extract commit subject (best effort)
SUBJECT=""
HEREDOC_LINE=$(echo "$COMMAND" | awk '/<<["\047]?EOF["\047]?/,/^[[:space:]]*EOF[[:space:]]*$/' | sed -n '2p')
if [ -n "$HEREDOC_LINE" ]; then
  SUBJECT="$HEREDOC_LINE"
else
  SUBJECT=$(echo "$COMMAND" | sed -nE 's/.*-m[[:space:]]+"([^"]+)".*/\1/p' | head -1)
  if [ -z "$SUBJECT" ]; then
    SUBJECT=$(echo "$COMMAND" | sed -nE "s/.*-m[[:space:]]+'([^']+)'.*/\1/p" | head -1)
  fi
fi

# Skip mechanic-agnostic commit types
case "$SUBJECT" in
  docs:*|docs\(*) exit 0 ;;
  chore:*|chore\(*|style:*|style\(*|test:*|test\(*|refactor:*|refactor\(*|build:*|build\(*|ci:*|ci\(*) exit 0 ;;
esac

# Anti-loop: Backlog.md already staged → skip backlog check
BACKLOG_CHECK=1
echo "$STAGED_FILES" | grep -qE '^docs/Backlog\.md$' && BACKLOG_CHECK=0

# --- Collect open backlog items ---
BACKLOG_LIST=""
BACKLOG_FILE="docs/Backlog.md"
if [ "$BACKLOG_CHECK" -eq 1 ] && [ -f "$BACKLOG_FILE" ]; then
  BACKLOG_LIST=$(awk '/^## Archive/{exit} /^- \[ \]/{print}' "$BACKLOG_FILE" | \
    sed -nE 's/^- \[ \] \*\*([BIT]-[0-9]+)\*\*[[:space:]]*(\([^)]*\)[[:space:]]*)?(.*)/  - \1 \3/p')
fi

# Nothing to report → allow commit
[ -z "$BACKLOG_LIST" ] && exit 0

# --- Emit block message to stderr ---
{
  echo "=== Pre-commit check: commit blocked pending review ==="
  echo ""
  echo "Commit subject: ${SUBJECT:-(unparsed)}"
  echo ""
  FILE_COUNT=$(echo "$STAGED_FILES" | wc -l | tr -d ' ')
  echo "Staged files ($FILE_COUNT total, first 15):"
  echo "$STAGED_FILES" | head -15 | sed 's/^/  /'
  if [ "$FILE_COUNT" -gt 15 ]; then
    echo "  (+$((FILE_COUNT - 15)) more)"
  fi
  echo ""
  echo "Open backlog items (check if commit closes any):"
  echo "$BACKLOG_LIST"
  echo ""
  echo "Claude: via AskUserQuestion (multiSelect) ask the user which backlog items to close. For each confirmed:"
  echo "  - Run /done <ID> for each selected backlog item"
  echo "Then 'git add' the modified Backlog.md, and retry the same git commit (identical message)."
  echo "If user declines everything, retry commit as-is to unblock."
  echo "If no match seems correct, retry commit immediately without interaction."
  echo "Do not mention this hook ran unless user interaction is actually needed."
} >&2

exit 2
