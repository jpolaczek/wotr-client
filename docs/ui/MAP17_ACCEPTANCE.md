<a id="map-17--raport-odbioru-mapy-z-konturów"></a>
# MAP-17 — acceptance report on contour maps

Final report from 2026-10-08 . Verification shall include . NET **3723/3723**, Editor **478/478** and full Play **440/440** in 42 hosts; new regression card total **441** unique Play cases, zero skip. HTTPS/WSS , three builds, macOS smokes and Development Player with 36 measurements and inspections of all 42 PNG counted within the defined scope. The status of the task remains only within [Register](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/tasks/tasks.json) The limitations on platforms and window placement are publicized below.

<a id="dane-i-zakres"></a>
## Data and scope

- `content/map-geometry.json`: `contours-2`, SHA-256
  `b1ed10e22091b22ea602df1d1b017387f7c5bf071d8ec5c1140a2c8721f4801a`; 105 regions, 106 rings, 323
  source paths. One viewBox transformation [335,40,1632,1338] by subtraction (335,40); 1632×1338
  client.
- `map-boundaries`: 360 classified parts, 282 pairs of physical contacts, including 232 legal and 50
  extra without transition. Traffic still results from the graph of rules and current server
  options.
- Canonic bands: 935 cut rings, 97 746 points. Profile counts 3 186 points of the water mask:
  BandPoints = 100 932 The regions with the source paths have SourcePoints = 41 232 so together,
  renderer gets 142 164 points. These are input data counters with MapProfileMemory/MapRenderer , no
  GPU vertics or the sum of variants.
- Sources: [import and
  restrictions](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/sources/MAP-12-GEOMETRY.json),
  [Comparison of borders with
  board](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/sources/map13-review/README.md),
  [plan and pallet](MAP_CONTOURS_UX.md), [Interaction](MAP15_INTERACTION_COVERAGE.md). interpolated
  source lines under mountains/panels and primary route tolerance remain public; acceptance does not
  add imaginary transitions.

<a id="mc01mc18-i-dowody"></a>
## MC01-MC18 and evidence

Each entry concerns the actual final result of MAP-17. Historical failures and repairs remain in a separate document of the task.

