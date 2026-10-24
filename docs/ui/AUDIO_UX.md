<a id="dyskretne-dźwięki--audio-00audio-08"></a>
# Discrete sounds — AUDIO-00–AUDIO-08

<a id="cel-zlecenie-i-granice"></a>
## Purpose, order and boundaries

User's agreement with 2026-10-15: sounds are intended to enrich the gameplay and help recognize the action or need to act, maintaining the smooth handling of the long batch. A subtle board, paper and wood were selected, and in the background only the required decision and end of the game. Added the start of battle, the light steps of Fellowship and the heavier metallic slide Ring. The ring stands for the Corruption (Corruption) tag, operating both sides.

**The latest decision:** when the same effect moves Fellowship and changes Corruption, both animations are performed in succession and we play both sounds. This replaces the earlier selection of the Play only Ring.

AUDIO-00 has saved this specification and task. The implementation order with 2026-10-15 included AUDIO-01-AUDIO-08; implementation describe the sections “implementation AUDIO-0x”, and acceptance AUDIO-08 section. The status is in the central votr-server registry. Scope: current base game, Windows and macOS, without music, continuous background, new system notifications, observing other saved batches or changes to the rules, Contracts and API server.

<a id="katalog-efektów-i-kanały"></a>
## Effect and Channel Folder

The name of the `SoundCue` client is in English. Each cue has a clear channel, priority and set of clips; we do not recognize it from the UI label text. The effects are short, spatially fixed, independent of camera position.

| Cue | Event and sound | Channel | Owner of integration |
| --- | --- | --- | --- |
| `UiClick` | Quietly taping wood after an effective click or local selection. | Effects | AUDIO-04 |
| `CardInspect` | Soft rustle when manually opening or closing the card preview. | Effects | AUDIO-04 |
| `CardMove` | One rustle for approved selection, play or discard, also several cards at once. | Effects | AUDIO-05 |
| `DiceRoll` | Short sound of dice on the table, once per full throw or metastatic action, fight or Hunt. | Effects | AUDIO-05 |
| `ActionAccepted` | A fuller tap after taking your own command when there is no more appropriate effect. | Effects | AUDIO-05 |
| `BattleStarted` | From AUDIO-11 great war drums under the murmur of the army and far shortness of iron, no corner, to 900 ms; once on the new battle on both players. | Alerts | AUDIO-06, AUDIO-11 |
| `FellowshipAdvanced` | Two light, silent steps of shoes, to 400 ms; actual progress of the trip or Mordor. | Effects | AUDIO-06 |
| `CorruptionChanged` | Heavy metallic slide with suppressed resonance, up to 400 ms; increase of Corruption and treatment. | Effects | AUDIO-06 |
| `DecisionRequired` | A gentle metallic tone when the game starts to require the player's decision after waiting. | Alerts | AUDIO-07 |
| `ActionFailed` | Short, muffled tone after a conscious failed attempt requiring correction or manual re-renewal. | Alerts | AUDIO-05 |
| `Victory`, `Defeat` | Quiet chords to one second, consistent with the player's perspective. | Alerts | AUDIO-07 |

The paper, clicks, steps and Ring retain 2–3 very similar variants for the often repeated effects. We recognize the heavier Ring after lower timbre and resonance, not volume jump. The sounds are to be leveled, without overshooting, long reverberation, sharp peaks and noise at the beginning or end. The WAV files are attached locally to the client; the manifest describes the origin, variants, length and assignment of the cue. The game does not download or generate sounds during the batch.

<a id="gesty-fakty-i-cisza"></a>
## The gestures, facts and silence

- Local click plays after the actual gesture is done: release within the control or equivalent
  Enter/Space. Sam `pressed`, hower, focus, drag cancel and the unavailable control do not play.
- The special effect replaces the usual click. The sending control command does not add clicks
  before the later sound of its result. The same applies to the final target and legal drop.
- Approval, card, dice and tracks result exclusively from the accepted command or new safe facts,
  never with draft, preview, just sending a request or starting technical animation.
- One move of many cards and a throw of many dice give one proper effect. A new projection of
  identical values is still a new event; a refresh of the same results is not.
- Custom card and publicly revealed events of the opponent can play according to the projection.
  Hidden declarations cards Fights, private elections and undisclosed identities cards do not become
  an audio guide.
- Quiet remain the tooltips, automatic bookmarks, pan/zoom, other figurine movements, counter
  changes, opponent flags and viewing earlier results. Manual opening of the historical card may
  have a rustle preview, but it does not repeat its playing or pitching.
- No separate gong for any change of the phase or the turn number. The required decision includes
  the beginning of your own action and the response after waiting for your opponent. The next steps
  of your own and automatically omitted choices, e.g. No card without legal card, remain toneless.
- Audio error affects the apparent problem after player's attempt, once a try. We do not signal any
  field validation, preview or automatic retry.

<a id="bitwa-i-sekwencja-torów"></a>
## battle and track sequence

`BattleStarted` is recognized after the new real battle and its ID, not after displaying the window. New round, changing the mode of the same battle, losing the event without battle and manually reopening do not repeat the cue.

Shoe steps require an increase in `Progress` or a real `MordorStep`. The stopped attempt, the reset of progress on the declaration, the undoing, the same input/turnover of the track and the first view do not succeed in the march. Changing Corruption in any direction gives one metallic effect to settle, regardless of the number of fields.

