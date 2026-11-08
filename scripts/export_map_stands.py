"""Export the UI-422 stand places: where every region's figures stand on the board.

Each region gets fixed places in board units (the map geometry's space): the garrison at the gate of its fortress
drawing, three characters beside it, the field army on open ground near the settlement, six characters beside the
army, the Fellowship and the planned recruits. A place is the point under a figure's feet; its footprint is a base
ellipse on the ground and the figure rectangle that rises from it (TABLETOP_FIGURES_UX). Places are chosen on an
anisotropic distance transform of the region, so every base lies inside its contour, no two bases overlap, and no
figure rectangle covers a settlement gem, a fortress drawing or a region name. The names follow the places at run time
(MapRegionNamePoints with an obstacle predicate); the search is ported here so the export can prove that every name
keeps a clear spot. `capacity` is the largest footprint scale at which the region's whole group fits; below 1 the
region shows tokens at the worst-case zoom (1366x768 at 125 %).

UI-425: no map object hides a force's label or covers a settlement. The field force's and the garrison's plaque (at
the footprint scale) and token count (on the whole map of the worst case, where tokens are largest on the board) stay
clear of every other stand of their region drawn in front of them and of the neighbouring regions' forces; no token
or medallion lies on a gem; and every region gets a `sovereign` place for a Corrupted king's marker that lies on no
label and no gem at any zoom.

Inputs: map-geometry.json, MapCanvas/landmarks.json and the size of its drawings, the regions of base-board.json
(names and settlements) and scripts/map-stands-overrides.json: the settlements expansion modules give regions (their
gems are kept clear too) and hand pins, {"pins": {"<region>": {"field": [x, y], "characters[2]": [x, y]}}}, which
replace the search for those places. Output: Resources/MapCanvas/stand-slots.json with the hashes of its sources;
--check verifies that it is current and that every place passes the acceptance checks. --preview draws the places
over the contours for review. Requires Pillow, NumPy and SciPy (scripts/requirements-map-canvas.txt).
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / 'unity/Wotr.Client/Assets/Wotr/Resources'
GEOMETRY = RESOURCES / 'map-geometry.json'
LANDMARKS = RESOURCES / 'MapCanvas/landmarks.json'
BOARD = RESOURCES / 'base-board.json'
OVERRIDES = ROOT / 'scripts/map-stands-overrides.json'
OUTPUT = RESOURCES / 'MapCanvas/stand-slots.json'
VERSION = 'stand-slots-3'
MAP_W, MAP_H = 1632.0, 1338.0

# The worst case: 1366x768 at 125 % is a 1092.8x614.4 point map; figures show down to 1.2 x its fit zoom
# (MapMarkerVisibility.TokensBelow). Footprints at scale 1 are the readable minimum there.
WORST_VIEW = (1366 / 1.25, 768 / 1.25)
TOKENS_BELOW = 1.2
# Base depth as a share of its width: every base is seen at the same angle.
DEPTH = 0.4


class Footprint:
    """A base ellipse (width, depth) on the ground and the figures (width, height) that rise from its centre."""

    def __init__(self, base_width, figure_width, figure_height):
        self.base = (float(base_width), round(base_width * DEPTH, 3))
        self.figure = (float(figure_width), float(figure_height))

    def radius(self, scale):
        return self.base[0] * scale / 2

    def rect(self, x, y, scale):
        """From the top of the figures to the front of the base, as wide as the wider of the two."""
        half = max(self.base[0], self.figure[0]) * scale / 2
        return x - half, y - self.figure[1] * scale, x + half, y + self.base[1] * scale / 2

    def to_json(self):
        return {'base': list(self.base), 'figure': list(self.figure)}


ARMY = Footprint(40, 40, 40)
CHARACTER = Footprint(14, 16, 36)
FELLOWSHIP = Footprint(16, 18, 36)
FOOTPRINTS = {'army': ARMY, 'character': CHARACTER, 'fellowship': FELLOWSHIP}

# Settlement gems (SettlementGem): girdle, size in points and the gold setting of a Stronghold.
GIRDLES = {
    'Town': ([(math.cos(math.radians(22.5 + 45 * i)), math.sin(math.radians(22.5 + 45 * i))) for i in range(8)], 6.0),
    'City': ([(-1, -.42), (-.72, -.7), (.72, -.7), (1, -.42), (1, .42), (.72, .7), (-.72, .7), (-1, .42)], 7.5),
    'Stronghold': ([(-1, -.72), (-.72, -1), (.72, -1), (1, -.72), (1, .72), (.72, 1), (-.72, 1), (-1, .72)], 7.5 * 1.3),
    'Fortification': ([(-.14, -.98), (.14, -.98), (.95, .42), (.8, .66), (-.8, .66), (-.95, .42)], 8.0),
}
GEM_MARGIN = 2.0
# UI-425: on the whole map of the worst case a gem is drawn at the gem scale's smallest share of its size (.85,
# MapStandScale.Gem), larger on the board than at the worst-case Detail zoom, with its side band below (at most 1.6
# points); tokens keep this margin from it.
GEM_SMALLEST, SIDE_BAND, STONE_MARGIN = 0.85, 1.6, 1.0

# Region names (MapRegionNames, MapRegionNamePoints).
LETTER_HEIGHT, MAJOR_LETTER_HEIGHT = 11.0, 12.5
CAPITAL_WIDTH, READING_SIZE, LETTER_SPACING, LINE_HEIGHT = .72, 15.0, 1.6, 1.25
CLOSEST_ZOOM = 2.6
NAME_GRID, NAME_SIMPLIFICATION, NAME_REFINEMENT = 14, 1.0, 6
MINIMUM_CLEARANCE, CLEARANCE_CAP, BELOW_BONUS, BELOW_MARGIN, PENALTY = 5.0, 26.0, 6.0, 12.0, 1000.0
FLOAT_MIN = -3.4028234663852886e38

# The search. Pixels per board unit of the region rasters, and the lattice of candidate feet (board units).
RESOLUTION = 2
LATTICE = 1.0
# Board units a base keeps from its border, from another base (while laid out, and when its capacity is measured) and
# a figure rectangle from an obstacle.
MARGIN = 1.5
GAP = 1.5
CAPACITY_GAP = 0.5
CLEARANCE = 0.25
# The footprint scales a group is laid out at, largest first, and the largest capacity recorded.
SCALES = [round(1 - 0.05 * i, 2) for i in range(15)]
LARGEST_CAPACITY = 2.0
# Score: a pull towards each slot's target (target()), open ground up to a cap, figures on their own region, figures
# that do not hide each other (heavily first, lightly where that does not fit), and room left for a moved name.
ROOM_CAP, ROOM_WEIGHT = 8.0, 0.5
# UI-513 (owner): the Fellowship stands well inside its region, nearer its middle than its border, so where it is reads
# at a glance; its place weighs open ground far more, and further, than the other places do.
FELLOWSHIP_ROOM_CAP, FELLOWSHIP_ROOM_WEIGHT = 30.0, 1.0
OUTSIDE_WEIGHT, ROOM_FOR_NAME_WEIGHT = 40.0, 25.0
HIDDEN_WEIGHTS = (200.0, 30.0)
# In a siege the besieger in the field and the garrison never hide more of each other than this.
SIEGE = {'field', 'garrison'}
ARMIES_HIDDEN = 0.25
# The gate: this share of the drawing's height above its lower edge.
GATE_INSET = 0.2
# Points tried for the garrison and the field, next-best points per slot when backing up, and the steps allowed.
BEAM = {'garrison': 4, 'field': 10}
BRANCHES, BACKTRACKING = 3, 40
SUPPRESSION = 5.0
# Other spots a tight region's name may take, the grid they are found on and their spacing; rounds of shared names.
NAME_SPOTS, NAME_SPOT_STEP, NAME_SPOT_SPACING = 6, 4.0, 14.0
NAME_ROUNDS = 10


def sha(data):
    return hashlib.sha256(data).hexdigest()


def worst_case_zoom():
    return TOKENS_BELOW * max(WORST_VIEW[0] / MAP_W, WORST_VIEW[1] / MAP_H)


def overlaps(a, b):
    """Rect.Overlaps: open rectangles (x0, y0, x1, y1) that share an area."""
    return b[2] > a[0] and b[0] < a[2] and b[3] > a[1] and b[1] < a[3]


# ---- Inputs -------------------------------------------------------------------------------------------

def load_inputs():
    geometry = json.loads(GEOMETRY.read_text())
    if (geometry['width'], geometry['height']) != (MAP_W, MAP_H):
        raise ValueError('The map size changed; the worst-case zoom must be derived again')
    board = {r['id']: r for r in json.loads(BOARD.read_text())['regions']}
    overrides = json.loads(OVERRIDES.read_text())
    drawings = load_drawings()
    regions = [Region(shape, board[shape['id']], drawings.get(shape['id']), overrides) for shape in geometry['regions']]
    return regions, overrides


def load_drawings():
    """The rectangle of every fortress drawing, its height from the drawing's aspect (MapLandmarks.Rect)."""
    rects = {}
    for landmark in json.loads(LANDMARKS.read_text())['landmarks']:
        with Image.open(RESOURCES / (landmark['texture'] + '.png')) as image:
            width, height = image.size
        cx, cy = landmark['centre']
        w = landmark['width']
        h = w * height / width
        rects[landmark['id']] = (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)
    return rects


