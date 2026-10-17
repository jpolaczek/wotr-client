<a id="t09--klient-z-rzeczywistą-sesją"></a>
# T09 — Client with actual session

Unity launches login, then a private table list. Registration requires an administrator code; a separate table code takes second place. The page is selected when creating; joining takes free. Account cannot be both players. Login, selection of the page and configuration have their own UI Toolkit components.

Change of any switch saves configuration and removes acceptances. UI presents dependent variants and inaccessibility of expansions startup. Each player accepts settings; after start, both switch to the map. Lobby refreshes the room every 2 s and keeps the view at a momentary loss of the network.

<a id="granice-modułów"></a>
## Module boundaries

- `Runtime/Networking`: `GameApi` (HTTPS/JSON, Bearer, timeout, errors) and `MatchConnection` (WSS,
  revision notifications, re-download, re-download, re-run to 15 s). Session remains in process
  memory; PlayerPrefs only stores the server address. Screen shutdown cancels tasks and ends
  subscription.
- `Runtime/Application`: bootstrap and `ClientFlow`, managing screens and the life of connections.
- `Runtime/UI`: components of login, lobby, room and board. Inspection does not clean decision tray.
- `Runtime/Map`: public view model, camera and rendering. Static regions/geometry indexed after ID.
  The symbols of existing armies are reused; in drawing no repeated `Single`, and the layout does
  not create a list of all candidates.
- `Runtime/Presentation/AssetCatalog`: symbols and optional sprite paths by
  `Resources/asset-manifest.json`. Graphics remain substitute and do not determine legality.

The client only refers to Contracts, never Domain/Server. HTTP uses system certificate validation; there is no bypass TLS. Only Editor/development build allows HTTP to loopback. Release version requires HTTPS. The revision number remains in the transport model, except for normal HUD.

<a id="odtworzenie-i-widoczność"></a>
## Restoration and visibility

Two sessions have different tokens, roles and hands. The same public map comes from server `PlayerView.Units` and `Control` Transport never accepts a full domain snapshot. WSS always precede GET view; message with the same revision does not rebuild UI The change of data does not change pan/zoom. Session Error Leaves Last board to view and allow you to return to login.

card have temporary type/decks/number labels, without imaginary texts and effects. T10 added the decisions of the turn cycle described in [Rule Report](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/rules/T10-TURNS.md); movement, combat and full card remain the next tasks. This is not a complete batch of the database.

<a id="wykonane-sprawdzenia"></a>
## Checks carried out

- Unity EditMode: 4/4, map data, camera and contracts.
- `bash scripts/test-client.sh`: PostgreSQL 17 + real Kestrel HTTP/WS on loopback + Unity PlayMode,
  3/3 without omission. Two independent instances of client/session in one Unity process (not two
  operating systems).
- Registration, join, two acceptances, configuration change, re login/reading lobby, start, role
  comparison and public setup with HTTP response; interrupt WSS, reconconnect, camera behavior and
  cancel session.
- Render  1366×768  /  1920×1080 and simulated events mice/g smooth; no tracers, location HUD,
  login and nine configuration options without scroll. Drops in `.artifacts/screenshots/`,  XML/logi
  in `.artifacts/`.
- Image overview detected a collision of the inspection panel with the map center and a low contrast
  of inputs; improved and re-re-re-re-editored. The user did not perform manual tests. The target
  physical test of devices and Windows runtime remains in the acceptance platforms.
