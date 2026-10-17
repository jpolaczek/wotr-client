"""Verify final forest coverage, edit locality and export actual PNG close-ups."""

import base64
from copy import deepcopy
import json
from pathlib import Path
import re
import subprocess
import tempfile
from xml.etree import ElementTree as ET

import numpy as np
from PIL import Image
from shapely import contains_xy
from shapely.geometry import LineString
from shapely.ops import unary_union

from prepare_references import FOLDER, ROOT, SVG, XLINK, canvas, load_previous, render


OUTPUT = FOLDER.parent / "middle-earth-pencil-v3.png"
WINDOWS = {
    "lorien": [1045, 440, 185, 185],
    "mirkwood": [1248, 209, 241, 423],
    "fangorn": [1005, 610, 210, 200],
}


def grayscale(path):
    with Image.open(path) as image:
        return np.asarray(image.convert("L"), dtype=np.int16)


def pixel_box(view_box):
    x, y, width, height = view_box
    return (round((x - 335) * 6144 / 1632), round((y - 40) * 5037 / 1338),
            round(width * 6144 / 1632), round(height * 5037 / 1338))


def locality_and_details():
    before = grayscale(FOLDER.parent / "middle-earth-pencil-v2.png")
    after = grayscale(OUTPUT)
    outside = np.ones(before.shape, dtype=bool)
    atlas = canvas([0, 0, 1536, 900], 1536)
    ET.SubElement(atlas, SVG + "rect", {"width": "1536", "height": "900", "fill": "white"})
    for index, (name, view_box) in enumerate(WINDOWS.items()):
        x, y, width, height = pixel_box(view_box)
        outside[y - 4:y + height + 4, x - 4:x + width + 4] = False
        crop = FOLDER / f"detail-{name}.png"
        subprocess.run([
            "sips", "--cropToHeightWidth", str(height), str(width),
            "--cropOffset", str(y), str(x), str(OUTPUT), "--out", str(crop),
        ], check=True, capture_output=True, timeout=30)
        ET.SubElement(atlas, SVG + "image", {
            "x": str(index * 512), "y": "0", "width": "512", "height": "900",
            "preserveAspectRatio": "xMidYMid meet",
            XLINK + "href": "data:image/png;base64," + base64.b64encode(crop.read_bytes()).decode(),
        })
    difference = np.abs(after - before)
    maximum = int(difference[outside].max())
    changed = int(np.count_nonzero((difference > 1) & outside))
    assert maximum <= 1, f"Unexpected edits outside forest windows: maximum difference {maximum}"
    render(atlas, FOLDER / "map-details.png")
    return {"outside_windows_maximum_gray_difference": maximum,
            "outside_windows_changed_pixels_above_1": changed,
            "details_are_crops_of_final_png": True, "detail_order": list(WINDOWS)}


def forest_pixels(composition, name, view_box, temporary):
    root = canvas(view_box, round(view_box[2] * 4))
    x, y, width, height = view_box
    ET.SubElement(root, SVG + "rect", {
        "x": str(x), "y": str(y), "width": str(width), "height": str(height), "fill": "white",
    })
    root.append(deepcopy(composition.find(SVG + "defs")))
    terrain = composition.find(f'{SVG}g[@id="terrain-layers"]')
    layer = terrain.find(f'{SVG}g[@data-forest="{name}"]')
    if layer is None and name == "lorien":
        previous = [node for node in terrain if any(
            use.get(XLINK + "href") == "#forest-art" for use in node.iter(SVG + "use"))]
        layer = previous[1]
    assert layer is not None
    root.append(deepcopy(layer))
    path = temporary / f"{name}.png"
    render(root, path)
    return grayscale(path)