def regions_digest():
    """The regions' ids, names and settlements: the part of base-board.json the places depend on."""
    regions = sorted(json.loads(BOARD.read_text())['regions'], key=lambda r: r['id'])
    text = '\n'.join(r['id'] + '|' + r['name'] + '|' + (r['settlement'] or '') for r in regions)
    return sha(text.encode('utf-8'))


def source_hashes():
    return {
        'map-geometry.json': sha(GEOMETRY.read_bytes()),
        'MapCanvas/landmarks.json': sha(LANDMARKS.read_bytes()),
        'base-board.json#regions': regions_digest(),
        'scripts/map-stands-overrides.json': sha(OVERRIDES.read_bytes()),
        'scripts/export_map_stands.py': sha(Path(__file__).read_bytes()),
    }


# ---- Contour math (MapContourMath, MapRegionNamePoints) -------------------------------------------------

def segments(ring):
    """A closed ring as arrays of its segments' ends: (ax, ay, bx, by)."""
    points = np.array(ring, dtype=np.float64)
    return points[:-1, 0], points[:-1, 1], points[1:, 0], points[1:, 1]


def location(segs, x, y):
    """MapContourMath.Location: 1 inside, 0 on the ring, -1 outside."""
    ax, ay, bx, by = segs
    cross = (bx - ax) * (y - ay) - (by - ay) * (x - ax)
    on = (cross == 0) & (np.minimum(ax, bx) <= x) & (x <= np.maximum(ax, bx))
    if (on & (np.minimum(ay, by) <= y) & (y <= np.maximum(ay, by))).any():
        return 0
    straddles = (ay > y) != (by > y)
    with np.errstate(divide='ignore', invalid='ignore'):
        at = (bx - ax) * (y - ay) / (by - ay) + ax
    return 1 if np.count_nonzero(straddles & (x < at)) % 2 else -1


def segment_distances(px, py, segs):
    ax, ay, bx, by = segs
    dx, dy = bx - ax, by - ay
    length = dx * dx + dy * dy
    with np.errstate(divide='ignore', invalid='ignore'):
        t = np.where(length > 0, np.clip(((px - ax) * dx + (py - ay) * dy) / length, 0, 1), 0)
    return np.hypot(px - (ax + dx * t), py - (ay + dy * t))


