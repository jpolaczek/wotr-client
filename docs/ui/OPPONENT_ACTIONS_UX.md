<a id="opponent-actions"></a>
# Opponent action presentation — specification (UI-302)

**Status: UI-303–UI-312 shipped (§12); UI-313's aggregate acceptance is recorded in §6.17 and waits for the owner's
live review.** It is the authoritative design for the opponent-action
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
- **Objects** (a figure stack, a die, a crest, a track marker, a portrait, a card): the object's own **glow** — since
  UI-314 (owner review 2026-10-24: "armies and characters are not marked by a rectangle around them but glow") a soft
  pale-blue light around the object's own shape (`PresentationGlow`: a zero-offset `drop-shadow` filter, a tight
  core and a wide halo), with no plate, no pulse and no lift, fading in over 200 ms and out over 400 ms. It is never
  the gold ChoiceGlow halo. Before UI-314 this was a 2 pt pale-blue ring; settlements keep that ring.
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

<a id="wdrozone-306"></a>
### 6.10 Shipped in UI-306 (2026-10-23): army movement, attack and capture

Code: `Runtime/UI/Playback` (`StagedBoard`, `ArmyMovementPresenter`, `AttackPresenter`, `ControlPresenter`) and
`Runtime/Map/MapObservedTrace.cs`. These presenters register after the cost die and the camera, so the camera always
settles first.

- **Staged board.** The presented revision as the presentation stages it: a copy of the board's view (made once per
  presentation) in which only recorded facts change, namely the recorded movers relocated or one region's control.
  `BoardScreen.Stage` draws it on the map's markers and settlements. `board.View`, the decision tray, the windows and
  the region panels stay on the presented view. When a beat ends, the staged view becomes the map's figure baseline
  (`MapFigureMotion.Rebase`), so the release moves only what the presentation did not show and keeps the presented
  destinations revealed. The staged board is discarded at the release.
- **March, travel, retreat, advance.**
  1. The source stack is traced (350 ms; 0.7 s since UI-315, §6.19).
  2. Exactly the recorded participants fly from source to destination with the board's own figure motion
     (`MapFigureMotion.Fly`, 0.65–1.05 s, or an in-place arrival under Reduced motion).
  3. Meanwhile the staged source shows the rear guard, and the staged destination its true final composition, dimmed
     until the figures land.
  4. The destination is traced for 0.6 s (0.9 s since UI-315).
  - Successive marches with one die are successive beats on the same staged board with one cost mark (§6.1). A merge
    never doubles the count, and a partial move never hides the rear guard.
  - Field and garrison follow the recorded participants: a retreat into a stronghold lands on the walls, and a sortie
    leaves them in the same region.
  - An advance without participants moves nothing.
- **Attack.** A recorded attack is intent, not occupation. The attackers are traced where they stand (1 s), together
  with the defending force and an outline of the defended region. The battle window opens at the release, at its
  normal host.
- **Capture.** The settlement's gem takes the new controller's colour on the staged board, with a ring (1 s).
  - A settlement that scores shows a delta badge (`RegionDefinition.VictoryPoints`): "+N VP" for the side that takes it
    from its original owner, "−N VP" for the side whose capture is retaken, in that side's enamel.
  - A region without a scoring settlement shows no point.
  - Control and points change only with the recorded control fact, never before.
- **Observed trace on the map.** `MapObservedTrace` draws rings around a force's marker or a settlement, region
  outlines and delta badges. A set appears together (200 ms), follows the camera and the markers, and fades together
  (400 ms). Nothing in it takes the pointer, a tooltip or a cursor.
- **Skip and Reduced motion.** Skip lands the running flights and quiets the next projection
  (`MapFigureMotion.Land`, `Quiet`), so nothing skipped moves afterwards. Skip and Reduced motion end on the same
  field and garrison counts and control as a full presentation.
- **Game end.** The game-end screen opens in the released view, after the presentation's last beat, as before.
- **Checks.**
  - EditMode `BoardFigureChangesTests` (re-base and quiet) and `PresentationCameraTests` (capture points).
  - PlayMode `OpponentArmyPresentationTests` on a mounted board:
    - two marches with one die (exact mover ids, rear guard, merge, one cost mark);
    - an off-screen source framed before any figure moves;
    - a retreat into a stronghold and a sortie;
    - a partial and an empty advance;
    - capture, recapture and a non-scoring capture;
    - the attack hand-off;
    - Skip and Reduced motion ending on the latest counts without a replay;
    - clips at 1366 / 100 % and 1920 / 125 % (`ui306-clip-marches-*`).
  - The network motion tests read the observer's flight during its presentation.

<a id="wdrozone-307"></a>
### 6.11 Shipped in UI-307 (2026-10-23): recruitment, upgrades and political tracks

Code: `Runtime/UI/Playback/FigureChangePresenter.cs` and `PoliticsPresenter.cs`, `StagedBoard.Apply` and `Nation`,
plus the delta badge in `Runtime/Map/MapObservedTrace.cs` and `FigureDelta.cs`.

- **Figure changes.** Each recorded group of one kind in one region is one beat: recruits, an upgrade, a removal to the
  reserve or the casualties, a character entering or transforming, a Companion leaving the Fellowship.
  - The camera frames it (§6.9) and the region's force is traced.
  - The staged board takes the recorded changes, so the stack settles to its true count. New figures land in place
    with the board's arrival accent.
  - A **delta badge** beside the stack shows each kind's figure art, the nation's crest and a signed count ("+3",
    "−1").
  - An upgrade is one replacement (Regular → Elite, no extra figure), and so is a new version of a character.
  - Several regions of one source are successive beats under one cost mark, and equivalent units of one place form one
    entry.
  - A battle's own figure changes stay in its window.
  - The engine records a recruit with the same participant before and after, from `reserve` to the region; the
    classification depends only on the places.
- **Excess units.** The player's own excess choice is their decision on the region's figures. An opponent's returned
  excess is a removal, presented only from its committed step.
