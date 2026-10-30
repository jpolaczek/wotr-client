"""UI-400, UI-406: key the Codex stands and pucks (raw/) off their magenta background, trim and size them at 2x their
display size into the UI kit (Resources/Wotr/Art/UI/armies, a king's puck into kings)."""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ART = str(Path(__file__).resolve().parent) + "/"
KIT = str(Path(__file__).resolve().parents[2] / "unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/UI") + "/"
MAGENTA = np.array([255.0, 0.0, 255.0])
# Display size in points; the PNG is twice that (graphicalassets/ui rule). The art is scaled to fit and stands on the
# canvas's bottom edge, centred, so every stand of a kind has one size and one feet line.
SIZES = {
    "stand-field-free": (112, 44), "stand-field-shadow": (112, 44),
    "stand-garrison-free": (112, 48), "stand-garrison-shadow": (112, 48),
    "stand-character-free": (40, 28), "stand-character-shadow": (40, 28),
    "token-puck-free": (40, 32), "token-puck-shadow": (40, 32),
    "token-puck-garrison-free": (40, 32), "token-puck-garrison-shadow": (40, 32),
    "puck-corrupted": (30, 24),
}
# The kit category each key goes to; armies unless named here.
FOLDERS = {"puck-corrupted": "kings"}


def alpha_of(rgb):
    """Distance from magenta, with green weighted (magenta has none), ramped between two thresholds."""
    distance = np.sqrt(((rgb - MAGENTA) ** 2 * np.array([1.0, 1.6, 1.0])).sum(axis=2))
    return np.clip((distance - 70.0) / 90.0, 0.0, 1.0)


def despill(rgb, alpha):
    """Remove the magenta mixed into edge pixels: colour = (observed - (1 - a) * magenta) / a."""
    a = np.maximum(alpha, 1e-3)[..., None]
    clean = (rgb - (1.0 - a) * MAGENTA) / a
    clean = np.clip(clean, 0, 255)
    # Any remaining magenta cast (red and blue above green) is pulled toward the pixel's grey.
    cast = np.minimum(clean[..., 0], clean[..., 2]) - clean[..., 1]
    edge = (alpha < 0.98)[..., None] & (cast > 20)[..., None]
    grey = clean.mean(axis=2, keepdims=True)
    return np.where(edge, grey * 0.6 + clean * 0.4, clean)


def key(name):
    image = Image.open(ART + "raw/" + name + ".png").convert("RGB")
    rgb = np.asarray(image).astype(np.float64)
    alpha = alpha_of(rgb)
    colour = despill(rgb, alpha)
    rgba = np.dstack([colour, alpha * 255.0]).round().astype(np.uint8)
    keyed = Image.fromarray(rgba, "RGBA")
    box = keyed.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    keyed = keyed.crop(box)
    width, height = (2 * side for side in SIZES[name])
    scale = min(width / keyed.width, height / keyed.height)
    fitted = keyed.resize((round(keyed.width * scale), round(keyed.height * scale)), Image.LANCZOS)
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    canvas.alpha_composite(fitted, ((width - fitted.width) // 2, height - fitted.height))
    canvas.save(KIT + FOLDERS.get(name, "armies") + "/" + name + ".png")
    return {"name": name, "size": [width, height], "art": [fitted.width, fitted.height]}


if __name__ == "__main__":
    names = sys.argv[1:] or list(SIZES)
    report = [key(name) for name in names]

    print(json.dumps(report))
