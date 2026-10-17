#!/usr/bin/env python3
"""Render the UI-120 portrait atlas and the four battle scene strips to Unity PNGs with headless Chrome,
write the atlas cell JSON, Unity .meta files and manifest-v1.json. Hand-authored SVG; replaceable."""
import hashlib, json, re, shutil, subprocess, sys, tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
sys.path.insert(0, str(ROOT / "graphicalassets" / "ui"))
import render as ui  # noqa: E402  (shares Chrome invocation and the texture .meta template)

ART = ROOT / "unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art"
ATLAS = ART / "battle-portraits-v1.png"
CELLS = ART / "battle-portraits-v1.json"
SCENES = ART / "battle-scenes"
TEXT_META = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
ORDER = ["gimli", "boromir", "legolas", "gandalf-grey", "gandalf-white",
         "strider", "aragorn", "pippin", "merry", "gollum",
         "smeagol", "ring-bearers", "elrond", "galadriel", "balrog",
         "gothmog", "saruman", "mouth-of-sauron", "witch-king"]
COLS, ROWS, CELL = 5, 4, 200


def guid(path):
    return hashlib.md5(("wotr-portraits:" + str(path.relative_to(ROOT))).encode()).hexdigest()


def meta(path, template, wrap_u=1):
    target = path.with_name(path.name + ".meta")
    if not target.exists():
        target.write_text(template.format(guid=guid(path), wrap_u=wrap_u), encoding="utf-8")


def render(renderer, svg_path, output, scale=1):
    svg = svg_path.read_text(encoding="utf-8")
    size = ui.logical_size(svg)
    page = renderer.page(output.stem, ui.BASE_CSS, svg)
    output.parent.mkdir(parents=True, exist_ok=True)
    renderer.screenshot(page, output, size, scale)
    width, height, alpha = ui.png_header(output)
    return {"file": str(output.relative_to(ROOT)), "size": [width, height], "alpha": alpha, "sha256": ui.sha256(output)}


def cells():
    # Each portrait is centred in its 200 px cell; the medallion vignette fills a 176 px square (margin 12 px).
    result = []
    for index, name in enumerate(ORDER):
        row, col = divmod(index, COLS)
        x = (col * CELL + 12) / (COLS * CELL)
        y = (ROWS * CELL - (row + 1) * CELL + 12) / (ROWS * CELL)
        result.append({"name": name, "x": x, "y": y, "width": 176 / (COLS * CELL), "height": 176 / (ROWS * CELL)})
    return {"schema": "battle-portraits-1", "width": COLS * CELL, "height": ROWS * CELL,
            "coordinates": "normalized bottom-left; separate non-overlapping bounds", "cells": result}


def main():
    workspace = Path(tempfile.mkdtemp(prefix="wotr-portraits-"))
    try:
        renderer = ui.Renderer(workspace)
        entries = [render(renderer, HERE / "src/portraits-v1.svg", ATLAS)]
        meta(ATLAS, ui.TEXTURE_META, wrap_u=1)
        CELLS.write_text(json.dumps(cells(), indent=2) + "\n", encoding="utf-8")
        meta(CELLS, TEXT_META)
        SCENES.mkdir(parents=True, exist_ok=True)
        meta(SCENES, ui.FOLDER_META)
        for name in ("field", "siege", "sortie", "relief"):
            output = SCENES / (name + ".png")
            entries.append(render(renderer, HERE / "src/scenes" / (name + ".svg"), output))
            meta(output, ui.TEXTURE_META, wrap_u=0)
        manifest = {
            "generator": "Hand-authored SVG (graphicalassets/portraits/src) rendered with headless Google Chrome",
            "generated_utc_date": "2026-10-13",
            "pixels_modified": False,
            "bounds_method": "fixed 5x4 grid of 200 px cells, 12 px margin; metadata only",
            "purpose": "Replaceable vector placeholder portraits and scene strips; not scanned official components; not AI-painted bitmaps",
            "prompts": ["prompt-v1.txt", "scenes-prompt-v1.txt"],
            "sources": {str(p.relative_to(HERE)): ui.sha256(p) for p in sorted((HERE / "src").rglob("*.svg"))},
            "outputs": entries,
        }
        (HERE / "manifest-v1.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
        print(f"rendered {len(entries)} textures")
    finally:
        shutil.rmtree(workspace, ignore_errors=True)


if __name__ == "__main__":
    main()