- **Political track.**
  - The Politics panel opens read-only for the beat, never while it is live for the player's own decision (a legal
    target on its tab or a station).
  - A panel the player had open stays open; one the presentation opened closes at the release.
  - A panel the player closes during the presentation is not reopened by its later beats.
  - The nation's token is traced. An **activation** lights the crest first, then the token travels its recorded steps
    (an eased 0.6 s `left` transition only while `presenting-politics` is set; a jump under Reduced motion). Since
    UI-315 the settled panel, then the lit row and token, then a 0.7 s slide and a hold (§6.19).
  - **Reaching war** adds the short caption "At war" beside the War station.
  - A before-value the record lacks comes from the presented view.
  - With opponent rows hidden, the trace marks the Politics tab.
- **Controls.** No Recruit, Politics or Continue control is added; only Skip shows while it plays.
- **Hooks for UI-308/UI-309.** Event cards and character abilities reuse these presenters through their recorded
  facts: `FigureChangePresenter` (unit deltas), `PoliticsPresenter` (nation transitions), `TraceSet.Delta` and
  `FigureDelta`.
- **Checks.**
  - EditMode `FigureDeltaTests`.
  - PlayMode `OpponentRecruitmentPoliticsTests` on the shared `PresentedBoardStage`:
    - two distant sites under one source with the camera visiting each;
    - several nations in one region;
    - an upgrade as a replacement, and a removal;
    - activation, then advancement, then war with its caption;
    - a user-open panel kept and a closed one not reopened;
    - Skip on the latest counts and tracks;
    - muted clips `ui307-clip-*` at 1920 / 125 %.
  - `OpponentRecruitmentPoliticsNetworkTests` with two real clients on `sh-muster-recruit` and `sh-muster-politics`.

<a id="wdrozone-308"></a>
### 6.12 Shipped in UI-308 (2026-10-24): the public card and its chained effects

Code: `Runtime/UI/Playback/CardRevealPresenter.cs` and `LayerRing.cs`, plus `CardInspector.PlaceIn`/`Note`,
`ConsoleStatus.PresentSource`/`EndSource` and `PresentationPlayback.Upcoming`.

- **Reveal.** Only a recorded public Event (`ActionCardUse.Event` with an id) introduces a face-up card. Commits and
  draws never do, and combat reveals belong to the battle window.
  - The card opens in the existing CardInspector, read-only: no Play bar, and the status line "Event played".
  - It opens at its own readable width (the inspector's viewport rules; text is never shrunk and nothing scrolls).
  - The used Event half carries the observed treatment: a pale-blue bar and tint. The inspector's own
    `focused-effect` heading was indistinguishable on the board's parchment.
  - It stands at the top of the half of the safe map area away from the action's first effect (the first upcoming cue
    of the action with a region). It never covers the console or that effect.
  - It holds for **3 s**. It is a transient view, so it never pauses itself.
- **Read.** A click on the shown card pins it, and the presentation holds until the player closes it (its × or Esc). No
  Read, Pause or Resume control exists. Inspecting the source card is read-only: it never plays or discards a card and
  never marks a pennant read; pennants keep their manual-read behaviour.
- **Dock.** After the hold the card closes and docks as a source chip in the console trail ("✓ Orcs Multiplying Again").
  - The chip opens and closes the same read-only card, and stays through every effect beat.
  - It is presentation-only: the server projects only the viewer's own trail.
  - It returns, without a new reveal, when a later presentation continues the same action (after a real choice).
  - It leaves at the release.
- **Effects.** Each committed effect plays through its own presenter, in causal order, under that one source: movement
  (§6.10), recruitment, upgrades, removals and politics (§6.11), and later characters, the Fellowship and battle. The
  reveal, the cost and the side name are not repeated per effect.
- **Choices.** A real choice ends the committed presentation, and its decision opens on its own host at the release. No
  preview of what the opponent might choose is invented.
- **Table cards.** A card kept on the table settles at the Effects tab with a brief ring (`TableEntered`). Its later
  removal (`TableRemoved`) is a brief ring at its tile, or at the tab when the panel is closed. Neither is a new
  reveal.
- **Cancelled / No effect.** No history fact records a cancellation or a void effect today, so no such caption is shown
  (§6.4).
- **Hooks for UI-309–UI-311.** `CardRevealPresenter` (reveal, dock and recall) and `LayerRing`.
- **Checks.**
  - PlayMode `OpponentCardPresentationTests` on the shared `PresentedBoardStage`:
    - the reveal (read-only, used half marked, clear of the console and of the first effect), the 3 s hold, the dock
      and both recruitment regions under one source;
    - a reading click held for 4 s and resumed by closing;
    - 1366 × 768 at 125 % with rules text of at least 14 pt, unclipped and without scrolling;
    - Skip during the reveal;
    - table entry and removal;
    - a muted clip with a reading click (`ui308-clip-*`).
  - `OpponentCardPresentationNetworkTests` with two real clients on `sh-muster-event` (Orcs Multiplying Again) and
    `sh-character-event` (Flocks of Crebain, kept on the table).

<a id="wdrozone-309"></a>
### 6.13 Shipped in UI-309 (2026-10-24): characters

Code: `Runtime/UI/Playback/CharacterPresenter.cs`, `CharacterMarks.cs` and `FellowshipSeparation.cs`; the moment and the
identities in `Runtime/Map/MapObservedTrace.cs` (`CharacterMark`); the separation in `ArmyMovementPresenter` and
`StagedBoard.Separate`; and `PresentationCamera.Frame`.

