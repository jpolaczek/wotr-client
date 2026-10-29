<a id="ilustrowane-okno-bitwy-i-stan-naciśnięcia"></a>
# Illustrated battle window and press state

Plan agreed 2026-10-13. UI-113 (`wotr-server`) receives documentation preparation; UI-114-UI-121 implements response, window frame, card, dice, loss, end of battle, portraits and acceptance. Status and evidence remain only in the `wotr-server` registry. This document does not confirm implementation.

<a id="cel-i-granice"></a>
## Purpose and boundaries

The application: "Currently everything is very tabular, there is no place for nice graphics". The battle window is to get a frame consistent with the rest of the board (wood, gold, page enamel, parchment from UI-86-UI-111) and the place for illustrations: figure, portraits, dice and combat mode scene. Size remains roughly present (approx. 760 entitled Width), but shape and proportions can change.

The decisions of the user from this conversation:
- documentation, tasks and implementation in succession;
- hybrid graphics: luminaire, icons, dice and ribbons as our own SVG in `graphicalassets/ui`;
  background scenes and portraits as generated PNG with alpha;
- portraits of named characters: yes, generated replacements;
- the general rule of response (below) for the whole board, visible in battle at reroll, marking
  losses and selecting cards.

The REQUIREMENTS R06, R08, R15–R19, R21–R25, UX_SPEC U01/U03/U08, [BATTLE_ACTIVITY_UX](BATTLE_ACTIVITY_UX.md) and [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md) are valid. No changes in the rules, server, Contracts, commands or projections. All the gestures (die → army → target, double click, drop, Keep roll/Reroll selected, Apply looses, retreat on the map, armament card with UI-91) are left. The graphics do not contain text, digits or rules; numbers, legality and hitboxes belong to code (R08). The principle “as little as useless place” applies.

<a id="zasada-naciśnięcia--responsywność-planszy"></a>
## Pressing principle — response board

Each clickable element of board (button, die, card, loss token, medallion, bookmark, map region) has three readable states:

| Status | When | Appearance |
| --- | --- | --- |
| Rest | always | Afordance: a frame or a glow suggesting click; on a hover brighter gold border, a cursor pointing. |
| Pressed (`pressed`) | from PointerDown do PointerUp/PointerLeave/PointerCancel | The item ‘presses’: move 1–2 from down and darker tint or variant `pressed` from graphics; move ≤ 80 ms. |
| Implementation | after release of the button inside the element | Only then does action (`clicked`) enter. |

- action never performs on PointerDown. The cursor output from the item before release removes the
  status and does not perform action.
- Start drag (card, dice, figure) removes `pressed`; drag is not a click. Edge you and map master
  after PointerDown cancel pressing the region without making the goal.
- Two-click and armament (UI-91): each of the two clicks has its own press state; the weapon remains
  a separate signal after the first click.
- Limited traffic (no `motion-enabled` class) disables the transitions, retains the status itself.
- Keyboard: Enter/Space shows the status of `pressed` over the time of pressing the key and performs
  action after release, like a mouse.
- Map regions are drawn in `MapCanvas`; pressing them is changing the highlight in drawing, not the
  USS class.

Implementation ( UI-114 ): one behaviour `PressFeedback` ( `Runtime/UI/PressFeedback.cs` ) record the event of the indicator, add and remove the class `pressed` , does not perform action ; common rules in `Resources/Wotr/Styles/Interaction.uss` ; variants `pressed` In `graphicalassets/ui/assets.json` Where the tint is not enough. `BoardScreen.Button` , `HandCard` and tiles `combat-choice` , `ActionPoolView` (medalions dice ), `BoardEdgeLayout` (carts), `FloatingWindow.Header` (window buttons), `MapCanvas` (regions) and in battle `BattleDieButton` , `.loss-figure` , decision buttons.

<a id="docelowy-układ-okna"></a>
## Target window layout

```text
╔═[panel-header]═ ⚔ Lossarnach · Field battle · Round 1 ─────── [?][×] ╗
⁇ [pass from scene ~44 entitled: background of mode Field/Siege/Sortie/Relief, coat of arms FP ⁇ coat of arms SH, ⁇
⁇ "Your decision: Choose a Combat card" on a ribbon] ⁇
⁇ ⁇ ATTACKER · FREE PEOPLES ⁇ ⁇ DEFENDER · SHADOW · garrison ⁇ ⁇
║ │ (portraits) ◉ Gandalf  ◉ Boromir   │ │ ◉ Witch-king                    │  ║
⁇ ⁇ [Wheel figures from the atlas: 3 reg, ⁇ ⁇ [Wero figures] ⁇ ⁇
⁇ ⁇ 2 elite, leader, nation's coat of arms] ⁇ ⁇ ⁇ ⁇
⁇ ⁇ 3 ⁇ 1 ⁇ 3 ⁇ 5+ (medalions) ⁇ ⁇ ⁇ ⁇ ⁇ ⁇ ⁇ ⁇
⁇ ⁇ [Slot card: parchment / reverse] ⁇ [Slot card] ⁇ ⁇
⁇ ⁇ [dice: d6 with dice with ivory] ⁇ [dice] ⁇ ⁇
⁇ ⁇ ⁇ not fighting: [dated figure] (only if they exist) ⁇ ⁇
║ └───────────────────────────────────┘ └─────────────────────────────────┘  ║
⁇ [strand of stages with icons: Cards › Roll › Re-roll › Losses › Next] ⁇
⁇ [decision zone: hand cards fight ⁇ figure loss ⁇ choice Intrusion/elite] ⁇
⁇ [foot: short status. [main button] ⁇
╚═══════════════════════════════════════════════════════════════════════════╝
```

