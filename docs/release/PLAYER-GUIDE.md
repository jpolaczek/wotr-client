<a id="war-of-the-ring-online--rozpoczęcie-gry"></a>
# War of the Ring Online — Start the game

This private version supports the game 1v1: Second Edition, Lords of Middle-earth, Kings of Middle-earth, Fate of Erebor and variants available in the table settings. The interface is in English. The game connects to the `https://wotr.example.test` server. Its address is built into the installer (from MENU-20), so nothing needs to be entered. The release of `0.2.0-rc1` requires a server with the 2 protocol; the previous server from the `0.1.0-rc1` package does not support new steps of action.

<a id="instalacja"></a>
## Installation

The game is installed by the installer for your system. You will download it from the `https://wotr.example.test` game page (version, size and SHA-256 at the button) or you will get it from the administrator together with the `SHA256SUMS` sum. Installers do not have Microsoft or Apple signature yet, so the system warns on the first installation. Install only the file from the administrator and do not disable system protection globally.

**Windows (x64):** Windows is still waiting for a real PC to try; until then, the administrator only transmits macOS installer. Testers run `WotrOnline-<version>-win-Setup.exe`. The viewer can ask if to save the file: select the behavior. If you see the Windows system to protect this computer (SmartScreen), click “More information” and then “Run anyway”. Installer does not require administrator permission: installs the game for the current user in `%LocalAppData%\WotrOnline`, creates the shortcut “War of the Ring Online” on the desktop and the Start menu, then starts the game. Uninstall it in Settings → Applications. When Windows has Smart Application Control enabled, it will block the unsigned installer and games you cannot install; report this to the administrator.

**macOS (Apple Silicon and Intel):** Open `WotrOnline-<version>-osx-Setup.pkg`. macOS will answer that it cannot verify that the file contains malware: click “Ready”, open System Settings → Privacy and security, click “Open” and confirm password with the installer. Select **In the installer, please install only for me**: the game will go to `Aplikacje` in your home directory and later updates will not require password. After installation, the game starts itself.

**Linux (x86_64):** Download `WotrOnline-<version>-linux.AppImage` into a folder of your own, for example `~/Applications`: updates replace this file, so it must stay where you can write. Make it executable — in the file manager Properties → Permissions → “Allow executing file as program”, or `chmod +x WotrOnline-<version>-linux.AppImage` in a terminal — and start it. Nothing else is installed; to remove the game, delete the file. The AppImage needs FUSE (`fusermount3` or `fusermount`), which most desktop distributions have; if it does not start and mentions FUSE, install the `fuse3` package of your distribution. Settings and local records stay in `~/.config/unity3d/WoTR Private/War of the Ring Online`.

**Steam Deck (experimental):** The Deck runs the Linux version; it has not been checked on a device yet. Switch to Desktop Mode (Steam button → Power → Switch to Desktop), download the AppImage with the browser into `~/Applications`, then right-click → Properties → Permissions → “Is executable” and start it. To play from Game Mode, add it in Steam (still in Desktop Mode): Games → Add a Non-Steam Game to My Library → Browse → the AppImage. Steam + X opens the on-screen keyboard for the name, password and table code; a mouse and keyboard work best for now. Report what does not work to the administrator.

The new version is installed with the same installer, on the existing one. Settings and local game records are outside the application directory, so they will also stay when you uninstall. If you have a `0.2.0-rc1` version unpacked from the ZIP archive, install the new version with the installer and delete the old folder; the data stays.

<a id="gdy-muzyka-z-innych-aplikacji-przerywa-na-macu"></a>
## When music from other apps interrupts on Mac

If YouTube or music starts to shimmer while playing on full screen, turn off the system **Game mode** for WoTR. On macOS Sonoma 14 and Sequoia 15 move the pointer to the top of the screen, click the icon falls in the menu bar and select **Turn off Game mode**. On macOS Tahoe 26 open the game overlay by Command–Esc, then its settings and **Game mode**.