- **Moment.** A character that enters, transforms, leaves the Fellowship without a recorded movement or is removed
  from play gets a short larger moment before its figure change lands.
  - Its portrait stands in its side's medallion over its title ("The Witch-king", "Gandalf the White"), on a dark plate
    above its force. The portrait is the painted one where it exists, otherwise the figure.
  - The plate stays inside the safe map area. With no room above the force, it stands below it.
  - Like every plate of the trace (the delta and VP badges, a group's identities), it stands above every map marker,
    so a neighbouring stack never covers a name.
  - The moment holds for 1 s. Then the figure change plays (§6.11), and the moment stays beside the landing figure
    until the next cue.
  - A removal dims the portrait. The figure then leaves the stack with its "−1".
  - The moment ignores the pointer and adds no control. The character inspector stays reachable through the figure, as
    today.
- **One identity.** A transformation shows the new version once: Strider → Aragorn, or Gandalf the Grey from the
  casualties → Gandalf the White.
  - The figure keeps its id, so the stack never holds two figures.
  - The cost and the delta are shown once.
- **Moving groups.** A movement with characters or Nazgûl names its group beside the destination force as it lands.
  - With an army, the army's units come first as one entry with their count ("×2"). Characters marching with an army
    therefore read differently from a group travelling alone. The Nazgûl follow with their count, then each character
    by its portrait and short name ("Gimli", "Witch-king").
  - The difference holds under Reduced motion, where nothing flies.
  - A plain army march keeps its UI-306 traces.
- **Separation.** A recorded Travel of Companions that are not on the presented board but are in its Fellowship is
  their separation (`FellowshipSeparation`). The board's own figure difference stages it.
  - While the Fellowship's position is known, the Companions fly from its figure, which is traced.
  - With hidden progress, they arrive at their destination and the camera frames only the destination. No route from
    the last known region is suggested.
- **Ability source.** A reaction records its character: an `Ability` cost with the character's definition id. A die
  ability records only its ability id. That id names a character when the public catalog lists it for one identity
  (`CharacterLibrary.AbilityHolder`), as for Gimli's, Boromir's and Legolas's political abilities.
  - The source is ringed on its existing portrait, in the battle window or the Fellowship roster.
  - Otherwise the camera frames its figure on the map (never for the player's own action), and the same moment stands
    at its force. It stays there as the action's compact source while its effects play through their own presenters.
  - Voice of Saruman's ability ids are not in the catalog. Its die stays the only source (§6.1) until the history
    records the character (§9).
- **Timing.** An effect on future dice (a Minion's or Gandalf's extra die) follows the server's own timing. No die is
  invented.
- **Classification fix.** The engine records a character new to the game (a Minion entering) from `fellowship`, its
  default origin. The Shadow has no Fellowship, so a Shadow figure recorded from there is now an Entry (§9.1). A Free
  Peoples character recorded from there still leaves the Fellowship (a Relocation).
- **Controls.** No Enter, Ability, Continue or inspection control is added; only Skip shows while it plays.
- **Hooks for UI-310/UI-311:**
  - `CharacterPresenter` (the moment and the ability source) and `CharacterMarks`;
  - `TraceSet.Portraits`, `TraceSet.Identities` and `TraceSet.Fellowship`;
  - `FellowshipSeparation` and `PresentationCamera.Frame`.
- **Checks.**
  - EditMode `CharacterPresentationTests`, and `OpponentPresentationModelTests` for the entry classification.
  - PlayMode `OpponentCharacterPresentationTests` on the shared `PresentedBoardStage`, which can now watch as the
    Shadow:
    - the Witch-king's entry, with the moment first and inside the safe area;
    - Gandalf the White from the casualties and Aragorn in place, each shown once;
    - Gimli and Legolas travelling together;
    - the Witch-king marching with an army while two Nazgûl travel alone;
    - a separation from a known Fellowship and from a hidden one;
    - Boromir's removal;
    - the Shadow's reaction during the Free Peoples' own action, with the camera kept;
    - Skip;
    - muted clips `ui309-clip-normal-*` and `ui309-clip-reduced-*` at 1920 / 125 %.
  - `OpponentCharacterPresentationNetworkTests` with two real clients:
    - `entry`: Gandalf the White, the Witch-king and Aragorn;
    - `white-rider`: the ring on the battle portrait.

<a id="wdrozone-310"></a>
### 6.14 Shipped in UI-310 (2026-10-24): the Fellowship and the Hunt

Code: `Runtime/UI/Playback/HuntPresenter.cs`, `FellowshipPresenter.cs` and `FellowshipReading.cs`;
`StagedBoard.Hunt`/`Fellowship`/`Guide`/`Leave`; `BoardScreen.StageFellowship`/`StageHunt`/`StageRoster`/`QuietTracks`;
`StepsBeat`; the cue order in `PresentationCueExtraction`; and `IPresentationSounds` in `GameSounds`.

- **Order.** The engine records a move's progress on the move itself, before the Hunt it causes. A revision's
  Fellowship cue therefore plays after that revision's last Hunt cue of the same chain. The paid die comes first, then
  the Hunt, then the tracks, so a move's attempt never moves the progress token ahead of the roll and the tile.
  - Storyboard D6 lists corruption before progress. The shipped AUDIO-06 order (`TrackSequence`) wins: on the journey
    progress comes first, then corruption; in Mordor corruption comes first, then the step.
  - A choice in the middle of a Hunt ends the presentation, and its revision's tracks are shown before the release. The
    player's decision then sees the current tracks.
- **The Hunt window.** It is staged entry by entry from the recorded snapshots and report: the window's own dice, tile
  and report strip.
  - The Hunt's start opens the window as a new Hunt does, with a trace on the Hunt Box plaque.
  - A roll shows its dice and successes for 1 s, with one dice sound. A drawn tile shows face up for 1 s, and the damage
    for 0.6 s.
  - A response marks its object: the Fellowship's Ring, the Guide's or a Companion's portrait, or a table card at the
    Effects tab.
  - A casualty is marked on its portrait, then leaves the roster.
  - A new Guide takes the badge on its portrait.
  - The completed Hunt keeps its result in the open window, as today.
  - The window hosts no choice here: a real response opens at the release.
- **The Fellowship.**
  - A declaration, a reveal or a revealed move flies the figure between the two public regions, and the camera frames
    the new one. Entering Mordor puts the figure on the Mordor track and traces the panel's Mordor row; the track is
    no region, so the camera stays.
  - Hiding or revealing in place marks the plaque's Ring as the symbol changes.
  - The tracks move as separate stages, each tracing its row (or the plaque's value) and sounding its cue once.
    Healing is corruption going down.
  - A hidden move never frames, relocates or traces anything on the map.
- **The Fellowship panel** opens read-only for these beats, by the Politics panel's rule (§6.11). It is never taken
  from the player's own decision, never reopened after the player closed it, and closed at the release only when the
  presentation opened it. An open Hunt window steps aside from it.
- **Once.** What the beats showed is the release's point of comparison (`BoardScreen.StageFellowship`): the released
  view never stages the tracks again, and the map figure does not fly twice. After Skip the latest tracks show at once,
  without a second stage.
- **Sound.** A beat sounds its own fact through the game's sound policy, so muted, background and focus rules apply.
  The view released after the presentation leaves those cues out (`GameSounds.Play`).
- **Controls.** No response, Continue or result control is added; only Skip shows while it plays.
- **Hooks for UI-311–UI-312:** `StepsBeat`, `IPresentationSounds` and `FellowshipReading`.
- **Checks.**
  - EditMode:
    - `OpponentPresentationModelTests`: the Fellowship change follows its Hunt on the recorded `hunt-shadow` fixture,
      and only a public relocation is framed;
    - `GameSoundsTests`: a presented march is not sounded again.
  - PlayMode `OpponentHuntPresentationTests` on the Shadow's `PresentedBoardStage`:
    - a miss;
    - a hit with both tracks in two stages;
    - the Ring response;
    - a reveal's flight;
    - a casualty;
    - a Guide change;
    - a table card's response;
    - hiding and healing;
    - entering Mordor;
    - Skip;
    - muted clips `ui310-clip-1366-*` and `ui310-clip-1920-*` at both scales.
  - `OpponentHuntPresentationNetworkTests` with two real clients on `fp-character-move-fellowship`.

<a id="wdrozone-311"></a>
### 6.15 Shipped in UI-311 (2026-10-24): the battle window

Code: `Runtime/UI/Playback/BattlePresenter.cs`; `RegionalBattlePanel.Present`/`EndPresentation`;
`BoardScreen.StageBattle`; the battle order in `PresentationCueExtraction`; and `Runtime/Map/ObservedDelta.cs`.

- **The window.** After the attack's trace, the new battle's window opens at its normal place, as a new battle opens
  it today (UI-149). This supersedes §6.10's opening at the release. A window the player closed during this battle stays
  closed, and its beats then wait for nothing.
- **The recorded state.** While a presentation plays, the window shows the battle's recorded public state read-only,
  in place of the decision controls, as the battle report draws one: the stages, then both forces with their figures,
  cards, dice and hits. The window's header and place stay. The release gives the controls back to the latest view.
- **Order.** A decision step records its revision's final battle state; the automatic steps after it record the states
  on the way (each side's roll, the rerolls, the losses). Its snapshot plays after theirs, so a round never steps back.
- **Cards.**
  - A committed card stays a back of its deck until the public reveal; no card identity comes from a commit.
  - Both cards of a round turn face up together for 3 s, ringed with the observed treatment and showing their
    initiative.
  - A click opens the card read-only (the battle's own combat view) and holds the presentation until it is closed.
  - A card cancelled later is ringed with its "Cancelled".
- **Dice.** A roll or a reroll rings the dice it changed in their side's row, and they lift for 0.25 s (only the ring
  under Reduced motion). It plays one dice sound and holds 1 s. The faces, hit marks and totals are the window's own.
- **Casualties.** Only committed losses are shown, never an unsubmitted selection.
  - A delta plate at the right of the side's figures shows each kind's art, crest and count; an Elite's downgrade
    shows as its replacement (Elite → Regular). It holds 1 s before the next state removes them.
  - A leader or character lost with its army is ringed on its portrait.
  - A siege extension's figure change is shown the same way.
- **Later rounds** stay in the same window. A retreat, an advance and a capture return to the map through their own
  presenters (§6.10).
- **Camera and controls.** No camera move for No card, Hold, Continue, an empty reroll or a decline. No round-level
  Next or Continue control: only Skip.
- **Hooks for UI-312:** `ObservedDelta` and `BattlePresenter`'s state diff.
- **Checks.**
  - EditMode `OpponentPresentationModelTests`: a decision's final state follows the states on the way, on both recorded
    attack fixtures.
  - PlayMode `OpponentBattlePresentationTests` on the shared `PresentedBoardStage`:
    - the window opening after the attack and the controls returning at the release;
    - a committed back, then both reveals together for 3 s;
    - a read-only card click that holds the presentation;
    - a roll and a reroll;
    - a cancelled card;
    - a removal with a downgrade;
    - the Witch-king lost with his army;
    - a later round in the same window;
    - a retreat and an advance on the map;
    - a siege extension;
    - Skip;
    - a muted clip `ui311-clip-*` at 1920 / 125 %.
  - `OpponentBattlePresentationNetworkTests` with two real clients on `sh-army-attack`: the window opens in the
    presentation, the Shadow's card is a back, and the shared rolls ring their dice before the next decision.
  - `OpponentPresentationNetworkTests.Attack` now expects the window to open once the attack's trace ends.

<a id="wdrozone-312"></a>
### 6.16 Shipped in UI-312 (2026-10-24): resources, quiet events and pennants

Code: `Runtime/UI/Playback/ResourcePresenter.cs` and `PresentationMarks.cs`, `StagedBoard.Die`/`Hand`, and
`BoardScreen.StageOpponentDice`/`StageOpponentSummary`.

- **Visual priority.** Each kind of resource change has one place to show, and only these show:

  | Recorded fact | Where it shows | Beat |
  | --- | --- | --- |
  | A Ring's cost | ringed at its console socket; the new holder shows at the release with the socket's own pulse | 0.6 s (0.7 s, UI-315) |
  | The die a Ring changed | ringed in the opponent's pool, its old → new face under it | 1 s |
  | A Hunt allocation and the action-dice roll | one beat for the step's dice: the rolled dice ringed with their faces, the Hunt plaque ringed as its Eyes count rises; silent — the released view sounds the roll, so a decision passed with it keeps AUDIO-05's one tone | 1 s |
  | Draws or discards | one beat for a run of them at the opponent's hand count: card backs and the signed count, never an identity | 0.8 s (since UI-315 lit 0.7 s first, then changed and held 0.8 s) |
  | A forfeited die | its cost copy (§6.1) | 0.35 s |
  | A pass, a decline, "No card", a technical completion | no scene: nothing is recorded to show | — |

- **Consequences.** An important consequence of a decline still plays once, through its own presenter.
- **Repeats.** A repeated revision never charges a Ring or a die twice: the action's memory keeps its costs.
- **Not interactive.** No resource mark takes the pointer or reads as a strategic choice. The map camera never moves
  for these beats.
- **Pennants.** The pennants, the compact click-open action card, Full record and read persistence are unchanged:
  - only an explicit opening marks a notice read, and it marks exactly the version it displayed;
  - a later step of the same action returns as a new unread update;
  - playback, Skip, a missing history, focus loss and inspecting a card never acknowledge anything.
- **Hand-off to UI-313:** the quiet-event and resource cases above. The notification persistence format is unchanged.
- **Checks.**
  - EditMode `OpponentPresentationModelTests`: ten empty declines make no cue, and a repeated Ring step charges once.
  - EditMode `NotificationReadTests`, `ActionNoticeTests` and `GameSoundsTests`.
  - PlayMode `OpponentResourcePresentationTests`:
    - the Ring at its socket with its die's old → new face, the camera kept;
    - an allocation with the roll as one beat;
    - two draws as one beat of backs and "+2";
    - a discard as "−1";
    - a pass and quiet declines with no Skip target;
    - a forfeit as its cost copy.
  - `OpponentResourcePresentationNetworkTests` on `fp-event-draw`: the full playback and a Skip leave the Free Peoples'
    pennants unread.
  - `ActionNoticeNetworkTests`.

<a id="akceptacja-313"></a>
### 6.17 Aggregate acceptance in UI-313 (2026-10-24): evidence and open checks

UI-313 adds no presentation of its own. It collects the component evidence, adds the cases the components left to the
aggregate, and records what is still open. It changes one behaviour: the opponent's action-dice roll beat is silent
(§6.16), because the released view already sounds the roll with AUDIO-05's single tone
([AUDIO_UX](AUDIO_UX.md#ui-310)).

New code: `OpponentPresentationAcceptanceNetworkTests`, `OpponentPresentationMatrixTests` and
`OpponentPresentationPerformanceTests`. The component scene builders they reuse are internal to the test assembly.

- **Coverage matrix.** "Network" means two real clients on the pinned server and seeder; the other suites play
  recorded cues on a staged offline board.

  | Scope item | Evidence | Result |
  | --- | --- | --- |
  | Two marches with one die | `OpponentArmyPresentationTests.TwoMarchesWithOneDie…`; network `OpponentPresentationNetworkTests.TheOpponentsMarchesPlayInOrder…` | pass |
  | A multi-region event card | `OpponentCardPresentationTests` (Orcs Multiplying Again read, docked, both regions follow); network `OpponentCardPresentationNetworkTests` | pass |
  | Recruitment, upgrade, politics | `OpponentRecruitmentPoliticsTests` (two distant sites, several nations, an upgrade and a removal, activation then advancement); network `OpponentRecruitmentPoliticsNetworkTests` | pass |
  | Character entry and transformation | `OpponentCharacterPresentationTests` (the Witch-king's entry, Gandalf the White, Aragorn); network `OpponentCharacterPresentationNetworkTests` | pass |
  | A complete battle with retreat and advance | `OpponentBattlePresentationTests` (reveal, roll and reroll, losses, a later round, retreat and advance, a siege extension); `OpponentArmyPresentationTests` (a retreat into a stronghold, a partial advance); network `OpponentBattlePresentationNetworkTests` | pass |
  | Fellowship and Hunt | `OpponentHuntPresentationTests` (a miss, a hit with both tracks, the Ring response, a reveal, a casualty, a Guide, a table card, hiding and healing, Mordor); network `OpponentHuntPresentationNetworkTests` | pass |
  | Ring use | network `OpponentPresentationAcceptanceNetworkTests.TheShadowsRingIsCharged…` on `sh-elven-ring`; `OpponentResourcePresentationTests.ARingIsCharged…` | pass |
  | A real opponent response during our action | network `OpponentPresentationAcceptanceNetworkTests.TheShadowsCombatCardDuringOurAttack…` on `fp-army-attack`; `OpponentCharacterPresentationTests.TheShadowsReactionDuringTheFreePeoplesAction…`; `Shadow03NetworkTests.TableBalrog…` | pass |
  | Handoffs: decision host, one sound, game end | `OpponentPresentationNetworkTests.AnAttackOpensItsDecisionsAtTheirBoundaries…`; `SoundResultsNetworkTests` and EditMode `GameSoundsTests`; `GameEndNetworkTests.AFreePeoplesMilitaryVictoryReachesBoth…` | pass |
  | The layout matrix, both sides | `OpponentPresentationMatrixTests` (a long march, an event card, a battle and a Hunt at 1366 × 768 and 1920 × 1080, 100 % and 125 %); `OpponentPresentationLayoutTests.TheCostSkipTargetAndCameraHoldTheirPlacesAtFourLayouts…`; the components' four-layout cases | pass, one finding below |
  | Long card text, crowded armies, wide travel, a moved or closed panel, Reduced motion | `OpponentCardPresentationTests.TheCardStaysReadableAt1366By768At125Percent…`; `OpponentPresentationLayoutTests.AMarchBesideACrowdedRegion…` and `…ReducedMotionJumpsTheCamera…`; the matrix's long march; `OpponentRecruitmentPoliticsTests.APanelThePlayerOpenedStays…` | pass |
  | Late, duplicated or reordered history, missing snapshots, the missing-history fallback | EditMode `OpponentPresentationModelTests` (duplicated, reordered and split pages, a missing intermediate view, a battle without its opening snapshot); `OpponentPresentationQueueTests.AHistoryStillMissingAfterOneSecond…` | pass |
  | An in-flight command, Skip, card inspection, manual pan, focus loss, reconnect | `OpponentPresentationQueueTests` (a private update, Skip, a disconnect or focus loss, a pause); `OpponentPresentationLayoutTests.AWheelZoomDuringTheTravel…` and `…EscClosesAReadCardFirst…`; network `OpponentPresentationNetworkTests` (a reconnect shows the latest state) | pass |
  | Pennants stay unread | network `OpponentResourcePresentationNetworkTests` (full playback and Skip); EditMode `NotificationReadTests` | pass |
  | Privacy | `OpponentCharacterPresentationTests.ACompanionLeavingAHiddenFellowshipArrivesWithoutARoute`; `OpponentBattlePresentationTests.TheCommittedCardStaysABack…`; `Shadow03NetworkTests.TableBalrog…`; the cue contract's EditMode tests (§9.1) | pass |
  | Observer, replay and expansion smoke | the full PlayMode run below | the 23 known main failures, no regression |
  | 1080p at 60 FPS | `OpponentPresentationPerformanceTests` (below) | batch-mode measurement only; open |
  | Normal-speed animation | the components' muted clips (frame sequences) | frames reviewed; live viewing open |

- **Budget.** Every frame of the matrix scenes, at every layout, counts the new controls outside the existing hosts
  (the battle and Hunt windows, the card inspector, the panels, the console trail and the map's markers). It finds
  at most one, Skip, clear of the console, and at most one caption. No Pause, Read, Next, speed or return control and
  no event strip exist.
- **Readability review.** These frames were reviewed at 1366 × 768 at 125 % and 1920 × 1080:
  - the matrix frames (`.artifacts/screenshots/ui313-{march,card,battle,hunt}-<width>-<scale>.png`);
  - the components' muted clips (`ui305-clip-far`, `ui306-clip-marches`, `ui307-clip`, `ui308-clip`,
    `ui309-clip-normal`/`-reduced`, `ui310-clip`, `ui311-clip`);
  - the resource frames (`ui312-*`).

  In each scene the source or resource, the participants, the place and the outcome read from one viewing, with the
  Log closed and sound muted:
  - a march: the cost die, the source's ghost and the destination with its count;
  - a card: its title and texts, then its docked chip and both regions;
  - a battle: both cards, the dice and the losses in the window;
  - a Hunt: the roll, the tile, then the two track stages and the roster;
  - Reduced motion keeps the same marks.
- **Finding.** At 1366 × 768 at 125 % the "Choose an action" chip and the Log button (top left) cover the start of
  the battle window's header (the region name). This placement predates the program: the window is the battle
  window's normal position. It is left to a layout task.
- **Measurements.** `OpponentPresentationPerformanceTests` wrote these to `.artifacts/ui313-performance.json`.
  - **Configuration:** MacBookPro18,3 (Apple M1 Pro, 16 GB), Unity 6000.6.0f1, 1920 × 1080 at 100 %, rendered to a
    texture in batch mode without vsync. No other test run was active.
  - **Results:**

    | Scene | Frames | Mean | p95 | Max | Allocated per frame (mean / max) |
    | --- | --- | --- | --- | --- | --- |
    | Idle board | 120 | 20.5 ms | 32.8 ms | 34.8 ms | 6.1 KB / 24 KB |
    | A long march with camera travel and figure flight | 132 | 19.4 ms | 30.2 ms | 32.8 ms | 21.5 KB / 464 KB |
    | A battle round's reveal and roll | 248 | 20.2 ms | 32.2 ms | 34.1 ms | 7.4 KB / 408 KB |

  - **Reading:** the presentation adds no frame time to the board's own. The allocation peaks are single staging
    frames.
  - **Limits:** batch-mode rendering to a texture runs about 20 ms even when idle, so this run cannot confirm the
    60 FPS target; that needs a player build on a display. Board rebuilds were not counted separately. The presenters
    update only the staged objects (`StagedBoard`, `BoardScreen.Stage*`).
- **Commands.**
  - EditMode: `bash scripts/unity.sh -runTests -testPlatform EditMode -testResults <abs.xml> -logFile <abs.log>`.
  - PlayMode: `bash scripts/test-client.sh -testFilter "Wotr.PlayTests.<Class>[.<Method>]|…"` with
    `WOTR_TEST_DB` set to an isolated database and with `WOTR_CLIENT_TEST_RESULTS`/`WOTR_CLIENT_TEST_LOG`.
  - Every run is muted by the `SilentTestRun` fixture.
  - The pinned rc.11 backend allows about 20 sign-ins a minute. The full run therefore goes in chunks of eight
    classes with single reruns, and `MordorNetworkTests` runs one method at a time.
- **Results.**
  - EditMode: 1075/1075.
  - Full silent PlayMode run: 670 tests; after single reruns 25 failed, the 23 known main failures and two new:
    - `SoundResultsNetworkTests`: the roll beat's sound. Fixed as above.
    - `Shadow03NetworkTests.TableBalrog…`: `MatchTestRoom.NoCard` read a view still held behind the presented
      commit. It now waits for the release.
  - Both are green after the fix. So are `Shadow03NetworkTests` 6/6, `OpponentResourcePresentationNetworkTests`
    (Skip clicked once it is attached), `BattleNetworkTests`, `T22BoundaryNetworkTests` and
    `OpponentResourcePresentationTests`.
  - `OpponentPresentationMatrixTests` 4/4, `OpponentPresentationAcceptanceNetworkTests` 2/2 and
    `OpponentPresentationPerformanceTests` 1/1.
- **Open, for the owner:**
  - viewing the presentation live at normal speed (the clips are frame sequences);
  - the 60 FPS check on a display build;
  - adding this matrix to the server's TESTING (the server repository cannot be committed from this branch);
  - the follow-ups recorded with the components: the Voice of Saruman's unnamed source (§6.13) and the header
    finding above.

<a id="wdrozone-314"></a>
### 6.18 Shipped in UI-314 (2026-10-24): the glow instead of a frame

The owner's live review (2026-10-24), points 6 and 7: "armies and characters should not be marked with a rectangle around
them but glow", and "units the opponent recruits should glow for a moment after they appear".

Code: `Runtime/UI/Playback/PresentationGlow.cs` (`PresentationGlow`, `ObjectGlow`) and `.presentation-glow` in
`Presentation.uss`. It replaces `LayerRing` and the force and Fellowship rings of `MapObservedTrace`. Also
`MapCanvas.MarkerArtwork`, `PresentationMark`, `PresentationMarks.Light` and the new figures' hold in
`FigureChangePresenter`.

- **The glow.** A soft light around the object's own shape (§5): three zero-offset `drop-shadow` filters — a white
  core, a light-blue glow and a wide saturated halo — so it reads on the parchment and on the dark HUD.
  - It fades in over 200 ms and out over 400 ms (a `filter` transition under `.motion-enabled`); under Reduced motion
    it appears and leaves at once.
  - It changes only the object's filter. The object keeps its size, place, pointer, cursor and tooltip, and the
    presentation adds no element.
  - Two marks of one object share its glow, which goes out with the last of them.
- **On the map.** Each of these glows around its plinth, figures, emblems and count:
  - a presented force (a march's source and destination, an attack's two sides, a capture's force);
  - a character's force;
  - the Fellowship's figure at a separation's public origin.

  The glow follows the marker through camera moves and moves to a marker the staged board rebuilds. Settlements keep
  their 2 pt ring; region outlines, badges, deltas, portraits and identities are unchanged.
- **In the HUD.** Each of these lights itself instead of a ring:
  - a Ring's socket, the opponent's changed dice, the Hunt plaque and the opponent's card counter;
  - the Hunt Box tab, the Fellowship's Ring, tracks and portraits;
  - the Effects tab or a table card's tile, the politics token or the Politics tab;
  - a battle portrait.
- **New figures.** Figures that arrive (recruits, an upgrade's Elite, an entering character) land with the 0.45 s
  arrival accent and keep their force's glow for 1.5 s (`FigureChangePresenter.ArrivalGlowSeconds`), with the delta
  badge. A beat that only removes figures still lasts 1 s.
- **Skip and the release** put every glow out at once, together with the beat's marks.
- **Checks.**
  - EditMode: 1075/1075.
  - PlayMode offline, every presentation suite: 74/74, now asserting the glow of the marked object.
  - `OpponentRecruitmentPoliticsTests.NewRecruitsGlowAroundTheirFiguresForAMomentAfterLanding`: no frame around the
    force; it still glows 1.2 s after the landing; the beat holds the landing plus 1.5 s; nothing glows after the
    release.
  - Network: `OpponentPresentation`, `OpponentCard`, `OpponentCharacter`, `OpponentRecruitmentPolitics`,
    `OpponentHunt`, `OpponentBattle`, `OpponentResource` and `OpponentPresentationAcceptance` network tests, and
    `ActionNoticeNetworkTests`: 16/16.
  - Frames reviewed: `ui313-march-*` (source and destination glowing, no rectangle), `ui307-clip-*` (recruits
    landing), `ui314-recruit-glow` (the stack lit 1.2 s after the landing), `ui312-draws` and `ui312-ring` (the
    counter and the socket lit in the HUD).

<a id="wdrozone-315"></a>
### 6.19 Shipped in UI-315 (2026-10-24): mark, then change

The owner's live review (2026-10-24), points 2 and 4: "windows open very fast and you often cannot see what really
happens, e.g. on the political track — the track the opponent chose must be marked and visibly move"; "mark that the
opponent drew a card: their counter clearly lights up and only then changes its number. Everything that changes the state
must be clearly marked: an army's move, a Ring, the hidden state".

Code: `Runtime/UI/Playback/PresentationPace.cs` and the presenters that use it.

- **The pace.** One tempo for every presented change, which supersedes the component durations of §6.10–§6.16:
  1. a panel the presentation opens settles first (0.4 s);
  2. the object that will change glows with its old value (§6.18; 0.7 s);
  3. the value changes with a visible motion while the object stays lit (a token's 0.7 s slide, a count or a face
     turning);
  4. the object stays lit at its new value (0.8 s), then its glow fades.

  Re-marking the same object keeps its glow, so the light never dips between "before" and "after". Skip still ends
  everything at once; the release, the command gate and the decision hosts are unchanged.
- **Political track.**
  1. The panel the step opens settles.
  2. The chosen nation's whole row and its token light up.
  3. An activation lights the crest.
  4. The token slides its steps (0.7 s, eased; a jump under Reduced motion).
  5. Row and token stay lit at the new station.

  "At war" stays the one caption.
- **The opponent's card counter.** It lights up first. Its number then changes while it is lit, with the card backs
  and the signed count beside it, and it holds.
- **Dice and the Ring.**
  - A Ring's socket glows (0.7 s).
  - The die it changed then glows on its old face, turns to the new face with the old → new plate, and holds.
  - An allocation and the action-dice roll light the dice and the Hunt plaque first, then change them.
  - The Ring itself leaves at the release with its socket's own pulse: the console's Rings are not staged.
- **The Fellowship.**
  - Hiding or revealing lights the plaque's state (Hidden, Revealed) before it turns.
  - Each track stage lights its row before the token moves, then sounds its cue once as it moves.
  - A reveal's flight follows the lit state.
  - The panel the presentation opens settles (0.4 s instead of 0.25 s), as does the Hunt window before its Box is
    marked.
- **Armies.**
  - A march glows at its source for 0.7 s before it flies (was 0.35 s) and at its destination for 0.9 s (was 0.6 s).
  - An attack's two sides glow for 1.5 s.
  - A capture lights its settlement before its gem turns, and the points join it as it turns.
  - The battle window the presentation opens settles for 0.4 s more before its round plays.
- **Checks.**
  - EditMode: 1076/1076.
  - PlayMode offline presentation suites (Army, RecruitmentPolitics, Card, Character, Hunt, Battle, Resource, Layout,
    Matrix), with three new cases:
    - `AStepToWarLightsTheTokenBeforeItSlidesAndHoldsItLit`: the panel settles; row and token are lit before the
      token moves; it slides; both are lit at the new station.
    - `TheHandCountLightsUpBeforeItsNumberChanges`: the old number shows while lit for the mark; the number changes
      while lit.
    - `HidingMarksTheRingAndHealingTheCorruptionTrack`: the state lights before it turns and turns while lit.
  - The presentation network suites 12/12.
  - FellowshipMotion, FigureMotion, HuntActivity, SoundResults, ActionNotice and Battle network suites 13/13.
  - `MordorNetworkTests` 7/7, one at a time.
  - Frame: `ui315-politics-held` (the Sauron row and token lit at the new station).

<a id="wdrozone-316"></a>
### 6.20 Shipped in UI-316 (2026-10-24): the card comes from its place and returns to it

The owner's live review (2026-10-24), points 3 and 5: "a played card should not just appear on the screen but slide out of
the place it takes effect from (the cards in hand, the cards on the table and so on)"; "a card that stays on the table
needs an animation that presents it and then puts it back in its place, as if it drifted toward the Effects tab".

Code: `Runtime/UI/Playback/CardFlight.cs` and the reveal beat of `CardRevealPresenter`.

- **Arrival.** The opponent's Event grows out of their hand count in the top bar into its reading place (§6.12), eased in
  and out over 0.6 s. It starts just below the counter, at the counter's height (at most 0.35 of its size) and at 55 %
  opacity.
  - The card's stage lies under the top bar and the Fellowship's plaque, so its first frames emerge from behind them.
  - Only the card's `translate`, `scale` and `opacity` change, so its placement, its width and the board's layout are
    its own.
  - The 3 s of reading start when it stands still.
- **Departure.** After the reading the card shrinks into where it goes, eased over 0.6 s:
  - an Event shrinks into the console trail (or the console's plaque while the trail is empty), where it docks as the
    compact source, and that source glows for 0.8 s;
  - a card that stays on the table drifts to the Effects tab, which then lights up with the table card's own cue.
- **A pinned card.** A click pins the card at once at its place, even mid-flight (the presentation clock stops while it
  is read). Once the player closes it, it docks without a flight.
- **Reduced motion.** The card fades in and out at its place; the destination still lights up.
- **Skip, a reconnect or a missing history** end the flight with the card's own close: nothing is left moving.
- **Unchanged.** Combat cards keep the battle window's reveal (UI-311). No control, caption or window is added.
- **Not covered.** A card coming from the table into a reveal: the presentation reveals only cards played from the
  hand, and a table card's entry or removal is its Effects tab's or tile's glow (§6.12).
- **Checks.** PlayMode `OpponentCardPresentationTests`:
  - `TheCardGrowsOutOfTheOpponentsHandCountIntoItsPlace`: smaller and nearer the counter early, at 1366 × 768 at 125 %;
  - `TheReadEventShrinksIntoTheConsoleTrailWhereItsSourceGlows`: smaller and nearer the trail, then the docked source
    lit, at 1920 × 1080;
  - `ACardThatStaysOnTheTableDriftsToTheEffectsTab`: nearer the tab, then the tab lit;
  - `UnderReducedMotionTheCardFadesInPlace`: no translate or scale;
  - the existing reveal, pin, Skip, table and readability cases, which now read the card once it stands still.

  Also the presentation matrix and `OpponentCardPresentationNetworkTests`. Frames: `ui316-arriving`, `ui316-leaving`
  and `ui316-to-effects`.

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
| Fellowship order | the move's progress is recorded on the move, before its Hunt | the Fellowship cue plays after the revision's Hunt cues (UI-310) |
| Dice, rings, draws | die face before/after; ring ownership in `Resources`; draw count | Ring use is text only: take it from the Ring cost |
| Pass, decline, siege shelter | text only | no scene needed; siege extension is a Figure cost |
| Ordering | `Revision`, `Order`, `ActionId`, `ParentStepId` | revisions coalesce (the client sees only the latest view): stage from facts, never from snapshots |
| Character entry | `Figures` with the character after; a character new to the game is recorded from `fellowship` | a Shadow figure from there is an Entry (UI-309) |
| Ability source | a reaction's `Ability` cost names the character's definition id; a die ability's cost names only its `AbilityId` | the holder comes from the public catalog when only one identity lists it; Voice of Saruman needs the character recorded in the history (server follow-up) |

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
  character replaces the figure), `Relocation` (a Companion leaving the Fellowship; since UI-309 a Shadow figure
  recorded from `fellowship` is an Entry); `Fellowship`, `Hunt`,
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
| UI-306 Army movement (shipped) | §8 army march, attack, retreat; storyboards A and C1–C3 — delivered as §6.10; reusable `StagedBoard`, `MapObservedTrace`, movement/capture presenters |
| UI-307 Recruitment and politics (shipped) | §8 recruitment and politics; the delta badge; storyboard B4–B5 — delivered as §6.11; reusable figure-change and politics presenters |
| UI-308 Public cards (shipped) | §6.2 reveal and dock; §8 event card; storyboard B — delivered as §6.12; reusable reveal/dock/recall and `LayerRing` |
| UI-309 Characters (shipped) | §8 characters — delivered as §6.13; reusable character moment, group identities, separation staging and `PresentationCamera.Frame` |
| UI-310 Fellowship and Hunt (shipped) | §8 Fellowship/Hunt; storyboard D — delivered as §6.14; reusable `StepsBeat`, `IPresentationSounds`, `FellowshipReading` and Hunt/Fellowship staging |
| UI-311 Battle (shipped) | §8 battle round; storyboard C4–C9 — delivered as §6.15; reusable read-only window state, `ObservedDelta` and the battle state diff |
| UI-312 Resources and pennants (shipped) | §8 rings/dice/drawing and pass; §3 rule 4 (manual-read pennants) — delivered as §6.16; reusable `PresentationMarks` and the pool and hand staging |
| UI-316 The card's origin and return (shipped) | the owner's live review, points 3 and 5 — delivered as §6.20; `CardFlight` |
| UI-315 Mark, then change (shipped) | the owner's live review, points 2 and 4 — delivered as §6.19; `PresentationPace` |
| UI-314 Glow instead of a frame (shipped) | the owner's live review, points 6 and 7 — delivered as §6.18; reusable `PresentationGlow`/`ObjectGlow` for UI-315 and UI-316 |
| UI-313 Acceptance (evidence recorded; the owner's live review open) | §11 and the shared acceptance checklist — recorded as §6.17 with the coverage matrix, measurements and open checks |
