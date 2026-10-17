<a id="scenariusze-odbioru-ui"></a>
# Scenarios acceptance UI

Each scenario is done by clicking; for drag operations we check the equivalence of the shortcut. The sample record includes build, hardware, platform, resolution, UI scale, input status, result and specific problems. The descriptions below are a plan, not results of tests carried out.

| ID | Situation and required result | Tasks |
| --- | --- | --- |
| S01 | The entire army, one legal die: army → target → approval, three clicks; cost and remaining readable composition. | UI-03, UI-04, UI-05 |
| S02 | A mixed army of three nations, commanders and figures: a precise division without entering the number and without scroll. | UI-04, UI-05 |
| S03 | Two armies away from each other: both movements in one draft, amendment one, no re-movement of the same figures. | UI-04, UI-05 |
| S04 | Movement causes a temporary excess: UI allows legal action and leads to proper removal after it is finished. | UI-04, UI-10 |
| S05 | The usual die, Will of the West and the special die fit: no automatic cost selection of any other meaning. | UI-03, UI-09, UI-10 |
| S06 | Recruitment of one elite or two figures in various settlement, no reserve parts: ghosts and legal targets, without a form of abundance. | UI-04 |
| S07 | Hand at the limit and above: readable card, full text, target on the map, covered discard without leak. | UI-06, UI-10 |
| S08 | card with multiple targets and a break to response: one tray, a distinguished draft and an already approved stage. | UI-06, UI-10 |
| S09 | Approval of card attacking, disconnect, choice of defender: visible reverse, secret content for joint disclosure. | UI-07, UI-12 |
| S10 | Two hits, regulars and elites: all legal loss schedules, public degradation/deletion, no double confirmation. | UI-07 |
| S11 | No regular to degradation and death of the last unit: a thorough preview of the elimination and fate of the character. | UI-07 |
| S12 | siege, rescue, getaway and extension: garrison separate, legal election in the same tray. | UI-07 |
| S13 | Include Skip combat cards, cancel, new card in hand, reconnet: the shortcut expires at the correct moment and does not make any other decisions. | UI-11, UI-12 |
| S14 | Hidden Fellowship moves without drawing the route; declaration/disclosure with different routes gives an informed choice. | UI-08 |
| S15 | Hunt: Guide or random Companion, response card, additional tile after disclosure; no preview of future RNG. | UI-08 |
| S16 | Mordor, final move and border Corruption: proper order and victory without premature message. | UI-08, UI-10 |
| S17 | Keeper/Lesser/Ruler, several Eyes of Different Origins: the cost and effect of the selection can be distinguished without debug table. | UI-09 |
| S18 | Kings: Secret Tiles and variant outright/sequential; correct view for both players, clear Draw / Stop. | UI-09 |
| S19 | Council and Fate: private versions, Gimli's start selection, no absent sovereigns, changed cities. | UI-09 |
| S20 | The response to command is lost after the recording; re-use, two click and held Enter do not spend the second dice. | UI-10, UI-12 |
| S21 | The opponent performs action while viewing his own card: no camera jump and preview closure; modified draft marked. | UI-10, UI-12 |
| S22 | Several saved batches: finding your own decision, entering without playing animation, information only in the application. | UI-12 |
| S23 | All above important screens on 1366×768 and 1080p with enlarged UI; no cut buttons and forced scroll. | UI-13 |
| S24 | Full map, long battle and preview cards: saved feedback measurements, FPS, allocations and reduced motion. | UI-13 |

<a id="protokół-oceny-prototypu"></a>
## Protocol for the assessment of the prototype

Review UI-01: [WIREFRAMES](UX_SPEC.md) saves the click paths and extreme cases 28 from families. The inventory binds S01–S19 to decisions; S20–S24 are cross-cutting controls for transport, layout and performance. The verification of the mockup does not yet include scripts of the game running.

UI-07 (S09–S12): `BattleLayoutTests` checks the reverse from Shadow projections, subsequent disclosure and full Combat text, constant power order, transparent Downgrade/Remove, different nations, large compositions and separate regular/special throws. The matrix includes 1366×768 and 1920×1080 at 100/125%, without scroll and numerical fields. Preview can describe the elimination of all 28 fixture commanders without overfilling. This fixture protocol/presentation, not production effects of cards; secret approvals and their reboot additionally have HTTP T18 tests.

`BattleNetworkTests` and `StrongholdNetworkTests` open two real Unity clients with Kestrel/PostgreSQL in nine domain scenarios. They perform attack, cards refusal in the right order, actual draw and selection of reroll, one/two loss, degradation without available regular and Gimli elimination, Continue → retreat on the map → progress, shelter of six Stay defenders without losses, establishment of siege and only then reduction to five. Check the selection of the elite and reconconnect before extension, the trip stopped in the garrison and the separation combined with the reduction of eleven figures That's the one.. Camera and single cost remain preserved. For the initial state of the scripts, the throw is saved by the engine before the start of the server; new commands use the production serwera/RNG. No test endpoints.

The final round report shows the actual throws after the automatic end and after reading the write. Passes are instant, without the fake roll/animation to be turned off. The drop overview was done automatically and by reading images; this is not a deferred manual user test. Skipping cards remains in UI-11, full Combat programs in T21.

UI-05 includes at least S01–S06 with the most difficult legal compositions. The performer performs interaction and visual review. If an external player is available who knows the rules, he additionally checks the operation without explaining the location of the buttons. The lack of such person is recorded as a limitation; no test is declared.

We note: number of clicks, incorrect indications, necessity to undo, hidden information required for decision and scrolling. An attempt with an external player is additional proof, not mandatory consent to further implementation. The required attempts of the performer and improvement of errors detected remain documented. Layout corrections are performed before repeating the pattern in the subsequent screens.

<a id="granica-prototyp--integracja"></a>
## Limit prototype / integration

T22 bind all 22 base `DecisionKind` with [normal and difficult case](T22-ACCEPTANCE.md).  `T22BoardNetworkTests` moves extreme S02 – S06 by a true backend: the exact division of the three nations, the choice of Will of the West, the amendment of the first of the two movements, both types of excess and recruitment in two settlement with limited reserves. `T22TurnNetworkTests` / `T22AbilityNetworkTests` include allocation Hunt, Ring, politics, pass and active abilities. `T22BoundaryNetworkTests` complements a random victim S15, choice battle Common Stronghold and both military victories and their priority. `T22InformationTests` adds the first load and watch the exact composition, policy and safe history without changing the decision. The test results save the protocol; the script entry itself is not the result PASS.

acceptance T22: full PlayMode **172/172 PASS**, no omissions, 2026-10-05 22:20:03 UTC . Includes all the new scenarios listed and previous S01– S16/S20 - S21. Full. NET **1668/1668** and final EditMode **118/118** also PASS Eight new staffs information/history includes 1366×768 and 1920×1080 with 100/125% ; the smallest systems were viewed. Repairs and earlier failures were recorded, without replacing them with a full result. expansions S17–S19, final S22–S24, Windows–macOS game and user tests are not included in T22.

UI-10 extends S20–S21 to [three controlled sequences of the real HTTP/WebSocket](UI-10-NETWORK.md): late receipt with newer board, conflict of two devices with invalid preview and old projection with a fastened card. Tests delay or lose response after execution of the server. The addition of scripts does not include acceptance; actual results and possible environmental limitations save the protocol.

acceptance UI-10 ed 2026-10-05 : After the re-authorisation, the actual S20–S21 passed **3/3** in Unity/Kestrel/PostgreSQL , including the old receiver at a newer view, two devices and delayed GET Full PlayMode gave 150/152 ; both failures of independent fixture were corrected and re-entered the entire class BoardInteractionTests:6/6 PASS The protocol separates these reports and retains earlier locks. T22 supplements acceptance all options for clicking and complete state information.

UI-02 has [review report](SCENARIOS.md), EditMode and PlayMode as well as minimum and target resolution. The attempts are on maps/HUD and navigation events; they do not include future scenarios of cards, losses or full play.

UI-02–UI-08 can develop components with open-label fixtures. UI-10 connects base interactions to authoritative contracts. UI-09 includes expansions after implementing their rules. T22 and T34 are acceptance respectively bases and expansions, rather than the second task of implementing the same views.

T12 provides backend scenarios S01–S04: two distant movements, mixed nations, division with left-wing commander, no re-movement at the merge and legal transitional excess. `MovementTests` compares preview with result and plays choice of excess after serialization. It tests rules and contracts, not crediting user interaction; these belong to UI-04/UI-05 and T13 integration.

<a id="próby-ui-04"></a>
## UI-04 Trials

T15 provides backend S14 and separation: legal routes, cost, new Guide and private options. `FellowshipPersistenceTests` restarts host HTTP/PostgreSQL in the route, group and Guide selection and renews command without second cost. acceptance interface describes UI-08 below.

UI-05 closes [Evaluation protocol](SCENARIOS.md): click paths, actual layout fixes, 6/6 PlayMode attempts after the patch and review of both resolutions/scales. It also includes two already selected deployments with 125%, a clear special cost and paid die during the over-excess. No rehearsal with an external player was done.

Conditions: Units 6000.6.0f1, macOS / M1 Pro, public `BoardFixture` and responses HTTP isolating interactions. RenderTexture 1366×768 and 1920×1080, the scale 100% and 125%. The authoritative rules movement/recruitment have separate tests T11/T12; the complete sequence of these actions on two real clients receives T13.

T13 adds `MovementNetworkTests` with two full panels, HTTP/WSS and PostgreSQL: movement and alternate recruitment, click and drag, one commit, malicious unobstructed target, loss of response after the commit and recovery of the result without a return cost. [Description of conditions](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/TESTING.md) distinguishes the repetitive input snapshot from the actual execution. The result does not include future special abilities or a test between two operating systems.

| Scenario | Actual scope of the check |
| --- | --- |
| S01 | `WholeArmyUsesThreeClicksAndDragPreparesTheSameCommand`: three clicks, then equivalent drag, identical payoad and zero commits on the drop. |
| S02 | `ExtremeMixedArmyAndReservesFitAtBothResolutionsAndLargerScale`: the units of three nations, all base commanders of FP and eight apparent character slots. Selection below 100 ms, the same control and Bounds before/after; no number field or ScrollView. Division with left lone commander blocks the model. |
| S03 | `TwoDistantMovesCanBeEditedAndRecruitmentHasTwoVisiblePlacements`: Minas Tirith and Dale, first-order fix after second move, two ghosts, one commit. EditMode also checks the re-indication of the same figure after the merge and undo. |
| S04 | `TemporaryExcessLocksArrivalsAndMandatoryRemovalUsesIndividualFigures`: two full armies, 48 figures after the connection with UI, the 125% scale; the incoming ID are inaccessible, the residents of the previous time move on. A separate mandatory excess decision selects a specific figure. A correct limit moment and durability checks T12. |
| S05 | EditMode: normal dice equivalent, Will of the West and special source; no arbitrary auto-choice. Character requires commander and one army. |
| S06 | Two recruits in different settlement and a separate drag elite. Empty reserves are disabled. EditMode rejects the same settlement and an additional figure after the elite, checks the undoing and copy of command. |

UI-04 also includes a click of the nation's emblems, track preview and Muster cost. The final results and commands are in the task history. Local artifacts: `.artifacts/screenshots/army-*.png`, `recruit-*.png`, `army-merged-1366-1.25.png`. No study has been conducted with the outside player; the full evaluation protocol and further visual overview are part of the UI-05. This is not a measurement of 60 FPS.

Problems detected and corrected: large-scale overflow at 125%, target buttons coming, target spirit label, too wide resource counters, loss of selected subset at drag. Interaction tests use open minimum viewport; the default Unity window can be less than the supported minimum. The error cask after failed scale attempt was removed by independent creation and release of rendering surfaces in each test.

### UI-06 — S07/S08 (2026-10-05)

