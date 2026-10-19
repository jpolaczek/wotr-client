<a id="armie-na-mapie-panel-posiłków-i-ruch-jednostek"></a>
# Armies on the map, meal panel and movement units

The plan agreed 2026-10-13. UI-123 (`wotr-server` registry) receives the documentation preparation; UI-124-UI-131 implements card selected dice, graphics, tags, movement, reasons for recruitment on the server, meals panel and recruitment with cards; UI-132 receives the whole. Status and evidence remain exclusively in the `wotr-server` registry. This document does not confirm implementation.

<a id="cel-i-granice"></a>
## Purpose and boundaries

Report: "Let us now plan exactly how to look and behave army tags on the map, recruitment panels, moving units".

Base State (recognition with 2026-10-13):
- the whole map view does not draw armies; the tags appear from 160% matches (UI-30,
  `MapMarkerVisibility`);
- the tag does not know the nation, connects Nazgûle with leaders, is moved against the anchor and
  obscures the symbol settlement; siege is two squares 32 px differing only ‘ ⁇ ’; the rule
  `.illustrated-army` is likely to zero the colors of the pages and the state frame; the legend does
  not describe the army;
- The recruitment palette in the region panel says "Unavailable here with the remaining budget" at
  each lock, because the server removes groups without purpose and `RecruitmentOption` has no
  reason; no review of the reserves of all nations; recruitment with cards and capabilities has
  three separate interfaces;
- click your own army only opens the panel, figure selects individually; no preview of the goal and
  the sign figures after the first march; one click of figure is a few steps Esc; the disappearance
  of the target at the border of the nation without war is not explained; some texts talk about
  drag; the master's token, the cost of ability and movement with cards follow the tray path with
  Confirm;
- card status of the selected dice is counted (`CardActions.CanPlay`), but the style of
  `.hand-card.playable-card` will zero it.

The decisions of the user from this conversation:
- armies are always visible in two detail levels; replaces hiding with UI-30 (160%/145% threshold);
- The full tag is **figures on the stand** in the colour of the side with the coat of arms of the
  nation and the total number; garrison on the stand with blanks;
- recruitment by **the Meals Panel** along with the map; after the selection the Muster is
  highlighted by card possible to play with this dice;
- click your own army at dice Army/Character **signs the entire army**;
- the reasons for the recruitment block come from the server**; this requires publication of the
  test server package in GitHub releases for client tests;
- documentation, tasks and implementation in turn; no push.

The REQUIREMENTS R08, R09, R15–R20, R22, R23, UX_SPEC U01, U04, U05, [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md) and the rule of pressing with [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md). Without changing the rules of the game, commands and Contracts. Army movement remains only clicks (UI-105); drag remains only for reserves (R16). The graphics do not contain text, digits or rules; numbers, legality and hitboxes belong to the code (R08).

<a id="znaczniki-armii"></a>
## Army markers

<a id="dwa-poziomy-szczegółu"></a>
### Two detail levels

| Level | When | Appearance |
| --- | --- | --- |
| Token | approximation < 1,3× match (return below 1,2×) | puck approx. 28 pt in the enamel of the page, coat of arms of the nation with the highest number of figures, total number; garrison has blanks on the upper edge |
| Figures on the stand | approximation ≥ 1,3× match | up to three figures from the `board-figures-v2` atlas on the base of the page (approx. 76×56 pt, from UI-185 100×72 pt, from UI-192 116×84 pt); in front of the base game coats of arms of the nation (up to two, hereinafter ‘+’) and total number; garrison on the stand with blanks |

The thresholds are start values; UI-126 fine-tunes them on the entire 1366×768 and 1920×1080 map. No tag disappears at a distance. The level switch changes only the class (`army-token`/`army-figures`) without redevelopment of the elements. The Silvets retain the priority of `MapArmyArtwork` (the character, elite, regular, leader or Nazgûl). figure in the atlas is common to the page; the nation shows the coat of arms with `Art/UI/icons/nations`.

<a id="położenie"></a>
### Location

- The settlement symbol and control remains visible. The forces of the region with settlement are in
  one row just under the symbol: first the garrison (the “walls”), then the field armies; the army
  besieging the garrison, save further.
- Region without settlement: forces centered on anchor.
- Hitbox is a visible shape, centered; transparent fields do not capture clicks.
- `LayoutMarkers` bypasses the “ ⁇ n” and “romb” effect label; in the absence of space it reduces
  the interval, it never puts the tags on itself.
- Fellowship tag unchanged, in the same collision layer.

<a id="treść-i-informacja"></a>
### Content and information

- The total number includes regular, elite, leaders, Nazgûle and characters.
- Tooltip after 200 ms (R19) gives the composition per nation: regular, elite, leaders, Nazgûle
  separately, character names; garrison/pole/obl Only data on the safe projection.
- The `ArmyMarker` model in `BoardPresentation` gets counters per nation and a separate Nazgûli
  counter; still one tag per (region, page, garrison).
- The map legend (`MapLegend`) gets entries: token, figurines, garrison, siege, state of legal
  source and elected army.

<a id="stany"></a>
### States

| Status | Class | Appearance |
| --- | --- | --- |
| Hover | `:hover` | base game Lightening |
| Legal Source | `eligible-army` | green glow under base game |
| Chosen Army | `selected-force` | Gold border and lift 2 |
| Capacity owner | `ability-owner` | gold border without lifting |
| Pressing | `pressed` | principle UI-114 |
| After the march | `moved` | extinguishing and sign ✓ on the stand |
| The Spirit of Purpose | `army-ghost` | 40% opacity, without hitbox |

The `.illustrated-army` rule stops overwriting the colors of the pages and the border of the states; the pixel test in UI-126 checks each state.

<a id="wydajność"></a>
### Productivity

