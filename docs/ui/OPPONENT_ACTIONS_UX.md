<a id="opponent-actions"></a>
# Opponent action presentation — specification (UI-302)

**Status: planned. Nothing in this document is shipped yet.** It is the authoritative design for the opponent-action
presentation program UI-303–UI-313; until each of those tasks ships, the current behaviour described in
[SIGNALS_UX](SIGNALS_UX.md), [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md) and [UX_SPEC](UX_SPEC.md) stays in force.
Owner decisions of 2026-10-21 (the shared brief of the program); the program was renumbered from UI-280–UI-291 to
UI-302–UI-313 on 2026-10-23, because those numbers already name released client changes.

<a id="cel"></a>
## 1. Outcome and scope

A player following the board can name, for every important opponent action, **the resource that was used, the pieces
that acted, the place, and the actual result** — with the Log closed and the sound muted. The presentation leads the eye
through source → place → action → consequence, then gives the board back.

- **Scope:** the base game (Second Edition) in a player's session, both sides, including the opponent's reaction during
  the player's own action and shared public Battle/Hunt resolutions completed by the player's own click.
- **Out of scope:** expansion-specific choreography, observers and replays (they keep working as today through the
  shared code), the game-end screen (reused as it is), and the separately blocked audio acceptance program.
- **Not a rules change:** the presentation only delivers information the server has already recorded and authorized for
  this player. Rules and randomness stay on the server.

<a id="dzis"></a>
## 2. What ships today (the baseline this program changes)

- The opponent's actions appear only as **pennants** under the top bar, one per action or battle, with the result in the
  tooltip ([SIGNALS_UX](SIGNALS_UX.md), [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md)). A click opens the compact action
  card or the Log; only that explicit, complete display marks the step read (`ActionNoticeModel`,
  `NotificationReadStore`). Hover, time and automatic panels never mark anything read.
- Figure moves of either side animate after the server confirms them (0.65–1.05 s, arrivals 0.45 s), **but only when
  the next view is exactly the next revision** (`BoardFigureChanges`); a coalesced revision jumps without motion.
- The camera never moves by itself, no card opens by itself, and the battle and Hunt windows open in place without a
  camera move. The new decision is rendered in the same frame as the board update (`MatchSession.Update`).
- The pennant feed (`filter=Opponent`) excludes automatic consequences (`Actor=null`): retreats, advances, Hunt
  facts, combat-card reveals, draws.

<a id="zasady"></a>
## 3. Binding rules

From the owner's decisions and the current standards; an implementer does not choose these.

1. **Present before the decision.** An important live action plays before the resulting local decision is enabled;
   the single Skip target finishes it and reaches the current state at once.
2. **Cards.** A played public card is revealed at a readable size for at least 3 s, then docks beside its effects.
   Combat cards, rolls and casualties stay in the existing battle window.
3. **Camera.** It is left where the last map effect ended. A deliberate pan or zoom takes it over for the rest of the
   current presentation; passive edge pan does not.
4. **Pennants and Log stay as they are.** Only an explicit opening marks a notice read; playback, Skip and focus
   changes never acknowledge it.
5. **No scene for nothing.** An empty decline, "No card" or a technical step completion has no scene of its own; an
   important consequence still appears even when a decline triggered it.
6. **Objects host everything.** Dice, figures, cards, portraits, nation rows, Fellowship/Hunt and battle keep their
   existing hosts. The HUD never reflows: the dice pool, hand, console and decision targets keep their size and place;
   unavailable controls stay visible and disabled.
7. **Interaction standards stand.** Single click, action on mouse-up inside, pressed feedback, drag cancels, keyboard
   activation; no double-click, no extra Confirm; even a single legal strategic option needs the player's click
   (UI-279). **A presentation never sends a command and never acknowledges anything.**
8. **Inspection.** Every clickable object keeps its short tooltip (200 ms or focus) describing inspection or
   dismissal — never implying that a historical action can be played. Inspectors open on click or Enter/Space.
9. **Text budget.** Icons, figures, crests, portraits, die faces and signed counts carry the information. At most **one**
   short optional caption per beat (a place or name, "At war", "Cancelled", "No effect"). No sentence repeating a move
   that is visible, no side names, commands, costs or backend identifiers in captions. Card rules text is never
   abbreviated or shrunk.
10. **Controls budget.** At most **one** new control: the transient Skip target (§6.3). No Pause, Resume, Read, Next,
    Continue, Skip all, speed menu, timeline, event strip, unread counter, replay or return-view button.
11. **Accessibility.** Shape and symbol as well as colour; the same facts with sound muted and under Reduced motion.
    Fonts per [TYPOGRAPHY](TYPOGRAPHY.md): Cinzel SemiBold for names and headings, Alegreya for text and numbers; never
    a smaller emergency size.

<a id="wyjatek"></a>
## 4. The limited exception and what it supersedes

The owner's request supersedes the older "no automatic camera" and "no automatic card" wording **only for the live
presentation of an important opponent action (and the shared public resolutions named in §1)**, while it plays. For
that presentation the client may move the camera (§6.5) and show the played public card (§6.2) without a click.
Nothing else changes: hover, tooltips, decisions, window placement, privacy, own actions, the Log, pennants, history
framing, first load and reconnect keep their current rules.

Passages superseded within that limit, once the implementing task ships (each keeps applying to everything else):

