<a id="walki-powiadomienia-i-dziennik-akcji"></a>
# Combats, notifications and the action journal

Plan agreed 2026-10-07. UI-37 receives documentation preparation; UI-38–UI-47 realizes data, client and integration. Status and evidence remain only in Register. This document does not confirm implementation.

<a id="cel-i-granice"></a>
## Purpose and boundaries

The player understands the opponent's action without comparing current stacks and guessing the cost. One click of the red tab opens the corresponding UI with a full explanation: who, what paid, what he did and what the result was. The same card action occurs in the switched log of the entire batch.

The REQUIREMENTS R05–R25, UX_SPEC U08/U11 and the existing rules LOGIC/EXPANSIONS. UI and the code are in English, documentation in Polish. The replacement graphics are interchangeable. Notifications only in the application; no new rules, AI, external communicators and publications.

The task preparation order does not start UI-38–UI-47. UI-35 retains its acceptance. New client changes begin after it is completed; this condition is saved with UI-41. We do not overwrite scenes or builds of the user.

<a id="karta-akcji--wspólna-prezentacja"></a>
## card action — a joint presentation

Target example, with neutral icons:

```text
SHADOW · Army movement                     [Army die]

2× ● Regular 3× ◆ Elite
Morannon → Dagorlad

Result: Army moved
```

- The contractor, cost, operation, participants, places and actual result are readable without
  further opening details after clicking the alert. Companion symbols; tooltip is not the only
  explanation.
- We group individuals by nation and kind. Commanders, Nazgûle and named characters remain
  distinguished. The historical version of the character does not change after its subsequent
  transformation.
- The cost shows a specific type of resource and application. Will or Army/Muster does not turn into
  a description in the usual die Army. Tokens, Rings, Capacities, Cost of card and free reactions
  have their own description.
- One paid action has a cost header and chronological steps. The common cost is displayed once; the
  additional response cost is included in this reaction. Each step has a real contractor, regardless
  of the initiator action.
- action continues as In progress to complete the proper continuation. The first movement of the
  army is already made, even when the second move remains or Finish. We do not show future effects
  as a result.
- The revealed effects, e.g. gaining settlement, loss, scoring and politics, have their own short
  lines. Inside battle we clearly separate the start of the attack, the outcome of the round and the
  actual entry.
- The source of the saved facts is approved server events. Current stack in the region, current dice
  wall and text history do not reproduce what has been moved or released in the past.

<a id="dziennik-log"></a>
## Journal Log

The Log button replaces the History input and switches the separate window at the left edge of the map, above the dice tray. By default closed. The header moves the window according to UI-32; the position, development, filter and scrolling place remain in session. The log closes not open at the card action object.

- Default All Filter; other Opponent and You. Entries have the turn designation and are from the
  latest, steps of one action from the oldest to the latest. Filtering concerns the step performer;
  the response of the opponent in our action retains the visible context header and the correctly
  assigned cost.
- Log includes approved actions of both players, refusal/Finish, reactions and automatic
  consequences. The system effect has a source and does not pretend to be an additional action from
  any player. Private preferences of UI, sketches, previews and rejected command do not create
  gameplay entries.
- The compact entry shows the cost, operation, participants and place. Click or Enter/Space extends
  the course or opens the full card at the appropriate UI. The cursor departure does not close the
  open view. Hover/fokus only provides a passive description of the already downloaded data; it does
  not read history or signify entry as read.
- Older entries read during scrolling. The common list model does not limit history to 50 recent
  events and the steps that are still in place action When connecting pages.
- The new entry does not stop reading the older passage. We keep anchor and focus, and New entries
  allow you to go to the latest one. We do not play all controls on each revision or draw the entire
  story.
- The opening of the log/filter and the hover does not mean reading the alerts. The open of the
  entry means reading only the displayed version of these events, as well as clicking the
  appropriate red tab.
- Past entries without metadata remain textual. No author is guessed from the text: such entry
  occurs in All, not in the filters of the performer. It does not create a fictional route, cost or
  new alert.

<a id="zakładki-powiadomień"></a>
## Notification Bookmarks