| Criterion | Specific proof / configuration | Result MAP-17 |
|---|---|---|
| MC01 | export_map_geometry `--check --borders`; DataTests `ReviewedRegionsRingsAndAnchorsShareOneCoordinateSystem` and `SourceAnchorsAndPngUseTheDocumentedTranslationAndUniformAspectFit` | Checker PASS 105/106/323/232; Data Editor 16/16 PASS |
| MC02 | DataTests `WaterHoleSeaAndConcaveNotchesDoNotSelectLand`, `EveryReviewedPathRetainsItsOwnersAndAllLegalContacts`; Python 8 Trials | Python 8/8 and Data Editor 16/16 PASS |
| MC03 | DataTests `MalformedContentFailsBeforeTheMapIsCreated` (11 wrong inputs) | All 11 variants PASS in full Editor |
| MC04 | export_map_boundaries `--check`; BorderTests `EverySourceSegmentHasExactlyOneClassificationAndSource`, `EveryLegalPairHasPassageAndAdditionalContactsRemainBlocked` | Checker PASS 323/360, 232+50; Python 7/7 and Border Editor 18/18 PASS |
| MC05 | BorderTests 5 Passes and Eastern Nurn; comparisons of source-review and Player morannon/minas-morgul/moria/high-pass before and after the highlight | Sources preserved; Border Editor 18/18 PASS; 7 par before/after Player PASS |
| MC06 | BorderTests `TwoNationBandsFollowActualRingSidesEvenWhenPathDirectionIsReversed`, `NationBandsDoNotUseMilitaryControlAndDoNotCoverBarriers`, `PaletteKeepsEightPrintedNamesAndDistinctColors`; legend of 4 layouts | Python bands/assets 4/4, Border Editor 18/18 and 4 legends Player PASS |
| MC07 | export_map_terrain `--check`; asset tests for the preservation of MAP-10 retouches and the removal of burnt borders; RenderingTests | Export 4096 ², assets  4/4, Rendering Play  3/3 and Review Player PASS |
| MC08 | RenderingTests `TerrainAndSharedBordersRenderBeforeAndAfterEveryEmphasis`, `EveryHighlightStaysBelowPrintedBarriersAndInsideItsRegion`, a legend with a real Pick; Player 7 frame pairs | Rendering Play 3/3 and 7 par Player PASS: barriers/bands preserved after highlight |
| MC09 | NavigationTests `BothRolesInspectContoursAtFourLayoutsWithoutChangingProjectedState`; both roles, empty fields, forces, card and keyboard; ContextHover/RegionInspection/SideControl | Navigation 2/2, full Editor and Play PASS |
| MC10 | InteractionTests 5 methods; 13 families and additional specific methods listed in MAP15_INTERACTION_COVERAGE | Interaction 5/5 and 31 exact methods 13 families PASS |
| MC11 | NavigationTests 105 anchors × pan/zoom, 4 source edges, small/concave fields; AcceptanceTests dense field/garrison in 4 layouts of both roles; Fellowship/routes | Navigation 2/2, Acceptance 2/2 and the correct regressions of the PASS routes |
| MC12 | InteractionTests reconnect the same revision and second device; NavigationTests `BattleNoticeAndRecordedActionUseCurrentAnchorsWithoutAutomaticCameraMotion`; privacy/projection .NET | Interaction 5/5, Navigation 2/2 and Full .NET 3723/3723 PASS |
| MC13 | OverrideTests 5 cases; RecoveryTests `BothRolesRenderServerOverridesAndRetainThemAcrossReconnect` ; database/Fate/New Cities both roles | Override Editor 5/5 and Recovery Play 4/4 PASS |
| MC14 | RecoveryTests `RemovingWhiteHandChangesTwoBandsButRetainsCaptureAndItsPoints`; gaining from 2 VP and 2 with changed bands; White Hand profiles | Recovery Play 4/4 and 4 workloads White Hand PASS; exactly 2 band on a change |
| MC15 | RecoveryTests switching records and `HistoricalPaidStepKeepsItsDraftAndRecoversUncertainCompletionAfterServerRestart` ; scheme- 3 Corps before contours- 2 exactly one receipt | Recovery Play 4/4 PASS |
| MC16 | New AcceptanceTests 2 method, 40 PNG with pan diagnostics; actual WheelEvent ±3 and ±0,25, right drag, power pickup; Player 4 fit, 4 maxzoom, 4 legends and dense scene | Acceptance and focus card+dense 2/2 PASS; 12 Play and 42 Player PNG viewed, limits of overlay |
| MC17 | `map-profile-2`, 36 workloads × 360 samples after 120 warmup; p95/p99, local handler/do render end, texture/heap/graphics/vertices/cache | 36/36 PASS: minimum 238,48 FPS in 1080p, max p99 8,856 ms, max feedback 17,808 ms; memory below |
| MC18 | 8 combination Lords/Kings/Fate both roles + New Cities/White Hand, full .NET/Edit/Play, build Windows/macOS and available Player; historical record and uncertain command | .NET 3723, Editor 478, Play 441 unique, TLS/WSS, three builds and smokes nonDevelopment macOS PASS |

<a id="polecenia-wykonane"></a>
## Executed commands

```sh
python3 scripts/export_map_geometry.py --check --borders
python3 scripts/export_map_boundaries.py --check
python3 scripts/test_map_geometry.py
python3 scripts/test_map_boundaries.py
python3 scripts/export_map_terrain.py --check
/tmp/wotr-map-trace-venv/bin/python scripts/export_map_border_bands.py --check
/tmp/wotr-map-trace-venv/bin/python scripts/map_render_asset_tests.py -v
python3 scripts/test_map_profile.py
python3 -m py_compile scripts/review-map-client.py scripts/test_map_profile.py
```

PASS: 8 geometries tests, 7 borders, 4 assembles and 6 validator measurements. Exports identical to canonical resources. Independent read-only review of fixture/test ed/profilera has not found the blocker; it does not replace compilation or required startup.

<a id="granice-pomiaru-i-platform"></a>
## Measurement and platform boundaries

