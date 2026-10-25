<a id="mapa-okna-i-polowanie--ui-30ui-35-oraz-ui-48ui-54"></a>
# Map, windows and Hunt — UI-30-UI-35 and UI-48-UI-54

Plan accepted 2026-10-07. The state of performance is defined by task registry; the target description does not constitute proof of acceptance. UI in English, replacement graphics.

Panel Extension Fellowship and detailed breakdown UI-34 recorded in [FELLOWSHIP_UX](FELLOWSHIP_UX.md) and documentation UI-36 . UI-33 He's still delivering the report. UI-34 is acceptance six parts: PANEL , ROUTES , GUIDES , HUNT , RESPONSES and MORDOR ; UI-35 preserve acceptance the entire range of the map, cards , windows, Fellowship and Hunt The description below remains a contract Hunt and F01–F07 specify its operation.

The later agreed order Hunt describes the section “New layout Hunt”. UI-48 receives documentation, UI-49–UI-53 implement, and UI-54 integrate. The results of previous tasks do not receive a new layout.

<a id="mapa-i-karty"></a>
## Map and card

Remote hides tags figures , while maintaining the regions, settlement , control and legal purposes. Hover site only gently illuminates the region; the composition description opens click or Enter/Space. The superstructure can reveal figure according to the visibility rule of markers, but does not open the region panel. The selected army remains visible until the end of the selection. Zoom from 160% matches show figure ; return below 145% hides them without flickering. Collision gives a compact stack at the region instead of a distant label. Hidden tags do not capture clicks. From UI-126 armies do not disappear: below 1,3 × Match (return 1,2 ×) are tokens with crest and number, above the figures on the stand ( [ARMY_MAP_UX](ARMY_MAP_UX.md) ); the same threshold shows the tag Fellowship .

The two-click rejects one card excess, a single click just watches it. Each discard is saved on the server, and the continuation is waiting for the limit of six. The hand remains available, without Confirm; the remaining excess at the Cards tab. The gesture does not pass to the next card or decision. The discards at the expense of the event do not get new semantics.

<a id="okna"></a>
## Windows

Windows kept open and pinned The previews have a drag header. The controls are not handles. The position remains at zoom, projection and reopen, only in the memory of the current client session. The reboot restores the layout. Viewport and scale restrict the position; the layout reset is available in the settings. We open the panels at the objects by clicking or Enter/Space ; the cursor leaves does not close them. Bookmarks and trays dice remain constant. From UI-80 only passive tooltip appears after 200 ms howera either immediately after the seal. Reads the existing allowed metadata, does not download data, does not change the choice and does not capture the click or drag. Leaving, losing focus or starting gestures extinguish the description. Esc extinguishes the tooltip and still closes the active window. Details: [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md) .

<a id="przebieg-polowania"></a>
## Hunt sequence

Click or Enter/Space on the fixed eye next to the status of Fellowship opens a separate sliding window. Hover/fokus gives only an information tooltip. The new decision opens the window automatically without changing the camera. The closing continues until the next decision, not the next projection.

1. Allocation: click a specific dice adds it to the set (UI-88), drag on the Eye too. Reclick it
   withdraws. One Allocate & roll approves the set, also zero if legal.
2. The throw: graphic meshes, hits and boxes. Bonus and the sources of the spreaders have tooltips.
   The server draws, animation only shows the known result.
3. Transfers: selection of legal failed dice and one approval; empty set saves the score. No results
   revealed between the selection of dice.
4. Tile: central object, independent injury symbols, Reveal and Stop. Undetermined value remains
   unknown. Previous/listed tiles are in compact run, without a list of all possible future
   effects.
5. Reactions: legal card or portrait in its rules window and public resignation.
6. Damage: Ring, Guide portrait, covered stack of random Companion. The raid explains the effect.
   First click selects; two click, drag on a tile or click object → click the tile makes the
   decision. Random never chooses ID victim.
7. The following: new Guide/zdolno, other Corruption, disclosure and additional tiles according
   to the server. Route on the map without automatic camera.
8. Mordor: skips the throws; the motion attempt does not promise to move before the tile. Stop and
   Reveal remain different. Corruption 12 can finish the game before the step.

The current step dominates; full explanations are in tooltips. The last score remains under the Eye also after automatic decision-free settlement. The report is typed, perpetuated and public; it does not parse textual history, does not reveal future draws/pulls and does not reconstruct the unknown past.

<a id="odbiór"></a>
## Acceptance

Historical acceptance UI-30 – UI-35 ed 2026-10-07 UTC and subsequent corrections before UI-80 The following results maintain the behavior of the time hover/pin ; the current opening rules are defined by the sections ‘Owns’ and ‘Runway’ Hunt This is not evidence. ” acceptance new gestures UI-80.

UI-34-RESPONSES: Ring/Guide/Random prepares the local selection, and the final click on the tile, double click or drop performs it. Random is a covered object without ID victims. Known injuries, remaining value and Corruption are in the preview; Hobbit's return is not confused with the separation ability. Reactions have Skip and source inspection. Once the tile details of the previous throw remain in the report, and the window focuses on the current step.

