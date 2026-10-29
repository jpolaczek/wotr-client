#!/bin/bash
# Generate one image with the owner's Codex CLI (built-in image_gen tool) and copy it to $2.
# Usage: codex_image.sh "<prompt>" <output.png>
PROMPT="$1"
OUT="$2"
MARK=$(mktemp $(dirname "$0")/codex-mark.XXXXXX)
codex exec --skip-git-repo-check -s read-only \
  "Use your built-in image generation tool to create exactly one image, then stop. Do not write any files or run commands. Image: $PROMPT" \
  > "$OUT.log" 2>&1
NEWEST=$(find "$HOME/.codex/generated_images" -type f -name '*.png' -newer "$MARK" | head -1)
rm -f "$MARK"
if [ -z "$NEWEST" ]; then
  echo "NO IMAGE"; tail -5 "$OUT.log"; exit 1
fi
cp "$NEWEST" "$OUT"
echo "$NEWEST -> $OUT"
