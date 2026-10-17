#!/usr/bin/env python3
"""Run one client test process with its own disposable database and server port."""
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import sys
import uuid

ROOT = Path(__file__).resolve().parents[1]
PAIR = re.compile(r'''\s*([^=;]+)\s*=\s*("(?:[^"]|"")*"|'(?:[^']|'')*'|[^;]*)(?:;|$)''')


def connection_fields(value):
    fields = {}
    position = 0
    while position < len(value):
        match = PAIR.match(value, position)
        if not match:
            raise ValueError('Unsupported test database connection string format.')
        key, raw = match.group(1).strip().lower(), match.group(2).strip()
        if raw.startswith(('"', "'")):
            quote = raw[0]
            raw = raw[1:-1].replace(quote * 2, quote)
        fields[key] = raw
        position = match.end()
    return fields


def connection_string(fields):
    return ';'.join(key + '="' + value.replace('"', '""') + '"' for key, value in fields.items())


def postgres_env(fields):
    env = dict(os.environ, PGDATABASE='postgres')
    for key, target, default in [('host', 'PGHOST', '127.0.0.1'), ('port', 'PGPORT', '55432'),
                                 ('username', 'PGUSER', 'wotr'), ('password', 'PGPASSWORD', '')]:
        env[target] = fields.get(key, default)
    return env


def free_port():
    with socket.socket() as sock:
        sock.bind(('127.0.0.1', 0))
        return sock.getsockname()[1]


def main():
    fields = connection_fields(os.environ['WOTR_TEST_DB'])
    database = 'wotr_batch_' + uuid.uuid4().hex + '_test'
    tools = Path(os.environ.get('WOTR_PG_BIN', '/opt/homebrew/opt/postgresql@17/bin'))
    env = postgres_env(fields)
    subprocess.run([str(tools / 'createdb'), database], env=env, check=True)
    try:
        fields['database'] = database
        settings = dict(os.environ, WOTR_TEST_DB=connection_string(fields), WOTR_CLIENT_ISOLATED_DB=database,
                        WOTR_CLIENT_TEST_PORT=os.environ.get('WOTR_CLIENT_TEST_PORT') or str(free_port()))
        settings.pop('WOTR_CLIENT_TEST_RESET_DATABASE', None)
        directory = Path(settings.get('WOTR_CLIENT_TEST_RESULTS', '.artifacts/client-play.xml')).parent
        directory.mkdir(parents=True, exist_ok=True)
        evidence = directory / 'isolation.json'
        evidence.write_text(json.dumps({'database': database, 'port': int(settings['WOTR_CLIENT_TEST_PORT']),
                                        'cleanup': 'pending'}, indent=2) + '\n')
        result = subprocess.run(['bash', str(ROOT / 'scripts/test-client.sh'), *sys.argv[1:]], env=settings, cwd=ROOT)
        return result.returncode
    finally:
        subprocess.run([str(tools / 'dropdb'), '--force', '--if-exists', database], env=env, check=True)
        if 'evidence' in locals():
            record = json.loads(evidence.read_text())
            record['cleanup'] = 'dropped'
            evidence.write_text(json.dumps(record, indent=2) + '\n')


if __name__ == '__main__':
    raise SystemExit(main())
