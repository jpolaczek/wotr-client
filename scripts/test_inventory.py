"""Current Unity discovery, source fingerprints and exact test-result accounting."""
import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def source_manifest(root=ROOT):
    paths = subprocess.check_output(['git', 'ls-files', '--cached', '--others', '--exclude-standard',
        '-z', '--', 'unity/Wotr.Client/Assets/Wotr', 'unity/Wotr.Client/Packages',
        'unity/Wotr.Client/ProjectSettings', 'scripts', 'wotr-deps.lock.json'], cwd=root).decode().split('\0')
    return {name: digest(root / name) if (root / name).is_file() else None for name in sorted(set(paths)) if name}


def cases(path, results=False):
    tree = ET.parse(path).getroot()
    entries = {}
    for case in tree.iter('test-case'):
        name = case.attrib['fullname']
        if name in entries:
            raise ValueError(f'Duplicate test case: {name}')
        if results:
            entries[name] = {'result': case.attrib.get('result', 'Missing'),
                             'message': case.findtext('failure/message', ''),
                             'stack': case.findtext('failure/stack-trace', '')}
        else:
            entries[name] = case.attrib['classname']
    if not entries:
        raise ValueError('No test cases in ' + str(path))
    return entries


def inventory(paths, root=ROOT):
    found = {}
    sources = source_manifest(root)
    for path in paths:
        metadata = json.loads(Path(str(path) + '.json').read_text())
        if metadata.get('kind') != 'unity-discovery' or metadata.get('mode') != 'PlayMode':
            raise ValueError('A current PlayMode discovery is required, not a historical result XML.')
        if metadata['xml_sha256'] != digest(path) or metadata['source_files'] != sources:
            raise ValueError('Discovery or test sources changed; run discover-tests.py again.')
        current = cases(path)
        if found.keys() & current.keys():
            raise ValueError('Discovery inputs overlap.')
        found.update(current)
    return found


def validate_selection(required, actual):
    missing = sorted(set(required) - actual.keys())
    extra = sorted(actual.keys() - set(required))
    if missing or extra:
        raise ValueError(f'Selection mismatch: missing={missing}, extra={extra}')
    return [name for name in required if actual[name]['result'] != 'Passed']


def provenance(root=ROOT):
    lock = json.loads((root / 'wotr-deps.lock.json').read_text())
    return {'source_commit': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip(),
            'working_diff_sha256': hashlib.sha256(subprocess.check_output(['git', 'diff', 'HEAD'], cwd=root)).hexdigest(),
            'source_files': source_manifest(root), 'dependency_lock_sha256': digest(root / 'wotr-deps.lock.json'),
            'backend_packages': lock.get('testPackages', []),
            'unity_project_version': (root / 'unity/Wotr.Client/ProjectSettings/ProjectVersion.txt').read_text()}
