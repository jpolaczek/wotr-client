"""Render the generated terrain with the unchanged authoritative SVG boundaries."""

import base64
from copy import deepcopy
import hashlib
import json
from pathlib import Path
from xml.etree import ElementTree

import cairosvg
import cv2


FOLDER = Path(__file__).resolve().parent
ROOT = FOLDER.parents[2]
OUTLINES = ROOT / "graphicalassets/region-map/map-regions.svg"
TERRAIN = FOLDER / "terrain-generated.png"
CITY_DETAIL = FOLDER / "city-detail-generated.png"
CITY_DETAIL_BOX = (745, 710, 210, 170)
TERRAIN_SIZE = (1385, 1136)
SVG = "{http://www.w3.org/2000/svg}"
XLINK = "{http://www.w3.org/1999/xlink}"


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify_inputs():
    manifest = json.loads((FOLDER / "inputs.json").read_text())
    checked = {}
    for name, expected in manifest["sha256"].items():
        actual = sha256(ROOT / name)
        if actual != expected:
            raise ValueError(f"Source changed: {name}")
        checked[name] = actual
    return checked


def image_element(path, element_id, box):
    x, y, width, height = box
    return ElementTree.Element(SVG + "image", {
        "id": element_id,
        "x": str(x),
        "y": str(y),
        "width": str(width),
        "height": str(height),
        "preserveAspectRatio": "none",
        XLINK + "href": "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode(),
    })


def detail_box_in_map(view_box):
    x, y, width, height = view_box
    crop_x, crop_y, crop_width, crop_height = CITY_DETAIL_BOX
    base_width, base_height = TERRAIN_SIZE
    return (
        x + crop_x * width / base_width,
        y + crop_y * height / base_height,
        crop_width * width / base_width,
        crop_height * height / base_height,
    )


def compose_map(source):
    root = deepcopy(source)
    root.find(f"{SVG}title").text = "Middle-earth terrain with region boundaries"
    root.find(f"{SVG}desc").text = (
        "Illustrative terrain from imagegen, with the original region geometry. "
        "No visible text. This image is a base for subsequent styling.")
    regions = root.find(f'{SVG}g[@id="regions"]')
    regions.set("fill", "none")
    view_box = tuple(map(float, root.get("viewBox").split()))
    terrain = ElementTree.Element(SVG + "g", {"id": "terrain-layers"})
    terrain.append(image_element(TERRAIN, "terrain-base", view_box))
    terrain.append(image_element(CITY_DETAIL, "terrain-minas-tirith", detail_box_in_map(view_box)))
    root.insert(list(root).index(regions), terrain)
    return root


def verify_geometry(source, composition):
    for group_id in ("regions", "boundaries"):
        query = f'{SVG}g[@id="{group_id}"]/{SVG}path'
        before = [path.attrib for path in source.findall(query)]
        after = [path.attrib for path in composition.findall(query)]
        if before != after:
            raise ValueError(f"Changed geometry: {group_id}")
    assert len(composition.findall(f'{SVG}g[@id="regions"]/{SVG}path')) == 105
    assert not composition.findall(f".//{SVG}text")


def main():
    sources = verify_inputs()
    image = cv2.imread(str(TERRAIN), cv2.IMREAD_UNCHANGED)
    detail = cv2.imread(str(CITY_DETAIL), cv2.IMREAD_UNCHANGED)
    if image is None or detail is None:
        raise ValueError("Save the selected imagegen base and city detail first")
    if (image.shape[1], image.shape[0]) != TERRAIN_SIZE:
        raise ValueError("The detail box must be recalibrated for this base image size")
    source = ElementTree.parse(OUTLINES).getroot()
    composition = compose_map(source)
    verify_geometry(source, composition)
    svg = ElementTree.tostring(composition, encoding="utf-8", xml_declaration=True)
    (FOLDER / "map-composition.svg").write_bytes(svg)
    target = FOLDER.parent / "middle-earth-terrain-v1.png"
    preview = FOLDER / "map-preview.png"
    cairosvg.svg2png(bytestring=svg, write_to=str(target))
    cairosvg.svg2png(bytestring=svg, write_to=str(preview), output_width=1536,
                    output_height=1259)
    output = cv2.imread(str(target), cv2.IMREAD_UNCHANGED)
    assert output.shape[:2] == (5037, 6144)
    report = {
        "task": "MAP-02",
        "generation_mode": "built-in image_gen",
        "generated_terrain_size": [image.shape[1], image.shape[0]],
        "generated_city_detail_size": [detail.shape[1], detail.shape[0]],
        "city_detail_box_in_terrain_pixels": list(CITY_DETAIL_BOX),
        "composition_png_size": [output.shape[1], output.shape[0]],
        "composition_channels": output.shape[2],
        "unchanged_named_region_paths": 105,
        "unchanged_boundary_paths": len(source.findall(f'{SVG}g[@id="boundaries"]/{SVG}path')),
        "svg_visible_text_elements": 0,
        "source_sha256": sources,
        "output_sha256": {
            "terrain-generated.png": sha256(TERRAIN),
            "city-detail-generated.png": sha256(CITY_DETAIL),
            "map-composition.svg": sha256(FOLDER / "map-composition.svg"),
            "../middle-earth-terrain-v1.png": sha256(target),
        },
    }
    (FOLDER / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
