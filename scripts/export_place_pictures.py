"""Export the UI-436 place pictures: a small picture of every place a character or a sovereign can be.

A character's window shows where the figure stands as a picture of that place instead of a text button. Every region
gets a crop of the map canvas art around it (the MAP-14/MAP-18 terrain over the UI-102 margins, the painted landmarks
and the land borders), its own outline in gold and the land around it dimmed, so even a small region reads at a glance.
The places off the map are painted from the board figures (Art/board-figures-v2) on the bare paper of the margins: the
Fellowship's miniature for a Companion with the Fellowship, a ghosted figure for one not in play, a figure laid down for
one removed, and the Ruler figure laid down for a removed sovereign. No character stands on the Mordor track (only the
Fellowship does, with its Companions inside it), so the track has no picture.

The pictures are replaceable art: Resources/Wotr/Art/Places/<place>.jpg, 256x160 px (twice the window's 128x80 points),
named by the region id or the place key (PlacePresentation.SpecialPlaces in the client). The script is deterministic:
--check renders every picture again and fails unless the files and the report (docs/sources/UI-436-PLACE-PICTURES.json)
match. New pictures get a Unity .meta with a path-derived GUID; existing metas are kept.
Requires Pillow, NumPy and SciPy at the versions of scripts/requirements-map-canvas.txt.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path

import numpy as np
import PIL
import scipy
from PIL import Image, ImageDraw
from scipy import ndimage

Image.MAX_IMAGE_PIXELS = None
ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / 'unity/Wotr.Client/Assets/Wotr/Resources'
TERRAIN = RESOURCES / 'MapCanvas/terrain.jpg'
MARGINS = RESOURCES / 'MapCanvas/margins.jpg'
LANDMARKS = RESOURCES / 'MapCanvas/landmarks.json'
GEOMETRY = RESOURCES / 'map-geometry.json'
BOARD = RESOURCES / 'base-board.json'
FIGURES = RESOURCES / 'Wotr/Art/board-figures-v2.png'
FIGURE_CELLS = RESOURCES / 'Wotr/Art/board-figures-v2.json'
OUTPUT = RESOURCES / 'Wotr/Art/Places'
REPORT = ROOT / 'docs/sources/UI-436-PLACE-PICTURES.json'
VERSION = 'place-pictures-1'

MAP_W, MAP_H = 1632.0, 1338.0
# The UI-102 canvas (scripts/export_map_canvas.py): margins.jpg covers it, terrain.jpg covers the map inside it.
CANVAS = (-300.0, -220.0, MAP_W + 200.0, MAP_H + 320.0)
# The working canvas: the map and a band of margin round it, at SCALE pixels per board unit.
WORK = (-160.0, -160.0, MAP_W + 160.0, MAP_H + 160.0)
SCALE = 2.5
SIZE = (256, 160)
ASPECT = SIZE[0] / SIZE[1]
# A region's crop: its bounds with a margin, never narrower than MIN_WIDTH board units.
PADDING = 1.35
MIN_WIDTH = 120.0
SUPERSAMPLE = 4
QUALITY = 88

BORDER = (250, 244, 226)
OUTLINE = (238, 198, 112)
OUTLINE_UNDER = (44, 30, 18)
# The land round a region: dimmed and partly desaturated.
AROUND_DIM = 0.6
AROUND_GREY = 0.45
VIGNETTE = 0.18
# Bare paper of the north margin, above the map, for the places off the map.
PAPER = (680.0, -158.0, 920.0, -8.0)


# ---- Inputs -------------------------------------------------------------------------------------------

def load_json(path):
    return json.loads(path.read_text())


def canvas_pixels(rect):
    x0, y0, x1, y1 = rect
    return (round((x0 - WORK[0]) * SCALE), round((y0 - WORK[1]) * SCALE),
            round((x1 - WORK[0]) * SCALE), round((y1 - WORK[1]) * SCALE))


def working_canvas():
    """The margins, the terrain and the landmarks at SCALE px per board unit over WORK."""
    width = round((WORK[2] - WORK[0]) * SCALE)
    height = round((WORK[3] - WORK[1]) * SCALE)
    margins = Image.open(MARGINS).convert('RGB')
    sx = margins.width / (CANVAS[2] - CANVAS[0])
    sy = margins.height / (CANVAS[3] - CANVAS[1])
    box = ((WORK[0] - CANVAS[0]) * sx, (WORK[1] - CANVAS[1]) * sy, (WORK[2] - CANVAS[0]) * sx, (WORK[3] - CANVAS[1]) * sy)
    canvas = margins.resize((width, height), Image.LANCZOS, box=box)
    terrain = Image.open(TERRAIN).convert('RGB')
    left, top, right, bottom = canvas_pixels((0.0, 0.0, MAP_W, MAP_H))
    canvas.paste(terrain.resize((right - left, bottom - top), Image.LANCZOS), (left, top))
    canvas = canvas.convert('RGBA')
    for landmark in load_json(LANDMARKS)['landmarks']:
        paste_landmark(canvas, landmark)
    draw_borders(canvas)
    return canvas.convert('RGB')


def paste_landmark(canvas, landmark):
    art = Image.open(RESOURCES / (landmark['texture'] + '.png')).convert('RGBA')
    width = landmark['width']
    height = width * art.height / art.width
    cx, cy = landmark['centre']
    left, top, right, bottom = canvas_pixels((cx - width / 2, cy - height / 2, cx + width / 2, cy + height / 2))
    sized = art.resize((right - left, bottom - top), Image.LANCZOS)
    canvas.alpha_composite(sized, (left, top))


def draw_borders(canvas):
    """The land borders, pale as on the board; the coast keeps the terrain's own drawing."""
    overlay = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    for boundary in load_json(GEOMETRY)['boundaries']:
        if None in boundary['owners']:
            continue
        points = [((x - WORK[0]) * SCALE, (y - WORK[1]) * SCALE) for x, y in boundary['points']]
        draw.line(points, fill=BORDER + (200,), width=round(1.1 * SCALE), joint='curve')
    canvas.alpha_composite(overlay)


