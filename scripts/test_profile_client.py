"""Evidence validation for the presentation suites; does not launch a Player."""
import copy
import hashlib
import importlib.util
from pathlib import Path
import unittest


SPEC = importlib.util.spec_from_file_location("profile_client", Path(__file__).with_name("profile-client.py"))
PROFILE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROFILE)


def report(suite):
    summary = {"Count": 24, "Mean": 2, "P95": 3, "P99": 4, "Max": 5}
    results = []
    for kind in PROFILE.KINDS[suite]:
        for width, height in ((1366, 768), (1920, 1080)):
            for scale in (1, 1.25):
                item = {"Kind": kind, "Width": width, "Height": height, "Scale": scale,
                                "ReducedMotion": scale == 1.25,
                                "Metrics": {"WarmupFrames": 120, "SampleFrames": 360, "Fps": 120,
                                            "FrameMilliseconds": dict(summary, Count=360),
                                            "MainThreadMilliseconds": None, "GcBytesPerFrame": None,
                                            "HandlerMilliseconds": copy.deepcopy(summary) if kind != "map-idle" else None,
                                            "ToRenderEndMilliseconds": copy.deepcopy(summary) if kind != "map-idle" else None}}
                if suite == "figure-motion":
                    hidden = kind.startswith("motion-hidden")
                    translates = scale == 1 and not hidden and not kind.startswith("motion-separation")
                    item["MotionEvidence"] = {"BeforeRevision": 2, "AfterRevision": 3,
                        "ExpectedTranslation": translates, "ExpectedFigures": 0 if hidden else 3,
                        "Frames": [{"Phase": phase, "Elapsed": elapsed, "ActiveGroups": 0 if hidden or phase == "finished" else 1,
                                    "X": 20 if translates and phase == "middle" else 0, "Y": 0}
                                   for phase, elapsed in (("start", .01), ("middle", .25), ("finished", 1.4))]}
                results.append(item)
    return {"Editor": False, "Development": True, "Suite": suite, "Results": results}