| Document | Passage | Superseded by |
| --- | --- | --- |
| [SIGNALS_UX](SIGNALS_UX.md) | the opponent's card action is never opened by itself; the camera never moves alone (top of the document, "Not automatic" lists and AO01) | §6.2, §6.5 (UI-305, UI-308) |
| [UX_SPEC](UX_SPEC.md) | "Only a click of the notifications moves the camera… No automatic view acquisition"; "The arrival of the state, event or reconnect does not center the map" (live presentation only; first load and reconnect still never move it); "card action in the opponent never; the camera does not move alone" | §6.2, §6.5, §7 |
| [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md) | a confirmed move animates "and does not open the preview" | §8 (event card family) |
| [BATTLE_ACTIVITY_UX](BATTLE_ACTIVITY_UX.md), [HUNT_UX](HUNT_UX.md), [FELLOWSHIP_UX](FELLOWSHIP_UX.md), [IN_GAME_UX](IN_GAME_UX.md) | "a new projection / incoming alert / opening does not move the camera" | §6.5 for the live presentation only |
| Tests asserting the old rule (`AutoOpenPolicyTests`, `FellowshipNetworkTests` "Opening a decision does not Locate it", `BattleNoticeAndRecordedActionUseCurrentAnchorsWithoutAutomaticCameraMotion`) | they stay valid for decisions, battle-window opening and history framing; the implementing tasks add the presentation case beside them | UI-305, UI-311 |

<a id="slownik"></a>
## 5. Visual vocabulary: an observed result is never a choice

An observed consequence must not be confused with an offered action. Existing meanings stay:

| Existing treatment | Meaning |
| --- | --- |
| Green / red region edge glow (`map-target-glow`) | a region a click may enter / attack |
| Gold rim on a warm plate pulsing every 750 ms (ChoiceGlow, `eligible-army` halo) | an object a click may choose |
| Gold border with a lift (`selected-force`, `selected`) | chosen in the player's own draft |
| 40 % ghost (`army-ghost`, planned recruits and flights) | the player's own plan |
| Faded figure with ✓ (`moved`) | already marched in the player's own action |
| Darker, one point lower (`pressed`) | the control under the held pointer |

The new **observed trace** is the program's single result treatment, derived from the existing history outline
(`MapActionOverlay`, pale blue `rgba(146, 212, 242, 0.85)`):

- **Regions:** a 2 pt pale-blue outline that fades in over 200 ms, holds for the beat and fades out over 400 ms. It
  never pulses and never fills, and it is hidden while the player drags, pans or has draft targets (as the history
  outline is today).
- **Objects** (a figure stack, a die, a crest, a track marker, a portrait, a card): a 2 pt pale-blue ring with no plate,
  no pulse and no lift, same fade.
- **Delta badge:** beside the affected stack or row, a small dark plate with the unit-type icon or symbol, the nation
  crest and a signed count in Alegreya SemiBold ("+3", "−1"); for an upgrade, the regular and elite icons with an
  arrow. It appears with the change and fades with the beat.
- Every trace element ignores the pointer (`PickingMode.Ignore`), changes no cursor and has no tooltip; the objects
  under it keep their own tooltip and inspection.
- Under Reduced motion the trace appears and disappears without fades, still for the whole beat.

<a id="elementy"></a>
## 6. The fixed parts of every presentation

<a id="koszt"></a>
### 6.1 Cost

- The action's cost is emphasized **once per action**, before its first effect: the die by its stable id in the
  opponent's dice (top bar), a Ring at its holder, a ruler or council token at its row, a card at its source (§6.2).
- If the authoritative pool has already consumed the die, a **visual copy** stands at the die's last anchor for the
  whole action, with an observed trace; when the die was used as another face (Will of the West), the copy shows the
  rolled face, a small arrow and the used face. The copy never reflows the live pool and disappears with the action's
  last beat. A spent die never reappears as available.
- A continuation of the same action (a second march with the same die, the next region of a card) does not emphasize
  the cost again.

<a id="zrodlo"></a>
### 6.2 Source: the card and its dock

- Only a public **Event** or **CombatReveal** fact can show a face-up card; a draw or a CombatCommit shows only a back or
  a count.
- **Reveal:** the card opens in the existing read-only CardInspector (no Play bar, the used half emphasized as
  `focused-effect`), at its normal readable width (336/400, 440/500 for long text), placed in the half of the safe map
  area away from the first effect region and never over the console, the dice pool or a decision host. It holds at
  least **3 s**.
- **Read:** a click on the shown card pins the same inspector; the presentation holds while it is open and resumes when
  the player closes it (its own × or Esc). No Read, Pause or Resume control exists.
- **Dock:** after the hold the card shrinks into the console's existing effect-trail chip ("✓ <card>", UI-297), which
  is the compact source for the rest of the action; a click on the chip opens and closes the card as it does today.
  A card waiting for either player's real choice keeps only this compact source.
- A card kept on the table settles at its Effects tile; its later removal or expiry is a brief trace at that tile, not
  a new reveal.
- A source that is not a card (an ability, a die action) shows its object — the character's portrait or ability
  picture, the die copy — as the compact source; no new chip is added.

<a id="pomin"></a>
### 6.3 The Skip target

- **One transient control:** a 36 × 36 pt round medallion with the "skip" glyph, at a fixed place over the map: the
  bottom-right corner of the map area, 8 pt above the console and 8 pt left of the right column. It covers no HUD
  text and moves nothing. It exists only while a presentation is playing or queued, and is removed when the queue is
  empty.