The common effect changing both tracks has two more presentations**:

1. On a regular track: the progress of Fellowship and the steps of the shoes, then the change of
   Corruption and metallic slide.
2. In Mordor: First the settlement of Corruption and metallic slide, then the actual progress of
   Fellowship and shoe steps. The order follows the decision of Hunt before moving on the track.

Each sound starts with its animation. The second animation and its video start after the first presentation; the effects do not overlap. The whole pair does not exceed 900 ms. Panel Fellowship and the tag on the map are representations of one event and do not double the sound. We do not open the panels or move the camera only for audio.

This is a limited presentation sequence, not delaying rules or sending commands: state, legal options and winner come immediately from the current projection. We do not wait to settle the game at the end of the clip. If Mordor moves are stopped or not found in the approved result, we do not create fictional animation and steps.

With the Reduced motion, both changes are signaled by a short distinction of the appropriate tag, with both sounds, without smooth moving. When muted, the visual order of both changes is still valid. The closed panel or invisible track does not cause it to open.

<a id="priorytety-powtórzenia-i-cykl-życia"></a>
## Priorities, repetitions and life cycle

For a simple solution we choose one effect in order: end of the batch → error → start of battle → required decision → Ring → Fellowship → dice → card → approval → click. Start of battle can replace the tone of the decision of the same event.

**Exception is the common change of both tracks:** we keep both stages in order above. Click, approval, card and dice If the common effect also ends the batch or transmits a new decision, we play one more important end/note signal after pair, without delaying itself UI This short sequence does not create a queue of other outstanding events.

Besides this sequence, we play one clip at a time; more important can be less important and the rest omitted. The click has a interval of at least 100 ms, a preview noise of at least 250 ms. The limit does not remove the second stage of the agreed track sequence.

- identifier batch, commands/step, decision and battle protect against repetition by different
  controllers, retrieve and refresh the view. Simply comparing icons, results of dice or positions
  in pixels is not enough.
- First view, entry into the finished batch, catch-up after reconnect, re-receipt re-opening,
  history and layout redevelopment are quiet. There is no chord replay after manually opening the
  game end screen.
- Also when muted and in the background we update the reference point; return focus or turn on sound
  does not play the omitted effects.
- Newer track change, exiting a party or reconnect invalidate an unfinished sequence. The current
  state is set without a queue of old moves, and the old callbacks do not speak in the new batch.
- No clip or audio device blocks the game. One module belongs to the application life cycle; the
  event controller is connected to one party.

<a id="settings-i-gra-w-tle"></a>
## Settings and background game

Sound section in existing Settings: main Sound switch, Effects and Alerts sliders, Background alerts and listen button. UI labels in English, local record according to the previous preferences. By default Sound enabled, Effects 40%, Alerts 60%, Background alerts enabled. Older scale, motion and screen settings remain preserved. Moving the slider does not create continuous beeping; listening starts consciously.

Without focus you can hear only the new required decision or the end of the currently open batch. battle requiring its own choice gives the usual `DecisionRequired`; the very beginning of battle, both tracks and other effects remain silent. The background filter works before priority selection. Disable background alerts means complete silence without focus. Without repeating reminders, system notifications and catching up on return.

<a id="źródła-i-integracja"></a>
## Sources and integration

The source of the sound requirements is the user's agreement, not a new interpretation of the rules. Mechanics describe LOGIC B02/B06/B11/B12/B15/B17/B18/ B19/B20 in votr-server ( BASE-2 . 4.1 p. 18–23 , 28–29 , 36–44 Existing presentation contracts: [SIGNALS_UX](SIGNALS_UX.md) , [FELLOWSHIP_UX](FELLOWSHIP_UX.md) , [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md) , [HUNT_UX](HUNT_UX.md) , [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md) and [MAIN_MENU_UX](MAIN_MENU_UX.md) .

Integration uses `TurnSignal`, current `PlayerView`, adopted receipts and safe `ActionFacts`. It does not parse history descriptions, reads full server status and does not change the randomness of the rules. The presentation layer recognizes and groups events; the play module is responsible for cue, channels, clips, limits, sequence and life cycle.

<a id="scenariusze-i-właściciele"></a>
## Screenplays and owners