macOS remembers the shutdown for this game also after it is restarted. You can still play full screen and enjoy sound effects. [Instructions Apple](https://support.apple.com/en-lamr/105118). New packages containing the AUDIO-09 amendment disable this optimization in the application metadata; older installation requires the above setting.

<a id="aktualizacje"></a>
## Updates

The installed game itself checks new releases: when you start, what 15 minutes and when the server rejects its version. The version and state of the update can be seen in `Settings` → `Game version`; `Check now` checks immediately.

- **The optional version:** The top of the screen bar gives the number and size (description in the
  hints). `Download update` takes it in the background and `Later` hides the bar for the next start.
  Before `Download update` the game does not download or byte the package. `Cancel` interrupts the
  download; the `Download update` resumes it from the interrupt.
- **The version required from a specific moment:** The bar shows the time and time that has been
  ("Update required at 20:00 (in 1 h 12 min)"). The game takes the package itself, within minutes,
  and you continue playing until now.
- **The `Update required` window stops new moves; the batch is visible under it and remains stored
  on the server. When the package is ready, `Restart and update` saves the state and starts the game
  in a new version. `Decline and quit` saves the state and closes the game without installation;
  when the next start, the selection returns and the downloaded package waits.
- **`Settings` → `Repair installation` downloads the current version again and installs it after
  `Restart and repair`. Settings and saves remain. When the current version is withdrawn, the game
  proposes a newer one.

The installation starts only after you click. Before it the game ends local records, just like `Quit game`; if the recording fails, the game opens with the message and you can try again. After reboot, log in normally; the unfinished action will be tested without sending it a second time. The game runs in one copy: the second runable copy shows “War of the Ring is already running”. The copy not installed with the installer (e.g. unpacked with ZIP) does not update itself, just heads to the game page (`Game page`).

<a id="konto-i-stół"></a>
## Account and table

1. The start screen connects to the server from the installer; `Settings` has no address field.
2. You can assume an account on the game page: `Create account` opens it in your browser. Enter a
   name (3–32 characters: letters without Polish characters, digits or `._-`), email address and
   password (12–128 characters), and then confirm the address with a link from the message. Return
   to the game and sign in (`Login`) name or email address. The forgotten password resets `Forgot
   password?`, also in the browser; the reset link only comes to the confirmed address.
3. Open `Tables`. Create `New table · Free Peoples` or `New table · Shadow`. `Copy table invitation`
   copy the code for the second player; it typed it in `Table invitation` and click `Join`.
4. Set expansions and variants. Both click `Accept configuration`, then host `Start game`. The
   configuration changes require new permissions.

The game does not set up accounts and does not accept invitations to accounts (from AUTH-03). The table code is only used to join the party. Do not pass your password on to anyone; the administrator can only assign an email address to the older account.

<a id="w-czasie-gry"></a>
## While playing

Click on the specific die; with Army/Muster and Will of the West select the symbol. Highlighted objects show the actions available. Visit the region to view its units and characters. The army's own click selects all figure that can move; click on figure in the region panel shows it. The highlighted target click saves the step. The army does not drag on the map. On two marches, the first one is already saved: execute the second one or click on `Finish` at dice. Clicking on another source only replaces the unsaved selection.

die Muster opens the panel `Reinforcements` : Each of your nations has a line in it with a policy track, reserves and traffic icons on the track. The reason for the lock shows the line tooltip. Click the token figure , and the view will go to the highlighted sediments; click settlement recruits, and two clicks of a token recruits when only one fits settlement Closed panel you will open with a button `Reinforcements` in tray or in a click settlement , a `✓` In his header, he ends recruitment early. card and recruiting capabilities use the same panel: planned figure you can see as semi-transparent figures on the map, `×` returns them to the reserve, and `✓` recruiting the whole plan. card they have a visual after the raid and pinned ; two-click or drop starts playing. Fellowship has its own panel; ordinary movement increases progress without indicating a secret route. The path of declaration, disclosure or character travel ends with a two-click of the last region. In battle select the entire set of losses or reroll and use one `✓` , also for a voluntary empty set. Resources and legality check the server. Hover does not spend anything; details can also be seen in the opponent's decision.

`Settings` provides the size of the 100%/125% interface, `Reduced motion` and the version of the game with updates (`Game version`). In battle optional omission cards applies only to this battle. It does not choose for the player loss, retreat, route or cost of action.

<a id="powrót-i-połączenie"></a>
## Return and Connection

The game saves every approved decision. `Games` leads to `Your games`; `Your decision` shows the parts waiting for you and `Resume` opens the current state. After the application closes, log in to the same server and account. You do not need to play late animations. Unsent draft stays on the device; the status of the approved game returns to another.

The `◎ Your decision` button above the map opens the panel of the current decision and, if it concerns a particular region, leads to it. The notification itself does not move the camera or close the attached card.

`Menu → Delete game` removes the game for both players after confirmation; this cannot be undone. If the opponent removes the game you have opened, you will see `Game deleted` and `Main menu` button return to the main menu.

If the response to action is missing, the client will recover the result; the available `Check result` checks the same action. Do not start a new batch in the replacement of an old record. `Save unavailable` requires contact with the administrator; you can click the status to re-re-download the status. Notifications work only in the application.

If you cannot connect, check the server availability at the administrator. Do not skip the certificate error. Enter the installer name version and the visible message when you report; do not share the password or invitation code. Players should use the same release packages.