- Tooltip "Skip presentation"; standard press feedback, mouse-up inside, focusable, Enter/Space activate it.
- **Effect:** finishes the active presentation and every presentation already received, applies the latest
  authoritative state and releases the local decision. Later live events animate normally. No confirmation; it never
  sends a game command, never marks a notice read and never skips a game decision.
- **Esc** does the same only when no modal, inspector, menu, drag, pan or other gesture owns Esc. An Esc that closes
  the card inspector only closes it (and resumes the presentation).
- The click that skips cannot reach a decision under it: the decision released by the skip accepts input from the next
  pointer-down only.

<a id="podpisy"></a>
### 6.4 Captions

- At most one short caption per beat, only for an otherwise unclear subject or outcome: a place that has no visible
  label in the framed view, a character's name if the portrait alone is ambiguous, "At war", "Cancelled", "No effect".
- A dark plate under the subject, Alegreya SemiBold 13 pt (Cinzel SemiBold for a place or name), pointer-ignoring,
  shown for the beat. Never a sentence; never side names, die names, command names or ids.

<a id="kamera"></a>
### 6.5 Camera

- Frame the subject in the **safe map area**: the map minus the top bar and notice bar, the fixed console, the left
  rail and right column, any open panel and the revealed card. Use `CameraState.TryFrame`; if source and destination
  fit at a readable zoom, keep both; otherwise follow the travel; if the subject is already legible in the current
  view, do not move.
- Order: source emphasis (≈350 ms), camera (450–750 ms, eased), then the movement; units never start moving before
  the camera settles.
- **Manual takeover:** a wheel zoom, a right/middle drag or a click-drag pan by the player during the presentation stops
  camera following for the rest of that presentation; passive edge pan is suspended while the camera travels and
  never fights a held gesture.
- The camera stays where the last map effect ended; there is no return move and no return button.
- Under Reduced motion the camera jumps (no travel) to the same framing.
- First load, reconnect, focus return and a late history response never move the camera.

<a id="czas"></a>
### 6.6 Timing

| Part | Normal | Reduced motion |
| --- | --- | --- |
| Source emphasis (die, card corner, object) | ≈350 ms | static trace, 350 ms |
| Camera travel | 450–750 ms | jump |
| Figure movement | 650–1050 ms by distance (arrivals 450 ms), the existing `MapFigureMotion` | in-place arrival accent (`figure-arrival`) |
| Result dwell (trace, badge, caption) | ≈1 s | ≈1 s, no fades |
| Card reveal hold | ≥ 3 s, longer while inspected | ≥ 3 s |
| A whole normal move | ≈2–4 s | ≈1.5–2.5 s |

Several regions of one source are successive beats of that source, not new introductions; the die charge and the card
entrance are not replayed for a continuation.

<a id="ruch"></a>
### 6.7 Reduced motion

The existing preference (`InterfacePreferences.ReducedMotion`, the `motion-enabled` / `reduced-motion` root classes).
No travel, no pulsing, no flashing: each beat shows the same source, place, participants and result as a static frame
with the observed trace and the delta badge, for the same dwell.

<a id="dzwiek"></a>
### 6.8 Audio

The existing subtle cues, once per actual beat, through the current `GameSounds` policy: muted means silent, no
replay after focus or mute returns, first view and reconnect silent. The presentation never relies on a sound.

<a id="wdrozone-305"></a>
### 6.9 Shipped in UI-305 (2026-10-23): cost, camera and Skip

UI-305 registers the first production presenters, so the playback of §7.1 is live in every player's session (observers
and replays are unchanged). Code: `Runtime/UI/Playback`, registered by `OpponentPresenters` in `MatchSession` and the
PlayMode `MountedMatchClient`.

- **Beat order per cue.** Every presenter that shows a cue plays its beat in registration order: `CostEmphasis`, then
  `PresentationCamera`, then the family presenters of UI-306–UI-312.
- **Cost (§6.1).** A die cost shows the observed trace over the die in the presented opponent pool, by its stable id,
  for 350 ms. The pool is never touched, and its die keeps its own tooltip and inspection. If the pool no longer holds
  the die, a copy stands at the die's last known place, or after the pool's last die. A die used as another face adds
  a badge with an arrow and the used face. The mark stays as the action's source symbol until a cue of another action
  begins or the presentation ends. A later presentation of the same action shows no cost again, because UI-303's
  memory never repeats it.
- **Camera (§6.5).** Map families get a camera beat: marches, travel, attack, retreat, advance, figure changes,
  control and a visible Fellowship. Battle, Hunt, card, cost, dice and politics cues play in their own windows and rows.
  Inside a battle only the attack, a retreat, an advance and a capture return to the map; its casualties and other
  figure changes stay in the battle window.
  - **Only the opponent's actions move the camera.** A cue whose revision holds the player's own decision
    (`PresentationCue.OwnCommand`) is a shared result of their own click, such as their own retreat, advance or the
    rolls they completed. It plays, but the camera stays where the player put it.
  - The safe area is the map minus every HUD rectangle the map already avoids (top bar, notices, console, open panels
    and windows, the card inspector) and the left and right columns. Each occluder cuts the area to the largest band
    beside it (`SafeMapArea`).
  - **No move** when every anchor of the cue already lies 48 pt inside the safe area, whatever zoom the player chose.
  - Otherwise `CameraState.TryFrame` frames source and destination together at a readable zoom: at least 1.2 × the fit
    zoom, the threshold below which the map shrinks armies to tokens. If they don't fit at that zoom, the camera frames
    the source, then the destination.
  - The travel eases over 450–750 ms by distance and zoom change (`CameraTravel`); Reduced motion jumps.
  - Edge pan is suspended during the travel (`MapEdgePan.Suspended`).
  - A wheel zoom or a right/middle-drag pan (`MapCanvas.Steered`) ends the travel at once, and the camera follows
    nothing more until the presentation ends.
  - The camera stays where the last map effect left it.
