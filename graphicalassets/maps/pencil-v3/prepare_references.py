"""Prepare exact vector silhouettes and terrain crops for imagegen."""

import base64
from copy import deepcopy
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
from xml.etree import ElementTree as ET

from shapely.ops import unary_union
from shapely.geometry import Polygon


FOLDER = Path(__file__).resolve().parent
ROOT = FOLDER.parents[2]
SVG = "{http://www.w3.org/2000/svg}"
XLINK = "{http://www.w3.org/1999/xlink}"


def load_previous():
    path = FOLDER.parent / "pencil-v2/compose_map.py"
    spec = importlib.util.spec_from_file_location("pencil_v2", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def render(root, target):
    with tempfile.TemporaryDirectory(prefix="wotr-map05-render-") as folder:
        source = Path(folder) / "source.svg"
        source.write_bytes(ET.tostring(root))
        subprocess.run([
            "node", str(FOLDER.parent / "pencil-v2/render_svg.cjs"),
            str(source), str(target),
        ], check=True, timeout=60)


def canvas(view_box, width):
    return ET.Element(SVG + "svg", {
        "width": str(width), "height": str(round(width * view_box[3] / view_box[2])),
        "viewBox": " ".join(map(str, view_box)),
    })


def lorien_target():
    previous = ET.parse(FOLDER.parent / "pencil-v2/map-composition.svg").getroot()
    terrain = previous.find(f'{SVG}g[@id="terrain-layers"]')
    forests = [node for node in terrain if any(
        use.get(XLINK + "href") == "#forest-art" for use in node.iter(SVG + "use"))]
    root = canvas([1061, 447, 161, 161], 1024)
    ET.SubElement(root, SVG + "rect", {
        "x": "1061", "y": "447", "width": "161", "height": "161", "fill": "white",
    })
    root.append(deepcopy(previous.find(SVG + "defs")))
    root.append(deepcopy(forests[1]))
    render(root, FOLDER / "lorien-target.png")


def main():
    previous = load_previous()
    source = ET.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    polygons = previous.region_polygons(source)
    layout = json.loads((FOLDER / "forest-layout.json").read_text())
    for name in ("mirkwood", "fangorn"):
        item = layout[name]
        polygon = unary_union([polygons[key] for key in item["regions"]])
        outline = Polygon(item["forest_outline"]) if "forest_outline" in item else polygon.buffer(-2)
        root = canvas(item["view_box"], 964 if name == "mirkwood" else 1000)
        x, y, width, height = item["view_box"]
        ET.SubElement(root, SVG + "rect", {
            "x": str(x), "y": str(y), "width": str(width),
            "height": str(height), "fill": "white",
        })
        ET.SubElement(root, SVG + "path", {
            "d": previous.polygon_path(outline), "fill": "#d8d8d8",
        })
        render(root, FOLDER / f"{name}-shape.png")
    for name, relative in (
        ("fangorn-target", "pencil-v1/terrain-pencil.png"),
        ("fangorn-board", "terrain-v1/source-layout-reference.png"),
    ):
        root = canvas([995, 565, 240, 250], 1000)
        path = FOLDER.parent / relative
        ET.SubElement(root, SVG + "image", {
            "x": "335", "y": "40", "width": "1632", "height": "1338",
            "preserveAspectRatio": "none", XLINK + "href":
            "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode(),
        })
        render(root, FOLDER / f"{name}.png")
    lorien_target()
    inputs = json.loads((FOLDER.parent / "pencil-v2/inputs.json").read_text())
    inputs["task"] = "MAP-05"
    for relative in (
        "middle-earth-pencil-v2.png", "pencil-v2/map-composition.svg",
        "pencil-v2/compose_map.py", "pencil-v2/forest-atlas.png",
    ):
        path = FOLDER.parent / relative
        inputs["sha256"][str(path.relative_to(ROOT))] = hashlib.sha256(path.read_bytes()).hexdigest()
    (FOLDER / "inputs.json").write_text(json.dumps(inputs, indent=2) + "\n")
    print("Prepared two silhouettes, Fangorn terrain/board crops and 20 source hashes")


if __name__ == "__main__":
    main()