| Scenario | Receipt condition | Owner |
| --- | --- | --- |
| AU01 | Complete catalog of local clips, variants, origin and length limits; listen to the timbre and volume. | AUDIO-01 |
| AU02 | One cue support, priorities, no overlap and late queue, limits 100/250 ms, no blocking of the game. | AUDIO-02 |
| AU03 | 40/60% settings, save and migrate without losing other preferences, listen and immediately mute. | AUDIO-03 |
| AU04 | In the background only decision/End, filter before priority; return without catching up. | AUDIO-03 |
| AU05 | Click/Enter/Space/drop give the right single effect; cancellation and inaccessibility are silent. | AUDIO-04 |
| AU06 | Manual preview card has rustles, automatic panels and history do not play events. | AUDIO-04 |
| AU07 | The accepted operation cards gives one effect, rejected no success game; hidden data does not affect audio. | AUDIO-05 |
| AU08 | The whole roll/reroll gives one effect; identical new game result, duplicate of the same throw silent. | AUDIO-05 |
| AU09 | Success only after acceptance, error once on conscious attempt, retry/recovery without replay. | AUDIO-05 |
| AU10 | The new battle plays once on both sides; the round, window opening and event loss do not repeat the signal. | AUDIO-06 |
| AU11 | The actual progress of Journey/Mordor gives steps; Stop, reset, undo and change the layout silently. | AUDIO-06 |
| AU12 | The increase and decrease of Corruption give one metallic slide regardless of the number of fields. | AUDIO-06 |
| AU13 | The common effect is given by two consecutive animations and both sounds once, in order of Journey/Mordor; without additional approval. | AUDIO-06 |
| AU14 | The sequence works at Reduced motion and mute; the newer stan/reconnect/wyjt removes old callbacks. | AUDIO-06 |
| AU15 | Moving to your own action/reaction gives you a signal; your own next steps and auto-skip remain without it. | AUDIO-07 |
| AU16 | The live end of the game gives the right chord once; loading the finished game and reopening the game silent. | AUDIO-07 |
| AU17 | First load, old view, reconconnect, repeated receipt and history do not play back the backlog. | AUDIO-02, Integration AUDIO-04–AUDIO-07 |
| AU18 | All scenarios have evidence; targeted regression, listening and background behavior on Windows/macOS. | AUDIO-08 |

<a id="kolejność-wykonania-i-odbiór"></a>
## Order of execution and acceptance

After a separate implementation order: AUDIO-01 → AUDIO-02, then AUDIO-03–AUDIO-07 according to the register's dependencies, at the end of AUDIO-08. The technical state of the ready itself does not start this work.

Each task provides tests to preserve its boundary. acceptance aggregate real-world connections command a server with a cue observation and animation order, without mandatory audio file analysis in network tests. You also need a real listening session: a call test does not prove the quality of the sound. Windows/macOS , fokus/Minimisation , calm analysis board and intensive handling cards/battle The lack of environment or listening to it remains an open limit, not counted PASS - According to WORKFLOW without mandatory drop and local builds; existing artifacts can be used for system tests after a separate execution order. This document does not declare the performance of these tests.

<a id="moduł-odtwarzania--wdrożenie-audio-02-2026-10-15"></a>
## Playback module — implementation AUDIO-02 (2026-10-15)

- `Runtime/Audio` (client):
  - `SoundCue` and `SoundChannel`;
  - `SoundCues`: priorities from this document (CardInspect right above UiClick), 100/250 ms and cue
    intervals heard in the background;
  - `SoundRequest` (cue + event key), `SoundSequence` (time steps and one final signal),
    `SoundLevels`;
  - `SoundPolicy` (pure logic on the clock from the outside), `SoundCatalog` (`audio-catalog.json`,
    clips loaded with first use);
  - `UnitySoundOutput`: two sources of 2D; a new clip on one, the previous one is extinguished on
    the other by 40 ms, so there is no crack on the replacement or mute. The client builds a scene
    in the code, so the output adds `AudioListener` when it is not there;
  - `SoundModule`: one owner for the application in `InterfaceController` (`Sounds`), updated in
    `ClientBootstrap.Update`. The project incorporates the built-in Unity `com.unity.modules.audio`
    module.
- `SoundPolicy` rules:
  - the event key (part, command/step, decision, battle) plays once per batch. It is also remembered
    when muted and in the background, so nothing is catching up. The memory includes the last 512
    keys and `BeginGame` cleans it;
  - The mute and background filter is working before priority. Without focus, you can only hear
    DecisionRequired, Victory and Defeat, as long as the background alerts are enabled. The focus
    loss mutes the playing other clip;
  - one settlement (`Resolve`) only plays the most important allowed cue;
  - One video game at a time. More important one saves less important one less important one is
    dropped in the course of the game. Nothing gets in line;
  - variants play in succession, so the same clip never repeats twice in a row;
  - sequence (`Sequence`) plays each stage at its time, regardless of priority and limits, then at
    most one final signal. The newer sequence, `CancelSequence` and `BeginGame` remove what is left;
  - Turn off the sound immediately extinguishs the playing video.

<a id="ustawienia-i-tło--wdrożenie-audio-03-2026-10-15"></a>
## Settings and background — implementation AUDIO-03 (2026-10-15)

- `InterfacePreferences` version 3 has `SoundEnabled`, `EffectsVolume`, `AlertsVolume` and
  `BackgroundAlerts` (default enabled, 40%, 60%, enabled).
  - Settings in the 1 and 2 versions retain scale, motion, window mode and resolution (since the 2
    version), and the sound gets the default values.
  - Saved 0% and the sound is disabled after reboot. Only the level outside 0–100% returns to the
    default.
  - The levels change as 5%.
- Settings, Interface column, under interface settings (`SoundSettingsSection`):
  - Sound;
  - Effect and Alerts sliders with a percentage;
  - Background alerts (tooltip: only required decision and game end);
  - Test sound.

The slide slide moves nothing. Test sound plays ActionAccepted and after 0,3 s DecisionRequired, on the current levels. When the sliders sound is switched off, the background alerts and test are greyed out at their places. The change goes immediately to `SoundPolicy.Levels`, and the turn off of the video.
- Background: `Application.focusChanged` sets `SoundPolicy.Focused` (also when minimizing; the game
  starts with focus). The background filter with AUDIO-02 only passes DecisionRequired, Victory and
  Defeat, and the omitted events do not return after focus. `runInBackground` in ProjectSettings is
  already enabled, so the client can play these signals in the background.

