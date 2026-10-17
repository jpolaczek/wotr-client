#!/usr/bin/env python3
"""Export the shared application artwork to PNG, Windows ICO and macOS ICNS.

Run on macOS: python3 scripts/export_app_icons.py
The generated files are checked in; players and release packaging do not need
to run this conversion or install an image-processing dependency.
"""

from pathlib import Path
import shutil
import struct
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[1]
MASTER = ROOT / "graphicalassets/branding/wotr-icon-master.png"
OUTPUT = ROOT / "release/icons"
UNITY_ICON = ROOT / "unity/Wotr.Client/Assets/Wotr/Branding/WotrAppIcon.png"
WINDOWS_SIZES = (16, 24, 32, 48, 64, 128, 256)


def resize(destination, size):
    subprocess.run(
        ["sips", "-z", str(size), str(size), str(MASTER), "--out", str(destination)],
        check=True, stdout=subprocess.DEVNULL,
    )


def write_ico(path, images):
    offset = 6 + 16 * len(images)
    entries = []
    payloads = []
    for size, image in images:
        payload = image.read_bytes()
        dimension = 0 if size == 256 else size
        entries.append(struct.pack("<BBBBHHII", dimension, dimension, 0, 0,
                                   1, 32, len(payload), offset))
        payloads.append(payload)
        offset += len(payload)
    path.write_bytes(struct.pack("<HHH", 0, 1, len(images))
                     + b"".join(entries) + b"".join(payloads))


def main():
    if not MASTER.is_file():
        raise SystemExit("Missing application icon master: " + str(MASTER))
    if not shutil.which("sips") or not shutil.which("iconutil"):
        raise SystemExit("Regeneration requires the macOS sips and iconutil tools.")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    UNITY_ICON.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="wotr-app-icons-") as temporary:
        directory = Path(temporary)
        iconset = directory / "Wotr.iconset"
        iconset.mkdir()
        for size in (16, 32, 128, 256, 512):
            resize(iconset / f"icon_{size}x{size}.png", size)
            resize(iconset / f"icon_{size}x{size}@2x.png", size * 2)
        subprocess.run(["iconutil", "-c", "icns", str(iconset), "-o",
                        str(OUTPUT / "wotr.icns")], check=True)
        images = []
        for size in WINDOWS_SIZES:
            image = directory / f"windows-{size}.png"
            resize(image, size)
            images.append((size, image))
        write_ico(OUTPUT / "wotr.ico", images)
        shutil.copyfile(iconset / "icon_512x512@2x.png", OUTPUT / "wotr.png")
        shutil.copyfile(OUTPUT / "wotr.png", UNITY_ICON)
    print("Exported release/icons/wotr.{png,ico,icns} and the Unity default icon.")


if __name__ == "__main__":
    main()