The narrow bar below the top bar does not change the geometry of the map on each message. The golden decision first, further to the two newest red tabs; in a narrower one. Remaining under +N, with the list opened by clicking or Enter/Space. Sam hover/fokus does not develop it, leaving does not close it. The list does not mean messages as read.

| Type | Example | Life Cycle |
| --- | --- | --- |
| Red, the enemy's action | Shadow moved 2 Regular + 3 Elite · Morannon → Dagorlad | To open the summary openly. |
| Gold, your own decision | Assign 2 hits · Minas Tirith | To decide the decision by the server. |

The color always has a symbol and text. The basic description is visible without clicking. From UI-80 tooltip to 200 ms howera or immediately after seal shows only the existing allowed metadata. There is no controller, no gesture capture, no data download, and no choice, no camera or alert reading. Departing, losing focus or starting gestures extinguishs the description. Esc extinguishs it and continues to close the active window. The panels retain a dragable header, position in the session and Close/Esc according to [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md).

- Reds are created for the actions of the opponent of the visible recipient. Private settings for
  the omission of cards do not create an alert; the actual response of the game caused by this
  setting is subject to normal visibility rules.
- Unread steps of the same action are grouped into one tab; in battle the key of context is
  BattleId. The tab gives the last step and number of new steps, card shows the unread part with
  context.
- The first click opens card, the appropriate panel and the known sites of action; it only removes
  the shown version of the alert. The later step creates a new message. Time, hower, automatic panel
  opening and general match seen do not delete it.
- Gold results from the current own Decision and legal options, not from the log. Shows a specific
  action/number, and for the usual Action: Choose an action. Click once again solves the current ID
  decision and opens its controls. Bookmark remains after the window is closed, command error and
  offline. It disappears after the decision is resolved or the game is over.
- We keep the Fellowship and Hunt windows automatically open without adding camera movement; their
  Location is still a conscious operation. Notification does not open battle itself or play the
  animation motion.
- Reading alerts locally per server/account/game as ID/Versions , separately from MatchSeenTracker And
  snapshots of the game. Reconnect/restart This client will not resurrect the read version. Another
  device has its own read tags; we do not sync them through the public state of the batch.

<a id="podsumowanie-przy-ui-i-mapa"></a>
## Summary with UI and map

The red tab or log entry click shows immediately card action at its host: region, battle, Fellowship, Hunt, card, policy, form or resource. It does not force the log to open. Historical summary and current controls have separate signatures; the old entry never becomes command or the current source of legality.

- The movement summary distinguishes the known source and target. Names, moved composition and order
  of the saved episodes remain in the action tab; the map does not draw arrows or floating
  descriptions. The open can fit the frame within the map limits; the normal refresh does not move
  the camera.
- The attack distinguishes a known source and target, without an arrow suggesting a trespass. Other
  activities distinguish the right objects. No location opens the panel without creating a point on
  the map.
- The markings remain to close the summary, change entry or batch. They are not legitimate targets,
  do not capture the game gestures and do not move the actual figures. During the active selection
  target/drag are extinguished to not cover the current decision; card historical retains its
  description.
- Completed battle opens the correct report after BattleId, also when another battle began later in
  the same region.
- Opening the old Hunt shows saved facts of a given chain, does not replace them with the latest
  HuntReport. Views without active mechanics provide a clear historical preview in its context.
- Late replies from the previous alert/game do not replace the preview. Download error shows
  available description and Retry; without pretending to read the unloaded part and without
  performing command gameplay.

<a id="okno-walki"></a>
## Fight Window

- Click or Enter/Space on the battle region or notify opens a window; your new decision retains the
  existing auto-open. Hover site only gently highlight the region, without description and panel.
  The cursor exit does not close the open window; drag/pan does not open the passing battles. The
  close retains the legal draft; the header moves the window with the memory of the position in the
  session.
- Header: region, Field/Siege/Sortie/Relief, round and decision owner. Attackers on the left,
  defender on the right. Units, characters, Leadership, dice, hit threshold, cards/reverse and
  modifiers are clear. The garrison and left figure do not mix with participants.
- Entry: die → army → participants → target. Legal final click/drop performs the step without
  additional Confirm. The attack itself does not move the army.
