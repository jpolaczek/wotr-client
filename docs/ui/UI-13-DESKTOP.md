<a id="ui-13--dostępność-i-pomiary-desktopu"></a>
# UI-13 — desktop availability and measurement

<a id="plan-przed-implementacją"></a>
## Plan before implementation

R16/R18/R22/R23, S23–S24 and regression of changed S01–S22 paths. We do not change the rules or options/commands server. Interface settings are local and do not select any cost, card, route or reaction.

1. The settings have a 100/125% scale of buttons selected and a Reduced Motion. The change works
   immediately, it is stored on the device and does not switch the batch or reset the draft.
   Opening/closing and focus have available clicks; Escape closes the settings without undoing
   draft under the panel.
2. The choice has a separate outline/symbol, except for the color; focus from UI-191 (user decision
   with 2026-10-16) has no outline. Hitboxes and contrast are checked on the actual controls, in
   four layouts. The lack of number entry and scroll of the decision remains a requirement.
3. The current client does not play historical animations or wait for the opponent to move. Short
   cosmetic passages do not change geometry or condition; the reduced motion turns them off
   immediately. We do not delay visible selection or approval.
4. The desktop measurement includes a full map, long battle/loss, card and a hot local selection
   path. The report has hardware, OS, Unity/build, resolution, scale, warm-up, frame count,
   medium/p95 times, allocations and feedback limit 100 ms. We separate Editor/fixtures from the
   real Player. The unavailable counter cannot be reported as zero.
5. Profiling has a separate tool code; it does not add test endpoints, does not reveal tokens and
   does not enter normal UI. We use ProfilerRecorder from Dispose, and we serialize samples after
   measurement, not in every frame.
6. We will build macOS and Windows. You confirmed the absence of Windows environment:
   start/profiler Windows and the game between platforms remain unexecuted, compilation does
   not replace them.

<a id="odniesienia-techniczne"></a>
## Technical references

Unity documents the availability of ProfilerRecorder in Editor and Player, validation of meters and the need to Dispose in [API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html). Panel scale is a [PanelSettings.scale](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UIElements.PanelSettings-scale.html) property. [Passages USS](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-Transitions.html) is used only for cosmetic color change, without a animation of the layout.

<a id="weryfikacja"></a>
## Verification

The results below are actually performed controls; acceptance all saves the to-do register after the last regression.

<a id="implementacja-ustawień"></a>
## Implication of settings

`InterfaceController` surrounds the changing screens with one container. The settings block the entry to the screen under the bottom but do not stop acceptance from the projection. Escape in the settings does not go to `DraftInput`. Scale and movement are local preferences of the 1 version; damaged/unknown save restores readable default 100%. They do not store the batch data or authentication.

Focus has a separate cover with a clear outline inside the control. It is not a child `TextElement`: the first attempt showed a change in the measurement of the button text and the regression of clicks. The cover tracks the rectangle only during focus, does not take over the indicator and does not rebuild the controls. From UI-122 the cover is visible only after interaction with the keyboard, not after clicking. UI-191 (user decision with 2026-10-16) removed the cover: the focus does not draw any contour.

Player measurement will use separate, open-ended projections to load the presentation (start map, expanded loss panel, card). This is not acceptance rules or games over the network: these check the domeny/HTTP/Unity. scenarios The tool does not execute commands and does not charge kont/tokens. The report distinguishes the cost of synchronous handling the click and the time until the frame rendering ends; it does not present it as a measurement of the delay of input/monitor equipment.

<a id="pomiar-macos--2026-10-06"></a>
## Measurement of macOS — 2026-10-06

`python3 scripts/profile-client.py` launched the built Development Player (without Editor in the application process), macOS 15.6.1 , Apple M1 Pro CPU/GPU , 16 GB RAM , Metal, Unity 6000.6 .0f1. Artefact: `.artifacts/ui13-player-profile.json` and corresponding log. Additional Unity process built Windows at that time; the measurement takes into account the load. All 16 the cases were correct. 120 warm-up frames and 360 Zoom worked every frame, casualties/cards a 15 frames. Clicks verify the actual change of selection.

`targetFrameRate=60` , `vSyncCount=1` screen 120 Hz: effective synchronization led to approximately 120 FPS , not the limit 60 The result shall assess the objective at least 60 FPS on this configuration, does not guarantee the result on each desktop. 125% included Reduced motion, at 100% color transitions 80 ms. Limit 100 ms is about local response, not waiting for server response.

| Scene | Resolution | Scale | WED FPS | Frame p95 ms | Max ms support | To the end of the maximum rendering ms | GC WED B/frame |
| --- | --- | --- | --- | --- | --- | --- | --- |
| map-idle | 1366×768 | 100% | 120.04 | 9.17 | — | — | 41 |
| map-zoom | 1366×768 | 100% | 107.19 | 16.66 | 6.40 | 10.29 | 591 |
| battle | 1366×768 | 100% | 116.29 | 9.29 | 5.84 | 14.05 | 1431 |
| card | 1366×768 | 100% | 118.36 | 9.28 | 2.77 | 5.94 | 112 |
| map-idle | 1366×768 | 125% | 118.38 | 9.26 | — | — | 41 |
| map-zoom | 1366×768 | 125% | 106.93 | 16.69 | 4.53 | 8.24 | 599 |
| battle | 1366×768 | 125% | 113.96 | 15.87 | 3.17 | 11.37 | 1431 |
| card | 1366×768 | 125% | 114.90 | 9.35 | 2.18 | 6.63 | 112 |
| map-idle | 1920×1080 | 100% | 120.01 | 9.20 | — | — | 41 |
| map-zoom | 1920×1080 | 100% | 116.47 | 9.31 | 5.30 | 7.65 | 751 |
| battle | 1920×1080 | 100% | 112.94 | 15.85 | 3.70 | 14.74 | 1432 |
| card | 1920×1080 | 100% | 116.28 | 9.35 | 2.66 | 6.88 | 112 |
| map-idle | 1920×1080 | 125% | 119.67 | 9.25 | — | — | 41 |
| map-zoom | 1920×1080 | 125% | 104.35 | 16.74 | 5.25 | 12.07 | 670 |
| battle | 1920×1080 | 125% | 114.01 | 15.97 | 4.85 | 11.64 | 1431 |
| card | 1920×1080 | 125% | 117.39 | 9.32 | 2.43 | 6.98 | 112 |

