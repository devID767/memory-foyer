#!/bin/bash
# Claude Code PreToolUse hook: Blocks edits to protected files
# Protects: Plugins/, TextMesh Pro/, PlayerInput_Actions.cs
# Exit 0 = allow, Exit 2 = block (stderr shown to Claude)

INPUT=$(cat)

if ! command -v jq >/dev/null 2>&1; then
  exit 0
fi

FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty')

if [ -z "$FILE_PATH" ]; then
  exit 0
fi

case "$FILE_PATH" in
  */Assets/Art/Plugins/*)
    echo "BLOCKED: $FILE_PATH is in Assets/Art/Plugins/ — DO NOT MODIFY" >&2
    exit 2
    ;;
  */Assets/Plugins/*|*/Plugins/*)
    echo "BLOCKED: $FILE_PATH is in Assets/Plugins/ — DO NOT MODIFY" >&2
    exit 2
    ;;
  *"/Assets/TextMesh Pro/"*|*"/TextMesh Pro/"*)
    echo "BLOCKED: $FILE_PATH is in Assets/TextMesh Pro/ — DO NOT MODIFY" >&2
    exit 2
    ;;
  *PlayerInput_Actions.cs)
    echo "BLOCKED: $FILE_PATH is auto-generated — DO NOT MODIFY" >&2
    exit 2
    ;;
esac

exit 0