The new profile has 9 workloads × 4 layouts. The data layer comes from the safe projection of the real engine, including the state after the approved movement; the profiling process has no account or network connection. The real TurnInteraction refreshes the tray and contexts. Only MatchId/Revision have a monotonous replay envelope to ensure that the repeated snapshots pass a production order control; the game/options data remain unchanged. The `scripts/test-client.sh` tests use separate HTTP/WS/PostgreSQL clients on 5088. The separate TLS/WSS dragons with 13:48:12Z have been credited with two Python clients by isolated proxys with local verification of CA and localhost. This is not a test of the TLS stack of the Unity client and does not change the server implemented.

Texture has a measurement of native Unity bytes. Geometry has a measurement of the managed memory of the first data/index charging and the calculated payoad coordinates. Mesh Memory counter and the respect of the driver are global for the Player; we do not assign the whole to the map. Public API does not isolate native UIR buffers only of this layer. P99 does not replace the maximum condition of local feedback 100 ms. FPS without limit/vsync describes rendering performance, not screen frequency, nor display delay.

Build Windows is not a Windows runtime test. Manual user, Intel, Windows machine and external host remain postponed as ordered. No publication or implementation server is performed.

<a id="końcowe-testy-i-transport"></a>
## Final tests and transport

- Full Editor `.artifacts/map17-editor-final.xml` : **478/478 PASS** including 39 cases MapContour
  , zero skip; 14:01 :04Z, after all the runtime amendments karty/Victory. First full score
  `.artifacts/map17-editor.xml` also had 478/478 PASS.
- Full Play: `.artifacts/map17-play-batches.json` indicates exact 440 with unique cases in 42 hosts;
  collector compares all names, variants and results, not only totals.
  `.artifacts/map17-play-result.json` — **440/440 and 42/42 PASS**. Each batch has a separate XML;
  the production login limiter remains on.
- Final T36 `.artifacts/map17-full-T36DesktopGameTests.xml`: **2/2 PASS** (13:49:07–50:48Z), full
  base and expansions at 125% and reduced motion, with normal recording, reconnect and game end.
- New CardViewportLayout and repeated dende: **2/2 PASS**, `.artifacts/map17-card-fix.xml`
  (13:38:27–39Z). Separately Victory Network/Layout **8/8** and
  CardInteraction/ContextHover/FloatingWindow **13/13 PASS**, zero skip. CardViewportLayout adds one
  new case to the frozen matrix; the remaining repetitions do not increase the unique number.
- `.artifacts/map17-family-coverage.json`: **31/31 PASS** with method names, the final XML and SHA,
  covering all 13 families interacting the map.
- Full .NET: `tests/Wotr.Tests/TestResults/map17-domain.trx` and `.artifacts/map17-domain.log`:
  **3723/3723 PASS**, zero skip. The script sets WOTR_TEST_DB; the tests PostgreSQL are not omitted.
- `.artifacts/map17-tls.json`: **PASS**, 13:48:12Z. Two actual script accounts, separated by private
  hands, HTTPS, living WSS, both roles containing only the revision keys, the reconnect of both
  sessions, the same command and the receipt. A separate Python transport test complements Unity
  HTTP/WS/PostgreSQL.

```sh
python3 .artifacts/map17-run-play.py
python3 .artifacts/map17-collect-play.py
bash scripts/test.sh --no-restore --logger 'trx;LogFileName=map17-domain.trx' > .artifacts/map17-domain.log 2>&1
.artifacts/deployment-venv/bin/python scripts/test-map-tls.py --fixture .artifacts/client-integration.json --output .artifacts/map17-tls.json
python3 .artifacts/map17-build-smoke.py build
```

Runner Play was resumed for specific full batch after repair, retaining the original defeat. The final collector uses only the appropriate final XML. The commands of the individual filters and the full run describes the MAP-17.

<a id="buildy-i-smoke-dostępnego-playera"></a>
## Builds and Smokes Available Player

Three builds ended with PASS with exactly the desired Development flag:

| Build | Configuration | Resources and manifesto |
| --- | --- | --- |
| BuildLocalMac | macOS Universal x64/arm64, nonDevelopment | 158 files; `.artifacts/map17/macos`, `BuildLocalMac-source.json` |
| BuildLocalWindows | Windows PE x64, nonDevelopment | 157 files; `.artifacts/map17/windows`, `BuildLocalWindows-source.json` |
| BuildMac | macOS Universal x64/arm64, Development | 247 files; `.artifacts/map17/profile`, `BuildMac-source.json` |

