<a id="expansions-ui-audit"></a>
# Expansions UI audit — 2026-10-26

Owner (2026-10-26): "audit the new features, abilities, mechanics and characters so that all of them have a UI that
matches the shape implemented now; log the ones that are not obvious and need a decision on what goes where and in what
shape". Read-only audit of client `cafae9bf` (`expansions` with `main` merged, EXP-14) against the server's `expansions`
(`c0909e2`); paths are relative to `unity/Wotr.Client/Assets/Wotr/Runtime` (client) and `src/Wotr.Domain` (server). It
succeeds the expansion part of the 2026-10-19 object controls audit
([OBJECT_CONTROLS_AUDIT_EXPANSIONS](OBJECT_CONTROLS_AUDIT_EXPANSIONS.md)). Nothing below is fixed; the gaps are registered
as UI-344–UI-349 and the open questions as EXP-15.

## The standard

The shape implemented now ([UX_SPEC](UX_SPEC.md), AGENTS.md):

1. **Object first.** Every choice is made on its own object: a map figure or region, a pool die, a hand or Effects card,
   a portrait, a track token, a battle die or figure stack, a Hunt response object. Text buttons that repeat an object and
   the generic DecisionWindow are legacy; a console option stands only for a mode with no object (CARD_CHOICES rule 4).
2. **Glow and clicks.** A choosable object glows gold and pulses in the shared rhythm (UI-301); a completing click sends
   at once (UI-142), nothing is sent without a click (UI-279), and the console's Pass declines a reaction (UI-291).
3. **The opponent's actions are presented** step by step on their objects (UI-302–UI-343, OPPONENT_ACTIONS_UX), for
   observers and replays too (UI-328, UI-330).
4. **Smooth transitions** (UI-321/UI-322): panels fade, changed numbers pulse, faces crossfade; Skip animations is instant.
5. **Names, never codes** (UI-331).

## What conforms

Most of the expansions already follow the shape:

- **Fate.** The map (Erebor, Dale, the Cities), the configuration, Gimli's version and homeland on the portraits and the
  map, Dwarf of Renown and Messenger as pictured abilities, the Fate cards on their objects (UI-266; no Fate step reaches
  the window), its Combat cards, observers and replays; the opponent's Fate actions play through the generic figure,
  Politics, control and battle cues.
- **Lords.** The special dice in the pool, Vilya, Nenya and the Ring costs at their sockets, Lore's die and result, every
  character's entry, the Balrog's activation on his figure, the pictured abilities (Use Balrog, Great Sorcery, Messenger,
  Prince of Mirkwood), Gothmog in the Reserve, Spear of Terror at the deck, He Sees at the Chief, the Council's versions
  and Guide on the portraits, Art of Disappearing on the map, Wind from the West and Safe Paths, the Lords Hunt table cards
  in the response row (UI-285 joined them on merge), Servant of the Shadow on the battle portrait.
- **Kings.** The Ruler die and its discard (UI-273), the sovereigns' portraits, crowns and corrupted marker, awakening,
  the corruption attempt (its buttons beside the portraits are owner-approved, UI-274), removal and inspection, the
  Chieftains' entry, the Kings Hunt tile, the sovereign targets outside battle, Work of the Enemy, Denethor's Folly.

## The 2026-10-19 rows

