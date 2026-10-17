MAP-08 — Subtly Darker Mordor, 2026-10-07

Result ../middle-earth-parchment-v2.png — map 6144   ×   5037  px.  map-preview.png — preview 1536   ×   1259  px.  detail-mordor.png — Final clipping PNG.  map-composition.svg — composition with embedded rasters.

Change the darker tone of paper under Morannon, Minas Morgul, Barad-dûr, Gorgoroth and Nurn. The mask connects these fields without interruptions at their internal borders, retains the cut of the lake and has a soft edge. The darker paper has 50% cover; the current texture remains visible. The whole layer of the bar, including mountains and boundaries, is identical to MAP-07.

Sources and restoration Used built-in imagegen to edit earlier aged-paper.png. Full prompt: mordor-paper.prompt.txt. Result: mordor-paper.png.  provenance.json indicates the preserved original in generated_images. inputs.json pin four SHA-256 sources, a layout.json contains fields, cover and mask parameters. Python folds native SVG and reads pixels; does not paint rasters. Export does not re-create imagegen.

Python with packages ../pencil-v2/requirements.txt , Node and resvg 2.6.2 : e.g. au --prefix graphicalassets/maps/pencil-v2 python3 graphicalassets/maps/parchment-v2/compose_map.py In this session u rye NODE_PATH =/ tmp/wotr-map04-render/node_modules and / tmp/wotr-map-trace-venv/bin/python. Exclude preview requires macOS sips.

Checks verification.json Confirms 105 regions unchanged, 323 border paths, identical complete bar layer and four compatible hashe sources. Changed 1749471 pixels; except for Mordor with anti aliasing margin 0,75 the unit has no change. Median decrease in brightness in its interior: 26, 79/255, i.e. 12,54%. The whole and the cutout were viewed: the tone is toned down, and the mountains, boundaries and texture remain clear. The first reading of the mask required the operation of the second loop SVG in Gorgoroth (lake); parser takes into account the principle of everodd without changing the source paths.

Restrictions The native invoice has 1385 × 1136 px, just like the previous background. The remaining detail is preserved. The resource is not integrated with Unity; it does not change the rules or data. No next ready stage A task.
