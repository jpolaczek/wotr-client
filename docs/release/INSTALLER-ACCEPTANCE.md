<a id="odbiór-instalatorów-dist-06-na-prawdziwym-sprzęcie"></a>
# acceptance installers DIST-06 on real equipment

CI (workflow **Installers**) checks installation, update, damaged package and uninstall on the runners, but the game starts there without graphics, and installers do not pass through the browser, Gatekeeper or SmartScreen. These two attempts are done by a man: DS14 publisher on Mac, DS13 tester on Windows. The package from Installers run on `main` is tested. It has a version with `release/version.json`, but a different commit than the former edition, so it serves only for acceptance and does not go to players.

<a id="paczka-do-próby"></a>
## Test package

GitHub → Actions → Installers → Last Green Pass → Artifacts → `installers-<version>`. Download it with a browser: macOS will label the file quarantined as the player file.

- `macos-universal/WotrOnline-<version>-osx-Setup.pkg` — Mac;
- `windows-x64/WotrOnline-<version>-win-Setup.exe` — for the tester Windows;
- `SHA256SUMS` — totals to compare (`shasum -a 256 <file>` on Mac, `Get-FileHash <file>` on
  PowerShell).

## DS14 — Mac (Apple Silicon)

1. If `~/Applications/War of the Ring Online.app` exists, remove it. Game data and old `Wotr.app`
   with ZIP can stay.
2. Open `.pkg` double-click and proceed as in [Guide player](PLAYER-GUIDE.md#instalacja) : Ready →
   System Settings → Privacy and protection → Open yet → installation **only for me**. Note the
   exact messages.
3. The game should run itself and show the main menu. This is the first graphical build start from
   Linux (CI starts it without graphics). Close the game.
4. In Terminal:

   ```sh
   app="$HOME/Applications/War of the Ring Online.app"
   codesign --verify --deep --strict "$app" && echo signature OK
   xattr -l "$app" | grep quarantine || echo without quarantine
   lipo -archs "$app/Contents/MacOS/War of the Ring Online"
   grep -m5 Velopack "$HOME/Library/Logs/WoTR Private/War of the Ring Online/Player.log"
   ```

5. Intel under Rosetta (if CI has written "Rosetta is not installed"): in the `arch -x86_64
   "$app/Contents/MacOS/War of the Ring Online"` Terminal; the game is to open as usual. Close it.
6. Optionally log into the test server and open the table as usual.

Send: messages from the 2 step, whether the game started itself and showed the menu, the 4 step score, the 5 step score. Installations do not need to be deleted.

## DS13 — Windows (tester)

Give the tester the `Setup.exe` file (e.g. through a cloud disk) and its amount SHA-256. The tester needs a regular Windows account 10 or 11 x64, without administrator privileges and no development tools.

1. Download the file with your browser and run it. Follow the [Guide
   player](PLAYER-GUIDE.md#instalacja): keep the file, with SmartScreen “More information” → “Start
   anyway”. Note or photograph warnings.
2. After installing the game, it should start itself and show the main menu, in a window without a
   black screen. Check the shortcut “War of the Ring Online” on the desktop and in the Start menu.
   Close the game and restart it.
3. Uninstall: Settings → Applications → War of the Ring Online → Uninstall. Shortcuts and folder
   `%LocalAppData%\WotrOnline` are to disappear and folder `%USERPROFILE%\AppData\LocalLow\WoTR
   Private\War of the Ring Online` (game settings) is to remain.
4. Install the same file again: the game is to start without a warning SmartScreen or with it again
   (note which ones).

Tester sends: Windows version (Settings → System → Information) whether Smart App Control, accurate warning from the 1 step, 2–4 step result, with a screenshot error and `%LocalAppData%\velopack\velopack_WotrOnline.log` file.
