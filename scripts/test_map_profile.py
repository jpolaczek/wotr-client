#!/usr/bin/env python3
"""Reject incomplete or misleading Development Player performance reports."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

SPEC = importlib.util.spec_from_file_location('map_review', Path(__file__).with_name('review-map-client.py'))
REVIEW = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(REVIEW)


class MapProfileReportTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.folder = Path(self.directory.name)
        self.report = {
            'Version': 'map-profile-2', 'Editor': False, 'Development': True,
            'Texture': {'Bytes': 11185272, 'Format': 'DXT1'},
            'GeometryMemory': {'CoordinatePayloadBytes': 1137312},
            'Screenshots': [f'{index}.png' for index in range(42)], 'Results': []}
        for name in self.report['Screenshots']:
            (self.folder / name).touch()
        for kind in REVIEW.KINDS:
            for width, height in ((1366, 768), (1920, 1080)):
                for scale in (1, 1.25):
                    builds = {'override-fate': 9, 'override-whitehand': 2}.get(kind, 0) * 480
                    self.report['Results'].append({
                        'Kind': kind, 'Width': width, 'Height': height, 'Scale': scale,
                        'BorderBuilds': builds, 'ExpectedBorderBuilds': builds, 'BorderRepaints': builds,
                        'Metrics': {'WarmupFrames': 120, 'SampleFrames': 360, 'Fps': 120,
                                    'FrameMilliseconds': {'P95': 8, 'P99': 9},
                                    'ToRenderEndMilliseconds': None if kind == 'idle' else {'Max': 10}},
                        'Memory': {'GraphicsDriverEstimatedPeakBytes': 100000000}})

    def validate(self):
        (self.folder / 'report.json').write_text(json.dumps(self.report))
        return REVIEW.validate(self.folder)

    def test_accepts_all_workloads_with_required_measurements(self):
        self.assertEqual(36, len(self.validate()['Results']))

    def test_rejects_previous_profile_without_refresh_or_dense_workloads(self):
        self.report['Results'] = self.report['Results'][:12]
        with self.assertRaisesRegex(SystemExit, 'Incomplete'):
            self.validate()

    def test_rejects_unnecessary_static_mesh_rebuild(self):
        self.report['Results'][0]['BorderBuilds'] = 1
        with self.assertRaisesRegex(SystemExit, 'unexpected set'):
            self.validate()

    def test_rejects_uncompressed_terrain_even_when_bytes_are_small(self):
        self.report['Texture']['Format'] = 'RGB24'
        with self.assertRaisesRegex(SystemExit, 'uncompressed'):
            self.validate()

    def test_rejects_slow_local_feedback(self):
        self.report['Results'][4]['Metrics']['ToRenderEndMilliseconds']['Max'] = 101
        with self.assertRaisesRegex(SystemExit, '100 ms'):
            self.validate()

    def test_rejects_missing_screenshot(self):
        (self.folder / '3.png').unlink()
        with self.assertRaisesRegex(SystemExit, 'screenshots'):
            self.validate()


if __name__ == '__main__':
    unittest.main()
