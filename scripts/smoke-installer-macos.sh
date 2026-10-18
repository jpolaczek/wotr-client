#!/usr/bin/env bash
# DIST-06 installer smoke on macOS (CI runner or a Mac without WoTR in ~/Applications): the app from the release
# .pkg goes to ~/Applications as "install for me only" does, self-check, a damaged package is refused without
# touching the app, UpdateMac applies the next package, self-check, player data unchanged, ad-hoc signature valid
# throughout. Inputs from scripts/package_velopack.py:
# the release folder (installers/macos-universal) and the --smoke-output folder. Writes <out>/report.txt.
# The .pkg's own installer UI and Gatekeeper are checked by hand (spikes S3/S3b, player guide).
# Usage: scripts/smoke-installer-macos.sh <release directory> <next package directory> <output directory>
set -euo pipefail

release="$(cd "${1:?release directory}" && pwd)"
next="$(cd "${2:?next package directory}" && pwd)"
out="${3:?output directory}"
mkdir -p "$out" && out="$(cd "$out" && pwd)"
report="$out/report.txt"
app="$HOME/Applications/War of the Ring Online.app"
: > "$report"

record() { echo "$*" | tee -a "$report"; }
fail() {
  record "FAIL: $*"
  cp "$HOME/Library/Logs/velopack_WotrOnline.log" "$out/" 2>/dev/null || true
  exit 1
}

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

check_app() {
  codesign --verify --deep --strict "$app" || fail "codesign --verify --deep --strict ($1)"
  local executable archs
  executable="$(defaults read "$app/Contents/Info" CFBundleExecutable)"
  for binary in "$app/Contents/MacOS/$executable" "$app/Contents/MacOS/UpdateMac"; do
    archs="$(lipo -archs "$binary")"
    [[ " $archs " == *" x86_64 "* && " $archs " == *" arm64 "* ]] || fail "$binary is not universal: $archs"
  done
  record "$1: codesign OK ($(codesign -dv "$app" 2>&1 | grep -o 'Signature=[a-z-]*')), $executable and UpdateMac universal"
}

# The installed game reports itself (InstallationSelfCheck) and quits before opening a window.
# An optional fourth argument starts that architecture's slice (arch -x86_64: Intel code under Rosetta).
self_check() {
  local name="$1" installed="$2" build="$3" slice="${4:-}" json="$out/self-check-$1.json" executable
  rm -f "$json"  # SEC-01: the game never overwrites an existing report
  executable="$(defaults read "$app/Contents/Info" CFBundleExecutable)"
  ${slice:+arch -$slice} "$app/Contents/MacOS/$executable" -batchmode -nographics -logFile "$out/player-$name.log" --wotr-self-check "$json" \
    || fail "self-check $name exit $?"
  record "self-check $name: $(python3 -I -c 'import json, sys; print(json.dumps(json.load(open(sys.argv[1]))))' "$json")"
  [[ "$(json_field "$json" installedVersion)" == "$installed" && "$(json_field "$json" buildVersion)" == "$build" &&
     "$(json_field "$json" platform)" == macos-universal && "$(json_field "$json" development)" == False &&
     "$(json_field "$json" canUpdateItself)" == True && "$(json_field "$json" velopackStartup)" == ok ]] ||
    fail "self-check $name — expected installed $installed, build $build, a release that can update itself"
}

[[ ! -e "$app" ]] || fail "$app exists — remove War of the Ring Online from ~/Applications before the smoke test"
version="$(json_field "$release/build-info.json" version)"
installer="$release/WotrOnline-$version-osx-Setup.pkg"
next_package="$(ls "$next"/WotrOnline-*-osx-full.nupkg)"
next_version="$(basename "$next_package" | sed -E 's/^WotrOnline-(.+)-osx-full\.nupkg$/\1/')"
record "macOS $(sw_vers -productVersion) $(uname -m); release $version, next $next_version"

expanded="$(mktemp -d)"
trap 'rm -rf "$expanded"' EXIT
pkgutil --expand-full "$installer" "$expanded/pkg" >/dev/null
payload="$(find "$expanded/pkg" -maxdepth 4 -name '*.app' -type d | head -1)"
[[ -n "$payload" ]] || fail "no application in $installer"
mkdir -p "$HOME/Applications"
ditto "$payload" "$app"
check_app install
self_check release "$version" "$version"
if arch -x86_64 /usr/bin/true 2>/dev/null; then
  self_check rosetta "$version" "$version" x86_64
  [[ "$(json_field "$out/self-check-rosetta.json" processArchitecture)" == X64 ]] || fail "the Rosetta run was not x86_64"
  record "Intel: the x86_64 slice starts under Rosetta (no Intel hardware)"
else
  record "Intel: Rosetta is not installed, the x86_64 slice was not started"
fi

data="$(json_field "$out/self-check-release.json" persistentDataPath)"
mkdir -p "$data"
uuidgen > "$data/installer-smoke-marker.txt"
before="$(fingerprint "$data")"

# A damaged package (as after a broken transfer or a full disk) fails while Velopack extracts it: the app stays.
broken="$out/WotrOnline-broken-osx-full.nupkg"
head -c "$(( $(stat -f%z "$next_package") / 2 ))" "$next_package" > "$broken"
if "$app/Contents/MacOS/UpdateMac" apply --silent --norestart --package "$broken"; then
  fail "UpdateMac applied a damaged package"
fi
check_app "damaged package refused"
self_check after-damaged "$version" "$version"

"$app/Contents/MacOS/UpdateMac" apply --silent --norestart --package "$next_package" || fail "UpdateMac apply exit $?"
check_app update
self_check next "$next_version" "$version"
[[ "$(fingerprint "$data")" == "$before" ]] || fail "player data changed during the update"
record "update: $version -> $next_version without a password, player data unchanged"

rm -rf "$app"
[[ "$(fingerprint "$data")" == "$before" ]] || fail "player data changed when the app was removed"
record "removed the app; player data unchanged"
cp "$HOME/Library/Logs/velopack_WotrOnline.log" "$out/" 2>/dev/null || true
record PASS
