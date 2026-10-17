#!/usr/bin/env bash
set -euo pipefail
wotr_root="$(cd "$(dirname "$0")/.." && pwd)"
wotr_pg="${WOTR_PG_BIN:-/opt/homebrew/opt/postgresql@17/bin}"
wotr_data="$wotr_root/.artifacts/postgres/data"
wotr_socket="$wotr_root/.artifacts/postgres"
mkdir -p "$wotr_socket"
case "${1:-start}" in
  start)
    if [[ ! -f "$wotr_data/PG_VERSION" ]]; then "$wotr_pg/initdb" -D "$wotr_data" -U wotr -A trust --encoding=UTF8 --locale=C > "$wotr_socket/init.log"; fi
    if ! "$wotr_pg/pg_ctl" -D "$wotr_data" status >/dev/null 2>&1; then "$wotr_pg/pg_ctl" -D "$wotr_data" -l "$wotr_socket/server.log" -o "-p 55432 -h 127.0.0.1 -k $wotr_socket" start; fi
    for wotr_db in wotr wotr_test; do
      if [[ "$("$wotr_pg/psql" -h 127.0.0.1 -p 55432 -U wotr -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$wotr_db'")" != 1 ]]; then "$wotr_pg/createdb" -h 127.0.0.1 -p 55432 -U wotr "$wotr_db"; fi
    done ;;
  stop) "$wotr_pg/pg_ctl" -D "$wotr_data" stop -m fast ;;
  *) exit 2 ;;
esac
