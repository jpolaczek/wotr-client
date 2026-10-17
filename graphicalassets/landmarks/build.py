"""Turn the landmarks-v1 drawings into the map's fortress art (UI-234).

Usage: python3 build.py [--check]
Reads v1/raw/<key>.png (1024x1024, transparent), crops it to its drawing, multiplies its light wash by the map's
parchment so it sits on the terrain like the drawn mountains, scales it to fit 256x256 and writes
Resources/Wotr/Art/map-landmarks/<key>.png with the UI kit's .meta template (no mipmaps, clamp), plus
v1/manifest.json with the SHA-256 of sources and outputs. --check only verifies that the outputs match the manifest.
The places on the map are in Resources/MapCanvas/landmarks.json.
"""
import hashlib
import json
import sys
from pathlib import Path

from PIL import Image, ImageChops

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
sys.path.insert(0, str(ROOT / "graphicalassets" / "ui"))
import render as ui  # noqa: E402  (the texture .meta template and stable GUIDs)

SOURCE = HERE / "v1"
OUTPUT = ROOT / "unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/map-landmarks"
MANIFEST = SOURCE / "manifest.json"
SIZE = 256
PARCHMENT = (238, 214, 168)


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def drawing(source):
    image = Image.open(source).convert("RGBA")
    image = image.crop(image.split()[-1].getbbox())
    rgb, alpha = image.convert("RGB"), image.split()[-1]
    tinted = ImageChops.multiply(rgb, Image.new("RGB", image.size, PARCHMENT))
    tinted.putalpha(alpha)
    tinted.thumbnail((SIZE, SIZE), Image.LANCZOS)
    return tinted


def build(spec):
    OUTPUT.mkdir(parents=True, exist_ok=True)
    folder_meta = OUTPUT.with_name(OUTPUT.name + ".meta")
    if not folder_meta.exists():
        folder_meta.write_text(ui.FOLDER_META.format(guid=ui.stable_guid(OUTPUT)), encoding="utf-8")
    entries = []
    for key in spec["items"]:
        source = SOURCE / "raw" / (key + ".png")
        output = OUTPUT / (key + ".png")
        image = drawing(source)
        image.save(output, optimize=True)
        meta = output.with_name(output.name + ".meta")
        if not meta.exists():
            meta.write_text(ui.TEXTURE_META.format(guid=ui.stable_guid(output), wrap_u=1), encoding="utf-8")
        entries.append({"key": key, "source": str(source.relative_to(HERE)), "sourceSha256": sha256(source),
                        "output": str(output.relative_to(ROOT)), "outputSha256": sha256(output), "size": list(image.size)})
    manifest = {"version": spec["version"], "task": "UI-234, UI-256", "model": spec["model"],
                "generator": "OpenAI Images API via v1/generate.py (v1/generations.jsonl); cropped, parchment-multiplied and scaled with Pillow by build.py",
                "pixels_modified": "cropped, colour-multiplied, scaled and re-encoded", "outputs": entries}
    MANIFEST.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"built {len(entries)} landmarks")


def check():
    manifest = json.loads(MANIFEST.read_text())
    stale = [entry["key"] for entry in manifest["outputs"]
             if sha256(ROOT / entry["output"]) != entry["outputSha256"] or sha256(HERE / entry["source"]) != entry["sourceSha256"]]
    if stale:
        sys.exit("Stale landmarks: " + ", ".join(stale))
    print(f"{len(manifest['outputs'])} landmarks match their sources")


if __name__ == "__main__":
    spec = json.loads((SOURCE / "prompts.json").read_text())
    check() if "--check" in sys.argv else build(spec)
