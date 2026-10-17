<a id="t34--odbiór-dodatków-w-kliencie"></a>
# T34 — acceptance expansions in client

<a id="zakres-i-źródła"></a>
## Scope and sources

R02/R03 and R15/R17–R20; S17–S19. The sources of rules and all 93 legal configurations are pinned in [T33](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/rules/T33-COMPOSITION.md), and the implementation of controls and previous results is described by [UI-09](UI-09-IMPLEMENTATION.md). We do not change the interpretation of rules during acceptance. The server is still responsible for the legality, drawing, recording and separate projections of both sides.

<a id="plan-przed-zmianami"></a>
## Plan before changes

1. Link all eight protocol windows added to the database to the actual Unity scenarios:
   CouncilVersions, CouncilHomelands, CouncilToken, SpecialDieChoice, KeepActionDie, RulerDiscard,
   Dismay and CorruptionTile. Also check the use of common Action/EffectChoice/Reaction by character
   capabilities and BattleContinue/SiegeContinuation by expansions.
2. Complete the difficult compositions: multiple travel groups, full resources during battle,
   recruiting Branda/Dáin and sources of active Lords abilities. Each selection retains a clear
   cost, local preview and single approval.
3. Pick up the normal lobby and only after checking the completeness to make available the start of
   all the legal configurations. Two people accept the same version; change the configuration
   invalidates the consent. After start configuration is read only. Illegal dependencies remain
   rejected.
4. Remove the historical description of the prototype from the lobby. Provide a server-compatible
   description of the default Dismay, the dependencies and the unchanging rules of the batch. A
   separate room must refresh the controls also with the identical version number.

<a id="macierz-decyzji"></a>
## Decision matrix

| Window | Owner and visibility | UI-09 proof for re-use |
| --- | --- | --- |
| CouncilVersions | FP; hidden versions remain private, Guide revealed according to the rule | ExpansionDecisionNetworkTests; privacy and reconnect |
| CouncilHomelands | FP; selection visible only after approval | same class; both Fate Gimli's seats |
| CouncilToken | SH; public selection after approval | same class; token in both projections |
| SpecialDieChoice | the page indicated by the queue; public performance | same class; Keeper/Lesser and separate sources |
| KeepActionDie | FP; Vily's specific cost and just paid die | same class; return after reconnect |
| RulerDiscard | First SH, then FP; public recoil | same class; both dice Ruler |
| Dismay | SH; token and designated Ruler | same class; entry through the pool and one commit |
| CorruptionTile | SH; private selection also in Visible | ExpansionCorruptionNetworkTests; normal selection and Draw/Stop |

`ExpansionTokenNetworkTests` includes token + Ruler, mandatory additional selection of Denethor, Far-sighted, Council without dice and Council as the cost of the chief input. Existing tests cards Lords/Kings/Fate use common effect, reaction, travel and battle windows; their complete execution is recorded in the UI-09 regression. The matrix must distinguish these existing evidence from new attempts.

<a id="sprawdzenia-i-granice"></a>
## Checks and boundaries

- HTTP/PostgreSQL: start legal configurations, invariability after startup, acceptance versions and
  strict projections of both roles.
- Unity: new skill and normal start scenarios, preview/commit, reconnect, 1366×768/1920×1080 at
  100%/125%, without number fields and scroll.
- Difficulty: boundaries, no hitboxes, maintaining strategic alternatives and correct Fate
  composition without Dáin/Brand.
- Regression of changed modules, source/cover checkers, `tasks.py validate` and `git diff --check`.

The results are added after startup. T34 does not replace UI-13 benchmark, Windows/macOS or implementation released tests; user tests remain postponed. Status and history are only in task registry.

<a id="checkpoint-trudne-składy-i-wspólne-okna"></a>
## Checkpoint: Difficult Compositions and Shared Window

2026-10-06 , 10:53:37–10:54:39 UTC : `bash scripts/test-client.sh -testFilter 'ExpansionAbilityNetworkTests;ExpansionWindowNetworkTests;ExpansionTokenNetworkTests;FellowshipLayoutTests'` — **21/21 PASS**, without omission, `.artifacts/t34-windows.xml` and log. This one. new scenarios are Brand/D áin, activation/deactivation Balroga, Gothmog recruit/upgrade , 12 groups FP , 15 groups SH Tests use actual HTTP/WebSocket/PostgreSQL , preview, reconnet and commit. Evil Knowledge retains elite and alternatives Stop/Extend ; Nenya only passes her own Ring and returns to injury Hunt All systems pass 1366/1920 with 100/125% ; the resource pool also has control over the imposition of hitboxes, including voluntary omitting policy cards In battle .

First run: 5/8 , two groups overflowing and an inaccurate selector of two Dáin abilities. 18/19 , overflow of the initial composition FP with 125% Fixed group view: for large composition four bookmarks and buttons previous/ next groups, portraits of the selected group and not assigned figures , separate controls added/usuni . Nzgûle and all 12/15 groups remain accessible by clicking, without scroll and automatic selection of routes. Minimum portrait height 34 px; basic panel still 240 px. Pre-recorded initial lineup FP and resources siege with 1366/125%.

The lobby configuration and final decision matrix are still in progress; this checkpoint does not mean acceptance T34 or the gate startup expansions.

<a id="konfiguracja-i-kontrola-protokołu"></a>
## Configure and control the protocol