- card Combat are inside the window; miniature + combat text and passive description of the known
  content on hover/fokus. Full card with controls opens with a click or Enter/Space. Play or a
  double, separate No card. Only one set of command controls; the usual hand can still be viewed.
  Covered choices remain covered for the proper disclosure window.
- The throws show a natural score, bonus, hit and result of the reroll. Selection of eligible
  dice + Reroll selected or Keep roll. The other marking of the reroll remains private. Special
  attacks have named sources and retain their own order.
- Regular: keep/remove. Elite: keep/downgrade/remove/keep. Visible legend, order of operations,
  assigned/remaining and consequences for replacements/commanders. One Apply looses approves the
  set. No regular can mean removing the elite for one hit: we show the actual preview, not the
  permanent establishment of "elite = two".
- Continue/Stop/Hold are direct; retreat indicates the region of the map. Enter: the membership of
  the participants → target or Stay. Extension of siege: the choice of elite shows the actual cost,
  the two-click approves a single legal choice; aggregate decisions retain one approval.
- The stepbar is informative. Nested reactions, event losses, card and expansions have their
  windows; we do not overlook strategic choices. The uncertain sent command always has a recovery
  mechanism available for the receipt.
- The battle report shall contain all available rounds and their costs, card after disclosure,
  projections, losses, follow-up decisions, retreat/intrusion, termination, control and points at
  the actual time of their resolution.

Illustrated frame of this window ( pass scenes, strength framework, figure , medallions, parchment card , dice d6, loss tokens, report chronicle) and general rule of state of pressing are in [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md) , UI-113 – UI-121 The behaviour described above remains unchanged.

<a id="dane-widoczność-i-odpowiedzialność"></a>
## Data, visibility and responsibility

UI-38 defines a permanent record of events and a type of DTO list, step and action: stable ID, action/step/parent, contractor, turn/order, context, cost and fact. Contract details go to CONTRACTS before implementing dependent views. Journal, card, bookmarks and map use one source, without independently recording the same commands in Unity.

The preview, rollback and retry do not match the steps performed. Root action and return after reaction survive the reboot; we do not remove the identity of action from the current view. The server filteres the accepted facts before the pagination and counting of the meters. Hidden payoad, other people's choices, future RNG and private timestamps do not get to the client. Later disclosure creates the right open step.

The reading has pages of older entries, downloading new steps and specific action. The reading of the whole story does not enlarge every PlayerView projection. New steps of the existing group cannot disappear between pages. UI-40 adds a permanent reading of the report after BattleId; LastBattleRound is not a battle archive. Old entries work further: unknown earlier stages remain unknown, without playing from text, new draw or reset the game.

| Family (owner of the cover: UI-39) | Facts and context needed |
| --- | --- |
| turn, dice, pass and Finish | Contractor, proper wall/applicability, remaining budget and state of completion. |
| Army and character movement | Actual composition/nations/Versions, source, route, target, common cost and impact of entry. |
| Recruitment and policy | figure, regions, nation, cost, activation and actual track change. |
| card and reactions | Public source/target, expenditure, choice and effect; selection/discharge without other people's identities cards. |
| battle and fortifications | Participants, mode/Round, outright card, throws, losses, decisions and link BattleId; report develops UI-40. |
| Fellowship | Public Progress/stan/position, declaration/disclosure, Guide, Separation; No imaginary secret route. |
| Hunt and Mordor | Chain Source, Public rolls/tiles, Reactions, casualties/corruption, Reveal/Stop; No Future Pool. |
| Lords and Council | Special dice, Rings, Characters and Tokens; private versions only for the authorized. |
| Kings, Dismay and variants | Ruler, sovereign, token, test Corruption, legally public tile and effects. |
| Fate, New Cities and the End game | Relevant regional definitions/figures, control/points, victory; no change in rules. |

<a id="scenariusze-i-właściciele-odbioru"></a>
## Screenplays and owners acceptance

The owner prepares the evidence in his task; UI-47 takes the joint course. No row is credited with the existence of this table alone.

