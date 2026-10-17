<a id="plansza-inspekcja-i-ruch-figurek--ui-73ui-80"></a>
# board, inspection and movement of figurines — UI-73–UI-80

The 2026-10-08 order and the UI-80 specification separate the short information into hover/fokus from the panel opened by clicking. Priority: clear motion, screen map and convenient inspection. The results of the tasks so far remain historical; the state of implementation of the new version is in the register.

<a id="interakcje"></a>
## Interactions

Hover area only slightly lightens the region, regardless of the composition. Its panel opens klik/Enter/Space. Other objects and controls can show a short information tooltip after 200 ms howera or after seal. Tooltip contains only public local data, has no buttons or choices and does not capture input. Quit or lose focus closes it. card , drawers, menus or download history.

The object or Enter/Space click opens its interactive panel, which remains after the cursor has left. Another object replaces the unattached preview; ×/Esc closes. pinned and the moved windows retain the position. Except for manual opening, the existing new decisions and excess hands; it is a rule signal, not a hover.

Legal action preserves priority: click dice / figure The preview does not add second clicks to the cost or movement. Pressing, drag, pan, wheel, cancel or disconnect the object extinguishs the passive description. The tooltip itself does not use the first Esc intended for the active window. card keep Play and drop; from UI-194 one click plays card with one method of payment (without arming), and from UI-142 The excess hand is rejected by one click after one card. From UI-147   card The fight plays one click of her tile in the window battle and playing none card does not open a window with its contents. From UI-155 Mordor track on the map accepts clicks: plate 0 at the legal entrance, the next plate at the motion test, the figurine at hiding (F07a in FELLOWSHIP_UX Politics open by clicking the tab. From UI-144 shift and Activate arrows stand immediately at each of their own people, who have chosen die can move or activate; click arrow or token steps. Exception UI-92/UI-98: edge tab panels (left slat, tab) Fellowship and Hunt in the corner) develop interactively after invading.

<a id="kadr-i-cele"></a>
## Personnel and objectives

The map raster covers the entire screen under HUD. The minimum zoom is cover, so with other ratios you can initially not see the whole map simultaneously. Each piece is reachable; you can never reveal the background outside of its rectangle. Resize, Location and manual master have the same limits. The cursor at the shore moves the map smoothly; the corner works in two axes. Panels and active controls block scrolling under each other, except for the narrow external pass reserved for navigation (expression of UI-81). Loss of focus, cursor output, decks or manual master stop this path.

Legal targets are highlighted regions. Without arrows, floating descriptions, target buttons and indicators that create a list of options over board. The choice of source/participants and budget still shows the actual state. On the routes, many steps, the next clicks select the area; the step that uses all of the Progress ends the route and a shorter one is sent with ✓ (UI-219: no reclick, no double click). We do not choose the path behind the player. History can distinguish the known source/target, without the arrow of motion.

<a id="drużyna-i-hunt"></a>
## Fellowship and Hunt

The two complementary panels have a common upper edge and lie side by side within the viewport, over the tray. Fellowship focuses the state and journey; Hunt score, reactions and the remaining cost of Hunt. Opening the second panel does not push it under board and does not center the cameras. Manual positions and resets retain the context; automatic layout does not obscure the current decision. A conscious move of the window with a mouse or keyboard unzips it from the pair and has priority in this session. Window reset restores the common government; it does not change draft or the camera.

<a id="figury-i-animacja"></a>
## figure and animation

Simple rasters have a clear background, a recognizable type figure, a separate page designation and a replaceable resource directory. The stack on the map uses several silhouettes and a discrete number; in the composition we choose specific ID. base game garrison differs from the field army. We do not change the geometry of the hit-test depending on the details of the illustration.

The traffic after the server confirmation takes about 0,65–1,05 p. figure or a compact group clearly moves from a known source to a target. Animation is only a presentation of subsequent public projections: without RNG , sending commands or queue blocking next choice. We do not add the supposed intermediate fields. Hidden progress Fellowship the first view, the reconnet and the revision gap show the current score without replay. Reduced motion skips the move and leaves a short accent. After animation, the actual target stack remains visible also at the distance, until the next projection or reset connection. This result indication does not open the preview, does not change the selection and does not show the secret route. UI-95 another projection that leaves flying figure for their purpose (e.g. next step turn right after the declaration Fellowship ), does not interrupt the flight; this new move figure replaces it and her disappearance ends. Corruption on track Fellowship moves smoothly to the new field (approx. 260 ms), and the first position and the change of scale set it immediately.

<a id="macierz-odbioru"></a>
## Acceptance matrix

| ID | Condition | Owner |
| --- | --- | --- |
| BI01 | Cover, pan/zoom/resize/Locate without external background | UI-73 |
| BI02 | Edge pan, horns, UI, focus and boundaries | UI-73 |
| BI03 | Hover region exclusively highlight | UI-73, UI-74 |
| BI04 | Information hover/fokus; interactive panels of all families click/Enter/Space | UI-74, UI-80 |
| BI05 | Tooltip does not capture gesture; priority action, no additional Confirm/GET/POST, privacy | UI-74, UI-80 |
| BI06 | Targets without arrows/options, armies/attack/routes and their final | UI-73 |
| BI07 | Both panels without collision in four systems | UI-75 |
| BI08 | Close, positions, new decisions and reconnect | UI-75 |
| BI09 | Exchangeable rasters, readable Types/piles and stable ID | UI-76 |
| BI10 | Approved movement, intermediate position, split/merge | UI-76 |
| BI11 | Hidden Fellowship, reconnect, new projection, Reduced motion | UI-76 |
| BI12 | Regression, measurements, builds, dragons and viewings | UI-77 |

<a id="granice"></a>
## Boundaries

The source of legality remains options and server validation. BASE/expansions are not changed by this project. We do not reveal foreign hands or private wersji/wybor. Windows/Intel runtime, physical smooth, manual acceptance and implementation remain deferred according to the previous order.

<a id="doprecyzowanie--zwartość-i-graficzne-tory"></a>
## Precision — Complexity and Graphical Tracks

UI-78 removes blank fixed heights of HUD and excessive widths of simple inspections. The panel grows with real choices, not with the mode name itself. UI-79 after these amendments designs two parallel tracks Fellowship with simple interchangeable symbols, minimal text and readable tags. acceptance BI12 in UI-77 includes both parts. The hand oversize is still rejected with two clicks; the first inspection click cannot capture the second click.

<a id="ostateczne-rozdzielenie-informacji-i-opcji--ui-80"></a>
## Final separation of information and options — UI-80

