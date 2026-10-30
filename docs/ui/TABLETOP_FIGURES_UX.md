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
   the figure in place too.
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
  snaps into place: no lift, arc or landing.

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
- **Landing mark.** `landing-mark`, a baked ring on the table at the prospective place: green for a move, red for an
  attack.
- **Lift pose.** Scale about 1.10 and a rise of about 12 px × scale; the shadow stays on the ground at scale .8 and
  opacity .35.

**As built (UI-400, 2026-10-29).** The atlases already paint each figure's stature on one cell height with the feet on
the bottom edge (a regular's cell is 78 % filled, a hobbit's 62 %), so a figure's box is its cell's shape at the elite's
height and only the Balrog (1.1) and the fallback silhouettes need `FigureStature`. Sizes and states:
[ARMY_MAP_UX](ARMY_MAP_UX.md), "Miniatures on the table — UI-400".

## Stand places, scale, depth and hit shape

- **Places** (UI-422): `Resources/MapCanvas/stand-slots.json`, generated by `scripts/export_map_stands.py` from the map
  geometry, `landmarks.json`, the settlements, the name sizes and hand pins (`scripts/map-stands-overrides.json`), with
  `sourceHashes` and a `--check` mode. Per region: `garrison` (the gate of the fortress drawing, or beside the
  settlement), `field` (open ground inside the contour, near the settlement but clear of it), `characters[0..5]`,
  `garrisonCharacters[0..2]`, `fellowship`, `planned`, and `capacity` (the largest footprint scale at which the group
  fits). Region overrides of expansion modules always get a place (fallback beside the settlement). Region names keep
  clear of the places (`MapRegionNamePoints.For` with an obstacle predicate). As built (2026-10-29):
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
    in a row to the west.
  - **Hand pins.** `pins` in `scripts/map-stands-overrides.json` (`{"<region>": {"field": [x, y],
    "characters[2]": [x, y]}}`) replace the search for those places; none is needed today. Its `settlements` lists
    the settlements expansion modules give regions (Fate of Erebor: Dale a Fortification, the Iron Hills a City; New
    Cities: Ered Luin and South Rhûn Cities), so their larger gems are kept clear too.
  - **Region overrides.** A region the file does not hold, or a stronghold an expansion module makes of a region
    without a garrison place, stands beside its settlement: the field to its right and the garrison to its left, the
    characters beyond them, the Fellowship above and the planned recruits below, 14 units clear of the gem, with
    capacity 0 so it shows tokens. No module makes such a stronghold today.
  - **Checks.** `python3 scripts/export_map_stands.py --check` lays everything out again, runs the acceptance checks
    and compares the result with the file (about 80 s; NumPy, SciPy and Pillow as in
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
  `planned` (`MapCanvas.GhostPlace`); a corrupted king's marker on the last character place (`MapCanvas.BesideForces`);
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
  about as wide as its base: `clamp(40 × zoom, 32, 44)` points. A king's marker keeps its 30 × 34 points.
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
- **Not yet.** Flights (`MapFigureMotion`) still fly between region points at their own size (UI-404); the token keeps
  its UI-125 pictures (UI-406). The Mordor track's stones and the effects labels still move by `left`/`top`.
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
- **Not yet.** Flights still fly between region points (UI-404); lifting (UI-403) and carrying (UI-405) are next.
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

## Flight profile (UI-404)

Flights run between stand points: the previous place at the source and the current place at the destination, one
flight per destination marker, rendered with the same miniature.

- 0–15 %: lift; 15–85 %: arc of height `clamp(.12 × distance, 20, 80)` px; 85–100 %: landing with a short settle.
- The shadow follows the ground. The duration bounds stay as today (0.65–1.05 s).
- Handoff: `Figures.Expect` (figure keys, point, pose, expiry about 5 s) lets a lifted or dropped figure start where it
  is, never from its source; an attack, a rejection, a disconnect or the expiry returns it to its base.
- Reduced motion and Skip behave as before.

## Defaults the owner can change at review

- A character click opens no region panel. As built (UI-402): a click on the player's own character that chooses
  nothing does what its region's click does, without the panel; an enemy character's click is its region's.
- A stacked pair's click chooses its front character (UI-402).
- Far out a character is its portrait in its side's medallion (UI-402, until UI-406's tokens).
- A Nazgûl inside a mixed stack lifts with its base.
- The lifted figure is clipped to the map.
- Edge pan works during a figure drag after a ~250 ms dwell.
- Where a place's capacity is too small, the region shows tokens.

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
| 8 | Same | Drag onto an illegal region or the HUD | Nothing is sent; the army returns to its base |
| 9 | An attack (`fp-army-attack`) | Hover the enemy region | A red landing mark; the drop or click opens the battle and the attackers return to their base |
| 10 | Character move with a Character die (`fp-character-move-*`) | Click Strider's figure | Only Strider lifts; clicking a destination flies him there |
| 11 | Opponent's turn | Watch the opponent move | The opponent's figures arc and land the same way |
| 12 | Helm's Deep siege | Look at the region | The besieger and the garrison both stand on their places, readable |
| 13 | Character move with an Army die (an army with Strider, `fp-army-move`) | Choose the Army die, click Strider's figure, then a destination | Strider is selected without a panel opening; he moves as the chip would move him (a lone character needs a soldier first) |
| 14 | Any position with a crowded region (seven or more characters) | Zoom in on it | Five single figures and a stacked pair with "+n"; the army's click selects them all |

## Superseded statements

- UX_SPEC (UI-105 section): "army movement remains only clicks; drag remains only for reserves" — superseded for units
  and characters by UI-421 (2026-10-29).
- ARMY_MAP_UX: "armies are never dragged" — idem.
- BOARD_INTERACTION_UX (UI-105, UI-128 sections) — idem.
- Dice, cards, tokens and reserves keep their gestures.
