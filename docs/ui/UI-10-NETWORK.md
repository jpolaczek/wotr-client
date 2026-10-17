<a id="ui-10--testy-granicy-sieciowej-w-unity"></a>
# UI-10 — Network border testing at Unity

<a id="kontrakt-przed-implementacją"></a>
## Contract before implementation

The source of the behavior is R09–R11, R16–R17, R19, R21, S20–S21 scenarios and the UI-10 document. Rules and costs remain in the engine. The new start script is to include the action phase, the legal hand and the available dice Events; it will be created only in `Wotr.Scenarios`, outside the server and its API.

command, previews, projections and confirmations supports the actual Kestrel with PostgreSQL. Controlled border HTTP can stop or lose the already received response. It does not create projections, legal options or command results. Notifications WebSocket go through the actual server channel.

<a id="planowane-sekwencje-odbioru"></a>
## Scheduled sequences acceptance

1. Loss of response: selection card The answer is waiting and the client receives the new
   condition by WebSocket The second player performs action The held Enter and the multi-click do
   not send another command . After losing the answer the recovery button reads the saved receipt;
   the older number of the receivet does not undo board nor does it fetch another card Next action
   requires a new hit on Enter.
2. Race of devices: two clients of the same page prepare action. The second device releases
   selected die, the opponent performs action, and the first one receives a new revision. The legal
   selection of deck remains, used die disappears from draft without automatic replacement. Delayed
   preview of the previous revision does not unlock approval. The player selects a new cost and
   reconfirms.
3. Delayed projection: GET response with an earlier revision waits while WebSocket delivers a newer
   state. Release of the old response does not undo the state. pinned card, camera position and
   magnification remain during action opponent and reconnection.

The regression will cover current actual traffic, recruitment, cards, Fellowship, Hunt, battles, sieges and characters. A separate audit of the UI-10 server will check raw payodays, bugs and privacy. The above sequences have not yet been implemented or executed.

<a id="checkpoint-2026-10-05--brak-miejsca-na-dysku"></a>
## Checkpoint 2026-10-05 — no disk space

The system refused even small records ( `ENOSPC` , approximately 134 Mi B the reported free space). Failed patche did not change the production code or the tests. The integrity of the affected files has been checked; new `InteractionScenario.cs` , client helpers and tests have not been created. [transport](UI-10-TRANSPORT.md) , [draft](UI-10-DRAFTS.md) and [server](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/server/UI-10-ACCEPTANCE.md) contain an audit and a plan before the code.

Removing the reproducible cache of Swift `.artifacts/swift-cache` modules recovered approximately 55 Mi B for transfer. Earlier cleaning included the unpacked Windows installer, old applications builds and byte identical copies of audit images; tools, canonical sources, reports and user data were preserved. Currently no standalone builds are available; they should be re-generated in a demanding build task. Do not start compilation with such a small space reserve.

