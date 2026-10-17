"""MAP-18: candlelit parchment and hand-tinted terrain for the UI-102 map canvas.

The paper grade works in board units, so the margins and the terrain textures get the same tone, grain,
fibers, foxing and candle light wherever they meet. The terrain tint follows the existing drawings: a dark
green wash over Mirkwood, a lighter one over the other forests, grey for the mountains and an ash wash over
Mordor. The forests are the ink of the forest art images of the MAP-10 composition inside their clip paths;
the Lindon coast trees, the mountains and Mordor come from the region polygons of map-geometry.json and the
density of the terrain's ink. No line is redrawn, moved or removed.
"""
import base64
import io
import json
import re

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

SEED = 1818
LUMA = np.array([0.299, 0.587, 0.114], np.float32)
# The composition's pencil-ink filter turns the art's Rec. 709 luminance into ink.
INK_LUMA = np.array([0.2126, 0.7152, 0.0722], np.float32)

# Paper: a deeper yellow-ochre, uneven tone, grain, fibers, foxing and a candle's warm falloff.
PAPER_GAIN = np.array([0.965, 0.885, 0.735], np.float32)
MOTTLE = {'cells': [600.0, 240.0, 90.0, 36.0], 'weights': [0.36, 0.3, 0.2, 0.14], 'depth': 0.2,
          'color': [0.78, 0.63, 0.45]}
GRAIN = {'cell': 0.9, 'depth': 0.05}
FIBERS = {'per_unit_area': 1 / 420, 'length': [2.5, 11.0], 'width': 0.35, 'dark': 0.09, 'light': 0.06,
          'color': [0.82, 0.70, 0.55]}
FOXING = {'color': [0.70, 0.47, 0.27], 'small': [420, 0.5, 2.4, 0.12, 0.34], 'medium': [44, 2.4, 7.0, 0.08, 0.2],
          'stains': [9, 24.0, 72.0, 0.05, 0.09]}
CANDLE = {'center': [780.0, 600.0], 'radius': [1300.0, 1120.0], 'falloff': [0.32, 1.3], 'edge': [0.62, 0.50, 0.37]}

# MAP-19: the parchment layer, only on bare paper (see Parchment.skin): a deeper gold, cloudy cream and amber
# patches, streaks of the skin, small dark specks and the paper's own grain brought forward.
DEEPEN = [1.0, 0.93, 0.78]
CLOUDS = {'cells': [420.0, 170.0, 70.0, 28.0], 'weights': [0.38, 0.3, 0.2, 0.12], 'warp': [230.0, 80.0],
          'contrast': 2.6, 'dark': 0.36, 'dark_color': [0.80, 0.63, 0.42], 'light': 0.32,
          'cream': [247.0, 229.0, 180.0]}
STREAKS = {'cells': [150.0, 16.0], 'angle': 0.35, 'depth': 0.1, 'color': [0.86, 0.74, 0.58]}
SPECKS = {'cell': 1.6, 'rate': 0.003, 'radius': [0.35, 0.8], 'depth': [0.25, 0.6], 'color': [0.52, 0.38, 0.26]}
SHARPEN = {'sigma': 3.0, 'amount': 0.9, 'limit': 14.0}
LIGHT_PAPER = [90.0, 160.0]
# How much of a wash or the mountains' reach shuts the parchment layer off (soft, so no pale rims remain).
COVERED = {'mountains': 0.9, 'fade': [0.55, 1.0]}

