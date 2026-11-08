# Tabletop figures — UI-421 (2026-10-29)

Owner request (2026-10-28, translated): "Units and characters feel detached from the table, like icons drawn at fairly
random spots on the map. Figures and units should be real objects that belong to the map, which the player can actually
pick up from the table and carry somewhere else — and the lifted thing is that object, not a square or its shadow."

This document is the one design the tabletop-figures set implements: UI-422 (stand places), UI-400 (miniatures),
UI-401 (placement and scale), UI-402 (characters as miniatures), UI-403 (the lifted figure), UI-404 (flight), UI-405
(pick up and carry), UI-406 (tokens, settlements, kings, the Fellowship) and UI-407 (acceptance).

The art of the bases, the garrison stand, the character stand and the token puck is painted with the owner's Codex
CLI (`codex exec`, built-in `image_gen`, as `graphicalassets/battles`); the contact shadow, the state rims and the landing
mark are drawn as SVG, since they must be exact and translucent. Prompts and checksums are recorded beside the art
(`graphicalassets/ui/armies/v1/generations-codex.jsonl`, UI-400).

## Owner decisions (planning session, 2026-10-28)

1. **Style.** 2.5D miniatures on the existing flat UI Toolkit map. The upright three-quarter cut-outs of the `units-v3`
   and `characters-v1` atlases stand on an oval base in the side's colour, with a contact shadow on the map. Lifting
   scales the figure up and raises it; the shadow stays on the ground. No 3D scene.
2. **Drag and click, for units and characters only** (armies, Nazgûl, companions, minions). Dice, cards, tokens and the
   reserve keep their gestures (the reserve drag is UI-393). Clicks remain the full path (R16); clicking a source lifts
   the figure in place too. *Superseded for the Reserve's figures by UI-454 (owner, 2026-11-03): a Reserve figure is
   carried onto the map on the same principle as a map figure, and a recruit placed by clicks flies from its slot; see
   "As built (UI-454)" below.*
3. **Two zoom levels stay.** Below the threshold the token remains, but it becomes a physical object (thickness, rim,
   contact shadow).
4. **Scope.** Armies, characters, the Fellowship figure (look and flight; it is not dragged), settlements and control
   markers (look only).
5. **Every character is its own miniature** on a small round base beside the army in its region, lifted on its own.
6. **Figures scale with the map** (sizes in board units, clamped to a readable screen size) and keep one fixed place in
   their region at every zoom.