| ID | Owner | Observable scenario |
| --- | --- | --- |
| BA01 | UI-38 | Stable zapis/kursor: preview without entry, commit once, retry/rollback/restart, over 50 entries and legacy. |
| BA02 | UI-39 | All families action, 2 Regular + 3 Elite, two steps behind one die, reaction of the other side, cost and privacy. |
| BA03 | UI-40 | Several battles/round, ending before throw, retreat/enter, later effects and reading the correct BattleId. |
| BA04 | UI-41 | The same card action: text + symbols, nations/characters, correct cost and unchanged old composition. |
| BA05 | UI-41 | Toggle/filters, the pages of the whole story, a new step of the old group, stable scroll/fokus and New entries. |
| BA06 | UI-42 | One click of the entry opens the appropriate UI with full description and static markings; an old report and no location. |
| BA07 | UI-43 | Red grouping/+N/reading a particular version, a newer event during a click, reconnect and independence from the seen. |
| BA08 | UI-43 | The Golden Decision lasts after click/Close/b power/offline; disappears after settlement; auto-open Fellowship / Hunt preserved. |
| BA09 | UI-44, UI-80 | Click/Enter/Space opens the window, hover 200 ms/fokus only a passive description of objects; drag/Close, the composition/back guard/garage and card Combat inside, without revealing secrets. |
| BA10 | UI-45 | Natural throws, bonuses, special attacks, choice of both sides to metastatic and one approval. |
| BA11 | UI-45 | The cycles of loss, order, lack of replacement, death of leaders, mutual destruction and loss of event. |
| BA12 | UI-46 | Continue/Stop/Hold, legal retreat, partial entry/Stay and actual kontrola/punkty. |
| BA13 | UI-46 | Field/Siege/Sortie/Relief, shelter, garrison limit, elite extension and odwrotu/wkroczenia. restrictions |
| BA14 | UI-47 | Combined run with reactions and card in all eight configurations and relevant variants of R03. |
| BA15 | UI-47 | Two clients/ed stems, lost receipt, old click, reconnect/restart , end and older record in half action. |
| BA16 | UI-47 | Four layouts, hit-test/fokus, no mandatory scroll decision, long log stability, R23 and local builds. |

<a id="weryfikacja-i-przekazanie"></a>
## Verification and transmission

Each task gives future instructions .NET/Unity and the range of scenarios; an empty test list does not mean PASS . UI-47 requires real HTTP/WSS and PostgreSQL , two clients, drop review 1366 × 768/1920 × 1080 with 100/125% , feedback ≤ 100 ms and 60 FPS The scrolling is allowed in the historical log, is not the required step of the current decision. Full text and accurate values remain available.

Results, commands, limitations and commits go to the job specification, SCENARIOS/CONTRACTS/ARCHITECTURE and register. Builds in separate folders; Windows runtime, manual acceptance the user and the target host remain separate, uncounted trials according to the existing scope.

<a id="integracja-z-nowym-układem-hunt--ui-53"></a>
## Integration with new Hunt system — UI-53

[Plan Hunt](HUNT_UX.md) and UI-53 use ready-made UI-38–UI-43. They do not duplicate the action, log component, bookmarks or read tags. The automatic result does not open Hunt and does not create text at the Eye. The system-specific effect of action goes into its entry; the red alert still requires visible action of the opponent, gold of the current decision. The decision window keeps auto-open.

The red tab click shows immediately the full card at Hunt, with source, cost and known effects. The old string is not replaced by the latest HuntReport; the historical view does not perform command. UI-53 receives H11/H12, and UI-54 regresses all after the finished UI-47.

<a id="ba01--fundament-dziennika-ui-38"></a>
## BA01 — the foundation of the UI-38 log

Implementation descriptions and results below UI-38 – UI-72 document the condition of 2026-10-08 UTC before UI-80 They keep historical gestures hover/pin and actual results; the current opening standards are in sections above, and BOARD_INTERACTION_UX These results are not acceptance new behavior of tooltips and windows.

API , typical models and cursors define [CONTRACTS](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/CONTRACTS.md) The source is the existing GameEvent with fixed metadata and context action Two. marches/Finish read older pages and grow filter secrets before you limitami/rodzicami/licznikami. Older texts don't get guessed facts. `ActionLog*Tests` include actual movement, > 120 entries, 5000 entries for measurement, legacies, migration, privacy and process restart Kestrel/PG. Evidence and limitations: UI-38 The foundation itself does not count BA02 neither views UI-41 – UI-47 ; other families are within the scope UI-39 .

