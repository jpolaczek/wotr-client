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
     the die; UI-430 (2026-10-30): a card's Finish is the console's ✓ and names the card and what it leaves undone
     (`Finish · End The Nazgûl Strike! without discarding a table card or rolling for the Hunt`; Wind from the West's, the
     Pass's example above, is `Finish · End Wind from the West without inspecting the Shadow hand` now);
   - UI-430: a lit object that sends an option names the action, the effect that offers it and what it does: a red table
     card `Discard Wizard's Staff · Click to discard it because of The Nazgûl Strike!: a Nazgûl is in the Fellowship's
     region`, the Hunt Box `Roll for the Hunt · Click to start a Hunt roll now because of The Nazgûl Strike!: … · The
     Fellowship does not move and no die is spent · 1 Eye in the Hunt Box`; UI-431: a force section `3 dice · 5+ · Garrison
     · Click to choose the garrison because of Work of the Enemy · …`, the Reserve's Nazgûl `Recruit · 2 Nazgûl · Click to
     recruit them with the Witch-king because of The Black Captain Commands`, a Nazgûl force `Move Nazgûl · Click to move
     these Nazgûl and the others, the Witch-king among them, because of …`, a red portrait `Eliminate · Click to eliminate
     Gimli because of Lure of the Ring · …`, the corruption marker `Corruption +2 · Click to keep Gimli and take 2
     Corruption because of Lure of the Ring · …`, a Monsters Roused force `East of the mountains · Click to recruit east of
     the mountains because of Monsters Roused · …`, a response's deck `Draw matching card · Click to draw a Strategy card
     because of Gandalf the Grey`, and an army event's ✓ `Finish · End The Black Captain Commands without moving or
     attacking`; UI-432 (2026-10-31): a battle card's decline on the Skip effect and the Pass `Skip Black Breath · Eliminate
     no Leader or Companion` (`Skip Heroic Death · Eliminate no figure and take every hit`, `Skip Andúril · Convert no miss;
     Aragorn still forfeits his Leadership`, `Skip Fateful Strike · Eliminate no Minion`), Worn with Sorrow and Toil's red
     table card `Discard Mithril Coat and Sting · Click to discard it because of Worn with Sorrow and Toil: a Companion was
     taken as a casualty`, its red hand counter `Discard a random card · Click to discard a random card from the Free
     Peoples hand because of Worn with Sorrow and Toil · …`, its Skip effect `Skip Worn with Sorrow and Toil · Discard no
     Free Peoples Character card`, and Scouts' battle region's forces `Withdraw into siege · Click to withdraw into the
     stronghold because of Scouts · End this battle before the Combat roll.`;
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
- `Presentation/OptionalDecline.cs`, `ReactionSkip.cs` — what Pass and Skip effect skip; UI-430: `FinishingOption.cs`
  (what the ✓'s Finish leaves undone), `NazgulStrikeChoice.cs` (the Hunt Box), `TableDiscardText.cs` (red table cards);
  UI-431: `RegionForceChoices.cs`, `BlackCaptainOrder.cs`, `LureChoice.cs`, `MonstersHalves.cs`, `ReactionDecks.cs`;
  UI-432: `OptionalDecline.cs` (the battle declines and Keep cards), `WornDiscardChoice.cs`, `ScoutsShelter.cs`.
- `UI/Battles/BattleChoices.cs` (`DirectTooltip`), `BattleForceText.cs` (one statistic per medallion, `Composition`).
- `Presentation/FellowshipActions.cs` (`HideTooltip`, `MoveTooltip`), `PoliticalActions.cs` (`Distance`,
  `AdvanceTooltip`), `HuntTileText.cs`, `HuntWindowState.cs` (`HuntAllocationText.Confirm`), `ArmyMarkerText.Composition`.
- `Presentation/SovereignPresentation.cs` (`Describe`), `CharacterLibrary.Abilities`, `DieDescriptions.HuntOrigin`.

## Tests

EditMode: `TooltipTextTests` (parts, units, nations, a card choice's ✓, moves and recruits), `BattleArtTests`
(`MedallionsShowOnlyTheirOwnNonZeroModifiers`, `DirectBattleChoicesNameWhatTheyDoAndWhere`), `BattleResolutionTests`,
`HuntPoolListTests`, `HuntGuideResponsesTests`, `HuntReactionPresentationTests`, `MordorTrackStateTests`,
`ArmyMarkerTests`, `PoliticalActionTests`, `SovereignPanelTests`, `ExpansionPresentationTests`, `DieDescriptionsTests`,
`CharacterRosterPanelTests`, `CharacterRowTooltipTests`; PlayMode tests asserting console, battle and Hunt tooltips.
Follow-up (2026-10-30): two PlayMode tests still read the pre-audit wording and now read the audit's —
`DicePanelLayoutTests` expects each dice-pool counter to lead with its location and count (`Spent · 1`,
`In the Hunt Box · none`, `Out · 2`), and `ExpansionAbilityNetworkTests` (Gothmog) expects a reserve token to lead with
the figure as printed (`Sauron Regular · 6 in reserve`, rule 4). Gothmog's second line names the figure again
(`Click to recruit a Sauron Regular in Minas Morgul · Gothmog`) because his token's click uses the ability at once, and
an action names its object (rule 1).
