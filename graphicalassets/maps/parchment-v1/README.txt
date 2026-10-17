MAP-07 — first map on old paper, 2026-10-07

Result ../middle-earth-parchment-v1.png — map 6144   ×   5037 px on yellow paper. map-preview.png — whole 1536   ×   1259  px.  detail-minas-tirith.png  /  detail-east.png — Final clippings PNG.  map-linework.png — an improved line on a white background, before styling. map-composition.svg — a self-contained composition with embedded rasters.

The range of the redundant forest was removed at the very right edge, including two small trees. On the western tip of Minas Tirith two small peaks were added, connecting to the existing band; the middle and east fields remained open. The adjustment of the mountains is cut to the interior of Minas Tirith with the distance of 2 working units from its borders. The remaining area and earlier river removals were preserved. No names, cities, icons, streams or map ornaments were added.

The Full rectangular sheet has a warm cream-mode shade, fine fibers, delicate spots of aging and darker patina at the edges. The edge is a warm dark graphite. Paper and the colour of the line are separate layers: they do not re-paint the geometry of the map or impose colors on the regions. It is the first version of styling prepared for further remarks.

Sources and promotions User's discretion with 2026-10-07 and a map of the pencil- v4 after MAP-06.  inputs.json pin 30   SHA-256 sources. layout.json provides two correction windows, the exact location of the rasters and the shade of the line. Embedded imagegen was used, without CLI/API:
- minas-mountains.prompt.txt/minas-mountains.png — local mountain supplement; minas-target.png
  input, excrement of earlier PNG with boundaries.
- right-forest-removal.prompt.txt  /  right-forest-clean.png — removal of the forest; entry
  right-forest-target.png Without borders.
- aged-paper.prompt.txt  /  aged-paper.png — a new paper texture.
provenance.json points the originals in generated_images; they were preserved there and copied the selected results to the project. There is no dependence on these external paths during re-export.

Composition compose_map.py extend original SVG  pencil- v4 two local masks. The borders of the regions are above the edited area. Filter SVG replaces the brightness of the entire finished line to cover the warm graphite and puts it on paper. 105 region paths and 323 limit paths retain all attributes. Python prepares native composition SVG and measures pixels, does not paint rasters; all new bitmap fragments created by imagegen.

Verification.json verification: PNG 6144 × 5037 , 105/323 unchanged paths, four identical separate layers of forests and 30 compatible SHA-256 sources. edit-verification.json: 369611 modified pixel drawing, zero except for two windows with anti aliasing margin 0,75 units; 6604 pixels of a clearer line on the Minas Tirith tip. The correction of the mountains is entirely within its geometry. In the area of the right forest the number of dark pixels dropped from 151518 ed 0 When the original region border is switched off, the final background is warm (R>G>B) and the whole PNG is opaque. A full map and both sections were viewed: the forest removed, mountains limited to the tip, the limits readable, the paper consistent, without the re-establishment of the river network.

Python playback with ../pencil-v2/requirements.txt, Node and resvg packages 2.6.2: e.g. --prefix graphicalassets/maps/pencil-v2 python3 graphicalassets/maps/parchment-v1/compose_map.py python3 graphicalassets/maps/parchment-v1/inspect_map.py Exports use the recorded result and does not re-create imagegen. Cut control requires macOS sips. Python /tmp/wrotr-map-trace-venv/bin/python and NODE_PATH=/tmp/wotr-map04-render/node_modules. is used in this session.

An attempt to zoom the clipping by changing the viewBox of the SVG complex caused a resvg error; instead, the existing PNG by sips was cut. The control script improved the transfer of the geometry list to the Shapela and the original limit from the measurement of the removed forest was disabled. Final checks passed.

Limitations and further work Paper and unchanged backing have natively 1385 × 1136 px; both new local rasters 1254 × 1254 px. Export 6144 × 5037 does not increase the depth of the foundation. This is a graphical resource without Unity integration and without changing the rules. There is no next graphic stage A task ready.
