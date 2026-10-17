<a id="mapa-z-istniejących-konturów--plan-map-12map-17"></a>
# Map from existing contours — MAP-12-MAP-17 plan

The project with 2026-10-08 after correction of MAP-10. The status of the implementation is only in [Register](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/tasks/tasks.json). MAP-11 receives the documentation; the change of Unity map itself remains to be performed in subsequent tasks.

<a id="rezultat-i-źródła"></a>
## Result and sources

The client map already uses the prepared shapes of 105 regions, with basic, readable graphics. The borders show the area of the impenetrable black, ordinary transitions to white, and the national membership with the right colors next to the passerby boundaries. Final styling, new illustrations and publication do not belong to this package. We keep the rules, saved parties and gestures of UI.

Sources:

- BASE-2.4.1 (rulebook 2.4.1, `wotr-server:docs/sources/materials/base-rulebook-v2.4.1.pdf`, local-only — D17),
  p.9: eight colors of nations; p.10: white boundaries, rivers, black barriers, two colors of
  neighboring nations and water areas. These are existing designations of board.
- Board scan `wotr-server:graphicalassets/highedfmap.jpeg` (local-only, not in git — D17),
  [audit
  data](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/sources/T04-DATA-AUDIT.md)
  and
  [base-board](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/content/base-board.json):
  names, nations, settlement, 105 ID and 232 uncomplicated connections of ordinary motion.
- [geometry.json](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/graphicalassets/region-map/geometry.json),
  [map-regions.svg](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/graphicalassets/region-map/map-regions.svg),
  [README](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/graphicalassets/region-map/README.txt)
  and mask/seed points and audit contacts in `graphicalassets/region-map/reference/`.
- [Composition MAP-10](../../graphicalassets/maps/parchment-v4/README.txt): accepted paper and
  terrain, improved Black Gate and Erebor.
- [T32-SETUP](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T32-FATE-SETUP.md),
  [EXPANSIONS](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/EXPANSIONS.md)
  and
  T28:
  public Fate/New Cities and White Hand caps.
- [UX_SPEC](UX_SPEC.md): gestures, empty regions, own controls, zoom, tags, panels and privacy
  remain a common contract.

<a id="stan-zastany-i-granica-danych"></a>
## State of state and data limit

Unity loads `content/map-geometry.json` geometry-1: approximates the 1920 × 1324, single Polygon and Anchor. `MapCanvas.Draw` draws the outline of each field separately and the color of the fill comes from page control. This is not yet a map from the contour file. New geometry requires open ring/hole support and common border sections.

Source viewBox is [335, 40, 1632, 1338], PNG has 6144 × 5037 pixels. `number` in geometry indicates mask label, not table index; 0 means water or the outside. `point` in region-seeds is a workspace — it is not allowed to confuse it with historical `seed` on a different scale. Importer documents one transformation to client coordinates, used by all layers.

Reading from 2026-10-08: 323 contour path, 282 different pairs of contiguous regions, including all 232 legal pairs and 50 additional contacts. Geometric contact does not give pass permission. `impassableBorders` in base-board has only four selected pairs; it is not a complete list of sections to color. MAP-13 must check all contacts and their fragments with the source. Some contours under the mountains were interpolated; this fact does not become the source of the new connection.

We separate three things: field shape, permanent geographical border passivity and the legality of the current action. The latter still comes from the option and validation of the server; the white border does not promise the legal movement of a particular army at the constraints of politics, capacity or cost. The temporary ban does not paint the geographical border black.

<a id="wygląd-granic-i-paleta"></a>
## Borders and pallet appearance

| Kind | Appearance and meaning |
| --- | --- |
| Non-transitable section between terrestrial regions | A clear, continuous, thicker black line. The color of a nation or a highlight cannot look like a passage through it. |
| Passing the border within one nation or between neutral regions | White line with a clear width. |
| Passing the border of the nation with neutral territory | White line and pass color on the inside of the region of a given nation. |
| Passing the border of two different nations | Two appropriate bands, each on their side, separated by a white line; without mixing into one color. |
| River forming the border | Only the existing section of the border, the clear white banks; the fact of the river does not block the passage. We do not draw rivers through the interiors of the fields. |
| Lake, sea and exterior | Separate calm tone water/Background, no regionId and hit the field. The shore is not a traffic connection. |

