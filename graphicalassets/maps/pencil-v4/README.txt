MAP-06 — removing drawn rivers, 2026-10-06

Result ../middle-earth-pencil-v4.png — map 6144 × 5037 px. map-preview.png — whole 1536 × 1259 px. map-details.png — west cuts, Anduina valleys, east and Gondor valleys, in the left to right order, first the upper row. details-*.png — end cuts PNG, without regenerating the site. map-composition.svg — accurate outlines and reproducible layers of illustration.

The range was removed by an additional grey river network from the pencil-v3 illustrations, also lines intersecting the fields, running alongside the borders and between trees in the east. No new streams were added. The water course remains exclusively designated by the original maps-regions outlines. Borders, coast, mountains, swamps and passages are preserved. Separate layers of Old Forest, Lórien, Mirkwood and Fangorn are identical to those of MAP-05 and lie again over the background.

Sources and method of editing User's disposition with 2026-10-06 and a map of the pencil- v3.   inputs.json pin 26   SHA-256 sources, including outlines, data board and previous illustrations. Embedded imagegen, without CLI/API. Selected rasters and propts:
- background-cleanup.prompt.txt / background-clean.png — cleared background without four separate
  layers of forest; background-target.png input.
- rhun-cleanup.prompt.txt / rhun-clean.png — local removal of both river banks in the inter-tree
  break; rhun-target.png entrance.
provenance.json indicates the originals in generated_images, left on site, and the exact Rhûn staff. The first attempt with the entire area (remove-rivers.prompt.txt / terrain-target.png) is not used as a result.

compose_map.py expand SVG with MAP-05.  river-corridors.json contains 27 Line corridors and one small area of tree reconstruction at the removed river. Clips and soft masks SVG They only apply these fragments of rasters. Coordinates come from terrain-target.png   1632   ×   1338 px; shift to SVG  to [ 335,40 The boundaries are a separate layer above the illustration. Python prepares geometry SVG and reads pixels to control; does not paint rasters. Exports do resvg, clippings do macOS sips.

Verification verification.json:  6144   ×   5037  px, R=G=B,  105 regions and 323 border paths with identical attributes, four unchanged layers of forests, compatibility of all 26 hashy sources and preserved full frame map. removal-verification.json:  1991176 changed pixels, no changes except saved masks with anti aliasing margin 0,75 work units. 27 samples of old rivers have a brightness after editing at least 249/255. An overview of the whole, four clippings and a close-up of Rhûn confirms the removal of the river network and the preservation of the site. Samples complement the view; they do not themselves prove the lack of a line at every possible point of the map.

The original sample of 5 × 5 px in the narrow Rhûn break also included the preserved tree crown; it was corrected to 3 × 3 px lying on the former line. Final checks passed.

Play Python with ../pencil-v2/requirements.txt, Node and resvg 2.6.2 packages: e.g. --prefix graphicalassets/maps/pencil-v2 python3 graphicalassets/maps/pencil-v4/compose_map.py python3 graphicalassets/maps/pencil-v4/inspect_map.py Exports use the recorded result imagegen and does not cause model. In this session both scripts were run by /tmp/wotr-map-trace-venv/bin/python ed NODE_PATH=/tmp/wotr-map04-render/node_modules. Repository checks: python3 scripts/tasks.py validate git diff --check

Limits and transfer The unchanged background and background have natively 1385 × 1136 px; the local Rhûn correction has 1254 × 1254 px. The large PNG retains the same foundation detail. This graphical resource without Unity integration and changing the rules of motion. Coloring remains another separate order. After MAP-06 no further finished stage A graphic task.
