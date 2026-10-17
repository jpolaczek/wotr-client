<a id="t22--odbiór-interfejsu-gry-podstawowej"></a>
# T22 — acceptance Basic Game Interface

<a id="zakres-i-warunki"></a>
## Scope and conditions

acceptance includes R15–R21 and S01– S16/S20 –S21. The source of the decision is server `PlayerView` , and the cost, targets and settlement continues to check the domain. 34 rule windows; protocol groupes them in 24 values `DecisionKind` of which 22 They belong to the base. `CouncilVersions` and `CorruptionTile` expect implementation expansions The convergence of the model name with the future window does not mean the finished code.

T22 completes four product gaps: the real state of loading before the first projection, the constantly available political state of nations, the inspection of the exact composition of forces without changing the prepared action and the history of safe projection. Contract and Trials: [lot information](T22-INFORMATION.md). Missing transitions after clicks: [turn and capabilities](T22-TURNS.md), [Hunt, defence and termination](T22-BOUNDARIES.md).

Each network scenario has a separate record and two Unity clients. The item preparation is explicitly tested, apart from the production server and its endpoints. The next command passes through the actual HTTP/Kestrel/PostgreSQL, draws by `ServerRandom`, and the second client receives updates from WSS. The delay tests do not produce a substitute game state. Isolated layout tests remain a separate category of proof.

<a id="komplet-decyzji-bazy"></a>
## Database decision set

Class names indicate existing scenarios in `Assets/Wotr/Tests/PlayMode`; the matrix below defines coverage, and the results of the run are recorded separately. "Hard" means the checked branch of rules or interactions, not only a larger number of figures.

| DecisionKind | Presentation and ordinary case | Difficult case and scenarios |
| --- | --- | --- |
| FellowshipPhase | Declaration tray, changes to Guide or Continue; `FellowshipNetworkTests`. | Several declarations routes, treatment, entry to Mordor; same class. |
| AllocateHunt | Specific dice transferred to Hunt, allowed zero; `T22TurnNetworkTests`. | Mandatory minimum after the previous turn movement, empty selection rejected without throw, openly selected die. |
| Action | Map, intent and selected cost: traffic, recruitment, selection; `MovementNetworkTests`, `CardNetworkTests`. | Two armies, cost after Ring, policy and pass; `T22TurnNetworkTests`. Active Gimli/Voice with reserve shortage: `T22AbilityNetworkTests`; entrances and standalone movement: `CharacterNetworkTests`. |
| DiscardCards | Select your own spare card; `CardNetworkTests`. | Selection to seven, secret recoil, the other side sees only number; same class and large-handed tests `CardInteractionTests`. |
| Reaction | Card/zdolno or single Decline; `CardPackNetworkTests`. | Protection Mithril/Axe against later draw Shelob, reconnect and proper continuation; FP Character 01–03 and Shadow Character 01–03 packages. |
| CombatCard | Own card or missing card; `BattleNetworkTests`. | Adversary reverse before disclosure, restore after reconnect and private skip shorthand; `BattlePolicyNetworkTests`, Combat parcel testing. |
| Losses | figure and the open Downgrade/Remove; `BattleNetworkTests`. | No regular, last unit and fate of the character; same class. Wydarzenia/ochrona: `CardPack04NetworkTests`, `ShadowStrategy01NetworkTests`. |
| HuntDamage | Corruption or Guide; `FellowshipNetworkTests`. | Random victim without revealing RNG in preview, fix after reconnect and new Guide: `T22BoundaryNetworkTests`; hobbit and Gollum: `CharacterNetworkTests`. |
| RemoveExcess | Select the real figures to return after action; `StrongholdNetworkTests`. | Suit six reduced only after the attacker enters, save eleven, excess after blowing up Corsairs: `StrongholdNetworkTests`, `ShadowStrategy02NetworkTests`. |
| EffectChoice | Target tray, figures or open-ended alternative; 16 T21 packages tests. | Multi-stage mandatory effects, forwarding decision to the opponent, reserve shortage, Ents chain, Denethor/Jerkír; `CardPack*`, `Strategy*`, `Shadow*`, `ShadowStrategy*` classes. |
| FellowshipRoute | Destination on the map and selection of non-equivalent route; `FellowshipNetworkTests`. | Several Shadow fortresses, additional tiles and interruptions after disclosure; the same class and `Shadow02NetworkTests`. |
| Guide | Portraits of Authorised Guides; `FellowshipNetworkTests`. | New Guide after the random death of one of three equal Companion; `T22BoundaryNetworkTests`. |
| CompanionSeparation | Public group and target region; `FellowshipNetworkTests`. | Forced joint return of two Hobbits before the appearance of Gollum; `CharacterNetworkTests`. |
| HuntReroll | Selection of dice to be renewed or omitted directly; `FellowshipNetworkTests`. | Empty pool, refilla and maintaining the same decision after reconnect; same class and durability tests Hunt. |
| BattleReroll | Actual available reroll and Round Result; `BattleNetworkTests`. | Separate regular/special throws and effects of Leadership cancellation; `BattleLayoutTests` and actual T21 parcel combat effects. |
| BattleContinue | Continue or Stop; `BattleNetworkTests`. | Continuation, choice of the second player's retreat and camera behavior; same class. |
| BattleRetreat | Hold Or legal region on the map; `BattleNetworkTests`. | No legal retreat, no retreat after the card and no different fates of the commanders; same class and Shadow Strategy packages. |
| BattleAdvance | Reveal progress or remain; `BattleNetworkTests`. | Progress after shelter and separate reduction of garrison; `StrongholdNetworkTests`. |
| StrongholdDefense | Shelter or Field battle in one tray; `StrongholdNetworkTests`, `T22BoundaryNetworkTests`. | Seven defenders choose battle field at Stronghold, without reducing to the garrison limit and without a second fee. |
| SiegeContinuation | Elite to either extend or Stop; `StrongholdNetworkTests`. | Two nations, no regular exchange, reconnect retaining a specific elite; the same class and durability tests siege. |
| ArmyEvent | Groups, figure and routes on the map; `Strategy02NetworkTests`, `Shadow04NetworkTests`. | Four Moving groups, mandatory two armies of Lengthens, sea landing/excess, a rescue passing through battle; `ShadowStrategy02NetworkTests`. |
| CharacterEvent | Portraits and independent goals; `Shadow02NetworkTests`. | Search: flight, then the choice of the FP; Weather: move of the figure itself. Big groups and reconnection: `CharacterNetworkTests`. |