<a id="integracja-pozostałego-ui-partii--ui-62"></a>
## Integration of the remaining UI batch — UI-62

[IN_GAME_UX](IN_GAME_UX.md) and UI-62 connect Effects, resources, sovereigns and Victory to the existing card action the logo and red/ed The following task depends on the UI-43 and UI-53 ; does not alter the scope or dependence UI-38 – UI-47 Historical cost/target/result comes from a fixed entry, and the gold tab leads to the current only decision host. G16/G17 supplement BA without third-type notifications and without duplication of recorded facts.


<a id="ba02--producenci-i-dowody-ui-39"></a>
## BA02 — Producers and Evidence UI-39

Each command has a clear context in ActionCommandCatalog and an English description of ActionCommandText. The UI-41 preview will use Facts instead of analyzing Text. The following tests check new data, not only the rule result.

| Family | Manufacturer / data | Normal and border proof |
| --- | --- | --- |
| Turn/kows/pas/Finish | HistoryCapture, TurnFlow.Phase, ActionBoardChanges.Dice/Budget | ActionLogCardFactsTests: pass and the End of turn; ActionLogProjectionTests: Second Step, Finish/legacy and the Original Will |
| Movement and characters | MovementFacts and Army Producers, ShadowTravel, CompanionRules and cards | ActionLogProjectionTests: 2 Regular + 3 Elite, two steps; ActionLogCardFactsTests: Mixed Army after the death of the character, Dead Men and Eagles |
| recruitment/politics | RecruitmentRules, PoliticalRules and command limit | ActionLogFamilyTests: Regular/Elite/lonely Leader; active and passive tor |
| cards/reaction | CardFlow, EffectContext, EffectRuntime, public table changes | ActionLogFamilyTests: Selection and recoil of excess; ActionLogHuntFactsTests: Mithril/Axe; RegionProtectionCardTests and LordsWeWontGoBackTests: Hidden card as cost |
| Battle | BattleRuntime, BattleRoll, BattleCards, BattleLosses, BattleMovement | ActionLogBattleFactsTests: commitment/reveal, private reroll, Regular as replacement, retreat/partial progress; SiegeTests: zero rounds and costs of subsequent rounds |
| Fellowship | Public score, CompanionRules and HuntReports | HuntReportTests with AssertReport: changing the Guide before Corruption, revealing and additional tiles; ActionLogHuntFactsTests: no hidden motion route |
| Hunt/Mordor | HuntReports: copy of entries and chain closure | ActionLogHuntFactsTests: 0/1 dice, preserved former string and two wins; HuntReportTests: Stop/Reveal/treatment |
| Lords/Council | Facts dice, Rings, Versions and Tokens | ActionLogExpansionFactsTests: Narya/Vilya, Politics/Travel; ActionLogPrivacyTests: two secret versions of the Council with identical public logo |
| Kings/Dismay | public sovereigns, private tile, HistoryCapture costs | ActionLogExpansionFactsTests: four variants Corruption, both Dismay goals, Ruler Character→Muster and separate token |
| Fate/won sage | control/Outcome, existing configurations board | ActionLogExpansionFactsTests: gaining Erebor and own Iron Hills; ActionLogFamilyTests: all eight configurations; Mordor: score only after injury |
| HTTP/persistence/poufnodc | common projection, stable HistoryId and index | ActionLogPrivacyTests: real accounts and reboot after private reroll, All/You/Opponent, page/Details/number/kursory; ActionLogPersistence/Paging/RestartTests: retry, rollback, legacies, old groups and >120 entries |

The manufacturer does not turn into a second rule engine. New facts are expansions to the existing event and the same transaction. Nested decisions retain its own contractor and group action to the actual end. Multi-run report, card action, panels and acceptance ergonomics remain in the corresponding UI-40-UI-47; this matrix does not count them.


UI-40 provides backend reports after BattleId : Rounds and public steps, distinction of the end battle and settlements of the whole action , snapshots regional forces with garnisone/stra They have a back and control/points. CONTRACTS . Final arrangement figures read from Place and the contestants and round kicks from her snapshots; we do not count the former battle from the current map. LastBattleRound The older record remains an open part of the passage. acceptance not yet count the report interface UI-42/UI-46.

