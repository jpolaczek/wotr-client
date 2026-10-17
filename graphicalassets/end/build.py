"""Cut the end-v1 paintings into the end-of-game screen's scene (UI-233).

Usage: python3 build.py [--check]
Reads v1/raw/<key>.jpg (1536x1024) and writes Resources/Wotr/Art/end-scenes/<key>.jpg: the 1536x768 band around the
subject (row 512 unless prompts.json names another centre in "crop"), scaled to 960x480 (twice the 480x240 points of
the scene), JPEG quality 88. Writes the Unity .meta files from the UI kit template (no mipmaps, clamp) and
v1/manifest.json with the SHA-256 of sources and outputs. --check only verifies that the outputs match the manifest.
"""
import hashlib
import json
import sys
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
sys.path.insert(0, str(ROOT / "graphicalassets" / "ui"))
import render as ui  # noqa: E402  (the texture .meta template and stable GUIDs)

SOURCE = HERE / "v1"
OUTPUT = ROOT / "unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/end-scenes"
MANIFEST = SOURCE / "manifest.json"
WIDTH, HEIGHT, BAND, CENTRE, QUALITY = 1536, 1024, 768, 512, 88
SIZE = (960, 480)


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def band(image, centre):
    top = min(max(0, centre - BAND // 2), HEIGHT - BAND)
    return image.crop((0, top, WIDTH, top + BAND)).resize(SIZE, Image.LANCZOS), top


def build(spec):
    OUTPUT.mkdir(parents=True, exist_ok=True)
    folder_meta = OUTPUT.with_name(OUTPUT.name + ".meta")
    if not folder_meta.exists():
        folder_meta.write_text(ui.FOLDER_META.format(guid=ui.stable_guid(OUTPUT)), encoding="utf-8")
    entries = []
    for key in spec["items"]:
        source = SOURCE / "raw" / (key + ".jpg")
        image = Image.open(source).convert("RGB")
        if image.size != (WIDTH, HEIGHT):
            sys.exit(f"{source.name}: expected {WIDTH}x{HEIGHT}, got {image.size}")
        scene, top = band(image, spec.get("crop", {}).get(key, CENTRE))
        output = OUTPUT / (key + ".jpg")
        scene.save(output, quality=QUALITY, optimize=True)
        meta = output.with_name(output.name + ".meta")
        if not meta.exists():
            meta.write_text(ui.TEXTURE_META.format(guid=ui.stable_guid(output), wrap_u=1), encoding="utf-8")
        entries.append({"key": key, "source": str(source.relative_to(HERE)), "sourceSha256": sha256(source),
                        "output": str(output.relative_to(ROOT)), "outputSha256": sha256(output),
                        "band": [0, top, WIDTH, top + BAND], "size": list(SIZE)})
    manifest = {"version": spec["version"], "task": "UI-233", "model": spec["model"],
                "generator": "OpenAI Images API via v1/generate.py (v1/generations.jsonl); band cut and scaled with Pillow by build.py",
                "pixels_modified": "cropped, scaled and re-encoded only", "outputs": entries}
    MANIFEST.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"built {len(entries)} scenes")


def check():
    manifest = json.loads(MANIFEST.read_text())
    stale = [entry["key"] for entry in manifest["outputs"]
             if sha256(ROOT / entry["output"]) != entry["outputSha256"] or sha256(HERE / entry["source"]) != entry["sourceSha256"]]
    if stale:
        sys.exit("Stale scenes: " + ", ".join(stale))
    print(f"{len(manifest['outputs'])} scenes match their sources")


if __name__ == "__main__":
    spec = json.loads((SOURCE / "prompts.json").read_text())
    check() if "--check" in sys.argv else build(spec)
