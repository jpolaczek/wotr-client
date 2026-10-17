"""Composite two imagegen corrections below the original region contours."""

from copy import deepcopy
import json
from xml.etree import ElementTree as ET

import numpy as np
from PIL import Image
from shapely import contains_xy
from shapely.geometry import Polygon, box
from shapely.ops import unary_union

from prepare_inputs import FOLDER, ROOT, PREVIOUS, SVG, crop


OUTPUT = FOLDER.parent / "middle-earth-parchment-v4.png"


def patch_area(patch, source):
    if "region" in patch:
        return PREVIOUS.GEOMETRY.region_area(source, patch["region"]).intersection(box(*patch["limits"]))
    return Polygon(patch["polygon"])


def add_patch(root, helper, patch, area):
    prefix = "map10-" + patch["id"]
    definitions = root.find(SVG + "defs")
    path = PREVIOUS.BASE.NATIVE.geometry_path(area)
    clip = ET.SubElement(definitions, SVG + "clipPath", {
        "id": prefix + "-area", "clipPathUnits": "userSpaceOnUse",
    })
    ET.SubElement(clip, SVG + "path", {"d": path, "clip-rule": "evenodd"})
    terrain = root.find(f'{SVG}g[@id="map-ink"]/{SVG}g[@id="terrain-layers"]')
    group = ET.SubElement(terrain, SVG + "g", {
        "id": prefix, "clip-path": f"url(#{prefix}-area)",
    })
    if patch["feather"]:
        feather = ET.SubElement(definitions, SVG + "filter", {"id": prefix + "-feather"})
        ET.SubElement(feather, SVG + "feGaussianBlur", {"stdDeviation": str(patch["feather"])})
        mask = ET.SubElement(definitions, SVG + "mask", {
            "id": prefix + "-mask", "maskUnits": "userSpaceOnUse",
            "x": "335", "y": "40", "width": "1632", "height": "1338",
        })
        ET.SubElement(mask, SVG + "path", {
            "d": path, "fill": "white", "filter": f"url(#{prefix}-feather)",
        })
        group = ET.SubElement(group, SVG + "g", {"mask": f"url(#{prefix}-mask)"})
    placement = dict(zip(("x", "y", "width", "height"), map(str, patch["placement"])))
    placement["preserveAspectRatio"] = "none"
    group.append(helper.raster_element(FOLDER / (patch["id"] + "-terrain.png"), placement))


def verify_pixels(areas, layout):
    with Image.open(FOLDER.parent / "middle-earth-parchment-v3.png") as image:
        before = np.asarray(image.convert("RGB"))
    with Image.open(OUTPUT) as image:
        assert image.size == (6144, 5037)
        after = np.asarray(image.convert("RGB"))
    ys, xs = np.where(np.any(before != after, axis=2))
    xx = 335 + (xs + 0.5) * 1632 / 6144
    yy = 40 + (ys + 0.5) * 1338 / 5037
    outside = ~contains_xy(unary_union(areas).buffer(0.75), xx, yy)
    assert not outside.any(), "Changes outside the two local edit masks"
    protected = {}
    for name, bounds in layout["protected"].items():
        count = int(contains_xy(box(*bounds), xx, yy).sum())
        assert count == 0, name
        protected[name] = count
    return {"changed_pixels": len(xs), "outside_edit_changed_pixels": int(outside.sum()),
            "protected_changed_pixels": protected, "antialias_tolerance_working_units": 0.75}


def main():
    helper = PREVIOUS.BASE.NATIVE.exporter()
    helper.FOLDER = FOLDER
    inputs = helper.verify_sources()
    layout = json.loads((FOLDER / "layout.json").read_text())
    previous = ET.parse(FOLDER.parent / "parchment-v3/map-composition.svg").getroot()
    composition = deepcopy(previous)
    composition.find(SVG + "title").text = "Middle-earth — Black Gate passage and clear Erebor"
    composition.find(SVG + "desc").text = (
        "Local terrain corrections on preserved parchment with unchanged region contours.")
    source = ET.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    areas = []
    for patch in layout["patches"]:
        area = patch_area(patch, source)
        areas.append(area)
        add_patch(composition, helper, patch, area)
    counts = helper.verify_paths(source, composition.find(f'{SVG}g[@id="map-ink"]'))
    query = f'{SVG}image[@id="aged-paper"]'
    assert ET.tostring(previous.find(query)) == ET.tostring(composition.find(query))
    query = f'{SVG}g[@id="map-ink"]/{SVG}g[@id="terrain-layers"]'
    assert [ET.tostring(n) for n in previous.find(query)] == [ET.tostring(n) for n in list(composition.find(query))[:-2]]
    (FOLDER / "map-composition.svg").write_bytes(ET.tostring(composition, encoding="utf-8", xml_declaration=True))
    PREVIOUS.BASE.NATIVE.render(composition, OUTPUT)
    preview = deepcopy(composition)
    preview.set("width", "1536")
    preview.set("height", "1259")
    PREVIOUS.BASE.NATIVE.render(preview, FOLDER / "map-preview.png")
    for patch in layout["patches"]:
        crop(OUTPUT, FOLDER / (patch["id"] + "-after.png"), patch["window"])
    report = {"task": "MAP-10", "unchanged_paths": counts,
              "unchanged_paper_layer": True, "previous_terrain_layers_retained": True,
              "composition": helper.raster_statistics(OUTPUT),
              "pixel_comparison": verify_pixels(areas, layout),
              "source_sha256": inputs["sha256"], "output_sha256": helper.sha256(OUTPUT)}
    (FOLDER / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({k: v for k, v in report.items() if k != "source_sha256"}, indent=2))


if __name__ == "__main__":
    main()
