# Character ability inventory — UI-439 (2026-11-01)

Every printed ability of every character card the client knows, where the client shows it, the icon it had before
UI-439 and the icon it gets. Sources: the pinned public content package `com.wotr.public-content` 1.0.17
(`unity/Wotr.Client/Assets/Wotr/Resources/character-ability-text.json` and `*-characters.json`; the same 110 card
abilities as `wotr-server` `content/` on `expansions`), and the client at `80df28fd` (`expansions`). The icons and how
they are made: [README.md](README.md).

## Summary

- **43 character cards**: base game 14, Lords of Middle-earth 14 (its six companions are also the Council's
  versions), Kings of Middle-earth 13 (five kings, awakened and corrupted, and three Dark Chieftains), The Fate of
  Erebor 2. The Council's tokens and the cities are not character abilities and have no cards here.
- **110 card abilities, 76 printed ability ids.** An id printed on several cards is one ability (Captain of the West
  on eight cards, the hobbits' Guide on six, Corrupted ruler on five kings). One id is printed as two different
  abilities: `not-minion-for-gandalf-white` is *Gandalf the White entry* on the Balrog, Gothmog and the Witch-king, and
  *Dark Chieftain* (with the shared Ruler die) on the three Chieftains — it gets a second icon, `dark-chieftain`.
  **77 icons in all.**
- **Commands that stand for a printed ability** carry their own ids: `activate-balrog`, `deactivate-balrog`,
  `use-balrog` → `balrog-activation`; `gothmog-recruit`, `gothmog-upgrade` → `fling-them-into-the-fray`
  (`CharacterLibrary.cs:101-108`); `voice-recruit`, `voice-upgrade` → `muster-each-isengard-settlement-or-upgrade-two`
  (`CharacterAbilityChips.cs:21-25`); the Kings' `kings.<id>` (prefix dropped); `UseNarya` → `narya`; `UseVilya` and
  `DeclineVilya` → `vilya`; the reaction `lords.nenya.replace` → `nenya`; `UseEvilKnowledge` → `evil-knowledge`;
  `lords.chief.spear-of-terror` → `spear-of-terror`; `lords.chief.he-sees-he-knows` → `he-sees-he-knows`.
- **Before UI-439, no ability had its own art.** The 76 ids shared 22 pictures borrowed from other parts of the kit
  (a battle's crossed swords for 12 abilities, the Politics map for 9, the Guide's crown and the Hunt's eye for 6
  each, the Muster die face for 5…). The character window's chips showed one scroll for every ability, and the two
  maps the client had disagreed (`AbilityIcons` on the roster, `AbilityPictures` on component buttons). On one card,
  several abilities shared a picture:
  - Gollum (`base.character.gollum`): Guide · Unseen path, Guide · Reveal show `hunt/eye`
  - Lady Galadriel (`lords.character.galadriel`): Valour of the Elven People, Combat-card level show `actions/battle`
  - The Balrog of Moria (`lords.character.balrog`): Fire and Shadow, Flame of Udûn, Activation show `dice/faces/use-balrog`
  - Meriadoc (`lords.character.merry`): Guide, Art of Disappearing show `hunt/eye`
  - Peregrin (`lords.character.pippin`): Guide, Art of Disappearing show `hunt/eye`

## Where the client shows an ability

Paths are under `unity/Wotr.Client/Assets/Wotr/`: `R/` is `Runtime/`, `S/` is `Resources/Wotr/Styles/`. Lines are
those of `80df28fd`.

### With a picture

| # | Place | file:line | Abilities | Shows before UI-439 |
| --- | --- | --- | --- | --- |
| 1 | Characters roster row: up to four ability buttons (also the Kings' sovereign rows) | R/UI/Characters/CharacterAbilityStrip.cs:44 (placeholder), :94 (`AbilityIcons.For`); S/Characters.uss:205-236, 255-268 (15–19 pt buttons, 12–15 pt glyph) | every printed id of the row's version | `AbilityIcons` map (R/Presentation/AbilityIcons.cs:12-108) |
| 2 | Character window: ability chips | R/UI/Characters/CharacterAbilityChips.cs:56-57; S/Characters.uss:105-106 (16 pt) | every printed id | always the scroll `characters/ability` |
| 3 | Character window: Click to use | CharacterAbilityChips.cs:200-201; S/Characters.uss:146-147 | the unrolled chip's actions | `actions/confirm` (an action, not the ability) |
| 4 | Component action buttons (decision dock, region hover panel, roster inspector) | R/Presentation/ComponentActions.cs:66 (character actions), :77 (movement costs), :88 (Far-sighted); rendered by R/UI/Expansions/ComponentActionView.cs:60-69; `Resources/Wotr.uss`:1028-1030 (24 pt glyph); hosts R/UI/BoardScreen.cs:62, R/UI/RegionHoverPanel.cs:61-70, R/UI/Characters/CharacterRosterContext.cs:32-42 | `UseCharacterAbility`/`EnterCharacter` actions incl. `kings.*`, `voice-*`, `gothmog-*`, `activate-`/`deactivate-balrog`; movement costs (`muster-as-army-once-per-turn`, `great-sorcery`); `kings.far-sighted` | `AbilityPictures.For(kind, id)` (R/Presentation/AbilityPictures.cs:34-52): by command kind or name pattern; the Move or Attack picture for movement costs and Far-sighted; the scroll for `balrog`, `prince-of-mirkwood` and unprefixed Kings ids |
| 5 | Component action: Narya | ComponentActions.cs:99-104 | `UseNarya` | text "Narya" (no picture) |
| 6 | Battle direct button: Evil Knowledge | R/UI/Battles/BattleChoices.cs:155, 214-224; S/Battle.uss:593 | `UseEvilKnowledge` | `battle/evil-knowledge` |
| 7 | Battle combat-replacement chip and applied marker | R/UI/Battles/BattleContext.cs:221-253; R/Presentation/CombatReplacementChoice.cs:22, 90-131; S/Interaction.uss:459-490, S/LeftRail.uss:217-224 | `heir-of-bard`, `shadows-in-the-north`, `red-wrath`, `lord-of-the-bats`, `i-command` | the nation emblem (`icons/nations/<nation>`), a stand-in by its own comment |
| 8 | Console ring sockets | R/UI/Console/ConsoleRings.cs:75-105; R/Presentation/NamedRings.cs:45-62; S/Console.uss:309-330 | `vilya`, `nenya`, `narya` | `console/ring-<key>` (the rings' own socket art) |
| 9 | Politics panel: Narya's activate button | R/UI/Politics/NationTrackRow.cs:72-76, 189-192 | `UseNarya` | `actions/confirm` |
| 10 | Politics panel: a nation's advance arrow | R/UI/Politics/PoliticalPanel.cs:196-201; R/Presentation/CharacterPolitics.cs:74-87 | `politics-*`, `messenger-*`, `prince-of-mirkwood` | `actions/next`, the ability in the tooltip |
| 11 | Hunt window: Guide response row | R/UI/Hunt/HuntResponsesView.cs:81-102; R/Presentation/HuntGuideResponses.cs:17-40 | `guide-separate-to-reduce-hunt-one`, `voluntary-reveal-to-reduce-hunt-one`, `guide-cancel-standard-hunt` | the companion's figure art |
| 12 | Starting Guide's crown (setup) | R/UI/Fellowship/FellowshipRoster.cs:114-120; `Resources/Wotr.uss`:1436-1451 | the candidate's `guide-*` ability | `fellowship/guide-crown` (the Guide role) |
| 13 | Map figure and region portrait offering an ability | R/Application/FigureAbilityInteraction.cs:48-62; R/Map/MapCharacterFigures.cs:206; S/Armies.uss:76, 111, 419 | `activate-`/`deactivate-balrog`, `lords.chief.he-sees-he-knows` | the generic ability rim `armies/rim-*-ability` |
| 14 | Character deck offering a draw | R/UI/Cards/DeckView.cs:47-60; S/Interaction.uss:339 | `lords.chief.spear-of-terror`, Gandalf's Guide draw | the deck's glow |

### Text, portrait or tooltip only

| # | Place | file:line | Abilities |
| --- | --- | --- | --- |
| 15 | Battle portrait reactions (glow, tooltip) | R/UI/Battles/BattleContext.cs:204-217, 257-269; R/Presentation/BattleReactions.cs:16-41 | The White Rider, Sorcerer, Evil Knowledge |
| 16 | Battle portrait cancelled | BattleContext.cs:84-96; R/UI/Battles/PortraitMedallion.cs:19-21 | any (glyph "⦸") |
| 17 | Decision tray action buttons | R/Presentation/ExpansionDecision.cs:33-39; R/UI/DecisionTray.cs:600-612, 667, 714 | Use Narya, Use/Keep Vilya, Evil Knowledge, Use ability |
| 18 | Vilya's decline / Pass | R/Presentation/NamedRings.cs:65-78; R/Presentation/OptionalDecline.cs:84-127 | `vilya`, the combat replacements |
| 19 | Nenya hint, Narya draft label | R/UI/Cards/CardChoices.cs:111-117; R/UI/Expansions/ExpansionChoices.cs:35-36 | `nenya`, `narya` |
| 20 | Hunt reaction buttons, tray effect buttons, console options | R/UI/Hunt/HuntReactionView.cs:16-26; CardChoices.cs:155-211; R/Presentation/CardConsoleOptions.cs:201-229 | other character reactions |
| 21 | Gothmog's reserve row | R/Presentation/GothmogReinforcements.cs:20-43; R/Application/ReinforcementsInteraction.cs:58-66 | `fling-them-into-the-fray` |
| 22 | Card cost chips | R/UI/Cards/CardCostPicker.cs:33-40 | Event costs paid by an ability (die face only) |
| 23 | Far-sighted travel header | R/UI/Fellowship/FellowshipChoices.cs:37-38 | `far-sighted` |
| 24 | Character choice buttons | R/UI/Characters/CharacterChoices.cs:25-37 | several abilities in one draft |
| 25 | Companion setup card | R/UI/Characters/CompanionSetupCard.cs:29-36 | every printed id (glyph "◇") |
| 26 | Region hover effect chip | R/UI/RegionHoverPanel.cs:159 | ability effects (glyph "◇") |
| 27 | Effects panel entry | R/UI/Cards/EffectEntry.cs:26-37 | a lasting ability effect (e.g. Mark of the White Hand) |
| 28 | History and playback | R/UI/Activity/ActionCard.cs:108-116; R/UI/Playback/CharacterPresenter.cs:98-111; R/Presentation/ReadableText.cs:80-82 | a used ability (die face or card icon; the source's portrait glows) |
| 29 | Tooltips | RegionHoverPanel.cs:209, 263-266; R/Presentation/CharacterRowTooltip.cs:47-50; FellowshipRoster.cs:134-138; R/UI/Expansions/SovereignInspector.cs:79-81; R/Presentation/TurnSummary.cs:57-58 | every printed id |

### Tests that pin today's pictures (`Tests/Editor/`)

`CharacterAbilityStripTests.cs:30-33, 43-44` (`AbilityIcons.For` values, every picture loads);
`ComponentActionTests.cs:100-108, 126-135, 231` (`AbilityPictures.For` values, voice → Muster/Upgrade, Far-sighted →
Move); `KingsCards02Tests.cs:205, 221, 341-346` (Kings pictures, the replacement chip's nation emblem);
`BattleArtTests.cs:191`, `UiArtKitTests.cs:142` (`battle/evil-knowledge`); `NamedRingsTests.cs:18-46`,
`GlowingHintClassesTests.cs:77-100` (ring sockets); `HuntGuideResponsesTests.cs:31` (figure art in the Hunt row);
PlayMode `RulerTokenMenuTests.cs:198-203` (Dáin's roster icons by name).

## The pictures before UI-439

| Icon today | Ability ids | Card rows | Ability ids sharing it |
| --- | --- | --- | --- |
| `actions/battle` | 12 | 22 | `champion-dwarves`, `champion-elves`, `champion-gondor`, `champion-north`, `combat-card-level-three`, `combat-strength-plus-one`, `fling-them-into-the-fray`, `forth-eorlingas`, `i-command`, `red-wrath`, `spear-of-terror`, `valour-of-the-elven-people` |
| `actions/political` | 9 | 11 | `entry-activates-all-free-peoples`, `messenger-dwarves`, `messenger-gondor`, `messenger-north`, `politics-dwarves-at-erebor`, `politics-dwarves-at-fate-homelands`, `politics-elves-at-stronghold`, `politics-gondor-at-city-or-stronghold`, `prince-of-mirkwood` |
| `fellowship/guide-crown` | 6 | 6 | `always-guide`, `enter-and-guide-when-no-companions`, `guide-conditional-narya-die`, `guide-draw-after-event-die-card`, `guide-first-move-to-fellowship-box`, `guide-hide-with-any-action-die` |
| `hunt/eye` | 6 | 13 | `art-of-disappearing`, `guide-cancel-standard-hunt`, `guide-separate-to-reduce-hunt-one`, `he-sees-he-knows`, `ignore-standard-numbered-reveal`, `voluntary-reveal-to-reduce-hunt-one` |
| `dice/faces/muster` | 5 | 5 | `call-to-arms`, `iron-guard`, `muster-as-army-once-per-turn`, `muster-each-isengard-settlement-or-upgrade-two`, `mustering-of-the-mark` |
| `characters/movement` | 4 | 7 | `far-sighted`, `move-four-alone-or-with-one-hobbit`, `movement-level-two`, `we-march-day-and-night` |
| `actions/expand` | 3 | 3 | `dwarvenking`, `elvenking`, `stewardship` |
| `actions/pinned` | 3 | 3 | `cannot-leave-lorien`, `cannot-leave-orthanc`, `cannot-leave-rivendell` |
| `dice/faces/use-balrog` | 3 | 3 | `balrog-activation`, `fire-and-shadow`, `flame-of-udun` |
| `fellowship/versions` | 3 | 10 | `may-transform-to-aragorn`, `may-transform-to-white`, `not-minion-for-gandalf-white` |
| `icons/cards` | 3 | 3 | `great-sorcery`, `household-of-elrond`, `shadows-in-the-north` |
| `icons/effects` | 3 | 3 | `fate-of-mirkwood`, `fate-of-the-lonely-mountain`, `fate-of-the-white-tower` |
| `icons/ring` | 3 | 3 | `narya`, `nenya`, `vilya` |
| `battle/leadership` | 2 | 2 | `forfeit-leadership-to-cancel-nazgul-for-battle`, `isengard-elites-have-leadership` |
| `dice/faces/card-draw` | 2 | 2 | `draw-after-first-round-combat-card`, `palantir-of-anarion` |
| `hunt/die` | 2 | 2 | `evil-knowledge`, `lord-of-the-bats` |
| `hunt/smeagol` | 2 | 2 | `add-gollum-when-no-companions`, `smeagol-departure` |
| `characters/flight` | 1 | 1 | `mark-of-the-white-hand` |
| `characters/level` | 1 | 1 | `level-of-highest-other-companion` |
| `characters/may-return` | 1 | 2 | `return-after-fellowship-elimination-except-mordor` |
| `characters/on-board` | 1 | 1 | `heir-of-bard` |
| `kings/status-corrupted` | 1 | 5 | `corrupted-ruler` |

## The new icons, for the code

- **One rule.** The icon of a printed ability is `Wotr/Art/UI/abilities/<ability id>` (a Resources path; 56 × 56 px,
  28 points), for all 76 ids. One card-specific form: the Dark Chieftains' `not-minion-for-gandalf-white` is
  `Wotr/Art/UI/abilities/dark-chieftain`. Keyed like the ability summaries
  (`character-ability-summaries.json`), that is `"<card id>#<ability id>"`:
  `kings.character.black-serpent#not-minion-for-gandalf-white`, `kings.character.shadow-of-mirkwood#…`,
  `kings.character.ugluk#…` → `dark-chieftain`.
- **Command ids** resolve to their printed ability first (the summary's list above): drop an expansion prefix
  (`kings.`), then `activate-balrog`/`deactivate-balrog`/`use-balrog` → `balrog-activation`,
  `gothmog-recruit`/`gothmog-upgrade` → `fling-them-into-the-fray`, `voice-recruit`/`voice-upgrade` →
  `muster-each-isengard-settlement-or-upgrade-two`, `UseNarya` → `narya`, `UseVilya`/`DeclineVilya` → `vilya`,
  `lords.nenya.replace` → `nenya`, `UseEvilKnowledge` → `evil-knowledge`, `lords.chief.spear-of-terror` →
  `spear-of-terror`, `lords.chief.he-sees-he-knows` → `he-sees-he-knows`.
- **The ring sockets** in the console (`console/ring-<key>`) and **the starting Guide's crown** draw the ring and the
  Guide role themselves rather than an ability; whether they switch to the ability icons is the code's call. The
  chips, the roster, the component buttons and Narya's buttons name the ability and take its icon.
- **The kit's test contract.** `Tests/Editor/UiArtKitTests.cs` `EveryTextureInTheKitIsCoveredByTheContract` fails for
  a kit texture missing from its `Artwork` list, so the 77 files need entries there, each `("abilities/<key>", 28, 28,
  true)` (56 × 56 px, alpha, clamp, no mipmaps, uncompressed — the files meet
  `ArtworkIsRenderedAtTwiceItsLogicalSizeWithUiImportSettings` and its transparent-edge check).
- **Integrated (UI-439).** `AbilityArt` is the one map (it replaced `AbilityIcons` and `AbilityPictures`), and the
  contract takes its keys (`UiArtKitTests.Kit`). Places 1, 2, 4, 5, 6, 7, 9 and 11 above show the ability's own icon;
  one ability's several commands side by side on component buttons differ by a small badge (`AbilityBadges`); a battle
  portrait whose click uses an ability now (Evil Knowledge, White Rider, Sorcerer, place 15) wears its icon; the Hunt's
  Guide response keeps the companion's figure and wears the response's icon as a badge. The ring sockets (8), the
  Politics arrow (10, the nation's step; the ability in its tooltip), the starting Guide's crown (12), the map's ability
  rim (13), the deck (14) and Click to use (3) keep their own art: they stand for the ring, the step, the role, the
  object or the action, not the ability. Details: `docs/ui/UX_SPEC.md`, UI-439.

## Every card ability

The last two columns are the picture before UI-439 (under `Wotr/Art/UI/`) and the new icon.
### base

| Character | Ability id | Title | Rules text | Icon today | New icon |
| --- | --- | --- | --- | --- | --- |
| Gimli (`base.character.gimli`) | `combat-strength-plus-one` | Captain of the West | While participating in a battle, add 1 to the Combat Strength of the Free Peoples army, up to the limit of 5 Combat dice. | `actions/battle` | `abilities/combat-strength-plus-one` |
| Gimli (`base.character.gimli`) | `politics-dwarves-at-erebor` | Dwarf of Erebor | Spend any available Action die while Gimli is in unconquered Erebor to advance the Dwarves Political counter one step toward At War. | `actions/political` | `abilities/politics-dwarves-at-erebor` |
| Boromir (`base.character.boromir`) | `combat-strength-plus-one` | Captain of the West | While participating in a battle, add 1 to the Combat Strength of the Free Peoples army, up to the limit of 5 Combat dice. | `actions/battle` | `abilities/combat-strength-plus-one` |
| Boromir (`base.character.boromir`) | `politics-gondor-at-city-or-stronghold` | High Warden of the White Tower | Spend any available Action die while Boromir is in an unconquered Gondor City or Stronghold to advance the Gondor Political counter one step toward At War. | `actions/political` | `abilities/politics-gondor-at-city-or-stronghold` |
| Legolas (`base.character.legolas`) | `combat-strength-plus-one` | Captain of the West | While participating in a battle, add 1 to the Combat Strength of the Free Peoples army, up to the limit of 5 Combat dice. | `actions/battle` | `abilities/combat-strength-plus-one` |
| Legolas (`base.character.legolas`) | `politics-elves-at-stronghold` | Prince of Mirkwood | Spend any available Action die while Legolas is in an unconquered Elven Stronghold to advance the Elves Political counter one step toward At War. | `actions/political` | `abilities/politics-elves-at-stronghold` |
| Gandalf the Grey (`base.character.gandalf-grey`) | `guide-draw-after-event-die-card` | Guide | After an Event card played with an Event die has fully resolved, if Gandalf is the Guide, you may draw one card from the matching deck. Finish all battles and decisions from that event first; Gandalf may have become Guide during the action. | `fellowship/guide-crown` | `abilities/guide-draw-after-event-die-card` |
| Gandalf the Grey (`base.character.gandalf-grey`) | `combat-strength-plus-one` | Captain of the West | While participating in a battle, add 1 to the Combat Strength of the Free Peoples army, up to the limit of 5 Combat dice. | `actions/battle` | `abilities/combat-strength-plus-one` |
| Gandalf the Grey (`base.character.gandalf-grey`) | `may-transform-to-white` | Emissary from the West | Outside the Fellowship, or after being eliminated, Gandalf the Grey may become Gandalf the White by spending a Will of the West die once a Minion other than the Balrog, Gothmog, the Witch-king — Chief of the Ringwraiths, or any Dark Chieftain has entered play. Replace a surviving Grey in his region; otherwise enter in Fangorn or an unconquered Elven Stronghold, even under siege. | `fellowship/versions` | `abilities/may-transform-to-white` |
| Strider (`base.character.strider`) | `guide-hide-with-any-action-die` | Guide | While Strider is the Guide, you may spend any available Action die to hide a revealed Fellowship. | `fellowship/guide-crown` | `abilities/guide-hide-with-any-action-die` |
| Strider (`base.character.strider`) | `combat-strength-plus-one` | Captain of the West | While participating in a battle, add 1 to the Combat Strength of the Free Peoples army, up to the limit of 5 Combat dice. | `actions/battle` | `abilities/combat-strength-plus-one` |
| Strider (`base.character.strider`) | `may-transform-to-aragorn` | Heir to Isildur | Outside the Fellowship, Strider may become Aragorn by spending a Will of the West die in unconquered Minas Tirith, Dol Amroth or Pelargir. Replace his card while keeping the same figure and location. | `fellowship/versions` | `abilities/may-transform-to-aragorn` |
| Peregrin Took (`base.character.pippin`) | `guide-separate-to-reduce-hunt-one` | Guide | When the Fellowship takes at least 1 Hunt damage, you may separate this Guide to reduce that damage by 1. Choose the new Guide before resolving the remaining damage. Separation on the Mordor Track removes the Hobbit from play. This cannot reduce damage that must be taken entirely as Corruption. | `hunt/eye` | `abilities/guide-separate-to-reduce-hunt-one` |
| Peregrin Took (`base.character.pippin`) | `return-after-fellowship-elimination-except-mordor` | Take Them Alive! | If eliminated while in the Fellowship, this Hobbit immediately returns to play as a separated Companion. This ability does not apply on the Mordor Track. | `characters/may-return` | `abilities/return-after-fellowship-elimination-except-mordor` |
| Meriadoc Brandybuck (`base.character.merry`) | `guide-separate-to-reduce-hunt-one` | Guide | When the Fellowship takes at least 1 Hunt damage, you may separate this Guide to reduce that damage by 1. Choose the new Guide before resolving the remaining damage. Separation on the Mordor Track removes the Hobbit from play. This cannot reduce damage that must be taken entirely as Corruption. | `hunt/eye` | `abilities/guide-separate-to-reduce-hunt-one` |
| Meriadoc Brandybuck (`base.character.merry`) | `return-after-fellowship-elimination-except-mordor` | Take Them Alive! | If eliminated while in the Fellowship, this Hobbit immediately returns to play as a separated Companion. This ability does not apply on the Mordor Track. | `characters/may-return` | `abilities/return-after-fellowship-elimination-except-mordor` |
| Gandalf the White (`base.character.gandalf-white`) | `move-four-alone-or-with-one-hobbit` | Shadowfax | Gandalf may move up to 4 regions when moving alone or with exactly one Hobbit. With a larger group, use normal Companion movement. His level remains 3 for other purposes. | `characters/movement` | `abilities/move-four-alone-or-with-one-hobbit` |
| Gandalf the White (`base.character.gandalf-white`) | `forfeit-leadership-to-cancel-nazgul-for-battle` | The White Rider | At the start of a battle, after any Stronghold shelter decision and before Combat cards, you may forfeit Gandalf’s Leadership to cancel the Leadership of all Nazgûl, including the Witch-king, for the entire battle. Their presence and other abilities are unchanged. | `battle/leadership` | `abilities/forfeit-leadership-to-cancel-nazgul-for-battle` |
| Aragorn (`base.character.aragorn`) | `combat-strength-plus-one` | Captain of the West | While participating in a battle, add 1 to the Combat Strength of the Free Peoples army, up to the limit of 5 Combat dice. | `actions/battle` | `abilities/combat-strength-plus-one` |
| Gollum (`base.character.gollum`) | `enter-and-guide-when-no-companions` | Guide | Gollum joins the Fellowship and becomes its Guide when no Companions remain. He is not a Companion. | `fellowship/guide-crown` | `abilities/enter-and-guide-when-no-companions` |
| Gollum (`base.character.gollum`) | `ignore-standard-numbered-reveal` | Guide · Unseen path | While Gollum is Guide, ignore the Reveal symbol on standard numbered Hunt tiles. Eye tiles and special Hunt tiles retain their normal effects. | `hunt/eye` | `abilities/ignore-standard-numbered-reveal` |
| Gollum (`base.character.gollum`) | `voluntary-reveal-to-reduce-hunt-one` | Guide · Reveal | If the Fellowship is hidden and is not already required to reveal, you may reveal it in a legal destination to reduce Hunt damage by 1, to a minimum of 0. The usual restrictions on revealing the Fellowship still apply. This cannot reduce damage that must be taken entirely as Corruption. | `hunt/eye` | `abilities/voluntary-reveal-to-reduce-hunt-one` |
| The Ring-bearers (`base.character.ring-bearers`) | `add-gollum-when-no-companions` | Master of the Precious | When all Companions have left the Fellowship or been eliminated, add Gollum as its Guide. | `hunt/smeagol` | `abilities/add-gollum-when-no-companions` |
| Saruman (`base.character.saruman`) | `cannot-leave-orthanc` | Bound to Orthanc | Saruman cannot leave Orthanc, including with an army, during retreat or when advancing after combat. He may still lead an attack from Orthanc. | `actions/pinned` | `abilities/cannot-leave-orthanc` |
| Saruman (`base.character.saruman`) | `muster-each-isengard-settlement-or-upgrade-two` | The Voice of Saruman | While Saruman is in Shadow-controlled Orthanc and is not under siege, spend a Muster die to recruit one Isengard Regular in each available unconquered Isengard Settlement, or replace two Isengard Regulars in Orthanc with Elites. Use available reserves and follow normal recruitment and stacking restrictions; a shortage permits only the replacements possible. | `dice/faces/muster` | `abilities/muster-each-isengard-settlement-or-upgrade-two` |
| Saruman (`base.character.saruman`) | `isengard-elites-have-leadership` | Servants of the White Hand | While Saruman is in play, every Isengard Elite also counts as a Leader for movement and combat. It remains a single army unit. | `battle/leadership` | `abilities/isengard-elites-have-leadership` |
| The Mouth of Sauron (`base.character.mouth-of-sauron`) | `muster-as-army-once-per-turn` | Messenger of the Dark Tower | Once per turn, you may use a Muster die as an Army die: move up to two armies, make an attack or play an Army Event card. Spend the selected die as the cost of that action. | `dice/faces/muster` | `abilities/muster-as-army-once-per-turn` |
| The Witch-king (`base.character.witch-king`) | `entry-activates-all-free-peoples` | Terror on entering play | When the Witch-king enters play, activate all Free Peoples Nations. | `actions/political` | `abilities/entry-activates-all-free-peoples` |
| The Witch-king (`base.character.witch-king`) | `draw-after-first-round-combat-card` | Sorcerer | After the first combat round resolves, if the Witch-king is still participating and you played a Combat card in that round, you may draw one card from the matching deck. A cancelled Combat card still counts as played. Finish any round continuation or retreat decisions before drawing. | `dice/faces/card-draw` | `abilities/draw-after-first-round-combat-card` |

### lords

| Character | Ability id | Title | Rules text | Icon today | New icon |
| --- | --- | --- | --- | --- | --- |
| Lord Elrond (`lords.character.elrond`) | `vilya` | Vilya | After you finish an action, you may give Vilya to the Shadow to keep the Action die you used available (not a Will of the West). A kept Fellowship movement die does not go to the Hunt Box. | `icons/ring` | `abilities/vilya` |
| Lord Elrond (`lords.character.elrond`) | `household-of-elrond` | Household of Elrond | Each Elven Elite in Elrond’s region also counts as a Leader for all combat purposes, but not for movement. | `icons/cards` | `abilities/household-of-elrond` |
| Lord Elrond (`lords.character.elrond`) | `combat-card-level-three` | Combat-card level | Treat Elrond as level 3 when resolving Combat card effects. His normal level remains 0. | `actions/battle` | `abilities/combat-card-level-three` |
| Lord Elrond (`lords.character.elrond`) | `cannot-leave-rivendell` | Bound to Rivendell | Elrond cannot leave Rivendell, even with an army. Remove him from play if the Shadow conquers Rivendell. | `actions/pinned` | `abilities/cannot-leave-rivendell` |
| Lady Galadriel (`lords.character.galadriel`) | `nenya` | Nenya | When a standard Eye Hunt tile is drawn, you may give Nenya to the Shadow to cancel it and draw another tile. The cancelled Eye is removed from the game. | `icons/ring` | `abilities/nenya` |
| Lady Galadriel (`lords.character.galadriel`) | `valour-of-the-elven-people` | Valour of the Elven People | You may recruit in Lórien even while it is under siege. | `actions/battle` | `abilities/valour-of-the-elven-people` |
| Lady Galadriel (`lords.character.galadriel`) | `combat-card-level-three` | Combat-card level | Treat Galadriel as level 3 when resolving Combat card effects. Her normal level remains 0. | `actions/battle` | `abilities/combat-card-level-three` |
| Lady Galadriel (`lords.character.galadriel`) | `cannot-leave-lorien` | Bound to Lórien | Galadriel cannot leave Lórien, even with an army. Remove her from play if the Shadow conquers Lórien. | `actions/pinned` | `abilities/cannot-leave-lorien` |
| Gandalf the Grey (`lords.character.gandalf-grey`) | `narya` | Narya | While Gandalf is outside the Fellowship in an unconquered Free Peoples Settlement, give Narya to the Shadow and spend any Action die to activate that Settlement’s Nation and move it straight to At War. | `icons/ring` | `abilities/narya` |
| Gandalf the Grey (`lords.character.gandalf-grey`) | `guide-conditional-narya-die` | Guide · Narya die | At Recover, if Gandalf is Guide and you recover a Fellowship movement die from the Hunt Box, add the Narya die to your Action dice. If he is the starting Guide, add it in turn 1. | `fellowship/guide-crown` | `abilities/guide-conditional-narya-die` |
| Gandalf the Grey (`lords.character.gandalf-grey`) | `may-transform-to-white` | Emissary from the West | While Gandalf is outside the Fellowship or eliminated, you may spend a Will of the West die to replace him with Gandalf the White once a Minion has entered play. The Balrog, Gothmog, the Chief of the Ringwraiths and Dark Chieftains do not count. | `fellowship/versions` | `abilities/may-transform-to-white` |
| Sméagol (`lords.character.smeagol`) | `always-guide` | Guide | When a Sméagol tile is drawn, Hunt damage is 0 and the tile leaves the game; the first one brings Sméagol in as the Guide. Mithril Coat and Sting cannot stop this. If Gollum entered first, discard the tile and draw another. | `fellowship/guide-crown` | `abilities/always-guide` |
| Sméagol (`lords.character.smeagol`) | `level-of-highest-other-companion` | Level X | Sméagol’s level equals the highest level among the other Companions in the Fellowship. He counts as a Companion for the Hunt Box dice limit and may be taken as a Hunt casualty, but is not Gollum for card effects. | `characters/level` | `abilities/level-of-highest-other-companion` |
| Sméagol (`lords.character.smeagol`) | `smeagol-departure` | Departure | Sméagol leaves if he separates, is eliminated or the Fellowship is declared in an unconquered Free Peoples City or Stronghold; We Shall Get It then enters play. If he is ever the only Companion, Gollum replaces him instead. Later Sméagol tiles are discarded and another tile is drawn. | `hunt/smeagol` | `abilities/smeagol-departure` |
| The Balrog of Moria (`lords.character.balrog`) | `fire-and-shadow` | Fire and Shadow | When the Fellowship is declared or revealed and moves into, out of or through the Balrog’s region, or stays in it, draw an additional Hunt tile, ignoring Reveal. An Eye eliminates the Balrog and the Guide (with Gollum as Guide, the Eye is discarded instead). | `dice/faces/use-balrog` | `abilities/fire-and-shadow` |
| The Balrog of Moria (`lords.character.balrog`) | `flame-of-udun` | Flame of Udûn | While participating in a battle, the Balrog adds 2 to the Combat Strength of the Shadow army, up to the limit of 5 Combat dice. | `dice/faces/use-balrog` | `abilities/flame-of-udun` |
| The Balrog of Moria (`lords.character.balrog`) | `balrog-activation` | Activation | A Use Balrog result or A Balrog is Come! activates the Balrog (level 2); after that, each Use Balrog result moves or attacks with his army, or moves him alone. The Free Peoples may deactivate him with a Will of the West, or a Character die if Gandalf is in his region; outside Moria he is eliminated. | `dice/faces/use-balrog` | `abilities/balrog-activation` |
| The Balrog of Moria (`lords.character.balrog`) | `not-minion-for-gandalf-white` | Gandalf the White entry | The Balrog does not count as a Minion for bringing Gandalf the White into play. | `fellowship/versions` | `abilities/not-minion-for-gandalf-white` |
| Gothmog (`lords.character.gothmog`) | `fling-them-into-the-fray` | Fling Them into the Fray | While Gothmog is with a Shadow army in a free region, spend a Muster die to recruit one Sauron Regular there or replace one Sauron Regular there with an Elite. A region where his army besieges a Stronghold counts as free. | `actions/battle` | `abilities/fling-them-into-the-fray` |
| Gothmog (`lords.character.gothmog`) | `not-minion-for-gandalf-white` | Gandalf the White entry | Gothmog does not count as a Minion for bringing Gandalf the White into play. | `fellowship/versions` | `abilities/not-minion-for-gandalf-white` |
| The Mouth of Sauron (`lords.character.mouth-of-sauron`) | `evil-knowledge` | Evil Knowledge | While the Mouth of Sauron is with a besieging army, you may continue the siege battle for one more round without downgrading an Elite. Once per action. | `hunt/die` | `abilities/evil-knowledge` |
| The Mouth of Sauron (`lords.character.mouth-of-sauron`) | `great-sorcery` | Great Sorcery | Once per turn, while the Mouth of Sauron is with a Shadow army, you may use a Muster die as a Character die to move or attack with that army. | `icons/cards` | `abilities/great-sorcery` |
| The Witch-king (`lords.character.witch-king`) | `spear-of-terror` | Spear of Terror | When you play a Character Event card while the Witch-king is in the Fellowship’s region, you may immediately draw a Character Event card. With The Palantír of Orthanc in play, draw only one card in total. | `actions/battle` | `abilities/spear-of-terror` |
| The Witch-king (`lords.character.witch-king`) | `he-sees-he-knows` | He Sees, He Knows | When the Fellowship is declared in a region without an unconquered Free Peoples Stronghold, you may move the Witch-king there at once, without spending an action. | `hunt/eye` | `abilities/he-sees-he-knows` |
| The Witch-king (`lords.character.witch-king`) | `not-minion-for-gandalf-white` | Gandalf the White entry | The Chief of the Ringwraiths does not count as a Minion for bringing Gandalf the White into play. | `fellowship/versions` | `abilities/not-minion-for-gandalf-white` |
| Boromir (`lords.character.boromir`) | `guide-separate-to-reduce-hunt-one` | Guide | While Boromir is Guide, you may separate him during a Hunt to reduce the Hunt damage by 1 (to a minimum of 0). Companions separated against the same tile move together as one group. | `hunt/eye` | `abilities/guide-separate-to-reduce-hunt-one` |
| Boromir (`lords.character.boromir`) | `champion-gondor` | Champion | When Boromir takes part in a battle where the defending army is in a Gondor region or Osgiliath, add 1 to the Free Peoples army’s Combat Strength (still at most 5 Combat dice). | `actions/battle` | `abilities/champion-gondor` |
| Boromir (`lords.character.boromir`) | `messenger-gondor` | Messenger | While Boromir is in a Gondor City or Stronghold, you may spend any Action die to activate Gondor and move it one step toward war. | `actions/political` | `abilities/messenger-gondor` |
| Boromir (`lords.character.boromir`) | `movement-level-two` | Movement level | Boromir moves as level 2; for everything else, including Hunt damage and Combat cards, his level is 1. | `characters/movement` | `abilities/movement-level-two` |
| Gimli (`lords.character.gimli`) | `guide-separate-to-reduce-hunt-one` | Guide | While Gimli is Guide, you may separate him during a Hunt to reduce the Hunt damage by 1 (to a minimum of 0). Companions separated against the same tile move together as one group. | `hunt/eye` | `abilities/guide-separate-to-reduce-hunt-one` |
| Gimli (`lords.character.gimli`) | `champion-dwarves` | Champion | When Gimli takes part in a battle where the defending army is in a Dwarven region, add 1 to the Free Peoples army’s Combat Strength (still at most 5 Combat dice). | `actions/battle` | `abilities/champion-dwarves` |
| Gimli (`lords.character.gimli`) | `messenger-dwarves` | Messenger | While Gimli is in Erebor, you may spend any Action die to activate the Dwarves and move them one step toward war. | `actions/political` | `abilities/messenger-dwarves` |
| Gimli (`lords.character.gimli`) | `movement-level-two` | Movement level | Gimli moves as level 2; for everything else, including Hunt damage and Combat cards, his level is 1. | `characters/movement` | `abilities/movement-level-two` |
| Legolas (`lords.character.legolas`) | `guide-separate-to-reduce-hunt-one` | Guide | While Legolas is Guide, you may separate him during a Hunt to reduce the Hunt damage by 1 (to a minimum of 0). Companions separated against the same tile move together as one group. | `hunt/eye` | `abilities/guide-separate-to-reduce-hunt-one` |
| Legolas (`lords.character.legolas`) | `champion-elves` | Champion | When Legolas takes part in a battle where the defending army is in an Elven region, add 1 to the Free Peoples army’s Combat Strength (still at most 5 Combat dice). | `actions/battle` | `abilities/champion-elves` |
| Legolas (`lords.character.legolas`) | `prince-of-mirkwood` | Prince of Mirkwood | While Legolas is in an Elven Stronghold, you may spend any Action die to move the Elves one step toward war. | `actions/political` | `abilities/prince-of-mirkwood` |
| Legolas (`lords.character.legolas`) | `movement-level-two` | Movement level | Legolas moves as level 2; for everything else, including Hunt damage and Combat cards, his level is 1. | `characters/movement` | `abilities/movement-level-two` |
| Meriadoc (`lords.character.merry`) | `guide-cancel-standard-hunt` | Guide | While Merry is Guide and Pippin is also in the Fellowship, both as Hobbits of the Shire, you may eliminate both to cancel a standard Hunt tile. | `hunt/eye` | `abilities/guide-cancel-standard-hunt` |
| Meriadoc (`lords.character.merry`) | `champion-north` | Champion of the Shire | When Merry takes part in a battle where the defending army is in a North region, add 1 to the Free Peoples army’s Combat Strength (still at most 5 Combat dice). | `actions/battle` | `abilities/champion-north` |
| Meriadoc (`lords.character.merry`) | `art-of-disappearing` | Art of Disappearing | When Merry is eliminated in a battle, return him at once to a region adjacent to his army’s region (not across an impassable border or into a besieged Stronghold). | `hunt/eye` | `abilities/art-of-disappearing` |
| Meriadoc (`lords.character.merry`) | `messenger-north` | Messenger of Elrond | While Merry is in a North City, you may spend any Action die to activate the North and move it one step toward war. | `actions/political` | `abilities/messenger-north` |
| Peregrin (`lords.character.pippin`) | `guide-cancel-standard-hunt` | Guide | While Pippin is Guide and Merry is also in the Fellowship, both as Hobbits of the Shire, you may eliminate both to cancel a standard Hunt tile. | `hunt/eye` | `abilities/guide-cancel-standard-hunt` |
| Peregrin (`lords.character.pippin`) | `champion-north` | Champion of the Shire | When Pippin takes part in a battle where the defending army is in a North region, add 1 to the Free Peoples army’s Combat Strength (still at most 5 Combat dice). | `actions/battle` | `abilities/champion-north` |
| Peregrin (`lords.character.pippin`) | `art-of-disappearing` | Art of Disappearing | When Pippin is eliminated in a battle, return him at once to a region adjacent to his army’s region (not across an impassable border or into a besieged Stronghold). | `hunt/eye` | `abilities/art-of-disappearing` |
| Peregrin (`lords.character.pippin`) | `messenger-north` | Messenger of Elrond | While Pippin is in a North City, you may spend any Action die to activate the North and move it one step toward war. | `actions/political` | `abilities/messenger-north` |
| Strider (`lords.character.strider`) | `guide-first-move-to-fellowship-box` | Guide | While Strider is Guide, if the Fellowship’s Progress is at least 1 when it first moves in a turn, that move’s die goes to the Fellowship Box instead of the Hunt Box. The Hunt still happens. | `fellowship/guide-crown` | `abilities/guide-first-move-to-fellowship-box` |
| Strider (`lords.character.strider`) | `combat-strength-plus-one` | Captain of the West | While Strider takes part in a battle, add 1 to the Free Peoples army’s Combat Strength (still at most 5 Combat dice). | `actions/battle` | `abilities/combat-strength-plus-one` |
| Strider (`lords.character.strider`) | `may-transform-to-aragorn` | Heir to Isildur | While Strider is outside the Fellowship in unconquered Minas Tirith, Dol Amroth or Pelargir, you may spend a Will of the West die to replace him with Aragorn. | `fellowship/versions` | `abilities/may-transform-to-aragorn` |

### kings

| Character | Ability id | Title | Rules text | Icon today | New icon |
| --- | --- | --- | --- | --- | --- |
| Brand — King of Dale (`kings.character.brand.awakened`) | `heir-of-bard` | Heir of Bard | Once per battle, when Brand and a North Army unit take part, you may replace your Combat card’s effect after the reveal: cancel the Shadow Combat card at initiative 0. | `characters/on-board` | `abilities/heir-of-bard` |
| Brand — King of Dale (`kings.character.brand.awakened`) | `call-to-arms` | Call to Arms | With the North At War, spend the Ruler die showing Muster: recruit a North Regular or Leader in each of two free North Settlements and one more in Brand’s region, if free. | `dice/faces/muster` | `abilities/call-to-arms` |
| Brand — Corrupted Ruler (`kings.character.brand.corrupted`) | `corrupted-ruler` | Corrupted ruler | North Leaders and Elites cannot be recruited anywhere, except by Event cards. Free Peoples may remove Brand with a Character result while a Companion with the North or Free Peoples icon is in his region. | `kings/status-corrupted` | `abilities/corrupted-ruler` |
| Brand — Corrupted Ruler (`kings.character.brand.corrupted`) | `shadows-in-the-north` | Shadows in the North | Once per battle in which a North Army unit takes part, Shadow may replace its Combat card’s effect after the reveal: cancel the Free Peoples Combat card at initiative 0. Brand need not be in the battle. | `icons/cards` | `abilities/shadows-in-the-north` |
| Dáin Ironfoot — King Under the Mountain (`kings.character.dain.awakened`) | `dwarvenking` | Dwarvenking | While the Dwarves are At War and Dáin is in Erebor, spend a Muster result to replace one Dwarven Regular in his army with an Elite, even during a siege. | `actions/expand` | `abilities/dwarvenking` |
| Dáin Ironfoot — King Under the Mountain (`kings.character.dain.awakened`) | `iron-guard` | Iron Guard | With the Dwarves At War, spend the Ruler die showing Muster: recruit a Dwarven Regular or Elite in Dáin’s region (if free or besieged) and a Dwarven Regular or Leader in another free Dwarven Settlement. | `dice/faces/muster` | `abilities/iron-guard` |
| Dáin Ironfoot — Corrupted Ruler (`kings.character.dain.corrupted`) | `corrupted-ruler` | Corrupted ruler | Leaders and Elites cannot be recruited in Erebor, except by Event cards. Free Peoples may remove Dáin with a Character result while a Companion with the Dwarves or Free Peoples icon is in his region. | `kings/status-corrupted` | `abilities/corrupted-ruler` |
| Dáin Ironfoot — Corrupted Ruler (`kings.character.dain.corrupted`) | `fate-of-the-lonely-mountain` | Fate of the Lonely Mountain | Once per siege battle at Erebor, Shadow may continue the siege for another round by eliminating one participating Shadow Regular instead of downgrading an Elite. | `icons/effects` | `abilities/fate-of-the-lonely-mountain` |
| Denethor — Lord Steward of Gondor (`kings.character.denethor.awakened`) | `stewardship` | Stewardship | While Gondor is At War and Denethor is in Minas Tirith, spend a Muster result to replace one Gondor Regular in his army with an Elite, even during a siege. | `actions/expand` | `abilities/stewardship` |
| Denethor — Lord Steward of Gondor (`kings.character.denethor.awakened`) | `palantir-of-anarion` | Palantír of Anárion | While Denethor is in Minas Tirith, after you spend the Ruler die showing Event to draw or play an Event card, you may draw one more card from either Event deck. | `dice/faces/card-draw` | `abilities/palantir-of-anarion` |
| Denethor — Corrupted Ruler (`kings.character.denethor.corrupted`) | `corrupted-ruler` | Corrupted ruler | Leaders and Elites cannot be recruited in Minas Tirith, except by Event cards. Free Peoples may remove Denethor with a Character result while a Companion with the Gondor or Free Peoples icon is in his region. | `kings/status-corrupted` | `abilities/corrupted-ruler` |
| Denethor — Corrupted Ruler (`kings.character.denethor.corrupted`) | `fate-of-the-white-tower` | Fate of the White Tower | Once per siege battle at Minas Tirith, Shadow may continue the siege for another round by eliminating one participating Shadow Regular instead of downgrading an Elite. | `icons/effects` | `abilities/fate-of-the-white-tower` |
| Théoden — King of the Riddermark (`kings.character.theoden.awakened`) | `forth-eorlingas` | Forth, Eorlingas! | Théoden has Leadership 3 in the first round of each battle and 1 in later rounds; no Combat card is needed. | `actions/battle` | `abilities/forth-eorlingas` |
| Théoden — King of the Riddermark (`kings.character.theoden.awakened`) | `mustering-of-the-mark` | Mustering of the Mark | With Rohan At War, spend the Ruler die showing Muster: recruit a Rohan Regular or Leader in each of two free Rohan Settlements and one more in Théoden’s region, if free. | `dice/faces/muster` | `abilities/mustering-of-the-mark` |
| Théoden — Corrupted Ruler (`kings.character.theoden.corrupted`) | `corrupted-ruler` | Corrupted ruler | Free Peoples Leaders and Elites cannot be recruited in any region of Rohan, except by Event cards. Free Peoples may remove Théoden with a Character result while a Companion with the Rohan or Free Peoples icon is in his region. | `kings/status-corrupted` | `abilities/corrupted-ruler` |
| Théoden — Corrupted Ruler (`kings.character.theoden.corrupted`) | `mark-of-the-white-hand` | Mark of the White Hand | While Shadow controls Helm’s Deep, it is an Isengard Stronghold but still counts as a captured Free Peoples Stronghold for victory points. Removing Théoden ends this at once. | `characters/flight` | `abilities/mark-of-the-white-hand` |
| Thranduil — King of the Woodland Realm (`kings.character.thranduil.awakened`) | `elvenking` | Elvenking | While the Elves are At War and Thranduil is in the Woodland Realm, spend a Muster result to replace one Elven Regular in his army with an Elite, even during a siege. | `actions/expand` | `abilities/elvenking` |
| Thranduil — King of the Woodland Realm (`kings.character.thranduil.awakened`) | `far-sighted` | Far-sighted | While the Fellowship is off the Mordor Track, spend the Ruler die showing Character: move the Fellowship, and Shadow rolls one Hunt die fewer (minimum one); or move or separate Companions one region further. | `characters/movement` | `abilities/far-sighted` |
| Thranduil — Corrupted Ruler (`kings.character.thranduil.corrupted`) | `corrupted-ruler` | Corrupted ruler | Elites cannot be recruited in the Woodland Realm, except by Event cards; Leaders still can. Free Peoples may remove Thranduil with a Character result while a Companion with the Elves or Free Peoples icon is in his region. | `kings/status-corrupted` | `abilities/corrupted-ruler` |
| Thranduil — Corrupted Ruler (`kings.character.thranduil.corrupted`) | `fate-of-mirkwood` | Fate of Mirkwood | Once per siege battle at the Woodland Realm, Shadow may continue the siege for another round by eliminating one participating Shadow Regular instead of downgrading an Elite. | `icons/effects` | `abilities/fate-of-mirkwood` |
| The Black Serpent — Chieftain of the Haradrim (`kings.character.black-serpent`) | `red-wrath` | Red Wrath | When the Black Serpent is in a battle, before the reveal you may give up your Combat card’s effect and take one hit: +1 to your Combat roll at initiative 3 (+2 if it is Relentless Assault). | `actions/battle` | `abilities/red-wrath` |
| The Black Serpent — Chieftain of the Haradrim (`kings.character.black-serpent`) | `not-minion-for-gandalf-white` | Dark Chieftain | A Minion for all rules and cards, except Gandalf the White’s entry condition; he moves like a non-flying Minion. While a Dark Chieftain is in play, Shadow has one shared Ruler die, from the next Recover. | `fellowship/versions` | `abilities/dark-chieftain` |
| The Shadow of Mirkwood — Chieftain of the Dark Lord (`kings.character.shadow-of-mirkwood`) | `lord-of-the-bats` | Lord of the Bats | When the Shadow of Mirkwood is in a battle, before the reveal you may give up your Combat card’s effect and his Leadership: +1 to your Leader re-roll at initiative 4. With Swarm of Bats, also cancel the Free Peoples card at initiative 0. | `hunt/die` | `abilities/lord-of-the-bats` |
| The Shadow of Mirkwood — Chieftain of the Dark Lord (`kings.character.shadow-of-mirkwood`) | `not-minion-for-gandalf-white` | Dark Chieftain | A Minion for all rules and cards, except Gandalf the White’s entry condition; he moves like a non-flying Minion. While a Dark Chieftain is in play, Shadow has one shared Ruler die, from the next Recover. | `fellowship/versions` | `abilities/dark-chieftain` |
| Uglúk — Chieftain of the Uruk-hai (`kings.character.ugluk`) | `i-command` | I Command | When Uglúk is in a battle, before the reveal you may give up your Combat card’s effect: at initiative 3, both armies get +1 to their Combat roll and Leader re-roll (only Shadow if the card is Desperate Battle). | `actions/battle` | `abilities/i-command` |
| Uglúk — Chieftain of the Uruk-hai (`kings.character.ugluk`) | `we-march-day-and-night` | We March Day and Night | While Uglúk is in or adjacent to the Fellowship’s region, even if it is hidden, each Hunt die the Shadow re-rolls gets +1; the first Hunt roll does not. | `characters/movement` | `abilities/we-march-day-and-night` |
| Uglúk — Chieftain of the Uruk-hai (`kings.character.ugluk`) | `not-minion-for-gandalf-white` | Dark Chieftain | A Minion for all rules and cards, except Gandalf the White’s entry condition; he moves like a non-flying Minion. While a Dark Chieftain is in play, Shadow has one shared Ruler die, from the next Recover. | `fellowship/versions` | `abilities/dark-chieftain` |

### fate

| Character | Ability id | Title | Rules text | Icon today | New icon |
| --- | --- | --- | --- | --- | --- |
| Gimli (`fate.character.gimli`) | `combat-strength-plus-one` | Captain of the West | When Gimli takes part in a battle, add 1 to the Free Peoples army’s Combat Strength (at most five Combat dice). | `actions/battle` | `abilities/combat-strength-plus-one` |
| Gimli (`fate.character.gimli`) | `politics-dwarves-at-fate-homelands` | Dwarf of Renown | While Gimli is in the Iron Hills or Ered Luin, even if the Shadow controls it, you may spend any Action die to move the Dwarves one step toward war. | `actions/political` | `abilities/politics-dwarves-at-fate-homelands` |
| Gimli (`fate.character.gimli-dwarven-lord`) | `guide-separate-to-reduce-hunt-one` | Guide | When a Hunt tile is drawn while Gimli is the Guide, you may separate him to reduce its damage by 1 (minimum 0), unless that damage must be taken as Corruption. | `hunt/eye` | `abilities/guide-separate-to-reduce-hunt-one` |
| Gimli (`fate.character.gimli-dwarven-lord`) | `champion-dwarves` | Dwarven Champion | When Gimli takes part in a battle in a Dwarven region (not Erebor), attacking or defending, add 1 to the Free Peoples army’s Combat Strength (at most five Combat dice). | `actions/battle` | `abilities/champion-dwarves` |
| Gimli (`fate.character.gimli-dwarven-lord`) | `messenger-dwarves` | Messenger of Elrond | While Gimli is in the Iron Hills or Ered Luin, even if the Shadow controls it, you may spend any Action die to activate the Dwarves and move them one step toward war. | `actions/political` | `abilities/messenger-dwarves` |
| Gimli (`fate.character.gimli-dwarven-lord`) | `movement-level-two` | Movement level | Gimli moves as Level 2 but is Level 1 for everything else, such as Hunt casualties. He has no nation icon and may start in the Iron Hills or Ered Luin. | `characters/movement` | `abilities/movement-level-two` |
