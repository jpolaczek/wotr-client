<a id="ui-12--powrót-do-zapisanej-partii"></a>
# UI-12 — Back to Saved Lot

<a id="kontrakt-i-plan-przed-implementacją"></a>
## Contract and plan before implementation

R05/R07/R10/R13/R21; S09/S13/S20–S22. We use the received endpoints [T35](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/server/T35-CORRESPONDENCE.md), the existing UI-10 revision gate, the draft reconcilization and the private UI-11 policy. We do not change the rules of the game.

1. The list of games shows the opponent, the page, the short decision context, the status of Your
   decision/Waiting/result and the symbol of the new changes. Filters and pages are clickable.
   Preparation rooms remain a separate part; no debug columns, revision numbers and ID. Refresh is
   to keep existing controls.
2. Resume opens the current board and tray without animating history. Changes were optional preview
   (removed in MENU-13, 2026-10-15); GET does not mean reading or responsible for the player. The
   client saves the seed only for the revision. Old screen/filter answers cannot replace the active
   list or board.
3. The list refreshes periodically only when it is displayed. The state of the non-connection does
   not remove the last list. Notifications are only in the application.
4. The local warehouse has a server + account + party range, separate from PlayerPrefs and from the
   authoritative state. It retains draft with a small decision context and the pending command
   (gameplay and cards policy). It does not save password, session token, full projection or
   opponent data.
5. command with the constant ID/payloadem must be placed in a permanent log before HTTP. The write
   error stops the upload. Upon return, the client first takes the receipt; only its absence allows
   you to re-write the same command. Removing the entry requires the correct ID, so the late
   response does not delete the newer command.
6. The files are stored atomicly in the user profile directory, with the version and validation of
   the scope. We perform disk operations outside UI rendering. draft is not automatic command: after
   playback passes existing CanCarry/Reconcile and new server preview. The unmatched decisions
   remove draft; legal parts remain without strategically matching kosztu/celu.
7. Offline allows you to view the map/card and correct the unsent draft. Approval requires a current
   connection. The new device is playing the server save; the unsent local draft remains local, it
   is not a secret decision of the game.

<a id="sprawdzenia"></a>
## Checks

- EditMode: range isolation, permanent save/reading, incorrect record before HTTP, ID/payloadu
  compatibility, reconnection and old answers.
- PlayMode with real HTTP/WSS/PostgreSQL: list and filters of both accounts, changes without ack,
  return and seen, restart the client after sending before the response, lost the receipt of the
  cards policy, two devices and reboot the server.
- Returns to the family records: Ring/cost, covered card, casualties/retreat, Hunt/route, effect
  card, Council, Corruption Kings and game over.
- S09/S13/S20–S22 and 1366×768/1920×1080 × 100/125%, HR review, no new number fields and scrolls in
  decisions. List/history can scroll.
- Regression of transport, draft and previous views; results, structure, `tasks.py validate` and
  local commits. Status only in task registry.

<a id="checkpoint-lokalny-dziennik"></a>
## Checkpoint: local journal

`Runtime/Persistence` distributes atomic files, the command log and the queue of the newest draft. The directory name is server/accounts/game hash and the record is valid for full range and version. Access lock includes Checks/recording/usuni flex; the temporary file is flushed before the atomic swap. CommandSender waits for a permanent record before HTTP and removes only matching ID after the retrieve of the receiver. Error leaves the command unvariable. Sketches save minimal context and choices; local history Undo is not recorded. Quick edits are combined into the recording of the latest draft without working on the disk while rendering. The server still validates all choices.

First EditMode layer: 175/175. After adding seven attempts: 178/182, `.artifacts/ui12-journal-editor.xml`; the InvalidDataException support required open packaging, and two fixtures had no Content and wrong deck field. Next 181/182, `.artifacts/ui12-journal-editor-fixed.xml`: the last fixture mutated the previous projection in place instead of delivering a new one. It was changed to an independent view of the next window. The result of the re-entry will be written below. This is still a local layer; the production connection to ClientFlow and the views are in progress, and network scenarios with the client reboot are not yet included.

Renewing `scripts/unity.sh -runTests -testPlatform EditMode -testFilter 'LocalRecoveryTests;CommandJournalTests;DraftContinuityTests;UI10TransportTests'`: **15/15 PASS**, no omissions, `.artifacts/ui12-journal-final.xml`, 12:06:45–12:06:49 UTC. Includes permanent ID/payloady before HTTP, both results of Receipt compilation, isolation, atomic substitutions, mistaken records and reconcilation.

<a id="checkpoint-integracja-klienta"></a>
## Checkpoint: Client integration

ClientFlow opens the batch list by LobbyBrowser; the lines keep the controls refreshed, and the filter and cursor reject the late answers. Tables leave the table set and preparation. Changes only show the history allowed by the server without reading confirmation. MatchSession is responsible for board, transport and local continuation; MatchSeenTracker saves the revision only after the scheduled rendering mounted board. Failure to save this auxiliary writing does not block the decision.

Before changing the batch, we finish the draft queue. New opening reads the action/politics logs and then starts the connection. Reconct automatically recovers the unsolvable receipt with the unchanged ID. The answers of the released controller cannot clear draft from the new instance. The damaged magazine leaves a clear error and the possibility of returning; we do not delete the unrecognized record and send the new command to its place.

Full EditMode after integration: **182/182 PASS**, zero skip, `.artifacts/ui12-integrated-editor.xml`, 12:20:06–12:20:11 UTC. Command: `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/ui12-integrated-editor.xml -logFile .artifacts/ui12-integrated-editor.log`. This confirms the compilation and earlier regression; a new list and new returns in PlayMode require separate checking.

<a id="macierz-nowych-prób-sieciowych"></a>
## New network test matrix