Sources: BASE p.19,22–23,36–43; T16-HUNT, T17-MORDOR and T21-HUNT-INTERACTIONS. Each task has its own tests and checkpoints. acceptance integration UI-35 includes both clients, restart/receipt, four 1366/1920 × 100/125% circuits, a review of the dumps and structures, a build macOS. Windows runtime and manual tests postponed.

The UI-32 tightening: drag header temporary preview is clearly clipping it. The focus header also supports the arrow. Clicking the hand tab or policy keeps the panel open with the × button; the hover itself continues to close after exit.

UI-33 provides durable `HuntReport`: common ID chain, known throws, tiles before/after exchange, reactions, departures/returns, guides and result. Automatic box and cancellation without a tile also leaves a report. acceptance data: 416 tests .NET and 260 EditMode PASS; new view Eye remains within the scope of UI-34-HUNT/RESPONSES. Details and limitations in UI-33.

UI-34-HUNT connects the report to a separate Eye and transfers the allocation, spreads and existing reakcje/obra. The regional route still has one owner at Fellowship; Eye remains an overview of the effects. Specific die allocation selects before clicking the Eye or through drop; the undo is local to Allocate & roll. The last report runs after the automatic box. The results and limitations of the parts record UI-34-HUNT/RESPONSES/MORDOR. The Combined F01–F07 matrix and the transitions between windows receive UI-34.

UI-34-MORDOR adds a public nullable sample marker in turn. In Fellowship it distinguishes the reminder about +1 Corruption, confirmation of the sample and unknown information from the older DTO. Hunt displays `No Hunt roll` and the value of Eye. The input retains Hidden/Revealed; the trial and Hide use the indicated dice. The result comes from the server, and the recovery of the final uses an existing receipt.

Integration UI-34 checks one cost and one public report by Guide, Reveal, additional fortresses and later Hide. Uncertain receipt shows Check result in the available window also after moving to the enemy's decision. Detailed matrix and results: UI-34.

UI-35 reopens range UI-30 – UI-34 on the real server and in four systems. The discard continues to save one card on a two-click, and the default preview saves space for the whole hand also after changing width or scale. After manually shifting, the player's local position is valid. The focus has a layer of his own window. R23 and local package and postponement saves UI-35.

<a id="nowy-układ-polowania--plan-ui-49ui-54"></a>
## New Hunt layout — UI-49-UI-54 plan

Agree with 2026-10-07 cleans the existing interface using HuntView , HuntReportView , current Decision, legal options and preview. command authorisation and RNG We do not create a second log or command sender. UI and labels are in English; replacement symbols have text and can be replaced.

<a id="cztery-części-okna"></a>
### Four Parts of Window

1. Header: Hunt, current activity, for example Resolve Damage, and decision owner. Without our own
   decision we show Waiting for Free Peoples/Shadow or completed the result. Last Hunt and
   historical report have a public signature.
2. Short run: symbols and brief descriptions of known throws, tiles, exchanges and reactions. The
   last three completed steps are in the bar; the previous ones are available with backward/Come on.
   buttons and in the full report. Inspection does not scroll the game state or change local
   selection.
3. Main object: assigned dice, cast, tile or required portraits. The current step dominates. The bar
   does not promise a rigid sequence of subsequent decisions; reactions and additional tiles result
   from the current projection.
4. Own answers and the known effect of choosing: no scrolling and no need to open the tooltip to
   understand the basic consequence.

Window opens click or Enter/Space ; hover/fokus shows only passive description according to the window section. The cursor pass outside the panel does not close it. The header, session position and ×/Esc are kept. The new decision is opened once without changing the camera. Close continues to be legal draft and survives GET and reconnect the same decision. Manually, it allows you to view the last report. No report in the older record does not become a fictional result.

The ownership and legality of the controls are derived from the current server projection, according to UI-47-CONTEXT. Other buttons do not appear as disabled either. Public dice, tiles, effects and Guide remain available to both parties; private choice and undisclosed versions of the characters do not reach the opponent. One current decision has one set of controls and one host of a common tray.

<a id="przydział-rzut-i-kafel"></a>
### Allocation, throw and tile

