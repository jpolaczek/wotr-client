<a id="object-controls-audit"></a>
# Object controls audit, base game — 2026-10-19

The owner's report (screenshot, Book of Mazarbul): "there are still fragments of the design that use unnecessary buttons
instead of the existing infrastructure; identify this case and audit again with this in mind", and "the Axe and Bow
card also made me confirm in a strange way". Audit of the base game on `main` (`74335178`) against the standard:
decisions on their objects (map regions and figures, Fellowship/Characters portraits, dice in the pool, cards in the
hand or on the table, the Hunt window's response row, the battle window), no text buttons duplicating an object, the
console's ↶/✓ for confirming (UX_SPEC, TURN_CONSOLE_UX, CARD_CHOICES_UX, BOARD_INTERACTION_UX). The expansions' part is
kept with the expansions on their own branch. Paths are relative to `unity/Wotr.Client/Assets/Wotr/Runtime`. Nothing
below is fixed yet except UI-280 (last section).

## The reported case: companion journeys (blocks the standard)

Book of Mazarbul runs the server's `OutsideCompanionJourneyStep`: a `CompanionSeparation` decision offering
`MoveCompanions` (`FellowshipFlow.BeginJourney`). The same path serves Fear! Fire! Foes!, Gwaihir the Windlord and We
Prove the Swifter. In the client a journey is never stepwise (`TravelActions`), `FellowshipActions.UsesPanel` and
`DecisionRoutes.Of` send it to the Fellowship panel, the tray moves there and `DecisionTray.AddDraftChoices` builds
`FellowshipChoices(contextual: true)`:

- `UI/Fellowship/FellowshipChoices.cs:39` reads `(... && draft.Kind is SelectGuide or SeparateCompanions) ||
  IsGrouped(kind)`, so every grouped journey always gets a row of text buttons with the companions' names (rectangles
  without art), whatever `rosterHasChoices` says;
- "No group" / "Group n / m", "Stay all", "Stay here" and "Choose a group" (`FellowshipChoices.cs:105-153`);
- the tray's own footer, "<" and a text "Move companions", while the console's ↶ and ✓ stay greyed
  (`CardDecisions.Card` leaves CompanionSeparation out).

What already exists for the same choice: `TravelActions.Option` returns the journey option; `TravelContextInteraction`
lights the companions' regions and figures; a figure click goes through `RegionHoverPanel.AddFigure` →
`ActionContextInteraction.SelectFigure` → `TravelDraft.ToggleCompanion`; a region click is the destination
(`TravelDraft.RouteTo`); the origin region glows and its click is "Stay" (`TravelDraft.Stay`); an empty plan is already
a legal ✓ (`FellowshipDraft.Ready`). Precedents: Nazgûl flights (UI-212), Dead Men of Dunharrow (UI-215). Minimal
change: drop `|| IsGrouped` from line 39; no Stay, Stay all, group or summary labels for target-route kinds; add the
journey to `CardDecisions.Card` so the console's ✓ confirms; route a MoveCompanions journey to the map; rewrite
`Strategy01NetworkTests`, `Strategy01LayoutTests` and `DecisionRoutesTests`, which click or pin the tray.

## Axe and Bow (blocks the standard)

`base.fp.character.06` on the table answers the Hunt's `hunt.table` Reaction (`card.axe-and-bow`, cost
DiscardTableCard, "Reduce damage"). The Hunt panel's response row (`HuntResponsesView`) has only the Ring, Guide and
random objects, no table cards, so the reaction falls to `CardChoices.Effect`: a text rectangle "Axe and Bow / Reduce
damage / Reduce Hunt damage by 1 / Discard this card" plus "Skip". Its one click sends (correct, UI-142), but the same
button also carries the card inspector (`UI/Cards/CardChoices.cs:285` → `InspectSource`), which pins the full card
with a disabled Play button and "Select a matching die to play" — it reads as a confirmation still pending. Mithril,
Horn of Gondor, Wizard's Staff and Crebain take the same path. Minimal change: table-card reactions as card objects in
the Hunt response row, with no pinning inspector on Hunt reaction buttons. To check against the rules on the server:
the protection window may open on a tile with no damage (`HuntCardSteps.ShadowHuntDraw`), forcing a pointless Skip.

## Other findings, most severe first