This section of Interaction describes the final agreement of UI-80. Historical UI-74 also received textual information on click; its results are not prescribed for the new model. acceptance BI12 in UI-77 includes the version of UI-80 and the status of implementation remains in the register.

UI-78 after the interval correction has the actual acceptance with simple and dense panels in four layouts of both sides. The height is derived from two columns of resources and a visible budget; an empty region uses a narrower panel. Results, previous failures and viewing of 12 with new images: UI-78. acceptance does not replace new UI-79 tracks or the final tooltips semantics of UI-80.

acceptance clicked inspection UI-74 has completed Victory18 test (1/1 PASS, four systems); full migration and double click coverage describes UI-74. UI-80 will keep the clicked panels, restoring only short information to hover/fokus according to the user's later specification.

<a id="tory-drużyny--ui-79"></a>
## Fellowship tracks — UI-79 tracks

The dominant element of the window is two parallel rows of the same width: travel and warp. The usual travel shows the fields 0–12 ; accurate progress greater than 12 remains at the end of the tag, without cutting the data. The warping maintains the exact value and clear threshold 12 Hood and exposed Ring distinguishes Hidden/Revealed Mordor replaces only the upper row with six fields 0–5 : Gate→Fire. The motion attempt does not move the token before the authoritative result Hunt .

This is the separation of two indicators of the common track board, not the new rule of the progress limit. Source: BASE-2.4.1 p. 36–39, 43–44. Example: Progress 3, Corruption 4 are two different fields; the declaration resets the first indicator, the warp changes only according to the outcome of the rules. After entering Mordor, it counts MordorStep, normal progress does not specify the position of figure on the map.

The tracks are informative. action, Guide and the composition retain existing windows and costs. Their graphics have separate slots and do not define hitboxes. Exchange contract and prompt: [Fellowship symbols](../../graphicalassets/fellowship/README.md). Hunt remains the second, adjacent window; manual offset and reset retain the behavior of UI-75. Evidence implementation/tests: UI-79.

<a id="wspólny-odbiór--ui-77"></a>
## Common acceptance — UI-77

acceptance includes the final UI-80 model, compact panels, two tracks and movement of figurines. Clicking on the legal target retains priority and the information tooltip does not take the first Esc. The hand oversize has a real double click with a single save. The selected die reserves a seat for the budget, so the first choice of figure does not move the panel under the cursor.

Results 600 Editor, 60 different Play methods, profiles 56+32, builds and dragons are described in UI-77; detailed coverage of each BI retains [evidence matrix](profiles/ui77-validation.json). Older parts evidence is separated from final re-runs. Measurements do not replace the manual batch or client startup on Windows/Intel.

<a id="regresje-po-odbiorze--ui-81ui-82"></a>
## Regressions after acceptance — UI-81/UI-82

The 2026-10-09 application requires continuous edge pan on each edge, also when the figure is entered under the motionless cursor. The narrow external pass remains available at HUD; its interior still protects the controls from scrolling. Pressing, capture, focus loss and output stop movement. The exact width and proven scenarios are in UI-81.

The two clicks of the excess hand work on the thumbnail and the content of the large preview. The first click remains an inspection; two clicks of the same card in one context reject it only once. Quick click of another control is not part of this gesture. The buttons and the preview holder have their own support. UI-82 also verifies the engine input path; earlier tests with the artificially set click counter did not prove its correctness.


Clarification UI-84: BI02 includes input from the physical screen, not only events delivered to the panel. The last pixel and exact edge are to keep the move with the stationary cursor. We do not extend the active area beyond the game surface; focus, capture, panels and cover remain binding.

