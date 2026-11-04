# Opponent presentation audit (2026-11-04)

Scope: client worktree `ui-282-main` and server worktree `expansions`, both on `release/0.10.0-rc.1` (server HEAD `4ec9242`).

Owner's question: are all actions performed by a player properly reflected and animated for the opponent, so that it is clear what the opponent is doing?

**How this was verified.** Everything below comes from reading code, plus three recorded fixtures (`Tests/Editor/OpponentActions/*.json`). I did not run the game or Unity, so timings are the constants in the code and nothing was judged by eye. One exception: a throwaway probe ran the compiled server engine (test-output DLLs from today, `tests/Wotr.Tests/bin/Release/net10.0`) to confirm the recruit facts in gap 1. It lives outside both repos. No repo file was changed.

## 1. Summary

| | CLEAR | PARTIAL | MISSING | Rows |
| --- | --- | --- | --- | --- |
| Player commands (all 69 `CommandKind`s; `UseCharacterAbility` takes two rows, plus a row for the Ruler token payload) | 41 | 28 (11 are "no scene" by design) | 2 (both hidden by the rules) | 71 |
| Effects with no command of their own | 22 | 9 (1 by design) | 1 (real) | 32 |
| **Total** | **63** | **37 (12 by design)** | **3 (2 by the rules, 1 real)** | **103** |

The architecture is sound. Recorded facts go through one cue pipeline, and the presenters are almost entirely side-agnostic. The real gaps are specific:

- **Two bugs give wrong or no information:**
  - Recruit badges and tooltips show double the count.
  - Event casualties play no scene, and the camera travels to nothing.
- **One thing affects every action:** the spent die never visibly leaves. It is ringed, then vanishes at the release.
- **Facts dropped because the viewer's own click caused them:** the Shadow never gets a beat for the Free Peoples' dice roll, and about half of the opponent's start-of-turn draws go unpresented.
- **Losing focus discards the presentation.** Alt-tab ends the running beat, drops the queue and shows the latest state.

## 2. Reading the table

Paths: `PB/` = `Runtime/UI/Playback`, `PO/` = `Runtime/Presentation/Opponent`, `S/` = server `src/Wotr.Domain`. Other client paths are relative to `Runtime/` or `Runtime/UI/`. Test names are in `Tests/PlayMode` unless marked `[EM]` (`Tests/Editor`). `[N]` marks a two-client network test.

Shorthand for the six columns:

| Code | Meaning |
| --- | --- |
| **D** | The standard die treatment. A gold ring goes on the die in the opponent's row: a 0.35 s beat, then it stays as the action's source (`PB/CostEmphasis.cs:31-44`). A "→ face" badge appears when the die is used as another face (`:81-82`). The die **stays in the row for the whole presentation** because the cost die is excluded from die cues (`PO/PresentationCueExtraction.cs:287-291`). At the release the row is rebuilt: **the die vanishes in one frame, the others shift left** (`Expansions/ActionPoolView.cs:62-68`), and the Spent chip's number swells for 0.32 s (`TopBar/OpponentSummary.cs:67,158-165`; `ValuePulse.cs:14`). No flight. |
| **D1** | D on the first step only. Between steps the die sits in the row with a "•" badge, disabled (`ActionPoolView.cs:150,160-165`). The ring is gone once a presentation has been released (`OpponentPresenters.cs:136`). |
| **—** | No die or other cost. |
| **Pn "x"** | A pennant: no text, tooltip "x" plus the first result line (`Activity/ActionNoticeStack.cs:66-107`, `Presentation/Activity/ActionResultText.cs:16,284-315`). It appears **when the history page arrives, before the presentation** (`Application/ActionNoticeController.cs:139-157`). It is read only on click. Every pennant also has a Log entry, refreshed at the release (`Application/MatchSession.cs:61`). |
| **no Pn** | A private command: its step is `OnlyFor` the actor (`S/History/ActionDecisionFacts.cs:27`, `ActionCommandCatalog.cs:39-40`). |
| **Alert** | `DecisionRequired` sounds at the release if the decision passes to the viewer (`Audio/SoundFacts.cs:38-42`). It applies to every row and is not repeated below. |

Ratings: **C** CLEAR, **P** PARTIAL, **M** MISSING. **design** = the spec deliberately plays no scene (§3.5, §6.16). **hidden** = the rules keep the rest private.

## 3. Player commands

### Turn and resources

| # | Command (who) | (1) Die | (2) Notice | (3) Beat | (4) Motion | (5) Sound | (6) Hidden | Rate | Evidence | Test |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ContinueFellowship (FP) | — | Pn "Fellowship phase" | none | none; the phase label changes at the release | — | — | P design | `S/Turns/TurnCommands.cs:57-59`; no facts recorded | indirectly `[EM] EmptyDeclinesMakeNoScene…` |
| 2 | AllocateHunt (Sh) | — | Pn "Hunt dice", "Hunt Box +N" | one beat: rolled dice and the Eye count lit 0.7 s, then changed and held 0.8 s | faces cross-fade; Eye number steps | silent beat; `DiceRoll` at the release | — | C | `PB/ResourcePresenter.cs:73-114`; recorded in `hunt-shadow.json` rev 7 | `TheHuntAllocationAndTheRollAreOneBeat…` |
| 3 | DiscardCards (both) | — | Pn "Cards discarded" | hand count lit 0.7 s, then count −N with a "−N" plate of backs, held 0.8 s | two backs rise and dissolve out of the count | `CardMove` at the release | which cards | C | `PB/ResourcePresenter.cs:124-142,177-193`; `S/Turns/CardFlow.cs:50-62` | `ADiscardShowsItsCountAtTheHand`, `CardDissolveTests` |
| 4 | DiscardDie (both) | D (ends in Out) | Pn "Die discarded" | only the 0.35 s cost beat | die vanishes; Out chip pulses | — | — | **P** | `S/History/HistoryCapture.cs:45-46`; cost dice excluded `PO/PresentationCueExtraction.cs:288` | `AForfeitedDieKeepsItsCostCopy`, `[N] PlaybackAndSkipLeave…` |
| 5 | Pass (both) | — | Pn "Pass" | none | none | — | — | P design | `S/Turns/TurnCommands.cs:82-92` | `APassAndQuietDeclinesPlayNoScene` |
| 6 | DrawCard (both) | D | Pn "Card drawn" | count lit, then back(s) fly | `DrawFlight` from the deck plaque into the count, which flashes and steps; "+1" plate | none (hand counts are silent, `Audio/SoundFacts.cs:10`) | identity (deck is public) | C | `PB/ResourcePresenter.cs:146-155`, `PB/DrawFlight.cs`; `S/Turns/CardFlow.cs:16-28` | `ADrawnCardFliesFromItsDeck…`, `TheHandCountLightsUp…`, `[N] PlaybackAndSkipLeave…` (Sh watching) |
| 7 | UseRing (both) | no die spent | Pn "Elven Ring" | Ring socket lit 0.7 s; die lit, then old→new plate; Ring's chip and socket lit | die face cross-fades | — | — | C | `S/Turns/RingRules.cs:29-55`; `PB/ResourcePresenter.cs:64-71`; `PB/TokenPresenter.cs:48-90` | `ARingIsChargedAtItsSocket…`, `[N] TheShadowsRingIsCharged…`. **The FP's Ring is never tested** |
| 69 | FinishActionSequence (both) | D1 leaves here | Pn "Finished the remaining action steps" | none | die vanishes | — | — | P design | `S/Turns/Sequences/ActionSequences.cs:106-111` | none |
| 8 | DeclineReaction (both) | — | Pn "Declined the reaction" | none | none | — | — | P design | `S/GameEngine.cs:112-124` | `APassAndQuiet…`, `[EM] ADeclineCausingAPublicRevealShowsOnlyTheReveal` |