Rebuild only with new projection (as today, reuse of key buttons). You and zoom convert positions and possibly level class. `map17` profile (Dense board) cannot be worse than before the change; dumps of an empty and dense map of both sides.

<a id="ruch-jednostek"></a>
## Unit movement

Flow: die Army/Character → legal armies shine → **Army click marks the entire army** → goals shine → click target executes step.

- **Selection.** The Army's own click with legal `ArmyOption` indicates all figure options
  (`UnitIds`, `CharacterIds`; at the cost of Character with commander with `CommanderIds`). An army
  without command at dice Character does not shine as source. Goals with `ArmyActionTargets.For`
  shine immediately. The region's panel opens with the marked figure.
- ** The figure click in the panel shows it (and re-checks it). Selection without regular or elite
  has no goals; the panel says "A leader can not move without armies units." Based on lack of
  targets in the server option, without its own rule.
- Another army. ** A click of another army replaces the selection. A click of an enemy army or
  region with an enemy is an attack so far.
- **Review of the goal.** The implementation of the legal target shows the spirit marked figures
  (`army-ghost`) at the place they will take, and tooltip "Move n figures" or "Attack ·
  <BattleMode>" with `ArmyTargetOption.BattleMode`. Without predicting political effects and
  control: tooltip does not ask the server (R19).
- **The target click sends a step immediately (without a Confirm) as today.
- Second march. figure They've already moved, they've got a condition. `moved` in panel (✓, not
  selected) and on the tag; the tray shows ‘ →   → “ from `ActionProgress`,  ✓ He's done early.
- **Esc.** First Esc clears the entire selection on an open panel (one snapshot per gesture); second
  one closes the panel; another one undoes the choice of using dice so far. Right button unchanged
  (inspection/pan).
- **When the region is the target only for the parts marked figures
  (`ArmyTargetOption.EligibleIds`), tooltip and panel state which figure cannot enter. When the
  selection has no purpose, the panel says it directly instead of the general "No legal
  destination".
- ** `ArmyActionTargets.Hint`, `HelpTopics`, UX_SPEC (stretch and arrow lines) and `T12-MOVEMENT.md`
  according to UI-105.
- One flow.** The master's token, the cost of the capabilities and movements of the army with cards
  use the same map flow when the server option has the same shape (`ArmyOption`  +
  `MovementCostOption` Exceptions that require a common plan of several armies before sending remain
  in the tray with one ✓ and are listed in UI-128 Dead branch. `BoardDraft.Drop` and
  `DraftGesture.Drop` For the army he disappears along with tests of only this branch.

<a id="panel-posiłków"></a>

### Card army events — UI-216 (2026-10-16)

A card's army event (Help Unlooked For, Paths of the Woses, Through a Day and a Night, Shadows Gather, The Shadow
Lengthens, The Shadow is Moving, Corsairs of Umbar, the Nazgûl manoeuvres, Grond, The Fighting Uruk-hai) is
planned like an Army die: the armies the event may command glow (`eligible-army`); a click on one starts its move
with the whole army and opens its region panel, whose lit figures toggle; a lit destination completes the move
(routes keep their step-by-step clicks); another lit army starts the next move. The console counts "Armies moved
n of m", offers the event's alternatives (another kind of army step, Skip maneuver) as buttons, ↶ undoes the last
step and ✓ sends. The decision window no longer lists plans, routes or figures. Code: `CardArmyEvent`
(presentation), `CardArmyInteraction` (application); details in [CARD_CHOICES_UX](CARD_CHOICES_UX.md).

## Meal Panel

From UI-179 (user decision with 2026-10-15) the Meals window is a tab on the left list under Politics, and the lines do not show the policy track neither action Politics/Narya ([POLITICS_REINFORCEMENTS_UX](POLITICS_REINFORCEMENTS_UX.md)). The window description below is historical in the window part, track and action; the gestures of chips remain.

<a id="otwarcie"></a>
### Opening

The Muster (die Muster, Army/Muster as Muster, Will of the West as Muster) window opens the "Reinforcements" (`FloatingWindow`) over the dice tray, without blocking the center of the map; you can move them like other windows. The closing of the window does not undo the dice selection; reopening through the button in the tray. Change of dice or the end of the sequence closes the window.

<a id="zawartość"></a>
### Content

- Headline: title, two budget slots (elite occupies both), ✓ Finish in the process of sequence.
  `ActionProgress` remains the main indicator (R17).
- A poem for each own nation (FP five, Shadow three): coat of arms, name, status from the policy
  track (At war), ‘1 step to war’, ‘Passive · n steps’), reserve as mini-base game with regular
  counters, elites, leaders and Nazgûli.
- Nation without recruitment: extinguished line, reason from server (e.g. “Rohan is 1 step from
  war”), Politics button opens a policy panel where the same die can move the nation.
- All rows are placed without scrolling (R18) in four layouts.

<a id="gesty"></a>
### Gestures

- The reserve token clicks it; legal settlement with `Targets` shine on the map, the other
  settlement nations fade with the reason in the tooltip.
- The settlement highlighted click recruits immediately (sequence step as today).
- The chip click at exactly one legal settlement recruits there (from UI-142 instead of a double
  click; with several settlement these settlements shine).
- Draging a token to settlement is a shortcut (R16).
- The settlement click on the map at the active Muster mark the line of its nation; the pallet in
  the region panel is abbreviated and uses the same reasons.
- Elite at one budget point: disabled from the server.
- Warning about leader without army remains (`MusterActions.OrphanLeader`, server preview).
- Recruitment animated the arrival of figure to settlement (`MapFigureMotion`, arrival of 0,45 s),
  without blocking commands.

<a id="rekrutacja-z-kart-i-zdolności"></a>
### Recruitment with cards and capabilities

