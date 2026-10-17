<a id="klient-unity--budowanie-i-testy"></a>
# Unity Client — Building and Testing

Moved from `docs/DEVELOPMENT.md` monorepo at the repository division (DIST-03). The instructions for the backend, the database and implementation are in [DEVELOPMENT server](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/DEVELOPMENT.md). Before running Unity `scripts/unity.sh` downloads pinned with dependencies (`python3 scripts/bootstrap_deps.py`, description in [README](../README.md)).

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

The above `BuildMac`/`BuildWindows` is a development for testing/profileing. Menu counterparts without Development: `Wotr.Editor.BuildTasks.BuildLocalMac` and `Wotr.Editor.BuildTasks.BuildLocalWindows`. Before using CLI close the editor of this project by saving your work. The pre-released packages with the requirements of a clean commit are described by [instructions for issue](../release/README.md).

`Assets/Wotr/Resources/WotrPanel.asset` is part of the project. The client creates its instance, and build thus contains the required text resources ICU and the UI Toolkit theme. Do not replace this resource with `CreateInstance<PanelSettings>()` itself in runtime: the editor can then work properly, but the built application does not contain text data. `BuildTasks.PrepareProject` plays missing resource before the build.