### Muster and armies

| # | Command (who) | (1) Die | (2) Notice | (3) Beat | (4) Motion | (5) Sound | (6) Hidden | Rate | Evidence | Test |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 9 | AdvancePolitics (both) | D | Pn "Politics · nation" | panel opens and settles 0.4 s; row and token lit 0.7 s; flare; "At war" caption | token slides 0.7 s, crest cross-fades; panel closes at the next scene | Politics bell at the beat | — | C | `PB/PoliticsPresenter.cs:40-52,149-179` | `AStepToWarLights…`, `ActivationThenAdvancement…`, `[N] TheShadowsPoliticalStep…` (FP watching only) |
| 10 | Recruit (both; up to 2 steps) | D1 | Pn "Muster", **"+2 Regular" for one unit** | camera frames; force glows; **delta badge doubled** | new figures land with the arrival accent and emerge; glow held 1.5 s | none | — | **P (bug)** | Gap 1: `S/Armies/RecruitmentRules.cs:98-104` + `S/History/ActionLogRecorder.cs:67`; `PO/PresentationCueExtraction.cs:159`, `PO/FigureCues.cs:94-95`; `PB/FigureChangePresenter.cs:64-72`; `Presentation/Activity/ActionResultText.cs:168-174` | `TwoDistantRecruitmentSites…` (synthetic facts); `[N] TheShadowsRecruitsLand…` asserts only `StartsWith("+")` |
| 11 | RemoveExcess (both) | none (continuation) | Pn "Returned excess units…" | force glows; "−N" badge for 1 s | figures dim; a stack emptied fades out | — | — | C | `PO/FigureCues.cs:89-90`; `PB/FigureChangePresenter.cs:35-49` | removal path only: `AnUpgradeReplaces…ARemovalLeaves…` |
| 12 | MoveArmies (both; two-army step) | D1 | Pn "Army move" with route and counts | source glows 0.7 s; camera first; destination glows 0.9 s; named group plate if characters or Nazgûl | rear guard stays; movers rise, arc and land as one miniature (`MapFigureMotion.Fly`) | none | — | C | `PB/ArmyMovementPresenter.cs:34-41,104-123`; `march-free-peoples.json` | `TwoMarchesWithOneDie…`, `TheCameraSettles…`, `[N] TheOpponentsMarchesPlayInOrder…` (FP watching only) |
| 44 | FinishArmyEvent (both) | — | Pn "Finished the army Event" | none | none | — | — | P design | `S/Armies/MovementCommands.cs:32-36` | none |

### Event cards

| # | Command (who) | (1) Die | (2) Notice | (3) Beat | (4) Motion | (5) Sound | (6) Hidden | Rate | Evidence | Test |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 13 | PlayEvent (both) | D | Pn "Event · title" | card grows from the hand count, read 3 s (a click pins it), docks in the console trail or drifts to the Effects tab; effects follow through their own presenters | card flight; **hand count only steps at the release** | `CardMove` **at the release**, seconds after the reveal | nothing (public) | C | `PB/CardRevealPresenter.cs:53-66,80-93,258-271`; `PB/CardFlight.cs` | 10 in `OpponentCardPresentationTests`, 2 `[N]` (FP watching only) |
| 14 | ChooseEffect (both) | — | Pn "Resolved an effect choice" | the card's chip returns (`PB/CardRevealPresenter.cs:202-211`); effect plays through its presenter | per effect | per effect | the selection step is private (`S/Effects/EffectRuntime.cs:250-261`) | C | `S/History/ActionLogRecorder.cs:55-77` | indirect, card suites |
| 15 | RespondEffect (both) | — | Pn "Resolved a reaction" | character source glows (portrait or moment), or a table card's tile glows | per effect | per effect | — | C | `S/Effects/EffectRuntime.cs:193-218`; `PB/CharacterPresenter.cs:75-103` | `TheShadowsReactionDuring…`, `ATableCardAnsweringTheHunt…`, `[N] TheWhiteRider…`, `[N] Shadow03 TableBalrog…` |
| 42 | TakeEventLosses (both) | — | Pn "−N Regular · region" | **camera travels, then nothing plays** | none in the presentation; at the release the army dims or fades (UI-338) | — | — | **P (bug, the worst row)** | Gap 2: `PO/PresentationCueExtraction.cs:176-181`, `PB/BattlePresenter.cs:66-67`, `PB/PresentationCamera.cs:39-41`; `S/Effects/EventLosses.cs:30-35` | **none**; `[N] Shadow04…EventLossesRemainTheDefendersChoice` checks rules only |
| 43 | DiscardProtection (Sh) | D | Pn with the card named in the record | hand −2 beat; Effects tab or tile glows 0.8 s | discard backs dissolve | `CardMove` at the release | which two cards | **P** (which protection left is not named in the scene) | `S/Effects/RegionProtection.cs:46-73`; `PB/CardRevealPresenter.cs:134-139` | none at command level |
| 45 | DiscardPalantir (FP) | D, plus Ring if used | Pn "Discarded the Palantír of Orthanc" | Effects tab or tile glows 0.8 s | none | — (the card is the viewer's own; `Audio/SoundFacts.cs:112-121` watches only the opponent's table and discard) | — | **P** | `S/Effects/PalantirRemoval.cs:18-24`; `S/History/ActionResourceChanges.cs:30-37` | `ATableCardEntersAndLeaves…` (generic) |
| 46 | DiscardDenethor (FP) | D | Pn "Discarded Denethor's Folly" | same | none | — | — | **P** | `S/Effects/DenethorRemoval.cs:22-28` | generic only |

### Fellowship and Hunt (FP sends; the Shadow watches, except RerollHunt)