| nationId | Base color according to s.9 |
| --- | --- |
| dwarves | brown |
| elves | light green |
| gondor | dark blue |
| north | light blue |
| rohan | dark green |
| isengard | yellow |
| sauron | red |
| southrons | orange |

Shades are interchangeable tokens of presentation. MAP-13 saves a specific palette and contrast; we do not replace eight nations by the division red/blue FP/Shadow. The color of the current conqueror is a separate control marker.

Each common section has a stable identifier, both owners, type, orientation of the pages and the source of classification. In case of a partial barrier we share a path at a confirmed passage point. Legal couple must have a clear passage passerby; it is not enough to consider the whole pair as a passerby when part of it runs through the mountains. Black Gate, Minas Morgul, Moria and High Pass require separate approximation and comparison with the graph. We do not add connections from the very similarity of the drawing.

<a id="warstwy-i-wydajność"></a>
## Layers and performance

Order: paper/l → existing terrain → delicate fillings choice/Legal goals → common boundaries with bands of nations → markings action , figures , sediment and control, and tekst/panele. The highlight has its own symbol or filling and does not remove information about the barrier. Simple legend on demand explains the difference between the border and the current legal target.

Full PNG parchment-v4 has already burnt uniform contours. We do not put new colours on them as a second contour. From the composition we export paper/area without layers of regions/boundaries, in the same viewBox, and the limits are drawn from the data. A quiet replacement graphics are to be readable before further styling; the decorations do not obscure two corrected places.

The importer checks the fields, holes, anchors, contacts and scale; we do not perform the recognition of PNG or the parsing of SVG in each frame. Geometry, spatial index and grids have cache. Colour control on the overlay updates only affected regions and their edges. Any simplification arises once from a common edge, with documented error; separate simplification of two fields cannot create gaps or close passes. Selecting the renderer and memory cost justify the measurement, not the only number of lines.

<a id="interakcje-dodatki-i-zapis"></a>
## Interactions, expansions and save

Drawing, hitting the cursor, anchors, routes, drop, Location and panels use the same transformation. Seas, lakes, ground holes and the outside do not choose the region. The point on the common line has a stable, documented result, without flashing between the fields. The camera continues to zoom in under the cursor.

Hover each area, empty or occupied, only gently highlight the region; it does not open a description or panel. Click or Enter/Space shows his information, and click the legal target with active intent takes the existing step. From UI-80 passive description of the specific object/controls is reported after 200 ms howera or immediately after seal, using only the existing allowed data. It does not download data, capture gestures and does not open a window with options. Exit, lose focus and start gesture only saves a description. The clicked window remains open, retains a position in the session and the moved header; Close/Esc Esc also extinguishs the tooltip without picking up the window closure. Details: [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md) Own and hostile objects retain various controls, public Fellowship Your visibility. field/garrison are separate; small and concave regions remain available under markers. The map does not change the reception of the new revision.

Fate changes the national colour of Erebor to Sauron, Dale to neutral, and settlement Iron Hills to City. New Cities changes the symbols of the settlement. Kings White Hand can change the current national membership of the region according to the public RegionOverrides; the military self-taking control does not do so. The client does not reconstruct the rules of expansions from flags or colors.

Game recording, command, projections and history continue to use regionId, without coordinates as identity. When changing the geometry version, you can rematch the camera and recalculate local anchors; you need to keep the correct draft, the paid sequence, revision and uncertain commandId/receipt. Hidden position/route Fellowship cannot appear in new layers.

<a id="scenariusze-odbioru"></a>
## Acceptance scenarios

