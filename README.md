<a id="war-of-the-ring--klient-wotr-client"></a>
# War of the Ring — client (wotr-client)

Unity 6 (6000.6.0f1) client for War of the Ring online 1v1 on Windows x64 and macOS.
Documentation, tracked project notes, code and UI are in English (DOC-01).
The server, rules, Contracts, canonical content and central task registry live in
[wotr-server](https://github.com/jpolaczek/wotr-server). Both are public open source from one
fresh commit (D17); the earlier history (split from the `WotR` monorepo in DIST-03) stays in
private archives. Boundaries and link conventions are documented in
[REPOSITORY_SPLIT](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/release/REPOSITORY_SPLIT.md).

<a id="pierwsze-uruchomienie"></a>
## First launch

Requirements: Python 3, Unity 6000.6.0f1 with macOS and Windows modules, .NET 10
runtime (PlayMode only). The pinned packages are public releases of `jpolaczek/wotr-server`;
no token is needed (`WOTR_DEPS_TOKEN` or `gh auth login` only for a private channel).

```bash
python3 scripts/bootstrap_deps.py            # Contracts and public content from wotr-deps.lock.json
python3 scripts/bootstrap_deps.py fetch --anonymous --test  # prove all four public packages need no credentials
bash scripts/unity.sh -runTests -testPlatform EditMode -testResults .artifacts/editmode.xml -logFile .artifacts/editmode.log
bash scripts/unity.sh -quit -executeMethod Wotr.Editor.BuildTasks.BuildLocalMac -logFile .artifacts/build-mac.log
bash scripts/unity.sh -quit -executeMethod Wotr.Editor.BuildTasks.BuildLocalWindows -logFile .artifacts/build-win.log
```

`scripts/unity.sh` runs bootstrap before every Unity start. Bootstrap downloads
only archives matching the lock's SHA-256 and rejects different bytes, unknown
versions and unavailable access, without substituting another version. For offline
use or a local channel, set `WOTR_DEPS_SOURCE=file:<folder>`.
`fetch --anonymous` ignores `WOTR_DEPS_TOKEN`, `GITHUB_TOKEN` and the local GitHub
CLI login, so a successful empty-cache run is evidence that the pinned release is
public. It does not delete or modify any credential.

<a id="testy-z-backendem"></a>
## Tests with a backend

PlayMode connects to a real server and test database. The server and seeder are
not compiled: `scripts/test-client.sh` downloads the pinned `wotr-server` and
`wotr-seeder` archives (`bootstrap_deps.py fetch --test`) and runs them against
an isolated PostgreSQL database whose name ends in `_test`.

```bash
WOTR_TEST_DB='Host=127.0.0.1;Port=55432;Username=wotr;Database=wotr_client_test' \
  bash scripts/test-client.sh -testFilter Wotr.PlayTests.MainMenuNetworkTests
python3 scripts/test-client-batches.py --inventory <EditMode/PlayMode XML> --output .artifacts/play
```

Full PlayMode requires an earlier EditMode run in the same checkout
(`DesktopLayoutStabilityTests` reads `.artifacts/ui13-profile-views.json`).

<a id="zależności-serwera"></a>
## Server dependencies

`wotr-deps.lock.json` pins `com.wotr.contracts` (UPM `.tgz` from `.deps/`),
`com.wotr.public-content` (copies in `Assets/Wotr/Resources` with their `.meta`),
and test-only `wotr-server` and `wotr-seeder`. To change versions, run
`python3 scripts/bootstrap_deps.py pin <name>@<version> …` and commit the lock,
Unity manifest and `Resources`. `wotr-server` publishes packages as releases
named `<name>-<version>`.

<a id="wersja-klienta-i-zgodność-z-serwerem"></a>
## Client version and server compatibility

Every `BuildTasks` build embeds the identity from `release/version.json`
(`ClientBuildIdentity`) and writes identical bytes to `build-info.json` beside
the app; the editor declares a development version. The client sends its version
and protocol with every request and WebSocket handshake. The DIST-05 server
returns 409 when the client is too old (`ClientUpdateRequired`) or its protocol
is unsupported (`ServerUpdateRequired`); the client explains the reason without
automatic retries. Details: [release/README](release/README.md).

<a id="dokumentacja"></a>
## Documentation

- [UI and interactions](docs/ui/UX_SPEC.md), [board](docs/ui/BOARD_INTERACTION_UX.md), [menu](docs/ui/MAIN_MENU_UX.md)
- [Client packages and releases](release/README.md), [player guide](docs/release/PLAYER-GUIDE.md)
- [Working instructions](AGENTS.md); the registry, workflow and requirements live in `wotr-server`

`python3 scripts/check_docs.py` checks local links and links to server documents
pinned in `docs/repositories/wotr-server.json`.

## The board game

War of the Ring is a board game designed by **Roberto Di Meglio, Marco Maggi and Francesco
Nepitello**, created, published and distributed worldwide by
[Ares Games](https://www.aresgames.eu/games/war-of-the-ring-line). Credits from the English
rulebooks:

| Role | Second Edition | Lords of Middle-earth | Kings of Middle-earth |
| --- | --- | --- | --- |
| Game design | Roberto Di Meglio, Marco Maggi, Francesco Nepitello | Roberto Di Meglio, Marco Maggi, Francesco Nepitello | Roberto Di Meglio, Marco Maggi, Francesco Nepitello |
| Art | John Howe | John Howe; additional art Jon Hodgson | John Howe |
| Art direction and graphic design | Fabio Maiorana | Fabio Maiorana | Fabio Maiorana |
| Sculptures | Bob Naismith | Bob Naismith and Dust Studio | Bob Naismith |
| Sculpture design | John Howe and Matteo Macchi | Matteo Macchi and Francesco Nepitello | — |
| Photos | Christoph Cianci | Christoph Cianci | Christoph Cianci |
| Production | Roberto Di Meglio and Fabrizio Rolla | Roberto Di Meglio and Fabrizio Rolla | Roberto Di Meglio and Fabrizio Rolla |

The Fate of Erebor expansion is also by Ares Games. This online version exists thanks to their
work and with great respect for it. If you enjoy it, please buy the board game from Ares Games or
your local game store.

## License

MIT ([LICENSE](LICENSE)). Fonts and libraries shipped with the game keep their own licenses, listed
in [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.md).
