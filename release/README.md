<a id="instalatory-dla-graczy"></a>
# Players' installers

The local build from the current checkout is enough for the menu **WoTR → Build** in Unity; [editor instruction](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/DEVELOPMENT.md) describes profiles, fullscreen and `.artifacts/builds/local/` outputs. It does not require a clean commit and is not a process of creating a re-versioned package below.

`version.json` defines the version of the release. From DIST-06 players are hit by Velopack installers 1.2.161 ( `Setup.exe` for Windows x64, `.pkg` for macOS Universal, an `.AppImage` for Linux x64 and the Steam Deck — LNX-02, D18) with full update package `.nupkg` , not ZIP installers do not have a system signature (D13, DIST-12 ); the integrity of the updates will be ensured by the signature Ed25519 manifest ( DIST-05/07 Instructions for players: [PLAYER-GUIDE](../docs/release/PLAYER-GUIDE.md) .

<a id="instalatory-z-github-actions"></a>
## Installers from GitHub Actions

Main path is workflow **Installers** (`.github/workflows/installers.yml`): GitHub → Actions → Installers → Run workflow on selected branch. DIST-10 will call it for each release tag. Steps:

1. GameCI builds on Linux `BuildTasks.BuildReleaseMac` (Universal), `BuildReleaseWindows` and
   `BuildReleaseLinux` with a commit passed as `-wotrCommit`.
2. Linux packs Windows (`vpk [win] pack`, its own `Wotr.exe`) and Linux (`vpk [linux] pack`, an
   AppImage; needs `squashfs-tools`), macOS packs macOS (`vpk [osx] pack` with ad-hoc signature);
   each also packs the same player as the next version for the test smokes.
3. Smoke on Windows, macOS and Linux runners: installation, self-checking game, update to the next
   pack, player data unchanged, uninstall or removal. Evidence: `evidence-windows`, `evidence-macos`
   and `evidence-linux` artifacts.
4. `package_velopack.py sums` and `verify`; result: `installers-<version>` artifact (the three
   platform folders with `SHA256SUMS`).

Repository secrets: `UNITY_LICENSE` (content of `Unity_lic.ulf` Personal license), `UNITY_EMAIL`, `UNITY_PASSWORD` (Unity account). The pinned `jpolaczek/wotr-server` releases are public; the workflows read them with the job token. Sets them by the owner repo command `gh secret set <NAME> -R jpolaczek/wotr-client`, who asks for value.

Variable repository ( MENU-28 ): `WOTR_SERVER_URL` (game server) and `WOTR_DOWNLOADS_URL` (origin update package, from `/` at the end). Host addresses are not in git. Workflow saves them before building to `.env` and without them ends with a mistake. Sets the owner repo: `gh variable set WOTR_SERVER_URL -R jpolaczek/wotr-client` and `gh variable set WOTR_DOWNLOADS_URL -R jpolaczek/wotr-client` Locally the same keys are in `wotr-client/.env` (model: `.env.example` Repo is private: the course consumes approx. 25 minutes of Linux, 3 micro-series (multiplier ×) 10 ) and 2 Windows minutes (× 2 ).

<a id="wydanie-z-tagu--dist-10"></a>
## Tag release — DIST-10

1. In `release/version.json`, set the version, add `release/notes/<version>.md`
   ([rules](notes/README.md)) and confirm both files to `main`.
2. `git tag v<version> && git push origin v<version>`.
3. Workflow**Release** ( `.github/workflows/release.yml` ) check the secrets, the tag matches
   `version.json` and notes ( `release.py check-tag` ), launches the testing of release tools,
   EditMode ( `editmode.yml` : pinned CLI GameCI v0. 1.72 , without code cover) and `installers.yml`
   and at the end `package_velopack.py verify` and `assets` Bad tag or no notes ends in several
   seconds, before build. The whole continues 12–14 - Just a minute. EditMode you can also run
   separately: Actions → EditMode → Run workflow.
4. The result is **draft**GitHub Release tag, invisible to players, with flat files: the full
   packages and installers of the three platforms, `build-info-windows-x64.json`,
   `build-info-macos-universal.json`, `build-info-linux-x64.json`, `RELEASE-NOTES.md` and
   `SHA256SUMS` (sum of each of these files). The version with suffix
   (`-rc.1`) is marked as pre-release. Restarting replaces draft with this tag; the published
   release of workflow does not change.

draft signs and publishes the publisher on Mac with one checkout command wotr-server: `python3 scripts/release_client.py v<version>` (first `--dry-run` ; runbook in `wotr-server:infra/README.md` , DIST-11 According to D14 and D18 ( `wotr-server:docs/DECISIONS.md` ) all three platforms are published and the game page offers all three, Windows offered as a regular platform since the owner's check of 2026-10-18 ( DIST-13 The run uses approx. 30 Linux minutes (including EditMode ), 3 minutes macOS (× 10 ) and 1 Windows minute (× 2 ), which means c. 62 minutes from the account limit.

<a id="wynik"></a>
## Result

`.artifacts/releases/<version>/installers/`:

```text
windows-x64/WotrOnline-<version>-full.nupkg full update package
windows-x64/WotrOnline-<version>-win-Setup.exe user installer
windows-x64/build-info.json build identity (DS10)
macos-universal/WotrOnline-<version>-osx-full.nupkg
macos-universal/WotrOnline-<version>-osx-Setup.pkg
macos-universal/build-info.json
linux-x64/WotrOnline-<version>-linux-full.nupkg
linux-x64/WotrOnline-<version>-linux.AppImage  the AppImage players download and start
linux-x64/build-info.json
SHA256SUMS sum of all files above
```

`packId` is always `WotrOnline`: Velopack recognizes the installation after it, so its change would cut players off from the upgrade. App name and shortcuts: “War of the Ring Online”. `build-info.json` creates `ClientBuildIdentity` and contains exactly those bytes that the player has embedded as a resource `wotr-build-info`. `Wotr.ReleaseTool sign --build-info ... --package
<full package> --installer <installer>` z `wotr-server` sign the issue
each platform (DIST-11 will do it with one command).

`package_velopack.py verify` refuses, when:

- `build-info.json` is not a build release of this version and platform, or the platforms have
  different commit, protocol or public content, or a platform is missing;
- the sources of the client differ from the build commit or the public content has changed;
- the package contains test kits, fixtures, `*_DoNotShip`, `build-info.json`, `.env` or `secrets`;
- `.nuspec` does not indicate `WotrOnline` of this version and system;
- Windows: `Wotr.exe` is not a launcher or `Wotr.exe`/`UnityPlayer.dll` are not x64;
- macOS: any Mach-O file does not have both architectures (x86_64 and arm64) or no `UpdateMac`
  helper;
- Linux: the package does not carry `lib/app/WotrOnline.AppImage` or it or the installer is not an
  x86_64 AppImage;
- `SHA256SUMS` does not cover the carefully packed files or the sum does not match.

When packing macOS, the script also unpacks `.pkg` and checks the application signature (`codesign --verify --deep --strict`, also `UpdateMac`).

<a id="pakowanie-na-macu"></a>
## Packaging on Mac

Without GitHub Actions, on Mac with Unity 6000.6.0f1 (MacOS and Windows modules), .NET SDK 8+, `vpk` and mingw-in 64 and icoutils to Windows launcher:

```sh
brew install mingw-in 64 icoutils
dotnet tool update --global vpk --version 1.2.161
python3 scripts/release.py build                       # both playery of approved commitu
python3 scripts/package_velopack.py pack --platform macos
python3 scripts/package_velopack.py pack --platform windows
python3 scripts/package_velopack.py sums
python3 scripts/package_velopack.py verify
python3 scripts/smoke-macos.py                         # visible application window with .pkg
```

`smoke-macos.py` requires the completion of other processes of WoTR. Unpacks the application with `.pkg`, temporarily postpones its data and preferences (Unitedy keeps macOS data in `~/Library/Application Support/<bundle id>`), checks the visible window and log, saves the windowshot and restores the earlier profile. It does not count on login or online games.

Smoke installer outside CI (Mac without installed game, Windows tester): pack the next version and run the script.

```sh
python3 scripts/package_velopack.py pack --platform macos --smoke-output .artifacts/smoke/macos
bash scripts/smoke-installer-macos.sh .artifacts/releases/<version>/installers/macos-universal .artifacts/smoke/macos .artifacts/smoke/evidence-macos
```

On Linux (squashfs-tools, FUSE, `vpk`; the player from GameCI or `release.py build --platform linux`):

```sh
python3 scripts/package_velopack.py pack --platform linux
python3 scripts/package_velopack.py pack --platform linux --smoke-output .artifacts/smoke/linux
bash scripts/smoke-installer-linux.sh .artifacts/releases/<version>/installers/linux-x64 .artifacts/smoke/linux .artifacts/smoke/evidence-linux
```

```powershell
./scripts/smoke-installer-windows.ps1 -Release <katalog windows-x64> -Next <katalog paczki smoke> -Out evidence
```

<a id="elementy-w-grze-i-w-paczce"></a>
## Elements in the game and in the package

- `Assets/Plugins/Velopack`: Velopack 1.2.161 and dependencies with SHA-256 fastened
  ([SOURCES](../unity/Wotr.Client/Assets/Plugins/Velopack/SOURCES.md)).
- `Assets/Wotr/Runtime/Updates`: Velopacka start before splash screen without automatic use of
  packages, installation status (build release only installed by Velopack can update; Editor, build
  developer and copy with ZIP-a-a-a-a-a-a-a-a-a-a-a-a-a) and `--wotr-self-check <file>` for test
  dragons: saves build and install version, platform and data path, then closes the game before
  opening the window.
- `release/windows/WotrLauncher.c`: `Wotr.exe` from Windows installation. Corresponds to 0 code for
  `--veloapp-*` hooks without loading Unity (installing and updating do not open the game window),
  and also transmits the command line to `UnityMain` from `UnityPlayer.dll`. It is built by
  `release/windows/build-launcher.sh` with the icon and manifest of Unity file.
- Bundle id macOS is pinned (`com.WoTR-Private.War-of-the-Ring-Online`, `DesktopBuildSetup`): Unity
  holds the player's data and preferences under it. Company and product (`WoTR Private`, `War of the
  Ring Online`) set data to Windows (`AppData\LocalLow`). None of these values are changed.

Players with ZIP `0.2.0-rc1` install the new version once; data and settings remain because identifier has not changed.

<a id="serwer-i-zgodność--dist-05"></a>
## Server and compatibility — DIST-05

The server, its image and acceptance restore releases `wotr-server` (DIST-03) separately. The administrator uses [Compose configuration and instructions](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/infra/README.md).

The client sends `X-Wotr-Client-Version` and `X-Wotr-Protocol-Version` in each request and handschake WebSocket. The server corresponds to 409 `ClientUpdateRequired` or `ServerUpdateRequired`; the client then shows a clear reason and does not reconnect the call automatically. The public key of the publisher is pinned in `Assets/Wotr/Runtime/Networking/ReleasePublisher.cs`. The commands and layout of the directory on the host: [infra/README](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/infra/README.md).

## Application icon

The desktop builds use the shared Unity icon texture. Native [Windows ICO](icons/wotr.ico),
[macOS ICNS](icons/wotr.icns) and [PNG](icons/wotr.png) exports are available for release tooling.
The [artwork record](../graphicalassets/branding/README.md) contains the original master,
the exact imagegen prompt and format-regeneration instructions.