S07: `CardInteractionTests` checks all 96 descriptions Event and Combat at 1366×768 and 1920×1080, 100% and 125% scale. Controls full text limits (font ≥ 16), no controls, nine clear titles, no scroll, no focus without reflow, pin, and no region/pan selection without closure. Inspector does not apply to confirmation; more than half the width of the map remains exposed. Screenshots, extra hand and recruitment with two deployments were viewed. This review of the performer, no user test, nor measurement of 60 FPS.

S08: second test UI conducts two approved elections of the regions, leaving earlier steps with ✓, and then the response to the reaction in the same tray. It is a clear fixture of the protocol UI, not a completion declaration card from parcels T21.   `EffectRuntimeTests` checks the true mechanism of nested reactions, permanent record and privacy of approved elections.

`CardNetworkTests` launches two Unity panels with real Kestrel/PostgreSQL: Celeborn’s Galadhrim → reconnect when selecting Regular/Elite → selection, Threats and Promises on the table, simple selection over the limit and covered discard, a Visdom of Elrond with an outright nation, Bilbo’s Song. Nine revisions, four paid action FP; the opponent does not get ID covered discard or private EffectTrail. Texts 96 cards and four implemented events remain separate ranges.

The first launch detected an overflow of older trays after the extension of the hand and the application of Close control in the inspector. Fixed the rolled-down list of titles while working with armies, reserve palette, tray of rings and long preview size. The final `bash scripts/test-client.sh`: **15/15 PlayMode, zero omissions**, 2026-10-05 01:54:27 UTC; contains the full regression of UI-04/UI-05/T13. Dumps: `.artifacts/screenshots/cards-*`, `army-*`, `recruit-*`, `turn-*`.

### UI-08 — S14–S16 (2026-10-05)

`FellowshipNetworkTests` uses two mounted UI Toolkit clients, the real Kestrel and PostgreSQL. Six input states save a separate `Wotr.Scenarios` program; the production server does not have an endpoint to modify fixtures. The reduced pool and the saved earlier throw are explicitly test data. The next command, RNG, costs, previews and projections support the normal server.

| Scenario | Test performed |
| --- | --- |
| S14 | The route-free movement increases progress and releases one die to Hunt. A separate declaration leads five edges to Lórien and heals. The separation of Gimli and Merry to Moria stops the journey at Stronghold Shadow, opens the Guide selection; then they both start as two independent groups behind one die. Map clicks retain the full indicated route. |
| S15 | Shadow selects the failed die to be spread. FP sacrifices the Strider, points Gimli and retains the choice after reconnect. The Moria disclosure → Dimrill Dale → North Anduin Vale → Dol Guldur gives two more extra tiles. The empty pool plays the allowed standard discard; the cost remains Resolving to the end. The next action Hide retains the region. |
| S16 | The entrance from Morannon retains Revealed and camera; the Fellowship button heads to the track. Two steps states 4 with Corruption 10/11 and the 1 tile stop at the injuries without the win message. Only the approved settlement gives the ring to destruction or failure of the Trustees accordingly; both clients see the result. |

`FellowshipDraftTests` checks separately the lack of a default response to the injury, the empty selection of the reroll, Will of the West, copies of nested groups at Undo/send, the lack of a repeat movement of figure and the passage through the sanctuary as a step of disclosure. Random's selection sends the procedure, without ID victim. `FellowshipLayoutTests` also checks the overall response of Guide as a clear fixture of the protocol; this does not mean that the ability/cards ed T20/T21. Durability of the nested reactions itself has the actual tests of HTTP in T16.

Layout:  1366×768 and 1920×1080, scale 100% / 125%, six decision types, seven groups, five dice   Hunt, hand six cards,  Corruption   11 and step 4. Without ScrollView/p numbers, buttons within tray limits, preserved map, pinned   card and route correction by Back. `.artifacts/screenshots/companions-1366-1.25.png`,  `mordor-1366-1.25.png` and `fellowship-*` The map avoids visible panels when placing labels. This is an overview of the performer on macOS/M1 Pro, no user and no declaration 60   FPS.

Full `bash scripts/test-client.sh`: **19/19 PlayMode**, no skip, no end 03:47:31 UTC. EditMode: **27/27**, no skip. Additional final Layout attempt includes private statistics tooltips; commands and results are in the UI-08 registry. Full .NET: **234/234**.

### UI-11 — S13 (2026-10-05)

`BattlePolicyNetworkTests` performs two scenarios with reversed attacking roles: private inclusion when selecting the opponent without changing it decision/revisions, reboot, shut-off before the window and turn on in its window. Cancellation after the written response preserves the round 1 and shows `Ask next · Round 1 kept`. The further decision of the game remains unresolved by the slot. The actual sessions, HTTP/WSS, PostgreSQL and `TurnInteraction` were used; initial fixture has armies to battle, subsequent choices are executed by client clicks. Full PlayMode: **28/28**, zero skips, end 05:53:40 UTC.

`AutoDecisionTests` complements S13 with a change of hand in effect card , both automatic responses in the second round, subsequent Metallage windows/ strat/retreat , new battle And illegal versions. `AutoDecisionPersistenceTests` checks the actual restart of both roles, rollback, both forced order of competing transactions, privacy receipt and identical GET the opponent after changing preferences. The selection program in the test is open `test.*` , without attributing this ability to base card . `PrivateProjectionTests` rejects delayed GET with a lower private version on the same game revision.

The layout review includes the same panel and matrix UI-07, extended with 10 own dice, three Rings and information about the saved round. No manual tests were performed with the user, Windows runtime or new measurement of FPS.
<a id="ui-09--scenariusze-integracji-s17s19"></a>
## UI-09 — S17–S19 integration scenarios

S17 now also includes the Keeper/Lesser, Ruler recoil, token recruitment and dice, Narya/Vilya, Dismay and separate Far-sighted selection. S18 checks the simultaneous legal Draw/Stop and inspection of the covered tile without value/ID in all UI texts. S19 includes seven pairs of Councils, both Gimli Fate's homeland, leaving in Fellowship, the Council's entrance fee fee and returning to its own draft after reconnect. Status of actual launches: [UI-09-IMPLEMENTATION](UI-09-IMPLEMENTATION.md).

Automatic UI-09 tests use `ExpansionDecisionNetworkTests`, `ExpansionTokenNetworkTests` and `ExpansionCorruptionNetworkTests` classes on the real backend. `ExpansionMatrixLayoutTests` checks 186 server projection in four systems and full dice/tokens pools, also the application of hitboxes. Regression includes the source/conversion of the Ruler, the required second selection of Denethor and the privacy of both decisions Corruption. Manual user tests and performance/Windows remain separate acceptance.


<a id="t34--odbiór-s17s19"></a>
## T34 — acceptance S17–S19

[Report](T34-ACCEPTANCE.md) binds eight new protocol windows with the scenarios of both roles. Additional attempts include 12/15 separate travel groups, Branda/Dáina, Balroga/Gothmoga, Nenya, Evil Knowledge and full pool in battle, the order Lore/Dismay and simple refusal. The 93 configuration includes normal start HTTP, lobby layout and Rules; eight modules' compositions also start with Unity controls and reach the first phase of action. Description distinguishes the scripts' sample on the actual server from the independent matrix. It does not include manual user attempts, Windows or FPS measurement.

Final checker: 37/37 expansions scenarios. Additional Lore selects Lesser Eye through Hunt, distinguishes Ruler/Balrog and retains order with Dismay. Full PlayMode 254/257 completed with the re-enactment of all three repaired failures (18/18) and final control symbols/windows (14/14). All details and results of previous attempts remain in the report; this is not the report 257/257..

<a id="ui-12--s09-s13-s20s22-i-powroty"></a>
## UI-12 — S09, S13, S20–S22 and return

[Return Protocol](UI-12-RETURN.md) includes production ClientFlow, separate accounts, 23 saved parts and actual Kestrel/PostgreSQL. ReturnListTests checks pages, filters, no ack at Changes and read only after board, and then both views of the family records. Covered card, Council and Kings remain in line with the private server projection. Opening does not change the revision and does not answer for the player; the tray does not require a scroll or numbers.

ReturnRecoveryTests interrupts response after commit, restores the entire client instance and restarts the server process. The original ID returns from the local file; the creation of a receipt gives one selection and cost without the new POST. The second attempt recovers the private cards combat setting, retaining the identical payoad of the opponent. ReturnDraftTests actually stops the server: map and card remain available, you can change the deck in draft but not approve. The second device consumes the selected die; the return deck, emptys the cost and requires an open choice.

First completed run: **11/11 PlayMode**, 0 skip, `.artifacts/ui12-second-play.xml`; contains six new attempts and five regressions UI-10/UI-11. Four layouts and Changes have drops, minimum enlarged screen viewed. After additional closing of the end logs EditMode: **186/186**, `.artifacts/ui12-flush-editor.xml`. Full regression PlayMode remains in progress; the result does not replace these targeted controls.

Final regression UI-12 : Full PlayMode **263/264**, zero skip, `.artifacts/ui12-full-play.xml` , 12:40:26–12:55:21 UTC The old motion test expected a hand check result despite automatic recovery of the result. After switching to actual GET the original receipt and one expense was re-established the entire class with Return/UI10/BattlePolicy: **11/11 PASS**, zero skip, `.artifacts/ui12-regression-fix.xml` , 12:57:39–12:58:18 UTC The production code has not changed with this test repair. This is a combined result of a full run and targeted re-renewal; not a report 264/264.

UI-13 — S23/S24: scale and motion settings, focus outline, behavior draft after changing preferences and no scroll check `InterfaceSettingsTests` Matrixs expansions They also guard the size of the targets dice . [Report desktopu](UI-13-DESKTOP.md) and [raw measurements macOS](profiles/ui13-macos.json) show 16 Development Player workloads at 1366 × 768/1920 × 1080 and 100/125%. Medium 104–120 FPS max. 14,74 ms locally to the end of the render. These are not network times or reading of the physical input device. Windows has only build; the user explicitly postponed its launch.

T36 — [acceptance final](T36-ACCEPTANCE.md) connects S01–S24 with the above evidence and supplements it with two full lots at 125%/Reduced motion (2/2 PASS) and the regression 60/60 PlayMode, without skips. The route target was detected and repaired by an army label at the attached card. The current [Player profile](profiles/t36-macos.json) includes this change: 16 workloads, average 85,89–120 FPS, maximum. 15,06 ms local feedback. Windows run, physical Intel Mac and manual user attempts remain unexecuted; the results relate to the named macOS configuration.

<a id="nowy-odbiór-ui-14ui-24"></a>
## New acceptance UI-14–UI-24

Historical S01–S24 retain actual results of the earlier client, are not acceptance new gestures. The following scenarios apply a new specification:

| ID | Screenplay and condition |
| --- | --- |
| D01 | Hover region shows units/garrison/Nazgûle/characters without clicking. Passing region→panel does not close it; output closes, does not delete the choice. Focus gives the same information. |
| D02 | Hover does not send anything, does not change the source and does not reveal secrets. Panning/drag does not open random panels; no click-through and tooltip cascades. |
| D03 | Army and Army/Muster/Will symbol selection highlight legal objects. Two-click banner + figures correction; click another source replaces only an unfinished selection. |
| D04 | Two marches: the first permanent, second or Finish; a friendly busy target, used ID after the merger, illegal drop/Esc, final excess and politics. |
| D05 | Restart after first step, lost response, retry, second client and outdated gesture; cost once, no return of RNG, old record and retrieve still readable. |
| D06 | Hover/pin/double-click/drop card; both applications, all required targets and correct secrecy. Talie and aggregate discard. |
| D07 | Attack by one click target; regional panel, damage with corrections and order, reroll 0..limit, retreat, trespass and extension siege. |
| D08 | settlement→figure: two compatible locations or elite/Finish, reserves, leader and limits. Policies on the track, without the action menu. |
| D09 | Panel Fellowship, normal traffic without a route, declaration/disclosure, portraits, route on regions/double-click, Hunt/Mordor and all reactions. |
| D10 | Each expansions family and all configurations: Council, special dice, Rings, Dismay, Ruler, tiles and abilities without losing exceptions cards. |
| D11 | The notification does not take over the camera. Click leads to the correct regionu/panelu. Back restores context and sequence, also with the lost receipt. |
| D12 | 1366/1920 , 100/125% , dense piles/kraw screen run, no mandatory scrolla/numbers ; feedback ≤ 100 ms, measurement FPS/allocations and reduced motion. Build both platforms; runtime Windows unexecuted. |

