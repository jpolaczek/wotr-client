#!/usr/bin/env python3
"""Render the UI art catalog (assets.json) to RGBA PNG textures with headless Chrome.

Every source is an SVG fragment with its logical size in points. It is rendered
together with palette.css, the variant's token overrides and src/shared/defs.svg,
at `scale` times its size, into the Unity Resources tree mirroring src/.
New textures also get a Unity .meta with a path-derived GUID; existing metas are kept.
Usage: render.py [--preview] [--only CATEGORY]
"""
import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
MAC_CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
# WOTR_CHROME picks the browser; otherwise Chrome on a Mac, then Chromium or Chrome on the PATH (Linux).
CHROME = (os.environ.get("WOTR_CHROME") or (MAC_CHROME if Path(MAC_CHROME).exists() else None) or
          shutil.which("chromium") or shutil.which("google-chrome") or MAC_CHROME)
FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
# UI textures: no mipmaps, no CPU copy, uncompressed RGBA32, bilinear; wrap only for repeating surfaces.
TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 13
  internalIDToNameTable: []
  externalObjects: {{}}
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
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
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: {wrap_u}
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
BASE_CSS = "html,body{margin:0;padding:0;background:transparent;overflow:hidden}body>svg:last-child{display:block}"


class Renderer:
    def __init__(self, workspace):
        self.workspace = Path(workspace)
        self.profile = self.workspace / "chrome"
        self.shots = 0

    def screenshot(self, page, output, size, scale):
        # A fresh profile per shot and a time limit: a stuck headless Chrome must not stall the whole kit.
        for attempt in range(2):
            self.shots += 1
            try:
                subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                                "--user-data-dir=" + str(self.profile / str(self.shots)),
                                "--default-background-color=00000000",
                                "--force-device-scale-factor=" + str(scale),
                                "--window-size={},{}".format(*size),
                                "--screenshot=" + str(output), page.as_uri()],
                               check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=60)
                return
            except subprocess.TimeoutExpired:
                if attempt == 1:
                    raise

    def page(self, name, css, body):
        page = self.workspace / (name + ".html")
        page.write_text("<!doctype html><html><head><meta charset=\"utf-8\"><style>" + css +
                        "</style></head><body>" + body + "</body></html>", encoding="utf-8")
        return page


def logical_size(svg):
    match = re.search(r'<svg[^>]*\bwidth="(\d+)"[^>]*\bheight="(\d+)"', svg)
    if not match:
        raise SystemExit("SVG root needs integer width and height")
    return int(match.group(1)), int(match.group(2))


def token_overrides(tokens):
    return ":root{" + "".join(f"{name}:{value};" for name, value in tokens.items()) + "}"


def png_header(path):
    """Return (width, height, has_alpha); Chrome writes fully opaque renders as RGB."""
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n" or data[24] != 8 or data[25] not in (2, 6):
        raise SystemExit(f"{path} is not an 8-bit RGB/RGBA PNG")
    return int.from_bytes(data[16:20], "big"), int.from_bytes(data[20:24], "big"), data[25] == 6


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stable_guid(path):
    return hashlib.md5(("wotr-ui-art:" + str(path.relative_to(ROOT))).encode()).hexdigest()


def ensure_meta(path, text):
    meta = path.with_name(path.name + ".meta")
    if not meta.exists():
        meta.write_text(text.format(guid=stable_guid(path), wrap_u=0), encoding="utf-8")


def ensure_unity_metas(catalog, output, repeat_x):
    art_root = ROOT / catalog["output"]
    folder = output.parent
    while folder != art_root.parent:
        ensure_meta(folder, FOLDER_META)
        folder = folder.parent
    meta = output.with_name(output.name + ".meta")
    if not meta.exists():
        meta.write_text(TEXTURE_META.format(guid=stable_guid(output), wrap_u=0 if repeat_x else 1), encoding="utf-8")


def output_path(catalog, source, variant):
    relative = Path(source).with_suffix("")
    name = relative.name + ("-" + variant if variant else "") + ".png"
    return ROOT / catalog["output"] / relative.parent / name


def selected(asset, only):
    if only is None:
        return True
    only = only.rstrip("/")
    return asset["source"].startswith(only + "/") or asset["source"] in (only, only + ".svg")


def render_assets(renderer, catalog, only=None):
    palette = (HERE / "palette.css").read_text(encoding="utf-8")
    shared = (HERE / "src/shared/defs.svg").read_text(encoding="utf-8")
    scale = catalog["scale"]
    entries = []
    for asset in catalog["assets"]:
        if not selected(asset, only):
            continue
        source = HERE / "src" / asset["source"]
        svg = source.read_text(encoding="utf-8")
        size = logical_size(svg)
        base = [None] if asset.get("base", True) else []
        for variant in base + asset.get("variants", []):
            css = BASE_CSS + palette + token_overrides(catalog["variants"][variant] if variant else {})
            output = output_path(catalog, asset["source"], variant)
            output.parent.mkdir(parents=True, exist_ok=True)
            renderer.screenshot(renderer.page(output.stem, css, shared + svg), output, size, scale)
            ensure_unity_metas(catalog, output, asset.get("repeat") == "x")
            *pixels, alpha = png_header(output)
            if tuple(pixels) != (size[0] * scale, size[1] * scale):
                raise SystemExit(f"{output.name}: rendered {pixels}, expected {size[0] * scale}x{size[1] * scale}")
            entries.append({"output": str(output.relative_to(ROOT)), "source": "src/" + asset["source"],
                            "variant": variant, "logical": list(size), "pixels": pixels, "alpha": alpha,
                            "source_sha256": sha256(source), "png_sha256": sha256(output)})
            print(f"{output.relative_to(ROOT / catalog['output'])}: {pixels[0]}x{pixels[1]}")
    return entries


def render_previews(renderer, catalog):
    for preview in catalog.get("previews", []):
        page = HERE / preview["page"]
        output = page.with_suffix(".png")
        renderer.screenshot(page, output, preview["size"], 1)
        print(f"{output.relative_to(HERE)}: preview")


# --only keeps the manifest entries of the other categories in catalog order. After a change to the palette
# or the shared definitions that other categories use, render the whole catalog instead.
def merged_entries(catalog, rendered, only):
    if only is None:
        return rendered
    previous = json.loads((HERE / "manifest.json").read_text(encoding="utf-8"))["assets"]
    result = []
    for asset in catalog["assets"]:
        source = "src/" + asset["source"]
        pool = rendered if selected(asset, only) else previous
        result.extend(entry for entry in pool if entry["source"] == source)
    return result


def write_manifest(entries):
    shared = {name: sha256(HERE / name) for name in ("palette.css", "src/shared/defs.svg", "assets.json")}
    manifest = {"task": "UI-86", "renderer": "headless Google Chrome", "shared_sha256": shared, "assets": entries}
    (HERE / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--preview", action="store_true", help="also render previews/*.html to PNG")
    parser.add_argument("--only", metavar="CATEGORY",
                        help="render only src/CATEGORY (e.g. armies) or one source (e.g. actions/political) "
                             "and keep the other manifest entries")
    arguments = parser.parse_args()
    catalog = json.loads((HERE / "assets.json").read_text(encoding="utf-8"))
    with tempfile.TemporaryDirectory() as workspace:
        renderer = Renderer(workspace)
        rendered = render_assets(renderer, catalog, arguments.only)
        write_manifest(merged_entries(catalog, rendered, arguments.only))
        if arguments.preview:
            render_previews(renderer, catalog)


if __name__ == "__main__":
    main()
