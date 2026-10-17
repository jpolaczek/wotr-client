<a id="t36--końcowy-odbiór-ergonomii"></a>
# T36 — End-of-life acceptance ergonomics

<a id="plan-przed-weryfikacją"></a>
## Plan before verification

The R15–R23 range, S01–S24. We do not add rules or strategic shortcuts. Automatic test scenario uses only legitimate self-projection options and UI clicks. It does not set the result, does not inject RNG, does not read the full state of the server. The start comes from normal room, common acceptance of configuration and production setup.

1. The full batch of the base and the Lords + Kings + Fate + Council + New Cities with default
   Dismay. FP leads Fellowship through a legal corridor, Shadow rejects dice, Hunt uses the server
   RNG. Gandalf Keeper/Guide version in the expansions test exercises the dice election. This script
   checks continuity and UI; exceptions cards/battle/Corruption still have separate, targeted tests. We
   do not manage to cover each rule with one game.
2. Both sides at 125% and Reduced motion. Check trays and maps before and before the decision is
   approved, without scroll and number fields. Watched card and the waiting player camera are to
   remain stable. Reconct both clients every three turn and after the result.
3. The regression of difficult states and the S01–S24 matrix indicate specific tests, previous
   results and current acceptance. The UI-13 Player profile remains separate from the two panels in
   the Editor.
4. Windows runtime, physical Intel Mac and manual user tests are not performed. The user has
   commissioned Windows build with no attempt to save. We do not change the dependency graph or
   replace the run test with static check of the PE file.

<a id="wyniki"></a>
## Results

Full parts after map repair: **2/2 PASS**, zero skip, `bash scripts/test-client.sh -testFilter T36DesktopGameTests`, 2026-10-06 13:59:27–14:01:23 UTC, 116,13 p. Report: `.artifacts/t36-full-games.xml` and corresponding log.

| Configuration | Turn / decisions | Production result RNG |
| --- | --- | --- |
| Base | 11 / 148 | RingCorrupted, Shadow |
| Lords + Kings + Fate + Council + New Cities + Dismay | 7 / 100 | RingCorrupted, Shadow |

Two clients UI Toolkit 1366×768 scale 125% , Reduced motion, macOS 15.6.1 , Apple M1 Pro, Unity 6000.6 .0f1, Kestrel/.NET 10 and PostgreSQL 17.11 Both results are consistent in projections; reconnect every three turn And then he doesn't change the outcome. command sent after completion returns GameFinished without a new revision. Each decision checks the tray and resources, and each route indication checks the actual hit-test target. The observer keeps the camera and card The tracks are in ignored artifacts. `t36-base-full-game.json` and `t36-expansions-full-game.json` No accounts session.

These are automatic games with a deliberately simple, permanent test strategy. They are not a balance or study attempt with an external player. They do not cover all exceptions to cards and expansions; these have targeted tests.

First run `T36DesktopGameTests` : **0/2**, 13:45:06–13:48:08 UTC The Assertion of the Beginning Council was expecting turn 1 although private preparation is still in turn 0 The second case exceeded the default limit of Unity test. After the assumption amendment and addition of diagnostic single decision second course **0/2** ( 13:52:24–13:52:38 ) indicated the choice of the route of the declaration. XML/logi `t36-games-first` and `t36-games-second` and error-related personnel. These are not results acceptance .

The diagnostic tests `t36-route`, `t36-target`, `t36-pick` showed that the visible route button did not receive a click. In an enlarged view with full setup and pinned card `panel.Pick` indicated the `army-south-dunland-Shadow` label over `target-fords-of-bruinen`. This product error, not server or randomness. The space booking for the targets was moved before the Army label; labels without space go to compact, clickable symbols with full tooltip. The script control now checks the actual target hit test, its visibility and no overlap with pinned card.

<a id="powiązanie-scenariuszy-z-dowodami"></a>
## Linking scenarios to evidence

| Scenarios | Attempts to preserve and prior protocol |
| --- | --- |
| S01–S06 | BoardInteractionTests, T22BoardNetworkTests, T22TurnNetworkTests, T22AbilityNetworkTests; [T22](T22-ACCEPTANCE.md). Divisions, both movements, excesses, costs and recruitment. |
| S07–S08 | CardInteractionTests, CardNetworkTests, cards packages; [SCENARIOS](SCENARIOS.md), UI-13 regression cards. |
| S09–S12 | BattleNetworkTests, StrongholdNetworkTests, BattleLayoutTests, ReturnListTests; [UI-12](UI-12-RETURN.md). |
| S13 | BattlePolicyNetworkTests, ReturnRecoveryTests; private record of preferences and expiration in the new battle, [UI-12](UI-12-RETURN.md). |
| S14–S16 | FellowshipNetworkTests, FellowshipLayoutTests, T22BoundaryNetworkTests and full T36 parts. |
| S17–S19 | ExpansionDecision/Token/Corruption/Ability/WindowNetworkTests, ExpansionMatrixLayoutTests; [T34](T34-ACCEPTANCE.md). |
| S20–S21 | UI10NetworkTests, MovementNetworkTests, ReturnDraft/RecoveryTests; [UI-10](UI-10-NETWORK.md), [UI-12](UI-12-RETURN.md), observer T36. |
| S22 | ReturnListTests and SavedGamesLayoutTests; list of 23 parties and the families of entries, [UI-12](UI-12-RETURN.md). |
| S23 | Layers of the layout 1366×768/1920×1080 × 100/125%, InterfaceSettingsTests and full T36 parts. |
| S24 | [UI-13](UI-13-DESKTOP.md): 16 Player measurements, [raw report](profiles/ui13-macos.json), reduced motion in T36. |

