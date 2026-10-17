#!/usr/bin/env python3
"""Release inputs: build the players on macOS from committed sources (DIST-06: scripts/package_velopack.py packs
them) and check that a release tag names release/version.json and has its notes (DIST-10)."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
# Folder: (BuildTasks method, Unity build target, ClientBuildInfo platform of DIST-05).
PLATFORMS = {"macos": ("BuildReleaseMac", "StandaloneOSX", "macos-universal"),
             "windows": ("BuildReleaseWindows", "StandaloneWindows64", "windows-x64"),
             "linux": ("BuildReleaseLinux", "StandaloneLinux64", "linux-x64")}
# Client inputs only (DIST-03): server sources reach the client as archives pinned in the lock.
SOURCE_PATHS = ["unity/Wotr.Client/Assets", "unity/Wotr.Client/Packages", "unity/Wotr.Client/ProjectSettings",
                "scripts", "release", "wotr-deps.lock.json"]
# Unity's Package Manager UI preferences, not its manifest or package lock.
# Only the generated, untracked file is excluded; a tracked edit still blocks release.
LOCAL_EDITOR_PREFERENCES = "unity/Wotr.Client/ProjectSettings/PackageManagerSettings.asset"
# ReleaseManifest.MaximumNotesLength in wotr-server: the signed manifest carries the trimmed notes.
NOTES_LIMIT = 4000


def run(*args, **kwargs):
    return subprocess.run(args, cwd=ROOT, check=True, **kwargs)


def read(path):
    return json.loads(Path(path).read_text())


def digest(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def destination():
    version = read(ROOT / "release/version.json")["version"]
    if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[a-z0-9.]+)?", version):
        raise ValueError("Invalid release version.")
    return ROOT / ".artifacts/releases" / version


def source_commit():
    status = run("git", "status", "--porcelain", "-z", "--", *SOURCE_PATHS, stdout=subprocess.PIPE).stdout
    changes = [entry for entry in status.split(b"\0") if entry and
               entry != ("?? " + LOCAL_EDITOR_PREFERENCES).encode()]
    if changes:
        raise ValueError("Commit source changes before building or packaging a release.")
    return run("git", "rev-parse", "HEAD", stdout=subprocess.PIPE).stdout.decode().strip()


def build(platform):
    commit = source_commit()
    folder = destination()
    folder.mkdir(parents=True, exist_ok=True)
    for name in PLATFORMS if platform == "all" else [platform]:
        method, target, _ = PLATFORMS[name]
        run("bash", "scripts/unity.sh", "-quit", "-buildTarget", target,
            "-executeMethod", "Wotr.Editor.BuildTasks." + method, "-wotrCommit", commit,
            "-logFile", str(folder / f"build-{name}.log"))


def notes_path(version):
    return ROOT / "release/notes" / f"{version}.md"


def check_tag(tag):
    """A release tag is v<release/version.json> and the version has English release notes for the manifest."""
    version = destination().name
    if tag != "v" + version:
        raise ValueError(f"Tag {tag} does not match release/version.json; this commit releases v{version}.")
    path = notes_path(version)
    if not path.is_file():
        raise ValueError(f"Missing release notes {path.relative_to(ROOT)}.")
    length = len(path.read_text(encoding="utf-8").strip())
    if not 0 < length <= NOTES_LIMIT:
        raise ValueError(f"Release notes need 1-{NOTES_LIMIT} characters after trimming, not {length}.")
    return version


def public_content_hash():
    resources = ROOT / "unity/Wotr.Client/Assets/Wotr/Resources"
    entries = [path.name + ":" + digest(path) for path in sorted(resources.glob("*.json"))]
    return hashlib.sha256(("\n".join(entries) + "\n").encode()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    builder = commands.add_parser("build")
    builder.add_argument("--platform", choices=["all", *PLATFORMS], default="all")
    checker = commands.add_parser("check-tag", help="refuse a tag that is not v<version> or lacks release notes")
    checker.add_argument("tag")
    args = parser.parse_args()
    if args.command == "build":
        build(args.platform)
    else:
        try:
            print(f"Release {check_tag(args.tag)}: tag and notes match.")
        except ValueError as error:
            sys.exit("ERROR: " + str(error))


if __name__ == "__main__":
    main()