All scenarios below use production ClientFlow, normal sessions, HTTP/WSS and PostgreSQL. ReturnScenario saves a separate fixture for two test accounts; does not add endpoint state modification. The family's base comes from a verified T23 body, expansions from fixture UI-09.

| Test | Observable condition |
| --- | --- |
| ReturnListTests/list | 23 parties, 20+3 pages, four filters, preserved controls, Changes without ack, ack after board. |
| ReturnListTests/families | Both sides open the cost after the Ring, covered by card, losses, retreat, Hunt, route, effect, Council, Kings and end. View equal to direct GET; openly does not execute the decision. |
| ReturnRecoveryTests/command | Answer stopped after the commit. Client released, Kestrel process discontinued and restarted; new client with the same directory recovers the receipt without POST and the second kosztu/doboru. |
| ReturnRecoveryTests/politics | Lost response to private settings; a fresh client recovers the journal, the opponent gets the same view. |
| ReturnDraftTests | The actual server stop: map/card available, draft edit works, commit is disabled. Other device uses selected die; return preserves the deck, removes the cost and requires a new one to be indicated. |
| SavedGamesLayoutTests | List and optional Changes in four layouts; available navigation, text and hitboxes. |

The client interrupts the entire ClientFlow and panel instance, releases/API connections and re-logs a fresh session; it does not retain draft or sender objects. This is an automatic attempt to restart client logic in one Unity process, not a declaration to kill the system application process. The server is a real separate process running on its own port and restarted by the test. Manual user attempts remain postponed.

The first course of this matrix and the regression of UI-10/UI-11 is underway; results and possible repairs will be added after it is completed.

The first PlayMode run was discontinued without the final report (exit 143): the helper clicked the party buttons outside the ScrollView viewport. It was corrected to actually reveal the line before the indicator event, without replacing the private method call with a click. The fixture Kings control also showed that the source script started before AttemptCorruption; the new fixture performs this legal command and forces waiting inside CorruptionTile. We do not count the interrupted run or the creation of tests.

<a id="wyniki-macierzy"></a>
## Matrix results

`DOTNET_CLI_HOME="$PWD/.artifacts/dotnet-cli" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true bash scripts/test-client.sh -testFilter 'ReturnRecoveryTests;ReturnListTests;ReturnDraftTests;SavedGamesLayoutTests;UI10NetworkTests;BattlePolicyNetworkTests'`: **11/11 PASS**, zero skip, `.artifacts/ui12-second-play.xml/log`, 12:33:38–12:34:15 UTC. Server and seeder built with 0 bugs/warnings. Included are all six new attempts and five regressions UI-10/UI-11. Watched `ui12-games-1366-1.25.png` and `ui12-changes-1366-1.25.png`: text is not cut, navigation and Resume available, panel remains on the screen. Scroll only applies to list and optional history.

Additional EditMode after three tests late lists/exempt screen and ack connection: **185/185 PASS**, `.artifacts/ui12-final-editor.xml`, 12:36:17–12:36:23 UTC. The final review completed the wait when the screen was closed with the ongoing entries of both logs (not only draft); errors remain reported by the sender and another batch does not inherit the rejected Task. Added the attempt to reopen after a parallel recording of both logs. The latter change requires a repeat of EditMode and a full PlayMode.

Final EditMode after waiting for the logs: **186/186 PASS**, zero skip, `.artifacts/ui12-flush-editor.xml`, 12:38:32–12:38:38 UTC. Command: `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/ui12-flush-editor.xml -logFile .artifacts/ui12-flush-editor.log`. Full PlayMode will also include re-testing new tests on this version.

Full PlayMode `bash scripts/test-client.sh` : **263/264**, 0 skips, `.artifacts/ui12-full-play.xml/log` , 12:40:26–12:55:21 UTC ( 894,70 s). Only failure: old MovementNetworkTests He expected a permanent Check result button and manually clicked it after the reconnect. UI-12 retrieves the receipt after fresh projection/after The connection, so this button could have disappeared before reading the test. Instead of forcing manual behavior, the test measures now reliable GET primary receipt ID and no new POST/wydatku. Others 263 tests, including full return matrix, have passed. Targeted re-class re-class and tests are underway UI-10/UI-11 and return; we do not declare a single report 264/264.

Final Renew `scripts/test-client.sh -testFilter 'MovementNetworkTests;ReturnRecoveryTests;ReturnDraftTests;ReturnListTests;UI10NetworkTests;BattlePolicyNetworkTests'` : **11/11 PASS**, zero skip, `.artifacts/ui12-regression-fix.xml/log` , 12:57:39–12:58:18 UTC Automatic confirmed GET motion receipt, fixed ID and one cost, along with all new returns and earlier cases of hand check result. acceptance It's full **263/264** plus targeted re-establishment of the repaired class **11/11**, not single 264/264. The final production code also has **186/186 EditMode**.

<a id="przekazanie"></a>
## Handoff

The revised modules: Application (ClientFlow, LobbyBrowser, MatchSession, MatchSeenTracker, decision controllers), Persistence, snapshot/session draft, UI/Rooms, tray, Games button and styles. Tests separate local I/O, HTTP/WS/PG, fixture entries, restart process and layout. Documents of contracts, architecture, UX, scripts and TESTING are up-to-date; the structure and validation of the register is checked. There are no new rules or gate source.

Next stage: UI-13 — measurements and availability of desktop. The user 2026-10-06 confirmed the absence of Windows environment and recommended to prepare a build with the open record of the unexecuted Windows test. We do not count runtime Windows or gameplay between platforms based on macOS automation. Manual user attempts are still postponed. The compatibility of the receipt between versions remains the open, already planned T37 range.