<a id="wdrożenie-ba04ba05--ui-41"></a>
## implementation BA04-BA05 — UI-41

Log replaces History; by default it is closed. All/You/Opponent have separate scrolling and anchors. Older and scrolling to the end read the pages, New entries lead to the start. Offline and error GET retain the downloaded data; Retry does not perform command game.

card retains its historical wall and application of cost, nationality, type and version of the character, route and recorded effects. Additional cost belongs to the responder. In progress also takes time to remove excess after Finish. No legacy data is marked.

Hover/fokus opens temporary card au 200 ms, leaving the entry and panel closes after 150 ms; click clip. Log and card use UI-32 . pinned card does not automatically go to the newer version; the Log closes it not. At the border UI-41 card is next to Log; the target context and map are the task UI-42 and the reading of notifications UI-43 .

<a id="wdrożenie-ba06--ui-42"></a>
## implementation BA06 — UI-42

Open click on the entry to fasten card at the host saved action Log remains open; its closure does not close card . External opening through the router does not open Log. Description and cost are historical, and current legal controls retain draft. Armies and characters use the saved regions and definitions; no location does not set the default target.

The map shows static sections of the route with stage number and different attack sign. We do not move figures; active target/drag/pan extinguishs the markings. Close or other action removes them. Click can frame familiar places, hower or new projection do not change the camera. Summary battle contains archival composition and result before the list of steps. The former battle remains the former BattleId after a new fight in the same region; Hunt does not use the latest HuntReport.

Full card is limited to the version intercepted by clicking. Errors and offline retain the facts available, show Retry and do not confirm missing details. acceptance and actual results are found in UI-42.

<a id="wdrożenie-ba07ba08--ui-43"></a>
## implementation BA07-BA08 — UI-43

The fixed bar under the header contains the gold of the current decision and 1/2 red tabs, depending on the logical width. The remaining ones are under +N with the hoverem/fokusem drop-down list and the buttoned-up. pinned window can be moved. The booking of the space does not change the map area when the message appears or reads.

The red tab describes the last visible step of the opponent and the number of new steps of the group. Hover does not move the camera or confirm the reading. Click opens the common card and context UI-42 without opening Log; click on Log confirms the same facts. Error/Retry keeps the message, and the new step while reading remains on the bar. The read status is local to the server, account and batch, separate from match seen.

Gold shows a specific decision and the required numbers from the server. Close/offline does not mean a solution. battle still does not open itself; the current auto-open Fellowship and Hunt stay. Current controls are solved again after clicking. UI-44 will place Combat inside the same host battle. Results: UI-43.

<a id="wdrożenie-ba09--ui-44"></a>
## implementation BA09 — UI-44

The regional window uses the common Hover 200/150 ms. Moving from the region to the window and preview card retains reading; pan/drag does not open the passing battles. Click/Selection attaches, the header moves, Close retains draft and remains respected for the next projection. The new battle does not open the window or take over the camera. The usual view of the region does not duplicate battle.

The header defines the region, mode, round and decision owner. Both columns separate participants from the rear guard and non-participating garrison; strength, Leadership, number of dice and threshold are server values. The details of modifiers and characters are in tooltips. The information step bar does not ignore the response or costs of cards.

Combat selection is located in this window: miniatures with combat text, full preview card , Play/double-click/drop and separate No card. The usual hand during Combat is readable, without second play controls. The covered opponent remains the reverse; bound/archive card are read without Play. Gold decision opens battle Red opens a common card UI-42 and current window only with compatible BattleId Results and limitations acceptance : UI-44 .

<a id="realizacja-ba10ba11--ui-45"></a>
## BA10-BA11 — UI-45

The battle results use the meshes, hit flags and the signed modifier. The die flip-off retains both results; tooltip shows the threshold, bonuses and card. Special sources have Combat effects names and separate pages of five dice; signature sums up source hits, regardless of the page viewed. Own reroll selects only authorized own dice, from zero to server option limit. Keep roll/Reroll selected sends one set after conscious click.

