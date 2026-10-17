"""Export the UI-102 map canvas: paper beyond the map to the north, east and south, the sea to the west,
and pencil water art for the sea and the inland lakes.

Inputs are the MAP-14 terrain raster, the aged paper of the MAP-10 composition and the water rings of the
render bands. Outputs are two textures and the runtime layout in Resources/MapCanvas, and a report of hashes.
The paper of the margins is a seamless tile of the original paper's interior; near the old map edge the
terrain's own burnt edge is replaced by that paper under the ink, so the two textures meet without a seam.
MAP-18 grades both textures as candlelit parchment and tints the terrain's forests, mountains and Mordor;
MAP-19 adds the parchment skin of the bare paper (scripts/map_canvas_grade.py). --preview writes small copies for review without touching the outputs.
Requires Pillow, NumPy and SciPy (scripts/requirements-map-canvas.txt).
"""
import argparse
import base64
import hashlib
import io
import json
from pathlib import Path
import re

import numpy as np
from PIL import Image, ImageDraw
import PIL
import scipy
from scipy import ndimage

import map_canvas_grade as grade

Image.MAX_IMAGE_PIXELS = None
ROOT = Path(__file__).resolve().parents[1]
TERRAIN = ROOT / 'graphicalassets/maps/terrain-map14/MapTerrain.png'
COMPOSITION = ROOT / 'graphicalassets/maps/parchment-v4/map-composition.svg'
BANDS = ROOT / 'unity/Wotr.Client/Assets/Wotr/Resources/map-border-bands.json'
GEOMETRY = ROOT / 'unity/Wotr.Client/Assets/Wotr/Resources/map-geometry.json'
GRADE = ROOT / 'scripts/map_canvas_grade.py'
OUTPUT = ROOT / 'unity/Wotr.Client/Assets/Wotr/Resources/MapCanvas'
LAYOUT = OUTPUT / 'map-canvas.json'
REPORT = ROOT / 'docs/sources/UI-102-MAP-CANVAS.json'
MAP_W, MAP_H = 1632.0, 1338.0
# Board units beyond the map; wide enough to bring every edge region clear of the HUD at the Fit zoom.
MARGIN = {'west': 300.0, 'north': 220.0, 'east': 200.0, 'south': 320.0}
CANVAS = (-MARGIN['west'], -MARGIN['north'], MAP_W + MARGIN['east'], MAP_H + MARGIN['south'])
SIZE = 4096
EDGE_BLEND = 230.0
SEA_FADE = (2.0, 45.0)
INK = np.array([38, 50, 56], np.float32)
SHALLOW = np.array([168, 184, 172], np.float32)
DEEP = np.array([120, 148, 152], np.float32)
WATER_OPACITY = 0.88
SEA_LINES = ((0.9, 0.7), (6.0, 0.42), (12.5, 0.28), (20.0, 0.16))
LAKE_LINES = ((0.9, 0.7), (4.0, 0.42), (8.0, 0.26))


class Grid:
    """A raster over a rectangle of board units; pixels may be anisotropic like the MAP-14 terrain."""

    def __init__(self, bounds, width, height):
        self.x0, self.y0, self.x1, self.y1 = bounds
        self.w, self.h = width, height
        self.sx = width / (self.x1 - self.x0)
        self.sy = height / (self.y1 - self.y0)

    def px(self, points):
        return [((x - self.x0) * self.sx, (y - self.y0) * self.sy) for x, y in points]

    def coords(self):
        xs = self.x0 + (np.arange(self.w, dtype=np.float32) + 0.5) / self.sx
        ys = self.y0 + (np.arange(self.h, dtype=np.float32) + 0.5) / self.sy
        return np.meshgrid(xs, ys)

    def coords_in(self, window):
        rows, cols = window
        xs = self.x0 + (np.arange(cols.start, cols.stop, dtype=np.float32) + 0.5) / self.sx
        ys = self.y0 + (np.arange(rows.start, rows.stop, dtype=np.float32) + 0.5) / self.sy
        return np.meshgrid(xs, ys)

    def sampling(self):
        return (1 / self.sy, 1 / self.sx)

    def padded(self, pad):
        px, py = int(round(pad * self.sx)), int(round(pad * self.sy))
        bounds = (self.x0 - px / self.sx, self.y0 - py / self.sy, self.x1 + px / self.sx, self.y1 + py / self.sy)
        return Grid(bounds, self.w + 2 * px, self.h + 2 * py), (slice(py, py + self.h), slice(px, px + self.w))


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def sha(data):
    return hashlib.sha256(data).hexdigest()


