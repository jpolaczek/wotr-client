<a id="konsola-tury-okno-polowania-i-pierścienie-elfów--ui-158ui-164"></a>
# turn console, Hunt window and Elf Rings — UI-158-UI-164

The user application from 2026-10-14: "Let's plan interfaces for the hunt, the use of rings and the most important thing let's set the final design and purpose of the wide bar at the bottom, where information about who is turn is now shown".

The decisions of the user from this conversation:
1. The lower bar is **turn**: a fixed place for "who turn and what to do now", the Hunt field, the
   Elf Rings of both sides and Pass and the rejection of dice. The big decisions remain in the
   windows at the objects.
2. **Hunt in the window like the battle** window: the illustrated window opens itself to both
   players for Hunt time, with the step bar. The dice to Hunt assignment is done in the console.
3. **Ring → die → score**: Click the dice highlight ring to change, click dice opens the selection
   of the allowed results over it, click the result changes die immediately, without Confirm.
4. Hunt tab in the top right corner of the map disappears; the corner tab stays for Fellowship.

5. The choice that has the object (talia, card, region, figure, panel) occurs with it; only choices
   without the object open one decision window with constant width over the console. The console
   never grows (section E).
   **Since UI-210 (user decision, 2026-10-16), card decisions never open the decision window:**
   their objects are highlighted, object-less options and ✓ Confirm choice sit in the console.
   See [CARD_CHOICES_UX](CARD_CHOICES_UX.md).

<a id="zasady-układu-decyzja-użytkownika-z-2026-10-14"></a>
### Layout rules (user decision with 2026-10-14)

> everything we want to design so that 1) the sizes are as constant as possible, we do not want this
> bar at the bottom to change the height of 2) we want as little empty space as possible 3) we want
> everything to take place, we do not want to put randomly buttons in places that do not fit due to
> one of the 1000 mechanics

- **Persistent sizes**: the bottom bar and its zones have a fixed height and width; the text is in
  fixed lines (short with full text in the tooltip), it does not enlarge the bar.
- **A small empty slot**: each zone has content in every state of the game; we do not reserve spaces
  that usually stand empty.
- **The fixed place of each control**: the control is always in the same place and when not
  available, it is decorated (like “No card”, UI-151) and not disappears. The control of one
  mechanic belongs to the panel or window of the object concerned; we do not stick it to the common
  bar.

