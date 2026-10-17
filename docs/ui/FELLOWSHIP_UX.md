<a id="drużyna-i-polowanie--docelowy-interfejs"></a>
# Fellowship and Hunt — target interface

The design with 2026-10-07, written in UI-36. Specification and description of the interaction; the status of the execution is determined only by [registry](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/tasks/INDEX.md). It develops [U09](UX_SPEC.md) and [plan Hunt](HUNT_UX.md). UI uses English labels and simple interchangeable icons.

<a id="graficzne-tory--ui-79"></a>
## Graphical tracks — UI-79

UI-79 replaces textual indicators F01/F02 with two parallel rows. Journey and Corruption have a 13 field from 0 to 12, common scale edges and a separate symbol and an exact value. The travel marker shows Hidden or Revealed; the state is also written with a short label. Progress over the printed scale remains accurate next to the final field marked "+". This is not the limit of legal movement. In Mordor only the upper track changes to six fields 0–5, from gate to fire; the settlement of the caf still precedes the tag movement. The warp does not promise the win of FP at step 5.

Sources: BASE-2.4.1 pp.36–39,43–44 and board BOARD-2E. No track point is the objective of action or drag handle. Existing Move/Hide, declaration, Guide, separation and Hunt options retain their seats, costs and legality. During the route and Hunt both tracks remain visible. Full name Guide and its legal description remain available; the composition portraits retain full names in the description and visible brief signatures. Shadow does not recognize undisclosed versions of the Council by graphics or tooltip.

The Fellowship shortcut uses symbols, names and exact values instead of long sentences. The panel prefers 380 px and breaks the line composition; the Hunt pair has a common upper edge and still respects the handheld pozycje/reset. tracks update existing cells without delayed animation, also after reconnect and at Reduced Motion. Short descriptions use the common RuntimeTooltip: from UI-80 appear after 200 ms hovera or immediately after the seal. They are non-interactive and only use the data available to the recipient.

The exchangeable atlas and eight symbol roles describe the [graphics contract](../../graphicalassets/fellowship/README.md). The status of the implementation and the actual tests are recorded in the UI-79.

The six parts of UI-34 have separate implementation evidence. F01–F07 use the Fellowship panel, separate Eye and map, with one host of each decision. The results of the aggregate acceptance includes UI-34, and the measurements and build belong to UI-35.

<a id="podział-odpowiedzialności"></a>
## Division of responsibility

| Task | Result |
| --- | --- |
| UI-33 | A durable, typed public report of the current/last Hunt. The existing task, without duplicate implementation. |
| UI-34-PANEL | Fixed status, panel Fellowship, Move/Hide and selection of specific dice. |
| UI-34-ROUTES | Declaration and forced disclosure by route on the map. |
| UI-34-GUIDES | Guide, composition and separation of Companion. |
| UI-34-HUNT | A separate window of the Eye, assignment, throws, spreads, tile, and report. |
| UI-34-RESPONSES | card, capabilities, injuries and sequels Hunt. |
| UI-34-MORDOR | Entry to the track, travel attempts, idleness and result of the party. |
| UI-34 | acceptance the combination of all six parts. |
| UI-35 | Regression of the whole UI-30–UI-34, layouts, performance and local build macOS. |

<a id="f01--dwa-miejsca-obsługi"></a>
## F01 — two service spaces

In the upper right corner remains a compact abbreviation `Fellowship` with state, progress and warp symbols and their exact values. A separate `Hunt` has an Eye symbol. Symbols have short labels; an information description appears after 200 ms invading or immediately after the seal. The map remains the main part of the screen.

Click or Enter/Space at short notice or tag opens the Fellowship panel; the Eye opens the Hunt. The Hover and focus itself do not open the control window. The cursor exits only the passive tooltip; the window remains open, and the header retains the moving and memory of the position in the session with UI-32. Close/Esc closes the window. Esc also extinguishs the tooltip without capturing the window close. `Locate` openly centres the map in the available Fellowship position; the opening or decision itself does not move the camera. The description does not take the data, does not change the selection and disappears when the gesture starts. Full contract: [BOARD_INTERACTION_UX, UI-80](BOARD_INTERACTION_UX.md).