| ID | Owner | Required result |
| --- | --- | --- |
| MC01 | MAP-12 | Exactly 105 same ID, version and transparent transformation; reproducible export. |
| MC02 | MAP-12 | An anchor of every field on land; holes/lakes/Outer the effusions do not belong to the field; 232 Connections preserved. |
| MC03 | MAP-12 | A damaged ring, foreign/repeated ID, version or scale dispersal stop import with a clear error. |
| MC04 | MAP-13 | Each of the 323 The source contours and all 282 the pairs are classified; 50 additional contacts do not become transitions. |
| MC05 | MAP-13 | Black Gate, Minas Morgul, Moria and High Pass: Confirmed Passes and adjacent Barriers in accordance with board; Partial Separate Barriers. |
| MC06 | MAP-13 | Eight colors, white internal/neutral lines, two nations on the right sides; the reversal of the path does not change the colour in places. |
| MC07 | MAP-14 | Renderer uses layers without burned borders; open Black Gate, single Erebor, no new rivers/decoration and double lines. |
| MC08 | MAP-14 | White/colour and black distinguishable limits after zoom, at nodes and after zaznaczeniu/ataku/protection/attention. |
| MC09 | MAP-15, UI-80 | Hover terrain only backlight; click/Enter/Space opens empty/zajnit region with the right controls. Hover/fokus object shows only passive description, without panel or command. |
| MC10 | MAP-15 | Click/drop movement, attack, recruitment, retreat/Intrusion and selection cards indicates the same field as the drawing, also in a small region and panel. |
| MC11 | MAP-15 | Fellowship, characters, field/garrison, route and anchors remain in the respective regions; zoom and camera traffic do not change the draft. |
| MC12 | MAP-15 | Location and summary action/log use new anchors; entering a new revision or reconnect does not send gestures or reveal secrets. |
| MC13 | MAP-16 | Base/Fate/New Cities change colors correctly and settlement; graph and black barriers remain the same. |
| MC14 | MAP-16 | The usual capture does not change national color; White Hand changes the right bands from the projection, retaining the setup property for scoring. |
| MC15 | MAP-16 | Overlay withdrawal, batch change and read older recording without old colors/ anchors; receive and paid-for steps retained. |
| MC16 | MAP-17 | Viewing the four systems 1366 × 768/1920 × 1080 × 100/125% , mice/g smooth, fit/close ginia; small fields and colours readable with symbols as well. |
| MC17 | MAP-17 | Player: local feedback ≤100 ms, 60 FPS target in 1080p on the stored equipment; memory report, apex, loaded scene and cache. |
| MC18 | MAP-17 | Two actual clients/server, pre-exchange record, restart/reconnect, base and expansions; build Windows/macOS and the available Player smokes. |

MAP-17 requires proof of all MC01–MC18, not the screenshots themselves. New tests should be written; filters without new cases are not acceptance. Windows runtime, manual acceptance user and publication retain the existing limit with REQUIREMENTS and are not declared by build.

<a id="wdrożenie-importu--map-12"></a>
## implementation Import — MAP-12

Implementation descriptions and results below MAP-12 – MAP-17 come from 2026-10-08 UTC before UI-80 . They retain the gestures and test results of the day; the current hover and opening standards are defined in the section « Interactions , expansions and record’ above. These results do not count acceptance new behavior of tooltips and windows.

`contours-2` replaces geometers- 1 : 105 ID , 106 rings, 26 455 points without re-closure, 323 original paths and 14 348 unique episodes. All 232 legal couples have contact; additional 50 ed 282 physical pairs remain only geometry. pinned entrances are in [MAP-12-GEOMETRY](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/sources/MAP-12-GEOMETRY.json) Import does not simplify episodes again. Previous source tolerance 0,6 The working pixel and the reading limitations under the mountains/panels remain public.

Rings and edges use offset (x− 335 ,y- 40 ) and anchors of the same conversion `point` , without historical `seed` All anchors are closely within the inland part of the land. The only hole of the polygon, in Gorgoroth, retains a contact with the outer ring at one point. The sea and the cut do not choose the region exactly on the common land line. The draw picks smaller ID Ordering the original, regardless of the order of the source. The incorrect resource stops the loader; it does not restore the approximate map.

The client layout is 1632×1338. PNG 6144×5037 uses `xMidYMid meet`: scale min(6144/1632, 5037/1338), about 0,108 px horizontal margin. This information is stored by DTO, instead of independently extending the axis. Drawing and hit-test use the same rings; coloring of common borders and graphics without burnt contours remain within the range of MAP-13/MAP-14.

<a id="map-13--katalog-granic-i-paleta"></a>
## MAP-13 — border directory and pallet

W `borders-1` all classified 323 contours: 360 parts, including 225 Passage, 8 River, 86 Impassable, 17 Coast, 11 Lake and 13 Frame. 33 paths have a division, so the white pass does not remove the mountain fragment of the same legal couple. Nurn/Gorgoroth This is an example of two separate contacts: Western `0212` It's got a passageway, east `0213` remains black. Audit includes 9 sectors in general board and the approximation of gates, Moria, High Pass and Grey Havens/Ered Luin; [evidence](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/sources/map13-review/README.md) also show inherited route restrictions MAP-12 .