The same presentation serves `EventRecruitmentOption` (limits per nation and region, `MaximumRegions`,  `DistinctNations`), elections " ReserveFigures ” recruiting Kings rulers and optional recruitment at Balrog's entrance. Mode card shows the source in the header, scheduled deployments as ghosts on the map with ×, and the aggregate distribution approves one ✓  (R17,  UI-67 Scope and exceptions: UI-131.

<a id="karty-przy-wybranej-kości"></a>
## card on selected dice

Selected die (each, including Muster) distinguishes card into the hands that you can play with it ( `CardActions.CanPlay` ): green glow of the variant `parchment-playable` , in the colour of legal purposes board (talies, armies); others card extinguish to 50% opacity, with the previous explanation in the tooltip, and developed card It's back to full cover. card ( UI-91 ) Without selected dice no distinctions and extinguishing. Change dice refresh only classes, without redevelopment of hand and without moving cards Muster: card Strategy with the Muster icon (test scenario `fp-muster-event` ).

Derogations from the original plan (UI-124): without lifting card, because the hand layout with UI-91 requires fixed slot positions; without the counter on the rolled-up hand, because from UI-91 the hand is permanently docked, and its tab hidden.

<a id="serwer-powody-rekrutacji"></a>
## Server: Reasons for recruitment

New `src/Wotr.Domain/Views/RecruitmentAvailabilityProjection.cs` projection included in `InGameProjection.Availability`, in the model of `ResourceAvailabilityProjection` and `SovereignAvailabilityProjection`:

- one `ActionAvailabilityView` for each own nation of the viewer, `SubjectId` = nation, `ActionId` =
  `recruit`, `Targets` = legal settlement of the regular recruitment in the current state (including
  sequence);
- `Conditions` with codes: `at-war` (description with number of steps), `reserves` , `settlement`
  (per settlement : `captured` , `enemy-army` , `besieged` , `used-this-action` ,
  `nazgul-stronghold` , `kings-weakness` ), `elite-budget` ;
- the reasons for counting one diagnostic function in `RecruitmentRules` used also by `CanPlace`, so
  the rule exists once;
- only public data (policy track, control, armies, siege, viewer sequence status); without changing
  Contracts.

Client reads `PlayerView.Availability` (`ActionId == "recruit"`). Client tests require a new package of the test server (`scripts/package_deps.py --test-backend`) in GitHub releases and conquests `wotr-deps.lock.json` (user's agreement with 2026-10-13).

<a id="grafika-do-utworzenia"></a>
## Graphics to create

Category `graphicalassets/ui/src/armies/`, entries in `assets.json`, contract in `Tests/Editor/UiArtKitTests.cs`, preview `previews/armies.png`.

| Group | Files | Options |
| --- | --- | --- |
| Bases | `base-field` (76×22), `base-garrison` (76×28, wall with blanks) | `free`/`shadow` × base, `selected`, `eligible`, `pressed`, `moved` |
| Tokens | `token-field` (30×30), `token-garrison` (30×34, crown blank) | like a stand |
| Characters | `moved-check` (14×14), `ghost-outline` (48×48, 9-slice) | — |
| Reserves | `reserve-slot` (48×48, 9-slice), `budget-slot` (18×18) | `selected`, `pressed`; `filled` |
| Cards | variant `playable`lic card in hand: green glow (UI-124) | — |

Silicas from `Art/board-figures-v2` , coats of arms with `Art/UI/icons/nations` No change. Pallets Tokens `--army-glow` (gold, in `eligible` green of the legal purpose), `--army-veil-opacity` (slighting) `moved` ) and `--slot-fill-opacity` (budget item). The elite occupying both points shows the code, not a separate variant. Preview with real silhouettes and coats of arms: `previews/armies.png` ( UI-125 ).

<a id="kod"></a>
## Code

- `Runtime/Map/BoardPresentation.cs` — meters per nation and Nazgûle.
- `Runtime/Map/MapCanvas.cs`, `Runtime/Map/Figures/MapArmyArtwork.cs` — two levels, base game, coat
  of arms, layout at settlement, states, spirit.
- `Runtime/Presentation/MapMarkerVisibility.cs` — from visibility to level selection.
- `Runtime/Map/MapLegend.cs`, the new `Resources/Wotr/Styles/Armies.uss` (Army rules transferred
  from `Wotr.uss`).
- `Runtime/Presentation/ContextArmySelection.cs`,
  `Runtime/Application/ActionContextInteraction.cs`,
  `Runtime/UI/RegionHoverPanel.cs`, `Runtime/Map/MapDraftOverlay.cs`,
  `Runtime/UI/Menu/BoardEscape.cs`, `Runtime/Presentation/ArmyActionTargets.cs`,
  `Runtime/Presentation/Help/HelpTopics.cs` — movement.
- `Runtime/UI/DecisionTray.cs`, `Runtime/Presentation/BoardDraft.cs`,
  `Runtime/Presentation/DraftGesture.cs` — one flow and a dead code.
- New `Runtime/UI/Muster/ReinforcementsPanel.cs` (+ nation line),
  `Runtime/Application/MusterContextInteraction.cs`, `Runtime/Presentation/MusterActions.cs`,
  `Runtime/UI/RegionRecruitPalette.cs`, `Runtime/UI/EventRecruitChoices.cs`,
  `Runtime/UI/Characters/CharacterRecruitChoices.cs` — recruitment.
- `Runtime/UI/Cards/HandView.cs`, `Runtime/UI/Cards/HandCard.cs`, `Resources/Wotr/Styles/Cards.uss`
  — card selected dice.
- Server: `src/Wotr.Domain/Armies/RecruitmentRules.cs`,
  `src/Wotr.Domain/Views/RecruitmentAvailabilityProjection.cs`,
  `src/Wotr.Domain/Views/InGameProjection.cs`.

<a id="scenariusze-i-właściciele-odbioru"></a>
## Screenplays and owners acceptance

The owner prepares the evidence in his task; UI-132 takes the joint course.

