# Fortress drawings on the map — UI-234, UI-256

Every stronghold of the base board is drawn on the terrain like its forests and mountains: Minas Tirith, Minas Morgul
and Helm's Deep (UI-234), then Barad-dûr, Dol Amroth, Dol Guldur, Erebor, the Grey Havens, Lórien, the Morannon,
Moria, Mount Gundabad, Orthanc, Rivendell, Umbar and the Woodland Realm (UI-256, 2026-10-17): ink and pencil
line work in the style of the map's mountains, on a transparent background. They lie in the terrain layer in board
units, so they scale with the map and never hide with the zoom (`MapLandmarks`, `MapTerrainVisual`); names,
settlements and forces are drawn above them. Their places (`Resources/MapCanvas/landmarks.json`: centre and width in
board units, height from the drawing's aspect) keep clear of the region's name and the forces' row
(`MapLandmarksTests`). Where the region's name and forces leave little room (Moria, Rivendell, the Woodland Realm) the
drawing is 30 board units wide and may reach over the region's edge; it never covers a neighbour's name.
Since UI-258 (2026-10-18, owner's request) every drawing stands at least half on its own region, and Minas Tirith
(moved right, 44 wide) and Minas Morgul (in the tip of its region under its name, 30 wide) at least three quarters.

**Generation.** OpenAI Images API (`gpt-image-1.5`, 1024×1024, medium quality, transparent background) with the
user's key from the environment. [Prompts](v1/prompts.json), [generation script](v1/generate.py) and
[journal](v1/generations.jsonl) as for the battle paintings; the key never enters the files.

**Build.** [`build.py`](build.py) crops each drawing to its ink, multiplies its light wash by the map's parchment
(238, 214, 168) so it sits on the terrain, scales it to fit 256×256 and writes
`unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/map-landmarks/<key>.png` with `.meta` files and the
[manifest](v1/manifest.json). `build.py --check` verifies outputs against the manifest.

Replacement artwork, not taken from the original game's components.