- Header: region, mode (Field/Siege/Sortie/Relief), round and decision owner, in the
  `frames/panel-header` frame like other windows. Attacker on the left, defender on the right
  (BATTLE_ACTIVITY_UX "Fighting window").
- pass scenes: from UI-206 painted illustration battle at the place of combat ( settlement and the
  site page, two versions); without it stylized pass horizon for combat mode ( UI-115 , SVG ; UI-120
  Herby sides, the current decision on the dark plate in the middle.
- The strength frame in the page enamel (FP blue, SH red) instead of the text columns. Statistics as
  four medalions with icon and number: strength, Leadership, number dice, hit threshold; tooltip
  retains the current description of modifiers (`BattleForceText.Modifiers`).
- figure from the `units-v3` (`FigureArtwork`; from UI-207 36×48 on the subject), grouped with
  nations with the coat of arms with `icons/nations`. Garrison and rear guard on a separate, quelled
  shelf "not fighting"; no such figures = no shelf.
- Named characters as portrait medalions (UI-120); until the bitmap medallion SVG with a figure with
  an atlas and initial drawn by code. A abolished ability = red seal on the medallion with the
  existing tooltip.
- card battles: own as parchment tiles (`cards/parchment`, UI-91), card opponent covered as leather
  reverses (`cards/back`), revealed pair with initiative seal. "No card" as parchment button. card
  not available with condition (UI-111) are greyed out after available.
- dice: d6 with dice ivory with ink eye (SVG, six walls); hit = amber glow and gold border; metasta
  = two cubes with small arrow; die to the metasta = gold mark ring; special attacks as cards with
  sides of five dice as today.
- Losses: figure as illustrated chips with state overlay (keep → missing, downgrade → arrow down,
  remove → cross) and operation number; budget hits as wax seals “assigned/remaining”; legend as
  icons; one Apply looses. Preview server remains in tooltipach.
- End of battle: Continue/Stop/Hold/Retreat/Advance/Extend siege as buttons with `frames/button`
  frame and icon; cost of the elite as a preview of figure; retreat still indicates the region on
  the map.
- Report battle: parchment-only chronicle (`frames/panel` + `cards/parchment`), header with mode,
  rounds as bookmarks ‹ Round 2/3 ›, same power layout (read only), steps as entries with icons.
  `BattleArchiveSummary` in the tab action in the same style.
- Size: 760 Width (losses to 820), Content height, still at region (`MapPanelPlacement.Near`),
  Sliding (`FloatingWindow`), Position in Session. At 1366×768 and 125% (1093×614 pt) decisions
  without scroll.

<a id="grafika-do-utworzenia"></a>
## Graphics to create

Category `graphicalassets/ui/src/battle/`, entries in `assets.json`, contract in `Tests/Editor/UiArtKitTests.cs`, preview `previews/battle.png`.

| Group | Files | Options |
| --- | --- | --- |
| Scenes | `scene-field`, `scene-siege`, `scene-sortie`, `scene-relief` | repeat-x; interchangeable for UI-120 bitmaps |
| Framework | `force-frame`, `portrait-medallion`, `card-slot`, `garrison-shelf` | `free`/`shadow`, `cancelled` (portrait), `pressed` |
| Statistics and Decision Icons | `strength`, `leadership`, `dice`, `hit`, `garrison`, `continue`, `stop`, `hold`, `retreat`, `advance`, `extend-siege`, `no-card`, `skip-cards` | — |
| dice | `die-1` … `die-6`, `reroll-arrow` | `hit`, `selected`, `rerolled`, `pressed` |
| Casualties | `loss-keep`, `loss-downgrade`, `loss-remove`, `hit-seal` | `spent` |
| Stages ribbon | `stage-ribbon`, `stage-marker`, `stage-cards`, `stage-roll`, `stage-reroll`, `stage-losses`, `stage-next` | — |
| Report | `chronicle-header`, `round-tab` | `selected` |
| Press (UI-114) | variants `pressed` for `frames/button`, `frames/button-square`, `dice/medallion`, `cards/parchment` | — |

Portraits and backgrounds ( UI-120 ): `graphicalassets/portraits/` with an atlas PNG , JSON rects, prompts and manifesto SHA-256 pattern `figures/` and `fellowship/` ; keys after the name of the characters common to base game and Lords: gimli, boromir, legolas, gandalf-grey, gandalf-white, strider, aragon, Pippin, merry, gollum, smeagol, ring-bearers, elrond, galladriel, balrog, gothmog, saruman, mouth-of-sauron, witch-king. `PortraitArtwork` pattern `FigureArtwork` Generating bitmap requires a tool outside Claude session; to deliver them, placeholders operate SVG ed UI-115 , a UI-120 remains blocked with ready-to-use props.

<a id="kod"></a>
## Code

- `Runtime/UI/Battles/RegionalBattlePanel.cs` — header with mode/round, pass ed stage, slots.
- `Runtime/UI/Battles/BattleContext.cs` — strength framework, medallions statistics, figure,
  garrison shelf, character medallions, card slot (reused by report).
- `Runtime/UI/Battles/BattleStages.cs` — an icon ribbon.
- `Runtime/UI/Battles/BattleCombatHand.cs` — tiles pergaminowe.
- `Runtime/UI/Battles/BattleDieButton.cs`, `BattleDiceStrip.cs` — walls d6 as class, states;
  `DiePips` to be removed when unused.
- `Runtime/UI/Battles/BattleLossChoices.cs`, `BattleChoices.cs` — chips with covers, seals, buttons
  with icons.
- `Runtime/UI/Battles/BattleReport.cs`, `BattleReportDetails.cs`,
  `Runtime/UI/Activity/BattleArchiveSummary.cs` — chronicle.
- `Runtime/UI/DecisionTray.cs` — only class and order, without changing logic.
- New: `Runtime/UI/PressFeedback.cs`, `Resources/Wotr/Styles/Interaction.uss`,
  `Resources/Wotr/Styles/Battle.uss` (transfer of battle rules from `Wotr.uss` and remove triple
  width definitions windows/ko), `Runtime/UI/Battles/BattleStatMedallion.cs`,
  `Runtime/UI/Battles/PortraitMedallion.cs`, `Runtime/Map/Figures/PortraitArtwork.cs`.
- Untouched: `BattleContextInteraction`, `TurnInteraction`, `BattleDraft`, sender, Contracts,
  server.

<a id="scenariusze-i-właściciele-odbioru"></a>
## Screenplays and owners acceptance

The owner prepares the evidence in his task; UI-121 takes the joint course.

| ID | Owner | Observable scenario |
| --- | --- | --- |
| BW01 | UI-114 | PointerDown adds `pressed`, PointerUp inside removes and performs one `clicked`; PointerLeave/Cancel removes without action; drag does not perform action. |
| BW02 | UI-114 | Button, locket dice, card from hand, tab, window button and map region show the state of pressing; edge pan after PointerDown does not perform the target. |
| BW03 | UI-114 | Two-click and armament card (UI-91) and excess discard are working so far; limited traffic without passing. |
| BW04 | UI-115 | Window in board frame: header with mode/round/owner, pass stage of each of the four modes, strength frame in the enamel of the pages. |
| BW05 | UI-115 | figure from the atlas grouped nations with a crest; garrison/Rear guard on the sedated shelf; no = no shelf. |
| BW06 | UI-115 | Medals of statistics with server values and the existing modifier tooltip; medallions of characters with a stamp of abolished ability. |
| BW07 | UI-115 | Four layouts of both sides: width ≤ 820, decisions without scroll, map visible around the window, no empty areas. |
| BW08 | UI-116 | Parchment-plated hand-to-hand tackles with press and weapons; double-click/drop/Play plays; No card separate; card with condition greyed out behind available. |
| BW09 | UI-116 | card opponent covered as reverse to reveal; revealed pair with initiative; preview of obligated/archived card without Play. |
| BW10 | UI-117 | Six d6 walls with eyelets, hit with glow, metastasized as two dice with arrow, signed modifier; pages of special attacks. |
| BW11 | UI-117 | Selection of dice to the metastasize by pressing (the selection ring), zero to the server limit, Keep roll/Reroll with one click; another's private selection. |
| BW12 | UI-118 | Tokens of losses with keep/downgrade/remove overlays and operation number, assigned/remaining seals, icon legend, one Apply loses; preview server in tooltip. |
| BW13 | UI-118 | No replacement, death of commanders, excess hits and loss of events in the same tray with the same style. |
| BW14 | UI-119 | Continue/Stop/Hold/Retreat/Advance/Extend siege with icons and pressing; retreat on the map; elite cost as preview figure; two-click approves single selection. |
| BW15 | UI-119 | Report Chronicle: header, rounds tabs, read-only power layout, steps with icons; old battle by BattleId; `BattleArchiveSummary` in the action tab. |
| BW16 | UI-120 | Portraits and backgrounds from atlas replace squareholders without changing the layout, hitboxes and rules; no atlas retains placeholder. |
| BW17 | UI-121 | Drops 4 Systems × 2 Parties × stages ( card , throw, spread, loss, end, report, Siege/Sortie ) with pressured states; 60 FPS and ≤ 100 ms; class regression battle and cards ; builds. |

<a id="weryfikacja-i-przekazanie"></a>
## Verification and transmission

- EditMode: `UiArtKitTests` (new keys, sizes 2×, import), `BattleWindowTests`, `BattleDraftTests`,
  `BattleReportContractTests`, `CombatCardConditionTests`, new `PressFeedbackTests` and
  `BattleArtTests`.
- PlayMode with the actual server in the packages: `BattleLayoutTests` , `BattleWindowNetworkTests`
  , `BattleNetworkTests` , `BattleResolutionNetworkTests` , `BattleEndingNetworkTests` ,
  `BattleReportNetworkTests` , `BattlePolicyNetworkTests` , `UI63SiegeNetworkTests` ,
  `FloatingWindowTests` , `WindowFamilyNetworkTests` , `LiveActivityNetworkTests` ,
  `LastBattleIconClickTests` , regression cards ( UI-91/UI-111 ).
- `BattleScreenFixture`/profiles: 1366×768 and 1920×1080 at 100/125%, both sides, each stage;
  overview of empty space and cut.
- Builds macOS/Windows with checkout, smokes without user profile, `python3 scripts/check_docs.py`
  (client), `tasks.py validate` and `pin-repo` (server) after each client commit.

Results, commands and commits go to the to-do documents, UX_SPEC, BOARD_INTERACTION_UX, TESTING and the register. Push and publication outside the scope.

<a id="wdrożenie-ui-114-i-ui-115"></a>
## implementation UI-114 and UI-115

UI-114: `PressFeedback`, `Styles/Interaction.uss`, `pressed`, `MapCanvas.PressedRegion` (BW01-BW03, `ui114-pressed-*.png` discharges).

UI-115: `Styles/Battle.uss`, `graphicalassets/ui/src/battle/`, `BattleContext` with `PortraitMedallion`, figure with atlas and `BattleStatMedallion`, garrison shelf, pass with stage in `RegionalBattlePanel` and `BattleStages` ribbon (BW04–BW07). Evidence and boundaries in the `wotr-server` register (UI-115). UI-116 : parchment tiles of arm fight and second click by `DecisionTray.CardFinalized` , reverse and parchment card pages, frames and icons No card / Skip cards ( BW08 – BW09 ). UI-117: d6 walls in `graphicalassets/ui/src/battle/die-*.svg` with variants `hit`/`rerolled`/`die-selected`/`die-pressed`, `reroll-arrow`; Illustration of `DiePips` in `BattleDieButton` (BW10-BW11). UI-118: Atlas loss chips with `loss-downgrade`/`loss-remove` covers, nation's coat of arms and a badge of order, `hit-seal`/`spent` seals, icon legend (BW12–BW13). UI-119: `battle/continue|stop|hold|retreat|advance|extend-siege` decision icons on footer buttons, report chronicle (`battle-chronicle`, `round-tab`), iconized "View battle rounds" (BW14–BW15). Instead of a separate `chronicle-header` graphic, the chronicle uses `frames/panel-header`. UI-120: `PortraitArtwork`, `BattleSceneArtwork`; portraits and scenes strips drawn as SVG and rendered to PNG (`graphicalassets/portraits/`), propty preserved to be replaced on painted bitmaps (BW16).

<a id="podgląd-kart-walki-i-równa-ramka--ui-137"></a>
## Preview cards battle and equal frame — UI-137

The user's application from 2026-10-14: the content of cards the fight is to appear after invading as in the main panel of cards, and the window of battle was "a bit of a curve".

- `CombatCardPreview` is a child of the order of tiles (`battle-combat-hand`), set over the most
  easily pressed tiles without moving neighbors: initiative and title, full combat text (on the card
  not available also condition) and name card. It does not accept cursor; it hides when leaving and
  pressing. Tooltip tile only tells how to use it.
- Cause of curvature: the window contents lay 6–8 px from the edge, and the corner of `frames/panel`
  has an arc reaching approx. 18 px (center of arc approx. 11,5 px), so the force frames and footers
  came upon the arch and the corner romb. The window of battle, the chronicler of battle and the
  meal panel now have 12 px space on the sides and at the bottom.


<a id="zaznacz-wszystkie-przy-wejściu--ui-146"></a>
## Select all at the entrance — UI-146

User notification from 2026-10-14: “battle does not have a “mark all” button after the fight is over, when we decide whether we are going further or stay.”

- Decision `BattleAdvance` has the same switch as the region panel in the footer, before ‘Stay’ (
  UI-140 ): Select-all icon selects all figure ed `EligibleIds` options `AdvanceBattle` and when
  checked, the icon “select-all” clears the selection. Tooltip tells you what the next click will
  do.
- One click is one step back and does not send command; the input performs the battle region clicks
  so far. Single figures continue to switch.
- Code: `BattleChoices.AddSelectAll`, `#battle-select-all` style in `Battle.uss` (square frame of
  icons in footer).

<a id="karta-walki-jednym-kliknięciem--ui-147"></a>
## card One-Click Fight — UI-147

User applications from 2026-10-14: "When selecting card during the combo we introduce the same rules regarding clicking – one click card means using it" and "after playing card we do not display a separate window with content".

- The card Combat Tile (`BattleCombatHand`) no longer has the weapon state: one click plays card
  covered by the same path as the drop (`CommitCombatCard`), without Confirm. The seal, the red hint
  “Click again” and the `CardInspector` opening with the tile, tooltip says “Click to play face
  down”.
- Full card before the decision shows preview after raid (UI-137); “No card”, drop and card not
  available unchanged. Quick second click after approved step skips protection with UI-142.
- Play any game card (fight tile, hand, Play in preview, drop) does not open a window with content
  card ; preview of this card if it was open, closes with the play ( `CardContextInteraction.Played`
  .) A clear suspicion card (click card forces in window battle The chronicle) works so far.

<a id="okno-otwiera-się-z-nową-bitwą--ui-149"></a>
## Window opens with new battle — UI-149

User notification from 2026-10-14: “After starting the new battle window, battle should open automatically”.

- When the view brings battle about the new `Battle.Id` (also after loading the match during
  battle), `RegionalBattlePanel.Apply` opens and extends the window with both players. Losses from
  the event without battle opens the decision note so far.
- The map camera does not move. The window closed during battle is closed on subsequent revisions of
  this battle; it returns with the battle button, by clicking the region or by decision note, and
  the next battle opens it again.
- It replaces the earlier rule that the window opens only the player.

<a id="no-card-bez-kliknięcia-i-stały-przycisk--ui-151"></a>
## “No card” without clicking and fixed button — UI-151

The user's application from 2026-10-14: "If there is no card to play, the player should not have to click “no card”. This button should not disappear and appear, changing the size of the window, but should be grey if not available to click".

- Superseded by UI-279 (owner, 2026-10-19: every choice waits for a click). When the `CombatCard`
  decision belongs to the player and the `CommitCombatCard` option has no legal card (`EligibleIds`
  empty), No card glows alone (`ChoiceGlow`) and its tooltip reads "No legal Combat card · Click to
  fight this round without one"; the player clicks it. The tiles of unavailable cards keep their
  terms.
- In the cards stage battle (`BattleStages.Index` 0) the player footer that waits for the opponent
  has a greyed "No card" at the same place, so the button does not disappear between selection and
  expectation.
- The player's own click also hides the lack of a legal card better than an instant automatic
  answer did.

<a id="cele-33-px-w-oknie-bitwy--ui-132"></a>
## 33 px targets in battle window — UI-132

acceptance UI-132 has shown that from UI-149 (the window opens itself) the system tests cards also include the window footer battle. The panel scales with 1366×768, so with 1920×1080 and 125% the point has about 1,757 pixel, and the 32 target is rounded to 56 px, or 31,87, below the minimum goal 32.
- Footer buttons (`.battle-choice-footer .action`: ‘No card’, ‘Keep all’, the end election of
  battle, action alternative like Evil Knowledge), `#confirm-decision` in the window and fields of
  cards battle (`.battle-card`) have 33 px, like `.tray .action` and `.card-planning` from the Kings
  set.
- This also removes two failures of 31,87 px (`KingsCards02NetworkTests`,
  `ExpansionWindowNetworkTests`, `battle-card-attacker` field).

<a id="ilustracja-miejsca-bitwy--ui-206"></a>
## Illustration of the place battle — UI-206

The user's application with 2026-10-16: pass over battle is to be an illustration of battle, dependent on the region, with other images for villages and cities, two versions for the places Shadow and Free Peoples.

- `BattleSceneArt` (presentation, without UI) selects the `<rodzaj>-<strona>-<version>` key from the
  region:
  - type with settlement: `open` (without settlement), `fortification`, `town`, `city`,
    `stronghold`;
  - the original owner's page: `free` or `shadow`; the open field without the owner is `wild` and
    the fortified (Osgiliath, Fords of Isen) are `free`;
  - version: regions of the same group arranged according to identifier get the 1 and 2 alternate.
    Both players see the same picture in all rounds, and each of the 20 illustration has its own
    regions.
- `BattleSceneArtwork.Illustration` loads `Resources/Wotr/Art/battle-illustrations/<klucz>.jpg`
  (1536×320, without mipmap, Clamp). `RegionalBattlePanel` sets it as a background of the bar
  (`cover`, center) with the class `battle-scene-illustrated`. Without the file, pass mode is left.
- The battle window bar has 72 pt (from UI-208 26 pt on low screen), the 28 coat of arms (18 pt).
  The window of the event itself remains at the low pass without an image. The decision lies on a
  translucent dark plate between the arms.
- Graphics: `graphicalassets/battles/` (prompts, generator, log, cut pass and SHA-256 manifest).
  Test: `BattleSceneArtTests`.

<a id="większe-figury-w-oknie-bitwy--ui-207"></a>
## Bigger figure in battle window — UI-207

User notification from 2026-10-16: figures in the battle columns were very small and poorly visible.

- Strength composition: figure 36×48 pt with counter `×n` 12 The people’s name and the name of the
  nation 20 pt; on the low screen ( 1366×768 with 125% ) 26×34 pt, 10 pt and 16 entitled For each
  figure soft gold glow ( `IconGlow` , as in panels with UI-201 ) to make the dark figure Shadow
  They didn't die on the arm of force.
- Government of loss: figure 52×58 pt ( 42×46 low screen), emblem 14 pt, loss overlay 22 The rules
  `.action.loss-figure` has a higher specificity than 33 -point tray buttons ( `.loss-planning .tray
  .action` In `Wotr.uss` ) that have so far squeezed figure loss to 33 heights.
- On the low screen, the battle window fills the height above the console: the stage bar has the 26
  set there, and the figure are intermediate.
- The composition is wrapped in rows in constant column width; the window does not scroll
  (`BattleLayoutTests` also checks the minimum sizes of figures).

<a id="decyzje-bitwy-po-stronie-gracza--ui-208"></a>
## battle decisions on the player side — UI-208 decisions

The user's application from 2026-10-16: the reroll and hit chapter is to be on the player's side, not always on the left.

- The choice of figures in battle stands in the column of the page to which it relates, under its
  dice, and replaces the force composition there (`battle-force-choosing`): these are the same
  units, now to choose. This includes the loss block (legend, hit seals, budget and figure) in the
  column of the page with `BattleView.LossOwner` and the progress grid and extension grid siege in
  the player column. Since UI-394 (owner, 2026-10-28) this column keeps the other column's width:
  its figures wrap into a second row inside it rather than widening it (`.loss-figures`); on a short
  viewport its loss figures take the compact size (42 × 46) and the names under its portraits give
  way (the portraits and their tooltips keep them), so two rows stay above the console. The block
  header of the loss block will be wrapped in the column width. The losses of the event remain in
  the tray.
- dice to the reroll are in the player column (so far).
- The battle decision-button foot (transmittance, loss, "Keep all", card) stands on the player side:
  with the attacking buttons on the left, and description next to them (`battle-footer-attacker`,
  `row-reverse`); with the defender on the right.
- Columns do not switch places: the attacker stays on the left.
- Test: `BattleLayoutTests.LossesAndRerollsStandOnTheDecidingPlayersSide`; `BattleLayoutTests` also
  checks that the entire battle window (with a footer) ends above the turn console. This Asertion
  detected that the progress grid under the columns hid the buttons under the console on the low
  screen.

## The window keeps its place during a battle — UI-225 (2026-10-17)

Reported by the owner in 0.9.0-rc.2: after "No card" the window vanished for a moment and reappeared elsewhere. The
window re-ran its placement search on every size change, and the card step changes both its content (the Combat
tiles leave) and the room it keeps free for a card preview. Now a window placed for a battle keeps its place when its
content changes size; it is placed again only when it no longer fits there (outside the board or over a HUD panel),
when the map is resized or panned (it follows its region, as before) or when it is reopened. A window the player
moved keeps the player's place, as before.

## Battle pictures by nation — UI-226 (2026-10-17)

A battle at a town, city or stronghold shows its nation's picture (`<kind>-<nation>-<1|2>`, the region's current
nation, so Fate and the White Hand count); a missing picture falls back to the side's UI-206 picture. Open land and
fortifications keep their UI-206 pictures. Set and generation: [graphicalassets/battles](../../graphicalassets/battles/README.md#nation-set--ui-226-2026-10-17).


## Combat-card targets on the battle's own objects — UI-245 (2026-10-17)

A combat card's choice whose id is a battle die or a figure in the battle is made on that object in the Battle
window (`BattleObjectChoices`): Andúril's and Mighty Attack's misses on their dice in the dice strip, Mighty
Attack's paying Companion, Black Breath's, Fateful Strike's, Words of Power's and Blade of Westernesse's figures on
the force column's portraits or figure stacks. A target wears the shared `ChoiceGlow` and pulses; its tooltip starts
with the choice's title and detail (the cost, e.g. the forfeited Leadership). A single target sends with its click;
an adjustable set (Aragorn's two misses, Mighty Attack's Companion and miss) keeps its one confirmation. Identical
figures of one nation and kind are equivalent, so a stack's click takes its next eligible figure. An exclusive
outcome without an object — Keep misses, Keep Minions — keeps its button, and the list no longer repeats the dice
and figures. Test: `BattleObjectChoicesTests`.

## Leadership points and Heroic Death on their objects — UI-246 (2026-10-17)

Cruel as Death, They Are Terrible and Dread and Despair (`NazgulLeadership`) show each offered Leadership point as a
gilt pip (`BattleChoicePip`, ◆) on the figure that forfeits it — the choice's `SourceId`: the Witch-king's points on
his portrait, a Nazgûl's on its figure stack. Point ids, the printed cost and the group limits stay the server's: a
fixed cost closes the other pips once it is complete, a variable cost accepts any positive set, a click on a chosen
pip takes it back so the source can change before the one confirmation, and spent Leadership offers no pip. Heroic
Death (`SacrificeAndHits`) takes the sacrificed Leader or Companion on its portrait or stack (UI-245) and the
cancelled hits as pips (●) on the opponent's roll heading beside its hit count; an amount opens only after a figure
that can cancel it is chosen, the chosen amount fills from the left, and a smaller pip replaces a larger one. Keep
hits (zero hits, a legal costly choice for a Companion) and Keep all stay buttons; the confirmation sends the figure
and the amount together. Pips keep fixed sizes (14 px, 11 px in short viewports) and never move the figures. Test:
`BattleObjectChoicesTests`.

## Character reactions on their portraits — UI-247 (2026-10-17)

A character's reaction in a battle is used on its portrait in the force column (`BattleReactions`): Gandalf the
White's White Rider at the start of the battle and the Witch-king's Sorcerer draw when the first round closes. The
portrait glows and pulses, its tooltip starts with "Click to use White Rider · …", and one click sends the reaction
(no Respond step, no list). Decline stays the tray's button. A character with more than one reaction at once keeps
the list. Test: `ReactionSourcesTests`.

## Event losses are a region's losses — UI-251 (2026-10-17)

Losses from an event outside a battle (Ent attacks, Dreadful Spells) keep the regional panel at the affected region
with its loss controls, hit count and one confirmation, but without the battle scene strip and side emblems
(`event-loss-panel`): it reads "<Region> · Event losses", never as a battle. Losses inside a live battle stay in the
Battle window.

## Portraits and figure stacks are pressed, not captured — UI-245 follow-up (2026-10-17)

A portrait or figure stack that a choice makes clickable (UI-245–UI-247) uses `ObjectPress`: it is pressed on
mouse-down, acts on mouse-up inside it and is cancelled when the pointer leaves (UI-113), without taking the pointer
capture. Its action rebuilds the Battle window, and a capture held by an element leaving the panel kept the next
click (a die, a pip) from reaching its object. Tests: `CardPackNetworkTests` (Mighty Attack: Companion then miss),
`CardPack02NetworkTests` (Heroic Death: figure then hits).

## Leadership points on their figure; full-size hit buttons — UI-246 revision (2026-10-17)

The final acceptance found the 14 px pips below the 32 px click target every control in the Battle window keeps.
Leadership points are now taken on the figure or portrait that has them: its click takes its next point while one can
be added and gives its points back once none can, and a badge on it counts them ("◆ 1/2"); the source glows while a
point can be added and is marked once one is chosen. Heroic Death's amounts are 32 px buttons "−1", "−2", "−3" beside
the opponent's hits; the chosen one is selected. Tests: `BattleObjectChoicesTests`, `Shadow01NetworkTests`,
`Shadow01LayoutTests`, `Shadow02LayoutTests`, `CardPack02NetworkTests`.

<a id="ui-311"></a>
## The opponent's rounds in the window — UI-311 (2026-10-24)

While the opponent's action plays, the window shows the battle's recorded public state read-only, in place of the
decision controls ([OPPONENT_ACTIONS_UX §6.15](OPPONENT_ACTIONS_UX.md#wdrozone-311)).

- **Opening.** A new battle opens its window after the attack's trace, as before (UI-149). The window keeps its place
  (UI-225), and a window the player closed during this battle stays closed.
- **Cards.** A committed card is a back. Both cards turn face up together, ringed with their initiative, for 3 s. A
  click opens the card read-only and holds the presentation.
- **Dice.** A roll or a reroll rings the dice it changed, with one dice sound.
- **Losses.** Committed losses play one hit at a time, in their recorded order, before the round's next state (UI-337,
  below). A character lost with its army is ringed on its portrait.
- **Controls.** The marks are observed traces, never a choice glow: nothing in the presented state can be clicked
  except a revealed card. The release gives the player's own controls back, with their usual click rules.

<a id="srodek--ui-324"></a>
## The window opens in the middle — UI-324 (2026-10-25)

Owner's decision: this window opens centred in the board's middle, between the top bar and the console and between the side columns, in every phase; a position the player dragged it to wins ([IN_GAME_UX](IN_GAME_UX.md#okna-na-srodku--ui-324)).

<a id="wstega--ui-327"></a>
## The stage marker glides — UI-327 (2026-10-25)

The stage ribbon is one lasting row per window (`StageRibbon`, `BattleStages.Ribbon`): when the battle moves to another
stage its gilt marker glides there (eased, 0.35 s) and the labels crossfade, also when the window redraws its content;
at once under Skip animations ([HUNT_UX](HUNT_UX.md#fazy--ui-327)).

<a id="zamkniecie--ui-333"></a>
## A decided battle's window steps aside — UI-333 (2026-10-25)

Owner: "once the battle is resolved and the next animations start, close its window, unless the user moved it".

- In a presentation, the first cue that is not part of what the window shows (its opening, rounds, cards, casualties and a
  siege's figure changes) closes the battle window when the battle is over in the latest view and nothing more of it is
  queued but its state after the decision (Advance, Finish, Complete) — so a retreat, an advance or the next action plays on
  an open map (`BattleWindowRetirement`, registered before every other presenter).
- A window the player dragged to a place of their own stays through the rest of the presentation.
- Outside a presentation the window already closes with its battle (the view without it).

<a id="jedna-wstega--ui-335"></a>
## One ribbon per battle window — UI-335 (2026-10-25)

Owner, on the ribbon at Losses: "the animation of moving to Losses fires twice — after the previous phase ends and again
when the window refreshes to show the damage assignment; it should be the same tab all the time, to which the damage
assignment panel is added, and the animation should fire only once".

- The battle window has one stage ribbon (`RegionalBattlePanel.Ribbon`); the presented round and the decision tray show
  that same element, so the decision only adds its controls under the row the presentation left.
- Its marker glides only when the current stage changes; a new parent, a redraw or a resize at the same stage puts it in
  place at once (a glide under way is only re-aimed).
- UI-339: a new battle (another id) starts the row afresh — its marker stands at Cards at once instead of gliding back
  from where the last battle ended ([HUNT_UX](HUNT_UX.md#fazy--ui-327)).

<a id="trafienia--ui-337"></a>
## Casualties hit by hit — UI-337 (2026-10-25)

Owner, from a battle against the CPU: "I had two hits, but from the combat roll it went straight to the next round; it did
not show the opponent assigning the damage. It should show small animated hits that either turn a unit from Elite to
Regular or kill a Regular, each server-committed decision shown separately."

- Cause: the engine keeps a removed figure among a force's participants, so the client's battle fingerprint saw the state
  after the opponent's losses as the state before them and dropped it; the opponent's final state (the next round) then
  played first. The fingerprint now includes the force's figures, and a decision's final state always follows the
  revision's casualties of its battle (`PresentationCueExtraction.FinalBattleStatesLast`).
- Each committed loss is its own beat (`BattlePresenter.Losses`, `BattleHits`): a small red ring bursts over the figure's
  stack in its column while the stack flashes red and swells (0.35 s); a stack losing its last figure fades out meanwhile.
  Then the state steps (0.45 s): the count drops, and an Elite downgraded becomes a Regular whose stack lights. The
  window stays at its Losses stage throughout; the round's next state comes after the last hit.
- Only committed facts play (`Facts.LossDetails`, in the order of the decision); a decision of several losses plays them
  one after another. The player's own committed casualties play the same way (a shared battle resolution, UI-303).
- Reduced motion keeps the flash, the ring and the fade without movement; Skip animations shows the latest state at once.

## Orderly Withdrawal and Evil Knowledge on their objects — UI-347 (2026-10-27)

The owner's decisions EXP-15 D10 and D12 ([EXPANSIONS_UI_AUDIT](EXPANSIONS_UI_AUDIT.md)):

- **Orderly Withdrawal** (Wind from the West's and Three Rings' Combat side) cancels one hit on the opponent's hits: the
  same "−1" as Heroic Death (UI-246, `BattleObjectChoices.WithdrawalCancel`; 33 px since UI-347, so the panel scale's
  rounding keeps the 32 px target), glowing, whose tooltip says what
  the withdrawal does; its click completes the step and sends it. "Keep the hit" is the console's Pass
  (`OptionalDecline`, tooltip "Pass · Keep the hit and fight on"); no text button remains.
- **Evil Knowledge** is the Mouth of Sauron's portrait in the besieging column while the siege may go on
  (`EvilKnowledgeChoice`, `BattleContext.EvilKnowledgeSource`): the portrait glows, its tooltip says the siege goes on
  one round without downgrading an Elite, and its click sends `UseEvilKnowledge`. The button beside Extend / Stop is
  gone, also from the option row the tray shows when Extend is disabled (no Elite to downgrade, so the portrait is the
  only way on); it comes back only if no portrait carries the ability (`EvilKnowledgeChoice.OnPortrait`).

Tests: `BattleObjectChoicesTests.OrderlyWithdrawalCancelsItsHitOnThePipAndKeepsItThroughPass`,
`LordsBattleNetworkTests` (the pip, the Pass, the Mouth's portrait), `ExpansionWindowNetworkTests` (the portrait with
an Elite); seeder scenarios `ui347-orderly-withdrawal`, `ui347-orderly-withdrawal-pass`, `ui347-evil-knowledge`.

## A Combat card's replacement is a gilt chip — UI-348 (2026-10-27)

The owner's decision EXP-15 D17 ([EXPANSIONS_UI_AUDIT](EXPANSIONS_UI_AUDIT.md)): after the reveal, Brand's Heir of Bard
(Brand Awakened, Free Peoples) and Shadows in the North (Brand Corrupted, Shadow) — and a Dark Chieftain's substitution
(UI-277) — are a gilt chip beside the player's own Combat card (`CombatReplacementChoice`,
`BattleContext.ReplacementChip`). The chip wears its source's arms (the nation emblem — the North for Brand, the
Chieftain's nation — stands in until their own art exists), glows and pulses with the window's choices, and its tooltip
names the card it cancels ("Click to use Heir of Bard · Cancels Valinor at initiative 0; …'s effects are given up · Once
per battle"); its click sends the replacement. The card and the chip share a row only during that step. "Keep card
effects" is the console's Pass (`OptionalDecline`, a decline that counts only in its own step since its id is generic);
no text button remains.

Tests: `KingsCards02Tests.BrandsReplacementIsAChipAndKeepingIsThePass`, `KingsDecisionNetworkTests` (Heir of Bard,
Shadows in the North, Keep through Pass); seeder scenarios `ui348-brand-heir`, `ui348-brand-heir-keep`,
`ui348-brand-shadows`; test-menu group "Kings · Decisions" (wotr-server `KingsDecisionScenarios`).

## The Dark Chieftains' substitution and the applied replacement — UI-277 (2026-10-27)

The owner's decisions EXP-15 D7 and D17 for the Kings Dark Chieftains:

- **Before the reveal** the Shadow may substitute a Chieftain's effect for its Combat card's (the Black Serpent's Red
  Wrath, the Shadow of Mirkwood's bats, Uglúk's command — `Kings/KingsChieftainCombat.cs`). Each Chieftain who may is a
  gilt chip with his nation's arms beside the Shadow's own card (`CombatReplacementChoice`, as Brand's, UI-348); its
  tooltip names the card whose effects it replaces and what the Chieftain does instead ("Click to use The Black Serpent
  instead of Deadly Strife's effects · Take one hit before reveal…"). Keep card effects is the console's Pass. The Free
  Peoples see neither the choice nor the Shadow card (server privacy, `KingsChieftainCombatTests`).
- **Once made**, a replacement — Brand's or a Chieftain's — stands as the same gilt marker, solid and without a click,
  beside the replaced card for both players (`BattleContext.AppliedReplacement`, from `CombatReplacements`); its tooltip
  names the source, and the opponent's card stays unnamed until the reveal ("The Black Serpent replaces the Combat
  card's effects this round").
- **Entry.** A Chieftain enters from his Characters row (UI-108): with a Council token the medallion marks it and lights
  his row (UI-347, D13); without the Council the die alone pays.

Tests: `KingsCards02Tests` (the Chieftain's chip, the applied marker for both players), `KingsDecisionNetworkTests`
(the Chieftain's chip before the reveal, Keep through Pass, the entry without a token; Brand's applied marker),
`ExpansionTokenNetworkTests.CouncilTokenAndMusterEnterAKingsChieftain`; seeder scenarios `ui277-chieftain`,
`ui277-chieftain-keep`, `ui277-chieftain-entry`; test-menu scenario `kings-chieftain-substitution`.

## A king chosen in the battle — UI-278 (2026-10-27)

The owner's decision EXP-15 D16 for There Is Hope for Victory (Ever at Your Service, Men of Little Villages) and His Power
Waxes (the Palantír of Barad-dûr, Work of the Enemy): while a king takes part in the battle — an Initial king's Leader,
an Awakened king's portrait in the force column — his figure glows there and its click chooses him; its tooltip reads
"Théoden · Awaken here if permitted. Add +1 to your Leader re-roll." A king outside the battle (a Corrupted king has no
figure) keeps his Characters portrait, and the console's hint names both places ("Click a glowing king in the battle
window's army or by his portrait"). The battle window stays open for the choice (`SovereignChoices.InBattleWindow`).

Tests: `KingsCardChoicesTests.AKingWhoTakesPartInTheBattleIsChosenInItsArmy`,
`KingsCards02NetworkTests.HopeChoosesWhichInitialSovereignAwakensDuringBattle`,
`KingsBranchNetworkTests.HisPowerWaxesFindsTheDefendingKingInHisArmy`; seeder scenario `ui278-power-defender`;
test-menu scenarios `kings-card-hope`, `kings-card-power-waxes`.
