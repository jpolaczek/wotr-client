"""Check the edit extent and prepare six close-ups of the exported PNG."""

import base64
import json
from pathlib import Path
import subprocess
import tempfile
from xml.etree import ElementTree as ET

import numpy as np
from PIL import Image


FOLDER = Path(__file__).resolve().parent
TARGET = FOLDER.parent / "middle-earth-pencil-v2.png"
SVG = "{http://www.w3.org/2000/svg}"


def render(source, target):
    subprocess.run(["node", str(FOLDER / "render_svg.cjs"), str(source), str(target)], check=True, timeout=60)


def pixel_box(view_box):
    x, y, width, height = view_box
    return (
        round((x - 335) * 6144 / 1632), round((y - 40) * 5037 / 1338),
        round(width * 6144 / 1632), round(height * 5037 / 1338),
    )


def main():
    patches = json.loads((FOLDER / "patches.json").read_text())["patches"]
    patches[3]["view_box"] = [980, 300, 340, 340]
    with tempfile.TemporaryDirectory(prefix="wotr-map04-inspect-") as folder:
        temporary = Path(folder)
        baseline = temporary / "baseline.png"
        render(FOLDER.parent / "pencil-v1/map-composition.svg", baseline)
        with Image.open(baseline) as image:
            before = np.asarray(image.convert("L"), dtype=np.int16)
        with Image.open(TARGET) as image:
            after = np.asarray(image.convert("L"), dtype=np.int16)
        outside = np.ones(before.shape, dtype=bool)
        root = ET.Element(SVG + "svg", {"width": "1536", "height": "1024", "viewBox": "0 0 1536 1024"})
        for index, patch in enumerate(patches):
            x, y, width, height = pixel_box(patch["view_box"])
            outside[max(0, y - 4):y + height + 4, max(0, x - 4):x + width + 4] = False
            crop = temporary / f'{patch["id"]}.png'
            subprocess.run([
                "sips", "--cropToHeightWidth", str(height), str(width),
                "--cropOffset", str(y), str(x), str(TARGET), "--out", str(crop),
            ], check=True, capture_output=True)
            ET.SubElement(root, SVG + "image", {
                "x": str(index % 3 * 512), "y": str(index // 3 * 512),
                "width": "512", "height": "512", "preserveAspectRatio": "none",
                "{http://www.w3.org/1999/xlink}href": "data:image/png;base64," + base64.b64encode(crop.read_bytes()).decode(),
            })
        difference = np.abs(after - before)
        maximum = int(difference[outside].max())
        changed = int(np.count_nonzero((difference > 1) & outside))
        # Filter rounding may change an isolated antialiased pixel by 2/255.
        if maximum > 2 or changed > 4:
            raise ValueError(f"Changed {changed} pixels outside local windows; max difference {maximum}")
        atlas = temporary / "details.svg"
        atlas.write_bytes(ET.tostring(root))
        render(atlas, FOLDER / "map-details.png")
        report = {
            "comparison": "v1 and v2 rendered with the same resvg backend",
            "outside_local_windows_changed_pixels_above_1": changed,
            "outside_local_windows_changed_pixels_above_2": int(np.count_nonzero((difference > 2) & outside)),
            "outside_local_windows_maximum_gray_difference": maximum,
            "detail_order": [patch["id"] for patch in patches],
            "details_are_crops_of_final_png": True,
        }
        (FOLDER / "locality-verification.json").write_text(json.dumps(report, indent=2) + "\n")
        print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