Each attempt saves the actual command, version, report and limitations. D01–D12 are not marked as counted by the mere addition to the documentation.

UI-15 , 2026-10-06 : `ContextInteractionTests` and `ContextHoverTests` check D01–D02 and the common part D03 (symbol dice selection figure , replacing the source, the range of the two clicks). Unity 6000.6 .0f1 per macOS: full EditMode 214/214 and aimed PlayMode 18/18 four systems 1366/1920 × 100/125% , actual projections HTTP FP/SH and a clear dense fixture. Panel retains the choice, controls and pinned card ; does not change the search or camera. 1366/125% The immediate marches and the full path D03–D05 receive UI-16/UI-17 ; not counted with this test. Commands and previous errors: UI-15 .

UI-16 extends server part D04–D05: `ArmySequenceTests` , `RecruitSequenceTests` , `TravelSequenceTests` and `ExpansionSequenceTests` check one cost, merge, budget, routes and moment polityki/Vilya. `ActionSequencePersistenceTests` launches three more hosts HTTP over the same PostgreSQL , resumes after the first and second steps, renews the receipts, rejects the alien side, the old revision and the changed payoad. Corps `paid-army-step` keeps v3 inside action ; old files remain unchanged. These are server tests; real clicks/drops of new sequences require acceptance UI-17/UI-20/UI-21. Results: UI-16 .

UI-17 updates the army's historical tests to D03–D05: the target saves the step, there is no correction of the first march after the commit. `ArmyContextNetworkTests` includes the merge, reconconnect, its own banner as source, Finish and excess, and the attack FP/SH and the out. `MovementNetworkTests` retains the actual loss of response and retra, now between steps. `T22BoardNetworkTests` retains the division of nations, the exact ID and Will as Character. `BoardInteractionTests` remains the open fixture of Layout/transport, without pretending to be server rules. `ContextHoverTests` measures feedback and four circuits. Results and previous failures are recorded in UI-17.


UI-18 expand D02/D06/D12: `CardActionTests` checks the exact cost, the clear Will symbol, the desired target, free Combat/reakcj s and separation of single-elections from harvests. `CardInteractionTests` check hower→panel→ output, pin, no automatic dice , border of the two-click, all texts and excess hand in four layouts. `CardNetworkTests` perform events, the purpose of the emblem, decks, private recoil and reconnect; `CardGestureNetworkTests` checks the actual commitment of both cards ed double-click/drop and a secret to the reveal. Exceptions of Jerkíra/Denethor and free events have updated network attempts. Results of actual runs save UI-18 .

UI-19 updates D07: BattleDraftTests check the cycle and sequence of operations, copy of the losses sent and zero reroll. BattleLayoutTests Opens and closes the regional panel, retains local loss, checks the lack of automatic camera opening and motion and four systems. BattleNetworkTests/StrongholdNetworkTests perform a few retreat targets, a step in, an elite two-click, loss adjustments and reroll by the actual server; card And nested effects have separate network regression. BattlePersistenceTests adds three hosts over the same PostgreSQL in reverse and intrusion, supplementing the earlier reboot casualties/secret card and SiegePersistenceTests . Actual results and previous errors in UI-19 .

UI-20 update D08:   T22BoardNetworkTests performs a two-click regular, reconnect with one remaining point and click the target of the other settlement; the realised cost and the missing reserves come from the server. MusterNetworkTests checks the drop of the elite, a legitimate lone commander with a preview of the removal, earlier ✓, excess after closure action, hover without effect and drag emblems. T22TurnNetworkTests links policy to precise dice changed with the Ring. BoardInteractionTests includes four systems, stable controls and dense armies with pallet. Special recruitments Brand/D áin/ Gothmog/Th éoden and Balrog's entrance have a regression ExpansionAbilityNetworkTests/ExpansionTokenNetworkTests.  Server  MusterPersistenceTests and ActionSequencePersistenceTests check PostgreSQL, actual host reboots, resume steps and resumes. Results, fixes and postponements: UI-20.

UI-21 update D09:   FellowshipNetworkTests performs declaration, hidden move without route, separation, separate choice Guide,  Hunt And both Mordor finals through the server. CharacterNetworkTests includes Walking/volatile Characters and forced travel. TravelGestureNetworkTests checks the replacement of the unsent group, the portrait drop, the cost saved and the reconnect, the exclusion of re-movement; and hover/click tag without the choice of region and camera movement. TravelActionTests controls the exact cost, the open wall of Will, free Guide and the end of the route. Fellowship /forms and exceptions cards are checked with 1366/1920   ×   100/125%; regression Lords/Kings/Fate maintain separate rules. Previous failures, corrections and actual commands: UI-21. Final acceptance all families of the new model expansions remains in UI-22.

UI-22 expand D10 about `ExpansionCostNetworkTests`: conversion of the Ombudsman's Muster to two steps, Great Sorcery only with the Ombudsman, army with Balrog and the journey of Balrog himself without the law of the Nazgûla movement. `ExpansionTokenNetworkTests` checks also Far-sighted separation with Ruler token and without, keeping the cost after reconnect and no additional menu. `ExpansionDecisionNetworkTests` includes a drag portrait of the Council on homeland and a direct special election dice, Ruler discard and received token. `ExpansionCorruptionNetworkTests` checks hower sovereign with outright/covered With no state mutation or camera, the removal of all permanent effects triggers the correct card on the table; cost of two cards Protection continues to finish one ✓ Control of the systems also includes hitboxes of regional components and both pages 93 configuration. Results and previous failures are recorded in UI-22.

UI-23 extends D05/D11: `DecisionNoticeNetworkTests` receives battle input without camera movement, click notifications from both sides and press the deck by reconnect at the same revision. `ReturnListTests` opens all families on both sides, checks the property notification, navigation or no location and no command after opening. `ReturnDraftTests` records incomplete line-up of the army, retains it after releasing a specific dice by the second device and plays a lasting first step in the new client. `ReturnRecoveryTests` retains primary commandId after final deck click and loss of response. `DraftInputTests` isolates capture, release, Esc, double and held Enter with delayed preview; does not replace the server. The actual results and failures: UI-23.

UI-24: Full lots `T23FullGameNetworkTests` / `T36DesktopGameTests` use accurate dice panel Fellowship and the final gesture of the route, from the usual room and from RNG server. `FellowshipLayoutTests` includes eight members with Sméagol, Mordor, and nine clickable route handles next to the fastened card in four layouts. `T22InformationTests` retains read-only control also for the new reserve portfolio. `RuntimeTooltipTests` checks the visible text of both projections, intercepted indicator, focus, no take-over click and four window corners at both scales. D12 has **28 real Player measurements**: 104,88–120,00   FPS,  feedback do  27,14 ms, with open allocations and scope of equipment in UI-24 and [final report](profiles/ui24-macos-accepted.json) A drop of hints from Player 1366/125% The full regression results and the build limit are recorded in the same task.

The final course of `ui24-play-accepted.xml` with 2026-10-07: **309/309 PASS**, without omission. Includes the entire base and expansions, five new tooltip tests and full 109/104/43 parts with decision. Checker UI confirms all 43 scripts required with expansions on this one XML. D01–D12 have a map of evidence in UI-24; the limit of Windows runtime and manual acceptance remains open.

<a id="d13--porównywalne-tory-polityczne-ui-25"></a>
## D13 — comparable political tracks (UI-25)

The Politics tab → all positions on the common scale → token → legal next field. Compare your own website and opponent, switch Show the opponent, leave the panel and return without losing a choice. Hover does not choose the nation. Check for source replacement, inappropriate target, drag and exact cost and passive nation before At War. The council uses the track after reconnect, without dice. Narya has the Actinate button at the right token, with the cost seen in the tooltip; the ordinary Muster cannot activate the nation.

`PoliticalActionTests` checks the local intent against the current options. `PoliticalPanelTests` controls the hover, fokus/Esc, the visibility of action, order and filter for both sides, stability of the controls and positions/hitboxy at 1366/1920 × 100/125%. `MusterNetworkTests`, `T22TurnNetworkTests`, `ExpansionDecisionNetworkTests`, `ExpansionTokenNetworkTests` and `CardNetworkTests` bind the gestures to the real server and PostgreSQL. The results and limits of acceptance save UI-25.

acceptance D13: **254/254 EditMode**, **53/53 PlayMode**, additionally **6/6** policy trials after final changes in appearance/light and **3/3** system control at conflicting resource preview. These are the successive stages of parts of the same tests, not 62 different scenarios. Images of both pages viewed; Windows runtime and manual acceptance remain postponed.

Clarification D13 in UI-26 : no click of the token opens the second panel. After selecting the cost, the first click selects the token and only its next box, the second click makes the move. `PoliticalGestureTests` check the absence of automatic cost/Inspector , start drag without prior click, highlight the dragged nation and restore choice after Esc. `MusterNetworkTests` adds illegal drop to another track with a choice, then legal drag. `T22TurnNetworkTests` controls the exclusive target after first click and the actual cost after second. `T22InformationTests` Watch the state in the Politics tooltip and keep the Army draft. Results of the new verification: UI-26 .

acceptance UI-26: **254/254 EditMode and 55/55 PlayMode**, without omission; drag and final click checked with server, target drop in the panel viewed. The previous mouse slowdown simulation error and its re-verification are explicitly saved in the task.

UI-27 complements D13 with missing case: local save AdvancePolitics with missing dice, normal Muster, Will or Army/Muster. Recreate the client, confirm missing bottom track/lists nations/Confirm, select the required cost and continue only in Politics. `PoliticalRecoveryTests` uses LocalMatchPersistence and the actual Unity controller; fixture options do not replace separate server gesture tests. Results and proof of the old path are in UI-27.

acceptance UI-27: a pre-register error, the same test after the correction **1/1 PASS** for four costs, then **254/254 EditMode** and **49/49 PlayMode**. A separate build macOS Universal was created from a revised runtime; its launch as a user client was not counted.

<a id="d14--kości-wielofunkcyjne-bez-wyboru-kategorii-ui-28"></a>
## D14 — dice multi-functional without selection of categories (UI-28)

One click Army/Muster or Will: simultaneous sources of army, recruitment pallet and policy chips. Select figure, then target: pallet does not capture traffic. Separate parties check recruitment and two clicks on the track. Will also provides selection, travel, Fellowship and card. The saved march does not allow you to go to recruitment/politics; the selection of another object before sending retains the exact die. The restored policy draft immediately provides the target without re-election of the symbol. The previous requirement to wait for the category in UI-27 is replaced by the order UI-28.

MultiUseDiceNetworkTests uses separate server states Army/Muster and Will. PointerGestureEvents.ClickAt selects the control through the panel.Pick and checks the lack of a veil. Narya also uses this path. Tests do not grant activation to the normal Muster; the passive nation remains unattainable to enter the war. Results: UI-28.

<a id="d15--schowana-ręka-i-bezpośrednie-play-ui-29"></a>
## D15 — Hidden hand and direct Play (UI-29)

Cards → hower card → both effects → small Play. Passing to the button does not close the hand; output closes only temporary preview. No dice does not entitle to play. Verify the distinction, stability of the controls, fokus/Esc, pin, dragging out of hand, reconconnect and hidden card from the opponent. Full cards and the excess hand must be placed without scroll in four 1366/1920 × 100/125% systems, without blocking the discard approval.

CardNetworkTests uses Play for the event with and without further effect, as well as clicking deck and discard. MultiUseDiceNetworkTests checks both costs Will: Event retains Gandalf's reaction, Muster does not create it. CardGestureNetworkTests combines Play card with combat, reconnect and drag second card; both remain private to reveal. Results, limitations and build saves UI-29.

