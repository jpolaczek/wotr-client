"""Cut the battles-v1 paintings into the battle window's scene strip (UI-206).

Usage: python3 build.py [--check]
Reads v1/raw/<key>.jpg (1536x1024) and writes Resources/Wotr/Art/battle-illustrations/<key>.jpg: the 1536x320 band
around the armies (row 560 unless prompts.json names another centre in "crop"), JPEG quality 88. Writes the Unity
.meta files from the UI kit template (no mipmaps, clamp) and v1/manifest.json with the SHA-256 of sources and outputs.
--check only verifies that the outputs match the manifest.
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
OUTPUT = ROOT / "unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/battle-illustrations"
MANIFEST = SOURCE / "manifest.json"
WIDTH, HEIGHT, CENTRE, QUALITY = 1536, 320, 560, 88


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def band(image, centre):
    top = min(max(0, centre - HEIGHT // 2), image.height - HEIGHT)
    return image.crop((0, top, WIDTH, top + HEIGHT)), top


def build(spec):
    OUTPUT.mkdir(parents=True, exist_ok=True)
    folder_meta = OUTPUT.with_name(OUTPUT.name + ".meta")
    if not folder_meta.exists():
        folder_meta.write_text(ui.FOLDER_META.format(guid=ui.stable_guid(OUTPUT)), encoding="utf-8")
    entries = []
    for key in spec["items"]:
        source = SOURCE / "raw" / (key + ".jpg")
        image = Image.open(source).convert("RGB")
        if image.size != (WIDTH, 1024):
            sys.exit(f"{source.name}: expected {WIDTH}x1024, got {image.size}")
        strip, top = band(image, spec.get("crop", {}).get(key, CENTRE))
        output = OUTPUT / (key + ".jpg")
        strip.save(output, quality=QUALITY, optimize=True)
        meta = output.with_name(output.name + ".meta")
        if not meta.exists():
            meta.write_text(ui.TEXTURE_META.format(guid=ui.stable_guid(output), wrap_u=1), encoding="utf-8")
        entries.append({"key": key, "source": str(source.relative_to(HERE)), "sourceSha256": sha256(source),
                        "output": str(output.relative_to(ROOT)), "outputSha256": sha256(output),
                        "band": [0, top, WIDTH, top + HEIGHT]})
    manifest = {"version": spec["version"], "task": "UI-206, UI-226", "model": spec["model"],
                "generator": "UI-206 set: OpenAI Images API via v1/generate.py (v1/generations.jsonl); UI-226 nation set: "
                             "Codex built-in image_gen (v1/generations-codex.jsonl); band cut with Pillow by build.py",
                "pixels_modified": "cropped and re-encoded only", "outputs": entries}
    MANIFEST.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"built {len(entries)} illustrations")


def check():
    manifest = json.loads(MANIFEST.read_text())
    stale = [entry["key"] for entry in manifest["outputs"]
             if sha256(ROOT / entry["output"]) != entry["outputSha256"] or sha256(HERE / entry["source"]) != entry["sourceSha256"]]
    if stale:
        sys.exit("Stale illustrations: " + ", ".join(stale))
    print(f"{len(manifest['outputs'])} illustrations match their sources")


if __name__ == "__main__":
    spec = json.loads((SOURCE / "prompts.json").read_text())
    check() if "--check" in sys.argv else build(spec)