`map-style-1` defines interchangeable shades of eight nations according to BASE p.9. The band takes Nation from the right normal side, never Control. Biel separates two different colors and the inner contact of one nation remains white. Working widths: the crossing 1,5, the barrier 3,2, the band of the nation 3,0. The black bar has a contrast 9,066 relative to the background, the white 8,620 relative to the dark outline. Bright national bands themselves do not constitute a contrasting legal sign action; dark outline and white are separate tokens. These values do not replace the view by zoom and measurements of the MAP-14/MAP-17. renderer.

Normal comes from a directed ring; reverse path replaces the names positive/negative side, maintaining the physical color of the field. Sea/Lake/frame are presentation types, without neighbors and right click. Importer and runtime rejects lack of class, overlapping ranges, lack of origin, inconsistent hash or white transition outside the T04 graph. The directory is ready for rendering in MAP-14; MAP-13 does not yet count MC07–MC08.

<a id="map-14--wykonanie-mc07mc08"></a>
## MAP-14 — MC07-MC08 performance

The map uses source contours and separate paper/area MAP-10 without burnt regions/boundaries. Water is formed outside the sum of land contours and in its holes. It does not become a region of interaction. Passes have a common white middle and colored bands are cut on the right side to the nation of the owner. Black barriers are drawn after crossings, also on nodes. They do not depend on the selection, legal purpose or control of settlement.

The source/selection/target/attack/protection delicate fillings remain under the borders. Attention has a separate symbol. Control settlement uses the Free Peoples triangle or the diamond Shadow; it does not overwrite the color of the nations. The markings of sediment and control remain visible in fit; units retain the earlier rule zoom/hover. Button “?” opens the English legend of the line, eight nations, sediment and control; it itself does not choose the region. The legend is the overlay over the map controls, with a compact pallet in up to two columns; all names and samples remain available in four layouts.

acceptance includes actual pixels of transitions and fillings, no regeneration of static boundaries in interaction, close-up of both gates of Mordor, Erebor, Moria, High Pass, water, two nations and nodes, and images of the final Player Development. Layers and cache describe [procedure renderera](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/sources/MAP-14-RENDERER.md), and actual results record MAP-14 Task. acceptanceThe gesture and variant mechanics still belongs to MAP-15/MAP-16, a complete matrix to MAP-17.

<a id="map-15--dowody-mc09mc12"></a>
## MAP-15 — evidence MC09–MC12

`MapContourInteractionTests` 5/5 and `MapContourNavigationTests` 2/2 PASS on the real server. Results: `.artifacts/map15-interaction-final.xml/log` and `.artifacts/map15-navigation-final.xml/log`. The first fixture error (non-existent `belfalas`) was repaired by existing `dol-amroth`; the first problem with the obscured Old Forest solves the open pan test with the behavior of pinned and the position of card. The hit test has not been loosened.

All 16 drops of the common edge Buckland/Old Forest (both roles × four layouts × two sides) as well as representative samples of the concave region, the small Osgiliath and the coast of Dol Amroth. Results correspond to ID from the source contours; the connection and revision change during the drag do not allow to send the old action. The 64 PNG is stored in an isolated checkout `.artifacts/screenshots/map15-*.png`. The matrix assigns all 13 from the target families and explains why the other 18 from the families does not choose the site: [Coverage MAP-15](MAP15_INTERACTION_COVERAGE.md).

No hand batch or Windows client launch was performed. Evidence of variants and durability includes MAP-16, and the final acceptance MAP-17.

The final regressions of core/routes/abilities have passed 14/14, 7/7 and 4/4; together with new attempts this gives 32/32 PASS. Full filters and results are in document MAP-15.

<a id="map-16--mc13mc15-narodowość-osady-i-wznowienie"></a>
## MAP-16 — MC13–MC15: nationality, settlement and renewal

The band colour means the current public nationality of the region. A separate symbol of control shows the acquisition of Shadow in Helm’s Deep still sees Rohan until the public cover White Hand changes its nationality to Isengard. Removing the contaminated Theoden restores Rohan's colour, but does not receive the gained settlement or its 2 points. Fate changes Erebor to Sauron, removes North from Dale and shows City in Iron Hills; New Cities shows City in Ered Luin and South Rhûn without changing their nationality.