- **Skip (§6.3).** `SkipControl` is a 36 pt round icon button (`actions/skip`) with the tooltip "Skip presentation",
  standard press feedback, focus and Enter/Space. It sits in the presentation layer, 8 pt above the console and 8 pt
  left of the right column. It exists only while `PresentationPlayback.Presenting`, that is while a cue plays or waits;
  a quiet revision's history read shows nothing.
  - A click releases the latest view on the next frame.
  - **Esc** reaches it through `BoardEscape.SkipPresentation`, after every window, menu, gesture and pan.
  - An Esc from inside the card inspector only closes the card.
- **Reading a card (§6.2).** A card the player opens (pins) while a presentation plays pauses the presentation clock
  (`PresentationPlayback.Paused`), and closing it resumes. A card already open before the presentation does not hold
  it.
- **Hooks for UI-306–UI-312.**
  - `PresentationLayer` holds marks above the HUD that ignore the pointer.
  - `ObservedTrace.ClassName` and `Styles/Presentation.uss` give the ring and its fades.
  - `TimedBeat` is a dwell beat.
  - `PresentationCamera.Frames` and `SafeArea()`.
  - `PresentationPlayback.Released` ends per-presentation state.
- **Interpretations (record).**
  - "Legible" ignores zoom, so a player who plays zoomed out is not zoomed in on every opponent action.
  - The source symbol lives for the presentation, not across later presentations of the same action.
  - Only a card opened during a presentation holds it.
  - The camera exception covers only revisions the opponent caused; the player's own click never moves it, even when
    its shared result (a retreat, an advance) is presented.
- **Checks.**
  - EditMode `OpponentPresentationQueueTests`: presenter order, paused clock, `Presenting` and `Released`.
  - EditMode `PresentationCameraTests`: safe-area cuts, travel endpoints and durations.
  - PlayMode `OpponentPresentationLayoutTests`:
    - at all four size/scale combinations: the die's mark over an unreflowed pool, exactly one added control, the
      medallion's size and place clear of the HUD, no caption, both regions in the safe area after the travel, and
      nothing left behind;
    - Reduced motion jumps;
    - an already visible march does not move the camera;
    - a march beside a crowded region (stress board, 1366 / 125 %) is framed clear of the open hand;
    - a die the presented pool no longer holds stands as a copy, with its used face;
    - Skip on the next frame leaves the camera;
    - Esc closes a read card first, then skips;
    - a wheel zoom during the travel wins;
    - frames `ui305-*` in `.artifacts/screenshots`: far, crowded, legible, Reduced motion, spent die, card reading and
      manual takeover, and the far march as clips (`ui305-clip-far-normal-*` and `-reduced-*`, one frame every 80 ms;
      the test environment has no video encoder).

<a id="cykl"></a>
## 7. Lifecycle and command gating

- **One playback coordinator** sits between received views and their visual application (UI-304). It keeps the
  **latest authoritative view** separately from the **presented** state; network receipt, revision intake, receipts and
  recovery continue normally underneath.
- **Gate:** while a presentation plays or is queued, every command path is closed — decision tray, console, keyboard
  (`DraftInput`), map clicks, figure and die callbacks, delayed clicks — through one gate (an extension of
  `TurnInteraction.SelectionBusy` and the battle policy). Inspection, the menu, settings, chat, the Log, pennants and
  connection feedback stay usable.
- **Release:** at the end or on Skip, the coordinator applies the latest view (revision, private version and
  `Decision.Id`), reconciles stale drafts, renders the decision once, and emits its turn signal and sound once.
- **Real choices:** when either player has a genuine choice inside an effect, the current committed beat finishes and
  that choice opens on its existing object host; the presentation keeps only the compact source and resumes after the
  next commit. Nothing waits for a whole action or card to finish.
- **Observing player:** while the opponent decides, the player can browse, pan and inspect freely.
- **First load, reconnect, focus return, an interrupted session:** show the latest state with no catch-up; old events
  stay in the pennants. A history page that is still missing **1 s** after its view arrived releases the current state
  without animation; a late response never restarts a camera move.
- **Duplicates and order:** pages are processed in order, duplicates by stable step, figure and cost ids are dropped,
  and facts newer than the received view wait for it.

<a id="koordynator"></a>
### 7.1 The playback coordinator (UI-304, shipped 2026-10-23)

Code: `Runtime/Application/Playback` — `PresentationPlayback` (pure, clock-driven), `PlaybackBinding` (the board's
16 ms scheduler and `Application.focusChanged`), `ICuePresenter`/`IPresentationBeat` (the presenter hooks).

- **Intake.** `MatchSession` (and the PlayMode `MountedMatchClient`, through the same binding) sends every view the
  connection accepted to the history feed first (`ActionNoticeController.Update`: cues and the read mark of the
  revision) and then to the coordinator. A released view goes through one `Present` chain: `BoardScreen.Apply`,
  `TurnInteraction.Update` (draft reconciliation, decision, turn ribbon), `GameSounds`, the Log and the seen marker —
  so its decision, auto-opened windows, turn signal and sound come once, at the release.