New own decision opens the proper window once, according to the identity of the decision. The shutdown retains the legal local draft and the pending decision indicator. Another projection of the same decision does not open the window again. The change of decision cancels the outdated election without automatically choosing a replacement.

Each decision has exactly one execution place. The dice tray retains the selected die and cost; it does not duplicate the form from the window. During the Hunt route is handled at Fellowship/map, and the Eye shows its context. The selection of the new Guide in Hunt belongs to the Eye and uses a common portrait view. The opponent sees the public state and the decision owner.

<a id="f02--status-ruch-i-ukrycie"></a>
## F02 — status, movement and hiding

The panel shows `Hidden`/`Revealed`, `Progress`, `Last known position`, `Corruption n/12` with track and Guide portrait and composition. The own versions of the Council and the abilities remain private to the disclosure rules.

In the action phase, the selected specific legal die Character or Will of the West makes `Move` or `Hide` available. Without dice, the panel provides `Select a Character die` and distinguishes the authorized dice without selecting one. Optional capabilities to change cost/Mode require an open choice.

`Move` hidden Fellowship outside Mordor increases abstract progress by one and starts Hunt. It does not select a secret route and does not move the region tag. One sentence describes this effect before clicking. With `Revealed` traffic is unavailable with explanation `Hide first`; `Hide` changes the cover, without moving and without adding dice to Hunt. The result and cost is decided by the server; the final click sends one command through the existing sender.

<a id="f03--deklaracja-i-ujawnienie"></a>
## F03 — declaration and disclosure

In the Fellowship `Declare position` phase, it starts a voluntary declaration if the server options allow it. Forced disclosure opens a separate context. Panel clearly distinguishes the effects of `Remain hidden` and `Become revealed`.

The player points to the next neighbouring regions. Sees the used range, legal next steps, goal and known effects of the preview: progress reset, possible treatment, nation activation or additional fortress tiles. UI does not set a strategic route. `Undo step` undoes the last unsent step; `Cancel` abandons local draft, does not cancel the disclosure obligation.

The legal subsequent regions and the selected end are highlighted, without arrows, floating descriptions or target buttons. The reclick of the selected end approves the ready-to-way; its two-click is an equivalent gesture. The click of the current region prepares to remain if legal and the next click of the same end approves them. The lack of goal does not lead to a fictional route or consumption of dice. The disclosure effects and additional tiles must be completed before returning to the usual action.

<a id="f04--przewodnik-i-towarzysze"></a>
## F04 — Guide and Companion

Portrait of Guide has a `Guide` badge, name and access to the ability. `Change guide` only backlights legal candidates. In the Fellowship phase, before selection, it is displayed by `Choosing a guide ends the Fellowship phase`; click on the portrait makes the decision. `Finish phase` leaves the current legal Guide. The mandatory selection after re-synthesis has its own context and continuity. The server solves the forced single kandydat/Golluma.

`Separate companions` leads through portraits → common route → `Separate here`. The selected group, range and effects are visible before the final click. Information `Separated companions cannot rejoin` explains the decision durability. The change of the group converts the legality of draft according to options; it does not select alternate characters. The usual action requires a specific dice, the forced effect retains its own cost and returns to the overriding decision.

In siege, with special capacity and Mordor, the presentation is based on options/preview server. Removing from the game is not shown as a move on the map. Once separated, the self-traveling uses the existing UI-21; the project does not rebuild this separate flow.

The UI-34-GUIDES: portraits of non-candidates retain tooltip and focus; click does not finish the phase before entering the Change guide. Forced Guide does not require this mode. The descriptions of the Council version appear only after public disclosure. Cancel the imposed return group removes its route, retaining the composition of the RequiredGroup. In the absence of a route in the final Mordor, the choice is Remove from play. Preview separately calls placing and removal, also when Sméagol leaves with figure placed on the map.

<a id="f05--przydział-rzut-i-kafel"></a>
## F05 — Allocation, Throw and Tile

UI-50 distributes visible bonus components: previous move FP, effect on throw and spread and the spread bonus. Named sources of the right to the spread are visible without a hiver. dice retains natural meshes, hit and first value after the flip. An older report with the sum alone means no break; the known absence of dice has zero without auto-open.

The Eye window leads through the current step; the completed stages are a short report.

1. `Allocate dice`: click on a specific dice and Eye or drag; re-election allows it to be withdrawn.
   Number and limits are visible. `Allocate & roll` approves the entire set, also empty if the
   server allows it.