# Terrain: forest art, region groups, ink thresholds and the washes laid over them.
VIEW_ORIGIN = (335.0, 40.0)
DARK_FOREST_ART = ['mirkwood-art']
LIGHT_FOREST_ART = ['fangorn-art', 'lorien-art', 'forest-art']
COAST_FORESTS = ['harlindon', 'forlindon', 'grey-havens']
NOT_FOREST = ['north-ered-luin', 'ered-luin', 'south-ered-luin']
MORDOR = ['gorgoroth', 'nurn', 'barad-dur', 'minas-morgul', 'morannon']
INK_FLOOR = 0.12
FOREST_INK = {'sigma': 5.0, 'density': 0.30, 'opening': 7, 'coherence': 0.28, 'area': 300, 'reach': 4}
TREE_INK = {'sigma': 2.0, 'density': 0.12, 'closing': 6}
MOUNTAIN_INK = {'sigma': 4.0, 'density': 0.10}
WASH = {'mirkwood': [0.36, 0.52, 0.38], 'forest': [0.62, 0.81, 0.58], 'mordor': [0.56, 0.46, 0.43]}
WASH_OPACITY = {'mirkwood': 0.92, 'forest': 0.78, 'mordor': 0.92}
# A wash never darkens the paper below its floor colour, so region highlights (an attack's red at 28 %) stay
# legible. Shading darker than the floor moves part of the way up to it; the drawn lines (darker than 'lines')
# keep their value.
WASH_FLOOR = {'mirkwood': {'color': [76.0, 88.0, 66.0], 'lift': 0.6, 'lines': [35.0, 60.0]},
              'mordor': {'color': [102.0, 88.0, 74.0], 'lift': 0.6, 'lines': [35.0, 60.0]}}
MORDOR_REACH = {'region': 10.0, 'mountains': 45.0, 'closing': 22, 'feather': 6.0, 'fade': 40.0, 'rim': 0.4,
                'ash': 0.6}
GREY = {'paper': 0.34, 'ink': 0.88, 'shade': 0.94, 'tint': [0.97, 0.99, 1.03]}
EDGE_FADE = 60.0


def parameters():
    return {
        'seed': SEED, 'paperGain': PAPER_GAIN.tolist(), 'mottle': MOTTLE, 'grain': GRAIN, 'fibers': FIBERS,
        'foxing': FOXING, 'candle': CANDLE,
        'deepen': DEEPEN, 'clouds': CLOUDS, 'streaks': STREAKS, 'specks': SPECKS, 'sharpen': SHARPEN,
        'lightPaper': LIGHT_PAPER, 'covered': COVERED,
        'darkForestArt': DARK_FOREST_ART, 'lightForestArt': LIGHT_FOREST_ART, 'coastForests': COAST_FORESTS,
        'notForest': NOT_FOREST, 'mordor': MORDOR, 'inkFloor': INK_FLOOR, 'forestInk': FOREST_INK,
        'treeInk': TREE_INK, 'mountainInk': MOUNTAIN_INK, 'wash': WASH, 'washOpacity': WASH_OPACITY,
        'washFloor': WASH_FLOOR,
        'mordorReach': MORDOR_REACH, 'grey': GREY, 'edgeFade': EDGE_FADE,
    }


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def luminance(rgb):
    return rgb @ LUMA


def multiply(rgb, alpha, color):
    """Lay a transparent wash of a multiply colour over the image."""
    factor = 1 - alpha[..., None] * (1 - np.asarray(color, np.float32))
    return rgb * factor


# ---- Noise in board units -----------------------------------------------------------------------------

def lattice(i, j, seed):
    h = (i * 374761393 + j * 668265263 + seed * 2246822519) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF).astype(np.float32) / 0xFFFFFF


def value_noise(x, y, cell_x, cell_y, seed):
    """Smooth value noise in [0, 1] on a lattice of the given cell size; the same at any resolution."""
    u, v = x / cell_x, y / cell_y
    fu, fv = np.floor(u), np.floor(v)
    tx, ty = smoothstep(0, 1, u - fu), smoothstep(0, 1, v - fv)
    i, j = fu.astype(np.int64), fv.astype(np.int64)
    top = lattice(i, j, seed) * (1 - tx) + lattice(i + 1, j, seed) * tx
    bottom = lattice(i, j + 1, seed) * (1 - tx) + lattice(i + 1, j + 1, seed) * tx
    return top * (1 - ty) + bottom * ty


