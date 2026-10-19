<a id="object-controls-audit-expansions"></a>
# Object controls audit, expansions part — 2026-10-19

The expansion half of the 2026-10-19 audit of text buttons that duplicate existing objects (the owner: "there are
still fragments of the design that use unnecessary buttons instead of the existing infrastructure"). The base-game half
and the fixes for it are prepared on `main` (`docs/ui/OBJECT_CONTROLS_AUDIT.md` there, UI-280); this branch keeps only
what concerns Lords, Kings and Fate, to be checked with the expansions (owner, 2026-10-19: "expansions completely
separate on their own branch, checked separately later"). Read-only audit of client `40f84df7`; paths are relative to
`unity/Wotr.Client/Assets/Wotr/Runtime`. Nothing below is fixed.

| # | Where | Now | Existing object | Module |
| --- | --- | --- | --- | --- |
| 1 | Council Travel token (`ExpansionChoices.cs:27-31, 98-110`) through `FellowshipChoices` | text token buttons repeating the clicked pool token, text figure buttons ("N" for Nazgûl, names for Minions), Stay all / Stay here, a group label and a text confirm; reached after the Dice & tokens panel's text `component-council.*` button | map figures and destinations (`TravelActions`, `TravelContextInteraction`) | Lords (Council) |
| 2 | Kings Nation step (`KingsNationStep`, Wisdom → "Activate and advance") | five nation text buttons in the generic DecisionWindow; no test covers it | Politics track nation tokens (`NationTrackRow`, `CardContextInteraction`, today only for `PlayEvent` targets) | Kings |
| 3 | Palantír of Anárion "Skip extra card" (`KingsAnarion`) | text button in the generic window: "Deck" is missing from `CardConsoleOptions.Modes` | a console option (CARD_CHOICES rule 4) | Kings |
| 4 | Brand's combat choice (`CombatReplacement`) | "Use Brand" / "Keep card effects" text buttons in the battle window | Brand's portrait in the force column (as `BattleReactions` portraits) | Kings |
| 5 | Welcome removal (`DecisionTray.cs:552-562`; `CostDice.Removal` leaves DiscardWelcome out) | die text buttons | dice in the pool | Kings |
| 6 | Dismay (`ExpansionChoices.cs:36-37, 89-96`) | "Dismay · X" and die text buttons; Ruler/Dismay pool tokens are text rectangles "♛ Thr" (`ActionPoolView.cs:210`) | the pool's tokens and Ruler dice (pending UI-276) | Kings |
| 7 | Abilities as text buttons (`ComponentContextInteraction.cs:37-45`): Narya (`ComponentActions.cs:82-87`, although `NamedRings.cs:10` puts it on the Politics row), Far-sighted (`ComponentActions.cs:66-74` and the "Standard"/"Far-sighted" toggle in `FellowshipChoices.cs:45-57`) | text buttons in the region panel, the Characters inspector and the decision dock | the Politics row's `Activation`, Thranduil's portrait, the inspector's ability chips (UI-271 did this for Balrog and Gothmog only) | Lords, Kings |
| 8 | Lords Hunt table-card responses (`Presentation/HuntCardResponses.cs:48`) | the response row admits only `lords.*` ids; when the base fix for table-card reactions on `main` merges here, both must share one row | `HuntCardResponses` + `HuntContextInteraction.CardResponse` | Lords |
| 9 | Alternate Guides in the Hunt (`HuntGuideResponses.Alternate`, `:27`) | admits only `council.*` and `fate.gimli-guide`; the base Guides' fix on `main` must join this row on merge | `HuntResponsesView.GuideButton` | Lords, Fate |
| 10 | Orderly Withdrawal (`CombatEffect`) | "Cancel hit · withdraw" / "Keep the hit" text buttons | the hit pips (`BattleChoicePip`, UI-246) | Lords |
| 11 | Council entry cost "No token" / token names (`CharacterChoices.cs:90-105`) | text buttons duplicating the pool's Council medallions | the medallions | Lords (Council) |
| 12 | Balrog order (`BalrogEventStep`) | "Move Balrog alone" / "Move or attack with army" console buttons | the figure and its army on the map (`CardArmyInteraction`, UI-216) | Lords |
| 13 | Latent text fallbacks: `CorruptionAttemptChoices`, `CorruptionInspectionChoices` (`SovereignPresentation` owns these first, `DecisionTray.cs:387`) | nearly dead code | — | Kings |

Exceptions recorded only by agents, for the owner to confirm or drop when the expansions are checked: Barad-dûr's
buttons until UI-278; Dismay and Council in Dice & tokens until UI-276; "Keep with the Fellowship" (UI-270); Lure,
Captain and Balrog alternatives (UI-271); the Fate and Kings card branches as console buttons (UI-264/266).
Owner-approved: the kings' attempt buttons beside their portraits (UI-274).
