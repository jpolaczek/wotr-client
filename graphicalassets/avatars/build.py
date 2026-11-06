#!/usr/bin/env python3
"""Builds the ten avatars (ART-09, AVATAR-01 A1) from the project's own art, so they stay replaceable (R08).

Each regular figure is cropped to its head and shoulders on a framed disc; the Eye and the Ring are drawn on the same
disc from their SVG sources, so the ten read as one set at 16 px and at 128 px. Outputs: the client's
Resources/Wotr/Art/Avatars/<id>.png (128 px), and with --site the website's assets/avatars/<id>.png (128 px) and
<id>@2x.png (256 px). manifest.json records each output's source and SHA-256.
Usage: build.py [--site <folder>] [--check] [--preview <contact sheet>]
Needs Pillow, and Chrome for the two SVG sources (graphicalassets/ui/render.py).
"""
import argparse
import hashlib
import importlib.util
import json
import re
import sys
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
UI = ROOT / "graphicalassets/ui"
FIGURES = ROOT / "graphicalassets/figures/v3/raw"
CLIENT = ROOT / "unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/Avatars"
MANIFEST = HERE / "manifest.json"
MASTER = 1024
# The head and shoulders of each regular in its 512 px picture: centre x, centre y, side of the square.
REGULARS = {
    "gondor": (246, 128, 176),
    "rohan": (236, 130, 176),
    "elves": (214, 132, 176),
    "dwarves": (262, 122, 196),
    "north": (232, 130, 176),
    "sauron": (262, 136, 196),
    "isengard": (252, 112, 196),
    "southrons": (238, 128, 176),
}
EMBLEMS = {"eye": "dice/faces/eye.svg", "ring": "icons/ring.svg"}
IDENTIFIERS = ["eye", "ring", "gondor", "rohan", "elves", "dwarves", "north", "sauron", "isengard", "southrons"]
# Mipmaps, unlike the other UI art: an avatar is drawn at 16 px from its 128 px texture.
TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 13
  internalIDToNameTable: []
  externalObjects: {{}}
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
  isReadable: 0
  streamingMipmaps: 0
  textureFormat: 4
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 2
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  compressionQuality: 100
  alphaSource: 1
  alphaIsTransparency: 1
  textureType: 0
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def disc_field():
    """The parchment inside the frame: light at the centre, darker towards the edge, so a figure stands out."""
    field = Image.new("RGB", (MASTER, MASTER))
    pixels = field.load()
    centre = MASTER / 2
    for y in range(MASTER):
        for x in range(MASTER):
            distance = min(1.0, ((x - centre) ** 2 + (y - centre) ** 2) ** 0.5 / centre)
            mix = distance ** 1.8
            pixels[x, y] = (int(226 - 72 * mix), int(208 - 80 * mix), int(168 - 86 * mix))
    return field.convert("RGBA")


def circle(radius):
    mask = Image.new("L", (MASTER, MASTER), 0)
    edge = MASTER / 2 - radius
    ImageDraw.Draw(mask).ellipse((edge, edge, MASTER - edge - 1, MASTER - edge - 1), fill=255)
    return mask


def framed(content):
    """The disc: dark rim, gilt band, dark line, then the content clipped to what is left."""
    outer = MASTER / 2 - 2
    band = outer - 22
    line = band - 40
    out = Image.new("RGBA", (MASTER, MASTER), (0, 0, 0, 0))
    out.paste(Image.new("RGBA", (MASTER, MASTER), (34, 25, 12, 255)), mask=circle(outer))
    out.paste(Image.new("RGBA", (MASTER, MASTER), (205, 164, 78, 255)), mask=circle(band))
    out.paste(Image.new("RGBA", (MASTER, MASTER), (48, 34, 16, 255)), mask=circle(line))
    out.paste(content, mask=circle(line - 10))
    return out


def regular(identifier):
    centre_x, centre_y, side = REGULARS[identifier]
    source = FIGURES / f"{identifier}-regular.png"
    picture = Image.open(source).convert("RGBA")
    half = side / 2
    crop = picture.crop((centre_x - half, centre_y - half, centre_x + half, centre_y + half))
    content = disc_field()
    content.alpha_composite(crop.resize((MASTER, MASTER), Image.LANCZOS))
    return framed(content), source