| # | Item | Now |
| --- | --- | --- |
| 1 | Council Travel token | fixed (UI-347): the medallion's click starts its use at once |
| 2 | Kings Nation step (Wisdom, It Is Not So Dark Here) | fixed (UI-278): the nation tokens in Politics, which opens for the step; Wisdom's politics branch carries its nation there |
| 3 | Palantír of Anárion "Skip extra card" | fixed (UI-348): the console's Pass (D7, every optional decline); the decks draw |
| 4 | Brand's combat replacement | fixed (UI-348): the gilt chip beside the own card; Pass keeps the card's effects |
| 5 | Welcome removal | fixed (UI-278): the pool die and ×; the tray lists no removal's dice; Barad-dûr's Ring on its socket |
| 6 | Dismay | fixed (UI-276): the glowing Ruler die's click, the console's Pass; the Dice & tokens panel deleted |
| 7 | Narya and Far-sighted as text components | fixed: Narya on the nation row and Gandalf's row (UI-347); Far-sighted is Thranduil's picture (UI-276) |
| 8 | Lords Hunt table cards | fixed by the merge (one `HuntCardResponses`) |
| 9 | Alternate Guides in the Hunt | Gimli's and the Council's are portrait objects; the base Guides still respond elsewhere (D9) |
| 10 | Orderly Withdrawal | fixed (UI-347): the "−1" on the hits, Pass keeps the hit |
| 11 | Council entry token | fixed (UI-347): the medallion marks it and lights the Chieftain's row; no buttons |
| 12 | A Balrog is Come! | fixed (UI-347): the Balrog's figure and his army's figures; Pass finishes |
| 13 | Corruption choices' tray fallbacks | removed (UI-348) with `CorruptionAttemptChoices` and `CorruptionInspectionChoices` |

## Gaps

### The opponent's expansion actions jump (UI-344)

The opponent presentation was built on the base game; OPPONENT_ACTIONS_UX §1 left expansion choreography out of scope,
but §6.1 promised "a ruler or council token at its row".

- **Sovereigns.** The server records `Facts.Sovereigns` (awaken, corrupt, remove, tile attached;
  `History/HistoryFacts.cs:84-89`); the client never reads it (`Presentation/Opponent/PresentationCueExtraction.cs`), so the
  portrait, status, meter and tiles jump. An awakening in place (a Leader unit becoming the character with the same id)
  gives no figure cue (`FigureCues.cs:44-66`), and the staged board moves the Leader unit when Brand moves
  (`StagedBoard.cs:76-79`). The corrupted king's map marker appears in one frame (`MapSovereignMarkers.cs:31-44`), and the
  tiles revealed at corruption (`Facts.Targets`) make no cue.
- **Tokens.** Ruler, Dismay and Council token costs (`History/HistoryCapture.cs:39-44`) and grants and spends
  (`ActionResourceChanges.cs:9-56`) build cues that no presenter handles (`CostEmphasis.cs:35` takes die costs only,
  `ResourcePresenter.cs:49-55` Ring costs, dice and draws): the tokens jump, the Ring's holder changes only at the release.
- **Dismay rerolls the watcher's own Ruler die** (`Kings/KingsDismay.cs:50-60`), but own dice are filtered out
  (`PresentationCueExtraction.cs:263-266`): the Free Peoples see only the release's crossfade.
- **The Balrog's activation and deactivation** change his level in place: no figure cue, and the source is not framed
  because `CharacterLibrary.AbilityHolder` searches content ability names (`balrog-activation`) while the costs carry the
  command's ids (`activate-balrog`, `use-balrog`, `gothmog-recruit`).
- **Sméagol leaving the Fellowship** (`Lords/SmeagolRules.cs:29-36`): no figure kind applies; only the Guide change plays.
- **Character-sourced Hunt responses** — Gimli's and the Council's Guide responses, Nenya (Galadriel) — are recorded
  without a character (`Effects/EffectRuntime.cs:201-205`), so `HuntPresenter.Response` marks nothing
  (`HuntPresenter.cs:93-103`); resolve the source through `CharacterLibrary.Find(SourceId)`.
- Observers and replays use the same presenters, so every gap applies to them (they do see the Dismay reroll).

### Transitions and glow (UI-345 — done 2026-10-26; the corruption draw counter went with the dead fallbacks in UI-348)

- **One-frame panels:** the Dice & tokens panel's close and its tab (`ResourcePanel.cs:62,82`), the sovereign inspector
  and its rows (`SovereignPanel.cs:156,172`, `SovereignInspector.cs:76,104,127`), the Council choices
  (`ComponentActionView.cs:57`).