2. The shot: graphic meshes with a difference in hits/boxes, bonuses and sources of reroll.
   Animation shows a known score, never draws a draw in the client.
3. Transfer: Clicks select legal failed dice. `Reroll selected` approves set; `Keep roll` explicitly
   resigns. Results are not disclosed between selections.
4. Tile: central object with separate injuries, `Reveal` and `Stop`. Unset value is `?`, not zero.
   The source of the additional or listed tile remains understandable in the current Hunt.

The eye retains its last public score also in the absence of hits and after deciding without the player's decision. It uses `HuntReportView` UI-33, without parsing text history or reconstructing unknown throws. An older record without report shows no available report, no fictional score.

The UI-34-HUNT execution: The Eye window is separate from Fellowship, with one joint tray decision. The dice can be withdrawn by clicking and indicated before the Eye die is not yet part of the set. The shot has vector meshes, separate hit, bonus, tooltip of sources and the meter of selected reroll. Keep roll/Reroll selected approves one set. The tile separates the value, damage (also ?) and Reveal/Stop. The report bar shows the current entry and allows you to watch previous public events, even after the end. Ring/Guide/Random answers use the F06 gestures described below. Closing/hover does not change the selection or camera.

<a id="f06--reakcje-i-obrażenia"></a>
## F06 — Reactions and injuries

Legal card and capabilities appear only in the proper rule window, with short effect and open-ended option `Skip` when you are allowed to refuse. The order and window owner are from the server.

When settling injuries, the available answers show the following objects: `Ring`, `Guide` portrait and the covered stack `Random companion`. The first click selects the answer and shows the known effect, e.g. `Corruption 4 → 6`, other injuries or separation Guide. The label victims/separation results from the actual ability. With Random, the future victim and her result are unknown.

Reply Click → click the tile makes the choice. Equivalent shortcuts are two clicks of answers or drag per tile; no additional Confirm appears. After the change of decision, the remaining part of the gesture cannot make the next decision. Random does not allow you to indicate ID of the future victim.

Replace the tile, protect, Guide ability, victim, new Guide and its ability, the other Corruption, Reveal and additional tiles retain the engine order. UI does not offer a second regular victim after changing the Guide. The route of the disclosure goes to maps/panel Fellowship; the eye retains the summary and returns to handling the next tile. One cost/receipt covers the right step; the reconnect does not duplicate dice, RNG nor the effects.

Implementation UI-34-RESPONSES : three stable objects under the tile have symbols, hints and selection distinction. The tile is highlighted as the final target. The first click does not send command ; after the selection, a preview of known effects appears. The usual victim Guide does not pretend to be able to separate: Hobbit's return is described separately and Random does not give a future character. Legal responses use existing kart/zdolno • If you have selected the tile, the previous throws are in the report bar. There are no duplicate controls or additional Confirm for injury response.

## F07 — Mordor

`Enter Mordor` is an open-ended option of action only in the legal server window. After entry, normal progress replaces the track from 0 to 5. `Last known position` fails to succeed in position in Gorgoroth; `Locate` indicates the track presentation.

`Attempt move` On Hidden, he describes the selection of the tiles, does not guarantee the move. roll/reroll ed Hunt , even at zero dice The track doesn't change until it's settled. `Stop` blocks movement, `Reveal` changes the cover; Reveal itself is not a stop. `Hide` keeps position and its own cost. No simple route opens deklaracji/ujawnienia.

The panel shows a reminder of +1 Corruption for turn without trying to move or hide on track. The public information about such a attempt comes from the server projection, calculated from the existing state of turn (`LastMordorAttemptTurn`). UI-34-MORDOR is responsible for this additive field of `FellowshipView`, its contract and tests. No field in older data means no information, no proof of inactivity; the entry into Mordor itself does not count as a test.

The implementation uses `MordorAttemptedThisTurn` (`bool?`). False displays reminder, true confirms the attempt, null shows `Turn activity unknown`. Once the game is finished, the reminder disappears. Hunt window adds `No Hunt roll` and the public value Eye; the track remains on the current step to settle.

Corruption ≥12 takes precedence over the win-by-step 5. The result screen uses the authoritative ending of the game, without promising a victory in the preview. Forced separation on the track shows the removal from the game.

