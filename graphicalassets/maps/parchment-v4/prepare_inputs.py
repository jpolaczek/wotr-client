"""Export existing terrain for two local imagegen edits without changing artwork."""

from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import subprocess
from xml.etree import ElementTree as ET


FOLDER = Path(__file__).resolve().parent
ROOT = FOLDER.parents[2]
spec = importlib.util.spec_from_file_location("map09", FOLDER.parent / "parchment-v3/compose_map.py")
PREVIOUS = importlib.util.module_from_spec(spec)
spec.loader.exec_module(PREVIOUS)
SVG = PREVIOUS.SVG


def crop(source, destination, window):
    x, y, width, height = window
    px = round((x - 335) * 6144 / 1632)
    py = round((y - 40) * 5037 / 1338)
    pw = round(width * 6144 / 1632)
    ph = round(height * 5037 / 1338)
    subprocess.run([
        "sips", "--cropToHeightWidth", str(ph), str(pw), "--cropOffset", str(py), str(px),
        str(source), "--out", str(destination),
    ], check=True, capture_output=True, timeout=30)
    return [335 + px * 1632 / 6144, 40 + py * 1338 / 5037,
            pw * 1632 / 6144, ph * 1338 / 5037]


def main():
    source = ET.parse(FOLDER.parent / "parchment-v3/map-composition.svg").getroot()
    terrain = deepcopy(source)
    drawing = terrain.find(f'{SVG}g[@id="map-ink"]')
    drawing.attrib.pop("filter")
    for name in ("regions", "boundaries"):
        drawing.remove(drawing.find(f'{SVG}g[@id="{name}"]'))
    terrain.remove(terrain.find(f'{SVG}image[@id="aged-paper"]'))
    background = ET.Element(SVG + "rect", {
        "x": "335", "y": "40", "width": "1632", "height": "1338", "fill": "white",
    })
    terrain.insert(list(terrain).index(drawing), background)
    target = Path("/tmp/wotr-map10-terrain-before.png")
    PREVIOUS.BASE.NATIVE.render(terrain, target)
    patches = [
        {"id": "black-gate", "window": [1500, 755, 195, 155]},
        {"id": "erebor", "window": [1400, 45, 190, 180]},
    ]
    for patch in patches:
        patch["placement"] = crop(target, FOLDER / (patch["id"] + "-before-terrain.png"), patch["window"])
        crop(FOLDER.parent / "middle-earth-parchment-v3.png", FOLDER / (patch["id"] + "-before.png"), patch["window"])
    layout_path = FOLDER / "layout.json"
    if layout_path.exists():
        layout = json.loads(layout_path.read_text())
        by_id = {patch["id"]: patch for patch in layout["patches"]}
        for patch in patches:
            by_id[patch["id"]].update(patch)
    else:
        layout = {"patches": patches}
    layout_path.write_text(json.dumps(layout, indent=2) + "\n")
    helper = PREVIOUS.BASE.NATIVE.exporter()
    inputs = json.loads((FOLDER.parent / "parchment-v3/inputs.json").read_text())
    inputs["task"] = "MAP-10"
    for relative in ("graphicalassets/maps/parchment-v3/map-composition.svg",
                     "graphicalassets/maps/middle-earth-parchment-v3.png"):
        inputs["sha256"][relative] = helper.sha256(ROOT / relative)
    for patch in patches:
        for suffix in ("-terrain.png", "-before-terrain.png"):
            path = FOLDER / (patch["id"] + suffix)
            if path.exists():
                inputs["sha256"][str(path.relative_to(ROOT))] = helper.sha256(path)
    (FOLDER / "inputs.json").write_text(json.dumps(inputs, indent=2) + "\n")
    print("Prepared two local crops and source hashes.")


if __name__ == "__main__":
    main()