- **No pulse or crossfade:** a sovereign's art, status and meter (`SovereignPortrait.cs:60-116`, `SovereignInspector.cs:63-67`),
  the Ruler/Dismay token row rebuilt without an arrival (`ActionPoolView.cs:234-247`), a Council token's spent state
  (`:261-269`), the corruption draw counter (`CorruptionAttemptChoices.cs:20-21`).
- **Choices without the shared glow:** the sovereign portrait glows but never pulses (the roster's pulse skips it,
  `CharacterRosterPanel.cs:164`); the attempt button's and the tiles' ChoiceGlow is probably invisible (no `.action`
  class; `LeftRail.uss:306-338` zeroes their border); the Ruler/Dismay tokens have no glow; `ExpansionChoices` buttons
  only `selected` (`ExpansionChoices.cs:115-120`); the Lords version cards only `selected` (`CompanionSetupCard.cs:41`);
  the Guide response row only `legal-target` (`HuntResponsesView.cs:72`), and its tooltip still says "Or Skip"
  (`HuntGuideResponses.cs:36`; Pass since UI-291). The lit art of the special dice, sockets, medallions, Lore dice and the
  Spear's deck never pulses (D6).

### Names in the log (UI-346 — done 2026-10-26, UX_SPEC "No identifier is ever shown")

- Standard die ids reach the log as codes: `RulerDieDiscarded` and Vilya write `FreePeoples.standard.2`
  (`Kings/KingsDice.cs:71`, `Lords/VilyaCompletion.cs:36`), and `ReadableText` (`ReadableText.cs:15-24`) turns only the
  side, so the row reads "Free Peoples.standard.2".
- `Lords/LordsArmyCosts.cs:38` writes "{figure}: use-balrog."; a kebab-case word that is no region or nation stays.
- Ring, token, die and ability ids read "an Elven Ring", "a token", "a die", "an effect" (`ReadableText.cs:73-90`): no code,
  but Vilya, the token's owner or the ability's title is lost; `ExpansionLabels` has the names.
- The server-side half (texts with names for installed clients) belongs with QUAL-21.

### Lords objects (UI-347 — done 2026-10-27; the owner's playtest of D7 and D10–D14 is pending)

Narya's dock component (row 7) goes: the nation row and Gandalf's Characters row already host it. Council token use
(row 1), the entry token (row 11), Orderly Withdrawal (row 10), A Balrog is Come! (row 12), Evil Knowledge's button beside
Continue / Stop (`BattleChoices.cs:150-166`) and the homelands' "Keep with the Fellowship" (`CouncilHomelandChoices.cs:35`)
move onto objects once D10–D14 are decided.

### Kings outside UI-276–UI-278 (UI-348 — done 2026-10-27; the owner's playtest of D17 is pending)

Brand's replacement (row 4, D17), Anárion's skip (row 3), the dead fallbacks (row 13), and the Kings ability pictures:
the upgrades and the Ruler abilities fall back to the generic scroll (`AbilityPictures.cs:26-31`). UI-276 keeps the Ruler
tokens, Dismay and Far-sighted (D18–D20); UI-278 the Nation step, the card branches, the in-battle sovereign targets,
Welcome and Barad-dûr (D15–D16; done 2026-10-27, the owner's playtest pending); UI-277 the Chieftains (done 2026-10-27: their substitution is the gilt chip, Keep card
effects the Pass, an applied replacement a gilt marker for both players).

### Fate (UI-349)

The owner's playtest (EXP-04) lacks test scenarios: "Fate · Gimli" holds only Dwarf of Renown; Messenger, the Guide
response, the homeland and a Fate Combat card exist only in the PlayMode seeder (`tests/Wotr.Scenarios/FateGimliScenario.cs`).
Messenger of the Dwarves shows the generic ability picture (`AbilityPictures.cs:26-32`).

## Decisions for the owner (EXP-15)

Each is a question of what goes where and in what shape; a recommendation follows each.

