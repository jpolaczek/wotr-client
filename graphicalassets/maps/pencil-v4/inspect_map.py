"""Check river removal and edit locality; crop the final PNG for visual review."""

import base64
import json
import subprocess
from xml.etree import ElementTree as ET

import numpy as np
from PIL import Image
from shapely import contains_xy

from compose_map import FOLDER, OUTPUT, SVG, XLINK, render, river_geometry


DETAILS = {
    "west": [485, 270, 430, 430],
    "anduin": [1120, 340, 430, 430],
    "east": [1525, 310, 420, 420],
    "gondor": [1065, 890, 425, 425],
}


def pixels(path):
    with Image.open(path) as image:
        return np.asarray(image.convert("L"), dtype=np.int16)


def verify_locality():
    before = pixels(FOLDER.parent / "middle-earth-pencil-v3.png")
    after = pixels(OUTPUT)
    difference = np.abs(after - before)
    ys, xs = np.where(difference > 0)
    world_x = 335 + (xs + 0.5) * 1632 / before.shape[1]
    world_y = 40 + (ys + 0.5) * 1338 / before.shape[0]
    outside = ~contains_xy(river_geometry().buffer(0.75), world_x, world_y)
    changed = difference[ys[outside], xs[outside]]
    maximum = int(changed.max()) if changed.size else 0
    assert maximum <= 1, f"Changed terrain outside cleanup corridors: {maximum}"
    return {
        "changed_pixels": len(xs), "outside_corridors_changed_pixels": int(changed.size),
        "outside_corridors_maximum_gray_difference": maximum,
        "antialias_tolerance_in_working_units": 0.75,
    }


def verify_probes():
    before = pixels(FOLDER / "terrain-target.png")
    after = pixels(FOLDER / "terrain-review.png")
    report = []
    for item in json.loads((FOLDER / "river-probes.json").read_text()):
        x, y = item["point"]
        radius = item.get("radius", 4)
        area = (slice(y - radius, y + radius + 1), slice(x - radius, x + radius + 1))
        old = int(before[area].min())
        new = int(after[area].min())
        assert old < 230 and new >= 245, f"River residue at {item['id']}: {old} -> {new}"
        report.append({"id": item["id"], "minimum_gray_before": old, "minimum_gray_after": new})
    return report


def make_details():
    atlas = ET.Element(SVG + "svg", {"width": "1400", "height": "1400", "viewBox": "0 0 1400 1400"})
    for index, (name, view_box) in enumerate(DETAILS.items()):
        x, y, width, height = view_box
        px = round((x - 335) * 6144 / 1632)
        py = round((y - 40) * 5037 / 1338)
        pw, ph = round(width * 6144 / 1632), round(height * 5037 / 1338)
        target = FOLDER / f"detail-{name}.png"
        subprocess.run([
            "sips", "--cropToHeightWidth", str(ph), str(pw), "--cropOffset",
            str(py), str(px), str(OUTPUT), "--out", str(target),
        ], check=True, capture_output=True, timeout=30)
        ET.SubElement(atlas, SVG + "image", {
            "x": str(index % 2 * 700), "y": str(index // 2 * 700), "width": "700", "height": "700",
            XLINK + "href": "data:image/png;base64," + base64.b64encode(target.read_bytes()).decode(),
        })
    render(atlas, FOLDER / "map-details.png")


def main():
    report = {"task": "MAP-06", "locality": verify_locality(), "river_probes": verify_probes(),
              "detail_order": list(DETAILS), "details_are_crops_of_final_png": True}
    make_details()
    (FOLDER / "removal-verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
