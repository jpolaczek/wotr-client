"""Place two imagegen edits and lay preserved map linework over aged paper."""

from copy import deepcopy
import importlib.util
import json
from pathlib import Path
from xml.etree import ElementTree as ET

from shapely.geometry import box


FOLDER = Path(__file__).resolve().parent
ROOT = FOLDER.parents[2]
SVG = "{http://www.w3.org/2000/svg}"
OUTPUT = FOLDER.parent / "middle-earth-parchment-v1.png"


def load_module(name, relative_path):
    spec = importlib.util.spec_from_file_location(name, FOLDER.parent / relative_path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


NATIVE = load_module("map06_composer", "pencil-v4/compose_map.py")
GEOMETRY = load_module("map04_composer", "pencil-v2/compose_map.py")


def patch_geometry(patch, regions):
    shape = box(*patch["window"])
    if "region" in patch:
        shape = shape.intersection(regions[patch["region"]].buffer(-patch["inset"]))
    return shape


def add_patch(root, helper, patch, shape):
    prefix = "parchment-patch-" + patch["id"]
    definitions = root.find(SVG + "defs")
    path = NATIVE.geometry_path(shape)
    clip = ET.SubElement(definitions, SVG + "clipPath", {
        "id": prefix, "clipPathUnits": "userSpaceOnUse",
    })
    ET.SubElement(clip, SVG + "path", {"d": path})
    feather = ET.SubElement(definitions, SVG + "filter", {"id": prefix + "-feather"})
    ET.SubElement(feather, SVG + "feGaussianBlur", {"stdDeviation": "1.2"})
    mask = ET.SubElement(definitions, SVG + "mask", {
        "id": prefix + "-mask", "maskUnits": "userSpaceOnUse",
        "x": "335", "y": "40", "width": "1632", "height": "1338",
    })
    ET.SubElement(mask, SVG + "path", {
        "d": path, "fill": "white", "filter": f"url(#{prefix}-feather)",
    })
    terrain = root.find(f'{SVG}g[@id="terrain-layers"]')
    group = ET.SubElement(terrain, SVG + "g", {"clip-path": f"url(#{prefix})"})
    blend = ET.SubElement(group, SVG + "g", {"mask": f"url(#{prefix}-mask)"})
    placement = dict(zip(("x", "y", "width", "height"), map(str, patch["placement"])))
    placement["preserveAspectRatio"] = "none"
    blend.append(helper.raster_element(FOLDER / patch["file"], placement))


def style_on_paper(linework, helper, layout):
    root = deepcopy(linework)
    root.find(SVG + "title").text = "Middle-earth map on aged paper"
    root.find(SVG + "desc").text = (
        "Warm aged paper under pencil terrain and unchanged region geometry; no labels or added rivers.")
    definitions = root.find(SVG + "defs")
    ink = ET.SubElement(definitions, SVG + "filter", {
        "id": "aged-map-ink", "color-interpolation-filters": "sRGB",
        "filterUnits": "userSpaceOnUse", "x": "335", "y": "40",
        "width": "1632", "height": "1338",
    })
    red, green, blue = layout["ink_rgb"]
    ET.SubElement(ink, SVG + "feColorMatrix", {
        "type": "matrix",
        "values": f"0 0 0 0 {red}  0 0 0 0 {green}  0 0 0 0 {blue}  -0.2126 -0.7152 -0.0722 0 1",
    })
    drawing = ET.Element(SVG + "g", {"id": "map-ink", "filter": "url(#aged-map-ink)"})
    for name in ("terrain-layers", "regions", "boundaries"):
        layer = root.find(f'{SVG}g[@id="{name}"]')
        root.remove(layer)
        drawing.append(layer)
    placement = dict(zip(("x", "y", "width", "height"), map(str, layout["paper_view_box"])))
    placement.update(id="aged-paper", preserveAspectRatio="none")
    root.append(helper.raster_element(FOLDER / "aged-paper.png", placement))
    root.append(drawing)
    return root


def write_svg(root, filename):
    (FOLDER / filename).write_bytes(ET.tostring(root, encoding="utf-8", xml_declaration=True))


def main():
    helper = NATIVE.exporter()
    helper.FOLDER = FOLDER
    inputs = helper.verify_sources()
    layout = json.loads((FOLDER / "layout.json").read_text())
    source = ET.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    previous = ET.parse(FOLDER.parent / "pencil-v4/map-composition.svg").getroot()
    regions = GEOMETRY.region_polygons(source)
    linework = deepcopy(previous)
    for patch in layout["patches"]:
        add_patch(linework, helper, patch, patch_geometry(patch, regions))
    counts = helper.verify_paths(source, linework)
    before = previous.find(f'{SVG}g[@id="terrain-layers"]')
    after = linework.find(f'{SVG}g[@id="terrain-layers"]')
    assert [ET.tostring(node) for node in NATIVE.forest_layers(before)] == [
        ET.tostring(node) for node in NATIVE.forest_layers(after)]
    NATIVE.render(linework, FOLDER / "map-linework.png")
    composition = style_on_paper(linework, helper, layout)
    helper.verify_paths(source, composition.find(f'{SVG}g[@id="map-ink"]'))
    write_svg(composition, "map-composition.svg")
    NATIVE.render(composition, OUTPUT)
    preview = deepcopy(composition)
    preview.set("width", "1536")
    preview.set("height", "1259")
    NATIVE.render(preview, FOLDER / "map-preview.png")
    report = {
        "task": "MAP-07", "composition": helper.raster_statistics(OUTPUT),
        "unchanged_paths": counts, "unchanged_separate_forest_layers": 4,
        "source_sha256": inputs["sha256"], "output_sha256": helper.sha256(OUTPUT),
    }
    assert report["composition"]["size"] == [6144, 5037]
    (FOLDER / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: value for key, value in report.items() if key != "source_sha256"}, indent=2))


if __name__ == "__main__":
    main()