# ---- Regions ------------------------------------------------------------------------------------------

def outer_rings(region):
    return [ring['points'] for ring in region['rings'] if not ring['hole']]


def crop_window(region):
    """A rectangle of ASPECT round the region's bounds, kept inside the working canvas."""
    points = np.array([p for ring in outer_rings(region) for p in ring], np.float64)
    (x0, y0), (x1, y1) = points.min(0), points.max(0)
    width = max((x1 - x0) * PADDING, (y1 - y0) * PADDING * ASPECT, MIN_WIDTH)
    height = width / ASPECT
    cx = np.clip((x0 + x1) / 2, WORK[0] + width / 2, WORK[2] - width / 2)
    cy = np.clip((y0 + y1) / 2, WORK[1] + height / 2, WORK[3] - height / 2)
    return (float(cx - width / 2), float(cy - height / 2), float(cx + width / 2), float(cy + height / 2))


def to_picture(window, points, factor=1):
    x0, y0, x1, y1 = window
    sx = SIZE[0] * factor / (x1 - x0)
    sy = SIZE[1] * factor / (y1 - y0)
    return [((x - x0) * sx, (y - y0) * sy) for x, y in points]


def region_mask(region, window):
    """The region's area in the picture, antialiased, holes cut out."""
    big = (SIZE[0] * SUPERSAMPLE, SIZE[1] * SUPERSAMPLE)
    mask = Image.new('L', big, 0)
    draw = ImageDraw.Draw(mask)
    for ring in region['rings']:
        draw.polygon(to_picture(window, ring['points'], SUPERSAMPLE), fill=0 if ring['hole'] else 255)
    return np.asarray(mask.resize(SIZE, Image.LANCZOS), np.float32) / 255


