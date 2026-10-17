"""The libraries in Assets/Plugins are the pinned NuGet files (DIST-06 Velopack, DIST-07 BouncyCastle), and vpk
packs with the Velopack library's version."""
import hashlib
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
PLUGINS = ROOT / "unity/Wotr.Client/Assets/Plugins/Velopack"


class VelopackPluginTests(unittest.TestCase):
    def test_libraries_match_the_hashes_in_sources(self):
        folders = sorted(path for path in PLUGINS.parent.iterdir() if path.is_dir())
        self.assertEqual(["BouncyCastle", "Velopack"], [folder.name for folder in folders])
        for folder in folders:
            sources = (folder / "SOURCES.md").read_text()
            pinned = dict((name, digest) for digest, name in re.findall(r"^([0-9a-f]{64})  (\S+\.dll)$", sources, re.M))
            present = {path.name for path in folder.glob("*.dll")}
            self.assertEqual(present, set(pinned), folder.name)
            for name, digest in pinned.items():
                with self.subTest(library=name):
                    self.assertEqual(digest, hashlib.sha256((folder / name).read_bytes()).hexdigest())

    def test_vpk_has_the_library_version(self):
        library = re.search(r"\| velopack ([0-9.]+) \|", (PLUGINS / "SOURCES.md").read_text()).group(1)
        packer = re.search(r'^VPK_VERSION = "([0-9.]+)"$', (ROOT / "scripts/package_velopack.py").read_text(), re.M)
        self.assertEqual(library, packer.group(1))


if __name__ == "__main__":
    unittest.main()