<a id="f07a--tor-mordoru-na-mapie-ui-152ui-156"></a>
## F07a — Mordor track on map (UI-152-UI-156)

The user's application with 2026-10-14: "Let's plan a graphical representation, UI and the operation of the Fellowship track The Ring in Mordor; for now there is no sign of this track when Fellowship enters it." Until now the track was only a line of 0–5 in the panel, and `MapFellowshipMarker` hid the figurine after the entrance. User's decisions: the track on the map and the compact row in the panel, the stone ink trail, the camera moves only at the entrance. Server, Rules (T17, LOGIC B19/B20) and Contracts unchanged.

<a id="grafika-i-położenie"></a>
### Graphics and location

- The track is an element of board: always visible on both players, extinguished when Fellowship is
  not in Mordor, uniform with a pencil map (MAP-03).
- Geometry in the presentation data of the `Resources/MapCanvas/mordor-track.json` client (1632×1338
  map layout): two entry lines from the Morannon and Minas Morgul anchors converged in the 0 step,
  the 1–4 step on the Gorgoroth plateau and the 5 step on the Orodrain from the drawing of the site.
  The points do not obscure the sediment symbols (Barad-dûr, Morannon, Minas Morgul) and reserve the
  place like the markers (`ReserveMarker`). Mordor is still not the graph region.
- The path is vector-drawed with a barrier-style ink from UI-143 (thin core of warm ink, soft
  shadow) in the layer "map-mordor-track"; the reconstruction only when changing step or state, the
  panorama does not over-drape it.
- Graphics records `graphicalassets/ui/src/mordor/` : plain, past (sinked), next (light), Extinction
  rift (crater with rift and red heat) and entry mark according to pattern `mordor-gate` Numbers 0–5
  In ink on the records.
- Two levels of detail like army tags (UI-126): on the whole map small plates without digits,
  approximately the plate with the digits and the signature "Crack of Doom".
- Legend of the map gets entries „Mordor track” and „Crack of Doom”.

<a id="drużyna-na-torze"></a>
### Fellowship on track

- At `MordorStep`, the Fellowship figure stands on the disc of the current step with the cliff ⁇
  Hidden / ◆ Revealed; it does not disappear from the map.
- After changing the step, it moves along the path (approx. 400 ms, like figures moves); at Stop it
  stays with a short "Stop" sign at the disc.
- At the figure: a small counter Corruption ‘x/12’ and, when `MordorAttemptedThisTurn == false`, a
  mark of no attempt in this turn with a tooltip o +1 Corruption at the end of the phase action (the
  same text as in the panel).
- The selected Hunt tile is displayed for a moment next to the figurine (`HuntTileFace` with
  UI-141); the full score and answers remain in the Hunt panel.
- Tooltipy: CD „Mordor · Step n of 5”, step 5 „Crack of Doom · Reaching
  it with Corruption below 12 destroys the Ring”, figurine: stan, step,
  Break and test in the round.
- The Fellowship panel retains a compact line 0–5; its status agrees with the map.

<a id="działanie"></a>
### Action

- Entry: at the legal `EnterMordor` (Fellowship in Morannon or Minas Morgul, server window) the 0
  plate and the input line are lit; one click of the 0 plates sends the input (UI-142). The button
  “Enter Mordor” in the panel stays.
- Motion test: with the selected dice Character or Will and the legal `MoveFellowship` next plate
  shines; one click sends a test. Tooltip warns that the tile can stop the movement; preview does
  not promise a shift or victory (T17).
- Hidden: at Revealed and legal `HideFellowship` the figurine shines; one click hides it.
- Shadow sees the same track and figure, no targets.
- Camera: when the player Free Peoples `MordorStep` changes from lack to value after its own
  command, the map once covers the track. U Shadow camera stands; the track pulsates once, and the
  chronicle and attention lead to it.
- The Fellowship button in the header and click the track line in the panel centre the map on the
  track when Fellowship is in Mordor (so far the button has only been focusing the panel).
- Game end: step 5 and victory shows the existing victory panel, the figurine stands on Parsley;
  Corruption 12 has priority so far.

<a id="wdrożenie-ui-153--grafika-i-geometria"></a>
### implementation UI-153 — Graphics and Geometry