Final verification of D15: 255/255 EditMode; wide regression of 66/68 and targeted re-reductions of all failures — 5/5 layout and 4/4 gestures with server at the final runtime. Intermediate set of cards 30/30. Full 135 cards in four layouts houses both text over tray; nine cards retains two rows at 125%. This overlapping run, not the sum of unique attempts. Build macOS Universal and actual limitations describes UI-29.

D09 / UI-34-PANEL: `FellowshipPanelNetworkTests` checks Move/Hide with specific Will, no automatic cost, no stable focus on GET, both views, loss of actual response and recovery of the receipt without second expenditure. `FellowshipNetworkTests` closes and resumes the same declaration, maintaining the local route and camera. `FellowshipLayoutTests` controls English labels and stable controls in four systems. Old hower test first brings the map closer, according to the visibility of the UI-30 tags. Results and previous failures: UI-34-PANEL.

D09 / UI-34-ROUTES : `FellowshipRoutesNetworkTests` prepare a declaration with progress of zero in Minas Tirith, check preview Treatment/activation , privacy draft and lost the receipt at a single click of the handle. `FellowshipNetworkTests` includes Undo/Cancel , declaration in Lorien, forced Reveal and further tiles Moria/Dol Guldur. HTTP/PostgreSQL In `FellowshipPersistenceTests` restarts before the route and between additional tiles after its reception; retry preserves RNG and cost. The systems control the reachability of the named end and buttons at both rozmiarach/skalach. Results: UI-34-ROUTES .

D09 / UI-34-GUIDES: `FellowshipGuideNetworkTests` compares voluntary change and Finish phase, lost response, public disclosure of the Council version, separation to besieged garrison and removal of the entire group in Mordor with the Gollum settled by the server. `CharacterNetworkTests` checks Cancel required Hobbit group and further route, and existing cards scenarios — free separation and new Guide. `FellowshipGuideTests` covers the difference of voluntary/forced mode, range of the changed group, RequiredGroup, visible versions and mixed effects of Sméagol. The layouts have a separate release of the Guide in four rozmiarach/skalach. Actual results and errors are in UI-34-GUIDES.

D09 / UI-34-HUNT : `HuntNetworkTests` check two clicks/drop to the Eye, withdraw a specific dice , privacy of the set, legal zero allocation, spread of the selected box, Keep roll, lost receipt and automatic box with report after reconnect. `CardPackNetworkTests` control ? before Shelob's replacement and report Mithril/Axe after completion. `HuntLayoutTests` checks four systems, separate injuries/ Reveal/Stop , one controller host, hover and close to a new decision. `FloatingWindowTests` also includes eye drag, position memory and camera. `HuntInteractionTests` checks the exact ID , limit/zero/snapshot/reconcile , public data and unknown injuries. Actual results, errors and limit acceptance : UI-34-HUNT .

D09 / UI-34-RESPONSES: `HuntResponseNetworkTests` performs Ring/Guide/Random by clicking the object → tile, double click and drop. Checks the wrong target, preview without effect, private draft, loss of response, one receipt, reconconnect and the discarded end of the old gesture on the new Guide. `HuntResponseTests` includes legality, no ID victim, snapshot behavior and loss of candidacy. The response matrix bases/Lords/Council/Fate and reboot tests is in UI-34-RESPONSES; status acceptance and the actual results remain in its dokumencie/rejestrze.

D09 / UI-34-MORDOR: `MordorNetworkTests` passes both inputs with both statuses of hiding and refusal, Stop/Reveal, Eye at zero dice, hiding, idle and changing turn. Two finals recover the lost response without repeating the tile. `MordorProjectionTests` and `MordorPresentationTests` distinguish null/false/true in both serializers; `VictoryPersistenceTests` checks the public tag and result after reboots. `MordorLayoutTests` controls geometry, hits controls, focuss and camera in four systems. The actual results and limitations record UI-34-MORDOR.

D09 / UI-34: extended `FellowshipNetworkTests` follows one public report and paid die by Guide, route, two additional tiles and Hide. Checks the change of the owner of the Eye tray → Fellowship → Eye and behavior of the report after GET. `FellowshipGuideNetworkTests` recovers the receipt after moving to HuntAllocation opponent: Eye opens once, Close survives GET, reopens and Check results do not send the second command. The Matrix of all scripts and actual results are found in UI-34.

<a id="ui-30--kontekstowe-znaczniki"></a>
## UI-30 — contextual tags

Match the map: figure hidden, legal regions readable. Come to a dense region with two sides and a garrison: only local tags shown and one interactive composition. Go to figures, select one, leave the panel: the source stays. Close to 160%, go back to 150% to 144%: no flicker, the other figure only disappear below 145%. The movement/drop objectives remain available.

UI-31 : `HandDiscardNetworkTests` check on Kestrel/PostgreSQL Hands of eight cards : preview by single click, single discards without Confirm, quick third click, privacy, reconjection after first rejection, rejection of gesture started before reconnction and transition to the second player only after the six-lime. `HandDiscardTests` checks the set limits, old decision, continuation of Anárion's recovery and effect by restart/receipt. `TurnTests` includes the continuation of the cost of the regular selection and the compatible discard of the entire set. Results of actual launches in UI-31 .

UI-32: `WindowLayoutTests` controls boundaries, too large window, reset and isolation of the session. `FloatingWindowTests` performs actual header gestures in four layouts, checks buttons/checkbox, reopening, pan/zoom, projection, intercept/Esc, new BoardScreen and reset in settings. Includes card, hand, policy, Fellowship, information, resources, sovereign, battle and report. Hover remains anchored, drag openly clip. Results in UI-32.

UI-33:  `HuntReportTests` check automatic roll/pud ed, zero dice,  first/ko End-of-life reroll values, common additional report Stronghold, unknown value of listed Shelob, protection, order of new Guide before Corruption, both Fire and Shadow eliminations, independent Stop/Reveal/ treatment, both endings in Mordor, privacy, invariability of the old score, older record and damaged connection. HTTP/PostgreSQL   restart/receipt compare both views and RNG between command Historical files remain unchanged; exact range and results in UI-33. The final location of the returning Hobbit and Staff without selection of tiles was also checked. UI-34.

UI-35 Renews D09/F01 –F07 and regression maps/r slang/ windows/ko s/policy. 83 different PlayMode full 284 EditMode and 3575 . NET PASS ; accurate filters, previous failures and re-writes UI-35 . CardInteractionTests checks the physical availability of each card hand during inspection of all 135 previews in four layouts. FocusIndicatorTests checks the rendered pixel of the outline, obscure and restore unchanged fokusu/geometrii. Test Will uses a visible Strider preview and physical clicks after setting the layout; it still checks the cost and view of the second player.

HandDrawerTests additionally moves the indicator beyond the entire client's surface: pinned hand stays, temporary disappears. UI Toolkit generates the output event itself based on the actual PointerMove. Baseline with the old code played the closed hand; the correction result saves UI-35.

UI-47-CONTEXT : `RegionInspectionTests` check empty region, effect without figures , Nazgûla, garrison, character, public Fellowship and the passage to Mordor. PlayMode performs physical hover/click in both roles and four layouts, refresh after departure figure and inspection without change dice Or a search. `ContextHoverTests` uses both actual server projection, `MusterNetworkTests` recruits when you click empty settlement ; regression includes movement to the target, Fellowship , Hunt , politics, Dismay and Corruption Controls Free Peoples They disappear in public view Fellowship Shadow . 295 EditMode and 41 different PlayMode PASS ; commands, improved initial failure scenario and the watched dropouts save UI-47-CONTEXT Full builds pick up UI-47 .

<a id="h01h14--nowy-układ-polowania-plan-ui-49ui-54"></a>
## H01–H14 — new Hunt (Plan UI-49-UI-54)

The [HUNT_UX](HUNT_UX.md) Matrix sets out new requirements, not tested. H01/H02 belong to UI-49, H03/H04/H05 to UI-50, H06/H07/H08 to UI-51, H09/H10 to UI-52, H05/H11/H12 to UI-53. UI-54 receives all H01–H14 including common H13 (restart/receipt) and H14 (systems, performance and build).

Each acceptance binding ID with actual test method, role, configuration, command and result. D09/F01 –F07 remain a regression; they do not include new descriptions of effects and integration of alerts. Both actual views are required. HTTP/WSS/PostgreSQL , history of the old chain, no leak RNG , click empty route and full cost through additional tiles and game end. Automatic result without decision does not open a window and its own system effect does not succeed with the opponent. 1366 × 768/1920 × 1080 and 100/125% The main decision is to be kept unwinded; log can scroll history.

<a id="h03h05--kości-polowania-ui-50"></a>
## H03–H05 — dice Hunt (UI-50)

UI-50 binds H03 to HuntInteractionTests (exactly ID , minimum/limit , illegal die , withdrawal and inspection of origin) and HuntNetworkTests by real HTTP/PostgreSQL: click/drop , legal zero, compulsory one die , limit and recovery of the receipt. H04: natural meshes, separate bonuses and stability of the controls in HuntInteractionTests ; public both views and private subset in HuntNetworkTests ; rules Kings/Lords/Ugl úk in domain tests. Shadow03HuntTests separates Crebain and earlier traffic without the cost of Resolving; Shadow02StrikeTests includes the throw from the event. H05: automatic box/zero in HuntNetworkTests , no fictitious pre-flip result in HuntPresentationTests and compatibility of the old record in HuntReportTests . HuntLayoutTests checks five decisions in four systems; results and previous failures record UI-50 Integration of logs and alerts remains in UI-53 , measurement/build in common UI-54 .

<a id="ba01--trwałość-i-odczyt-dziennika-ui-38"></a>
## BA01 — Log durability and reading (UI-38)

ActionLogProjectionTests check 2 Regular + 3 Elite, Army/Muster/Will , second movement/Finish , unchanged figure and versions and reaction of the opponent. ActionLogPersistenceTests use real accounts/HTTP/PostgreSQL for preview, rollback, retry, reboot host and no mutations after GET/seen. ActionLogPagingTests includes over 120 public private entries, joint revisions, contractor filters and new step of the old group. ActionLogMigrationTests preserves snapshot and full text after migration 003 → 004 ; GET It doesn't rebuild the index, the record does it atomicly. ActionLogRestartTests reboots the real Kestrel process and isolated cluster PG between the steps. Unity ActionLogContractTests reads the record of the actual answer HTTP Results of full regression, repetition and measurement are in UI-38 .

<a id="g01g18--pozostały-interfejs-partii-plan-ui-56ui-63"></a>
## G01–G18 — Other Party Interface (Plan UI-56-UI-63)

The [IN_GAME_UX](IN_GAME_UX.md) Matrix describes future requirements, not performed verification. G01 and G15 data belong to UI-56; G02–G04 to UI-57; G05–G07 to UI-58; G08–G11 to UI-59; G12/G13 to UI-60; G14 and presentation of G15 to UI-61; G16/G17 to UI-62. UI-63 receives G01–G18 and performs G18.

Each line is linked to the actual test, role, configuration, command and result. Cover covers sources/expirement Effects, cost and resource queue, all sovereign/type selection states, four victories, Help and reasons of inaccessibility and common log/alerty/receipt.

Mandatory boundaries: an empty region with effect requires a click; Visible does not reveal the ongoing selection; reaching the point threshold does not finish the batch before the phase 6 Restart before/after the cost, the cav and the end of the game preserves RNG And the cost. acceptance includes 93 configurations projection/uk order and execution of each unique branch, both actual views HTTP/WSS/PostgreSQL , 1366 × 768/1920 × 1080 with 100/125% , measurements and build by UI-63 .

New test classes listed in tasks need to be implemented. A filter returning zero required new scenarios is not proof. The former D10/D11 and S17–S24 remain regression; their historical results do not receive new previews, data or integration.

<a id="cm01cm18--characters-przygotowanie-i-menu-plan-ui-65ui-72"></a>
## CM01-CM18 — Characters, preparation and Menu (Plan UI-65-UI-72)

