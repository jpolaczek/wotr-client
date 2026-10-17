MAP-05 — continuous and readable forests, 2026-10-06

Result ../middle-earth-pencil-v3.png — map 6144 × 5037 px. map-preview.png — whole 1536 × 1259 px. map-details.png — close-up Lórien, Mirkwood and Fangorn, from the left. details-*.png — separate end-out-sections PNG, without regenerating. map-composition.svg — accurate contours and reproducible layers of the illustration.

The scope of the correction Lórien retains the position accepted in MAP-04, but has larger tree crowns and clearer lines. Raw imagegen drawing complements in SVG strengthening the coverage of the line about 25%; background white remains transparent.

Mirkwood is one layer on a total of seven polygons: Western Mirkwood, Woodland Realm, Old Forest Road, Narrows of the Forest, Eastern Mirkwood, Dol Guldur and Southern Mirkwood. Trees pass through the internal boundaries; only the outer contour has a small space. There are seven separate clumps.

The entire former Fangorn, also fragments in neighboring fields, removed with a local imagegen background layer. The new forest occupies the eastern part of the proper region, according to the original board. The mountains and rivers remain surrounded by the forest. Previous maps and all source outlines are preserved unchanged.

Sources and promotions User's comments from 2026-10-06, two drops of Fangorn from 19:22, earlier reference Mirkwood, original board  and pencil- v2.   inputs.json pin 20   SHA-256 sources. forest-layout.json provides regions, staff and shape of Fangorn. Grey shape.png are auxiliary outlines, not part of the map.

Built-in imagegen, without CLI/API. Four selected rasters and promptets:
- mirkwood.prompt.txt / mirkwood.png — a single continuous release.
- fangorn.prompt.txt / fangorn.png — a compact forest in the right field.
- fangorn-cleanup.prompt.txt / fangorn-cleanup.png — removal of the old forest.
- lorien.prompt.txt / lorien.png — larger, clearer trees.
provenance.json indicates the originals in generated_images; they are left in place. There are self-copying resources in the repository.

Composition and verification compose_map.py expands the composition MAP-04 Clips SVG limit trees to the right regions; Mirkwood is cut once after a total of polygons, without gaps at internal borders. The original boundaries are above the forest. Python prepares SVG and reads pixels for measurements; does not paint rasters.

verification.json: PNG has R=G=B, 105 regions and 323 boundaries paths have identical attributes, all 20 sourced hashs consistent, full frame preserved.forest-verification.json: 847795 visible pixels of forest layers in the right fields; zero outside. Mirkwood takes 613/613 sampled cells of the field interior and 260/260 samples along 10 internal borders. This measurement of the bar deployment, rather than the declaration of the continuous black surface. With the same rendering scale, the lower brightness quatyl of the Lórien bar changed from 127 to 73. The view confirmed larger, clearer crowns. Besides the three local PNG windows, the same pixel with the pencil-v2.

Python playback and packages with ../pencil-v2/requirements.txt, Node and resvg 2.6.2: e.g. --prefix graphicalassets/maps/pencil-v2 python3 graphicalassets/maps/pencil-v3/compose_map.py python3 graphicalassets/maps/pencil-v3/inspect_map.py Inspecting image crops uses macOS sips. prepare_references.py plays the input to imagegen; export the finished map does not re-create the model. In this session, NODE_PATH=/tmp/wotr-map04-render/node_modules and Python /tmp/wotr-map-trace-venv/bin/python. The first two reference attempts without NODE_PATH did not find the package; after the existing installation was indicated, export worked. The variant with the same dimming of the old Lórien was rejected.

Restrictions The sub-bed still has nativeity 1385 × 1136 px. New rasters: Mirkwood 947 × 1661 , Fangorn 1183 × 1330 , Lórien 1254 × 1254 Fangorn background 1229 × 1280 Big PNG does not increase the detail of the unchanged foundation. This illustration, without changing the movement data and without integrating Unity. Coloring is a separate step; after MAP-05 no further stage A graphic task.