- `graphicalassets/ui/src/mordor/`: `slab` (`passed`, `next`), `crack-of-doom` (`next`), `gate`
  (`next`); `--slab-*` tokens in `palette.css`, warm ink like a barrier with UI-143; `--only mordor`
  render.
- `Resources/MapCanvas/mordor-track.json` ( `mordor-track-1` ): the gates of Morannon ( 1256 , 852 )
  and Minas Morgul ( 1250 , 924 ), step 0 ( 1286 , 887 ) on the plain of ash, steps 1–4 After the
  southern slope of Ordruina and step 5 on the crater ( 1349 , 851 ). `MordorTrackGeometry` (loaded
  with map by `BoardPresentation.MordorTrack` ) requires six steps on the path in order, to enter
  from both gates ending in step 0 , points on the map, step spacing at least 18 and distances at
  least 20 from sediment anchors. `BoardPresentation` saves shared data only after all parts of the
  map have been loaded.

<a id="wdrożenie-ui-154--tor-na-mapie-i-drużyna-na-torze"></a>
### implementation UI-154 — track on map and Fellowship on track

- The vector layer "map-murmor-track" (`MapRenderer`) draws the path and two entry lines with
  barrier ink (0,7 core and the shadow 1,4 core width of the barrier); without Fellowship in Mordor,
  the layer has a 0,55 cover. The change of condition changes only the cover, the panorama does not
  overstrip the layers (test).
- `MapMordorTrack` stacks in the screenspace of the disc `mordor-track-step-0…5` and gates
  `mordor-track-gate-<region>` before the army tags and reserves their place. Across the board map
  18×14, The rift 26×22, the gates 14×14, without digits; approximately
  (`MapMarkerVisibility.Detail`) plates 30×22 with digits, Szelina 42×35 with the signature ‘Crack
  of Doom’, the gates 20×20 Classes `mordor-passed` / `mordor-current`, the whole track
  `mordor-track-inactive` without Fellowship The state comes from `MordorTrackState` (public view
  Fellowship).
- `MapFellowshipMarker` in Mordor stands feet on the board of step ( 36×40 , all over the map 28×32
  ) and at a new step goes along the path through 0,4 s ( `MordorTrackGeometry.PointAt` ); first
  show and undoing put it immediately. Under the plate row: tile Hunt ed `HuntView.Face` (only
  approximately) and the badge ‘ x/12 ” with a ring and an hourglass when `MordorAttemptedThisTurn
  == false` ; tooltip as in panel ( `MordorTrackState.ActivityHint` Tag Fellowship there is no
  margin of the button anymore, so it stands exactly where the map has been set.
- Map legend: “Mordor track” with gate, plate, next plate and Parsley.

<a id="wdrożenie-ui-155--działanie-z-mapy"></a>
### implementation UI-155 — map action

- `FellowshipContextInteraction.OfferTrack` reads server options via `FellowshipActions.Option` with
  selected dice: legal `EnterMordor` backlights the 0 CD and the Fellowship Region gateway, legal
  `MoveFellowship` in Mordor next CD, legal `HideFellowship` at Revealed figurine (class
  `fellowship-hide-target`). Click sends the same selection as the panel button (`Choose`); no
  offer, click the CD does nothing and click the figurines opens the panel so far. Tooltipy says
  what they will do click.
- Entry: `MapMordorTrack.Entered` (transit from missing to track in the same match, not when loaded)
  with Free Peoples calls `MapCanvas.FocusMordorTrack` (direction of track, zoom of at least level
  with digits) and with Shadow disposable pulse of plates (`mordor-track-pulse`) without camera
  motion.
- The Fellowship button in the header and click the track line in the panel (except the traffic
  symbol) centre the map on the track.

<a id="odbiór-ui-156"></a>
### acceptance UI-156

- `MordorLayoutTests.AcceptanceScreensForBothSidesAndSteps`: both sides, steps 0, 3 and 5, Hidden
  and Revealed, four sets, close-up `FocusMordorTrack`; whole track in view, figure on the step
  plate, overshot plates extinguished, hourglass only without attempt (`ui156-mordor-*` discharges).
- On Parsley, the Corruption badge stands on the left side of the crater, so as not to obscure the
  signature or the 4 disc.
- Mordor regression, Fellowship, Guide, routes, Hunt, victories, map and full T23 game without
  defeats.
- Map profile17 before and after the track (`2bb4442`) (2026-10-14,
  [summary](profiles/map17-ab-ui143-ui156.json)): tops +3 thousand, idle frame, cursor and heavy
  loads ±1%; panorama with a zoom at Morannona longer by 0,08–0,2 ms (eight track elements stacked
  every frame like tags), p99 unchanged. Minimum FPS and feedback unchanged; user accepted this
  cost.

<a id="wyraźniejszy-tor--ui-157"></a>
### Clearer track — UI-157

User notification from 2026-10-14 (Fellowship in the Gorgoroth region, track poorly visible): ‘Delete the inscription Crack of Doom and increase the visibility of this track’.

- The rift has no signature on the map; they call it tooltip and legend.
- Path and entrances: bright paper backing ( 2,6 and 2 width of the core barrier) and deep red ink
  of Mordor ( 1,3 and 0,9 ), clearly different from the grey barriers of the mountains. 34×25 (all
  over the map) 22×17 ), Specification 46×38 ( 30×25 ), the gates 23×23 ( 17×17 ), digits 12
  ‘Without Fellowship The track has a cover 0,9 (plates) and 0,85 (cart) instead of 0,6 and 0,55 .