<a id="gesty--wdrożenie-audio-04-2026-10-15"></a>
## Gestas — implementation AUDIO-04 (2026-10-15)

- `PressFeedback` (UI-113/114) reports `Began` when the pressure starts on the controller, and
  `Completed` when it ends with a gesture: release inside the still active control or release
  Enter/Space. The cursor output, drag, cancel and inactive control do not end the gesture. Hover
  and focus do not begin the pressure.
- `GestureSounds` (one per interface) makes a snapshot on `Began` before the control works (the
  button with the intercepted indicator works before `Completed`). After `Completed`, in the next
  bar, looks at what the gesture has done:
  - sent command (`TurnInteraction.CommandSending`) — silence, because the sound will give the
    result (AUDIO-05/06);
  - changed the view of card (`CardInspector`: visibility and card) — the rustling of `CardInspect`
    instead of a click, also when opening card from history;
  - board omitted it as a quick second click after a step (UI-142, `DraftInput.LastPressSuppressed`)
    — silence;
  - in other cases, `UiClick`.

One gesture gives one sound, not separately in the controller, parent and inspector. Limits 100/250 ms come from AUDIO-02. The preview card opened automatically (without gesture) is silent.
- `GameSounds` (one for open batch, in `MatchSession`) transmits to the module the number of
  commands sent, the status of the card preview and the omission of the gesture. Starts the batch
  without remembered events, and when closed cancels the unfinished sequence. Menu and rooms play
  simple clicks.
- Clicking the map area is not a button and remains silent (opening the region panel does not play).

<a id="karty-kości-przyjęcie-i-błąd--wdrożenie-audio-05-2026-10-15"></a>
## card, dice, party and error — implementation AUDIO-05 (2026-10-15)

- `SoundFacts.Between(przed, po)` (pure presentation) reads only the player's projection. Each event
  has a key, so a new throw is known after the event, not after the results:
  - dice action turn: dice faces from zero to "cast out", key according to turn;
  - a round kick of battle (both sides together, one sound) and each of its spreads: a key according
    to battle and a round, so a new round with the same results plays again;
  - Hunt projection and spread;
  - card: own hand (choice, play, discard, also several cards — one rustle) and a public table and
    discard of the opponent. The number of cards in the hands of the opponent does not play, so the
    hidden declaration card of the fight is silent.
- `GameSounds` (one per batch):
  - one solution is a new projection plus an accepted response to its own command. It plays the most
    important cue and `ActionAccepted` itself only if nothing more special happened — whether the
    first future response or the projection;
  - The rejected command gives one `ActionFailed` together with the existing message;
  - the recovery of the waiting response of command is silent;
  - first view and catching up after reconnect (any condition other than Connected) only set the
    reference point.
- `TurnInteraction` provides `ICommandEvents` (`CommandSending`, `CommandAnswered`,
  `GestureSuppressed`) and `GameSounds` only uses this interface.

<a id="bitwa-i-sekwencja-torów--wdrożenie-audio-06-2026-10-15"></a>
## battle and track sequence — implementation AUDIO-06 (2026-10-15)

- `BattleStarted`: new `Battle.Id` in the projection (`battle/<id>` key), once for both players. New
  round, mode change, close and reopen the window do not repeat.
- `TrackSequence` (Clean presentation):
  - march is the growth of Progress outside Mordor or the further step on the Mordor track. Entrance
    to the track, reset on declaration, withdrawal and Stop are not a march;
  - Corruption counts both ways;
  - a single change gives one cue track;
  - both at once give two stages: on the journey track first Fellowship , then Corruption ; in
    Mordor first Corruption The second stage begins after 0,38 s (end of the first video), and the
    pair with a possible end signal shall be in 0,9 p.
- `BoardScreen` shows a pair in the Fellowship panel and on the map (marker and Mordor track):
  - First `FirstStage`, or second track still on the old value, and after 0,38 s full view;
  - the state, election and winner come from the screening, only the image;
  - the newer view during the first stage is immediately shown in full;
  - The closed panel is not opening, the camera is not moving.
- With the Reduced motion tag does not move, only on 0,3 s increases (`track-changed`), in turn for
  both tracks and with both sounds. On mute, the order of the image stays.
- `GameSounds` plays pair as `SoundSequence` (both cue once, without a click, reception, cards and
  dice of the same solution), with at most one decision signal or ending after pair (AUDIO-07).
  Newer projection cancels unfinished pair. Without focus both tracks silent (background filter).

<a id="decyzja-i-koniec-partii--wdrożenie-audio-07-2026-10-15"></a>
## Decision and end of the batch — implementation AUDIO-07 (2026-10-15)

- `TurnSignal.Passes(przed, po)` is a common condition for UI-167 ribbon and sound: a new decision
  of your own right after a decision that was not a player in an ongoing game. Sound includes every
  type of one: the beginning of action, the phase of Fellowship, the allocation of Hunt and the
  reactions in battle, the Hunt and the effect of card. The ribbon stays with the types of UI-167.
  The next steps and the selection omitted by the server (which never reached the player) are
  silent. Key: `decision/<Decision.Id>`.