| # | Where | Now | Existing object |
| --- | --- | --- | --- |
| 1 | Guide reactions in the Hunt: Merry, Pippin (`guide-separate-to-reduce-hunt-one`), Gollum's reveal (`hunt.guide`) | text rectangles in the Hunt tray (`FellowshipActions.RosterReaction` leaves the Hunt out) | the Guide's portrait as a response object in `HuntResponsesView` |
| 2 | Fellowship-hosted tray footer: Declare/Reveal ("Undo step", "Cancel", text "Declare"/"Reveal"), separation ("Cancel"), Mordor ("Remove from play") | the footer duplicates the console | the console's ↶/✓ (greyed meanwhile) |
| 3 | Worn with Sorrow and Toil (`CharacterCardDiscard`) | text "Mithril", "Axe and Bow", "Horn", "Staff", "Random hand", "Keep cards" in the Hunt tray; `DecisionRoutes` claims Hand (`:65`) but the hand lights nothing | the Effects panel's table cards with × (`TableEffectsView.Offer`) |
| 4 | Character abilities and components (`Application/ComponentContextInteraction.cs`), e.g. Saruman's Voice | text buttons in the region panel, the Characters inspector and the decision dock | the figure's portrait, the inspector's ability chips |
| 5 | Duplicates: the console's "Discard a table card" / "Use a table card" (`CardConsoleOptions.cs:94, 165`) only open an Effects tab that is already lit; Dreadful Spells' region-panel headers plus console buttons; "Open decision" / "Resume character action" (`UI/Characters/CharacterRosterContext.cs:44-46`) beside the console plaque and the row click; `VictoryPanel`'s "Fellowship" (`:63`) beside the Fellowship tab | two controls for one choice | the object already lit |
| 6 | "Finish phase" hidden when not legal (`UI/Fellowship/FellowshipControls.cs:65`) | breaks disabled-not-hidden | — |
| 7 | Missing graphics: the Effects panel's table cards are title rectangles; the Hunt response objects are glyphs ○ ★ ? | cosmetic | card art, portraits |
| 8 | Latent text fallbacks reachable only if routing slips (`CardChoices.Event` nation and "Use as" rows, `ArmyFigures`/`CompanionDestination` rows, `EffectFigurePicker`, `AttackChoices`, FigureGrid in Army/Excess/EventArmy choices) | dead or nearly dead code | — |

Why the tests did not catch these: `GenericWindowWatch.OnItsObject` (Tests/PlayMode) checks only that the host is not
the generic window, so text buttons in the Hunt panel, the battle window or the Fellowship panel pass. `DecisionRoutes`
does not place the tray either: "CharacterCardDiscard → Hand" has no object behind it.

## Earlier inventories

- The UI-210 inventory (CARD_CHOICES_UX, last row) lists Gwaihir, Swifter, Book of Mazarbul, Fear! Fire! Foes!, I Will
  Go Alone and There and Back Again as "Their panels and windows · Unchanged" while moving Dead Men and the Nazgûl
  flights onto objects; UI-250 removed the group buttons but kept the text roster, "Group n/m" and "Stay all"; the
  server's `docs/ui/interaction-inventory.json` (CompanionJourney) names the target "map groups + shared console" and
  counts it delivered. UI-244 pins CompanionSeparation to the Fellowship panel (`DecisionRoutesTests`).
- UI-244 `base_coverage` puts the Hunt reactions of Axe and Bow, Mithril, Horn, Staff, Crebain, Worn, Merry, Pippin and
  Gollum under "source card, portrait or table card ×", which is not what the Hunt does.
- Owner-approved exceptions, not reported: the Politics advance arrow (2026-10-15), the red decision notice (UI-254),
  edge-tab hover (UI-92/98), object-less card options as console buttons (UI-210 rule 4), UI-142, UI-279, UI-11,
  UI-189.
- Exceptions recorded only by agents, for the owner to confirm or drop: Stay / Stay all (UI-250), There Is Another
  Way's Stay and Lure of the Ring's console alternatives (UI-249), the Reserve panel's own ✓, Keep misses / hits / all
  (UI-245/246), Worn's alternatives (UI-247), the console's table-card opener (UI-214/247), the Dreadful Spells double
  control (UI-251).

## Fixed with this audit

- UI-280 (FELLOWSHIP_UX): the Fellowship's figure on the map hides it on its region as on the Mordor track, and the
  panel's hint names the figure and the Ring instead of the old plaque.

## Owner's decisions (2026-10-19)

Controls deliberately approved stay as they are. Axe and Bow → UI-285 (HUNT_UX); finding 1 (Guide reactions) awaits
the owner's decision; findings 2 and 3 stay; finding 4 → small clickable ability icons that glow only when usable
(UI-286); finding 5 stays for now; finding 6 → UI-287 (FELLOWSHIP_UX); finding 7 → graphics (UI-288); finding 8 →
remove the unreachable fallbacks (UI-289). Companion journeys → portraits (UI-284, FELLOWSHIP_UX).
