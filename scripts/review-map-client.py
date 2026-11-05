#!/usr/bin/env python3
"""Capture and measure the actual Development Player map without accounts or a server."""
import argparse
import json
import plistlib
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
KINDS = ('idle', 'pan-zoom', 'highlight', 'pointer', 'projection',
         'override-fate', 'override-whitehand', 'stress-pan', 'stress-hover',
         'drag-edge-fp', 'drag-edge-sh')
LAYOUTS = tuple((width, height, scale) for width, height in ((1366, 768), (1920, 1080)) for scale in (1, 1.25))
# Per layout: the whole map, the maximum zoom and the legend, plus one picture per stress, override and drag workload;
# then seven regions before and after their highlight.
SCREENSHOTS = len(LAYOUTS) * (3 + sum(kind.startswith(('stress-', 'override-', 'drag-')) for kind in KINDS)) + 14
# UI-407: the recorded projections the drag workloads carry an army on, one per side.
DRAG_VIEWS = {'drag-fp': 'FreePeoples', 'drag-sh': 'Shadow'}
# A projection's side, written as the enum's number or its name.
SIDES = {0: 'FreePeoples', 1: 'Shadow', 'FreePeoples': 'FreePeoples', 'Shadow': 'Shadow'}


def default_player():
    builds = ROOT / '.artifacts/builds'
    if sys.platform == 'darwin':
        contents = builds / 'mac/Wotr.app/Contents'
        with (contents / 'Info.plist').open('rb') as stream:
            executable = plistlib.load(stream)['CFBundleExecutable']
        return contents / 'MacOS' / executable
    return builds / 'windows/Wotr.exe'


def validate(folder):
    report = json.loads((folder / 'report.json').read_text())
    expected = {(kind, width, height, scale) for kind in KINDS for width, height, scale in LAYOUTS}
    actual = {(case['Kind'], case['Width'], case['Height'], case['Scale']) for case in report['Results']}
    if (report.get('Version') != 'map-profile-2' or report['Editor'] or not report['Development']
            or actual != expected or len(report['Results']) != len(expected)):
        raise SystemExit('Incomplete map review or report not from a Development Player.')
    if (len(report['Screenshots']) != SCREENSHOTS
            or any(not (folder / name).is_file() for name in report['Screenshots'])):
        raise SystemExit('Missing map review screenshots.')
    if report['Texture']['Bytes'] >= 20 * 1024 * 1024 or report['Texture']['Format'] != 'DXT1':
        raise SystemExit('Unexpected uncompressed terrain texture.')
    if report['GeometryMemory']['CoordinatePayloadBytes'] <= 0:
        raise SystemExit('Missing geometry allocation evidence.')
    for case in report['Results']:
        validate_case(case)
    return report


def validate_case(case):
    changed = {'override-fate': 9, 'override-whitehand': 2}.get(case['Kind'], 0)
    expected_builds = changed * (case['Metrics']['WarmupFrames'] + case['Metrics']['SampleFrames'])
    if case['BorderBuilds'] != expected_builds or case['ExpectedBorderBuilds'] != expected_builds:
        raise SystemExit('Map update rebuilt an unexpected set of national bands.')
    if not changed and case['BorderRepaints'] != 0:
        raise SystemExit('Map interaction unexpectedly regenerated static border meshes.')
    metrics = case['Metrics']
    if metrics['FrameMilliseconds']['P99'] < metrics['FrameMilliseconds']['P95']:
        raise SystemExit('Invalid frame percentiles.')
    if case['Width'] == 1920 and metrics['Fps'] < 60:
        raise SystemExit('1080p workload missed the 60 FPS target.')
    feedback = metrics['ToRenderEndMilliseconds']
    if feedback is not None and feedback['Max'] > 100:
        raise SystemExit('Local response exceeded 100 ms.')
    if case['Memory']['GraphicsDriverEstimatedPeakBytes'] <= 0:
        raise SystemExit('Missing Player graphics memory estimate.')
    if case['Kind'].startswith('drag-'):
        validate_drag(case)


def validate_drag(case):
    """UI-407: the side's own army was carried in every measured frame while the edge pan moved the map."""
    drag = case.get('Drag')
    side = 'FreePeoples' if case['Kind'].endswith('-fp') else 'Shadow'
    if not drag or drag['Side'] != side or drag['Units'] <= 0:
        raise SystemExit('A drag workload has no evidence of its own carried army.')
    if drag['SampleFrames'] != case['Metrics']['SampleFrames'] or drag['CarriedFrames'] != drag['SampleFrames']:
        raise SystemExit('The army was not carried in every measured drag frame.')
    if drag['PannedFrames'] <= 0 or drag['PannedPoints'] <= 0:
        raise SystemExit('The edge pan did not move the map under the carried army.')


def validate_drag_input(path):
    """The drag workloads need each side's own recorded decision with an army that can march."""
    views = json.loads(path.read_text())
    for name, side in DRAG_VIEWS.items():
        view = views.get(name)
        if not view or SIDES.get(view.get('You')) != side or not view.get('Decision'):
            raise SystemExit(f'Missing {side} drag projection: {name}.')
        if any(key in view for key in ('Password', 'Token', 'Resolution', 'RandomSamples')):
            raise SystemExit('The drag input must contain projections only.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--player', type=Path)
    parser.add_argument('--output', type=Path, default=ROOT / '.artifacts/map17-player')
    parser.add_argument('--input', type=Path, required=True, help='map17-profile.json from test-client scenario export')
    parser.add_argument('--drag-input', type=Path, default=ROOT / '.artifacts/ui407-map-drag.json',
                        help='ui407-map-drag.json recorded by the PlayMode TabletopAcceptanceMatrixTests')
    parser.add_argument('--timeout', type=int, default=900)
    args = parser.parse_args()
    player = (args.player or default_player()).resolve(strict=True)
    drag_input = args.drag_input.resolve(strict=True)
    validate_drag_input(drag_input)
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    report = output / 'report.json'
    report.unlink(missing_ok=True)
    command = [str(player), '-screen-fullscreen', '0', '-screen-width', '1366', '-screen-height', '768',
               '-logFile', str(output / 'player.log'), '--wotr-map-review', str(output),
               '--wotr-map-input', str(args.input.resolve(strict=True)), '--wotr-map-drag', str(drag_input)]
    with subprocess.Popen(command, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT) as process:
        try:
            code = process.wait(timeout=args.timeout)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
            raise SystemExit('Map review timed out; inspect player.log.')
    if code or not report.is_file():
        raise SystemExit(f'Map review failed ({code}); inspect {output / "player.log"}.')
    result = validate(output)
    print(f"{result['Platform']}, {result['CPU']}, {result['GPU']}, Unity {result['Unity']}")
    for case in result['Results']:
        print(f"{case['Kind']:18} {case['Width']}x{case['Height']} {case['Scale']:.2f}: "
              f"{case['Metrics']['Fps']:.2f} FPS, p99 {case['Metrics']['FrameMilliseconds']['P99']:.3f} ms, "
              f"vertices {case['GraphicsVertices']['Peak']}, "
              f"static border repaints {case['BorderRepaints']}")
    print(f"PASS: {len(result['Results'])} measured workloads, {SCREENSHOTS} screenshots. "
          f"Human visual review remains required: {output}")


if __name__ == '__main__':
    main()