def local_density(pixels, view_box, x, y, radius=4):
    vx, vy, vw, vh = view_box
    height, width = pixels.shape
    left = max(0, round((x - radius - vx) * width / vw))
    right = min(width, round((x + radius - vx) * width / vw))
    top = max(0, round((y - radius - vy) * height / vh))
    bottom = min(height, round((y + radius - vy) * height / vh))
    return float(np.mean(pixels[top:bottom, left:right] < 220))


def region_coverage(pixels, view_box, polygon):
    vx, vy, vw, vh = view_box
    height, width = pixels.shape
    xs = vx + (np.arange(width) + 0.5) * vw / width
    ys = vy + (np.arange(height) + 0.5) * vh / height
    inside = contains_xy(polygon.buffer(-4), xs[None, :], ys[:, None])
    density = float(np.mean(pixels[inside] < 220))
    x0, y0, x1, y1 = polygon.bounds
    samples = [(x, y) for x in np.arange(x0 + 4, x1, 8) for y in np.arange(y0 + 4, y1, 8)
               if contains_xy(polygon.buffer(-5), x, y)]
    covered = sum(local_density(pixels, view_box, x, y) > 0.07 for x, y in samples)
    assert density > 0.20 and covered / len(samples) > 0.90
    return {"dark_pixel_fraction": round(density, 4), "occupied_cells": covered, "cells": len(samples)}


def boundary_coverage(source, regions, pixels, view_box):
    report = []
    for node in source.findall(f'{SVG}g[@id="boundaries"]/{SVG}path'):
        a, b = node.get("data-region-a"), node.get("data-region-b")
        if a not in regions or b not in regions:
            continue
        points = [tuple(map(float, pair)) for pair in re.findall(r"[ML]([\d.]+),([\d.]+)", node.get("d"))]
        line = LineString(points)
        samples = [line.interpolate(distance) for distance in np.arange(4, line.length - 4, 3)]
        if not samples:
            continue
        covered = sum(local_density(pixels, view_box, point.x, point.y) > 0.07 for point in samples)
        assert covered / len(samples) > 0.90, f"Sparse forest at boundary {a}/{b}"
        report.append({"regions": [a, b], "wooded_samples": covered, "samples": len(samples)})
    assert report
    return report


def verify_forests():
    source = ET.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    composition = ET.parse(FOLDER / "map-composition.svg").getroot()
    polygons = load_previous().region_polygons(source)
    layout = json.loads((FOLDER / "forest-layout.json").read_text())
    report = {}
    with tempfile.TemporaryDirectory(prefix="wotr-map05-inspect-") as folder:
        for name, item in layout.items():
            pixels = forest_pixels(composition, name, item["view_box"], Path(folder))
            x, y, width, height = item["view_box"]
            py, px = np.where(pixels < 240)
            xx = x + (px + 0.5) * width / pixels.shape[1]
            yy = y + (py + 0.5) * height / pixels.shape[0]
            polygon = unary_union([polygons[key] for key in item["regions"]])
            outside = int(np.count_nonzero(~contains_xy(polygon, xx, yy)))
            assert outside == 0, f"Forest outside allowed regions: {name}"
            report[name] = {"visible_ink_pixels": len(px), "outside_regions": outside}
            if name == "mirkwood":
                report[name]["region_coverage"] = {
                    key: region_coverage(pixels, item["view_box"], polygons[key]) for key in item["regions"]}
                report[name]["internal_boundaries"] = boundary_coverage(source, item["regions"], pixels, item["view_box"])
            if name == "lorien":
                previous = ET.parse(FOLDER.parent / "pencil-v2/map-composition.svg").getroot()
                old = forest_pixels(previous, name, item["view_box"], Path(folder))
                before = float(np.quantile(old[old < 240], 0.25))
                after = float(np.quantile(pixels[pixels < 240], 0.25))
                assert after < before
                report[name]["ink_gray_lower_quartile"] = {"before": before, "after": after}
    return report


def main():
    report = {"task": "MAP-05", "locality": locality_and_details(), "forests": verify_forests()}
    (FOLDER / "forest-verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