Decisions 2 and 5 partly supersede UI-105 and UI-128 ("army movement remains only clicks; drag remains only for
reserves") for units and characters only; the superseded statements in UX_SPEC, ARMY_MAP_UX and BOARD_INTERACTION_UX
carry a dated pointer here.

## Diagnosis: why placement reads as random today

- **Fixed-pixel markers on a scaling map.** Army markers are fixed-size Buttons whose `left`/`top` follow the camera
  (`MapCanvas.Refresh`), so at every zoom they cover a different share of their region and drift across its contour.
- **Non-geographic offsets.** A region's forces try four offsets around its single `anchor` in a fixed order
  (`MapCanvas.LayoutRegion`, `PlaceRow`, `RowCentre`, `ApplyRow`); which offset wins depends on the order regions are
  laid out in (`OverlapsPlaced`), not on the region's shape.
- **Rows cross contours.** A row of markers is centred on the anchor and can stand half in a neighbouring region; the
  fortress drawings (`landmarks.json`) stand 60–125 board units from their anchors, so a garrison floats beside its
  fortress.
- **No depth.** Markers are not sorted by their feet; a lower (nearer) figure can be drawn behind a higher one.
- **No common feet line.** `MapArmyArtwork` centres up to three 50×60 `ScaleToFit` silhouettes in their boxes, so wide
  cells float and every figure is the same height.
- **Characters folded in.** A force's characters are merged into the army marker and only the first one's art shows;
  Gandalf and Strider alone show two Gandalfs. (Resolved by UI-402.)
- **Flat motion.** `MapFigureMotion` slides a 96×84 box straight from anchor to anchor and the destination row then
  snaps into place: no lift, arc or landing. (Resolved by UI-404.)

## Visual specification

Light comes from the upper left. No CSS filter on any map object (UI-322, UI-334; the UITK filter crash): glows are
baked textures and opacity.

- **Miniature.** A figure cut-out from `units-v3` / `characters-v1`, anchored at the bottom on its base's feet line.
  Two or three figures of an army stand staggered in depth on one base, the nearer lower and in front.
- **Base.** Art in `graphicalassets/ui/src/armies/`, rendered to 2× PNGs in `Resources/Wotr/Art/UI/armies/`:
  `stand-field` (oval, the side's enamel with a bevelled rim), `stand-garrison` (crenellated, for a stronghold's
  garrison), `stand-character` (small round). The front of the base carries a code-drawn plaque with the nation
  emblems and the count. The art has no text and no digits.
- **Contact shadow.** `contact-shadow`, a soft ellipse under the base, offset down-right; it never lifts.
- **Stature** (`Runtime/Map/Figures/FigureStature.cs`), as a share of the elite's height: regular .78, leader .88,
  elite 1.0; hobbits about .62; Gimli and Gollum about .70; large minions (the Balrog, the Mûmak) about 1.1; otherwise
  the `FigureAppearance` kind decides. A wide cell keeps its feet on the line.
- **States** are rim overlays (`rim-eligible`, `rim-selected`, `rim-pressed`, `rim-moved`, `rim-ability`) plus the
  shared ChoiceGlow, instead of eight plinth PNGs.
- **Token (far zoom).** `token-puck`: a top face, a side band for thickness, a rim and the contact shadow; states as
  overlays.
- **Settlements.** The painter2D gems gain a soft fake shadow and a facet band, sized `clamp(zoom / reference, .85,
  1.4)`; the control colour is unchanged (UI-195).
- **Kings.** A sovereign marker is a small puck with a shadow.
- **Fellowship figure.** It stands on a round Free Peoples base with a shadow, on its place and on the Mordor track.
  Since UI-427 a plaque on the base's front shows ◇ Hidden or ◆ Revealed.
- **Landing mark.** `landing-mark`, a baked ring on the table at the prospective place: green for a move, red for an
  attack.
- **Lift pose.** Scale about 1.10 and a rise of about 12 px × scale; the shadow stays on the ground at scale .8 and
  opacity .35. Since UI-428 a carried figure (and one resting where it was dropped) is held 34 px × scale up, so the
  landing mark shows under it.

**As built (UI-400, 2026-10-29).** The atlases already paint each figure's stature on one cell height with the feet on
the bottom edge (a regular's cell is 78 % filled, a hobbit's 62 %), so a figure's box is its cell's shape at the elite's
height and only the Balrog (1.1) and the fallback silhouettes need `FigureStature`. Sizes and states:
[ARMY_MAP_UX](ARMY_MAP_UX.md), "Miniatures on the table — UI-400".

## Stand places, scale, depth and hit shape

- **Places** (UI-422): `Resources/MapCanvas/stand-slots.json`, generated by `scripts/export_map_stands.py` from the map
  geometry, `landmarks.json`, the settlements, the name sizes and hand pins (`scripts/map-stands-overrides.json`), with
  `sourceHashes` and a `--check` mode. Per region: `garrison` (the gate of the fortress drawing, or beside the
  settlement), `field` (open ground inside the contour, near the settlement but clear of it), `characters[0..5]`,
  `garrisonCharacters[0..2]`, `fellowship`, `planned`, `sovereign` (since UI-425: a Corrupted king's marker) and
  `capacity` (the largest footprint scale at which the group fits). Region overrides of expansion modules always get a
  place (fallback beside the settlement). Region names keep clear of the places (`MapRegionNamePoints.For` with an
  obstacle predicate). As built (2026-10-29):
  - **Data.** Every place is the point under the figures' feet, in board units. A stronghold's record also holds its
    `fortress`: its drawing together with its garrison's figures at the gate, which names keep clear of.
    `MapStandSlots.Load` reads the file once and rejects it, like `MapBandGeometry.Validate`, when a recorded source
    no longer matches: the bytes of `map-geometry.json` and `MapCanvas/landmarks.json`, a digest of the regions' ids,
    names and settlements in `base-board.json` (the rest of that file may change freely), the worst-case zoom, the
    footprints, a missing region or a garrison place that disagrees with the strongholds. `For(shape, current)` gives
    a region's places for its current definition.
  - **Footprints** (`StandFootprint`). Scale 1 is the readable minimum in the worst case: 1366×768 at 125 % is a
    1092.8×614.4 point map, figures show down to 1.2 × its fit zoom (`MapMarkerVisibility.TokensBelow`), that is
    0.8035 points per board unit. An army (field, garrison, planned): a base of 40 × 16 units (32 × 13 points there)
    and figures 40 units tall; a character: a base of 14 × 5.6 and figures 16 × 36; the Fellowship: a base of 16 × 6.4
    and figures 18 × 36. Every base is an ellipse 0.4 times as deep as it is wide. The figure rectangle runs from the
    top of the figures to the front of the base, as wide as the wider of the two. UI-401 clamps a figure to at least
    this size (`px >= units × 0.8035`), so at the Detail level a footprint is never larger than scale 1.
  - **Choosing a place.** On an anisotropic distance transform of each region, so a base fits where the distance
    reaches its half width, 1.5 units clear of the border and of the other bases. The garrison's feet lie on its
    drawing, low on its middle (the gate: a fifth of the drawing's height above its lower edge), its three characters
    beside it. The field stands as close to the settlement point as its figures allow without covering the gem, so
    above or beside it; the planned recruits, the six characters and the Fellowship stand around the field.
    UI-513 (owner, 2026-11-07, a picture of the Fellowship on Dagorlad's southern border): the Fellowship is placed
    right after the field (and the garrison's characters), before the recruits and the field's characters, and its
    place weighs open ground up to 30 units (weight 1, against 8 units at 0.5 for the others), so it stands well
    inside its region where it reads at a glance. The median distance from its feet to the border went from 10 to
    25.5 units and the nearest from 3.7 to 5 (Helm's Deep); `MapStandSlotsTests.TheFellowshipStandsWellInsideItsRegion`
    keeps both. The release check of 0.10.0-rc.4 found that it had stood Minas Morgul's Fellowship on its gate of the
    Mordor track: the marker takes clicks over its whole box (64 × 80 design points at 1.8 a unit, its plaque grown to
    86 at the worst case), so a click on the glowing gate went to the Fellowship and it could not enter Mordor there
    (`MordorNetworkTests`). Where the Fellowship stands in a region, the track offers both gates and its first step; the
    Fellowship's marker box now keeps clear of each as large as it is at the figures' design zoom (1.35 points a unit;
    the marks keep their size in points, so at every zoom that shows figures at least that middle of a mark shows and
    takes its click). The export reads `MapCanvas/mordor-track.json` and records its hash; only Minas Morgul's
    Fellowship (back to 1235, 893) and Morannon's (1207, 779, its box had covered the gate's upper half) moved, with
    their characters around them.
    Characters keep to the army's line: figures that hide one another cost heavily, and only where that leaves the
    group below scale 1 does a lighter cost get its chance. A besieger in the field and the garrison never share more
    than a quarter of either's figure rectangle, so a siege shows both. No figure rectangle covers a settlement gem
    (as drawn at the worst-case zoom, the larger of the printed settlement and an expansion's, plus 2 units), another
    region's fortress drawing or any region's name; a garrison group stands on its own drawing.
  - **Capacity.** A tight region is laid out at the largest scale its group fits (steps of 0.05); `capacity` is the
    largest scale, up to 2, at which the fixed places still hold. Fifteen regions are below 1 and show tokens in the
    worst case: Western Mirkwood 0.57, Minas Morgul 0.60, Woodland Realm 0.62, Gladden Fields 0.70, Rhosgobel 0.70,
    Dol Guldur 0.82, Southern Mirkwood 0.85, Ash Mountains 0.86, Moria 0.87, High Pass 0.91, Old Ford 0.95, Old Forest
    Road 0.96, South Anduin Vale 0.96, Lossarnach 0.97, Edoras 0.98. Their figures show once the zoom shrinks the
    footprint to their capacity (UI-401's maximum clamp). Minas Morgul, Woodland Realm and Moria stay tight even
    without names: their drawings stand at narrow ends of their regions. The Helm's Deep siege fits at scale 1: the
    garrison at the gate in the east of the region, the besieger's field north-west of it, the garrison's characters
    in a row to the west. Since UI-425 (2026-10-30) seventeen regions are below 1 and the garrison's characters at
    Helm's Deep stand to the west, the east and behind the gate ("As built (UI-425)" below).
  - **Hand pins.** `pins` in `scripts/map-stands-overrides.json` (`{"<region>": {"field": [x, y],
    "characters[2]": [x, y]}}`) replace the search for those places; none is needed today. Its `settlements` lists
    the settlements expansion modules give regions (Fate of Erebor: Dale a Fortification, the Iron Hills a City; New
    Cities: Ered Luin and South Rhûn Cities), so their larger gems are kept clear too.
  - **Region overrides.** A region the file does not hold, or a stronghold an expansion module makes of a region
    without a garrison place, stands beside its settlement: the field to its right and the garrison to its left, the
    characters beyond them, the Fellowship above and the planned recruits below, 14 units clear of the gem, with
    capacity 0 so it shows tokens. No module makes such a stronghold today.
  - **Checks.** `python3 scripts/export_map_stands.py --check` lays everything out again, runs the acceptance checks
    and compares the result with the file (about 80 s, about 3 minutes since UI-425; NumPy, SciPy and Pillow as in
    `scripts/requirements-map-canvas.txt`); `--preview <folder>` draws the places, names, gems and drawings over the
    contours. EditMode `MapStandSlotsTests` check the file on their own with `MapContourMath`;
    `MapRegionNamePointsTests` and `MapLandmarksTests` check the names and drawings against the places.
- **Scale** (UI-401): `feet = Camera.ToScreen(place)`; size `px = clamp(boardUnits × Zoom, min, max)`, applied only
  through `style.translate`/`style.scale` with `UsageHints.DynamicTransform`; width and height are never written per
  frame.
- **Level.** Figures show at the Detail zoom when the footprint fits the place's capacity (1.3/1.2 hysteresis; focused
  regions are forced to figures); otherwise the token.
- **Depth.** One y-sorted `standLayer` holds armies, the Fellowship marker, planned recruits, flights and journeys,
  sovereign markers and corrupted kings; it is sorted on sync only, never per frame. Nearer figures (lower on the
  screen) are drawn in front.
- **Hit shape.** `MiniatureButton.ContainsPoint` covers the base ellipse and the figure columns; transparent headroom
  above a figure never takes a click — it goes to whatever lies behind.

**As built (UI-401, 2026-10-30).**

- **Layout pass.** `MapStandLayout` replaces the row layout (`LayoutRegion`, `PlaceRow`, `RowCentre`, `ApplyRow`,
  `OverlapsPlaced`). Places are assigned when the board changes (`Assign`), never per frame: a garrison stands at its
  fortress's gate (`garrison`); the region's main field force (the one with army units, otherwise the larger) on
  `field`; a second field force — Nazgûl or companions beside the other side's army, until UI-402 gives characters their
  own places — on `planned`, and any further one on the character places. Planned recruits, a card's planned Nazgûl
  flights and journeys stand on `planned`; the move ghost on `field` when no field force stands there, otherwise on
  `planned` (`MapCanvas.GhostPlace`); a corrupted king's marker on the last character place (`MapCanvas.BesideForces`;
  since UI-425 on its own `sovereign` place);
  the Fellowship on `fellowship`, and on the Mordor track on its step. Characters still stand inside their army's
  miniature (UI-402). Since UI-402 a second field force can only be units (Nazgûl beside the other side's army) and it
  always takes `planned`; the characters stand on their own places (below).
- **Transform only.** Each standing object (`MapStand`) keeps its design size inside a holder of a fixed size with
  `UsageHints.DynamicTransform`. Per frame only the holder's `style.translate` and `style.scale` are written, and only
  when they change: the design point under the feet goes to `Camera.ToScreen(place)`. Width, height, left and top are
  never written per frame, and the element keeps its own style transforms (a hover or pressed scale).
- **Scale** (`MapStandScale`). The points per board unit are `p = clamp(zoom, 0.8035, 1.35)`: the minimum is the
  worst-case Detail zoom (`MapStandSlots.WorstCaseZoom`, the footprints' scale 1), the maximum the miniature's design
  size. The miniature is designed in board units at 1.35 points per unit (`MapArmyArtwork.DesignUnit`): its stand is the
  army's 40 × 16 unit base (54 points wide) and an elite stands 40 units (54 points, the UI-192 figure height) tall; it is
  drawn at scale `p / 1.35`, from 0.595 (a 32-point base and elite) to 1. The footprint's scale on the board is
  `p / zoom`: 1 between the clamps, larger below the minimum (a focused region far out), smaller past 1.35 points per
  unit, which gives the tight regions room. The Fellowship's 64 × 72 point figure is designed at 1.8 points per unit and
  drawn at the same `p`; on the Mordor track it keeps its UI-283 size (80 or 64 points tall). An army's token is a puck
  about as wide as its base: `clamp(40 × zoom, 32, 44)` points. A king's marker keeps its 30 × 34 points. (Since UI-425
  a field force's token stands on its puck's lower edge, and past the whole map of the worst case tokens and kings'
  markers keep their board size: "As built (UI-425)".)
- **Level** (`MapStandLayout.Figures`). A focused region (inspected, holding a selected force, or just reached by a
  flight) shows figures; otherwise figures need the Detail level (1.3/1.2 × the fit zoom) and the footprint within the
  region's capacity, and they stay until the footprint exceeds the capacity by the same 1.3/1.2 ratio. With the
  1.35-point maximum every region gets figures before the closest zoom (2.6): the tightest, Western Mirkwood (0.57),
  from a zoom of 2.37. The Fellowship's figure has no token, so it shows at the Detail level in every region (and at
  any zoom while it offers a click), as before.
- **Depth.** One stand layer (`map-stands`, above the Mordor track and under the history and draft overlays, the
  flights, the observed traces and the move ghost) holds the armies, the Fellowship, the planned ghosts and the kings'
  markers. It is sorted by the feet's board y, then x, when what stands there changes (an army sync, the planned ghosts,
  the kings, the Fellowship's position), never per frame, so nearer figures are drawn in front.
- **Callers.** `MarkerBounds` gives a stand's visible part (the stand, its figures and plaque, or the token);
  `GhostPlace` and `BesideForces` give places; `NearView` hides a stand while its feet are more than 160 points
  (`MarkerReach`) outside the view. Region names still hide under a stand that covers them (the reserved bounds).
- **The miniature** (`MapArmyArtwork`, `Styles/Armies.uss`): a 66 × 86 point box with the feet at (33, 64), the middle
  of the stand's top face; the stand 54 × 21 (a garrison's 54 × 23); the nearer figures stand 2 points in front of the
  middle and the farther one 4–6 points behind; two figures stand ±9 points apart, three ±13, kept within the stand and
  a 4-point overhang, and a cell wider than 62 points is drawn smaller with its feet on the line. The plaque (emblems 13
  points, count 12) hangs at the stand's front, as wide as the stand, and takes a press with the stand and the figures.
- **Cost.** A camera change writes no layout property of a stand, so it lays nothing out again. Measured in PlayMode on
  the stress fixture (600 frames panned every frame, uncapped, against `expansions` 41882b0c): frame p95/p99 at
  1366 × 768 on the whole map 3.21/3.51 ms (before 3.22/3.94), zoomed in 2.15/2.37 ms (2.23/2.35); at 1920 × 1080
  3.55/3.91 ms (4.30/4.95) and 2.33/2.62 ms (2.60/2.88); `MapCanvas.Refresh` 0.21–0.38 ms at p99 (0.29–0.45). The
  Development Player profile (`map-profile-2`) was not run for UI-401.
- **Not yet.** Flights (`MapFigureMotion`) still fly between region points at their own size (resolved by UI-404: they
  fly between stand points as the miniature); the token keeps its UI-125 pictures (resolved by UI-406: a puck, "As built
  (UI-406)" below). The Mordor track's stones and the effects labels still move by `left`/`top`.
- **Tests.** EditMode `MapStandScaleTests` (clamps, footprint, capacities within the closest zoom, hysteresis, token,
  the miniature's design on the footprint), `MapContourDataTests.FellowshipFigureStandsInsideItsRegion`; PlayMode
  `MapStandLayoutTests` (the Helm's Deep siege and its neighbours at 1366/1920 × 100/125 % and four zooms: feet on the
  places, design size kept, board-unit size, panning moves every stand with the map, the layer sorted by depth, both
  sides of the siege shown with the nearer garrison in front, Edoras's token until there is room), `MapVisibilityTests`,
  `LayoutTests`, `MapMarkerEdgeTests`, `MapArmyArtworkTests`.

**As built (UI-402, 2026-10-30): every character is its own miniature.**

- **Data.** `BoardPresentation.Characters` holds every character on the map (`CharacterFigure`: id, definition, region,
  side, field or garrison, level, leadership, art, and an Awakened king's `SovereignId`). `ArmyMarker` keeps a force's
  units (`Units`, each an `ArmyUnit` with its id, nation and kind); its `Total` and its plaque's count no longer count
  characters, and a region where characters stand alone has no army marker. Only a ghost carries characters on its base
  (`ArmyMarker.Carried`, `FigureCount`: the planned move, a card's recruits, a journey), each its own figure. An Initial
  king still crowns his army; an Awakened one stands on his own figure. Nazgûl are units: a lone Nazgûl is an army of
  one; a Nazgûl in a mixed stack stands (and will lift) with its base.
- **The miniature** (`MapCharacterArtwork`, `Styles/Armies.uss`) is designed at the army's 1.35 points a unit on the
  character footprint: the painted round stand (`stand-character-*`, 40 × 28 art) drawn 19 × 13.3 points (14 units), the
  feet at the middle of its top face, (28, 56) in a 56 × 70 box; the figure its `characters-v1` cell 48.6 points (36
  units) tall × `FigureStature` (the Balrog 1.1), at most 54 points wide, feet on the line; the contact shadow 1.5 points
  right and below; the state rims `rim-character-*` (moved, ability owner, eligible or king's target, selected, pressed)
  and ChoiceGlow as a capsule round the stand, pulsing with the armies (`MapCanvas`'s pulse). It is drawn at the army's
  scale for the zoom (`MapStandLayout.LayoutCharacter`, the same `FigureScale`). At the token level it is a medallion:
  the portrait in its side's `portrait-medallion-*`, `clamp(20 × zoom, 18, 24)` points (`MapStandScale.CharacterToken`).
  No filter; the flare is the glow texture and the fades are opacity.
- **Places** (`CharacterPlacement`, `MapCharacterFigures.Assign`, on every board change, never per frame). A field's
  characters stand on `characters[0..5]`, a garrison's on `garrisonCharacters[0..2]` (with the field's where the region
  has no garrison place); the side of the army on `field` first, then by id. A character keeps its place while another
  arrives or leaves; a newcomer takes the next free place. More characters than a group's places: all but the last place
  hold one each, and the rest stand as a stacked pair on the last place — the front figure 3 points left, a second one on
  its own stand 7 points right and 4 behind, a code-drawn "+n" (the characters behind the front one) on the stand's
  front. The pair shows a state when any of its characters has it (marched when all have), and its click chooses the
  front one; the others are chosen in the region panel or by the army's click. All of them share the y-sorted stand
  layer with the armies, so a nearer character is drawn in front of an army behind it.
- **Hit shape.** `MiniatureButton` with the stand's ellipse, a column under each figure as wide as the footprint's
  figure (16 units, 21.6 points) from its head to its feet, and the "+n"; a token takes its disc. A wide cell (Strider's
  sword, Boromir's shield) overhangs its neighbour's place, but only its column takes a press.
- **Click.** A `character-<id>` button with `DraftObject.Miniature` (its region in `DraftToken.Region`) raises
  `MapCanvas.MiniatureSelected`; `TurnInteraction.ChooseMiniature`: a lit Awakened king chooses the king; an enemy's
  character is its region's click; the player's own calls `ActionContextInteraction.SelectFigure`, the chip's own path,
  so the commands are the chip's — with a Character die it starts travel (`TravelContextInteraction.Figure` through
  `FigureSelector`), with an Army die it joins the army (the commander-first rule), and a card's figure choice, the
  Balrog's order or the Chief's reaction lit on the figure is used. A click that chooses nothing does what its region's
  click does but never opens the region panel. A drop on a miniature is a drop on its region
  (`DraftToken.AsRegion`). The army base's click is unchanged; a press on a character holds the open panel as a press on
  an army does, and its right click reads its region.
- **Shared highlights.** `FigureChoices` (Eligible, Selected, Moved, by figure id) is owned by the map and is the region
  panel's `Eligible`/`Selected`/`Moved`; the contexts compose it as before, and `RegionHoverPanel.RefreshSelection` now
  tells both readers (`FigureChoices.Refresh`): the panel's chips and the miniatures. A character also shows selected
  while an unsent move or journey takes it (`MapCanvas.ShowDraftSources`), and an ability owner by the owners' ids
  (`HighlightOwners(regions, figures)`). An army is marked marched when all of its units have. A region holding a
  selected character shows figures (`MapCanvas.Focused`, from a set kept with the states, read every frame).
- **Per character.** A character that enters, or changes its place without a flight, flares and emerges there; one a
  flight brings (`MapFigureMotion.ArrivingCharacter`; the force's arrivals and departures are its units') waits at a
  quarter opacity until it lands; one removed fades out where it stood (`leaving-character-<id>`). The first view, quiet
  views, Reduced motion and Skip show the state (`MapCanvas.EndEmerging`).
- **Traces.** `MapCanvas.ForceArtworks` (the army's artwork and its characters' miniatures) glow together (`ForceGlow`);
  the delta badges and portraits stand beside `ForceBounds`, the region panel keeps clear of `FigureBounds`.
- **Cost.** Per frame a character writes only its holder's translate and scale (the UI-401 rule); its states change on
  a choice refresh, its place and art on a board change.
- **Not yet.** Flights still fly between region points (resolved by UI-404); lifting (UI-403) and carrying (UI-405) are
  next (both built since, "As built (UI-403)" and "As built (UI-405)" below).
- **Tests.** EditMode `CharacterPlacementTests` (field, garrison and side order, kept places, the stacked pair),
  `ArmyMarkerTests` (units only, two distinct figures, the character's tooltip), `SovereignMapTests` (an Awakened king's
  figure); PlayMode `CharacterMiniatureTests` (two distinct figures on their places inside their region at the armies'
  size, 1366/1920 × 100/125 %, the hit shape, a token far out; a figure's click equals its chip's and opens no panel,
  the army's click takes its characters; the stress fixture's stacked pair "+2"), `ArmyEmergeTests`,
  `TravelGestureNetworkTests` (a Character die's click on Gimli's figure travels him).

## The lifted figure, drag and drop

- **Click-lift** (UI-403). A click on a legal source lifts exactly the selected figures into the hand
  (`MapFigureHand`, a pointer-ignoring layer above `standLayer`, clipped to the map); the rest of the army stays on its
  base, and the base disappears when the whole force is lifted. States: InPlace, Returning, Planned. The content comes
  from the draft's unsent move (`ArmyDraft`, `TravelActions`, `NazgulFlightDraft`).
- **Target.** On a hovered legal target the landing mark appears at the prospective place and the caption hangs above
  the lifted figure. The translucent army ghost is removed; `ArmyGhost.Of` survives only as the caption.
- **Return.** Esc and undo drop the figures back onto their base. Reduced motion shows a static raised pose.
- **Drag** (UI-405). A press on the player's own legal (or already selected) figure that moves past the drag threshold
  lifts it the same way and carries it under the pointer; no text drag ghost. A drop on a legal target that is not the
  source calls the target click (`TurnInteraction.ChooseRegion`), so the payload is identical; sent → the figure settles
  and hands off to the flight; planned → it dissolves into the planned ghost; nothing → it returns. Esc, focus loss or a
  revision change restore the pre-press draft and animate the return. The region under the figure lights
  (`SetDragTarget`); edge pan works after a ~250 ms dwell. The opponent's figures and any figure while the board is busy
  never lift.

**As built (UI-403, 2026-10-30): the lifted figure.**

- **What lifts** (`Presentation/LiftedFigures.cs`). The draft's unsent move: the army selection without a destination
  (`ArmyDraft.Current`), the travelling group being chosen (`TravelActions.Selected`, the Council token's chosen group)
  and the figure the next destination click flies (`NazgulFlightDraft.Next`). Also the figures the draft sends on their
  way (a march, a character's travel) while they still stand where they leave from, unless a planned ghost shows them (a
  card's flight or journey). An attack's army is not on its way: it comes back to its base.
- **The hand** (`Runtime/Map/Figures/MapFigureHand.cs`, the layer `map-hand` right above `map-stands`, inside the map so
  its edge clips it; it never takes the pointer). One piece (`MapHandPiece`, `hand-<the source's name>`) per force — its
  lifted units drawn by `MapArmyArtwork` on a base of their own, with their count on the plaque and the gold rim — and
  one per character, drawn by `MapCharacterArtwork` on its stand. A piece's holder is a `MapStand` on the source's feet
  at the armies' `FigureScale`, moved by translate and scale only, and only when they change.
- **The pose** is the class `figure-lifted` on the piece. Its `hand-lift` (transform origin at the feet) moves 12 points
  up and scales 1.10. Being inside the scaled holder, that is 12 px × the figure's scale on the screen. Its
  `hand-shadow` (the contact-shadow art where the miniature's own shadow lies, which is hidden in the hand) stays on the
  table at scale .8 and opacity .35. The transitions run only under `.motion-enabled`: 0.18 s up, 0.22 s down, 0.25 s to
  dissolve. Reduced motion shows the raised pose at once and takes a piece away at once. A new piece takes the class
  after its first layout, so it rises.
- **States** (`MapHandPiece.PieceState`). InPlace: raised over its place. Returning (`hand-returning`): it drops onto
  its base, then goes. Planned (`hand-planned`): a planned ghost now shows it at its destination, so it dissolves where
  it hovers, then goes. A piece whose figures the view carried away (the step was accepted) goes at once, and the flight
  shows them.
- **The source** (`MapCanvas.SetLiftMask`). Every army and character stand shows only what stays behind. An army draws
  its staying units (figures, emblems and count). A stand none stays on takes `lifted-away`: its artwork is hidden at
  once (visibility, no fade, so nothing shows twice when a piece lands) and its ChoiceGlow is off, but its hit shape
  stays, so a click there is still its click (the army's second click keeps its selection). The mask holds the hand's
  figures and those the planned ghosts show (`MapPlannedRecruits.FigureIds`). A stand changes only when its own lifted
  figures change. A region holding a lifted figure shows figures (`MapCanvas.Focused`).
- **A split** (`MapFigureHand.Regather`). A figure left behind in the region panel leaves the raised piece at once and
  drops back onto the base as a piece of its own; the base then shows it.
- **The landing mark** (`Runtime/Map/MapLandingMark.cs`). On a hovered region that has a caption, the ring
  (`landing-mark`, 58 × 18.6 design points: the art's ellipse as wide as the army's 54-point base, centred 1.5 points
  below the feet) lies on the prospective place at the figures' scale. For units that is `MapCanvas.LandingPlace`
  (UI-401: the field, or the planned place when a field force stands there). For characters alone it is the region's
  first free character place (`MapCharacterFigures.NextPlace`), at .35 of the army's size. It is green (`landing-move`)
  or red (`landing-attack`). Its layer `map-landing` lies under the stands, so it never covers a figure near it. The
  caption (`landing-caption`, 120 points wide) hangs 4 points above the raised figures' visible top, centred; while they
  are out of view it hangs above the ring.
- **Captions** (`ArmyGhost`, set on every refresh by `ActionContextInteraction` through `MapCanvas.SetLanding`). The
  army's come from the server's targets ("Move n figures", "Attack · Siege"; `Attacks` makes the ring red). A card's
  flight names the armed figure ("Fly a Nazgûl", "Fly the Witch-king", "Move the Balrog", UI-416's names), as does the
  Council token's group. A character's travel names the travellers ("Move Strider", "Move Strider and Gimli", "Move 4
  figures" beyond three; the Shadow's by UI-416's names).
- **Removed.** The army ghost (`army-ghost`, `SetGhost`, `UpdateGhost`, `EnsureGhost`), its rules and `ArmyGhost.Army`;
  UI-401's `GhostPlace` is now `LandingPlace`; the hand's layer stands above the stands, the landing mark's under them.
  The planned recruits, flights and journeys keep their faint ghost, now classed `planned-ghost`.
- **Cost.** Per camera frame each piece, the ring and the caption write a translate (and a scale) only when it changes;
  the pieces and the mask change on a draft or board change only. No filter.
- **Not yet.** A sent move stays raised until the view arrives, then the flight starts from the source's place (since
  UI-404 the hand gives the flight its place and raised pose, "As built (UI-404)" below); dragging (UI-405, built
  2026-10-30: "As built (UI-405)" below).
- **Tests.** EditMode `MapFigureHandTests` (exactly the selection lifts and the base shows the rest; a split; Esc or
  undo; a sent march kept until the view takes it; an attack's return; a planned flight's figure leaving its place) and
  `ArmyMovementTests` (the army's and travellers' captions, the attack set). PlayMode `FigureHandTests` (the pose and
  scale, the rest on its base, the green and red landing mark on its place, the caption above the lifted figure, no
  translucent copy, a character alone, Escape, Reduced motion) and
  `BoardInteractionTests.UI127OneClickLiftsTheArmyALandingMarkPreviewsTheTargetAndEscapePutsItBack`.

**As built (UI-405, 2026-10-30): a unit or character is picked up and carried; its drop is the target's click.**

- **Input** (`Runtime/UI/DraftInput.cs`, `Runtime/UI/IFigureDrag.cs`). A press on an `Army` or a `Miniature` that crosses
  the drag threshold (6 points) asks `IFigureDrag.Begin`, once per press. Refused, the press stays a click: nothing takes
  the pointer, no label shows, and its release over the figure is its click (R16). Accepted, the board takes the pointer
  and every move goes to `IFigureDrag.Carry` with the object under it (`DraftInput.At`, the release's own pick); no
  `.drag-ghost` label and no UI-393 icon is shown for a figure. The release is the drop (the input's drop callback, as for
  any drag); `ResetDrag` — Esc, focus loss, a cancelled pointer, a changed scope (revision, decision or connection) —
  calls `IFigureDrag.Cancel`. Region-panel chips (`Figure`), Characters rows, Fellowship portraits, dice, cards, tokens
  and the reserve keep their gestures. Without an `IFigureDrag` (isolated input tests) figures never drag.
- **Begin** (`Runtime/Application/FigureLiftInteraction.cs`). It lifts the player's own figure while a decision waits and
  nothing is busy (`SelectionBusy`: a command on its way, an opponent's presentation, an older revision); a force holding
  a lit king never lifts (its click chooses the king). The draft is kept as a checkpoint with its undo history
  (`TurnDraft.Checkpoint`). A figure already raised for the unsent move (`MapCanvas.Lifts`: its own piece in the hand
  holds part of `LiftedFigures.Selection`) is carried as it is; otherwise its click is made quietly
  (`TurnInteraction.SelectToLift`: `ChooseArmy` or `ChooseMiniature` with `RegionHover.Quiet`, so no panel opens). If
  that click would send a step (an ability used on the figure), the send is held back and the press stays a click. The
  figure lifts only when its piece is then raised, a lit target other than its own region exists
  (`DraftOverlay.Targets`) and the hand takes it up; otherwise the checkpoint is put back (`TurnDraft.Rewind`, saved and
  shown) and the press stays a click.
- **Carried** (`MapFigureHand.Grab`/`Follow`, `MapHandPiece`). Every raised piece of the unsent move goes with the
  pointer — the force's units and the characters chosen with them — each keeping its feet's offset from the press, so the
  grabbed figure stays under the pointer. It is UI-403's piece itself in its raised pose: the same miniature at the same
  scale, the contact shadow under it; only its holder's translate changes (map-local, so an edge pan leaves it under the
  pointer). It stays clipped to the map. The source's base keeps showing only what stays behind (`lifted-away` when
  nothing does). The caption hangs above the carried figures.
- **The region under it** (`MapCanvas.SetDragTarget`, `MapRenderer`). The region under the pointer (terrain, an army
  marker or a character's miniature, and since UI-428 the Fellowship's figure or a king's marker; "" over the HUD or
  water) is the drag target: a legal target lights more strongly in
  its own colour (`CarriedTarget` green .50, `CarriedAttack` red .52), any other region gets the hover tint, and on a legal
  target the landing mark and its caption show as on a hover (`LandingRegion`). `RegionHover.PreviewDrag` treats a carried
  army or character as a reserve figure: the open region panel turns translucent and lets the drop through.
- **Edge pan** (`MapEdgePan.SampleDrag`). While a figure is carried the pointer is pressed and captured, so the drag feeds
  its own samples (each move, and the screen's native samples, `MapScreenPointer`); the usual pick rules still apply.
  Once the pointer has rested in the 26-point edge zone for `DragDwellSeconds` (0.25 s) the map pans; each step raises
  `DragPanned` and the region under the resting pointer is read again. Leaving the zone resets the dwell; the drop or a
  cancellation ends it (`EndDrag`).
- **Drop** (the first branch of `TurnInteraction.Drop`, `FigureLiftInteraction.Drop`). The carried pieces rest on the
  board point under them (`MapFigureHand.Release`). When the drop's region is in `DraftOverlay.Targets`, is not the
  figure's own region and nothing is busy, `TurnInteraction.ChooseRegion(region)` runs — the target click itself
  (`context.Target` and `SubmitPreparedStep`, `travel.Reach`, `flights.Region`, a card's armies, the Council token's
  travel) — so the payload is the click's. Outcomes, read from whether the carried figures left the unsent move:
  - **sent**: the step leaves and `MapCanvas.ExpectFlights` expects each dropped piece at the point it rests on
    (`MapHandPiece.Ground`, UI-404's `Figures.Expect` with the drop point); it stays raised there, and when the view
    takes the figures the flight goes on from there in the raised pose. A rejection, a disconnect or the 5 s expiry
    glides it home onto its base (UI-404's lapse);
  - **planned** (a card's flight or journey, a card's army move waiting for ✓): a piece the planned ghost now shows
    dissolves where it was dropped; when a card arms the next Nazgûl of the same stack, the dropped piece leaves from
    there and the next rises at the stack on a piece of its own (`MapFigureHand.Replace`); a move that only waits for ✓
    glides home over its source, raised, as its click shows it;
  - **nothing** (a drop on Lamedon beyond the mountains, on the HUD, on its own region, or a click that did nothing; until
    UI-428 also a drop on the Fellowship's figure): the checkpoint is put back and the figure glides home — onto its base when the press itself had chosen it, otherwise
    raised over its place as it was chosen before.
  An attack's drop is sent like its click and, as with the click (UI-403), its army comes back to its base at once.
- **Cancel.** Esc, focus loss, a cancelled pointer, a changed connection or decision, and a new revision (cancelled before
  the new view meets the draft, `TurnInteraction.Update`) put the checkpoint back, undo history included, save it, and
  glide the figures home. Leaving the board mid-drag rewinds the draft without animation.
- **Glide home** (`hand-gliding`, Styles/Armies.uss). A piece away from its source that goes home gets the class for its
  0.22 s glide alone (a transition on its holder's translate under `.motion-enabled`), so camera moves never lag; a
  return drops the pose at the same time. Reduced motion puts it back at once.
- **Cost.** Per move: one pick, a translate per carried piece, the caption's and ring's translates when they change, and
  a region repaint only when the region under the pointer changes. No layout property is written, no
  `GeometryChangedEvent` callback writes layout, no CSS filter. Measured in PlayMode (`BoardInteractionTests`
  `UI405EdgePanMovesTheMapUnderACarriedFigureAfterADwell`, 1366 × 768, Editor batch mode, Apple silicon): the CPU work of
  one drag frame with edge pan — a carried move (pick, carry, caption, drag target, edge sample) and a pan step (camera,
  `MapCanvas.Refresh`, the region read again) — over 240 frames: p50 0.217 ms, p95 0.226 ms, max 0.572 ms; rendering is
  not included, and the Development Player profile was not run.
- **Tests.** EditMode `DraftTests.ACheckpointPutsBackTheDraftAndItsUndoHistoryExactly`, `MapFigureHandTests`
  (`OnlyARaisedPieceOfTheUnsentMoveIsCarried`, `ACarriedNazgulFlownByItsDropLeavesFromThereAndTheNextRisesAtTheStack`),
  `MapFlightTests` (`ADroppedArmyHandsTheDropPointToItsFlight`, `ACarriedArmyDroppedWhereNothingTakesItGoesHome`);
  PlayMode `DraftInputTests.AFigurePressLiftsOnlyWhenItsDragAcceptsAndNeverShowsALabel`, `BoardInteractionTests`
  (`FinalTargetClickSendsTheStepWithoutConfirmAndTheArmysDropSendsTheSameStep`: the drop's payload equals the click's;
  `UI405TheCarriedArmyLightsItsTargetAndFliesOnFromWhereItWasDropped`: pictures `ui405-carried`, `-carried-target`,
  `-dropped`, `-handoff-start`, `-landed`; `UI405AnIllegalDropOrEscapeSendsNothingAndLeavesTheDraftAsItWas`: Lamedon, the
  HUD, Esc, a press-made selection undone, an enemy and a busy board; `UI405EdgePanMovesTheMapUnderACarriedFigureAfterADwell`:
  `ui405-edge-pan`; `ArmyDragAndEscapeCannotSpendTheSelectedDie`), `FigureHandTests`
  (`UI405TheChosenArmyAndStriderAreCarriedTogetherAsTheirOwnMiniatures`: `ui405-carried-army-and-strider`,
  `ui405-escape-back`), `MapContourInteractionTests` (both sides: a HUD drop is no step, a drop on the target's contour
  commits the click's step, Lamedon returns), `TravelGestureNetworkTests` (Gimli's figure carried onto Moria travels him);
  `MapContourProbe.DragFigure`/`LiftFigure`/`Carry`/`RefusedDrag` replace `AttemptDrag`.

**As built (UI-444, 2026-11-02, owner: fix the half-cut carried army at the map's edge).** While carried, a raised piece
stands in its own pointer-ignoring layer laid over the map in `#board-screen`, in front of the HUD and the windows
(`MapFigureHand.CarryLayerName`, `map-hand-carried`), so neither the map's edge nor the top bar, the console or a rail
cuts it. When the pointer is so near the screen's edge that a carried piece would cross it, the carried pieces shift
inward together just enough to stay `ScreenMargin` (6 points) inside it (`KeepOnScreen`; their base and raised
miniature, `MapHandPiece.CarriedBounds`); elsewhere they stay exactly under the pointer. The drag target stays the
region under the pointer; a dropped piece rests where it shows. Set down — dropped, sent, home or Esc — a piece is back
in the hand's layer, clipped to the map as before. Transforms only; nothing is laid out in a geometry callback.
Tests: `BoardInteractionTests.UI405EdgePanMovesTheMapUnderACarriedFigureAfterADwell` and `TabletopCarryScene`'s
edge-pan frames (`TabletopFrameChecks.AssertCarriedWhole`: in the carry layer in front of everything on the board,
every drawn part inside the screen, moved from under the pointer only inward and only when it would cross the edge);
the press-lift and Esc checks find the carried piece in the board's root.

**As built (UI-454, 2026-11-03, owner: "when recruiting units we want animation; if the user just picks with a click, the
unit should glide smoothly from its Reserve slot to its proper place on the board, like drawn cards, and there should
also be the option to drag the unit on exactly the same principle as moving it on the map").** It supersedes decision 2
for the Reserve's figures.

- **The carry layer** (`Runtime/Map/Figures/MapCarryLayer.cs`) is UI-444's layer made shared: the hand's carried pieces,
  the carried Reserve figure and the recruits' flights stand in it, above the HUD, in map-local points; `Inward` is
  UI-444's keep-on-screen shift.
- **Which recruits fly** (`Runtime/Application/RecruitPlacementWatch.cs`, `RecruitFlightInteraction.cs`). Every refresh of
  the Reserve (`ReinforcementsInteraction.Refresh`, before the panel changes) hands over the units the draft's recruitment
  places now — a card's or an ability's plan (`ReinforcementPlan.Placements`, not the characters a plan moves), or the
  Muster die's step (`draft.Payload.Placements`, sent at once) — with its scope (the match, the side, the decision and
  plan or Muster). A unit not placed at the last look launches a flight; a planned one no longer placed ends its flight
  (fade out). A new scope only sets the baseline, so a restored draft, a new decision or a reconnection flies nothing.
- **Where it starts** (`ReserveFlightStart.OnSlot`). The slot's picture of the figure (`ReinforcementsPanel.Art(nation,
  kind)`, the token's `reserve-token-figure` while the open panel shows it; otherwise the Reserve tab): the miniature's
  lone figure (`MapArmyArtwork.LoneFigure`) is laid exactly over the picture's drawn part — scale = the drawn height ÷
  the figure's height, the figure's feet on the picture's lower edge. A recruit dropped by a carry starts where the
  carried miniature shows, held up (Lift 1 and UI-428's extra height, as `MapHandPiece.Handoff`), at the figures' scale.
- **The flight** (`Runtime/Map/Figures/ReserveFlight.cs`, `ReserveFlights.cs`). The miniature it will be
  (`FlightMiniature.Recruit`: the unit on a field army's base) in a `MapStand` holder with UI-404's `flight-lift` pose and
  `flight-shadow`, named `reserve-flight-<unit id>`, classed `figure-flight`. `FlightProfile` drives it: the lift in place
  (from a slot it fades in over the lift, rising out of the picture), the arc of `clamp(.12 × screen distance, 20, 80)`
  px, the landing and settle; the duration is UI-404's from the board distance. The ground runs from the start to the
  destination's point on the screen now (`Camera.ToScreen` on every layout, so a pan or zoom is followed) and the scale
  from the start's to `Stands.FigureScale`; the last frame stands on the place at the figures' scale. Per frame: the
  holder's translate and scale, the pose's translate and scale, the shadow's scale and opacity and the root's opacity,
  each only when it changes; `MapCanvas.Refresh` lays them out on camera moves.
- **A Muster recruit** lands on its force's place: `MapCanvas.UnitFeet` once the map shows the unit, before that
  `RecruitPlace` (its side's field force there, else `LandingPlace`). At launch `MapFigureMotion.Bring(key)` (kept 5 s):
  the view that has the unit on the board takes its arrival out of the projection's moves (no `figure-arrival` fade in
  place, no UI-334 emergence) and tracks it as an arrival, so its force shows faintly (.25) as for any flight. The flight
  stays on its place after landing until the map has shown the unit for a whole frame, then goes and reports `Brought`:
  the force shows in full. A rejected receipt (`Figures.Lapse`), a disconnect, a reset, the expiry, or a newer view
  without the unit on the board (a lone Leader removed at once) lapse it (`Lapsed`): the flight fades out in 0.2 s.
- **A planned recruit** lands on its region's planned place (`Stands(region).Planned`), fading to the ghosts' .6 opacity
  as it lands; its ghost leaves it out while it flies (`MapPlannedRecruits.Withhold`) and shows it once it lands
  (`Release`). When the step that sends the plan leaves while it still flies (a ruler's last figure, whose click sends
  the recruitment, UI-450; a ✓), `TurnInteraction.Submit` hands it over (`ReserveFlights.Send`): it flies on, in full, to
  the place the view gives it, which waits for it as a Muster recruit's does.
- **Carried** (`Runtime/Application/ReserveCarryInteraction.cs`, `Runtime/Map/Figures/MapRecruitCarry.cs`,
  `FigureDragRoute`). DraftInput asks one `IFigureDrag` (`FigureDragRoute`): a Reserve press goes to
  `ReserveCarryInteraction`, an army or character to `FigureLiftInteraction`. Begin keeps a checkpoint and arms the
  figure as its click would but never places or sends it (`ReinforcementsInteraction.Arm`: `MusterActions.Select`, or the
  plan source's new `Arm` — `EventRecruitDraft.Arm`, `CharacterRecruitDraft.Arm`, a Muster plan's `RecruitDraft.Select`;
  an Effects card's figure that already has its region, a Kings card's branch, The Black Captain Commands and Gothmog's
  ability arm nothing). Without a lit target, or when nothing arms it, the checkpoint is put back and the press stays
  UI-393's drag. Accepted, the slot takes `drag-lifted` and the figure is carried in the carry layer: over the Reserve and
  the HUD as its picture 1.15× (`carried-recruit-icon`, centred under the pointer by translate); over the map (the pick
  is the map or a figure on it) as its miniature (`carried-recruit`, the hand's `figure-hand-piece figure-lifted
  hand-carried` pose: 1.10, 34 points up, the shadow on the table), its feet under the pointer, shifted inward to show
  whole. The region under it is the drag target (`SetDragTarget`: `CarriedTarget` on a lit settlement); on a lit one the
  landing mark lies on the place its recruit would stand on (the planned place for a plan) with the caption "Recruit
  <figure>" (`TooltipText.Unit`) above the miniature; edge pan samples the pointer. Switching looks writes display only
  when it crosses the map's edge.
- **Drop and cancel.** A drop on a lit target while nothing is busy calls `TurnInteraction.ChooseRegion` — the region's
  click — so the step and payload are the click's; the recruit's flight starts where the carried miniature showed
  (`RecruitFlightInteraction.Carried`). A drop that places nothing, Esc, focus loss, a cancelled pointer or a new revision
  put the checkpoint back and the picture glides back to its slot (`carried-recruit-returning`, 0.22 s under
  `.motion-enabled`) and goes.
- **Reduced motion and Skip animations**: no flight is launched (`MapCanvas.MotionReduced`), the Muster recruit arrives in
  place as before and a planned ghost shows at once; a flight in the air lands at once.
- **Tests.** EditMode `RecruitPlacementWatchTests` (the baseline, new and taken-back recruits, a new scope),
  `ReserveFlightTests` (a Muster recruit arrives with its flight and never again in place; a view without it lapses it; a
  planned ghost shows it only once its flight lands; the start on the slot's picture at its scale). PlayMode
  `MusterNetworkTests.UI454AClickedRecruitFliesFromItsReserveSlotToItsPlace` (on the slot's picture first, in the carry
  layer, nearer its place frame by frame, landed within 2 px at the figures' scale, gone, no arrival in place; pictures
  `ui454-flight-rise`, `-arc`, `-landed`) and `UI454UnderReducedMotionARecruitIsPlacedWithoutAFlight`;
  `BoardInteractionTests.UI454AReserveFigureIsCarriedAsAMiniatureAndItsDropSendsTheClicksStep` (the icon over the panel,
  the miniature in the carry layer over the map, `CarriedTarget`, the landing mark with "Recruit Gondor Elite", the drop's
  payload equal to the clicks', Esc sends nothing; pictures `ui454-carry-icon`, `ui454-carry-miniature`) and
  `EliteDragAndPoliticalEmblemNeedOnlyOneCommitEach`; `RulerTokenMenuTests.DainsIronGuardStartsFromItsIconInTheCharactersTab`
  (Iron Guard's figure carried over the map, `ui454-iron-guard-carried`); `ExpansionAbilityNetworkTests`.

## Flight profile (UI-404)

Flights run between stand points: the previous place at the source and the current place at the destination, one
flight per destination marker, rendered with the same miniature.

- 0–15 %: lift; 15–85 %: arc of height `clamp(.12 × distance, 20, 80)` px; 85–100 %: landing with a short settle.
- The shadow follows the ground. The duration bounds stay as today (0.65–1.05 s).
- Handoff: `Figures.Expect` (figure keys, point, pose, expiry about 5 s) lets a lifted or dropped figure start where it
  is, never from its source; an attack, a rejection, a disconnect or the expiry returns it to its base.
- Reduced motion and Skip behave as before.

**As built (UI-404, 2026-10-30): a moving figure rises, flies in an arc and lands on its place.**

- **Endpoints** (`Runtime/Map/Figures/FlightPlaces.cs`). Every sync records, before the new places are assigned, where
  each unit and character stands (`MapCanvas.SyncMarkers` → `FlightPlaces.Record`). A flight leaves from that previous
  place and lands on the place its figures stand on now: its force's stand (`MapCanvas.ForceFeet`; a force the map does
  not show yet, its gate or `LandingPlace`), a character's own stand (or `MapCharacterFigures.NextPlace`), the
  Fellowship's region `fellowship` place. A Companion separating from a Fellowship whose position is known leaves from
  the Fellowship's place (`BoardFigurePosition.InFellowship`); from a hidden one it arrives in place, as before.
  `BoardFigurePosition` gains `Nation` and `UnitKind`, and names its keys (`unit:`, `character:`, `fellowship`).
- **One flight per destination marker and source** (`FigureFlight`, `FlightMiniature`): a force's moving units together,
  each character and the Fellowship alone. It is drawn by the destination's own class at the figures' scale:
  `MapArmyArtwork` with the flying units on the destination's base (a garrison's crenellated one) and their count on the
  plaque (an Initial king among them keeps his crown), `MapCharacterArtwork` on its stand, the Fellowship's figure
  (`MapFellowshipMarker.Dress`, its Hidden or Revealed symbol). Two forces landing on one force are two flights, since
  each leaves from its own place. The miniature's own contact shadow is hidden; the flight's `flight-shadow` (the same
  art at the same spot) stays on the ground. A flight's root keeps its name, `moving-<the first figure's key>`, and never
  takes the pointer.
- **Profile** (`Runtime/Map/Figures/FlightProfile.cs`). 0–15 %: it rises in place into UI-403's raised pose (1.10 round
  its feet, 12 design points up, the shadow at .8 and .35), eased out. 15–85 %: it moves along the ground on a
  smoothstep, in a parabola of `clamp(.12 × screen distance, 20, 80)` px above that pose whose top is half way; its
  shadow follows the ground under it, up to a fifth smaller and two fifths fainter at the top. 85–100 %: it drops from
  the raised pose onto its place (accelerating, the first 60 % of the landing), then settles with a hop of 2 design
  points.
  The last frame is the miniature standing on its place at the stand's scale, so the stand that takes over shows the
  same picture: no snap. The duration is unchanged, `clamp(.65 + board distance / 1600, .65, 1.05)` s, and an arrival in
  place .45 s.
- **Transform only.** The holder is a `MapStand` on the ground point (translate and scale); inside it the pose
  (`flight-lift`, origin at the feet: translate and scale) and the shadow (`flight-shadow`: scale and opacity), all
  `DynamicTransform`, written each frame only when they change. No layout property, no `GeometryChangedEvent`, no
  filter, no transition on these parts (Styles/Armies.uss, `figure-flight`). Each flight keeps its own clock.
- **Hand-off** (`MapFigureMotion.Expect(keys, point, pose)`, `ExpectSeconds` = 5). As a step leaves
  (`TurnInteraction.Submit`, before the command is sent), `MapCanvas.ExpectFlights(LiftedFigures.OnTheirWay(...))`:
  every raised hand piece holding sent figures expects them at its place, `Raised`; a card's planned flight or journey
  expects its figures `Standing` at its faint figure (the destination's planned place). The hand keeps an expected piece
  raised whatever the draft says (an accepted step clears the draft before its view arrives). When the view takes the
  figures, the hand lets the piece go at once and the flight starts from that point in that pose: a raised start skips
  the lift (it begins at 15 %) and keeps the hand's gold rim, so the same miniature goes on from the same spot; a
  planned figure steps from its faint figure onto its place. An expectation lapses (`Lapsed`) on a rejected receipt
  (`Figures.Lapse`), a disconnect (`SetConnected(false)`), a reset (focus loss) or after 5 s: the hand drops its piece
  back onto its base and keeps it there while the draft still sends it; a later view flies it from its base like any
  flight. An attack is never on its way (UI-403), so it expects nothing and its army comes back to its base.
- **Taken over mid-air (UI-95).** A projection that moves a flying figure again ends that flight where it was last
  drawn, and the new flight starts there, at that lift and height: its old height fades out over the new arc, and one
  interrupted while landing first rises again. Flights whose figures it does not move go on while the projection still
  has them where they land; a figure that left the board ends its flight.
- **An opponent's march** flies only the recorded movers (one flight with their count) from their force's place, which
  the rear guard keeps (UI-306's staged board).
- **Reduced motion and Skip** as before: an arrival on the destination's place (no lift, no arc, .45 s, fading from 1
  to .4, `figure-arrival`), and Skip lands every flight (`Land`, `Quiet`).
- **Removed.** The 96 × 84 box of images and its count (`.moving-figures`, `.moving-figure-count`), the old
  `.figure-arrival` and `.figure-garrison` borders, `MapFigureMotion.FlightFeet` and `LiftedFigures.Held`.
- **Cost.** Per frame and flight one profile evaluation and at most seven style writes (translate, scale, opacity);
  flights are built on a projection only.
- **Tests.** EditMode `FlightProfileTests` (the duration and arc bounds; lift, arc top, landing and the last frame
  standing; a raised start begins at the arc in the hand's pose; a mid-air start keeps its height) and `MapFlightTests`
  (stand points before and after, one flight per marker with exactly the movers and the rear guard on its place; the
  hand-off with no second lift; a rejection, a disconnect and the expiry put the figures back and keep them there; a
  planned flight's figure flies from its faint figure), `ReplayEffectsTests`, `MapFigureHandTests`. PlayMode
  `FigureMotionTests` (`UI404AMarchRisesArcsAndLandsOnItsPlace`: frame by frame the ground runs straight, the shadow
  lies on it and the miniature above it, the arc's top, the landing on the place at the stand's scale, pictures
  `ui404-march-lift`, `-arc-top`, `-landing` and `ui404-landed`; `UI404AClickLiftedArmyFliesOnFromItsRaisedPose`:
  `ui404-handoff-raised`, `-start`, `-arc-top`, `-landing`, `-landed`; the UI-95 take-over mid-air),
  `MapClickMoveNetworkTests` (a real sent march's first frame is already raised and the hand is empty), and
  `FigureMotionNetworkTests`, `FellowshipMotionNetworkTests`, `ArmyLeaveTests`, `ArmyEmergeTests`, the opponent's
  presentation classes, `FigureHandTests`, `BoardInteractionTests`, `MovementNetworkTests`, `TravelGestureNetworkTests`,
  `CharacterMiniatureTests`, `MapVisibilityTests`, `MapStandLayoutTests`.
- **Not yet.** UI-405's drop will expect a dropped figure at its drop point (the same `Expect`; built 2026-10-30, "As
  built (UI-405)" above); the Fellowship's base and the tokens are UI-406's (built 2026-10-30, "As built (UI-406)"
  below).

**As built (UI-406, 2026-10-30): tokens, settlements, kings and the Fellowship's figure lie on the table.**

- **The token** (`MapArmyArtwork`, `Styles/Armies.uss`, decision 3). Far out an army is a puck lying on the table: the
  painted enamel puck (`armies/token-puck-free`, `-shadow`: a domed top face, a side band for its thickness and a gilt or
  bronze rim; a garrison's `token-puck-garrison-*` is the same puck walled with crenellated stone, so the garrison keeps
  its UI-125 battlements), 44 × 35.2 design points in its 44 × 44 box, over the `contact-shadow` a point right and below
  it. Its nation emblem (16 points) lies on the top face and its count on the band at the lower right, as before. The
  state is a rim drawn round the puck's outline (`rim-token`: `rim-eligible` legal green, `rim-selected` gold for a
  chosen or armed force, `rim-pressed` dim gold, `rim-ability` amber, `rim-moved` a veil, with the ✓) instead of twenty
  whole pictures; the colour meanings are unchanged, and ChoiceGlow's halo is a capsule round the puck. A garrison's
  token is now as large as a field's (it was 4 points taller). The plinth element and UI-125's `base-*` and `token-*`
  pictures are gone; the legend shows the puck with its rims. The token's size is UI-401's `clamp(40 × zoom, 32, 44)`.
- **A character's token** (`MapCharacterArtwork`). The portrait in its side's medallion is a thick coin: a darkened copy
  of the medallion 2.5 points below gives its edge, and the contact shadow lies under it.
- **Settlements** (`MapSettlementGems`, `MapSettlementVisual`, `SettlementGem`). Every gem is a stone on the table: a soft
  shadow falls from it down and right (two translucent fills of its silhouette, the outer one 1.16 times larger, offset
  1.6 × 1.9 points), and a band of side facets below its lower edges (below a Stronghold's gold setting) gives it a
  thickness of a fifth of its size (1.2–1.6 points), each facet shaded by the light from the upper left, darker than the
  crown and in its own colour. Painter2D only, no filter. The control colour is unchanged (UI-195: sapphire, ruby, quartz).
- **Gem size.** `clamp(zoom / reference, .85, 1.4)` of its UI-195 size (`MapStandScale.Gem`). The reference is the
  worst-case Detail zoom (`MapStandSlots.WorstCaseZoom`, 0.80 points per board unit), where the stand places (UI-422)
  keep 2 units clear of the gems as drawn at their UI-195 size; between the clamps a gem keeps one size on the board like
  the figures, so it never grows into a figure's place (EditMode
  `MapStandScaleTests.TheGemScalesWithTheMapWithinItsClampsAndNeverOutgrowsTheStandPlacesMargin`). On the whole map
  that is about .85 of the old size at 1366 × 768 and 125 %, 1.04 at 100 %, 1.17 at 1920 × 1080 and 125 % and 1.4 at
  100 % (the fit zoom over the worst-case Detail zoom); zoomed in it grows to 1.4.
- **Zero repaints on camera moves.** The gems left the screen-space foreground layer (which drew them again at every
  camera frame). Each is its own element on a layer of its own (`map-settlements`, above the region names and under the
  foreground, the Mordor track and the stand layer), painted once at its design size round the middle of a 32 × 32 box,
  and stood on its anchor by a `MapStand` holder: a pan, a zoom or a glide writes only the holder's translate and scale
  (`DynamicTransform`, and only when they change). A gem repaints only when its settlement or its controller changes
  (`MapCanvas.UpdateData` → `MapSettlementGems.Apply`). The map's vector layers are untouched. PlayMode
  `TabletopObjectsTests.CameraMovesRepaintNoVectorLayerAndNoGemAndACaptureRepaintsItsGemOnly` pans and zooms 40 frames and
  glides to Minas Tirith: `MapRenderer.Repaints` and `MapSettlementGems.Repaints` stay the same while each gem's scale
  follows the zoom; a capture of Osgiliath repaints exactly one gem.
- **Names and gems** (`MapRegionNames.ClearOfGem`). The name points (UI-186, UI-422) keep clear of the stand places,
  not of the gems, and a name may stand just under its settlement; zoomed in, a gem 1.4 times its old size reached into
  some (Minas Tirith's). A name its own settlement's stone (crown and side band, `MapSettlementGems.Bounds`) reaches
  into now moves just clear of it, 1 point away — down when it stands below the stone, up when above — as long as it
  stays within its vertical room at its point; otherwise it stays where it was. Per frame that is one rectangle test
  per shown name, and the name's transform is written as before. (Superseded by UI-426: the step may also go sideways,
  and a name that cannot step clear hides, "As built (UI-426)".)
- **Kings** (`MapSovereignMarkers`). A Corrupted king's marker is a small puck in its 30 × 34 box, at a fixed screen size
  as before (since UI-425 on its own place, on its puck's lower edge, keeping its board size past the whole map): the dark iron puck with an ember rim (`kings/puck-corrupted`, Codex, 30 × 24 points) over the contact shadow,
  the crown under the ember Eye (`kings/emblem-corrupted`, UI-274's marker without its disc) on its top face. When a click
  chooses him, the steady gold halo lies behind the puck (`sovereign-glow`), no pulse.
- **The Fellowship's figure** (`MapFellowshipMarker.Dress`). It stands on the characters' round Free Peoples stand
  (`stand-character-free`, 16 units wide: 28.8 × 20.2 design points), its feet on the middle of the stand's top face
  (32, 64), over the contact shadow down and right; its box grew to 64 × 80 below the feet to hold them, and the figure
  keeps its 64 × 72 box above. UI-201's blue underline is gone (the stand is the side's colour). On the Mordor track it
  stands on the step on the same base at UI-283's size (the scale is 80 or 64 points over the figure's former 72-point
  box). Its flight (UI-404) now has a standing shadow, as the other miniatures, and hides the figure's own one in the air.
- **Art** (`graphicalassets/stands`, README). Three new Codex generations: `token-puck-garrison-free`, `-shadow` and
  `puck-corrupted`, keyed by `key.py` (a king's puck into `kings/`); the rim and the emblem are SVG in the UI kit
  (`armies/rim-token`, `kings/emblem-corrupted`). Every picture is replaceable in place: the code and the USS name the
  files only, and a new picture of the same size needs no change.
- **Cost.** Per camera frame each gem writes at most a translate and a scale; tokens, kings and the Fellowship were
  already transform-only (UI-401). No layout property, no `GeometryChangedEvent` callback and no CSS filter.
- **Tests.** EditMode `MapSettlementGemsTests.TheStoneHasASideBandBelowItsGirdleInItsOwnColour`, `MapStandScaleTests`,
  `UiArtKitTests`, `SovereignMapTests`, `FlightProfileTests`; PlayMode `TabletopObjectsTests` (both levels in four layouts,
  pictures `ui406-far-*`, `ui406-near-*`, `ui406-mordor-track`, `ui406-legend`; the repaint count; Minas Tirith's name
  clear of its gem; the legend's pucks and rims),
  `MapArmyArtworkTests` (each token's puck, its shadow and its state's rim), `MordorLayoutTests` (the base on its step),
  `MapRegionNamesTests`, `MapVisibilityTests`, `FellowshipTrackLayoutTests`, `CharacterMiniatureTests`,
  `FigureHandTests`, `MapStandLayoutTests`, `FigureMotionTests` and `FellowshipMotionNetworkTests` (the flights' shadows),
  `OpponentExpansionPresentationTests` (the kings' markers), `ArmyEmergeTests`, `MapMarkerEdgeTests`, `LayoutTests`.

**As built (UI-425, 2026-10-30): no map object hides a force's count or covers its own settlement.**

- **Labels.** A force's label is what tells its strength: its token's count badge far out, its plaque (the emblems and
  the count) at the figures' level. UI-422's places kept the figure rectangles apart but not the plaque, which hangs
  14.8 units below the feet, nor the tokens, which far out are larger on the board than the footprints. The export now
  keeps every field force's and garrison's label clear of every other stand of its region drawn in front of it — in a
  siege the other side, and everywhere the force's own characters, the planned recruits and the Fellowship: the plaque
  at the footprint scale against the figures (an army's 40 × 55.6 units down to its plaque, a character's 16-unit column
  down to its stand's front), the count on the whole map of the worst case against the tokens and medallions. Forces of
  neighbouring regions keep clear of each other's labels at one level (figures at scale 1, tokens on the whole map):
  the export first lays every region out, and where two regions' forces then cover each other's labels, the one of the
  two that keeps more of its capacity is laid out again around the other's forces and stays bound to them
  (`Rounds.settle`; Fords of Isen's field, for one, now stands west of its settlement). A tight region's token beside a
  roomy neighbour's figures at the Detail zoom is not checked: that would cost Orthanc its figures.
- **Placement order.** A stronghold's field force is now placed right after the garrison and before the garrison's
  characters, so they keep clear of its label; the beam runs over the gate and the field. The Helm's Deep siege still
  fits at scale 1: the garrison at the gate (752, 833), the besieger's field north-west of it at (712, 813) with its
  plaque clear of the garrison's figures, and the garrison's characters to the west, the east and behind the gate.
- **Tokens on their places.** A field force's token stands on its puck's lower edge (`MapArmyArtwork.Feet`), as its figures
  stand on their feet, so it rises above its place, and the settlement's gem below the place stays in view; a garrison's
  walled token lies with the middle of its top face on the gate (a raised puck would reach over the stone above Moria's and
  Minas Morgul's drawings); a character's medallion stands on its coin's lower edge (`MapCharacterArtwork.Feet`). No place
  lets a token, a medallion or a king's marker lie on a settlement's stone on the whole map of the worst case (the gem at
  .85 of its size there, with its side band, plus a point of margin).
- **Past the whole map** (zoomed out onto the canvas margins, below 0.67 points per unit on 1366 × 768 at 125 %) the
  tokens, the medallions, the kings' markers and the gems keep the board size they have on the whole map of the worst
  case (`MapStandScale.Distant`, `WholeMap`), so what the places keep clear there stays clear at every zoom.
- **Kings.** A Corrupted king's marker stands on its own place, `sovereign` in `stand-slots.json` (`RegionStands.Sovereign`,
  `MapCanvas.BesideForces`), on its puck's lower edge: the point nearest the region's last character place (its UI-401
  place) off the armies' bases on which, from the whole map to the closest zoom, it lies on no count or plaque of a force
  it stands in front of — the region's field, garrison and planned forces and its neighbours' — and on no gem. In 53 of
  the 105 regions it is still the last character place; at Minas Tirith it is, and the field force it covered moved to
  (1012, 919).
- **Capacity.** Seventeen regions are below 1 (fifteen before): Western Mirkwood 0.57, Minas Morgul 0.60, Woodland Realm
  0.62, Gladden Fields 0.70, Rhosgobel 0.70, Dol Guldur 0.71 (0.82), Southern Mirkwood 0.80 (0.85), Lossarnach 0.81
  (0.97), South Anduin Vale 0.85 (0.96), Ash Mountains 0.85 (0.86), Moria 0.85 (0.87), High Pass 0.92, Old Ford 0.95, Edoras
  0.96 (0.98), Lorien 0.96 (≥ 1), Narrows of the Forest 0.96 (≥ 1), Old Forest Road 0.96; every region still shows figures
  before the closest zoom. 90 of the 105 regions' places moved; capacities above 1 now stop at 1.0–1.02 in many regions.
- **Format.** `stand-slots-2` (the `sovereign` point; `MapStandSlots.Validate` rejects a region without it); since UI-522
  `stand-slots-3`, which adds each region's `fieldRow` and, for a stronghold, `garrisonRow`.
- **Row reach** (UI-522, 2026-11-07, ARMY_MAP_UX). A force that shows one figure of each kind stands them side by side on
  a row stand up to 100 units wide (71 before UI-523, whose figures stand at least 85 % of an army's height). The places
  stay as laid out; each field and garrison place records how far its row stand may reach before and after its feet (20
  to 50 units a side, in half-unit steps, its figures up to 40 units tall): inside the region (a stadium,
  its round ends an army's, along its spine), off the other force's and the king's marker's bases, on no gem, other
  fortress drawing or name, and on no label of a force it stands in front of. The client narrows the side a character,
  the Fellowship or the planned recruits stand on at the moment (`MapArmyRowRoom`). Reserving a full row in the layout
  itself left 42 regions below capacity 1 instead of 15, and re-laying each region around a row widened barely a
  quarter of them; the owner chose a row as wide as each place allows.
- **Checks.** `python3 scripts/export_map_stands.py --check` lays everything out again, runs UI-422's checks and UI-425's
  (`verify_readability`: tokens off the gems, labels clear within a region and across neighbours, kings' markers) and
  compares the result with the file; it now takes about 3 minutes. EditMode `MapStandSlotsTests`
  (`NoTokenOrKingsMarkerLiesOnASettlement`, `NoForcesLabelLiesUnderAStandOfItsRegion`, `AKingsMarkerLiesOnNoForcesLabel`)
  check the file on their own; `MapStandScaleTests` the token feet and the board size past the whole map.
- **Cost.** Nothing new per frame: the places, the kings' points and the token feet are fixed; a king's marker writes its
  translate and scale like any stand.
- **Tests.** PlayMode `TabletopAcceptanceMatrixTests` assert in every static frame (`TabletopFrameChecks.AssertReadable`)
  that no count or plaque lies under a stand or a king's marker drawn in front of it (points along its middle line inside
  that stand's picture: a token's puck and badge, a medallion's coin, an army's visible part, a character's column and
  stand, a king's puck), that no token, medallion or king's marker lies on a settlement's stone, and that no name lies on
  its own stone; `TabletopObjectsTests` assert the same with Minas Tirith's Corrupted king at both levels in four
  layouts. Before the fix they failed with the UI-407 defects (below); after it they pass.
- **Verification** (2026-10-30, macOS, Unity 6000.6.0f1, `expansions` 3ee14b42 plus this change):
  `python3 scripts/export_map_stands.py --check` PASS (3 min 2 s, after a fresh export: deterministic); EditMode, the
  whole suite, 1330/1330 PASS; PlayMode, one class per run: `TabletopAcceptanceMatrixTests` 4/4 (the two static matrices,
  both carries with edge pan and flights), `TabletopObjectsTests` 4/4, `MapRegionNamesTests` 2/2, `MapStandLayoutTests` 1/1,
  `MapVisibilityTests` 2/2, `CharacterMiniatureTests` 3/3, `FigureHandTests` 5/5, `OpponentExpansionPresentationTests` 12/12
  (the kings' markers), `MapArmyArtworkTests` 1/1, `MapMarkerEdgeTests` 1/1. The same assertions before the change:
  `TabletopAcceptanceMatrixTests`' two static tests, `TabletopObjectsTests`' first and the new `MapRegionNamesTests`
  sweep failed (0/4).

**As built (UI-426, 2026-10-30): a region's name stays clear of its own settlement's gem.**

- **Steps** (`MapNameSteps`, found once with the name points, `MapRegionNames.StepsOf`). For each settlement region's name
  and each way — down, up, left, right — two bounds in board units: the free distance from its point to the region's
  border that way (`MapRegionNamePoints.Reach`, on the simplified borders), and how far its largest extent at its point
  (`BoardRect` of the lines it breaks into there) goes before it meets a stand place or a fortress
  (`MapStandSlots.NameObstacle`). The name points themselves are unchanged.
- **The frame** (`MapRegionNames.ClearOfGem`): one rectangle test per shown name — does its own stone reach into it? If
  so, of the four steps that leave it 1 point clear of the stone, the shortest one is taken that keeps its edge inside the
  border on its middle line and its extent clear of the stand places; where the places leave no such step (the export
  packs them close to the names' points), the shortest one that only keeps it inside its region — a stand standing there
  then hides it, as at its point; and where no step is allowed at all, the name hides for that frame rather than lie
  under its own stone. The rest is arithmetic; nothing is laid out.
- **Results.** Helm's Deep's name, squeezed between its gem, the besieger's place and the region's south-western border,
  hides where no step clears the stone (the matrix's 2.2 × whole-map frames at 125 %) instead of being split by it; on the
  whole map of the stress board and at the closest zoom it stands left of its gem (`ui426-names-fit-*`,
  `ui426-helms-deep-closest-*`). Minas Tirith's name steps down below its stone, as under UI-406.
- **Not done.** No name breaks its line differently to dodge its gem: a label's lines are set once (UI-186's break at the
  middle space where the name is wider than its region), and changing them on a zoom would lay the label out again.
- **Tests.** PlayMode `MapRegionNamesTests.NoNameLiesOnItsOwnSettlementAtTheWholeMapOrTheClosestZoom` (the whole map and
  the closest zoom on every settlement, four layouts), `TabletopFrameChecks.AssertReadable` in every matrix frame,
  `TabletopObjectsTests` (Minas Tirith's name visible and clear of its gem).

**As built (UI-427, 2026-10-30): the Fellowship's figure shows ◇ Hidden or ◆ Revealed.**

- **The defect.** `MapFellowshipMarker.Apply` wrote ◇/◆ as the button's text. Since UI-406 the base, the contact shadow
  and the figure, children drawn after the button's own text, covered it, so no frame showed the state (UI-406's report;
  FELLOWSHIP_UX "Fellowship on track" requires it).
- **The plaque** (`Runtime/Map/FellowshipStateBadge.cs`, `Styles/Armies.uss` `.fellowship-state`). A code-drawn plaque
  hangs on the front of the base, as an army's plaque hangs on its stand: 24 × 16 of the figure's design points (1.8 a
  unit) from the feet line down, centred under the feet, inside the figure's 64 × 80 box, the Free Peoples' blue enamel
  with a gilt rim. Its diamond is painted by `Painter2D` in ivory, 9.5 × 13 points: outlined (◇, a 2.2-point line) while
  the Fellowship is hidden, filled (◆) once revealed (`fellowship-hidden`, `fellowship-revealed`). It is the figure's
  last child, so nothing of the figure covers it, and it repaints only when the state changes. The button has no text
  any more; `MapFellowshipMarker.Symbol` and `Hidden` still give the state.
- **Both levels.** It scales with the figure: on a region zoomed in 13–18 points wide (the figure at 0.56–0.75 of its
  design size), on the Mordor track 21–27 (UI-283's 80 or 64 points over the figure's 72). Far out a region's figure is
  drawn as small as 0.45 (it shows there only while it offers a click, UI-280, UI-282); the plaque never draws smaller
  than at 0.62 (`FellowshipStateBadge.SmallestScale`): it grows round the middle of its top edge, so it stays below the
  feet, and the figure's visible part (its `MapStand` bounds, which names keep clear of) reaches down to it. The scale is
  written only when the zoom changes it. The flight (UI-404, `FlightMiniature.Fellowship(hidden)`) draws the same plaque
  and fits it the same way (`FlightMiniature.Scaled`).
- **Clicks.** The plaque and its diamond take no pointer: the figure's click is still the move, the declaration or the
  hide (UI-236, UI-280, UI-282), and its ChoiceGlow and tooltips are unchanged (they already name the state).
- **Cost.** Nothing per frame but the plaque's scale, written only when the zoom changes it below 1.116 points a unit
  (0.62 × 1.8); no layout property, no `GeometryChangedEvent`, no filter.
- **Tests.** EditMode `FellowshipFigureHideTests.ThePlaqueOnTheBaseShowsHiddenOrRevealed` (a region and the track) and
  `FarOutThePlaqueKeepsAReadableSize`.
  PlayMode `TabletopObjectsTests` (`AssertFellowshipState`, `AssertDiamondPainted`: in four layouts on its region zoomed in
  and, offering a click, on the whole map, and at 1366 × 768 on the track zoomed in and on the whole map, the state
  hidden, revealed and hidden again: the plaque shown on the base's front below the feet, at least 9.5 points tall, no
  map object over it and a click there the figure's, and the diamond read from the frame — an ivory middle for ◆, the plaque's blue middle
  inside an ivory outline for ◇; pictures `ui427-near-*`, `ui427-far-*`, `ui427-track-near-*`, `ui427-track-far-*`),
  `MordorLayoutTests` (the UI-156 acceptance screens: both sides, steps 0, 3 and 5, hidden and revealed, four layouts; and
  the track in four layouts at both levels, where the Hunt window does not lie over it), `OpponentHuntPresentationTests`.
- **Verification** (2026-10-30, macOS, Unity 6000.6.0f1, `expansions` 25636789 plus UI-427 and UI-428): EditMode, the
  whole suite, 1335/1335 PASS; PlayMode, one class per run: `TabletopObjectsTests` 4/4, `MordorLayoutTests` 5/5,
  `OpponentHuntPresentationTests` 13/13, `FellowshipTrackLayoutTests` 2/2, `FellowshipMotionNetworkTests` 1/1,
  `FigureMotionTests` 6/6, `CharacterMiniatureTests` 3/3. With the plaque left off the figure (as before the change) the
  new assertions failed: EditMode `ThePlaqueOnTheBaseShowsHiddenOrRevealed` (both cases), `TabletopObjectsTests` 2/4,
  `MordorLayoutTests` 3/5. Pictures checked: `ui427-near-*`, `ui427-far-*` (four layouts, both states; the worst case,
  1366 × 768 at 125 % on the whole map, still reads ◇ against ◆), `ui427-track-near-*`, `ui427-track-far-*`.

**As built (UI-428, 2026-10-30): a drop on any map object is a drop on its region, and the landing mark shows whole.**

- **The drop on the Fellowship's figure** (UI-405's open point). `FigureLiftInteraction.Region` read only the terrain, an
  army marker or a character's miniature, so a figure carried onto the Fellowship's figure in a lit target found no region
  and glided home. The region a map object stands in is now one rule, `DraftToken.StandsIn`: the terrain's region, an
  army marker's, a miniature's, the Fellowship's figure's (its token carries its region; on the Mordor track, which is no
  region of the map's graph, the region under its step) and a Corrupted king's marker's (its token carries his capital).
  The drag target and the drop read it, so a drop on the Fellowship's figure or a king's marker is that region's click
  (`TurnInteraction.ChooseRegion`), payload and all. Every other map element either takes no pointer (gems, names,
  labels, the hand, the landing mark, the planned ghosts) or has no token and reads the terrain under it (the Mordor
  track's stones and gates, a planned recruit's ×). For other drags `DraftToken.AsRegion` turns the Fellowship's figure
  into its region as it does a character's miniature; a king's marker keeps its own drop (a die on it chooses him,
  UI-274).
- **The landing mark under the carried army** (UI-407's defect 5). The ring lies on the table under the stands, and a
  carried army whose feet were on its place covered all but its rim. A piece away from its source — carried, or resting
  where it was dropped — is now held higher (`MapHandPiece.CarriedRise`, `hand-carried`: 34 design points instead of 12,
  still grown 1.10 round its feet), so its lowest part, the plaque 22 points below the feet, hangs 4.2 points above the
  ring's top edge when its feet (its shadow) are on the place: the whole ring shows round the shadow, the army clearly
  over it. The rise transitions with the pose (0.18 s up, 0.22 s down, only a class change; Reduced motion: at once). A
  piece gliding home comes down to the raised pose; one that drops back onto its base drops from there. The caption
  hangs above the held figures (`LiftedBounds` reads the rise). A sent drop stays at that height where it was dropped, and
  its flight starts there: the hand hands the flight the extra height (`MapHandPiece.Handoff`, `FlightStart.Height`,
  `MapFigureMotion.Expect(keys, start)`), which the arc leaves behind as it does a flight taken over mid-air (UI-95), so
  nothing snaps down.
- **Cost.** No new per-frame write: a class on grab, drop and home; the flight's height is part of its profile.
- **Tests.** EditMode `DraftTests.EveryMapObjectStandsInTheRegionADropOnItLandsIn`,
  `FellowshipFigureHideTests.TheFiguresTokenStandsInItsRegionOrUnderItsStep`, `SovereignMapTests` (the marker stands in
  Minas Tirith; its token keeps its own drop for a die), `MapFlightTests` (a carried army is held higher, comes down when it goes
  home, and a dropped one's flight starts at that height; a click-lifted one at none). PlayMode `BoardInteractionTests`
  (`UI428ADropOnTheFellowshipsFigureSendsItsRegionsClick`: an army carried onto the Fellowship's figure in Osgiliath lights
  Osgiliath and its drop sends the click's payload, JSON-equal; `UI405TheCarriedArmyLightsItsTargetAndFliesOnFromWhereItWasDropped`:
  carried with its feet on Osgiliath's planned place, no drawn part of the hand on the ring,
  `TabletopFrameChecks.AssertLandingMarkClear`; pictures `ui428-carried-over-the-fellowship`, `ui428-carried-on-its-place`),
  `TabletopAcceptanceMatrixTests` (the 16 carried frames now carry each side's army with its feet on its landing place and
  assert the ring clear), `FigureHandTests` (the carried pose at 34), `MapContourInteractionTests`.
- **Verification** (2026-10-30, the same run): EditMode 1335/1335 PASS; PlayMode `BoardInteractionTests` 22/22,
  `FigureHandTests` 5/5, `TabletopAcceptanceMatrixTests` 4/4 (all 72 frames), `MapContourInteractionTests` 6/6,
  `FigureMotionTests` 6/6, `FellowshipMotionNetworkTests` 1/1, `CharacterMiniatureTests` 3/3. With the old drop rule,
  regionless tokens and the carried pose at 12 the new assertions failed: EditMode
  `TheFiguresTokenStandsInItsRegionOrUnderItsStep`, `SovereignMapTests.EachKingStandsOnTheMapByHisState`,
  `MapFlightTests.ADroppedArmyHandsTheDropPointToItsFlight`; `BoardInteractionTests` 19/22 (the drop on the
  Fellowship's figure found no target; the carried army was not held higher); the matrix 2/4, the ring under the carried
  army's stand in all 16 carried frames (both sides, four layouts, both levels); `FigureHandTests` 4/5. Pictures checked:
  `ui428-carried-on-its-place`, `ui428-carried-over-the-fellowship`, `ui407-carried-{fp,sh}-*-near` (the whole ring under
  the held army).

## Defaults the owner can change at review

- A character click opens no region panel. As built (UI-402): a click on the player's own character that chooses
  nothing does what its region's click does, without the panel; an enemy character's click is its region's.
- A stacked pair's click chooses its front character (UI-402).
- Far out a character is its portrait in its side's medallion (UI-402); since UI-406 the medallion is a thick coin with
  a contact shadow.
- A Nazgûl inside a mixed stack lifts with its base.
- The lifted figure is clipped to the map; since UI-444 a carried one stands above the HUD, whole on the screen.
- The lifted part of a split rises on a base of its own; the figures left behind stay on the source's base behind and
  below it, so at the default pose mostly their plaque and count show under the raised base (UI-403).
- A sent move stays raised over its place until the view arrives (UI-403; UI-404 lets the flight start from there),
  at most 5 s (`MapFigureMotion.ExpectSeconds`); then, or on a rejection or a disconnect, it comes back down.
- A flight handed over by the hand keeps the hand's gold rim until it lands (UI-404).
- A planned flight's or journey's figure flies from its faint figure at the destination onto its place, not from its
  source (UI-404).
- The arc's height is measured on the screen, so it follows the zoom; the settle is a hop of 2 design points (UI-404).
- A lone character's landing mark is the army's ring at .35 of its size on the region's first free character place;
  while the lifted figure is out of view the caption hangs above the ring (UI-403).
- Edge pan works during a figure drag after a ~250 ms dwell (UI-405: `MapEdgePan.DragDwellSeconds` = 0.25 s in the
  26-point edge zone, at the usual edge-pan speed).
- A press carries every raised figure of the unsent move, from the army's base or a chosen character: the army and the
  characters chosen with it go together, each keeping its place beside the pointer (UI-405).
- The drag target is the region under the pointer, which holds the carried figure, as the release reads it; a legal
  target under it lights in its own colour more strongly (green .50, red .52 instead of .34 and .36), any other region
  only takes the hover tint (UI-405).
- A drop that sends nothing puts the draft back as it was before the press: a figure the press itself picked up goes
  home onto its base, one a click had chosen before goes home raised over its base, still chosen (UI-405). The glide home
  takes 0.22 s.
- A dropped figure whose step was sent stays raised where it was dropped, not over its source, until the view arrives
  (at most 5 s), and its flight leaves from there (UI-405).
- A press on a figure whose click would send a step at once (an ability used on it) does not lift it; that press stays
  a click (UI-405).
- The region panel turns translucent and lets the drop through while a figure is carried, as for a reserve figure
  (UI-405).
- Where a place's capacity is too small, the region shows tokens.
- A garrison's token is the field's puck walled with crenellated stone (Codex art), the same 44-point size (UI-406).
- A token's state is a rim round its puck in the stands' colours — green eligible or a king's target, gold selected or
  armed, dim gold pressed, amber ability owner, a veil and ✓ when marched — with ChoiceGlow as a capsule round the puck
  (UI-406).
- A gem is `clamp(zoom / reference, .85, 1.4)` of its UI-195 size with the worst-case Detail zoom (0.80 points per board
  unit) as the reference, so far out on a small screen it is a little smaller than before and on a large one up to 1.4
  times larger; its side band is a fifth of its size and its shadow falls 1.6 × 1.9 points down and right (UI-406).
- A Corrupted king's marker is a dark iron puck with an ember rim and the crown under the Eye, at its fixed 30 × 34
  points (UI-406).
- A region's name that its own settlement's gem reaches into moves just clear of the stone (1 point) within its room in
  the region; the name points themselves are unchanged (UI-406). Changed by UI-426: it takes the shortest of the four
  steps (down, up, left, right) that keeps it clear of the stand places, else the shortest that keeps it inside its
  region, and hides where none does.
- A field force's token stands on its puck's lower edge on its place (UI-406: centred on it), so it rises above the place
  like the figures; a garrison's walled token lies with the middle of its top face on the gate (UI-406: centred); a
  character's medallion stands on its coin's lower edge (UI-406: centred); a king's marker on its puck's lower edge
  (UI-425).
- Zoomed out past the whole map of the worst case (0.67 points a unit), tokens, medallions, kings' markers and gems keep
  their board size instead of their readable 32-, 18- and 30-point and .85 minimum (UI-425).
- No force's count or plaque lies under another stand of its region drawn in front of it — in a siege the other side,
  and everywhere its own characters, the planned recruits and the Fellowship — nor under a neighbouring region's force at
  one level; a stronghold's field force is placed before the garrison's characters. This moved 90 regions' places and
  put seventeen regions below scale 1 instead of fifteen (UI-425).
- A Corrupted king's marker stands on its own place, the nearest to the last character place that is off the armies'
  bases and covers no force's label and no gem (UI-401: the last character place) (UI-425).
- The Fellowship's state is a plaque on its base's front — the Free Peoples' blue enamel with a gilt rim, an ivory ◇
  outlined while hidden, ◆ filled while revealed — 24 × 16 of the figure's design points; far out it never draws smaller
  than on a figure at 0.62 of its design size, growing downward from the feet line (UI-427; before it, a ◇/◆ text that
  never showed).
- A carried figure, and one resting where it was dropped, is held 34 design points up instead of the raised pose's 12
  (still grown 1.10), so with its feet on the landing place its plaque hangs 4.2 points above the ring; it comes down to
  12 when it goes home, and a sent drop's flight starts at 34 (UI-428; before it, 12 and the ring under the army).
- A drop on any map object standing in a region — the Fellowship's figure and a king's marker too — is a drop on that
  region; on the Mordor track, the region under the Fellowship's step (UI-428; before it, the Fellowship's figure and a
  king's marker took no carried figure and it went home). A king's marker keeps its own drop for a die (UI-274).

## Acceptance (UI-407)

The automated part of UI-407, run on `expansions` 34bfe20d plus this change (2026-10-30, macOS 15.6.1, Apple M1 Pro,
Unity 6000.6.0f1). The owner's playtest (the checklist below) and the owner's review of the defaults above are still
open: UI-407 is not done until both are recorded.

**Screen matrix** (PlayMode `TabletopAcceptanceMatrixTests`, with `TabletopFrameChecks` and `TabletopCarryScene`). Every
frame is saved as `.artifacts/screenshots/ui407-*.png` before its checks run, and a failed check is collected with its
frame, so one run shows the whole matrix. 72 frames:

| Frames | Board and seats | What each frame asserts |
| --- | --- | --- |
| `ui407-map17-{fp,sh}-{1366,1920}-{1.00,1.25}-{far,near}` (16) | The seeder's `map17-dense` match, both real seats (HTTP/WebSocket): Helm's Deep's garrison of 7 with Strider, Gimli and Legolas besieged by 10 Isengard figures and 3 Nazgûl; Gondor's 14 in Westemnet. Far: the whole map (tokens); near: 2.2 × the whole map on Helm's Deep (miniatures). Nothing is sent. | The level (Detail only near; the focused forces as figures); every force's feet on its place at its board-unit size and design size (`MapStandLayoutTests.AssertOnPlaces`); the stand layer sorted by depth; every shown army's and character's feet inside its own region's contour; no shown region name under a stand; near: no figure on the middle of a settlement's stone, no two bases' top faces into each other, a garrison and its besieger sharing at most a quarter of either's picture. Per layout: both seats see every force on the same board point. |
| `ui407-siege-{fp,sh}-…-{far,near}` (16) | UI-401's Helm's Deep siege with its neighbours (`MapStandLayoutTests.Siege`), seen from each seat | The same, plus UI-401's siege checks (`AssertSiege`: both sides as figures, the nearer garrison in front, Edoras's token until there is room). |
| `ui407-carried-{fp,sh}-…-{far,near}` (16) | A fresh server scene per layout, `fp-army-move` and `sh-army-move`: the side's Army die, then a press that crosses the drag threshold on its largest army that can march (Minas Tirith's 5; Dol Guldur's 7, on the full setup board of 100 figures), carried over its first target (Drúadan
Forest; Eastern Brown Lands); since UI-428 with its feet on the place it would take | The miniature under the pointer (its offset from the press kept); the target is the drag target and lights `CarriedTarget`; the green landing mark on the place the army would take; since UI-428 no drawn part of the carried army on the mark (`AssertLandingMarkClear`); the caption above the carried figure; the figure on the map; no army ghost and no text label; the whole army lifted leaves its base empty. Esc puts it back onto its base. |
| `ui407-edge-pan-{fp,sh}-…` (8) | The same scenes, near | Held in an edge zone that can pan: within the dwell the map stays, after it the map has panned more than 20 points, the army is still under the pointer, and the drag target is the region under the pointer again. |
| `ui407-flight-{fp,sh}-…` (8) | The same scenes: the army dropped on its target, so the real server takes the march | The own seat's flight leaves from where the army was dropped (within a board unit) and, about half way, its ground lies on the straight line between its two points, the miniature above it, landing on the destination force's place. |
| `ui407-flight-observed-{sh,fp}-…` (8) | The opponent's seat watching that march | The flight leaves from the force's own stand point and runs on the ground between the stand points, above its shadow, to the destination's place. |

Command: `bash scripts/test-client.sh -testFilter Wotr.PlayTests.TabletopAcceptanceMatrixTests` (the pinned server and
seeder; one class per run). Results: first run **4/4 PASS** (dense board 8.8 s, siege 7.7 s, Free Peoples carry 36.4 s,
Shadow carry 36.2 s) with no finding; the static half again with the occlusion measurements below, 2/2 PASS; and the final
run on the finished change with the classes whose helpers it reuses — `TabletopAcceptanceMatrixTests`,
`BoardInteractionTests`, `FigureHandTests`, `MapStandLayoutTests`, `TabletopObjectsTests` — **35/35 PASS** (21, 5, 1, 4,
4), no finding, the same measurements, and UI-405's drag cost p50 0.299 ms, p95 0.329 ms, max 0.644 ms. The first layout
of each carry test also records the side's projection as it arrived (`.artifacts/ui407-map-drag.json`, `drag-fp` and
`drag-sh`) for the profile's drag workloads.

**Occlusions measured** beyond UI-422's limits (logged per frame as `UI-407 measure`: a pick at a count badge's middle,
or at a settlement stone's middle, that takes another stand's button; since UI-425 and UI-426 asserted in every static
frame instead, `TabletopFrameChecks.AssertReadable`):

- The besieger's count is under the garrison at Helm's Deep in every far frame (16/16, both boards and seats), near on
  the siege fixture at every layout (8/8) and near on map17 at 125 % (4/8).
- A field army's token lies on the middle of its own region's settlement in every far frame: Westemnet's (16/16) and, on
  the siege fixture, Edoras's (8/8). Near, no figure covers a settlement's middle (asserted).

**Defects found** (seen in the pictures, measured where they could be):

1. **The besieger's count is hidden at Helm's Deep** — resolved by UI-425 (`ui407-map17-*-far`, `ui407-siege-*-far`,
   `ui407-siege-*-near`, `ui407-map17-*-1.25-near`). Far out the garrison's walled puck lies on the besieger's puck over its count badge (at
   125 % the "13" is gone); zoomed in the garrison's figures — and on map17 Gimli, the last of the garrison's companions
   in their row west of the gate — stand on the besieger's plaque, so its emblem and count read only at 100 % on map17.
   UI-422's quarter rule holds for the pictures' rectangles but not for the plaque. Suggested fix: in
   `scripts/export_map_stands.py` keep the field force's plaque (and, for tokens, its count badge) clear of the garrison's
   group in the siege check, or hang the besieger's plaque on the side away from the gate.
2. **Far out a token covers its own settlement** — resolved by UI-425 (`ui407-map17-*-far`: Westemnet under the 14;
   `ui407-siege-*-far`: Westemnet and Edoras). The puck is centred on the field place, which UI-422 sets just above the gem for figures at scale
   1, so its lower half lies on the stone. Suggested fix: stand the token's lower edge (not its middle) on the place, as
   the figures' feet are, or keep the token level clear of the gem the way the places keep the figures clear.
3. **"Helm's Deep" is split by its own stone at 125 %** — resolved by UI-426 (`ui407-map17-*-1.25-near`,
   `ui407-siege-*-1.25-near`: "Helm' ◆ Deep"). UI-406's name-clear rule leaves the name where it is when it has no vertical room. Suggested fix: let the
   name move sideways, or break the line before the stone.
4. **At the map's edge the carried army is half cut off** — resolved by UI-444 ("As built (UI-444)") (`ui407-edge-pan-*`): the pointer rests 8 points inside the
   26-point zone and the figure is centred under it, so the map's edge clips half of it while the map pans. This is the
   default "the lifted figure is clipped to the map"; the owner may prefer it drawn whole above the HUD while it is
   carried.
5. Minor: **over the target the landing mark lies under the carried army** — resolved by UI-428 (`ui407-carried-*-near`):
   when the pointer is on the landing place only its rim showed; the lit region and the caption still told the move. A
   carried army is now held higher, so the whole ring shows round its shadow, asserted in all 16 carried frames.

None of these blocks a step: every frame's asserted checks pass. Each needs its own task (or the owner's acceptance as
a default) before UI-407 can close. Defects 1 and 2 are resolved by UI-425 and defect 3 by UI-426 (2026-10-30, "As built
(UI-425)", "As built (UI-426)"): the assertions that replace the measurements failed before the change in all 32 static
frames of the matrix (16 map17, 16 siege: the besieger's plaque under the garrison, Gimli or Legolas near, its count under
the garrison's puck far at 125 %, Strider's and Legolas's medallions and the Westemnet, Edoras and Dale tokens on their
stones far, Fords of Isen's plaque under the besieger near, "Helm's Deep" on its stone at 125 % near), in
`TabletopObjectsTests` (Denethor's marker on Minas Tirith's count, Dale's token on its stone, "Helm's Deep" on its stone)
and in the whole-map frames of `MapRegionNamesTests` ("Helm's Deep" in all four layouts), and pass after it. Defect 5 is
resolved by UI-428 ("As built (UI-428)"), defect 4 by UI-444 ("As built (UI-444)").

**Profile `map-profile-2`** with the drag-with-edge-pan workloads ([MAP17_ACCEPTANCE](MAP17_ACCEPTANCE.md), "Drag with
edge pan (UI-407)"): `drag-edge-fp` and `drag-edge-sh` carry each side's own largest army that can march, on the
projections the matrix recorded from the server, to an edge of the map and hold it there, so every measured frame carries
the army while the edge pan runs (`MapDragProfile`). Run on a Development Player of this change
(`BuildTasks.BuildMac`), `python3 scripts/review-map-client.py --input .artifacts/map17-profile.json --drag-input
.artifacts/ui407-map-drag.json --output .artifacts/map17-player-ui407`, 2026-10-30 07:59 UTC: 44 workloads × (120 warm-up
+ 360 measured frames), 50 pictures, all in a 1366 × 768 or 1920 × 1080 window. Summary:
[profiles/ui407-map17-macos.json](profiles/ui407-map17-macos.json).

- **The drag workloads meet MC17's thresholds at every layout:** lowest FPS at 1080p 455.3 (Free Peoples) and 495.9
  (Shadow), at 1366 × 768 706–958; frame p95 at most 4.86 ms, p99 at most 6.04 ms; the longest input-to-render 8.13 ms
  (limit 100); no static border repaint. The army was carried in all 360 measured frames of each of the 8 cases (one lift
  each), and the edge pan moved the map in 23–43 of them by 171–323 points.
- **The script's verdict is FAIL, on MAP-17's own workloads:** `projection` at 1366 × 768 × 100 % and 1920 × 1080 × 125 %
  had a longest input-to-render of 121.0 and 106.8 ms (limit 100); at 1080p `override-fate` reached 59.1 and 52.3 FPS
  (its longest input-to-render 192.8 ms, also over the limit) and `override-whitehand` 57.1 FPS (target 60). MC17's quiet
  run had 298, 260 and 238 FPS for these. The machine was heavily loaded during this run (load average 48–53 on 10 cores: other sessions,
  Spotlight indexing, a running release build), and the idle map ran at about a quarter of MC17's rate too (397.6 against
  1438.5 FPS at 1080p), so this run cannot tell a regression from the load. MC17 needs a rerun on an idle machine; if those three
  workloads still miss it there, applying a whole projection has become slower since MAP-17 and needs its own task.
- Two harness fixes were needed to run the profile at all, since no Player profile had run since MENU-12: a Development
  profile run no longer lets the saved window mode (Fullscreen, a maximized window on macOS) override the window its
  command line asks for (`InterfaceController(ownsWindow: false)` when `ProfileResolution.Requested`); and the stress
  workloads centre Helm's Deep in the side of the map the pinned card leaves clear, since the card has grown and covered it
  at 125 % (`MapProfileScene.ExposeSiege`). The profile switches seats with a fresh `TurnInteraction`, as a match session
  does.

**EditMode:** `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/u407-edit.xml`, **1327/1327
PASS**, 33.8 s. Python: `python3 scripts/test_map_profile.py` 14/14 PASS. After UI-425–UI-428: EditMode **1335/1335 PASS**.

**Batched PlayMode** (2026-10-30, `expansions` 58fa1ca4, after UI-425–UI-428): `scripts/discover-tests.py` then
`scripts/test-client-batches.py --batching mixed --batch-size 8 --seed 1` — **819/856 passed**, complete scope. 24 of the 37
failures are on the known baseline list (Effects panel UI-424, ContextHover, MapContourRendering ×2, T22, T23, the network
flows, Kings03/04, GuideDraw, CharacterRoster, BoardCompletion and others). Of the 13 others, rerun one class per run:

- `HandCardLayoutTests`, `MainMenuLayoutTests` and `FateAcceptanceNetworkTests.AFateGameAt100PercentAndItsReplay` pass alone
  (a crowded-run "Layout update failed to stabilize" and an Escape timing flake).
- `DicePanelLayoutTests` and the two Gothmog tests of `ExpansionAbilityNetworkTests` expected the wording before UI-420; the
  tests now follow it (4bba4fb0; the base game's `DicePanelLayoutTests` likewise on `ability-chips`) and pass.
- Six fail identically on the session's baseline `eb42551b`, so none is caused by the tabletop figures: the 125 % games of
  `T36DesktopGameTests` and `FateAcceptanceNetworkTests` (UI-305's presentation camera frames the opponent's map cue, which
  `FullGameDesktopChecks.Observer` forbids), `HuntResponseNetworkTests.RandomClickAndDropNeverSendAVictim` (when Merry or
  Pippin is the random casualty the recovery is the console's ✓, UI-298, and the test looks only in the tray; about 1 run
  in 3), `BoardProfileDriverTests` (UI-347's second click returns a placed companion; the Homelands workload's step 4
  still expects its location), and `ReturnListTests`/`BoardMenuNetworkTests` (the server's match list omits the two
  finished return matches).

## Playtest checklist

| # | Scenario | Steps | Expectation |
| --- | --- | --- | --- |
| 1 | Standard setup (`setup`) | Zoom in on Gondor and Rohan | Each army stands on an oval base with a shadow inside its region; regulars shorter than elites; the garrison stands on the crenellated base at the fortress gate |
| 2 | Standard setup | Zoom in and out, pan across the map | No figure jumps or drifts out of its region; nearer figures in front; at far zoom the tokens read as pucks |
| 3 | Standard setup, Free Peoples | Find Rivendell / the Fellowship's companions outside it | Every character is its own small figure beside the army; Gandalf and Strider are two different figures |
| 4 | A move with an Army die (`fp-army-move`) | Click the army | The army lifts off its base; the base empties; hover a legal region: a green landing mark and the caption |
| 5 | Same | Press Esc | The figures drop back onto the base |
| 6 | Same | Click a destination | The figures rise, fly in an arc and land on the destination's place without a snap |
| 7 | Same | Press the army and drag it onto a legal region | The army is carried as itself (no square or text), the region lights, edge pan works after a short pause; the drop moves it exactly as the click would |
| 8 | Same | Drag onto an illegal region (Lamedon, beyond the mountains from Minas Tirith) or the HUD | Nothing is sent; the army glides back: onto its base when the press picked it up, raised over its base, still chosen, when a click had chosen it first |
| 9 | An attack (`fp-army-attack`) | Hover the enemy region | A red landing mark; the drop or click opens the battle and the attackers return to their base |
| 10 | Character move with a Character die (`fp-character-move-*`) | Click Strider's figure | Only Strider lifts; clicking a destination flies him there |
| 11 | Opponent's turn | Watch the opponent move | The opponent's figures arc and land the same way |
| 12 | Helm's Deep siege | Look at the region | The besieger and the garrison both stand on their places, readable |
| 13 | Character move with an Army die (an army with Strider, `fp-army-move`) | Choose the Army die, click Strider's figure, then a destination | Strider is selected without a panel opening; he moves as the chip would move him (a lone character needs a soldier first) |
| 14 | Any position with a crowded region (seven or more characters) | Zoom in on it | Five single figures and a stacked pair with "+n"; the army's click selects them all |
| 15 | A move with an Army die (`fp-army-move`, an army of three or more) | Click the army, then click one lit figure in its region panel | That figure drops back onto the base (its count on the base's plaque); the others stay raised with their own count |
| 16 | A Shadow card that flies several Nazgûl (UI-212) | Click a lit Nazgûl stack, then a lit destination | One Nazgûl lifts; at the destination click it dissolves into the faint planned figure there and the next one lifts; the stack's base shows only the Nazgûl not yet flown |
| 17 | Settings → Skip animations on, then a move with an Army die | Click the army, then press Esc | The army shows raised at once and is back on its base at once, without motion |
| 18 | A move with an Army die (`fp-army-move`, an army with Strider) | Click the army, click a destination and watch the raised figures | They go on from where they hover — nothing drops onto its base and rises again — and each arcs (the army, then Strider on his own) and lands on its place; their shadows slide along the ground under them; the base the army lands on shows the same figures, nothing jumps |
| 19 | Same, when the server refuses the step (the state changed meanwhile) | Click the army and a destination | The raised figures come back down onto their base and stay there until you choose again |
| 20 | Same, with a slow connection (more than 5 s for the answer) | Click the army and a destination | After about 5 s the figures come back down onto their base; when the view arrives they fly from there |
| 21 | A Shadow card that flies a Nazgûl (UI-212) | Plan the flight and confirm the card | The faint Nazgûl at the destination becomes the real one and steps onto its place; nothing flies from where it stood |
| 22 | Opponent's turn: a march that leaves a rear guard | Watch the opponent's march | Only the moving figures rise from the force's place and fly; the rear guard stays on its base with its count |
| 23 | A move while the opponent's next move arrives quickly (or two quick own steps) | Watch a figure that is still in the air when it moves again | It turns in the air towards its new place; it never jumps to its first destination or back to its source |
| 24 | Settings → Skip animations on | Move an army | The army stands on its destination's place at once with a short fade, no lift or arc |
| 25 | A move with an Army die (`fp-army-move`), nothing chosen yet | Choose the Army die, press the army and drag it a little, then press Esc while still holding | It lifts as you drag (no panel opens) and follows the pointer; Esc puts it back onto its base; releasing afterwards does nothing |
| 26 | Same, an army with Strider | Click the army, then press it and carry it | The army and Strider go together under the pointer, each as its own miniature; the caption hangs above them over a legal region |
| 27 | Same | Carry the army to the map's edge and hold it there | After a short pause (about a quarter second) the map scrolls; the army stays under the pointer and the region under it keeps lighting |
| 28 | Same, a slow connection | Carry the army onto a legal region and drop it | It stays raised where you dropped it until the answer comes, then flies on from there; if the server refuses, it glides back onto its base |
| 29 | Character move with a Character die (`fp-character-move-*`) | Press Strider's figure and carry him onto a lit region | Strider alone is carried; the drop moves him as the destination click would |
| 30 | A Shadow card that flies several Nazgûl (UI-212) | Press the lit Nazgûl stack and carry one onto a lit destination | The carried Nazgûl dissolves where you dropped it as its faint planned figure appears; the next one rises at the stack |
| 31 | Opponent's turn, or while your step is on its way | Press one of your armies (or one of the opponent's) and drag | Nothing lifts; the press stays a click |
| 32 | An attack (`fp-army-attack`) | Carry the army onto the enemy region and drop it | The battle opens as with the click; the attackers glide back to their base |
| 33 | Standard setup (`setup`) | Zoom out to the whole map | Every army is a puck lying on the table — a top face, a side band and a rim, its shadow down and right; a garrison's puck is walled with battlements; Free Peoples blue, Shadow red; the emblem and count read |
| 34 | A move with an Army die (`fp-army-move`), the whole map | Choose the Army die; after a march look at the army that marched | The armies that may move have a green rim round their puck and the pulsing gold halo; a marched one is veiled with ✓ (a clicked army shows its lifted figures, UI-403) |
| 35 | Standard setup | Zoom from the whole map to the closest and back, pan across the map | Each settlement is a gem stone with a darker side band and a soft shadow; it grows a little as you zoom in and stays put on its spot while panning, without flicker |
| 36 | A capture (a Shadow army takes a Free Peoples Town or City) | Watch its settlement | The stone turns from sapphire to ruby, keeping its cut, size, band and shadow |
| 37 | A Kings game with a Corrupted king | Zoom in on his capital; then choose an action that acts on him | A small dark iron puck with an ember rim and the crown under the Eye lies beside the forces with its shadow; while a click chooses him a gold halo lies behind it |
| 38 | The Fellowship on a region, then on the Mordor track | Zoom in on it | The figure stands on a round blue base with a shadow; on the track it stands on its step on the same base, as large as before |
| 39 | A lone character outside the armies (`fp-character-move-*`) | Zoom out to the whole map | Its portrait medallion reads as a thick coin with a shadow |
| 40 | The dense board (`map17-dense`): Helm's Deep besieged, Strider, Gimli and Legolas in the garrison | Look at Helm's Deep on the whole map, then zoomed in | Both counts read: the besieger's 13 and the garrison's 7, far out on the pucks and zoomed in on the plaques; no companion stands on a plaque (UI-425) |
| 41 | Standard setup | Zoom out to the whole map, then a little further onto the margins | Every army's puck stands just above its settlement's stone, which stays in view; zooming out past the whole map, the pucks and stones shrink with the map (UI-425) |
| 42 | A Kings game with Corrupted Denethor (or any Corrupted king) | Look at his capital from the whole map to the closest zoom | His marker stands beside the forces and never covers an army's count or plaque, or a stone (UI-425) |
| 43 | Any board | Zoom from the whole map to the closest over Helm's Deep, Minas Tirith and Fords of Isen | A name is never split or covered by its own stone: it steps aside, or where it cannot (Helm's Deep, zoomed in at 125 %), it hides until there is room (UI-426) |
| 44 | The Fellowship hidden on a region, then revealed (a Hunt that reveals it, or `fp-character-move-*`) | Zoom in on its figure; then zoom out to the whole map while a Character die is chosen | A small blue plaque with a gilt rim on the front of its base shows an outlined ◇ while hidden and a filled ◆ once revealed; on the whole map, where the figure glows to be clicked, the plaque is still large enough to read (UI-427) |
| 45 | The Fellowship on the Mordor track (Enter Mordor from Morannon or Minas Morgul) | Look at its figure on its step, zoomed in and on the whole map; reveal it, then hide it | The same plaque on its base shows ◇ or ◆ and changes with the state; clicking the glowing revealed figure still hides it (UI-427) |
| 46 | A move with an Army die (`fp-army-move`) | Press the army and carry it over a legal region until its shadow lies in the green ring | The army hangs clearly above the ring — the whole ring shows round its shadow — then drops onto its place when released, flying on from that height (UI-428) |
| 47 | A move with an Army die whose target holds the Fellowship (move the Fellowship there first, or `fp-army-move` with the Fellowship next to Minas Tirith) | Carry the army onto the Fellowship's figure in the lit region and release | The region lights under the Fellowship's figure and the drop moves the army there, exactly as clicking the region would; it does not glide home (UI-428) |

## Superseded statements

- UX_SPEC (UI-105 section): "army movement remains only clicks; drag remains only for reserves" — superseded for units
  and characters by UI-421 (2026-10-29); built by UI-405 (2026-10-30).
- ARMY_MAP_UX: "armies are never dragged" — idem.
- BOARD_INTERACTION_UX (UI-105, UI-128 sections) — idem.
- Dice, cards, tokens and reserves keep their gestures; so do the region panel's figure chips, the Characters rows and
  the Fellowship portraits. *Reserves: superseded by UI-454 (owner, 2026-11-03) — a Reserve figure is carried onto the map
  as a miniature, and a clicked recruit flies from its slot ("As built (UI-454)").*