- **Hold.** A newer revision is held while its history is read (`ActionNoticeController.HistoryRead`, at most **1 s**
  after the view arrived) and while its cues play, one beat at a time through the first presenter that accepts each cue
  (a cue no presenter shows takes no time). Views arriving meanwhile replace `Latest`; their cues queue behind the
  current beat; a coalesced revision is never applied on its own. A private-only update (same revision) waits for the
  release while holding and is applied at once otherwise.
- **Gate.** While holding, `TurnInteraction.Hold` closes every new-command path: `SelectionBusy` (map, figures, dice,
  cards, contexts, Characters), `Drop`, `CanChoose`, the `Submit` guard, the tray, the console (Pass, Back, ✓, plaques),
  rings and the battle policy. Inspection, the menu, the Log, pennants and connection feedback stay usable; a command
  already sent keeps recovering its receipt.
- **Release.** At the end the latest view is applied with its revision, private version and `Decision.Id`. **Skip**
  (`PresentationPlayback.Skip`) ends the beat and the backlog and releases on the next tick, so the skip click cannot
  reach a control the release opens; it neither answers a decision nor marks a notice read. A **disconnect**, a
  **reconnect** or a **lost focus** ends the beat and applies the latest view at once (no catch-up), **disposal** ends the
  beat and applies nothing more, and cues of an already presented revision are dropped, so a late page never restarts a
  move. A deferred release that throws is reported (`[wotr] presentation …`) and applied again on the next tick, up to
  three times (QUAL-09), and the gate then opens.
- **Dormant until UI-305.** Without a registered presenter, and whenever the session is not live, every view is applied
  at once exactly as before; UI-304 alone changed nothing a player sees, and UI-305 (§6.9) registered the first ones. The family
  tasks register theirs with `MatchSession.Playback.Register`. A beat stages its change on the presented board, ends by
  `Advance(now)` or at once by `Finish()`, and leaves no override behind.
- **Checks.** EditMode `OpponentPresentationQueueTests` (ordering, history wait, late cues, private versions, Skip,
  rebase, disposal, failed release, presenter choice); PlayMode `OpponentPresentationNetworkTests` with two real clients
  and a stand-in presenter on the test scenarios `sh-army-move` (the source army and the unspent die stay until the
  march's beat ends and nothing flies meanwhile, then the board's figure motion moves it from its source; a second
  march queues behind the first; a reconnect shows the latest state and replays nothing) and
  `sh-army-attack` (no battle window before the attack's beat ends; Skip on the next frame; the Shadow's Combat card
  choice is presented before ours opens; our click sends one command and the shared rolls it completes play before
  the next decision).

<a id="rodziny"></a>
## 8. Presentation families

Every family has a start, an effect, a finish and a fallback. "Host" is the existing object that carries the result.
"Fallback" is what the client shows when the recorded facts cannot represent the transition: the latest state and the
existing history, never an invented intermediate board.

| Family | Start | Effect | Finish | Host | Fallback | Caption (optional) |
| --- | --- | --- | --- | --- | --- | --- |
| **Army march** | cost (§6.1); trace on the source stack | camera frames source and destination; the moving group flies; the rear guard stays visibly at the source | the destination stack settles with its true composition; trace on both stacks fades | army markers, `MapFigureMotion` | no movement fact → both stacks show final state with a trace | none (region labels identify places) |
| **Attack** | cost; trace on attackers and on the defended region | frame both forces; attackers stay at their origin; the battle window opens at its normal host (§8 battle round) | handover to the battle window | army markers, battle window | no attack movement is recorded today: derive attacker and defender from the battle's opening snapshot; if absent, open the battle window as today | none |
| **Retreat / siege / advance** | trace on the affected group | the group moves (retreat, advance) or changes field ↔ garrison (siege entry, sortie) | final position; a real control change and VP delta follow | army markers, control markers, VP counter | final state with trace | "No effect" only when recorded |
| **Recruitment / upgrade** | cost or source | frame the region; new figures appear; an upgrade shows the regular replaced by the elite (not an extra figure); delta badge with icon, crest and count | true final count; several regions are visited in recorded order under one source | army markers, delta badge | final count with badge, no animation | place name only if not visible |
| **Politics** | cost or source | the Politics panel row of the nation opens read-only (without replacing a live decision host); trace on the crest; the token moves its recorded steps | activation (crest lights) and entry into war (token on the war step) are visibly distinct | Politics panel row | final position with trace | "At war" |
| **Event card** | the card reveal (§6.2) with its cost | after ≥ 3 s it docks; its committed effects play in causal order through their own family presenters | the last committed effect; a real choice ends the beat (§7) | CardInspector, console trail chip, family hosts | card shown, effects as final state | "Cancelled" / "No effect" only when recorded |
| **Characters** | the portrait (or ability picture) as source | travel flies the figure; entry places it; a transformation replaces the figure (never two figures); an ability's public effect plays through its family; a removal fades the figure to casualties | final placement; the roster/portrait changes | figures, Characters panel, Fellowship roster | final state with trace | the character's name if ambiguous |
| **Fellowship / Hunt** | cost; trace on the progress track | the progress marker moves only after the Hunt resolves; the Hunt window shows public dice, rerolls and the revealed tile; corruption and progress move as separate beats; a casualty changes its portrait; declaration/reveal relocates the public figure | final tracks; a local response opens after its causal beat | Fellowship panel, Hunt window, Mordor track | final tracks with trace; hidden travel never shows a route | none |
| **Battle round** | the battle window | committed cards stay backs; both reveal together with initiative; the combat half is emphasized; rolls and rerolls animate in their side's row; hits, then committed casualties grouped (an elite downgrade as its replacement) | later rounds in the same window; retreat, siege and capture return to the map | battle window | the window shows the latest state | none |
| **Rings / dice / drawing** | trace on the resource | a Ring changes holder; a die shows before → after face; the opponent's draw shows a card back and the hand count | final counts | top bar resources, opponent dice, hand count | final state | none |
| **Pass / discard / decline** | — | no scene; the pool or status updates where useful | — | dice pool, console status | — | none |