def octaves(x, y, cells, weights, seed):
    total = np.zeros(x.shape, np.float32)
    for index, (cell, weight) in enumerate(zip(cells, weights)):
        total += weight * value_noise(x, y, cell, cell, seed + index)
    return total / sum(weights)


# ---- Paper --------------------------------------------------------------------------------------------

class Parchment:
    """Tone, marks and candle light of the paper, defined over the whole canvas in board units."""

    def __init__(self, canvas):
        self.canvas = canvas
        rng = np.random.default_rng(SEED)
        self.spots = [spot for kind in ('small', 'medium', 'stains') for spot in self.scatter(rng, kind)]
        self.fibers = list(self.strands(np.random.default_rng(SEED + 50)))

    def scatter(self, rng, kind):
        count, r0, r1, a0, a1 = FOXING[kind]
        x0, y0, x1, y1 = self.canvas
        for _ in range(int(count)):
            yield (kind, rng.uniform(x0, x1), rng.uniform(y0, y1), rng.uniform(r0, r1), rng.uniform(a0, a1),
                   int(rng.integers(1 << 20)))

    def strands(self, rng):
        """Short, slightly bent paper fibers, half darker and half lighter than the paper."""
        x0, y0, x1, y1 = self.canvas
        count = int((x1 - x0) * (y1 - y0) * FIBERS['per_unit_area'])
        for index in range(count):
            cx, cy = rng.uniform(x0, x1), rng.uniform(y0, y1)
            angle, bend = rng.uniform(0, np.pi), rng.uniform(-0.5, 0.5)
            length = rng.uniform(*FIBERS['length'])
            t = np.linspace(-0.5, 0.5, 7)
            along = t * length
            across = bend * length * (t * t - 0.25)
            xs = cx + along * np.cos(angle) - across * np.sin(angle)
            ys = cy + along * np.sin(angle) + across * np.cos(angle)
            yield index % 2 == 0, list(zip(xs, ys))

    @staticmethod
    def tone(rgb):
        return rgb * PAPER_GAIN

    def marks(self, rgb, grid):
        x, y = grid.coords()
        rgb = multiply(rgb, self.mottle(x, y), MOTTLE['color'])
        rgb = rgb * self.grain(x, y)[..., None]
        dark, light = self.fiber_layers(grid)
        rgb = multiply(rgb, dark * FIBERS['dark'], FIBERS['color'])
        rgb = rgb * (1 + light * FIBERS['light'])[..., None]
        return multiply(rgb, self.foxing(grid), FOXING['color'])

    @staticmethod
    def mottle(x, y):
        # A sum of octaves gathers around 0.5; stretch it so blotches and clean paper both show.
        noise = 0.5 + 2.4 * (octaves(x, y, MOTTLE['cells'], MOTTLE['weights'], SEED) - 0.5)
        return MOTTLE['depth'] * smoothstep(0.2, 1.0, noise)

    @staticmethod
    def grain(x, y):
        """Fine grain of the paper as a luminance factor around 1."""
        grain = value_noise(x, y, GRAIN['cell'], GRAIN['cell'], SEED + 10)
        return 1 + GRAIN['depth'] * (grain - 0.5)

    def fiber_layers(self, grid):
        layers = [Image.new('L', (grid.w * 2, grid.h * 2), 0) for _ in range(2)]
        draws = [ImageDraw.Draw(layer) for layer in layers]
        width = max(1, int(round(FIBERS['width'] * (grid.sx + grid.sy))))
        for dark, points in self.fibers:
            pixels = [((x - grid.x0) * grid.sx * 2, (y - grid.y0) * grid.sy * 2) for x, y in points]
            draws[0 if dark else 1].line(pixels, fill=255, width=width)
        return [np.asarray(layer.resize((grid.w, grid.h), Image.BOX), np.float32) / 255 for layer in layers]

    def foxing(self, grid):
        alpha = np.zeros((grid.h, grid.w), np.float32)
        for kind, cx, cy, radius, strength, seed in self.spots:
            window = spot_window(grid, cx, cy, radius * 1.4)
            if window is None:
                continue
            x, y = grid.coords_in(window)
            distance = np.hypot(x - cx, y - cy) / radius
            distance *= 0.78 + 0.44 * value_noise(x, y, radius * 0.45, radius * 0.45, seed)
            profile = stain_profile(distance) if kind == 'stains' else spot_profile(distance)
            alpha[window] = 1 - (1 - alpha[window]) * (1 - strength * profile)
        return alpha

    def skin(self, rgb, grid, weight):
        """The parchment layer over bare paper; weight (0..1) keeps it off washes, mountains and water.

        Colour, amber patches, streaks and specks reach the drawing's shading too, so no pale rim remains around
        it; the cream patches and the sharpened grain stay on light paper only."""
        x, y = grid.coords()
        light_paper = weight * smoothstep(*LIGHT_PAPER, luminance(rgb))
        rgb = rgb * (1 + weight[..., None] * (np.asarray(DEEPEN, np.float32) - 1))
        dark, light = clouds(x, y)
        rgb = multiply(rgb, weight * dark * CLOUDS['dark'], CLOUDS['dark_color'])
        cream = np.asarray(CLOUDS['cream'], np.float32)
        rgb = rgb + (cream - rgb) * (light_paper * light * CLOUDS['light'])[..., None]
        rgb = multiply(rgb, weight * streaks(x, y) * STREAKS['depth'], STREAKS['color'])
        rgb = sharpen(rgb, grid, light_paper)
        return multiply(rgb, weight * specks(x, y), SPECKS['color'])

    def candle(self, rgb, grid):
        """A warm light from the board's middle falling off toward the canvas corners."""
        x, y = grid.coords()
        (cx, cy), (rx, ry) = CANDLE['center'], CANDLE['radius']
        reach = np.hypot((x - cx) / rx, (y - cy) / ry)
        alpha = smoothstep(*CANDLE['falloff'], reach) ** 1.2
        return multiply(rgb, alpha, CANDLE['edge'])