`ExpansionLobbyTests` performs normal HTTP creating a room, entry, configuration, both consents and start for **93 configuration**. Test checks the invalidation of the consent, rejection of the old version and incomplete acceptance, repetition of the start without the second batch, appropriate setup/role/Secrets and reject the change after startup. Filter LobbyTests/KingsVariantMatrixTests/ExpansionConfigurationTests: **96/96 PASS**, `.artifacts/test-results/t34-lobby.trx` Full `scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false` : **3441/3441 PASS**, no omissions, 3 min 25 s, `.artifacts/test-results/t34-full.trx` Checker `expansion_acceptance.py --results .artifacts/test-results/t34-full.trx` : 1107 methods / 2560 cases PASS .

gate starts now with any legal configuration of MVP. Client separates the option description, edit and summary. Switches show the data adopted by the server; default Dismay corresponds to Lords + Kings, turning off the module removes its dependent variants. Refreshing includes ID room and version. The Rules Panel on board shows the unchanging configuration from the player's projection, without a private setup and without editing controls.

`scripts/expansion_ui_acceptance.py` separates 22 from the base values of the protocol from eight new ones. It requires network tests of new windows, tokens and capabilities, variants of Corruption, full pools and configuration. Change of enumu or removal of the test interrupts control. NUnit reports are required to confirm execution; the compatibility of names does not include it. Dismay common window also includes a clear order selection with Lore; it does not combine both effects into one command.

Checkpoint client configuration: **8/8 PASS**, `.artifacts/t34-configuration-first.xml`, 11:05:56–11:07:20 UTC. Six attempts of the controller performs events in the actual panel PlayMode (unplugged controls EditMode did not send changes; two previous reports had 175/179). The other two attempts include 93 configurations of the lobby and panel Rules in four layouts and eight normally created and accepted parties, setup, throw and reconnect. Return between rooms with the same version, default Dismay, dependencies and values changed only after server response. Viewed the Rules panel at 1366/125%. was checked.

Order Test Lore/Dismay: First run**6/7**, the new scenario detected unnecessary second confirmation of Use Lore first. Added common classification of simple choices expansions : Use Lore first, Decline Dismay and Decline Vilya send one command one button each, like Pass/Decline reaction. Other action maintain the cost choice, preview and approval. An attempt has been made to preserve Vilya, which verifies the completion action and no handover of the Ring. Final score below.

Lore + regression Lords + configuration controls: **14/14 PASS**, zero omissions, `.artifacts/t34-lore-final.xml`, 11:12:34–11:12:52 UTC. Lore before Dismay, token behavior, Dismay refusal and Vilyi behavior pass through single buttons and a real recording. Room control also rejects the delayed, lower version of configuration of the same room.

<a id="końcowa-regresja-i-poprawki"></a>
## Final regression and amendments

Full EditMode : **173/173 PASS**, no omissions, `.artifacts/t34-editor-verified.xml` , 11:14:03–11:14:05 UTC Full `bash scripts/test-client.sh` : **254/257**, no omissions, `.artifacts/t34-client-full.xml` , 11:15:21–11:28:32 UTC The whole base party completed: 9 Turn, 121 decisions, RingCorrupted Two old tests were to simultaneously show all the group bookmarks; now they pass all the pages and check the availability of each group. A third failure detected the application of the map bar on pinned card After adding Rules. Inspect retains a constant name and width, and the mode shows with distinction and tooltip.

Renew `bash scripts/test-client.sh -testFilter 'ExpansionWindowNetworkTests;CharacterLayoutTests;Shadow02LayoutTests;T22InformationTests;ExpansionAbilityNetworkTests'`: **18/18 PASS**, zero omissions, `.artifacts/t34-regression-fix.xml`, 11:31:40–11:33:01 UTC. Includes all three previous defeats and a new Lore journey by die Lesser in Inspector Hunt. The Ruler and Balrog source choices have separate labels, and only legal dice remain active. Tests Lore/KingsLords after correction of the description of the Ruler: **15/15 PASS**, `scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~LordsLoreTests|FullyQualifiedName~KingsLordsIntegrationTests'`, `.artifacts/test-results/t34-lore-source.trx`.

Review of discharges 1366/125% confirmed improvements to panels and detected no symbols CardDraw/UseBalrog in the Resource directory. Added Draw/Balrog and control of the completeness and distinction of all walls. A separate regression after this change is recorded below. We do not identify the full course 254/257 with a single flawless course of the entire package.

Final `scripts/unity.sh -runTests -testPlatform EditMode` : **174/174 PASS**, no omissions, `.artifacts/t34-symbols-editor.xml` , 11:37:02–11:37:04 UTC . `scripts/test-client.sh -testFilter 'ExpansionWindowNetworkTests;ExpansionDecisionNetworkTests;ExpansionMatrixLayoutTests'` : **14/14 PASS**, no omissions, `.artifacts/t34-symbols-play.xml` , 11:37:24–11:38:53 UTC Lore was re-watched 1366/125%: All symbols, cost and approval are in the tray. Checker `expansion_ui_acceptance.py --results .artifacts/t34-client-full.xml .artifacts/t34-regression-fix.xml .artifacts/t34-symbols-play.xml` confirms **37 scenarios** 8 new and 22 inherited protocol windows.

The structure overview leaves separate configuration, resource presentation, draft, rules and transport modules. Manifesto symbols is a interchangeable resource and the test includes a set of enumu. Travel tests check the availability of each group instead of requiring simultaneous display. Further range: T35 (permanent batch and return list), later UI-12/UI-13 and release of both platforms. No manual user tests, new standalone, Windows or FPS benchmark have been performed.