<a id="dane"></a>
## 9. Data mapping and known gaps

The presentation consumes only authorized data: `PlayerView`, `ActionLogStep` (`Costs`, `Movements`, `Facts`,
`Context` with Battle and Hunt links), `BattleReportView`, `HuntView`. It is driven by the **All** feed incrementally,
not by notification sentences or today's unit counts.

| Need | Recorded today | Gap for UI-303 |
| --- | --- | --- |
| Cost | `Costs` on the primary step (die face and used face) | — |
| March, travel, retreat, advance | `Movements` (route, participants); `Figures` before/after | retreat/advance/travel sit on automatic steps (`Actor=null`): read the All feed |
| Attack | `BattleStarted` with the battle snapshot and place | no attack movement: derive from the snapshot |
| Recruitment | `Figures` reserve → region (`Before=null`) | upgrade is two unrelated figure changes: pair by region and nation |
| Politics | before/after on the decision step | automatic political steps carry only after values: take before from the previous presented state |
| Event / combat card | Event `Cards.Use=Event` with id; CombatReveal step with id | CombatCommit carries only the deck back — never use it for a face |
| Battle round | per-die results, rerolls and hits in the battle snapshot; `LossDetails` | no per-roll delta: diff consecutive snapshots |
| Fellowship / Hunt | `FellowshipBefore/After`; Hunt steps with dice, tile, damage, casualty | — (hidden travel stays progress-only by design) |
| Dice, rings, draws | die face before/after; ring ownership in `Resources`; draw count | Ring use is text only: take it from the Ring cost |
| Pass, decline, siege shelter | text only | no scene needed; siege extension is a Figure cost |
| Ordering | `Revision`, `Order`, `ActionId`, `ParentStepId` | revisions coalesce (the client sees only the latest view): stage from facts, never from snapshots |

Privacy: private steps are filtered by the server before paging; the presentation adds no inference (no route for a
hidden Fellowship, no card identity from a commit or a draw, no opponent selection before it is committed).

<a id="kontrakt-wskazowek"></a>
### 9.1 The cue contract (UI-303, shipped 2026-10-23)

UI-303 ships the data layer only; nothing is played yet and the pennants, read markers and Log are unchanged.
Code: `Runtime/Presentation/Opponent`.

- **Feed.** `ActionNoticeController` reads the newest page of `actions?filter=All` first: it is the presentation's
  baseline and its cursor continues the All feed. The pennants still read the opponent's whole history backwards with
  the Opponent filter, so a first load costs one page more than before (the All history of a battle is about five
  times the opponent's in bytes, measured on the recorded attack). Later fetches read the All feed `after` the cursor.
  `ActionNoticeModel` makes pennants only from the opponent's steps, so the pennants and read state are those the
  Opponent feed gave. `OpponentPresentationModel` never reads or marks a notification and never sends a command.
- **Lifecycle.** The first load (and a reload after a discarded cursor) is the **baseline**: remembered, never played.
  `Merge(pages)` takes one fetch and `Advance(revision)` takes each new `PlayerView`; a step newer than the shown view
  waits for it (`Held`), a repeated step is dropped by its id, and the steps of one fetch are grouped by
  (`Revision`, `ActionId`) and ordered by revision and `Order`, so page boundaries, duplicates and arrival order cannot
  change the cues. `CuesReady` publishes the released cues in causal order — UI-304 consumes it.
- **Selection.** A revision group shows the opponent's steps (its reactions during the player's action included), the
  automatic steps (`Actor=null`) of an action the opponent started or of a revision in which it acted, and every
  shared Battle/Hunt step (`Context.BattleId` or `HuntChainId`), also when the player's click completed it (C8, D6).
  The player's own steps and legacy steps release nothing, but they are **remembered**: the attacker's own attack is
  no attack scene for the attacker, and its die no cost.
- **Cue.** `PresentationCue` is immutable and holds copies of its facts: `Id` = `ActionId/Kind/key`, `Kind`, `StepId`,
  `Revision`, `Order`, `Actor`, `BattleId`, `HuntChainId`, `Regions` (in camera order) and one payload (`Cost`, `Card`,
  `Movement`, `Figures`, `Losses`, `Battle`, `Hunt`, `FellowshipBefore/After`, `Nation`, `Control`, `Die`, `Resource`,
  `Outcome`). `ActionPresentation` collects the cues of one action with its `Initiator`, Event `CardId` and `Cost`.
  Within a step the order is cost → card → movement → figure changes → losses → battle → Hunt → Fellowship →
  politics → control → dice → resources → outcome.