UI-174 ( 2026-10-15 ): In full screen modes the limit is the physical edge of the screen. The cursor outside the game image (black pass at other ratios or off-the-edge screen) is cut to the nearest edge of the image ( `MapScreenPointer.TryInside` ), so scrolls the map as at the edge. In the window (Windows and View of the game in Editor) the output by one pixel still stops the movement. Loss of focus, no mouse, pressed button, capture and open windows still stop scrolling. On Windows, the full screen closes the cursor in the game window ( `CursorLockMode.Confined` Full screen presets have screen aspect ratios, so there are no belts ( MAIN_MENU_UX , UI-174 ). UI-176 : The proportions are derived from the game window screen and are re-matched after the window is moved. UI Expands (Expand) with ratios other than 16:9 , so board fills screens 16:10 , MacBooka and 21:9 No seat belts.

UI-180 (2026-10-15): in full screen with focus the cursor does not leave the game screen. `CursorBarrier` works every frame in the build:
- au Windows He's watching. `CursorLockMode.Confined`;
- on macOS (Union does not support Controlled) remembers the screen under the cursor when the cursor
  is over the game (CoreGraphics: `CGGetDisplaysWithPoint`, `CGDisplayBounds`). The cursor going
  beyond this screen returns to the nearest edge (`CGWarpMouseCursorPosition` with
  `CGAssociateMouseAndMouseCursorPosition`, without the move pause after moving).

Loss of focus (Cmd-Tab, Alt-Tab), Windowed mode and Editor release cursor. Cutting in `MapScreenPointer` remains as a security: position from the first frame behind the edge and scrolls the map.

<a id="stały-układ-przy-krawędziach--ui-85"></a>
## Permanent arrangement at edges — UI-85

Screenshot 2026-10-09 17:11:43 replaces the horizontal pair and the manual positions of Politics/Effects/Characters/Fellowship/Hunt/Cards. The left panel switch and the right column constantly shows the full Fellowship and Hunt, with Cards below at the bottom edge. These panels do not have sliding handles and do not play the former positions of WindowLayout. Victory is in Menu, the batch end message remains on board. BI07/BI08 now requires a vertical column, without collision and without closing it by inspection. Other windows retain the existing support of clicks, gestures and tooltips.

UI-85: the right column reaches the physical bottom of the screen, and HUD dice reserves its width. The extended Hunt response expands the column; in the absence of height the cards net extends to the left of Cards, above HUD. Geometry changes the presentation, without closing the hand and without changing the draft.

<a id="ilustrowany-górny-pasek--ui-86"></a>
## Illustrated upper bar — UI-86

The strip remains the surface of the map edge (`map-edge-surface`). Its frame, separators and shadow are only decoration with `PickingMode.Ignore`; click, hover and edge pan are to the same goals as in UI-85. diceThe opponent remains buttons with legal targets, and the chips meters only accept the indicator to show the non-interactive tooltip (UI-80).

<a id="lewa-listwa--ui-87"></a>
## Left strip — UI-87

Bookmarks, headers and nation crests are a decoration with `PickingMode.Ignore`; click and hover go to the bookmark, button or row like in UI-85. Selected bookmark, legal targets and markings have open styles above the frame.

<a id="przydział-polowania--ui-88"></a>
## Allocation Hunt — UI-88

dice sets the allocation without an additional target: click dice in the pool switches it in the set and Allocate & roll is the only approval. The eye remains the drop target and the set header without action click. The dice hints in the pool say if the die clicks, withdraws it or stops on the limit.

## Podokna — UI-89

The parent of the window is the closest closed window board (Esc registry) which contains a clicked button. When the parent disappears, the window closes in the same frame or next frame, cascaded. Fellowship and Hunt held by decision do not disappear, so their sub-windows behave as yet; since the UI-98, the panel from the tab closes its window, unless they hold the panel (`Busy`). Unfitted help continues to close also after clicking outside it (preserving before UI-89).

<a id="prawa-kolumna--ui-90"></a>
## Right column — UI-90

The button of the Fellowship header is the only clickable state goal: state labels and places have `PickingMode.Ignore`, and the full description is in the button bar. Headers icons do not capture the indicator. The space click moves only the camera, without command playing and without changing the selection.

<a id="ręka--ui-91"></a>
## Hand — UI-91

The cards slots have fixed positions (absolute layout counted in `HandView`), so the development does not move the neighbours. The developed lico captures the indicator: within the slot, click supports the card button, over it `ClickEvent` lica (the second double click is part of the two click gesture). Drag and two clicks work as before, also from the developed part. Drag wraps the preview.

<a id="podgląd-lewej-listwy--ui-92"></a>
## Left strip preview — UI-92

`LeftRailPreview` opens the panel with the normal `Open` path (one tab at a time), means it with the `edge-panel-preview` class. The UI-97 curtain stopped pressing and fastened the panel; UI-98 removes it (see below). The panel opened earlier by context is remembered and opened again after the preview has been rolled.

<a id="zakładka-w-rogu-i-panele-bez-przypinania--ui-98"></a>
## Corner tab and non-pinning panels — UI-98

`LeftRailPreview` supports two groups: the left panel and the tab Fellowship and Hunt in the top right corner (`Edges.Corner`). The preview only restores the panel of its own group and only when it is gone. The tab and the Enter tab replace it with `Clickable` and only expand the panel; the tabs inside the tab trigger `Show("corner")`, so nothing is attached. The focus on the display does not hold the panel; the focus on the keyboard holds it, also on the item inside the panel. When the panel pulls down, the current decision (the panel on the left, `Held` in the corner), is left unattached and the preview forgets the replaced panel. The closed panel in the preview by × or code does not leave an outdated state. `Busy` holds the scroll until the window opens from the panel (help, card, character inspector) is visible.

The full form is `Edges.CornerPanels` with Fellowship and Hunt panels; it develops over the tab, and the tab is opened by the tab. `FellowshipPanel.Held` and `HuntPanel.Held` are derived from `UsesPanel` when docking: the panel is visible as long as the decision hosts are in the decision, and it rolls over it itself unless it is still under the cursor. The held panel gets the `edge-permanent-panel` class, so Esc skips it like the former fixed column and comes to cancel the selection. The new own decision continues to close the unstable windows (the `Opening` event). `FitHand` counts the bookmark and panels held or opened from the code; the preview from the invading does not move the hand.

<a id="stos-powiadomień--ui-99"></a>
## Notification stack — UI-99

`ActionNoticeStack` is one button called `action-notice-<id>` with the latest group. Pressing remembers the group in front, so the newer notification on the click does not replace the target. Click adds a pair (group, latest step) to the local read, opens card by `ActionContextRouter` and overwrites the stack; confirmation of steps in `NotificationReadStore` removes the group from the model so far. `ActionNoticeBar` retains hidden `Overflow`, `More`, `Close` and `Pin` for the compatibility of calls.

<a id="ikonki-powiadomień-karta-na-środku-prawy-klik--ui-100"></a>
## Notification icons, card in the middle, right click — UI-100

**Stoll notifications.**Stack UI-99 draws flags instead of ribbons: The `action-notice-<id>` button has a fixed size of approximately 42 × 30 pt and has no label. The text of the latest action gives `tooltip` read by `RuntimeTooltip`.

- A notification box.
- Order: decision indicator, Log icon (`open-log`), last fight icon (`last-battle`, only when there
  is an unread round, UI-112), stack, re-read.
- The bar is visible throughout the batch, because Log is always available.

**Action Tab.**
- `ActionLogPanel.PositionCard` sets card in the middle of the board area.
- `CardBounds` presses it below the notification bar.
- `Select` forgets the stretched position (`FloatingWindow.Forget`), so each new card starts in the
  middle.
- The change in height when reaching content recenters card.
- `ActionContextRouter` no longer counts the space next to the context object.
- `Frame` tries to successively strap around card (left, right, upper, lower) from the largest.
  Selects the first one in which `TryFrame` will fit all regions at the acceptable approximate and
  limit of the map.
- Before the first layout card is the replacement size of `CardSize`.
- The open Log list only cuts pass, which it blocks.

Right click on the map.
- `MapCanvas` remembers the point of pressing with the right button, and for the army tag its
  region.
- The movement by at least 6 pt turns the gesture into a move.
- Unmoved calls `RegionInspected` with the press region. `BoardScreen.Inspect` closes the fight
  report and opens `BoardInformation`.
- The `Inspecting` mode and drag lock in it are gone, because the gesture is disposable and does not
  change the entry state.
- `DecisionNotice` calls `CloseInformation`.

<a id="władcy-w-panelu-characters--ui-101"></a>
## Overlords in the Characters Panel — UI-101

Buttons are placed.
- `SovereignPanel.Root` (`sovereign-strip`) is no longer a child of the board scene.
- `BoardCharacterIntegration` inserts it as the `characters-section-Sovereigns` section of the
  Characters panel (`AddSovereigns`).
- Buttons have a `character-roster-grid` class, so they get the size and frame of the list entries.
- Awakened ruler whose player character list contains (`ListedAsCharacter` →
  `CharacterRosterPanel.Lists`), hides his button.
- After `Apply` panel reports `Applied`, and integration sets section visibility
  (`ShowSovereigns(HasShownPortraits)`) and an empty list string.

The window of the ruler.
- The window anchor (`Source`) is a button, if visible, or `FallbackAnchor`, or Characters tab. This
  also applies to the inspector of the awakened ruler.
- `Highlight` reports `Highlighted(any)`, which turns `legal-target` on the page.
- `EdgePanelBusy` holds the Characters panel developed as long as the ruler's window is open.

**Inputs from other places.** `ActionContextRouter` points to a panel or Characters tab for the sovereign context. `BoardEscape` points to the focus of the tab.

<a id="powiadomienia-poza-układem-przełączanie-panelu-regionu--ui-103"></a>
## Off-system notifications, switching the region panel — UI-103

**Notifications.**
- `ActionNoticeBar.Root` lies in the root of board behind the stage, so it draws over it. It has
  `position: absolute` under the top bar (44 pt) and width resulting from the content
  (`Styles/Notices.uss`).
- It does not take up the space in the column: the scene and `topbar-shadow` begin at the bottom
  edge of the top bar.
- `CardBounds` continues to press card action for notifications.

**Panel region.**
- `RegionHover` remembers when pressing the map (`pressedOpen`) region of the open panel. Pressing
  the site closes the panel before clicking.
- `Toggle(region)` closes the panel when the click hits the region open at press or still open (the
  army tag does not close it) or opens it.
- `TurnInteraction.ChooseRegion` and `ChooseArmy` use `Toggle` where clicks are not a legitimate
  target; `Open` stays for program calls.

Corrections by the way.
- On the low board card action has `max-height` equal to the place under the notifications (highest
  460 pt) and its steps scroll. Otherwise, the floating window would press card down the stage and
  push it to the notifications.
- `CharacterRosterPanel.PositionInspector` counts the rectangle of the list from its layout. The
  scale of the entry (0,96 in horizontal) narrowed `worldBound`, so the inspector then came to the
  edge of the developed list.

<a id="płótno-mapy--ui-102"></a>
## Map canvas — UI-102

Camera and map area.
- `MapCanvas.CanvasBounds` (with `MapCanvasLayout`) is a canvas rectangle in the map units;
  `TerrainBounds` remains a map rectangle.
- `Camera.Constrain` in `Refresh` and in history-frameping uses canvas, so `MinimumZoom` is a cover
  screen for canvas.
- `Fit` and `FitZoom` (marker thresholds) count from the map.

** Hits (`HitTest`) mark the contours of the regions, so the margin point does not give the region a hover, click, right click or drop.

<a id="ruch-kliknięciami-na-mapie--ui-105"></a>
## Move clicks on the map — UI-105

Enter.
- `DraftInput.Move` does not start drag for `DraftObject.Army`, `Figure` or `FellowshipPortrait`.
  Pressing and moving the indicator does not capture it and show no spirit.
- `TurnInteraction.Drop` no longer has branches of the army, figures, travel, battle or portraits;
  `RegionHover.PreviewDrag` only responds to reserve.

The figures election.
- `ActionContextInteraction.SelectFigure` (region panel) first joins figure in an army collected in
  the same region.
- Then he asks `FigureSelector` (a journey of the character).
- At the end of the army begins: it is joined by characters and Nazgûle previously selected in this
  region, so they do not lose the mark.
- `CharacterRosterPanel` does not choose figures; the line only opens the inspector.

**Character purpose.**
- `TravelDraft.TargetRoute` includes types with route outside the declaration and disclosure of
  Fellowship.
- `TravelDraft.Reachable` gives highlighted targets; `RouteTo` sets the shortest legal route (BFS
  within range, without going beyond the detention regions and without being blocked) or will
  remain.
- `TurnInteraction.ChooseRegion` calls `TravelContextInteraction.Reach`. Another region opens or
  closes its panel.
- `Reach` sends a step immediately when draft is ready. Exception: travel with card outside the
  action phase, in which one command includes several groups (group type, `MaximumGroups` different
  from 1). Then click on the group target and wait for common approval. This only applies if another
  figure event can create another group.

*A click of the terrain on the open panel.**
- `RegionHover.PointerDown` closes the panel when pressing hits the field. The tags then work
  without occlusion of the panel.
- `MapCanvas.PointerUp` accepts playing over the map or over her child (`OnMap`) when `HitTest`
  gives the same region.
- Pressing started on the tag does not go to this path because the tags stop propagation.

**Card flights (UI-212).** A card's Nazgûl flights (`CharacterEvent`) no longer use the exception
above: `NazgulFlightInteraction` runs before `TravelDraft.TargetRoute` in `TurnInteraction.ChooseRegion`.
A lit source arms its unmoved Nazgûl and opens its panel; each lit destination flies one; the console's ✓
sends the shared command. Details: [CARD_CHOICES_UX](CARD_CHOICES_UX.md).

**Card figure choices (UI-215).** `CardFigureInteraction.Region` runs before the card branches of
`TurnInteraction.ChooseRegion`: a lit region holding a card's offered figures (or excess figures outside a
battle) opens its panel, and `FigureSelector` routes the panel's figure clicks to it before travel. Details:
[CARD_CHOICES_UX](CARD_CHOICES_UX.md).