def clouds(x, y):
    """Warped, high-contrast cloud noise: (dark amber patches, light cream patches), each 0..1."""
    warp_cell, warp_reach = CLOUDS['warp']
    wx = x + warp_reach * (value_noise(x, y, warp_cell, warp_cell, SEED + 60) - 0.5) * 2
    wy = y + warp_reach * (value_noise(x, y, warp_cell, warp_cell, SEED + 61) - 0.5) * 2
    noise = 0.5 + CLOUDS['contrast'] * (octaves(wx, wy, CLOUDS['cells'], CLOUDS['weights'], SEED + 62) - 0.5)
    return smoothstep(0.5, 1.0, noise), 1 - smoothstep(0.0, 0.5, noise)


def streaks(x, y):
    """Long soft streaks running across the skin in one direction."""
    angle = STREAKS['angle']
    along = x * np.cos(angle) + y * np.sin(angle)
    across = y * np.cos(angle) - x * np.sin(angle)
    wobble = 12.0 * (value_noise(x, y, 60.0, 60.0, SEED + 70) - 0.5)
    noise = value_noise(along, across + wobble, *STREAKS['cells'], SEED + 71)
    return smoothstep(0.55, 0.95, noise)


def specks(x, y):
    """Small dark specks scattered on a jittered lattice."""
    cell = SPECKS['cell']
    i, j = np.floor(x / cell).astype(np.int64), np.floor(y / cell).astype(np.int64)
    chosen = lattice(i, j, SEED + 80) < SPECKS['rate']
    cx = (i + 0.25 + 0.5 * lattice(i, j, SEED + 81)) * cell
    cy = (j + 0.25 + 0.5 * lattice(i, j, SEED + 82)) * cell
    size = lattice(i, j, SEED + 83)
    radius = SPECKS['radius'][0] + (SPECKS['radius'][1] - SPECKS['radius'][0]) * size
    depth = SPECKS['depth'][0] + (SPECKS['depth'][1] - SPECKS['depth'][0]) * size
    core = 1 - smoothstep(0.5 * radius, radius, np.hypot(x - cx, y - cy))
    return np.where(chosen, depth * core, 0).astype(np.float32)