| ID | Owner | Observable scenario |
| --- | --- | --- |
| AM01 | UI-124 | The selected die distinguishes card from the possible green glow (Muster, Army, Character, Event, Will), the remaining extinguished with explanation. |
| AM02 | UI-124 | Without dice no distinction and extinguish; change dice without rebuilding and moving cards. |
| AM03 | UI-125 | Army graphics set with page and state variants in 1× and 2×; contract `UiArtKitTests`; preview. |
| AM04 | UI-126 | The view of the entire map shows the token of each force; switching levels with hysteresis without rebuilding. |
| AM05 | UI-126 | Figures on the stand: silhouettes, base game sides, coats (≤ 2 + ‘+’), number; centered hitbox. |
| AM06 | UI-126 | Garrison on the stand with blanks at settlement, besieging next to, help further; readable in both levels. |
| AM07 | UI-126 | Tags do not obscure the settlement symbol and control, effects labels or a diamond of attention; dense MAP-17 scenes without application. |
| AM08 | UI-126 | States hover, eligible, selected, ability-owner, pressed and moved visible in pixels; legend entries. |
| AM09 | UI-126 | Tooltip per nation with Nazgûla separately and character names, only public data; `map17` profile is no worse. |
| AM10 | UI-127 | The click of his own army at dice Army marks the entire army and immediately shows the targets; at Character with the commander. |
| AM11 | UI-127 | Panel with selected figure; click select (division); leaders themselves without targets with explanation. |
| AM12 | UI-127 | Target Name: spirit and tooltip Move/Attack without request of the server; click the target sends a step without Confirm. |
| AM13 | UI-127 | Second march: figure after movement in the state `moved` in the panel and on the map; ✓ He's done early. |
| AM14 | UI-127 | First Esc clears selection, second closes panel; die stays; no command. |
| AM15 | UI-127 | Partially legal purpose lists figure which cannot enter; texts without drag. |
| AM16 | UI-128 | The master's token, the cost of ability and movement with cards in the map flow or the explicit exception; dead code removed; regression. |
| AM17 | UI-129 | Server: entries `recruit` per nation with conditions and targets; Rohan tests step away from war, settlement scored, enemy, siege, used settlement, Nazgûl, elite at one point; no hidden data; parcel and lock. |
| AM18 | UI-130 | Muster opens the Meals panel; nation lines with status and reserves; nation without right extinguished with the server's reason and the Politics shortcut. |
| AM19 | UI-130 | The chip → settlement shine → click recruits; with one goal click the token (UI-142, formerly two-click); drag; budget slots; elite at one point disabled; lone leader; region pallet without budget message. |
| AM20 | UI-131 | Recruitment from cards, Kings and Balrog rulers in the Meals Panel with Limits and One ✓; Regression of packages cards. |
| AM21 | UI-132 | 4 discharges of × 2 chipsOn both levels, dense scenes, siege Helm's Deep, meal panel; regression PlayMode; profiles; builds. |

<a id="weryfikacja-i-przekazanie"></a>
## Verification and transmission

- EditMode: `UiArtKitTests`, `MapArmyArtworkTests`, `MapVisibilityTests`, `ArmyContextTests`,
  `BoardDraftTests`, `MusterActionTests`, `EventRecruitDraftTests`, new level tests, layout, panel
  and cards.
- PlayMode with real server in packages: `MapContourAcceptanceTests`, `BoardInteractionTests`,
  `ArmyContextNetworkTests`, `MapClickMoveNetworkTests`, `T22BoardNetworkTests`,
  `FigureMotionTests`, `FigureMotionNetworkTests`, `MusterNetworkTests`, `HandCardLayoutTests`,
  cards recruiters regression.
- Server: `MusterTests`, `RecruitSequenceTests`, new projection tests of reasons, full
  `scripts/test.sh`.
- 1366×768 and 1920×1080 dumps at 100/125%, both sides, both levels, dense board and siege; `map17`
  profile.
- Manually: test scenarios mode (`fp-muster-recruit`, `fp-army-move`, `fp-muster-event`) on local
  Development server.
- Builds macOS/Windows from checkout, `python3 scripts/check_docs.py` (client), `tasks.py validate`
  and `pin-repo` (server) after each client commit.

Results, commands and commits go to the to-do documents, UX_SPEC, BOARD_INTERACTION_UX, TESTING and the registry. Push out of range; publication of the test server package only in UI-129.

<a id="wdrożenie-ui-126"></a>
## implementation UI-126

- `ArmyMarker.From` counts figure per nation (the largest first), Nazgûle separately from the
  leaders of the nation, and saves character names; `ArmyMarkerText.Describe` builds tooltip.
- `MapArmyArtwork` has base game or a token (`army-plinth`), up to three silhouettes, two coats of
  arms and a "+", number and character `moved`; unused parts hides class, and images choose
  `Styles/Armies.uss` (48 rules equal in weight in the order of states). Old rules of tags left
  `Wotr.uss`.
- Sizes: figurines 76×56 (garnison) 76×60 ), token 30×30 (garnison) 30×34 ); from UI-185 figurines
  100×72 (garnison) 100×78 ), token 38×38 (garnison) 38×42 ); from UI-192 figurines 116×84
  (garnison) 116×90 ), token 44×44 (garnison) 44×48 ); the crest of the token 12 pt in the upper
  left quarter, number 10 pt in the right bottom.
- Order layout, position sequence and region panel rule:
  [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md), “implementation army tags — UI-126”. The layout
  does not allocate when panning (without LINQ and arrays in the region loop).
- The map legend has an “Armies” section with chips: army, garrison, possible action, selected
  force, after march.
- Derogation from the plan: the government stands above the settlement symbol, not under it, because
  the sign of effects lies under the symbol.

<a id="wdrożenie-ui-127"></a>
## implementation UI-127

Flow and class describes [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md), ‘implementation with my — UI-127’. Derogations and clarifications:
- the target signature is a label under the spirit, not a tooltip, because the map area does not
  have its own tooltip; the content is only from the server option;
