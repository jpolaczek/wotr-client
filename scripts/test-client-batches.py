#!/usr/bin/env python3
"""Execute a current discovered PlayMode inventory with isolated fixtures and strict accounting."""
import argparse
import json
import os
from pathlib import Path
import random
import re
import subprocess
from test_inventory import ROOT, cases, digest, inventory, provenance, source_manifest, validate_selection


def make_batches(required, size, batching, seed):
    groups = [sorted(required)] if batching == 'mixed' else [
        sorted(name for name in required if required[name] == cls) for cls in sorted(set(required.values()))]
    rng = random.Random(seed)
    if seed is not None:
        for group in groups:
            rng.shuffle(group)
        rng.shuffle(groups)
    return [group[offset:offset + size] for group in groups for offset in range(0, len(group), size)]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inventory', type=Path, nargs='+', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--only-failures', type=Path)
    parser.add_argument('--batch-size', type=int, default=8)
    parser.add_argument('--batching', choices=['class', 'mixed'], default='class')
    parser.add_argument('--seed', type=int, help='Shuffle batch membership and execution order reproducibly.')
    args = parser.parse_args()
    required = inventory(args.inventory)
    complete = sorted(required)
    if args.only_failures:
        failures = {item['test'] for item in json.loads(args.only_failures.read_text())}
        required = {name: cls for name, cls in required.items() if name in failures}
        if required.keys() != failures or not required:
            raise ValueError('Requested failures must be nonempty and belong to the discovered inventory.')
    if args.batch_size < 1:
        raise ValueError('Batch size must be positive.')
    args.output.mkdir(parents=True, exist_ok=False)
    summary = dict(provenance(), required=sorted(required), complete_inventory=complete,
                   scope='targeted' if args.only_failures else 'complete', seed=args.seed,
                   inventory_files={str(p): digest(p) for p in args.inventory}, batches=[], outcomes={}, status='running')
    sources = summary['source_files']

    def save():
        (args.output / 'results.json').write_text(json.dumps(summary, indent=2) + '\n')

    save()
    try:
        for number, names in enumerate(make_batches(required, args.batch_size, args.batching, args.seed), 1):
            if source_manifest() != sources:
                raise RuntimeError('Test inputs changed; start a new verified discovery and run.')
            directory = args.output / f'batch-{number:03d}'
            directory.mkdir()
            report, log = (directory / 'tests.xml').resolve(), (directory / 'unity.log').resolve()
            settings = dict(os.environ, WOTR_CLIENT_TEST_RESULTS=str(report), WOTR_CLIENT_TEST_LOG=str(log))
            filters = ';'.join('^' + re.escape(name) + '$' for name in names)
            with (directory / 'run.log').open('w') as stream:
                result = subprocess.run(['bash', 'scripts/test-client.sh', '-testFilter', filters],
                                        cwd=ROOT, env=settings, stdout=stream, stderr=subprocess.STDOUT)
            actual = cases(report, results=True) if report.exists() else {}
            # Never overwrite a first failure with a retry. Reruns require a separate output directory.
            batch = {'name': directory.name, 'filter': names, 'exit_code': result.returncode,
                     'report': str(report), 'report_sha256': digest(report) if report.exists() else None,
                     'outcomes': actual}
            isolation = directory / 'isolation.json'
            batch['isolation'] = json.loads(isolation.read_text()) if isolation.exists() else None
            summary['batches'].append(batch)
            summary['outcomes'].update(actual)
            save()
            failed = validate_selection(names, actual)
            if not batch['isolation'] or batch['isolation']['cleanup'] != 'dropped':
                raise RuntimeError('Missing proof of isolated fixture cleanup.')
            if result.returncode and not failed:
                raise RuntimeError(f'{directory.name} exited {result.returncode} despite passing tests.')
            print(f'{directory.name}: {len(actual)-len(failed)}/{len(actual)} passed; exit={result.returncode}', flush=True)
        if source_manifest() != sources:
            raise RuntimeError('Test sources changed before final coverage verification.')
        failed = validate_selection(required, summary['outcomes'])
        summary.update(failed=failed, missing=[], passed=len(required)-len(failed), status='failed' if failed else 'passed')
        save()
        print(f'{summary["passed"]}/{len(required)} passed; scope={summary["scope"]}.', flush=True)
        return 1 if failed else 0
    except BaseException as error:
        summary.update(status='incomplete', error=str(error), missing=sorted(set(required)-summary['outcomes'].keys()))
        save()
        raise


if __name__ == '__main__':
    raise SystemExit(main())