Manifests in `.artifacts/map17` include SHA each input and output file, Development flag, canonical resources, executable format and log build. The source is the frozen `01c8397` database with the described corrections MAP-17; the current HEAD other tasks does not describe this image. Build re-prepares Main and Resources from public-content. The final `.artifacts/map17-public-inputs-final.json` manifest confirms the compliance of 18 files in four copies of content/Resources main and isolated checkout.

`python3 .artifacts/map17-build-smoke.py smoke` is included in the new nonDevelopment macOS. `.artifacts/map17/smoke-final/result.json` and `login.png` confirm the correct 1366×768 screen, default localhost:7443 and empty certifications; previous player profile restored. SHAWith executable file: `83ab699ffb00beafbe419e37c00ef873c6ddb9ad04d0721835d31a3acd08dd3f`. This dragon does not log in the account and does not count Windows runtime.

<a id="wykryte-wady-i-sposób-zamknięcia"></a>
## Deficiencies and method of closure detected

| Primary score | Determination and amendment | Final proof |
| --- | --- | --- |
| Dense: card Eagles cut off at 125% | Adaptive widget responds to actual text with action bar; without reducing font or scroll. | CardViewportLayout and denise 2/2 PASS; all 4 card-fit PNG. |
| Focused 1/2 | The test used a name instead of className header/toolbar. Fixed the selector, without changing runtime. | Final focused 2/2 PASS. |
| Stage fit PASS, but Victory was blocking Combat | Cards.Root added to the existing tab allocator; the movement of the tab retains pinned item card. | Actual Pick of the End of Both Effects, open/close Victory, 4 Layouts; Victory 8/8 and card/hover/pin 13/13. |
| ReturnList 4/5 | Historical fixed number of records replaced by a precise comparison of API ID/kolejnowst/NextCursor. | Final full return party PASS. |
| Cached-01 39/40 | The geometric center of the Combat tray was card source. Test selects the actual exposed Tray; no self-playing was added. | 40/40 PASS, privacy and exactly one step preserved. |
| Cached-04 37/39 | Old Information Path replaced by Help; fixture with two Mouth versions repaired by the same canonical figure version. | 39/39 PASS; the actual GET both roles and ability. |
| Cached-05 39/40 | Close report clicked before laying out layout after HTTP; a seat was added. | 40/40 PASS and both maps17-live-history-report PNG. |
| Cached-06 24/25, cached-07 8/9 | Driver openly manages the attached Hunt/Fellowship/card, keeping the strict Pick and privacy. | 25/25 and 9/9 PASS. |
| Next T36 0/2 | Complete the inspection before your own decision; seat on changing Hunt mode; click the exposed handle before covered Close. Produced auto-open and Observer.Check preserved. | Two full parts 2/2 PASS. |

Final viewing of 12 PNG with 13:38Z: both Eagles effects are exposed in 1366×768/1920×1080 with 100/125%, and eight denses of both roles shows 23 figure in separate polu/garnizonie. The region's temporary preview can cover a part of the manually attached card; it is a clear limit for the application of windows with MAP-15, not a declaration of the simultaneous full visibility of both windows.

Original `*-server.log` is a stdout wrapper build/seed. Detailed Kestrel was rotated in client-server.log; later runs archived separate `*-http-server.log`. We do not declare the preservation of all old detailed HTTP logs. Trace and primary XML failures retain the context of each repair; the final result does not change the historical FAIL to PASS.

<a id="końcowy-profil-development-playera"></a>
## Final Development Player Profile

`python3 .artifacts/map17-build-smoke.py profile`; secure [full report](profiles/map17-macos.json) was saved 2026-10-08T13:54:50Z. macOS 15.6.1, Apple M1 Pro, 16 GiB RAM, Metal, Unity 6000.6.0f1; Development Player, not Editor. Each of 36 workloads has 120 warmup and 360 samples. Four chips: 1366×768 and 1920×1080 at 100/125%.

