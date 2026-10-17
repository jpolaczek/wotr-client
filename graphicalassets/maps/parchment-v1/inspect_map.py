"""Verify local map edits and inspect crops taken from the final parchment PNG."""

import json
import subprocess
from xml.etree import ElementTree as ET

import numpy as np
from PIL import Image
from shapely import contains_xy
from shapely.geometry import box
from shapely.ops import unary_union

from compose_map import FOLDER, ROOT, OUTPUT, GEOMETRY, patch_geometry


def pixels(path):
    with Image.open(path) as image:
        return np.asarray(image.convert("L"), dtype=np.int16)


def world_coordinates(xs, ys):
    return 335 + (xs + 0.5) * 1632 / 6144, 40 + (ys + 0.5) * 1338 / 5037


def verify_locality(before, after, shapes):
    difference = after - before
    ys, xs = np.where(difference != 0)
    xx, yy = world_coordinates(xs, ys)
    outside = ~contains_xy(unary_union(list(shapes.values())).buffer(0.75), xx, yy)
    assert not outside.any(), "Linework changed outside the two edit windows"
    new_mountains = difference[ys, xs] < -12
    in_spur = contains_xy(shapes["minas-tirith"].buffer(0.75), xx, yy)
    mountain_pixels = int((new_mountains & in_spur).sum())
    assert mountain_pixels > 1000, "The mountain spur has not been visibly extended"
    return {
        "changed_pixels": len(xs), "outside_windows_changed_pixels": int(outside.sum()),
        "mountain_spur_darker_pixels": mountain_pixels,
        "antialias_tolerance_in_working_units": 0.75,
    }


def crop_indices(view_box):
    x, y, width, height = view_box
    return (round((x - 335) * 6144 / 1632), round((y - 40) * 5037 / 1338),
            round(width * 6144 / 1632), round(height * 5037 / 1338))


def verify_forest_removal(before, after, regions):
    x, y, width, height = crop_indices([1784, 544, 160, 158])
    old = before[y:y + height, x:x + width]
    new = after[y:y + height, x:x + width]
    area = box(1784, 544, 1944, 702)
    interiors = unary_union([region.buffer(-2) for region in regions.values() if region.intersects(area)])
    ys, xs = np.indices(old.shape)
    xx, yy = world_coordinates(xs + x, ys + y)
    inside = contains_xy(interiors, xx, yy)
    old, new = old[inside], new[inside]
    old_ink, new_ink = int((old < 240).sum()), int((new < 240).sum())
    assert old_ink > 50000 and new_ink == 0, f"Forest ink before/after: {old_ink}/{new_ink}"
    return {"ink_pixels_before": old_ink, "ink_pixels_after": new_ink,
            "minimum_gray_after": int(new.min()), "original_region_boundaries_excluded": True}


def verify_paper():
    with Image.open(OUTPUT) as image:
        assert image.size == (6144, 5037)
        rgb = np.asarray(image.convert("RGB"), dtype=np.int16)
        alpha = image.convert("RGBA").getchannel("A").getextrema()
    red, green, blue = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    warm = (red > green) & (green > blue)
    ratio = float(warm.mean())
    assert ratio > 0.98 and alpha == (255, 255)
    return {"warm_pixel_fraction": ratio, "alpha_extrema": list(alpha),
            "mean_rgb": [round(float(rgb[..., i].mean()), 2) for i in range(3)]}


def make_details():
    crops = {"minas-tirith": [1200, 835, 245, 245], "east": [1695, 460, 272, 305]}
    for name, view_box in crops.items():
        x, y, width, height = crop_indices(view_box)
        subprocess.run([
            "sips", "--cropToHeightWidth", str(height), str(width), "--cropOffset", str(y), str(x),
            str(OUTPUT), "--out", str(FOLDER / f"detail-{name}.png"),
        ], check=True, capture_output=True, timeout=30)
    return {"crops": crops, "source": str(OUTPUT.relative_to(ROOT))}


def main():
    layout = json.loads((FOLDER / "layout.json").read_text())
    source = ET.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    regions = GEOMETRY.region_polygons(source)
    shapes = {patch["id"]: patch_geometry(patch, regions) for patch in layout["patches"]}
    assert regions["minas-tirith"].covers(shapes["minas-tirith"])
    before = pixels(FOLDER.parent / "middle-earth-pencil-v4.png")
    after = pixels(FOLDER / "map-linework.png")
    report = {
        "task": "MAP-07", "locality": verify_locality(before, after, shapes),
        "forest_removal": verify_forest_removal(before, after, regions), "paper": verify_paper(),
        "details": make_details(), "mountain_patch_inside_minas_tirith": True,
    }
    (FOLDER / "edit-verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
