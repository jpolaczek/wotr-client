"""Apply imagegen river cleanup only within traced corridors on the native SVG."""

from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
from xml.etree import ElementTree as ET

from shapely.geometry import LineString
from shapely.ops import unary_union


FOLDER = Path(__file__).resolve().parent
ROOT = FOLDER.parents[2]
SVG = "{http://www.w3.org/2000/svg}"
XLINK = "{http://www.w3.org/1999/xlink}"
OUTPUT = FOLDER.parent / "middle-earth-pencil-v4.png"
BASE = FOLDER.parent / "pencil-v3/map-composition.svg"


def exporter():
    path = FOLDER.parent / "pencil-v1/build_map.py"
    spec = importlib.util.spec_from_file_location("pencil_exporter", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.FOLDER = FOLDER
    return module


def river_geometry(source=None):
    data = json.loads((FOLDER / "river-corridors.json").read_text())
    corridors = []
    for item in data["paths"]:
        if source is not None and item.get("source", "background") != source:
            continue
        points = [(x + 335, y + 40) for x, y in item["points"]]
        corridors.append(LineString(points).buffer(item.get("radius", data["default_radius"])))
    return unary_union(corridors)


def geometry_path(geometry):
    polygons = list(geometry.geoms) if geometry.geom_type == "MultiPolygon" else [geometry]
    paths = []
    for polygon in polygons:
        for ring in [polygon.exterior, *polygon.interiors]:
            paths.append("M" + " L".join(f"{x:.4f},{y:.4f}" for x, y in ring.coords) + " Z")
    return " ".join(paths)


def forest_layers(terrain):
    return [node for node in terrain if node.get("data-forest") or any(
        use.get(XLINK + "href") == "#forest-art" for use in node.iter(SVG + "use"))]


def add_cleanup(root, helper, source, filename, view_box):
    definitions = root.find(SVG + "defs")
    prefix = "river-cleanup-" + source
    path = geometry_path(river_geometry(source))
    clip = ET.SubElement(definitions, SVG + "clipPath", {
        "id": prefix + "-area", "clipPathUnits": "userSpaceOnUse",
    })
    ET.SubElement(clip, SVG + "path", {"d": path, "clip-rule": "evenodd"})
    feather = ET.SubElement(definitions, SVG + "filter", {"id": prefix + "-feather"})
    ET.SubElement(feather, SVG + "feGaussianBlur", {"stdDeviation": "1.2"})
    mask = ET.SubElement(definitions, SVG + "mask", {
        "id": prefix + "-mask", "maskUnits": "userSpaceOnUse",
        "x": "335", "y": "40", "width": "1632", "height": "1338",
    })
    ET.SubElement(mask, SVG + "path", {
        "d": path, "fill": "white", "fill-rule": "evenodd",
        "filter": f"url(#{prefix}-feather)",
    })
    terrain = root.find(f'{SVG}g[@id="terrain-layers"]')
    cleanup = ET.SubElement(terrain, SVG + "g", {
        "id": prefix, "clip-path": f"url(#{prefix}-area)",
    })
    blend = ET.SubElement(cleanup, SVG + "g", {"mask": f"url(#{prefix}-mask)"})
    placement = dict(zip(("x", "y", "width", "height"), map(str, view_box)))
    placement["preserveAspectRatio"] = "none"
    blend.append(helper.raster_element(FOLDER / filename, placement))


def compose(root, helper):
    root.find(SVG + "desc").text = "Pencil terrain without added rivers; original boundaries define region and water edges."
    terrain = root.find(f'{SVG}g[@id="terrain-layers"]')
    forests = forest_layers(terrain)
    assert len(forests) == 4
    for node in forests:
        terrain.remove(node)
    add_cleanup(root, helper, "background", "background-clean.png", [335, 40, 1632, 1338])
    add_cleanup(root, helper, "rhun", "rhun-clean.png", [1745, 540, 210, 210])
    terrain.extend(forests)
    return root


def render(root, target):
    with tempfile.TemporaryDirectory(prefix="wotr-map06-render-") as folder:
        source = Path(folder) / "map.svg"
        source.write_bytes(ET.tostring(root))
        subprocess.run([
            "node", str(FOLDER.parent / "pencil-v2/render_svg.cjs"), str(source), str(target),
        ], check=True, timeout=60)


def terrain_only(composition):
    root = deepcopy(composition)
    for name in ("regions", "boundaries"):
        root.remove(root.find(f'{SVG}g[@id="{name}"]'))
    root.set("width", "1632")
    root.set("height", "1338")
    return root


def main():
    helper = exporter()
    inputs = helper.verify_sources()
    previous = ET.parse(BASE).getroot()
    source = ET.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    composition = compose(deepcopy(previous), helper)
    counts = helper.verify_paths(source, composition)
    before = forest_layers(previous.find(f'{SVG}g[@id="terrain-layers"]'))
    after = forest_layers(composition.find(f'{SVG}g[@id="terrain-layers"]'))
    assert [ET.tostring(node) for node in before] == [ET.tostring(node) for node in after]
    (FOLDER / "map-composition.svg").write_bytes(ET.tostring(composition, encoding="utf-8", xml_declaration=True))
    render(composition, OUTPUT)
    preview = deepcopy(composition)
    preview.set("width", "1536")
    preview.set("height", "1259")
    render(preview, FOLDER / "map-preview.png")
    render(terrain_only(composition), FOLDER / "terrain-review.png")
    statistics = helper.raster_statistics(OUTPUT)
    assert statistics["size"] == [6144, 5037] and statistics["maximum_rgb_channel_difference"] == 0
    report = {
        "task": "MAP-06", "composition": statistics, "unchanged_paths": counts,
        "unchanged_separate_forest_layers": 4, "source_sha256": inputs["sha256"],
        "viewport_landmarks": helper.verify_viewport(source, OUTPUT), "output_sha256": helper.sha256(OUTPUT),
    }
    (FOLDER / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({"composition": statistics, "paths": counts, "preserved_forest_layers": 4}, indent=2))


if __name__ == "__main__":
    main()
