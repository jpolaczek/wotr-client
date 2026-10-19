<a id="poczekalnia--układ-usuwanie-gracza-i-czat-menu-23menu-26"></a>
# Waiting room — layout, player removal and chat (MENU-23–MENU-26)

User order with 2026-10-15:

> The panel of the game created is very primitive, adjust it to the rest, set a neat layout with a
> place on one and the other player, with the possibility of removing it from the game by host, the
> back button is simply to have a label “back”, refresh is not to interrupt two larger buttons. add
> basic chats for players, it does not have to store history

<a id="układ-menu-25"></a>
## Layout (MENU-25)

The waiting room accepts the layout of the main menu (MENU-11/MENU-15): dimmed map, header, fixed body 980×540 with window in the `frames/panel` frame and column action 250 px on the right and the status row. The sizes are fixed and each control has a fixed place: it is not available brightened, it does not disappear.

```
              EVENING GAME                     (name of room)
   Private · Base game · Two players · Play at your own pace
┌──────────────────────────────────────────┐ ┌──────────────┐
│ Players                             [⟳]  │ │ Ready        │
│ ┌──────────────────┐ ┌─────────────────┐ │ │ Start game   │
│ │ ◯ FREE PEOPLES   │ │ ◉ SHADOW        │ │ │ Play demo    │
│ │ Alice            │ │ Waiting for a   │ │ │   vs CPU     │
│ │ Host · You       │ │ player…         │ │ │              │
│ │ Ready            │ │                 │ │ │              │
│ │                  │ │        [Remove] │ │ │ Settings     │
│ └──────────────────┘ └─────────────────┘ │ │ Back         │
│ Invitation [code………………] [Copy] [New]   │ └──────────────┘
│ Chat                                     │
│ ┌──────────────────────────────────────┐ │
│ │ Alice: ready when you are            │ │
│ └──────────────────────────────────────┘ │
│ [message………………………………………] [Send]          │
└──────────────────────────────────────────┘
                 status / errors
```

- Window header (`frames/panel-header`): "Players" and refresh icon (`refresh-room`) at the right
  end of the table, as in the menu. Refreshing is no longer between bigger buttons.
- Two player seats, always in order of Free Peoples, Shadow, in the `frames/entry` frame. Each
  contains:
  - Page emblem: ring or eye as on menu lists;
  - the name of the page;
  - the name of the player or ‘Waiting for a player.’
  - the indications ‘Host’ and ‘You’;
  - ‘Ready’ or ‘Not ready’ state.

The opponent's place has the “Remove” button (from OBS-04 each place has its own `seat-<free|shadow>-remove`). Active is only for host when the place is occupied; in other cases it is greyed, with tooltip.
- Invitation: code field (`room-invitation`), "Copy" (`copy-room-invitation`) and "New code"
  (`generate-room-invitation`). The bar is always visible; outside the host and free seat it is
  greyed.
- action column (in the frame, as in the menu):
  - „Ready” / „Not ready” (`room-ready`);
  - ‘Start game’ (`start-game`) — for the guest greyed with tooltip ‘Only the host starts the game’.
  - „Play demo vs CPU” (`room-demo`, DEMO-04);
  - interval;
  - „Settings” (`open-interface`);
  - "Back" (`waiting-back`) — just that word. Tooltip says the host closes the room and the guest
    releases his seat.
- The player removed at the next refresh returns to the main menu with the message “The host removed
  you from this room.”

<a id="puste-poczekalnie-menu-27"></a>
## Empty waiting room (MENU-27)

The game that hasn't started is not saved. The waiting room is alive as long as someone is in it (refreshs it what 2 s). Empty or closed by host disappears from the server after 30 s, also from the list of "My games". Whoever left the waiting room (Back, demo, closing the game), this one returns to it only when in the meantime there was another player.

<a id="czat-menu-26"></a>
## Chat (MENU-26)

- Simple chat of room players, only in the waiting room. The server keeps in mind the most 50 the
  last message of the room, without writing it in the database, so restart it removes it (it does
  not have to store history).
- The chat window has a fixed height, scrolls inside, and the latest message is at the bottom. Poem:
  player's name in the color of his page, colon and text. The text is displayed literally, without
  tags.
- Message field (`room-chat-input`, to 200 characters) and "Send" (`room-chat-send`); Enter sends.
  Empty message does not send anything.
- Refresh every 1 s, regardless of the room's refresh. The message error leaves the text in the
  field and appears in the status.
- Host also writes when no one has joined (MENU-29). Status tells why the message has not passed:
  400 — message rules; 404 — chat-free server (before MENU-24) needs to be updated; 429 — break
  time; other errors — “Message not sentence Try again”.

Server and contracts: `wotr-server:docs/server/T08-LOBBY.md` (MENU-24 section).

<a id="obserwatorzy--obs-04-2026-10-16"></a>
## Observers — OBS-04 (2026-10-16)

The room accepts two players and to 16 observers (server: OBS-01, wotr-server `docs/server/T08-LOBBY.md`).

- Each place has two fixed buttons at the bottom:
  - “Take seat” (`seat-<free|shadow>-take`) — a free seat is taken by the viewer or player from
    second place. Both players are losing readiness;
  - "Remove" (`seat-<free|shadow>-remove`) — the host removes the player from this place.

Both only work before the launch.
- Under the places is the "Observers n/16" line with constant height 52 px (two rows of labels;
  seats now have 128 px, and a chat list at least 40 px). Each viewer is a name tag (host with the
  addition "· Host") and "×" (`remove-observer-<name>`). "×" removes the viewer: active only for
  host, not for himself. The removed viewer returns to the menu with a reason and cannot return.
- The action column has an "Observe" (`room-observe`): the player slows down the seat and moves to
  the audience.