<a id="kolejność-lewej-listwy--ui-107"></a>
## Left strip order — UI-107

`BoardEdgeLayout` adds the tabs in the order Politics, Characters, Setup, Effects; the order of the children `board-left-tabs` sets the layout from the top. Styles `LeftRail.uss` do not depend on the position of the tab.

<a id="wejście-postaci-z-listy-characters--ui-108"></a>
## Character input from Characters list — UI-108

**Line.**
- The list line has a `DraftObject.Character` chip, other than figure on the map. It cannot be
  dragged.
- Two-click (`DraftInput`) goes to `TurnInteraction.EnterFromRoster`.
- Enter/Space calls `CharacterRosterPanel.EntryRequested`.

- The choice of entrance.
- `CharacterRosterContext.Entry` returns the only `EnterCharacter` enabled for figure at current
  cost. With several candidates, it returns null and the buttons remain in the inspector.
- The entrance goes through `ChooseComponent` so far. It is instant when ready; at several regions
  it is completed by a click of the region (`CharacterDraft.Region`).

No window.
- `CharacterRosterActions.OwnsDecision` omits `EnterCharacter`, so the tray does not go to the
  inspector.
- `ComponentContextInteraction` does not enter the `board.Components` or the Council chip panel.
- `CharacterRosterPanel.Enterable` disables opening the inspector by a single line click that can
  enter.

<a id="zwarty-panel-polowania--ui-109"></a>
## Hunt — UI-109

