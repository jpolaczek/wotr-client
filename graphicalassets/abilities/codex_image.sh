#!/bin/bash
# Generate one image with the owner's Codex CLI (built-in image_gen tool) and copy it to $2 (UI-439).
# Usage: codex_image.sh "<prompt>" <output.png> [<log file>]
# As graphicalassets/stands/codex_image.sh, but safe to run several at once: the image is taken from this run's own
# session folder (~/.codex/generated_images/<session id>/), named in the run's log, not from the newest file. The
# agent only passes the prompt to the image tool, so it runs at low reasoning effort to spare the owner's Codex usage.
# Its stdin is closed: without a terminal, codex exec would otherwise wait for more input forever.
PROMPT="$1"
OUT="$2"
LOG="${3:-$OUT.log}"
codex exec --ephemeral --skip-git-repo-check -s read-only -c model_reasoning_effort='"low"' \
  "Use your built-in image generation tool to create exactly one image, then stop. Do not write any files or run commands. Image: $PROMPT" \
  < /dev/null > "$LOG" 2>&1
SESSION=$(grep -m1 -oE 'session id: [0-9a-f-]+' "$LOG" | awk '{print $3}')
if [ -z "$SESSION" ] || [ ! -d "$HOME/.codex/generated_images/$SESSION" ]; then
  echo "NO IMAGE"; tail -5 "$LOG"; exit 1
fi
IMAGE=$(find "$HOME/.codex/generated_images/$SESSION" -type f -name '*.png' | head -1)
if [ -z "$IMAGE" ]; then
  echo "NO IMAGE"; tail -5 "$LOG"; exit 1
fi
cp "$IMAGE" "$OUT"
echo "$IMAGE -> $OUT"