| # | Command (who) | (1) Die | (2) Notice | (3) Beat | (4) Motion | (5) Sound | (6) Hidden | Rate | Evidence | Test |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 16 | DeclareFellowship | — | Pn "Fellowship declared" with route | panel opens; row lit; camera frames the new region | figure flies between public regions | — | later progress stays hidden | C | `PB/FellowshipPresenter.cs:82-127,135-142`; `PO/PresentationCueExtraction.cs:243-247` | `[EM] OnlyAPublicRelocation…`; flight via the reveal test |
| 17 | RevealFellowship | — | Pn "Fellowship revealed" | state plaque lit, turns; camera | figure flies; token flips | — | — | C | same; `S/Fellowship/FellowshipFlow.cs:67-77` | `ARevealFliesTheFellowship…` |
| 18 | HideFellowship | D | Pn "Fellowship hidden" | panel opens; Hidden state lit, turns | Ring art turns over (`TokenFlip`) | — | — | C | `PB/FellowshipPresenter.cs:114-132`; `S/Fellowship/FellowshipFlow.cs:51-56` | `HidingMarksTheRing…` |
| 19 | SelectGuide | — | Pn "Selected the Guide" | panel opens; new Guide's portrait lit | badge moves | — | — | C | `PB/FellowshipPresenter.cs:108-109,167-171` | `ANewGuideTakesTheBadge…` |
| 20 | SeparateCompanions | D | Pn "Companions separated" | moment and identities; figure lit if its position is known | Companions fly from the Fellowship, or arrive in place if hidden | — | no route when position is unknown | C | `PB/FellowshipSeparation.cs:14-23`; `S/Fellowship/CompanionRules.cs:88` | `ACompanionLeaves…KnownPosition`, `ACompanionLeavingAHidden…` |
| 21 | MoveCompanions | D1 | Pn "Companions moved" | source and destination glow; names by portrait | flight | — | — | C | `PB/ArmyMovementPresenter.cs`; `PB/CharacterMarks.cs:22-40` | `GimliAndLegolasTravelTogether…` |
| 22 | MoveFellowship | D, **then the die enters the Hunt Box silently** (E14) | Pn "Fellowship moved · Hunt" | Hunt window: start 0.6 s, roll 1 s, tile 1 s, damage 0.6 s; then track rows lit, moved, held | tracks move in two stages; progress never ahead of the roll | Dice, progress, corruption at their beats | route and position while hidden | C | `PB/HuntPresenter.cs:37-44,65-98`; `PO/PresentationCueExtraction.cs:76-87`; `hunt-shadow.json` rev 3 | `AMissOpens…`, `AHitShows…`, `[N] TheShadowWatchesTheHunt…` |
| 23 | RerollHunt (Sh) | — | Pn "Resolved the Hunt re-roll selection" | window shows the new dice, 1 s | none beyond the dice | Dice | — | C | `PB/HuntPresenter.cs:76-78` | no named test |
| 24 | ResolveHuntDamage | — | Pn "Resolved Hunt damage" | response marked on the Ring or a portrait; casualty leaves the roster; tracks | roster changes; tracks | track sounds | — | C | `PB/HuntPresenter.cs:102-114,124-129` | `TheRingResponseIsMarked…`, `ACasualtyLeavesTheRoster…` |
| 25 | EnterMordor | — | Pn "Entered the Mordor Track" | panel; Mordor row lit | figure placed on the track without a flight; camera stays | — | — | C | `PB/FellowshipPresenter.cs:134-142` | `EnteringMordorPuts…` |

### Battle

| # | Command (who) | (1) Die | (2) Notice | (3) Beat | (4) Motion | (5) Sound | (6) Hidden | Rate | Evidence | Test |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 26 | AttackArmy (both) | D | Pn "Attack · region" | both forces and the region glow 1.5 s, camera frames both, window opens and settles 1 s | window fades in; attackers stay | `BattleStarted` **at the release, after the window opened** | — | C | `PB/AttackPresenter.cs:19-29`; `PB/BattlePresenter.cs:93-97`; `Audio/SoundFacts.cs:45-49` | `AnAttackTraces…`, `TheAttackOpens…`, `[N] AnAttackOpensItsDecisions…` |
| 27 | CommitCombatCard (both) | — | Pn "Battle · region" | back marked 0.6 s; "No card" shows only the stage change; both reveal together, ringed, 3 s | hand count steps at the release | — | identity until the reveal | C | `S/Battles/BattleCards.cs:33-38,58-60,63-74`; `PB/BattlePresenter.cs:99-138` | `TheCommittedCardStaysABack…`, `AClickOnARevealedCard…` |
| 28 | RerollBattle (both) | — | **no Pn** | re-rolled dice ringed and lifted 0.25 s, held 1 s | lift | Dice | which dice were selected, until rolled | C | `S/Battles/BattleCommands.cs:65-69`, `BattleRoll.cs:62-65`; `PB/BattlePresenter.cs:107-114` | `ARollRingsTheDice…` |
| 29 | TakeBattleLosses (both) | — | Pn "Battle" | each loss is its own hit in the window | small red burst 0.35 s, count steps or stack fades, Elite→Regular lights 0.45 s | none | unsubmitted selection | C (no beat if the player closed the window) | `S/Battles/BattleLosses.cs:49-53`; `PB/BattlePresenter.cs:142-188`, `PB/BattleHits.cs` | `CommittedLossesAreMarked…`, `[N] TheCpusCasualtiesPlayHitByHit…` (Sh watching) |
| 30 | ContinueBattle (attacker) | — | Pn "Battle" | ribbon advances, 0.6 s | none | — | — | P design | `S/Battles/BattleCommands.cs:73-76`; `PB/BattlePresenter.cs:120` | `ALaterRoundStaysInTheSameWindow` |
| 31 | StopBattle (attacker) | — | Pn "…Attack stopped" | ribbon only; window closes at the next scene | none | — | — | **P** (the decision is readable only from the tooltip) | `S/Battles/BattleCommands.cs:77-80`; `PB/BattleWindowRetirement.cs` | none |
| 32 | RetreatBattle (defender) | — | Pn "…retreats" | returns to the map: source glows, camera, destination glows | flight | — | — | C | `S/Battles/BattleMovement.cs:37-47` | `ARetreatAndAnAdvanceReturn…` |
| 33 | HoldBattle (defender) | — | Pn "Battle" | ribbon only | none | — | — | P design | `S/Battles/BattleCommands.cs:84-87` | implied |
| 34 | AdvanceBattle (attacker) | — | Pn "…Attacker advances" | as a march; an empty advance moves nothing | partial group flies | — | — | C | `S/Battles/BattleMovement.cs:53-60` | `APartialAdvance…`, `ARetreatAndAnAdvance…` |
| 35 | FightFieldBattle (defender) | — | Pn "Battle" | ribbon only | none | — | — | **P** | `S/Battles/StrongholdCommands.cs:30-34` | none |
| 36 | RetreatIntoSiege (defender) | — | Pn "…Defender withdraws into the stronghold" | **no scene**: only the battle state | a figure flight **at the release**, only if the view is exactly the next revision; otherwise a jump | — | — | **P** | Gap 6: `S/Battles/StrongholdRules.cs:11-24` records no Movement; the From==To change has no cue (`PO/FigureCues.cs:58`); `Map/Figures/BoardFigureChanges.cs:62-64,79` | `ARetreatIntoAStronghold…` uses a Movement **the server never sends for this** |
| 37 | ExtendSiege (attacker) | figure cost, no cue | Pn "Battle" | "−1 Elite", "+1 Regular" plates in the window | none | — | — | C | `S/Battles/StrongholdRules.cs:33-38`; `PB/BattlePresenter.cs:197-211` | `ASiegeExtensionShows…` |
| 38 | SetCombatCardPolicy (both) | — | Pn when it answers a pending choice | as "No card" | none | — | — | P design | `S/GameEngine.cs:41-43`; `S/History/ActionLogRecorder.cs:61-62` | none |

