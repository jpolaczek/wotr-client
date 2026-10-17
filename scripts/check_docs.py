#!/usr/bin/env python3
"""Check the client documentation links, including pinned links to wotr-server (DIST-03).

The task registry and its validator live in wotr-server. This repository checks its
own Markdown: local links resolve, links into wotr-server use the commit pinned in
docs/repositories/wotr-server.json and name a file tracked there, and no line ends
in whitespace. `pin` moves the pinned commit and the links with it.
"""
import argparse
from pathlib import Path
import re
import subprocess
import sys

import crossrepo

ROOT = Path(__file__).resolve().parents[1]
LINK = re.compile(r"\[[^\]]*\]\(([^\s)]+)\)")


def markdown(root):
    """Every tracked Markdown file, including release/ and graphicalassets/ (DIST-05)."""
    names = subprocess.run(["git", "ls-files", "-z", "--", "*.md"], cwd=root, check=True,
                           stdout=subprocess.PIPE).stdout.decode().split("\0")
    return sorted(root / name for name in names if name)


def check(root):
    known = crossrepo.peers(root)
    errors = []
    for path in markdown(root):
        if not path.is_file():
            continue
        body = path.read_text(encoding="utf-8")
        name = path.relative_to(root)
        for target in LINK.findall(body):
            pinned = crossrepo.link_error(known, target)
            if pinned:
                errors.append(f"{name}: {pinned}")
            if pinned is not None or re.match(r"[a-zA-Z][\w+.-]*:", target) or target.startswith("#"):
                continue
            if not (path.parent / target.split("#", 1)[0]).resolve().exists():
                errors.append(f"{name}: broken link {target}")
        if any(line.rstrip() != line for line in body.splitlines()):
            errors.append(f"{name}: trailing whitespace")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    commands = parser.add_subparsers(dest="command")
    pinning = commands.add_parser("pin", help="Pin a wotr-server commit from a local checkout.")
    pinning.add_argument("--checkout", type=Path, required=True)
    pinning.add_argument("--repository", default="jpolaczek/wotr-server")
    pinning.add_argument("--revision", default="HEAD")
    args = parser.parse_args()
    if args.command == "pin":
        old, new, changed = crossrepo.pin(ROOT, "wotr-server", args.repository, args.checkout.resolve(),
                                          args.revision, markdown(ROOT))
        print(f"wotr-server: {old or '-'} -> {new}; moved links in {len(changed)} files.")
    errors = check(ROOT)
    if errors:
        print("\n".join("ERROR: " + error for error in errors))
        return 1
    print(f"PASS: {len(markdown(ROOT))} Markdown files; local and pinned wotr-server links.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
