"""Pinned references to the other WoTR repository (DIST-03).

docs/repositories/<alias>.json names the peer repository, the commit this repository
was checked against and every path tracked there. References use `<alias>:<path>` in
registry fields and https://github.com/<repository>/blob/<commit>/<path> in Markdown,
so each one names its repository, commit and path and can be checked without access
to the peer. `pin` moves the commit and rewrites the Markdown links to it.
"""
import json
from pathlib import Path
import re
import subprocess

FOLDER = "docs/repositories"
LINK = re.compile(r"https://github\.com/([\w.-]+/[\w.-]+)/(blob|tree)/([0-9a-f]{40})/([^)\s#]*)")


class Peer:
    def __init__(self, alias, data):
        self.alias = alias
        self.repository = data["repository"]
        self.commit = data["commit"]
        self.files = set(data["files"])

    def has(self, path, folder=False):
        if folder:
            return any(name.startswith(path.rstrip("/") + "/") for name in self.files)
        return path in self.files


def peers(root):
    folder = root / FOLDER
    if not folder.is_dir():
        return {}
    return {path.stem: Peer(path.stem, json.loads(path.read_text())) for path in sorted(folder.glob("*.json"))}


def reference(known, value):
    """True/False for an `<alias>:<path>` reference to a known peer, None for anything else."""
    alias, separator, path = value.partition(":")
    if not separator or alias not in known:
        return None
    return known[alias].has(path)


def link_error(known, target):
    """None when the target is not a peer link, "" when it is valid, otherwise the reason."""
    match = LINK.match(target)
    if not match:
        return None
    repository, kind, commit, path = match.groups()
    peer = next((p for p in known.values() if p.repository == repository), None)
    if peer is None:
        return None
    if commit != peer.commit:
        return f"{repository} link at {commit[:12]}, but {FOLDER}/{peer.alias}.json pins {peer.commit[:12]}"
    if not peer.has(path, folder=kind == "tree"):
        return f"{repository} has no {path} at {commit[:12]}"
    return ""


def describe(checkout, revision):
    commit = subprocess.run(["git", "rev-parse", revision + "^{commit}"], cwd=checkout, check=True,
                            stdout=subprocess.PIPE, text=True).stdout.strip()
    files = subprocess.run(["git", "ls-tree", "-r", "--name-only", commit], cwd=checkout, check=True,
                           stdout=subprocess.PIPE, text=True).stdout.splitlines()
    return commit, sorted(files)


def pin(root, alias, repository, checkout, revision, markdown):
    """Pin the peer at `revision` of a local checkout and move this repository's links to it."""
    commit, files = describe(checkout, revision)
    path = root / FOLDER / (alias + ".json")
    old = json.loads(path.read_text())["commit"] if path.is_file() else None
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps({"repository": repository, "commit": commit, "files": files}, indent=2) + "\n")
    changed = []
    if old and old != commit:
        prefix = f"https://github.com/{repository}/"
        for document in markdown:
            text = document.read_text(encoding="utf-8")
            moved = re.sub(re.escape(prefix) + r"(blob|tree)/" + old + "/", prefix + r"\g<1>/" + commit + "/", text)
            if moved != text:
                document.write_text(moved, encoding="utf-8")
                changed.append(document)
    return old, commit, changed