Scope: MVP = base game (decision with 2026-10-14 repeated at UI-160 “For now, we are doing everything with the basic version of the game in mind, no expansions , expansions for now are postponed). Design, code and testing acceptance only base game . Elements expansions (Council chips, Narya, rulers' chips, Jerkír/Lore costs) remain without a new project, and their tests do not enter the regression of these tasks. No server changes, Contracts and rules: all data and legality come from `PlayerView` ( `Hunt` , `HuntReport` , `HuntPool` , `Rings` , `Dice` , `Decision` , `Options` ).

<a id="stan-przed-zmianą"></a>
## Before change

- Lower row: dice panel (212 px), "Decision dock" (`decision-dock`: `ComponentActionView` with ⁇
  Pass and ⁇ Discard die and `DecisionTray` hand. The tray is host to the current decision control
  and moves to the window it has; then the wide bar is almost empty with the inscription "Your
  action" or "Waiting for ...".
- Hunt: a compact panel in the top right corner tab (UI-98, UI-109, UI-141), full only during
  allocation, projection, tile and response.
- Rings: buttons in the dice panel; click on the draft `UseRing` panel, and the tray shows the
  general buttons “O” and walls.

<a id="a-konsola-tury"></a>
<a id="a-tura-console"></a>
## A. Turn console

```
┌ dice panel ┐┌ status ─────────────────────────┬ resources ───┬ shares ┐┌ hand ┐
│ FREE PEOPLES││ ◉ YOUR ACTION · Army die        │ ◉ 0  ⇢ 0  │ [↶][✓]││      │
│ [C][A][M][W]││ Click a highlighted region …    │   Hunt    │ [⇥][✕]││      │
│ ✕2 ◉0 ⊠0    ││                                  │ ○  ○  ●   │       ││      │
└─────────────┘└──────────────────────────────────┴───────────┴───────┘└──────┘
```

The console replaces the present dock and has a **fixed height** of the entire lower row ( 140 pt; panel dice 212 × 132 pt in all layouts, two rows dice and meters; expansion of the panel on short screen removed), illustrated frame `console/frame` and three zones: status (remain width), resource column board ( 108 p. p.: inner Hunt over the three Ring sockets) and action ( 80 pt, grid 2 × 2 Four systems ( 1366×768 , 1920×1080 × 100/125% ) without scrulla and cut; targets ≥ 32 pt; 1366×768 and 125% status is approx. 270 The refresher changes the content of the zone, never its size.

<a id="strefa-statusu-nagłówek-i-podpowiedź-do-dwóch-linii"></a>
### Status Zone (header and hint to two lines)

- `status-plaque` badge in enamel running side with a emblem and one sentence: who works and what
  now (YOUR ACTION · Army die · 1 of 2 moves left", "Waiting for Shadow · battle at Minas Tirith",
  "Fellowship phase · Declare, change the guide or enter Mordor", "Shadow allocates Hunt dice",
  "Game over · ...").
- Second line: a hint of the current step (texts of today's tray, such as `army-context-hint`,
  Muster, card, ring, “The response was interrupted...”).
- When a decision has a window (battle, Hunt, Fellowship, decision window...), the status tells
  where the decision is and the status click opens this window.
- Texts build a presentation class without UI (`TurnConsoleState`).
- UI-244 (2026-10-17): every own decision has one object host (`DecisionRoutes`, the families of the server's base
  coverage matrix): the map, the region panel, Reserve, Effects, the dice, the Rings, the hand, the Battle or Hunt
  window, the Fellowship panel, the Characters list or the console's options. The status reopens that host — since
  UI-244 also the Fellowship panel and the table effects — and a choice no object owns yet keeps the decision
  window as its fallback.
- UI-254 (2026-10-17, owner's request): the plaque opens the host of the decision and a second click closes it again
  (Reinforcements, the decision window, the battle window, the Hunt panel, the Fellowship panel, the table effects,
  the end of the game, or the window the decision notice opens), so a decision's window can be put away and brought
  back from the console; its tooltip says "Click to open or close …". Excess units outside a battle — and a card's
  figure choice made in the region panel — now have a host: the plaque (and the decision notice) focus the map on the
  region of the first offered figure (`MapCanvas.Focus`) and open its region panel as a decision panel
  (`RegionHover.OpenDecision`) that stays while the decision waits. A decision the game waits on (`RequiredDecisions`:
  excess units, battle and event losses, Hunt damage, excess hand cards) also turns the decision notice at the top into
  its red required variant (`DecisionNotice.Required`). No new window is made for any of them. Tests:
  `RequiredDecisionTests`, `BoardInteractionTests.RequiredExcessOpensItsRegionPanelFromThePlaqueAndTheNotice`.
- UI-242 (2026-10-17): hints and notices never say "components"; they name what to click for the step — dice,
  armies and regions, units, characters, cards, a nation, regions — from the step's command and presentation
  (`DecisionObjects`), e.g. "Select the highlighted armies and regions, then confirm." A refused drop names the object
  ("That die cannot be used here.").
- UI-241 (2026-10-17): while any decision of this player waits (an action, a reaction, a Hunt step, a Guide, a
  battle choice), the status plaque wears its gold halo on its own, the halo pulses in the shared 750 ms rhythm under
  `.motion-enabled` and the headline turns gold (`ConsoleStatus.SetMustAct`, `TurnSignal.MustAct`); the opponent's
  turn and observers keep it still. The untouched starting decision keeps its UI-167 halo and ribbon. Choosing the next
  Guide shows no captions ("Choose the next Guide…", "Choose a Guide from the highlighted portraits"): the glowing
  portraits (UI-229) and the console's hint carry the step.
- UI-238 (2026-10-17): choosing the die an Elven Ring changes is its own, clearly marked moment. The console's
  headline reads "ELVEN RING · CHOOSE THE DIE TO CHANGE" (then "… CHOOSE THE NEW RESULT"), the dice panel's band
  replaces the side name with a gold "CHOOSE A DIE", the dice the Ring can change wear a thick gold rim on a warm plate
  that pulses in the shared 750 ms rhythm under `.motion-enabled`, and the other dice fade to 28 %
  (`RingChoice.Headline`, `RingInteraction.ChoosingClass`/`PulseClass`).
- UI-239 (2026-10-17): bringing a character into play opens no window of choiceless buttons. The tray shows character
  buttons only while none is chosen, Change only with several characters, and cost dice only when they differ
  (`CharacterChoices.DiceChoice`); one die, or identical dice, are taken for the player (`CharacterDraft.UniformDie`).
  A chosen character that needs only a region leaves the tray without choices, so the window stays closed and the
  console reads "Gandalf the White · Choose a highlighted region on the map" (`CharacterChoices.RegionHint`).
- UI-237 (2026-10-17): action dice are chosen by clicks only. Pressing a die and moving the pointer starts no drag,
  shows no ghost and drops nothing (`DraftInput`, as for armies and figures since UI-105); the Hunt allocation, costs
  and every other die choice work by click. Rings, ruler tokens and cards keep their drops.
- UI-230 (2026-10-17): the resource inspector (a window with Spent, Hunt, Discarded, Fellowship, Removed and Tokens
  tabs opened from the dice panel's location counts) is removed. The counts are information only (read-only, icon,
  number and tooltip). A die outside the pool that the player's decision may choose (e.g. a Balrog or Nenya die in the
  Hunt box for an expansion effect) stands lit in the pool's row for that decision (`DiceOutsidePool`).
- UI-227 (2026-10-17): a step being sent never opens the decision window. A die discarded or a Pass from the console
  is sent at once, and for that moment the tray still held the step's die buttons, which flashed the window (titled
  with the turn's action) above the console. A window already open keeps its place until the answer.
- QUAL-19 (2026-10-17): problems and notices the player must read (a refused choice such as "Server update
  required", offline, refreshing the saved result, an unavailable preview, a refused command, a local save error) reach
  the hint through `DecisionTray.Notice`, also while the tray sits in the closed decision window. Step hints and
  preview summaries stay in the tray, so a step's own hint is not replaced by them.
- UI-214: a card's object-less options and the object-less responses of a reaction outside the Hunt and
  battles replace the hint as up to three buttons (`console-options`, 38 pt like the hint), so the console keeps
  its height. A click that completes the decision sends it. Their model is `CardConsoleOptions`
  ([CARD_CHOICES_UX](CARD_CHOICES_UX.md)).
- From UI-167 the badge is lit (`your-turn`) until the player starts to work, and the ribbon "YOUR
  ACTION" means the pass of action; from UI-169 the status "GAME OVER" opens the game end screen
  ([SIGNALS_UX](SIGNALS_UX.md)).

<a id="polowanie-w-kolumnie-zasobów"></a>
### Hunt in the resource column

- Inner `hunt-box` with a plate Hunt moved from the corner of the map: Eyes ( dice Shadow In Hunt )
  and dice Free Peoples used in motion Fellowship , both meters always visible (also zero). Click
  opens the panel Hunt with a pool list ( UI-141 ) and report; the plaque is the object of dropping
  dice at the assignment. The console does not have a separate pool button – the list belongs to the
  panel (from UI-162 to the window) Hunt .
- At the assignment of Shadow the recess is lit (`hunt-box-eligible`, section C).

<a id="pierścienie-w-kolumnie-zasobów-trzy-gniazda"></a>
### Rings in the resource column (three sockets)

- The three sockets of `ring-socket` are three Elf Rings of base game; each shows where its ring is:
  with Free Peoples (`free`, `icons/ring`), taken over by Shadow (`shadow`, `icons/ring-captured`)
  or removed from the game (empty, extinguished socket). The number of sockets does not change.
- The ring that the player can now use is active (hover, pressed, `selected` after the weapon);
  others are information with tooltip and the reason for the server. The meaning of the click is due
  to the current step (`ExpansionActions.RingKind`): change of dice (`UseRing`), Vilya at the
  stopped dice or cost of the selected card (Jerkír, Lore). The ring is therefore always chosen in
  its nest, also as a cost of card.
- From UI-175 (user decision with 2026-10-15) the socket has no graphics `ring-socket` or border:
  you can only see round ring graphics (32 pt in place 34 pt). State shows itself:
  - Rest: slightly extinguished;
  - legal use and hover: full brightness;
  - armed ring (`selected`): full brightness and magnification of 1,15;
  - push: minus 0,9;
  - removed from the game: gray graphics at 35% opacity; place stays.

<a id="strefa-akcji-siatka-2--2"></a>
### Zone action (network 2 × 2)

- Always four buttons: ⁇ undo and ✓ confirm at the top, ⁇ Pass and ⁇ reject selected die
  Inaccessible is greyed with a reason in the tooltip, never disappears.
- ✓ and ⁇ apply to the current draft, whose elections are on board (map, panels, hand), e.g.
  completion of march or recruitment earlier. Since UI-210 this includes every card decision:
  ✓ is "Confirm choice" and ↶ undoes its last step ([CARD_CHOICES_UX](CARD_CHOICES_UX.md)). Decision in window (battle,  Hunt, decision window)
  has approval in the window footer at its election; then ✓ and ⁇ consoles are greyed out. With the
  server's reply interrupted ✓ acts as ‘Check result’.
- Other action components (form capabilities, expansions chips) belong to the panels of their
  objects (Characters, Dice & tokens), not to the console.

<a id="zasady"></a>
### Rules

- Hunt tab in the corner of the map disappears (field, pool and report are in the console);
  Fellowship plaque stays.
- The decision note in the upper left corner becomes a shorthand navigation.

<a id="wdrożenie-ui-160"></a>
### implementation UI-160

- Code: Status text builds `Presentation/TurnConsoleState` (without UI), zones are in `UI/Console/`
  (`TurnConsole`, `ConsoleStatus`, `ConsoleHunt`, `ConsoleRings`, `ConsoleActions`), decision window
  in `UI/Windows/DecisionWindow`, and `Application/TurnConsoleInteraction` powers the console from
  the projection and draft and directs it action to the existing tray paths. Styles:
  `Styles/Console.uss`.
- The progress of action (`ActionProgress`) entered the status header. The pass and discard of dice
  are no longer components on board, only fixed console buttons. The rings have disappeared from the
  dice panel. The opening button of battle from the dock has replaced the status. Private setting of
  cards battle (the "Skip cards this battle") is in the window header of battle, in the size of its
  controls (22, the constant width), and the status of the setting is shortened by a multidot.
- Status click selects one goal in order: Meals (recruitment on board), decision window (the tray on
  the dock has elections), battle window, Hunt panel (own decision Hunt or Hunt opponent), note your
  decision. Tooltip status calls the same target. battle and Hunt open without camera movement.
- The decision window shows the “Check result” after the broken response when the tray is in the
  dock; ✓ the console performs the same check.
- Recruitment from card or ability (the meal plan) has choices and own ✓ in the Meals panel, so the
  decision window is then closed and does not block the panel; the status click opens meals.
- Games with Lords or Kings (beyond MVP) retain the height of the dice panel; its reserves (tokens
  of rulers and council, jackpots dice special) stand in the second column next to dice, so the
  panel has then 432 entitled width, and the console status zone narrows accordingly. base game
  always has 212 entitled.
- Transitional status to UI-162: the Hunt panel continues to expand in the upper right corner,
  opening with a button in the console. The extended Hunt response panel passes from the bottom row
  so that the console does not block its choices.

<a id="poprawka-ui-171"></a>
### Amendment UI-171

The game application on two clients (2026-10-14): at the player who just got the action, the line under the status remained at "The opposing is deciding...", although the header already said "YOUR ACTION".

- Cause: tray reports hint (`HintChanged`) before `TurnConsoleInteraction.Refresh`. The line was
  therefore given a hint of the previous condition, and `Refresh` only supplemented an empty line.
- Now the console remembers the last hint of the tray. The line shows it when the step has it, and
  otherwise the current hint of the console.
- This happens with every refresh and every change of hint.

<a id="sygnał-tury--ui-167"></a>
### turn signal — UI-167 signal

When action passes to the player, the ribbon passes over the map in his hand enamel, and the status badge lights up gold (`your-turn`) as long as his decision to start action (action, Fellowship phase, the Hunt allocation) has an empty draft and nothing awaits the server. First step of draft (die, selection) or sending command lights up. At the reduced motion instead of the flash ribbon once (700 ms). Design and rules: [SIGNALS_UX](SIGNALS_UX.md), Section B and "implementation UI-167 ZXX".

<a id="b-okno-polowania"></a>
## B. Window Hunt

```
╔═ HUNT · Fellowship in Moria · 3 dice ═══════════[?][×]╗
║ Roll › Reroll › Tile › Damage › Result                ║
║  [6][4][2]  hits 1 (+1 moved)                        ║
║  ┌────┐  Tile: 2 · Reveal                            ║
║  │ 2 Ⓡ│  Damage 2 → choose:                         ║
║  └────┘  [Corruption 4→6] [Guide: Gandalf] [Random]  ║
║  Status / Skip / Reroll selected · Keep roll         ║
╚══════════════════════════════════════════════════════╝
```

- Opens itself to both players at the new Hunt (`HuntView.SourceId` different from the previous) as
  the battle window with UI-149; closes to the next Hunt; returns from the console (status, last
  Hunt) and due to the decision. The camera does not move.
- Header: cause (movement Fellowship, card, Mordor), region Fellowship, number dice. Stepbar with
  `HuntView.Stage`; in Mordor without throw: Tile › Damage › Step.
- Main object of the stage: dice with hits, motion bonus and metastatic sources (Clip failed dice,
  one "Reroll selected / Keep roll", large tile (`HuntTileFace`) with Reveal and Stop, choice of
  injuries as illustrated buttons with preview effect (Corruption 4→6, portrait of Guide, covered
  pile of random Companion), cards reactions and capabilities with cost and Skip. One click
  completes the selection (UI-142).
- Reveal: the window shows the context; the route and location remain on the map and in Fellowship
  (UI-52).
- The result stays in the window until closing; log and card action unchanged.
- The interior is today's parts of `UI/Hunt/*` moved from `HuntPanel`; the window uses
  `FloatingWindow` and the graphics of the battle window.
- Secretity: the opponent sees public facts (dice, tile, effects), without private choices and
  undisclosed versions of the character (H02).

<a id="c-przydział-polowania-w-konsoli"></a>
## C. Allocation of Hunt in console

- Decision `AllocateHunt`  (Shadow):  dice with options they shine in the panel dice; click dice
  She's moving her to the field. Hunt, click in the field withdraws it; drop dice in the field as an
  abbreviation. min/max from the server. From UI-172 the allocation approves one click of the plaque
  Hunt, also at legal zeros (previously ✓ "Allocate" console).
- Free Peoples sees the allocation only after approval (the server projection).

<a id="wdrożenie-ui-162"></a>
### implementation UI-162

- The Hunt window is an old `HuntPanel` window as a floating window (`FloatingWindow` frame and
  window header battle, `Styles/HuntWindow.uss`). It stands next to the Fellowship region as a
  battle window next to its region and does not move the camera. The panel at the corner of the map
  disappeared (`BoardEdgeLayout` docks only Fellowship) and with it the dead layout of the
  Fellowship–Hunt steam system (`FellowshipHuntLayout`, `PairedPanelPlacement`).
- `HuntWindowState` (presentation without UI ) gives the key Hunt ( `HuntReport.Id` and
  `StartedRevision` ) and stages of the belt. New Hunt opens the window at both players and the
  window closed by the player is closed to the next Hunt , also with a new decision in the same Hunt
  Result Hunt , for which the window opened, stays closed (report and last state dice or a tile),
  and the bar shows “Result”. At rest, the window shows the field Hunt ( UI-109 ).
- The Stage Bar (`HuntStages`) uses the battle window tags: Roll › Re-roll › Tile › Damage › Result,
  in Mordor Tile › Damage › Step (`battle/stage-roll|reroll` and `hunt/stage-*` icons with UI-159).
- Assignment belongs to the console. Window Hunt it does not host it (`HuntActions.UsesPanel` only
  applicable to the ongoing Hunt), and the tray stays in the dock with a hint. Click dice in panel
  dice adds or returns die and drop dice on the tablet Hunt it adds. The tablet shows “Allocate
  n/max In. ” UI-162 her click returned the last die, and the allocation sent ✓ console; from UI-172
  it is approved by a sign (lower). Free Peoples They don't see the assignment until after approval.
- On the low screen (`short-viewport`, e.g.1366×768 at 125%) the window has a compact variant (lower
  rows of tiles, dice and answers, targets still ≥ 32 pt), so each stage, also "Check result", ends
  above the console.

<a id="wdrożenie-ui-172"></a>
### implementation UI-172

The user request from 2026-10-14: "confirm the allocation of dice to the hunt we should use this rectangular tile with icons of the eye and steps". According to the principle of the system, the allocation control belongs to the Hunt object, not to the common bar.

- With its own assignment Shadow the Hunt sign is its approval. The click sends one allowance with a
  throw when:
  - draft is located in min/max;
  - server preview is legal (`DecisionTray.Legal`);
  - Nothing waits for an answer.

  Ready sign lights up (`hunt-confirm-ready`) and in tooltipie says
  „Allocate & roll · n dice · Click the plaque to allocate and roll”.
- The non-ready plaque stands at the same place, decorated (`hunt-confirm-waiting`). Its tooltip
  tells how much dice is missing. The click does not send anything and does not open the Hunt window
  at the time of the assignment.
- The ready count is `TurnConsoleInteraction.RefreshAllocation`, also after the preview response
  (`LegalChanged`). The table (`HuntPanel`) reports the `AllocationConfirmed` event.
- The second quick click is omitted (UI-142), because after sending the sign is no longer ready.
- ✓ console stays for completion action multi-stage and "Check result". Allocation does not approve.
- die returns to the pool with the second click dice. The shortcut “the table click returns the last
  die” from UI-162 disappeared.

<a id="panel-kości-przy-wyborze-kości--ui-183-2026-10-15"></a>
### Panel dice when selecting dice — UI-183 (2026-10-15)

User application: “When selecting dice for something, e.g. allocate hunt phase, let's highlight these dice or frame with them, so that it is known that this functionality is where it takes place”.

- The dice own panel gets the `dice-decision` class (`DiceDecision.Active`): a gold frame and light
  background lighting. Padding narrows down by the frame thickness, so no die moves. This happens
  when a player's decision takes dice from this panel:
  - the allocation of Hunt;
  - armed Elf Ring before the choice of dice;
  - card waiting for die to pay for it.
- At the assignment of dice, which Hunt can take (`hunt-candidate`), they have a green border of a
  legal target. dice already assigned (`hunt-allocated`) have a gold border instead of the current
  extinguishing.
- The enemy can't see the backlight.

<a id="d-pierścienie-elfów"></a>
## D. Elves rings

Source: BASE p. 21, LOGIC B05. Free Peoples use the Ring and give it back to Shadow; Shadow uses the Overtaken and removes it from the game. At the highest one in turn per player. The change occurs before normal action, which can use another dice; Free Peoples do not obtain Will of the West; Shadow can change die into an Eye that goes to Hunt.

- Custom ring click: dice with `UseRing.EligibleDice` candle (`DieOptionGlow`), the remaining
  extinguished; status suggests "Click a die to change it".
- The dice click opens a selection of walls with `option.Faces` (`DieFaceIcon` icons, current score
  disabled). The wall click sends `UseRing` right away (without Confirm).
- After confirmation: die shows a new score; Free Peoples ring passes to Shadow page, the seized
  Shadow ring disappears; Shadow eye passes to Hunt field. Reduced motion without animation.
- Esc, re-click the ring or click on the map cancels without command. UI does not force the use of
  the modified dice.
- Ring costs in card expansions (Jerkír, Lore) remain in the tray.

<a id="wdrożenie-ui-161"></a>
### implementation UI-161

- `RingChoice` (presentation without UI) reads the gesture step (`None`, `Die`, `Face`), dice with
  `UseRing.EligibleDice`, the `option.Faces` walls with the current and hint for the status of the
  console disabled.
- `RingInteraction` means dice with `ring-candidate` (gold border) and `ring-muted` (dysed), shows
  `RingFacePicker` over the selected dice and cancels the gesture at the click in the map. Reading
  other panels (e.g. "Why unavailable?") does not interrupt the gesture. The order can be the
  opposite: the previously selected die goes straight to the walls.
- `RingFacePicker` is a `console/picker` slop with a `picker-tail` tail and walls on the medallions
  of the page (34 pt). It stands over the whole panel of dice, in the column of the selected dice,
  so it does not cover either dice or the panel header with the help links, and it adheres to the
  limits of board.
- The Core Ring no longer opens the panel “Dice & tokens” ( `ResourcePresentation.OwnsDecision` The
  tray only shows a hint, so the decision window does not open. draft The gesture is recorded
  locally so far, and after returning the smoky returns over die.
- The socket whose ring changes the owner once pulses (`ring-transferred`); the reduced motion
  leaves them motionless.

<a id="e-okno-decyzji-i-wybory-przy-obiektach"></a>
## E. Decision and Object Elections Window

> Since UI-210 (2026-10-16) card decisions do not use this window: see
> [CARD_CHOICES_UX](CARD_CHOICES_UX.md), which replaces the card rows of the inventory below.

`DecisionTray` becomes the sole host of the decision control, but does not live in the console. Its default host is **decision window**: a fixed width window ( 560 pt) and fixed location, anchored above the console at the panel dice and growing up (highest to the upper edge of the stage), in the window frame battle The window shows only when the tray in the dock has an election (planlist, answers, effects choices, “Check result”). A step whose choice is on the map or in the object panel hides the window, and the next step with the choices of the same decision shows them again. × closes them to the next decision or step. The console status click and decision note opens them back. Objects windows ( battle , Hunt , Fellowship , Characters, Meals, Politics, Resources expansions They're taking over the tray so far.

The choice that has the object is moved to the object (UI-164). The inventory from 2026-10-14:

| Decision or control | Destination | Task |
| --- | --- | --- |
| Army movement and attack, targets, „Leave out …” | Map and panel of the region; status hint | UI-160 (status) |
| Muster and recruitment; Reinforcements button in tray | Meal Panel; Reopen from console status (UI-317: only while the Muster die can recruit) | UI-164 |
| Politics, activation | Panel Politics | unchanged |
| Characters: input, abilities | Panel Characters | unchanged |
| Fellowship: movement, cover, declaration, Mordor, separation, route of disclosure | Panel Fellowship and map | unchanged |
| Play card, cost dice, cancel card selection | Hand and Preview card; cancels the ⁇ console | UI-164 (cancellation) |
| Select card dice Event (Talley selection) | Talia, by the hand | UI-164 |
| Over-hand, card recoil as cost | Hand | unchanged |
| The Elves Ring | Console Rings Zone | UI-161 |
| Allocation Hunt | Hunt Zone | UI-162 |
| Throw, reroll, tile, injury, Guide, reactions in Hunt | Hunting Window | UI-162 |
| card combat, throws, losses, continuation, retreat, trespass, siege | Battle Window | unchanged |
| card reaction (Use/Skip, Decline) outside battle and Hunt | Preview card reaction; without card — decision window | UI-164 |
| Effect card with the choice of region, figures , reserves, nation, Companion , cards table | Map, Region panel, Meals, Politics, Fellowship/Characters, Effects | UI-164 |
| Plan cards Army (a few armies, rescue), loss of events | Decision window (Plan list) and map (Objects) | UI-160 |
| Other effects selection without object | Decision Window | UI-160 |
| Pass, dice rejection | action Zone | UI-160 |
| Broken Response (Check result) | ✓ console; at the tray in the dock also the decision window | UI-160 |

The decisions of expansions (Council, Kings, Lords) remain at their panels (Dice & tokens, sovereigns, preparation) without a new project.
UI-264 (2026-10-19) gives each of them a host in `DecisionRoutes.Expansion`: the special-die pick and the Ruler discard
are made on the lit dice in the pool, with the console's hint saying what the click does ("Click a highlighted die to
keep it …") and no panel, window or Back; Vilya's keep, Dismay and the Council token stay with Dice & tokens until
UI-268, UI-276 and UI-270; Companion versions and homelands stay with the setup panel (UI-269, UI-270); a corruption
tile and a card's choice of a sovereign belong to the sovereigns (UI-274, UI-278). Fate's Monsters branch and the
Kings cards' two-way branches are console buttons like the base cards' modes (`CardConsoleOptions`).

<a id="wdrożenie-ui-164"></a>
### implementation UI-164

- The tray itself says whether it has window choices (`DecisionTray.HasChoices`): it is a visible
  button other than ⁇ (`decision-back`) and the Food flag. The selection of the region on the map,
  dice in the panel dice or decks at the hand leaves the decision window closed, and the hint and ⁇
  are in the console. The choice with its own approval, reaction or "Check result" continues to open
  the window.
- The reaction with card in your hand has the “Use” button in the preview of this card
  (`CardInspector.Reaction`, `CardPlayButtons.Reaction`): one click selects the reaction and sends
  it immediately. The response list and Skip remain in the decision window because Skip has no
  object.
- Inventory items without change in code:
  - decks on the dice Event selection were already working at hand (`Decks`);
  - canceling selected card: playing closes its preview (UI-147), so the × role takes over the
    constant ⁇ consoles (UI-160);
  - Meals return from the console (status), and the flag in the tray is no longer visible, because
    the meal plan leaves the decision window closed (UI-160).
- The decision window leaves the election without a separate object: general election figures ,
  Companion and cards table, the king's destination, figure Army from
  several regions, Captain's order, reactions and Skip. (UI-289, 2026-10-19: the figure table
  `EffectFigurePicker`, the king's destination and the Army figures of several regions left the
  tray; they are chosen in the Battle window, the region panel and on the map.)


<a id="scenariusze-i-właściciele"></a>
## Screenplays and owners

| ID | Owner | Scenario and expected result |
| --- | --- | --- |
| TC01 | UI-159 | Console frame graphics, Hunt with Eye, rings of both sides (ordinary, seized, extinguished), selection of walls, frames and steps of the Hunt window in 1× and 2×; contract `UiArtKitTests`. |
| TC02 | UI-160 | Status for both sides in the Fellowship phases, allocation, action (die and budget), waiting for the opponent and decision in the window; click status opens the decision window. |
| TC03 | UI-160 | The Hunt field in the console on both sides, the pool list as a pushover, the last Hunt opens the report; Hunt plates in the corner are not there. |
| TC04 | UI-160 | Four fixed action buttons (scared when unavailable), three Ring sockets with the state of each ring, a fixed console height in each state of the game; four layouts of both sides without scroll, targets ≥ 32 entitled |
| TC06 | UI-160 | Decision window: fixed width, auto-open on your own decision without another host, return from console and attention; object windows take over tray so far. |
| OB01 | UI-164 | Selections for objects according to inventory from E section: deck, cancellation of card in preview, Reinformments from left strip, cards reactions in preview, cards effects on map and panels; decision window for selection only without object. |
| ER01 | UI-161 | Ring Click → dice shine → click dice → selection of walls with options → click the walls send `UseRing` without Confirm. |
| ER02 | UI-161 | The Free Peoples ring goes to Shadow; the seized Shadow ring disappears; the Shadow eye goes to the Hunt field; the limit of turn and the reason in the tooltip. |
| ER03 | UI-161 | Canceling Esc/click outside; no forcing use of the modified dice; the opponent can only see the result. |
| HW01 | UI-162 | Auto-open at both players at the new Hunt, close to the next, return from console and attention; no camera movement. |
| HW02 | UI-162 | Assignment in the console (click, withdraw, drop, min/max, zero) and view of Free Peoples after approval. |
| HW03 | UI-162 | Throw, bonus, metastatic selected or Keep roll, tile with Reveal/Stop, injury (Corruption, Guide, random Companion), reactions and Skip; one click. |
| HW04 | UI-162 | Mordor without throw (Tile › Damage › Step), both finals; Reveal with a route on the map. |
| HW05 | UI-162 | Secretity of both views, playback after reconnection, report of the last Hunt. |
| TC05 | UI-163 | acceptance: the runs of both sides, the dumps, the regression, the `board` profile, the builds. |

<a id="weryfikacja-i-przekazanie"></a>
## Verification and transmission

- EditMode: `TurnConsoleStateTests`, `RingFlowTests`, `HuntWindowStateTests`, `UiArtKitTests`, full
  EditMode.
- PlayMode with server: `TurnConsoleLayoutTests` ( 4 systems × 2 pages), `RingUseNetworkTests` (
  `fp-elven-ring` , `sh-elven-ring` ), `HuntWindowNetworkTests` ( `fp-character-move-fellowship` ,
  `sh-eye-hunt-allocation` , `fp-mordor-track` Regression `Hunt*` , `Fellowship*` , `Mordor*` ,
  `Battle*` , `BoardInteraction` , `DicePanelLayout` , `EdgeBoardLayout` , `CompactBoardLayout` ,
  `Victory*` .
- 1366/1920 × 100/125% dumpsOn both sides; `profile-client.py --suite board`; `BuildLocalMac`,
  `BuildLocalWindows`; `check_docs.py`.
- Manually: test scenarios mode on the local server (`fp-character-move-fellowship`,
  `sh-eye-hunt-allocation`, `fp-elven-ring`, `sh-elven-ring`, `fp-mordor-track`).

Results, commands and commits go to the to-do documents, this document, TESTING and the register. Push only on request.

## Fellowship guidance — UI-249 (2026-10-17)

In the Fellowship phase the console reads "Guide: click a glowing companion" (no Change guide button). For There Is
Another Way it names the Fellowship's controls first: "Move: click the glowing track" or "Hide: click the glowing
Ring", then "or Stay to finish after healing"; Stay is the console's only button for the card.

## Payment dice light the pool — UI-248 (2026-10-17)

While a payment has alternative dice, the dice pool's frame lights (`DiceDecision`) and the alternatives glow in the
shared rhythm; the console and the decision window do not repeat them.

## Lords special dice in the pool — UI-267 (2026-10-19)

A Lords special die (Vilya, Nenya, Narya, the Balrog, Gothmog) is a round die in the art of its origin
(`graphicalassets/ui/src/dice/special.svg`: a rune-studded band and enamel in the origin's colours — Vilya sapphire,
Nenya mithril, Narya ruby, the Balrog's ember, Gothmog's Morgul green) with the face glyph, the origin emblem at its
lower right (`dice/origins`: three rings with their stones, a flame, the moon of Minas Morgul) and, for a result
printed with the Remove star, the star at its upper right (`dice/remove-star`). The same dice stand at the top bar's
size among the opponent's dice. Ruler dice keep their tile until UI-273.

The special-die choice (`SpecialDieChoice`, LORDS pp. 9–13, rules T24) is made in the own pool, as UI-264 routed it:

1. The side that rolled more dice chooses first; on a tie the Free Peoples do. The console's headline reads
   "Special dice" (the hint below explains the click) and the decision notice wears the required red variant.
2. Each die the choice can keep shows its lit art; its tooltip says what the click keeps and does ("Click to keep
   Vilya · Eye. This Eye goes to the Hunt Box and strengthens the Shadow's Hunt.") and, for a Remove star, when the
   kept die leaves the game (at the next Recover if Gandalf the White, or for the Shadow the Witch-king, is then in
   play). One click keeps it; there is no Confirm.
3. An Eye among the dice forces the choice of an Eye. A die it rules out is disabled and says why; when only one die
   is legal, it glows alone and the hint reads "An Eye must be kept: click a highlighted Eye" — since UI-279 the
   player clicks it (nothing is sent without a click).
4. The dice not kept stay in their slot, dimmed (`set-aside`), until the next roll, in the own pool and in the
   opponent's top bar; their tooltip says they were not kept and that an unkept Remove star removes nothing. They are
   not counted among the dice out of play.

Tests: `SpecialDiceTests` (origins, set-aside dice, forced Eye, tooltips); `SpecialDiceNetworkTests` (a tie lets the
Free Peoples keep first, lit dice survive a reconnect, the set-aside dice in both views, the forced Eye waiting for its
click, layouts at 1366/1920 and 100%/125%, no generic window); `ResourcePanelNetworkTests` and
`ExpansionDecisionNetworkTests` (both sides with two Eyes each, with the Ruler dice). Scenarios: seeder `ui267-tie`,
`ui267-forced-eye`; test menu `lords-forced-eye`.

## Named Elven Rings — UI-268 (2026-10-19)

A Lords game replaces the three Elven Rings with Vilya, Nenya and Narya (LORDS pp. 15, 19–20; rules T24). They stand in
fixed console sockets in that order, each in its own art (`graphicalassets/ui/src/console/ring-<name>.svg`): Vilya a
gold band with a great sapphire between two wings of air, Nenya a mithril band with a white adamant glinting like a
star, Narya a red-gold band with a ruby crowned by flame. The captured art wears an ember halo; the lit art the gold
aura of a Keeper ability offered now (`keeper-ready`). A base game keeps its plain rings.

1. Every named ring still changes a die like any Elven Ring: click the socket, then a die and its new face (UI-161).
   The tooltip names the ring, its owner and its Keeper ability.
2. Nenya: in the Hunt's tile step, when Galadriel can replace a drawn Eye tile, the Nenya socket glows; its click
   removes the tile from the game and draws another, and Nenya passes to the Shadow. The Hunt window lists no Nenya
   button; a line in it points at the ring, and the console's Pass declines (UI-291).
3. Vilya: after a paid action Elrond can keep the action's die. The paid die glows in the pool and the Vilya socket
   glows; a click on either keeps the die (one paid die makes the socket's click complete) and Vilya passes to the
   Shadow. "Spend the die" is the console's button (`action-DeclineVilya`). No panel or window opens.
4. Narya: Gandalf's activation stays on the nation's row of the Politics panel (`nation-activate-<nation>`); the
   Narya socket's tooltip says so.

Tests: `NamedRingsTests` (fixed sockets and art, the Nenya and Vilya Keeper steps, the console's Spend the die);
`ExpansionWindowNetworkTests.NenyaTransfersOnlyItsOwnRingAndResumesTheHunt` and
`ExpansionDecisionNetworkTests.VilyaKeepsTheResolvingDieAvailable` (socket glow, one click, reconnect, layouts, no
window); `ExpansionDecisionNetworkTests.NaryaActivatesItsNationWithTheChosenDie` (the nation row).

## Ruler dice and the steps before the first action — UI-273 (2026-10-19)

- **The Ruler die** wears a gilt crown on its top edge (`dice/ruler-crown`), in the own pool and the opponent's row.
- **The Ruler discard** after the roll is made in the pool for each side that rolled its Ruler die (Shadow first): the
  eligible standard and Ruler dice glow and their tooltip starts "Click to discard this die · It returns at the next
  Recover"; a Lords special die is never eligible. One click discards; since UI-279 a single eligible die also waits
  for its click ("Click your only eligible die to discard it"). No panel or window opens.
- **A discarded die** keeps its slot in a Kings game, dimmed and disabled, for both players, with "Discarded · Returns
  at the next Recover"; it leaves the Out count (`RulerDice.Discarded`, `ActionPoolView.DiscardedClass`).
- **Lords with Kings**: the console's hint numbers the step before the first action — "Step 1 of 4" special dice,
  "Step 2 of 4" Ruler discards, "Step 3 of 4" Dismay, "Step 4 of 4" The Lore of the Elven Rings (`PreActionSteps`); a
  step whose objects speak for themselves shows its name. The tray's hint is built by `DecisionTray.StatusHint`.

Tests: `RulerDiceTests`, `UiArtKitTests`, `ResourcePanelNetworkTests.SpecialChoicesAndBothDiscards…` (both sides,
candidates, crown, dimmed slot, step hints, four layouts).
## Pass also declines a reaction — UI-291 (2026-10-20)

The owner's decision: the console's Pass also declines the player's own reaction window — a Guide's or character's
skill (Gandalf the Grey's draw, the White Rider, Merry's or Pippin's separation, Gollum's reveal), a table card's or a
hand card's response, the Ents' free Event. It lights with the shared ChoiceGlow whenever a click can use it (a legal
Pass or a decline), keeps its size, and its tooltip names what it skips ("Pass · Skip Gandalf the Grey · Draw matching
card"; several responses are listed) (`ReactionSkip`). The separate wide "Decline" console option and the tray's
"Decline"/"Skip" buttons are gone, so the console keeps its hint ("… · or Pass to skip it."), and three object-less
responses now fit the console. The opponent's window never enables it. Tests: `CardConsoleOptionsTests`,
`ReactionSourcesTests`, `HuntLayoutTests`, `HuntAftermathNetworkTests`, `BoardCompletionHostTests`,
`CardPack04NetworkTests`.

Expansions (owner, EXP-15, 2026-10-26; not yet implemented): D7 extends this to every optional decline of the
expansions — Vilya's "Spend the die", Lore's "Keep results", Wind's and the Balrog card's "Finish", Orderly
Withdrawal's "Keep the hit", We Won't Go Back's "No Corruption", "Keep card effects" (Brand, the Chieftains) and
Dismay's "Keep tokens" become the console's Pass, its tooltip naming what it skips (UI-276, UI-277, UI-347, UI-348). D8
keeps forced single steps (one legal region or plan) resolved by the server without a click, the result named in the
effect trail and the log. Checks: [EXPANSIONS_DECISIONS_PLAYTEST](EXPANSIONS_DECISIONS_PLAYTEST.md).

## The status plaque lights only when its click opens something — UI-292 (2026-10-20)

The owner's decision: the plaque ("YOUR ACTION", "YOUR DECISION · …") no longer glows, pulses or presses when its click
would open nothing — a plain action whose choices are the dice and the map, for example. It lights and keeps its "Click
to …" tooltip only when the click opens the decision's window, battle, Hunt, Fellowship or Effects panel, Reinforcements,
the end of the game, or the notice's region, sovereign or hand (`DecisionNotice.Opens`, `DecisionHostNavigation.Hosted`);
otherwise it is read-only (`ReadOnlyControl`). The headline and the arrival flash still announce the turn. Test:
`TurnRibbonTests` (the action's plaque stays unlit and read-only).

UI-291 follow-up (2026-10-20): a reaction the console hosts no longer shows the tray's generic "Select the highlighted
choices, then confirm."; the console's hint says where the response is and that Pass skips it ("Click the Guide's
glowing portrait in the Fellowship panel to use Draw matching card · or Pass to skip it.", otherwise "Use a glowing
response, or Pass to skip …"). Test: `MultiUseDiceNetworkTests.OneDieClickExposesArmiesRecruitmentAndPolitics`
(Gandalf the Grey's draw after an Event die: no Decline anywhere, Pass lit and named, the Fellowship tab lit).

<a id="ui-297"></a>
## The played card is recalled only in the console — UI-297 (2026-10-22)

The owner's decision: the chip that recalls a played card or effect ("✓ Book of Mazarbul", a sovereign's choice, the
figures a card committed) appears only in the console, left of the hint (`ConsoleStatus.Trail`, `console-trail`), and a
second click on it closes the card it opened (`TurnConsoleInteraction.ToggleCard`; in a battle the card opens in its
combat form). This is the default for every card and effect. No window, panel or tray repeats the chips
(`DecisionTray` renders no `committed-choice`), so a decision whose only content was the trail no longer opens the
decision window. The strip is rebuilt only when the trail changes, holds at most 45 % of the line, and its chips are
shortened with an ellipsis (the tooltip keeps the full text). The tray's ⟵ (`decision-back`) is gone from reactions,
where it did nothing; it stays where it steps back (an action's draft, an ArmyEvent, a die-and-result or region
choice). Tests: `GuideDrawNetworkTests` (no ⟵ and no tray chip in Gandalf's draw; the console chip opens Book of Mazarbul
and its second click closes it), `EffectTrailTests`, `NazgulFlightDraftTests`, `CardConsoleOptionsTests`,
`CardNetworkTests`, `CardInteractionTests`, `ShadowStrategy03LayoutTests`, `KingsCards01NetworkTests`,
`KingsCards04NetworkTests`, `LordsFinalCardsNetworkTests`.

<a id="ui-298"></a>
## Back and ✓ for the Fellowship panel — UI-298 (2026-10-22)

The console's Back, otherwise enabled only while the tray waits on the board, is also enabled while the Fellowship panel
holds a move, hide or entry left after a failed send, which the panel no longer offers an arrow for. While the panel
holds an interrupted response, the console's ✓ glows and is its only Check result. Details:
[FELLOWSHIP_UX](FELLOWSHIP_UX.md#ui-298).

<a id="blokada-odtwarzania--ui-304"></a>
## The playback gate — UI-304 (2026-10-23)

While an opponent presentation plays or waits for its history
([OPPONENT_ACTIONS_UX §7.1](OPPONENT_ACTIONS_UX.md#koordynator)), `TurnInteraction.Hold` keeps the console and the
decision tray in their fixed places but disabled: Pass, Back, ✓, the plaques, console options, rings and the battle
policy start nothing, and Enter does not confirm. The tray and console keep the presented view's state, and the
decision of the newest view appears once, at the release. A command already sent keeps recovering its receipt. The gate
is dormant until UI-305 registers a presenter, so the console behaves exactly as before.

<a id="ui-312"></a>
## The opponent's Rings and dice in a presentation — UI-312 (2026-10-24)

While the opponent's action plays ([OPPONENT_ACTIONS_UX §6.16](OPPONENT_ACTIONS_UX.md#wdrozone-312)):

- **A Ring's cost** is ringed at its socket in the resource column. The socket's own pulse shows the new holder at the
  release, once.
- **The opponent's dice.** A changed die is ringed in the top bar's pool with its old → new face, and an allocation's
  Eyes ring the Hunt plaque in the console well.
- **The console's status** keeps the presented view's state, and the newest decision appears once, at the release
  (UI-304).
- **Nothing to click.** The marks take no pointer, and no console control is added.
