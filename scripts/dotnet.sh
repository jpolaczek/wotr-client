#!/usr/bin/env bash
set -euo pipefail
wotr_root="$(cd "$(dirname "$0")/.." && pwd)"
if [ -x "$wotr_root/.artifacts/dotnet/dotnet" ]; then
  exec "$wotr_root/.artifacts/dotnet/dotnet" "$@"
fi
exec dotnet "$@"