- `Victory`/`Defeat`: changing the running batch to the finished one (Key `end`), from the player's
  perspective. First view of the finished batch, View board, GAME OVER and Log do not play the chord
  again.
- Fugitives:
  - new battle with player decision: battle jaws play in the foreground, in the background the
    filter rejects it and the decision tone game;
  - dice kick passing action: for the player who is now running, the decision tone game (higher
    priority), for the second throw;
  - a track pair with decision or end: both stages, then one signal (`GameSounds.Trailing`), without
    waiting the interface.
- No reminders, no system notifications, no catch-up after the focus comes back.

<a id="odbiór--audio-08-2026-10-15"></a>
## acceptance — AUDIO-08 (2026-10-15)

The proof matrix. “Auto” means passing test and “open” means an unexecuted part that we do not count. EditMode tests are in `Tests/Editor`, PlayMode in `Tests/PlayMode`. “Server” means real command on the local test server.

| ID | Proof | Result |
| --- | --- | --- |
| AU01 | `AudioCatalogTests` (complete, channels, import, length, edges); generator manifesto; listen to the first version | auto PASS ; re-hearsal of five amendments open |
| AU02 | `SoundPolicyTests` (priority, keys, 100/250 ms, one clip, no queue); `SoundModuleTests` (directory clips, one owner) | auto PASS |
| AU03 | `InterfacePreferencesTests` (v3, migration, 0%); `SoundSettingsTests` (right away, game test, slider silent, grey); `InterfaceSettingsTests` | auto PASS; level listening opened with AU01 |
| AU04 | `SoundPolicyTests` (backend filter prior to priority, no catching up, background sequence) | Auto PASS; Windows/macOS Open Trials |
| AU05 | `GestureSoundsTests` (click and Enter once; hower, release outside, drag, inactive, command and omitted click silent) | PASS auto; legal drop without separate sound test (plays its result) |
| AU06 | `GestureSoundsTests` (speak instead of a click) | auto PASS; automatic silent preview from the design (sound only from gesture), without separate test |
| AU07 | `SoundFactsTests` (own hand, public table/reject, not number of cards opponent); `GameSoundsTests` (rejected without success) | PASS auto; card on server not checked |
| AU08 | `SoundFactsTests` (action, round and relay battle, Hunt); `SoundResultsNetworkTests` (turn kick once, server) | PASS auto; battle and Hunt to-run on server unverified |
| AU09 | `GameSoundsTests` (adopted once in both orders, rejected once, recovered silently); `SoundResultsNetworkTests` (adopted, server) | auto PASS; rejection and loss of response on server unverified |
| AU10 | `TrackSequenceTests`; `BattleSoundNetworkTests` (once both, reopen, server) | auto PASS |
| AU11 | `TrackSequenceTests` (March only at step; reset, input, Stop silent) | auto PASS |
| AU12 | `TrackSequenceTests` (Corruption in both directions, one cue) | auto PASS |
| AU13 | `TrackPairTests` (stage and cue register: travel and Mordor, without application); `SoundPolicyTests` (sequence) | PASS car at fixed views; pair on server unconfirmed (random tiles) |
| AU14 | `TrackPairTests` (Reduced motion, newer state); `SoundPolicyTests` (shut, cancel); `GameSounds.Dispose` | auto PASS |
| AU15 | `DecisionSoundsTests`; `SoundResultsNetworkTests` (allocation and action, server); `TurnRibbonTests` | auto PASS |
| AU16 | `DecisionSoundsTests`; `GameEndNetworkTests` (Victory/Defeat at both, View board, reopen, server) | auto PASS |
| AU17 | `GameSoundsTests` (first view, catch-up, retrieved response); `SoundResultsNetworkTests` (reconnect); `GameEndNetworkTests` (final batch) | auto PASS |
| AU18 | Full EditMode and regression PlayMode below | auto PASS; open listening and system tests |

Regression 2026-10-15 on the client's `792b615` client's clone with the local test server (separate packages with breaks, because the server limits the number of requests):

- full EditMode 786/786;
- Local sound: `SoundModuleTests`, `SoundSettingsTests`, `GestureSoundsTests`, `TrackPairTests`,
  `InterfaceSettingsTests` 11/11;
- sound with server: `SoundResultsNetworkTests` 1/1, `BattleSoundNetworkTests` 1/1,
  `GameEndNetworkTests` 3/3;
- signals and windows: `TurnRibbonTests` 2/2, `AutoOpenPolicyTests` 4/4;
- gestures kart: `CardGestureNetworkTests` 1/1;
- Fellowship, Mordor and Hunt: `FellowshipMotionNetworkTests` 1/1, `MordorNetworkTests` 7/7 (4 one
  more time, because the first run stopped the limit of requests `TooManyRequests`),
  `HuntResponseNetworkTests` 3/3, `MordorLayoutTests` and `FellowshipLayoutTests` 10/10;
- recovery (client and server reboot, lost response): `InGameContextNetworkTests` 3/3 (ring, tile
  Corruption, victory) and `BoardCompletionContextNetworkTests` 1/1.

Open limits acceptance (not included):

- re-hearsing of five improved sounds (CardInspect, corner BattleStarted, steps, Ring, lower
  decision) by the user on the listening page with current clips. The same listening session
  includes the recognition of important signals at low volume and a calm and intense sequence;
- a real focus and minimization attempt on Windows and macOS. The background filter logic is tested,
  but not system events;