- `HuntPanel.Underway` is true at assignment, current Hunt (`view.Hunt`) or unfinished report.
- The panel also has a `hunt-idle` class. It does not then show the report or the last Hunt with
  `HuntActions.Displayed`, and the title is "Hunt".
- `HuntBoxView` does not have a label "HUNT BOX" and hides the row dice Free Peoples at zero.

<a id="ruch-drużyny-ikoną-toru--ui-110"></a>
## Move Fellowship track icon — UI-110

**Track icon.**
- `FellowshipTrackRow` has a `Move` button with a `track-move-glow` symbol and layer.
- `Offer` turns on the button, class `track-move`, tooltip and pulsing (`track-move-bright` every
  700 ms; transition only at `motion-enabled`).
- The visible track gets the name `fellowship-progress` (`FellowshipStatus.OfferMove`).

- The choice of motion.
- `FellowshipControls` has no traffic button. Counts `MoveOffered` with `FellowshipActions.Option`
  and a description of the effect.
- `FellowshipPanel` transmits the icon clicks as `ActionSelected(MoveFellowship)` to the same path
  as the former button.

<a id="proste-powiadomienia-i-pergaminowe-karty--ui-106"></a>
## Simple notifications and parchment card — UI-106

- `ActionFactText.Effects` is a mode without source, changes to dice, budget and page name running.
  Exception: dice assigned to the Hunt field gives "Hunt Box +N". `ActionFactText.Lines` is a
  complete record for the history list.
- `ActionNoticeText.Sentence` gives a sentence without a page. `ActionNoticeStack.Tooltip` only
  shows them.
- `ActionCard`:
  - shows the page in the header and status only with incomplete action;
  - omits the steps without effect;
  - played card (`ActionCardUse.Event`, `CombatReveal`) adds as `action-played-card`.
- The `CardInspector` parchate is in `Styles/Cards.uss`, card action describes `Styles/Notices.uss`.

<a id="niedostępne-karty-walki--ui-111"></a>
## Inaccessible card combat — UI-111

- `CombatCardCondition.Eligible` reads `EligibleIds` options
  `CommitCombatCard`.
- `CombatCardCondition.Text` returns the first sentence of the fighting part beginning with
  “Requires” or “Not playable in this battle”.
- `BattleCombatHand` adds `combat-unavailable-<id>` buttons: disabled, without `DraftToken`, with
  `combat-choice-condition` label.
- `HandView` adds the `combat-unavailable` class and a hint with a condition.

<a id="ikona-ostatniej-walki--ui-112"></a>
## Last fight icon — UI-112

- `BattleReport.RoundKey` is `battle-round/<mecz>/<bitwa>/<runda>` with
  `PlayerView.LastBattleRound`. The server changes this round once, after discovering cards
  (`BattleProjection.RememberRound`).
- `BattleReport.Apply` shows `last-battle` only for rounds outside the set read.
- `ShowLast` ( ⁇ click) and `Show` Report the same battle with the finished round of this issue add
  the key and report `RoundRead`.
- `BattleReportController` saves the key in `NotificationReadStore` (server range + account + party,
  `notification-read` file). On startup, it transmits the saved keys to `BattleReport.MarkRead`,
  which only accepts the prefix `battle-round/`. Without storage, the icon disappears for session
  time.
- The hidden ⁇ loses focus, and the key would then go to the root of the panel, except
  `BoardEscape`. `ShowLast` in the next frame transmits focus of the report window (`FloatingWindow`
  has a focusable root without a tooltip), so Esc closes it.

<a id="korekty-zachowania-w-regresji-menu-08"></a>
## Behavior corrections in the MENU-08 regression

acceptance started on UI-97 transitions to completed UI-98/UI-99. Esc closes the selected panel without reopening by the focus restored, also when the focus returns to the tablet inside the corner tab. Fresh override, click or new focus from the keyboard continues to expand the panel. Inspector Characters recalculates the position after the animation of the parent.

The group's movement allowed by the server options can remain openly in the starting region: the first click of the region prepares the route of zero length, the final click approves this step. We do not automatically select the target or change the legality. TravelActionTests also checks the behavior of the exact dice cost.

Combat miniature does not change position after opening full card: the location of the preview is reserved when opening your Combat decision. The two-click remains one gesture at the same place. The Army foot wraps the message in a separate row in a short view. FitHand retains a new distinction of preview from the held panel, also counting actual margins and padding hand.

The default information window bypasses the left slat and is located next to the attached card. Help (to UI-173) bypassed the visible Fellowship location buttons and Hunt help. The new menu does not receive the expansions option.

The thick view profiles check Pick for all currently open controls. When the panel is rolled in, they do not expect access to its internal button; separate UI-98 tests check development and interactiveness of the full form. Authoritative status, costs and privacy still have strict assertions.

The focus transition between the panel and the poster belongs to the same group. It does not remove the focus lock restored by Esc. The Characters profile returns to the tab with the cursor's actual motion when closed, without attaching. The dense profile reads the Corruption change from the visible plaque when the full panel is rolled up; visibility also includes all the parent elements.

Esc also suppresses the cursor input from the pointer itself when the tab is exposed under the motionless pointer after the panel is rolled in. It is only when the cursor position is changed that unlocks the hower. The click continues to expand the panel immediately, and the focus suppression is applied to a particular plaque to which Esc has restored it; moving the keyboard to the second tablet is the new conscious input.

The interactive preview must keep its own source and targets when dragging started inside the panel. The owner indicates the actual item remembered when pressing, transmitted by DraftInput; we do not guess it from the type command. The cursor cannot start scrolling such panel. After finishing or canceling the gesture, the normal exit and focus conditions apply again. Previews not related to the source still disappear. The regression of the Buster requires a visible, attainable policy slot after 350 hold and zero revisions before the release; the legal final drop continues to perform exactly one step.

<a id="stan-naciśnięcia-i-ilustrowane-okno-bitwy--ui-113"></a>
## Press condition and illustrated window battle — UI-113

User decision from 2026-10-13 completes the “information on the hower, click action” model by third state: press. Each click item (button, die , card , loss token, medallion, tab, map region) suggests a clickability at rest, after PointerDown Apparently, it's pushing in. `pressed` , offset 1–2 pt and darker tint or variant of graphics, transition ≤ 80 (ms), a action performs only release of the button inside the item. PointerLeave/PointerCancel And the beginning of drag, edge you and master maps take off the state without execution action . Enter/Space works the same way for the key push time. The limited traffic disables the pass, not the state. Hover still does not open the panels or does not perform action .