def load_renderer():
    spec = importlib.util.spec_from_file_location("wotr_ui_render", UI / "render.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def emblem(identifier, renderer, workspace, module):
    """An SVG source at the master's scale, on the same disc, a little inside it. The Eye and the Ring are glyphs on nothing,
    so a dark field behind them keeps them bright, as they stand on the game's dice and counters."""
    source = UI / "src" / EMBLEMS[identifier]
    svg = source.read_text(encoding="utf-8")
    width, height = module.logical_size(svg)
    css = module.BASE_CSS + (UI / "palette.css").read_text(encoding="utf-8")
    defs = (UI / "src/shared/defs.svg").read_text(encoding="utf-8")
    page = renderer.page(identifier, css, defs + svg)
    shot = Path(workspace) / f"{identifier}.png"
    scale = 24
    renderer.screenshot(page, shot, (width, height), scale)
    glyph = Image.open(shot).convert("RGBA")
    box = glyph.getchannel("A").getbbox()
    glyph = glyph.crop(box)
    target = int(MASTER * 0.7)
    ratio = target / max(glyph.size)
    glyph = glyph.resize((int(glyph.width * ratio), int(glyph.height * ratio)), Image.LANCZOS)
    content = Image.new("RGBA", (MASTER, MASTER), (46, 34, 20, 255))
    content.alpha_composite(glyph, ((MASTER - glyph.width) // 2, (MASTER - glyph.height) // 2))
    return framed(content), source


def build_all():
    module = load_renderer()
    pictures = {}
    sources = {}
    with tempfile.TemporaryDirectory() as workspace:
        renderer = module.Renderer(workspace)
        for identifier in IDENTIFIERS:
            if identifier in EMBLEMS:
                pictures[identifier], sources[identifier] = emblem(identifier, renderer, workspace, module)
            else:
                pictures[identifier], sources[identifier] = regular(identifier)
    return pictures, sources


def sized(picture, side):
    return picture.resize((side, side), Image.LANCZOS)


def preview(pictures, path):
    sizes = [16, 20, 32, 48, 128]
    sheet = Image.new("RGBA", (len(IDENTIFIERS) * 140 + 20, sum(sizes) + 20 * len(sizes) + 20), (24, 20, 14, 255))
    y = 20
    for side in sizes:
        for index, identifier in enumerate(IDENTIFIERS):
            sheet.alpha_composite(sized(pictures[identifier], side), (20 + index * 140, y))
        y += side + 20
    sheet.convert("RGB").save(path)


def stable_guid(path):
    return hashlib.md5(("wotr-ui-art:" + str(path.relative_to(ROOT))).encode()).hexdigest()


def write_png(picture, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    picture.save(path, optimize=True)


def write_outputs(pictures, sources, site):
    outputs = {}
    for identifier in IDENTIFIERS:
        target = CLIENT / f"{identifier}.png"
        write_png(sized(pictures[identifier], 128), target)
        meta = target.with_name(target.name + ".meta")
        if not meta.exists():
            meta.write_text(TEXTURE_META.format(guid=stable_guid(target)), encoding="utf-8")
        entry = {"source": str(sources[identifier].relative_to(ROOT)), "sourceSha256": digest(sources[identifier]),
                 "client": digest(target)}
        if site is not None:
            small, large = site / f"{identifier}.png", site / f"{identifier}@2x.png"
            write_png(sized(pictures[identifier], 128), small)
            write_png(sized(pictures[identifier], 256), large)
            entry["site"] = digest(small)
            entry["site2x"] = digest(large)
        outputs[identifier] = entry
    folder_meta = CLIENT.with_name(CLIENT.name + ".meta")
    if not folder_meta.exists():
        guid = hashlib.md5(b"wotr-ui-art:avatars-folder").hexdigest()
        folder_meta.write_text(f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n"
                               "  userData: \n  assetBundleName: \n  assetBundleVariant: \n", encoding="utf-8")
    MANIFEST.write_text(json.dumps({"avatars": outputs}, indent=2) + "\n", encoding="utf-8")


def check(site):
    """Fails if an avatar is missing, or stale against its source or the manifest."""
    if not MANIFEST.exists():
        raise SystemExit("manifest.json is missing: run build.py")
    avatars = json.loads(MANIFEST.read_text(encoding="utf-8"))["avatars"]
    problems = []
    if sorted(avatars) != sorted(IDENTIFIERS):
        problems.append("The manifest does not list the ten identifiers.")
    for identifier, entry in avatars.items():
        source = ROOT / entry["source"]
        if not source.exists() or digest(source) != entry["sourceSha256"]:
            problems.append(f"{identifier}: its source changed: {entry['source']}")
        target = CLIENT / f"{identifier}.png"
        if not target.exists() or digest(target) != entry["client"]:
            problems.append(f"{identifier}: the client's picture is missing or stale.")
        if site is not None:
            for name, key in ((f"{identifier}.png", "site"), (f"{identifier}@2x.png", "site2x")):
                if not (site / name).exists() or digest(site / name) != entry.get(key):
                    problems.append(f"{identifier}: the site's {name} is missing or stale.")
    if problems:
        raise SystemExit("\n".join(problems))
    print(f"PASS: {len(avatars)} avatars match their sources" + (" in the client and on the site." if site else " in the client."))


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--site", type=Path, help="The website's assets/avatars folder, in a server checkout.")
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--preview", type=Path, help="Only draw a contact sheet here; write nothing else.")
    arguments = parser.parse_args()
    if arguments.check:
        check(arguments.site)
        return
    pictures, sources = build_all()
    if arguments.preview:
        preview(pictures, arguments.preview)
        print("contact sheet", arguments.preview)
        return
    write_outputs(pictures, sources, arguments.site)
    print(f"wrote {len(pictures)} avatars")


if __name__ == "__main__":
    sys.exit(main())
