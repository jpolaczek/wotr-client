#!/usr/bin/env python3
"""Run an explicit PlayMode inventory in batches, retaining every result and failure."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def inventory(paths):
    cases = {}
    for path in paths:
        for case in ET.parse(path).getroot().iter('test-case'):
            cases[case.attrib['fullname']] = case.attrib['classname']
    if not cases:
        raise ValueError('The inventory contains no test cases.')
    return cases


def outcome(path):
    cases = {}
    for case in ET.parse(path).getroot().iter('test-case'):
        cases[case.attrib['fullname']] = {
            'result': case.attrib['result'],
            'message': case.findtext('failure/message', ''),
            'stack': case.findtext('failure/stack-trace', ''),
        }
    return cases


def source_manifest():
    # The backend and seeder are archives pinned in the lock, so the lock stands for them (DIST-03).
    paths = subprocess.check_output(['git', 'ls-files', '-z', '--',
        'unity/Wotr.Client/Assets/Wotr', 'unity/Wotr.Client/Packages',
        'unity/Wotr.Client/ProjectSettings', 'scripts/bootstrap_deps.py', 'wotr-deps.lock.json',
        'scripts/unity.sh', 'scripts/test-client.sh'], cwd=ROOT).decode().split('\0')
    return {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
            for path in paths if path and (ROOT / path).is_file()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inventory', type=Path, nargs='+', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--only-failures', type=Path)
    parser.add_argument('--batch-size', type=int, default=8)
    parser.add_argument('--batching', choices=['class', 'mixed'], default='class',
                        help='Keep suites separate, or pack the complete inventory into smaller server batches.')
    args = parser.parse_args()
    required = inventory(args.inventory)
    if args.only_failures:
        failures = {item['test'] for item in json.loads(args.only_failures.read_text())}
        required = {name: cls for name, cls in required.items() if name in failures}
        if required.keys() != failures:
            raise ValueError('Some requested failures are absent from the inventory.')
    if args.batch_size < 1:
        raise ValueError('Batch size must be positive.')
    args.output.mkdir(parents=True, exist_ok=False)
    source = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    diff = subprocess.check_output(['git', 'diff', 'HEAD'], cwd=ROOT)
    sources = source_manifest()
    summary = {'source_commit': source, 'working_diff_sha256': hashlib.sha256(diff).hexdigest(),
               'source_files': sources,
               'required': sorted(required), 'batches': [], 'outcomes': {}}
    env = dict(os.environ)

    def run(names, label):
        if source_manifest() != sources:
            raise RuntimeError('Test inputs changed during the run; start a new verified inventory.')
        directory = args.output / label
        directory.mkdir()
        report = (directory / 'tests.xml').resolve()
        log = (directory / 'unity.log').resolve()
        settings = dict(env, WOTR_CLIENT_TEST_RESULTS=str(report), WOTR_CLIENT_TEST_LOG=str(log))
        filters = ';'.join('^' + re.escape(name) + '$' for name in names)
        with (directory / 'run.log').open('w') as output:
            process = subprocess.run(['bash', 'scripts/test-client.sh', '-testFilter', filters],
                                     cwd=ROOT, env=settings, stdout=output, stderr=subprocess.STDOUT)
        results = outcome(report) if report.exists() else {}
        summary['batches'].append({'name': label, 'filter': names, 'exit_code': process.returncode,
                                   'report': str(report.relative_to(ROOT)), 'executed': len(results)})
        summary['outcomes'].update(results)
        (args.output / 'results.json').write_text(json.dumps(summary, indent=2) + '\n')
        failed = sum(item['result'] != 'Passed' for item in results.values())
        print(f'{label}: {len(results) - failed}/{len(results)} passed; exit={process.returncode}', flush=True)
        if not results:
            raise RuntimeError(f'{label} produced no test cases; inspect its log before continuing.')
        if results.keys() != set(names):
            missing = sorted(set(names) - results.keys())
            extra = sorted(results.keys() - set(names))
            raise RuntimeError(f'{label} selection mismatch: missing={missing}, extra={extra}')
        if process.returncode != 0 and failed == 0:
            raise RuntimeError(f'{label} exited with {process.returncode} despite passing cases; inspect its logs.')
        return results

    suites = [sorted(required)] if args.batching == 'mixed' else [
        sorted(name for name in required if required[name] == cls) for cls in sorted(set(required.values()))]
    batches = [names[offset:offset + args.batch_size] for names in suites
               for offset in range(0, len(names), args.batch_size)]
    for number, batch in enumerate(batches, 1):
        results = run(batch, f'batch-{number:03d}')
        # Do not hide a rate limit behind a generic retry. Preserve the first report,
        # then give each affected case its own ordinary server with unchanged limits.
        limited = [name for name, result in results.items() if 'TooManyRequests' in result['message']]
        for index, name in enumerate(limited):
            run([name], f'batch-{number:03d}-rate-{index + 1}')
    missing = sorted(required.keys() - summary['outcomes'].keys())
    if source_manifest() != sources:
        raise RuntimeError('Test inputs changed before the final coverage audit.')
    failed = [name for name in required if summary['outcomes'].get(name, {}).get('result') != 'Passed']
    summary.update(missing=missing, failed=failed, passed=len(required) - len(failed))
    (args.output / 'results.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(f'{summary["passed"]}/{len(required)} passed; {len(missing)} missing.', flush=True)
    return 1 if failed else 0


if __name__ == '__main__':
    raise SystemExit(main())