Implementation and plug-in (`PressFeedback`, `Interaction.uss`, regions in `MapCanvas`) and design of the illustrated battle window, which shows this principle at reroll, losses and card combat, are in [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md); implementation receive UI-114–UI-121 in the `wotr-server` register.

<a id="wdrożenie-stanu-naciśnięcia--ui-114"></a>
## implementation press condition — UI-114

One behavior `Runtime/UI/PressFeedback.cs` supports all board : Buttons ( `.action` , Bookmarks, Window Buttons, Medals dice , card from the hand, dice combat, loss tokens), elements `pressable` and, by his own condition `MapCanvas.PressedRegion` State, the map area. `pressed` not execute command , does not change draft or markings; two-click and arms card ed UI-91 and the discard of excess works unchanged. `pressed` The graphics are in `graphicalassets/ui` , rules in `Styles/Interaction.uss` Evidence: `PressFeedbackTests` (Editor), `PressFeedbackClickTests` (Play, drop `ui114-pressed-loss-figure.png` , `ui114-pressed-region.png` ).

<a id="bez-obrysu-fokusu--ui-191"></a>
## No focus outline — UI-191

User decision from 2026-10-16 : white outline rectangle never appears, with any object, also by keyboard and text fields (chat, login, invitation code). Client no longer has `FocusIndicator` , `InputModality` style `.focus-indicator` neither class `focus-outline` li card Focus, Tab, Enter/Space Esc, seal-to-focus tooltip, `pressed` ( UI-113 ) and `LeftRailPreview` The focus control can still have its own state. `:focus` (same background or graphics as with the hiver), but without a separate frame. Evidence: `NoFocusOutlineTests` (click, key, text field, focus from code) and `HandCardLayoutTests` ( card with no outline).

<a id="obrys-fokusu-tylko-z-klawiatury--ui-122-zastąpione-przez-ui-191"></a>
## Focus outline from keyboard only — UI-122 (replaced by UI-191)

The user's decision with 2026-10-13: the white focus contour does not appear after clicking. `Runtime/UI/InputModality.cs` remembers the last type of panel input, similar to `:focus-visible` in the browsers. PointerDown switches to the indicator; a keyboard other than the modifier itself (Shift, Ctrl, Alt, Cmd) and navigation events switch to the keyboard. The listening is running in the TrickleDown phase on `panel.visualTree`, because the focusless key goes to the panel root, not the document. Before the first entry, the keyboard is valid, so the focus given only by the code still has the outline.

`FocusIndicator` follows focus so far, but with the indicator it hides the contour and stops tracking without `panel.Pick`. The key with the preserved seal immediately shows the contour on the current control, for card on the lid (UI-93). The focus transmitted by the code after clicking, e.g. the window with the open click, there is no contour. Focus, tooltip after the seal, Enter/Space, `pressed` and the `LeftRailPreview` rules remain unchanged. Evidence: `FocusIndicatorTests` (`UI122ClickKeepsFocusWithoutOutlineUntilKeyboardInput`) and `HandCardLayoutTests` (the face only after the key).

<a id="armie-na-mapie-ruch-i-posiłki--plan-ui-123"></a>
## Armies on the map, traffic and meals — plan UI-123

User decisions with 2026-10-13 save [ARMY_MAP_UX](ARMY_MAP_UX.md) : armies always visible in two levels (token, figurines on the stand), click your own army at dice Army/Character highlight the entire army, the meal panel for Muster with the reason for the blockage from the server and the distinction cards selected to play dice . implementation and acceptance : UI-124 – UI-132 in the register `wotr-server` . The descriptions shall apply to their closure. UI-30 , UI-73 , UI-76 , UI-103 and UI-105 above.

<a id="wdrożenie-znaczników-armii--ui-126"></a>
## implementation Army Markers — UI-126

`MapMarkerVisibility` selects only the level: `Detail` ed 1,3 × to drop below 1,2 × match. `MapCanvas` puts the forces of the region in one row (garnison, then field forces) over the symbol settlement and in the region without settlement on an anchor. A government that would be on a set marker (from UI-199 Not on panel anymore HUD or open panel of the region), tries to turn to the right, left and bottom sides; the government shown with figures only because of zoom then returns to chips, and the government of the region open by clicking, with the chosen force or upon arrival becomes figures. The government of the region opened in the panel does not bypass this panel; it is a panel ( `RegionHover` ed `MapPanelPlacement.Near` ) avoids `MarkerBounds` So the cursor mark doesn't run away when you open it.

<a id="wdrożenie-ruchu--ui-127"></a>
<a id="implementation-z-moj--ui-127"></a>
## implementation with my — UI-127

- `TurnInteraction.ChooseArmy` in the active context of the army is called
  `ActionContextInteraction.SelectArmy`: the legal source gets `ContextArmySelection.SelectAll` in
  one gesture and an open panel; the choice of the same force remains (dividing survives another
  click); the force without option only switches the panel so far.
- `TurnDraft.Gesture` saves one step of undoing to gesture (an army selection, click figure, two
  clicks), instead of a step on each edit of draft.
- `BoardEscape` at the visible panel of the region first calls `ClearSelection`
  (`TurnInteraction.ClearArmySelection`: pure draft with the same dice and ability, the sequence
  rebuilds its step); the next Esc closes the panel. The key goes to the focus element; when the
  focus is nowhere, Esc goes to the panel root, except board (state finds).
- `ArmyGhost` builds a selection model and target signatures only with options
  (`ArmyActionTargets.Options`, `BattleMode`); `MapCanvas.SetGhost` draws an uninteractive
  `army-ghost` spirit with a signature next to the target force at `SetHovered`, without queries to
  the server.
- `ActionSequence.UsedIds` gives `moved` figures status in the panel (`RegionHoverPanel.Moved`) and
  on the tag (`MapCanvas.ShowMarched`, when all figure forces have moved).
- `ArmyActionTargets.Blocking` lists figure outside `EligibleIds` of any purpose; the text trays and
  help no longer mention the army's drag.

<a id="jeden-przepływ-ruchu--ui-128"></a>
## One movement flow — UI-128

