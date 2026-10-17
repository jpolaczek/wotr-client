# Fortress drawings on the map — UI-234

Minas Tirith, Minas Morgul and Helm's Deep are drawn on the terrain like its forests and mountains: ink and pencil
line work in the style of the map's mountains, on a transparent background. They lie in the terrain layer in board
units, so they scale with the map and never hide with the zoom (`MapLandmarks`, `MapTerrainVisual`); names,
settlements and forces are drawn above them. Their places (`Resources/MapCanvas/landmarks.json`: centre and width in
board units, height from the drawing's aspect) keep clear of the region's name and the forces' row
(`MapLandmarksTests`).

**Generation.** OpenAI Images API (`gpt-image-1.5`, 1024×1024, medium quality, transparent background) with the
user's key from the environment. [Prompts](v1/prompts.json), [generation script](v1/generate.py) and
[journal](v1/generations.jsonl) as for the battle paintings; the key never enters the files.

**Build.** [`build.py`](build.py) crops each drawing to its ink, multiplies its light wash by the map's parchment
(238, 214, 168) so it sits on the terrain, scales it to fit 256×256 and writes
`unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/map-landmarks/<key>.png` with `.meta` files and the
[manifest](v1/manifest.json). `build.py --check` verifies outputs against the manifest.

Replacement artwork, not taken from the original game's components.