def sharpen(rgb, grid, weight):
    """Bring the paper's own fine texture forward: add back its detail above a few board units.

    The blur averages paper only (weighted by light paper), so dark ink, forests or Mordor next to the paper
    do not leave a bright halo; the added detail is also limited."""
    sigma = (SHARPEN['sigma'] * grid.sy, SHARPEN['sigma'] * grid.sx)
    mass = ndimage.gaussian_filter(weight, sigma) + 1e-4
    result = rgb.copy()
    for c in range(3):
        base = ndimage.gaussian_filter(rgb[..., c] * weight, sigma) / mass
        detail = np.clip(rgb[..., c] - base, -SHARPEN['limit'], SHARPEN['limit'])
        result[..., c] += detail * SHARPEN['amount'] * weight
    return result


def spot_window(grid, cx, cy, reach):
    left = int(np.floor((cx - reach - grid.x0) * grid.sx))
    right = int(np.ceil((cx + reach - grid.x0) * grid.sx))
    top = int(np.floor((cy - reach - grid.y0) * grid.sy))
    bottom = int(np.ceil((cy + reach - grid.y0) * grid.sy))
    left, top = max(left, 0), max(top, 0)
    right, bottom = min(right, grid.w), min(bottom, grid.h)
    if right <= left or bottom <= top:
        return None
    return slice(top, bottom), slice(left, right)


def spot_profile(distance):
    """A foxing spot: a darker core fading out at its ragged edge."""
    return (1 - smoothstep(0.45, 1.0, distance)) * (0.75 + 0.25 * (1 - smoothstep(0, 0.5, distance)))


def stain_profile(distance):
    """A dried water stain: a faint fill with a darker tide mark at its rim."""
    fill = 0.35 * (1 - smoothstep(0.82, 1.0, distance))
    rim = 0.65 * np.exp(-0.5 * ((distance - 0.93) / 0.045) ** 2)
    return np.clip(fill + rim, 0, 1)


# ---- Terrain tint -------------------------------------------------------------------------------------