The path inventory and justification of exceptions (the ruler's token, card with routes and rescues) is in [ARMY_MAP_UX](ARMY_MAP_UX.md), ‘implementation UI-128’. `DraftGesture.Drop` rejects armies, and `BoardDraft.Drop` only supports reserves; the dead army drag branch with UI-105 disappeared.

<a id="panel-posiłków--ui-130"></a>
## Meal Panel — UI-130

- `ReinforcementsInteraction.Refresh` (from UI-131; in UI-130 `MusterContextInteraction`) builds
  `ReinforcementsState` at each view and draft; the panel rebuilds the lines only when changing the
  set of nations, and updates the chips in place.
- `ReinforcementsPanel.FigureChosen` → `MusterActions.Select` (one step back, without command);
  `MapDraftOverlay` shines only settlement figure. Click settlement →
  `MusterContextInteraction.Target` sends step.
- `DraftInput` transfers a two-click token to `MusterContextInteraction.DoubleClick`: when the panel
  of the region is opened, it recruits in it, otherwise in the only legal settlement; drag the token
  to settlement sends a step.
- `BoardEscape` closes the panel as a normal window; die stays. `DecisionTray` shows the
  `open-reinforcements` button when the Muster is selected.
- ✓ in the header causes the same `FinishActionSequence` tho `ActionProgress` and is visible only in
  the viewer's recruitment sequence.
- The settlement (opening the region panel) click reports the nation by
  `RegionHoverPanel.SettlementInspected` (to UI-135 `RegionRecruitPalette`); the panel denotes its
  poem.

<a id="rekrutacja-z-kart-i-zdolności--ui-131"></a>
## Recruitment with cards and capabilities — UI-131

- Plan card or capacity ( `ReinforcementPlans` ) has priority over Muster; click the chip edits
  draft one step through `EditChoice` , so single choice ‘ ReserveFigures “ Still send itself as in
  a tray.
- The region click on the map goes the way it has been (`EventRecruitDraft.Region`,
  `CharacterDraft.Region`) and the drag of the token by `DraftGesture.Drop`.
- × spirit on the map (`MapPlannedRecruits.Removed`) and × in the panel
  (`PlannedReinforcementsView`) return figure to the reserve; the spirit cannot be clicked and ×
  stops pressing before the map treats it as a region click.

<a id="jedno-okno-rekrutacji--ui-135"></a>
## One Recruitment Window — UI-135

- The region panel has no reserve pallet (`RegionRecruitPalette` removed); it only reports the
  nation viewed settlement (`SettlementInspected`) and `ReinforcementsInteraction` stands for its
  line and restores the closed panel.
- `MusterContextInteraction` supports only the settlement click, the two-click chip on one
  settlement and the drop of the token. When the selected figure cannot take the settlement click
  from its nation, the tray shows the server's reason (`ReinforcementRows.Refusal`).
- Recruitment without a step sequence (lord's token, ability) is the source of the
  `MusterPlanReinforcements` plan on `RecruitDraft`; the tray shows the button "Reinformations", and
  the `MusterChoices` pallet has become `ExcessChoices` only for excess.

<a id="zaznacz-i-odznacz-wszystkie--ui-140"></a>
## Select and uncheck all — UI-140

Application of the user with 2026-10-14: when selecting units to move, always need the select/unselect button all.

- The header of each force in the region panel has a switch (
  `hover-select-all-<strona>-field|garrison` ) visible only if any figure This force can move the
  chosen one dice.
- Click selects all figure forces that can move (`ContextArmySelection.SelectAll`); when checked,
  the next click leaves them all (`ActionContextInteraction.ToggleAll`). One click is one step
  undoing, without command. figure after the march is done, they remain out of the choice.
- Tooltip tells you what to do (Select all or "Select all"). Characters ⁇ /☐ replaces the graphics
  in UI-138.
- The choice of characters to travel in the Characters panel and figure Army selected by card in
  tray have their own gestures (UI-67, UI-128); the switch concerns Army movement with dice.

<a id="poświata-opcji-wybranej-kości--ui-136"></a>
## Highlight of the selected option dice — UI-136

The user request from 2026-10-14: each die highlightes available options, e.g. Will of the West indicates that you can move Fellowship.

- `DieOptions.For` counts from server options (`ActionDieContext.Allows` and dice at action with
  character), which opens the selected die: characters (entry, ability, character movement),
  politics (step, Narya, Council chip), Fellowship (move, hide), card in hand, select card, armies
  and recruitment.
- `DieOptionGlow` adds every control child with a glow `frames/glow` : Characters tab, Politics tab,
  plaque Fellowship and both decks. Switches only class `die-option` ; Light does not accept cursor.
- The light is shining when the die is selected in the player action decision, and draft is not yet
  of the action genus. card in hand (UI-124), armies (UI-126) and meal panel (UI-130) have their own
  distinctions.
- The Utility of dice action, dice opponent and dice fight no longer draws the pale rectangle behind
  dice; only the graphics of dice itself are changed (user notification with 2026-10-14).

<a id="złoty-medalion-dostępnej-zakładki--ui-193"></a>
## Gold medallion available on the tab — UI-193

User application with 2026-10-16: left-strip bookmarks are the only medallions without background, so it shines a medallion, not a rectangle around a non-existent background; the available tab shines gold.

- The tab is available when it is a legitimate target for the current decision (`legal-target`) or
  can be used by the selected die action (`die-option`, UI-136). Both signals show the same variant
  of `rail/edge-tab-available`: a golden glow around the medallion; when the
  `edge-tab-available-hover` is invaded. The green variant of `eligible` of the bookmark is gone.
- The marked tab (open panel) becomes a red laze, and the press is pressed with `pressed` graphics,
  also when available.
- `DieOptionGlow.Light` switches only the bookmark class, without a child with a rectangular glow
  `frames/glow`. The Fellowship tab and decks that have their own background, retain the glow with
  UI-136.
- UI-196 (user's report with 2026-10-16): The Characters page only shines when the selected dice can
  be done: enter the character, use its ability or move paid by the character's ability. The usual
  move of Companion and servants takes place on the map, so the tabs do not highlight.

<a id="inwentarz-dwukliku--ui-142"></a>
## Two-click inventory — UI-142

The user's decision with 2026-10-14: the game does not use a double click. `DraftInput` no longer recognizes the double click; `DraftClickSequence` only leaves the protection: quick click, which continues the series of devices after changing the scope of the decision (match, revision, decision, connection period), is omitted. In the same decision, each click is a simple click.

| A Past gesture (dwuklik) | Replacement from UI-142 |
| --- | --- |
| Characters row — character input (UI-108) | The line click that the selected cost can enter; another line opens card. |
| card in hand — play | The first click armed, the second plays (UI-91, unchanged). |
| Over-hand — card recoil (miniature or preview) | One click of card or the content of its preview. |
| Response Hunt (Ring, Guide, Random) | The answer click ends step; drop on the tile stays. |
| siege extension — figure extension | The figure click extends the siege. |
| Reserve chip at one settlement (UI-130) | The chip click recruits in the only legal settlement; with several settlements they shine. |
| Army — Selection of the whole (UI-127) | Army click selects the whole; switch select/unselect (UI-140). |
| End of route Fellowship/figure | Figures: click the destination (UI-105). Fellowship: the step that uses all of the Progress, or ✓ for a shorter route (UI-219). |
| dice reroll and losses in battle | Each click switches/cyclically changes the selection; aggregate ✓ He's staying. |
| card combat in battle window | First clicks armed, second one plays covered. |
| Content of card in the inspector with excess hand | The content click rejects card. |

Complex elections (loss set, reroll, route, versions Companion with Guide when preparing Fellowship,  figure march) ends a public click target or one ✓ Tests: `EngineClickTests`,  `DraftInputTests`,  `ContextHoverTests.AFastSecondClickCannotCrossDecisionBoundary`,  `HandDiscardNetworkTests`,  `EngineHandDiscardNetworkTests`,  `CardServerCompatibilityTests`,  `StrongholdNetworkTests`.

<a id="pierścień-elfów-i-konsola-tury--plan-ui-158"></a>
## The Elf Ring and console turn — plan UI-158

Using the Elves Ring is a ring gesture →   die   → result: ring click in the highlight console dice of option `UseRing`, click dice opens over it a selection of walls with `option.Faces`, click the walls send command without Confirm; Esc or click outside cancels. UI does not force the changed use dice  (B05 Allocation Hunt: Click dice She's moving her to the field. Hunt in the console, click in the field withdraws, one ✓ Details: [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md) Sections A, C and D.

<a id="małe-okienko-zwykłego-regionu--ui-188-2026-10-15"></a>
## Small window of the normal region — UI-188 (2026-10-15)

User notification from 2026-10-15: “Draw the tooltips to the basic regions that contain only their names, have a lot of free space, reduce them, are to be small and neat”.

- Region without power window, Fellowship, effects and action gets `name-only` class
  (`RegionHoverPanel.NameOnly`):
  - width by text (at most 440 pt) instead of constants 240 pt;
  - two lines: name (Cinzel 15 pt) and state (Wilderness · Neutral · Uncontrolled), 11 pt) without
    label spacing;
  - without the line ‘No forces’ and empty line of effects;
  - 5–6 space from frame.
- The power window, Fellowship, effects or action retains a fixed layout (fixed size rule). The
  appearance of action (e.g. the expansions component) removes the class.
- The location next to the region counts from the actual size, so a small window stands right next
  to the region.


<a id="okno-regionu-znika-po-odjechaniu-kursorem--ui-189-2026-10-15"></a>
## The region window disappears after passing the cursor — UI-189 (2026-10-15)

User decision: The region information window, when you click, disappears when the cursor leaves the region and this window. This is an exception to the principle that interactive panels stay after the cursor leaves (R19).

- `RegionHover.FollowPointer` checks the cursor when each move is not pressed and without moving the
  map. The window stays when the cursor is:
  - over the open region (terrain or army marker of the region);
  - over the window;
  - over preview opened from window (card character, `RelatedInspection`).

Where else the window closes after 0,3 s (`LeaveDelay`), unless the cursor returns earlier. This allows you to drive from region to window through the slot.
- The cursor output from the game window works the same. A gesture with the button pressed (stuck,
  moving the map) cancels the countdown.
- No change:
  - opening requires either Enter/Space and the overground only the highlight;
  - click on the open region to switch the window (UI-103);
  - click next to the window and Esc closes the window.

Selected in the figure window belong to draft move and remain after the window is closed.
- It also applies to a small window of the name itself (UI-188).

<a id="panele-lewej-listwy-przy-swojej-zakładce--ui-190-2026-10-15"></a>
## Panels of the left strip at its tab — UI-190 (2026-10-15)

User decision: the left-hand tab panel opens at the height of its tab, not always at the top of the strip.

- `BoardEdgeLayout.AlignWithTab` connects Politics, Reserve, Characters and Effects pairs to their
  panels. Each time the panel geometry changes or the top edge of the panel is placed at the level
  of the tab.
- When the panel is not above the bottom edge of the map, it moves up just enough to leave 8 on the
  backup. It does not go higher than the top of the strip.
- No change: width, preview after invading (UI-92/UI-98), insert animation, Esc and decisions in
  panels.

<a id="osady-jako-kamienie-szlachetne--ui-195-2026-10-16"></a>
## settlement as precious stones — UI-195 (2026-10-16)

User application from 2026-10-16: “Add nice colorful city tags, sediments and ruins, let them resemble made of precious stones in player's color”.

- `SettlementGem` (geometry and colors, without drawing) gives each type of settlement its own
  grinding:
  - Town (Town) — round diamond, radius 6 pt;
  - City (City) — emerald, wider than high, 7,5 pt.;
  - Stronghold (Stronghold) — cushioning cut 7,5 set in gold ×1,3;
  - Fortification (ruins: Osgiliath, Fords of Isen) — a trillion blade up, 8 entitled
- The color of the stone is the side that controls settlement (`BoardPresentation.Control`): the
  sapphire Free Peoples, the ruby Shadow; without control of the pale quartz. The acquisition
  changes the color of the stone, so the former triangle or the page romb next to settlement
  disappeared. The shape tells about the type of settlement, and the region panel and tooltip about
  the control, so the color is not the only information.
- `MapSettlementVisual` draws a stone through `Painter2D` (acute at each magnification):
  - planes between the roundabout and the panel, brighter from the side of the lamp to the left at
    the top;
  - a brighter pan and a small flash;
  - a dark outline that separates the stone from the bright and dark terrain.
- The army government stands 12 set above the anchor settlement (ex 8), over the largest stone in
  the frame. The region name zone (`MapRegionNamePoints`) remains unchanged: its extension hid one
  name approximately, and the name test passes with figure UI-192 at the former zone.

## Fellowship figure inside its region — UI-228 (2026-10-17)

The Fellowship's figure in an ordinary region stands above the region's forces when its feet fall inside the region at
the current zoom. Otherwise it stands below them, then right or left of them (`MapFellowshipMarker.RegionCentre`).
Gorgoroth's point lies just under the Barad-dûr border, so its figure used to stand in Barad-dûr and now stands below
the forces. A region too small for every place keeps the figure above its forces, as before. On the Mordor track the
figure stays on its step, which lies inside Gorgoroth (the Crack of Doom is drawn at Barad-dûr's foot). Test:
`MapContourDataTests.FellowshipFigureStandsInsideGorgoroth`.