- Fellowship in the Gorgoroth region stands out of track: only action "Enter Mordor" from Morannon
  or Minas Morgul (BASE pp. 43, T17) enters the track. Tooltips of plates and gates at Fellowship
  outside the track explain this.

<a id="zadania"></a>
### Tasks

UI-153 (graphics and geometry), UI-154 (track on map and Fellowship on track), UI-155 (action from map, camera, button Fellowship), UI-156 (acceptance).

<a id="źródła-i-granica-zmian"></a>
## Sources and limit of change

The source of the rules and their sides are in [T15](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/rules/T15-FELLOWSHIP.md) ( BASE p. 24,34,36–39 , FAQ p. 1–2 ), [T16](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/rules/T16-HUNT.md) ( BASE p. 37–42 ; allocation dice also p. 19,22–23 ), [T17](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/rules/T17-MORDOR.md) ( BASE p. 39,43–44 ), [T20](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/rules/T20-CHARACTERS.md) and [interaction kart](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/rules/T21-HUNT-INTERACTIONS.md) Exceptions Lords/Council/Kings/Fate are compatible with [EXPANSIONS](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/rules/EXPANSIONS.md) and existing server options.

command, cost, legal options, preview, shared sender and moved windows are reused. UI-33 is the owner of the report. The new public test mark in Mordor belongs to UI-34-MORDOR. The project does not require sending full status, adding a secret route or a new RNG in the client. Each part must keep running early flows until they are replaced in the appropriate task; hiding the old form without an available successor is not the completion of the task.

<a id="wspólny-odbiór"></a>
## Common acceptance

Each implementation task extends the tests of the proper flow and updates the D09 in SCENARIOS. The screenshot, mockup or compilation itself does not replace the pass through the real server. Clicks, equivalent shortcuts, views of both sides are required, outdated decision, rejection of command, retras and recovery of approved steps. Unsent draft is local; the window closes it, the client reboot fails to save such decision.

Uncertain response to your own command opens once a window with `Check result`, also after changing the phase to your opponent's decision. The player can close them; the same projection or GET does not force you to reopen.

UI-34 connects F01–F07 in full D09: movement → Hunt → damage → disclosure → coverup, declaration/Guide/separation and Mordor final, also with card and expansions. UI-35 checks 1366×768 and 1920×1080 at 100%/125%, no mandatory scrolling and double controls, no focus, running map, refresh costs and a separate local macOS build. Manual acceptance, Windows runtime and target host retain previously documented postponements.

<a id="historyczny-odbiór-ui-35--2026-10-07-utc"></a>
### Historical acceptance UI-35 — 2026-10-07 UTC

The following result and further implementation descriptions of UI-49-UI-72 with 2026-10-08 UTC document the previous versions. They do not constitute acceptance gestures UI-80; the current window opener and tooltip standard is defined by F01 above.

acceptance regression UI-35: F01–F07 and the usual journey separated Companion passed the matrix of both clients, return/receipt and exceptions expansions. Final layouts and focus were tested after the common window system fixes; the Fellowship window outlines are not drawn by Hunt on the upper layer. Detailed results, measurements and limits of local build: UI-35.

