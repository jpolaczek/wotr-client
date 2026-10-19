# Card choices in the fixed UI — UI-210

User request, 2026-10-16 (translated): "We are changing the UI of cards like Nazgûl Search.
When choices have to be made about what goes where, and when cards have less obvious rules of
this kind, everything should be handled in the space at the bottom, with a Confirm choice
option there to end the turn. If there are many figures to choose from, one click on the
figure and then distribute them on the board for as long as the rules require. The figure is
highlighted to show that it can be chosen. The same rules apply to the UI of other cards: use
the existing UI we designed, minimize additional windows that are not part of the fixed UI,
and minimize clicks." Follow-up: Nazgûl that are already on the board are highlighted on the
map as movable (Nazgûl Search does not take them from the reserve); figures recruited by a
card are highlighted in the Reserve panel and its tab.

This document supersedes, for card effects, rule 5 and sections A ("Action zone") and E of
[TURN_CONSOLE_UX](TURN_CONSOLE_UX.md): a card decision no longer opens the floating decision
window. MVP scope: base-game cards. Expansion decisions stay on their own panels.

## Rules

1. **Objects first.** A choice that has an object is made on it: figures on the map, the
   Reserve panel, the region panel, dice, decks, the hand, the Fellowship panel. The object is
   highlighted while it can be chosen, and so is the edge tab that opens its panel.
2. **One selection, many placements.** One click on a highlighted figure (a stack on the map or
   a token in the Reserve) arms it. Every click on a legal region then moves or places one
   figure, and the selection stays armed while the rules allow more of it. Arming another
   figure replaces the selection; a click on the armed figure releases it.
3. **The console finishes the decision.** The turn console (its fixed size does not change)
   carries the card's title, the progress and the remaining limits in its status zone, and the
   card decision's controls in its action zone:
   - ✓ **Confirm choice** sends the draft when it is legal (also with nothing chosen when the
     card allows "any"); disabled with the reason in its tooltip otherwise;
   - ↶ undoes the last placement, move or selection; with nothing left to undo it cancels the
     draft as before.
   An object panel's own ✓ (the Reserve panel) stays and does the same.
4. **Choices without an object sit in the console.** A card's modes and yes/no options
   (e.g. "Recruit 2 Nazgûl" / "Move Nazgûl", "Discard a card" / "Hunt roll" / "Finish",
   "Decline") replace the hint lines of the status zone as up to three compact buttons in one
   row. A click that completes the decision sends it at once (UI-142).
5. **No floating decision window for base-game card decisions.** Battles, the Hunt, the
   Fellowship panel and Politics keep hosting the decisions that belong to them (they are part
   of the fixed UI). A list that cannot fit in the console is the only exception and has to be
   named in the inventory below.
6. **Fewest clicks.** Nothing is sent without a click, even a single legal option (UI-279, owner
   decision 2026-10-19); the single option glows and says what its click does. A click that
   completes a choice sends it. Counting what still has to be chosen stays visible in the
   console, so the player never needs to open a panel to see progress.

## Inventory of base-game card decisions

Today's behaviour was inventoried on 2026-10-16 (server steps in `Wotr.Domain/Cards`,
client branches in `DecisionTray`, `CardChoices`, `ReinforcementPlans`). "Window" means the
floating decision window (UI-160).