| Workload | Lowest FPS in 1080p | Highest frame p95 / p99 [ms] | Max feedback [ms] | Rebuilding Band in 480 Steps |
| --- | ---: | ---: | ---: | ---: |
| idle | 1438.53 | 0.842 / 1.092 | — | 0 |
| pan-zoom | 1075.94 | 1.235 / 1.377 | 1.230 | 0 |
| highlight | 1228.54 | 1.000 / 1.101 | 1.065 | 0 |
| pointer | 1440.29 | 0.829 / 1.111 | 0.620 | 0 |
| projection | 297.99 | 6.188 / 7.002 | 7.178 | 0 |
| override-fate | 260.45 | 6.604 / 7.395 | 11.108 | 4320 |
| override-whitehand | 238.48 | 7.239 / 8.856 | 17.808 | 960 |
| stress-pan | 1471.25 | 0.872 / 1.004 | 3.408 | 0 |
| stress-hover | 1586.58 | 0.784 / 0.942 | 0.563 | 0 |

All reconstruction/repaint correspond to exactly the changed bands: Fate 9 on a change, White Hand 2 ; camera movement, hover, highlight and traffic projection have 0 Aim 60 FPS in 1080p and feedback ≤ 100 ms completed. The single longest frame had 23,256 ms when the projection is refreshed 1920/125% ; we do not declare that each frame continued below 16,67 ms. FPS is without limit/vsync , does not mean the number of refreshes of the monitor.

Texture 4096 ²   DXT1,  13 minps: **11   185   272 B**, below 20   Mi B The first load of contours/index keeps 4   612   096 B managed memory, bandwidth 7   188   480 B; the payoad of the coordinates itself has 1   137   312 B. Values include, respectively, objects/indexes and the numbers themselves — you must not add them up as separate costs. UnityAllocated   234   852   183 B, driver respect 290   752   565  B,  924   295 graphics vertics. Global Mesh Memory counter was available, but reported 0 This does not prove zero cost of native buffers UIR **. The report does not attribute global graphics only to the map.

<a id="oględziny-wszystkich-42-png-playera"></a>
## Viewing all 42 PNG Player

The files are in `.artifacts/map17-player`, and the full names are in the JSON report. `.artifacts/map17-visual-review.json` binds each image to SHA and the viewer. The 26 image and source were individually viewed, and independently 16 image overlays/ dense scenes. Automatic measurement does not replace this review.

| Set | Count | Audit result |
| --- | ---: | --- |
| whole — four systems | 4 | Full fit and aspect ratios of the atlas, all edges; settlement southern preserved. |
| maximum-zoom — four systems | 4 | Morannon: White crossing, national bands and black barriers remain separated. |
| Legends — Four Layouts | 4 | All eight names and samples readable, without a cover; maximum of two columns. |
| override-fate — four systems | 4 | Changed Nations and settlement readable, geometry unmoved. |
| override-whitehand — four systems | 4 | The Helm’s Deep control symbol and two bands change independently from settlement. |
| Stress-Pan / stress-hover — four systems | 8 | All 23 figure in separate groups FP/Garrison and SH/Field are located without scroll. |
| morannon / minas-morgul / erebor / moria / high-pass / grey-havens / nurn — before and after | 14 | Highlight remains in the context, does not cover barriers/band, retains passes, retouch Erebor and incision of water. |

Fit does not cut east or south. The 1632×1338 multi-gym corresponds to a proportion of approximately 619×508 px in whole-1920-1.25; all 105 anchors are located in the atlas. The contours occupy [20,20]–[1612,1318], with an equal margin of 20 units. The flat edge of the eastern is part of the source viewBox. Gorgoroth, Khand, Far Harad and East Rhûn and Umbar/Near Harad are inside the full frame; the wide free sides result from the map height matching.

The clear limits of the covers: with 125% fixed Fellowship HUD partially obscures the northeast/Iron Hills; you allow to reveal this passage. In the dense frame of the region, the hiver covers the text of the attached Phial, although its 23 figure and holder/Close card remain clear. We do not consider these frames as a simultaneous full visibility of both windows. The retained limit of the hand-held batch, physical smooth, Windows runtime, Intel and external host is not replaced by this profile.
