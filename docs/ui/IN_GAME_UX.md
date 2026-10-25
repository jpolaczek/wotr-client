<a id="pozostały-interfejs-podczas-partii--projekt-ui-56ui-63"></a>
# Other interface during the batch — UI-56-UI-63 project

The 2026-10-07/08 call project, saved in the UI-55 documentation. The status of the execution is determined only by registry. This document describes the target behavior, does not confirm implementation or the performance of future tests.

From UI-173 (user decision with 2026-10-15) the game does not have a Help panel or links "?" and "Why unavailable?"; Help descriptions below are historical ([UX_SPEC](UX_SPEC.md#bez-help--ui-173)).

<a id="zakres-wybory-i-odpowiedzialność"></a>
## Scope, elections and accountability

The user selected: only the interface during the party, small shortcut Effects with map markings, help on request and a small sovereign portrait bar. The package includes Effects, Special Resources, Sovereigns, Victory and Help. Lobby, a list of the batch and a separate summary screen remain out of range. The result of the game is presented on the previous board.

| Task | Liability |
| --- | --- |
| UI-56 | Secure descriptions of effects, availability and points, common to server rules. |
| UI-57 | Effects, sources and targets, map markings, deletion and expiration. |
| UI-58 | dice/Pings/tokens, the appropriate costs and the current selection panel at the pool. |
| UI-59 | A belt and a preview of sovereigns, awakening, attempted corruption and purification. |
| UI-60 | Victory, origin of points, moment of check and score on board. |
| UI-61 | Help, contextual explanations, Why unavailable? and orientation in the decision. |
| UI-62 | Joint decision hosts, log, two types of notifications and recovery. |
| UI-63 | Collective acceptance G01–G18, both sides, expansions, systems and performance. |

UI-22/UI-35/UI-47-CONTEXT is an existing foundation. UI-38–UI-43 provides log and alerts; UI-49–UI-54 new Hunt. The package does not create a second transport, server, log or notification system.

A separate Characters project, preparation and Menu was written by UI-64 In [CHARACTERS_SETUP_MENU_UX](CHARACTERS_SETUP_MENU_UX.md) and UI-65 – UI-72 The own list includes only the name characters, entry conditions and direct travel choice; Nazgûle remains on the map. UI-65 uses availability UI-56 , UI-66 from common preview UI-59 , UI-70 with Help UI-61 We don't copy the conditions to Unity or become addicted UI-56 – UI-63 back to the new package. The tasks of this document still do not mean implementation Characters; aggregate acceptance extension belongs to UI-72 .

Behaviour sources: [UX_SPEC](UX_SPEC.md), [REQUIREMENTS](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/REQUIREMENTS.md), [LOGIC](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/LOGIC.md) B02–B06/B20, [EXPANSIONS](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/EXPANSIONS.md), [T24](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T24-LORDS-DICE-RINGS.md), [T28](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T28-SOVEREIGNS.md), [T29](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T29-RULER-CHIEFTAINS.md), [T31](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T31-INTEGRATION.md), [T17](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T17-MORDOR.md) and [T33](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T33-COMPOSITION.md). The content of cards and the capabilities come from existing verified folders.

<a id="wspólny-model-interakcji"></a>
## Common model of interaction

The map dominates. From UI-80 passive tooltip appears after 200 ms howera or immediately after seal. It reads only existing allowed metadata, without GET/POST, buttons and gesture capture. Departing, losing focus or starting gestures extinguishs the description. Hover terrain only backlights the region.

The panel with options, card and the drawer open with a click or Enter/Space, never hoverem/fokusem. The cursor's departure does not close them. The header allows you to move, Close/Esc closes according to the common focus range; Esc extinguishs the tooltip without receiving the closing of the active window. The position is remembered in the session. The inspection does not change the camera, draft or cost; Location is public. Details: [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md).

Public information of both pages remains clear, but controls depend on the role and options of the server. We do not show other people's buttons as disabled. The enemy object can still be a legitimate target of its own action. The temporary inaccessible actions receive a safe explanation.

Complete Final click/drop Sends a step without additional Confirm. Collections have one ✓ Cost and identity are not automatically replaced. pass, discard, end of paid sequence and phase transitions retain existing rules. One choice has one host and a common sender/receipt.

## Effects

The small shortcut `Effects · n` at the edge of the map extends the list of public active sources of both sides, also at zero. `n` counts sources, not targets of the same effect. The list includes effects and card on a table without a separate active modifier; card does not occur double.

The entry shows the icon, source, owner, short description, targets and exact condition of duration. The compound source retains separate effects and their different dates, instead of assigning the time of the first modifier to everyone else. The source can be card or ability. Hover/fokus shows a passive description of the known content. Click or Enter/Space opens the details with the controls; the Location is concerned with the consciously selected target.

Local effects are marked on objects; several sources create one tag with a number. The general effect is described with the appropriate nation/parameter, without marking each region. The empty region with the effect still requires a click for inspection. The click of the legal target of the current action retains priority over opening the information. The markings do not accidentally take over the drop.

The removal is at source and shows the entire cost: die, Ring, Rejects required or targets according to option. The own legal action can remove the enemy effect. Expiration updates the list and map; pinned card can remain clear with the information that the effect has ended without the previous controls. The reason goes to the existing log by UI-62.

<a id="zasoby-specjalne"></a>
## Special resources

The rings and chips are at the player's pools. Special die shows the source, score and rune; preview explains the owner, condition, application and proper moment of recovery/removal. The expected effect dependent on the future condition at Recover is described conditionally, not as a guarantee.

| Stock | Gest and approval limit |
| --- | --- |
| The Normal Ring | Ring → specific die → new wall; last click saves change, without automatic use of dice. |
| Ruler token | Token → specific Ruler → assigned ability and targets; token and the appropriate action are associated at a cost. |
| Council | Token → permitted use → targets; the entrance of the chief may also require dice. |
| Dismay | Token → authorized Ruler; the result of the reroll settles the server. |
| Keeper/Lesser | The selected dice is selected by the player on the awarded candidates, also with several Oki. |
| Discard Ruler | The dice Authorised Click is a complete choice; the consequence is visible before clicking. |

We do not equal the usual Ruler result, ability and action with token. We preserve limitations of Rings/Ruler, Eye Sources, Unauthorized dice and Order of Lords → Reject SH → Reject FP → Dismay/Lore → action with T31. Voluntary order of Lore/Dismay and refusal remain the player's choices.

Narya is at the right position, Nenya in Hunt's reaction, Vilya after the completion of the entire correct action. We do not add a competitive decision to them at the pool. UI-62 combines these places with the current Hunt and context of action.

The new own required decision opens a compact panel at the pool once according to ID, without camera. Shows the current selection and actor, without empty Next stages. Close does not delete the legal draft; the gold tab allows you to return. Automatic score only updates the open view and history.

<a id="suwereni"></a>
## Sovereigns

The portrait bar is only for Kings configuration. The portrait has a name, Initial/Awakened/Corrupted/Removed status and a symbol next to the color. Fate configuration disables absent sovereigns. The preview shows region, resistance, attached tiles, explicit capabilities, usage of the sample in this turn and legal activities and safe conditions of inaccessibility. Awakened uses the same data of the characters as the map; it does not create a second figure.

Awakening: specific die → Awaken at the portrait → route or stay. Preview before the end indicates the return of tiles, token and the moment of sharing the common dice Ruler; it includes the variant Renovating Ruler Tokens. It does not teleport through siege and does not give an additional Ruler for each consecutive sovereign.

Try to corrupt: a specific authorized Eye with Hunt → portrait. View at the sovereign shows cost, resistance, limit of selection and current step. The usual variant offers Shadow a legitimate selection of tiles. Sequential shows Draw/Stop and only the last tile as a possible result. Skip/change and Kings tile retain the server procedure. We do not run the normal response of Hunt.

FP sees only the public run and allowed awersy/rewersy. Hidden sum is not zero or estimated value. Visible reveals assigned tiles, not private selection. UI, descriptions, ID reverses, log and errors apply the same policy. Sources of the Lords/Captain eye are distinguished according to T31.

Removing the tile after politics is an optional random return, without choosing value. Removing the Corrupted is another action, with a valid Companion and a cost. Corrupted does not move or die like an army. The new own decision opens the right view once without a camera; the automatic result does not create a new popup. The public condition remains available to both parties.

## Victory

The fixed compact `FP 2/4 · SH 6/10` indicator opens with a click or Enter/Space with a list of the acquired cities/fortresses values and regions from Location. Hover/fokus only provides a passive description of the indicator. The source of the points is the server, with the Fate script and Kings effects. The threshold achievement shows `Threshold reached · Checked at turn end`; it does not announce an earlier victory.

Preview also explains the destruction of the Ring and Corruption and leads to an existing panel Fellowship The decision comes from Winner/Victory: Corruption ≥ 12 Before destruction, military only in phase 6 , SH before FP at simultaneous thresholds ( BASE p. 44 ).

The game ends with a small message on the same board. The map, your own information and log remain legible; we do not reveal the hand or other secrets. Mutating controls are disabled by the end state; there is no new summary screen. The retry ending command still recovers its receipt.

<a id="help-i-orientacja"></a>
## Help and orientation

Tooltip explains the object; `Why unavailable?` gives a specific safe reason; `?` opens the right subject. Global Help develops symbol legend, gestures, turn runtime and read-only batch configuration. Content in English, from verified sources and only for the enabled expansions. No permanent instructions for each gesture or strategic recommendations.

The bar shows turn, phase, actual decision owner and state of connection. With action, there is no cost, budget and election. Help distinguishes no cost, target/condition, wrong time and limit used. No option in someone else's turn does not mean that all the conditions of the given ability are not met. Descriptions do not explain the private decision of the opponent. Help does not delete draft; optional reading can be scrolled, decision forms do not require a scroll.

<a id="dane-integracja-i-odzyskiwanie"></a>
## Data, integration and recovery

UI-56 extends the safe projection of sources/outputs, break points and accessibility explanations. Terms and scoring come from common predicates/validation planners, not from copies of rules in Unity. New fields have neutral values for existing entries/contracts; no data means no description, no fictitious result or permission. Reading does not change revisions or RNG. The contract is planned in UI-56 and CONTRACTS/ARCHITECTURE is updated with its actual implementation.

UI-62 uses the common card action: cost, contractor, target and public score. The red tab opens a complete historical summary with the correct UI; gold leads to the current decision of its own. The own automatic result without a decision remains in the log. We do not add a third type of alert. The story does not replace the current draft nor does it repeat the old action.

The error is with action, not the new notification tab. When you send, we block the recommit by maintaining the inspection. Reconconnect first recovers the receipt and the current condition; retains legal choices, does not replace the resource or performs draft automatically. The change of decision/game invalidates the old gestures.

<a id="macierz-odbioru-g01g18"></a>
## Matrix acceptance G01–G18

All rows are the requirements of future execution, not the PASS entries. Each owner provides actual testing; UI-63 receives the whole matrix.

| ID | Screenplay and required result | Owner |
| --- | --- | --- |
| G01 | Sources, targets, accessibility and points projection: both views, clean reading without RNG, compliance with validation and older recording. | UI-56 |
| G02 | Effects: card on table without modifier, ability, multiple targets/terms of one source; counter without duplicates. | UI-57 |
| G03 | local/ogal, grouping, Location, click empty region and priority legal target. | UI-57 |
| G04 | Removal at full cost, termination in open preview and legal own action against foreign effect. | UI-57 |
| G05 | Origin and state of the resource, rune and conditional Recovery; own costs without anyone else's checks. | UI-58 |
| G06 | Ring, Ruler and Council: identity, one-off/limits, no substitution and proper cost-binding. | UI-58 |
| G07 | Selection of special dice, both Ruler discards and both orders Dismay/Lore; no automatic strategic choice. | UI-58 |
| G08 | States and sovereign bar, three/pi The truncations of portraits according to Fate, public inspection and a joint representation of Awakened. | UI-59 |
| G09 | Awakening with route/remaining and siege; return of tiles, token/Ruler; random cleaning and removal of Corrupted. | UI-59 |
| G10 | A simple attempt to corrupt: specific Eye, limits, choice of tile, Kings/pominiêau, both views and no Hunt response. | UI-59 |
| G11 | Sequential and Visible in combinations: only the final score, private selection despite Visible, the source of the Eye and the exceptions of the Lords. | UI-59 |
| G12 | Military Victory: Point break, Fate/Kings, threshold before phase 6, loss of points and both thresholds simultaneously. | UI-60 |
| G13 | Both Ring Ends, priority Corruption, score on board, further inspection without revealing secrets. | UI-60 |
| G14 | Help/?, legend, sources and configuration; own draft/fokus remain, no constant advice and other details. | UI-61 |
| G15 | Why unavailable?: cost, phase, condition and use; compliance with validation and update after revision. | UI-56, UI-61 |
| G16 | Log and red/ed The context of the pitch: full result at UI , return to the right decision, no third type and double host. | UI-62 |
| G17 | Restart/timeout/two device during cost, selection and end of game: one cost/RNG, the same receiver, without automatic execution of draft. | UI-62 |
| G18 | Two × two slabs, full expansions, stability/focus, click/drop, feedback ≤100 ms, 60 FPS target, build and dragons. | UI-63 |

<a id="fundament-danych-ui-56"></a>
## The UI-56 data base

Implementation descriptions and results below UI-56 – UI-72 document the condition of 2026-10-08 UTC before UI-80 Historical gestures hover/pin are part of these records; the current standard is the ‘Common Model of Interaction’ above. Past results do not constitute acceptance new behavior of tooltips and windows.

G01 and G15 data are shared with the projection of effects descriptions, their own availability and points. The conditions are still readable in another's window; this condition does not give action execution controls. costs/targets/Limits and the current moment are separated, and the client uses existing ActionOption to gestures. Awakening, cleaning through politics, removing Corrupted and attempting to corrupt remain four different applications. Full contracts and sources: [CONTRACTS](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/CONTRACTS.md), UI-56. UI-57–UI-61 are responsible for the target panels using these data; the delivery of projections is not acceptance their interaction or the entire G01–G18.


<a id="ui-57--effects-przy-źródłach"></a>
## UI-57 — Effects at sources

The Effects · n tab is at the left edge under Politics and also works at zero. The counter groupes the sources of both sides, including public capabilities and card on a table without a separate modifier. A larger list has pages of four sources without scrolling. Hover source shows the correct full card or public ability, click pin; Location is an open navigation to the selected region. Effects/time limits they are not glued together. On the low screen, the range group are side by side. Own legal removal of the alien effect is at the source and shows the exact cost. Two discards as protection cost have one ✓; the selected Ring ends the Jerkíra cost without additional Confirm. The incomplete cost does not spend resources. The viewer's lack of permission hides the removal. The expire removes the button and the tags, but pinned text can be left with Effect ended. Close does not delete the starting cost. Local tags group sources and do not take over clicks/drops; legitimate target action The region with the effect itself is still empty for the hover and requires a click. The overall/national impact does not multiply icons in each region.

<a id="ui-58--zasoby-i-decyzje-przy-puli"></a>
## UI-58 — resources and decisions at the pool

Dice & tokens is host of the same tray for its own Keeper/Lesser, Ruler, Council, Dismay/Lore, Vilya and a reworked Ring. New ID opens once, Close retains draft; the gold tab allows you to return. The preserved/odrzucanej dice selection is done on the actual pool object, without its copy in the form and without Confirm. The consequence is visible before clicking. The own legitimate targets Dismay also include die opponent.

The council first selects a specific token and distinguishes its applications. The player chooses a printed use or the leader's input; the input requires the indicated dice , without automatic cost conversion. Neutral selection of the token will survive the reconnect. Ruler token dragged to a specific Ruler will charge but does not choose the ability for the player. The ring ends changing the wall and leaves die not published. Inspection of resources of both parties shows public sources, condition and conditional Recovery; explanations of his own inaccessibility reads from UI-56 . Narya/Nenya keep their hosts, and integration with new Hunt and logo remains acceptance UI-62 .

<a id="ui-60--wynik-na-planszy-i-dostępność-podglądów"></a>
## UI-60 — score on board and preview availability

The fixed FP n/4 · SH n/10 opens the hoverem/fokusem/klikiem panel of both sides. The button, source, preview and result remain available while waiting, reconnect and end of the game. Columns show city/fortress, region, scoring and control page; at over three regions have pages (low view of two lines), without scroll. The threshold achieves the right page and shows Threshold readed · Checked at turn end, not calculated by the client. The final score is a small banner over a shortcut; both sides retain an inspection without new secrets. The conditions of the Ring lead to an existing Fellowship without changing the camera and without copying the track. The location is an open navigation and leaves no unreached destination, even after the previously moving the clip. The fixed shortcut retains its own area, and the banner omitting inspector. The header uses the window memory; the location is not re-opening, or re-resing the selection of the panel.

The Fellowship system's regression revealed the cover of the region's portraits by pinned window. RegionHover raises the new/re to the top of the open, without changing the state of the game or the order of the windows at the same preview. The FellowshipGroupsHuntAndMordorFitBothSizesAndScalesWithoutScrolling test uses a real hit-test and checks the selection of all seven characters in four layouts. Results and previous failures remain in UI-60.

<a id="ui-59--wdrożenie-g08g11"></a>
## UI-59 — implementation G08–G11

Sovereigns use one public inspector and one decision tray for its real purpose. The bar retains portraits and state symbols; three/pi figures comes from the configuration projection. Unknown sum is ⁇ , allowed disclosures do not replace private choice, and the conditions of their own availability come from UI-56.

Awakening, cost, route/Stay, return the tiles, token and conditional Recovery use existing legal options. The attempt has a separate context from Hunt; Sequential, omissions and Kings retain the server procedure. CorruptionRemoval has a separate remove/decline by policy, without the choice of the tile and is also recognized as a break from battle. Awakened uses the same identity of the character; implementation does not create an additional unit.

Close, return with a gold tab and reconnect retain draft and a specific cost. Automatic state updates only open preview. Test results, four layouts and previous failures save UI-59; history integration and notifications remain within the scope of UI-62.

<a id="ui-61--pomoc-kontekstowa-i-wyjaśnienia-dostępności"></a>
## UI-61 — Contextual assistance and accessibility clarifications

Global Help replaces Rules; the preserved internal BoardInformation. Rules still serves the old configuration preview. Small ? with dice, card, politics, Fellowship, Hunt, fighting, effects, resources, sovereigns, characters and Victory opens the right subject. Why unavailable? presents costs, conditions, targets, limits and timing of the server; no explanation remains an obvious lack of data. The themes of expansions/variants are filtered according to configuration; Fate explicitly omits Dáin/Brand as sovereigns and leaves the remaining three; the modified card form of Fate concerns two Gimli's obverses.

The help is optional to read scrolling; decisions still use their unscrewed hosts. Esc closes Help only and restores the existing visible source focus (fallback: global Help). Enter in Help does not go to action approval. The click outside the unsealed window closes the help itself; pin keeps it open, and the moves are remembered by FloatingWindow. Reconct retains draft and the subject and indicates the last item received. The changes to the decision refresh actor/timing, without the action and without the camera motion.

<a id="integracja-kontekstu--ui-62"></a>
## Context Integration — UI-62

card historic selects the host Effects, resources, sovereign or Victory based on fixed steps and their automatic consequences from the same revision. It does not read current legal options to play the former cost. The lack of saved winner does not become the end of the game on the point threshold.

The gold tab opens the real parent of one tray, also after Close. Help and older entry do not move the current form. Manual entry to battle indicates its region; the incoming alert itself does not move the camera. The names of the Council and sovereign sources use public catalogues, without adding a historically unknown version or Corruption.

Seven new G16/G17 scenarios have passed the actual HTTP/WS/PostgreSQL, including reboot before cost and receive after cost, private kaflu and end of the game. The final set has 39/39 required Play PASS and 26/26 required Editor in full 516/516. Re-use includes the battle navigation and readability of sources; the 17 currently used PNG. The exact results, historical failures and the border of the separate TLS describes UI-62. This does not replace the aggregate acceptance UI-63.

<a id="rozszerzenie-g16g17-na-characterssetup--ui-71"></a>
### G16/G17 extension to Characters/setup — UI-71 extension

The common card history renders the saved characters and Guide pages without reading the current roster. Preparation of version/homelands and public disclosure in Fellowship has a Fellowship anchor, with the preserved priority of the context of Hunt/bitwy. Read version and actors remain in the current model; automatic own consequences do not become an enemy alarm. The exact token Council and physical die are preserved by Games/restart, the receipt and old gestures are checked on the real server. Evidence of UI-71.

acceptance UI-71 : 33/33 the required Play methods, 13/13 Full Editor cases 542/542 , 7/7 PNG and 399/399 unchanged domain cases in full 3726/3726 PASS . Separate fresh HTTPS/WSS dragons are credited with trusted CA ; Unity tests benefit from HTTP/WS/PostgreSQL. Accurate filters, reusing identical methods and limits: UI-71 . .

<a id="ui-63--zbiorczy-odbiór-g01g18"></a>
## UI-63 — Collective acceptance G01–G18

acceptance combines existing Effects, resources, sovereigns, Victory and Help with one tray decision and a common history. G01–G18 Matrix indicates specific methods and actual reports in `.artifacts/ui63-test-acceptance.json`; collector checks the exact set and number of cases of each batch. Exporting the layouts itself does not replace the execution of the rules.

Evidence of 93 configuration in full 3726/3726 domains/server includes:

| Proof | Actual Test |
| --- | --- |
| Composition figures, dice, tiles and the first turn | 93 cases `ExpansionCompositionTests.EveryLegalConfigurationHasExactSourceSlotsFiguresDiceTilesAndFirstTurn` |
| Create and invariable configuration by HTTP | `ExpansionLobbyTests.All93AcceptedConfigurationsStartTheirActualSetupAndRemainImmutableThroughHttp` — 93 loop configuration |
| First decision of preparation, private views and retras | `ExpansionPersistenceTests.All93ConfigurationsRestartTheirFirstSetupDecisionWithPrivateViewsAndIdempotentRetry` — 93 loop configuration |
| First paid action after preparation and reboot | `ExpansionPersistenceTests.All93ConfigurationsRestartTheirFirstPaidActionWithoutResettingSetupOrShufflingAgain` — 93 loop configuration |
| Both roles and four layouts | `ExpansionMatrixLayoutTests.BothPerspectivesOfAllConfigurationsFitWithoutScrolling` — 186 projection × four circuits |
| Just a start from the lobby | `ExpansionLobbyNetworkTests.AllEightModuleCombinationsStartThroughAcceptedLobbyControls` — eight combinations expansions |

Separate scenarios G01–G17 perform unique cost branches, Corruption, siege, endings and recoverys. The actual Unity uses HTTP/WS/PostgreSQL; the separate HTTPS/WSS dragons are not a sample of TLS Client Unity. The final full editor of frozen import has 551/551 PASS (2026-10-08, 17:11:27–17:11:42 UTC). Full domain report is `tests/Wotr.Tests/TestResults/ui63-domain.trx`, Editor: `.artifacts/ui63-final-edit.xml`.

G18 requires simultaneous Effects and Help, full pools and available five portraits in both roles. Default windows bypass the sovereign bar; the hand-picked position remains superior. Stronger asers control the middle and four corners of each portrait, also after changing the Effects page. 8/8 prior to the extension of this sample is not the final acceptance. Final re-opening, viewing PNG,  56 measurements of the old profile, 32 measurements of new panels, build and smoke remain separate evidence; this entry does not count them. Status and full results: UI-63.

The original domain manifesto included 847 files, except for three scenarios compiled for the test design by `Compile Include`. The `.artifacts/ui63-domain-supplementary-inputs.json` complement links their exact bytes to the `3d3ec34` commit and three export tests performed; it does not alter the scope of the historical manifesto. Final build requires its own source manifesto and identical profiler input.

The final comparison also detected one outdated line in the isolated copy of `ActionPoolView`: the Council's backlight only included the printed token. The previously written `CanSelectCouncil`, which also includes the legal cost of the Chieftain input. Sender already used this condition; the change concerns the apparent availability of the token. The editor's regression checks the actual pool and the behavior of the same control after the option has been removed. Earlier reports are not proof of this amendment; fresh sample of resources, tokens and the Council's recovery have separate results. The trace of both hash, actual import and re-renews stores `.artifacts/ui63-stale-import-review.json`.

<a id="końcowy-odbiór-ui-63"></a>
### Final acceptance UI-63

Final Editor after the stabilisation amendment has 551/551 PASS ( 17:57:00–17:57:15 UTC ), required filter 36 methods and final seven batch 44/44 PASS The numbers are not summing up. The G01–G18 matrix binds 165 unique methods with 24 reports; 93 configurations have actual domain evidence, HTTP and reboots, except for export UI Default Effects/Help/Victory maintain the availability of portraits and abbreviations and the manual positions remain superior. Fresh builds, dragons and profiles 56 + 32 PASS ; 52 final PNG have confirmed an inspection in UI-63 .

<a id="powiązanie-z-characters-i-menu--ui-72"></a>
## Binding to Characters and Menu — UI-72

New preparation views and Characters use the same windows, hints and resources as the previous batch interface. The menu does not stop the game or approve local selection. 56 network views check privacy, stable draft and available targets in four systems. Historical G01–G18 results keep dates and SHA ; callback correction of Characters has public new attempts, unaltered historical report. acceptance CM01 – CM18 and additional measurements: UI-72 .

## Chat on the board — CHAT-01 (2026-10-17)

A small chat icon sits at the bottom left of the board, beside the legend (`GameChatPanel`). A click unrolls a compact
260 × 176 px panel upward from the icon, over the map corner only, with the room's messages, a text field and Send;
Esc or another click on the icon rolls it up. A message that arrives while the panel is rolled up marks the icon
with a "?" badge until the panel opens; the first read after connecting marks nothing. Players read only the
players' messages and observers read everyone's — the server filters by role (`GET/POST /api/matches/{id}/chat`).
A game without a room (a demo or a server without the route) answers 404: the icon hides and polling stops. The panel
polls every 3 s while the board is open. Test: `GameChatPanelTests`.

UI-259 (2026-10-18, owner's report): the chat field shows what is typed — parchment text on a dark field
(rgb 245,239,222 on rgb 46,41,34), a gold caret, selection and focus rim, and a quiet placeholder. Before, the text
had a contrast of about 1.1:1 against the field. Test: `BoardInteractionTests.ChatInputShowsTypedTextWithReadableContrast`
(at least 7:1).

UI-281 (2026-10-19, owner's report): with the chat open, Enter in its line sends the message. Before, the board's
confirm key (`DraftInput`) took Enter first, on its way down from the board's root, so the message stayed unsent and a
ready choice could be confirmed instead; the chat is now one of the inputs that key ignores
(`GameChatPanel.ContainsInput`), and Esc still rolls the panel up (`BoardEscape`). The panel always opens on its newest
message, also when messages came while it was rolled up or the player had scrolled up before closing it: the list
scrolls to its end on the tick after it is laid out (`RoomChatPanel.ShowLatest`). Tests:
`DraftInputTests.EnterInTheOpenChatSendsTheMessageAndConfirmsNothing` (a ready Hunt allocation stays unconfirmed by
the chat's Enter and is confirmed by Enter outside it), `BoardInteractionTests.ChatOpensOnItsNewestMessage`.

<a id="karta-przeciwnika--ui-308"></a>
## The opponent's card during a presentation — UI-308 (2026-10-24)

The card inspector may open without a click for one case only: the opponent's public Event during its presentation
([OPPONENT_ACTIONS_UX §6.12](OPPONENT_ACTIONS_UX.md#wdrozone-308)). It is transient and read-only, stands in the half
of the map away from the card's first effect, keeps the inspector's viewport rules (no shrunk text, no scrolling), and
closes into the console trail after 3 s; a click pins it and holds the presentation until it is closed. The inspector's
remembered position, pinning and Esc behave as before.

<a id="okna-na-srodku--ui-324"></a>
## Windows open in the middle — UI-324 (2026-10-25)

Owner: "every window that opens should open in the middle — the Hunt, the cards and so on".

- The Hunt window, in every phase, and the battle window open centred in the board's middle (`FloatingWindow.Centre`,
  `WindowLayout.Middle`): between the top bar and the console, between the side columns. Before, they stood beside the
  Fellowship and beside the battle's region; a Combat choice still keeps its card's room on the right.
- So does any window without a placement of its own: the information, resource, Companion setup and battle report windows,
  the Log.
- A position the player dragged a window to still wins. The positions are on the pixel grid, and a window already within
  a pixel of its middle stays put: a middle on a half pixel cannot rock it between two pixels (a layout loop).
- The middle leaves out a panel held open beside the left rail, so a decision's Reserve or Politics stays usable;
  windows move clear when such a panel opens (`WindowLayout.MiddleMoved`). A hover preview does not count.
- A window wider than the middle of a narrow board keeps the hand clear and may touch the left edge.
- The region panel (it belongs beside its region) and the edge panels are unchanged.
