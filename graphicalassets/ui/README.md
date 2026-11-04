<a id="grafika-interfejsu--ui-86-ui-87-ui-90-ui-91-ui-97-ui-98-ui-99-ui-100-ui-110-ui-114-ui-115-ui-116-ui-117-ui-118-ui-119-ui-124-ui-125-ui-159-ui-167-ui-169-ui-193-ui-200-ui-202-ui-203"></a>
# Interface graphics — UI-86 , UI-87 , UI-90 , UI-91 , UI-97 , UI-98 , UI-99 , UI-100 , UI-110 , UI-114 , UI-115 , UI-116 , UI-117 , UI-118 , UI-119 , UI-124 , UI-125 , UI-159 , UI-167 , UI-169 , UI-193 , UI-200 , UI-202 , UI-203

A set of interchangeable elements of the board interface in the style of lacquered wood, gold and enamel, matching the parchment map. Recipients: top bar (UI-86) and left strip with Politics/Effects/Characters panels (UI-87) and Fellowship/Hunt panels (UI-90), from UI-98 developed from the tab at the top of the map. Each element is a separate, proprietary file SVG; the bar only folds them in Unity (USS). There is no single image of the entire strip or origin from the original components of the game.

<a id="struktura"></a>
## Structure

| Path | Content |
| --- | --- |
| `palette.css` | Common color tokens (golden, enamel, record, accents icons). From UI-200 the plate, buttons and neutral enamels are in a warm shade of carbon instead of grenade; page enamel (`free`, `shadow`) unchanged. |
| `src/shared/defs.svg` | Common gradients and filters; colors take from tokens. |
| `src/surfaces/` | Surfaces: striped board tile, shadow thrown on the map. |
| `src/frames/` | Framework 9 -slice (from UI-200 CD `panel` has 256 pt; from UI-203 deep coal with soft discolorations instead of grain and shadow at the frame, and the header `panel-header` has closed, cut ends without rhombs; corners and cut 22 Unchanged: strap frame (44 ed UI-98; from UI-202 the lines themselves and a straight pole at the ends, without wraps), cartouche, button/zak handle, panel, header, small button, list entry, notification flag (UI-100, replaces the banner UI-99; enamel variants `free` / `shadow` with `hover`, without the base version). |
| `src/ornaments/` | Separators and fine ornaments, wax seal with number of notifications (UI-99), gold glow track icons when its click moves Fellowship (`move-glow`, UI-110). |
| `src/dice/` | dice mounts: regular medallion dice (variants of the `free`/`shadow` page with `hover` and `selected`), the round special dice of `special.svg` (the Lords dice, UI-267; the Kings Ruler die's `ruler-free`/`ruler-shadow`, ART-07), the framed tile. |
| `src/dice/faces/` | Wall symbols (UI-97): Character, Army, Muster, Army/Muster, Event, Will of the West, Eye, and Card Draw and Use Balrog dice special; die ivory with ink contour on enamel. |
| `src/tokens/` | Track tokens, e.g. the party's enamel policy; the Council medallions (UI-270); the Kings Ruler token (UI-276: a crowned gilt coin, `free` blue for a Ruler token, `shadow` red for a Dismay token, `-lit` while usable; the code lays the king's nation emblem over it). |
| `src/cards/` | card Hand: parchment lico (variants `playable` with a green glow of legal purpose from UI-124 , `armed` with a golden glow card selected, for example as cost), leather waist-back; arms seal removed in UI-194 ( card One click plays). |
| `src/icons/` | Counter icons (cards, ring, spent, hunt, out), panels (politics, effects, characters, columnship; Hunt uses `hunt`) and Log at the notification stack (`log`, UI-100). |
| `src/icons/nations/` | The shebs of eight nations: original, simplified symbols. |
| `src/battle/` | Window battle  (UI-115): four-mode stage lanes (`scene-field/siege/sortie/relief`, repeat-x), strength frame in hand enamel (`force-frame`,  `free` / `shadow`), the character medallion (`portrait-medallion`, side variants and `cancelled` with red ring), slot card, garrison shelf, statistics icons (`strength`,  `leadership`,  `dice`,  `hit`,  `garrison`), a ribbon of stages (`stage-ribbon`,  `stage-marker`, icons `stage-*`), decision icons `no-card` and `skip-cards`  (UI-116),  dice Fights `die-1` … `die-6` with variants `hit`,  `rerolled`,  `die-selected`,  `die-pressed` and `reroll-arrow`  (UI-117), loss caps `loss-downgrade` / `loss-remove` and the hit seal `hit-seal` with variant `spent`  (UI-118), decision icons `continue`,  `stop`,  `hold`,  `retreat`,  `advance`,  `extend-siege` and parchment round tab `round-tab`  (UI-119 Portrait or figure in the medallion draws the code. |
| `src/armies/` | UI-400/UI-403/UI-406 (tabletop figures): the miniature's `contact-shadow`, the state rims `rim-field`, `rim-character` and the far-zoom token's `rim-token` (`rim-eligible`, `rim-selected`, `rim-pressed`, `rim-ability`, `rim-moved`) and the `landing-mark` (`landing-move`, `landing-attack`); the painted stands and pucks beside them are Codex art from `graphicalassets/stands`. UI-406 removed UI-125's plinths `base-field`/`base-garrison` and tokens `token-field`/`token-garrison` (a picture per state). Reserves (UI-125): the sign `moved-check`, the ghost's outline `ghost-outline`, the reserve slot `reserve-slot` (`selected`, `pressed`) and the budget point `budget-slot` (`filled`); emblems and numbers are drawn by the code. |
| `src/rail/` | The left-hand tabs: `edge-tab`, 94×66 pt without 9-slice, icon centre (47, 22). Since UI-475 it is drawn after `frontend/medallion`: a dark enamel disc in one thin ring (`--fe-line`) and a hairline ring, on the icon (drawing the code); the signature draws code under the medallion. USS puts each tab on a nearly opaque dark plate under one thin line (`--game-rail-*`, no art) and gives usable tabs a thick gold rim that pulses under `.motion-enabled` (UI-223). Variants: `hover` (lighter ring and disc), `selected` (a panel held open: a warm disc, `--tab-enamel-*`, in a bright ring), `pressed` (darker); from UI-193 `available` and `available-hover` — a gold halo round the disc. |
| `src/mordor/` | Mordor track on the map (UI-153): `slab` stone plate (`passed` variant extinguished and `next` with green glow of legal purpose), `crack-of-doom` doom rift (warm crater, `next`) and `gate` gate sign (`next`), drawn with warm pencil ink map. |
| `src/console/` | Console turn  (UI-159): plate 9 -slice  `frame`, status badge `status-plaque` in the enamel page (`free` / `shadow`, without a base), field recess Hunt   `hunt-box` (variant `eligible` with green glow on assignment), smoke of choice of walls `picker` with a tail `picker-tail` (since UI-476 re-drawn thin for the board's card; `frame`, `status-plaque`, `hunt-box`, `picker` and `ribbon` stay in the kit, unreferenced: the console wears `frontend/card-board`, `button-free`/`button-shadow`, `row`, `row-selected`, `row-free`/`row-shadow`) The Ring is `icons/ring`, taken over by Shadow   `icons/ring-captured`; from UI-175 without socket `ring-socket` (removed). UI-167: ribbon `ribbon` in the enamel page (`free` / `shadow`, without a base; 9 -slice  28 pt on the sides, 12 pt top and bottom; text draws code), flying over the map when action passes to the player, and gold glow of the badge `status-glow`  (9 -slice  16 pt) for the condition `your-turn` And a flash at the Reduced Motion. |
| `src/hunt/` | Hunt : tiles `tile-*` values `eye` , `die` , `smeagol` and signs `reveal` , `stop` , `kings` ( UI-141 ); window Hunt ( UI-159 ): Step icons `stage-tile` , `stage-damage` , `stage-result` , `stage-step` (drop and metastatic `battle/stage-*` ), Seat belts of scenes `scene-hunt` and `scene-mordor` (repeat-x) and injury choices icons `choice-corruption` and `choice-random` . Portrait Guide draws code in the medallion with `battle/` . |
| `src/end/` | End of game screen (UI-169): the 300×56 title plaques; since UI-233 the scenes are painted (`graphicalassets/end`) — `title-victory` (gold, green enamel, bay leaves) and `title-defeat` (dyed iron and red, crack). |
| `src/rules/` | UI-263: the waiting room's rules strip: `medallion` (56 pt, gilt rim with beads round domed enamel; variants `hover`, `pressed` and the lit `fate-on`, `lords-on`, `kings-on` in each module's enamel with a gold glow), the expansions' crests `crest-fate` (the Lonely Mountain under the Arkenstone), `crest-lords` (three linked Elven rings with red, white and blue stones) and `crest-kings` (a crown), 32 pt, ivory and gilt with an ink outline, drawn on the medallion and at 18 px on rows and plaques; `check-off`/`check-on` rings for the variant chips. Original simplified symbols. |
| `src/frontend/` | UI-470 (owner, 2026-11-03): the menus' thin-line chrome, worn by every window outside the game's own interface (sign-in, the main menu and its pages, the waiting room, Settings, the questions, the board's Menu panel) through `Styles/Menus.uss` and `Styles/Frontend.uss`; since UI-471 `button` and its `hover`, `pressed` and `selected` also frame every button of the game, through the `--game-button*` tokens of `Styles/GameChrome.uss` (`frames/button` and `frames/button-square` stay in the kit, unreferenced). `card` (64 pt, 9-slice 12 pt): translucent charcoal under one muted-gilt rule with small concave corners, a tiny gilt lozenge in each cut and a hairline inner rule. `button` (64×32 pt, 9-slice 6 pt): a dark field edged with one muted-gold line; `hover`, `pressed`, `selected` (an open page, a chosen segment or variant chip: a warm field with a bright line) and `primary`, `primary-hover`, `primary-pressed` (the one main action: Login, Create game, Start game, in a warm-gold gradient). `row` (96×40 pt, 9-slice 8 pt): a list row, side card or seat with a dimmer line; `hover`, `selected` (the chosen side, your seat). `divider` (200×8 pt, drawn unscaled): a hairline fading at both ends round a tiny open lozenge. `medallion` (56 pt): an expansion's checkbox, a dark enamel disc in one thin ring; `hover`, `pressed`, `fate-on`, `lords-on`, `kings-on` (the module's enamel, a bright ring and a soft halo). `check` (14 pt) and `check-selected`: a small box and its gold tick. `icon-account`, `icon-password`, `icon-search` (16 pt): line icons inside the sign-in and search fields. Since UI-472 the board's panels and windows wear two more, through the `--game-card` and `--game-title-rule` tokens of `Styles/GameChrome.uss`: `card-board` (64 pt, 9-slice 12 pt), the menus' `card` with a nearly opaque field and a dark hairline outside the muted-gilt rule, because the thin line and a translucent plate do not read over the bright parchment map; and `title-rule` (200×1 pt), the hairline under a title, fading at both ends and stretched across the header's bottom point (`background-size: 100% 1px`), so it takes no layout. `frames/panel` stays in the kit, unreferenced. Since UI-475 the game's rows wear `row` (with `row-hover`, `row-selected` and the new `row-pressed`) through `--game-row*`; since UI-476 `row-free`, `row-shadow` (with `-hover`) and `button-free`, `button-shadow` (with `-hover`, `-pressed`) are the same sources in a side's enamel, by `--fe-*` overrides in the `free`, `shadow` variants: the dice band, Shadow entries, Politics rows, force frames and the turn band on rows, the console's status plaque on the button. Its tokens are the `--fe-*` group of `palette.css`; no arrows on any button. |
| `assets.json` | Catalog: source, role, variants, 9-slice and repetition. |
| `render.py` | Renderer directory to PNG with real alpha (headless Chrome). |
| `manifest.json` | Result of the render: sizes, alpha and totals SHA-256 (generated). |
| `previews/` | Preview of folding with separate PNG (strip, tab in corner UI-98, Log icon and stack of UI-100 flags, elements battle UI-115); the real layout is in USS. |

Mirror output to `src/`: `unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/UI/<kategoria>/<name>.png`. Variants get suffixes, e.g. `dice/medallion-hover.png`. The strip composition describes [TopBar.uss](../../unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Styles/TopBar.uss).

<a id="zasady"></a>
## Rules

- Size in attributes `width`/`height` SVG is the size of the display in points; `viewBox` may have
  another drawing grid. PNG has exactly 2 × this size, so with the scale 100% sampling is 2:1 and
  with 125% approx. 1,6:1.
- 9-slices in USS are given in textual pixels (2×points) with `-unity-slice-scale: 0.5px`. The
  stretched centre is only passed by simple lines or vertical gradients; ornaments lie in corners.
- The colors come from `palette.css` (`style="fill: var(--gilt-light)"`), gradients and filters
  common to `src/shared/defs.svg`. Local identifier have a file prefix (`ring-stone`, `frame-cap`)
  to avoid interfering with the commons.
- The variant is a set of overwriting tokens in `assets.json`; one source gives several states:
  `hover`,  `selected` (red paint of the bookmark), `free` / `shadow` (page enamel), `shadow-hover`
  and from UI-114   `pressed` (dark lacquer and gilded pressed control: `frames/button`,
  `frames/button-square`,  `dice/medallion` also as `free-pressed` and `shadow-pressed`).  `"base":
  false` omits the version without a variant when the resource exists only in variations (policy
  token).
- `actions/` (UI-138): icons action 20 point (close, back, continue, confirm, pin and pinned,
  refresh, help why, show on the map, last battle, more and less, mark and unmark all, Meals, pass,
  reject die; from UI-305 skip the opponent's presentation) in gold with the ink border; `IconButton` puts them on the shared button frame (`frontend/button` since UI-471, before it `frames/button-square`), and
  tooltip says what it does. A horizontal line draw filled with rectangle: the line without height
  gets neither a `gilt` gradient nor a `lift` filter (PNG blanks).
- `hunt/` (UI-141): Hunt tiles — 28 tiles by pool (standard in beige cardboard, special FP in blue
  enamel, special Shadow tiles in dark red, Sméagol tiles in dusty green), 16 values by pool (Eye,
  die, Sméagol) and 10 characters (Reveal, Stop, Kings). Numbers draws the client on the plate. Icon
  `actions/hunt-pool` opens the pool list.
- `characters/` (UI-139): card symbols16 characters: level (star), movement (but), flight (wing),
  level of combat (crossed swords), ability (stairs with seal), emblem of all Free Peoples and
  states: on board (pine), outside the game (the hourglass), removed (the broken sword), possible
  return (the rising sun). Leadership uses `battle/leadership`, Fellowship `icons/fellowship`.
- `abilities/` (UI-439): one icon per printed character ability, named by its ability id (28 pt, 56 px),
  painted with Codex and keyed in [graphicalassets/abilities](../abilities/README.md), not drawn from an SVG
  here; the same import settings, with path-derived GUIDs.
- `frames/glow` (UI-136) is a green glow 9-slice (16 pt) drawn 7 px outside the control that can be
  used by the selected die action; the medium remains transparent and the control does not change
  its own graphics.
- The elements do not contain text, numbers or rules. Numbers, signatures, states of legality and
  hitboxes belong to the code; the change of graphics does not change them (R08).
- Unity import: without mipmap and copies CPU, RGBA32 without compression, bilinear, clamp;
  horizontal wrap only for surface with `"repeat": "x"`.

<a id="modyfikacja-i-rozszerzanie"></a>
## Modification and expansion

1. Edit or add SVG in the appropriate `src/` category; for the new file, add an entry in
   `assets.json` (roll, possible `variants`, `slices`, `repeat`).
2. Start `python3 graphicalassets/ui/render.py --preview`. Renderer saves PNG, creates the missing
   `.meta` Unity (GUID from the path; existing ones are) and refreshes `manifest.json` and previews
   `previews/*.png`. From UI-125 `--only <kategoria>` (e.g. `--only armies`) it renders only this
   category and retains the remaining manifest entries; a full rendering (approx. 25 min) is needed
   after changing the pallet tokens or definitions used by other categories.
3. Use the resource in USS by `resource("Wotr/Art/UI/<kategoria>/<name>")` and add it to the
   contract in `Tests/Editor/UiArtKitTests.cs` (test also detects textures outside the contract).
4. Change the pallet or common definitions refreshes the whole set with one rendering.

Required Google Chrome in `/Applications`; renderer uses a temporary profile (separate for each drop, with a limit of 60 s and one re-test, because suspended headless Chrome blocked the entire render) and does not touch the user profile. Chrome version can change anti-aliasing minimally, so we save PNG and their totals, not just the sources. The same Chrome rendering gave the byte identical PNG and the manifest.

<a id="przegląd"></a>
## Review

The preview of the 1× and 2× folds and the Unity drops of four layouts of both sides was observed: clear icon shapes (card, Elve ring with stone, crossed swords, flaming Eye, cracked die with cross), medallions with dice symbol, dice special tiles with signature, frame without cut. The first version of the Eye had lower flames similar to the fangs — they were replaced with side tongues of fire.
