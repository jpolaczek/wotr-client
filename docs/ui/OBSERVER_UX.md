<a id="plansza-widza--obs-03obs-05-2026-10-16"></a>
# Observer board

The product contract and OB01–OB09 scenarios are in the server's
`docs/server/OBSERVERS-REPLAYS.md`. Observer reads use `/api/observe/{id}`;
`docs/server/T07-TRANSACTIONS.md` describes the server protocol.

<a id="wejście-i-wyjście"></a>
## Entry and exit

Observers enter from the waiting room when the game starts, from My games
(Watching · In progress), or directly from a public room or invitation code when
the game is already running. Exit match, Quit game and Main menu on the end screen
release the observer seat with `leave`, then return to the menu or close the app.
A replay is saved only when requested. If access ends because the lease expires or
the host removes the observer, a Watching ended window offers Main menu.

<a id="pasek-observing"></a>
## Observing bar

A fixed bar below notifications occupies the test-scenario bar position. It shows
OBSERVING · VIEWING and Free Peoples/Shadow, highlighting the selected perspective.
Free Peoples is the default. Switching shows that side's hand, knowledge, history
and reports at the same revision, preserving the camera, tabs and panels. A late
response for the previous perspective cannot replace the board: connection
generation and `MatchConnection.Rebase` guard this transition. Selecting a
perspective never grants player permissions.

<a id="co-działa-a-czego-nie-ma"></a>
## Read-only interaction

The observer can inspect regions, battles, armies, hand/table cards, tabs, the Log,
battle reports, Settings and menus. There is no `TurnInteraction`, command sending,
command preview, draft, command journal, automatic decision, `MatchSeenTracker`,
opponent-action notification or local command persistence. The client clears
`Decision` before rendering. The console neutrally identifies the decision owner
and phase; it does not show YOUR ACTION or highlight a personal decision badge.

The end screen says FREE PEOPLES WIN or SHADOW WINS and offers Save replay.

<a id="połączenie-i-obecność"></a>
## Connection and presence

`MatchReadRoutes.Observer` supplies the perspective parameter for reads and updates.
WebSocket updates, refresh and reconnect with backoff use the same transport as
player sessions. Entry and reconnect display current state without replaying old
events. Presence is renewed every 15 s with `POST /api/rooms/{id}/presence`. A 404
ends observation; a network error waits for the next retry. Leaving stops presence
and updates.

<a id="kod"></a>
## Implementation

- `Networking/MatchReadRoutes` selects player or observer routes for
  `MatchConnection`, `ActionLogController`, `ActionDetailLoader` and
  `BattleReportController`.
- `Application/Observing/ObserverSession` assembles the observer board;
  `ObserverInteraction` handles inspection and `ObserverConsole` supplies status.
- `UI/Observing/ViewingBar` and `BoardScreen.Observe(side)` control perspective.
- `ActionLogController.Reset` clears the previous perspective's pages, action and
  reports; `MatchConnection.Rebase` resets revision tracking.