Both views of the player use the same public data. Return to the database after Fate/Kings, reconnect and resume the record do not retain the old overwrites. None of these steps calculate the cost or perform the action. The camera can be matched after the creation of the new client; the correct selection of figurine/regions and the paid sequence remain preserved.

<a id="map-17--dowody-końcowego-odbioru-mc01mc18"></a>
## MAP-17 — final evidence acceptance MC01–MC18

[Receipt report](MAP17_ACCEPTANCE.md) connects source checkers, 478 editor cases, 441 unique Play, 3723. NET, separate HTTPS/WSS, buildy  macOS/Windows and viewing 42   PNG the real Development Player. The result applies to `contours-2` / `borders-1`:  105 fields, 323 paths, 360 classified parts, 232 legal couples and 50 contacts without transition. Legality remains in the rules and options of the server; color or physical contact does not create movement.

The Morannon, Minas Morgul, Moria and High Pass passes, Erebor retouch, Grey Havens coast and Nurn water before and after the highlight are checked. All eight colors of the legend remain legible in four layouts. The map has full fit; the source rectangle ends in the east, and the anchors and southern settlement are not cut off. A separate limit remains the HUD and pinned covers of windows, described with each set of images in the report.

[Profile macOS](profiles/map17-macos.json) includes 36 workloads on M1 Pro: minimum 238,48 FPS in 1080p, maximum p99 8,856 ms and feedback 17,808 ms. This uncapped Development Player, does not monitor frequency or Windows. Texture occupies 11 185 272 B; the range of geometry counter and global buffers is described separately. Pan/zoom/hover/highlight/projection After the movement they do not rebuild static boundaries, Fate changes 9 band, White Hand 2 . acceptance is not a publication or approval of the final styling. 2026-10-14 In UI-132 ( [profile](profiles/ui132-macos-map17.json) ), with army figures, belts UI-143 and Mordor track: minimum 168,6 FPS , feedback 13,8 ms; 1920×1080 in a full screen window on the 1080p monitor. Comparison: [ARMY_MAP_UX](ARMY_MAP_UX.md) ‘, acceptance UI-132 I am not a man of God. ”

<a id="pasy-nacji--ui-143"></a>
## Nation Belts — UI-143

User notification from 2026-10-14: colour strips between regions were "very thick, single-colour, flat and non-matching". From UI-143:

