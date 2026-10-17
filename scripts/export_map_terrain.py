"""Export the existing MAP-10 paper/terrain composition without region or boundary lines."""

import argparse
from copy import deepcopy
import hashlib
import os
from pathlib import Path
import struct
import subprocess
import tempfile
from xml.etree import ElementTree as ET

from export_map_geometry import ROOT, write_or_check
from map_geometry_validation import require

SOURCE = ROOT / 'graphicalassets/maps/parchment-v4/map-composition.svg'
# UI-102: the raster is an input of the map canvas export, not a runtime resource.
OUTPUT = ROOT / 'graphicalassets/maps/terrain-map14/MapTerrain.png'
REPORT = ROOT / 'docs/sources/MAP-14-TERRAIN.json'
RENDERER = ROOT / 'graphicalassets/maps/pencil-v2/render_svg.cjs'
SVG = '{http://www.w3.org/2000/svg}'
SIZE = (4096, 4096)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def terrain_only():
    source = ET.parse(SOURCE).getroot()
    require(list(map(float, source.get('viewBox').split())) == [335, 40, 1632, 1338], 'Terrain viewBox changed')
    result = deepcopy(source)
    ink = result.find(f'{SVG}g[@id="map-ink"]')
    require(ink is not None, 'Missing MAP-10 ink layers')
    for name, count in (('regions', 105), ('boundaries', 323)):
        node = ink.find(f'{SVG}g[@id="{name}"]')
        require(node is not None and len(node) == count, f'Missing source {name}')
        ink.remove(node)
    require([node.get('id') for node in ink] == ['terrain-layers'], 'Unexpected ink layer')
    for query in (f'{SVG}image[@id="aged-paper"]', f'{SVG}defs',
                  f'{SVG}g[@id="map-ink"]/{SVG}g[@id="terrain-layers"]'):
        require(ET.tostring(result.find(query)) == ET.tostring(source.find(query)), 'Terrain or paper changed')
    terrain = ink.find(f'{SVG}g[@id="terrain-layers"]')
    require({'map10-black-gate', 'map10-erebor'} <= {node.get('id') for node in terrain}, 'Missing MAP-10 retouch')
    result.set('width', str(SIZE[0]))
    result.set('height', str(SIZE[1]))
    result.set('preserveAspectRatio', 'none')
    return result


def render(composition, output):
    with tempfile.TemporaryDirectory(prefix='wotr-map14-terrain-') as folder:
        path = Path(folder) / 'terrain.svg'
        path.write_bytes(ET.tostring(composition, encoding='utf-8', xml_declaration=True))
        subprocess.run(['node', str(RENDERER), str(path), str(output)], check=True, timeout=120,
                       env=os.environ.copy())


def report(composition, output):
    png = output.read_bytes()
    require(png[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>II', png[16:24]) == SIZE,
            'Terrain PNG size or format mismatch')
    return {'version': 'terrain-2', 'source': str(SOURCE.relative_to(ROOT)),
            'sourceHash': sha(SOURCE.read_bytes()), 'compositionHash': sha(ET.tostring(composition)),
            'removedLayers': {'regions': 105, 'boundaries': 323}, 'preservedMap10Patches': ['black-gate', 'erebor'],
            'paperAndTerrainUnchanged': True, 'sourceViewBox': [335, 40, 1632, 1338],
            'pngSize': list(SIZE), 'pngScale': [SIZE[0] / 1632, SIZE[1] / 1338],
            'pngOffset': [0, 0], 'preserveAspectRatio': 'none (restored by the runtime map quad)',
            'output': str(OUTPUT.relative_to(ROOT)), 'outputHash': sha(png), 'pngBytes': len(png),
            'renderer': '@resvg/resvg-js (existing pencil-v2/render_svg.cjs)', 'newGeography': False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    composition = terrain_only()
    if not args.check:
        render(composition, OUTPUT)
    write_or_check(REPORT, report(composition, OUTPUT), args.check)
    print(f'PASS: MAP-10 terrain {SIZE[0]}x{SIZE[1]}, 105 region/323 border paths removed; '
          f'{"export matches" if args.check else "export written"}')


if __name__ == '__main__':
    main()
