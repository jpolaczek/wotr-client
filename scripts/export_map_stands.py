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
VERSION = 'stand-slots-1'
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
        """The slots in the order they are placed: the garrison group, then the largest first."""
        result = []
        if self.stronghold:
            result.append(Slot('garrison', None, ARMY))
            result += [Slot('garrisonCharacters', i, CHARACTER) for i in range(3)]
        result.append(Slot('field', None, ARMY))
        result.append(Slot('planned', None, ARMY))
        result += [Slot('characters', i, CHARACTER) for i in range(6)]
        result.append(Slot('fellowship', None, FELLOWSHIP))
        return result


def gem_rect(anchor, kinds):
    """The gem's drawn bounds in board units at the worst-case zoom, the largest of the region's settlements."""
    if not kinds:
        return None
    zoom = worst_case_zoom()
    x0 = y0 = x1 = y1 = 0.0
    for kind in kinds:
        girdle, size = GIRDLES[kind]
        x0 = min(x0, min(p[0] for p in girdle) * size / zoom)
        y0 = min(y0, min(p[1] for p in girdle) * size / zoom)
        x1 = max(x1, max(p[0] for p in girdle) * size / zoom)
        y1 = max(y1, max(p[1] for p in girdle) * size / zoom)
    ax, ay = anchor
    return ax + x0 - GEM_MARGIN, ay + y0 - GEM_MARGIN, ax + x1 + GEM_MARGIN, ay + y1 + GEM_MARGIN


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


# ---- Layout -------------------------------------------------------------------------------------------

class Obstacles:
    """Gems, fortress drawings and reserved names that no figure rectangle may cover, and the room a name would like
    to keep, which figures only avoid where they can."""

    def __init__(self, regions, reserved=None, rooms=None):
        self.items = []
        for region in regions:
            if region.gem:
                self.items.append(('gem', region.id, region.gem))
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
        return kind != 'room' and not (kind == 'drawing' and owner == region.id and slot.garrison_group)


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

    def __init__(self, region, candidates, obstacles, scale, hidden):
        self.region = region
        self.candidates = candidates
        self.obstacles = obstacles
        self.scale = scale
        self.hidden = hidden

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
        """A stronghold's garrison groups from a short beam of gate points, each with its three characters."""
        if not self.region.stronghold:
            return [[]]
        gates = self.options(slots[0], [], BEAM['garrison'])
        groups = (self.complete(slots[:4], [gate]) for gate in gates)
        return [group for group in groups if group]

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
        score -= OUTSIDE_WEIGHT * (1 - self.region.raster.inside_share(*rects))
        for place in placed:
            score -= self.hidden * hidden_share(rects, place.rect(self.scale))
        for item in self.obstacles:
            if item[0] == 'room':
                score -= ROOM_FOR_NAME_WEIGHT * hidden_share(rects, item[2])
        return feasible, score

    def feasible(self, slot, placed, rects, radius):
        """Where the base fits the region clear of the other bases, and the figures cover no obstacle."""
        xs, ys, reach = self.candidates
        feasible = reach >= radius + MARGIN
        for place in placed:
            distance = np.hypot(xs - place.x, (ys - place.y) / DEPTH)
            feasible &= distance >= radius + place.slot.footprint.radius(self.scale) + GAP
            if {slot.kind, place.slot.kind} == SIEGE:
                feasible &= hidden_share(rects, place.rect(self.scale)) <= ARMIES_HIDDEN
        for item in self.obstacles:
            if Obstacles.applies(item, self.region, slot):
                feasible &= ~covers(rects, item[2], CLEARANCE)
        if slot.kind == 'garrison':
            dx0, dy0, dx1, dy1 = self.region.drawing
            feasible &= (xs > dx0 + 1) & (xs < dx1 - 1) & (ys > dy0 + 1) & (ys < dy1 - 1)
        return feasible


def lay_out(region, obstacles):
    """The region's places at the largest scale they fit and the capacity of the whole group; None if none fits.
    Figures that hide each other weigh heavily first; only where that leaves the group below scale 1 does a lighter
    weight get its chance, and the larger capacity wins."""
    near = obstacles.near(region)
    candidates = region.raster.lattice()
    best = None
    for hidden in HIDDEN_WEIGHTS:
        result = lay_out_with(region, candidates, near, hidden)
        if result and (best is None or result[1] > best[1]):
            best = result
        if best and best[1] >= 1:
            break
    return best