def segment_distance(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    length = dx * dx + dy * dy
    t = min(1.0, max(0.0, ((px - ax) * dx + (py - ay) * dy) / length)) if length > 0 else 0.0
    return math.hypot(px - (ax + dx * t), py - (ay + dy * t))


def simplify(points, tolerance):
    """Douglas-Peucker as in MapRegionNamePoints.Simplify: a closed ring keeps its first and middle points."""
    if len(points) < 4:
        return points
    keep = [False] * len(points)
    middle = len(points) // 2
    keep[0] = keep[middle] = keep[-1] = True
    spans = [(0, middle), (middle, len(points) - 1)]
    while spans:
        first, last = spans.pop()
        farthest, distance = -1, tolerance
        for i in range(first + 1, last):
            d = segment_distance(*points[i], *points[first], *points[last])
            if d > distance:
                distance, farthest = d, i
        if farthest < 0:
            continue
        keep[farthest] = True
        spans.append((first, farthest))
        spans.append((farthest, last))
    return [p for p, k in zip(points, keep) if k]


def chord(borders, px, py, horizontal):
    """MapRegionNamePoints.Chord: the free distance along an axis, whichever side is shorter."""
    forward = backward = float('inf')
    for ax, ay, bx, by in borders:
        across, origin = (py, px) if horizontal else (px, py)
        a0, b0, a1, b1 = (ay, by, ax, bx) if horizontal else (ax, bx, ay, by)
        hit = (a0 > across) != (b0 > across)
        if not hit.any():
            continue
        t = (across - a0[hit]) / (b0[hit] - a0[hit])
        distance = a1[hit] + t * (b1[hit] - a1[hit]) - origin
        ahead = distance[distance >= 0]
        behind = distance[distance < 0]
        if ahead.size:
            forward = min(forward, float(ahead.min()))
        if behind.size:
            backward = min(backward, float(-behind.max()))
    return min(forward, backward)


def clearance(borders, px, py):
    return min(float(segment_distances(px, py, segs).min()) for segs in borders)


# ---- Regions ------------------------------------------------------------------------------------------

class Region:
    def __init__(self, shape, definition, drawing, overrides):
        self.id = shape['id']
        self.name = definition['name']
        self.settlement = definition['settlement']
        self.rings = [[tuple(p) for p in ring['points']] for ring in shape['rings']]
        self.holes = [ring['hole'] for ring in shape['rings']]
        self.anchor = tuple(shape['anchor'])
        self.stronghold = self.settlement == 'Stronghold'
        self.drawing = drawing
        self.gem = gem_rect(self.anchor, self.gem_kinds(overrides))
        self.stone = stone_rect(self.anchor, self.gem_kinds(overrides))
        self.pins = overrides.get('pins', {}).get(self.id, {})
        self.segments = [segments(ring) for ring in self.rings]
        self.borders = [segments(simplify(ring, NAME_SIMPLIFICATION)) for ring in self.rings]
        xs = [p[0] for p in self.rings[0]]
        ys = [p[1] for p in self.rings[0]]
        self.bounds = (min(xs), min(ys), max(xs), max(ys))
        self.raster = Raster(self.rings, self.holes)

    def gem_kinds(self, overrides):
        kinds = [self.settlement, overrides.get('settlements', {}).get(self.id)]
        return [kind for kind in kinds if kind]

    def contains(self, x, y):
        """MapContourMath.Contains: inside the outer ring and outside every hole."""
        if location(self.segments[0], x, y) < 0:
            return False
        return all(location(segs, x, y) < 0 for segs in self.segments[1:])

    def letter_height(self):
        return MAJOR_LETTER_HEIGHT if self.settlement in ('Stronghold', 'City') else LETTER_HEIGHT

    def gate(self):
        """The front of the fortress drawing, low on its middle, where the garrison stands."""
        x0, y0, x1, y1 = self.drawing
        return (x0 + x1) / 2, y1 - (y1 - y0) * GATE_INSET

    def fortress(self):
        """What a stronghold's name keeps clear of: its drawing and its garrison's figures at the gate at scale 1."""
        if not self.drawing:
            return None
        gx0, gy0, gx1, gy1 = ARMY.rect(*self.gate(), 1.0)
        x0, y0, x1, y1 = self.drawing
        # Rounded outwards, so the recorded fortress holds the whole drawing.
        down = lambda value: math.floor(value * 100) / 100
        up = lambda value: math.ceil(value * 100) / 100
        return down(min(x0, gx0)), down(min(y0, gy0)), up(max(x1, gx1)), up(max(y1, gy1))

    def slots(self):
        """The slots in the order they are placed: the garrison, the field force (UI-425: before the garrison's
        characters, which keep clear of its label), the garrison's characters, the Fellowship (UI-513: before the
        field's recruits and characters, so it takes the open middle rather than what they leave), then the rest
        largest first."""
        result = []
        if self.stronghold:
            result.append(Slot('garrison', None, ARMY))
        result.append(Slot('field', None, ARMY))
        if self.stronghold:
            result += [Slot('garrisonCharacters', i, CHARACTER) for i in range(3)]
        result.append(Slot('fellowship', None, FELLOWSHIP))
        result.append(Slot('planned', None, ARMY))
        result += [Slot('characters', i, CHARACTER) for i in range(6)]
        return result


def gem_rect(anchor, kinds, unit=None, band=0.0, margin=GEM_MARGIN):
    """The gem's drawn bounds in board units, the largest of the region's settlements: by default at the worst-case
    zoom, `unit` board units per point otherwise, with `band` points of side band below."""
    if not kinds:
        return None
    unit = unit if unit is not None else 1 / worst_case_zoom()
    x0 = y0 = x1 = y1 = 0.0
    for kind in kinds:
        girdle, size = GIRDLES[kind]
        x0 = min(x0, min(p[0] for p in girdle) * size * unit)
        y0 = min(y0, min(p[1] for p in girdle) * size * unit)
        x1 = max(x1, max(p[0] for p in girdle) * size * unit)
        y1 = max(y1, max(p[1] for p in girdle) * size * unit)
    ax, ay = anchor
    return ax + x0 - margin, ay + y0 - margin, ax + x1 + margin, ay + y1 + band * unit + margin


def stone_rect(anchor, kinds):
    """UI-425: the gem as drawn on the whole map of the worst case, with its side band, plus a margin."""
    return gem_rect(anchor, kinds, GEM_SMALLEST / whole_map_zoom(), SIDE_BAND, STONE_MARGIN)


class Raster:
    """The region on a pixel grid: its mask, the anisotropic distance to its border and a summed-area table."""

    def __init__(self, rings, holes):
        points = np.concatenate([np.array(ring) for ring in rings])
        self.x0, self.y0 = np.floor(points.min(0)) - 4
        x1, y1 = np.ceil(points.max(0)) + 4
        width = int((x1 - self.x0) * RESOLUTION) + 1
        height = int((y1 - self.y0) * RESOLUTION) + 1
        image = Image.new('L', (width, height), 0)
        draw = ImageDraw.Draw(image)
        for ring, hole in zip(rings, holes):
            pixels = [((x - self.x0) * RESOLUTION, (y - self.y0) * RESOLUTION) for x, y in ring]
            draw.polygon(pixels, fill=0 if hole else 255)
        self.mask = np.asarray(image) > 0
        # An ellipse of semi-axes (r, DEPTH r) centred on a pixel lies inside the mask when this distance is at least r.
        # The smallest value around each pixel stands for it, so a point rounded off its pixel keeps that room.
        reach = ndimage.distance_transform_edt(self.mask, sampling=(1 / (RESOLUTION * DEPTH), 1 / RESOLUTION))
        self.reach = ndimage.minimum_filter(reach, size=3)
        self.table = np.pad(self.mask.astype(np.int64).cumsum(0).cumsum(1), ((1, 0), (1, 0)))

    def lattice(self):
        step = int(LATTICE * RESOLUTION)
        rows, cols = np.nonzero(self.mask[::step, ::step])
        rows, cols = rows * step, cols * step
        return self.x0 + cols / RESOLUTION, self.y0 + rows / RESOLUTION, self.reach[rows, cols]

    def reach_at(self, x, y):
        """The room at the pixel nearest to a point; the minimum filter already covers a pixel's step either way."""
        col = int(round((x - self.x0) * RESOLUTION))
        row = int(round((y - self.y0) * RESOLUTION))
        if not (0 <= row < self.reach.shape[0] and 0 <= col < self.reach.shape[1]):
            return 0.0
        return float(self.reach[row, col])

    def reach_along(self, x, y, left, right):
        """UI-522: the least room along a horizontal spine from `left` before a point to `right` after it, a pixel at a
        time: a row stand's round ends fit wherever their centres may stand on it."""
        offsets = np.arange(-left, right + 1e-6, 1 / RESOLUTION)
        return min(self.reach_at(x + offset, y) for offset in offsets)

    def inside_share(self, x0, y0, x1, y1):
        """The share of each rectangle's area that lies on the region (vectorised)."""
        c0 = np.clip(np.round((x0 - self.x0) * RESOLUTION).astype(int), 0, self.mask.shape[1])
        c1 = np.clip(np.round((x1 - self.x0) * RESOLUTION).astype(int), 0, self.mask.shape[1])
        r0 = np.clip(np.round((y0 - self.y0) * RESOLUTION).astype(int), 0, self.mask.shape[0])
        r1 = np.clip(np.round((y1 - self.y0) * RESOLUTION).astype(int), 0, self.mask.shape[0])
        inside = self.table[r1, c1] - self.table[r0, c1] - self.table[r1, c0] + self.table[r0, c0]
        area = np.maximum((x1 - x0) * (y1 - y0) * RESOLUTION * RESOLUTION, 1)
        return inside / area


class Slot:
    def __init__(self, kind, index, footprint):
        self.kind = kind
        self.index = index
        self.footprint = footprint

    @property
    def key(self):
        return self.kind if self.index is None else f'{self.kind}[{self.index}]'

    @property
    def garrison_group(self):
        return self.kind in ('garrison', 'garrisonCharacters')


class Place:
    def __init__(self, slot, x, y, score):
        self.slot = slot
        self.x = x
        self.y = y
        self.score = score

    def rect(self, scale):
        return self.slot.footprint.rect(self.x, self.y, scale)


# ---- Readability (UI-425) -----------------------------------------------------------------------------

# What a force shows beyond its footprint, in board units at footprint scale 1 (MapArmyArtwork at 1.35 points a unit):
# its figures down to the plaque that hangs below the stand, and the plaque with the emblems and the count; a
# character's figure (the footprint's column) down to the front of its painted stand.
ARMY_PICTURE = (-20.0, -40.0, 20.0, 15.6)
PLAQUE = (-20.0, 2.2, 20.0, 14.8)
CHARACTER_PICTURE = (-8.0, -36.0, 8.0, 6.3)
# The forces whose labels stay readable, and what each slot shows far out: a field force's token, a garrison's walled
# token or a character's medallion (the Fellowship has no token).
ARMIES = ('field', 'garrison')
TOKENS = {'field': 'field', 'garrison': 'garrison', 'planned': 'field', 'characters': 'medallion',
          'garrisonCharacters': 'medallion'}
# Other regions' forces whose labels a region's forces keep clear of, within this reach of its bounds (board units).
NEIGHBOURHOOD = 80.0


def whole_map_zoom():
    """The whole map of the worst case (1366x768 at 125 %): tokens, medallions, kings' markers and gems are largest on
    the board there, since zoomed out further they keep that board size (MapStandScale.Distant)."""
    return worst_case_zoom() / TOKENS_BELOW


def king_zooms():
    """The zooms a king's marker is checked at, from the whole map of the worst case to the closest."""
    return [whole_map_zoom(), worst_case_zoom(), 1.0, 1.35, 1.9, CLOSEST_ZOOM]


class Token:
    """A far-out picture in design points round the point under its feet (MapArmyArtwork, MapCharacterArtwork,
    MapSovereignMarkers): its parts, each an ellipse (a puck, a coin) or a rectangle (a count badge) in its box, drawn
    `units` x zoom points wide within [minimum, design], or always `design` wide."""

    def __init__(self, feet, parts, design, units=None, minimum=None):
        self.feet = feet
        self.parts = parts
        self.design = design
        self.units = units
        self.minimum = minimum

    def unit(self, zoom):
        """Board units per design point at a zoom; past the whole map it keeps its board size."""
        zoom = max(zoom, whole_map_zoom())
        size = self.design if self.units is None else min(self.design, max(self.minimum, self.units * zoom))
        return size / self.design / zoom

    def shapes(self, x, y, zoom=None):
        """Its parts on the board, (round, (x0, y0, x1, y1)); the points may be arrays."""
        unit = self.unit(zoom if zoom is not None else whole_map_zoom())
        fx, fy = self.feet
        return [(round_, (x + (x0 - fx) * unit, y + (y0 - fy) * unit, x + (x1 - fx) * unit, y + (y1 - fy) * unit))
                for round_, (x0, y0, x1, y1) in self.parts]

    def rect(self, x, y, zoom=None):
        """Its first part's box: a label is one rectangle."""
        return self.shapes(x, y, zoom)[0][1]


# A field force's puck and its count (a two-digit badge) stand on the puck's lower edge; a garrison's walled puck lies
# with the middle of its top face on its gate, as the gate is on the fortress drawing and a raised puck would reach over
# the stone above some (Moria, Minas Morgul); a character's medallion (its face and the edge 2.5 points below) stands on
# its coin's lower edge; a king's iron puck on its lower edge, at its fixed 30 points (MapArmyArtwork.Feet,
# MapCharacterArtwork.Feet, MapSovereignMarkers).
PUCK = (True, (0, 2, 44, 37.2))
BADGE = (False, (22, 25, 44, 44))
FIELD_FEET, GARRISON_FEET = (22, 37.2), (22, 16)
PICTURES = {'field': Token(FIELD_FEET, [PUCK, BADGE], 44, 40, 32),
            'garrison': Token(GARRISON_FEET, [PUCK, BADGE], 44, 40, 32),
            'medallion': Token((12, 26.5), [(True, (0, 0, 24, 26.5))], 24, 20, 18)}
COUNTS = {'field': Token(FIELD_FEET, [BADGE], 44, 40, 32), 'garrison': Token(GARRISON_FEET, [BADGE], 44, 40, 32)}
KING_MARKER = Token((15, 28), [(True, (0, 4, 30, 28))], 30)


def count_badge(kind, x, y, zoom=None):
    """The count badge of a force's token (planned forces stand as field forces)."""
    return COUNTS[TOKENS[kind]].rect(x, y, zoom)


def touches(shapes, rect, clearance):
    """Whether any of a picture's shapes comes within the clearance of a rectangle (vectorised)."""
    hit = False
    for round_, box in shapes:
        hit = hit | (ellipse_touches(box, rect, clearance) if round_ else covers(box, rect, clearance))
    return hit


def ellipse_touches(box, rect, clearance):
    """Whether the ellipse inscribed in a box, grown by the clearance, shares a point with a rectangle: the rectangle's
    point nearest the ellipse's centre lies inside it."""
    x0, y0, x1, y1 = box
    rx0, ry0, rx1, ry1 = rect
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    a, b = (x1 - x0) / 2 + clearance, (y1 - y0) / 2 + clearance
    px = np.minimum(np.maximum(cx, rx0), rx1)
    py = np.minimum(np.maximum(cy, ry0), ry1)
    return ((px - cx) / a) ** 2 + ((py - cy) / b) ** 2 < 1


def offset(rect, x, y, scale):
    return x + rect[0] * scale, y + rect[1] * scale, x + rect[2] * scale, y + rect[3] * scale


def figure_picture(kind, x, y, scale):
    """A stand's figures (and, for a force, its plaque) at a footprint scale, as one rectangle shape."""
    return [(False, offset(ARMY_PICTURE if TOKENS.get(kind) in COUNTS else CHARACTER_PICTURE, x, y, scale))]


def token_picture(kind, x, y):
    """A stand's token or medallion on the whole map of the worst case."""
    return PICTURES[TOKENS[kind]].shapes(x, y)


def in_front(ax, ay, bx, by):
    """Whether a stand on the first point is drawn in front of one on the second (MapStand.CompareDepth)."""
    return (ay > by) | ((ay == by) & (ax > bx))


def hides_label(owner, cover, scale):
    """Whether a stand drawn in front lies on a force's label, both of one region and so at one level: its figures on the
    plaque at the footprint scale, or its token on the count on the whole map. `owner` and `cover` are (kind, x, y);
    either point may be arrays."""
    (owner_kind, ox, oy), (kind, cx, cy) = owner, cover
    figures = touches(figure_picture(kind, cx, cy, scale), offset(PLAQUE, ox, oy, scale), 0)
    tokens = touches(token_picture(kind, cx, cy), count_badge(owner_kind, ox, oy), 0) if kind in TOKENS else False
    return in_front(cx, cy, ox, oy) & (figures | tokens)


def hides_label_across(owner, cover):
    """The same for forces of two regions at one level: figures at scale 1, tokens on the whole map. (A tight region's
    token beside a roomy neighbour's figures at the Detail zoom is not checked: it would cost Orthanc its figures.)"""
    return hides_label(owner, cover, 1.0)


def label_conflict(mover, placed, scale):
    """The field force's and the garrison's labels stay clear of every other stand of their region drawn in front of
    them — in a siege the other side, its own characters, the planned recruits and the Fellowship: `mover` is
    (kind, x, y) with arrays, `placed` a Place."""
    other = (placed.slot.kind, placed.x, placed.y)
    hit = hides_label(mover, other, scale) if mover[0] in ARMIES else False
    if other[0] in ARMIES:
        hit = hit | hides_label(other, mover, scale)
    return hit


def neighbour_conflict(mover, neighbours):
    """A force's label clear of the forces of the regions around it, and theirs of it."""
    hit = False
    if mover[0] not in ARMIES:
        return hit
    for other in neighbours:
        hit = hit | hides_label_across(mover, other) | hides_label_across(other, mover)
    return hit


def token_on_gem(kind, x, y, gems):
    """Whether a slot's token or medallion would lie on a settlement gem."""
    if kind not in TOKENS:
        return False
    picture = token_picture(kind, x, y)
    hit = False
    for gem in gems:
        hit = hit | touches(picture, gem, CLEARANCE)
    return hit


def armies_near(region, layouts):
    """The forces of the other regions laid out so far that stand within reach of a region: (kind, x, y)."""
    x0, y0, x1, y1 = region.bounds
    reach = (x0 - NEIGHBOURHOOD, y0 - NEIGHBOURHOOD, x1 + NEIGHBOURHOOD, y1 + NEIGHBOURHOOD)
    result = []
    for owner, layout in layouts.items():
        if owner == region.id:
            continue
        for place in layout:
            if place.slot.kind in ARMIES and reach[0] < place.x < reach[2] and reach[1] < place.y < reach[3]:
                result.append((place.slot.kind, place.x, place.y))
    return result


# ---- Kings (UI-425) -----------------------------------------------------------------------------------

def king_hides_label(owner, xs, ys):
    """Whether a king's marker on these points, drawn in front of a force, lies on its count or plaque at any zoom from
    the whole map to the closest."""
    owner_kind, ox, oy = owner
    hit = False
    for zoom in king_zooms():
        marker = KING_MARKER.shapes(xs, ys, zoom)
        hit = hit | touches(marker, count_badge(owner_kind, ox, oy, zoom), 0)
        if zoom >= worst_case_zoom():
            footprint = min(zoom, 1.35) / zoom
            hit = hit | touches(marker, offset(PLAQUE, ox, oy, footprint), 0)
    return in_front(xs, ys, ox, oy) & hit


def king_on_gem(xs, ys, regions):
    """Whether a king's marker on these points lies on a gem: on the whole map, or at the worst-case Detail zoom."""
    hit = False
    for zoom, stone in ((whole_map_zoom(), 'stone'), (worst_case_zoom(), 'gem')):
        marker = KING_MARKER.shapes(xs, ys, zoom)
        for region in regions:
            if region.gem:
                hit = hit | touches(marker, getattr(region, stone), CLEARANCE)
    return hit


def king_forces(region, layout, layouts):
    """The forces a region's king stands beside or near: its own field, garrison and planned force and its neighbours'."""
    own = [(place.slot.kind, place.x, place.y) for place in layout if place.slot.kind in ARMIES + ('planned',)]
    return own + armies_near(region, layouts)


def place_sovereign(region, layout, layouts, regions):
    """Where a corrupted king's marker stands: the point nearest the region's last character place (beside its forces,
    as before UI-425), off the armies' bases, whose marker lies on no force's label it stands in front of, at any zoom,
    and on no gem."""
    xs, ys, reach = region.raster.lattice()
    feasible = reach >= CHARACTER.radius(1.0) + MARGIN
    feasible &= clear(on_army_base(xs, ys, layout))
    for owner in king_forces(region, layout, layouts):
        feasible &= ~king_hides_label(owner, xs, ys)
    feasible &= clear(king_on_gem(xs, ys, regions))
    last = next(place for place in layout if place.slot.key == 'characters[5]')
    score = np.where(feasible, -np.hypot(xs - last.x, ys - last.y), -np.inf)
    best = int(np.argmax(score))
    if not feasible[best]:
        raise ValueError(f"No place for a king's marker fits {region.id}")
    return round(float(xs[best]), 1), round(float(ys[best]), 1)


def on_army_base(xs, ys, layout):
    """Whether a king's marker on these points would stand on a base of the region's field, garrison or planned force,
    taking the room of a character's base beside it (UI-422's gap)."""
    hit = False
    for place in layout:
        if place.slot.footprint is ARMY:
            distance = np.hypot(xs - place.x, (ys - place.y) / DEPTH)
            hit = hit | (distance < ARMY.radius(1.0) + CHARACTER.radius(1.0) + GAP)
    return hit


# ---- Layout -------------------------------------------------------------------------------------------

class Obstacles:
    """Gems, fortress drawings and reserved names that no figure rectangle may cover, and the room a name would like
    to keep, which figures only avoid where they can."""

    def __init__(self, regions, reserved=None, rooms=None):
        self.items = []
        for region in regions:
            if region.gem:
                self.items.append(('gem', region.id, region.gem))
                self.items.append(('stone', region.id, region.stone))
            if region.drawing:
                self.items.append(('drawing', region.id, region.drawing))
        for region_id, rects in (reserved or {}).items():
            self.items += [('name', region_id, rect) for rect in rects]
        for region_id, rects in (rooms or {}).items():
            self.items += [('room', region_id, rect) for rect in rects]

    def near(self, region):
        x0, y0, x1, y1 = region.bounds
        # Figures rise above their feet, so a region's figures reach further up than down.
        reach = (x0 - 40, y0 - 70, x1 + 40, y1 + 20)
        return [item for item in self.items if overlaps(item[2], reach)]

    @staticmethod
    def applies(item, region, slot):
        """Whether a hard obstacle binds a slot: a garrison group stands at its own drawing."""
        kind, owner, _ = item
        return kind in ('gem', 'drawing', 'name') and not (kind == 'drawing' and owner == region.id and slot.garrison_group)


def target(region, slot, placed):
    """The point a slot is drawn to, how strongly a vertical step counts against it and the pull's weight."""
    by_kind = {p.slot.kind: p for p in placed}
    if slot.kind == 'garrison':
        return region.gate(), 1.0, 1.0
    if slot.kind == 'garrisonCharacters':
        garrison = by_kind['garrison']
        return (garrison.x, garrison.y), 2.5, 1.0
    if slot.kind == 'field':
        return region.anchor, 1.0, 1.0
    field = by_kind['field']
    if slot.kind == 'characters':
        return (field.x, field.y), 2.5, 1.0
    if slot.kind == 'fellowship':
        return (field.x, field.y), 1.5, 0.7
    return (field.x, field.y), 1.5, 0.8


def covers(rects, obstacle, clearance):
    """Which rectangles come within the clearance of an obstacle (vectorised)."""
    x0, y0, x1, y1 = rects
    ox0, oy0, ox1, oy1 = obstacle
    return (ox1 + clearance > x0) & (ox0 - clearance < x1) & (oy1 + clearance > y0) & (oy0 - clearance < y1)


def hidden_share(rects, other):
    """How much of the smaller of each rectangle and another one the two share."""
    x0, y0, x1, y1 = rects
    ox0, oy0, ox1, oy1 = other
    width = np.clip(np.minimum(x1, ox1) - np.maximum(x0, ox0), 0, None)
    height = np.clip(np.minimum(y1, oy1) - np.maximum(y0, oy0), 0, None)
    smaller = np.minimum((x1 - x0) * (y1 - y0), (ox1 - ox0) * (oy1 - oy0))
    return width * height / smaller


def total(layout):
    return sum(place.score for place in layout)


class Search:
    """One attempt to lay a region's slots out at one scale, with one weight against figures hiding each other."""

    def __init__(self, region, candidates, obstacles, scale, hidden, neighbours=()):
        self.region = region
        self.candidates = candidates
        self.obstacles = obstacles
        self.scale = scale
        self.hidden = hidden
        self.neighbours = neighbours
        self.stones = [item[2] for item in obstacles if item[0] == 'stone']

    def solve(self):
        """The best layout: a short beam over the garrison and the field, the rest greedy."""
        slots = self.region.slots()
        layouts = []
        for prefix in self.prefixes(slots):
            fields = self.options(slots[len(prefix)], prefix, BEAM['field'])
            layouts += [self.complete(slots, prefix + [field]) for field in fields]
        layouts = [layout for layout in layouts if layout]
        return max(layouts, key=total) if layouts else None

    def prefixes(self, slots):
        """A stronghold's garrison on a short beam of gate points; its characters follow the field."""
        if not self.region.stronghold:
            return [[]]
        return [[gate] for gate in self.options(slots[0], [], BEAM['garrison'])]

    def complete(self, slots, placed, budget=None):
        """The remaining slots, each on its best point around the places already chosen. Where a slot finds no
        point, the search backs up and tries the next best points of the slots before it, within a budget of steps."""
        budget = budget if budget is not None else [BACKTRACKING]
        if len(placed) == len(slots):
            return placed
        if budget[0] <= 0:
            return None
        budget[0] -= 1
        for option in self.options(slots[len(placed)], placed, BRANCHES):
            layout = self.complete(slots, placed + [option], budget)
            if layout:
                return layout
        return None

    def options(self, slot, placed, count):
        """The best feasible points for a slot, at least SUPPRESSION apart; a pin is the only option."""
        if slot.key in self.region.pins:
            x, y = self.region.pins[slot.key]
            return [Place(slot, x, y, 0.0)]
        feasible, score = self.evaluate(slot, placed)
        order = np.argsort(-np.where(feasible, score, -np.inf), kind='stable')
        xs, ys, _ = self.candidates
        chosen = []
        for index in order[:4000]:
            if not feasible[index] or len(chosen) == count:
                break
            if all(math.hypot(xs[index] - c.x, ys[index] - c.y) >= SUPPRESSION for c in chosen):
                chosen.append(Place(slot, float(xs[index]), float(ys[index]), float(score[index])))
        return chosen

    def evaluate(self, slot, placed):
        """Feasibility and score of every candidate point for one slot."""
        xs, ys, reach = self.candidates
        radius = slot.footprint.radius(self.scale)
        rects = slot.footprint.rect(xs, ys, self.scale)
        feasible = self.feasible(slot, placed, rects, radius)
        (tx, ty), vertical, weight = target(self.region, slot, placed)
        score = -weight * np.hypot(xs - tx, (ys - ty) * vertical)
        score += ROOM_WEIGHT * np.minimum(reach - radius - MARGIN, ROOM_CAP)
        if slot.kind == 'fellowship':
            score += FELLOWSHIP_ROOM_WEIGHT * np.minimum(reach - radius - MARGIN, FELLOWSHIP_ROOM_CAP)
        score -= OUTSIDE_WEIGHT * (1 - self.region.raster.inside_share(*rects))
        for place in placed:
            score -= self.hidden * hidden_share(rects, place.rect(self.scale))
        for item in self.obstacles:
            if item[0] == 'room':
                score -= ROOM_FOR_NAME_WEIGHT * hidden_share(rects, item[2])
        return feasible, score

    def feasible(self, slot, placed, rects, radius):
        """Where the base fits the region clear of the other bases, and the figures cover no obstacle; UI-425: where
        its token covers no gem and no force's label lies under another stand of its region drawn in front of it."""
        xs, ys, reach = self.candidates
        mover = (slot.kind, xs, ys)
        feasible = reach >= radius + MARGIN
        for place in placed:
            distance = np.hypot(xs - place.x, (ys - place.y) / DEPTH)
            feasible &= distance >= radius + place.slot.footprint.radius(self.scale) + GAP
            if {slot.kind, place.slot.kind} == SIEGE:
                feasible &= hidden_share(rects, place.rect(self.scale)) <= ARMIES_HIDDEN
            feasible &= clear(label_conflict(mover, place, self.scale))
        for item in self.obstacles:
            if Obstacles.applies(item, self.region, slot):
                feasible &= ~covers(rects, item[2], CLEARANCE)
        feasible &= clear(token_on_gem(slot.kind, xs, ys, self.stones))
        feasible &= clear(neighbour_conflict(mover, self.neighbours))
        if slot.kind == 'garrison':
            dx0, dy0, dx1, dy1 = self.region.drawing
            feasible &= (xs > dx0 + 1) & (xs < dx1 - 1) & (ys > dy0 + 1) & (ys < dy1 - 1)
        return feasible


def clear(conflict):
    """Where a conflict (an array, or False where it cannot arise) does not hold."""
    return ~np.asarray(conflict, dtype=bool)


def lay_out(region, obstacles, neighbours=()):
    """The region's places at the largest scale they fit and the capacity of the whole group; None if none fits.
    Figures that hide each other weigh heavily first; only where that leaves the group below scale 1 does a lighter
    weight get its chance, and the larger capacity wins. `neighbours` are the forces of the regions around it."""
    near = obstacles.near(region)
    candidates = region.raster.lattice()
    best = None
    for hidden in HIDDEN_WEIGHTS:
        result = lay_out_with(region, candidates, near, hidden, neighbours)
        if result and (best is None or result[1] > best[1]):
            best = result
        if best and best[1] >= 1:
            break
    return best


def lay_out_with(region, candidates, obstacles, hidden, neighbours):
    for scale in SCALES:
        layout = Search(region, candidates, obstacles, scale, hidden, neighbours).solve()
        if layout:
            for place in layout:
                place.x = round(place.x, 1)
                place.y = round(place.y, 1)
            return layout, capacity(region, layout, obstacles, scale)
    return None


def fits(region, layout, obstacles, scale):
    """Whether the fixed places hold at a footprint scale."""
    for i, place in enumerate(layout):
        radius = place.slot.footprint.radius(scale)
        if region.raster.reach_at(place.x, place.y) < radius + MARGIN:
            return False
        for other in layout[:i]:
            distance = math.hypot(place.x - other.x, (place.y - other.y) / DEPTH)
            if distance < radius + other.slot.footprint.radius(scale) + CAPACITY_GAP:
                return False
        rect = place.rect(scale)
        if any(Obstacles.applies(item, region, place.slot) and overlaps(rect, item[2]) for item in obstacles):
            return False
        mover = (place.slot.kind, place.x, place.y)
        if any(bool(label_conflict(mover, other, scale)) for other in layout[:i]):
            return False
    return True


def capacity(region, layout, obstacles, laid_out):
    """The largest footprint scale at which the whole group still fits, to a hundredth, rounded down; at least the
    scale it was laid out at."""
    low, high = laid_out, LARGEST_CAPACITY
    if not fits(region, layout, obstacles, low):
        raise ValueError(f'The stand places of {region.id} do not fit at the scale they were laid out at')
    if fits(region, layout, obstacles, high):
        return high
    for _ in range(20):
        middle = (low + high) / 2
        if fits(region, layout, obstacles, middle):
            low = middle
        else:
            high = middle
    return math.floor(low * 100) / 100


# ---- Names (MapRegionNamePoints.For, MapRegionNames.Point) --------------------------------------------

def name_half_size(name, letter_height):
    """MapRegionNames.BoardHalfSize: the name's largest extent in board units, on one line or on two."""
    per_letter = (CAPITAL_WIDTH * READING_SIZE + LETTER_SPACING) * letter_height / READING_SIZE
    lines = 2 if ' ' in name else 1
    return len(name) * per_letter / 2, lines * LINE_HEIGHT * letter_height / 2


def name_rect(x, y, half, room):
    hw = min(half[0], room[0])
    hh = min(half[1], room[1])
    return x - hw, y - hh, x + hw, y + hh


class NameSearch:
    """The port of MapRegionNamePoints.For for one region: the point a name takes on the empty board, unless a name
    there would lie under an obstacle; then the search again with the obstacles costing their points. `path` holds
    the name rectangles that must stay clear for the search to end where it did."""

    def __init__(self, region, half=None, obstacle=None):
        self.region = region
        self.half = half
        self.obstacle = obstacle
        self.active = None
        self.path = []

    def find(self):
        point, room = self.searched(None)
        self.path = [self.rect(point)]
        if self.obstacle is None or not self.covered(*point):
            return point, room
        point, room = self.searched(self.obstacle)
        self.path = [self.rect(self.coarse), self.rect(point)]
        return point, room

    def searched(self, obstacle):
        self.active = obstacle
        x0, y0, x1, y1 = self.region.bounds
        step = max(2.0, min(x1 - x0, y1 - y0) / NAME_GRID)
        best, score = self.search((x0, y0, x1 - x0, y1 - y0), step, FLOAT_MIN, None)
        self.coarse = best
        fine = (best[0] - step, best[1] - step, step * 2, step * 2)
        best, _ = self.search(fine, step * 2 / NAME_REFINEMENT, score, best)
        return best, self.room(best)

    def room(self, point):
        return chord(self.region.borders, *point, True), chord(self.region.borders, *point, False)

    def rect(self, point):
        return name_rect(*point, self.half, self.room(point)) if self.half else None

    def search(self, area, step, start_score, start):
        ax, ay, width, height = area
        best, best_score = start, start_score
        y = ay + step / 2
        while y < ay + height:
            x = ax + step / 2
            while x < ax + width:
                if self.region.contains(x, y):
                    score = self.score(x, y)
                    if score > best_score:
                        best_score, best = score, (x, y)
                x += step
            y += step
        if best_score == FLOAT_MIN:
            best = self.region.anchor
        return best, best_score

    def score(self, x, y):
        distance = clearance(self.region.borders, x, y)
        if distance < MINIMUM_CLEARANCE:
            return distance - PENALTY * 2
        score = min(distance, CLEARANCE_CAP)
        if self.active and self.covered(x, y):
            score -= PENALTY
        if y > self.region.anchor[1] + BELOW_MARGIN:
            score += BELOW_BONUS
        return score

    def covered(self, x, y):
        hw, hh = self.half
        if not self.obstacle((x - hw, y - hh, x + hw, y + hh)):
            return False
        return self.obstacle(name_rect(x, y, self.half, self.room((x, y))))


def lines(name, room, letter_height):
    width = len(name) * CAPITAL_WIDTH * letter_height
    spaces = [i for i, c in enumerate(name) if c == ' ']
    if width <= room[0] * 2 * .9 or not spaces:
        return [name]
    middle = min(spaces, key=lambda i: abs(i - len(name) / 2))
    return [name[:middle], name[middle + 1:]]


def readable(name, room, letter_height):
    rows = lines(name, room, letter_height)
    width = max(len(row) for row in rows) * (CAPITAL_WIDTH * READING_SIZE + LETTER_SPACING)
    height = len(rows) * READING_SIZE * LINE_HEIGHT
    return width <= room[0] * 2 * CLOSEST_ZOOM and height <= room[1] * 2 * CLOSEST_ZOOM


def name_obstacles(regions, layouts=None, capacities=None):
    """What a name keeps clear of (MapStandSlots.NameObstacle): every stronghold's fortress and every stand's figure
    rectangle at the scale its region shows figures at the worst case, min(1, capacity)."""
    rects = [region.fortress() for region in regions if region.stronghold]
    for region in regions:
        if layouts and region.id in layouts:
            scale = min(1.0, capacities[region.id])
            rects += [place.rect(scale) for place in layouts[region.id]]
    return rects


def place_name(region, rects):
    """MapRegionNames.Point: the point clear of the obstacles where the name can be read, else the roomiest point
    (UI-363). Returns the name rectangles its search passes through; the last is the name's own."""
    half = name_half_size(region.name, region.letter_height())
    x0, y0, x1, y1 = region.bounds
    near = (x0 - half[0], y0 - half[1], x1 + half[0], y1 + half[1])
    local = [rect for rect in rects if overlaps(rect, near)]
    search = NameSearch(region, half, lambda name: any(overlaps(rect, name) for rect in local))
    _, room = search.find()
    if not readable(region.name, room, region.letter_height()):
        roomiest = NameSearch(region, half)
        _, roomiest_room = roomiest.find()
        if readable(region.name, roomiest_room, region.letter_height()):
            search = roomiest
    return search.path


# ---- Export -------------------------------------------------------------------------------------------

def lay_out_region(region, regions, shared, path, fortresses, alternatives, neighbours=()):
    """A region's places and the search path of its name. First around the name's path. Where the group does not fit
    at scale 1 that way and alternatives are allowed, the places may come first, around another spot the name can be
    read at or around none, as long as the name's search still ends on a clear point it can be read at; the largest
    capacity wins. Its forces' labels keep clear of the `neighbours`' forces (UI-425)."""
    first = lay_out(region, Obstacles(regions, {**shared, region.id: path}), neighbours)
    if first is None:
        raise ValueError(f'No stand places fit {region.id}; pin them in {OVERRIDES.relative_to(ROOT)}')
    best = first + (path,)
    if best[1] >= 1 or not alternatives:
        return best
    trials = [Obstacles(regions, {**shared, region.id: [spot]}) for spot in name_spots(region, path[-1])]
    trials.append(Obstacles(regions, shared, {region.id: path}))
    for obstacles in trials:
        trial = lay_out(region, obstacles, neighbours)
        if trial is None or trial[1] <= best[1]:
            continue
        layout, fitted = trial
        rects = fortresses + [place.rect(min(1.0, fitted)) for place in layout]
        around = place_name(region, rects)
        if not any(overlaps(rect, around[-1]) for rect in rects):
            best = layout, fitted, around
    return best


def name_spots(region, taken):
    """Other readable spots for a tight region's name, best first, apart from each other and from its own point."""
    half = name_half_size(region.name, region.letter_height())
    search = NameSearch(region, half)
    x0, y0, x1, y1 = region.bounds
    scored = []
    for y in np.arange(y0 + NAME_SPOT_STEP / 2, y1, NAME_SPOT_STEP):
        for x in np.arange(x0 + NAME_SPOT_STEP / 2, x1, NAME_SPOT_STEP):
            if region.contains(x, y) and readable(region.name, search.room((x, y)), region.letter_height()):
                scored.append((search.score(x, y), float(x), float(y)))
    scored.sort(key=lambda item: -item[0])
    centres = [((taken[0] + taken[2]) / 2, (taken[1] + taken[3]) / 2)]
    spots = []
    for score, x, y in scored:
        if score < MINIMUM_CLEARANCE or len(spots) == NAME_SPOTS:
            break
        if all(math.hypot(x - cx, y - cy) >= NAME_SPOT_SPACING for cx, cy in centres):
            centres.append((x, y))
            spots.append(search.rect((x, y)))
    return spots


def export(regions):
    """The names, then the places. A region's places keep clear of its own name's search path, so the run-time
    search, which keeps a name's point unless a stand covers it, leaves the name there; in a tight region the name
    may instead search around the places (lay_out_region). A neighbour's figures may still cover a name; the name then
    searches around them. Where it finds no clear point it can be read at, the name is shared: every region's places
    keep clear of its path from then on, and the regions that covered it are laid out again. A shared name whose own
    places must move goes back to its empty-board path for good. Shared names only accumulate and each stays clear,
    so the rounds end. UI-425: where two neighbouring regions' forces then cover each other's labels, the one of the two
    that keeps more of its capacity is laid out again around the other's forces, and stays bound to its neighbours'."""
    fortresses = name_obstacles(regions)
    empty = {region.id: place_name(region, fortresses) for region in regions}
    rounds = Rounds(regions, fortresses, empty)
    pending = regions
    for _ in range(NAME_ROUNDS):
        for region in pending:
            rounds.lay_out(region)
        final = name_obstacles(regions, rounds.layouts, rounds.capacities)
        names = {region.id: place_name(region, final)[-1] for region in regions}
        covered = [region.id for region in regions if any(overlaps(rect, names[region.id]) for rect in final)]
        if covered:
            rounds.shared.update(covered)
            pending = [region for region in regions
                       if covers_names(region, rounds.layouts, rounds.capacities, covered, rounds.paths)]
            continue
        clashes = label_clashes(regions, rounds.layouts)
        if not clashes:
            return rounds.layouts, rounds.capacities, names
        rounds.settle(clashes)
        pending = []
    raise ValueError('Region names stay covered by stands, or forces cover their neighbours\' labels')


class Rounds:
    """The layouts of the export's rounds: each region's places, capacity and name path, the shared names and the
    regions bound to their neighbours' forces (UI-425)."""

    def __init__(self, regions, fortresses, empty):
        self.regions = regions
        self.by_id = {region.id: region for region in regions}
        self.fortresses = fortresses
        self.empty = empty
        self.paths = dict(empty)
        self.shared = set()
        self.bound = set()
        self.layouts, self.capacities = {}, {}

    def lay_out(self, region):
        self.layouts[region.id], self.capacities[region.id], self.paths[region.id] = self.trial(region)

    def trial(self, region):
        """A region laid out around the others as they stand now, without keeping the result."""
        path = self.empty[region.id] if region.id in self.shared else self.paths[region.id]
        others = {key: self.paths[key] for key in self.shared if key != region.id}
        neighbours = armies_near(region, self.layouts) if region.id in self.bound else ()
        return lay_out_region(region, self.regions, others, path, self.fortresses, region.id not in self.shared,
                              neighbours)

    def settle(self, clashes):
        """Each clash once: the region that keeps more of its capacity around the other's forces moves."""
        moved = set()
        for a, b in clashes:
            if a in moved or b in moved:
                continue
            trials = {key: self.bound_trial(key) for key in (a, b)}
            trials = {key: trial for key, trial in trials.items() if trial}
            if not trials:
                raise ValueError(f'Neither {a} nor {b} fits around the other\'s forces')
            key = max(trials, key=lambda k: (min(1.0, trials[k][1]) - min(1.0, self.capacities[k]), trials[k][1]))
            self.bound.add(key)
            self.layouts[key], self.capacities[key], self.paths[key] = trials[key]
            moved.add(key)

    def bound_trial(self, key):
        """A trial of one region bound to its neighbours' forces, or None where nothing fits around them."""
        was_bound = key in self.bound
        self.bound.add(key)
        try:
            return self.trial(self.by_id[key])
        except ValueError:
            return None
        finally:
            if not was_bound:
                self.bound.discard(key)


def label_clashes(regions, layouts):
    """UI-425: the pairs of regions whose forces (field or garrison) cover each other's labels, at either level."""
    forces = [(region.id, (place.slot.kind, place.x, place.y)) for region in regions for place in layouts[region.id]
              if place.slot.kind in ARMIES]
    clashes = []
    for i, (a, mover) in enumerate(forces):
        for b, other in forces[i + 1:]:
            if a != b and (a, b) not in clashes and bool(hides_label_across(mover, other) | hides_label_across(other, mover)):
                clashes.append((a, b))
    return clashes


def covers_names(region, layouts, capacities, names, paths):
    """Whether a region's figures lie on the search path of another region's name."""
    rects = [place.rect(min(1.0, capacities[region.id])) for place in layouts[region.id]]
    return any(overlaps(rect, step) for key in names if key != region.id for step in paths[key] for rect in rects)


def place_sovereigns(regions, layouts):
    """UI-425: every region's place for a corrupted king's marker, once every region's forces have their places."""
    return {region.id: place_sovereign(region, layouts[region.id], layouts, regions) for region in regions}


def region_json(region, layout, capacity_value, sovereign, rooms):
    """A region's capacity, its places in a fixed order, the place of a corrupted king's marker, the room of its forces'
    row stands (UI-522) and, for a stronghold, the fortress its name keeps clear of."""
    result = {'capacity': capacity_value}
    result.update({f'{kind}Row': list(reach) for kind, reach in rooms.items()})
    if region.stronghold:
        result['fortress'] = list(region.fortress())
    order = ('garrison', 'garrisonCharacters', 'field', 'characters', 'fellowship', 'planned')
    for place in sorted(layout, key=lambda p: (order.index(p.slot.kind), p.slot.index or 0)):
        point = [place.x, place.y]
        if place.slot.index is None:
            result[place.slot.kind] = point
        else:
            result.setdefault(place.slot.kind, []).append(point)
    result['sovereign'] = list(sovereign)
    return result


def document(regions, layouts, capacities, sovereigns, rooms):
    return {
        'version': VERSION,
        'task': 'UI-422, UI-425, UI-522',
        'sourceHashes': source_hashes(),
        'worstCaseZoom': round(worst_case_zoom(), 5),
        'footprints': {name: footprint.to_json() for name, footprint in FOOTPRINTS.items()},
        'regions': {region.id: region_json(region, layouts[region.id], capacities[region.id], sovereigns[region.id],
                                           rooms[region.id])
                    for region in sorted(regions, key=lambda r: r.id)},
    }


def serialise(data):
    """Each source, footprint and region on one line, so a change to one region is one line of the diff."""
    lines = []
    for key, value in data.items():
        if isinstance(value, dict):
            rows = ',\n'.join(f'    {compact(name)}: {compact(entry)}' for name, entry in value.items())
            lines.append(f'  {compact(key)}: {{\n{rows}\n  }}')
        else:
            lines.append(f'  {compact(key)}: {compact(value)}')
    return '{\n' + ',\n'.join(lines) + '\n}\n'


def compact(value):
    return json.dumps(value, ensure_ascii=False, separators=(', ', ': '))


# ---- Row stands (UI-522) ------------------------------------------------------------------------------

# UI-522 (owner, 2026-11-07): a force of more units than all stand shows one figure of each kind side by side on a stand
# as wide as the row and as deep as an army's; UI-523 (owner, 2026-11-08: "decidedly bigger"): its figures stand at least
# 85 % of an army's height, overlapping a little where the row is short (MapArmyArtwork), so the row may reach further.
# The places keep their layout; each field and garrison place records how far its row stand may reach on either side of
# its feet — from a round base's half (20 units) up to 50, in half-unit steps — keeping inside the region, off the
# other force's and the king's marker's bases, on no gem, other fortress drawing or region name, and on no label of a
# force it stands in front of (its region's other force or a neighbour's). The owner chose this over a row stand
# reserved everywhere, which left 42 regions instead of 15 showing tokens at the worst case. The characters, the
# Fellowship and the planned recruits come and go, so the client narrows the side a stand of theirs stands on
# (MapArmyRowRoom); the row then reaches further on the open side.
ROW_REACH = 50.0
ROW_STEP = 0.5
# UI-523: a row's figures may stand as tall as an army's.
ROW_TOP = -40.0


def row_rect(x, y, left, right):
    """A row stand's figures down to its base's front, at scale 1."""
    return x - left, y + ROW_TOP, x + right, y + ARMY.base[1] / 2


def row_distance(x, y, reach, px, py, other=None):
    """The distance between a row stand's spine (its round ends an army's) and another base's — a round one, or a row
    of the given reach — with depth measured over DEPTH."""
    radius = ARMY.radius(1.0)
    left, right = reach
    other_left, other_right = other or (radius, radius)
    spine = (x - (left - radius), x + (right - radius))
    other_spine = (px - (other_left - radius), px + (other_right - radius))
    across = max(other_spine[0] - spine[1], spine[0] - other_spine[1], 0.0)
    return math.hypot(across, (y - py) / DEPTH)


def force_rooms(regions, layouts, names, sovereigns):
    """Each region's field and garrison row reach (left, right): the garrison's first, the field's clear of it."""
    covers = [('gem', r.id, r.gem) for r in regions if r.gem] + [('drawing', r.id, r.drawing) for r in regions if r.drawing]
    rooms = {}
    for region in regions:
        layout = layouts[region.id]
        neighbours = armies_near(region, layouts)
        forces = sorted((place for place in layout if place.slot.kind in ARMIES), key=lambda p: p.slot.kind != 'garrison')
        rooms[region.id] = {}
        for place in forces:
            context = (region, layout, covers, names, sovereigns[region.id], neighbours, rooms[region.id])
            rooms[region.id][place.slot.kind] = row_reach(place, context)
    return rooms


def row_reach(place, context):
    """The furthest each side reaches with the other round, then both together, the longer side giving way first."""
    radius = ARMY.radius(1.0)
    left = furthest(lambda value: row_fits(place, (value, radius), *context))
    right = furthest(lambda value: row_fits(place, (radius, value), *context))
    while (left > radius or right > radius) and not row_fits(place, (left, right), *context):
        if left >= right:
            left -= ROW_STEP
        else:
            right -= ROW_STEP
    return left, right


def furthest(fits):
    """The furthest reach, in steps down from the widest, that fits; a round base's half always does."""
    value = ROW_REACH
    while value > ARMY.radius(1.0) and not fits(value):
        value -= ROW_STEP
    return value


def row_fits(place, reach, region, layout, covers, names, sovereign, neighbours, rooms):
    """Whether a force's row stand reaching this far either side fits its place (force_rooms)."""
    x, y = place.x, place.y
    left, right = reach
    radius = ARMY.radius(1.0)
    if region.raster.reach_along(x, y, left - radius, right - radius) < radius + MARGIN:
        return False
    if row_distance(x, y, reach, *sovereign) < radius + CHARACTER.radius(1.0) + GAP:
        return False
    rect = row_rect(x, y, left, right)
    for kind, owner, box in covers:
        exempt = kind == 'drawing' and owner == region.id and place.slot.garrison_group
        if not exempt and covers_rect(rect, box):
            return False
    if any(covers_rect(rect, name) for name in names.values()):
        return False
    for other in layout:
        if other is place or other.slot.kind not in ARMIES:
            continue
        if row_distance(x, y, reach, other.x, other.y, rooms.get(other.slot.kind)) < 2 * radius + GAP:
            return False
        # A siege shows both armies (UI-422): the row's whole picture against the other force's.
        whole = (x - left, y + ARMY_PICTURE[1], x + right, rect[3])
        if hidden_share(whole, other.rect(1.0)) > ARMIES_HIDDEN:
            return False
    owners = [(other.x, other.y) for other in layout if other is not place and other.slot.kind in ARMIES]
    owners += [(ox, oy) for _, ox, oy in neighbours]
    for ox, oy in owners:
        if bool(in_front(x, y, ox, oy)) and overlaps(rect, offset(PLAQUE, ox, oy, 1.0)):
            return False
    return True


def covers_rect(rect, box):
    """Whether a rectangle comes within the clearance of an obstacle's box."""
    return bool(covers(rect, box, CLEARANCE))


# ---- Acceptance ---------------------------------------------------------------------------------------

def ellipse_inside(region, x, y, a, b):
    """The base lies inside its contour: its centre is inside and no border segment reaches into it."""
    if not region.contains(x, y):
        return False
    for ax, ay, bx, by in region.segments:
        scaled = ((ax - x) / a, (ay - y) / b, (bx - x) / a, (by - y) / b)
        if segment_distances(0.0, 0.0, scaled).min() < 1:
            return False
    return True


def verify(regions, layouts, capacities, names):
    """The acceptance checks of MapStandSlotsTests, run on the export before it is written."""
    obstacles = name_obstacles(regions, layouts, capacities)
    covers = [('gem', r.id, r.gem) for r in regions if r.gem]
    covers += [('drawing', r.id, r.drawing) for r in regions if r.drawing]
    problems = []
    for region in regions:
        name = names[region.id]
        problems += [f'{region.id}: its name lies under {rect}' for rect in obstacles if overlaps(rect, name)]
        problems += verify_places(region, layouts[region.id], min(1.0, capacities[region.id]), covers)
    return problems


def verify_places(region, layout, scale, covers):
    problems = [] if len(layout) == len(region.slots()) else [f'{region.id}: missing places']
    for i, place in enumerate(layout):
        a = place.slot.footprint.radius(scale)
        if not ellipse_inside(region, place.x, place.y, a, a * DEPTH):
            problems.append(f'{region.id} {place.slot.key}: base leaves the region')
        for other in layout[:i]:
            if math.hypot(place.x - other.x, (place.y - other.y) / DEPTH) < a + other.slot.footprint.radius(scale):
                problems.append(f'{region.id} {place.slot.key}: base overlaps {other.slot.key}')
        for kind, owner, rect in covers:
            exempt = kind == 'drawing' and owner == region.id and place.slot.garrison_group
            if not exempt and overlaps(place.rect(scale), rect):
                problems.append(f'{region.id} {place.slot.key}: covers the {kind} of {owner}')
        if place.slot.kind == 'garrison':
            x0, y0, x1, y1 = region.drawing
            if not (x0 < place.x < x1 and y0 < place.y < y1):
                problems.append(f'{region.id}: garrison feet outside the drawing')
    armies = [place.rect(scale) for place in layout if place.slot.kind in SIEGE]
    if len(armies) == 2 and hidden_share(armies[0], armies[1]) > ARMIES_HIDDEN:
        problems.append(f'{region.id}: the besieger and the garrison hide each other')
    return problems


def verify_readability(regions, layouts, capacities, sovereigns):
    """UI-425: no token or medallion on a gem; every force's label clear of the other stands of its region and of its
    neighbours' forces; a king's marker inside its region, on no label it stands in front of and on no gem."""
    stones = [(region.id, region.stone) for region in regions if region.gem]
    problems = []
    for region in regions:
        layout = layouts[region.id]
        neighbours = armies_near(region, layouts)
        for i, place in enumerate(layout):
            problems += verify_token(region, place, stones)
            problems += verify_labels(region, place, layout[:i], neighbours, min(1.0, capacities[region.id]))
        problems += verify_sovereign(region, sovereigns[region.id], layout, king_forces(region, layout, layouts), regions)
    return problems


def verify_token(region, place, stones):
    return [f'{region.id} {place.slot.key}: its token lies on the gem of {owner}'
            for owner, stone in stones if bool(token_on_gem(place.slot.kind, place.x, place.y, [stone]))]


def verify_labels(region, place, before, neighbours, scale):
    mover = (place.slot.kind, place.x, place.y)
    problems = [f'{region.id} {place.slot.key}: a label lies under {other.slot.key} or covers it'
                for other in before if bool(label_conflict(mover, other, scale))]
    if bool(neighbour_conflict(mover, neighbours)):
        problems.append(f"{region.id} {place.slot.key}: its label and a neighbouring force's cover each other")
    return problems


def verify_sovereign(region, point, layout, forces, regions):
    x, y = point
    problems = [] if region.contains(x, y) else [f"{region.id}: the king's marker stands outside the region"]
    xs, ys = np.array([x]), np.array([y])
    if on_army_base(xs, ys, layout).any():
        problems.append(f"{region.id}: the king's marker stands on an army's base")
    problems += [f"{region.id}: the king's marker lies on the label of {force[0]}" for force in forces
                 if king_hides_label(force, xs, ys).any()]
    problems += [f"{region.id}: the king's marker lies on the gem of {other.id}" for other in regions
                 if other.gem and king_on_gem(xs, ys, [other]).any()]
    return problems


# ---- Preview ------------------------------------------------------------------------------------------

COLOURS = {'garrison': (110, 60, 20), 'garrisonCharacters': (200, 140, 40), 'field': (30, 110, 40),
           'characters': (60, 160, 160), 'fellowship': (200, 180, 30), 'planned': (150, 150, 150)}


def preview(regions, layouts, capacities, names, sovereigns, folder, scale=3):
    """The contours, gems, drawings, names, every place at the scale its region shows figures at and the kings'
    markers on the whole map."""
    image = Image.new('RGB', (int(MAP_W * scale), int(MAP_H * scale)), (238, 228, 205))
    draw = ImageDraw.Draw(image)
    box = lambda r: [r[0] * scale, r[1] * scale, r[2] * scale, r[3] * scale]
    for region in regions:
        for ring in region.rings:
            draw.line([(x * scale, y * scale) for x, y in ring], fill=(120, 100, 80), width=1)
        if region.drawing:
            draw.rectangle(box(region.drawing), outline=(90, 90, 90))
        if region.gem:
            draw.rectangle(box(region.gem), fill=(70, 110, 200))
        draw.rectangle(box(names[region.id]), outline=(150, 40, 40))
        factor = min(1.0, capacities[region.id])
        for place in layouts[region.id]:
            colour = COLOURS[place.slot.kind]
            a = place.slot.footprint.radius(factor)
            draw.rectangle(box(place.rect(factor)), outline=colour)
            draw.ellipse(box((place.x - a, place.y - a * DEPTH, place.x + a, place.y + a * DEPTH)), fill=colour)
        draw.rectangle(box(KING_MARKER.rect(*sovereigns[region.id])), outline=(20, 20, 20))
        if capacities[region.id] < 1:
            label = f'{capacities[region.id]:.2f}'
            draw.text((region.anchor[0] * scale, region.anchor[1] * scale), label, fill=(200, 0, 0))
    folder.mkdir(parents=True, exist_ok=True)
    image.save(folder / 'stand-slots.png')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='verify that the output matches its sources and passes')
    parser.add_argument('--preview', type=Path, help='also draw the places to stand-slots.png in this folder')
    args = parser.parse_args()
    regions, _ = load_inputs()
    layouts, capacities, names = export(regions)
    sovereigns = place_sovereigns(regions, layouts)
    rooms = force_rooms(regions, layouts, names, sovereigns)
    problems = verify(regions, layouts, capacities, names)
    problems += verify_readability(regions, layouts, capacities, sovereigns)
    if problems:
        raise ValueError('Stand places fail the acceptance checks:\n' + '\n'.join(problems))
    text = serialise(document(regions, layouts, capacities, sovereigns, rooms))
    if args.preview:
        preview(regions, layouts, capacities, names, sovereigns, args.preview)
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text() != text:
            raise ValueError(f'Stale export: {OUTPUT.relative_to(ROOT)}')
    else:
        OUTPUT.write_text(text)
    tokens = sorted(region.id for region in regions if capacities[region.id] < 1)
    print('Tokens at the worst case: ' + (', '.join(f'{r} ({capacities[r]:.2f})' for r in tokens) or 'none'))
    print('PASS: stand places ' + ('match their recorded sources' if args.check else 'exported'))


if __name__ == '__main__':
    main()
