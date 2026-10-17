#!/usr/bin/env bash
# LNX-02 installer smoke on Linux x64 (CI runner or a PC without ~/Applications/WotrOnline.AppImage): the release
# AppImage is installed as players do (downloaded, made executable, started), self-check, a damaged package is
# refused without touching the AppImage, UpdateNix from the mounted AppImage applies the next package in place,
# self-check, player data unchanged, removal keeps player data. Needs FUSE (fusermount3 or fusermount).
# Inputs from scripts/package_velopack.py: the release folder (installers/linux-x64) and the --smoke-output folder.
# Writes <out>/report.txt.
# Usage: scripts/smoke-installer-linux.sh <release directory> <next package directory> <output directory>
set -euo pipefail

release="$(cd "${1:?release directory}" && pwd)"
next="$(cd "${2:?next package directory}" && pwd)"
out="${3:?output directory}"
mkdir -p "$out" && out="$(cd "$out" && pwd)"
report="$out/report.txt"
app="$HOME/Applications/WotrOnline.AppImage"
mount_pid=""
: > "$report"

record() { echo "$*" | tee -a "$report"; }
unmount() {
  if [[ -n "$mount_pid" ]]; then
    kill "$mount_pid" 2>/dev/null || true
    wait "$mount_pid" 2>/dev/null || true
    mount_pid=""
  fi
}
fail() {
  record "FAIL: $*"
  unmount
  cp /tmp/velopack_WotrOnline.log "$out/" 2>/dev/null || true
  exit 1
}
trap unmount EXIT

json_field() { python3 -I -c 'import json, sys; print(json.load(open(sys.argv[1]))[sys.argv[2]])' "$1" "$2"; }

# Player data outside the installation: every file with its SHA-256.
fingerprint() {
  python3 -I - "$1" <<'PY'
import hashlib, sys
from pathlib import Path
root = Path(sys.argv[1])
for path in sorted(p for p in root.rglob("*") if p.is_file()):
    print(hashlib.sha256(path.read_bytes()).hexdigest(), path.relative_to(root))
PY
}

# The installed game reports itself (InstallationSelfCheck) and quits before opening a window.
self_check() {
  local name="$1" installed="$2" build="$3" json="$out/self-check-$1.json"
  "$app" -batchmode -nographics -logFile "$out/player-$name.log" --wotr-self-check "$json" || fail "self-check $name exit $?"
  record "self-check $name: $(python3 -I -c 'import json, sys; print(json.dumps(json.load(open(sys.argv[1]))))' "$json")"
  [[ "$(json_field "$json" installedVersion)" == "$installed" && "$(json_field "$json" buildVersion)" == "$build" &&
     "$(json_field "$json" platform)" == linux-x64 && "$(json_field "$json" processArchitecture)" == X64 &&
     "$(json_field "$json" development)" == False && "$(json_field "$json" canUpdateItself)" == True &&
     "$(json_field "$json" velopackStartup)" == ok ]] ||
    fail "self-check $name — expected installed $installed, build $build, a release that can update itself"
}

# As in the game (VelopackBridge): UpdateNix from the mounted AppImage, which finds the file through $APPIMAGE.
apply_package() {
  local mount_point=""
  "$app" --appimage-mount > "$out/mount.txt" &
  mount_pid=$!
  for _ in $(seq 50); do
    mount_point="$(head -1 "$out/mount.txt" 2>/dev/null || true)"
    [[ -n "$mount_point" && -x "$mount_point/usr/bin/UpdateNix" ]] && break
    sleep 0.2
  done
  [[ -x "$mount_point/usr/bin/UpdateNix" ]] || fail "the AppImage did not mount (FUSE)"
  local status=0
  APPIMAGE="$app" APPDIR="$mount_point" "$mount_point/usr/bin/UpdateNix" apply --silent --norestart --package "$1" || status=$?
  unmount
  return "$status"
}

[[ ! -e "$app" ]] || fail "$app exists — move it away before the smoke test"
version="$(json_field "$release/build-info.json" version)"
installer="$release/WotrOnline-$version-linux.AppImage"
next_package="$(ls "$next"/WotrOnline-*-linux-full.nupkg)"
next_version="$(basename "$next_package" | sed -E 's/^WotrOnline-(.+)-linux-full\.nupkg$/\1/')"
record "$(. /etc/os-release && echo "$PRETTY_NAME") $(uname -m); release $version, next $next_version"

mkdir -p "$(dirname "$app")"
cp "$installer" "$app"
chmod +x "$app"
self_check release "$version" "$version"

data="$(json_field "$out/self-check-release.json" persistentDataPath)"
mkdir -p "$data"
uuidgen > "$data/installer-smoke-marker.txt" 2>/dev/null || python3 -I -c 'import uuid; print(uuid.uuid4())' > "$data/installer-smoke-marker.txt"
before="$(fingerprint "$data")"
installed="$(sha256sum "$app" | cut -d' ' -f1)"

# A damaged package (as after a broken transfer or a full disk) fails while Velopack extracts it: the AppImage stays.
broken="$out/WotrOnline-broken-linux-full.nupkg"
head -c "$(( $(stat -c%s "$next_package") / 2 ))" "$next_package" > "$broken"
if apply_package "$broken"; then
  fail "UpdateNix applied a damaged package"
fi
[[ "$(sha256sum "$app" | cut -d' ' -f1)" == "$installed" ]] || fail "a damaged package changed the AppImage"
self_check after-damaged "$version" "$version"
record "damaged package refused; AppImage unchanged"

apply_package "$next_package" || fail "UpdateNix apply exit $?"
[[ -x "$app" ]] || fail "the updated AppImage is not executable"
self_check next "$next_version" "$version"
[[ "$(fingerprint "$data")" == "$before" ]] || fail "player data changed during the update"
record "update: $version -> $next_version in place, player data unchanged"

rm -f "$app"
[[ "$(fingerprint "$data")" == "$before" ]] || fail "player data changed when the AppImage was removed"
record "removed the AppImage; player data unchanged"
cp /tmp/velopack_WotrOnline.log "$out/" 2>/dev/null || true
record PASS