# ---- Inputs -------------------------------------------------------------------------------------------

def load_water():
    bands = json.loads(BANDS.read_text())
    rings = [[tuple(p) for p in ring['points']] for ring in bands['water']]
    land, lakes = rings[1], rings[2:]
    coast_top = min(x for x, y in land if y <= 20.5)
    coast_bottom = min(x for x, y in land if y >= MAP_H - 20.5)
    return land, lakes, coast_top, coast_bottom


def load_paper():
    svg = COMPOSITION.read_text()
    match = re.search(r'id="aged-paper"[^>]*?href="data:image/png;base64,([^"]+)"', svg)
    if match is None:
        raise ValueError('Missing aged paper in the MAP-10 composition')
    raw = np.asarray(Image.open(io.BytesIO(base64.b64decode(match.group(1)))).convert('RGB'), np.float32)
    units_per_px = MAP_W / raw.shape[1]
    blur = np.stack([ndimage.gaussian_filter(raw[..., c], 30 / units_per_px, mode='reflect') for c in range(3)], -1)
    h, w = raw.shape[:2]
    target = np.median(blur[h // 4: 3 * h // 4, w // 4: 3 * w // 4].reshape(-1, 3), axis=0)
    return raw, blur, target


# ---- Paper --------------------------------------------------------------------------------------------

def sample(image, u, v):
    """Bilinear sample of an HxWx3 image at fractional pixel coordinates."""
    h, w = image.shape[:2]
    u = np.clip(u, 0, w - 1.001)
    v = np.clip(v, 0, h - 1.001)
    x0 = np.floor(u).astype(np.int32)
    y0 = np.floor(v).astype(np.int32)
    fx = (u - x0)[..., None]
    fy = (v - y0)[..., None]
    top = image[y0, x0] * (1 - fx) + image[y0, x0 + 1] * fx
    bottom = image[y0 + 1, x0] * (1 - fx) + image[y0 + 1, x0 + 1] * fx
    return top * (1 - fy) + bottom * fy


def seamless_tile(flat, inset):
    """A tileable patch of the paper's interior: the crop blended with its half-shifted copy."""
    h, w = flat.shape[:2]
    crop = flat[int(h * inset): int(h * (1 - inset)), int(w * inset): int(w * (1 - inset))]
    ch, cw = crop.shape[:2]
    shifted = np.roll(np.roll(crop, ch // 2, 0), cw // 2, 1)
    weight = np.sqrt(np.outer(1 - np.abs(np.linspace(-1, 1, ch)), 1 - np.abs(np.linspace(-1, 1, cw))))[..., None]
    mean = crop.reshape(-1, 3).mean(0)
    blend = crop * weight + shifted * (1 - weight)
    # Averaging two samples lowers the grain; restore its contrast around the mean.
    return mean + (blend - mean) / np.sqrt(weight ** 2 + (1 - weight) ** 2)


def wrap_sample(tile, u, v):
    h, w = tile.shape[:2]
    padded = np.concatenate([tile, tile[:1]], 0)
    padded = np.concatenate([padded, padded[:, :1]], 1)
    return sample(padded, np.mod(u, w), np.mod(v, h))


class PaperField:
    """Seamless paper over the whole canvas in the original paper's tone and grain, without its burnt edge."""

    def __init__(self, raw, blur, target):
        self.units_per_px = MAP_W / raw.shape[1]
        self.tile = seamless_tile(raw * (target / blur), 0.16)
        self.target = target
        rng = np.random.default_rng(1020)
        stains = ndimage.gaussian_filter(rng.standard_normal((96, 96)), 6, mode='wrap')
        self.stains = (stains / np.abs(stains).max()).astype(np.float32)

    def __call__(self, x, y):
        u, v = x / self.units_per_px, y / self.units_per_px
        mix = (0.5 + 0.5 * np.sin(x / 233.0 + 1.3) * np.cos(y / 187.0 - 0.4))[..., None]
        blend = wrap_sample(self.tile, u, v) * mix + wrap_sample(self.tile, u + 517.3, v + 291.7) * (1 - mix)
        paper = self.target + (blend - self.target) / np.sqrt(mix ** 2 + (1 - mix) ** 2)
        stain = ndimage.map_coordinates(self.stains, [np.mod(y / 1100.0 * 96, 96), np.mod(x / 1100.0 * 96, 96)],
                                        order=1, mode='grid-wrap')[..., None]
        return paper * (1 + 0.045 * stain)


def vignette_profile(blur, target):
    """Per-channel darkening of the original paper as a function of distance from its edge (units)."""
    h, w = blur.shape[:2]
    upx = MAP_W / w
    depth = int(260 / upx)
    edges = [blur[h // 4: 3 * h // 4, :depth].mean(0), blur[h // 4: 3 * h // 4, ::-1][:, :depth].mean(0),
             blur[:depth, w // 4: 3 * w // 4].mean(1), blur[::-1][:depth, w // 4: 3 * w // 4].mean(1)]
    return np.arange(depth) * upx, np.clip(np.mean(edges, axis=0) / target, 0, 1)


def apply_vignette(rgb, grid, profile):
    """Darken toward the canvas edge with the original paper's own edge profile."""
    distances, ratio = profile
    x, y = grid.coords()
    dx = np.minimum(x - CANVAS[0], CANVAS[2] - x)
    dy = np.minimum(y - CANVAS[1], CANVAS[3] - y)
    result = rgb.copy()
    for c in range(3):
        result[..., c] *= np.interp(dx, distances, ratio[:, c], right=1.0) * np.interp(dy, distances, ratio[:, c], right=1.0)
    return result


def edge_blended_terrain(grid, terrain, raw, field):
    """Replace the paper under the ink near the map's edge with the canvas paper, keeping the ink."""
    x, y = grid.coords()
    h, w = raw.shape[:2]
    original = sample(raw, x / MAP_W * w - 0.5, y / MAP_H * h - 0.5) * 0.995
    ink = np.clip(terrain / np.maximum(original, 1), 0, 1.0)
    edge = np.minimum(np.minimum(x, MAP_W - x), np.minimum(y, MAP_H - y))
    weight = (1 - smoothstep(0, EDGE_BLEND, edge))[..., None]
    paper = original * (1 - weight) + field(x, y) * weight
    return np.where(weight > 0.001, paper * ink, terrain)


# ---- Water --------------------------------------------------------------------------------------------

def polygon_mask(grid, rings, supersample=2):
    image = Image.new('L', (grid.w * supersample, grid.h * supersample), 0)
    draw = ImageDraw.Draw(image)
    for ring in rings:
        draw.polygon([(x * supersample, y * supersample) for x, y in grid.px(ring)], fill=255)
    return np.asarray(image.resize((grid.w, grid.h), Image.BOX), np.float32) / 255


def paper_zone(grid, coast_top, coast_bottom):
    """Outside-land paper: the strips N/E/S of the map east of the coast and its continuation into the margins."""
    x, y = grid.coords()
    north = (y < 20) & (x > coast_top + (y - 20) * 0.35)
    south = (y > MAP_H - 20) & (x > coast_bottom - (y - (MAP_H - 20)) * 0.4)
    return north | south | (x > MAP_W - 20)


def water_fields(grid, water):
    land_ring, lakes, coast_top, coast_bottom = water
    land = polygon_mask(grid, [land_ring])
    lake = polygon_mask(grid, lakes)
    land_solid = np.clip(land - lake, 0, 1)
    paper = paper_zone(grid, coast_top, coast_bottom)
    labels, _ = ndimage.label((land < 0.5) & ~paper)
    sea_core = labels == labels[grid.h // 2, 2]
    sampling = grid.sampling()
    fade = smoothstep(*SEA_FADE, ndimage.distance_transform_edt(~paper, sampling=sampling))
    sea = (1 - land) * ndimage.binary_dilation(sea_core, iterations=2) * fade
    return {
        'sea': sea.astype(np.float32),
        'lake': lake,
        'to_land': ndimage.distance_transform_edt(land_solid < 0.5, sampling=sampling).astype(np.float32),
        'to_shore': ndimage.distance_transform_edt(lake >= 0.5, sampling=sampling).astype(np.float32),
    }


def line(distance, at, width):
    return np.exp(-0.5 * ((distance - at) / width) ** 2)


def wave_strokes(grid, fields, spacing=58.0):
    """Small pencil wave strokes on a jittered lattice in open water."""
    rng = np.random.default_rng(102)
    layer = Image.new('L', (grid.w * 2, grid.h * 2), 0)
    draw = ImageDraw.Draw(layer)
    open_water = ((fields['sea'] > 0.95) & (fields['to_land'] > 34)) | ((fields['lake'] > 0.95) & (fields['to_shore'] > 16))
    row = 0
    for y in np.arange(CANVAS[1], CANVAS[3], spacing * 0.6):
        offset = spacing / 2 if row % 2 else 0
        row += 1
        for x in np.arange(CANVAS[0] + offset, CANVAS[2], spacing):
            jx, jy, size = x + rng.uniform(-14, 14), y + rng.uniform(-8, 8), rng.uniform(5.5, 8.5)
            px, py = int((jx - grid.x0) * grid.sx), int((jy - grid.y0) * grid.sy)
            if 0 <= px < grid.w and 0 <= py < grid.h and open_water[py, px]:
                stroke(draw, grid, jx, jy, size)
    return np.asarray(layer.resize((grid.w, grid.h), Image.BOX), np.float32) / 255 * 0.42


def stroke(draw, grid, x, y, size):
    """Two shallow arcs side by side, like a hand-drawn wave."""
    width = max(1, int(round(0.7 * grid.sx * 2)))
    for offset in (-size * 0.55, size * 0.55):
        points = [((x + offset + (t - 0.5) * size - grid.x0) * grid.sx * 2,
                   (y - np.sin(t * np.pi) * size * 0.28 - grid.y0) * grid.sy * 2) for t in np.linspace(0, 1, 9)]
        draw.line(points, fill=255, width=width)


def paint_water(rgb, grid, fields):
    grain = rgb.mean(-1)
    grain = (grain / ndimage.gaussian_filter(grain, 6))[..., None] ** 0.8
    sea, lake = fields['sea'][..., None], fields['lake'][..., None]
    depth = smoothstep(0, 90, fields['to_land'])[..., None]
    lake_depth = smoothstep(0, 30, fields['to_shore'])[..., None]
    out = rgb * (1 - sea * WATER_OPACITY) + (SHALLOW * (1 - depth) + DEEP * depth) * grain * sea * WATER_OPACITY
    out = out * (1 - lake * WATER_OPACITY) + (SHALLOW * (1 - lake_depth) + DEEP * lake_depth) * grain * lake * WATER_OPACITY
    ink = np.zeros(rgb.shape[:2], np.float32)
    for at, alpha in SEA_LINES:
        ink = np.maximum(ink, alpha * line(fields['to_land'], at, 0.55 + at * 0.01) * fields['sea'])
    for at, alpha in LAKE_LINES:
        ink = np.maximum(ink, alpha * line(fields['to_shore'], at, 0.5) * fields['lake'])
    ink = np.maximum(ink, wave_strokes(grid, fields))[..., None]
    return out * (1 - ink) + INK * ink


# ---- Export -------------------------------------------------------------------------------------------

def render(size=SIZE):
    water = load_water()
    raw, blur, target = load_paper()
    field = PaperField(raw, blur, target)
    profile = vignette_profile(blur, target)
    parchment = grade.Parchment(CANVAS)
    margins = Grid(CANVAS, size, size)
    fields = water_fields(margins, water)
    rgb = parchment.marks(parchment.tone(field(*margins.coords())), margins)
    rgb = parchment.skin(rgb, margins, dry(fields))
    rgb = paint_water(apply_vignette(rgb, margins, profile), margins, fields)
    yield 'margins.jpg', parchment.candle(rgb, margins), 90
    grid = Grid((0, 0, MAP_W, MAP_H), size, size)
    # Distance fields look past the map edge, so ripples and the sea fade meet the margins texture.
    padded, crop = grid.padded(100)
    fields = {key: value[crop] for key, value in water_fields(padded, water).items()}
    terrain = parchment.tone(edge_blended_terrain(grid, load_terrain(size), raw, field))
    tint = grade.TerrainTint(terrain, (MAP_W, MAP_H), GEOMETRY, COMPOSITION)
    terrain = parchment.marks(tint(terrain, grid), grid)
    terrain = parchment.skin(terrain, grid, dry(fields) * tint.paper_weight(grid))
    terrain = apply_vignette(terrain, grid, profile)
    yield 'terrain.jpg', parchment.candle(paint_water(terrain, grid, fields), grid), 92


def dry(fields):
    """Bare paper as opposed to the sea and the lakes, which keep the MAP-18 water."""
    return np.clip(1 - fields['sea'] - fields['lake'], 0, 1)


def load_terrain(size):
    image = Image.open(TERRAIN).convert('RGB')
    if image.size != (SIZE, SIZE):
        raise ValueError('MAP-14 terrain size changed')
    if size != SIZE:
        image = image.resize((size, size), Image.LANCZOS)
    return np.asarray(image, np.float32)


def layout():
    return {'version': 'map-canvas-1', 'map': [MAP_W, MAP_H], 'margins': MARGIN,
            'margins_texture': 'MapCanvas/margins', 'terrain_texture': 'MapCanvas/terrain'}


def report():
    outputs = {name: sha((OUTPUT / name).read_bytes()) for name in ('margins.jpg', 'terrain.jpg', 'map-canvas.json')}
    return {
        'version': 'map-canvas-1', 'task': 'UI-102',
        'sources': {str(p.relative_to(ROOT)): sha(p.read_bytes())
                    for p in (TERRAIN, COMPOSITION, BANDS, GEOMETRY, Path(__file__), GRADE)},
        'tools': {'numpy': np.__version__, 'scipy': scipy.__version__, 'Pillow': PIL.__version__},
        'canvas': {'bounds': list(CANVAS), 'margins': MARGIN, 'textureSize': SIZE,
                   'edgeBlend': EDGE_BLEND, 'seaFade': list(SEA_FADE)},
        'water': {'ink': INK.tolist(), 'shallow': SHALLOW.tolist(), 'deep': DEEP.tolist(), 'opacity': WATER_OPACITY,
                  'seaLines': [list(v) for v in SEA_LINES], 'lakeLines': [list(v) for v in LAKE_LINES]},
        'grade': {'task': 'MAP-18', **grade.parameters()},
        'outputs': {str((OUTPUT / name).relative_to(ROOT)): value for name, value in outputs.items()},
        'newGeography': False,
    }


def save(rgb, path, quality):
    Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8)).save(path, quality=quality, optimize=True)


def write_or_check(path, text, check):
    data = text.encode()
    if check:
        if not path.exists() or path.read_bytes() != data:
            raise ValueError(f'Stale export: {path.relative_to(ROOT)}')
    else:
        path.write_bytes(data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='verify that the outputs match their recorded sources')
    parser.add_argument('--preview', type=Path, help='write 1024 px previews to this folder and stop')
    args = parser.parse_args()
    if args.preview:
        args.preview.mkdir(parents=True, exist_ok=True)
        for name, rgb, quality in render(1024):
            save(rgb, args.preview / name, quality)
        print('PASS: map canvas previews written to ' + str(args.preview))
        return
    if not args.check:
        OUTPUT.mkdir(parents=True, exist_ok=True)
        for name, rgb, quality in render():
            save(rgb, OUTPUT / name, quality)
    write_or_check(LAYOUT, json.dumps(layout(), indent=2) + '\n', args.check)
    write_or_check(REPORT, json.dumps(report(), indent=2, ensure_ascii=False) + '\n', args.check)
    print('PASS: map canvas ' + ('matches its recorded sources' if args.check else 'exported'))


if __name__ == '__main__':
    main()
