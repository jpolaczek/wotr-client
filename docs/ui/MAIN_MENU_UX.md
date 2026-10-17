<a id="menu-wejściowe-i-lobby-1v1--menu-01menu-09"></a>
# Input menu and lobby 1v1 — MENU-01–MENU-09

Source: 2026-10-11 user agreement and "Implement the plan" order. It is a product contract, without new gameplay rules.

<a id="ekrany-i-decyzje"></a>
## Screens and decisions

The first screen (from AUTH-03, D16): Account name, Password, Login, Create account, Forgot password?, Settings. Since UI-255 (2026-10-17, owner's request) the card is one centred column 400 px wide: the title and tagline are centred, labels stand above their fields, the status line follows the fields, Login, Create account and Forgot password? stand one under another (`auth-actions`), then the browser hint, a thin divider and Settings and Replays side by side (`auth-secondary`). Fields and buttons share the column's edges, every button has one size, and labels keep the parchment colour on hover and turn gold on focus. It fits 1366×768 and 1920×1080 at 100/125 % with the failure message shown (`MainMenuLayoutTests`). The game only logs in, with the account name in any case (AUTH-05, 2026-10-17); an email address is refused with a hint. Create account and Forgot password? open in the browser of the `<serwer>/account/register` and `/account/forgot` pages; the signature under the buttons says that after the account is set up it returns to the game and logs in. Enter in the password also log in. Passwords are not saved locally. Invitation to the account is not there; table codes are left. Details: section "Login without registration – AUTH-03". Settings provides scale and reduced motion, and since MENU-12 also window mode and resolution (section "Settings of the screen – MENU-12"). From MENU-20 has no server address.

From MENU-28 host addresses are not in git (`ServerConfiguration`, `ClientEnvironment`):
- The source is environment variables and the untraceable file `wotr-client/.env`. Key pattern is in
  `.env.example`:
  - `WOTR_SERVER_URL` — build server;
  - `WOTR_DOWNLOADS_URL` — original update packages;
  - `WOTR_EDITOR_SERVER_URL` — Play mode server, default `https://localhost:7443`.
- The editor reads them directly.
- Build (`BuildTasks`) saves them for building time to
  `Assets/Wotr/Generated/Resources/Wotr/server-config.json`, next to the build identity, and removes
  them after build. Without `WOTR_SERVER_URL` or `WOTR_DOWNLOADS_URL` build stops.
- The Update Trust (`ReleaseTrust.Pinned`) takes original downloads from the same configuration.
- In CI workflow installers save `.env` from the variable repository.
- `PlayerPrefs` "wotr.server" is not used. Tests indicate your server by `ServerConfiguration.Use`.

The menu shows open games and My games and Host a game, Join by code, Settings and Sign out; screen layout from MENU-11 describes the section “ Menu layout — MENU-11 Public means visibility for logged users of this server, without automatic matchmaking. The list refreshes every 5 s, waiting room what 2 p. Requests are cancelled when changing the screen. From MENU-09 lists contain more rows and scroll without filters All/Your decision/Waiting/Finished and without Previous/Next. Further pages API load after scrolling to the end; refresh includes the loaded part, retains rows and position. Another page without scrolling only loads the list shorter than the view, evaluated after the new rows are placed ( DIST-01 Field Game code and Join by code stand by MENU-11 in column action One list error does not hide the others. Back at 404 returns to the menu. Source of this product adjustment: user request 2026-10-12 .

Host game: name (default {username}'s game, 1–64 characters after trim, no controls), Public/Private (default Public), Free Peoples/Shadow (default Free Peoples). Always base game and two seats. The new menu does not show expansions also in Development. Name/widocznoity/page are constant since creation. The public line opens and takes a free seat with one click.

Private room does not enter the public list. Host public and private room generates/copy random code; at the same time, one code is active, generating is impotent. Joining a member repeated after losing the reply does not take second place. Upon the guest's departure, the code is invalidated; the host can generate a new one. Start/close sizing prevents new attachments. The code never goes to public DTO.

Both players set/remove Ready for the current version. Only host can start a new game. Return before start: the guest slows down the place and resets both acceptances; host closes the room. The disconnect is not an exit. The join/leave/start races are settled under one room block. If the start wins, the exit retains the participants and saves. Each started game allows you to continue by correspondence and return by My games without playing the decision.

<a id="układ-menu--menu-11-2026-10-15"></a>
## Menu layout — MENU-11 (2026-10-15)

Source: User application from 2026-10-15 — “very simple window” with public games and information about who hosts; right-hand button “my games” with a list of games started; menu is to have design, not look like “trast tabs and buttons thrown together on poop”. User decision of that day: public games are open rooms on our server, visible to all accounts (R04), without public matchmaking.

The screen (`MainMenuScreen`) has three layers:

- background**: dim map (`MapCanvas/terrain`), without interaction;
- **header**: ‘WAR OF THE RING’ in Cinzel and ‘Signed in as {username}’;
- **Body** of constant size 980×540, (reduces only on the screen that does not accommodate it):
  - **window** (`menu-window`, `frames/panel` frame, `frames/panel-header` header with page title
    and Fresh button);
  - **column action** (`menu-actions`, 250 entitled, same frame).

The window shows one page at a time and does not change the size between the pages:

| Page | He's opening it. | Content |
| --- | --- | --- |
| Open games (default) | `show-public-games` | Public rooms waiting for the player. The poem (`join-public-<id>`) has a free side emblem (ring or eye), the game name, "Hosted by {host}" and "Join as {side}". The whole row joins with one click. |
| My games | `show-my-games` | Own rooms before start (`open-room-<id>`) and started parties (`resume-match-<id>`); from MENU-13 the line of the party only resumes it. |
| Host a game | `host-game` | HostGameForm form; Back returns to Open games. |
| Test scenarios | `open-test-scenarios` | Only in Test Mode (TEST-03); Back returns to Open games. |

The action column has a fixed order: Open games, My games, Host a game, Join by code (code field and Join), Test scriptios (only when the server offers them), Settings, and at the bottom Sign out. The action current list (art `button-selected`). On the "My games" gold stamp (`my-games-turns`) gives the number of pages you load with your decision; without such lots disappears. The status of the menu (`lobby-status`) has a constant height under the body.

The hidden list does not read the following pages; `RequestMoreIfNeeded` only evaluates the visible list, also after switching the page. The names of the items with MENU-09 remain, so the controller `MainMenuBrowser` and API have not changed. The server already gives a list of public rooms with the host (`PublicRoomSummary.Host`) without private rooms and codes (MENU-03, `PublicRoomTests`, `MenuLifecycleTests`).

Tests: EditMode `MainMenuTests` (default page, row with host and emblem, decision stamp); PlayMode `MainMenuLayoutTests` (1366×768 and 1920×1080 at 100/125%: action are located, My games replaces the list in the same window of the same size, Host form too), `MenuBrowserNetworkTests` (pagination and Refresh of each list on its site). Test helper `ReturnTestFixture.ShowList` switches the page before clicking in the row.

<a id="bez-changes--menu-13-2026-10-15"></a>
## Without ‘Changes’ — MENU-13 (2026-10-15)

The user decision with 2026-10-15: “Let's remove all of this tab and this button”. The starter batch line in My games has one button that resumes it; the “● New changes” tag is left. The `changes-match-*` button, the last change panel (`ActivityPanel`) and the old `LobbyScreen` screen with `LobbyBrowser`, the application of which was not created from MENU-06. The server retains `GET api/matches/{id}/activity`.

<a id="ustawienia-ekranu--menu-12-2026-10-15"></a>
## Screen Settings — MENU-12 (2026-10-15)

Source: User's application with 2026-10-15 — "In the options we want to have a setting for different screens in the most popular resolutions".

The Settings window has two columns: on the left interface (size, reduced motion, window positions, and before logging in the server), on the right screen:

- **Window mode** (`window-mode-*`):
  - Fullscreen: the game takes over the screen — on Windows exclusive full screen, on macOS system
    full screen space;
  - Borderless (default, like the current player setting): a window without frame on the entire
    screen;
  - Windowed: a window with frame in selected resolution.
- **Resolution** (`resolution-*`), always 7 buttons in the same order: Native, 1280×720, 1366×768,
  1600×900, 1920×1080, 2560×1440, 3840×2160 (from UI-174 in full screen height labels modes
  720p...2160p in screen proportions).
  - The preset larger than the screen is greyed out in its place, and the tooltip gives the reason
    and size of the screen.
  - Only the sizes that the screen reports as fullscreen modes are active in Fullscreen Windows
    mode.
  - Tooltip Native gives the screen resolution.

The click immediately uses the selection, without Confirm; the selected button has ✓ and frame `selected`, like the size of the interface. The resolution in use is selected. The saved preset, whose current screen does not support, gives Native (e.g. when the game is moved to a smaller monitor); the panel checks the screen at each opening.

Record: `InterfacePreferences` (version 1 key `wotr.interface.v1` new fields `WindowMode` , `Width` , `Height` The older record without these fields and values outside the list gives Borderless and Native. `DisplayController` uses the setting at startup and after each change ( `Screen.SetResolution` ); in the editor, the game view preserves its own size. Selection logic ( `DisplayOptions` ) is clean and tested without Unity. Screen reads `UnityDisplayScreen` : `Display.main` and `Screen.resolutions` .

The fixed game layout sizes do not change. The UI panel scales to 1366×768 (`WotrPanel`: ScaleWithScreenSize, match 0,5), so resolution changes the focus of the image rather than the size of the elements. Interface size still selects the 100/125%. scale

<a id="pełny-ekran-bez-pasów--ui-174-2026-10-15"></a>
## Full screen without belts — UI-174 (2026-10-15)

Source: User's application with 2026-10-15 — in full screen left and right edge scrolled the map perfectly, but at the top and bottom there were black belts outside the game image. Cause: 2560×1440 preset (16:9) on the 16:10 Unity screen scales with a ratio and adds the belts.

- (Replaced by MENU-17: full screen always in native resolution.) In Fullscreen and Borderless
  preset modes, it retains height and width takes the aspect ratio of the screen
  (`DisplayOptions.Shape`, even width; screen height gives the screen exactly). Buttons have labels
  720p, 768p, 900p, 1080p, 1440p and 2160p, and tooltip gives the full size and "Fills this screen".
  On the screen 2880×1800 preset 1440p to 2304×1440.
- Windowed stays at W×H sizes.
- The preset saved (e.g. 2560×1440 with MENU-12) in full screen mode is read as its height in the
  aspect ratio of the screen; the record does not change, so the return to Windowed gives 2560×1440
  again.
- The exclusive full screen of Windows still requires the size reported by the screen; other presets
  are greyed out with a reason in the tooltip.
- In full screen modes, the cursor is closed in the game window (`CursorLockMode.Confined`; runs on
  Windows), and the physical edges of the screen limit edge scrolling (BOARD_INTERACTION_UX,
  UI-174).
- macOS: Fullscreen is a system full screen space. The system menu bar extends when the cursor
  touches the top edge and the map continues to scroll at that time. Whoever does not want this bar
  choose Borderless.

<a id="pełny-ekran-domyślnie-rozdzielczość-tylko-w-oknie--menu-17-2026-10-15"></a>
### Full screen by default, window resolution only — MENU-17 (2026-10-15)

User request: ‘fullscreen click should disable the possibility of switching the resolution, because it doesn't make sense’ (after asking Borderless as well) and ‘fullscreen is the default mode in which the game starts’.

- Fullscreen and Borderless always use native window screen resolution. Seven resolution buttons
  stay in place but are out; Native has ✓, and tooltip says, "Full screen uses this screen's
  resolution (...) · Choose Windowed to pick a size". Shaping presets to the aspect ratio of screen
  and label 720p … 2160p with UI-174 disappear; labels are always W × H.
- Windowed chooses the size of the MENU-12. The saved selection is waiting for you to return to the
  window.
- The exclusive full screen of Windows works in screen resolution, so the list of screen modes is
  not needed (`IDisplayScreen` has only `Native`).
- The default mode is Fullscreen. `InterfacePreferences` saves the 2 version. The 1 version saves
  the interface size, Reduced motion and resolution, and starts in Fullscreen, because Borderless
  was only the default value. The default player mode in `ProjectSettings` remains FullScreenWindow;
  `DisplayController` switches to Fullscreen as soon as it starts.

<a id="ekran-okna-i-rozszerzanie-widoku--ui-176-2026-10-15"></a>
### Window screen and view extension — UI-176 (2026-10-15)

The user's application after UI-174: on the external screen 16:9 were still black stripes on the sides. The proportions were taken from the main screen of the system (`Display.main`, for example built-in screen MacBooka 3024×1964) rather than from the screen of the game window.

- `UnityDisplayScreen.Native` reads the screen on which the window is
  (`Screen.mainWindowDisplayInfo`); `Display.main` reads only emergency.
- `DisplayController.Refresh` compares the window screen every second. When a full screen game hits
  another screen or changes its resolution, the size is re-used in the proportions of this screen.
  Window (Windows) changes size only when it stops.
- Panel  UI  (`WotrPanel`) scale with Expand mode from 1366×768. In front 16:9 Nothing changes. With
  other ratios, the interface area grows in width or height instead of decreasing: 16:10 ♪ give ♪
  1366×854 pt, screen MacBooka   1366×887  pt, a  21:9   1820×768 "Excess Gets board The map fills
  the whole picture, outside the map is paper and sea (UI-102 Downstairs. dice They're sticking to
  the left edge, card right, and the console turn fill in the middle.

<a id="kontrakty-i-zapis"></a>
## Contracts and provisions

Common DTO in Wotr.Contracts: CreateRoomRequest.Name/Visibility , RoomVisibility Private/Public , RoomStatus Waiting/Started/Closed ; RoomView.Name/Host/IsHost/Status/Visibility ; PublicRoomSummary contains only ID , name, host and free side. PublicRoomListView has Rooms/NextCursor. RoomReadyRequest has ExpectedVersion/Ready. MatchSummary receives Name. No private gameplay data in menu contracts.

GET api/rooms/public: cursor, limit (default 20 , 1–50 ), stable order ID , exclusively public waiting rooms with one free place, without their own. POST api/rooms/ {id} Joining public peace. POST api/rooms/ {id} /invitation: host receives the current code of its free table. PUT api/rooms/ {id} "Ready: Your own reversible acceptance. POST api/rooms/ {id} /leave: idempotent return; host closes, guest leaves. register/login , rooms/join After the code, confirm and start remain. Identity is due solely to the session. All room mutations are transactional.

The metadata list is indexed in PostgreSQL; the name on start passes to the party header outside the engine. Older rooms remain Private; the host is the only participant or FP in full room. The entries started do not change konfiguracji/stanu. Compatibility of old clients: the absent name in CreateRoomRequest selects the current private table; the new form always provides the name and creates a room with the rules of the new menu. The endpoints of configurations remain for testing and the clients of expansions; the new rooms reject expansions also outside Production.

<a id="odbiór"></a>
## Acceptance

Registration, unique name, session/logout/expiry ; public list and pagination; private JSON no leakage; race for second place; incorrect/unvalidated codes; Ready/unready/host-only start; restarts of one take-off; host/guest leave and race with start; restart the server and return to the same pending/receipt. Unity: click/keyboard/fokus , loss of network, 1366×768 and 1920×1080 with 100/125%. Checks: tasks.py validate, scripts/test.sh ed PostgreSQL , EditMode/PlayMode MacOS Universal and Windows x64 nonDevelopment and isolated macOS smoke. Windows runtime, Intel, manual acceptance and the target host remain uncounted.

<a id="koordynacja"></a>
## Coordination

Separate worktree/ga bed codex/main-menu ; custom menu styles. We do not modify figures , dice , map or work UI-97 Integration uses the current MatchSession The register only merges records MENU ; does not overwrite Claude's story. No push, publication and notifications outside the application.

<a id="wynik-menu-02--2026-10-11"></a>
## Result MENU-02 — 2026-10-11

`WOTR_TEST_DB='Host=127.0.0.1;Port=55448;Username=wotr;Database=wotr_menu_test' bash scripts/test.sh --filter 'FullyQualifiedName~OpenRegistrationTests|FullyQualifiedName~AccountInputTests|FullyQualifiedName~LobbyTests' --logger 'trx;LogFileName=menu02.trx' --results-directory .artifacts/results`: **30/30 PASS, 0 skip**. `.artifacts/results/menu02.trx` report. Open registration, normal permissions, active session, logout, standardization, name race and previous invitations and configuration received. Tests work on separate 55448 cluster; `scripts/test-menu.sh` captures this selection.

<a id="wynik-menu-03--2026-10-11"></a>
## Result MENU-03 — 2026-10-11

`./scripts/test-menu.sh --no-restore --filter 'FullyQualifiedName~PublicRoomTests|FullyQualifiedName~MenuMigrationTests|FullyQualifiedName~LobbyTests' --logger 'trx;LogFileName=menu03-final.trx' --results-directory .artifacts/results` : **12/12 PASS , 0 skip**. Migration 006 preserves old private rooms, places/codes/Versions , designates a historical host and perpetuates the name in the party header. The public list has a separate indexed projection and pagination ID ; does not serialize RoomView Parallel joint allows one guest, retry preserves the version/place. The first attempt did not compile fixture with ambiguous new(); the second 10/12 revealed no enum converter in fixture HTTP Fixed open type and use BoardCatalog .Json; runtime of the protocol did not require a change.

<a id="wynik-menu-04--2026-10-11"></a>
## Result MENU-04 — 2026-10-11

`./scripts/test-menu.sh --no-restore --filter 'FullyQualifiedName~MenuLifecycleTests|FullyQualifiedName~PublicRoomTests|FullyQualifiedName~LobbyTests' --logger 'trx;LogFileName=menu04.trx' --results-directory .artifacts/results`: **14/14 PASS, 0 skip**. One transaction and room lock protect zaproszenia/Ready/leave/start. New rooms accept only the base also in Development; host-only start, unready, code invalidation, idmpotent leave and rebooted readings received. Eight races host/guest leave with start have retained all participants of each started batch. Old tables without name retain common configuration/start.

<a id="wynik-menu-05--2026-10-11"></a>
## Result MENU-05 — 2026-10-11

The new GatewayScreen and the open Login/Register; Settings stores the server address only before logging in. The old AuthScreen remains for tool testing compatibility. `bash scripts/unity.sh -runTests -testPlatform EditMode -testFilter GatewayScreenTests -testResults .artifacts/menu05-editor.xml -logFile .artifacts/menu05-editor.log`: 3/3 PASS. `WOTR_TEST_DB=…55448… WOTR_CLIENT_TEST_PORT=5098 bash scripts/test-client.sh -testFilter Wotr.PlayTests.AuthRegistrationTests`: 1/1 PASS, 0 skip; actual registration, logout and login. Runner allows the isolated port HTTP.

<a id="wynik-menu-06--2026-10-11"></a>
## Result MENU-06 — 2026-10-11

MainMenuScreen/MainMenuBrowser and HostGameForm use API public/rooms/matches. Two lists, separate cursors, stable lines, generation cancellation and polling 5 s; preserved history/filters existing records. Host by default public/Free Peoples/database ; privacy without expansions . `bash scripts/unity.sh -runTests -testPlatform EditMode -testFilter MainMenuTests -testResults .artifacts/menu06-editor.xml -logFile .artifacts/menu06-editor.log` : 2/2 PASS . `WOTR_TEST_DB=…55448… WOTR_CLIENT_TEST_PORT=5098 bash scripts/test-client.sh -testFilter Wotr.PlayTests.MainMenuNetworkTests` : 1/1 PASS , 0 skip, two accounts create and attach by clicking on the same room named. `.artifacts/menu06-play.xml` .

<a id="wynik-menu-07--2026-10-11"></a>
## Result MENU-07 — 2026-10-11

WaitingRoomScreen/Controller: Ready/unready , host-only start, host-only private invitation and copy of the code, polling 2 s, clear pregame leave and return after the host closes. MatchSession and safe exit board remain the write limit; the expired session turns to login. Settings disconnects server editing after logging in. `bash scripts/unity.sh -runTests -testPlatform EditMode -testFilter 'WaitingRoomTests;CorrespondenceClientTests' -testResults .artifacts/menu07-editor-final.xml -logFile .artifacts/menu07-editor-final.log` : 6/6 PASS New MainMenuNetworkTests : 2/2 PASS (Report `.artifacts/menu07-play.xml` ), both sides on the same board and after return the same revision; private kody/leave/cancel. `WOTR_TEST_DB=…55448… WOTR_CLIENT_TEST_PORT=5098 bash scripts/test-client.sh -testFilter 'Wotr.PlayTests.ReturnRecoveryTests;Wotr.PlayTests.BoardMenuNetworkTests;Wotr.PlayTests.InterfaceSettingsTests'` : 11/11 PASS , 0 skip, report `.artifacts/menu07-regression.xml` ; real process reboot and receipt without re-cost. First regression 10/11: the focus test indicated hidden hunt-tab; it was replaced by a visible open-board-menu without changing the blinding assertion.

<a id="menu-08--historyczny-odbiór-częściowy-i-blokada-2026-10-11"></a>
## MENU-08 — historical acceptance partial and lock, 2026-10-11

. NET 3740/3740 , final EditMode 627/627 and focused PlayMode menu/recovery 16/16 PASS macOS Universal and Windows x64 nonDevelopment and isolated macOS smoke PASS ( 836 come in Runtime/Resources ; existing profile 14 four layouts of five screens checked. Default ClientFlow shows the waiting room without an editor expansions also for older rooms; saved configuration and older API they are preserved.

Full PlayMode 439/553, 114 FAIL: 36 login limits, 78 other failures. Baseline e2e8146 confirmed two identical camera errors ReturnDraft/ReturnList; 8 MapViewport tests passed separately. Other 76 has not been assigned to the baseline. MENU-08 **blocked** to evaluate packages and repair the actual regression, without extending the menu with parallel implementation of board. Details, commands, reports, list of failures and unlocking steps: MENU-08.

After integration with completed UI-97 ( `0a77d64` ): EditMode 630/630 menu server 17/17 focused PlayMode in separate packages 10/10 + 7/7 PASS Menu tests use temporary recovery records. Current builds of both platforms and isolated dragons PASS ( 873 compatible runtime files, 16 unaltered profile files). Implementation UI-97 and 198 previous stories preserved. Global gate PlayMode remains blocked; details in MENU-08 And his index.

<a id="odbiór-w-izolacji--2026-10-12"></a>
## acceptance in isolation — 2026-10-12

After the extension of the order to all 114 failures were completed acceptance MENU-08. The works and reports are only found in the worktree `/private/tmp/wotr-menu-tests`, on the branch `codex/menu-tests`. Claude's approved design for `4f2df59` has been merged locally; unapproved main checkout files remain intact. All 207 other task records and their stories have been preserved.

New acceptance includes rolled-up corner panels UI-98 and notifications UI-99 Tests open full forms with real hover or focus. Esc suppresses the restored focus and PointerEnter The resulting change in geometry under the motionless cursor itself; fresh motion, click and new focus continue to open the panel. Both sides and four systems have passed gate `ui99-gate-08` ( 14/14 ) and `ui99-gate-09` ( 7/7 ), without omission. Full frozen PlayMode `1620402` gave 553/559 ; as amended 161 all cases 47 related classes ( 160/161 ), then full class UI10 ( 3/3 ) after correction of the waiting test for the final test GET Audit XML and the manifestos confirmed 559/559 and all 114 primary failures, no skip and no lack, and it preserves its original failures, nine manifest defeats. 429 and source differences; this is not one full green run on the final commission.

Runtime maintains the source panel while drags, and Settings has a clear contrast to the server address field. NET 3740/3740 ( 903 identical inputs), Editor 631/631 , four systems and a review 20 PNG PASS macOS Universal, Windows x64 nonDevelopment and isolated macOS smoke PASS : 891 identical inputs Runtime/Resources , 16 files of existing profile unchanged. Current index: `isolated_verification` In [menu-validation.json](profiles/menu-validation.json) ; command and transfer in MENU-08 Windows runtime, Intel, target HTTPS and manual acceptance two players remain postponed. The next step is integration of the branch; it was not merged into the main checkout and not executed push/publikacji.

<a id="odbiór-korekty-menu-09--2026-10-12"></a>
## acceptance Correction MENU-09 — 2026-10-12

The filters and Previous/Next have been removed; the lists scroll through the lines, charge the cursors in the background and retain the already loaded data/scroll by Refresh. Game code/Join by code remain under the lists; public and private host generates/copy code. Back also works with 404, and the error of one list does not hide the others.

Active localhost:8443 was on ui83 without API menu. After backup/restore/migration only local backend has been updated and the previous data has been preserved. Old tables remain private; the new public table is seen in the second account. Sources/clients remain in isolated worktree; the new UI requires the `.artifacts/menu09/macos/Wotr.app` package from this worktree to be launched.

18/18 server, 632/632 Editor, 21/21 related PlayMode, 20 associated PNG/four chip, macOS Universal/Windows x64 nonDevelopment and isolated dragons: PASS, zero skip. 893 runtime input and 16 profile files have been preserved. The entire engine and global PlayMode have not been repeated. [Report](profiles/menu09-validation.json) and transmission save commands, the first outdated test and its correction, the server copy and acceptance limits.

<a id="weryfikacja-integracji--dist-01-2026-10-13"></a>
## Verification of integration — DIST-01, 2026-10-13

The MENU-08/09 menu was merged with UI-100–111 on the `claude/dist-01` branch. Menu files remained identical to `codex/menu-tests` `4924cdd`. Full PlayMode revealed one drawback: `RequestMoreIfNeeded` read the scrolling range before placing new lines, so the full first page immediately loaded the second one. `155a7e0` checks the list on the next tick. `MenuBrowserNetworkTests` requires the lack of the other side of the public tables before changing and falls on the old code.

On `155a7e0` have passed all menu cases in full PlayMode 567/567: `MainMenuNetworkTests` ( 3 ), `MenuBrowserNetworkTests` ( 2 ), `MainMenuLayoutTests` , `SavedGamesLayoutTests` , `ReturnListTests` ( 2 ), `ReturnRecoveryTests` ( 2 ), `ReturnDraftTests` and `ExpansionLobbyNetworkTests` Design criteria and commands: DIST-01 .

<a id="scenariusze-testowe--test-03"></a>
## Test scenarios — TEST-03

The menu has a button in the action column (from MENU-11; previously in the header)**Test scriptios** (`open-test-scenarios`) when the server returns the `GET /api/test-scenarios` directory (test mode enabled). With 404 or error, the button remains hidden and the menu is running so far.

- The button replaces the game lists with a panel of scenarios in the menu window, just like Host a
  game. **Back** returns to Open games.
- The scripts are grouped like in the server directory (e.g. "Free Peoples · Army die"); the line
  shows the title and description.
- The line click (`test-scenario-<id>`) immediately starts the script, without confirmation, and
  opens board on the side that starts.
- Exit or Menu → Games ends the test session and returns to the menu; the test batch does not enter
  the My games list.

<a id="wyjście-z-gry--menu-14-2026-10-15"></a>
## Out of the game — MENU-14 (2026-10-15)

Source: User application from 2026-10-15 — “on the main menu and in the dropdown menu of the map, let's add the option to exit the game (with confirmation)”.

- Main menu: ‘Quit game’ (`quit-game`) is the last position of the action column, under ‘Sign out’,
  in the same silent style.
- The board menu: ‘Quit game’ (`board-menu-quit`) is the last constant position after ‘Games’. The
  write and ‘Retry’ message is under it, so its place is not changed.
- Click opens the question `ConfirmationDialog.Quit` (to MENU-22 `QuitConfirmation`) over the shaded
  background:
  - „Quit War of the Ring?” with one line of explanation: In the menu main „Your
    games stay on the server.”, in lot „This game stays on the server.
    Resume it from My games.”;
  - the buttons ‘Quit’ and ‘Cancel’ in fixed places; focus starts on ‘Cancel’, so Enter does not
    come out himself;
  - Esc, “Cancel” and click next to the window close the question without exit. On board Esc answers
    the question first and the menu is opened.
- The “Quit” in the batch goes the same way as “Games”: the menu shows “Saving recovery data...”,
  and after the board is written, it closes and the game ends. The write error leaves board with the
  message and “Retry” repeats the output. In the “Quit” test scenario, it ends the script on the
  server first.
- Output: `ApplicationQuit.Exit` (`Application.Quit`; in Unity Editor end play mode). `ClientFlow`
  takes action output, which tests replace the counter.

<a id="tło-mapy-przed-planszą--menu-15-2026-10-15"></a>
## Map background before board — MENU-15 (2026-10-15)

Source: User's application from 2026-10-15 — “Let's add the same background from the map that is on Selection/creation game to the main menu”.

The map background (`MapCanvas/terrain`, dimmed) has only a menu with a list of games, Host and Test scripts. From MENU-15 the same background has all the screens before board: login screen (`GatewayScreen`), menu (`MainMenuScreen`) and waiting room (`WaitingRoomScreen`). `FrontendTheme.Apply` inserts as the first child of the screen element `frontend-backdrop` (`PickingMode.Ignore`, absolute position, `scale-and-crop`), so the background does not take clicks and changes the layout. card login and the waiting panel are in the middle of the size.

<a id="autorzy-oryginalnej-gry--menu-16-2026-10-15"></a>
## Original game authors — MENU-16 (2026-10-15)

Source: User application from 2026-10-15 — “on the main menu and game selection submenu, etc., add disclaimer that “original game credited to “”.

All screens before board (Login, each menu page, waiting room) have one silent line at the bottom edge `frontend-credits` : "The original game credited to Roberto Di Meglio, Marco Maggi and Francesco Nepitello · Published by Ares Games". Inserts it `FrontendTheme.Apply` over the map background. The line does not take the indicator, has 12 px and standing 2 px above the edge. The menu page has at the bottom 20 px space, so the line does not go to the window, column action Or a state announcement. board the game does not have this line. The right to name, rules and graphics of the game describes `wotr-server:docs/sources/materials/map-data-MIT-LICENSE.txt` .

<a id="poczekalnia-bez-mrugania--menu-19-2026-10-15"></a>
## Wait without blinking — MENU-19 (2026-10-15)

User application: Back, Ready and Generate invitation blinked every few seconds. Refreshing every 2 went by clicking the player: it disabled buttons on request and cleaned the message.

- Background refresh (`WaitingRoomController.RefreshQuietly`) updates the view without busy status
  and does not move the player message (e.g. "Code copyid").
- The background refresh error shows the connection message (401 ends the session). The next
  successful refresh only cleans this message.
- The answer that came during action player is omitted.
- action player and the Refresh button show the busy so far.

<a id="usunięcie-partii--menu-22-2026-10-15"></a>
## Batch Removal — MENU-22 (2026-10-15)

> From MENU-32 (user notification from 2026-10-16) the board menu does not have a "Delete game" and
> "Games" is called "Exit match"; see section MENU-32 below. The `GameDeletedNotice` window is
> removed for the batch on the server.

Source: User application from 2026-10-15 — “We also need options to exit, remove, each player should have this option”. Server: `DELETE /api/matches/{id}` (MENU-21).

- The board Menu: "Delete game" (`board-menu-delete`) between "Games" and "Quit game", in a fixed
  place. It is greyed out at the time of writing or removing.
- Click opens the `ConfirmationDialog.DeleteGame` question in the same window as Quit (MENU-14):
  - title „Delete this game?”;
  - text „The game ends for both players and is removed from the server.
    This cannot be undone.”;
  - the buttons ‘Delete’ (`delete-confirm`) and ‘Cancel’ (`delete-cancel`).

Focus starts at “Cancel”. Esc, “Cancel” and click next to the window do not delete anything. On board Esc answers the question first.
- "Delete": menu shows "Deleting game...", client sends `DELETE`, closes the batch without writing
  draft and returns to the main menu. Network error leaves board with message and "Retry" which
  repeats deletion. 404 answers also ends the deletion, because the batch is gone (`MatchDeletion`).
- The second player with the board open: the server closes the update slot and the batch redownload
  gives 404. `MatchConnection` then ends connecting the `MatchDeleted` state because the re-download
  will not do anything. board:
  - the status of the ‘Game deleted’;
  - window `GameDeletedNotice`: „Game deleted”, text „The other player
    deleted this game. It is no longer on the server.” and one button
    „Main menu” (`game-deleted-menu`), that goes back to menu Main.

Esc and click next to it do not close this window. The player who selected the “Delete” himself does not get this window.
- Test scenario: ‘Delete game’ ends the scenario like ‘Games’.
- Local data of the deleted batch play remain on the disk, but are no longer read because the batch
  is not on the list “My games”.
- Common Question Window: `ConfirmationDialog` with name prefix (`quit-…`, `delete-…`) and
  `confirm-window`, `confirm-note`, `confirm-actions` classes.


<a id="demo-przeciw-cpu--demo-04-2026-10-15"></a>
## Demo v CPU — DEMO-04 (2026-10-15)

- The waiting room has a action third fixed button “Play demo vs CPU” (`room-demo`), visible to both
  room members. It is greyed out when the room is either not waiting or is in screen operation.
- `WaitingRoomController.Demo`: `POST api/rooms/{id}/demo`, then `openMatch` returned batch as at
  startup. Error 409 `MatchAlreadyStarted` gives the message “The game in this room has started.
  Open it from your games.”
- The room stays and waits. The demo is listed as “My games” as “Demo · ... vs CPU” with the
  opponent “CPU”. The new demo replaces the previous ones, and the tooltip button warns.
- Details: [DEMO_UX](DEMO_UX.md).

<a id="nowa-poczekalnia--menu-25menu-26-2026-10-15"></a>
## New waiting room — MENU-25/MENU-26 (2026-10-15)

The wait room has a main menu layout: a window with two player places, an invitation and chat and a column action. Host removes the guest with the “Remove” button. The “Back” button has only this word and the refresh is in the window header. Removed player returns to the message menu that stays in status (`MainMenuBrowser.Notice`). Details: [WAITING_ROOM_UX](WAITING_ROOM_UX.md).

<a id="exit-match-bez-delete-game--menu-32-2026-10-16"></a>
## ‘Exit match’ without ‘Delete game’ — MENU-32 (2026-10-16)

The user request from 2026-10-16: “we withdraw the delete game button from the game menu from the map. games are to be changed to exit match. exiting the game does not remove it, exiting the empty host of the game that has not started, removes this room”.

- The board Menu: Resume, Victory, Settings, Exit match, Quit game. “Exit match” (`back-to-games`)
  saves draft and returns to the main menu; the batch stays on the server and on the list “My
  games”. There is no longer a button “Delete game”, its question windows or `MatchDeletion` on the
  client.
- Party removed on server ( `DELETE /api/matches/{id}` , MENU-21 ) still goes to every open board
  "Game deleted" window.
- Waiting room: “Back” host in room without guest and without the party started immediately removes
  the room on the server (row and chat), so you can no longer see it in “My games”
  (`wotr-server:docs/server/T08-LOBBY.md`).

<a id="host-a-game-w-układzie-menu--menu-30-2026-10-16"></a>
## "Host a game" in menu layout — MENU-30 (2026-10-16)

User application from 2026-10-16: "Adjust host view a game to the rest of the design, now it is a common mess with butons, let everything take place". Page in menu window (title only in window header), from the top:

- the row ‘Game name’ with the name field (`room-name`), the label in the constant column 150
  entitled;
- "Choose your side": two card pages `SideChoice` in the frame `frames/entry`, like the seats in the
  waiting room, with the emblem (ring, eye), name and one sentence about how the page wins
  (`host-fp`, `host-shadow`); the selected card shines and has a golden outline;
- ‘Visibility’: segment buttons ‘Public’ and ‘Private’ (`host-public`, `host-private`) with one
  sentence of the explanation below;
- the line of rules „Base game · Two players · Play at your own pace”;
- Footer: ‘Back’ (`cancel-host`) to the left, message of the middle error, ‘Create game’
  (`submit-host`) to the right.

The "Private" switch and loose row of buttons are gone. card `SideChoice` also serves the demo page from the menu (MENU-31).

<a id="demo-vs-cpu-z-menu-głównego--menu-31-2026-10-16"></a>
## "Demo vs CPU" from the main menu — MENU-31 (2026-10-16)

The user request from 2026-10-16: "Do the best from the main menu option demo mode, which immediately creates a private game and starts the game vs CPU, all that is supposed to happen is to ask for a choice of faction".

- The action column has “Demo vs CPU” (`menu-demo`) under “Host a game”. “Sign out” and “Quit game”
  stand together in the last row of the column, so the new button fits into four layouts.
- "Demo vs CPU" opens a page with this title in the menu window. It has one sentence about demo,
  card pages `SideChoice` (`demo-fp`, `demo-shadow`), note that the new demo replaces the previous,
  and "Back".
- The card click is the whole selection (UI-142). Client:
  1. create a private room called "Demo" on this side;
  2. launches a demo (`POST /api/rooms/{id}/demo`, DEMO-04);
  3. release the room (`leave`; empty room server immediately deletes, MENU-32);
  4. opens the party as each started.

The error appears in the status of the menu, and the room is released anyway.

<a id="logowanie-bez-rejestracji--auth-03-2026-10-16"></a>
## Logging without registration — AUTH-03 (2026-10-16)

Decision [D16](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/DECISIONS.md): an account is set up on the game page, the client only opens it. `GatewayScreen`:
- one `Account name` (`auth-username`) field and password; the client checks that the entry is a
  valid account name (an email address gets "sign in with your account name, not the email address"),
  and the server finds the account by name only (AUTH-05, 2026-10-17);
- `Login` (`auth-submit`) is the only operation sent to the server;
- `Create account` (`auth-create`) and `Forgot password?` (`auth-forgot`) open `AccountPage(server,
  register|forgot)` by `Application.OpenURL`; the address of the server is from
  `ServerConfiguration`, so the Editor opens the local server pages and build — the server from the
  installer. Buttons also work when logging in;
- Failed login: „Account name or password not accepted. Try again,
  or reset your password in the browser.” and fokus in password.

`GameApi.Register(name, email, password)` is only left for tests and tools (addresses in reserved domain `example.test`). Old `AuthScreen` has been removed. PlayMode tests log in with seeder accounts (`AccountA`, `AccountB`) or start accounts with API; `scripts/test-client.sh` raises registration limit (`Wotr__Email__PermitsPerAddress`).

<a id="play-i-observe--obs-04-2026-10-16"></a>
## Play and Watch — OBS-04 (2026-10-16)

User order with 2026-10-16: "implement replay and watcherow" (project: wotr-server `docs/server/OBSERVERS-REPLAYS.md`).

- The open game line shows the name and details:
  - In the Waiting: „Hosted by … · Waiting · Seats n/2 · Observers n/16”;
  - during the lot: „Hosted by … · In progress · Observers n/16”.

Completed parties are not on the list.
- The end of the line is two fixed buttons: “Play” (`join-public-<id>`) and “Observe”
  (`observe-public-<id>`). It is not available, with tooltip: the game is started, both seats are
  occupied or full of audiences. The poem itself is no longer a button.
- The “Join by code” has the “Observe” button next to “Join” (`observe-invitation`). The same code
  allows the viewer also in with the occupied places and after the start of the batch. No space
  shows the status:
  - „All observer places in this game are taken.”;
  - „This game has ended. It no longer takes observers.”.
- “My games” also shows the rooms that the player watches: “Watching” and after the start of
  “Watching · In progress”.

<a id="lokalna-biblioteka-replayów--replay-03-2026-10-16"></a>
## Local play library — REPLAY-03 (2026-10-16)

Format file: wotr-server `docs/server/REPLAY-FORMAT.md` (`.wotrreplay`
v1).

- The files are in `<persistentDataPath>/Replays`, together for the entire installation, without an
  account and without a server. The file name is identifier replaya, e.g. `<matchId>.wotrreplay`.
- Record and import (`ReplayLibrary.Add`):
  - the file goes first to `.incoming-*.tmp` in this folder;
  - `ReplayArchive.Open(…, verify: true)` checks it in its entirety: permanent entries names,
    limits, numbers from the manifest, completeness, version, and size and SHA-256 of each entry;
  - Then the file is moved to the location.

The broken record does not leave an entry, and the old `.incoming-*` disappears when the library opens.
- The same replay is once: new export of the same batch (another export date) is "AlreadySaved".
  Another replay with the same identifier does not overwrite the saved and gives an error.
- The list reads only manifests, the latest first parties. The damaged file is a separate entry with
  a reason and does not block the rest.
- Error messages:
  - „The replay file is damaged.”;
  - „A part of the replay file is damaged.” (bad sum);
  - „This replay uses a newer replay format. Update the game to open it.”;
  - „This replay is incomplete.”;
  - „The replay file holds an unexpected part.” (e.g.. path `../`);
  - „The replay's parts do not match its manifest.”.
- Export copies the file through a temporary file next to the target. Delete only deletes a copy in
  the library.
- Selecting a file behind the `IReplayFilePicker` adapter:
  - macOS: AppleScript `choose file` / `choose file name` by `/usr/bin/osascript`;
  - Windows: `GetOpenFileNameW` / `GetSaveFileNameW` with `comdlg32`;
  - without API editor.

<a id="replays-w-menu-i-przed-logowaniem--replay-04-2026-10-16"></a>
## Replays in menu and before logging in — REPLAY-04 (2026-10-16)

- The action column menu has “Replays” (`show-replays`) under “My games”. The window shows the
  library page (`replay-library`), the latest pages on top. The poem contains:
  - the winning emblem and the name of the game;
  - details ‘Free Peoples: A · Shadow: B · 16 Oct 2026 · Shadow win · Military victim · 7 turns’;
  - fixed ‘Play’, ‘Export’ and ‘Delete’ (`replay-play|export|delete-<klucz>`).

The damaged file is a row “Damaged play · ...” with a reason; it only works “Delete”.
- U bottom standing „Import” (`replay-import`) and status line
  (`replay-status`): „Replay imported.”, „This replay is already in your
  library.”, „Replay exported to …”, „Replay deleted from this computer.”
  Or the reason for refusal. An empty library says, how to add replay.
- The login screen has “Replays” (`auth-replays`): the same page in the menu frame
  (`replay-library-screen`) from “Back” to login. No account, server or update required.
- The “Play” opens the player (UX_SPEC, REPLAY-05). “Exit match” in the board menu returns to the
  library from which the player came (menu or screen before logging in), with the list scrolling
  saved.
- Delete only removes a local copy and does not send anything to the server.

## Join by code in one readable row — UI-257 (2026-10-18)

The owner's screenshot: in the 250 px action column the code field showed "Cod" and Observe wrapped to "Observ/e".
Join and Observe are now as wide as their one-line words in 15 px type (`white-space: nowrap`), and the code field takes
the rest of the row (at least 84 px), so its "Code" placeholder shows whole. The row keeps its height, so the column
still fits 1366×768 at 125 % (`MainMenuLayoutTests` measures both labels on one line).