- The server did not check: cards, battle and Hunt throws, rejection and loss of response and a pair
  of tracks. These cases have EditMode tests or tests at constant views.

<a id="niższe-szeleszczące-efekty--audio-10-2026-10-15"></a>
## Lower, rustling effects — AUDIO-10 (2026-10-15)

User comments: "too high, much sounds like bamboo; they are to be more rustling, less stinging". Reason: Click, receive, position card and dice were formed from several suppressed sines (wood mode), which gives an empty, tonal ‘tok’. The card preview had sharp 3–8 kHz cracks.

- Effects are generated by ambient noise (`thump`, `rustle` in the generator):
  - Click: soft tap in felt with a touch of card edge;
  - preview card: rustler of the 350–3600 Hz card, without cracks;
  - card movement: move to dress and paper pad instead of crackling wood;
  - dice: deaf reflections on the felt mat and short rolling;
  - reception: round, deaf, pawn-knocking on the dress with a shelette;
  - Steps: gravel cartilage below (800–2800 Hz).
- Signals shall retain tone, but shall be lower and darker:
  - battle: iron and bar for 2,4 kHz, horn unchanged;
  - Ring: 2,2 kHz filter;
  - Decision: bell G4 → C5 (previously B4 → E5), without high partile, 3,2 kHz filter;
  - error: notes A3 → F ⁇ 3;
  - Game over: darker string.
- Lengths, volume (BS.1770), true peak ≤ −1 dBTP, channels, file names and grains unchanged;
  generator in 4 version.
- Measurement before and after (average on cue; tonality is the share of energy in narrow peaks
  above smoothed spectrum):

| Cue | Tonality | 2–4 kHz | > 4 kHz |
| --- | --- | --- | --- |
| UiClick | 99,6% → 12,6% | 0,0% → 3,8% | 0% → 0% |
| CardInspect | 8,1% → 8,2% | 29,7% → 16,6% | 51,8% → 0,2% |
| CardMove | 32,6% → 13,3% | 28,8% → 5,0% | 1,4% → 0,1% |
| DiceRoll | 60,1% → 8,9% | 50,0% → 1,9% | 3,1% → 0% |
| ActionAccepted | 100% → 44,2% | 0% → 0,1% | 0% → 0% |
| FellowshipAdvanced | 31,6% → 46,1% | 3,3% → 1,0% | 0,5% → 0% |

The tone of the steps increased because after the removal of the high cartilage the low "heave weight" (75 Hz) dominates rather than the resonating tone.
- The user listening to the new version is open (the listening page of the 3 version).

<a id="katalog-i-pochodzenie--wdrożenie-audio-01-2026-10-15"></a>
## Catalogue and origin — implementation AUDIO-01 (2026-10-15)

The user implementation order from 2026-10-15: "First implement the AUDIO tray ... we want these sounds to be of high quality and pleasant to the ear".

- Origin: only procedural synthesis in `graphicalassets/audio/generate.py` and `synth.py`
  (numpy/scipy, versions in `requirements.txt`), without recordings, taken samples and services.
  Permanent grains give the same files at each start. `manifest.json` in this directory saves grain,
  length, volume, true peak, edges, DC and SHA-256 each clip.
- Sound techniques:
  - wood: damped fashion and short transjent;
  - Paper: band noise grains and dark pink noise;
  - card: moving band filter and position;
  - dice: reflections with hard material mods and table resonance;
  - shoes: damped heel impact and shuffle sole;
  - Ring: heavy metal fashion with grind and settling, suppressed by 4,5 kHz filter;
  - battle: metal blades with rumbling and low drum;
  - decision: bell with bell proportions, quarter up;
  - error: two soft notes down;
  - Game over: arpeggio Tissue (Karplus–Strong).

All in a short wooden reverb.
- Files: WAV PCM 16-bit mono 48 kHz in `Resources/Wotr/Audio/<cue>-<n>.wav`, together 25 files (900
  kB). `audio-catalog.json` assigns a cue to the channel, clips and the longest time. Import Unity:
  PCM, Decompress On Load, preload (without delay of first play), without normalization and without
  the 3D flag.
- Alignment: Weighted volume K ( BS . 1770 ), calculated on the active part of the clip. The results
  have − 21 ...− 27 LUFS (click − 24 , preview card − 27 ), alerts − 17 ...− 20 LUFS True peak ≤ − 1
  dBTP. Game channels (Effects) 40% , Alerts 60% The ring is a heavier colour (a honey) 412–3472 Hz,
  filter), not volume (− 21 as steps − 22 ).
- Track sequence: steps 0,38 s + Ring 0,38 s = 0,76 s, in the limit 900 ms. The start battle takes
  0,88 s (from AUDIO-11 drums, see below).
- File Analysis (generator and `AudioCatalogTests`):
  - edges 0 and DC  <  5 · 10 ⁻ ⁴;
  - no over-drives;
  - length in limits;
  - import settings.

