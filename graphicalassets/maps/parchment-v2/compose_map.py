"""Shade Mordor's paper while preserving all existing map linework."""

from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import re
import subprocess
from xml.etree import ElementTree as ET

import numpy as np
from PIL import Image
from shapely import contains_xy
from shapely.geometry import Polygon
from shapely.ops import unary_union


FOLDER = Path(__file__).resolve().parent
ROOT = FOLDER.parents[2]
SVG = "{http://www.w3.org/2000/svg}"
OUTPUT = FOLDER.parent / "middle-earth-parchment-v2.png"
PREVIOUS = FOLDER.parent / "middle-earth-parchment-v1.png"
spec = importlib.util.spec_from_file_location("map07_composer", FOLDER.parent / "parchment-v1/compose_map.py")
BASE = importlib.util.module_from_spec(spec)
spec.loader.exec_module(BASE)


def region_area(source, name):
    node = source.find(f'{SVG}g[@id="regions"]/{SVG}path[@data-region="{name}"]')
    area = Polygon()
    for ring in re.findall(r"M([^Z]+)Z", node.get("d")):
        points = [tuple(map(float, pair.split(","))) for pair in re.findall(r"[-\d.]+,[-\d.]+", ring)]
        area = area.symmetric_difference(Polygon(points))
    assert area.is_valid, name
    return area


def add_shade(root, helper, area, layout):
    definitions = root.find(SVG + "defs")
    path = BASE.NATIVE.geometry_path(area)
    clip = ET.SubElement(definitions, SVG + "clipPath", {
        "id": "mordor-area", "clipPathUnits": "userSpaceOnUse",
    })
    ET.SubElement(clip, SVG + "path", {"d": path, "clip-rule": "evenodd"})
    feather = ET.SubElement(definitions, SVG + "filter", {"id": "mordor-feather"})
    ET.SubElement(feather, SVG + "feGaussianBlur", {"stdDeviation": str(layout["edge_feather"])})
    mask = ET.SubElement(definitions, SVG + "mask", {
        "id": "mordor-mask", "maskUnits": "userSpaceOnUse",
        "x": "335", "y": "40", "width": "1632", "height": "1338",
    })
    ET.SubElement(mask, SVG + "path", {
        "d": path, "fill": "white", "fill-rule": "evenodd", "filter": "url(#mordor-feather)",
    })
    shade = ET.Element(SVG + "g", {
        "id": "mordor-shade", "clip-path": "url(#mordor-area)", "opacity": str(layout["opacity"]),
    })
    blend = ET.SubElement(shade, SVG + "g", {"mask": "url(#mordor-mask)"})
    placement = dict(zip(("x", "y", "width", "height"), map(str, layout["placement"])))
    placement["preserveAspectRatio"] = "none"
    blend.append(helper.raster_element(FOLDER / "mordor-paper.png", placement))
    drawing = root.find(f'{SVG}g[@id="map-ink"]')
    root.insert(list(root).index(drawing), shade)


def inspect_pixels(area):
    with Image.open(PREVIOUS) as image:
        before = np.asarray(image.convert("RGB"), dtype=np.int16)
    with Image.open(OUTPUT) as image:
        assert image.size == (6144, 5037)
        after = np.asarray(image.convert("RGB"), dtype=np.int16)
    ys, xs = np.where(np.any(after != before, axis=2))
    xx = 335 + (xs + 0.5) * 1632 / 6144
    yy = 40 + (ys + 0.5) * 1338 / 5037
    outside = ~contains_xy(area.buffer(0.75), xx, yy)
    assert not outside.any(), "Changed pixels outside Mordor"
    inside = contains_xy(area.buffer(-8), xx, yy)
    old = before[ys[inside], xs[inside]] @ np.array([0.2126, 0.7152, 0.0722])
    new = after[ys[inside], xs[inside]] @ np.array([0.2126, 0.7152, 0.0722])
    reduction = float(np.median(old - new))
    assert 5 < reduction < 35, f"Tone change is not subtle: {reduction}"
    return {
        "changed_pixels": len(xs), "outside_mordor_changed_pixels": int(outside.sum()),
        "median_luminance_reduction_255": round(reduction, 2),
        "median_relative_darkening_percent": round(float(np.median((old - new) / old) * 100), 2),
        "antialias_tolerance_in_working_units": 0.75,
    }


def crop_mordor():
    view_box = [1470, 740, 497, 470]
    x, y, width, height = view_box
    px, py = round((x - 335) * 6144 / 1632), round((y - 40) * 5037 / 1338)
    pw, ph = round(width * 6144 / 1632), round(height * 5037 / 1338)
    subprocess.run([
        "sips", "--cropToHeightWidth", str(ph), str(pw), "--cropOffset", str(py), str(px),
        str(OUTPUT), "--out", str(FOLDER / "detail-mordor.png"),
    ], check=True, capture_output=True, timeout=30)


def main():
    helper = BASE.NATIVE.exporter()
    helper.FOLDER = FOLDER
    inputs = helper.verify_sources()
    layout = json.loads((FOLDER / "layout.json").read_text())
    previous = ET.parse(FOLDER.parent / "parchment-v1/map-composition.svg").getroot()
    composition = deepcopy(previous)
    source = ET.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    area = unary_union([region_area(source, name) for name in layout["regions"]])
    add_shade(composition, helper, area, layout)
    query = f'{SVG}g[@id="map-ink"]'
    assert ET.tostring(previous.find(query)) == ET.tostring(composition.find(query))
    counts = helper.verify_paths(source, composition.find(query))
    (FOLDER / "map-composition.svg").write_bytes(ET.tostring(composition, encoding="utf-8", xml_declaration=True))
    BASE.NATIVE.render(composition, OUTPUT)
    preview = deepcopy(composition)
    preview.set("width", "1536")
    preview.set("height", "1259")
    BASE.NATIVE.render(preview, FOLDER / "map-preview.png")
    crop_mordor()
    report = {"task": "MAP-08", "unchanged_paths": counts, "unchanged_complete_ink_layer": True,
              "composition": helper.raster_statistics(OUTPUT), "pixel_comparison": inspect_pixels(area),
              "source_sha256": inputs["sha256"], "output_sha256": helper.sha256(OUTPUT)}
    (FOLDER / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: value for key, value in report.items() if key != "source_sha256"}, indent=2))


if __name__ == "__main__":
    main()