- the `moved` sign on the map gets only the force that all figure moved; figure in the panel has it
  always;
- Esc closes the panel with one step and the empty focus is directed by a key outside board
  (preserved, outside the scope).

<a id="wdrożenie-ui-128--jeden-przepływ-ruchu-i-wyjątki"></a>
## implementation UI-128 — one movement flow and exceptions

Army traffic path inventory (2026-10-13):

| Path | The mechanism in the client | Flow | Reasons |
| --- | --- | --- | --- |
| die Army, Character, Army/Muster, Will of the West, also with the ability (`AbilityId`) | `ContextArmySelection`, context dice | map krokowo (UI-127) | server runs steps in `ActionStepRules` for dice and capabilities |
| King's ruler's token paying for the action army | `RulerTokenId` in the sketch, `ArmyChoices` | tray, whole plan and one approval | `ActionStepRules.Matches` takes another step without a token and with paid dice; the plan of the two armies must hit one command |
| card with route march (`ArmyEvent.RouteMovement`, e.g. Shadow marches) | `EventArmyChoices` | tray, whole plan and one approval | Several armies with a step limit are planned together (`SeparateRoutes`, `MaximumSteps`) |
| card with rescue (`ArmyEventOrder.ReliefAttack`) | `AttackChoices` | tray | The entrance and attack form one plan |

UI-289 (2026-10-19): the three tray paths above are gone. The server no longer lets a ruler token pay for armies (`KingsRulerTokens`), and every army event, route marches and relief attacks included, moves on the map and in the region panel (`CardArmyEvent`, UI-216), so `ArmyChoices`, `EventArmyChoices`, `EventArmyPlans` and `AttackChoices` were removed.

The step-step movement that would still go through the tray is gone, so nothing moved. In the tray paths, the tag click continues to mark the entire army (`BoardDraft.SelectRegion`) as on the map. The transfer of the ruler's token to the map requires a change of server (step sequence without dice) and does not belong to UI-128.

The dead branch of the Army drop was removed: `DraftGesture.Drop` rejects armies, and `BoardDraft.Drop` only supports figure from the reserve. Tests of the same branch were replaced by checking that the dropped army never changes draft (`BoardDraftTests.AnArmyDropNeverChangesTheDraft`, `BattleDraftTests.AttackClickPreservesOriginRequiresAUnitForAnyRearFigureAndIgnoresAnArmyDrop`).

<a id="wdrożenie-ui-130--panel-posiłków"></a>
## implementation UI-130 — Meals panel