<a id="uporządkowanie-hunt--ui-49ui-54"></a>
## Order Hunt — UI-49–UI-54

UI-48 prepares the development of F05–F07 described in [HUNT_UX](HUNT_UX.md), with the H01–H14 matrix. Four parts of the window focus on the current decision and its known effect. The main consequences of the response are seen under objects; the earlier course remains for inspection. The F06 gesture, the ownership of the controller and the order of the server remain common.

Automatic settlement uses log/notifications; without auto-opening result and text at the Eye. This hates the manual last report or auto-open your new decision. Disclosure and deployment still belong to Fellowship/map, with an empty legal region. UI-54 receives new course along with existing F01–F07; status determines the register.

<a id="przygotowanie-postaci-i-characters--ui-65ui-72"></a>
## Character preparation and Characters — UI-65–UI-72

[New specification](CHARACTERS_SETUP_MENU_UX.md) separates the private panel version/Guide UI-68 before the first Recovery from the Fatherland UI-69 in the first Fellowship Phase. Both stages retain separate command , checkpoint and moment of disclosure; the home country window also exists without candidates. After deployment Characters/Map/Dru The rye enjoy the same condition.

Characters do not copy the Fellowship, Golluma/Sméagola or Guide/Random/Ring. UI-71 forms directs responses and returns to the right host, and UI-72 receives CM01-CM18 with regression F01–F07. This does not change the status of previous tasks or the scope of the Hunt plan.

UI-49 organizes the Hunt window in four parts and moves the same tray to `DecisionHost`. The selection of the new Guide dominates the previous tile. Regional routes remain at Fellowship and Close/GET/receipt and the position of the window retains F01. The report of the last three steps is not the source of legality; the evidence of H01/H02 records UI-49.

<a id="ui-51--skutek-odpowiedzi-na-polowanie"></a>
## UI-51 — effect of response to Hunt

F06 shows the current loss before choosing Guide And the known injury reduction. Hobbit returning to the map is described separately; voluntary decoupling of ability remains a reaction with its own description, not a victim. Random victim includes the possibility of Guide but does not show the future ID New Guide before Corruption remains a separate decision before the residual effects. [HUNT_UX](HUNT_UX.md) , UI-51.

<a id="ui-52--jeden-gospodarz-następstw"></a>
## UI-52 — One host to the aftermath

The guide and its ability remain in Hunt. Regional return and Reveal move the same tray to Fellowship; the next tile returns to Hunt without moving the camera. The subsequent fortresses do not re-download the cost of action. The removal in Mordor does not show the route. During Hunt, the Fellowship preview retains the track and portraits, but ignores repeated instructions. Automatic positioning of both windows does not save the position as the player's choice; manual drag still takes precedence. Evidence of F03/F04/F06/F07 and limitations of acceptance: UI-52.

<a id="ui-53--oddzielenie-historii-od-aktualnej-drużyny"></a>
## UI-53 — separation of history from the current Fellowship

card historical action traffic shows the saved string Hunt, also when the next one is running. It does not substitute the current progress, Corruption or Guide. The closure of history and the gold tab returns to the current decision host; reading and reconnect do not send command or repeat the cost of the move.

<a id="ui-54--odbiór-polowania-i-integracji"></a>
## UI-54 — acceptance Hunt and integration

acceptance UI-54 re-establishs F01–F07 flows with current Hunt/Mordor, moving the Guide/routes host and later UI-60 result panel. The replacement of tiles and protection retains the existing cost of movement until the end of the chain; check hosta/receipt and images perform different roles in acceptance.

<a id="początkowy-przewodnik-lords--ui-68"></a>
## Initial Guide Lords — UI-68

The preparation of the version and the first Guide has its own common panel CompanionSetupPanel. It does not use action changes of Guide in the Fellowship phase. Comparison of the two versions is a local viewing; the choice of the Guide is separate and mandatoryly public. Approval only publishes what the server reveals. CouncilHomelands remains another decision, without automatic transition by Recovery or performing moves behind the player. Close retains draft; reboot uses existing local writing and receipt.