Full matrix and required results are in [CHARACTERS_SETUP_MENU_UX](CHARACTERS_SETUP_MENU_UX.md) . CM01/CM02 belong to UI-65 , CM03/CM04 ed UI-66 , CM05 – CM07 ed UI-67 , CM08/CM09 ed UI-68 , CM10/CM11 ed UI-69 , CM12 – CM14 ed UI-70 , CM15 – CM17 ed UI-71 . UI-72 performs CM18 and picks up the whole after UI-63 .

Each scenario requires a real test, page, configuration, command and result. New classes listed in the tasks will only be created; filter selecting zero new tests is not proof. Historical D09/D10/D11/F/G/BA/H is a regression, not a credit of new views.

Mandatory borders: only your own name characters in the list, figure separate from the version, return exceptions, no auto-choice cost/Capability, setup in two moments and private empty Council window. Games also check local Error without exception from the record. The draft save does not approve action, and return after uncertain command retrieves its receipt.

acceptance requires real HTTP/WSS/PostgreSQL and both roles, reboot, configuration matrix and unique behavior branches, four 1366×768/1920×1080 × 100/125% circuits, viewing, measurements and builds from UI-72.


<a id="ba02--historia-rodzin-akcji-ui-39"></a>
## BA02 — Family history action, UI-39

Automatic scripts ActionLogFamily/BattleFacts/CardFacts/HuntFacts/ ExpansionFacts/PrivacyTests read new DTO after actual command . Includes, among others, Regular deletion and use of the same figure as an alternate to the elite, card still hidden in the old post-reveal entry, private metastatic in HTTP also after reboot, cost Narya/Vilya/Ruler/Dismay/Council , two chains Hunt And the final result of Mordor. RegionProtectionCardTests and LordsWeWontGoBackTests They check the hidden discards as a cost. HuntReportTests compare copies of each entry with the log for both parties; SiegeTests verify the premature end and cost of the elite between rounds.

Full manufacturer matrix and limit UI-40-UI-47: BATTLE_ACTIVITY_UX, section BA02. These checks concern data; they do not include manual testing, new views or Windows client launch.


<a id="mc01mc18--mapa-z-konturów-i-kolory-granic-plan-map-12map-17"></a>
## MC01-MC18 — contour map and border colours (Plan MAP-12-MAP-17)

Full matrix with the required results: [MAP_CONTOURS_UX](MAP_CONTOURS_UX.md) . MC01 – MC03 belong to MAP-12 , MC04 – MC06 ed MAP-13 , MC07 – MC08 ed MAP-14 , MC09 – MC12 ed MAP-15 , MC13 – MC15 ed MAP-16 . MAP-17 performs MC16 – MC18 and receives a set of previous cases with actual evidence.

The checks include 105 ID, holes/wodE, source contours and edges, all 232 connection and 50 additional contacts, partial barriers at passes, eight colors and orientations of two bands. UI requires both roles, click/dropu/hovera, small regions, markers/panel and Location. Fate/New Cities/White Hand, a simple gain, change of the batch and older recording cannot leave the national colors wrong or lose the recapit.

The new MapContour classes listed in the tasks will only arise. Zero cases under the filter, the build itself or the historical MapTests do not receive the new geometrii/renderera. Required: layouts, Player measurements, actual server and both builds with open Windows runtime boundary.


UI-40 : BattleArchiveTests and BattleReportPersistenceTests check two rounds, two battle the same region, read after reboots at cards/loss/entry , Stay/retry , privacy and access control. BattleCardTests check AwaitingSettlement and two battle one action . SiegeTests/SortieReliefTests receive reports of four modes, policy, points, garrison/you the reduction of the risk of loss; Strategy02CombatTests includes Scouts before the throw and a real special attack of the Wises. Legacy fallback and the behavior of the passage after the next game do not change RNG . BattleReportContractTests in Unity reads the real answer HTTP from two rounds and old PlayerView No new fields. UI previews receive further tasks.

<a id="ba04ba05--wdrożenie-ui-41"></a>
## BA04-BA05 — implementation UI-41

- ActionLogModelTests: deduplication of 140 steps, order, reaction of the other side and its cost,
  historical composition/version, legacy, old response filter/details/released batch, offline and
  data recovery.
- ActionLogNetworkTests: the actual HTTP/WSS/PostgreSQL for both accounts, over 120 approved
  decisions with normal turn run, all pages/filters and hover/fokus unread. The click only transmits
  the complete version of card. Finish + RemoveExcess check new steps of the same action and the
  correct moment Completed without changing the already attached card.
- ActionCardLayoutTests: four 1366×768/1920×1080 × 100/125%, hit-tests, separated symbols/cost,
  moving and position memory; the new page does not mention the check of the read entry or its
  focus.
- T22InformationTests: Log includes safe projection texts of both sides and retains the Inspect.
  Opening Politics does not close the attached window; the layout next to card checks the conscious
  move of its handle with UI-32.

The results of the actual commands are recorded by UI-41.

<a id="kontekst-akcji-ui-42"></a>
## Background action UI-42

- `ActionContextTests`: all types without location, selected BattleId in action with several battle,
  saved composition and full character route, separate Hunt chains, limited version after late
  reading, offline/Retry/cache report and no later rounds.
- `ActionContextNetworkTests`: actual HTTP/WSS/PostgreSQL, two accounts; two made movements (2
  Regular + 3 Elite and the second army), left to the current draft/goal/ko, old battle after new in
  the same region, old Hunt after another, reconnect, all host families, action without region and
  opening without Log. The data comes from approved engine commands in `ActionContextScenario`.
- `ActionMapLayoutTests`: movement and attack in 1366×768/1920×1080, scale 100/125%, free frame
  area, non-interactive labels, master, closing and no mutation of the game. `ui42-moves-*` and
  `ui42-battles-*` dumps in an isolated screenshot directory.

Results and earlier detected errors document UI-42; automatic tests do not replace the deferred manual acceptance user.

<a id="powiadomienia-ui-43"></a>
## Notifications UI-43

- `ActionNoticeTests`: BattleId/ActionId groups, exclusion of own and automatic steps,
  deduplication, exact version and subsequent pages, description of all decisions, composition of
  movement and released session.
- `NotificationReadTests`: actual files, reboot, server/account/game ranges, merge parallel records,
  corrupt and unavailable disk. The file contains only ID and the version/range of writing.
- `ActionNoticeNetworkTests`: actual host HTTP/WSS/PostgreSQL, two accounts, private policy No card,
  hower, Close golden decision, reconconnect, new client and second profile. Reading does not change
  match see or revision; delayed GET with newer version does not delete new alert, failed report
  reading requires Retry.
- `ActionNoticeLayoutTests`: four 1366×768/1920×1080 and 100/125%, 1/2 bookmark, +N, hover/pin, read
  through the tab/Log, actual hit-tests and still viewport map. ui43-notes-* dumps.

The Fellowship, Hunt and session recovery regressions are run on separate host test instances to ensure that the fixture series logins do not exhaust the production limit. The login limits are not weakened. Actual commands and results: UI-43.

<a id="ui-44--regionalny-wybór-combat"></a>
## UI-44 — Regional Combat Selection

`BattleWindowNetworkTests` uses a real attack with mixed army, Gimlim and left behind rear guard and a hand with non-participating garrison. Checks 200/150 ms, region→window, pin, header move, Close/GET, master, gold tab, read only, Play and double, reveal and reconnect confidentiality. The second client of the same account approves No card during the run of the drag: the old gesture does not play another card and does not move the next decision. Four chips check the actual hit in miniatures and No card.

`CardGestureNetworkTests` also uses miniatures in battle at drop. Existing Battle/BattlePolicy and actual card with costs/reactions remain part of acceptance. Full register of results and regression repair: UI-44.

<a id="ui-45--rzuty-i-kolejność-strat"></a>
## UI-45 — kicks and order of losses

BattleResolutionScenario creates two written states by the real engine: ui45-rerolls has one each Leadership and two boxes of both sides; ui45-losses has two hits and no regular Gondor free. Does not add endpoint nor RNG test to the server. BattleResolutionNetworkTests checks gestures, legal zero/maximum Metallage, reconnect, confidential log before second choice and both natural results after resolution. The second scenario checks no replacement, change order, reuse of the fallen, four systems and recovery of lost response without second POST .

BattleResolutionTests includes natural 1/6 at various bonuses, a site without changing the region and excess hits. BattleLossPreviewTests performs Event and DestructiveEvent with the right fate of the commander.

<a id="ui-46--koniec-bitwy-i-odczyt-raportów"></a>
## UI-46 — end of battle and read reports

BattleEndingScenario prepares six states of ui46: retreat, report (two rounds), shelter and siege/no-regular/weakness. The last uses the true weakness of Denethor from Kings. BattleEndingNetworkTests performs actual gestures of both clients, checks the illegal goal, partial entry, Stay after shelter, cost siege, reconnet and lost retrieve without the second POST. The report without the throw is opened with Log, report two rounds from the last shot; the passage back only remains read-out. The four systems have hit-tests and dumps. StrongholdNetworkTests covers shelter/entry/excess, Siege/Sortie/Relief and limits. BattleConclusionTests checks the text of the cost and invariability of the report.

<a id="ui-47--wspólny-przebieg-i-granice-interakcji"></a>
## UI-47 — Joint course and limits of interaction

`LiveActivityNetworkTests` Starts before the first command : FP Move 2 Regular + 3 Elite to Osgiliath, and then second army to Westemnet for the same die . SH attacks with North Ithilien; both sides choose the real card Combat. Natural castings/przerzutach and aggregate losses SH continues, FP retreats to Minas Tirith, SH Every public step of the opponent opens card from a red alert without changing the search. card SH Both clients read the full report and the former composition of the first march despite subsequent losses.

BattleReportNetwork includes the late GET after Close and error/Retry report from real API. ActionNotice checks the newer step during reading and independent local status of the devices. The regression of the report version distinguishes the whole command with automatic effects from a single event. Hit-tests include the holding of the cards reaction, the double click Balroga with preview, the Fellowship tag and the inspection tab under another window. Results of the full BA01-BA16 matrix are in UI-47.

<a id="h01h02--wykonanie-ui-49"></a>
## H01/H02 — UI-49 execution

HuntPresentationTests separates the allocation from the previous report, the current Guide from the tile and the public facts from the internal resumption point. HuntLayoutTests checks three stable steps, focus, retention of the reroll and the older page at the new entry, no automatic opening of the result, no report and no change of the batch. Four layouts include five decisions and a priority portraits, no scroll form. HuntNetworkTests uses both real projections and checks the owner, concealing other people's options, Close by GET/reconnect and one receipt when the decision is given. Results, filters and dumps: UI-49.

<a id="mc01mc03--kontury-map-12"></a>
## MC01–MC03 — contours MAP-12

- MC01 : `python3 scripts/export_map_geometry.py --check` restore the resource and the source
  report; 105 identical ID , 323 path from index/SVG ID , the public transformation of the source
  and PNG . `MapContourDataTests` check ID , 106 rings, 26 455 points and compatibility of Rivendell
  anchor.
- MC02: anchors of all fields hit their own region; 282 contacts contain all 232 legal couples,
  without adding legality. Source point [1839.875,1028.5] in the Gorgoroth cut, the external and
  concave cut do not land. The same point on the common line has a stable result after the reverse
  of the data order and pan/zoom.
- MC03: foreign/powtor ID, 0 mask, evil version/scale, open or intersecting ring, anchor in water/NaN,
  damaged common edge and no origin end in import/load easy to read. `scripts/test_map_geometry.py`
  also includes unsupported command SVG, application of sections and unseparate contacts.

The evidence of the launch and the limit of the later renderer is in MAP-12. These scenarios do not yet include colors, target layers of the site or the final gestures of MAP-13–MAP-17.

<a id="g01g15--fundament-ui-56"></a>
## G01/G15 — foundation UI-56

InGameProjectionTests compares the diagnostics with the true validation of the Ring, Awakening, Corruption and Ruler, also in a different window. Checks the omission of Captain Eyes, separate time limits/Effects, card table without modifier and both ranges Corrupted Théodena. Eight configurations Lords/Kings/Fate has a compatible sum of regions/thresholds, without automatic Winner. Private selection at Visible and foreign hand does not change the projection of FP; ExpansionPrivacyTests retains a full matrix of secrets/version.