Allocation is a separate step in the beginning turn , not the beginning of everyone Hunt Click dice in the pool or drop on the Eye forms a reversible set (from UI-88 without clicking the Eye indirectly; reclick dice in the pool or in the allotment withdraws it. With the limit click another dice It doesn't change anything. minimum/maximum server. One Allocate & roll approves, also legal zero; from UI-172 is it click the tablet Hunt in the console. dice special retain the owner and origin; unauthorised cannot be assigned.

The throw shows natural eyes, hits/pud s, pre-motion bonus FP and named sources of reroll. dice Shadow The T16 limit is five. dice natural 1 miss, and the current cost of Resolving does not increase the bonus. The appropriate spread bonus from expansions does not create a new source of reroll. dice → Reroll selected or Keep roll resolves one set; we do not reveal results during selection.

Once you select the dice tile, you will have no space; their known score will remain in the course. The tile separates Damage/Corruption/Heal/Separate, Reveal and Stop according to data. Unfixed damage is ?. The exchange of the tile before the Shelob throw does not reveal the future value. No hits, zero dice and cancelled selection also have a score, without an empty step requiring Next.

<a id="reakcje-i-odpowiedzi"></a>
### Reactions and Responses

The legal card/ability appears in the right window with short description, source, cost and Skip when it is allowed to refuse. Hover/fokus gives a passive description of legally known content or ability. Full card with controls opens a click or Enter/Space; leaving does not close it. We do not skip the window on the basis of a secret hand.

- Ring: basic description of the known change, e.g. Corruption 4/12 → 7/12.
- Guide: portrait, loss of character and injury reduction, e.g. Damage 3 → 1. Clear New Guide before
  Corruption; no further ability is considered as a result. The Hobbit's return and separation do
  not pretend to be the victim.
- Random company: covered pile, description of random victim and unknown remaining injuries. We do
  not show ID, portrait or values before RNG.

The first click selects and updates the known preview; the final click on the tile, two click of answers or drop performs the same command. Without additional Confirm. The new step invalidates the end of the previous gesture. With an uncertain network response, the existing Check result remains available.

<a id="następstwa-i-mordor"></a>
### Influences and Mordor

The new Guide is selected as a portrait in Hunt. The route of the discovery and the location of the Companion belong to the Fellowship/map; Hunt shows the context. The click of the legal empty region continues to work despite the lack of automatic tooltip. The cost remains resolving until the end of the proper action, through the reactions, new Guide windows and additional tiles. We do not add Next.

Mordor uses the same layout with No Hunt roll and Eye value from source. There is no throw or reroll, also with an empty dice pool. The tile and its aftermath precede the known shift. Reveal and Stop are independent; on the Reveal track it does not create a regional route. Corruption 12 ends the game before the 5 step promise. The reminder of the movement/Hide test remains in Fellowship; null from the older view means no information.

<a id="wynik-log-i-powiadomienia"></a>
### Result, log and notifications

The user selected “Notice and log” for the automatic result: we do not open the window and we do not add text to the eye. The window can show a new result without changing the position and camera. The eye still gives manual inspection. The new decision retains the current auto-open.

Integration uses UI-38–UI-43 and the common card action. The red tab refers to the visible action of the opponent, the gold of the current decision. The system follows the action source; the automatic result of the own action without the new decision goes to the log without the red tab. We do not add a third type of alert or notifications outside the application.

The alert click shows the cost, the cause of Hunt, rolls/tiles, the reactions and known consequences at Hunt, without the need to open the log. The old entry uses the facts of the specified chain, not the latest HuntReport. Current controls and history are clearly separated. Reading the alert version and increments remain the joint responsibility of UI-43; auto-open your own decision does not delete the red message.

<a id="macierz-odbioru-h01h14"></a>
### Matrix acceptance H01–H14

The following cases are required by a new plan, without declaration of performance. Each owner adds test methods and results; UI-54 takes the whole.

| ID | Scenario and expected result | Owner |
| --- | --- | --- |
| H01 | Four parts, opening click/Enter/Space , Close/Esc and auto-open once on its own decision; hover/fokus Just a passive description, GET does not change the camera or draft. | UI-49, UI-80 |
| H02 | Both views: own legal controls, public facts of the opponent, without private choices. | UI-49 |
| H03 | Allocation of click/drop/withdrawal, legal zero, mandatory minimum, limit and dice special. | UI-50 |
| H04 | Limit of five, natural 1, bonus without Resolving, sources/promises of reroll, subset or resignation. | UI-50 |
| H05 | Automatic no hits/dice gives a real report; no tile, no fictional decision, and no auto-open result. | UI-50, UI-53 |
| H06 | The tile dominates; unknown injuries ?, exchange before Shelob, independent Reveal/Stop and special effects. | UI-51 |
| H07 | Ring/Guide/Random with a description of the effect without a hover; preview, three gestures commit and Random without ID/sp Common RNG. | UI-51 |
| H08 | kart/Capability and Skip reactions in the right windows, privacy Lords/Council/Fate and costs. | UI-51 |
| H09 | Guide/Very common: → Reveal → additional fortresses and return; one cost, click empty route, without second victim. | UI-52 |
| H10 | Mordor without throw, zero dice, Eye, Stop/Reveal, try/Hide/null, remove Companion and both finals. | UI-52 |
| H11 | Automatic result in log/istniejyce alerts, no text at the Eye; manual report and old record without report. | UI-53 |
| H12 | Red alert and old entry open the right string; gold current decision, read version and later growth. | UI-53 |
| H13 | Restart hosta/client and lost response in nested Hunt; receive without replay cost/RNG, saved Close. | UI-54 |
| H14 | Four chips, both players, without scroll decision, feedback ≤100 ms, 60 FPS target and separate build macOS. | UI-54 |

<a id="wdrożenie-kości--ui-50"></a>
## implementation dice — UI-50

The following implementation descriptions and results of UI-49–UI-72 are derived from 2026-10-08 UTC, before UI-80. They preserve historical preview behaviour and test results; they do not replace the current opening standards described above or acceptance UI-80.

UI-50: the throw has a separate row of natural meshes, the number of dice/trapped, the bonus of previous FP movements, the bonus of effect and named reroll sources. The Uglúka premium does not form a reroll; the first and final natural score is visible on the moved dice. Crebain is a separate component of the sum, and the old unbreaked reports do not attribute the entire bonus to the move. The known throw with dice shows `0 dice · 0 hits` after manual opening; the window before the throw fails to zero. The assigned die retains an inspection of ID, pages, origin and state. The selection continues to approve one set.

<a id="granica-z-zasobami-i-suwerenami--ui-58ui-62"></a>
## Limit with resources and sovereigns — UI-58/UI-62

[IN_GAME_UX](IN_GAME_UX.md) extends resource descriptions and links to their respective windows. UI-58 make available a joint cost presentation; UI-62 au UI-53 receives integration with new Hunt. Nenya remains in Hunt's reaction, Vilya at the end of the right action , and the cost is visible at the resource without a copy of the form. Trying to corrupt Kings is a view of the sovereign and does not receive a normal reaction Hunt Rules for automatic results and red/ed the black-headed bookmarks remain in line with H11/H12 And G16.

<a id="wdrożenie-układu-h01h02--ui-49"></a>
## implementation H01/H02 — UI-49

The window has a header with decision owner, the last three known steps, the main object and its own answers. Hunt allocation / Current Hunt / Last Hunt titles separate context; no report remains public. The assignment does not show the former tile, and the choice Guide takes the main place. The arrow and focus labels do not change the choice or camera. The closing and automatic results retain the previously agreed rules. The own controls continue to result from the server; another window shows Waiting for. Details of the tests and the limit of the follow-up UI-50 – UI-54 : UI-49 .

<a id="ui-51--kafel-reakcje-i-skutek-przed-wyborem"></a>
## UI-51 — tile, reactions and effect before selection

H06 separates Damage, Corruption, Heal and Sepate; also Separate ? retains unset damage. Reveal and Stop remain independent. The tile neither draws nor tells the outcome of the pending exchange Shelob. H07 has three fixed objects: Ring with Corruption before/after, Guide with loss or return Hobbit and reduction by a known level, Random company with a covered stack and unknown victim/remain. The information is visible before the hiver and first click. The joint note reminds New Guide before Corruption; the choice of a new Guide and its capability is separate.

H08 shows the source, short effect and cost of legal response in the existing server window, along with the existing Skip and inspection of karty/postaci. The ability without cost card describes Use ability, does not promise no loss or separation. The character version data comes exclusively from its own/outright view. Response click + tiles, double click or drop do one step; the change of decision still cuts off the late tail of the previous gesture.

The UI-51 response inspection bypasses its own Hunt panel and keeps its hover without pinning; the deferred PointerLeave does not delete the newer tab hover. The actual CardPackNetwork test includes another Mithril/Axe, inspection and click, and the BattleLayout/FloatingWindow retain the pinned positions from the windows.

<a id="ui-52--następstwa-i-mordor"></a>
## UI-52 — Consequences and Mordor

H09 passes through Guide, its separate ability, return, Reveal and subsequent fortresses with one cost of Resolving. The tray follows the decision between Hunt and Fellowship/map; the empty route region accepts clicks without automatic hover. Close and Check result retain access and order. The default position of the windows responds to their size; manually moved window retains position. H10 uses the same tile without a throw in Mordor. The eye reads the value of its source, Reveal and Stop are independent, and the step follows only after the settlement. The forced choice of the Companion shows Remove from play, without a regional goal. A passive preview of Fellowship during the Hunt is compact, without the second instruction of the Guide. The rule, both perspectives, prescriptions and layouts: UI-52.

<a id="ui-53--odczyt-wyniku-w-karcie-akcji"></a>
## UI-53 — read the result in the action tab

The red alert shows the full known consequences of the action, including the automatic Hunt saved in the same revision. The new decision still remains unread. card contains a compact record of the source, eye and hit, tile or overt missing and result. In the absence of dice/box/cancellation not open Hunt or create a third alert. The manual Eye continues to show the last report; the old Log entry shows its own ChainId. History is read only, the gold tab leads independently to the current decision.

<a id="ui-54--odbiór-polowania-i-integracji"></a>
## UI-54 — acceptance Hunt and integration

acceptance UI-54 binds H01–H14 to specific methods indicated in `docs/tasks/UI-54.md`. The client H13 has additional proof of lost Mithril replacement receiver (while maintaining the old real host reboot tests). The new benchmark uses the open division of the `MovementBonus=2`, `EffectBonus=0` bonus; the older contract still displays no known division. The known public tile in the red alert is called `Hunt tile · Die` from the library, without technical ID. This does not mean revealing future selection.

<a id="powrót-do-właściwego-gospodarza--checkpoint-ui-62"></a>
## Back to the right host — checkpoint UI-62

The gold tab opens the current parent tray before general steering by decision type. Nenya is still part of Hunt; attempt to corrupt and its private tile to the sovereign. Historical reading does not transfer the response form and does not substitute the current Hunt chain. Additional tests UI-62 confirmed the parent trial and the public reverse tile after loss of response and reboot. The secrets of both tiles do not get into the public entry before the legal choice. This acceptance is saved separately from previous results of UI-53/UI-54; current regression status: UI-62.

<a id="zagnieżdżone-reakcje-i-menu--ui-71"></a>
### Nestled Reactions and Menu — UI-71

New reactions Merry/Pippin During Menu and Settings, they update the same batch and host Hunt without automatic camera or closing the cap. The closure returns to the open source of focus. Gollum's refusal retains the further joint decision to return the Hobbits in Fellowship and primary clearing die The movement is only at the end. Details of the tests: UI-71.

acceptance UI-71 : 33/33 the required Play methods, 13/13 Full Editor cases 542/542 , 7/7 PNG and 399/399 unchanged domain cases in full 3726/3726 PASS . Separate fresh HTTPS/WSS dragons are credited with trusted CA ; Unity tests benefit from HTTP/WS/PostgreSQL. Accurate filters, reusing identical methods and limits: UI-71 . .

<a id="ui-63--przekazanie-pomiędzy-hunt-i-pozostałymi-panelami"></a>
## UI-63 — transfer between Hunt and other panels

The aggregate acceptance reiterates Nenya in Hunt, Vily's physical behavior of dice and both orders Lore/Dismay. Resource Inspection and Help does not take over Hunt's response. The Corruption sovereign attempt still has a separate host: its private tiles do not become Hunt tiles or public facts before disclosure.

G16/G17 uses current log, alert, older chain attempts during the new Hunt and receive UI-62. The results are assigned to specific methods in the UI-63 matrix; they do not declare the entire acceptance H01–H14. 93 configurations have separate domain proofs, HTTP and reboot described in [IN_GAME_UX](IN_GAME_UX.md), except for the export of projections. The old Hunt/log profile remains part of the required 56 Player measurements; the new 32 measurements of the remaining panels do not replace it. The final measurements, viewings and limitations save UI-63.

<a id="końcowy-odbiór-ui-63"></a>
### Final acceptance UI-63

The UI-63 attempts retain Hunt as host of their own reactions to the Nenya/Vilya, Help and History inspections; the Corruption tiles remain a separate private decision. G16/G17 have specific log/alerts and the UI-62 receipt, without the declaration of the whole acceptance H01–H14. Final tests of common windows have passed after the stabilisation adjustment. 56 measurements of the legacy, including Hunt, remains distinct from the new 32 panels.

<a id="menu-nad-przerzutem--sprawdzenie-ui-72"></a>
## Metastatic Menu — UI-72 check

The new network sample uses the actual HuntReroll belonging to Shadow. The selected die remains in the same SavedTurnDraft after opening Menu, Settings and Help, changing Reduced Motion and Esc. In both views, the projection does not change, command does not arise or the other host. The counted chips are not re-entered the acceptance H01–H14. The `board` profile measures the local menu presentation separately over the same type of decision; the results and limits: UI-72.

<a id="pula-polowania-i-grafika-kafla--ui-141"></a>
## Pool Hunt and Tile Graphics — UI-141

User applications with 2026-10-14: The Hunt tab gives a compact list of tiles present in the pool with symbols and explanations in tooltips, and the Hunt result shows the graphics of the selected tile.

- The box icon (stash of tiles) in the panel header Hunt opens and closes the list; the selection is
  in session. At the panel docked at the right edge, the list is a move on the left side of the
  panel, above the map: the opening does not move the current decision or push it under hand. The
  floating window has a list in the panel content.
- The list is derived exclusively from `PlayerView.HuntPool` (Contracts 0.3.1): one entry on the
  printed lico (the box plate, number or symbol of the Eye, dice or Sméagola, the characters Reveal,
  Stop and Kings) with the counter ‘×n’ and tooltipem meaning. The header gives the number of tiles
  in the pool; the Corruption tiles that are hidden outside the pool are in the list with the entry
  ‘n face down elsewhere’, because the server does not reveal their identity. Special tiles in the
  game waiting for Mordor are after the sign ‘Mordor’. The draw order is never shown; the list is
  rebuilt only when the composition changes.
- The resultant tile in the panel shows the graphics of the lica with `HuntView.Face` next to the
  injuries, Reveal and Stop; tooltip begins with the meaning of the tile. The older report without
  lica shows the old name.
- Short View (1366×768 at 125%) reduces list to 22 points.

Graphics: `graphicalassets/ui/src/hunt` (standard tiles, special FP and Shadow, Sméagol; Eye, die, Sméagol; Reveal characters, Stop, Kings) and `actions/hunt-pool` icon. Code: `Runtime/Presentation/HuntTileText.cs`, `Runtime/UI/Hunt/HuntTileFace.cs`, `HuntPoolList.cs`, `Resources/Wotr/Styles/HuntPool.uss` style.

<a id="okno-polowania-i-konsola--plan-ui-158"></a>
## Hunt and console window — UI-158 plan

The user decision with 2026-10-14 changes the position of Hunt: a compact panel in the top right corner tab (UI-98, UI-109) replaces the Hunt window as a model of the battle window (auto-opening both players at the new Hunt, Roll stage bar › Reroll › Tile › Damage › Result, in Mordor Tile › Damage › Step) and the Hunt field in the turn console (sanding dice, pool UI-141, final report). H01–H14 rules remain; the host is changed. Details: [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md), sections B and C.

<a id="okno-polowania-i-przydział-w-konsoli--ui-162"></a>
## Hunt window and allocation in console — UI-162

from UI-162   Hunt has its own window in window style battle, opened on its own at both players at the new Hunt, with a step bar. Allocation dice   Shadow is performed in the console turn:  dice are in the panel dice, and the tablet Hunt approve the allocation (from UI-172 He's done it before. ✓ console). Panel Hunt In the corner of the map, it's gone. [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md), „ implementation   UI-162 ” and “ implementation   UI-172 ”.

<a id="czytelna-pula-polowania--ui-184-2026-10-15"></a>
## Readable pool Hunt — UI-184 (2026-10-15)

User notification from 2026-10-15 (windowshot HUNT in the open pool rest: “Improve the readability of this panel”. The order of the tiles entered the header “In the pool · N”, the tiles had 28 pt, and meters ‘×N’ 12 pt.

- The reason for the application: the elements UI Toolkit by default shrink (`flex-shrink: 1`). In a
  window with limited header height, it lost height and the tiles entered its text. The list and its
  parts now have `flex-shrink: 0`.
- The header stands in a separate line above the tiles (13 pt).
- Entries form a network of equal cells 64 entitled: tile 34 pt (value 17 pt, symbol 22 pt) and
  counter ‘×N’ 14 pt bold in a fixed width field. Columns of subsequent rows overlap.
- The Reveal, Stop and Kings tags have 11 pt in corners of tiles and do not obscure values.
- Special tiles waiting for Mordor stand on a separate line behind the label “Mordor” in the same
  grid.
- List Place:
  - in rest (the window shows only Hunt Box, as on the drop) the list stands in the window under
    Hunt Box;
  - during Hunt develops from the pool icon in the header over the window content: 300 pt (4
    column), panel frame, drawn on top. Window does not grow and change space, so the decision stays
    in place above the console. It closes the same icon.

The former rule for the ejected docked panel list (before UI-162) was removed.
- By the way (short screen, injury step): in response to the height of 56, the name "Ring" was cut
  by ~4 (a pre-UI-184 error detected by `HuntLayoutTests`). The answer icon is 20, a 14 symbol, and
  the effect of 11 is not shrinking.
- The short screen (`short-viewport`) has the same grid with the 56 cell, the 28 tile, and the 13
  counter.


<a id="wyraźny-sygnał-przydziału--ui-204-2026-10-16"></a>
## Clear allocation signal — UI-204 (2026-10-16)

The user's notification from 2026-10-16: a clearer signal is needed, following the pattern of the Fellowship movement, that you need to add dice to Hunt and that then click the sign to approve the decision.

- dice, which Hunt can still accept (`hunt-candidate`), pulsates together with 0,7 with a brighter
  green outline and light filling. The dice panel switches the `hunt-candidates-bright` class.
- When the assignment is ready (`hunt-confirm-ready`), behind the Hunt plaque in the console lights
  the gold light `ornaments/move-glow`. When the movement is activated it pulsates
  (`hunt-glow-bright`, the scale 1,14), as with the movement Fellowship (UI-110). Tooltip: "Click
  the plate to allocate and roll".
- There is no pulsation at the Reduced motion: the outline and the glow are constant. The signal
  does not select anything; click the untied sign still does not send anything.

<a id="najpierw-kości-na-tabliczce-liczba--ui-205-2026-10-16"></a>
## First dice, on the number plate — UI-205 (2026-10-16)

The user's application with 2026-10-16: the sign signal was too strong. A player seeing the light sign “Allocate” clicked it to see what it was, and did not assign dice. Attention should go first to dice. Changes UI-204:

- Until there's none in the assignment dice panel dice He's classy. `hunt-choose-dice` The
  candidates have a thicker, brighter outline and filling; when they move they grow slightly in the
  rhythm of the pulse. dice is mild pulse with UI-204 while you can add another.
- The plaque no longer has a golden glow behind it. At zero dice (`hunt-allocation-empty`) is
  normal: without a golden frame and background, but active, because zero is allowed, and click
  approves the allocation so far.
- It's an inscription. „ALLOCATE” and the number tag `hunt-allocation-count`
  („n/m”):
  - The badge shall be extinguished at zero;
  - from the first dice has a golden background (`hunt-count-filled`);
  - the number is briefly increased (when moving) on each change.
- The ready-made tablet with at least one dice has a gold frame. Unready when the minimum is
  missing, it is extinguished.

<a id="kafel-otwiera-okno-z-pulą--ui-209"></a>
## Tile opens pool window — UI-209

User application from 2026-10-16: Hunt window opened by clicking the tile at the bottom is to be immediately developed and display the remaining tiles in the pool.

- Besides the allocation, click the tile (`HuntPanel.Tab`) to open the window and expand the pool
  list (`HuntPoolList`). If the window is already open with the rolled list, click to expand it.
- The icon in the window header (UI-141) continues to scroll and expand the list.
- The window opened with Hunt itself retains the status of the list from the icon.
- In the assignment, click the tile only approves the ready-made allocation (UI-172, UI-205) and
  does not develop the list.
- Test: `HuntNetworkTests` (caf, icon, assignment).

## Static pictures and the response row — UI-235 (2026-10-17)

User report: in a finished game (and so in a replay or for an observer) the Hunt window showed three empty response
buttons with the Guide line, and drawn tiles or rolled dice looked like buttons although a click did nothing.

- The response row (Ring, Guide, Random companion) is hidden from construction; only `HuntPanel.Context`, called by
  the live player's turn, shows it for a damage decision, with each legal choice named and its consequence.
- `ReadOnlyControl` marks a control that cannot act now (`read-only`): no button frame, hover or press
  (`PressFeedback` ignores it); its tooltip stays. The drawn tile is read-only until a damage decision makes it the
  place to confirm; a rolled die until a reroll lets the player choose it.

## The Breaking of the Fellowship on the Guide row's portraits — UI-249 (2026-10-17)

While The Breaking of the Fellowship's tile asks for Companions, the Hunt panel shows its roster row (the one the
forced Guide uses) with the candidates glowing; clicking a portrait selects or releases it, and the panel's
confirmation sends the set. The decision tray no longer lists the Companions by name. See FELLOWSHIP_UX, UI-249.

<a id="ui-285"></a>
## Table cards answer in the response row — UI-285 (2026-10-19)

The owner's decision after the object controls audit (OBJECT_CONTROLS_AUDIT, "Axe and Bow"): a table card that answers
in the Hunt — Axe and Bow, Mithril Coat and Sting, Horn of Gondor, Wizard's Staff, Flocks of Crebain — no longer falls
to the generic choice list as a text rectangle whose button also pinned the full card with a disabled Play button.

- `HuntCardResponses.Offered` lists the Reaction's legal `RespondEffect` choices whose source is a card on a player's
  table (or a lasting effect's card); `HuntResponsesView` shows each as a card object in its own row under the Ring,
  Guide and random-companion objects: a small card face, the card's name and the consequence. Legal objects glow with
  the shared ChoiceGlow and pulse.
- One click uses the card (UI-142): `HuntContextInteraction.CardResponse` sets the draft's reaction and sends it;
  the console's Pass declines (UI-291). The tooltip names the cost ("Click to discard Axe and Bow · Reduce damage …").
- `CardChoices` leaves these choices out of the tray and no Hunt reaction pins the card inspector; the card can still be
  read in the Effects panel. A reaction whose source is not a table card (Nenya, a character's ability) stays in the tray.
- Tests: `HuntLayoutTests` (RespondEffect), `CardPackNetworkTests.MithrilAndAxeRestoreBeforeShelobRollAndReachBothClients`,
  `CardPack02NetworkTests.StaffPreventsAnUnseenTileAndAdvancesMordor`, `HuntRecoveryNetworkTests`,
  `MordorNetworkTests.MithrilReplacesStopBeforeAdvancingAndSpendsItsCardOnce`, `Shadow01NetworkTests`,
  `Shadow03NetworkTests`; `MatchTestRoom.ObjectChoice` finds the card object in the Hunt window.

<a id="ui-288"></a>
## Pictures on the responses — UI-288 (2026-10-19)

The owner's decision after the object controls audit (finding 7): the response objects show pictures instead of the
glyphs ○ ★ ?. The Ring shows the Ring with its drop of Corruption (`hunt/choice-corruption`), the Guide the current
Guide's own figure in a gilt medallion (the version the Fellowship panel shows; it follows a change of Guide), the
random casualty the Companions' stack (`hunt/choice-random`), and a table card's response (UI-285) a small parchment
card with the symbol printed on the card's corner (`CardLibrary.Face`). The pictures are 28 points, 22 in a short
window. Tests: `HuntAndEffectPicturesTests`, `HuntLayoutTests` (all four layouts).

<a id="ui-310"></a>
## The Hunt in the opponent's presentation — UI-310 (2026-10-24)

The shared Hunt plays in its existing window, entry by entry from the recorded snapshots, before the decision it
leads to opens ([OPPONENT_ACTIONS_UX §6.14](OPPONENT_ACTIONS_UX.md#wdrozone-310)).

- **Opening.** The Hunt's start opens the window, as a new Hunt does today, with a trace on the Hunt Box plaque. A
  window the player closed during this Hunt stays closed.
- **Entries.**
  - A roll shows its dice and successes for 1 s, with one dice sound.
  - The drawn tile turns face up for 1 s, then the damage shows.
  - A response is marked on its object: the Fellowship's Ring, a portrait, or a table card at the Effects tab.
  - A casualty leaves the Fellowship's roster through its portrait. A new Guide takes the badge.
- **No choices.** The window hosts no choice during the presentation. A real response, Guide or damage decision opens
  on its usual host at the release. No Continue or result window is added.
- **Afterwards.** The Fellowship's tracks, a reveal's relocation and the progress move after the Hunt's entries.

<a id="ui-318"></a>
## The Shadow's allocation lights the Eye count — UI-318 (2026-10-24)

When the opponent's presentation shows the Shadow allocating dice to the Hunt, only the Hunt plaque's Eye medallion and
its number glow while the count rises (`HuntBoxView.EyeCount`), not the whole plaque (owner request). The Hunt's own
start, when its window opens, still marks the Hunt Box
([OPPONENT_ACTIONS_UX §6.19](OPPONENT_ACTIONS_UX.md#wdrozone-315)).

<a id="srodek--ui-324"></a>
## The window opens in the middle — UI-324 (2026-10-25)

Owner's decision: this window opens centred in the board's middle, between the top bar and the console and between the side columns, in every phase; a position the player dragged it to wins ([IN_GAME_UX](IN_GAME_UX.md#okna-na-srodku--ui-324)).

<a id="kosci--ui-325"></a>
## Hunt dice are dice — UI-325 (2026-10-25)

Owner, on a build's roll: "fix the graphics so the dice are visible and not only their pips; the dice that hit are to be
marked nicely in green".

- A Hunt die is the ivory d6 of the battle window (`Art/UI/battle/die-N`, UI-117) at 30 pt, on no box of its own
  (`HuntDieFace`, `.hunt-die-face`); a die chosen for the reroll takes the kit's gilt variant, a pressed one its pressed
  variant, and hover brightens the art.
- A hit glows green around the die's own shape (a light core, a green glow, a wide green halo) with a bold green check; a
  miss keeps the plain die and a muted cross. Green stays the Hunt's hit colour: the observed glow is blue, a choice gold.
- At the reroll a rerollable miss keeps the legal ring around its die (`.legal-target`), so a choosable die still shows it
  can be clicked; the recorded Hunt in the Log (`HuntArchiveSummary`) uses the same dice and green hits.

<a id="fazy--ui-327"></a>
## The Hunt's phases change smoothly — UI-327 (2026-10-25)

Owner: "the Hunt window's phases can appear in different places on the screen … we don't jump between the phases, we
need a smooth animation: first the active element lights up and then a smooth transition; on this track the marker is to
move smoothly; the change between the Ring's hidden and revealed art is to be smooth too, and at the Ring's track as
well; tokens that change their state should turn over to their other side smoothly".

- The window opens in the middle for each Hunt (UI-324) and then stays where it stands through its phases: a new size
  re-centres it across and keeps its top (unless it would leave the middle), gliding there over 0.25 s
  (`FloatingWindow.KeepInMiddle`); a drag stops the glide; the next Hunt opens in the middle again.
- The stage ribbon is one lasting row (`StageRibbon`): at a new phase its gilt marker glides from the old stage to the
  new one (eased, 0.35 s) while the labels and icons crossfade into the current stage; then the phase's content fades in
  (0.12 s after the marker sets off). The battle window's ribbon (the decision tray's and the presented round's) does the
  same.
- UI-339 (owner 2026-10-25: "the next Hunt sometimes starts from a phase"): the row glides only within one Hunt (one
  battle). A new Hunt — its window key — or a new battle — its id — starts the row afresh, so the marker stands at the
  new Roll (Cards) at once; before, it glided back from the last Hunt's Result and the next Hunt looked as if it began at
  a later phase.
- The Ring's Hidden/Revealed art — on the plaque, in the Fellowship panel and on the journey track's marker — turns over
  when it changes (`TokenFlip`: it narrows to its edge in 0.15 s, shows its other side at the middle of the turn and widens
  in 0.15 s); a nation's token turns over to its active side the same way.
- Under Skip animations the marker, the window and the tokens change at once. Nothing waits for an animation. A drawn
  Hunt tile has no face-down state in the client, so it does not turn.

<a id="kafel-w-grze--ui-332"></a>
## A tile put into play drops into the Hunt plaque — UI-332 (2026-10-25)

Owner: "in this and analogous cases let the tile be presented — its artwork, without broader explanations — and let it
drop into the Hunt tab with an animation; in the Hunt tab, where all tiles in the game are shown, the tiles that start to
play once the Fellowship is on the track must be shown separately". The client cannot name the card behind a prepared tile
(the projection carries its face only), so the tile alone is shown.

- When the Hunt pool gains tiles in a new view — a special tile put into play (Give it to Uss! and the like), the prepared
  tiles joining the pool at the Mordor entry, a tile returned to the pool — each gained tile's face (at most three, side by
  side) shows large in the middle of the board (0.25 s in, 0.55 s held), then drops, eased and shrinking, into the
  console's Hunt plaque (0.6 s), which lights as it lands (`HuntTileDrop`). No text, no pointer, no wait; nothing under Skip
  animations or on a quiet view (first view, reconnect, change of side, a replay's manual move); Skip clears it.
- The Hunt window lists the pool ("In the pool · N") and, apart from it under a gilt rule, the tiles waiting for the
  Fellowship to enter Mordor under their own heading ("Join on the Mordor track · N"); before, UI-184, they stood on a short
  "Mordor" line inside the pool's block.

