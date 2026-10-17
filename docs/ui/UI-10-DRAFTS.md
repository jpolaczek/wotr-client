<a id="ui-10--zachowanie-lokalnego-zamiaru-po-aktualizacji"></a>
# UI-10 — behavior of local intent after upgrade

The description precedes the changes to the code. The source is the requirements of R09–R11, R17, R19–R21, UX U02 and [Draft contract](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/CONTRACTS.md). Reconcilion does not interpret the new rules of the game; it uses only the projection and server options.

<a id="granica-decyzji-i-podglądu"></a>
## Decision and Preview boundary

`DraftContinuity.CanCarry(previous, next)` allows the same identifier decisions in the same lot, turn , phase and perspective. Amended identifier may only be transferred between ordinary decisions `Action` the same player, source and window, without active battle , Hunt or loss of event. New round, tile, step card , reaction or mandatory choice has an empty draft. Received command your own client ends his intention also when the next action has similar options; the application layer is responsible.

The application layer skips older and duplicate projections before reconnection. The update of private preferences of automatic decisions does not delete local click history. Each new revision of the game invalidates the earlier preview, even if the payoad remained identical. Fresh preview and conscious approval are required before sending command.

`TurnDraft.Reconcile(view)` returns `true` when it has actually removed or changed some of its intention or context. It never selects a new dice, capabilities, character, region, route or figure. The call is a border of Undo: the previous history disappears, subsequent local edits can be re-edited to the agreed state. The absence of an old story is not a change of choice in itself.

<a id="zakres-zachowania"></a>
## Behavior scope

| Family | Behavior with New Options |
| --- | --- |
| die, Ring, deck, politics | Keep still available exact ID/wynik/cel. Delete only the unavailable box, without choosing the replacement. |
| Event and reaction | Keep card and the exact cost option if you are still offered. Losing cost does not remove legal karty/celu. Losing condition card removes dependent choices. |
| Grouped Effect Selection | Remove the ID and the choices that the public `RequiresAny` is no longer fulfilled. Keep independent groups. With a new limit requiring strategic removal of one of the several still legal ID players improves the selection; the client does not indicate the victim. |
| Army, attack, march event | Keep source, legal figure, target and exact route/prefect. Removing one figure does not remove the others. Illegal route is removed without replacing another route or shorter target. The editing focus remains with the same existing group. |
| Recruitment | Keep the legal assignments of figure-region and the selected reserve. If the total set exceeds the new limit, player adjustment is required; we do not arbitrarily choose which legal recruitment will survive. |
| Companion, Nazgûle, Fellowship | Keep groups and permissible route prefixes, also when the last region is only transitioning. Change of range, blocking or stopping region removes unacceptable route; does not shorten it to another purpose. |
| Guide, Hunt | Keep the legal ID Guide, dice reroll and conscious injury. Loss of opportunity does not choose another Companion or Corruption. |
| battle and event losses | Keep card, legal dice Metallage, figure and type of selected loss. Remove the unavailable figure or unacceptable degradation without switching to elimination. Reverse and progress retain the exact choice. |
| Entry and capacity of the form | Keep a specific form, ability, cost and matching targets. Do not use a letter of other decision family to filter out this choice. |

Local `Ready` does not replace server validation. A set that remains individually legal but no longer meets the combined limit remains visible and does not obtain legal preview. The restrictions on the list do not justify automatic strategic selection.

<a id="plan-sprawdzenia-i-koszt"></a>
## Verification plan and cost

Targeted Tests Editors will include the continuity of the usual action and the limits of all other decisions; unchanged draft without false signal; removal of only illegal part; cost with different variants of the same dice; dependent effect groups; exact route and prefix; focus of the group after removal of the previous one; no restoration of the old revision by Undo. Integration of HTTP, preview, two devices and WSS is conducted separately by the UI-10 coordinator.

The comparison of intent includes only a small payoad and a local focus, not full `PlayerView`. It does not serialize the entire projection or re-read the static map. Reconcilation works after changing the authoritative revision, not in each frame or for a duplicate notification. The results of the actual checks will be added after the start.

<a id="implementacja-i-punkt-przekazania"></a>
## Implementation and point of transfer

`DraftContinuity` recognizes the continuity of the window and `DraftReconciliation` filters the election and compares the small payoad. Individual families maintain their own presentation rules in existing `*Draft` files. Calling does not create a new historical step. While temporarily waiting for the opponent, the application layer retains separately the last active context and copy of the uncomposed action; upon return, it uses the same continuity criterion and re-checks the availability of the cost. die selected before the type of action is also subject to availability control.

Added 31 on-target cases Editor in `DraftContinuityTests`, `DraftReconciliationTests`, `DraftRouteReconciliationTests` and `DraftSelectionReconciliationTests`. Compliance control includes all `DecisionKind` values and other cases check intent behaviour without repeating the game engine rules. Undo history does not participate in the change of choice signal. Traffic costs limitations compare exact `DieId`, `Face` and `AbilityId`; they do not replace lost ability with the usual use of the same dice.

Control of the `python3 scripts/tasks.py validate` documentation: PASS, 87 tasks. The Editor tests have not yet been launched at this transfer; the Unity launch is coordinated with integration tests of the entire UI-10. Structure overview: new helpers have 25 and 92 lines, they use existing deep copy of the small draft, without new domain or transport dependencies. The full test result will be added after the run.

<a id="dodatkowa-walidacja-bez-silnika-unity--2026-10-05"></a>
## Additional validation without Unity engine — 2026-10-05

Current client sources, fixtures, Editor, PlayMode and Contracts were compiled additionally by `csc` with the actual local libraries managed by: 168 files, exit 0. Then the direct runner caused **42/42 cases with PASS score**: 31 described above Presentation tests, 5 `UI10DraftSessionTests` and 6 `UI10TransportTests`. He also expected `Task` results; the actual NUnit and the existing test limit of `HttpMessageHandler`, without the lister and the native Unity.

Accurate commands and limitations: `.artifacts/ui10-draft-runner/README.md` Local results: `.artifacts/UI10-draft-offline-run.log` , `.artifacts/ui10-draft-runner/results.json` , `.artifacts/UI10-unity-offline-compile.log` Reports are reproducible artifacts, not a documentation validator dependencies in a fresh checkout. SHA-256 proven client library: `91edde0ab46dc5d6609e7282044aca7660f91c91e5bd2b3f02dd2fbe2fd05498`.

This is an additional control of the clean code and the controlled transport response, **not Unity Editor result nor PlayMode**. Native Unity launch blocked the environment (licensing IPC/GlobalMutex ); VSTest also cannot create a mailer. Direct runner does not check scenes, UI Toolkit, rendering, importer, asmdef or actual HTTP/WSS. acceptance These areas remain on the full-launch side of the unlocked environment.

<a id="właściwy-runner-unity--2026-10-05-2128-utc"></a>
## The correct runner Unity — 2026-10-05, 21:28 UTC

After the permissions were changed, `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/ui10-editor-full.xml -logFile .artifacts/ui10-editor-full.log` was launched: **118/118 PASS, zero omissions**. This is the actual import and execution of Unity 6000.6.0f1 per macOS, covering all 42 new cases. The previous offline result remains a separate additional check; the license limitation is no longer valid. Full PlayMode and the final status are in [network protocol](UI-10-NETWORK.md).