<a id="kontrole-przekrojowe"></a>
## Cross-cutting checks

- S01–S06: existing trials army/cost/recruitment , `MovementNetworkTests` and over-scenes in
  `StrongholdNetworkTests` ; Size and precision figures In `BoardInteractionTests` T22 adds
  `T22BoardNetworkTests` : division of the armies of three nations with commanders/Strider and the
  open cost of Will of the West as Character; two distant movements with the first amendment;
  transitional 12 units with re-traffication blockade; final 12 with two choice figures to reserves;
  two recruits of different nations in separate settlement with one other figure Gondor. Each case
  has a separate server recording, preview before the commit and comparison of both projections.
  These paths complement previous isolated attempts, do not change the rules B04/B08/B09 ( BASE p.
  20 – 21/26 – 28 ).
- S07–S08: `CardNetworkTests` and all T21 packages, especially the forwarding of the decision during
  Dead Men, protection, Search and half-life recruitment effects.
- S09–S13: `BattleNetworkTests`, `StrongholdNetworkTests`, `BattlePolicyNetworkTests` and all Combat
  programs verified in the 96 cards manifest.
- S14–S16: `FellowshipNetworkTests`, `CharacterNetworkTests` and random T22 victim. Both Mordor
  finals already use the server; T22 adds military FP/Shadow and priority Shadow at both thresholds,
  only after the dice has been exhausted.
- S20–S21: `UI10NetworkTests`, loss of post-write response, late receipt, two devices, delayed view
  and behavior attached to card. `T22InformationTests` extends the view of maps, policies and
  history in oczekiwaniu/reconnect.

The change of the selected item does not play the entire screen. The information uses existing projection, directory indexes and updated controls; expensive generation of the combination remains outside the client. Optionally scrolling history is not a decision panel. Inspection mode has a separate event from traffic planning and never sends command.