### Characters

| # | Command (who) | (1) Die | (2) Notice | (3) Beat | (4) Motion | (5) Sound | (6) Hidden | Rate | Evidence | Test |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 39 | EnterCharacter (both) | D | Pn "Character", "X enters · region" | portrait and name moment 1 s, then the figure lands with a "+1" badge | arrival accent and glow | none | — | C | `PB/CharacterPresenter.cs:38-41,61-73`; `PO/FigureCues.cs:84-87` | `TheWitchKingsEntry…`, `GandalfTheWhite…`, `AragornReplaces…`, `[N] EntriesAndTransformations…` (both directions) |
| 40a | UseCharacterAbility, named holder (both) | D | Pn "Ability · source" | source moment at the portrait or figure, camera frames; effect through its presenter | per effect | per effect | — | C | `PB/CharacterPresenter.cs:75-103`; `Presentation/CharacterLibrary.cs:99-104` | `[EM] CharacterPresentationTests`, `[EM] TheExpansionsAbilityCommandsNameTheirCharacter` |
| 40b | UseCharacterAbility, Voice of Saruman (Sh) | D | Pn "Ability" | **no source mark**; only the recruits or upgrades | per effect | — | — | **P** | `voice-recruit`/`voice-upgrade` are missing from `CharacterLibrary.cs:106-113` | none |
| 41 | MoveShadowCharacters (Sh) | D1 | Pn "Minions moved" | group named beside the destination | flight | — | — | C | `S/Characters/ShadowTravel.cs:87-99` | `TheWitchKingMarches…AndTheNazgulTravelAlone` |

### Lords of Middle-earth

| # | Command (who) | (1) Die | (2) Notice | (3) Beat | (4) Motion | (5) Sound | (6) Hidden | Rate | Evidence | Test |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 47 | ChooseSpecialDie (both) | stays in row, or to Hunt | Pn "Selected a special action die" | changed dice lit; the rest dim as set aside | dim; Eye count if an Eye | — | — | C | `S/Lords/LordsDiceRoll.cs:61-71`; `PB/ResourcePresenter.cs:73-114` | **none** |
| 48 | UseNarya (FP) | D, plus Ring | Pn "Used Narya" | socket; nation activation and token to war, "At war" | slide | bell | — | C | `S/Lords/KeeperRings.cs:38-50` | **none** |
| 49 | UseVilya (FP) | die returns to Available | Pn "Used Vilya…" | socket and chip lit; **the kept die is not marked** (it is a cost die, excluded from die cues) | none on the die | — | — | **P** | `S/Lords/VilyaCompletion.cs:26-40`; `PO/PresentationCueExtraction.cs:288` | **none** |
| 50 | DeclineVilya (FP) | die leaves | Pn "Declined Vilya" | none | die vanishes | — | — | P design | same | none |
| 51 | ChooseCompanionVersions (FP, setup) | — | **no Pn** | none | none | — | the versions | **M hidden** | `S/Lords/LordsCompanionSetup.cs:23-44` | privacy only |
| 52 | UseEvilKnowledge (Sh) | — | Pn "Extended the siege with Evil Knowledge" | ribbon only; **no source mark on the Mouth of Sauron** | none | — | — | **P** | `S/Lords/EvilKnowledge.cs:11-21` | **none** |
| 53 | ChooseCouncilHomelands (FP, turn 1) | — | Pn "Placed Companions…" | Relocation: moment and arrival per region | land | — | — | C | `S/Lords/Council/CouncilSetup.cs:30-37`; `PO/FigureCues.cs:58-59` | **none** |
| 54 | ChooseCouncilToken (Sh) | — | Pn "Selected a Council action token" | chip lit, count 0→1 | number swell | — | — | **P** (which token only in the log) | `PB/TokenPresenter.cs:48-90` | `[EM] TheOpponentsTokensHaveFixedChips…` |
| 55 | UseCouncilToken (Sh) | token instead of a die | Pn "Used a Council action token" | chip lit, count drops; then politics or travel | per effect | per effect | — | C | `S/Lords/Council/CouncilTokens.cs:33-41`; `S/History/HistoryCapture.cs:41-42` | `[EM]` chips only |
| 56 | DiscardLore (FP) | D, plus Ring if used | Pn "Discarded The Lore…" | Effects glow 0.8 s | none | — | — | **P** | `S/Lords/Cards/LordsTableRemoval.cs:29-39` | none |
| 57 | DiscardGates (FP) | D | Pn "Discarded The Gates…" | same | none | — | — | **P** | same | none |

### Kings of the West

