#!/usr/bin/env python3
"""Audit MENU-08 NUnit evidence against current sources and reviewed final reruns."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import runpy
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def check_source_inventory(required):
    pattern = re.compile(
        r'(?P<attributes>(?:\[\s*(?:UnityTest|Test|TestCase)\b[^\]]*\]\s*)+)'
        r'(?:public|private|internal)\s+(?:static\s+)?(?:async\s+)?'
        r'[\w.<>]+\s+(?P<name>\w+)\s*\(')
    methods = {}
    folder = ROOT / 'unity/Wotr.Client/Assets/Wotr/Tests/PlayMode'
    for path in sorted(folder.glob('*.cs')):
        source = path.read_text()
        namespace = re.search(r'\bnamespace\s+([\w.]+)', source)
        classname = re.search(r'\bclass\s+(\w+)', source)
        for match in pattern.finditer(source):
            name = f'{namespace[1]}.{classname[1]}.{match["name"]}'
            assert name not in methods, f'Duplicate test method: {name}'
            methods[name] = len(re.findall(r'\[\s*TestCase\b', match['attributes'])) or 1
    assert methods, 'No current PlayMode methods found.'
    actual = Counter(name.split('(', 1)[0] for name in required)
    differences = {name: {'source': methods.get(name), 'inventory': actual.get(name)}
                   for name in methods.keys() | actual.keys()
                   if methods.get(name) != actual.get(name)}
    assert not differences, f'Current source/inventory mismatch: {differences}'
    return len(methods)


def reports(summary):
    observed = {}
    references = {}
    for batch in summary['batches']:
        path = ROOT / batch['report']
        cases = list(ET.parse(path).getroot().iter('test-case'))
        assert set(batch['filter']) <= set(summary['required']), path
        assert len({case.attrib['fullname'] for case in cases}) == len(cases), path
        assert len(cases) == batch['executed'], path
        assert {case.attrib['fullname'] for case in cases} == set(batch['filter']), path
        failed = False
        for case in cases:
            name = case.attrib['fullname']
            result = {
                'result': case.attrib['result'],
                'message': case.findtext('failure/message', ''),
                'stack': case.findtext('failure/stack-trace', ''),
            }
            observed[name] = result
            references[name] = batch['report']
            failed |= result['result'] != 'Passed'
        assert batch['exit_code'] == 0 or failed, path
    assert observed == summary['outcomes']
    return observed, references


def input_delta(previous, current):
    return {path: {'before': previous.get(path), 'after': current.get(path)}
            for path in sorted(previous.keys() | current.keys()) if previous.get(path) != current.get(path)}


def check_complete_test_classes(paths, selected, required):
    for path in paths:
        if '/Tests/PlayMode/' in path and path.endswith('.cs') and (ROOT / path).exists():
            classname = re.search(r'\bclass\s+(\w+)', (ROOT / path).read_text())
            if classname:
                cases = {name for name in required if name.split('.')[2] == classname[1]}
                assert cases <= selected, f'Changed test class was not rerun: {path}'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('full', type=Path)
    parser.add_argument('repair', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--reviewed-delta', type=Path,
                        help='Exact before/after manifest for inputs changed since the complete run.')
    parser.add_argument('--affected-suite', action='append', default=[],
                        help='Every suite affected by the reviewed input delta; repeat as needed.')
    parser.add_argument('--final-test-rerun', type=Path,
                        help='Complete classes rerun after a final test-only correction; shipping inputs must be identical.')
    args = parser.parse_args()
    full = json.loads(args.full.read_text())
    repair = json.loads(args.repair.read_text())
    required = set(full['required'])
    assert len(required) == len(full['required']), 'Duplicate required cases.'
    method_count = check_source_inventory(required)
    assert set(repair['required']) <= required
    assert not repair['missing']
    current = runpy.run_path(str(ROOT / 'scripts/test-client-batches.py'))['source_manifest']()
    final = json.loads(args.final_test_rerun.read_text()) if args.final_test_rerun else repair
    assert current == final['source_files'], 'Inputs changed after the final rerun.'
    assert not final['failed'] and not final['missing']
    assert final['passed'] == len(final['required'])
    assert set(final['required']) <= required
    test_delta = input_delta(repair['source_files'], current)
    if args.final_test_rerun:
        assert test_delta, 'A final test rerun must explain an actual test-only input correction.'
        assert all('/Tests/PlayMode/' in path and path.endswith('.cs') for path in test_delta), \
            'Shipping or shared inputs changed; repeat all affected suites instead.'
        check_complete_test_classes(test_delta, set(final['required']), required)
    selected = set(repair['required']) | set(final['required'])
    previous = full['source_files']
    changed = input_delta(previous, current)
    if changed:
        assert args.reviewed_delta, 'Changed inputs need an explicit reviewed delta manifest.'
        assert json.loads(args.reviewed_delta.read_text()) == changed, 'Reviewed delta differs from current inputs.'
        assert args.affected_suite, 'List every suite affected by the reviewed changes.'
        for suite in args.affected_suite:
            cases = {name for name in required if name.split('.')[2] == suite}
            assert cases, f'Unknown affected suite: {suite}'
            assert cases <= selected, f'Incomplete affected suite: {suite}'
        check_complete_test_classes(changed, selected, required)
    outcomes, references = reports(full)
    repaired, repaired_references = reports(repair)
    outcomes.update(repaired)
    references.update(repaired_references)
    if args.final_test_rerun:
        final_outcomes, final_references = reports(final)
        outcomes.update(final_outcomes)
        references.update(final_references)
    assert set(outcomes) == required
    assert all(item['result'] == 'Passed' for item in outcomes.values())
    original = json.loads((ROOT / 'docs/ui/profiles/menu-full-play-failures.json').read_text())
    assert len(original) == 114
    assert all(outcomes[item['test']]['result'] == 'Passed' for item in original)
    shipping_delta = [path for path in changed if '/Runtime/' in path or '/Resources/' in path]
    result = {
        'result': 'PASS', 'passed': len(required), 'failed': 0, 'skipped': 0, 'missing': [],
        'original_failures_resolved': len(original),
        'source_methods': method_count,
        'full_run': str(args.full), 'full_source_commit': full['source_commit'],
        'affected_suite_rerun': str(args.repair), 'repair_source_commit': repair['source_commit'],
        'final_test_rerun': str(args.final_test_rerun) if args.final_test_rerun else None,
        'final_source_commit': final['source_commit'],
        'test_only_delta_after_affected_rerun': test_delta,
        'affected_rerun_initial_failures': repair['failed'],
        'changed_inputs_after_full_run': changed,
        'shipping_runtime_inputs_unchanged_after_full_run': not shipping_delta,
        'shipping_runtime_delta': shipping_delta,
        'affected_suites': args.affected_suite,
        'method': 'Complete frozen inventory plus every affected suite; final test-only correction reruns its complete class with identical shipping inputs.',
        'reports_by_case': references,
        'final_source_manifest_sha256': hashlib.sha256(
            json.dumps(current, sort_keys=True).encode()).hexdigest(),
    }
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(f'PASS: {len(required)}/{len(required)}, zero skips or missing cases; all original 114 failures resolved.')


if __name__ == '__main__':
    main()
