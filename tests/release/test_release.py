"""Release gates must preserve local editor preferences without accepting dirty inputs."""
import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch


SPEC = importlib.util.spec_from_file_location("wotr_release", Path(__file__).resolve().parents[2] / "scripts/release.py")
RELEASE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RELEASE)


class SourceGateTests(unittest.TestCase):
    def check_gate(self, status):
        commit = b"a" * 40
        with patch.object(RELEASE, "run", side_effect=[SimpleNamespace(stdout=status), SimpleNamespace(stdout=commit)]):
            return RELEASE.source_commit()

    def test_local_untracked_editor_preferences_preserve_source_commit(self):
        status = ("?? " + RELEASE.LOCAL_EDITOR_PREFERENCES + "\0").encode()
        self.assertEqual("a" * 40, self.check_gate(status))

    def test_tracked_settings_and_untracked_runtime_inputs_block_release(self):
        for status in [
            (" M " + RELEASE.LOCAL_EDITOR_PREFERENCES + "\0").encode(),
            b"?? unity/Wotr.Client/Assets/unreviewed.cs\0",
            b" M unity/Wotr.Client/Packages/manifest.json\0",
            b"R  unity/Wotr.Client/Assets/renamed.cs\0unity/Wotr.Client/Assets/original.cs\0",
        ]:
            with self.subTest(status=status), self.assertRaises(ValueError):
                self.check_gate(status)


class TagGateTests(unittest.TestCase):
    def check(self, tag, notes, version="0.9.0"):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "release/notes").mkdir(parents=True)
            (root / "release/version.json").write_text(json.dumps({"version": version}))
            if notes is not None:
                (root / f"release/notes/{version}.md").write_text(notes, encoding="utf-8")
            with patch.object(RELEASE, "ROOT", root):
                return RELEASE.check_tag(tag)

    def test_tag_names_the_version_with_its_notes(self):
        self.assertEqual("0.9.0", self.check("v0.9.0", "First release for macOS players.\n"))
        self.assertEqual("0.9.0", self.check("v0.9.0", "x" * RELEASE.NOTES_LIMIT + "\n\n"))

    def test_mismatched_tag_or_missing_or_empty_notes_stop_the_release(self):
        for tag, notes in [("v0.9.1", "Notes."), ("0.9.0", "Notes."), ("v0.9.0", None), ("v0.9.0", " \n "),
                           ("v0.9.0", "x" * (RELEASE.NOTES_LIMIT + 1))]:
            with self.subTest(tag=tag, notes=None if notes is None else len(notes)), self.assertRaises(ValueError):
                self.check(tag, notes)


if __name__ == "__main__":
    unittest.main()