- `ReinforcementsPanel` ( `Runtime/UI/Muster/` - I'm sorry. `FloatingWindow` on the left rail, above
  the tray; opens using the Muster when draft There's no other command The × or Esc closure does not
  reverse dice ; Reinforcements button in tray opens panel again. dice , transition to politics and
  the end of sequence closes it.
- The status of the view counts `ReinforcementsState.For`, and the lines `ReinforcementRows.For`:
  legality with the option `Recruit`, the reason for the entry `recruit` (UI-129) in order war →
  reserves → settlement. Without the entry (older server) the row shows the status itself. The elite
  at one point gets a description of the condition `elite-budget`.
- Header: two budget slots (`ReinforcementRows.Budget`),  ✓ visible only in the viewer recruitment
  sequence (same `FinishActionSequence` What's up? `ActionProgress`),  ×.
- The reserve chip (`MusterActions.Select`) button saves one step back: step up recruiting with
  selected figure without settlement, so the map only shines settlement with this figure
  (`MusterActions.Targets`); click settlement sends a step. The two-click of the chip recruited when
  figure had exactly one legal settlement; since UI-142 does this single click of the chip
  (BOARD_INTERACTION_UX, “The two-clicker – UI-142”).
- The settlement click on the map means the line of its nation (`reinforcement-focused`, event
  `RegionRecruitPalette.Inspected`). The region's pallet gives the reason for the server
  (`ReinforcementRows.PaletteReason`) instead of the "Unavailable here with the remaining budget"
  and still disappears for the enemy's nation.
- figure from the reserve (also the player entering the game) gets animation of arrival at the
  destination (`BoardFigureChanges`); separated Companion still flies from the Fellowship region.
- Derogation: the other settlement nations do not extinguish with a separate state and do not have a
  tooltip, because the map area has no tooltip (description requires click, AGENTS and U04). The
  reason is seen in the nation line, in the token tooltip and in the region pallet after clicking
  settlement. Rating in acceptance UI-132.
- Recruitment with cards and capabilities stays in the existing interfaces to UI-131; the panel
  opens only to dice Muster.

<a id="wdrożenie-ui-131--rekrutacja-z-kart-i-zdolności"></a>
## implementation UI-131 — recruitment with cards and capabilities

- The `ReinforcementPlan` model with three sources (`IReinforcementSource`): `EventReinforcements`
  (card with `EventRecruitment`), `EffectReinforcements` (the "ReserveFigures" election: each figure
  has its own region) and `CharacterReinforcements` (the Kings ruler's or figure entering with
  Balrog). Rules remain in `EventRecruitDraft`, `EffectChoiceDraft` and `CharacterRecruitDraft`; the
  source only builds poems and edits draft.
- The plan takes precedence over Muster. `ReinforcementsInteraction` is the only owner of the panel
  and the spirits on the map; `MusterContextInteraction` stays on the region's palette and click
  settlement.
- Panel in plan mode: title ‘Reinforcements · < card or ability>’, ‘Optional’ when the plan can be
  empty (Balrog, card minimum 0), slot for each figure the plan, ✓  (`reinforcements-confirm`) and
  the lines of the nation with limits ("Up to Minas Tirith 2  · …”, „One figure · …”, „To <region>”,
  settlement Under the lines planned figure with × They're going back to the reserve. ✓ triggers the
  same approval as the tray and is only active if the tray has a legitimate server preview (event)
  `DecisionTray.ConfirmChanged`), so there is still one approval (R17).
- card with the region to choose from is marked by figure, and a click of the highlighted region is
  planning it; the selected region adopts figure immediately (the `Select` logic). The
  ‘ReserveFigures’ chip plans figure right in its region; in the group of one figure another type
  replaces the scheduled one, as in the selection of card. The chip counter shows the reserve after
  subtracting the plan; the closed token and line give the reason (All references of this card are
  planned), ‘One figure per nation’, ‘No region left within this card', ‘This card' limit is
  reached’, ‘No settlement left for this figure).
- Map: `MapPlannedRecruits` draws in each region of the figures spirit plan on the stand
  (`planned-recruit-<region>`, non-interactive) with × in the corner
  (`planned-recruit-remove-<region>`), which returns all figure from this region
  (`ReinforcementPlans.RemoveRegion`). Location like the spirit of motion with UI-127, updated at
  each `MapCanvas.Refresh`.
- The tray no longer has separate pallets (`EventRecruitChoices`, `CharacterRecruitChoices` removed,
  “ReserveFigures” outside `CardChoices`); shows the decision title, “Reinformments” button and its
  own approval. The cost of dice is left in the inspector tray (UI-108).
- The names of the items retain the prefix of the source: `event-reserve-<nacja>-<rodzaj>`,
  `character-reserve-…`, `effect-reserve-…` and `event-placement-<n>`, `character-placement-<n>`,
  `effect-placement-<n>`.
- Derogation: two-click of the token in the plan mode does not recruit (there was no token in the
  tray); click and drag enough.
- The panel overlooks the top of the scene window when it opens for new use. The drawn Charters list
  lies above the stage, so after Balrog's input, it covers the panel until the cursor leaves it and
  steals it (UI-92, UI-98); the ruler inspector (the window) does not cover the panel.

<a id="wdrożenie-ui-134--zwarty-panel-posiłków"></a>
## implementation UI-134 — compact meal panel

User notification with 2026-10-14: less text, more graphically shown options, the state of the nation on the track and its action in the line.

- Nation's poem is one line: coat of arms, name, policy track, reserve chips (34 px), action. War
  status, card limits and reason for the blockage are in the line tooltip; the reason line and the
  Politics button disappeared.
- Track (`ReinforcementTrackView`): three paces and a war field, from left to right. The nation's
  flag fills its field when the nation is active, and outlines them when it is passive; the war
  field has red.
- action at Muster, only with server option: Politics icon moves the nation step
  (`PoliticalActions.Advance`, the same step as Politics panel, without Confirm), the ring icon
  activates it with Narya (`PoliticalActions.Activate`). Without the legal button option there is
  no.
- The reserve token click selects it and, when the illuminated settlement lie beyond the view, moves
  the map to cover all of it (`MapCanvas.Reveal`: motionless when visible; remoteness only if
  needed, no further than the view of the entire map). Nothing sends.
- The cards (card from hand that the selected die can play) bar has been removed in UI-150; card
  plays from hand. In the plan mode, the action track is not shown.

<a id="wdrożenie-ui-135--jedno-okno-rekrutacji"></a>
## implementation UI-135 — one recruitment window

The user's application from 2026-10-14: "Let's not show the old recruitment window anymore, just the new one".

- The reserve pallet in the region panel and the tray recruitment pallet are gone. Each recruitment
  (Muster, card, ability, King's ruler, Balrog, the ruler's token paying Muster) takes place in the
  meal panel.
- The settlement click on Muster represents the line of its nation and restores the closed panel
  (the open panel does not exit other windows, e.g. Politics); with the selected figure recruits or
  shows in the tray the reason for the server, why this settlement is out.
- Muster paid for with the master's token or skill (`RecruitDraft`, the whole plan in one command)
  is the fourth source of the plan: the title “Muster”, two slots, the elite occupying both, figure
  with × one ✓.
- A warning about a leader without an army is in the tooltip of the Leader chip
  (`ReinforcementFigure.Warning`) when one of its settlements has no army.
- Derogation: previously click the pallets first planned figure and showed the server preview, and
  the two-click sent it. Now, as from UI-130 in the panel, click settlement sends a step straight
  away; the effect for the leader announces tooltip.


<a id="źródła-kości-character--ui-145"></a>
## Sources dice Character — UI-145

User application from 2026-10-14: "click dice Character backlights the fields where armies stand without Nazgûla or heroes that can't really be moved".

- The server sends one option `MoveArmies` (and `AttackArmy`) with all armies; the cost of Character
  has `RequiresCommander`, and the cost of Lords' abilities also `RequiredCharacterId`. Previously,
  the client treated each option army as a source, and the army without commander rejected only on
  purpose.
- `ContextArmySelection.Forces` counts the sources from the cost of the selected dice
  (`ActionDieContext.ArmyCost`, with the priority of Army like `ArmyFace`): with `RequiresCommander`
  only armies with impenetrable `CommanderIds`, with `RequiredCharacterId` only army with this
  character. These sources benefit from the light of `eligible-army`, figure selected in the region
  panel, click the army, “mark all” and the journey of characters combined with the Army movement.
- die Army and Will as Army still shines every army with legal movement. Army click without
  commander at Character only opens the region panel, and the tray says "Click an army with a
  commander to select all of it".

<a id="panel-posiłków-bez-kart--ui-150"></a>
## Meal Panel without cards — UI-150

User application from 2026-10-14: "With taba reinforcements we remove playable card, it is enough that they will be possible to play from the main panel cards on the right at the bottom".

- Toolbar cards under the lines of the nation (UI-134:  `ReinforcementCardsView`,
  `ReinforcementCards`,  `ReinforcementsState.Cards`,  `HandView.Press`) was removed along with
  styles. The meal panel includes nation lines, slots, plan and ✓.
- card, which the selected die can play, distinguishes the hand in the lower right corner (UI-124)
  and there they are played; the light of dice (UI-136) unchanged.

<a id="odbiór-ui-132"></a>
## acceptance UI-132

acceptance zmielny 2026-10-14 zmielny ed UI-124–UI-157 (last changes to acceptance: `cbdc3de` profile tools, 33 px targets in battle window and map edge test), the lock test server (`dc3e6d61abc0`).

<a id="macierz-am"></a>
### Matrix AM

| ID | Proof |
| --- | --- |
| AM01–AM02 | UI-124: `UI124ChosenDieLightsItsCardsAndDimsTheRest`; in acceptance `HandCardLayout`, `CardInteraction`, `HandDrawer`, `ActionCardLayout`, `CardGestureNetwork`, `CardViewportLayout` 12/12. |
| AM03 | UI-125: 47 Army Textur with `.meta`, `UiArtKitTests`; in acceptance full EditMode 722/722. |
| AM04–AM08 | UI-126: `ArmyMarkerTests`, `MapMarkerVisibilityTests`, `MapArmyArtworkTests`, `MapVisibilityTests` (drops `ui126-map-fit/zoom` in four layouts), legend of "Armies"; dense scenes `map17-dense-*` both sides in four layouts and before/after with panorama (`MapContourAcceptanceTests`), field and garrison Helm's Deep separated. |
| AM09 | UI-126 (tooltip per nation, public data); `map17` Development Player profile performed only including acceptance, cf. “Profile”. |
| AM10–AM15 | UI-127 : `ArmyMovementTests` , `BoardInteractionTests` ( `UI127OneClickSelects…` ), `ArmyContextNetworkTests` ; in acceptance `BoardInteraction` and `ArmyContextNetwork` 19/19 , `MapClickMove` , `FigureMotionNetwork` , `Movement` (specify 1/1 ). |
| AM16 | UI-128: Packages expansions,  ShadowStrategy, Strategy,  CardPack, Kings,  Fate/Lords/Muster; in acceptance   `ExpansionToken` / `ExpansionAbility`   19/19,  `KingsCards01` / `LordsShadow`   9/9,  `KingsCards02`   7/7. |
| AM17 | UI-129: `RecruitmentAvailabilityTests`, `MusterTests`, `RecruitSequenceTests`; in acceptance full `scripts/test.sh` server 3848/3848; package published (UI-133). |
| AM18–AM19 | UI-130 (with UI-134, UI-135, UI-142, UI-150): `ReinforcementsLayoutTests` (split `ui130-reinforcements-*` from both sides in four systems), `MusterNetworkTests`, `MultiUseDiceNetworkTests`, `ResourcePanelNetworkTests` 12/12. |
| AM20 | UI-131: `ReinforcementPlansTests`; in acceptance Strategy01–04, CardPack04, ShadowStrategy03/04 and the Strategy03/04 and ShadowStrategy03/04 network without new failures after the 33 px amendment. |
| AM21 | This acceptance: drop, regression, profiles, builds (lower). |

<a id="regresja-i-buildy"></a>
### Regression and builds

- EditMode 722/722 before and after the amendments.
- PlayMode: 18 series of packages (65 classes of map, movement, recruitment, cards, battle,
  Fellowship and board edges) 199/211, then replays separately. First run failures:
  - 7× server login limit (`TooManyRequests`): `MapContourInteraction` 5/5 and
    `MapContourNavigation` 2/2 separately;
  - `MovementNetworkTests` depending on order after `FigureMotionNetworkTests` (known from UI-126):
    separately 1/1;
  - `MapViewportTests.MovingArmyCannotStopNavigationUnderAStationaryEdgePointerAtFourLayouts`: the
    test held the cursor at anchor height and from UI-126 the power row stands above the settlement
    symbol (the 7,5 px marker above the cursor); the test now measures the position of the row,
    class 8/8;
  - `ShadowStrategy04LayoutTests`: from UI-149 includes the window footer battle, where the 32
    target was rounded to 31,87; the 33 px targets ([BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md)), with
    `BattleLayoutTests` 4/4;
  - I will find: `ContextHoverTests.ClickedInspection…` (0,65 px, also on `c17fa15`) and
    `BattleReportNetworkTests.ClosedReportRejectsLateHttp…` (UI-115, also on `39a4e64`).
- After the 33 px amendment, two failures of 31,87 are also passed: `KingsCards02NetworkTests` 7/7
  and `ExpansionWindowNetworkTests` 5/5..
- `BuildLocalMac` and `BuildLocalWindows`: Success. Smoke without user profile
  (`scripts/smoke-macos.py`) not made: it transfers real application data and user preferences.

### Profile

- `map17` ( `scripts/review-map-client.py` , Development Player, M1 Pro, Metal): 36 workloads, 42
  discharges, lowest average FPS 168,6 ( `override-whitehand` ), longest feedback 13,8 ms; 1366×768
  in the window, 1920×1080 in full screen window on monitor 1080p (mode recorded in report).
  2026-10-08 (before) UI-126 ) vertices UI They've grown by about a hundred. 13% ( 0,91 → 1,04
  million), the time frame of the idle map with 0,5–0,7 ed 1,1–1,75 ms, and the reconstruction of
  the national belts from 3,8 ed 5,7 ms. The requirement R23 is met with the stock; the trend is an
  open risk to weaker equipment (no measurement of Windows and Intel Mac).