InGameProjectionResourceTests adds the cost of Council+ die chief, council limit, all three actual Keeper windows, Dismay, legal special candidates dice and optional random purification after politics. InGameProjectionPersistenceTests import v1   JSON real PostgreSQL, read both sides by HTTP, performs preview and restart host. Checks the whole state/revizj s/ RNG and invariability of raw old writing; the second scenario retains the single cost of Ruler and the idempotent receipt. InGameContractTests in Unity reads a record of the actual answer HTTP  (InGameWire.json) and neutral fields of the older contract. Results, previous failures and commands are in UI-56 These tests do not replace subsequent interactions and measurements G18 in UI-63.

## H06–H08 — UI-51

HuntInteractionTests distinguishing Damage/Corruption/Heal/Separate, Unestablished Separate ? and independent Reveal/Stop.   HuntResponseTests checks out the obvious consequences prior to selection, Hobbit's return, unknown victim, and no ID In payoada. HuntReactionPresentationTests cover cost/ a source without a hover, stability of the objects and privacy of the Fate version. HuntReactionDescriptionTests binding texts Staff/Mithril/Axe/Horn/Crebain/Worn with a real domain window. HuntResponseNetworkTests receives all three gestures for three answers to the real server, views of both players, reconnect/receipt And late click. HuntLayoutTests measures texts and objectives in 1366 × 768/1920 × 1080 with 100% / 125%, together with parallel open Fellowship. Exceptions base/Lords/Council/Fate and actual filters and results are saved in UI-51.

The UI-56 regressions additionally perform real Wormtongue, Denethor’s Folly and Jerkír plays in both projections. Separately, the use of the Ring and wear of the Ruler isolate the reason for the refusal without false information about the untested target.

The UI-51 response inspection bypasses its own Hunt panel and keeps its hover without pinning; the deferred PointerLeave does not delete the newer tab hover. The actual CardPackNetwork test includes another Mithril/Axe, inspection and click, and the BattleLayout/FloatingWindow retain the pinned positions from the windows.

## MAP-13 — MC04–MC06

- MC04: MapContourBorderTests checks the cover of 323 tracks and all 14 348 segments, 360 parts,
  sources and 232 Legal/50 additional pairs. Negative cases reject no path/source, overlapping
  ranges, other hash geometry, incorrect normal side and crossing outside the graph. Python
  additionally controls SHA-256 all inputs, set of 9 sectors, repeated entries and confusion of the
  shore of water from the land border.
- MC05: partial boundaries Morannon/Dagorlad, Minas Morgul/Gorgoroth, Minas Morgul/South Ithilien,
  Moria/Dimrill Dale and Goblin's Gate/High Pass retain transition and mountain passage.
  Nurn/Gorgoroth is passable only through western contact `0212`; `0213` remains completely black.
  Rivendell/High Pass and High Pass/Old Ford remain without the edge of motion.
- MC06: eight source colors, Grey Havens/Ered Luin with two bands, internal Gondor/Gondor, two
  neutral fields and no bar and water bands. Change Control does not change the band. Python
  reverses all 323 paths and checks normal conversion without changing the sides of the regions.

The final [Source caps](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/sources/map13-review/README.md) was viewed all over board, in 9 sectors and 5 details. The actual command results are in MAP-13. The domain regression remains BoardTests.PrintedBordersDetermineMovement; the graph code has not been modified. This acceptance catalog/pallets, not the final return declaration and gestures.


<a id="ui-57--effects-przy-źródłach"></a>
## UI-57 — Effects at sources

EffectsPresentationTests includes: grouping of sources of both parties and card without a modifier, different time limits/targets , public capacity, zero sources, no mapping of Saruman's expiration condition on Orthanc, no automatic hover of an empty region, stable controls and ended, own permission, Close cost without loss draft full pinned card After expiry and alternative costs of Jerkíra. EffectsNetworkTests uses five unique matches EffectsScenario real HTTP/PostgreSQL: Protection (partial cost rejected by the server without changing the entire projection, two discards/reconnect/single commit), Jerkír (chosen Ring as last click), natural expiration of Wormtongue upon arrival of Gandalf (pinned after reconnect), hower both roles/empty region and movement FP to protected Lórien by the actual final click of the map. No atrap of the backend. EffectsLayoutTests cover 1366×768 and 1920×1080 , scale 100/125% , multiple sources, independent terms, Location, ability, second party, zero/ended , full cost, window moving and focus behavior. Fixtures layouts are openly presentational.

<a id="ui-52--h09h10-w-rzeczywistych-klientach"></a>
## UI-52 — H09/H10 in actual clients

HuntAftermathNetworkTests combines Gimli's loss, Merry's choice, his ability, return to the empty Dimrill Dale, Reveal by two fortresses and two additional tiles. Both sides observe the same report, and die remains Resolving to the end; two lost receipts do not repeat the cost. MordorNetworkTests has been extended to replace Stop by Mithril, Eye with movement/events, Breaking and both finals with a response recovery and late gesture. MordorLayoutTests includes Guide and seven portraits of forced deletion in four systems. Sources, full screenplay matrix → test and actual results are found in UI-52.

<a id="ui-58--zasoby-koszty-i-ponowne-połączenie"></a>
## UI-58 — resources, costs and reconnection

ResourcePanelTests includes opening after ID, Close, both perspectives, conditional Recovery, Ruler binding and neutral Council, maintaining its cost and no automatic selection. ResourcePanelNetworkTests performs actual clicks of all Keeper/Lesser choices and both discards in four layouts, Dismay on dice opponent, drop Ruler and both Denethor selections and changing the Ring without using dice. ExpansionTokenNetworkTests starts Council by clicking the chip, separately selects the application and recovers the exact cost of the Chieftain input. ExpansionDecision/Matrix remain a regression of all configurations and full pool. Actual filters and results: UI-58.

<a id="ui-53--historia-polowania"></a>
## UI-53 — History of Hunt

H05/H11/H12 received HuntActivityTests (version with implications, next unread decision, older ChainId , no report, Mordor, late GET ), HuntActivityNetworkTests (actual ui53-none/miss/cancelled/reaction/history: both views, four systems, reaction SH at a separate cost, error/Retry, newer growth during GET and restart local reading) and HuntActivityPersistenceTests . RealHostRestartKeepsAtomicHuntFactsSeparateFromActorFilteredNoticesAndNewChains (two actual host reboots PostgreSQL , actor filtering, fixed cost/RNG , the unchanged old string after the new one). UI-53 .

<a id="ui-60--wynik-na-planszy-i-dostępność-podglądów"></a>
## UI-60 — score on board and preview availability