| Group | Cards | Today | Target | Task |
| --- | --- | --- | --- | --- |
| Map click only (`Region`) | Return to Valinor, Spirit of Mordor, Faramir's Rangers, Círdan's Ships, Riders of Théoden, Éomer, Half-orcs, Olog-hai, Rage of the Dunlendings (region), Eagles are Coming!, Cruel Weather | Map; hint in the console | Unchanged; card title and progress in the console | UI-211 |
| Same-region alternatives | Dreadful Spells (field army or garrison in one region) | Two buttons in the window | Two buttons in the console | UI-214 |
| Board components | Mirror of Galadriel (die), Palantír of Orthanc (deck) | Die in the pool, deck by the hand | Unchanged | — |
| Several dice and Confirm | The Lidless Eye (0–3 dice) | Dice in the pool; a window holding only Confirm | Dice in the pool; ✓ in the console | UI-214 |
| Nazgûl flights (`CharacterEvent`) | Nazgûl Search, The Nazgûl Strike!, The Ringwraiths Are Abroad, The Black Captain Commands (fly) | Portrait buttons, groups and "Finish movement" in the window; destinations on the map | Movable Nazgûl highlighted on the map; arm a stack once and distribute by destination clicks; ✓ in the console | UI-212 |
| Card recruitment (`RecruitmentPlan`, `ReserveFigures`) | A New Power is Rising, Shadows on the Misty Mountains, Horde from the East, Many Kings, Pits of Mordor, Círdan's Ships, Swords in Eriador, Dead Men (recruits), national recruit cards when both Regular and Elite are available | Reserve panel; the armed token is released after each placement | Reserve tab and eligible tokens highlighted; the armed token stays armed while it can still be placed; ✓ in the console | UI-213 |
| Card modes and options | The Black Captain Commands (order), The Nazgûl Strike! (discard, roll or finish), Lure of the Ring (Free Peoples' choice), There is Another Way | Option buttons in the window | Option buttons in the console; The Nazgûl Strike!'s table cards are discarded with their × in the lit Effects panel ("Discard a table card" opens it) | UI-214 |
| Reactions outside the Hunt and battles | The Ents Awake follow-up (Decline), Gandalf the Grey's draw, Balrog of Moria at a declaration, a reveal outside the Hunt | Response list and Decline in the window | Use in the card preview (UI-164) of the lit hand card; Decline and object-less responses in the console. Exception: more than three console buttons keep the window | UI-214 |
| Figures of one army (`ArmyFigure`, `ArmyFigures`) | Denethor's Folly, Stormcrow (Free Peoples' loss), The Grey Company, Hill-Trolls, Rage of the Dunlendings (units to move) | Region click, then figure buttons and Confirm in the window | Eligible regions highlighted; the region panel lists the army with eligible figures highlighted; figure clicks; ✓ in the console | UI-215 |
| Companions with a destination | Dead Men of Dunharrow | Companion buttons and Confirm in the window; destination on the map | Companions highlighted on the map and in the region panel; destination on the map; ✓ in the console | UI-215 |
| Excess figures after a card | Stack limit after card recruitment (`RemoveExcess`) | Figure list and Confirm in the window | Region panel with the excess figures highlighted; ✓ in the console | UI-215 |
| Card army events (`ArmyEvent`) | Help Unlooked For, Paths of the Woses, Through a Day and a Night, Shadows Gather, The Shadow Lengthens, The Shadow is Moving, Corsairs of Umbar, The Ringwraiths Are Abroad and The Black Captain Commands (manoeuvre), Grond, The Fighting Uruk-hai | Plan list, routes and Confirm in the window | Armies highlighted on the map; army moves and attacks with the map and region panel as with an Army die (UI-105); "Skip manoeuvre" and progress in the console; ✓ in the console | UI-216 |
| Fellowship, Hunt, battle and Politics hosts | Gwaihir, We Prove the Swifter, Book of Mazarbul, Fear! Fire! Foes!, I Will Go Alone, There and Back Again; Breaking of the Fellowship, Worn with Sorrow and Toil, Hunt tile cards and table-card reactions; event losses and combat cards; Wisdom of Elrond, Stormcrow (nation) | Their panels and windows | Unchanged | — |

## Interaction details

### Nazgûl flights (UI-212)

- Every Nazgûl that may fly (`ActionOption.Travel`) is highlighted where it stands on the map:
  the Witch-king's figure, a Nazgûl in an army marker, a lone Nazgûl. The region panel of such
  a region marks them as movable.
- A click on a highlighted region (or its Nazgûl in the region panel) arms the Nazgûl there.
  Each click on a lit destination moves one armed Nazgûl there (the Witch-king first only when
  he is clicked on his own). The source stays armed while it still has an unmoved Nazgûl; a
  click on another highlighted source switches.
