<a id="klient-unity--budowanie-i-testy"></a>
# Unity Client — Building and Testing

Moved from `docs/DEVELOPMENT.md` monorepo at the repository division (DIST-03). The instructions for the backend, the database and implementation are in [DEVELOPMENT server](https://github.com/jpolaczek/wotr-server/blob/f52b87442a9e2bc83bc14b53a60e9b987d3b24b9/docs/DEVELOPMENT.md). Before running Unity `scripts/unity.sh` downloads pinned with dependencies (`python3 scripts/bootstrap_deps.py`, description in [README](../README.md)).

For a clean contributor bootstrap, use `python3 scripts/bootstrap_deps.py fetch
--anonymous --test` in a fresh checkout. The flag prevents the bootstrap from
consulting token environment variables or `gh auth`; remove `.deps` first when
repeating the availability check. The command downloads the exact Contracts,
public-content, server and seeder archives in `wotr-deps.lock.json`, verifies their
sizes and SHA-256 values, and refuses a missing release or different bytes.

In the server settings, each build, also without Development Build, allows you to save local HTTP: `http://127.0.0.1:5088`, `http://localhost:5088` or `http://[::1]:5088`. The server on this port must actually support HTTP; the port change itself in the address HTTPS does not change the protocol. Other servers require HTTPS. The settings record checks the address format, not the server availability; the account and the parties are part of the database of the selected base.

## Unity

Open `unity/Wotr.Client` in Unity Hub. Mac Build Support and Windows Build Support (Mono) must be installed. In this environment, the missing Windows module has been added from Unity package with total `modules.json` editor.

<a id="build-z-otwartego-edytora"></a>
### Build from open editor

Repo features the Main scene on the build list, profiles **WoTR macOS** and **WoTR Windows** and settings **Fullscreen Window** with native resolution. Open the entire checkout, not the same copied Unity directory: Contracts package and public content are linked to `src/` and `content/` in the same repo.

1. Wait for the import and compilation to be completed in Unity 6000.6.0f1.
2. **WoTR → Build → Prepare Project** synchronizes public content, sets fullscreen and stage, plays
   missing profiles and the UI panel. It does not replace the currently open or unsaved scene.
3. **WoTR → Build → Build macOS** creates Universal client (Intel + Apple Silicon), **Build And Run
   macOS**additionally launches it, and**Build Windows**creates x64 client.**Open Builds
   Folder** opens the results.

Outputs without Development Build: `.artifacts/builds/local/macos/Wotr.app` and `.artifacts/builds/local/windows/Wotr.exe` (Windows also requires other files from this directory). These are local builds of the current branch and work changes, without the commit, Python or the variable `WOTR_BUILD_COMMIT`. To build master, switch repo to master before building and wait for reimport. The menu does not perform checkout/pull and does not overwrite `.artifacts/releases/`.

You can also use **File → Build Profiles**, select the WoTR profile, activate it via **Switch Profile** and press **Build** or **Build And Run**, indicating your own output directory. Both paths synchronize the same open list of public files and apply desktop settings before build. No scene, content file or platform module interrupts the message. The server starts separately; the client build does not implement it.

Unsaved scene in the editor can call the standard Unity save question, especially when changing the platform. Save your own work before switching the profile. The build goes to the versioned Main, and the project preparation does not replace the open scene. The client starts by default in fullscreen; the previous window preferences saved by Unity may require one-time start with `-screen-fullscreen 1`.

<a id="zwykły-build-z-bieżącego-master"></a>
### Normal build from current master

Save the work and close the editor of this project before starting CLI. In the main directory repo, perform the following (choose the necessary platform):

```sh
git switch master
bash scripts/unity.sh -quit -executeMethod Wotr.Editor.BuildTasks.BuildLocalMac -logFile .artifacts/unity-local-macos.log
bash scripts/unity.sh -quit -executeMethod Wotr.Editor.BuildTasks.BuildLocalWindows -logFile .artifacts/unity-local-windows.log
```

Results: `.artifacts/builds/local/macos/Wotr.app` and the entire `.artifacts/builds/local/windows/` directory with `Wotr.exe`. Start macOS: `open .artifacts/builds/local/macos/Wotr.app`. Build uses current checkout files and sets fullscreen; sandbox scene does not replace Main. For a repeatable commit build check out `git status --short` earlier. This does not take any changes from remote. Save local sources: BUILD-02.

<a id="build-i-testy-z-terminala"></a>
### Build and tests from terminal

```sh
bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/unity-editmode.xml -logFile .artifacts/unity-editmode.log
bash scripts/unity.sh -quit -executeMethod Wotr.Editor.BuildTasks.BuildMac -logFile .artifacts/unity-mac.log
bash scripts/unity.sh -quit -executeMethod Wotr.Editor.BuildTasks.BuildWindows -logFile .artifacts/unity-windows.log
```

`WOTR_UNITY_EDITOR` allows you to indicate a different location of the same editor. Results go to `.artifacts/builds/mac/Wotr.app` and `.artifacts/builds/windows/Wotr.exe`. Unity commands for the same project perform sequentially; editor blocks the simultaneous opening of the directory.

### Complete PlayMode inventory

Do not use an old result XML as the list of current tests. Export Unity's current
test tree and bind it to the exact client sources and dependency lock:

```sh
python3 scripts/discover-tests.py --output .artifacts/playmode-discovery.xml
```

Full PlayMode also needs the profile fixture produced by
`Wotr.Tests.DesktopProfileFixtureTests` in EditMode. Then run the discovered cases
in isolated batches:

```sh
WOTR_TEST_DB='Host=127.0.0.1;Port=55432;Database=wotr_test;Username=wotr' \
  python3 scripts/test-client-batches.py \
  --inventory .artifacts/playmode-discovery.xml \
  --output .artifacts/playmode-run --batching mixed --batch-size 8 --seed 1
```

Each batch creates a uniquely named disposable database and owns one server
process, port, fixture, accounts, Unity log and XML result. The runner records the
first outcome of every fully qualified test name and fails on missing, extra,
skipped or filtered cases, a non-test process failure, absent cleanup evidence, or
any source change. It never retries a failure into a passing aggregate. Use a new
output directory and seed for an independent second full order.

The above `BuildMac`/`BuildWindows` is a development for testing/profileing. Menu counterparts without Development: `Wotr.Editor.BuildTasks.BuildLocalMac` and `Wotr.Editor.BuildTasks.BuildLocalWindows`. Before using CLI close the editor of this project by saving your work. The pre-released packages with the requirements of a clean commit are described by [instructions for issue](../release/README.md).

`Assets/Wotr/Resources/WotrPanel.asset` is part of the project. The client creates its instance, and build thus contains the required text resources ICU and the UI Toolkit theme. Do not replace this resource with `CreateInstance<PanelSettings>()` itself in runtime: the editor can then work properly, but the built application does not contain text data. `BuildTasks.PrepareProject` plays missing resource before the build.


## BOT-02 player contract pin

Contracts 0.22.0 and public content 1.0.24 pin ordinary player rule facts v8.
The additional Lords/Council facts describe public resource uses, special-die
choice order and Vilya completion. Unrevealed companion versions remain private
to Free Peoples. Existing declaration/Hunt contexts are retained. These fields
carry no saved decision token, executable frame, hidden hand or entropy; they
require no new UI controls and do not grant command permission.
Source/visibility contract: [Player rule facts](https://github.com/jpolaczek/wotr-server/blob/f52b87442a9e2bc83bc14b53a60e9b987d3b24b9/docs/server/PLAYER-RULE-FACTS.md).

Unity EditMode compatibility verification passed **55 cases**, no skips, covering
contracts, opponent playback, Hunt reactions/responses/reports and aftermath.
Two added Lords cases verify enum-keyed dice counts, choice order, Council and
Vilya facts, optional older responses and absence of command permission.
Existing unrelated Unity deprecation/analyzer warnings remain. Evidence:
`.artifacts/bot02-completion/lords/client-final.xml`. The pinned BOT-13 PlayMode
backend was not replaced or retested; server-owned HTTP/restart checks cover the
new continuation metadata.


## Kings player-facts checkpoint — 2026-11-05

Contracts 0.23.0 / boundary v9 and public content 1.0.25 are pinned to server
`514244625095ef4a9224311616ecab447c0dc328`. Joint unknown tile faces, per-zone
hidden counts and public Kings resource flags survive the Unity JSON round trip;
covered sovereign tiles retain no identity/value. Reading these facts grants no
command permission and adds no controls or automatic actions.

The selected 57 Unity 6000.6.0f1 EditMode cases passed, no skips, including two new
Kings contract cases plus existing contracts/opponent playback/Hunt checks.
`python3 scripts/check_docs.py` passed for 72 Markdown files. Evidence:
`.artifacts/bot02-completion/kings/client-final.xml`. Existing unrelated Unity
warnings remain. The BOT-13 PlayMode backend pin and acceptance are unchanged.


## Active corruption facts checkpoint — 2026-11-05

Contracts 0.24.0 / boundary v10 and public content 1.0.26 pin server
`432d71bebd67cf41de8fca2b1100707b8d198d7b`. Shadow's earlier numeric draws remain
separate from the currently selectable tile during Sequential Corruption.
Free Peoples receives no private draw identities, even with Visible Corruption.
Older omitted fields remain empty; these facts grant no command permission.

All 59 selected Unity 6000.6.0f1 EditMode cases passed without skips, including two
new private-draw contract cases and the existing opponent/Hunt checks. The 72-file
pinned documentation check passed. Evidence:
`.artifacts/bot02-completion/corruption/client-final.xml`. Existing unrelated Unity
warnings remain; the BOT-13 PlayMode backend and acceptance were not changed.


## BOT-02 companion-journey contract pin — 2026-11-06

Contracts 0.34.0 and public content 1.0.34 pin ActionBoundary v18, introduced
by server `ad154db022c887bc45a0c3eae3e07b3a2cc4900d`. The optional fact identifies
only a paid Free Peoples Gwaihir or Swifter journey, its Separation/Guide stage
and printed travel parameters. The current server source pin is
`96853b10fc42377d4df3ff12711a2712acf9b1f0`; rules 0.15.14 extends the same
fact to the outside-companion journey on Book of Mazarbul, Fear! Fire! Foes!,
and the Fate Book. These are optional shared player facts only; they add no
client control or hidden-resolution data. The client package versions and
controls are unchanged.

Unity 6000.6.0f1 EditMode passed **1,092/1,092**, with no skips. The dependency
hash and pinned documentation checks passed. Evidence:
`.artifacts/bot02-completion/journey/client-edit-rerun.xml`.


## The CPU line on production main — 2026-11-11

The owner asked for the CPU to be played and tested on the version live in production. Local branch
`claude/bot-on-prod` is production `main` (0.10.0-rc.4, c0526696) with the CPU client line squashed on
top: the menu's **Play vs CPU** (a private waiting room with the CPU invited) stands beside Find game
and Leaderboards and replaces the menu's demo page; seats and saved games show the CPU and avatars
together. It pins contracts 0.76.0, public content 1.0.76 and the test backend of wotr-server
`claude/bot-on-prod` f52b874 (production main with the CPU host line), packaged into a local channel:
fetch with `WOTR_DEPS_SOURCE=file:<channel>`. Nothing here is pushed or released.

## BOT-02 last event roots contract pin — 2026-11-08

Contracts 0.73.0 and public content 1.0.73 pin ActionBoundary v57, introduced by server source
`fe76baf317d4de20c09bf9996f6439afbe0cfe69`. v57 adds `DirectEventChoice.RingRecoveryRolls` (The
Three Rings' public recovery dice) and marks The Wind from the West's choice from Shadow's
inspected hand, an event's completion window while a political consequence waits and the
discard after Gandalf the Grey's draw there. No runtime UI or controls changed. Unity
6000.6.0f1 EditMode passed **1,092/1,092**, no skips; evidence:
`.artifacts/bot02-completion/tail57-client.xml`.

## BOT-02 Palantir, Smeagol and Fate's Book contract pin — 2026-11-08

Contracts 0.72.0 and public content 1.0.72 pin ActionBoundary v56, introduced by server source
`dcad4877148481eb4727fcb52555f538b6cdc9f5`; the client skips v55, whose facts v56 keeps. v55
adds `EventPalantirDraw` (the Palantir of Orthanc's deck choice after a Shadow event paid with
an Event die) and marks the Guide chosen after Smeagol leaves a declared Fellowship and a
journey paid with an Army/Muster die; v56 marks Fate's Book recruitment and fixes v55's Smeagol
check, which threw for a Fellowship on the Mordor Track. No runtime UI or controls changed.
Unity 6000.6.0f1 EditMode
passed **1,092/1,092**, no skips; evidence: `.artifacts/bot02-completion/tail56-client.xml`.

## BOT-02 landing, battle and Lords Hunt tail contract pin — 2026-11-08

Contracts 0.70.0 and public content 1.0.70 pin ActionBoundary v54, introduced by server
source `f41422a77306ee15309c7895f11d1cda87b8b5f4`; the client skips v52 and v53, whose facts
v54 keeps. v52 marks the battle a Corsairs of Umbar landing begins (`BattleContextView.LandingEvent`);
v53 marks a Charge's losses after Durin's Bane at the same initiative and Relentless Assault in
a later round; v54 marks a movement Hunt after Flocks of Crebain and a declaration's Fire and
Shadow Hunt. No runtime UI or controls changed. Unity 6000.6.0f1 EditMode
passed **1,092/1,092**, no skips; evidence: `.artifacts/bot02-completion/tail54-client.xml`.

## BOT-02 battle and Kings tail contract pin — 2026-11-08

Contracts 0.67.0 and public content 1.0.67 pin ActionBoundary v51, introduced by server
source `0a2ebc87c003488e6f0db8cea48ce4ddd86bef0c`; the client skips v50, whose facts v51
keeps. v50 marked Relentless Assault after a Sudden Strike and Brand's replacement offer; v51
marks the Palantir of Anarion after a Ruler-die event (`AnarionDraw.CardId` and `CardCost`)
and Wisdom's politics branch. No runtime UI or controls changed. Unity 6000.6.0f1 EditMode
passed **1,092/1,092**, no skips; evidence: `.artifacts/bot02-completion/tail51-client.xml`.

## BOT-02 event tail contract pin — 2026-11-08

Contracts 0.65.0 and public content 1.0.65 pin ActionBoundary v49, introduced by server
source `1a4290977452829ca5d34c873ec16d57521a88e8`. `DirectEventChoice` also marks The Wind from
the West, the Nazgul strike, Fate's Orcs, Rangers of the North and an event's Guide with a
pending political consequence; `RegionId` also names the Rangers' and the Orcs' regions. No
runtime UI or controls changed. Unity 6000.6.0f1 EditMode passed **1,092/1,092**, no skips;
evidence: `.artifacts/bot02-completion/tail49-client.xml`.

## BOT-02 Kings chieftain replacement contract pin — 2026-11-08

Contracts 0.64.0 and public content 1.0.64 pin ActionBoundary v48, introduced by server
source `824b0f2de892992b969493054e8838925a6d59ec`. The `Battle` fact also marks a Kings
chieftain's offer to replace Shadow's card effects before the reveal. No runtime UI or
controls changed. Unity 6000.6.0f1 EditMode passed **1,092/1,092**, no skips; evidence:
`.artifacts/bot02-completion/chieftain-client.xml`.

## BOT-02 Kings closers and event follow-ons contract pin — 2026-11-08

Contracts 0.63.0 and public content 1.0.63 pin ActionBoundary v47, introduced by server
source `491178be2cfe2bad624ea9ced04bace7d24cebf5`; the client skips v45 and v46, whose facts
v47 keeps. v45 added `AnarionDraw` and `CorruptionRemoval` and marked the Mighty Attack
choice; v46 marked Wisdom's nested inspection, Madness and Horror (with
`DirectEventChoice.RegionId`) and the Guide chosen after an event's separation; v47 marks
Return to Valinor's repeated strikes (with `DirectEventChoice.TargetIds`) and the Orderly
Withdrawal choice, and accepts a lone inspected sovereign. No runtime UI or controls changed. Unity 6000.6.0f1 EditMode passed
**1,092/1,092**, no skips; evidence:
`.artifacts/bot02-completion/valinor-client.xml`.

## BOT-02 Sorcerer offer contract pin — 2026-11-08

Contracts 0.60.0 and public content 1.0.60 pin ActionBoundary v44, introduced by server
source `e28fd36235253a967cbd1be55424fe6b17426928`. The `Battle` fact also marks the
Witch-king's Sorcerer offer after round one. No runtime UI or controls changed. Unity
6000.6.0f1 EditMode passed **1,092/1,092**, no skips; evidence:
`.artifacts/bot02-completion/sorcerer-client.xml`.

## BOT-02 Kings inspection contract pin — 2026-11-08

Contracts 0.59.0 and public content 1.0.59 pin ActionBoundary v43, introduced by server
source `adeed7ca3617276daef8a2ecfd3a2d6936eedce7`. `DirectEventChoice` also marks every
step of the Kings inspection. No runtime UI or controls changed. Unity 6000.6.0f1 EditMode
passed **1,092/1,092**, no skips; evidence: `.artifacts/bot02-completion/inspection-client.xml`.

## BOT-02 Stormcrow figure loss contract pin — 2026-11-08

Contracts 0.58.0 and public content 1.0.58 pin ActionBoundary v42, introduced by server
source `5e0117eca950b1296bbe76d27398dbf508a510ed`. `DirectEventChoice` also marks
Stormcrow's last step, which the event's opponent chooses. No runtime UI or controls
changed. Unity 6000.6.0f1 EditMode passed **1,092/1,092**, no skips; evidence:
`.artifacts/bot02-completion/stormcrow-client.xml`.

## BOT-02 Heroic Death choice contract pin — 2026-11-08

Contracts 0.57.0 and public content 1.0.57 pin ActionBoundary v41, introduced by server
source `b2dbca12170567f3602d55cbb70bc4d4fb314f5a`. The `Battle` fact also marks the Heroic
Death choice, and special-attack losses in Kings games without a combat replacement. No
runtime UI or controls changed. Unity 6000.6.0f1 EditMode passed **1,092/1,092**, no skips;
evidence: `.artifacts/bot02-completion/heroic-client.xml`.

## BOT-02 Lidless Eye and event excess contract pin — 2026-11-07

Contracts 0.56.0 and public content 1.0.56 pin ActionBoundary v40, introduced by server
source `ed3c6a213109a714093cb957da72fe9344f2c8a4`. `DirectEventChoice` also covers The
Lidless Eye's dice choice and the stack-limit decision an event leaves before it completes.
No runtime UI or controls changed. Unity 6000.6.0f1 EditMode passed **1,092/1,092**, no
skips; evidence: `.artifacts/bot02-completion/lidless-client.xml`.

## BOT-02 every Onslaught card contract pin — 2026-11-07

Contracts 0.55.0 and public content 1.0.55 pin ActionBoundary v39, introduced by server
source `6af39ddf4318aeafbc97ee8d997dc11f74441d1f`. The `Battle` fact now marks the
Onslaught voluntary losses of Misty Mountains and Orcs Multiplying as well as Uruk-hai. No
runtime UI or controls changed. Unity 6000.6.0f1 EditMode passed **1,092/1,092**, no skips;
evidence: `.artifacts/bot02-completion/onslaught-client.xml`.

## BOT-02 announced battle cancellations contract pin — 2026-11-07

Contracts 0.54.0 and public content 1.0.54 pin ActionBoundary v38, introduced by server
source `373d342621dba59f2805203afa6ba0acd5c5f09f`. The `Battle` fact also carries each
force's cancelled hits for the Combat round, including those a Heroic Death sacrifice
cancelled. No runtime UI or controls changed. Unity 6000.6.0f1 EditMode passed
**1,092/1,092**, no skips; evidence: `.artifacts/bot02-completion/cancelled-client.xml`.

## BOT-02 Balrog event and Keeper Eye contract pin — 2026-11-07

Contracts 0.53.0 and public content 1.0.53 pin ActionBoundary v37, introduced by server
source `75a4d5370876acaabd51a8da0807dc551f327cf5`. `DirectEventChoice` also covers the
Balrog event and the Rage of the Dunlendings move, and an ordinary corruption attempt
paid with a Keeper Eye now has its boundary. No runtime UI or controls changed. Unity
6000.6.0f1 EditMode passed **1,092/1,092**, no skips; evidence:
`.artifacts/bot02-completion/balrog-client.xml`.

## BOT-02 Kings national cards and army events contract pin — 2026-11-07

Contracts 0.52.0 and public content 1.0.52 pin ActionBoundary v36, introduced by server
source `d0059e4be89ae7063318d62dc436411b0bfa989a`. `DirectEventChoice` also covers the
branch and recruitment of the four Kings national cards and the one army movement or
attack of an army event, with the same two fields. No runtime UI or controls changed.
Unity 6000.6.0f1 EditMode passed **1,092/1,092**, no skips; evidence:
`.artifacts/bot02-completion/national-client.xml`.

## BOT-02 battle cards contract pin — 2026-11-06

Contracts 0.51.0 and public content 1.0.51 pin ActionBoundary v35, introduced by server
source `65effa61a41e6771634f733febcb61a762612b05`. It covers event reaction windows, Nazgul
travel, event losses and separations, Aid, the Scouts choice, every Relentless Assault
card and Great Host. No runtime UI or controls changed. Unity 6000.6.0f1 EditMode passed
**1,092/1,092**, no skips; evidence: `.artifacts/bot02-completion/battlecards-client.xml`.

## BOT-02 direct events by step type contract pin — 2026-11-06

Contracts 0.48.0 and public content 1.0.48 pin ActionBoundary v32, introduced by
server source `f08705b26a5ee9d15a13da1404b9ba2b2c172eec`. The optional
`DirectEventChoice` fact now covers 43 events, with the same two fields: the public
card ID and payment face. No runtime UI or controls changed. Unity 6000.6.0f1
EditMode passed **1,092/1,092**, no skips; evidence:
`.artifacts/bot02-completion/types-client.xml`.

## BOT-02 Ever at Your Service contract pin — 2026-11-06

Contracts 0.46.0 and public content 1.0.46 pin ActionBoundary v30, introduced by
server source `92fe609524d7e3924deea1ea3dfbeac5c56058c6`. The optional
`DirectEventChoice` fact now also covers both Ever at Your Service choices, with
the same two fields: the public card ID and payment face. No runtime UI or
controls changed. Unity 6000.6.0f1 EditMode passed **1,092/1,092**, no skips;
evidence: `.artifacts/bot02-completion/service-client.xml`. Passing `-quit` with
`-runTests` exits with code 0 before any test runs and writes no results file, so
leave it out and check that the file exists.

## BOT-02 direct event choice contract pin — 2026-11-06

Contracts 0.45.0 and public content 1.0.45 pin ActionBoundary v29, introduced
by server source `1f4956cbf0834fbc4ce00f400bbddeaf7a014098`. The optional
`DirectEventChoice` fact is currently emitted only for the first direct paid
choice on `Kindred of Glorfindel`. It carries the public card ID and payment
face, not a saved frame, cursor, or private selection; the runtime reconstructs
the recruit choice and continues through the subsequent card draw.

`python3 scripts/bootstrap_deps.py fetch` verified both package hashes, and
`python3 scripts/check_docs.py` passed for all 72 Markdown files with the server
source links pinned to `1f4956c`. The contract round-trip test now expects the
current boundary version v29; no runtime UI or controls changed. Unity 6000.6.0f1
EditMode passed **1,092/1,092**, no skips; evidence:
`.artifacts/bot02-completion/direct-event-client-rerun.xml`.
