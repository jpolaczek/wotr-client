SVG renderer for the terrain export

render_svg.cjs renders an SVG map composition to PNG with Node (package.json, package-lock.json; run `npm ci` here
once). scripts/export_map_terrain.py calls it to turn graphicalassets/maps/parchment-v4/map-composition.svg into
graphicalassets/maps/terrain-map14/MapTerrain.png. The pencil-v2 map drafts that used to sit here were removed with
the other superseded map versions (QUAL-20, 2026-10-17); Git history keeps them.