def lay_out_with(region, candidates, obstacles, hidden):
    for scale in SCALES:
        layout = Search(region, candidates, obstacles, scale, hidden).solve()
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

def lay_out_region(region, regions, shared, path, fortresses, alternatives):
    """A region's places and the search path of its name. First around the name's path. Where the group does not fit
    at scale 1 that way and alternatives are allowed, the places may come first, around another spot the name can be
    read at or around none, as long as the name's search still ends on a clear point it can be read at; the largest
    capacity wins."""
    first = lay_out(region, Obstacles(regions, {**shared, region.id: path}))
    if first is None:
        raise ValueError(f'No stand places fit {region.id}; pin them in {OVERRIDES.relative_to(ROOT)}')
    best = first + (path,)
    if best[1] >= 1 or not alternatives:
        return best
    trials = [Obstacles(regions, {**shared, region.id: [spot]}) for spot in name_spots(region, path[-1])]
    trials.append(Obstacles(regions, shared, {region.id: path}))
    for obstacles in trials:
        trial = lay_out(region, obstacles)
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
    so the rounds end."""
    fortresses = name_obstacles(regions)
    empty = {region.id: place_name(region, fortresses) for region in regions}
    paths = dict(empty)
    shared = set()
    pending = regions
    layouts, capacities = {}, {}
    for _ in range(NAME_ROUNDS):
        for region in pending:
            if region.id in shared:
                paths[region.id] = empty[region.id]
            others = {key: paths[key] for key in shared if key != region.id}
            layouts[region.id], capacities[region.id], paths[region.id] = lay_out_region(
                region, regions, others, paths[region.id], fortresses, region.id not in shared)
        final = name_obstacles(regions, layouts, capacities)
        names = {region.id: place_name(region, final)[-1] for region in regions}
        covered = [region.id for region in regions if any(overlaps(rect, names[region.id]) for rect in final)]
        if not covered:
            return layouts, capacities, names
        shared.update(covered)
        pending = [region for region in regions if covers_names(region, layouts, capacities, covered, paths)]
    raise ValueError('Region names stay covered by stands: ' + ', '.join(covered))


def covers_names(region, layouts, capacities, names, paths):
    """Whether a region's figures lie on the search path of another region's name."""
    rects = [place.rect(min(1.0, capacities[region.id])) for place in layouts[region.id]]
    return any(overlaps(rect, step) for key in names if key != region.id for step in paths[key] for rect in rects)


def region_json(region, layout, capacity_value):
    """A region's capacity, its places in a fixed order and, for a stronghold, the fortress its name keeps clear of."""
    result = {'capacity': capacity_value}
    if region.stronghold:
        result['fortress'] = list(region.fortress())
    order = ('garrison', 'garrisonCharacters', 'field', 'characters', 'fellowship', 'planned')
    for place in sorted(layout, key=lambda p: (order.index(p.slot.kind), p.slot.index or 0)):
        point = [place.x, place.y]
        if place.slot.index is None:
            result[place.slot.kind] = point
        else:
            result.setdefault(place.slot.kind, []).append(point)
    return result


def document(regions, layouts, capacities):
    return {
        'version': VERSION,
        'task': 'UI-422',
        'sourceHashes': source_hashes(),
        'worstCaseZoom': round(worst_case_zoom(), 5),
        'footprints': {name: footprint.to_json() for name, footprint in FOOTPRINTS.items()},
        'regions': {region.id: region_json(region, layouts[region.id], capacities[region.id])
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


# ---- Preview ------------------------------------------------------------------------------------------

COLOURS = {'garrison': (110, 60, 20), 'garrisonCharacters': (200, 140, 40), 'field': (30, 110, 40),
           'characters': (60, 160, 160), 'fellowship': (200, 180, 30), 'planned': (150, 150, 150)}


def preview(regions, layouts, capacities, names, folder, scale=3):
    """The contours, gems, drawings, names and every place at the scale its region shows figures at."""
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
    problems = verify(regions, layouts, capacities, names)
    if problems:
        raise ValueError('Stand places fail the acceptance checks:\n' + '\n'.join(problems))
    text = serialise(document(regions, layouts, capacities))
    if args.preview:
        preview(regions, layouts, capacities, names, args.preview)
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