G12/G13 received VictoryPanelTests (provision of thresholds and sources, four results only from the server, neutral older contract, stable controls), VictoryPanelLayoutTests ( 1366 × 768/1920 × 1080 , 100/125% , pages without scroll, fokus/hover , drag, location and banner), VictoryPanelNetworkTests (two actual clients HTTP/WSS/PostgreSQL: military FP/SH/simultaneous threshold, loss of threshold by reflection of Moria, Fate+New Cities+Kings, control in siege Both Ring Finals, Retra, GameFinished and full JSON without ID Separate scenarios VictoryScenario do not share the altered parties with the older regression. They are prepared positions of legal components; each cost/result/draw performs a normal server. Fixture of the system examines geometry, does not replace acceptance rules. Existing VictoryPersistenceTests reboots the host before final injuries and after both results, checks one RNG/cost/receipt and the refusal of subsequent mutations. Fellowship/Mordor/T22 Assertions were transferred to the same banner, maintaining control of the lack of premature result and the actual expectation of readmission.

The Fellowship system's regression revealed the cover of the region's portraits by pinned window. RegionHover raises the new/re to the top of the open, without changing the state of the game or the order of the windows at the same preview. The FellowshipGroupsHuntAndMordorFitBothSizesAndScalesWithoutScrolling test uses a real hit-test and checks the selection of all seven characters in four layouts. Results and previous failures remain in UI-60.

<a id="ui-65--cm01cm02-wykaz-wersje-i-trwałość"></a>
## UI-65 — CM01/CM02: list, versions and durability

New CharacterRosterProjectionTests include both views, 8 configuration, privacy of the whole JSON Shadow at different versions of the Council, the compatibility of all available targets/costs with the true validator and the independence of the conditions from the decision window. CharacterRosterLifecycleTests include Grey/Keeper After the death of historic Minion, a living Gandalf in the garrison, Aragorn, both directions of a one-time Witch-king conversion, no Mouth swap and no return after death, Taken Alive, both awaiting the Hobbits' return, Fate and przebudzonego/wyeliminowanego A sovereign.

Five CharacterRosterPersistenceTests : real HTTP/PostgreSQL both sides, full snapshot before/after GET/preview , Senior schemaVersion= 1 without migration recording by reading, reboot, identical payload/b stem SH at the Private Council, as well as White's entrance, Witch-king's replacement and Hobbit's public return before/after Restart and retry without additional kosztu/RNG. CharacterRosterContractTests reads the actual two Wire Fixture HTTP and check neutral old DTO . acceptance : 75/75 tests. NET ( 27 new and 48 regression) and 5/5 New Editor PASS ; actual commands, previous failed attempts and their causes describe UI-65 .

<a id="map-14--mc07-i-mc08-obraz-i-warstwy"></a>
## MAP-14 — MC07 and MC08: image and layer

- MC07: fit and maximum zoom show common white transitions, bands on the right sides, eight colors
  of nations and clearer black barriers. Black Gate/Minas Morgul remain open, Erebor retains the
  retouch of the Lonely Mountain. The control of settlement is a separate symbol, water does not
  have a hit test region. View: all, both entrances of Mordor, Erebor, Moria, High Pass, Grey Havens
  (nation/neutral/water) and Nurn (water and partial barrier).
- MC08: source/selection/target/attack/protection do not re-paint the line. Filling changes the
  pixels of the region under the borders, the attention is separate. Pan/zoom/choice does not
  regenerate static paths. Nation changes touch the associated bands; Control remains a separate
  state. Legend opens on request and does not change the game's selection or status. Header, eight
  names and eight samples have a real hit-test over Zoom/Effects controls in four systems; Escape
  closes preview.

Evidence: MapContourRenderingTests save images before/after And pixel assertions on source coordinates. `scripts/review-map-client.py` collect 26 screenshots and 12 real Development Player measurements at 1366 × 768/1920 × 1080 and 100/125%. Separate reading format/graphicsFormat and texture blocks distinguish memory GPU from the editor's bedspread; the Player limit remains independent. Results and possible limitations: MAP-14 , configuration: [MAP-14-RENDERER](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/sources/MAP-14-RENDERER.md) .

<a id="ui-59--g08g11-portrety-próba-i-oczyszczanie"></a>
## UI-59 — G08–G11: portraits, testing and purification

SovereignPanelTests includes one opening according to ID, the behavior of draft after Close, the inspection of another target without moving the controls, 3/5 portraits and their stability, privacy despite Visible, ⁇ , explanations of their own terms, token variant and CorruptionRemoval interrupting battle.

SovereignPanelNetworkTests uses two real-time HTTP/WSS/PostgreSQL. clients Separate SovereignPanelScenario clients include:

- ui59-awaken and ui59-awaken-variant: specific die, portrait, Close/reconnect, Stay, single
  identity Awakened, return the tiles, token or its lack, Ruler still out of the pool for the proper
  Recovery.
- ui59-sequential: private score despite Visible, another Draw, only last Stop tile, Close/reconnect
  and no opening of the usual Hunt.
- ui59-replacement-kings: public omission of Eye, Kings ending the trial, one cost and refresh the
  open preview of both sides.
- ui59-cleanse-remove/decline: real AdvancePolitics opens the optional random non-Kings return,
  refusal preserves the tiles; reconnect does not repeat the cost.

SovereignPanelLayoutChecks saves the dropouts before the Assertions and checks 1366×768/1920×1080 at 100/125%, real pickup of controls, portraits and all fixed shortcuts (including Cards, Victory and map legend), no cross-section of their full boundaries, no dock border, and no ScrollView/TextField. The smallest viewport also checks the maximum height of 216 natural decision panel, consistent with the regression of pure geometry. The existing full-pool matrix also includes a compact dock; ExpansionCorruption/Decision/Matrix and Kings/persistence filters remain required regressions. Actual commands, results and limitations: UI-59.

<a id="map-15--mc09mc12-wykonane-na-contours-2"></a>
## MAP-15 — MC09–MC12 made on contours-2

- MC09:
  `MapContourNavigationTests.BothRolesInspectContoursAtFourLayoutsWithoutChangingProjectedState`
  checks empty hover, outright click/fokus, public force inspection/Fellowship, controls only your
  own website, 0 commands and non-variable projection.
- MC10: `MapContourInteractionTests.BothSidesClickAndDropOnContoursCommitTheSameArmyStep` compares
  specific soldiers, cost and step; `BothSidesAttackOnTheEnemyContourWithoutAnotherConfirmation`
  confirms both sides' attack. `WaterAndDistantTerrainNeverConsumeTheArmySelectionOrDie` rejects
  sea, mountain contact and remote area without releasing the resource.
- MC11: navigation checks all 105 anchors at three zooms and you, both sides of the four edges,
  Gorgoroth hole and Old Forest concave, Osgiliath and Dol Amroth. New interactions retain marked
  army after exiting the panel. Other families use indicated common drafts.
- MC12: `ReconnectCancelsCapturedArmyDropAtTheSameRevision` and
  `AnotherDeviceCommitCancelsCapturedArmyDropAtTheNewRevision` require 0 commands from the old
  gesture. `BattleNoticeAndRecordedActionUseCurrentAnchorsWithoutAutomaticCameraMotion` checks the
  notes, historical source/target, reconnect both projections and secrets. The pickup for step and
  declaration covers MovementNetwork/FellowshipRoutes.

Separate results of new classes: `.artifacts/map15-interaction-final.xml` 5/5 and `.artifacts/map15-navigation-final.xml` 2/2, without omission. Fresh attempts of UI-54/UI-59 on the same renderer and final regressions are arranged in [covering matrix](MAP15_INTERACTION_COVERAGE.md), without presenting fixture-only as proof of the actual server.

<a id="ui-54--odbiór-polowania-i-integracji"></a>
## UI-54 — acceptance Hunt and integration

`ui54-hunt-protection` — alias of independent real screenplay CardPack: Mordor with unset Shelob, Mithril, Axe injuries. `HuntRecoveryNetworkTests.LostReplacementReceiptKeepsOneTileCardAndPaidContinuation` loses only HTTP response after commit Mithril, retrieves the receipt by Check result and checks one new tile, one card, unchanged die Resolving, no premature throw of Shelob and both public reports. Close/reconnect does not reopen the same window; further Axe/Ring finish the same paid move. Four sets of both roles show only response to the owner and neutral information to the Waiting opponent.

## UI-66 — CM03/CM04

`CharacterRosterPanelTests` checks UI65 Wire, White return, sections, movement/level Fate separation, data ownership, older empty contract and completeness of 43 version/110 texts. `CharacterRosterPanelNetworkTests` performs the real White input at the attached card and seal of the future version; it checks the same line, focus and both views after the reconnect. The remaining attempts retain the selected die, camera and full view of the server during hover/fokus/pin/Enter, you reject by the tab and use one preview of mapy/Awakened. `CharacterRosterLayoutTests` requires a real hit of all the visible buttons of both roles in four systems, without ScrollView. The results and re-views are saved in UI-66.

<a id="map-16--mc13mc15-publiczne-nakładki-i-historyczny-zapis"></a>
## MAP-16 — MC13–MC15, public overlays and historical record

- `MapContourOverrideTests`: exact delta cache 9 Fate, 2 White Hand, 0 New Cities/Control; reset
  Setup/Clear and return to original metadata; unchanged 232 pair, all contours/classifications at each
  configuration and role, no hand/flag dependencies.
- `MapContourRecoveryTests.BothRolesRenderServerOverridesAndRetainThemAcrossReconnect`: the actual
  both views of Base/Fate/New Cities/acquisition, reconnect without the new commands/reconstruction band;
  PNG layers and sediment.
- `RemovingWhiteHandChangesTwoBandsButRetainsCaptureAndItsPoints`: command accepted by the server
  removes the overlay, both updates by WSS rebuild exactly 2 band, retain control of Shadow/2
  VP/topologie.
- `SwitchingSavedGamesRestoresBaseMetadataForBothRoles`: navigation from the lobby to New
  Cities→Base→White Hand→Base, new boards with common contour, no residue of sediment and color and
  no revision change.
- `HistoricalPaidStepKeepsItsDraftAndRecoversUncertainCompletionAfterServerRestart`: existing
  `paid-army-step.json` (commit `2a046ab`, 3, SHA-256
  `02222cf2b15a80cc12533bd496ea8ad67db8411ae909eea9322a8ce6d1f958b6`) returns to revision 1 with
  Resolving/1 the remaining step. Local incomplete selection of figures is preserved when the client
  is re-created. Only the public Finish creates a revision of 2; the lost response and restart of
  the process are recovered by the receipt, without the return POST, and the apparent duplicate
  still does not seem to cost.

Do not substitute these fixtures for API production; seed is exclusively in `Wotr.Scenarios`, and tests require `scripts/test-client.sh` and a real base.

<a id="ui-61--pomoc-kontekstowa-i-wyjaśnienia-dostępności"></a>
## UI-61 — Contextual assistance and accessibility clarifications

G14:   ContextHelpTests covers a map of themes, sources/symbols, configuration, privacy, lack of snapshot, stability of controls and existing server InGameWire.  G15:   ContextHelpLayoutTests check 186 actual projections 93 configuration and 4 Resource projections; each transmitted cause and its Met remain faithful projections. ContextHelpNetworkTests uses independent ui61-ring/ui61-full: prepared Ring+physical die survives Help/Esc/reconnect Enter doesn't send command, real UseRing updates the limitation in the attached aid. Both roles full expansions are documented in 1366 × 768/1920 × 1080 with 100% / 125%, with real clicking of the Hunt source, focus, pin and offset.

The PlayMode 5/5 PASS and the 17 PNG tests are described by UI-61. The full 473/473 editor and the focused 32/32 re-use after correction of Fate are saved separately. The common FloatingWindow/ContextHover/Sovereign regressions have been passed by 14/14, Effects 7/7 and ExpansionMatrix 2/2; details and previous failures are saved in the task.

<a id="ui-68--przygotowanie-wersji-i-przewodnika"></a>
## UI-68 — version preparation and Guide

The new `CompanionSetupPanelNetworkTests` creates normal games by API (Create/Join/Confirm/Start): base, Lords, Council+Kings and Council+Kings+Fate/New Cities. Check server options, view without changing the selection, required Guide/Gandalf, closure/reconnect, one approval and separate homeland. Four layouts compare all seven characters with actual pickup, label measurement and without ScrollView decision.

`CompanionSetupRecoveryTests` (part of the same test class) uses an existing separate ReturnScenario/council. Private Save draft reboots the client and the server process before selection, then cuts the actual response after commission. Restart recovers the receipt with the same ID , without the other POST or passing through CouncilHomelands Designed Assertions also compare unchanged projection SH during local changes and undisclosed versions Gandalf/Gimlego After choosing the Strider. acceptance UI68 : `CompanionSetupPanelNetworkTests` made three new methods: a simple start of five configurations, seven comparisons in four layouts and reboot client/server before and after the lost answer. All PASS , with seven ExpansionDecisionNetworkTests . CM08/CM09 include Enter/Esc , privacy Shadow and exactly one double-click search. Details of commands, images and boundaries: UI-68 .

<a id="ui-67--cm05cm07-akcje-characters"></a>
## UI-67 — CM05–CM07, action Characters

`CharacterRosterActionTests` covers the property of figures/obverse, maintaining a specific cost, rejecting an outdated option, Council without a fictional dice, one host and White Rider reaction after the transformation of a stable Gandalf. Panel tests do not replace verification of domain rules.

`CharacterRosterActionNetworkTests` uses eight independent aliases `ui67-*` in the script tool: travel FP and SH, entry White, Balrog, Ruler, Council entry/travel and Fate Gimli. New attempts combine the list with map, drop with clicks, check the cost of the paid sequence after the reconnect, stationary Saruman, clear choice of capabilities, three recruits in one commission, and cost of token and dice. Lost response of the White input leads to the creation of the receipt; the same POST does not pay twice. Three decisions measure all four systems and the actual hit-test, without scrolling and cover the fixed shortcuts.

Prepared regressions: CharacterNetworkTests, TravelGestureNetworkTests, ExpansionAbilityNetworkTests, ExpansionTokenNetworkTests and inspection UI66. siege, Shadowfax, Mouth and source restrictions also retain the required domain tests. All results record separately UI-67: 37 unique Play and 10 methodsWith cases, the editor PASS. New tests include actual HTTP/WS/PostgreSQL, loss of response, reconnect, exact costs and full controls in four systems.

<a id="ui-62--g16g17-historia-i-odzyskiwanie"></a>
## UI-62 — G16/G17, History and Recovery

InGameContextTests checks the host selection, the range of historical facts and the names of the preserved costs. InGameContextNetworkTests includes eight families in both projections, closed cost Effects, old log and Help at the new decision and invalidate the gesture by the second device. The parts of Draft/Recovery from this class restart the actual server and client: before selecting the Ring wall and after the commit Ring, the spoiling tile and the game end. They require a receipt, no recommit, the behavior of cost/parent and identical projection after the backdrop.

Seven new methods have passed the HTTP/WS/PostgreSQL: batch history/hosts/ the second 3/3 device and the reboots 4/4. The right private choice requires the completion of the real selection of two tiles; prior to Choosing, the visibility of your own tile does not entitle you to choose it. Tests control the exact second ID, the parent of the trial and the absence of both private ID in the public record.

The final full Editor has 516/516 PASS, including 26 required cases UI-62. The entire set of required Play has 39/39 PASS; re-fixing the gold alert battle retains the ratings of both roles. Four new cases of source names check the common history field and DecisionNotice; the new 17 PNG is re-watched. Detailed results, filters, previous failures and the date of re-use of the separate dragons HTTPS/WSS are in UI-62. The new images/profile four UI-63 systems remain a separate acceptance.

<a id="menu-przy-planszy--ui-70"></a>
## Menu at board — UI-70

UI-70 : BoardMenuTests check the saved Error on the correctly completed Task, both logs, unchanging identity/payoad at Retry, cleanup copy error and entries added during the previous I/O. BoardMenuLayoutTests They're checking four systems, picking up, all the outside sequence. press/release One recipient Esc, a helmet key and a focus. BoardMenuNetworkTests use real HTTP/PG: delayed/failed local record, Games during lost response, live updates at Menu, offline, final inspection and route sketches, cost card losses, Hunt and private versions of the setup. ReturnScenario make separate copies of ui70-*; no test changes the rules for fixture.

Final acceptance : 11 Play new and 5 Return/Settings and 4 FloatingWindow , all 20 PASS Full Editor 516/516 contains 10 BoardMenuTests and 5 UI10DraftSessionTests. The actual reattach restores the public focus receiver; the helmet Esc does not close the second window before KeyUp Four finals were watched. PNG ; Assertions measure each button and cover of the entire click. Accurate evidence and preserved attempts 8/11 and 10/11 describes UI-70 .

<a id="map-17--pełna-macierz-gęste-siły-i-rzeczywiste-piksele"></a>
## MAP-17 — full matrix, dense forces and actual pixels

`MapContourAcceptanceTests.BothRolesLoadAllEightExpansionCombinationsWithIdenticalPublicContours` checks both roles of all eight Lords/Kings/Fate sets, 105 regions, 323 path and 232 pair. Separate RecoveryTests include New Cities/White Hand, change of batch, older schema-3 record of paid step and uncertain command after resuming a real backend.

`DenseFieldAndGarrisonRemainDistinctDuringMouseAndFractionalWheelAtFourLayouts` has 23 figure in Helm’s Deep: 7 units/dow boaters FP and 3 Companion in a garrison, 10 units SH and 3 I'm in the field. role/four systems, real wheel ± 3/ ± 0,25 , right drag and panel.Pick with the card attached retains the status and zero commands. `CardViewportLayoutTests` measures the full both effects with toolbar, panel boundaries, Pick three points of the last line after a refresh, access to Victory and unchanged Pin location.

Full Play was performed in 42 with fresh hosts, maintaining the production limiter of login: 440 names/variant PASS; the new card regression gives 441 unique, without double counting of re-entry. The final T36 includes the full base and expansions at 125%/reduced motion. The driver completes the previous inspection with a real gesture; when Close blocks another window, clicks the exposed handle of card and only raised Close.

The final editor 478/478, .NET with PostgreSQL 3723/3723, 31 with specific methods 13 with families interaction, Player36/PNG42 matrix, 3 builds and dragons macOS PASS. Python HTTPS/WSS with two accounts and verification CA/localhost is a separate proof to Unity HTTP/WS. Accurate XML, primary failures, repairs, SHA and limitations include [MAP17_ACCEPTANCE](MAP17_ACCEPTANCE.md).

<a id="ui-69--cm10cm11-i-restart-obu-decyzji"></a>
## UI-69 — CM10/CM11 and restart both decisions

CouncilHomelandPanelNetworkTests has six methods: identical public projection SH with 0 or 5 private candidates; 0/Stay ; one departure and both token elections; 2/5 to leave with change and withdrawal; both Gimli's homelands with New Cities on/off ; layouts of both roles 1366 × 768/1920 × 1080 and 100/125% ; and five generations client/server for draft and both separate receipt.

Screenplays ui69- * they are created by normal preparation and a real choice of versions, without manually inserting decisions. The privacy comparison normalizes only MatchId two copies of the same initial state. Pointer/drop and ✓ maintain the actual hit, the system measures full subtitles and saves eight PNG Manifesto packages limit login to 12,  10   and 8 (recovery spread out into five hosts). Results and previous unique error ID fixture saves UI-69.

acceptance UI-69: all six new methods, thirteen targeted regressions and two full PASS parts; four of the two roles have eight watched PNG. The exact filters/XML and fixed fixture errors and the scraped USS are in the task.

### UI-71 — CM15–CM17

BoardCompletionContextTests covers historical versions, lack of old obverse, Guide, private Targets and priority Hunt/bitwy. New BoardCompletionContextNetworkTests includes 6 actual methods: both views of the transformation during the new entry, secrecy setupu/public Guide, Hobbit decisions at Menu/Settings , White Rider/Awakened , Council entry by Games/3 reboot generation and old press at second approval board Separate transport HTTPS/WSS uses a trusted CA Unity is checked separately by HTTP/WS/PostgreSQL. Actual filters, results, re-uptakes and PNG : docs/tasks/UI-71.md.

acceptance UI-71 : 33/33 the required Play methods, 13/13 Full Editor cases 542/542 , 7/7 PNG and 399/399 unchanged domain cases in full 3726/3726 PASS . Separate fresh HTTPS/WSS dragons are credited with trusted CA ; Unity tests benefit from HTTP/WS/PostgreSQL. Accurate filters, reusing identical methods and limits: UI-71 . .


<a id="ui-63--gęsta-inspekcja-i-bezpieczne-wejścia-profilu"></a>
## UI-63 — dense inspection and safe profile inputs

`ui63-dense` prepares full physical pools, five awakened sovereigns and six canonic sources Effects. `ui63-profile-views.json` export contains eight Projection projections.For, without full status and deck. A separate copy of SH shows five Dismay tokens. Victory copy has four gained settlement per page, 6 points FP/SH and no Winner; checks the threshold FP before settling and actual pagination of regional names. This controlled preparation, not a declaration of the play of the entire batch.

Three InGameProfileExportTests check roles, secrecy, export purity, canonical sources, real points and roundtrip. New dense Play passes both roles and four layouts, actual portraits and simultaneous Effects/Help with a phase-based approach, ID decision and both server revisions. The presentation driver performs all eight sequences separately and checks the effects after the render-end; the dense profile changes only public Corruption 2 ↔ 3 local clone. Results, failures and boundaries: UI-63 .

<a id="końcowy-odbiór-ui-63"></a>
### Final acceptance UI-63

Final acceptance UI-63 maps G01–G18 to 165 unique methods and 24 reports. Required filter36 and final44 of seven batch retain separate sets. Window-stability8 includes the restoration of the actual loop card four samples FloatingWindow , matching both effects card and two samples cards/historical view. After the amendment they all passed; original FAIL remains in a separate report. New Siege, full costs and private views still result from actual commands, not from offline measurements.

<a id="ui-72--pełny-katalog-przygotowanie-i-menu"></a>
## UI-72 — full catalog, preparation and Menu

`BoardCompletionLayoutTests.FullRosterVersionsAndHomelandsKeepBothViewsReadableInFourLayouts` passes 40 views: full catalog of both roles, normal lobby Lords, Council and Fate/NewCities and five homelands. Checks each ability, full tooltip, version and Guide, click/drop/Stay/Esc and no private Shadow form. Re-GET of both projections confirms no mutation.

`MenuSettingsAndHelpPreserveBattleAndHuntDraftsAcrossFourLayouts` passes 16 with real view Losses FP and HuntReroll SH. It fixs unapproved ID with selection, opens Menu/Settings/Help, switches Reduced motion and closes by Esc. It requires identical SavedTurnDraft, empty shipping log, zero POST and one tray host. Both new methods have 2/2 PASS; Player measurement remains a separate proof in UI-72.

<a id="zastosowanie-widoku-i-diagnostyka-połączenia--qual-09-2026-10-15"></a>
## Application of view and call diagnostics — QUAL-09 (2026-10-15)

Risk R09 review: the exception of the viewer's view went to the transport loop as ‘Reconnecting’, and the version of the view was moved before it was applied. After the error, the same view was rejected as already accepted.

**Contract.**
- `ViewVersion.IsNewer` only evaluates the view, and `Commit` saves it after real use.
- `MatchConnection.Apply` calls every `Updated` observer separately. The version only moves when
  everyone has used the view.
- One observer fails to stop the others. Gives the status of `DisplayError` ("Display error ·
  Retrying"), rather than "Reconnecting".
- The same view returns on the next server notification (at most 5 s), at most 3 times for this
  version. Then the client waits for a newer view; Reconct gives new attempts.
- After successful application, the status returns to "Live".
- `TurnInteraction` saves its version only after drawing, so the repetition starts from the
  beginning. Steps with side effects are idmpotent: automatic “No card” for decision, recovery of
  the pending command after `commandId`.
- Observers must use repeated view and be self-taught. This contract is taken over by QUAL-10/11.
- The older response of HTTP does not replace the newer view. The newer private version of the same
  revision is applied.

**Diagnosis categories** (`ClientDiagnostics`, one line with prefix `[wotr]`):
- `presentation`, `transport`, `authentication` and `compatibility`;
- plus match, revision, error type, server status and code and error location;
- without tokens, hands, hidden data, exception content and view.
- 401 and incompatible recording end up re-entering the correct status.
- 429 and 503 are waiting at least given `Retry-After` (highest 60 s).
- The reconconnect has a limited backoff (1–15 s) with a jitter ±20%, without reset the journal and
  `commandId`.

**Tests.**
- EditMode `ViewApplicationTests`:
  - the throwing observer, the second observer and the re-opening of the same view;
  - the test limit, newer view and Reconconnect;
  - unrecorded assessment and private version;
  - a line of diagnostics without secrets.
- PlayMode `ViewApplicationNetworkTests`: a real backend, an injected error at the Shadow client
  after pass Free Peoples, “Display error”, then return on next notification.

<a id="aktualizacje--dist-07-up01up12"></a>
## Updates — DIST-07 (UP01-UP12)

| ID | Situation | Expected result |
| --- | --- | --- |
| UP01 | Newer optional version in directory | Version and size bar; zero bytes of the package before `Download update`. |
| UP02 | `Later` | The bar disappears until the next start; the newer version shows up again. |
| UP03 | `Download update`, then `Cancel` | The game continues, the consent withdrawn, the part of the file is left; another `Download update` resumes. |
| UP04 | Minimum deadline | Downloading after 0–10 min without asking, counting by server time, the game lasts until the deadline. |
| UP05 | Date expired (also without network) | `Update required` window; new motions halted, visible batch. |
| UP06 | 409 `ClientUpdateRequired` from any request | The window immediately; the lobby and waiting room stop asking questions; the package is taken by itself. |
| UP07 | `Restart and update` with open batch | Save like `Quit game`, installation after exit, after start of simple login and retrieve the receiver. |
| UP08 | Write Error before reboot | The game is opened with the message; reclick re-writes. |
| UP09 | `Decline and quit` | Save and close without installation; the next start shows the selection again and uses the downloaded package. |
| UP10 | `Repair installation` | Confirmation, download the current version, `Restart and repair`; the withdrawn version proposes an update. |
| UP11 | Bad signature, alien host, bad SHA-256, no space, redirect | Message refusal, running version unchanged. |
| UP12 | Second copy game | „War of the Ring is already running”; the first copy shall proceed. |

Automatic cover: EditMode `UpdateCoordinatorTests` ( UP01 – UP06 , UP09 – UP11 without UI ), `PackageDownloaderTests` , `ManifestVerifierTests` , `UpdatePolicyTests` , `UpdateStateTests` . UI (strip, window, Settings) and UP07/UP08/UP12 test N → N+ 1 on an installed game ( DIST-11 ).

<a id="ob05ob08--plansza-widza-obs-03-obs-05"></a>
## OB05-OB08 — board viewer (OBS-03, OBS-05)

| ID | Screenplay and condition |
| --- | --- |
| OB05 | The viewer opens the ongoing game from “My games”. board shows the hand of Free Peoples under the “Observing” bar, click the map opens the region. The viewer does not send commands, previews, receipts or `seen`. |
| OB06 | Switching to Shadow keeps the camera and shows Shadow's hand on the same revision. Late response to the previous page does not replace the view (EditMode). |
| OB08 | The player's movement comes live, reconnect shows the current state. The presence renews every 15 s and stops after the board closes; the "Exit match" slows down the audience seat. |

Details: [OBSERVER_UX](OBSERVER_UX.md).

<a id="rp06rp09-rp13--zapis-biblioteka-i-odtwarzacz-replay-04-replay-05"></a>
## RP06–RP09, RP13 — recording, library and player (REPLAY-04, REPLAY-05)

| ID | Screenplay and condition |
| --- | --- |
| RP06 | Both players of the completed test party click “Save play”: the first gets “Saved”, the second “Already saved”, the library has one entry. The server error gives “Retry saving”, the party without recording “not available” (EditMode). |
| RP07 | Before logging in “Replays” shows the library without server: “Play”, back from board, “Export”, “Delete” and “Import” of the same file. |
| RP08 | Steps and turn both sides with grey ends. The change of page gives the same step, and Log has nothing later (EditMode, synthetic replay 60 steps). |
| RP09 | The playback comes to an end at Fast rate (0,6 + 0,25 s between steps); the manual step pauses. |
| RP13 | Quick Jumps: later read wins, in memory are the highest 3 frame portions. |
| RP10 | Play step with movement and change of both tracks: figure flies, tracks have two stages, sound sounds. Manual step, undo and change page: no flight, stage and sound; never personal Victory/Defeat (EditMode `ReplayEffectsTests`). |
| RP11 | The axle jump during the play interrupts the flight and the second stage of the track. The play is waiting for the presentation of the step at most 3 s. |
| RP12 | Replay of the entire batch downloaded from the real server (Wroter-Server `ReplayEndToEndTests`: 88 moves, 6 turn, win Shadow by corruption) goes to the library, export and second installation, and there without an account and server: each step forward with Free Peoples, back from Shadow, round-turn borders, "SHADOW WINS" at the end, Log without later action (EditMode `ReplayFullGameTests`). |