- Width pass Nation `nationWidth` in `map-style.json` (server content) drops from 3,0 to 2,3 map
  units; `map-border-bands.json` Regenerated (`export_map_border_bands.py`, Shapely  2.1.2  /  GEOS
  3.13.1, as in MAP-14 Borders, passages and graph of motion unchanged. 1,6 gave the full view of
  the map lines too thin to distinguish the nations.
- pass is two-tone in the tone of the nation: translucent, brightened towards the paper medium
  (`WashTowardLand` 0,12, opacity 0,88) and darker edge of the same colour (`EdgeShade` 0,58), width
  line 1,6 keyline along the contour pass. The grey keyline border around pass is no longer present;
  the ink remains only on the transition line. All solid are in `MapBorderPalette`, drawing in
  `MapBorderVisual` (`MapVectorDrawing.Outline`), the legend shows a sample like pass (fill and
  edge).
- The bands are rebuilt only when the owner is changed; the panorama and zooms do not re-paint them.
- Second application from 2026-10-14 : Black mountain barriers “look like a marker”. `Impassable` )
  is now a line with a feather: thin core of warm, dark ink ( `BarrierCore` 0,4 width `barrierWidth`
  colour `barrier` moved by 0,2 towards paper, cover 0,9 ) over soft shadow ( `BarrierShadow` 0,9
  width, cover 0,26 First all the shadows draw, then the cores, so the cross-resistances do not dim
  the ink. The barrier is still darker than the keyline and differs from the white passages.
  `map-style.json` The following table is inserted after the table: `MapBorderPalette` , drawing in
  `MapRenderer.DrawBarriers` (one vector layer, without cost on panorama).

Drops: `ui126-map-fit-*` and `ui126-map-zoom-*` (MapVisibilityTests) in four systems.

Profile map17 before and after (2026-10-14,  [summary](profiles/map17-ab-ui143-ui156.json)): Development Player with Tools UI-132 on a basis `e71a226` (content 1.0.3,  `nationWidth`   3,0), on `b2aca35` (belts), `7c1c5c4` (barriers) and after the Mordor track, alternately. acceptance No change: minimum 168–173   FPS, feedback ≤   16 ms, static belts do not rebuild, projection and reconstruction Fate/White  Hand  0,99–1,01 Cost: vertices 915 thousands. →   999 thousand (two-tone belts) →   1052 thousand (thin and barrier core), idle frame and at the cursor longer by 5–13%  (ok.  0,1  ms),  p99 When the cursor moves in 1366×768 of ~ 1,5  do ~ 5–6,5 ms. User accepted this cost 2026-10-14; the risk of weaker equipment (Windows, Intel Mac) remains open.

<a id="tor-mordoru--plan-ui-152"></a>
## Tor Mordor — plan UI-152

Implemented in UI-154. Vector layer “map-murder-track” draws Mordor track with ink of barriers from UI-143 over Gorgoroth area, with geometry from presentation data `Resources/MapCanvas/mordor-track.json` (UI-153). The track is not a region and does not change contours, pass or graph. Rebuild only on track state change. Details: [FELLOWSHIP_UX](FELLOWSHIP_UX.md), F07a.

<a id="kolory-i-faktura-płótna--map-18"></a>
## Colors and texture of canvas — MAP-18

The 2026-10-14 (UX_SPEC, “Pergamin by candle”) application. Only the `Resources/MapCanvas/terrain.jpg` and `margins.jpg` textures change. Geometry, contours, lanes, passages, Mordor track and their data remain unchanged.

**Export.**
- `scripts/export_map_canvas.py` calls `scripts/map_canvas_grade.py` module.
- Order for margins: paper → ton → flaws → vignette UI-102 → water → candle.
- The area has the same steps, and the tone is colored.
- All layers of paper (value noise, fibers, foxing and grain leaks 1818) are a function of board
  units. Both textures have the same paper at the interface.
- The coloration fades in the last 60 units at the edge of the map, so the contact does not change.
- The average difference at the contact is 1,4/255 in the north, east and south, as before the
  change.
- `--preview <katalog>` saves copies of 1024 px without moving results.
- The `docs/sources/UI-102-MAP-CANVAS.json` report has a `grade` section with parameters and hash
  `map-geometry.json` and module.

**Masks of colouring** (1 pixel per unit board, double-lined):

| Element | Mask Source | Colouring |
| --- | --- | --- |
| The Dark Forest | Ink of `mirkwood-art` image from MAP-10 composition cut down by its cutting path. | Dark green lawowanie. |
| Fangorn, Lothlórien, Old Forest | Ink images `fangorn-art`, `lorien-art` and `forest-art`, cut the same way. | Brighter green. |
| Lindon Coast Trees (Harlindon, Forlindon, Grey Havens) | Dent ink with low consistency of the line direction (corons, not mountain dashing). Without regions of Ered Luin and fine spots. | Brighter green. |
| Mordor | The Mordor regions and the adjacent mountains, enclosed in one area. | Ashed discoloration and dark lawing, the strongest in the depths. |
| Mountains | There were thick ink outside the woods. | Grey: ink hard, paper under it weak. |

**Readability of regions' overlays.** Dark lawing reduces the contrast of the translucent `MapRegionVisual` caps. Example: Red attack (0,28 alpha) must lower the green and blue pixel channel by more than 8 (`MapContourRenderingTests`).

The first version with darker Mordor did not pass this test on Morannon. Therefore, the lawing of the Forest and Mordor have a threshold (`WASH_FLOOR`):
- paper does not darken below the threshold colour;
- Shadowing darker than the threshold rises to it by 0,6;
- the lines of drawing (luminance below 35) remain unchanged.

Mordor's threshold takes into account the dimming from the candle in the southeast.

The share of pixels on which the red attack meets the test:

| Region | Before MAP-18 | MAP-18 |
| --- | --- | --- |
| Morannon | 0,96 | 0,99 |
| Minas Morgul | 0,96 | 0,99 |
| Nurn | 0,96 | 0,97 |
| Gorgoroth, Barad-dûr | 0,99 | 0,98–0,99 |
| Dol Guldur | 0,89 | 0,75 |

In the Forest (Dol Guldur) red gives mainly a clear shift of colour from green to brown red. The sum of channel changes exceeds 24 levels on each pixel. Channel test meets a smaller part of the pixel, because dark green has little blue to lose.

**Why ink of images, not ink density on board.** The forest-cutting paths also include the slopes of the Mistky Mountains. The mountain-trees are similar in brightness and saturation, so it is the ink of the forest itself that separates trees from the mountains. The lava shore is slightly jagged and darker, as in the watercolour.

**Verification.**
- `export_map_canvas.py`, And then `--check`.
- EditMode.
- PlayMode: `MapContourRenderingTests` (import BC1 and texture memory), `MapCanvasLayoutTests`,
  `MapVisibilityTests`, `MordorLayoutTests`, `MapViewportEdgeTests`.
- Mordor's track has a bright foundation, so it becomes readable on darker Gorgoroth.

<a id="skóra-pergaminu--map-19"></a>
## Parchment leather — MAP-19

User correction from 2026-10-14: forests, sea, Mordor and mountains are good, but the paper was not saturated enough and not enough like parchment.

** `Parchment.skin` in `scripts/map_canvas_grade.py` works after the MAP-18 flaws, and before the vignette and water, on both textures.
- color deepening (`DEEPEN`) towards gold-ochre;
- cloud-like lobes from twisted high contrast noise: darker amber and brighter cream (`CLOUDS`);
- long soft bands as in the skin (`STREAKS`);
- fine dark spots on the spreadable grid (`SPECKS`);
- reinforced own paper grain ( `SHARPEN` Blur counts solely on paper, and reinforcement has a limit
  14 levels, so with forests and drawings, no clear border is formed.

The layer does not touch areas that remain like in MAP-18:
- water (`dry` from sea and lake fields);
- forest and Mordor lavating (softly, from the lava power of 0,55 to 1);
- mountains (mountain range × 0,9).

Creamy lobes and grain lie only on bright paper. Colour, amber lobes, bands and spots also reach the shadows of the drawing, so there is no pale surrounding around the hills.

**Measurements.**
- Texture contact: average 1,7–2, 0/255 (p95 4,0–4,7), without visible suture on the eastern edge
  approximation.
- Red attack in regions with `MapContourRenderingTests` (pixel share):

  | Region | MAP-19 |
  | --- | --- |
  | Morannon, Minas Morgul | 0,98 |
  | Nurn | 0,96 |
  | Erebor | 0,94 |
  | Moria, High Pass | 0,98 |
  | Grey Havens | 0,85 |

<a id="nazwy-regionów--ui-186-2026-10-15"></a>
## Names of regions — UI-186 (2026-10-15)

User application from 2026-10-15: "let's add (well stylized) region names on the map".

- The `MapRegionNames` layer lies on the ground, under the symbols of sediment, tags and panels. It
  does not take the indicator, so click, the lighting and the region panel work as before.
- Style (`Styles/MapNames.uss`):
  - capitals Cinzel SemiBold (title cut from TYPO-01);
  - dark ink `rgb(52, 31, 13)` with soft parchment light (`text-shadow` 3 px);
  - the 1,2 shot,
  - Fortresses and cities slightly larger (12,5 instead of 11 units board to the height of the
    letters) and darker.
- The name point (`MapRegionNamePoints`) lies within the region, as far as possible from the borders
  (14×14 grid and clarification), outside the Army site over settlement or on the region's point
  without settlement. The priority takes place under settlement, as on board.
  - Counted once per session for the entire map, approx. 150 ms. Distances are calculated on
    simplified contours with tolerance 1 units (Douglas-Peucker, c. ¼ points); without this 0,6  s.
  - On the board basic, no name stands in the army's place.
- Size grows with an approximation to 15 pt (part of the item, without the layout being
  recalculated). The name is hidden when:
  - is less than 8 pt;
  - not located in a region horizontally or vertically;
  - exits the view;
  - harasssssssssssssssssssss to the army tag, Fellowship or cover panel (simple corners from the
    tag placement).

So, on the whole map of a small screen (1366×768 at 125%) the names of fortresses and cities remain.
- The long name of a few words, wider than the region, breaks into two lines at the middle space
  (e.g. “Narrows of / the Forest”).

