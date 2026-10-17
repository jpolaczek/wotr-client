MAP-04 — Field matching

Result ../middle-earth-pencil-v2.png — revised map, 6144 × 5037 px. map-preview.png — whole map, 1536 × 1259 px. map-details.png — six closeups cut out of the final PNG; order: Minas Tirith, Erebor, Minas Morgul, Moria/Lórien, Old Forest, Dark Forest. map-composition.svg — the existing contours with local layers of correction.

The White Mountains were moved back to the western edge of Minas Tirith. The lonely Mountain was placed inside Erebor. In Minas Morgul and Moria, there were visible gaps in the ridges; Moria signals a small entrance to the rock. Lórien, Old Forest and seven fields of the Dark Forest have separate clumps of trees, matching polygons with a distance from the borders. In Old Forest Road, the group is flattened to a narrow pass forest. Other locations of maps remain preserved.

Sources Notes and six pairs of user discharges from 2026-10-06, hour 18:14–18:21, the previous pencil- v1,  board highedfmap and outlines MAP-01. Reference  board-reference.png preserves the location of objects from the original board in the layout of six clippings; its names, colours and symbols are not copied. inputs.json pin SHA-256   16 enter, including the previous map and rules data.

The Prompts and Rasters Used an embedded imagegen, without CLI/API. The originals from the generated_images directory are left and the selected copies are in this directory. Details: provenance.json.
- correction.prompt.txt  /  corrections-atlas.png: corrections of mountains and passes.
- forests.prompt.txt  /  forest-atlas.png: nine separate clusters of forest.
- moria.prompt.txt / moria-passage.png: transition and subtle entry.
- cleanup.prompt.txt / clean-background-atlas.png: removing old objects.
- lorien-cleanup.prompt.txt / lorien-background.png: whole old forest of Lórien.
The other PNG with target, guide, shapes and reference names are editing inputs.

Composition and control compose_map.py uses the existing exporter of petil- v1 for reading and comparing geometry. New images are embedded in SVG only once; clips, transformations and masks restrict their use to indicated locations. The luminance filter allows you to put trees without white rectangles against the background, and the grey filter provides R=G=B. Soft masks combine local fixes with the background. The site was not painted in Python; Pillow/numpy they are for reading and measurement.

Visible forest pixels (clearness <240) are checked against a polygon field less 4 working units. Erebor has a margin of 7 units. Additional clip inside each field also includes brighter tracks. Checked 430639 forest pixels and 7809 mountain pixels Alone: zero outside the intended field. Contours 105 regions and 323 borders have unchanged attributes. Final report: verification.json.

inspect_map.py compares the previous and new composition with the same renderer. Apart from six correction windows, there is no difference greater than 1/255, resulting from the rounding of filters. The control retains this result, instead of declaring the identity of each pixel. locality-verification.json record the result; map-details.png shows the actual export cuts.

Play Python with requirements.txt, Node.js and @resvg/resvg-js 2.6.2 with package-lock.json: imci --prefix graphicalassets/maps/pencil-v2 python3 graphicalassets/maps/pencil-v2/compose_map.py python3 graphicalassets/maps/pencil-v2/inspect_map.py

Close-up control uses system sips au macOS. Package during operation Node installed in /tmp/wotr-map04-render and uruchamiano powy lower scripts with NODE_PATH =/ tmp/wotr-map04-render/node_modules and Python from temporary vev. prepare_references.py plays three initial atlases with pinned sources. The composition is not re-created by imagegen.

Limitations and export attempts The MAP-03 sub-system has natively 1385 × 1136 px; corrections/t wrong 1536 × 1024, and the forests and both local sections of 1254 × 1254. The larger PNG does not increase the depth of the foundation. The composition remains an illustration, not traffic data. The engine and the client of Unity have not been changed.

Quick Look exceeded the limit on complex filters, so the final export is resvg. A single atlas sample with six nested instances of the map caused a renderer error; the zooms are created by the final cut of PNG. CairoSVG was used only for the preparation of references and work preview.
