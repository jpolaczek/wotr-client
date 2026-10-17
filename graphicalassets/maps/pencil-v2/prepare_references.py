"""Render registered SVG contact sheets for local imagegen corrections."""

import base64
import json
from pathlib import Path
from xml.etree import ElementTree as ET

import cairosvg


FOLDER = Path(__file__).resolve().parent
ROOT = FOLDER.parents[2]
SVG = "{http://www.w3.org/2000/svg}"
XLINK = "{http://www.w3.org/1999/xlink}"


def image_element(path):
    return ET.Element(SVG + "image", {
        "x": "335", "y": "40", "width": "1632", "height": "1338",
        "preserveAspectRatio": "none",
        XLINK + "href": "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode(),
    })


def main():
    spec = json.loads((FOLDER / "patches.json").read_text())
    regions = json.loads((ROOT / "graphicalassets/region-map/geometry.json").read_text())["regions"]
    paths = {region["id"]: region["path"] for region in regions}
    variants = {
        "edit-target": FOLDER.parent / "pencil-v1/terrain-pencil.png",
        "region-guide": FOLDER.parent / "pencil-v1/terrain-pencil.png",
        "board-reference": FOLDER.parent / "terrain-v1/source-layout-reference.png",
    }
    for name, raster in variants.items():
        root = ET.Element(SVG + "svg", {"width": "1536", "height": "1024", "viewBox": "0 0 1536 1024"})
        ET.SubElement(root, SVG + "rect", {"width": "1536", "height": "1024", "fill": "white"})
        for index, patch in enumerate(spec["patches"]):
            panel = ET.SubElement(root, SVG + "svg", {
                "x": str(index % 3 * 512), "y": str(index // 3 * 512),
                "width": "512", "height": "512", "overflow": "hidden",
                "viewBox": " ".join(map(str, patch["view_box"])),
            })
            panel.append(image_element(raster))
            if name == "region-guide":
                for region in patch["regions"]:
                    color = "#bf00bf" if region == "moria" else "#008dcc"
                    ET.SubElement(panel, SVG + "path", {
                        "d": paths[region], "fill": color, "fill-opacity": "0.06",
                        "stroke": color, "stroke-width": "1.4", "stroke-linejoin": "round",
                    })
        cairosvg.svg2png(bytestring=ET.tostring(root), write_to=str(FOLDER / f"{name}.png"))
    print("Prepared three registered 3 × 2 reference atlases, 1536 × 1024")


if __name__ == "__main__":
    main()