class ProfileClientTests(unittest.TestCase):
    def test_clean_player_log_accepts_completed_workloads(self):
        PROFILE.validate_player_log("WOTR_PROFILE_CASE battle 1920x1080 1.25\n"
                                    "WOTR_PROFILE_CASE card 1920x1080 1.25\n")

    def test_recursive_layout_failure_rejects_successful_player_metrics(self):
        log = ("WOTR_PROFILE_CASE battle 1920x1080 1.25\n"
               "Layout update failed to stabilize after 10 iterations. Cause: One or more elements are "
               "responding to GeometryChangedEvent by modifying layout properties, creating a recursive loop. "
               "Solution: Avoid modifying layout properties in GeometryChangedEvent callbacks. "
               "Root element: PanelRootElement WotrPanel(Clone)\n"
               "UnityEngine.UIElements.VisualTreeLayoutUpdater:Update ()\n"
               "WOTR_PROFILE_CASE card 1920x1080 1.25\n")
        with self.assertRaisesRegex(ValueError, "layout failed to stabilize"):
            PROFILE.validate_player_log(log)

    def test_report_is_bound_to_the_exact_input_bytes(self):
        original = hashlib.sha256(b'{"input":1}\n').hexdigest()
        changed = hashlib.sha256(b'{"input":2}\n').hexdigest()
        PROFILE.validate_input({"InputSha256": original}, original, original)
        for value in ({}, {"InputSha256": changed}):
            with self.assertRaisesRegex(ValueError, "does not match"):
                PROFILE.validate_input(value, original, original)

    def test_changed_input_rejects_even_a_matching_final_report(self):
        original = hashlib.sha256(b'{"input":1}\n').hexdigest()
        changed = hashlib.sha256(b'{"input":2}\n').hexdigest()
        for reported in (original, changed):
            with self.assertRaisesRegex(ValueError, "changed while"):
                PROFILE.validate_input({"InputSha256": reported}, original, changed)

    def test_preserves_56_legacy_and_adds_32_distinct_in_game_cases(self):
        for suite, count in (("legacy", 56), ("in-game", 32)):
            value = report(suite)
            self.assertEqual(count, len(value["Results"]))
            PROFILE.validate_report(value, suite)

    def test_board_requires_all_40_role_setup_and_menu_workloads(self):
        value = report("board")
        self.assertEqual(40, len(value["Results"]))
        PROFILE.validate_report(value, "board")
        value["Results"] = [item for item in value["Results"] if item["Kind"] != "setup-fate-fp"]
        with self.assertRaisesRegex(ValueError, "Incomplete"):
            PROFILE.validate_report(value, "board")

    def test_board_requires_24_actual_handler_and_render_end_measurements(self):
        for key in ("HandlerMilliseconds", "ToRenderEndMilliseconds"):
            value = report("board")
            value["Results"][0]["Metrics"][key]["Count"] = 23
            with self.assertRaisesRegex(ValueError, "interaction samples"):
                PROFILE.validate_report(value, "board")
        with self.assertRaises(ValueError):
            PROFILE.validate_report(report("in-game"), "board")

    def test_motion_requires_32_cases_and_recorded_intermediate_positions(self):
        value = report("figure-motion")
        self.assertEqual(32, len(value["Results"]))
        PROFILE.validate_report(value, "figure-motion")
        value["Results"][0]["MotionEvidence"]["Frames"][1]["X"] = 0
        with self.assertRaisesRegex(ValueError, "intermediate"):
            PROFILE.validate_report(value, "figure-motion")

    def test_motion_rejects_hidden_travel_and_reduced_motion_translation(self):
        for kind, scale in (("motion-hidden-fp", 1), ("motion-army-sh", 1.25)):
            value = report("figure-motion")
            item = next(item for item in value["Results"] if item["Kind"] == kind and item["Scale"] == scale)
            item["MotionEvidence"]["Frames"][1]["X"] = 30
            with self.assertRaisesRegex(ValueError, "intermediate"):
                PROFILE.validate_report(value, "figure-motion")

    def test_motion_rejects_gaps_and_missing_or_lingering_animation_frames(self):
        for mutate in (lambda evidence: evidence.__setitem__("AfterRevision", 7),
                       lambda evidence: evidence["Frames"].pop(),
                       lambda evidence: evidence["Frames"][2].__setitem__("ActiveGroups", 1)):
            value = report("figure-motion")
            mutate(value["Results"][0]["MotionEvidence"])
            with self.assertRaises(ValueError):
                PROFILE.validate_report(value, "figure-motion")

    def test_motion_requires_screenshot_files_in_addition_to_coordinates(self):
        import tempfile
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(ValueError, "screenshot"):
                PROFILE.validate_motion_images(report("figure-motion"), Path(directory))

    def test_missing_and_duplicated_workloads_fail(self):
        for mutation in (lambda value: value["Results"].pop(),
                         lambda value: value["Results"].append(value["Results"][0]),
                         lambda value: value["Results"].__setitem__(0, value["Results"][1])):
            value = report("in-game")
            mutation(value)
            with self.assertRaises(ValueError):
                PROFILE.validate_report(value, "in-game")

    def test_wrong_suite_or_editor_or_release_fails(self):
        for key, replacement in (("Suite", "legacy"), ("Editor", True), ("Development", False)):
            value = report("in-game")
            value[key] = replacement
            with self.assertRaises(ValueError):
                PROFILE.validate_report(value, "in-game")

    def test_input_workloads_cannot_pass_without_measured_feedback(self):
        for key in ("HandlerMilliseconds", "ToRenderEndMilliseconds"):
            value = report("in-game")
            value["Results"][0]["Metrics"][key] = None
            with self.assertRaises(ValueError):
                PROFILE.validate_report(value, "in-game")

    def test_insufficient_frames_or_missing_reduced_motion_fail(self):
        for key, replacement in (("WarmupFrames", 119), ("SampleFrames", 359)):
            value = report("in-game")
            value["Results"][0]["Metrics"][key] = replacement
            with self.assertRaises(ValueError):
                PROFILE.validate_report(value, "in-game")
        value = report("in-game")
        value["Results"][1]["ReducedMotion"] = False
        with self.assertRaises(ValueError):
            PROFILE.validate_report(value, "in-game")

    def test_maximum_not_only_p95_must_meet_100_milliseconds(self):
        for key in ("HandlerMilliseconds", "ToRenderEndMilliseconds"):
            value = report("in-game")
            value["Results"][0]["Metrics"][key]["Max"] = 100.01
            with self.assertRaises(ValueError):
                PROFILE.validate_report(value, "in-game")

    def test_nonfinite_or_negative_timings_fail(self):
        for bad in (float("nan"), float("inf"), -1):
            value = report("in-game")
            value["Results"][0]["Metrics"]["FrameMilliseconds"]["P95"] = bad
            with self.assertRaises(ValueError):
                PROFILE.validate_report(value, "in-game")

    def test_slow_or_nonfinite_fps_fails(self):
        for fps in (59.99, float("nan"), float("inf")):
            value = report("in-game")
            value["Results"][0]["Metrics"]["Fps"] = fps
            with self.assertRaises(ValueError):
                PROFILE.validate_report(value, "in-game")

    def test_legacy_reports_without_suite_keep_the_original_contract(self):
        value = report("legacy")
        value.pop("Suite")
        PROFILE.validate_report(value, "legacy")
        with self.assertRaises(ValueError):
            PROFILE.validate_report(value, "in-game")


if __name__ == "__main__":
    unittest.main()
