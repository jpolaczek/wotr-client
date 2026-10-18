"""SEC-04: every third-party action in this repository's workflows is pinned to a full commit SHA.

A tag such as @v4 can be moved to other code; a commit cannot. Dependabot (.github/dependabot.yml) proposes the
updates, with the version in a comment after the SHA.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
USES = re.compile(r"^\s*-?\s*uses:\s*([^\s#]+)\s*(#\s*(\S+))?", re.MULTILINE)
PINNED = re.compile(r"^[\w.-]+/[\w./-]+@[0-9a-f]{40}$")


class WorkflowPinTests(unittest.TestCase):
    def test_every_action_is_pinned_to_a_commit_with_its_version_noted(self):
        workflows = sorted((ROOT / ".github" / "workflows").glob("*.yml"))
        self.assertTrue(workflows)
        for workflow in workflows:
            for match in USES.finditer(workflow.read_text(encoding="utf-8")):
                action, version = match.group(1), match.group(3)
                if action.startswith("./"):
                    continue
                with self.subTest(workflow=workflow.name, action=action):
                    self.assertRegex(action, PINNED)
                    self.assertIsNotNone(version, "the version goes in a comment after the SHA")


if __name__ == "__main__":
    unittest.main()
