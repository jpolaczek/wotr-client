#!/usr/bin/env bash
# DIST-06: replaces Unity's Wotr.exe in a staged Windows player with release/windows/WotrLauncher.c. The icon and
# application manifest come from Unity's executable; the icon also stays in <keep dir> as app.ico for Setup.exe.
# Needs mingw-w64 and icoutils (Linux: gcc-mingw-w64-x86-64 icoutils; macOS: brew install mingw-w64 icoutils).
# Usage: release/windows/build-launcher.sh <player directory containing Wotr.exe> <directory for the original executable and icon>
set -euo pipefail

player="${1:?player directory}"
keep="${2:?directory for the original Wotr.exe}"
here="$(cd "$(dirname "$0")" && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

for tool in x86_64-w64-mingw32-gcc x86_64-w64-mingw32-windres wrestool; do
  command -v "$tool" >/dev/null || { echo "ERROR: missing $tool (mingw-w64, icoutils)" >&2; exit 1; }
done
[[ -f "$player/Wotr.exe" && -f "$player/UnityPlayer.dll" && -d "$player/Wotr_Data" ]] || {
  echo "ERROR: $player does not appear to be a Windows Unity player" >&2
  exit 1
}
mkdir -p "$keep"
cp "$player/Wotr.exe" "$keep/Wotr.unity.exe"

# Group icon (type 14) as an .ico file and the manifest (type 24) of Unity's executable.
wrestool -x -t 14 "$keep/Wotr.unity.exe" > "$keep/app.ico"
wrestool -x --raw -t 24 "$keep/Wotr.unity.exe" > "$work/app.manifest"
[[ -s "$keep/app.ico" ]] || { echo "ERROR: no icon in the Unity Wotr.exe" >&2; exit 1; }
[[ -s "$work/app.manifest" ]] || { echo "ERROR: no manifest in the Unity Wotr.exe" >&2; exit 1; }
cp "$keep/app.ico" "$work/app.ico"
cp "$work/app.manifest" "$keep/unity.manifest"

cat > "$work/launcher.rc" <<'RC'
1 ICON "app.ico"
1 24 "app.manifest"
RC
(cd "$work" && x86_64-w64-mingw32-windres launcher.rc -O coff -o launcher.res)
x86_64-w64-mingw32-gcc -std=c11 -O2 -s -Wall -Wextra -Werror -municode -mwindows -static \
  -o "$player/Wotr.exe" "$here/WotrLauncher.c" "$work/launcher.res" -lshell32
echo "Wotr.exe: launcher $(sha256sum "$player/Wotr.exe" 2>/dev/null | cut -c1-16 || shasum -a 256 "$player/Wotr.exe" | cut -c1-16)…, Unity original at $keep/Wotr.unity.exe"
