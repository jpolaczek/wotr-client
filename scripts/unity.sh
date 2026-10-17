#!/usr/bin/env bash
set -euo pipefail
wotr_root="$(cd "$(dirname "$0")/.." && pwd)"
wotr_editor="${WOTR_UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$wotr_root/.artifacts"
# Fetch and verify the pinned Contracts and public content before Unity resolves packages.
python3 "$wotr_root/scripts/bootstrap_deps.py"
wotr_args=()
wotr_path_next=false
for wotr_arg in "$@"; do
  if $wotr_path_next && [[ "$wotr_arg" != /* ]] && [[ "$wotr_arg" != "-" ]]; then
    wotr_arg="$wotr_root/$wotr_arg"
  fi
  wotr_args+=("$wotr_arg")
  wotr_path_next=false
  case "$wotr_arg" in -logFile|-testResults) wotr_path_next=true ;; esac
done
exec "$wotr_editor" -batchmode -projectPath "$wotr_root/unity/Wotr.Client" "${wotr_args[@]}"