- **Kinds and keys.** `Cost` (die or resource; card, figure and free costs have none — the card or figure cue shows
  them); `Card` (public Event, CombatReveal and table entry/removal with the id; an opponent's CombatCommit as a back
  keyed by battle and round); `Draw` (the opponent's draws and discards as counts, no id); `March`/`Travel`/`Retreat`/
  `Advance` (route and participant ids); `Attack` (the battle's opening snapshot — round 1, no committed card, no dice —
  with the attacker region, then the defender region; a battle first seen later, e.g. opened in legacy history, has
  beats only); `Battle` (each later snapshot whose public state changed — UI-311 diffs consecutive ones);
  `Losses` (`LossDetails`, a downgrade with its replacement); `Recruit`, `Upgrade` (a Regular leaving and an Elite of
  its nation arriving in the region — one replacement), `Removal`, `Entry`, `Transformation` (a new version of a
  character replaces the figure), `Relocation` (a Companion leaving the Fellowship); `Fellowship`, `Hunt`,
  `Politics`, `Control`, `Die` (the opponent's dice other than the paying die), `Resource`, `Outcome`.
- **One fact, one cue.** Figure changes a movement or a loss already describes make no figure cue; a producer step
  repeating the primary step's facts, a repeated page and a later continuation of the same action reuse the action's
  memory of released keys, costs and battles, so nothing is shown twice. Both players give a shared step the same cue
  ids (history battle snapshots are public: no card identity before the reveal).
- **Fallback.** Only recorded facts make cues; a step without a representable fact makes none and the board shows the
  latest view. Politics after-only values and per-roll deltas stay with their family tasks (UI-307, UI-311).
- **Fixtures.** `Tests/Editor/OpponentActions/*.json` are recorded from the real engine by the server test
  `OpponentActionFixtureRecorder` (`WOTR_RECORD_OPPONENT_FIXTURES=<folder>`): storyboard A (`march`), B
  (`card-recruit`), C (`attack`, both viewers), D (`hunt`, both viewers) and an opponent reaction during the
  player's move (`reaction`). Each holds the baseline page and every later page with the view revision it arrived in.
- **Checks.** EditMode `OpponentPresentationModelTests`; PlayMode `LiveActivityNetworkTests` records both clients'
  cues (`OpponentCueLog`) and compares them with each other and with both final boards.

<a id="scenorysy"></a>
## 10. Storyboards

Each storyboard lists its beats: time from the start, what is emphasized, the camera, the participants, the host, the
result, when the beat ends, and how Skip and Reduced motion treat it. "FP" and "Shadow" name the sides only here.

<a id="sb-a"></a>
### A. A partial army march and a second march (Shadow observed by FP)

Sauron is at war. Shadow uses one Army die: a group of 3 Regulars and the Elite leaves Dol Guldur for the Eastern Brown
Lands (2 Regulars and the Nazgûl stay), then 2 Regulars march from Moria to Hollin.

| # | t | Emphasis | Camera | Participants / host | Result | Ends when | Skip / Reduced motion |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A1 | 0 | trace on the Army die in Shadow's dice; copy kept if consumed | — | top bar dice | cost known | 350 ms | Skip: latest state / static trace |
| A2 | 0.35 s | trace on the Dol Guldur stack | frames Dol Guldur and the Eastern Brown Lands (both fit) | army markers | source known | camera settled | jump |
| A3 | ≈1.0 s | — | — | the 4 moving figures fly; 2 Regulars + Nazgûl stay in Dol Guldur | destination stack settles to 3 R + 1 E | flight done + 1 s dwell | in-place arrival accents on both stacks |
| A4 | — | — | — | — | if the second march is not chosen yet: the beat ends; the compact source (the die copy) stays; the board is free to browse | the next commit | — |
| A5 | next commit | trace on the Moria stack (the die is not emphasized again) | frames Moria and Hollin | army markers | 2 R settle in Hollin | flight + 1 s | jump / accents |
| A6 | end | traces fade; die copy removed | stays at Hollin | — | the decision (if FP's) opens now | — | — |

Controls: Skip only. Captions: none.

<a id="sb-b"></a>
### B. A card that recruits in several regions (Shadow observed by FP)

Shadow plays *Orcs Multiplying Again* (3 Sauron Regulars in Dol Guldur and 3 in Mount Gundabad).

| # | t | Emphasis | Camera | Participants / host | Result | Ends when | Skip / Reduced motion |
| --- | --- | --- | --- | --- | --- | --- | --- |
| B1 | 0 | trace on the paying die | — | top bar dice | cost known | 350 ms | static |
| B2 | 0.35 s | the card opens read-only, Event half emphasized, in the half of the map away from Dol Guldur | — | CardInspector | card readable | ≥ 3 s (longer while the player inspects it) | Skip: latest state / no fade |
| B3 | ≈3.4 s | the card shrinks into the console trail chip | frames Dol Guldur | console trail chip | compact source | 450–750 ms | jump |
| B4 | ≈4.1 s | trace on Dol Guldur | — | the stack grows; badge "+3" with the Regular icon and Sauron crest | true count | 1 s dwell | static badge |
| B5 | ≈5.1 s | trace on Mount Gundabad | frames Mount Gundabad | badge "+3" | true count | 1 s dwell | jump |
| B6 | end | traces fade | stays at Mount Gundabad | — | FP's decision opens | — | — |

If a target cannot take the recruits (stacking), the excess choice is its own decision on the existing region figures;
its result appears only after it is committed. Controls: Skip only. Captions: none (both places carry labels).

<a id="sb-c"></a>
### C. An attack through public card reveal and losses (Shadow attacks, FP responds)

Isengard and Rohan are at war. Isengard's army attacks from Orthanc the Rohan army at the Fords of Isen. Both sides
play a Combat card.

| # | t | Emphasis | Camera | Participants / host | Result | Ends when | Skip / Reduced motion |
| --- | --- | --- | --- | --- | --- | --- | --- |
| C1 | 0 | trace on the Army die | — | top bar dice | cost known | 350 ms | static |
| C2 | 0.35 s | traces on the Orthanc stack and the Fords of Isen | frames both | army markers | attack known; attackers stay at Orthanc | camera settled + 0.5 s | jump |
| C3 | ≈1.4 s | — | — | the battle window opens at its normal host | battle visible | window open | — |
| C4 | — | Shadow's card shows as a back | — | battle window | — | **FP's real choice**: the presentation ends its beat; FP chooses a Combat card on the existing host | FP commits |
| C5 | after both commits | both cards turn face up together; initiative seal; combat halves emphasized | — | battle window (click opens the read-only card) | cards known | 3 s | Skip: latest state |
| C6 | — | Shadow's dice roll in its row; hits marked; FP's roll in its row | — | battle dice strip | hits known | 1 s dwell | static faces |
| C7 | — | — | — | — | **FP's real choice**: FP chooses its own casualties on the existing host | FP commits | — |
| C8 | after commit | the committed casualties leave their columns (an elite downgrade as its replacement) | — | battle window, army markers | composition updated | 1 s | static |
| C9 | — | — | — | — | the next round stays in the window; a retreat or advance returns to the map (§8 retreat) | — | — |

Controls: Skip only (while C1–C3, C5–C6, C8 play). Captions: none.

<a id="sb-d"></a>
### D. A Fellowship move, its Hunt and a genuine player response (FP's own move)

FP moves the Fellowship with a Character die; the Hunt (2 Shadow dice in the Hunt Box) succeeds and draws a tile
with 2 damage; FP must respond.

| # | t | Emphasis | Camera | Participants / host | Result | Ends when | Skip / Reduced motion |
| --- | --- | --- | --- | --- | --- | --- | --- |
| D1 | 0 | FP's own click: the die is spent as today | — | console, dice pool | own feedback | immediate | — |
| D2 | ≈0.2 s | trace on the Hunt Box dice | — | the Hunt window opens at its normal host | Hunt started | window open | — |
| D3 | — | the Shadow's dice roll in the Hunt window; successes marked | — | Hunt window | roll known | 1 s dwell | static faces |
| D4 | — | the drawn tile turns face up | — | Hunt window | damage 2 and its icons known | 1 s dwell | static |
| D5 | — | — | — | — | **FP's real choice**: the response row (Ring, Guide, companion, table cards) is enabled | FP commits | — |
| D6 | after commit | corruption moves on its track; then progress moves +1 (separate beats) | — | Fellowship panel tracks | tracks final | 2 × 0.6 s | static marks |
| D7 | — | a casualty, if chosen, leaves the roster | — | Fellowship roster | roster final | 1 s | static |

The hidden route never shows on the map. Controls: Skip only (D2–D4, D6–D7). Captions: none.

<a id="przeglad"></a>
## 11. Review against the shared checklist

- **Counts:** every storyboard adds exactly one new control (Skip) while it plays and none afterwards; captions: A 0,
  B 0, C 0, D 0. Card reading uses the existing inspector.
- **Four layouts:** at 1366 × 768 and 125 % the logical board is 1093 × 614 pt; with the 44 pt top bar and the 140 pt
  console the safe map height is about 430 pt, which takes the CardInspector at its normal width (already verified for
  hand cards in all four layouts) beside a framed region; at 1920 × 1080 both fit with room to spare. The Skip
  medallion sits over the map's bottom-right corner, covers no HUD text and moves nothing. No font is reduced.
- **Reduced motion:** every beat keeps its source, place, participants and result (§6.6, §6.7).
- **Card reading:** no Pause, Resume or Read button; reading holds the presentation and cannot play the card or send a
  command.
- **Quiet commands:** a decline or "No card" never gets its own scene, but a public reveal or result it causes is shown
  (C5 follows FP's own commit).
- **Final state:** after a full playback or a Skip, the board, resources and notification-read state are identical.

<a id="przekazanie"></a>
## 12. Handoff to the implementing tasks

| Task | Takes from this specification |
| --- | --- |
| UI-303 Semantic events (shipped) | §9 data mapping and gaps; the All feed; grouping by step, revision and ActionId; privacy — delivered as §9.1 |
| UI-304 Playback and synchronization (shipped) | §7 lifecycle and gate; release; real choices; first load, reconnect and missing history; consumes `CuesReady` (§9.1) — delivered as §7.1 |
| UI-305 Camera, source, Skip (shipped) | §6.1–§6.6; §5 observed trace; the manual takeover; registers the first presenters (§7.1) and puts the Skip target and Esc on `PresentationPlayback.Skip` — delivered as §6.9 |
| UI-306 Army movement | §8 army march, attack, retreat; storyboards A and C1–C3 |
| UI-307 Recruitment and politics | §8 recruitment and politics; the delta badge; storyboard B4–B5 |
| UI-308 Public cards | §6.2 reveal and dock; §8 event card; storyboard B |
| UI-309 Characters | §8 characters |
| UI-310 Fellowship and Hunt | §8 Fellowship/Hunt; storyboard D |
| UI-311 Battle | §8 battle round; storyboard C4–C9 |
| UI-312 Resources and pennants | §8 rings/dice/drawing and pass; §3 rule 4 (manual-read pennants) |
| UI-313 Acceptance | §11 and the shared acceptance checklist |
