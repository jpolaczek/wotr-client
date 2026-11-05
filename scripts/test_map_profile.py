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
            'Screenshots': [f'{index}.png' for index in range(REVIEW.SCREENSHOTS)], 'Results': []}
        for name in self.report['Screenshots']:
            (self.folder / name).touch()
        for kind in REVIEW.KINDS:
            for width, height, scale in REVIEW.LAYOUTS:
                self.report['Results'].append(self.case(kind, width, height, scale))

    @staticmethod
    def case(kind, width, height, scale):
        builds = {'override-fate': 9, 'override-whitehand': 2}.get(kind, 0) * 480
        case = {
            'Kind': kind, 'Width': width, 'Height': height, 'Scale': scale,
            'BorderBuilds': builds, 'ExpectedBorderBuilds': builds, 'BorderRepaints': builds,
            'Metrics': {'WarmupFrames': 120, 'SampleFrames': 360, 'Fps': 120,
                        'FrameMilliseconds': {'P95': 8, 'P99': 9},
                        'ToRenderEndMilliseconds': None if kind == 'idle' else {'Max': 10}},
            'Memory': {'GraphicsDriverEstimatedPeakBytes': 100000000}, 'Drag': None}
        if kind.startswith('drag-'):
            case['Drag'] = {'Side': 'FreePeoples' if kind.endswith('-fp') else 'Shadow', 'Units': 5,
                            'SampleFrames': 360, 'CarriedFrames': 360, 'PannedFrames': 24, 'PannedPoints': 310.5}
        return case

    def validate(self):
        (self.folder / 'report.json').write_text(json.dumps(self.report))
        return REVIEW.validate(self.folder)

    def drag(self, side='Shadow'):
        kind = 'drag-edge-fp' if side == 'FreePeoples' else 'drag-edge-sh'
        return next(case for case in self.report['Results'] if case['Kind'] == kind)

    def test_accepts_all_workloads_with_required_measurements(self):
        self.assertEqual(44, len(self.validate()['Results']))
        self.assertEqual(50, REVIEW.SCREENSHOTS)

    def test_rejects_previous_profile_without_refresh_or_dense_workloads(self):
        self.report['Results'] = self.report['Results'][:12]
        with self.assertRaisesRegex(SystemExit, 'Incomplete'):
            self.validate()

    def test_rejects_the_profile_before_the_drag_workloads(self):
        self.report['Results'] = [case for case in self.report['Results'] if not case['Kind'].startswith('drag-')]
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

    def test_rejects_a_drag_workload_whose_edge_pan_never_moved_the_map(self):
        self.drag()['Drag']['PannedPoints'] = 0
        with self.assertRaisesRegex(SystemExit, 'edge pan'):
            self.validate()

    def test_rejects_a_drag_workload_that_dropped_its_army_while_measuring(self):
        self.drag('FreePeoples')['Drag']['CarriedFrames'] = 359
        with self.assertRaisesRegex(SystemExit, 'every measured drag frame'):
            self.validate()

    def test_rejects_a_drag_workload_carrying_the_other_sides_army(self):
        self.drag()['Drag']['Side'] = 'FreePeoples'
        with self.assertRaisesRegex(SystemExit, 'own carried army'):
            self.validate()


class DragInputTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.path = Path(self.directory.name) / 'ui407-map-drag.json'
        self.views = {'drag-fp': {'You': 0, 'Decision': {'Id': 'a'}}, 'drag-sh': {'You': 1, 'Decision': {'Id': 'b'}}}

    def check(self):
        self.path.write_text(json.dumps(self.views))
        REVIEW.validate_drag_input(self.path)

    def test_accepts_each_sides_own_decision(self):
        self.check()

    def test_rejects_a_missing_side(self):
        del self.views['drag-sh']
        with self.assertRaisesRegex(SystemExit, 'Shadow'):
            self.check()

    def test_rejects_the_other_sides_view(self):
        self.views['drag-fp']['You'] = 1
        with self.assertRaisesRegex(SystemExit, 'FreePeoples'):
            self.check()

    def test_rejects_credentials_in_the_input(self):
        self.views['drag-fp']['Token'] = 'secret'
        with self.assertRaisesRegex(SystemExit, 'projections only'):
            self.check()


if __name__ == '__main__':
    unittest.main()
