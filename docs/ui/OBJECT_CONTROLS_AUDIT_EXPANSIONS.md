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
| 3 | Palantír of Anárion "Skip extra card" (`KingsAnarion`; the console's Pass since UI-348) | text button in the generic window: "Deck" is missing from `CardConsoleOptions.Modes` | a console option (CARD_CHOICES rule 4) | Kings |
| 4 | Brand's combat choice (`CombatReplacement`) | "Use Brand" / "Keep card effects" text buttons in the battle window | Brand's portrait in the force column (as `BattleReactions` portraits) | Kings |
| 5 | Welcome removal (`DecisionTray.cs:552-562`; `CostDice.Removal` leaves DiscardWelcome out) | die text buttons | dice in the pool | Kings |
| 6 | Dismay (`ExpansionChoices.cs:36-37, 89-96`) | "Dismay · X" and die text buttons; Ruler/Dismay pool tokens are text rectangles "♛ Thr" (`ActionPoolView.cs:210`) | the pool's tokens and Ruler dice (pending UI-276) | Kings |
| 7 | Abilities as text buttons (`ComponentContextInteraction.cs:37-45`): Narya (`ComponentActions.cs:82-87`, although `NamedRings.cs:10` puts it on the Politics row), Far-sighted (`ComponentActions.cs:66-74` and the "Standard"/"Far-sighted" toggle in `FellowshipChoices.cs:45-57`) | text buttons in the region panel, the Characters inspector and the decision dock | the Politics row's `Activation`, Thranduil's portrait, the inspector's ability chips (UI-271 did this for Balrog and Gothmog only) | Lords, Kings |
| 8 | Lords Hunt table-card responses (`Presentation/HuntCardResponses.cs:48`) | the response row admits only `lords.*` ids; when the base fix for table-card reactions on `main` merges here, both must share one row | `HuntCardResponses` + `HuntContextInteraction.CardResponse` | Lords |
| 9 | Alternate Guides in the Hunt (`HuntGuideResponses.Alternate`, `:27`) | admits only `council.*` and `fate.gimli-guide`; the base Guides' fix on `main` must join this row on merge | `HuntResponsesView.GuideButton` | Lords, Fate |
| 10 | Orderly Withdrawal (`CombatEffect`) | "Cancel hit · withdraw" / "Keep the hit" text buttons | the hit pips (`BattleChoicePip`, UI-246) | Lords |
| 11 | Council entry cost "No token" / token names (`CharacterChoices.cs:90-105`; removed by UI-347) | text buttons duplicating the pool's Council medallions | the medallions | Lords (Council) |
| 12 | Balrog order (`BalrogEventStep`) | "Move Balrog alone" / "Move or attack with army" console buttons | the figure and its army on the map (`CardArmyInteraction`, UI-216) | Lords |
| 13 | Latent text fallbacks: `CorruptionAttemptChoices`, `CorruptionInspectionChoices` (`SovereignPresentation` owns these first, `DecisionTray.cs:387`) | nearly dead code | — | Kings |

Exceptions recorded only by agents, for the owner to confirm or drop when the expansions are checked: Barad-dûr's
buttons until UI-278 (gone 2026-10-27); Dismay and Council in Dice & tokens until UI-276; "Keep with the Fellowship"
(UI-270); Lure, Captain and Balrog alternatives (UI-271); the Fate card branches as console buttons (UI-264/266; the
Kings branches stand on their objects since UI-278, D15).
Owner-approved: the kings' attempt buttons beside their portraits (UI-274).

Re-checked on 2026-10-26 after the merge of `main` (EXP-14): each row's status, the new findings and the owner's open decisions are in
[EXPANSIONS_UI_AUDIT](EXPANSIONS_UI_AUDIT.md). UI-278 (2026-10-27) fixed rows 2 and 5: the Nation step on the nation
tokens, Welcome's and Barad-dûr's dice in the pool and Barad-dûr's Ring on its socket.

<a id="effects-on-their-objects--ui-367-2026-10-27"></a>
## Effects on their objects — UI-367 (2026-10-27)

Owner request: the Renown pattern (UI-366) applies more widely — a die (or token) that can pay for an effect lights
the tab or panel where the effect's object is, the object glows with a tooltip naming the source and the effect, and
its click uses it; the result plays on the object. Second rule (UI-369): an informational element of something usable
glows, its first click selects it, and only then does Click to use appear. Read-only audit of client `expansions`
e31ff7b5, server `expansions` and `origin/main`; each accepted candidate becomes its own task.

Today every character ability takes one path: the die lights the Characters tab (`DieOptions`), the owner's region
glows, then the region panel's ability picture or the Characters inspector's button uses it at once.

| # | Effect | Line | Natural object | The die lights | Priority |
| --- | --- | --- | --- | --- | --- |
| C1 | Gimli (Erebor), Boromir (Gondor City/Stronghold), Legolas (Elven Stronghold): one political step | main | that nation's track | Politics; only that nation | high (UI-366's gesture) |
| C2 | Council Messengers (Boromir → Gondor, Gimli → Dwarves, Merry/Pippin → North): activate and advance | expansions | that nation's track (it glows even when only activation is left) | Politics; that nation | high |
| C3 | Council Legolas, Prince of Mirkwood: Elves one step | expansions | Elves track | Politics; Elves | high |
| C4 | Fate Gimli: Dwarf of Renown, and the Dwarven Lord's Messenger of Elrond (Iron Hills / Ered Luin) | expansions | Dwarves track | Politics; Dwarves | high (UI-366) |
| C5 | Saruman's Voice: recruit in each free Isengard settlement / two Elites in Orthanc | main | a "Saruman · Orthanc" row in the Reserve | Reserve | high |
| C6 | Mouth of Sauron: a Muster die as an Army die once per turn | main | the Shadow armies on the map | the map's armies | medium (an explicit choice; UX_SPEC forbids automatic abilities) |
| C7 | Great Sorcery (Lords Mouth): a Muster die as a Character die for his army | expansions | the Mouth's army | the map: that army | medium |
| C8 | Use Balrog: move him alone, or move/attack with his army | expansions | the Balrog figure / his army's figures | his region's panel | medium |
| C9 | Dwarvenking, Stewardship, Elvenking: a Regular becomes an Elite | expansions | the Elite in the Reserve ("Dáin · Erebor" row, as Gothmog's) | Reserve | medium |
| C10 | Ruler recruitments (Call to Arms, Iron Guard, Mustering of the Mark) | expansions | the king's nation row in the Reserve | Reserve | medium |
| C11 | Far-sighted (Thranduil) | expansions | the Fellowship's move | Fellowship | low (conflicts with D18: needs a decision) |
| C12 | The Black Captain Commands: recruit two Nazgûl / move Nazgûl | main | the Reserve's Nazgûl / the Nazgûl on the map | Reserve / map | medium |
| C13 | Scouts: withdraw into the siege | main | the Stronghold region | the map | low |
| C14 | Lure of the Ring: Corruption or eliminate | main | the lured Companion's portrait | Fellowship | low |
| C15 | The Nazgûl Strike!: roll for the Hunt | main | the Hunt Box | Hunt | low |
| C16 | Gandalf the Grey as Guide: draw from the matching deck | main | the deck (as Spear of Terror) | the deck | low |

Click to use (UI-369) elsewhere: the region panel's ability pictures use at their first click and duplicate the
object (remove them where C1–C11 move the use onto the object, otherwise select first); the king's hover inspector
lists abilities as text and never glows (give it the same chips); Narya's ring socket never glows and its Activate ✓
stands beside the token (fold it into the nation adapter); the Ruler coins select first but their use's object must
glow after the selection (C10–C11); the Fellowship tray still builds Standard/Far-sighted buttons (D18 says none; removed by UI-432, 2026-10-31 —
[UX_SPEC](UX_SPEC.md#declines-and-window-lists-follow-the-board-rule--ui-432)).
These stay as they are by earlier decisions: Gothmog's Reserve tokens, Council medallions (D13), nation tokens and
arrows, table cards, the kings' portraits and tiles, figures with an ability, Ring sockets, Hunt responses, battle
portraits and chips, decks and dice (a first click completes, UI-142), hand cards (already select-first).

Shared building blocks proposed: one nation-effect adapter (Muster step, Council token, Narya, C1–C4, the Kings nation
branches, base card nation targets — one priority rule, one tooltip, open-on-commit and the token's move), built on
`main` first; one ability-place table (ability → nation track, Reserve row, figure, army, Fellowship or deck) that
drives `DieOptions`, the components and the inspector; a Reserve ability row generalising Gothmog's (C5, C9, C10); an
army ability cost on the map (C6–C8). Inconsistencies found: the Characters tab lights for abilities whose objects are
elsewhere; the Reserve tab does not light for a die that only pays Gothmog; `DieHints` says only "Character
abilities"; political options target a region while Muster and cards target nations; a selected Ruler token does not
feed `DieOptions`; three deck-draw abilities sit on three different objects.