**Owner's answers (2026-10-26).** Every decision takes its recommended option (a); D8's automatic resolution has the
owner's explicit OK. Additions: D1 is a scene on the map at the sovereign's region (a Leader giving way to the
sovereign's figure at an awakening, the dark marker emerging at a corruption and fading at a removal), so D2's tile back
and D5's plate land there too; D14's second click needs an explicit console hint naming it; D17's gilt chip carries the
arms of Brand or the Chieftain and its tooltip names the card it cancels; D21's hover only highlights. The checks for
each answer, and the mechanics that needed explaining, are in
[EXPANSIONS_DECISIONS_PLAYTEST](EXPANSIONS_DECISIONS_PLAYTEST.md).

| Decision | Answer | Task |
| --- | --- | --- |
| D1–D5 | map scene; tile back to the region and a hatched pip; opponent token chips in the top bar; my Ruler die's reroll as the opponent's beat; revealed tiles as a brief plate | UI-344 |
| D6 | lit art plus the shared pulse | UI-345 |
| D7 | the console's Pass declines every optional decline, its tooltip naming it | UI-276, UI-277, UI-347, UI-348 |
| D8 | forced single steps stay automatic, the result in the effect trail and log | — |
| D9 | every Guide response in the Hunt's response row (the base Guides also on `main`) | UI-345 |
| D10–D14 | the "−1" pip; the Balrog's figure and the glowing army; the Mouth's portrait; a medallion's click uses it; a second click keeps the companion, with a console hint | UI-347 |
| D15–D16 | object first; the king in the battle column when he takes part | UI-278 (done 2026-10-27) |
| D17 | the gilt chip beside the own Combat card, Pass keeping its effect | UI-348 |
| D18–D20 | Thranduil's ability picture; the token in the pool and a badge on the portrait; the die's click alone | UI-276 |
| D21–D22 | two console buttons, hover highlights only; the region panel | UI-349 |

**The opponent's expansion actions**

- **D1 — Where a sovereign's change plays** (awakening, corruption, removal, a tile attached). The portraits live in the
  collapsed Characters panel. (a) a moment on the map at the sovereign's region, the portrait updated at the release;
  (b) the panel opens by itself; (c) both. *Recommended (a): panels never open by themselves for a presentation.*
- **D2 — A hidden corruption tile attached.** (a) a tile back travels from the Hunt pool to the sovereign and a hatched pip
  appears on the meter; (b) only the count changes. *Recommended (a), never the value.*
- **D3 — The opponent's Ruler, Dismay and Council tokens have no place on screen.** (a) fixed chips in the top bar's
  opponent summary; (b) a temporary medallion at the action's place; (c) the log only. *Recommended (a), as §6.1's "at its
  row" and the fixed layout.*
- **D4 — The opponent's Dismay rerolls my own Ruler die.** (a) present my die's change as the opponent's beat; (b) keep the
  release's crossfade. *Recommended (a).*
- **D5 — Tiles revealed at a corruption.** (a) their faces as a brief plate at the sovereign; (b) the log only.
  *Recommended (a).*

**Glow and declines**

- **D6 — Lit art versus the shared glow.** The special dice, Keeper Ring sockets, Council medallions, Lore dice and the
  Spear's deck light their art (UI-267/268/270 docs) but never pulse. (a) keep the art and add the shared pulse; (b) the
  art alone. *Recommended (a).*
- **D7 — Optional declines as named buttons:** Vilya's "Spend the die", Lore's "Keep results", Wind's and the Balrog
  card's "Finish", Orderly Withdrawal's "Keep the hit", We Won't Go Back's "No Corruption", "Keep card effects" (Brand,
  the Chieftains), Dismay's "Keep tokens". (a) the console's Pass declines all of them, its tooltip naming what is skipped;
  (b) keep the buttons. *Recommended (a); UI-291 covers only reactions today.*
- **D8 — Forced single steps resolved by the server** (Book of Mazarbul's region, Orcs Multiplying Again's second region,
  Monsters Roused's Elite region, forced recruitment plans; `Effects/EffectStep.cs:47-55`, `RecruitmentBatchStep.cs:31-39`;
  base cards too). (a) keep them automatic and show the result in the console's trail; (b) open the decision so the lone
  object glows and waits (UI-279's spirit). *Recommended (a), with your explicit OK.*
- **D9 — One shape for every Guide response in the Hunt.** Gimli's and the Council's are portraits in the Hunt window's
  response row; the base Guides (Merry, Pippin, Gollum) respond elsewhere (also open on `main`, OBJECT_CONTROLS_AUDIT
  finding 1). *Recommended: all in the response row.*

**Lords**

- **D10 — Orderly Withdrawal's object.** (a) a "−1" pip on the hit (`BattleChoicePip`, as UI-246), Pass keeps the hit;
  (b) the commander or Companion who enables it; (c) the revealed card. *Recommended (a).*
- **D11 — A Balrog is Come!** (a) the Balrog's figure moves him alone, a glowing army means "with the army", Pass
  finishes; (b) the console buttons (an agent-recorded UI-271 exception). *Recommended (a).*
- **D12 — Evil Knowledge.** (a) the Mouth of Sauron's portrait in the battle column, as the UI-247 reactions; (b) the button
  beside Continue / Stop. *Recommended (a).*
- **D13 — Council tokens.** (a) a medallion's click starts its printed use at once (no panel step); when a Kings Chieftain
  can enter with it, his Characters row lights instead; the "No token" buttons go; (b) keep the panel. *Recommended (a).*
- **D14 — Homelands' "Keep with the Fellowship"** (an agent-recorded UI-270 exception). (a) clicking the placed companion
  again keeps him; (b) the button. *Recommended (a).*

**Kings**

- **D15 — Card branches** (Wisdom; recruit-or-cleanse on the Stewards, Riders, Brand's Men, Dáin's Guard, Thranduil's
  Archers). (a) object first: a glowing Reserve figure recruits, a glowing king cleanses, nation tokens activate, kings
  inspect, the console only for a branch with no object; (b) console buttons (UI-278's text today). *Recommended (a).*
- **D16 — Hope and His Power Waxes during a battle.** (a) the king in the battle window's force column when he takes part,
  his Characters portrait otherwise (a Corrupted king has no figure); (b) always the Characters portrait (today).
  *Recommended (a).*
- **D17 — Brand's combat replacement.** Awakened Brand has a battle portrait; a Corrupted Brand gives the Shadow the
  replacement but has no figure. (a) a gilt chip beside the Combat card, Pass keeping the card's effects; (b) the map
  marker; (c) his Characters portrait; (d) a console option. *Recommended (a), the same Pass for the Chieftains.*
- **D18 — Far-sighted.** (a) Thranduil's ability picture, glowing with the eligible die ("Standard" is simply not using
  it); (b) a mode chip on the Fellowship's move; (c) a console toggle. *Recommended (a).*
- **D19 — Where the Ruler tokens sit.** UI-276 puts them beside the king's portrait, but they are spent with a die in the
  pool, and Dismay tokens are the Shadow's. (a) the live token in the pool and a badge on the portrait; (b) the portrait
  only; (c) the pool only. *Recommended (a).*
- **D20 — Which Dismay token is spent** makes no difference (`Kings/KingsDismay.cs:52-55`). (a) the die's click alone,
  the token picked for you and named in the tooltip and log; (b) pick the token too. *Recommended (a).*

**Fate**

- **D21 — Monsters Roused, West or East.** (a) the console's two buttons with the glow, hovering one lights its branch's
  regions; (b) a region's click picks the branch (ambiguous with East's Elite region). *Recommended (a).*
- **D22 — Gimli's political abilities.** UI-266 put them in the region panel and roster instead of the Dwarves' row on the
  Politics track that its task named. (a) keep the region panel (one place per mechanic); (b) the Politics row.
  *Recommended (a), confirmed.*

## Not verified

Nothing was run; the glow, the transitions and the presentation gaps come from the code. Not traced: the Chieftains'
movement and combat choices landing on battle portraits, the King's Banner and God of Old, Battle Is Vain's choices, the
Blade of Westernesse and Words of Power shapes, the Removing Ruler Tokens variant's slots, the observer's view of a
Companion's private version, and whether every Kings and Lords character has portrait art and a moment.
