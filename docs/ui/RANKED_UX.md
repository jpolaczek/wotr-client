# Ranked play — interface

Owner's decision of 2026-11-05 (R35). Status: every part stands (UI-497–UI-504); only the owner's playtest and the rollout remain (RANK-09). The rules of the mode (compatibility,
pairing, clocks, pause, reconnecting, rating) are in `wotr-server:docs/RANKED_PLAY.md`; this document describes only
how the client shows them. The implementation tasks are UI-497–UI-504.

Every rule of [UX_SPEC](UX_SPEC.md) and `AGENTS.md` applies: every control is clicked, tooltips are noninteractive,
nothing opens on hover alone, unavailable controls are disabled rather than hidden, and every control keeps a stable
place.

## Main menu

The action column gains **Find game** and **Leaderboards**. Host game, Join by code and the Open games list are
unchanged: they stay correspondence tables without matchmaking.

## Find game window (UI-497)

A window in the family of Host game. It has no name, visibility or variant controls.

| Row | Controls |
|---|---|
| Each released expansion | Its name and three segments: Off, Allow, Enforce. Default Allow. |
| Faction | Two faction cards, none selected. A click on the selected card releases it. Below them two segments, Allow and Enforce, disabled while no faction is selected. |
| Each time preset | Its name, its two limits (for example `3:00 / 0:30`) and three segments: Off, Allow, Enforce. Default Standard Allow, the others Off. |

- Enforcing a time preset lowers another enforced preset to Allow, and the last preset that is not Off cannot be
  turned off: its Off segment is disabled, with a tooltip saying why.
- Every segment's tooltip says what it means for the match, for example "Kings must be in the game".
- An expansion the server does not release stays Off and disabled, with a tooltip saying so. Until the server answers,
  every expansion is in that state.
- The player's rating stands in the window, with a line saying that leaving a ranked game or running out of time loses it.
- The page has two columns, the rows on the left and the side cards on the right, so that nothing scrolls at
  1366×768 at 125 %.
- The primary **Find ranked game** button has a fixed place. The window always opens in the defaults: the faction
  is never remembered.
- No numeric entry and no scrolling, in all four standard layouts.

## Searching (UI-498)

- Find ranked game starts the search. A moving indicator and the elapsed time stand beside the button, whose text
  becomes **Cancel search** in the same place.
- Every selection and the menu's other actions are disabled, not hidden, while the search runs. Only Cancel search
  responds, and a cancel restores every control.
- On a match a soft sound of its own plays (`MatchFound`, owner 2026-11-06: the decision alert was too sharp for this), also in
  the background, since the window may be there and the clock starts at once. The
  window then presents the opponent for 3 seconds: avatar, name and rating (owner, 2026-11-05; the avatar comes with
  UI-506), and the board opens. This fits within the 15 s the first decision gains for loading.
- A lost connection during the search is shown and the client keeps trying. If the search lapsed meanwhile, the window
  says it stopped and restores the controls. If a pairing happened meanwhile, the board opens.
- The indicator uses art and opacity only, never a CSS filter, and honours reduced motion: it stands still.
- A cancel that cannot reach the server stops trying after 8 s, when the server's ticket has lapsed, and says so; a search
  that merely lost its connection keeps trying. Closing the client tells the server if it can, and the ticket lapses
  within 6 s if it cannot.

## The clock (UI-499)

- A fixed-width clock in the top bar, present only in ranked games: `You 0:27` or `Opponent 2:41`. Normal games keep
  the top bar exactly as it is, since no space is reserved for what usually stands empty.
- It counts locally from the server's remaining time, in real time, and re-syncs on every view without jumping up by
  more than the sync error. A view is always late, so the earliest estimate of the deadline stands; only a change over 5 s
  moves it up, as after a restart gave back its downtime.
- A normal game's bar never holds it, so that bar is unchanged, and a finished game's clock leaves the bar at once.
- 10 s before the limit it turns red with a slight pulse, and its owner hears the `ClockWarning` sound once per
  decision, also with the window in the background. Mute silences the sound but not the colour; reduced motion drops
  the pulse but not the colour.

## Pause (UI-500)

- A small **Pause** button beside the clock. Its tooltip names the pauses left; at zero it is disabled.
- While paused, both clients show a banner naming who paused, with **Resume** for both players. The board's decisions
  are dimmed and inert, the clock is frozen, and chat works. The banner does not cover the chat.
- One veil lies over everything below the top bar, so it covers whichever panel or column holds a decision, and the menu
  above it can still concede. The board cannot be panned or inspected during a pause. Nothing new leaves while paused, also
  from the keyboard; a command already sent may still learn its receipt.

## Leaving and returning (UI-501)

- In a ranked game the board menu's Exit match becomes **Concede**, and Quit game warns that it concedes. Both ask for
  a confirmation with Cancel focused, then send the concession before anything else. A concession that cannot reach
  the server says so and leaves the player in the game.
- After Concede the board stays, with the end screen over it. After Quit and concede the game closes once the server has answered.
- The window's own close asks the same question while connected. Without a connection it just closes: nothing could be
  conceded, and a lost connection is not leaving.
- Signing in with an active ranked match opens it directly: the menu stands for the moment the question takes, so a player
  without a seat and a slow connection wait for nothing. The update restart is not offered while one is active, and a
  required update's overlay waits for its end, since RANK-10 lets a seat play on.
- My games marks ranked rows. A finished ranked game stands in the Finished tab and opens its end screen.
- Normal, test, replay and observer boards keep their menus unchanged.

## Reconnecting (UI-504)

- In a ranked game the connection uses the ranked policy of RANKED_PLAY: a heartbeat every 2 s, a dead socket noticed
  within 8 s, a retry every second.
- While disconnected, the clock keeps counting from the last known deadline, with `Reconnecting…` under the time in the
  same box, so both can be read; the warning still sounds. Pause and Resume grey out and say why. The map, cards and the unsent draft stay usable, and commit is
  disabled, as in [UI-12-RETURN](UI-12-RETURN.md).
- On return an uncertain commit recovers first, then the view applies quietly, without presenting the opponent's
  missed steps; the action log lists them. A game that ended meanwhile opens its end screen.
- The opponent's clock reads `Opponent reconnecting…` while their seat is offline.

## End screen (UI-502)

- Reasons: You conceded, Your opponent conceded, You ran out of time, Your opponent ran out of time.
- A rating line, only for ranked games: `Rating 1532 → 1547 (+15)`, rounded so that the sum holds. A normal game's screen
  has no line and no gap for one.
- Someone reading a replay reads the side instead: The Shadow conceded, The Free Peoples ran out of time. My games lists the
  player's own words.

## Leaderboards (UI-503)

- A page with a table: rank, name, rating, games, wins, losses. The player's own row is marked if listed, and their
  rank and rating stand below the table.
- Loading and error states follow My games: a failed read keeps what was shown and offers Refresh.
- Rows are not clickable. Before any ranked game, an empty message says how to get on the board.
- All 25 rows share the window's height, so nothing scrolls at 1366×768 at 125 %; a shorter list keeps their size.

## Statements this mode changes

- [MAIN_MENU_UX](MAIN_MENU_UX.md): open games stay without matchmaking; Find game is a separate search; a ranked game
  is live and is left only by conceding.
- [WAITING_ROOM_UX](WAITING_ROOM_UX.md): a ranked game has no table, so its rules come from the search.
- [AUDIO_UX](AUDIO_UX.md): the clock's warning is the one reminder.
