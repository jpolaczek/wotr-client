#!/usr/bin/env bash
set -euo pipefail
wotr_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$wotr_root"
if [[ -z "${WOTR_TEST_DB:-}" ]]; then
  bash scripts/postgres.sh start
  export WOTR_TEST_DB='Host=127.0.0.1;Port=55432;Database=wotr_test;Username=wotr'
fi
# QUAL-06: each invocation owns one database and server; never reset a shared schema.
if [[ -z "${WOTR_CLIENT_ISOLATED_DB:-}" ]]; then
  exec python3 scripts/test_fixture.py "$@"
fi
wotr_test_port="${WOTR_CLIENT_TEST_PORT:-5088}"
export WOTR_DB="$WOTR_TEST_DB"
export ASPNETCORE_ENVIRONMENT=Development
# QUAL-02 budgets are per account, and every PlayMode test shares the two fixture accounts. Integration runs the
# server with the highest allowed budgets; the limits themselves are tested by the server (GameRateLimitTests).
export WOTR_LIMIT_COMMANDS="${WOTR_LIMIT_COMMANDS:-10000}"
export WOTR_LIMIT_PREVIEWS="${WOTR_LIMIT_PREVIEWS:-10000}"
export WOTR_LIMIT_READS="${WOTR_LIMIT_READS:-10000}"
export WOTR_LIMIT_ROOMS="${WOTR_LIMIT_ROOMS:-10000}"
export WOTR_LIMIT_CONCURRENT="${WOTR_LIMIT_CONCURRENT:-256}"
export WOTR_LIMIT_SOCKETS="${WOTR_LIMIT_SOCKETS:-64}"
# AUTH-02: registrations count against the email limit (5 per IP address in 10 minutes); tests register many accounts.
export Wotr__Email__PermitsPerAddress="${Wotr__Email__PermitsPerAddress:-10000}"
export WOTR_CLIENT_TEST_REPOSITORY="$wotr_root"
wotr_test_results="${WOTR_CLIENT_TEST_RESULTS:-.artifacts/client-play.xml}"
wotr_test_directory="$(dirname "$wotr_test_results")"
mkdir -p "$wotr_test_directory"
wotr_test_fixture="$wotr_test_directory/client-integration.json"
# Client integration runs the pinned server and seeder from the lock; it never compiles the backend.
python3 scripts/bootstrap_deps.py fetch --test >/dev/null
wotr_server_dll="$wotr_root/.deps/test/wotr-server/Wotr.Server.dll"
export WOTR_CLIENT_TEST_SERVER="$wotr_server_dll"
bash scripts/dotnet.sh "$wotr_root/.deps/test/wotr-seeder/Wotr.Scenarios.dll" "$wotr_test_fixture"
python3 - "$wotr_test_port" "$wotr_test_fixture" <<'PYPORT'
import json,sys
p=sys.argv[2]
s=json.load(open(p));s['Address']='http://127.0.0.1:'+sys.argv[1];json.dump(s,open(p,'w'))
PYPORT
bash scripts/dotnet.sh "$wotr_server_dll" --urls "http://127.0.0.1:$wotr_test_port" > "$wotr_test_directory/client-server.log" 2>&1 &
wotr_server_pid=$!
trap 'kill "$wotr_server_pid" 2>/dev/null || true; wait "$wotr_server_pid" 2>/dev/null || true' EXIT
python3 - "$wotr_test_port" "$wotr_server_pid" <<'PY'
import time,urllib.request,sys,os
for attempt in range(50):
 try:
  os.kill(int(sys.argv[2]), 0)
  urllib.request.urlopen('http://127.0.0.1:'+sys.argv[1]+'/health',timeout=1)
  break
 except OSError: time.sleep(.1)
else: raise SystemExit('Local test server did not start.')
PY
export WOTR_CLIENT_TEST_CONFIG="$(cd "$wotr_test_directory" && pwd)/client-integration.json"
bash scripts/unity.sh -runTests -testPlatform PlayMode \
  -testResults "$wotr_test_results" \
  -logFile "${WOTR_CLIENT_TEST_LOG:-.artifacts/client-play.log}" "$@"