class TerrainTint:
    """Forest, mountain and Mordor washes over the terrain.

    Masks are built at one pixel per board unit and sampled bilinearly onto the output grid."""

    def __init__(self, rgb, map_size, geometry_path, composition_path):
        self.map_w, self.map_h = (int(round(side)) for side in map_size)
        self.regions = json.loads(geometry_path.read_text())['regions']
        self.arts = forest_art(composition_path.read_text())
        work = np.asarray(Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8)).resize(
            (self.map_w, self.map_h), Image.BOX), np.float32)
        lum = luminance(work)
        self.paper = ndimage.gaussian_filter(ndimage.maximum_filter(lum, 15), 8)
        ink = np.clip((ink_amount(lum, self.paper) - INK_FLOOR) / (1 - INK_FLOOR), 0, 1)
        self.mirkwood = wash_alpha(self.trees(DARK_FOREST_ART), SEED + 30)
        light = self.trees(LIGHT_FOREST_ART) | self.coast_trees(lum, ink)
        self.forest = wash_alpha(light, SEED + 31)
        mountains = self.mountain_blobs(ink)
        self.mordor = self.mordor_reach(mountains)
        forests = np.maximum(self.mirkwood, self.forest)
        self.mountains = ndimage.gaussian_filter(mountains.astype(np.float32), 3.0) * (1 - forests) * self.edge_fade()

    def canvas(self):
        image = Image.new('L', (self.map_w * 2, self.map_h * 2), 0)
        return image, ImageDraw.Draw(image)

    def to_mask(self, image):
        return np.asarray(image.resize((self.map_w, self.map_h), Image.BOX), np.float32) / 255

    def region_mask(self, ids):
        image, draw = self.canvas()
        for region in self.regions:
            if region['id'] not in ids:
                continue
            for hole in (False, True):
                for ring in region['rings']:
                    if ring['hole'] == hole:
                        draw.polygon([(x * 2, y * 2) for x, y in ring['points']], fill=0 if hole else 255)
        return self.to_mask(image)

    def trees(self, art_ids):
        """Where the forest art's own trees stand inside its outline, closed into one footprint."""
        ink = np.zeros((self.map_h, self.map_w), np.float32)
        for art in art_ids:
            ink = np.maximum(ink, self.placed(self.arts[art]) * self.outline(self.arts[art]))
        trees = ndimage.gaussian_filter(ink, TREE_INK['sigma']) > TREE_INK['density']
        trees = ndimage.binary_closing(trees, structure=disk(TREE_INK['closing']))
        return ndimage.binary_opening(trees, structure=disk(2))

    def outline(self, art):
        image, draw = self.canvas()
        draw.polygon([(x * 2, y * 2) for x, y in art.outline], fill=255)
        return self.to_mask(image)

    def placed(self, art):
        """The art's ink at one pixel per board unit, where the composition's transform puts it."""
        scale_x, _, _, scale_y, left, top = art.matrix
        height, width = art.ink.shape
        size = (max(1, round(width * scale_x * 2)), max(1, round(height * scale_y * 2)))
        scaled = np.asarray(Image.fromarray(art.ink).resize(size, Image.BOX), np.float32)
        layer = np.zeros((self.map_h * 2, self.map_w * 2), np.float32)
        paste(layer, scaled, round((left - VIEW_ORIGIN[0]) * 2), round((top - VIEW_ORIGIN[1]) * 2))
        return layer.reshape(self.map_h, 2, self.map_w, 2).mean((1, 3))

    def coast_trees(self, lum, ink):
        """The Lindon coast trees are drawn in the base pencil layer: solid blobs of round crowns there."""
        density = ndimage.gaussian_filter(ink, FOREST_INK['sigma'])
        blobs = ndimage.binary_opening(density > FOREST_INK['density'], structure=disk(FOREST_INK['opening']))
        coast = (self.region_mask(COAST_FORESTS) > 0.5) & (self.region_mask(NOT_FOREST) < 0.5)
        crowns = blobs & coast & (coherence(lum) < FOREST_INK['coherence'])
        labels, count = ndimage.label(crowns)
        areas = ndimage.sum(crowns, labels, np.arange(1, count + 1))
        kept = np.isin(labels, np.arange(1, count + 1)[areas >= FOREST_INK['area']])
        # The opening trims the outermost crowns; give them back.
        return ndimage.binary_dilation(kept, structure=disk(FOREST_INK['reach'])) & coast

    def mountain_blobs(self, ink):
        density = ndimage.gaussian_filter(ink, MOUNTAIN_INK['sigma'])
        return ndimage.binary_opening(density > MOUNTAIN_INK['density'], structure=disk(2))

    def mordor_reach(self, mountains):
        """Mordor's regions and the mountain ring around them, darkest on the plains inside the ring."""
        outside = ndimage.distance_transform_edt(self.region_mask(MORDOR) < 0.5)
        ring = mountains & (outside < MORDOR_REACH['mountains'])
        labels, _ = ndimage.label(mountains)
        touching = np.unique(labels[ring])
        footprint = (outside < MORDOR_REACH['region']) | np.isin(labels, touching[touching > 0])
        footprint &= outside < MORDOR_REACH['mountains']
        closed = ndimage.binary_fill_holes(ndimage.binary_closing(footprint, structure=disk(MORDOR_REACH['closing'])))
        depth = smoothstep(0, MORDOR_REACH['fade'], ndimage.distance_transform_edt(closed))
        rim = MORDOR_REACH['rim']
        alpha = wash_alpha(closed, SEED + 32, MORDOR_REACH['feather']) * (rim + (1 - rim) * depth)
        return alpha * self.edge_fade()

    def edge_fade(self):
        y, x = np.mgrid[0:self.map_h, 0:self.map_w].astype(np.float32) + 0.5
        edge = np.minimum(np.minimum(x, self.map_w - x), np.minimum(y, self.map_h - y))
        ragged = edge * (0.8 + 0.4 * value_noise(x, y, 40.0, 40.0, SEED + 33))
        return smoothstep(0, EDGE_FADE, ragged)

    def paper_weight(self, grid):
        """Where the terrain is bare paper: away from the forest and Mordor washes and the mountains."""
        x, y = grid.coords()
        at = [y - 0.5, x - 0.5]
        washes = np.maximum(np.maximum(self.forest, self.mirkwood), self.mordor)
        covered = np.maximum(resample(washes, at), resample(self.mountains, at) * COVERED['mountains'])
        return 1 - smoothstep(*COVERED['fade'], covered)

    def __call__(self, rgb, grid):
        x, y = grid.coords()
        at = [y - 0.5, x - 0.5]
        lum = luminance(rgb)
        ink = ink_amount(lum, resample(self.paper, at))
        rgb = grey_mountains(rgb, lum, ink, resample(self.mountains, at))
        return self.washes(rgb, x, y, at)

    def washes(self, rgb, x, y, at):
        """Watercolour washes whose strength wanders a little, as if laid by hand."""
        wander = 0.85 + 0.3 * octaves(x, y, [90.0, 30.0], [0.6, 0.4], SEED + 40)
        for name, mask in (('forest', self.forest), ('mirkwood', self.mirkwood), ('mordor', self.mordor)):
            alpha = np.clip(resample(mask, at) * WASH_OPACITY[name] * wander, 0, 1)
            washed = ashen(rgb, alpha * MORDOR_REACH['ash']) if name == 'mordor' else rgb
            washed = multiply(washed, alpha, WASH[name])
            if name in WASH_FLOOR:
                washed = np.maximum(washed, floor(rgb, alpha, WASH_FLOOR[name]))
            rgb = washed
        return rgb


