<a id="tryb-demonstracyjny-z-cpu--demo-04"></a>
# Demo mode with CPU — DEMO-04

The user's order with 2026-10-15: purely demonstration mode, run from the waiting room for the second player. The player performs his moves and the other side performs the simple CPU. The goal is to enable the game to be turned on and the next turn passes. Another demo overtakes the previous one, unlike the usual parties.

Server, bot and data: `wotr-server:docs/server/DEMO-MODE.md`.

<a id="pokój-oczekiwania"></a>
## Waiting room

- In the action Ready/Start row standing the “Play demo vs CPU” button (`room-demo`), visible to
  both room members.
- Garnish (solid arrangement principle):
  - the room does not wait (the party started or closed room);
  - another operation of the screen (`Busy`) is in progress.
- Tooltip: „Practice against a simple computer opponent playing the other
  side. Starting a new demo replaces your previous demo.”
- The click performs `POST /api/rooms/{id}/demo` and opens board returned the same way as the start
  of the regular batch (`openMatch`). The room stays on the server and waits for the second player.
- Errors show the same screen status as other room operations: 404 (outdoor room) and 409
  (`MatchAlreadyStarted`).

<a id="plansza-i-lista-partii"></a>
## board and batch list

- Demo is a simple game: the same windows, console, action flags, sounds, Log and end of game
  screen. CPU moves like enemy moves, at a rate of approximately 0,8 s per step.
- The opponent is called ‘CPU’, the party ‘Demo · Free Peoples vs CPU’ (or ‘Shadow vs CPU’). Names
  come from the server, and the client does not submit them.
- There is only one demo on the Games list. You can resume it; from the MENU-32 menu board there is
  no "Delete game". New demo replaces the previous one without additional confirmation, and tooltip
  button warns.

<a id="wdrożenie-demo-04-2026-10-15"></a>
## implementation DEMO-04 (2026-10-15)

- `WaitingRoomScreen`: button `room-demo` in the action row for Ready and Start, `SetEnabled(!busy
  && waiting)`; event `Demo`.
- `WaitingRoomController.Demo`: `POST api/rooms/{id}/demo` (`DemoMatchView`), then `openMatch`. 409
  `MatchAlreadyStarted` has its own message.
- Client attaches Contracts 0.5.0, public content 1.0.7 and test backend `489e5be31221`
  (`wotr-deps.lock.json`).

<a id="testy"></a>
## Tests

- PlayMode with server (`DemoRoomNetworkTests`):
  - the button is in the room and is greyed during the operation;
  - the start opens board demo with the opponent ‘CPU’;
  - CPU makes its first move when the decision is his (Shadow player);
  - a new start gives a new game and the previous one disappears from the Games list.
- acceptance DEMO-05: several turn sides against CPU.
- acceptance DEMO-05 (2026-10-15): `DemoRoomNetworkTests` 1/1 and the regression menu. On the server
  side, the whole demo party by HTTP on both sides until the end of the game. Matrix DM01–DM10:
  `wotr-server:docs/server/DEMO-MODE.md`.

<a id="demo-prosto-z-menu--menu-31-2026-10-16"></a>
## Demo straight from the menu — MENU-31 (2026-10-16)

The demo can also be launched from the main menu: “Demo vs CPU”, and then click card. The client creates a private room himself, launches a demo (the same path as DM09) and releases the room, so in “My games” there is no waiting room (`MAIN_MENU_UX.md`, section MENU-31).
