#!/usr/bin/env python3
"""Combine untouched public snapshots exported by the real FigureMotionNetworkTests."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path


SPEC = importlib.util.spec_from_file_location("profile_client", Path(__file__).with_name("profile-client.py"))
PROFILE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROFILE)


def merge(directory):
    views = {}
    sources = []
    for name in ("armies", "character", "fellowship"):
        path = directory / f"ui77-motion-{name}.json"
        content = path.read_bytes()
        exported = json.loads(content)
        if views.keys() & exported.keys():
            raise ValueError("Duplicate motion input key.")
        views.update(exported)
        sources.append({"path": str(path.resolve()), "sha256": hashlib.sha256(content).hexdigest()})
    expected = {key for kind in PROFILE.KINDS["figure-motion"] for key in (kind, kind + "-after")}
    if views.keys() != expected:
        raise ValueError("Motion input must contain exactly eight before/after pairs.")
    for kind in PROFILE.KINDS["figure-motion"]:
        before, after = views[kind], views[kind + "-after"]
        if not before["MatchId"] or before["MatchId"] != after["MatchId"] or before["You"] != after["You"]:
            raise ValueError("Motion pairs must belong to one match and one viewer.")
        if after["Revision"] != before["Revision"] + 1:
            raise ValueError("Motion pairs must be consecutive recorded revisions.")
        for view in (before, after):
            if any(key in view for key in ("Password", "Token", "Resolution", "RandomSamples")):
                raise ValueError("Motion input must contain projections only.")
            if kind.endswith("-sh") and (view["OwnCompanions"] or view["OwnCompanionVersions"]):
                raise ValueError("A Shadow motion input contains private Free Peoples choices.")
    return views, sources


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    views, sources = merge(args.directory)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(views, indent=2) + "\n", encoding="utf-8")
    args.output.with_suffix(".sources.json").write_text(json.dumps({
        "source": "Actual FigureMotionNetworkTests HTTP/WebSocket projections",
        "inputs": sources, "sha256": hashlib.sha256(args.output.read_bytes()).hexdigest()
    }, indent=2) + "\n", encoding="utf-8")
    print(f"Eight recorded public movement pairs: {args.output}")


if __name__ == "__main__":
    main()