- `board` ( `scripts/profile-client.py --suite board` ): 40 workloads, 71–73 FPS at VSync monitor 75
  Hz ( UI-121 : 102–110 FPS on screen 120 Hz), longest gesture service 12,9 ms and time to end
  rendering 21,4 ms (≤ 100 ms); mean gesture handling 2,6 ms to 2,0 ms in UI-121 .
- Profile tools ( `cbdc3de` ): resolution 1920×1080 goes to the full screen window when the window
  is not in; the map's load stage puts Helm's Deep outside the attached window card and moves the
  cursor after the region panel is located; control card characters read separate labels UI-139.

<a id="ocena-odstępstwa-ui-130"></a>
### Assessment of the UI-130 derogation

The remaining settlement nations do not extinguish with a separate state and do not have a tooltip. Adopted: the map area has no tooltip (AGENTS, U04), legal settlement shine, and the reason for the server is in the nation line, the tooltip of the token and the tray after clicking settlement, which figure cannot occupy (UI-135).

<a id="większe-figury--ui-185-2026-10-15"></a>
## Bigger figure — UI-185 (2026-10-15)

The user's application from 2026-10-15 (map dump at Orthanc): "Let's increase the visibility of figures and characters on the map, they are now extremely small and vague ... on the map and in tabs the same".