Spectrograms of the first version showed the cracks of cut-out mods (`action-failed`, steps), the rustling sys to 12 kHz and the too sharp doorbell input. Fixed: the end of each source, the 5,5–6 kHz filter for paper, the 9 ms bell input.
- Listen to the user 2026-10-15 (hearing page with all variants, both track and stage orders, 40/60%
  channels):
  - without any notice: UiClick, CardMove, DiceRoll, ActionAccepted, ActionFailed, Victory, Defeat;
  - CardInspect: "more rustling, it can be more like paper than card" — 0,24 s, dense micro-sharp
    1,5–7 kHz and cracklings;
  - BattleStarted: “a little more chaotic, as if there were a lot of iron”, then “good would be the
    sound of a trumpet/war horn”. Now it is a horn (98 Hz; variant 2 is a call 98 → 147 Hz) with the
    rise of brightness, high-rise, vibratory and breath, over chaotic iron, barbed and drum strokes.
    It continues 0,88 s, because the held horn tone does not fit in 500 ms;
  - FellowshipAdvanced: ‘not like steps’ — steps again as noise: heel stroke, finger transfusion
    after ~50 ms and gravel cartilage, without tone;
  - CorruptionChanged: "deeper sound, it sounds like something is tapping on the thermometer" —
    176–1372 Hz fashion, 0,9–1,7 kHz dust, 68 Hz deafness, 2,6 kHz filter;
  - DecisionRequired: "not that high" — octave lower (B4 → E5).

The accepted sounds are bit in bit unchanged. The re-hearsal of five fixes is open and belongs to acceptance AUDIO-08. The file analysis itself does not replace the listening.

| File | Cue | s | LUFS | dBTP |
| --- | --- | --- | --- | --- |
| `ui-click-1.wav` | UiClick | 0.08 | -24 | -9.8 |
| `ui-click-2.wav` | UiClick | 0.08 | -24 | -8.5 |
| `ui-click-3.wav` | UiClick | 0.08 | -24 | -7.9 |
| `card-inspect-1.wav` | CardInspect | 0.24 | -27 | -14.0 |
| `card-inspect-2.wav` | CardInspect | 0.24 | -27 | -15.8 |
| `card-inspect-3.wav` | CardInspect | 0.24 | -27 | -14.7 |
| `card-move-1.wav` | CardMove | 0.27 | -23 | -9.1 |
| `card-move-2.wav` | CardMove | 0.27 | -23 | -7.0 |
| `card-move-3.wav` | CardMove | 0.27 | -23 | -10.6 |
| `dice-roll-1.wav` | DiceRoll | 0.48 | -21 | -4.5 |
| `dice-roll-2.wav` | DiceRoll | 0.48 | -21 | -2.1 |
| `action-accepted-1.wav` | ActionAccepted | 0.17 | -21 | -5.2 |
| `action-accepted-2.wav` | ActionAccepted | 0.17 | -21 | -4.9 |
| `battle-started-1.wav` | BattleStarted | 0.88 | -17 | -1.0 |
| `battle-started-2.wav` | BattleStarted | 0.88 | -17 | -2.6 |
| `fellowship-advanced-1.wav` | FellowshipAdvanced | 0.38 | -22 | -5.2 |
| `fellowship-advanced-2.wav` | FellowshipAdvanced | 0.38 | -22 | -5.8 |
| `fellowship-advanced-3.wav` | FellowshipAdvanced | 0.38 | -22 | -3.2 |
| `corruption-changed-1.wav` | CorruptionChanged | 0.38 | -21 | -7.1 |
| `corruption-changed-2.wav` | CorruptionChanged | 0.38 | -21 | -7.9 |
| `corruption-changed-3.wav` | CorruptionChanged | 0.38 | -21 | -7.4 |
| `decision-required-1.wav` | DecisionRequired | 0.70 | -19 | -6.7 |
| `action-failed-1.wav` | ActionFailed | 0.26 | -20 | -8.6 |
| `victory-1.wav` | Victory | 1.00 | -17 | -4.0 |
| `defeat-1.wav` | Defeat | 1.00 | -18 | -5.8 |

<a id="muzyka-innych-aplikacji-i-game-mode-macos--audio-09"></a>
## Other applications and Game Mode macOS — AUDIO-09

WoTR is to interact with music and videos played in other applications. 2026-10-15 application: on macOS 15.6.1/M1 Pro and built-in speakers 48 kHz YouTube interrupts and shakes while playing with the system mode on. User confirmed that the game mode on the still open game removes the problem. The installed `0.2.0-rc2` package declares the category of the game but does not contain Game Mode keys. The reviewed Core Audio logs did not confirm underrun/overload; the exact internal system error has not been established.

