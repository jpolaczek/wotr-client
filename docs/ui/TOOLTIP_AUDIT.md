# Tooltip audit — UI-420 (2026-10-29)

Owner request (2026-10-29): every tooltip of the base game and the expansions is cleanly formatted, does not repeat
what its label or another of its parts already says, and every action's tooltip says plainly what it confirms or what it
passes. This document records the rules the audit applied and what it changed. The audit read all 275 tooltip
assignments of the client (`Runtime/`), split into action tooltips (✓, Pass, Skip, Finish, die and card uses, battle and
Hunt steps) and informational tooltips (map, characters, Effects, Politics, dice pool, battle statistics, rooms).

## Rules

1. **An action names its object.** A tooltip of something that commits, confirms, passes or skips says what it does with
   which card, effect, figure, die or region:
   - the console's ✓: `Confirm · <card or ability>: <chosen options>` (`ConfirmSummary`), or the server preview's summary
     with the die in front (`Confirm · Use your Muster die: Recruit Rohan Elite → Edoras.`);
   - an army move or a recruitment names routes and figures: `Move Rohan → Fangorn (3 figures)`,
     `Recruit Rohan Elite → Edoras, 2 Gondor Regular → Minas Tirith`, `Advance the Dwarves one step on the Political Track`;
     a choice with nothing named reads `Confirm the chosen options`;
   - Pass: `Pass · Take no action now; the opponent acts next`; an optional decline names the card or ability it skips
     (`Pass · Skip Wind from the West · Finish without inspecting the Shadow hand`); a reaction window names each
     response skill first, then its owner (`Pass · Skip Draw matching card (Gandalf the Grey)`); the battle window's Skip
     effect reads the same without Pass (`Skip Orderly Withdrawal · Keep the hit and fight on`);
   - Finish names the action it ends (`Finish · End the action now (Move armies); the steps already made stay`), Discard
     the die;
   - the battle window's direct steps name the region (`Continue battle · Fight another Combat round in Osgiliath`,
     `Retreat inside · Withdraw into the Minas Tirith stronghold`, `Stay · No figure advances into Osgiliath`), and Apply
     losses lists them (`Remove 1 Gondor Regular, downgrade 1 Gondor Elite in Lossarnach (2 hits)`);
   - Keep roll / Reroll selected say which roll and how many dice (`Accept the Hunt roll (1 hit); use none of your 2
     rerolls`, `Roll the 2 selected failed dice again · 2 of 3 rerolls`, `RerollChoiceText`). A summary that also stands
     as the battle footer's status line never repeats its button's label;
   - the Fellowship's hide and move read the same on the track marker, the map figure and the console
     (`Click to hide the Fellowship · No progress; this die does not enter the Hunt`, `Click to move the Fellowship ·
     Progress +1, then the Hunt; its last known position stays`).
2. **No repeated facts.** A tooltip does not repeat its own label as its whole content, nor say one fact twice: a Hunt tile
   reads `Standard tile · 3 Hunt damage`; a corrupted king `Dáin · ◉ Corrupted · Erebor` then what that means; a
   character's Move and Combat level appear only when they differ from its Level; a roster entry reads `Cost: X` without
   repeating the offer; a status tooltip equal to its label is dropped.
3. **Formatting.** A title line, then short parts; ` · ` between parts; no blank lines; no dangling separator when a server
   text is empty (`TooltipText.Parts`, `TooltipText.Lines`); no raw ids or enum names (`Nazgûl`, `Muster die`,
   `Palantír of Anárion`, `allocated by the Shadow`, `Southrons & Easterlings`); `reroll`, never `re-roll`; signed
   modifiers, and only the non-zero ones; `1 card` / `3 cards`, `1 step to war`.
4. **One form per object.** A force reads `Gondor · 3 Regular · 1 Elite · 1 Leader` on the map, in the region window and in
   the battle window; a character reads `Strider · Level 3 · Leadership 1 · North`; a deck `Character deck · 12 cards ·
   Click to draw`; a played card's committed chip `Played card · Click to inspect`; an Initial king leading an army
   `Led by Dáin · ♙ Initial`, as on his portrait.
5. **Hidden information stays hidden.** The audit found no tooltip that shows an opponent's card, a Hunt draw order or a
   Corruption tile value the viewer must not see.

## Where the rules live

- `Presentation/TooltipText.cs` — `Parts`, `Lines`, `Kind`, `Unit`, `Nation`, `NationPhrase`, `Capital`, `Count`;
  `Presentation/RerollChoiceText.cs`; `ArmyMarkerText.Composition` is the one force composition; `ActionCardText.Region`
  names a region with or without a board.
- `Presentation/ConfirmSummary.cs` — the ✓'s object: card choices, army moves, recruits.
- `Presentation/TurnSummary.cs`, `BattleSummary.cs`, `FellowshipSummary.cs` — the server preview's summaries without
  "Ready" fillers.
- `Presentation/OptionalDecline.cs`, `ReactionSkip.cs` — what Pass and Skip effect skip.
- `UI/Battles/BattleChoices.cs` (`DirectTooltip`), `BattleForceText.cs` (one statistic per medallion, `Composition`).
- `Presentation/FellowshipActions.cs` (`HideTooltip`, `MoveTooltip`), `PoliticalActions.cs` (`Distance`,
  `AdvanceTooltip`), `HuntTileText.cs`, `HuntWindowState.cs` (`HuntAllocationText.Confirm`), `ArmyMarkerText.Composition`.
- `Presentation/SovereignPresentation.cs` (`Describe`), `CharacterLibrary.Abilities`, `DieDescriptions.HuntOrigin`.

On the base-game line (`main`) the audit applies wherever the same tooltip exists. The parts that belong to features of
the expansions branch stay there: Pass's optional declines and the battle window's Skip effect (`OptionalDecline`),
Combat card replacements, crowned army leaders, the Lords dice's explained Remove star (this line keeps its
"Remove rune" mark), the Council's travel ✓, Evil Knowledge's portrait and the kings' lit hints. The Sovereign panel's
portrait reads `SovereignPresentation.Describe`.

## Tests

EditMode: `TooltipTextTests` (parts, units, nations, a card choice's ✓, moves and recruits), `BattleArtTests`
(`MedallionsShowOnlyTheirOwnNonZeroModifiers`, `DirectBattleChoicesNameWhatTheyDoAndWhere`), `BattleResolutionTests`,
`HuntPoolListTests`, `HuntGuideResponsesTests`, `HuntReactionPresentationTests`, `MordorTrackStateTests`,
`ArmyMarkerTests`, `PoliticalActionTests`, `SovereignPanelTests`, `ExpansionPresentationTests`, `DieDescriptionsTests`,
`CharacterRosterPanelTests`, `CharacterRowTooltipTests`; PlayMode tests asserting console, battle and Hunt tooltips.