- Map, about 30% more than in UI-126 (`MapArmyArtwork.Size` and `Styles/Armies.uss` have the same
  dimensions, so the distribution at settlement counts with new sizes):
  - Base figure 100×72 pt (garnison) 100×78 pt), base game 28 pt (garnison) 36 pt);
  - 44×52 figure from 23 step;
  - coats 13 pt (garnison) 15 pt), number 14 pt, motion marker 18 pt;
  - Token 38×38 pt (garnison) 38×42 pt), coat of arms 14 pt, number 13 pt.
- Expression: Behind each figure lies its dark copy (thin `rgba(18, 13, 8, 0.6)`), moved by 1,5 pt
  right and down. Shadow cuts figure off the clear terrain. Shadows are drawn in front of all
  silhouettes, so none falls on the neighboring figure.
- The Fellowship tag on the map has 56×62 pt (previously 44×48), and figure in flight
  (`MapFigureMotion`) have 56×62 pt (with step 17) entitled Point under the middle feet figure does
  not change.
- Panels:
  - in the region panel figure have 42×46 pt, a portrait of the character 46×46 pt (in a compact
    panel) 40 heights;
  - on the Reserve tab, reserve chips have 40×40 pt and number of 12 pt.
- No change: switching figures/ At an approximate rate (≥) 1,3 × fit) and relent to the plunger
  chips.

<a id="jeszcze-trochę-większe-figury--ui-192-2026-10-16"></a>
## Even slightly larger figure — UI-192 (2026-10-16)

The user's application from 2026-10-16: "Let's increase the size of the graphics of the individuals and heroes on the map". Heroes stand on the map as the first figure of the army or as a Fellowship tag, so they grow together with armies.

- Map, about 15% more than in UI-185 (`MapArmyArtwork.Size` and `Styles/Armies.uss` still have the
  same dimensions):
  - Base figure 116×84 pt (garnison) 116×90 pt), base game 32 pt (garnison) 42 pt);
  - 50×60 silhouettes with step 26, shadow like in UI-185;
  - coats 15 pt (garnison) 17 pt), number 16 pt, motion marker 20 pt;
  - Token 44×44 pt (garnison) 44×48 pt), coat of arms 16 pt, number 14 pt.
- The Fellowship tag in the region has 64×72 entitled and stands 64 entitled above the point of the
  region; figure in flight have 64×72 entitled with a step 20 pt, and the point under the feet of
  the centre figure to (32, 60).
- No change: panels ( UI-185 ), Mordor track, switching figures/ Tokens and relenting to chips with a
  piston.

<a id="poświata-ikon-w-reserve--ui-197-2026-10-16"></a>
## Imaging at Reserve — UI-197 (2026-10-16)

User notification from 2026-10-16 "Let them shine a little, in the reserve, too, to be more visible"): the coat of arms of each poem of the Reserve stands on a soft golden feast ( `IconGlow` , graphics `ornaments/move-glow` ) which leaves 7 The new coat of arms on the reserve chip also has a glow, within the slot, so the chip does not change size. The closed poem extinguishes the coat of arms along with the glow.

<a id="znaczniki-przy-brzegu-widoku--ui-199-2026-10-16"></a>
## Tags at the edge of the view — UI-199 (2026-10-16)

The user's report from 2026-10-16: when scrolling maps, the characters and units moved to fit or disappear, and were never cut in half. They are not to adjust and not jump, like other tags.

- `MapCanvas.LocalMarkerRect` no longer trims the rectangle to the view: the power row stands where
  it falls relative to the point of the region, and the map (`overflow: hidden`) cuts it off on
  shore.
- The row, the Fellowship tag (in the region and on the Mordor track), the recruitment plan and the
  label of the effects are shown when the point of the region lies in the view or closer than
  `MapCanvas.MarkerReach` (160 pt) from the shore (`NearView`). This allows them to slide in and out
  with the map.
- The government only gives up to forces before it, not to the HUD panels or the open panel of the
  region. So its place depends solely on the map and does not change when scrolling. The name of the
  regions still hides under the HUD panels.

<a id="figury-w-panelach-na-poświacie--ui-201-2026-10-16"></a>
## figure in the panels on the ground — UI-201 (2026-10-16)

The user's application from 2026-10-16: figure and the units in the panels are to get a glow or another effect to not die and see their details. Direction, not hard requirement: more "seamless", where you can without frames, the characters' graphics themselves, but visible.

- `FigureArtwork.Apply` places under each figure in the panel a soft gold glow (`IconGlow`,
  `figure-glow`, 0,6 cover, when invading 0,85). This applies to the region panel, losses in the
  window battle, Fellowship and the figures grids. The Fellowship tag on the map does not get it.
- The tile in rest has no background or frame, only a thin (2 pt) line in the color of the page
  under figure. The frame and filling appear only for the state: selected (gold), legal target
  (green), loss selected in battle (gold).
- The character medallions in the battle window remain because it is designed to frame the portrait.