At losses, you will see the cycle legend, the operation number and the assigned/remaining. Legal preview describes each operation and regular origin. Removing the elite by Downgrade without replacing costs one hit (BASE p.29). When the army is exhausted, the remaining hits are described as excess hits. Apply loses accounts a set of one command. Regular removed earlier can be reused by later Downgrade; decides the server. The death of the commanders is the result of the actual cause of the loss, not the assumption of UI.

<a id="realizacja-ba12ba13--ui-46"></a>
## BA12-BA13 — UI-46

Continue/Stop/Hold, the goal of retreat and partial entry retains the direct gestures of the existing sender. The control of the region changes only after command the entry; the recovery of the response does not repeat the cost or POST. The cost of ExtendSiege is a separate preview: the replacement of the elite, removal without replacement or regular Kings; the loss of the last figure the combat clearly announces the end of battle. The two-click of the selected authorized figure determines the cost, Stop remains a separate.

The shortcut of the last cast Combat takes the entire report after BattleId. battle without the throw is available with Log/notifications by View battle rounds; it does not replace LastBattleRound. The report retains its own ID/wersj and the selected round after the reconconnect, it does not give controls of the previous decisions. Status, saved steps, control and points come from the server. No kick is described directly. History can scroll; decisions Continue/Retreat/Advance/ExtendSiege do not require a scroll.

<a id="domknięcie-integracji--ui-47"></a>
## Integration Lock — UI-47

The full run combines two marches, card with both players, natural rolls/casualties, continuation, retreat and entry with the same log, alerts and report. The alert approved command includes its automatic effects in the same revision; it does not reveal later decision. The attachment of the open window and closing the preview during drags do not move the target.

The revision of the context of the pages and empty regions remains covered by acceptance. The BA01–BA16 evidence map, previous failures and actual re-enactments are saved in UI-47. This task also stores performance/builds and limits of Windows runtime, Intel, handheld acceptance and target host. The new plans of UI-49+ have a separate range.

<a id="ui-53--następstwa-klikniętej-wersji"></a>
## UI-53 — consequences of the clicked version

The opponent's alert includes the automatic consequences of his step in the same revision, without expanding to a later decision. Log reading continues to indicate the exact version. The Hunt facts remain part of the action source: cost of dice in the header once, actor and response cost in the appropriate step of the chronology. The compact HuntArchiveSummary does not have game controls and selects the saved string. The GET error retains the red alert, and the Retry and reboot play the readout limit.

<a id="ui-54--odbiór-polowania-i-integracji"></a>
## UI-54 — acceptance Hunt and integration

A short alert recognizes the source by a public library of the appropriate kind, without treating each DefinitionId as a character. Known card and characters retain titles; tiles and dice have clear names, unknown neutral sources fallback. H12 re-enters log, read versions, old string during the new Hunt and enemy response alarm in its own action.

<a id="historyczni-gospodarze--checkpoint-ui-62"></a>
## Historical hosts — checkpoint UI-62

InGameActionContext adds the anchors Effects, resources, sovereign and Victory only with ActionLogView The preview of the selected step can include automatic facts of the same revision with Actor=null. It does not reconstruct events from the current one. PlayerView Not a log text. InGameResourceText called the preserved Rings, Tokens and Special Sources dice , without telling me the hidden cards cost. Source field and gold instruction use common public names Council/sovereign they do not establish their former status from the current board One router and one card remain common to history and red bookmarks. Manually open the current battle by golden abbreviation still indicates its region; the decision-taking itself does not move the camera. Regressions of reading, verseing and actors and their actual results: UI-62 .

<a id="postać-jako-źródło-reakcji--ui-71"></a>
### Character as the source of reaction — UI-71

A link from the White Rider Inspector leads to the existing battle tray. Characters remains an inspection without a copy of the form; Skip retains the normal next card selection from the fight. The entry/change history shows the fixed versions and cost, and its subsequent view does not take over the current draft from another form. The range of version/actors remains in common with UI-62.

acceptance UI-71 : 33/33 the required Play methods, 13/13 Full Editor cases 542/542 , 7/7 PNG and 399/399 unchanged domain cases in full 3726/3726 PASS . Separate fresh HTTPS/WSS dragons are credited with trusted CA ; Unity tests benefit from HTTP/WS/PostgreSQL. Accurate filters, reusing identical methods and limits: UI-71 . .