[Apple describes](https://support.apple.com/en-lamr/105118) priority CPU/GPU for the game and limit background activity. For applications that do not require such optimization [recommends that metadata resign](https://developer.apple.com/documentation/bundleresources/information-property-list/lssupportsgamemode). In this turn-on game we choose compatibility with the sound of other applications:

- `MacGameModeBuildPolicy` supports the common callback unit after macOS build, including menus,
  Build Profiles and CI. In the main `Contents/Info.plist` sets the `false` bool for
  `GCSupportsGameMode` (macos 14/15) and `LSSupportsGameMode` (the current Apple key). Other
  metadata, the ID Bundles, the game category and the availability of the full screen are retained.
- After the change, the letter renews the ad hoc signature by `MacOSCodeSigning` Unity module. The
  mechanism does not require system `codesign` During the cross-build on Linux or MacOS module
  during Windows build. No signature module or incorrect plist stops build. Signer Unity opens the
  progress bar “Code signing app” which closes only its own steps build Unity. Callback was called
  from this callback to stay on the screen after successful build and as a window “Hold on... /
  Importing assets” blocked the Editor (user notification from 2026-10-15 ) Therefore, after the
  signature `finally` cause `EditorUtility.ClearProgressBar()` Write down. XML use UTF-8 without BOM
  and external declaration DTD empty `[]` : native test found that the signer Unity and Apple parser
  respectively reject these variants of Mono serialization.
- Packing checks both keys in the input Bundle, the isolated installer and the update package. It
  does not accept the lack of a key, `true`, the number and text of `"false"`. Velopack continues to
  sign the ready-to-installed Bundle after adding its own files.

This is a fix for the next builds. The current installation is not modified; the player can disable the game mode for WoTR from the macOS menu that remembers this selection. The player effects, sampling, volume and system audio services are not changed. The plist/signature tests and the player's automatic tests do not replace listening to another package; AUDIO-08 retains the limitations of listening and system tests.

Verification AUDIO-09 : Full EditMode **848/848 PASS** ( 0 skip), including 10 build policy cases. Test uses real-world copies of Unity stubs for x64 and arm64; both Bundles after change and reprocessing undergo native `plutil -lint` and `codesign --verify --deep --strict` This is a check of metadata and signature, without starting the game nor acceptance Intel runtime. Python test release tools: **34/34 PASS**. Aimed PlayMode `SoundModuleTests;SoundSettingsTests` : **3/3 PASS**, 0 skip (clips, levels, mute, save settings and delete player). Artefacts: `.artifacts/audio09-edit-accepted.xml` and `.artifacts/audio09-play.xml` No full build, installer or CI Linux; the revised version will be listened to after the next user build.

<a id="nowy-początek-bitwy--audio-11-2026-10-16"></a>
## New Start battle — AUDIO-11 (2026-10-16)

The user's application from 2026-10-16: "new sound for starting battle, now sounds like from a comedy from the years 50". A synthetic horn with a high-rise driveway, vibrator and a call for quinta up sounded like a trombone from comedy, so the horn and the tune disappeared (generator in the version 5):

- `war_drum`: a big drum impact, i.e. 84–92 Hz skin with a tone falling by 20%, two non-harmonized
  components (×1,59 and ×2,14) which carry a small speaker impact, a deaf baton hit to 420 Hz and a
  skin slapping 250–1200 Hz;
- variant 1: two strokes (0 and 0,34 s), variant 2: three (0; 0,17; 0,42 s);
- Under them, the increasing murmur of troops (pink noise 140–900 Hz and 90–240 Hz) and
  `distant_clash`: the long, dampened iron short circuit to 1,8 kHz, just after the last impact;
- room: tail 0,35 s, filter 1,8 kHz; −17 LUFS so far, true peak −1,0 and −2,6 dBTP.

Measurement of the band (both variants): approx. 15% energy in 150–300 Hz and approx. 4% in 300–800 Hz, nothing above 2 kHz. The old horn had the centre of gravity of the spectrum 357 Hz, the new one around 160 Hz. The other 23 files are bygone without change. New files are on the listening page (version 4). The reception is decided by the user; if the horn is to return, it is only as dark, far away, without the pull-up and melody.

<a id="widz-i-replay--replay-06-2026-10-16"></a>
## Spectator and replay — REPLAY-06 (2026-10-16)

- board viewer and player has the same tones of facts as board player (`GameSounds` with
  `NoCommands`), but without the alert of his own decision (`DecisionRequired`) and without personal
  "Victory"/ "Defeat".
- They play only new facts live and play step. The first view, reconnect, manual step, axle jump and
  page change only set the reference point (`GameSounds.Rebase`) and interrupt the late pair of
  sounds.
- Joint move Fellowship and change Corruption: two stages and both sounds in the same order as the
  player (Journal shoes → metal, Mordor metal → step). Jumping inside cancels the second stage.
- Quiet, Effects/Alerts and Reduced motion act as a player and do not stop playing.

<a id="dzwiek-po-odtwarzaniu--ui-304"></a>
## Sound after a presentation — UI-304 (2026-10-23)

`GameSounds` takes a view only when the playback releases it
([OPPONENT_ACTIONS_UX §7.1](OPPONENT_ACTIONS_UX.md#koordynator)), together with the board and the turn interaction:
the decision sound and the turn ribbon of a view held behind an opponent presentation come once, after it, and a
coalesced revision makes no sound of its own. Per-beat sounds belong to the family presenters (§6.8). Without a
registered presenter views are released at once, so today's sounds are unchanged.

<a id="ui-310"></a>
## Beat sounds of a presentation — UI-310 (2026-10-24)

A presentation's beat sounds its own fact when it shows it: a Fellowship track stage (march or corruption) and the
Hunt's roll ([OPPONENT_ACTIONS_UX §6.14](OPPONENT_ACTIONS_UX.md#wdrozone-310)).

- **Policy.** The sound goes through `GameSounds.Play` and the usual policy: muted means silent, a background window
  hears nothing, and a first view or a catch-up plays nothing.
- **Order.** The track stages follow the same order as AUDIO-06: on the journey march first, then corruption; in
  Mordor corruption first, then the step.
- **No repeat.** The view released after the presentation leaves those cues out, so a fact sounds once. A pair the
  presentation already played is not played again as AUDIO-06's two-stage sequence.
