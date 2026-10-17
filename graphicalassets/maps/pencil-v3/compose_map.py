"""Replace forest layers while retaining the approved map and exact paths."""

from copy import deepcopy
import json
from xml.etree import ElementTree as ET

from PIL import Image
from shapely.geometry import Polygon
from shapely.ops import unary_union

from prepare_references import FOLDER, ROOT, SVG, XLINK, load_previous, render


PREVIOUS = load_previous()
OUTPUT = FOLDER.parent / "middle-earth-pencil-v3.png"


class ForestComposer(PREVIOUS.Composer):
    def __init__(self, exporter, root):
        self.exporter = exporter
        self.root = root
        self.defs = root.find(SVG + "defs")
        self.terrain = root.find(f'{SVG}g[@id="terrain-layers"]')
        self.serial = 100
        self.sizes = {}


def replace_previous_forests(composer):
    layers = [
        node for node in composer.terrain
        if any(use.get(XLINK + "href") == "#forest-art" for use in node.iter(SVG + "use"))
    ]
    assert len(layers) == 9
    # MAP-04 ordering: Old Forest, Lorien, then the seven Mirkwood fields.
    for layer in layers[1:]:
        composer.terrain.remove(layer)


def clean_fangorn(composer):
    composer.add_raster("fangorn-clean", FOLDER / "fangorn-cleanup.png")
    width, height = composer.sizes["fangorn-clean"]
    outline = [
        (176, 586), (219, 531), (225, 463), (274, 432), (320, 403),
        (360, 443), (432, 458), (463, 412), (526, 409), (574, 458),
        (614, 452), (652, 435), (705, 475), (722, 529), (756, 569),
        (766, 625), (737, 690), (700, 726), (698, 783), (643, 833),
        (610, 867), (605, 940), (546, 950), (501, 925), (469, 943),
        (421, 908), (380, 906), (349, 876), (310, 884), (280, 852),
        (235, 823), (192, 795), (174, 732), (185, 669), (174, 636),
    ]
    mask = Polygon([(995 + x * 0.24, 565 + y * 250 / 1042) for x, y in outline]).buffer(4)
    composer.place(
        "fangorn-clean", (0, 0, width, height),
        (240 / width, 250 / height, 995, 565), mask,
    )
    composer.terrain[-1].set("data-cleanup", "old-fangorn")


def add_forest(composer, name, item, polygon):
    composer.add_raster(name + "-art", FOLDER / f"{name}.png")
    width, height = composer.sizes[name + "-art"]
    x, y, world_width, world_height = item["view_box"]
    composer.place(
        name + "-art", (0, 0, width, height),
        (world_width / width, world_height / height, x, y),
        polygon.buffer(-2), ink=True,
    )
    composer.terrain[-1].set("data-forest", name)
    if name == "lorien":
        strong = deepcopy(composer.defs.find(f'{SVG}filter[@id="pencil-ink"]'))
        strong.set("id", "lorien-clear-ink")
        strong.find(SVG + "feColorMatrix").set(
            "values", "0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  -0.26575 -0.894 -0.09025 0 1.25",
        )
        composer.defs.append(strong)
        for node in composer.terrain[-1].iter():
            if node.get("filter") == "url(#pencil-ink)":
                node.set("filter", "url(#lorien-clear-ink)")


def export(composition):
    (FOLDER / "map-composition.svg").write_bytes(ET.tostring(composition, encoding="utf-8", xml_declaration=True))
    render(composition, OUTPUT)
    preview = deepcopy(composition)
    preview.set("width", "1536")
    preview.set("height", "1259")
    render(preview, FOLDER / "map-preview.png")


def raster_sizes():
    sizes = {}
    for name in ("mirkwood", "fangorn", "lorien", "fangorn-cleanup"):
        with Image.open(FOLDER / f"{name}.png") as image:
            sizes[name] = list(image.size)
    return sizes


def main():
    exporter = PREVIOUS.load_exporter()
    exporter.FOLDER = FOLDER
    inputs = exporter.verify_sources()
    source = ET.parse(ROOT / "graphicalassets/region-map/map-regions.svg").getroot()
    root = ET.parse(FOLDER.parent / "pencil-v2/map-composition.svg").getroot()
    root.find(SVG + "desc").text = "Pencil map with continuous Mirkwood, stronger Lorien and contained Fangorn."
    composer = ForestComposer(exporter, root)
    replace_previous_forests(composer)
    clean_fangorn(composer)
    polygons = PREVIOUS.region_polygons(source)
    layout = json.loads((FOLDER / "forest-layout.json").read_text())
    for name in ("mirkwood", "fangorn", "lorien"):
        item = layout[name]
        polygon = unary_union([polygons[key] for key in item["regions"]])
        add_forest(composer, name, item, polygon)
    paths = exporter.verify_paths(source, root)
    export(root)
    statistics = exporter.raster_statistics(OUTPUT)
    assert statistics["size"] == [6144, 5037]
    assert statistics["maximum_rgb_channel_difference"] == 0
    report = {
        "task": "MAP-05", "composition": statistics, "unchanged_paths": paths,
        "source_sha256": inputs["sha256"], "output_sha256": exporter.sha256(OUTPUT),
        "viewport_landmarks": exporter.verify_viewport(source, OUTPUT),
        "lorien_stroke_strength": 1.25,
        "native_rasters": raster_sizes(),
    }
    (FOLDER / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({"composition": statistics, "paths": paths}, indent=2))


if __name__ == "__main__":
    main()