The meters were available, after 360 samples: Main Thread in nanoseconds, GC Allocated In Frame in Bytes. The report also stores mean/p95/max meters. The main thread includes waiting for frame synchronization. The battle allocations reached 21 016 B in the selection frame: update of draft, summary and data of the visible selection. The main trays are kept at the unchanged layout key. No conversion of the entire UI every frame; a stable map about 41 B/klatk including measurement. The threshold is preserved, so no control or additional cache pool without the shown profit. We do not report zero allocations.

<a id="dostępność-i-regresja-ustawień"></a>
## Availability and regression of settings

EditMode: `.artifacts/ui13-preferences-editor.xml` **191/191 PASS** and with the export of the load `.artifacts/ui13-profile-editor.xml` **192/192 PASS**, 0 skip. The initial profiler test was not compiled because the project does not turn on the ScreenCapture module; the unnecessary player discharge has been removed. Visual control uses real-world panel personnel from PlayMode.

PlayMode: `.artifacts/ui13-settings-play.xml` **6/9**, after the `.artifacts/ui13-focus-play.xml` adapter adjustment **4/4 PASS** (sets and battle). The first run also included both expansions matrix, configuration, list of saved batches and handling of mouse/smooth map. `ui13-settings-*` crews include four layouts; the 1366×768/125%. settings are located without scrolling and numbering fields, focus does not change geometry, Escape does not reverse motion, camera and draft remain preserved.

From the calculation of color pair luminance USS: Normal text/panel   12,72:1,  action   8,53:1, hover  5,72:1 labels muted/panel   8,01:1, outline fokusu/button   10,19:1 The choice of scale has ✓ and outline, losses have × /↓,  dice paid ✓ pages figures/labels — the information is not based solely on colour, these are the measurements of the indicated pairs, not the declaration of full audit of assistive technologies. plaice attempts are automatic fractional scrolling events; the user has still not tested manually.

<a id="buildy-i-zakres-platform"></a>
## Buildings and platform range

The first `BuildWindows` compiled ARM64, as shown by the header PE. `BuildTasks` now chooses openly Windows x64 and macOS Universal (x64 + ARM64), regardless of host architecture. The architecture is a setting separate from `StandaloneWindows64`; see [API Unity](https://docs.unity3d.com/ja/current/ScriptReference/BuildTarget.StandaloneWindows64.html). Windows runtime and Windows game ↔ macOS were not run according to the user's disposition. Mac was measured on ARM64; physical Intel Mac was also not available. T36/T39 must move these limitations.

<a id="polecenia-odtworzenia-i-buildów"></a>
## Play and Build commands

```sh
bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/ui13-profile-editor.xml -logFile .artifacts/ui13-profile-editor.log
bash scripts/unity.sh -quit -executeMethod Wotr.Editor.BuildTasks.BuildMac -logFile .artifacts/ui13-build-mac-universal.log
python3 scripts/profile-client.py
bash scripts/unity.sh -quit -executeMethod Wotr.Editor.BuildTasks.BuildWindows -logFile .artifacts/ui13-build-windows-x64.log
```

Both builds ended with `WOTR_BUILD_SUCCESS`. `file` confirmed macOS Mach-O Universal x86_64/arm64 and Windows PE32+ x86-64. They are locally in `.artifacts/builds/mac/Wotr.app` and `.artifacts/builds/windows/Wotr.exe`. They were not signed or published; T39 prepares release packages. The numerical report was preserved in [profiles/ui13-macos.json](profiles/ui13-macos.json).

Final regression of interactions ( `test-client.sh -testFilter` for InterfaceSettings , ExpansionMatrix , BoardInteraction , Movement/Battle/ Fellowship/Character/CardNetwork , ExpansionDecision/Token/Corruption , ReturnList and UI10Network: `.artifacts/ui13-regression.xml` **44/46 PASS**, 0 a skip, 13:31:32–13:34:29 UTC Two tests. UI10 stopped at the log of HTTP 429 ; Kestrel confirmed the exhaustion of the correct limit 20 logins/ min/IP. Production protection has not been changed. Separate replay UI10 uses a new server; its result is saved separately, without presenting 46/46 as one course. In T37 remains readable error mapping 429 and limit handling by test tools. All changed checks geometry/size dice in both matrixes expansions It's been through.

Review `test-client.sh -testFilter UI10NetworkTests`: `.artifacts/ui13-network-rerun.xml` **3/3 PASS**, 0 skip. Includes late receipt/przytrzymany Enter, race of two devices and reconnection. Structure after review: preferences in Persistence, coordination in Application, views/fokus in UI, profiler in separate Diagnostics, startup/build in scripts/Editor. Hot measurement does not serialize JSON nor seek static data; the buffer is of a fixed size.

After the final increase of the hitboxes, both builds were re-established: `.artifacts/ui13-build-mac-final.log` and `.artifacts/ui13-build-windows-final.log`, both `WOTR_BUILD_SUCCESS`. The binary headers retain Universal/x64. The change is for the expansions pool; the measured workloads of bases/battle/cards did not change the layout. Further acceptance full batch and complete regression S01–S24 leads T36.