<a id="ui-63--wspólna-historia-i-reguły-oblężenia"></a>
## UI-63 — A common history and rules siege

The actual `LiveActivityNetworkTests` re-enters marches, attacks, card, throws, retreats and enters with the same journal. G16/G17 retains the historical version of the entry, automatic aftermath and current host of the gold tab. The older card Effects, sovereign or result does not move the current battle tray and does not execute its decision.

The new `UI63SiegeNetworkTests` performs an awakening inside the garrison for each specific die Muster/Ruler and rejects the false route entering the besieged headquarters from the outside. The Ruler comes from the actual awakening and Recovery, without adding the available cost to the server. Both methods have passed on the real server. The Evil Knowledge regressions, cost and history retain their own public reports in the UI-63 matrix.

The proof of 93 configuration and its reboots is described by [IN_GAME_UX](IN_GAME_UX.md). We do not change the resolution of battle or the scoring. The full profile of the existing Fights/log is required next to the new panels; the final attempts of Player, PNG, build and dragons remain a separate acceptance UI-63, without passing the earlier G18 layout.

<a id="końcowy-odbiór-ui-63"></a>
### Final acceptance UI-63

UI-63 reiterated the shared history and the actual awakening of the sovereign in the garrison for Muster/Ruler and rejection of entry into siege from the outside. The older entry does not move the current tray. Additional window- stability8 also confirms window movement Fights/report,  card and maintain a manual position after refresh. Rules battle And the scoring hasn't changed. profile 56, including the fight/log, has passed the thresholds and log control; images are received separately in UI-63.

<a id="menu-nad-stratami--sprawdzenie-ui-72"></a>
## Loss menu — check UI-72

The new network trial uses the actual Losses FP decision, with the active battle in the overriding settlement frame. The selected unit remains in exactly the same SavedTurnDraft after Menu/Settings/Help and Esc, without command, issue of the cost or change of projection of any page. Both views and four layouts retain one tray host. The pre-attack status is not allowed as a replacement profile input of battle. Results and other regressions: UI-72.

<a id="wskazówki-bitwy--ui-303"></a>
## Battle and Hunt presentation cues — UI-303 (2026-10-23)

The activity fetch reads new steps from the **All** feed, so the automatic battle and Hunt steps (`Actor=null`) reach
the client alongside the opponent's decisions. The pennants, their BattleId grouping and the read markers above are unchanged.
`OpponentPresentationModel` turns the same pages into presentation cues
([OPPONENT_ACTIONS_UX §9.1](OPPONENT_ACTIONS_UX.md#kontrakt-wskazowek)):

- The battle's opening snapshot is the **attack** (attacker region, then defender region) for the defender only; the
  attacker's own attack is remembered, never played back to the attacker, and a battle first seen later (opened in
  legacy history) has no late attack scene.
- Each later snapshot whose public state changed is one **battle** cue (rolls, rerolls, hits, cards). A shared step makes
  the same cue ids for both players, whoever's click completed it.
- Casualties are **loss** cues from `LossDetails`, a downgrade with its replacement, never also a removal.
- An opponent's committed Combat card is a back until the reveal; both reveals are public card cues, once each.
- Hunt steps of one chain (`HuntChainId`) become Hunt cues once, even when the chain's end is recorded twice.

Checks: EditMode `OpponentPresentationModelTests` on the recorded attack and Hunt (both viewers); PlayMode
`LiveActivityNetworkTests` compares both clients' cues and their final boards.

<a id="atak-i-zdobycie--ui-306"></a>
## Attack, retreat, advance and capture on the map — UI-306 (2026-10-23)

An opponent's attack is shown before its battle window opens: the attackers are traced where they stand and the
defended force and region are traced too, without moving anything; the window opens at the release, at its normal
host. A retreat or advance returns to the map as a recorded movement of exactly its participants (an empty advance
moves nothing), and a capture changes the settlement's gem with a "+N VP" / "−N VP" badge only for a scoring
settlement ([OPPONENT_ACTIONS_UX §6.10](OPPONENT_ACTIONS_UX.md#wdrozone-306)). Casualties and other figure changes inside
a battle stay in the battle window. The player's own click (their retreat, their advance) moves no camera.