- The viewer has no "Ready" or demo (scared) and no place is marked as his.
- Host can lead the room from the audience: configures, shows the code, removes the participants and
  starts the game of two other players.
- The viewer's entry and output does not remove the readiness. The server's refusal shows the status
  and status of the server, for example, "That seat was just taken."
- Chat: The viewer writes as “Name (observer):” in neutral color.
- After the party starts, the viewer stays in the waiting room with status; board the viewer joins
  OBS-05.

<a id="rules-strip--ui-263-2026-10-19"></a>
## Rules strip — UI-263 (2026-10-19)

Owner decision 2026-10-19: the rules of a table are chosen only in its waiting room. Under the seats the window
splits in two: the audience, the invitation and the chat keep the left part, and the rules take a fixed column of
236 px on the right, every rule in a fixed place:

```
│ ┌ Free Peoples ─────────┐ ┌ Shadow ────────────────┐ │
│ └───────────────────────┘ └────────────────────────┘ │
│ Observers 2/16 [Ann ×][Bo ×]          (⛰) [✓ New Cities         ] │
│ Invitation [code……] [Copy] [New code] Fate [○ Council of Rivendell] │
│ Chat                                   (◎◎)[○ Visible Corruption  ] │
│ ┌───────────────────────────────┐     Lords[○ Sequential Corruption] │
│ │ Alice: ready when you are     │     (♛) [✓ Dismay              ] │
│ └───────────────────────────────┘     Kings[○ Removing Ruler Tokens] │
│ [message…………………………] [Send]                                       │
```

The three medallions (44 px, 58 px with the caption) and the six chips (25 px, 29 px apart) are both 174 px tall, so
the column fits the window at 125 % on 1366 x 768; the invitation's title and buttons are narrower (96 and 92 px) to
keep the code readable beside it; observers that do not fit the line are clipped and the count stays.

- Three expansion medallions in release order: Fate of Erebor (the Lonely Mountain under the Arkenstone), Lords of
  Middle-earth (three linked Elven rings), Kings of Middle-earth (a crown). Art: `graphicalassets/ui/src/rules`
  (`medallion` with `hover`, `pressed`, `fate-on`, `lords-on`, `kings-on`; `crest-*`; `check-off`/`check-on`). An
  expansion in the game lights its medallion in its own enamel with a gold glow and a coloured crest; one not in the
  game shows a grey crest. Names: `rules-module-fate|lords|kings`.
- Six variant chips in one column, grouped by the expansion they need: New Cities (Fate), Council of Rivendell
  (Lords), Visible Corruption and Sequential Corruption (Kings), Dismay and Removing Ruler Tokens (Lords and Kings)
  (`rules-variant-<id>`). A chip that is on wears the red lacquer of a selected
  button and a ticked gilt ring; off, an empty ring.
- Only the host of a waiting room changes the rules. A click sends `PUT /api/rooms/{id}/configuration` with the
  room's version and the configuration after that one change (`RulesChoices.Toggle`); the strip changes only when
  the server's room comes back. Removing an expansion removes its variants; adding or removing Lords or Kings sets
  Dismay to its default (on with both); switching Dismay off also switches Removing Ruler Tokens off.
- Every control stays in place and is disabled, never hidden, with the reason in its tooltip: a variant whose
  expansion is missing ("Council of Rivendell needs Lords of Middle-earth."), an expansion the server does not offer
  yet ("… · Not yet available on this server.", from `RoomView.OfferedModules`; an older server's
  `ExpansionsAvailable` offers all or none), the guest ("Only the host chooses the rules."), a started game ("The
  rules are fixed once the game starts."). An expansion that is in the game but no longer offered stays enabled for
  the host, who can still remove it. Every enabled control's tooltip says what its click does ("Click to add Fate of
  Erebor to this game. Both players get ready again."), and each variant's tooltip adds what it changes.
- Ready accepts the rules (its tooltip says so); every change of the rules resets both players' Ready on the server.
  Start explains a table whose expansion the server no longer offers.
- The masthead line names the rules: "Public · Fate of Erebor + Kings of Middle-earth · Two players · Observers 0/16".
- The crests (`ExpansionCrests`, 18 px) also mark a game's expansions on the rows of Open games, My games and Replays,
  after the phase in the top bar and in the Victory window's header; their tooltip names the expansions and the
  variants in play. A base game shows none.

The old `RoomScreen` and `ConfigurationEditor` (toggles, only used by tests) are gone; their tests moved to
`WaitingRoomRulesTests` and `RulesChoicesTests`.

<a id="scenariusze"></a>
## Scenarios

| ID | Scenario | Task |
| --- | --- | --- |
| WR01 | Host removes the guest; the guest cannot return to this room, and the invitation code changes. | MENU-24 |
| WR02 | Chat: messages from both players in order; aliens, empty, too long and control characters rejected; reboot cleans chat. | MENU-24 |
| WR03 | Arrangement in four window sizes: two seats, fixed control positions, ‘Back’, refresh in the header, without scrolling the page. | MENU-25 |
| WR04 | The guest sees the “Start game” and “Remove” grey; the removed guest returns to the message menu. | MENU-25 |
| WR05 | Two players exchange messages via the server; Enter sends; text with tags is displayed literally. | MENU-26 |
| OB03 | A viewer from a public poem and code, chat with role, simultaneous “Take seat” into one place (one sits, the other gets status), return to the audience, remove the viewer by the host. | OBS-04 |
| WR06 | The host adds and removes expansions and variants by clicks; the guest sees them read-only; an unreleased module is greyed with its reason; every change resets Ready; all 93 legal configurations are reachable and fit the window in four layouts. | UI-263 |