- Moved Nazgûl show as ghosts at their destinations, the console shows "Nazgûl moved n" and ↶
  undoes the last move. ✓ Confirm choice sends all moves (none is allowed: "any or all").
- The payload stays `CompanionMoves`: one group per (origin, destination); no server change.

### Card recruitment (UI-213)

- The Reserve tab and every token the card may still place are highlighted; the panel opens
  by itself as today.
- A click on a token arms it; every click on a legal region places one figure of it there and
  the token stays armed while the card's limits allow another (per region, per nation and in
  total). A click on another token switches; a click on the armed token releases it.
- Region first stays possible: with a region chosen, a token click fills that region.
- The console shows "Recruited n/m" and the remaining limits; ↶ removes the last placement;
  ✓ Confirm choice (or the panel's ✓) sends the plan.

### Console host (UI-211, UI-214)

- `TurnConsoleInteraction` enables ✓ for a card draft whose choices are on objects or in the
  console when the tray reports a legal preview, and routes it to the same confirm as the
  tray. ↶ undoes the draft's last step (`DraftHistory`) or cancels an empty draft.
- The status zone shows the card's title (headline) and the draft's progress (hint). For card
  options it shows up to three option buttons in place of the hint lines; the console keeps
  its height.
- `DecisionWindow` stays closed for every card decision of the inventory; a test pins this.

## Implementation

- **UI-211.** `CardDecisions` (presentation) owns EffectChoice, CharacterEvent, ArmyEvent and RemoveExcess
  decisions of the player outside a battle, the Hunt and event losses. The console's ✓ is "Confirm choice"
  for them when the tray's preview is legal (tooltip: the tray's confirm text, or the preview's problem)
  and sends through the tray's confirm. The tray's own Confirm no longer counts as a choice of the decision
  window, so a card decision whose choices are all on the board (The Lidless Eye) keeps the window closed.
  The headline names the card (`YOUR DECISION · Nazgûl Search`) and the hint leads with the progress
  ("Nazgûl moved 2", "Recruited 1 of 3", "Chosen 1 of 3"). ↶ is the draft's undo (`TurnDraft.Back`).
- **UI-212.** `NazgulFlightDraft` (presentation) keeps the armed source in `TurnDraft.ArmedSource` (a region or
  one figure; local, saved with the draft, restored by ↶ and after a reconnect, never sent). A region arms its
  unmoved Nazgûl in ordinal order and the Witch-king only when he is the region's last unmoved Nazgûl; a click on
  his figure in the region panel arms him alone. A flight joins the group of the same origin, garrison and
  destination (`CompanionMoves`, route `[origin, destination]`); an armed figure, or a region with nothing left
  to fly, is released. `NazgulFlightInteraction` (application) lights the sources (`eligible-army`), marks the
  armed marker (`armed-force`, the selected plinth), lights the movable figures in the region panel and draws
  the flown Nazgûl as ghosts (`MapCanvas.PlannedFlights`, without ×). A map click releases the armed region,
  flies to a lit destination or arms a lit source (and opens its panel); any other region opens its panel.
  `DecisionTray` builds no flight choices and hints in the console. In a card decision, the committed steps of
  the effect trail (`committed-choice`) do not open the window either; outside card decisions they still do,
  because the battle window's host depends on it (ShadowStrategy02 Corsairs).
- **UI-213.** `EventRecruitDraft.Drop` keeps an armed token armed with its group's next free figure while the
  card's limits leave it a region (`Next`), and releases it otherwise. With a token armed, a click on another
  token switches it instead of filling the last region; region first works as before when nothing is armed.
  Choosable Reserve tokens carry `eligible-token` (tinted slot, full glow); the tab keeps `legal-target`. ↶
  restores the last placement's armed token. Details: [POLITICS_REINFORCEMENTS_UX](POLITICS_REINFORCEMENTS_UX.md).
- **UI-214.** `CardConsoleOptions` (presentation) lists the console's buttons (`ConsoleOption`): the choices of
  CaptainOrder, LureCompanion (named after its Companion), FellowshipAction (There is Another Way) and NazgulStrike,
  a region's alternatives once the region is chosen (Dreadful Spells), and a reaction's responses whose source is
  not a card in the hand, plus Decline, outside the Hunt, battles and event losses. Buttons keep the tray's names
  (`effect-choice-<id>`, `action-DeclineReaction`). The Nazgûl Strike!'s table cards are offered in the Effects
  panel (`TableEffectsView.Offer`): the tab and the cards are lit, their × discards and sends; the console's
  "Discard a table card" opens the panel at the first offered card. A reaction card in the hand is lit
  ("Click, then Use its reaction") and answers with its preview's Use. `ConsoleStatus.SetOptions` replaces the hint
  with up to three buttons in its place and rebuilds them only when the set changes; `ConsoleOptionsInteraction`
  sends a completing click through `TurnInteraction` (CompleteChoice, or DeclineReaction). The tray builds none of
  these choices, and the committed steps of the effect trail do not count as window choices for them either
  (the Ents Awake follow-up), so the window stays closed. A reaction with more than three console buttons keeps the window
  (none in the base game is known). The Lidless Eye kept its dice and the console's ✓ from UI-211.
- **UI-215.** `CardFigureDraft` (presentation) owns a card's figure choice on the board — ArmyFigure (Denethor's
  Folly, Stormcrow), ArmyFigures (The Grey Company, Hill-Trolls, Rage of the Dunlendings), the Dead Men's companions
  (CompanionDestination) — and every RemoveExcess outside a battle (after a card, a Muster or a move; the same
  decision keeps one interface). `CardFigureInteraction` (application) lights the regions holding eligible figures
  (`eligible-army`) and, in the region panel, the figures a click may toggle (`eligible-figure`, chosen ones
  `selected`). A click on a lit region opens its panel; a figure click toggles it with the card's limits
  (`EffectChoiceDraft.Choose`, or the excess count). A choice of exactly one figure is sent with its click; otherwise
  the console counts "Chosen n of m" and its ✓ confirms. The Dead Men's destination stays a map click. The tray
  builds no figure buttons, so the window stays closed; excess after a battle stays in the battle window.
