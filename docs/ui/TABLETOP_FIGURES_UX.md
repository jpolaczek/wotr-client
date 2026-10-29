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
  Gandalf and Strider alone show two Gandalfs.
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

- A character click opens no region panel.
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

## Superseded statements

- UX_SPEC (UI-105 section): "army movement remains only clicks; drag remains only for reserves" — superseded for units
  and characters by UI-421 (2026-10-29).
- ARMY_MAP_UX: "armies are never dragged" — idem.
- BOARD_INTERACTION_UX (UI-105, UI-128 sections) — idem.
- Dice, cards, tokens and reserves keep their gestures.