The new UI68 scenarios confirmed a separate open Guide, preserved draft and one record after lost odpowiedzi/restarcie. Tests include Lords, Council and intersections of Fate/Kings; no this decision in the database. Results and limitations are in UI-68.

<a id="nawigacja-do-bieżącej-decyzji--checkpoint-ui-62"></a>
## Navigation for the current decision — checkpoint UI-62

DecisionHostNavigation leads to an existing Fellowship, Hunt tray or separate version preparation. It does not create a second Guide selection and does not play the current status based on the old log. The preview of the saved action, Help and Close retain the local draft tray; its recovery is not an approval. The gold shortcut first reads the actual tray host, so the new reaction can break the wider context without creating the second form. The tracks and Hunt retain the camera policy to date. The evidence of recovery and the current acceptance integration are in UI-62.

<a id="początkowe-ojczyzny-council--ui-69"></a>
## The initial homeland of the Council — UI-69

The choice of homelands remains a separate decision of the first Fellowship Phase, after the preparation of the version and Recovery. It is not an ordinary move or disconnect behind die Homelands uses the same private host, but new ID context; one ✓ also includes an empty set. Fellowship in the panel only removes the unsent allocation of the selected figure.

Only the public map, Characters and Fellowship are agreed to the approval. At one departure, the next step is the Shadow and its resources; the client resumes not choosing token for it. Sources and results: UI-69.

The UI-69 tests confirmed the transition through both checkpoints after reboots without automatic strategic selection and without a repeat token.

<a id="historyczny-guide-i-przekazanie-decyzji--ui-71"></a>
### Historical Guide and Transfer of Decision — UI-71

The historical Guide only shows the version established in visible facts; in its absence of the identity itself. The choice of the setup and public disclosure are separate steps and lead to Fellowship. After the Hobbits return, one aggregate route remains in the host, also after the Gollum refusal. The closed panel does not lose legal draft; the gold shortcut opens its current parent without creating another form. acceptance UI-71.

acceptance UI-71 : 33/33 the required Play methods, 13/13 Full Editor cases 542/542 , 7/7 PNG and 399/399 unchanged domain cases in full 3726/3726 PASS . Separate fresh HTTPS/WSS dragons are credited with trusted CA ; Unity tests benefit from HTTP/WS/PostgreSQL. Accurate filters, reusing identical methods and limits: UI-71 . .

<a id="ui-63--wspólna-inspekcja-i-zachowanie-gospodarza"></a>
## UI-63 — joint inspection and host behavior

acceptance other panels retain the separateness of Fellowship, the preparation of versions and homelands and Hunt. Help, Effects, resources and older log entry do not select a new Guide, do not change the route or create a second form. The gold tab still opens the actual tray parent; refreshing public information does not perform local draft.

The dense projection for the UI-63 trials includes recovered resources and initially open Fellowship. The test and the profiler warmup close it with a real click before the inspection of five sovereigns. They check the behavior of the revision, ID decision, phase and camera; the closing does not replace the preparation decision. The update of Corruption 2↔3 in the dense profile UI measures the rendering of the public field rather than the execution of command rules.

93 configurations have separate composition attempts, start by HTTP and restarts before/after first action, described in [IN_GAME_UX](IN_GAME_UX.md). The final G18 includes both roles and four chips with simultaneous Effects/Help; the stronger attempts and measurements of the Player remain open acceptance UI-63.

<a id="końcowy-odbiór-ui-63"></a>
### Final acceptance UI-63

UI-63 preserves the separation Fellowship , preparations and Hunt. Inspection Help/Effects/resource This does not change the Guide or the route and does not create a second tray. Close initially open Fellowship in a dense script is a real click with control ID decision, revision, phase and camera. Profile updates public Corruption 2 ↔ 3 only for the measurement of render. Integration tests are included; current measurements/images summarizes UI-63 .

<a id="wersje-guide-i-ojczyzny--sprawdzenie-ui-72"></a>
## Versions, Guide and Homeland — check UI-72

The actual attempts of the four layouts confirmed the separate local choices of the version and Guide in Lords/Council/Fate and click/drop/Stay at home. Esc closes the preview, retaining draft. The review of the full character list does not move the camera or Fellowship; the waiting Shadow does not get the opponent's form. The choice of the homeland is checked openly for the chart goal, without the strategic selection automation. Full report: UI-72.
