"""UI-439: key the Codex ability icons (raw/) off their magenta background, trim them and fit each, centred, into a
square at twice its display size in the UI kit (Resources/Wotr/Art/UI/abilities/<ability id>.png), with a Unity meta
that copies the kit's texture import settings under a path-derived GUID (as graphicalassets/ui/render.py).

Usage: python3 key.py [key ...]   (no keys: every raw/*.png)
"""
import hashlib
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
KIT = ROOT / "unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/UI"
OUT = KIT / "abilities"
# The kit's import settings (no mipmaps, uncompressed RGBA32, bilinear, clamp), copied from one of its icons.
TEMPLATE_META = KIT / "characters/ability.png.meta"
FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""
# Display size in points; the PNG is twice that (graphicalassets/ui rule). The art is scaled to fit inside the square
# less MARGIN pixels on each side, centred.
POINTS = 28
SIZE = 2 * POINTS
MARGIN = 1
# A pixel's magenta cast: min(red, blue) - green. The flat background has about 245; the icons are drawn without
# magenta, pink or purple (generate.py), so their own pixels have 0 or less. A pixel between is a mix of the two.
FOREGROUND_CAST = 12.0


def background(rgb):
    """The flat background colour, from the four corners."""
    corners = np.concatenate([rgb[:16, :16], rgb[:16, -16:], rgb[-16:, :16], rgb[-16:, -16:]]).reshape(-1, 3)
    return np.median(corners, axis=0)


def alpha_of(rgb, back):
    """How much of a pixel is icon: 1 - its magenta cast as a share of the background's (a linear mix of the two)."""
    cast = np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1]
    back_cast = min(back[0], back[2]) - back[1]
    return np.clip((back_cast - cast) / (back_cast - FOREGROUND_CAST), 0.0, 1.0)


def unmix(rgb, alpha, back):
    """Remove the background mixed into edge pixels: colour = (observed - (1 - a) * background) / a."""
    a = np.maximum(alpha, 1e-3)[..., None]
    return np.clip((rgb - (1.0 - a) * back) / a, 0, 255)


def keyed(name):
    rgb = np.asarray(Image.open(ART / "raw" / (name + ".png")).convert("RGB")).astype(np.float64)
    back = background(rgb)
    alpha = alpha_of(rgb, back)
    colour = unmix(rgb, alpha, back)
    image = Image.fromarray(np.dstack([colour, alpha * 255.0]).round().astype(np.uint8), "RGBA")
    box = image.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox()
    return image.crop(box)


def fitted(image):
    inner = SIZE - 2 * MARGIN
    scale = min(inner / image.width, inner / image.height)
    size = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
    # Halve in steps, then one Lanczos step: a cleaner small icon than one 14x reduction.
    while image.width > 4 * size[0] and image.height > 4 * size[1]:
        image = image.resize((image.width // 2, image.height // 2), Image.BOX)
    small = image.resize(size, Image.LANCZOS)
    small = small.filter(ImageFilter.UnsharpMask(radius=0.6, percent=60, threshold=0))
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    canvas.alpha_composite(small, ((SIZE - small.width) // 2, (SIZE - small.height) // 2))
    return canvas


def stable_guid(path):
    return hashlib.md5(("wotr-ui-art:" + str(path.relative_to(ROOT))).encode()).hexdigest()


def write_meta(path):
    meta = path.with_name(path.name + ".meta")
    text = re.sub(r"(?m)^guid: [0-9a-f]+$", "guid: " + stable_guid(path), TEMPLATE_META.read_text(encoding="utf-8"))
    meta.write_text(text, encoding="utf-8")


def ensure_folder():
    OUT.mkdir(exist_ok=True)
    meta = OUT.with_name(OUT.name + ".meta")
    if not meta.exists():
        meta.write_text(FOLDER_META.format(guid=stable_guid(OUT)), encoding="utf-8")


def key(name):
    image = keyed(name)
    path = OUT / (name + ".png")
    fitted(image).save(path, optimize=True)
    write_meta(path)
    return {"name": name, "trimmed": [image.width, image.height], "size": [SIZE, SIZE]}


if __name__ == "__main__":
    ensure_folder()
    names = sys.argv[1:] or sorted(path.stem for path in (ART / "raw").glob("*.png"))
    print(json.dumps([key(name) for name in names]))