class ForestArt:
    def __init__(self, outline, matrix, ink):
        self.outline = outline
        self.matrix = matrix
        self.ink = ink


FOREST_GROUP = re.compile(r'<ns0:g clip-path="url\(#[\w-]+\)"[^>]*>\s*'
                          r'<ns0:g clip-path="url\(#([\w-]+)\)">\s*'
                          r'<ns0:g filter="url\(#[\w-]+\)">\s*'
                          r'<ns0:use ns1:href="#([\w-]+)" transform="matrix\(([^)]+)\)"')


def forest_art(svg):
    """Each forest's art image, its transform and the innermost clip path around it in the composition."""
    arts = {}
    for match in FOREST_GROUP.finditer(svg):
        clip, art, matrix = match.groups()
        if art in DARK_FOREST_ART + LIGHT_FOREST_ART:
            arts[art] = ForestArt(clip_outline(svg, clip), [float(v) for v in matrix.split()], art_ink(svg, art))
    missing = set(DARK_FOREST_ART + LIGHT_FOREST_ART) - arts.keys()
    if missing:
        raise ValueError('Missing forest art: ' + ', '.join(sorted(missing)))
    return arts


def clip_outline(svg, clip):
    path = re.search(r'<ns0:clipPath id="%s"[^>]*>\s*<ns0:path d="([^"]+)"' % clip, svg).group(1)
    points = re.findall(r'[ML]\s*(-?[\d.]+),(-?[\d.]+)', path)
    return [(float(x) - VIEW_ORIGIN[0], float(y) - VIEW_ORIGIN[1]) for x, y in points]


