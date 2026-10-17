MAP-09 — Carbon-drawing Mordor, with site on Barad-dûr Date: 2026-10-07

Result ../middle-earth-parchment-v3.png — the whole map, 6144   ×   5037.  map-preview.png — a preview of the whole, 1536   ×   1259.  detail-mordor.png — the approximation of the revised Mordor. detail-mordor-before.png — the same passage MAP-07 before the change. map-composition.svg — composition with embedded images and borders.

The basis for the change is the accepted MAP-07 parchment, without the uniform dark tone of the fields introduced in MAP-08. Shadow now form irregular carbon footprints at the inner slopes, foothills and existing volcano. Clear clearances retain the open plain.

The northern ridge is higher. In four sections x = 1720, 1780, 1840, 1900 the offset is about 29–32 units of the map, or about 110–122 pixels of export. Under the ridge the space on Barad-dûr was left behind, without adding a structure or a inscription.

The mask includes a local drawing of Mordor along with the slopes of the mountains, not separate fields filling. This means the ridge does not break off at the border of the region. Its range is recorded in terrain_envelope in layout.json. Minas Morgul pass and the lake interior has been deactivated from this mask. Paper and accurate paths 105 regions / 323 The boundaries are preserved.

Generation and sources Mode: built-in imagegen, editing local raster; without CLI/API.  Target:  mordor-before.png — the drawing of the site itself MAP-07, without borders and paper, cut after full map render. The source area in pixels is saved in source_crop_pixels in layout.json.  prompt-mordor-refined.txt   →   mordor-ridge.png: Correction of the northern ridges. prompt-mordor-charcoal.txt   →   mordor-charcoal.png It's a delicate coal. prompt-mordor.txt documents the first, rejected test; its raster is not a component of the result because it added small hills.

The selected raster has 1269 × 1240. This is a local layer, scaled during export; the entire PNG 6144 × 5037 does not mean a new native detail of 6K. Barad-dûr remains empty for further graphic work. The client code, rules or regions concerned has not been changed.

Revival and verification From the project catalog, with the dependencies of existing composers: NODE_PATH =/ tmp/wotr-map04-render/node_modules  /tmp/wotr-map-trace-venv/bin/ python graphicalassets/maps/parchment-v3/compose_map.py PASS: ko >TABLE> PNG   6144   ×   5037, hashe sources, paper, 105/323 paths. PASS:  2   570   579 modified pixels with respect to MAP-07, no changes except the local mask and zero in Minas Morgul's protected passage. Anti aliasing tolerance 0,75 units, details in verification.json.  PASS: viewing map-preview.png and detail-mordor.png and comparison with detail-mordor-before.png: soft shading without flat tone, raised continuous ridges, free space and open passage.

Additional measurement Pillow/NumPy on the terrain layers: in the window [ 1760 , 865 , 1930 , 902 > share of clear background (grey > 245 ) increased from 65,752% ed 96,273% ; dark lines (< 220 ) fell from 19,031% ed 0,073% The measurement describes the drawing itself, before applying parchment.

Process checks: python3 scripts/tasks.py validate git diff --check Results ko ed and transfer in docs/tasks/MAP-09.md and tasks.json Initial preparation without NODE_PATH returned MODULE_NOT_FOUND; the correct command above was executed with the result PASS No C#/Unity tests were launched: this revision only covers graphics.

Transmission Only the new PNG, this directory, document MAP-09, ROADMAP and entry MAP-09 in the registry and generated index are changed. Previous versions remain available. Asset prepared for further styling, without automatic switching of textures in the client. After closing: python3 scripts/tasks.py ready --stage A — no further finished task in the ordered stage.