<a id="wyniki-i-dalszy-odbiór"></a>
## Results and follow-up of acceptance

Full. NET : 1668/1668 PASS , without omission; native EditMode : 118/118 PASS First focused `bash scripts/test-client.sh -testFilter Wotr.PlayTests.T22` gave **13/20 PASS , 7 FAIL , 0 skip** ( 2026-10-05 22:02:02–22:03:24 UTC ), `.artifacts/t22-focus-first.xml/log` All active abilities and turn , four of the five new army trips, a random victim and the first charging. The information system required an open-air `flex-direction: row` ; checking for no automatic cost revealed an existing highlight error for nameless Ring buttons at empty `DieId` The lighting is now connected to the type and ID component. The victory tests were mistakenly expected `Spent` instead of the right `Discarded` , and a new sequence cards battle click before the control is ready. Repairs require rerun; T22 remains open. Earlier evidence remains in [TESTING](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/TESTING.md) and [UI-10](UI-10-NETWORK.md) .

Full batch from a regular setup, ready-to-write regression and acceptance all-base belong to T23. User tests are postponed; builds of both platforms, work between Windows/macOS, measurements FPS and release have their own tasks. T22 does not mean to pass future expansions, release or manual ergonomics testing.

Second focus: **18/20 PASS , 2 FAIL , 0 skip**, `.artifacts/t22-focus-second.xml/log` , 22:05:52–22:07:23 UTC All new scenarios army/tur/zdolno The three military finals were scored, after the strip of nations was removed from the controls and the mutual closure information/report battle : **3/3 information tests PASS** in the third focused report (whole report 3/4 The last defeat was battle The matrix also contains visible `Last battle` , eight nations, 10 form, six cards and 50 History entries. `t22-information-1366-1.25.png` and `t22-history-1366-1.25.png` ; the basic tray remains without scroll.

The diagnosis of the clicks in battle corrected the previous supposition of the ready button: the button had the right bounces and was active, but `panel.Pick` returned `null` and callback clicks did not follow. Two RenderTexture panels in one process retained the previous context of the indicator. The public input of the indicator to the panel before PointerDown/Up gave **1/1 PASS**, `.artifacts/t22-field-fifth.xml/log`, 22:11:52–22:11:55 UTC. The common helper clicks now performs such a move; it does not callback directly and does not change the server. A complete regression after this correction is required before the task is completed.

Final `bash scripts/test-client.sh` : **172/172 PASS , 0 Fail, 0 skip**, 2026-10-05 22:13:25–22:20:03 UTC , 397,84 p. Artefacts: `.artifacts/t22-play-full.xml` , `.artifacts/t22-play-full.log` , `.artifacts/t22-play-full-server.log` This is one full counted run after all the amendments. Includes 20 new tests: 3 capacity ( 5 records), 5 Army, 5 boundaries/winnings , 3 information and 4 Turn. Other 152 tests check pre-implemented components, all packages cards and transport. The actual backend uses local PostgreSQL 17 and Kestrela; Unity 6000.6 .0f1 works on macOS. 1366 × 768/1920 × 1080 with 100/125% are automatically checked and the tightest information/history were also watched.

Structure overview: information is separated from three small modules UI; the map reports a separate inspection event, and the command adapter additionally blocks the drop in this mode. Static catalogues remain indexed. The nation bar retains controls, history plays maximum 50 entries only when changing content, army details count the current region at otwarciu/projekcji. No conversion of every frame, rules to Unity, new payoad with secrets or debug track. Commity implementations: `2e431a2`, `09b4f16`, `4fdaabd`. Next task: T23; manual user tests remain postponed.

Native Final EditMode : **118/118 PASS , 0 skip**, `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/t22-editor-final.xml -logFile .artifacts/t22-editor-final.log` , 22:20:20–22:20:22 UTC . `tasks.py validate` , `check_catalog.py` , `card_coverage.py` and `git diff --check` also PASS : 88 tasks, 30 rule families/ 34 Inventory-compatible windows UI and 96 complete kart/16 packages. T22 meets the criteria of its acceptance ; does not leave unexecuted S01– S16/S20 -S21 in the field of base.
