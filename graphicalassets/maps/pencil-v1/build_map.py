"""Compose a monochrome terrain raster with the unchanged map region paths."""

import base64
from copy import deepcopy
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
from xml.etree import ElementTree

from PIL import Image, ImageChops


FOLDER = Path(__file__).resolve().parent
ROOT = FOLDER.parents[2]
SVG = "{http://www.w3.org/2000/svg}"
XLINK = "{http://www.w3.org/1999/xlink}"
TERRAIN = FOLDER / "terrain-pencil.png"
TARGET = FOLDER.parent / "middle-earth-pencil-v1.png"


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify_sources():
    inputs = json.loads((FOLDER / "inputs.json").read_text())
    for name, expected in inputs["sha256"].items():
        if sha256(ROOT / name) != expected:
            raise ValueError(f"Source changed: {name}")
    return inputs


def raster_statistics(path):
    with Image.open(path) as image:
        rgb = image.convert("RGB")
        red, green, blue = rgb.split()
        channel_difference = max(
            ImageChops.difference(red, green).getextrema()[1],
            ImageChops.difference(red, blue).getextrema()[1],
            ImageChops.difference(green, blue).getextrema()[1],
        )
        histogram = rgb.convert("L").histogram()
        return {
            "size": list(image.size),
            "mode": image.mode,
            "maximum_rgb_channel_difference": channel_difference,
            "white_fraction_above_245": sum(histogram[246:]) / (image.width * image.height),
        }


def raster_element(path, attributes):
    attributes[XLINK + "href"] = "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode()
    return ElementTree.Element(SVG + "image", attributes)


def compose(source):
    root = deepcopy(source)
    root.find(SVG + "title").text = "Minimal pencil map of Middle-earth"
    root.find(SVG + "desc").text = (
        "Uncolored geographic line drawing without visible labels or buildings. "
        "Original region geometry over an imagegen terrain layer.")
    regions = root.find(f'{SVG}g[@id="regions"]')
    regions.set("fill", "none")
    definitions = ElementTree.SubElement(root, SVG + "defs")
    monochrome = ElementTree.SubElement(definitions, SVG + "filter", {
        "id": "pencil-monochrome", "color-interpolation-filters": "sRGB",
    })
    ElementTree.SubElement(monochrome, SVG + "feColorMatrix", {
        "type": "saturate", "values": "0",
    })
    x, y, width, height = root.get("viewBox").split()
    terrain = ElementTree.Element(SVG + "g", {
        "id": "terrain-layers", "filter": "url(#pencil-monochrome)",
    })
    terrain.append(raster_element(TERRAIN, {
        "id": "terrain-pencil-base",
        "x": x, "y": y, "width": width, "height": height,
        "preserveAspectRatio": "none",
    }))
    root.insert(list(root).index(regions), terrain)
    return root


def verify_paths(source, composition):
    counts = {}
    for name in ("regions", "boundaries"):
        query = f'{SVG}g[@id="{name}"]/{SVG}path'
        before = [node.attrib for node in source.findall(query)]
        after = [node.attrib for node in composition.findall(query)]
        if before != after:
            raise ValueError(f"Changed path attributes: {name}")
        counts[name] = len(after)
    assert counts == {"regions": 105, "boundaries": 323}
    assert not composition.findall(f".//{SVG}text")
    return counts


def render_pngs(composition):
    if not shutil.which("qlmanage") or not shutil.which("sips"):
        raise ValueError("This SVG export uses macOS Quick Look and sips")
    sizes = [(TARGET, 6144, 5037), (FOLDER / "map-preview.png", 1536, 1259)]
    with tempfile.TemporaryDirectory(prefix="wotr-pencil-render-") as temporary:
        temp = Path(temporary)
        for target, width, height in sizes:
            document = deepcopy(composition)
            document.set("width", str(width))
            document.set("height", str(height))
            document.set("x", "0")
            document.set("y", str((width - height) // 2))
            wrapper = ElementTree.Element(SVG + "svg", {
                "width": str(width), "height": str(width),
                "viewBox": f"0 0 {width} {width}",
            })
            wrapper.append(document)
            path = temp / f"map-{width}.svg"
            path.write_bytes(ElementTree.tostring(wrapper, encoding="utf-8"))
            subprocess.run([
                "qlmanage", "-t", "-s", str(width), "-o", str(temp), str(path),
            ], check=True, capture_output=True, timeout=30)
            # Quick Look produces a square. Trim only the extra export canvas.
            subprocess.run([
                "sips", "--cropToHeightWidth", str(height), str(width),
                str(path) + ".png", "--out", str(target),
            ], check=True, capture_output=True, timeout=30)


def verify_viewport(source, path):
    """Detect shifted or truncated export using the original top/bottom edges."""
    segments = []
    for boundary in source.findall(f'{SVG}g[@id="boundaries"]/{SVG}path'):
        if boundary.get("data-region-a") != "outside":
            continue
        points = [
            (float(x), float(y))
            for x, y in re.findall(r"[ML]([\d.]+),([\d.]+)", boundary.get("d"))
        ]
        segments.extend(zip(points, points[1:]))
    ys = [point[1] for segment in segments for point in segment]
    x, y, width, height = map(float, source.get("viewBox").split())
    checks = {}
    with Image.open(path) as raster:
        for name, edge in (("top", min(ys)), ("bottom", max(ys))):
            candidates = [
                (a, b) for a, b in segments if a[1] == b[1] == edge
            ]
            a, b = max(candidates, key=lambda pair: abs(pair[1][0] - pair[0][0]))
            px = round(((a[0] + b[0]) / 2 - x) * raster.width / width)
            py = round((edge - y) * raster.height / height)
            darkest = min(
                raster.getpixel((px + dx, py + dy))[0]
                for dx in range(-3, 4) for dy in range(-3, 4)
            )
            # Preview strokes cover fractional pixels and become antialiased gray.
            if darkest > 160:
                raise ValueError(f"Missing {name} boundary in export: {path}")
            checks[name] = {"pixel": [px, py], "minimum_gray": darkest}
    return checks


def main():
    inputs = verify_sources()
    terrain = raster_statistics(TERRAIN)
    if terrain["white_fraction_above_245"] < 0.65:
        raise ValueError(f"Terrain is too densely shaded for this version: {terrain}")
    source = ElementTree.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    composition = compose(source)
    counts = verify_paths(source, composition)
    svg = ElementTree.tostring(composition, encoding="utf-8", xml_declaration=True)
    (FOLDER / "map-composition.svg").write_bytes(svg)
    render_pngs(composition)
    output = raster_statistics(TARGET)
    assert output["size"] == [6144, 5037]
    assert output["maximum_rgb_channel_difference"] == 0
    report = {
        "task": "MAP-03",
        "generation_mode": "built-in image_gen",
        "monochrome_export": "SVG feColorMatrix saturate=0, macOS Quick Look, viewport trim by sips",
        "terrain": terrain,
        "composition": output,
        "unchanged_paths": counts,
        "viewport_landmarks": {
            "full": verify_viewport(source, TARGET),
            "preview": verify_viewport(source, FOLDER / "map-preview.png"),
        },
        "svg_text_elements": 0,
        "source_sha256": inputs["sha256"],
        "output_sha256": {
            "terrain-pencil.png": sha256(TERRAIN),
            "map-composition.svg": sha256(FOLDER / "map-composition.svg"),
            "../middle-earth-pencil-v1.png": sha256(TARGET),
        },
    }
    (FOLDER / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