def outline(region, window):
    big = (SIZE[0] * SUPERSAMPLE, SIZE[1] * SUPERSAMPLE)
    layer = Image.new('RGBA', big, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for ring in region['rings']:
        points = to_picture(window, ring['points'], SUPERSAMPLE)
        points.append(points[0])
        draw.line(points, fill=OUTLINE_UNDER + (170,), width=round(4.2 * SUPERSAMPLE), joint='curve')
    for ring in region['rings']:
        points = to_picture(window, ring['points'], SUPERSAMPLE)
        points.append(points[0])
        draw.line(points, fill=OUTLINE + (255,), width=round(2.2 * SUPERSAMPLE), joint='curve')
    return layer.resize(SIZE, Image.LANCZOS)


def region_picture(canvas, region):
    window = crop_window(region)
    crop = canvas.resize(SIZE, Image.LANCZOS, box=canvas_pixels(window))
    rgb = np.asarray(crop, np.float32)
    inside = ndimage.gaussian_filter(region_mask(region, window), 1.0)[..., None]
    grey = rgb.mean(axis=2, keepdims=True)
    around = (rgb * (1 - AROUND_GREY) + grey * AROUND_GREY) * AROUND_DIM
    picture = Image.fromarray(np.clip(vignette(rgb * inside + around * (1 - inside)), 0, 255).astype(np.uint8)).convert('RGBA')
    picture.alpha_composite(outline(region, window))
    own_landmark(picture, region['id'], window)
    return picture.convert('RGB')


def own_landmark(picture, region, window):
    """The region's own landmark (Minas Tirith's walls, Orthanc) stays bright where it overhangs the outline."""
    landmark = next((l for l in load_json(LANDMARKS)['landmarks'] if l['id'] == region), None)
    if landmark is None:
        return
    art = Image.open(RESOURCES / (landmark['texture'] + '.png')).convert('RGBA')
    scale = SIZE[0] / (window[2] - window[0])
    width = landmark['width'] * scale
    sized = art.resize((round(width), round(width * art.height / art.width)), Image.LANCZOS)
    (cx, cy), = to_picture(window, [landmark['centre']])
    picture.alpha_composite(sized, (round(cx - sized.width / 2), round(cy - sized.height / 2)))


def vignette(rgb):
    ys, xs = np.mgrid[0:SIZE[1], 0:SIZE[0]].astype(np.float32)
    dx = (xs + 0.5) / SIZE[0] * 2 - 1
    dy = (ys + 0.5) / SIZE[1] * 2 - 1
    falloff = np.clip(np.sqrt(dx * dx + dy * dy) / np.sqrt(2), 0, 1) ** 2
    return rgb * (1 - VIGNETTE * falloff)[..., None]


# ---- Places off the map -------------------------------------------------------------------------------

def figure(name):
    atlas = Image.open(FIGURES).convert('RGBA')
    cells = load_json(FIGURE_CELLS)
    cell = next(c for c in cells['cells'] if c['name'] == name)
    # Normalised from the bottom-left corner.
    left = cell['x'] * atlas.width
    right = (cell['x'] + cell['width']) * atlas.width
    top = atlas.height - (cell['y'] + cell['height']) * atlas.height
    bottom = atlas.height - cell['y'] * atlas.height
    return atlas.crop((round(left), round(top), round(right), round(bottom)))


def paper(canvas):
    return canvas.resize(SIZE, Image.LANCZOS, box=canvas_pixels(PAPER)).convert('RGBA')


def scaled(art, height):
    return art.resize((round(art.width * height / art.height), round(height)), Image.LANCZOS)


def ghosted(art, opacity, grey, dim):
    rgba = np.asarray(art, np.float32)
    rgb = rgba[..., :3]
    mean = rgb.mean(axis=2, keepdims=True)
    rgb = (rgb * (1 - grey) + mean * grey) * dim
    alpha = rgba[..., 3:] * opacity
    return Image.fromarray(np.clip(np.concatenate([rgb, alpha], 2), 0, 255).astype(np.uint8))


def contact_shadow(picture, centre, width, height, strength):
    layer = Image.new('L', SIZE, 0)
    x, y = centre
    ImageDraw.Draw(layer).ellipse((x - width / 2, y - height / 2, x + width / 2, y + height / 2), fill=round(255 * strength))
    alpha = ndimage.gaussian_filter(np.asarray(layer, np.float32), 3.0)
    shade = np.zeros((SIZE[1], SIZE[0], 4), np.float32)
    shade[..., :3] = (36, 24, 14)
    shade[..., 3] = alpha
    picture.alpha_composite(Image.fromarray(shade.astype(np.uint8)))


def empty_stand(picture, centre):
    """A dashed ring on the paper where the figure would stand: not on the board yet."""
    layer = Image.new('RGBA', (SIZE[0] * SUPERSAMPLE, SIZE[1] * SUPERSAMPLE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    x, y = centre[0] * SUPERSAMPLE, centre[1] * SUPERSAMPLE
    rx, ry = 34 * SUPERSAMPLE, 10 * SUPERSAMPLE
    for step in range(0, 360, 20):
        draw.arc((x - rx, y - ry, x + rx, y + ry), step, step + 11, fill=(70, 50, 30, 210), width=round(1.6 * SUPERSAMPLE))
    picture.alpha_composite(layer.resize(SIZE, Image.LANCZOS))


def finish(picture):
    rgb = np.asarray(picture.convert('RGB'), np.float32)
    return Image.fromarray(np.clip(vignette(rgb), 0, 255).astype(np.uint8))


def standing(canvas, name, ghost):
    picture = paper(canvas)
    art = scaled(figure(name), 118)
    feet = (SIZE[0] / 2, 138)
    if ghost:
        empty_stand(picture, feet)
        art = ghosted(art, 0.5, 0.75, 0.9)
    else:
        contact_shadow(picture, feet, 96, 22, 0.55)
    picture.alpha_composite(art, (round(feet[0] - art.width / 2), round(feet[1] - art.height)))
    return finish(picture)


def lying(canvas, name):
    picture = paper(canvas)
    art = ghosted(scaled(figure(name), 116), 1.0, 0.55, 0.62).rotate(90, resample=Image.BICUBIC, expand=True)
    centre = (SIZE[0] / 2, 98)
    contact_shadow(picture, (centre[0], centre[1] + art.height / 2 - 6), art.width * 0.9, 20, 0.6)
    picture.alpha_composite(art, (round(centre[0] - art.width / 2), round(centre[1] - art.height / 2)))
    return finish(picture)


# Every place off the map: key -> (how it is drawn, figure). The keys are PlacePresentation.SpecialPlaces in the client.
SPECIAL = {
    'fellowship': ('standing', 'fellowship'),
    'not-in-play-free-peoples': ('ghost', 'companion'),
    'not-in-play-shadow': ('ghost', 'minion'),
    'removed-free-peoples': ('lying', 'companion'),
    'removed-shadow': ('lying', 'minion'),
    'removed-sovereign': ('lying', 'ruler'),
}


def special_picture(canvas, key):
    kind, name = SPECIAL[key]
    if kind == 'lying':
        return lying(canvas, name)
    return standing(canvas, name, ghost=kind == 'ghost')


# ---- Export -------------------------------------------------------------------------------------------

def render():
    canvas = working_canvas()
    regions = {r['id']: r for r in load_json(GEOMETRY)['regions']}
    for region in load_json(BOARD)['regions']:
        yield region['id'], region_picture(canvas, regions[region['id']])
    for key in SPECIAL:
        yield key, special_picture(canvas, key)


def encode(picture):
    data = io.BytesIO()
    picture.save(data, format='JPEG', quality=QUALITY, optimize=True, subsampling=0)
    return data.getvalue()


def sha(data):
    return hashlib.sha256(data).hexdigest()


def guid(path):
    return hashlib.md5(('UI-436/' + str(path.relative_to(ROOT))).encode()).hexdigest()


FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""
# Small UI pictures: mipmaps for the 2:1 sampling at 100 %, sRGB, uncompressed, bilinear, clamped.
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
  textureFormat: -1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  compressionQuality: 100
  alphaSource: 0
  alphaIsTransparency: 0
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


def write_meta(path, template):
    meta = path.with_name(path.name + '.meta')
    if not meta.exists():
        meta.write_text(template.format(guid=guid(path)))


def report(outputs):
    sources = (TERRAIN, MARGINS, LANDMARKS, GEOMETRY, BOARD, FIGURES, FIGURE_CELLS, Path(__file__))
    landmark_art = sorted({RESOURCES / (l['texture'] + '.png') for l in load_json(LANDMARKS)['landmarks']})
    return {
        'version': VERSION, 'task': 'UI-436',
        'sources': {str(p.relative_to(ROOT)): sha(p.read_bytes()) for p in sources + tuple(landmark_art)},
        'tools': {'numpy': np.__version__, 'scipy': scipy.__version__, 'Pillow': PIL.__version__},
        'picture': {'size': list(SIZE), 'quality': QUALITY, 'scale': SCALE, 'padding': PADDING, 'minWidth': MIN_WIDTH},
        'regions': sum(1 for key in outputs if key not in SPECIAL),
        'places': list(SPECIAL),
        'outputs': {str((OUTPUT / (key + '.jpg')).relative_to(ROOT)): value for key, value in outputs.items()},
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--check', action='store_true', help='render again and verify the pictures and the report')
    args = parser.parse_args()
    outputs = {}
    stale = []
    if not args.check:
        OUTPUT.mkdir(parents=True, exist_ok=True)
        write_meta(OUTPUT, FOLDER_META)
    for key, picture in render():
        data = encode(picture)
        path = OUTPUT / (key + '.jpg')
        outputs[key] = sha(data)
        if args.check:
            if not path.exists() or path.read_bytes() != data:
                stale.append(str(path.relative_to(ROOT)))
            continue
        path.write_bytes(data)
        write_meta(path, TEXTURE_META)
    text = json.dumps(report(outputs), indent=2, ensure_ascii=False) + '\n'
    if args.check:
        if not REPORT.exists() or REPORT.read_text() != text:
            stale.append(str(REPORT.relative_to(ROOT)))
        extra = sorted(p.name for p in OUTPUT.glob('*.jpg') if p.stem not in outputs)
        stale.extend('unexpected ' + name for name in extra)
        if stale:
            raise SystemExit('FAIL: stale place pictures: ' + ', '.join(stale))
        print(f'PASS: {len(outputs)} place pictures match their sources')
        return
    REPORT.write_text(text)
    print(f'PASS: {len(outputs)} place pictures exported to {OUTPUT.relative_to(ROOT)}')


if __name__ == '__main__':
    main()
