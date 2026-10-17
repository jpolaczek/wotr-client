"""Composite a local charcoal edit below the unchanged map boundaries."""

from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import subprocess
from xml.etree import ElementTree as ET

import numpy as np
from PIL import Image
from shapely import contains_xy
from shapely.geometry import Polygon, box
from shapely.ops import unary_union


FOLDER = Path(__file__).resolve().parent
ROOT = FOLDER.parents[2]
SVG = "{http://www.w3.org/2000/svg}"
OUTPUT = FOLDER.parent / "middle-earth-parchment-v3.png"
PREVIOUS = FOLDER.parent / "middle-earth-parchment-v1.png"
spec = importlib.util.spec_from_file_location("map08_composer", FOLDER.parent / "parchment-v2/compose_map.py")
GEOMETRY = importlib.util.module_from_spec(spec)
spec.loader.exec_module(GEOMETRY)
BASE = GEOMETRY.BASE


def image_placement(layout):
    x, y, width, height = layout["source_crop_pixels"]
    return [335 + x * 1632 / 6144,
            40 + y * 1338 / 5037 + layout["placement_y_adjustment"],
            width * 1632 / 6144, height * 1338 / 5037]


def edit_area(source, layout):
    regions = [GEOMETRY.region_area(source, name) for name in layout["regions"]]
    water = unary_union([Polygon(ring) for region in regions for ring in region.interiors])
    area = Polygon(layout["terrain_envelope"]).difference(water)
    x, y, width, height = image_placement(layout)
    area = area.intersection(box(x, y, x + width, y + height))
    return area.difference(box(*layout["preserved_pass"]))


def add_charcoal(root, helper, area, layout):
    definitions = root.find(SVG + "defs")
    path = BASE.NATIVE.geometry_path(area)
    clip = ET.SubElement(definitions, SVG + "clipPath", {
        "id": "mordor-charcoal-area", "clipPathUnits": "userSpaceOnUse",
    })
    ET.SubElement(clip, SVG + "path", {"d": path, "clip-rule": "evenodd"})
    feather = ET.SubElement(definitions, SVG + "filter", {"id": "mordor-charcoal-feather"})
    ET.SubElement(feather, SVG + "feGaussianBlur", {"stdDeviation": str(layout["edge_feather"])})
    mask = ET.SubElement(definitions, SVG + "mask", {
        "id": "mordor-charcoal-mask", "maskUnits": "userSpaceOnUse",
        "x": "335", "y": "40", "width": "1632", "height": "1338",
    })
    ET.SubElement(mask, SVG + "path", {
        "d": path, "fill": "white", "fill-rule": "evenodd",
        "filter": "url(#mordor-charcoal-feather)",
    })
    terrain = root.find(f'{SVG}g[@id="map-ink"]/{SVG}g[@id="terrain-layers"]')
    group = ET.SubElement(terrain, SVG + "g", {
        "id": "mordor-charcoal", "clip-path": "url(#mordor-charcoal-area)",
    })
    blend = ET.SubElement(group, SVG + "g", {"mask": "url(#mordor-charcoal-mask)"})
    placement = dict(zip(("x", "y", "width", "height"), map(str, image_placement(layout))))
    placement["preserveAspectRatio"] = "none"
    blend.append(helper.raster_element(FOLDER / "mordor-charcoal.png", placement))


def inspect_pixels(area, pass_window):
    with Image.open(PREVIOUS) as image:
        before = np.asarray(image.convert("RGB"))
    with Image.open(OUTPUT) as image:
        assert image.size == (6144, 5037)
        after = np.asarray(image.convert("RGB"))
    ys, xs = np.where(np.any(after != before, axis=2))
    xx = 335 + (xs + 0.5) * 1632 / 6144
    yy = 40 + (ys + 0.5) * 1338 / 5037
    outside = ~contains_xy(area.buffer(0.75), xx, yy)
    inside_pass = contains_xy(box(*pass_window).buffer(-0.75), xx, yy)
    assert not outside.any(), "Changed pixels outside the local Mordor edit"
    assert not inside_pass.any(), "Changed pixels in preserved Minas Morgul pass"
    return {
        "comparison": "middle-earth-parchment-v1.png (before the rejected flat tint)",
        "changed_pixels": len(xs), "outside_edit_changed_pixels": int(outside.sum()),
        "preserved_pass_changed_pixels": int(inside_pass.sum()),
        "antialias_tolerance_in_working_units": 0.75,
    }


def crop_detail(source, target, window):
    x, y, width, height = window
    subprocess.run([
        "sips", "--cropToHeightWidth", str(height), str(width), "--cropOffset", str(y), str(x),
        str(source), "--out", str(target),
    ], check=True, capture_output=True, timeout=30)


def main():
    helper = BASE.NATIVE.exporter()
    helper.FOLDER = FOLDER
    inputs = helper.verify_sources()
    layout = json.loads((FOLDER / "layout.json").read_text())
    previous = ET.parse(FOLDER.parent / "parchment-v1/map-composition.svg").getroot()
    composition = deepcopy(previous)
    composition.find(SVG + "title").text = "Middle-earth on aged paper — charcoal Mordor"
    composition.find(SVG + "desc").text = (
        "Local charcoal shading and a raised northern Mordor ridge; original paper and region boundaries.")
    source = ET.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    area = edit_area(source, layout)
    add_charcoal(composition, helper, area, layout)
    drawing = composition.find(f'{SVG}g[@id="map-ink"]')
    counts = helper.verify_paths(source, drawing)
    paper_query = f'{SVG}image[@id="aged-paper"]'
    assert ET.tostring(previous.find(paper_query)) == ET.tostring(composition.find(paper_query))
    before = previous.find(f'{SVG}g[@id="map-ink"]/{SVG}g[@id="terrain-layers"]')
    after = drawing.find(f'{SVG}g[@id="terrain-layers"]')
    assert [ET.tostring(node) for node in before] == [ET.tostring(node) for node in list(after)[:-1]]
    assert composition.find(f'{SVG}g[@id="mordor-shade"]') is None
    (FOLDER / "map-composition.svg").write_bytes(ET.tostring(composition, encoding="utf-8", xml_declaration=True))
    BASE.NATIVE.render(composition, OUTPUT)
    preview = deepcopy(composition)
    preview.set("width", "1536")
    preview.set("height", "1259")
    BASE.NATIVE.render(preview, FOLDER / "map-preview.png")
    crop_detail(OUTPUT, FOLDER / "detail-mordor.png", layout["source_crop_pixels"])
    crop_detail(PREVIOUS, FOLDER / "detail-mordor-before.png", layout["source_crop_pixels"])
    report = {
        "task": "MAP-09", "unchanged_paths": counts, "unchanged_paper_layer": True,
        "original_terrain_layers_retained_below_local_edit": True,
        "flat_tint_layer_removed": True, "placement": image_placement(layout),
        "composition": helper.raster_statistics(OUTPUT),
        "pixel_comparison": inspect_pixels(area, layout["preserved_pass"]),
        "source_sha256": inputs["sha256"], "output_sha256": helper.sha256(OUTPUT),
    }
    (FOLDER / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: value for key, value in report.items() if key != "source_sha256"}, indent=2))


if __name__ == "__main__":
    main()
