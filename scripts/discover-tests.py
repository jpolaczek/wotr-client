#!/usr/bin/env python3
"""Export the current Unity test tree and bind it to exact source files."""
import argparse
import json
import os
import subprocess
from pathlib import Path
from test_inventory import ROOT, cases, digest, provenance, source_manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--mode', choices=['PlayMode', 'EditMode'], default='PlayMode')
    args = parser.parse_args()
    path = args.output.resolve()
    if path.exists() or Path(str(path) + '.json').exists():
        raise ValueError('Use a new output path; discovery evidence is immutable.')
    path.parent.mkdir(parents=True, exist_ok=True)
    before = source_manifest()
    env = dict(os.environ, WOTR_TEST_DISCOVERY_OUTPUT=str(path), WOTR_TEST_DISCOVERY_MODE=args.mode)
    subprocess.run(['bash', 'scripts/unity.sh', '-executeMethod', 'Wotr.Verification.TestDiscovery.Export',
                    '-logFile', str(path.with_suffix('.log'))], cwd=ROOT, env=env, check=True)
    if source_manifest() != before:
        raise RuntimeError('Sources changed during discovery (including Unity import). Repeat with a new output path.')
    discovered = cases(path)
    metadata = dict(provenance(), kind='unity-discovery', mode=args.mode, xml_sha256=digest(path), count=len(discovered))
    Path(str(path) + '.json').write_text(json.dumps(metadata, indent=2) + '\n')
    print(f'PASS: discovered {len(discovered)} {args.mode} cases with current source provenance.')


if __name__ == '__main__':
    main()