| # | Command (who) | (1) Die | (2) Notice | (3) Beat | (4) Motion | (5) Sound | (6) Hidden | Rate | Evidence | Test |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 58 | AwakenSovereign (FP) | D | Pn "Awakened a Sovereign" | camera; portrait moment; Leader replaced by the figure, in place or at the destination (no flight from the capital) | arrival accent | — | — | C | `PO/FigureCues.cs:73-74`; `PB/StagedBoard.cs:79-86`; `PB/SovereignPresenter.cs:46-47` | `AnAwakeningReplaces…`, `[N] TheFreePeoplesAwakening…` |
| 59 | RemoveCorruptedSovereign (FP) | D | Pn "Removed a Corrupted Sovereign" | marker glows, then fades | fade | — | — | C | `PB/SovereignPresenter.cs:134-138` | `AnOpponentsRemoval…` |
| 60 | AttemptCorruption (Sh) | Eye from the Hunt Box; **copy after the pool's last die, not at the Hunt plaque** (`PB/CostEmphasis.cs:71,87-94`) | Pn "Attempted to corrupt a Sovereign" | tile back flies plaque→force; meter plate with a hatched pip; revealed tile faces | `DrawFlight` | — | tile value | C, except **an attempt with no numeric tile has no beat** (camera travels, `Quiet` returns null, `PB/SovereignPresenter.cs:52-54`) | `S/Kings/CorruptionRuntime.cs:9-25` | `AnOpponentsCorruption…`, `[N] TheShadowsCorruptionAttempt…` |
| 61 | DrawCorruptionTile (Sh, Sequential) | — | **no Pn** | none | none | — | the tile. That a draw happened would be physically public; **check the rule** | **M hidden** | `S/History/ActionCommandCatalog.cs:39-40`; `S/Kings/CorruptionRuntime.cs:131-141` | none |
| 62 | ChooseCorruptionTile (Sh) | — | **no Pn** (the attempt's pennant stands) | public `CorruptionAdded`: as 60, or corruption: force glows, Leader leaves, dark marker emerges | flight, fade | — | chosen tile | C | `S/Kings/CorruptionRules.cs:28-41`; `PB/SovereignPresenter.cs:116-132` | `[EM] ACorruptionShowsTheAttachedTileOnce…` |
| 63 | DiscardRulerDie (both) | die dims in its slot | Pn "Discarded a die after the Action Roll" | die lit 0.7 s, then dim 0.8 s; Eye count if the Shadow's Eyes move | dim | — | — | C | `S/Kings/KingsDice.cs:60-72`; `PB/ResourcePresenter.cs:73-114` | **none** |
| 64 | DiscardWelcome (FP) | D | Pn "Discarded Your Welcome…" | Effects glow 0.8 s | none | — | — | **P** | `S/Kings/Cards/KingsWelcomeRemoval.cs:29-38` | none |
| 65 | DiscardBaradPalantir (FP) | D | Pn "Discarded the Palantír of Barad-dûr" | same | none | — | — | **P** | `S/Kings/Cards/KingsPalantir.cs` | none |
| 66 | UseDismay (Sh) | Dismay token | Pn "Used Dismay…" | chip lit; count drops; the re-rolled die glows with old→new | face turn | — | — | C | `S/Kings/KingsDismay.cs:50-60`; `PO/PresentationCueExtraction.cs:287-294` | `DismayRerollsMyRulerDie…`, `[N] TheShadowsDismay…` |
| 67 | DeclineDismay (Sh) | — | Pn "Declined Dismay" | none | none | — | — | P design | `S/Kings/DismayCommands.cs:37-39` | none |
| 68 | UseLoreBeforeActions (Sh) | — | Pn "Used the Lore…" | none; the following ChooseEffect plays | per effect | — | — | P design | `S/Lords/Cards/LoreOfTheRings.cs:17-25` | none |
| — | Ruler token on a command (FP; payload, not a kind) | Ruler die as the token's face | in that command's pennant | chip lit, count drops, Dismay chip rises | number swell | — | — | C | `S/Kings/KingsRulerTokens.cs:87-98`; `S/History/HistoryCapture.cs:39`; `PB/TokenPresenter.cs:39-46` | `TheOpponentsRulerTokenChangesOnItsChip` |

**Fate of Erebor** has no commands of its own. Its Gimli ability is row 40a and its cards use the same families. No opponent suite mentions it.

## 4. Effects with no command of their own

| # | Effect | What the opponent sees and hears | Rate | Evidence | Test |
| --- | --- | --- | --- | --- | --- |
| E1 | A card's recruit | Recruit beat per region; delta correct (cards record figures once, unlike Recruit) | C | `card-recruit-free-peoples.json` rev 1 | `[EM] ACardRecruitingInTwoRegions…`, `[N]` |
| E2 | A card's political step | Politics beat; before-value from the presented view; bell | C | `S/History/HistoryFacts.cs:33-38`; `PB/PoliticsPresenter.cs:46-47` | `[EM]`, `[N] OrcsMultiplying…` family |
| E3 | Nation activating (attack, capture, ability) | Crest cross-fade, token flare, bell | C | `S/History/ActionBoardChanges.cs:69-82` | `ActivationThenAdvancement…` |
| E4 | Figures removed by a card without a choice | Removal beat, "−N" | C | `PO/FigureCues.cs:89-90` | `AnUpgradeReplaces…` |
| E5 | **A card's dice rolled outside a battle** (Dead Men, Ents, Dreadful Spells, Rangers…) | **Log line only.** `Facts.Rolls` makes no cue | **M (real)** | `S/Effects/EffectExecution.cs:56-58`; no consumer in `PO/PresentationCueExtraction.cs:35-48` | none |
| E6 | Battle casualties, leader or character lost | Hit per loss; portrait ringed | C | `PB/BattlePresenter.cs:142-211` | `TheWitchKingLost…` |
| E7 | Sovereign corrupted | Force glows; Leader leaves; dark marker emerges; revealed tile faces | C | `PB/SovereignPresenter.cs:116-132` | `AnOpponentsCorruption…` |
| E8 | Sovereign removed automatically | Marker glows and fades | C | `S/Kings/SovereignLifecycle.cs:64-71` | `AnOpponentsRemoval…` |
| E9 | **FP's action-dice roll, seen by the Shadow** | **No beat**, because the Shadow's own AllocateHunt step carries it. Faces cross-fade in the top bar at the release; dice sound at the release. Every turn | **P** | `hunt-shadow.json` rev 7; `PO/PresentationSelection.cs:13-17`; `PO/PresentationCueExtraction.cs:50-51`; `Expansions/ActionPoolView.cs:210-219` | none |
| E10 | Opponent's dice recovered at turn start | If the opponent ended the turn: a die beat where **nothing is lit for 0.7 s** (the dice are not in the pool), then they appear lit. If the viewer ended it: they just appear | **P** | `hunt-shadow.json` rev 5; `PB/ResourcePresenter.cs:91-97`, `PB/PresentationMarks.cs:53-56` | none |
| E11 | **Opponent's start-of-turn draws** | Opponent's click ended the turn: `DrawFlight`, 2 cards. **Viewer's click ended it (about half the turns): the chip only swells 0.32 s** | **P** | `PO/PresentationSelection.cs:15-16,24`; `S/Turns/TurnFlow.cs:34-40` | `[EM] TheOpponentsDrawsAreCounts…` (not this case) |
| E12 | Draws from a card | `DrawFlight`, count steps | C | `S/Effects/BasicEffectSteps.cs:27-35` | `DrawsAreOneBeat…` |
| E13 | Forced or random discard (Worn, Wind from the West, We Won't Go Back) | Hand count −1, dissolve; identity hidden | C | `S/Cards/WornDiscardStep.cs:63-68`; `S/Lords/Cards/WindFromWestSteps.cs:68-71` | generic |
| E14 | **FP's paying die entering the Hunt Box** | **No beat.** The die vanishes from the top bar at the release and the plaque's FP number swells 0.32 s. Every Fellowship move | **P** | `S/Turns/DiceRules.cs:92-94`; excluded at `PO/PresentationCueExtraction.cs:288`; `UI/Hunt/HuntBoxView.cs:49-50` | none |
| E15 | A Shadow die entering the Hunt Box (Ring, rolled Eye) | Eye count lit, then steps | C | `PB/ResourcePresenter.cs:81,95-97,105-106` | `TheHuntAllocation…` |
| E16 | Hunt tile drawn or replaced | Face up in the window for 1 s; damage 0.6 s (brief, not judged by eye) | C | `PB/HuntPresenter.cs:39-41` | `AHitShows…` |
| E17 | A tile joining the Hunt pool | Face shown large, drops into the plaque (at the release) | C | `UI/Hunt/HuntTileDrop.cs` | `HuntTileDropTests` |
| E18 | Fellowship revealed by the Hunt | Row 17 | C | `PO/PresentationCueExtraction.cs:76-87` | `ARevealFlies…` |
| E19 | Corruption or progress | Two lit, moved, held stages with sounds | C | `PB/FellowshipPresenter.cs:95-107,147-155` | `AHitShows…`, `HidingMarks…` |
| E20 | Mordor inactivity or tracks changed by the viewer's own click | Primary step is the viewer's, so no beat; tracks in two stages (0.38 s) at the release, with the sound | **P** | `PO/PresentationSelection.cs:13-17`; `UI/BoardScreen.cs:698-711` | none |
| E21 | Companion casualty in the Hunt | Portrait lit, leaves the roster | C | `PB/HuntPresenter.cs:79-83` | `ACasualtyLeaves…` |
| E22 | Sméagol joins or leaves | Portrait flies from the tile into the roster; leaving marked | C | `PB/FellowshipJoin.cs`; `PB/FellowshipPresenter.cs:64-80` | `SmeagolsPortraitFlies…`, `SmeagolLeaving…` |
| E23 | Table card enters | Drifts to the Effects tab, which glows 0.8 s | C | `PB/CardRevealPresenter.cs:96-97,134-139` | `ACardThatStaysOnTheTable…` |
| E24 | **Table card removed or expiring** | 0.8 s glow at the tile, or at the **tab when the panel is closed: which card is not shown**. Nothing if the expiry falls in the viewer's own revision | **P** | `S/Effects/ActiveEffects.cs:63-79`; `PB/CardRevealPresenter.cs:128-139` | `ATableCardEntersAndLeaves…` |
| E25 | Capture and VP | Settlement traced, gem turns, "+N VP" or "−N VP" | C | `PB/ControlPresenter.cs:25-64` | `ACaptureChangesTheGem…` |
| E26 | Battle finished | Window closes as the next scene begins | C | `PB/BattleWindowRetirement.cs:21-28` | `AMovedBattleWindowStays…` |
| E27 | Game end | Existing screen at the release (`Outcome` cue has no presenter) | C | `UI/BoardScreen.cs:693`; `PO/PresentationCueExtraction.cs:300-304` | `[N] GameEndNetworkTests` |
| E28 | **Hand count after an Event or a committed Combat card** | Steps only at the release, seconds after the card left the hand | **P** | `PB/CardRevealPresenter.cs:80-93` never stages the hand; `S/Effects/EffectRuntime.cs:57`, `S/Battles/BattleCards.cs:30-31` | none |
| E29 | Ruler token spent, Dismay token gained | Chips lit, counts change | C | `S/History/ActionResourceChanges.cs:19-28`; `PB/TokenPresenter.cs:48-61` | `TheOpponentsRulerToken…` |
| E30 | Companion version revealed (Lords) | Log line and roster; the recorded change classifies to no cue | **P** | `S/Lords/LordsCompanionSetup.cs:55-63`; `PO/FigureCues.cs:44-61` | none |
| E31 | Balrog's level | Force glows, portrait moment | C | `PO/FigureCues.cs:76-78` | `[EM] TheBalrogsLevelChangesInPlace` |
| E32 | Phase or turn change | Label changes at the release; a ribbon only for the viewer's own decision | P design | `UI/BoardScreen.cs:663`; `Presentation/TurnSignal.cs:9-22` | none |

## 5. Cross-cutting findings

### How facts reach the client
Server `ActionLogRecorder.Record` stamps every event of a command. `ActionBoardChanges.Capture` adds the command's board diff to its primary step (figures, politics, control, dice, Fellowship, resources; `ActionLogRecorder.cs:55-77`). The client reads `actions?filter=All` (`ActionNoticeController.cs:139-157`), selects shown steps (`PO/PresentationSelection.cs`) and extracts ordered cues (`PO/PresentationCueExtraction.cs:25-54`). `PresentationPlayback` holds the view and plays each cue through every presenter that accepts it. A cue no presenter accepts takes no time and shows nothing, which is exactly how gap 2 goes unnoticed.

Facts recorded but shown by no cue: `Rolls`, `HitTarget` and `Hits` outside a battle; `Budget` (nothing tells the viewer a second step of the die may follow); `Targets` (except revealed tiles); `FellowshipRoute`; `Movement.Route` (flights go straight).

### The spent die
It does not visibly leave. See code **D** above. There is also a probable small artifact: at the release the row reflows first and the ring then fades for 0.4 s (`PresentationPlayback.cs:244,255`, `CostEmphasis.cs:46-55`), so it may briefly ring whichever die moved into the slot. Read from code only.

### Multi-step actions
Each step is its own revision under one action id. The cost shows once (`ActionMemory`), and the board keeps its staged positions between beats. Two marches and two recruits read correctly in code and are tested for marches (`[N] TheOpponentsMarchesPlayInOrder…`). Between released steps the die shows only its "•". `FinishActionSequence` has no scene.

### Sound
Sounding at their beat: Hunt and battle dice, track moves, the Politics bell (`PB/HuntPresenter.cs:77`, `PB/BattlePresenter.cs:111`, `PB/FellowshipPresenter.cs:152-154`, `PB/PoliticsPresenter.cs:183`).

Sounding only at the release, seconds late: `BattleStarted`, `CardMove`, the action dice roll, `DecisionRequired` (`Audio/GameSounds.cs:65-94`).

No sound at all: marches, recruits, character entries, captures, retreats, Rings, tokens, opponent's draws, battle hits, a drawn tile. The spec allows this (§6.8: a presentation never relies on sound, existing cues only).

### Several actions at once, and Skip
The queue is orderly (`PresentationPlayback.cs:60-84,177-206`). Newer views replace `Latest`, cues queue, the board stays on the earlier view and every command path is held (`TurnInteraction.Hold`). Nothing compresses a long backlog, so a fast opponent makes the viewer wait; Skip is the only way out. Skip ends the beat and the backlog, then releases on the next tick (`:123-130`) with flights landed and tracks quiet (`OpponentPresenters.cs:122-132`). It reaches the latest state and marks nothing read. Tested: `[EM] SkipEndsTheBeatAndTheBacklog…`, plus a Skip case per suite.

### Reconnect, async return, focus
- **Reconnect** shows the latest state and replays nothing. The status is "Reconnecting" during `Refresh()` (`Networking/MatchConnection.cs:174-175`), so the view is presented at once (`PresentationPlayback.cs:69-72`), and later cues are older than `PresentedRevision` and dropped (`:81-83`). Tested by `[N] …AReconnectShowsTheLatestState`.
- **Returning to an async game** is a state jump. The newest All page is a baseline, remembered but never played (`ActionNoticeController.cs:119-136`, `OpponentPresentationModel.cs:36-42`). The whole unread opponent history comes back as pennants, read newest first, one card per click.
- **Focus loss** (alt-tab) ends the beat, clears the queue and applies the latest view at once. While unfocused, every view applies immediately and every cue is dropped (`PresentationPlayback.cs:79-80,139-144,156-174`; `PlaybackBinding.cs:31`), even though `runInBackground` is 1. This is the spec (§7), so it is an owner decision. In a live game it means looking away for a moment loses the opponent's whole action, with only pennants left.
- **Slow history.** A history page still missing 1 s after its view arrived releases without animation, and its late cues are dropped (`PresentationPlayback.cs:19,117-118`). Silent on a slow connection. Frequency unknown; nothing measures it.

### Reduced motion ("Skip animations")
The code paths are consistent: no flights (`MapFigureMotion.AddFlights`), camera jumps (`PB/PresentationCamera.cs:63`), the card stands at once (`PB/CardRevealPresenter.cs:84`), draws become lit-then-changed (`PB/ResourcePresenter.cs:135`), tile backs likewise (`PB/SovereignPresenter.cs:86-91`), Sméagol stands (`PB/FellowshipJoin.cs:42`). **Beats keep nearly their full length**: the card still holds 3 s and marks still hold. That is UI-321's rule, but it means the setting does not shorten the wait. Coverage is uneven: the Kings suite runs both settings; the rest have spot cases only (`SkipAndReducedMotionEndOn…`, `UnderSkipAnimationsTheCardStandsAtOnce`, `ReducedMotionJumpsTheCamera…`). None for politics or the resource beats.

### Both directions
**Production code is nearly symmetric.** The only side-specific lines in `PB/` and `PO/` are rule-driven: sovereigns are FP, Dismay is the Shadow's, a Shadow figure recorded from `fellowship` is an entry, die art.

The real asymmetry is the selection rule. "Own step = nothing shown" hits the Shadow's AllocateHunt hardest (E9), because that one command also rolls the opponent's dice.

**Tests are biased to the FP watching.** Offline suites by viewer:

| Suite | Watching as FP | Watching as Shadow |
| --- | --- | --- |
| Army (8) | 8 | 0 |
| Recruitment and politics (10) | 10 | 0 |
| Card (10) | 10 | 0 |
| Battle (14) | 14 | 0 |
| Resource (8) | 8 | 0 |
| Character (11) | 7 | 4 |
| Hunt (16) | 1 | 15 |
| Expansion (6) | 2 | 4 |

Network tests have the Shadow watching only for the Hunt, a draw and a forfeit, character entries, the white rider, losses and an awakening. **The Shadow is never tested watching an FP march, recruit, political step, Event reveal, Ring, capture or attack window.** Recorded fixtures: only attack and Hunt exist for both viewers; march, card and reaction are FP viewer only.

The synthetic stage also hides real data shapes. It builds single recruit facts and a shelter Movement, neither of which the server produces (gaps 1 and 6).

### Privacy
No leak found in this read. Draws, commits and hidden-tile values carry no identity (`PO/PresentationCueExtraction.cs:117-124`, `PB/SovereignPresenter.cs:150-165`), and hidden moves frame nothing (`PO/PresentationCueExtraction.cs:243-247`).

## 6. Prioritised gaps

"Often" assumes about 10–14 actions per game turn.

### High value

| # | What the opponent misses | How often | Proposal | Files | Size |
| --- | --- | --- | --- | --- | --- |
| **1** | **Recruit counts are doubled:** one Regular shows "+2" on the map badge and "+2 Regular" in the pennant. The server records each command recruit twice with a different `From` (`''` from `HistoryFacts.Unit`, `reserve` from the boundary diff), and the client's dedupe key includes `From`. Confirmed by running the engine, see below. Also hits Kings' ruler recruitment and an entry with a recruit | Every Muster recruit, every game | Client: dedupe by figure id and destination in `FigureCuesByStep` and `ActionResultText.Recruits`. Better also: stop the server adding the duplicate (needs a pin). Make the network test assert the exact count | `PO/PresentationCueExtraction.cs`, `PO/FigureCues.cs`, `Presentation/Activity/ActionResultText.cs`; `OpponentRecruitmentPoliticsNetworkTests`; server `RecruitmentRules.cs` | Small |
| **2** | **Event casualties play nothing:** the camera travels to the region, no beat, then the stack drops at the release. `Covered` removes the figure change because a loss describes it, but only `BattlePresenter` shows losses and it refuses non-battle cues | A few per game (Dreadful Spells, Ents, Dead Men, Valinor, Rangers, Madness, Kings' enemy-army cards) | Limit `Covered`'s loss part to steps with a `BattleId`. The change then flows through `FigureChangePresenter` (trace, "−N", staged count; a downgrade as Elite out, Regular in). Remove `Losses` from `PresentationCamera.Frames` | `PO/PresentationCueExtraction.cs:176-181`, `PB/PresentationCamera.cs:39-41`; new `OpponentRecruitmentPoliticsTests` case | Small |
| **3** | **The spent die never visibly leaves,** and the FP's die entering the Hunt Box has no beat (E14) | Every action; every Fellowship move | Emit a die cue for the cost die's final move (`To` not Resolving). `ResourcePresenter` flies a copy with `DrawFlight` (its `back`, `size` and `dress` parameters already exist) into the Spent or Out chip or the Hunt plaque's FP count, steps it with `staged.Die`, and ends the cost ring. Skip clears it; Skip animations uses lit-then-changed. Adds about 0.8 s per action | `PO/PresentationCueExtraction.cs:287-291`, `PB/ResourcePresenter.cs`, `PB/CostEmphasis.cs`, `UI/Hunt/HuntBoxView.cs` (expose the FP count), `Presentation.uss`; cue-list assertions in `OpponentPresentationModelTests` | Medium |
| **4** | **Looking away loses the action.** Focus loss ends the presentation and drops the queue. An async return is a jump with pennants only | Every alt-tab during an opponent's action; every async return | Owner decision (§7). Focus: pause the clock instead of rebasing, resume on return, cap the backlog, keep the gate. Small. Async catch-up: playing missed actions needs earlier views (the replay path has them, the live path does not) and conflicts with §3.10's "no replay" budget. Large, and I would defer it | `Application/Playback/PresentationPlayback.cs:139-144`, `OpponentPresentationQueueTests`, spec §7 | Small (focus); Large (async) |
| **5** | **Opponent's resources changed by the viewer's own click are unpresented:** FP's roll for the Shadow (every turn), about half of the opponent's start-of-turn draws, dice recovery, Mordor inactivity | Every turn | Show these specific facts on the viewer's own step, keeping `OwnCommand` so the camera stays: opponent-owned dice leaving Unrolled, and automatic `Draw` steps of the opponent. Also skip the 0.7 s of nothing lit when no changed die is in the pool (E10). Adds 1.5–3 s after the viewer's click, so ask the owner | `PO/PresentationSelection.cs`, `PO/PresentationCueExtraction.cs:50-51,287-291`, `PB/ResourcePresenter.cs:91-97`; `OurOwnAttackIsNoAttackScene…` | Medium |

**Probe output for gap 1**, from the compiled engine:
```
Stepwise Recruit, one Regular at minas-tirith:
  UnitRecruited actor=FreePeoples figs=[base.gondor.regular.01 ''>'minas-tirith' ; base.gondor.regular.01 'reserve'>'minas-tirith']
Non-stepwise Recruit, Regulars at minas-tirith and pelargir:
  UnitRecruited actor=FreePeoples figs=[regular.01 ''>'minas-tirith' ; regular.01 'reserve'>'minas-tirith' ; regular.02 'reserve'>'pelargir']
  UnitRecruited actor=          figs=[regular.02 ''>'pelargir']
```
The engine facts are confirmed by execution. The client half (`Deltas` counting both changes, so "+2") is from reading, and I did not see it on screen. Worth one glance in the running game before fixing.

### Medium value

| # | What the opponent misses | How often | Proposal | Files | Size |
| --- | --- | --- | --- | --- | --- |
| **6** | **Retreat into siege has no scene;** the figures hop to the walls at the release, or jump | Most assaults on a stronghold | Server: record a `Retreat` Movement with From==To and Garrison true in `Shelter`. `ArmyMovementPresenter` already handles that shape, and the existing test uses it. Check `BattleResult`'s wording order | `S/Battles/StrongholdRules.cs:11-24`, `Presentation/Activity/ActionResultText.cs:123-136`; server test; pin | Small–Medium |
| **7** | **Quiet battle choices are unreadable** (Hold, Continue, Stop, Fight in the field, Evil Knowledge, No card): only the ribbon steps | Every battle round | One short caption plate per decision, from the step's command ("Holds", "Stops", "No card"), via `marks.Plate`. §3.9 allows one caption per beat, but §3.5 and §6.15 say no scene for some of these: owner decision | `PO/PresentationCue.cs` (add `Command`), `PB/BattlePresenter.cs:99-121` | Small–Medium |
| **8** | **A card's dice rolled outside a battle are invisible** (E5) | A few per game | New cue from `Facts.Rolls`: a plate of die faces at the region, 1 s, before the losses it causes, with the Dice sound | `PO/PresentationCue.cs`, `PO/PresentationCueExtraction.cs`, a small beat in `PB/` | Medium |
| **9** | **Which table card left** is anonymous when the Effects panel is closed; applies to rows 43, 45, 46, 56, 57, 64, 65 and expiries | Low–moderate | Add a title plate beside the tab using `PresentationMarks.Plate` and `CardLibrary.Title`; hold 1.5 s | `PB/CardRevealPresenter.cs:128-139` | Small |
| **10** | **Voice of Saruman names no source** (the §6.13 "server follow-up" is not needed) | Each Voice use | Map `voice-recruit` and `voice-upgrade` to `muster-each-isengard-settlement-or-upgrade-two` in `CommandAbilities` (Saruman's printed ability, server `content/base-characters.json:204`). Then remove the notes in §6.13 and §9 | `Presentation/CharacterLibrary.cs:106-113`, `[EM] TheExpansionsAbilityCommands…` | Small |
| **11** | **Hand count lags the card** (E28) | Every Event; combat commits | Stage −1 as the Event's flight starts (`staged.Hand`, `board.StageHand`). Combat commit likewise, except a card from the table, which the commit fact cannot tell apart today | `PB/CardRevealPresenter.cs`, `PB/BattlePresenter.cs` | Small |
| **12** | **Test bias and synthetic shapes** let gaps 1, 2 and 6 through | Ongoing | Parametrise the Army, Card, Recruitment, Resource and Battle suites by viewer. Add FP-acting network scenarios. Add a server-recorded `muster-recruit` fixture and an event-loss fixture | `PresentedBoardStage`, the suites; server `OpponentActionFixtureRecorder` | Medium |

### Lower value

| # | What the opponent misses | Proposal | Size |
| --- | --- | --- | --- |
| 13 | `BattleStarted` and `CardMove` sound late | Play them at `BattlePresenter.Opened` and the card reveal through the sink; `GameSounds` already drops presented cues | Small |
| 14 | 1 s history wait silently drops the animation | Measure first (a diagnostic count). If real, extend while a fetch is in flight | Small |
| 15 | A corruption attempt with no numeric tile has no beat; the Eye's copy stands in the dice row, not at the Hunt plaque | Show the revealed tiles' plate in the `Quiet` branch; anchor an Eye cost at `Hunt.Box.EyeCount` | Small |
| 16 | Vilya's kept die unmarked; Lords version reveal unmarked; which Council token chosen | Light the die; a portrait moment; the token's name as a plate | Small each |
| 17 | Sequential Corruption: a draw shows nothing to the FP | Check KINGS p. 18. If the draw itself is public, add a tile-back beat | Small |

## 7. What I could not verify

- Nothing was seen on screen. "Too brief" judgments (tile 1 s, die ring 0.35 s, Effects glow 0.8 s) are from constants, so please check them live.
- The ring artifact at the release (§5) is inferred.
- Frequencies are estimates from the rules, not from recorded games.
- Network behaviour on a slow connection is reasoned, not measured.
- The probe used test-output DLLs built today, not the release build. `RecruitmentRules.cs` and `ActionLogRecorder.cs` were read at HEAD and match its output.
