"""Place local imagegen corrections on the unchanged region SVG."""

from copy import deepcopy
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import tempfile
from xml.etree import ElementTree as ET

import numpy as np
from PIL import Image
from shapely import contains_xy
from shapely.geometry import Polygon, box


FOLDER = Path(__file__).resolve().parent
ROOT = FOLDER.parents[2]
SVG = "{http://www.w3.org/2000/svg}"
XLINK = "{http://www.w3.org/1999/xlink}"
OUTPUT = FOLDER.parent / "middle-earth-pencil-v2.png"
INK_LIMIT = 240


def load_exporter():
    path = FOLDER.parent / "pencil-v1/build_map.py"
    spec = importlib.util.spec_from_file_location("pencil_v1_exporter", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.FOLDER = FOLDER
    module.TARGET = OUTPUT
    return module


def polygon_path(polygon):
    points = list(polygon.exterior.coords)
    return "M" + " L".join(f"{x:.4f},{y:.4f}" for x, y in points) + " Z"


def region_polygons(source):
    return {
        node.get("data-region"): Polygon([
            tuple(map(float, pair))
            for pair in re.findall(r"[ML]([\d.]+),([\d.]+)", node.get("d"))
        ])
        for node in source.findall(f'{SVG}g[@id="regions"]/{SVG}path')
    }


class Composer:
    def __init__(self, exporter, source):
        self.exporter = exporter
        self.root = exporter.compose(source)
        self.defs = self.root.find(SVG + "defs")
        self.terrain = self.root.find(f'{SVG}g[@id="terrain-layers"]')
        self.serial = 0
        self.sizes = {}
        ink = ET.SubElement(self.defs, SVG + "filter", {
            "id": "pencil-ink", "color-interpolation-filters": "sRGB",
        })
        ET.SubElement(ink, SVG + "feColorMatrix", {
            "type": "matrix",
            "values": "0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  -0.2126 -0.7152 -0.0722 0 1",
        })
        feather = ET.SubElement(self.defs, SVG + "filter", {"id": "patch-feather"})
        ET.SubElement(feather, SVG + "feGaussianBlur", {"stdDeviation": "2.5"})

    def add_raster(self, name, filename):
        path = FOLDER / filename
        with Image.open(path) as image:
            width, height = image.size
        self.sizes[name] = (width, height)
        self.defs.append(self.exporter.raster_element(path, {
            "id": name, "width": str(width), "height": str(height),
        }))

    def clip(self, parent, polygon):
        self.serial += 1
        identifier = f"local-clip-{self.serial}"
        clip = ET.SubElement(self.defs, SVG + "clipPath", {
            "id": identifier, "clipPathUnits": "userSpaceOnUse",
        })
        ET.SubElement(clip, SVG + "path", {"d": polygon_path(polygon)})
        return ET.SubElement(parent, SVG + "g", {"clip-path": f"url(#{identifier})"})

    def place(self, name, source_box, matrix, polygon, ink=False):
        a, d, tx, ty = matrix
        sx, sy, sw, sh = source_box
        footprint = box(a * sx + tx, d * sy + ty, a * (sx + sw) + tx, d * (sy + sh) + ty)
        group = self.clip(self.terrain, footprint)
        group = self.clip(group, polygon)
        if ink:
            group = ET.SubElement(group, SVG + "g", {"filter": "url(#pencil-ink)"})
        else:
            self.serial += 1
            identifier = f"patch-mask-{self.serial}"
            x0, y0, x1, y1 = polygon.bounds
            mask = ET.SubElement(self.defs, SVG + "mask", {
                "id": identifier, "maskUnits": "userSpaceOnUse",
                "x": str(x0), "y": str(y0),
                "width": str(x1 - x0), "height": str(y1 - y0),
            })
            ET.SubElement(mask, SVG + "path", {
                "d": polygon_path(polygon), "fill": "white", "filter": "url(#patch-feather)",
            })
            group = ET.SubElement(group, SVG + "g", {"mask": f"url(#{identifier})"})
        ET.SubElement(group, SVG + "use", {
            XLINK + "href": f"#{name}", "transform": f"matrix({a} 0 0 {d} {tx} {ty})",
        })

    def tile(self, name, index, target_box, polygon):
        width, height = self.sizes[name]
        cell_width, cell_height = width / 3, height / 2
        sx, sy = index % 3 * cell_width, index // 3 * cell_height
        x, y, w, h = target_box
        a, d = w / cell_width, h / cell_height
        self.place(name, (sx, sy, cell_width, cell_height), (a, d, x - a * sx, y - d * sy), polygon)


def fit_forest(polygon, xs, ys, region):
    safe = polygon.buffer(-4)
    center = safe.representative_point()
    if region == "old-forest-road":
        options = []
        for sx in np.arange(0.94, 0.49, -0.04):
            for sy in np.arange(0.70, 0.19, -0.025):
                for dy in (4, 8, 12, 16):
                    ox, oy = 1380, 355
                    xx = ox + (xs - ox) * sx
                    yy = oy + (ys - oy) * sy + dy
                    if contains_xy(safe, xx, yy).all():
                        options.append((sx * sy, sx, sy, ox, oy, 0, dy))
        if not options:
            raise ValueError("No safe placement for Old Forest Road")
        _, sx, sy, ox, oy, dx, dy = max(options)
        return sx, sy, ox, oy, dx, dy
    for scale in np.arange(0.98, 0.19, -0.01):
        xx = center.x + (xs - center.x) * scale
        yy = center.y + (ys - center.y) * scale
        if contains_xy(safe, xx, yy).all():
            return scale, scale, center.x, center.y, 0, 0
    raise ValueError(f"No safe placement for {region}")


def add_forests(composer, polygons):
    layout = json.loads((FOLDER / "forest-layout.json").read_text())
    with Image.open(FOLDER / "forest-atlas.png") as image:
        pixels = np.asarray(image.convert("L"))
    height, width = pixels.shape
    cw, ch = width // 3, height // 3
    report = {}
    for item in layout["regions"]:
        region = item["id"]
        col, row = item["cell"]
        cell = pixels[row * ch:(row + 1) * ch, col * cw:(col + 1) * cw]
        py, px = np.where(cell < INK_LIMIT)
        vx, vy, vw, vh = item["view_box"]
        xs, ys = vx + (px + 0.5) * vw / cw, vy + (py + 0.5) * vh / ch
        sx, sy, ox, oy, dx, dy = fit_forest(polygons[region], xs, ys, region)
        xx, yy = ox + (xs - ox) * sx + dx, oy + (ys - oy) * sy + dy
        assert contains_xy(polygons[region].buffer(-4), xx, yy).all()
        a, d = sx * vw / cw, sy * vh / ch
        tx = ox * (1 - sx) + sx * vx + dx - a * col * cw
        ty = oy * (1 - sy) + sy * vy + dy - d * row * ch
        composer.place("forest-art", (col * cw, row * ch, cw, ch), (a, d, tx, ty), polygons[region].buffer(-2), ink=True)
        report[region] = {"ink_pixels": len(px), "outside_region": 0, "margin": 4, "scale": [round(sx, 3), round(sy, 3)]}
    return report


def add_erebor(composer, polygon):
    source_box = (600, 180, 270, 135)
    with Image.open(FOLDER / "corrections-atlas.png") as image:
        pixels = np.asarray(image.convert("L"))[180:315, 600:870]
    py, px = np.where(pixels < INK_LIMIT)
    px, py = px + 600, py + 180
    cx, cy = (px.min() + px.max()) / 2, (py.min() + py.max()) / 2
    for scale in np.arange(0.38, 0.09, -0.005):
        tx, ty = 1495 - scale * cx, 182 - scale * cy
        if contains_xy(polygon.buffer(-7), px * scale + tx, py * scale + ty).all():
            break
    assert contains_xy(polygon.buffer(-7), px * scale + tx, py * scale + ty).all()
    composer.place("corrections", source_box, (scale, scale, tx, ty), polygon.buffer(-3), ink=True)
    return {"ink_pixels": len(px), "outside_region": 0, "margin": 7, "scale": round(scale, 3)}


def assemble(exporter, source):
    composer = Composer(exporter, source)
    polygons = region_polygons(source)
    for name, filename in (("corrections", "corrections-atlas.png"), ("clean", "clean-background-atlas.png"), ("forest-art", "forest-atlas.png"), ("moria-art", "moria-passage.png"), ("lorien-bg", "lorien-background.png")):
        composer.add_raster(name, filename)
    patches = json.loads((FOLDER / "patches.json").read_text())["patches"]
    backgrounds = [
        ("corrections", 0, box(1190, 880, 1410, 1075)),
        ("corrections", 2, Polygon([
            (1490, 942), (1530, 926), (1585, 940), (1620, 970),
            (1638, 990), (1615, 1014), (1570, 1018), (1525, 1003),
            (1490, 991),
        ])),
        ("clean", 1, box(1380, 142, 1550, 227)),
        ("clean", 4, box(700, 370, 835, 482)),
        ("clean", 5, box(1255, 215, 1485, 635)),
    ]
    for name, index, polygon in backgrounds:
        composer.tile(name, index, patches[index]["view_box"], polygon)
    # Use generated blank paper under the relocated peak's former foothills.
    composer.place("clean", (700, 220, 70, 50),
                   (127 / 70, 35 / 50, 1388 - 700 * 127 / 70, 195 - 220 * 35 / 50),
                   box(1388, 195, 1515, 230))
    forest_outline = [(195, 120), (270, 141), (311, 183), (339, 222),
                      (340, 271), (400, 278), (459, 316), (455, 424),
                      (432, 457), (397, 464), (350, 460), (332, 474),
                      (300, 463), (275, 454), (251, 443), (204, 443),
                      (165, 407), (149, 378), (138, 340), (151, 305),
                      (140, 274), (156, 240), (147, 206), (167, 169)]
    cleanup = Polygon([(1070 + x * 0.3, 475 + y * 0.3) for x, y in forest_outline]).buffer(5)
    lw, lh = composer.sizes["lorien-bg"]
    composer.place("lorien-bg", (0, 0, lw, lh), (180 / lw, 180 / lh, 1070, 475), cleanup)
    mw, mh = composer.sizes["moria-art"]
    composer.place("moria-art", (0, 0, mw, mh), (300 / mw, 300 / mh, 980, 300), box(1030, 400, 1185, 510))
    forests = add_forests(composer, polygons)
    erebor = add_erebor(composer, polygons["erebor"])
    return composer.root, {"forests": forests, "erebor": erebor}


def render_pngs(composition):
    with tempfile.TemporaryDirectory(prefix="wotr-map04-render-") as directory:
        for target, width, height in ((OUTPUT, 6144, 5037), (FOLDER / "map-preview.png", 1536, 1259)):
            root = deepcopy(composition)
            root.set("width", str(width))
            root.set("height", str(height))
            path = Path(directory) / f"map-{width}.svg"
            path.write_bytes(ET.tostring(root))
            subprocess.run(["node", str(FOLDER / "render_svg.cjs"), str(path), str(target)], check=True, timeout=60)


def main():
    exporter = load_exporter()
    inputs = exporter.verify_sources()
    source = ET.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    composition, fit = assemble(exporter, source)
    counts = exporter.verify_paths(source, composition)
    (FOLDER / "map-composition.svg").write_bytes(ET.tostring(composition, encoding="utf-8", xml_declaration=True))
    render_pngs(composition)
    stats = exporter.raster_statistics(OUTPUT)
    assert stats["size"] == [6144, 5037]
    assert stats["maximum_rgb_channel_difference"] == 0
    report = {"task": "MAP-04", "composition": stats, "unchanged_paths": counts, "feature_fit": fit,
              "viewport_landmarks": exporter.verify_viewport(source, OUTPUT), "source_sha256": inputs["sha256"],
              "output_sha256": exporter.sha256(OUTPUT)}
    (FOLDER / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({"composition": stats, "paths": counts, "fit": fit}, indent=2))


if __name__ == "__main__":
    main()
