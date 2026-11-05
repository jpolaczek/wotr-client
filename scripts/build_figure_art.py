"""Build the figure and portrait atlases of ART-01/ART-02 from the generated raw images.

Raw images (graphicalassets/figures/v3/raw/<key>.png, transparent PNG) are cropped to their alpha, scaled to a
common cell height and packed in rows; portraits are cut to a soft circle. Each atlas is a PNG plus its named,
normalized bottom-left cells (the board-figures schema). A report records the sources, tools and outputs.
--check rebuilds in memory and fails when an output differs from the committed file.
Requires Pillow (scripts/requirements-map-canvas.txt).
"""
import argparse
import hashlib
import io
import json
from pathlib import Path

import PIL
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'graphicalassets/figures/v3'
ART = ROOT / 'unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art'
REPORT = ROOT / 'docs/sources/ART-FIGURES.json'
ALPHA = 8
PAD = 4
WIDTH = 2048
# Relative height of a figure in its cell, so a regular stands lower than its elite and a hobbit lower than a
# man wherever the cell is shown at one size; the figure stands on the cell's bottom edge.
STATURE = {'-regular': 0.78, '-leader': 0.88, 'merry': 0.62, 'pippin': 0.62, 'ring-bearers': 0.64, 'gollum': 0.55,
           'gimli': 0.74, 'gimli-dwarven-lord': 0.74, 'smeagol': 0.55}
# style -> (atlas name, cell height, widest cell)
ATLASES = {
    'unit': ('units-v3', 240, 400),
    'character': ('characters-v1', 240, 320),
    'portrait': ('battle-portraits-v2', 160, 160),
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def stature(key):
    return next((value for suffix, value in STATURE.items() if key == suffix or key.endswith(suffix)), 1.0)


def figure(path, key, height, widest):
    """The miniature cropped to its visible alpha, scaled to its stature in the cell and standing on its bottom."""
    image = Image.open(path).convert('RGBA')
    mask = image.getchannel('A').point(lambda a: 255 if a > ALPHA else 0)
    left, top, right, bottom = mask.getbbox()
    crop = image.crop((max(left - PAD, 0), max(top - PAD, 0), min(right + PAD, image.width), min(bottom + PAD, image.height)))
    tall = height * stature(key)
    scale = min(tall / crop.height, widest / crop.width)
    scaled = crop.resize((max(1, round(crop.width * scale)), max(1, round(crop.height * scale))), Image.LANCZOS)
    # The empty band above a shorter figure keeps its stature wherever the cell is drawn at one height.
    cell = Image.new('RGBA', (scaled.width, round(scaled.height / stature(key))), (0, 0, 0, 0))
    cell.paste(scaled, (0, cell.height - scaled.height))
    return cell


def portrait(path, size):
    """The central square cut to a circle with a soft one-pixel edge."""
    image = Image.open(path).convert('RGBA')
    side = min(image.size)
    image = image.crop(((image.width - side) // 2, (image.height - side) // 2,
                        (image.width + side) // 2, (image.height + side) // 2)).resize((size, size), Image.LANCZOS)
    mask = Image.new('L', (size * 4, size * 4), 0)
    ImageDraw.Draw(mask).ellipse((2, 2, size * 4 - 3, size * 4 - 3), fill=255)
    mask = mask.resize((size, size), Image.LANCZOS).filter(ImageFilter.GaussianBlur(0.4))
    image.putalpha(mask)
    return image


def pack(images):
    """Row packing in key order: returns the atlas and each image's pixel box (left, top, width, height)."""
    boxes, x, y, row = {}, 0, 0, 0
    for key, image in images.items():
        if x + image.width > WIDTH:
            x, y, row = 0, y + row + PAD, 0
        boxes[key] = (x, y, image.width, image.height)
        x += image.width + PAD
        row = max(row, image.height)
    height = 1 << max(0, (y + row - 1).bit_length())
    atlas = Image.new('RGBA', (WIDTH, height), (0, 0, 0, 0))
    for key, image in images.items():
        atlas.paste(image, boxes[key][:2])
    return atlas, boxes


def cells(atlas, boxes):
    return [{'name': key, 'x': left / atlas.width, 'y': (atlas.height - top - height) / atlas.height,
             'width': width / atlas.width, 'height': height / atlas.height}
            for key, (left, top, width, height) in boxes.items()]


def build():
    spec = json.loads((SOURCE / 'prompts.json').read_text())
    outputs, sources, pending = {}, {}, []
    for style, (name, height, widest) in ATLASES.items():
        keys = [key for key, (kind, _) in spec['items'].items() if kind == style]
        # ART-04: an item whose art is not generated yet (its expansion's ART task) waits outside the atlas; the
        # client shows the figure silhouette for it until then.
        pending += [key for key in keys if not (SOURCE / 'raw' / (key + '.png')).exists()]
        keys = [key for key in keys if key not in pending]
        if not keys:
            continue
        paths = {key: SOURCE / 'raw' / (key + '.png') for key in keys}
        # A portrait's cell takes its character's key ("strider-portrait" -> "strider"), as PortraitArtwork asks.
        images = {key.removesuffix('-portrait'): portrait(path, height) if style == 'portrait'
                  else figure(path, key, height, widest) for key, path in paths.items()}
        atlas, boxes = pack(images)
        png = io.BytesIO()
        atlas.save(png, format='PNG', optimize=True)
        definition = {'schema': 'board-figures-1', 'width': atlas.width, 'height': atlas.height,
                      'coordinates': 'normalized bottom-left; separate non-overlapping bounds',
                      'cells': cells(atlas, boxes)}
        outputs[name + '.png'] = png.getvalue()
        outputs[name + '.json'] = (json.dumps(definition, indent=2) + '\n').encode()
        sources.update({str(path.relative_to(ROOT)): sha(path.read_bytes()) for path in paths.values()})
    return outputs, sources, pending


def report(outputs, sources, pending):
    return {
        'version': 'art-figures-1', 'tasks': ['ART-01', 'ART-02', 'ART-04', 'ART-05', 'ART-06', 'ART-07'],
        'sources': {**sources, 'graphicalassets/figures/v3/prompts.json': sha((SOURCE / 'prompts.json').read_bytes()),
                    'scripts/build_figure_art.py': sha(Path(__file__).read_bytes())},
        'tools': {'Pillow': PIL.__version__},
        'atlases': {name: {'cellHeight': height, 'widestCell': widest} for _, (name, height, widest) in ATLASES.items()},
        'stature': STATURE,
        'outputs': {str((ART / name).relative_to(ROOT)): sha(data) for name, data in sorted(outputs.items())},
        'pending': sorted(pending),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='verify that the committed atlases match their sources')
    args = parser.parse_args()
    outputs, sources, pending = build()
    files = {ART / name: data for name, data in outputs.items()}
    files[REPORT] = (json.dumps(report(outputs, sources, pending), indent=2) + '\n').encode()
    for path, data in files.items():
        if args.check:
            if not path.exists() or path.read_bytes() != data:
                raise SystemExit('Stale: ' + str(path.relative_to(ROOT)))
        else:
            path.write_bytes(data)
    print('PASS: figure art ' + ('matches its sources' if args.check else 'built') + ': ' + ', '.join(sorted(outputs)))


if __name__ == '__main__':
    main()