- **UI-254.** A click on a lit region, the console's plaque or the decision notice opens the region panel as a
  decision panel (`RegionHover.OpenDecision`): it keeps no UI-189 leave delay, survives presses elsewhere, the wheel
  and focus loss, follows its region when the camera moves, and closes with Esc, the plaque, a click on a region, or
  when its decision is resolved (`RegionHover.Apply`, a new decision id). The plaque and the notice focus the map on
  the region of the first offered figure (`DecisionAttention` → `Region`). Excess units are a required decision: the
  decision notice wears its red variant until they are returned.
- **UI-216.** `CardArmyEvent` (presentation) owns the player's ArmyEvent decision with a MoveArmies or AttackArmy
  draft. `CardArmyInteraction` (application) lights the event's armies on the map and, in the region panel, the
  planned army's figures (eligible, and selected while they move). A click on a lit army that is not a destination
  starts or restarts its move through `BoardDraft.SelectRegion` and opens its panel; destinations, routes and attack
  targets keep their map clicks; a click on another lit army plans the next one (no "Add army" or plan tabs).
  Figure clicks toggle through `EventArmyDraft.Toggle` (route events) or `ArmyDraft.Toggle`. The event's other
  options are console buttons (`ConsoleOptionKind.Action`, named `action-<Kind>`, e.g. Skip maneuver); the console
  shows "Armies moved n of m" and its ✓ sends. The tray builds no plan list, route label, figure grid or alternatives.