The table identifies the evidence, does not declare a separate re-test of all historical tests. Results of the current course will be recorded separately.

<a id="regresja-po-naprawie"></a>
## Regression after repair

`bash scripts/test-client.sh -testFilter 'BoardInteractionTests;T22BoardNetworkTests;T22TurnNetworkTests;T22AbilityNetworkTests;T22BoundaryNetworkTests;BattleNetworkTests;StrongholdNetworkTests;BattleLayoutTests;FellowshipNetworkTests;FellowshipLayoutTests;ExpansionMatrixLayoutTests;LayoutTests;InterfaceSettingsTests'`

**60/60 PASS**, zero skip, 14:02:10–14:07:53 UTC , 342,53 s; `.artifacts/t36-regression.xml/log` Filter `LayoutTests` includes also Layout classes cards , expansions , characters, configuration and list of records. Attempts check both viewports and scales, full text, dense compositions, clicks and drags, and the most difficult choices army/battle/Dru The rye was observed after the amendment. `ui-1366-overview.png` (extreme prototype map) and `companions-1366-1.25.png` (fixture of seven travel groups). Previous targeted results UI-10/UI-12/UI-13 still include correspondence, private discard cards and resistance to loss of response.

S01–S24: PASS in the described range of automatic macOS tests and performer review. This is not acceptance with user or Windows.

<a id="ponowny-profil-playera"></a>
## Player Re profile

After change `MapCanvas` performed `BuildTasks.BuildMac` , then `python3 scripts/profile-client.py --report .artifacts/t36-player-profile.json` . [Full numerical report](profiles/t36-macos.json) : 2026-10-06 14:10:39 UTC , macOS 15.6.1 M1 Pro 16 GB Metal, Unity 6000.6 .0f1 Development Player, Universal running on ARM64 During this profile, the build Unity did not last. The same transparent workloads of presentations and method as in [UI-13](UI-13-DESKTOP.md) : 120 warm-up frame + 360 measurement, FPS from frame time, feedback from selection to end render, without network and hardware input. 125% Reduced motion; at 100% transitions 80 ms. VSync = 1 , monitor 120 Hz; targetFrameRate = 60 does not oversimulate sync.

| Workload | Render / scale | Average FPS | Max. feedback to end of render |
| --- | --- | --- | --- |
| map-idle | 1366×768 / 100% | 120.00 | — |
| map-zoom | 1366×768 / 100% | 120.00 | 6.46 ms |
| battle | 1366×768 / 100% | 114.56 | 13.98 ms |
| card | 1366×768 / 100% | 120.00 | 6.75 ms |
| map-idle | 1366×768 / 125% | 120.00 | — |
| map-zoom | 1366×768 / 125% | 120.00 | 6.40 ms |
| battle | 1366×768 / 125% | 113.98 | 13.43 ms |
| card | 1366×768 / 125% | 119.00 | 8.92 ms |
| map-idle | 1920×1080 / 100% | 119.67 | — |
| map-zoom | 1920×1080 / 100% | 119.66 | 8.97 ms |
| battle | 1920×1080 / 100% | 114.30 | 14.05 ms |
| card | 1920×1080 / 100% | 117.40 | 8.11 ms |
| map-idle | 1920×1080 / 125% | 117.08 | — |
| map-zoom | 1920×1080 / 125% | 85.89 | 14.09 ms |
| battle | 1920×1080 / 125% | 108.82 | 15.06 ms |
| card | 1920×1080 / 125% | 115.23 | 7.83 ms |

Averages of all workloads exceed the target 60 FPS ; the slowest zoom 1080p/125% achieved 85,89 FPS Maximum selection service: 6,20 ms; feedback to end of render: 15,06 ms, below 100 ms. This does not mean that each frame fits into 16,67 ms. Largest average allocation: idle 41 B/klatk s, zoom 710 B, battle 1431 B, card 112 B; summit when selection is changed in battle 21 016 B. Both meters ProfilerRecorder we do not declare zero allocations or result for other equipment.

Structure overview: Rules/transport behavior remains in their modules; repair affects two classes of `Runtime/Map` and style, full batch tests have separate selection strategies and desktop controls in `Tests/PlayMode`. Labels are re-used; selection of space is viewed only when the viewport grid is refreshed, without rebuilding the entire board. The cost of this path includes the above zoom profile.

<a id="buildy-i-przekazanie"></a>
## Building and transfer

`bash scripts/unity.sh -quit -executeMethod Wotr.Editor.BuildTasks.BuildMac` and the corresponding `BuildWindows`: **WOTR_BUILD_SUCCESS** in `.artifacts/t36-build-mac.log` / `t36-build-windows.log`. `file` confirms Mach-O Universal x86_64 + ARM64 and PE32+ Windows x86-64. Both builds contain map amendment. Only macOS ARM64 has been launched. Windows runtime, Windows–macOS, physical Intel Mac and manual user attempts are postponed. There is no open error blocking the proven operations. The next task is T37 (resistance, old records, privacy).
