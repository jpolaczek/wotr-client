<a id="sygnały-akcji-i-tury-okna-otwierane-same-koniec-gry--ui-165ui-170"></a>
# action and turn signals, windows opened alone, game end — UI-165-UI-170

User application from 2026-10-14:

> plan how to signal that the action was held and whose turn is (except for the current
> notifications, but we also need to change them – they must be more compact, summarize movement
> more, rather than give every detail, it is more about the result) if they are to open
> automatically the window data, which, and when, is to be the

and the note: "Let's add the game end and the victory screen to this plan."

> **Planned, not shipped (UI-302, 2026-10-23):** the live presentation of an important opponent action will show
> the played public card and move the camera without a click, before the resulting decision opens, with one
> transient Skip target. This limited exception and everything it supersedes are specified in
> [OPPONENT_ACTIONS_UX](OPPONENT_ACTIONS_UX.md); until UI-313 ships, the rules below stay in force except where
> UI-305–UI-312 already apply the exception (UI-314: every presented figure and object glows around its own shape
> instead of a frame, and new recruits glow 1.5 s after landing: [§6.18](OPPONENT_ACTIONS_UX.md#wdrozone-314); UI-313: the aggregate acceptance, its coverage matrix, measurements and
> open checks: [§6.17](OPPONENT_ACTIONS_UX.md#akceptacja-313); UI-312: the opponent's Rings, dice and draws show at their own HUD
> objects, and quiet events play no scene: [§6.16](OPPONENT_ACTIONS_UX.md#wdrozone-312); UI-311: a battle's recorded rounds play read-only in its window, which
> opens after the attack: [§6.15](OPPONENT_ACTIONS_UX.md#wdrozone-311); UI-308: the opponent's public Event card opens read-only by itself, then
> docks in the console trail: [§6.12](OPPONENT_ACTIONS_UX.md#wdrozone-308); UI-309: a character that enters,
> transforms, leaves the Fellowship or is removed shows its portrait and title for a moment, and a moving group is
> named where it lands: [§6.13](OPPONENT_ACTIONS_UX.md#wdrozone-309); UI-310: the shared Hunt plays entry by entry in its
> window and the Fellowship's tracks, figure and roster change after it, with the Fellowship panel opened read-only:
> [§6.14](OPPONENT_ACTIONS_UX.md#wdrozone-310)) (the camera frames an opponent's map action, one Skip target shows while it
> plays, the opponent's armies move, attack and capture, and their recruits, upgrades and political steps are staged on
> the presented board: [§6.9](OPPONENT_ACTIONS_UX.md#wdrozone-305), [§6.10](OPPONENT_ACTIONS_UX.md#wdrozone-306),
> [§6.11](OPPONENT_ACTIONS_UX.md#wdrozone-307)).
> UI-303 (2026-10-23) shipped only the data layer, with no visible change: see
> [Opponent presentation cues](#wskazówki-prezentacji-ui-303). UI-304 (2026-10-23) shipped the playback coordinator,
> dormant until a presenter registers: see [OPPONENT_ACTIONS_UX §7.1](OPPONENT_ACTIONS_UX.md#koordynator).

The decisions of the user from this conversation:

1. **action opponent: just a stack of flags**, without a toast being made. card opened from the flag
   is compact and describes the result instead of a list of steps. Full record stays in the Log.
2. **Your turn: ribbon and console.** When action passes to the player, the top of the map flies to
   about 1,5 s ribbon in the enamel of his site. With the reduced motion instead of the ribbon, the
   console is briefly illuminated. The console status badge lights up until the player starts to
   work. When the opponent runs, the ribbon is gone.
3. Only windows open themselves, where you have to work or look:** window battle and window Hunt in
   both players, their own decisions (decision window, phase Fellowship , Meals at Muster) and the
   game's end screen. card action the opponent never opens itself. The window closed by the player
   is closed until the next event. The camera never moves alone.
4. **Full victory screen** for both players: emblem, "Victory" or "Defeat", reason, short summary of
   the game and buttons "View board", "Game log" and "Main menu"; board is only available for
   reading.

They apply: only base game, the rules of the [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md) layout (permanent size, little empty space, fixed control position) and lighter checking (passing tests are enough, without drop and local builds). No changes to Contracts, rules and projections: data come from `PlayerView` and the action log (`ActionLogView`, `ActionFacts`). The server only gets game end test scenarios (TEST-07).

<a id="stan-przed-zmianą-klient-c107dbe"></a>
## Pre-change Status (`c107dbe` Client)

- action opponent is a stack under the top bar (UI-99, UI-100, UI-106). Its tooltip is one sentence
  (`ActionNoticeText.Sentence`). Click opens in the middle of card action (`ActionContextRouter` →
  `ActionLogPanel`) which scrolls the list of all steps of action.
- Whose is the turn, says the status of the UI-160 console (`TurnConsoleState`), the phase in the
  top bar and the golden decision note (`DecisionNotice`). The move to the player is not
  distinctive.
- They open themselves: the battle window at both players (UI-149), the Fellowship and Hunt panels
  at the decision they host (`Held`, `OpenDecision`), the decision window (UI-160) and the tray
  hosting panels. There is no single written rule or test that watches it.
- The game's end is the `victory-reason` label (`VictoryPresentation.Result`) and Victory panel with
  points in the Menu.

<a id="a-kompaktowa-karta-akcji-przeciwnika--ui-166"></a>
## A. Compact card action opponent — UI-166

- The flag and stack remain as in UI-99/UI-100: with one flag on action (battle: one on battle),
  click read the latest, read disappears.
- Tooltip banners: the score header and its first line.
- card stands as in the middle today (decision with UI-100), has a constant width of about 360 set
  and does not scroll:
  - **head** in the enamel of the action (die or card) type icon, e.g. "Army move · Osgiliath",
    "Muster · Edoras", "Event · Flocks of Crebain", "Fellowship moved · Hunt";
  - *at most 3 result lines** from the facts, in order of validity:
    1. the taken over cities and points (‘Osgiliath captured · +1 VP’);
    2. battle and its result (‘Battle at Minas Tirith · Shadow retreats’);
    3. movement („3 Orcs + Witch-king · Mordor → Osgiliath”);
    4. recruitment („+2 Regular · Edoras”);
    5. politics („Rohan · at war”);
    6. Fellowship and Hunt (Corruption 3 → 5 · Revealed in Moria);
    7. character („Gandalf the White enters · Fangorn”);
    8. dice to Hunt (‘Hunt Box +1’).

action A few steps (e.g. two movements of the army) give aggregate lines. When the results are more than three, the last line says "+N more". action without recognized effect, the text of the step is written.
  - **played card** as one line with title; click opens its preview in `CardInspector` (instead of a
    parchment insert with UI-106);
  - ** "Full record" opens this action in Log, where a full record of steps, costs and sources is
    left.
- The distinction of action regions on the map and the frame in the free pass around card are left
  (UI-100).
- Content builds presentation class without UI `ActionResultText` (entry: steps of one action with
  `ActionLogView`; output: header, lines, card). It replaces `ActionNoticeText.Sentence` in the
  tooltip and a list of steps in the card opened from the banner. The list of history in the Log
  remains unchanged.
- Secret: only facts from the safe projection of the journal; hidden card and the opponent's
  election do not go to the header or line (R11).

<a id="wdrożenie-ui-166-2026-10-15"></a>
### implementation UI-166 (2026-10-15)

- `ActionResultText` (`Runtime/Presentation/Activity`) builds `ActionResult`: header, at most 3
  lines, card and die action. The input is steps of one action (`ActionLogView`) or unread steps of
  the flag group.
  - Header: type with command first step (Army move, Attack, Muster, Event, Fellowship moved,
    Politics, Character, Battle for decision inside battle, for the rest of the description command)
    and place: final traffic goal, region battle, title card, nation, source of capacity or Hunt at
    Fellowship with Hunt.
  - Lines in order of validity:
    1. Game over („Shadow wins · Military victory”);
    2. acquisitions (‘Edoras captured · +1 VP’, ‘... recaptured’);
    3. battle („Battle at Edoras · Free Peoples retreats”, „Attack
       stopped”, „Attacker advances”, „Round N”);
    4. marches and travels ("2 Regular + 1 Elite + Witch-king · Minas Tirith → Osgiliath");
    5. recruitment from the reserve (‘2 Regular · Edoras’);
    6. losses outside battle (‘−1 Regular · Edoras’);
    7. politics („Rohan · At war”, „Rohan · Active · 1 step to war”);
    8. Team and Hunting („Corruption 3 → 5 · Revealed in Moria”,
       „Hidden again”, „Mordor step N”, „Progress a → b”);
    9. the character entering the game or its new version (Gandalf the White enters · Fangorn);
    10. dice added to the Hunt field (‘Hunt Box +N’).

With the results above 3, the third line is “+N more”. action without any recognized effect shows its text without page name.
  - Recruitment and loss recognizes the place of figure: from the reserve to the region or from the
    region outside board (`ActionBoardChanges` saves both places). Companion leaving Fellowship does
    not “enter the game”.
  - card is only card discovered to both players: Played Event or unveiled card fight. Selected and
    declared card fight fails to reach the result (R11).
- Ensign: `ActionNoticeGroup.Result`, tooltip `Result.Tooltip` (header and first line). The request
  from the flag has `Compact`.
- card : `ActionCard` in compact mode (class `compact` , 360 pt) has a header in the enamel of the
  page with the icon of the wall dice either card and `ActionResultView` : 3 fixed lines
  `action-result-line-*` line card `action-result-card` (opens) `CardInspector` without playing) and
  `action-result-full` "Full record". This button opens Log and the same action in full record (
  `ActionOpenRequest.Full()` ), with steps, insert card and archives battle and Hunt Distinguished
  and staffing regions remain.
- Tests:
  - EditMode `ActionResultTextTests` (7);
  - PlayMode `ActionNoticeCompactTests` on server data: SG01 `ui42-moves`, SG02 `ui42-battles`, SG04
    `ui42-hunts`, SG03 `grey` — card line and ‘Full record’;
  - `ActionNoticeLayoutTests` checks card compact in four systems;
  - `HuntActivityNetworkTests` reads the Hunt archive after "Full record".

<a id="wyraźny-krzyżyk--ui-177-2026-10-15"></a>
### Clear cross — UI-177 (2026-10-15)

User application: closing cross card   action The opponent was small and blurred (24 pt, mark around 17 pt) Header card has now 34 pt, a pin and a cross 30×30 pt with a sign 22 Holy Cross stands last, in the upper right corner card, and closes it with one click. `ActionNoticeCompactTests` checks its size, location and operation.

<a id="wskazówki-prezentacji-ui-303"></a>
### Opponent presentation cues — UI-303 (2026-10-23)

No visible change: pennants, the compact card, read markers and the Log behave as above. UI-303 is the data layer the
playback (UI-304) will consume; its contract is [OPPONENT_ACTIONS_UX §9.1](OPPONENT_ACTIONS_UX.md#kontrakt-wskazowek).

- `ActionNoticeController` now reads new steps from the **All** history feed (`filter=All`), so the automatic
  consequences (`Actor=null`) of an opponent action, and the shared Battle/Hunt steps, reach the client. The first load
  adds the newest All page (the presentation's baseline and the cursor) to the opponent's history, which it still reads
  with the Opponent filter. `ActionNoticeModel` still makes pennants only from the opponent's steps: the pennants, their
  grouping and what an explicit display marks read are unchanged (EditMode
  `TheAllFeedGivesThePennantsTheOpponentFeedGave`, PlayMode `ActionNotice*`).
- The same pages build `OpponentPresentationModel` (`Runtime/Presentation/Opponent`): ordered, deduplicated, immutable
  `PresentationCue`s per recorded fact, released only when the board shows their revision (`CuesReady`). The first load
  is remembered and never played.
- Privacy is unchanged: a committed Combat card is a back, the opponent's draws are counts, and the player's own steps
  never become cues.
- Tests: EditMode `OpponentPresentationModelTests` (18 cases) on fixtures recorded from the real engine; PlayMode
  `LiveActivityNetworkTests` compares both clients' cues with each other and with their final boards.

<a id="b-sygnał-twoja-tura--ui-167"></a>
## B. Signal ‘Your turn’ — UI-167

- `TurnSignal` (present without UI) compares the previous and current view. Returns the ribbon when
  the decision passes to the player (new `Decision.Id` with `DecisionOwner == You`) and is the
  beginning of its action:
  - action (`DecisionKind.Action`) → „YOUR ACTION · Turn N”;
  - phase Fellowship (`FellowshipPhase`) → ‘FELLOWSHIP PHASE’;
  - Hunt allocation (`AllocateHunt`, Shadow) → ‘ALLOCATE HUNT DICE’ allocation.

Decisions inside battle, Hunt and the card effect do not give ribbons because they have their own windows that open themselves. First loading of the batch and reconnecting it also does not give it; there is enough a glowing console.
- `TurnRibbon` (UI): a ribbon in the enamel of the page under the top bar, in the middle of the map,
  for about 1,5 s (200 ms, pause, 300 ms) . It does not take the indicator (`PickingMode.Ignore`),
  blocks anything and moves the camera. With the Reduced motion ribbon there is no signal, and the
  console badge flashes once.
- The console status badge has a `your-turn` (light) class as long as the player has a decision
  starting and empty draft. It expires at the first step of draft or after sending command.
- Graphics: `free`/`shadow` ribbon and light label (`graphicalassets/ui/src/console/ribbon-*.svg`)
  in the contract `UiArtKitTests`.

<a id="wdrożenie-ui-167-2026-10-15"></a>
### implementation UI-167 (2026-10-15)

- `TurnSignal` ( `Runtime/Presentation` ) watches the next view of one client. The ribbon arises
  when a new decision of the player starting operation ( `Action` , `FellowshipPhase` ,
  `AllocateHunt` ), and the previous decision was not his. Another own action in a row, decisions
  inside battle , Hunt and effect card (including `ActionSequence` The first view of the party and
  the first view after the connection is broken ( `Rebase` In `TurnInteraction.SetConnection` They
  only light the console.
- Text: „YOUR ACTION · Turn N”, „FELLOWSHIP PHASE”, „ALLOCATE HUNT DICE”.
- `TurnRibbon` (`Runtime/UI/Console`): a 440×52 web under the notification bar, in the middle of the
  map, with graphics `console/ribbon-free` and `console/ribbon-shadow`. Inserts into 200 ms, takes 1
  s and extinguishs in 300 ms. All its elements have `PickingMode.Ignore`; does not move the camera.
- `BoardScreen.SignalTurn`: with `reduced-motion` class instead of `ConsoleStatus.Flash()` ribbon,
  one flash of light on 700 ms.
- `ConsoleStatus.SetYourTurn`: the light `console/status-glow` 6 is set around the tag.
  `TurnConsoleInteraction` turns it on when `TurnSignal.Waiting(view, draft)` and the client is idle
  (connected, without sending command). `Waiting` means: the decision that starts the action, the
  lack of dice and the selections in draft; except for the empty share Hunt that the client opens
  alone.
- Tests:
  - EditMode `TurnSignalTests` (5) and the contract `UiArtKitTests` (a ribbon of both sides, a
    glow);
  - PlayMode `TurnRibbonTests` (2) on the fresh base game batch with server:
    - first view without ribbon and with a glowing console;
    - after the Fellowship phase, the allocation ribbon with Shadow and after the assignment of the
      action ribbon with Free Peoples, never with the opponent;
    - click on the ribbon place and the ribbon disappears alone;
    - at a reduced motion flash instead of ribbon;
    - the light is extinguished after the selection of dice.
- AUDIO-07 (2026-10-15): `TurnSignal.Passes(przed, po)` is a common condition for ribbon and tone
  `DecisionRequired`. The sound includes every new decision after a decision that was not a player,
  also reactions in battle, Hunt and effect card. Details in AUDIO_UX.

<a id="c-okna-otwierane-same--jedna-zasada-ui-168"></a>
## C. Windows opened by themselves — one rule (UI-168)

| Window | Opens itself when | U who | After the player closes |
| --- | --- | --- | --- |
| Battle Window | New battle (UI-149) begins | both | closed to the next battle |
| Hunting Window | New Hunt (UI-162) begins | both | closed to the next Hunt |
| Decision Window | your own decision has an object-free selection (UI-160) | owner | closed to next step with elections |
| Panel Fellowship | own decision of the Fellowship phase | Free Peoples | held to decision |
| Meals (from UI-179 to the strip under Politics, from UI-181 to the ‘Reserve’) | card or capabilities: the tab develops and is held; die Muster only backs the tab (UI-181) | functioning | closed for next use |
| Characters, Effects | They're hosting their own decision. | owner | Like today |
| End Screen game | the party has a winner, also when opening the finished batch | both | returns from console |
| card action opponent, Log, region panel, preview card | never, only click | — | — |

The camera never moves itself. It only sets up when you click, for example, the decision notes or flags. UI-168 checks every window against this table, improves the deviations and adds the matrix test.

<a id="wdrożenie-ui-168-2026-10-15"></a>
### implementation UI-168 (2026-10-15)

Audit of each row of table in code:

| Window | Where | Status |
| --- | --- | --- |
| Battle Window | `RegionalBattlePanel` (UI-149): new battle opens, closes to the end battle | compatible |
| Hunting Window | `HuntPanel.OpenNewHunt` (UI-162): new Hunt with decision opens to both, `closedFor` to the next; Hunt resolved without decision does not open the window | compatible |
| Decision Window | `DecisionWindow.Apply`: Selections without object open, `closedFor` to next step | compatible |
| Panel Fellowship | `FellowshipPanel.OpenNewDecision`: Own decision (except action) held (`Held`) | compatible |
| Meals | `ReinforcementsPanel.Apply`: new use develops and holds the tab (`Held`), `dismissed` until end of use (UI-179) | compatible |
| Characters, Effects | They're hosting their own decision so far. | compatible |
| End Screen game | `GameEndScreen` (UI-169) | compatible; derogation repaired in UI-169: Victory panel with Menu closes screen |
| card action, Log, region panel, preview card | open only by clicking (clerical, Log icon, region click, card click) | compatible |

The camera only moves when clicking (`ActionContextRouter.Frame` when opening card from the banner, location buttons). The battle and Hunt windows do not move the camera.

Test of the `AutoOpenPolicyTests` matrix (PlayMode, real server):
- AO01 and row Fellowship: fresh batch — own phase Fellowship holds the panel only with Free
  Peoples; the allocation Hunt and the normal action of the opponent (rejected die) do not open any
  window from the table and do not move the camera;
- AO02, battle (`ui43-stale`): window at both; closed by Shadow is closed after the next decision
  battle; card action the opponent does not open;
- AO02, Hunt (TEST-07 `sh-ring-corrupted`, track step always draws a tile with injuries, so Hunt is
  waiting for a decision): window at both; closed by Shadow is closed; Corruption 12 opens the end
  screen at both;
- AO03, Meals (`fp-muster-recruit`): die Muster opens the panel only to the player; it is closed for
  next use.

The decision window, Characters and Effects have their opening tests (`FellowshipGuideNetworkTests`, `HuntNetworkTests`, `CharacterRosterActionNetworkTests`, `EffectsNetworkTests`); game end screen — `GameEndNetworkTests`.

<a id="d-ekran-końca-gry--ui-169"></a>
## D. Game end screen — UI-169

- `GameEndScreen`: Full screen over the dim board, in the window frame battle. Opens on both players
  alone when `Winner` is set: once per batch and each time the finished batch is opened.
- Content of the `PlayerView`:
  - the emblem of the winner's side;
  - the title from the player's perspective: ‘VICTORY’ or ‘DEFEAT’;
  - subtitle, e.g.. „Free Peoples win · The Ring is destroyed”
    (`VictoryPresentation`, BASE s. 44, LOGIC B20);
  - an illustration of the reason: Loss rift (`RingDestroyed`), corruption (`RingCorrupted`),
    military victory (`ShadowMilitary`, `FreePeoplesMilitary`);
  - Summary in 3–4 lines: turn end, points of both sides (n/4, n/10), Fellowship (progress,
    corruption n/12, Mordor step), number of Companion in Fellowship.
- Fixed buttons:
  - ‘View board’ closes the cover; the status of the console ‘GAME OVER · ...’ (`TurnConsoleState`)
    reopens it;
  - ‘Game log’ opens Log;
  - The main menu exits the party by the existing Menu path.

Victory panel with a break of points stays in the Menu.
- Graphics: title `victory` and `defeat` , 3 Reason scenes ( `graphicalassets/ui/src/end/*.svg` ) in
  the contract `UiArtKitTests`.

<a id="wdrożenie-ui-169-2026-10-15"></a>
### implementation UI-169 (2026-10-15)

- `GameEndState` (`Runtime/Presentation`) ed `PlayerView`:
  - the title from the player’s perspective (‘VICTORY’ or ‘DEFEAT’);
  - subtitle „{page} win · {reason}”: „The Ring is destroyed”, „The
    Ring-bearers are corrupted”, „Military victory”;
  - scene (UI-233): a painting of the result from the viewer's side — `free-victory`, `free-defeat`,
    `shadow-victory`, `shadow-defeat`; an observer sees the winner's victory (`graphicalassets/end`);
  - four lines: turn; points of both sides n/4 and n/10 (`VictoryPresentation.ScoreText`);
    Fellowship (Specs of Doom, Mordor step or progress, Corruption n/12); number of Companion in
    Fellowship.
- `GameEndScreen` (`Runtime/UI/Victory`): cover for the whole board (Denmark 72%) with window in the
  `frames/panel` frame like window battle:
  - scene `Art/end-scenes/*` (480×240), the winner's emblem next to the title sign `end/title-victory` or
    `end/title-defeat`, subtitle and summary;
  - fixed buttons `game-end-view-board`, `game-end-log`, `game-end-save-replay`, `game-end-menu`.

UI-233 (2026-10-17): after View board the console keeps a "Results & replay" button in the place of its hint
(`GameEndConsole`, `ConsoleOptionsInteraction`); it reopens the end of the game and glows (`ChoiceGlow`) while the
replay can still be saved. The four buttons are 112 px wide, so they stay inside the window's frame. The title plate
stretches between its ornaments (9-slice, 400 px) and keeps the title clear of them; a longer title (an observer's
FREE PEOPLES WIN) is set at 21 px. PlayMode `GameEndNetworkTests` checks the buttons inside the frame, the title
within the plate and the reopen button.

Opens itself with the first view with the winner on a given board, so once per batch and each time the finished batch is opened. “View board” and Esc closes it. The status of the “GAME OVER” console opens it again (`TurnConsoleInteraction`, host `GameEnd`). “Game log” closes the screen and opens the Log. “Main menu” exits the same way as Menu → Games (`BoardMenu.LeaveToGames`). The Victory panel opens from the Menu, just like “View board”.
- Tests:
  - EditMode `GameEndStateTests` (4);
  - `UiArtKitTests` with new `end/*` graphics;
  - PlayMode `GameEndNetworkTests` (2) on TEST-07 scripts through the test session
    (`TestScenarioRoom`):
    - `fp-ring-destroyed`: GE01 in both, GE02 (View board, GAME OVER, Game log, Main menu), GE03
      (reopening the finished batch);
    - `sh-military`: titles of both sides and four layouts without scrolling.
  - The client tests on a fastened test base `53d6a0a1164f` (`wotr-deps.lock.json`) with TEST-07
    scenarios.
- AUDIO-07 (2026-10-15): at the live end of the game, the `Victory` or `Defeat` chord plays once
  from the player's perspective. The reopening of the screen (View board, GAME OVER) and the entry
  into the finished game are silent.

<a id="e-scenariusze-testowe-końca-gry--test-07-serwer"></a>
## E. End of game test scenarios — TEST-07 (server)

The ‘Game end’ group in test scenarios (R31, `src/Wotr.Domain/Testing`), deterministically, by the real command (BASE pp. 44, LOGIC B19, B20):

- `fp-ring-destroyed`: Fellowship at the last step of Mordor, and the Hunt jackpot cannot exceed the
  spoil. One move ends the game with a win of Free Peoples.
- `sh-ring-corrupted`: 11 and Hunt pool only with tiles causing damage. Fellowship move ends the
  game with a win of Shadow.
- `sh-military`: Shadow has 10 points and becomes the last action turn. Pass of both sides and end
  turn give military victory Shadow.
- `fp-military`: Free Peoples have 4 points, and the end of turn gives them military victory.

<a id="scenariusze-i-właściciele"></a>
## Screenplays and owners

| ID | Owner | Scenario and expected result |
| --- | --- | --- |
| SG01 | UI-166 | The opponent's army movement: a banner with tooltip "Army move · Osgiliath / 3 Orcs · Mordor → Osgiliath"; card without scrolling, ≤ 3 lines, regions distinguished. |
| SG02 | UI-166 | action with battle and city acquisition: lines in order of validity (city and points, battle, movement); "+N more" with excess. |
| SG03 | UI-166 | Played card events: the title line opens the preview of card; "Full record" opens action in Log with full save. |
| SG04 | UI-166 | Fellowship movement with Hunt: ‘Fellowship moved · Hunt’, ‘Corruption 3 → 5 · Revealed in Moria’; the secrecy of cards and elections. |
| SG05 | UI-167 | The action goes to the player: the ribbon “YOUR ACTION · Turn N” in the enamel of his side, approx. 1,5 s, takes no indicator; with the opponent nothing. |
| SG06 | UI-167 | The Fellowship phase and the Hunt allocation give their ribbons; decisions in battle, Hunt, card effect, first loading and reconnect — no ribbon. |
| SG07 | UI-167 | Reduced motion: no ribbon, one flash of badge; `your-turn` expires after the first step of draft or sending command. |
| AO01 | UI-168 | The usual action of the opponent does not open any window or move the camera. |
| AO02 | UI-168 | The new battle and the new Hunt open windows at both; it closes until the next event. |
| AO03 | UI-168 | Their own decisions open their windows (decision window, Fellowship, Meals); the opponent does not get them. |
| GE01 | UI-169 | End of game with both players: a screen with a emblem, “VICTORY”/“DEFEAT” from the player's perspective, reason and summary; board read-only. |
| GE02 | UI-169 | The "View board" closes the screen, the status of the console opens it again; the "Game log" opens the Log; the "Main menu" exits the batch. |
| GE03 | UI-169 | Opening the finished batch from the saved list shows the end screen immediately. |
| TE01 | TEST-07 | The four ‘Game end’ scenarios start in scenario mode and end in the right winner and reason after the indicated command. |
| SG08 | UI-170 | acceptance: the course of both pages with signals, windows and end of the game on TEST-07 scripts, activity and window regression. |

<a id="odbiór-ui-170-2026-10-15"></a>
## acceptance UI-170 (2026-10-15)

Each scenario has a test:

| ID | Test |
| --- | --- |
| SG01 | `ActionNoticeCompactTests.ArmyMoveReadsAsItsDestinationAndForce` (`ui42-moves`) |
| SG02 | `ActionNoticeCompactTests.AttackReadsAsItsBattle` (`ui42-battles`), order and "+N more" in `ActionResultTextTests` |
| SG03 | `ActionNoticeCompactTests.PlayedEventOpensItsCardAndFullRecordOpensTheLog` (`grey`) |
| SG04 | `ActionNoticeCompactTests.FellowshipMoveReadsAsTheHunt` (`ui42-hunts`), secrecy in `ActionResultTextTests` |
| SG05 | `TurnRibbonTests.TheActionPassingToAPlayerShowsTheirRibbonOnlyThere` |
| SG06 | Ibid. (allocation of Hunt and action) and `TurnSignalTests` (battle, Hunt, effect, loading, reconnect) |
| SG07 | `TurnRibbonTests.ReducedMotionFlashesTheConsoleAndTheGlowEndsAtTheFirstStep` |
| SG08 | `GameEndNetworkTests.AFreePeoplesMilitaryVictoryReachesBothWithTheOpponentsNotice` (`fp-military`: result flag, end of both games, no ribbon) and the course of packages below |
| AO01–AO03 | `AutoOpenPolicyTests` (4) |
| GE01–GE03 | `GameEndNetworkTests` (`fp-ring-destroyed`, `sh-military`, `fp-military`) and `GameEndStateTests` |
| TE01 | `GameEndScenarioTests` server; in the client, each TEST-07 scenario ends the game: `fp-ring-destroyed`, `sh-military`, `fp-military` (`GameEndNetworkTests`), `sh-ring-corrupted` (`AutoOpenPolicyTests`) |

<a id="weryfikacja-i-przekazanie"></a>
## Verification and transmission

- EditMode: `ActionResultTextTests`, `TurnSignalTests`, `GameEndStateTests`, `UiArtKitTests`, full
  EditMode.
- PlayMode with server, one target run to task: `ActionNoticeCompactTests`, `TurnRibbonTests`,
  `GameEndNetworkTests` (TEST-07 scripts), `AutoOpenPolicyTests` (C section matrix).
- Server: `GameEndScenarioTests` and script directory.
- Manually by user: "Game end" scenarios on local server 5088 and build in Unity Editor.

Results, commands and commits go to the to-do documents, this document, TESTING and the register. Push only on request.

<a id="plansza-widza--obs-03-2026-10-16"></a>
## board viewer — OBS-03 (2026-10-16)

The viewer does not receive personal signals:
- without the ribbon ‘YOUR ACTION’ and the sign-plate flashing;
- without the decision and notification window for action ‘adversary’;
- the console speaks neutrally who decides;
- The end screen calls the winner ("FREE PEOPLES WIN" / "SHADOW WINS").

The battle and Hunt windows, the region panel and Log windows open the same way as the players, because they affect both sides. Details: [OBSERVER_UX](OBSERVER_UX.md).

<a id="save-replay-na-ekranie-końca--replay-04-2026-10-16"></a>
## Save play on the end screen — REPLAY-04 (2026-10-16)

The end screen of the game has a fixed “Save play” (`game-end-save-replay`) button between “Game log” and “Main menu” and the status line under the buttons:

| Status | Button | Status |
| --- | --- | --- |
| Ready | „Save replay” | — |
| persistence | „Saving…” (wyszarzony) | „Downloading the replay…” |
| saved | „Saved” (wyszarzony) | „Saved to Replays in the main menu.” |
| already saved | ‘Allready saved’ (grey) | „This replay is already in Replays.” |
| error | „Retry saving” | reason: network, disk, server |
| not available | "Save play" (shrinked) | „This game started before replays were recorded.” |

- “Saved” only appears after the library has approved the file.
- Player and viewer present at the end write the same play.
- Exit from board interrupts download and does not move earlier records.
- On board the button is greyed: "You are watching a saved play.".

<a id="efekty-u-widza-i-w-replayu--replay-06-2026-10-16"></a>
## Effects in the viewer and in the play — REPLAY-06 (2026-10-16)

- Live and playing: flights figures, two stages of the Fellowship tracks and the sounds of facts as
  a player.
- Manual step, axle jump, page change, first view and reconnect: state without animation and sound;
  ongoing flights and second stage of the track are interrupted.
- No decision alert and no personal result (details: AUDIO_UX).

<a id="ui-309"></a>
## Observed characters — UI-309 (2026-10-24)

The opponent's character moment and a moving group's identities are observed marks, like the UI-305 trace
([OPPONENT_ACTIONS_UX §6.13](OPPONENT_ACTIONS_UX.md#wdrozone-309)):

- a dark plate with the portrait in its side's medallion and a name, without a glow, pulse, lift or press;
- never a legal target or a choice;
- they ignore the pointer, so the figure under them keeps its own tooltip and click;
- they fade in over 200 ms and out over 400 ms, and appear without fades under Reduced motion.

A dimmed portrait marks a removal. A reaction's source is ringed with the same 2 pt pale-blue trace.

<a id="ui-312"></a>
## Pennants and the opponent's resources — UI-312 (2026-10-24)

The opponent's presentation never acknowledges a notice ([OPPONENT_ACTIONS_UX §6.16](OPPONENT_ACTIONS_UX.md#wdrozone-312)).

- **What marks read.** Only an explicit opening marks read: a click on a pennant, its compact action card, or a pinned
  Log row whose details are complete. It marks exactly the displayed version: the action's steps up to the one shown.
- **Versions.** A later step of the same action returns as a new unread update.
- **What never marks read.** Playback, Skip, a missing history, focus loss and inspecting a card acknowledge nothing,
  so a skipped presentation leaves the same unread notices as a full one.
- **Visual priority of resources.** A Ring's cost shows at its console socket and its die's old → new face in the
  opponent's pool. A Hunt allocation and the roll are one beat in the pool and the Hunt plaque. Draws and discards are
  card backs with a signed count at the opponent's hand count, never an identity.
- **Quiet events.** A pass, a decline, "No card" and a technical completion play no scene.
- **Not choices.** Every mark is an observed trace, never a choice.