- **UI-164 (aggregate).** CC09 for both sides: while the other side decides outside a battle and the Hunt, the
  committed steps of the effect trail do not count as window choices either, so the waiting side gets no window that
  only recalls a card (a Nazgûl Search's player after the flights). The recorded Shadow04 layout failure was the Combat
  card's hover preview (UI-137), which overlays the map beside its tile by design; the tray's label check now exempts it.

## Scenarios

| ID | Scenario | Owner |
| --- | --- | --- |
| CC01 | Nazgûl Search with Nazgûl in three regions: one click per source, one per destination, ✓ in the console; no window | UI-212 |
| CC02 | Nazgûl Search confirmed with no move | UI-212 |
| CC03 | Shadows on the Misty Mountains: Nazgûl token armed once, 2 units and the Nazgûl placed by region clicks; Reserve tab and tokens lit; ✓ in the console | UI-213 |
| CC04 | Many Kings: three settlements filled with one armed token; ↶ removes the last placement | UI-213 |
| CC05 | The Black Captain Commands: Recruit/Move in the console; Move continues as CC01 | UI-214 |
| CC06 | The Nazgûl Strike! options in the console after the flights; a table card discarded with its × in the Effects panel | UI-214 |
| CC07 | Hill-Trolls: two regulars chosen in the region panel; ✓ in the console | UI-215 |
| CC08 | Shadows Gather: armies moved on the map; progress and ✓ in the console; "Skip manoeuvre" in the console | UI-216 |
| CC09 | No base-game card decision opens the decision window (both sides) | UI-164 |

## Tasks

UI-211 console host · UI-212 Nazgûl flights · UI-213 card recruitment · UI-214 card options
and reactions in the console · UI-215 figure choices in the region panel · UI-216 card army
events on the map · UI-164 acceptance of the whole (aggregate).


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

## Fellowship cards on the Fellowship's objects — UI-249 (2026-10-17)

The Breaking of the Fellowship's Companions are chosen on the Hunt panel's roster portraits, and There Is Another
Way's Move and Hide on the Fellowship's track and Ring (`FellowshipActions.RosterChoice`, `FellowshipCardActions`);
the choice list and the console no longer repeat them. Stay, Keep-style declines and Lure of the Ring's alternatives
stay buttons. See FELLOWSHIP_UX, UI-249.

## Reactions at their sources — UI-247 (2026-10-17)

A reaction is used where its source is: a card in the hand at that card (UI-164), a table card during the Hunt in
the Hunt panel's response area (UI-214), a battle character on its portrait in the Battle window, Gandalf the Grey's
draw on the Guide's portrait in the Fellowship panel, and a table card's own reaction outside a battle or the Hunt —
The Balrog of Moria — at that card's × in the Effects panel (the reaction discards the card). The console keeps
"Use a table card" (it opens the Effects panel) and Decline; it no longer lists a reaction that has a source object.
Worn with Sorrow and Toil keeps its three explicit alternatives. Tests: `ReactionSourcesTests`,
`CharacterNetworkTests`, `Shadow03NetworkTests`.

## Payments in the dice pool, on the Ring and in the hand — UI-248 (2026-10-17)

A payment by one of several different action dice is made in the own dice pool (`CostDice`): a character's entry
or ability with different dice, and the removal of Palantír of Orthanc, a protection card (A Power Too Great, Tom
Bombadil) or Denethor's Folly, started at the table card's × (UI-221). The alternative dice glow and pulse in the
pool, the chosen one is selected, the pool's frame lights, and a click on another die switches the payment; the
decision window lists no dice. Palantír of Orthanc takes its Elven Ring on the Ring in the console: a click on the
chosen Ring takes it back without choosing another payment, and choosing a Will of the West die (which pays alone)
drops a Ring chosen for another die. Hand discards keep their red marking (UI-222). An incomplete payment spends
nothing; a complete one keeps its single commit. The expansions' Palantír of Barad-dûr, Lore, Council tokens and
special dice keep their buttons. Test: `CostDiceTests`; `EffectsNetworkTests` (Palantír on the Ring).

## A region's two forces on their sections — UI-251 (2026-10-17)

When a card's region holds two eligible forces — Dreadful Spells' field army and garrison — each force's section in
the region panel glows and its header chooses it (`RegionForceChoices`); the console keeps the same two named
alternatives, so the region alone never collapses them. A region with one eligible force is chosen by the region
click. Test: `RegionForceChoicesTests`.

UI-246 revision (2026-10-17): Leadership points are taken on their source figure or portrait (a badge counts them) and
Heroic Death's amounts are full-size "−1/−2/−3" buttons; see BATTLE_WINDOW_UX.