def art_ink(svg, art):
    data = re.search(r'<ns0:image id="%s"[^>]*?ns1:href="data:image/png;base64,([^"]+)"' % art, svg).group(1)
    rgba = np.asarray(Image.open(io.BytesIO(base64.b64decode(data))).convert('RGBA'), np.float32) / 255
    rgb = rgba[..., :3] * rgba[..., 3:] + (1 - rgba[..., 3:])
    return np.clip(1 - rgb @ INK_LUMA, 0, 1)


def paste(layer, image, left, top):
    """Copy an image into a layer at an offset, cropping whatever falls outside."""
    height, width = image.shape
    x0, y0 = max(left, 0), max(top, 0)
    x1, y1 = min(left + width, layer.shape[1]), min(top + height, layer.shape[0])
    if x1 > x0 and y1 > y0:
        layer[y0:y1, x0:x1] = image[y0 - top:y1 - top, x0 - left:x1 - left]


def ink_amount(lum, paper):
    return np.clip((paper - lum) / (0.55 * np.maximum(paper, 1)), 0, 1)


def coherence(lum):
    """Local orientation coherence: mountain hatching runs in one direction, tree crowns do not."""
    gy, gx = np.gradient(ndimage.gaussian_filter(lum, 1.3))
    jxx = ndimage.gaussian_filter(gx * gx, 6)
    jyy = ndimage.gaussian_filter(gy * gy, 6)
    jxy = ndimage.gaussian_filter(gx * gy, 6)
    spread = np.sqrt((jxx - jyy) ** 2 + 4 * jxy ** 2)
    return np.where(jxx + jyy > 1e-3, spread / (jxx + jyy + 1e-6), 0)


def disk(radius):
    y, x = np.ogrid[-radius:radius + 1, -radius:radius + 1]
    return x * x + y * y <= radius * radius


def wash_alpha(mask, seed, feather=2.5):
    """A filled, slightly ragged footprint whose colour pools a little at its edge, like watercolour."""
    filled = ndimage.binary_fill_holes(ndimage.binary_closing(mask, structure=disk(4)))
    soft = ndimage.gaussian_filter(filled.astype(np.float32), feather)
    y, x = np.mgrid[0:mask.shape[0], 0:mask.shape[1]].astype(np.float32)
    alpha = smoothstep(0.3, 0.7, soft + 0.3 * (value_noise(x, y, 7.0, 7.0, seed) - 0.5))
    pooled = np.clip(alpha - ndimage.gaussian_filter(alpha, 5.0), 0, 1)
    return np.clip(0.88 * alpha + 0.5 * pooled, 0, 1)


def resample(mask, at):
    return ndimage.map_coordinates(mask, at, order=1, mode='nearest').astype(np.float32)


def floor(rgb, alpha, spec):
    """The lowest colour a wash may leave: the floor itself, or darker shading lifted part of the way to it."""
    color = np.asarray(spec['color'], np.float32)
    kept = np.minimum(rgb, color)
    shading = smoothstep(*spec['lines'], luminance(kept))
    lift = spec['lift'] * smoothstep(0, 0.3, alpha) * shading
    return kept + (color - kept) * lift[..., None]


def ashen(rgb, amount):
    """Drain the colour toward ash before Mordor's wash darkens it."""
    grey = luminance(rgb)[..., None]
    return rgb + (grey - rgb) * amount[..., None]


def grey_mountains(rgb, lum, ink, reach):
    """Pull the mountains' paper a little and their ink strongly toward a cool neutral grey."""
    grey = lum[..., None] * GREY['shade'] * np.asarray(GREY['tint'], np.float32)
    amount = reach * (GREY['paper'] + (GREY['ink'] - GREY['paper']) * ink)
    return rgb + (grey - rgb) * amount[..., None]