After release at least 5 GB or the indication of available volume resume UI-10 according to workflow. First implement documented transport improvements and draft and reproduce crude payoad errors ( `moves[0].route = null` , `from = null` Then add three network scenarios above, run tests. NET Unity Editor and actual regression PlayMode Do not run builds simultaneously. NET nor change sources during Unity work. No new test UI-10 is not yet included. Last full regression before UI-10 : T21, 1648/1648 tests. NET report `.artifacts/test-results/t21-full-final.trx` .

<a id="wznowienie-2026-10-05-1931-utc"></a>
## Resumption of 2026-10-05, 19:31 UTC

Volume has again around 11 GiB free space. Restarted implementation; earlier checkpoint remains a lock history. Added test `InteractionScenario` (three independent parties), `UI10NetworkTests` and helpers of peace and controlled border HTTP The border delays the response already returned by the server, does not implement the atrap of its behavior. The race attempt deliberately stops GET revisions 1 initiated by WebSocket ; independent GET revisions 2 precedes this response by allowing you to check the transfer draft between compatible ordinary action When receiving an intermediate waiting window draft is not a strategic queue for future commands.

The new environment has limitations on recording outside the project and network. The results of the run-up and test tests must be saved separately from the implementation status. New tests have not yet been launched at this checkpoint.

<a id="wynik-implementacji-i-przekazanie"></a>
## Implementation result and transmission

The production corrections described in the transport documents, draft and the server are saved. View versions do not reverse the state; preview is related to the current decision and revision; sender retains the exact command and reads the receipt before the retra. draft retains legal choices, also by transient waiting, without automatic approval or replacing the cost. New mandatory windows have their own intention. `CommandShape` rejects defective army routes before performing the rules.

Changes to the network client tests: three `UI10*.cs` files in `Tests/PlayMode`, their metadata and the test `InteractionScenario.cs` and registration in `Wotr.Scenarios/Program.cs`. Seed only provides normal initial dice 4/7 files; additional ones remain deleted to meet entry rules. The `MountedMatchClient.Apply` helper mounts a fresh controller for an independent layout fixture; the actual network stream retains a constant controller and version filter. No reduced production filtering to handle artificial fixtures with the same revision.

| Verification | Actual result |
| --- | --- |
| Final build `Wotr.Tests` | PASS, without warnings/mistakes, `.artifacts/UI10-shape-build.log`. |
| Final build `Wotr.Scenarios` with Domain/Server/Contracts | PASS, without warnings/mistakes, 1,64 s, `.artifacts/UI10-seed-final-build.log`. |
| Clean domain methods by additional runner | 13/13 PASS; three exceptions were played before the amendment. This is not a full xUnit/VSTest. regression |
| Clean client methods by additional runner | 42/42 PASS: 31 Presentation, 5 draft session, 6 transport with controlled response HTTP. Without Unity native and actual network. |
| Separate libraries with references and Unity flags | 6/6 PASS: Contracts, Client, Fixtures, EditorTests, PlayTests, Editor. C# 9/netstandard2.1, actual references, without Unity importer. `.artifacts/ui10-unity-assemblies/results.json`. |
| Relevant VSTest | Interrupted before being executed by `SocketException (13)` at `TcpListener.Bind`; `.artifacts/UI10-payload-repro.log`. |
| Additional TestHost sample with PostgreSQL | Six methods stopped before the HTTP Assertions: the connection of Npgsql to `127.0.0.1:55432` is refused. The seventh test of PostgreSQL is not performed. `.artifacts/UI10-inprocess-http-third.log`. |
| Unity Editor | It did not run importu/test: IPC/Global refusal to use the license service, timeout after 74,83 p. `.artifacts/UI10-editor.log`; process completed Ctrl+C, exit 130. |
| Unity PlayMode / actual Kestrel | Unexecuted after the above-mentioned locks are found. Three new sequences and family regression require launching. |
| Documentation, catalog and cover cards | `tasks.py validate`, `check_catalog.py`, `card_coverage.py --check`: PASS, 87 tasks, 30 families/34 windows, 96 kart/16 packages. |

Final scenario compilation command:

```sh
DOTNET_CLI_HOME="$PWD/.artifacts/dotnet-cli" DOTNET_CLI_TELEMETRY_OPTOUT=1 \
DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true \
bash scripts/dotnet.sh build tests/Wotr.Scenarios -c Release --no-restore \
  --disable-build-servers -m:1 -p:UseSharedCompilation=false
```

Structure review retains division: `Networking` is responsible for versions and retras, `Application/TurnDraftSession` for the intent cycle, `Presentation/DraftReconciliation` for legal selection parts, `Domain/CommandShape` for the shape of the untrusted input. Domain dependencies on Unity or DTO for the full state have not been added. Comparison of the version is constant-cost; reconnection copy small draft after state change, not all projection in each frame.

**The status of the checkpoint 19:54 UTC: UI-10 not received.** The limited runner does not check UI Toolkit, map, real WebSocket, two devices or durability PostgreSQL. The full regression of T21 concerns the code before these changes and is not the result of UI-10. The restoration requires an environment allowing VSTest to open a local port, Npgsql connect to the test base and Unity to use the license service. No environmental restrictions or transport to circumvent them were changed.

After unlocking: fresh prereads, `tasks.py start UI-10`, full `bash scripts/test.sh --no-restore`, proper Editor and `bash scripts/test-client.sh -testFilter 'Wotr.PlayTests.UI10NetworkTests'`; after correcting any problems full `bash scripts/test-client.sh` covering all the family base. Only then can you pick up UI-10 and go to T22. Manual acceptance user remains postponed. All started during this resume of compilation sessions and Unity were completed.

<a id="wznowienie-z-pełnym-dostępem--2026-10-05-2127-utc"></a>
## Full-access reboot — 2026-10-05, 21:27 UTC

The user changed the session permissions. The `socket.bind/listen` attempt to loopback passed, PostgreSQL and Unity license run. Full .NET: **1668/1668 PASS, zero skip**, `.artifacts/test-results/ui10-full.trx`; the correct Unity EditMode: **118/118 PASS**, `.artifacts/ui10-editor-full.xml`. The earlier locks are historical.

The first attempt of the new PlayMode tests revealed clicks before calculating the geometry of the new controls. The helper is now waiting for a visible, active button and the current layout. Then the actual late receipt scenario detected a production fault: the change of the draft instruction disabled `Check result`. `TurnInteraction.Render` shows such hints only for editable draft; network blocks and waits for revision are still working. Added the open access rating of the recovery button.

The final focus of `bash scripts/test-client.sh -testFilter Wotr.PlayTests.UI10NetworkTests`: **3/3 PASS**, without omission, 6,89 s, end of 21:36:47 UTC. Report of `.artifacts/ui10-focus-final.xml`, log `.artifacts/ui10-focus-final.log`; previous uncounted runs were preserved as `ui10-focus-first.log` and `ui10-focus-second/third/fourth.xml` and logs. The full regression of PlayMode is the next gate acceptance.

The repository had no commits before. `77453b5` saves existing implementation and history; `81cc3c6` recovery amendment and test. The instructions now require current local commits. `.gitignore` also excludes temporary scenes of the Unity Runner; account data, artifacts and cache remain outside Git.

<a id="wynik-odbioru-ui-10--2026-10-05-2144-utc"></a>
## Result acceptance UI-10 — 2026-10-05, 21:44 UTC

Full `bash scripts/test-client.sh` he's done all of them. 152 tests PlayMode **of Kestrel/PostgreSQL:** 150 PASS , 2 FAIL , no skip**, 346,89 p. `.artifacts/ui10-play-full.xml` and `.artifacts/ui10-play-full.log` keep the actual score. Both failures concerned the reset of independent fixture in `BoardInteractionTests` ; amendment `Decision.Id` with the same revision 0 was correctly discarded by the production filter. Helper now creates a new controller for the new layout, without weakening the validation of the view.

The entire revised class was then completed: `bash scripts/test-client.sh -testFilter Wotr.PlayTests.BoardInteractionTests` — **6/6 PASS , no skip**, 11,30 s, end 21:44:29 UTC Report `.artifacts/ui10-board-focus.xml` , log `.artifacts/ui10-board-focus.log` Other 146 cases include unchanged code after successful run; they were not repeated after local helper amendment. This is a full run with targeted regression of both repairs, **not a single report 152/152**.

Scope of integration received UI-10 : all the basic families use the real ones projection/options/podgl The sidings/commands, cost and randomness determine the server, and the new S20–S21 confirm recovery, race and delays. `git diff --check` They didn't show any problems. `tasks.py validate` , directory and coverage cards They're passing.

The next task is T22: full acceptance with UI product. Audit indicated to complete specific clicked variants (attribution Hunt, Ring/politics/pas, Active Skills, Random Victim, Defense Stronghold in the field, Military Finals) and four presentation shortcomings: loading before the first projection, constant policy access, detailed composition army/inspection during planning and safe history. These deficiencies are not considered to have been performed by transport tests. Manual acceptance user and Windows–macOS test remain postponed to the right tasks.
