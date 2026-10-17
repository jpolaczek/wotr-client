<a id="ui-oparte-na-kościach-płynność-i-czytelność"></a>
# UI based on dice: fluidity and readability

**The mandatory UI-73–UI-80 model:** [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md) separates non-interactive information on hover/focus from the panel opened by clicking or Enter/Space. The map area on the hover only stands out; the region opens by clicking. The legal end click/drop continues to perform a step. The map covers the screen and has the edge pan; figure moves readably and Fellowship/Hunt have a common layout. The U01–U17 below are current requirements; the clearly marked descriptions of historical implementations and results are not acceptance of this version. The status of implementation defines the register.

Specification accepted 2026-10-06 for UI-14–UI-24, developed by the subsequent tasks UI. The document itself does not mean finished runtime; status and evidence are in Register. Full decision coverage: [catalog](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/ui/interaction-inventory.json), attempts: [SCENARIOS](SCENARIOS.md).

<a id="u01--cel-i-układ"></a>
## U01 — Objective and arrangement

Typography approved 2026-10-14: **Cinzel SemiBold** in headers and **Alegreya Regular/SemiBold** in content and controls, on SIL OFL 1.1. Roles, licenses and acceptance in [TYPOGRAPHY](TYPOGRAPHY.md). We maintain the current text sizes and fixed panel dimensions; we verify in four layouts.

Priorities: correctness of principles and confidentiality, readability, precision and fluidity of interaction. Simple interchangeable symbols, portraits and portraits are sufficient. Production of final graphics is a separate stage.

From UI-200 (user notification with 2026-10-16) window surfaces, panels, bookmarks, tooltips and menus are warm charcoal gray with a delicate texture where the background is graphics instead of navy blue. Blue remains only as color Free Peoples.

The map dominates. From 2026-10-14 (User decision) three rules apply: fixed sizes (lower bar does not change height), as little empty space as possible and fixed place of each control – not available is enhanced and one mechanic control belongs to the panel of its object, not to the common bar ( [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md) Own dice are down, the opponent on top. Phase, decision owner, connection and remaining budget are constantly readable. Available/Resolving/Spent/Hunt/Discarded we distinguish the symbol and position. The hand is hidden in the right tab Cards with the number. Click or Enter/Space Development card and decks; hover/focus can show only a short information text. Closed hand leaves the map space. Console turn shows progress action in status, and the decision details are at the objects, and the choices without the object in the decision window over the console. From UI-158 The bottom bar is a console turn : status (who works and what now), field Hunt , Rings of Elves and action components ( [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md) ). battle and Hunt do not use a full screen modal or fixed large bottom panel; both have their own windows.

Clicking graphics suggests clickability; pressing the mouse button shows the status of `pressed` and action enters only after release inside the item (UI-113, [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md)).

We do not require you to enter numbers or scroll through the basic panels. Important numbers (storage, budget, injury, warp) remain accurate. History and full rules are optional previews, available also while waiting and reconnect.

Controls intended exclusively for the other side are hidden, also in the panels of the common public state. Own temporarily unavailable action may have an explanation. Hostile forces, dice, politics, Fellowship and reports remain available for viewing; the legitimate enemy own goal action, e.g. Dismay, remains interactive according to server options.

<a id="u02--kość-gest-komenda"></a>
## U02 — die, gesture, command

We choose a specific die. Army/Muster and Will of the West immediately highlight all legal applications (UI-28), without separate choice of categories. A gesture on army, reserve, track, character, Fellowship, card or deck determines the use of the same dice. The usual Army movement by Will uses Army; the character's journey uses Character. Special abilities and valuable resources still require your own open choice. Tooltip dice provides applications without redundant Action die/Available. The paid sequence retains the type and budget saved; the unsent intent can be replaced by another object.

Clicks are the full path of each operation; drag is the shortcut. The end-click of the target or legal drop takes a complete step without additional Confirm. An incomplete gesture collects the next required selection. A wrong target does not release the resource. A preview of known effects appears before the gesture, without promising the result of RNG. Clicking sends through the previously secure sender and retriever; UI does not approve the result itself.

Losses, reroll and allocation Hunt allow you to improve the set, then approve once by ✓ (from UI-222 discards from the hand are clicks on red-marked cards in the hand, sent once complete) An empty set of reroll requires open ✓ To signify resignation. Simple Continue/Stop/Pass/Decline perform the indicated answer without further question.

Esc only withdraws local unapproved selection. Approved step does not undo the choice of another army or panel closure. The gesture is associated with ID decision and review; the second click does not go to the next window. The upload blocks the replay, but does not disable. The strategic choice of dice, roads, victims or valuable resource belongs to the player.

<a id="u03--informacje-na-hover-interaktywne-podglądy-na-klik"></a>
## U03 — information on hover, interactive previews on click

Tooltip is a single text without buttons. Besides the ongoing gesture, after 200 ms above the object or immediately after the keyboard focus shows the existing metadata of the correct projection. It can explain the symbol, full name, ability or inaccessibility of its own action; it does not enable the inaccessible control. It does not download cards or history and does not show the values of the fields entered. The owner of the extended preview can share its own text, but hover/focus does not call its panel. Tooltip has `PickingMode.Ignore`, so it does not obscure the target of clicking, double click or drop.

From UI-191 (user's decision with 2026-10-16) focus does not draw any white outline or frame around the object, also after keyboard and text fields. Focus control can only have its own state of `:focus` as with the hiver ([BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md)).

The territory of each region on the hover only slightly illuminates, regardless of the presence figures The region click opens a stable panel with name, control, settlement , with open effects, armies by side, garrison, nazgûla and characters. Fellowship The former region is not occupied by Mordor. Click or Enter/Space au figure , tab or tab opens the corresponding interactive preview; focus itself gives only text. Legal selection figure or final objective action have priority, without any further confirmation.

The open panel remains after the cursor has left (except UI-189: the region window disappears after driving the cursor out of the region and window, [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md)). Another consciously indicated object can replace the unattached inspection; manually pinned card and the current decision remains stable. ×/Esc closes the panel without erasing the cost or step. The panel does not follow the cursor, it is in the screen and includes the other windows. Its controls do not pass clicks on the map. Tooltip does not change the selection, source, draft nor the camera and does not send GET/POST.

The output, focus change, object nullification or focus loss of the application removes the information. Pressing any pointer button, drag, pan, wheel and cancels it out and blocks it to the end of the gesture; the release itself does not open it again. At the same time, there is not a single tooltip. Esc removes the text and still reaches the active window or draft: you don’t have to press it again. Hover never replaces the open panel or opens its new decision — the latter exception is due solely to the server’s decision state.

<a id="u04--armie-i-atak"></a>
## U04 — armies and attack

Army → click your own army → illuminated targets → click the illuminated terrain; traffic is done immediately ( UI-105 : no drag figures and the army, no choice on the Characters list). From UI-127 click on your own army, which is a legal source, mark all of its figure can move and open the region panel; click figure in the panel it shows (dividing), another click of the same army retains division, and the two clicks again mark the whole. figures another region before the target replaces an unfinished selection. From UI-140 the power header in the region panel has a switch select/unselect all figure The following is the non-interactive spirit of the selected target. figures with the signature “Move n figures” or “Attack · mode” from the server option. The first Esc in the open region panel removes all unsent selection ( die stays), another one closes the panel. Click on the highlighted area selects the target, also with a busy friendly region.

Two symbols of the march at dice show the budget. The first move executed is permanent on the server and irreversible locally. Then the second army and the second goal; the second march ends action automatically. ✓ with dice lets you finish one. figure it cannot move again, even after the merger. We only settle the transitional surplus and the political effects in the right final window of rules.

After selecting dice and participants, a single click of the legal enemy region begins the attack. Without the prepared attack, the region serves for inspection. Attackers remain in the source for legal entry after battle. The availability of movement/attack depends on the specific composition, nations and Leadership.

Large stacks have stable groups and developed figures nets without scroll. The garrison remains visually separated. Map objectives cannot disappear under a label, tooltip or marker on another scale.

On the second march figure, which has already moved, are in the panel extinguished with a sign ✓ and non-selectable; force of which all figure They're moving, it's got this mark on the map (UI-127 When some of the selected figures cannot enter the region accessible to the rest, the tray lists them (‘Leave out...’). Traffic paid dice, also with the ability, goes this flow; movement from the master's token and card with a route plan or a rescue stay in a tray with one approval because the server accepts their entire plan in one command  (UI-128,  [ARMY_MAP_UX](ARMY_MAP_UX.md) The Army cannot be dropped; drag remains for the reserves and cards.

<a id="tor-mordoru-na-mapie--plan-ui-152-wdrożenie-ui-153ui-155"></a>
## Mordor track on map — plan UI-152, implementation UI-153–UI-155

Mordor's track is drawn on the map as a stone trail in ink from Morannon and Minas Morgul to the Doomsday Penetration in Ordruin; Fellowship stands at its step, and the entry, motion and hiding attempt work with one click. The camera moves only to the track at the player who entered. The Fellowship panel retains the line 0–5. Details: [FELLOWSHIP_UX](FELLOWSHIP_UX.md), F07a.

<a id="no-card-bez-kliknięcia--ui-151"></a>
## "No card" without clicking — UI-151

Without legal card the client sends the fight himself “No card”, once per decision; in the stage cards button is in footer, greyed while waiting. Details: [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md).

<a id="panel-posiłków-bez-kart--ui-150"></a>
## Meal Panel without cards — UI-150

The Meal Panel does not have a cards bar; card, which die can play, distinguish and play hand. Details: [ARMY_MAP_UX](ARMY_MAP_UX.md).

<a id="okno-bitwy-otwiera-się-z-nową-bitwą--ui-149"></a>
## The battle window opens with the new battle — UI-149 window

The new battle opens its window itself to both players, without camera movement; the window closed during battle is closed. Details: [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md).

<a id="karta-walki-jednym-kliknięciem--ui-147"></a>
## card One-Click Fight — UI-147

One click on the card tile from the fight in the battle window plays it hidden, without arming and Confirm; the content reads the preview after raid (UI-137). After playing any card window does not open separately with its contents. Details: [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md), “card with a single click — UI-147”.

<a id="zaznacz-wszystkie-przy-wejściu-po-bitwie--ui-146"></a>
## Select all at the entrance after battle — UI-146

The figures selection for entry after the win of the fight has in the window footer battle switch select/unselect all, like a region panel from UI-140. Details: [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md), “Tick all at the entrance — UI-146”.

<a id="pasy-granic-nacji--ui-143"></a>
## Nation boundary strips — UI-143

The national lanes between regions are narrower (2,3 instead of 3,0 map units) and two-tone in the shade of the nation: a brighter semi-transparent midrange and a darker edge of the same color, without a grey border; ink stays on the transition line. Mountain barriers are a thin line of warm ink with a soft shade instead of a black line. Details: [MAP_CONTOURS_UX](MAP_CONTOURS_UX.md), "Nation belts — UI-143".

<a id="bez-dwukliku--ui-142"></a>
## No double click — UI-142

Decision of the user from 2026-10-14: The game does not use two clicks. The click that completes the selection, performs it immediately, without Confirm: Guide, answer Hunt,  figure Extensions siege, entry of character from the Characters line (when cost allows one entry), reserve token with one legal settlement,  card to discard from an excess hand (miniature or preview content). Where the click selects only a part of the larger choice (set of losses, reroll, route, versions and Guide when preparing Fellowship,  figure march), selection ends with a public click of the target or one ✓.  card to play: the first click armed, the second one plays (UI-91 The second click is a simple click in the same decision, and after the approved step (new revision, decision or merger) is omitted, so it does not repeat the step. Earlier mentions of the two clicks in the job descriptions below and in the remaining documents are historical; this section applies. [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md), ‘The two-click inventory — UI-142 ”.

<a id="przyciski--ui-138"></a>
## Buttons — UI-138

User decision with 2026-10-14: the buttons are uniform, most are graphics with a tooltip explaining the symbol, and the buttons with the label have an illustrated frame matching the strategic fantasy game.

- Icon button (`IconButton`): icon action with `graphicalassets/ui/src/actions` on the frame
  `frames/button-square` (hover, push), with mandatory tooltip. Replaces characters ×, ‹, ›,  ✓, ⁇
  /◆, ⁇, ⁇, ?, +/−, ⁇, ⁇ /☐ and words Back, Close, Retry, Refresh, Location, Why?, Pin, Older,
  Previous, Next and the button “Reinforcements” in the tray. pass and rejection dice have their own
  icons; the development of the log entry switches the icon more/less. cards version Companion and
  the country of the Council is an icon; reroll becomes a word ("Keep roll" / "Reroll
  selected"), because the label says what it will do to press.
- Label button (`.action`): `frames/button` frame with hover, selected and pressed variants (UI-114
  dims and lowers), gold label with shadow in plain thickness (thickness widened narrow labels and
  came on the nation's tab); buttons with their own graphics (dice, card, figure, chips) retain it.
- Exceptions: “Back · Cancel game / Leave game” in the waiting room becomes a label because the word
  itself speaks of the effect. Edge tags and their corner tab do not have a tooltip, because
  invading opens their panel (UI-92, UI-98); the Fellowship tag on the map and policy track fields
  are graphics with state sign.
- `scripts/button_inventory.py --check` (client) lists each button and rejects the character instead
  of the icon, also in conditional label, in the initializer `new Button { text = … }` and in the
  assignment `text` variable button; table: [BUTTONS](BUTTONS.md). The `BoardChromeLayoutTests` test
  checks that the visible button without words has tooltip.

<a id="pula-polowania--ui-141"></a>
## Pool Hunt — UI-141

The pool icon in the panel header Hunt opens a compact list of tiles in the Hunt pool: the lico of each tile (graphic, number or symbol, Reveal characters, Stop, Kings) with the counter and tooltip meanings, the tiles waiting for Mordor and the number of tiles covered outside the pool. The draw order is not shown. When the panel is docked, the list is a trigger over the map. The Hunt result shows the graphics of the selected tile ([HUNT_UX](HUNT_UX.md)).

<a id="u05--rekrutacja-polityka-i-postacie"></a>
## U05 — Recruitment, politics and characters

Muster  → reserve chip in the Meals panel → Legal settlement (from UI-135 The only recruitment window; the pallet in the preview region and the tray pallet are gone). Hover terrain only distinguishes it. Click settlement or drag to settlement recruit; from UI-142 click a token that has one legal settlement, recruits there right away. Other budget at dice, termination earlier by ✓ Each location is saved; next settlement The lone commander, the reserves and the limits have a real preview of the effects, unimagined bans UI.

from UI-130  ([ARMY_MAP_UX](ARMY_MAP_UX.md)) the Muster selection opens the Meals Panel at the left slat: the line of each own nation, budget slots and ✓ during the sequence. From UI-134, the line is one line with a graphical policy track (active or passive), the reserves and the move icons on the track and the activation of Narya when the server allows them; The status and reason for the lock are in the tooltip. The chip click moves the view to the legal settlement; from UI-150 card, which die can play, are only in hand. The settlement highlight button on this figure, click on settlement to recruit without Confirm; At one legal settlement click the token immediately (UI-142, formerly two-click), and drag is a shortcut. The settlement click on the map means the line of its nation and restores the closed panel; click settlement, which the selected figure cannot occupy, shows the reason for the server in the tray. from UI-135 Muster paid for with the master's token or skill is a plan in the same panel (slots, figure with × one ✓), and the warning about a lone leader is in the tooltip of the token. The closed panel returns with the “Reinforcements” button in the tray. from UI-131 the same panel supports recruitment with cards  („ EventRecruitment ”, „ ReserveFigures), Kings and Balrog's rulers: the header calls the source, slots count the planned figure,  figure with × return to the reserve in the panel and from the map (spirits in the regions of the plan), and one ✓ approve the whole together with the tray. The selected die distinguishes card from playable (UI-124) and from UI-136 also the Green Light tabs Characters and Politics, the Fellowship and decks when server options allow it to be used.

Policy: click or Enter/Space On the left, the Politics page opens all tracks on a common Peace → War scale. Hover/fokus shows only the text of the bookmark. Own nations are first; checkbox Show opponent (from UI-197 The signature “Opponent”) shows the opponent by default. The chip stands in the actual position. From UI-197 (user notification from 2026-10-16 The coat of arms of the nation in the side medallion, over a soft golden glow. The coat of arms of the active nation is fuller and shines more, and the passive coat of arms is dim; the legend in the column header shows both appearances. The column of names no longer has a coat of arms, so the panel has 256 pt 290 . The state is supplemented by a outline and tooltip. The panel remains after the cursor leaves; ×/Esc closes it with the choice and cost. No scroll and separate track to open at each nation.

Muster, Army/Muster or Will → arrow on the next field of the nation, all in the same panel as Activate ( UI-26 . from UI-144 (user decision) we do not mark the nation first: after choosing the cost of each own nation that die can move according to the server option, immediately shows the arrow “→” in the next field, and its click takes a step without Confirm. A click of a token such a nation takes the same step. Drag of the token into the field becomes a shortcut: distinguishes the target of the drag chip and drop sends a step; canceling or illegal drop does not change anything. The nations of the opponent do not have arrows. There is no separate inspector, the list action or the Confirm button. A chip click without legal context leaves the panel; the tooltip explains the state and requirements, does not choose as the player dice The restored draft distinguishes the arrow of its nation. From UI-148 played card who is waiting for the choice of the nation (e.g. the Wisdom of Elrond, `NationTargets` server), opens Politics with highlighted nations, once per play; closed by the player the panel does not return on another revision. The token click selects the nation so far. The policy council uses the same tracks after the open selection of its token action , without a replacement dice A passive nation can approach the field before the war.

UI-27 removes the former lower renderer of the narodu/toru. selection No complete cost or restore the draft does not switch the policy to the bottom panel: a short tip points to a specific dice. UI-28 removes an additional selection of its categories. Track, token and target remain exclusively in the Politics. Recovery of the uncertain response of the server retains its own message and Check result.

From UI-144 Actiate stands without clicking on any passive nation that the server allows to activate (Narya); it is available when selected die can pay, otherwise excluded with explanation. Narya performs existing legal action with selected dice ; tooltip warns of the handover of the Ring, issue dice and entering the war. A nation without such an option has no button, and the source of activation explains the tooltip of its token. There is no any activation of the nation dice Muster or Will. Automatic effects Fellowship , Companion , enemy actions and effects cards Still running the server.

From UI-145 die Character shines as sources only armies with commander (FP: commander or Companion, Shadow: Nazgûl or Minion) according to `CommanderIds` and server cost; die Army continues to every army with legal movement ([ARMY_MAP_UX](ARMY_MAP_UX.md)).

Characters: the selection of figures in the region panel (or Fellowship portraits at separation), and then click the highlighted target; the move is executed immediately, and the shortest legal route within range is arranged by the game (UI-105). The drawing of the route step by step is left only for the declaration and disclosure of Fellowship. The flight indicates a legitimate goal without imaginary intermediate fields. Several groups use one active dice and an open end. Transformation/Introduction shows a new version and the right extension date of dice.

UI-21: The usual Character also distinguishes the sources of independent travel. The portrait from the region chooses the journey until the army's combat units are marked; then the character remains a participant. Another source replaces the unsent group. die shows figure already moved and others, and ✓ ends the paid sequence. The viewer does not choose the road to a remote region alone. The selected end remains a distinguished area also on flights with many legal regions.

<a id="u06--kości-pierścienie-i-żetony"></a>
## U06 — dice, Rings and chips

UI-50 shows in Hunt the number dice Natural. first/ko The results remain available for dice The assignment is kept ID/In the owner/origin in inspection; limit comes from option. We do not replace lack of data about the components of the old bonus by counting from the current pool. Known throw without dice remains manually available in the report.

Allocation Hunt: click specific dice adds them to the set or withdraws, then one ✓  (UI-88 Special Elections behaviour/discard they are held on their own dice The ring. →   die   → new wall; change does not force later use of this dice. Dismay  → The owner, source, state and consequences of the rune visible in the tooltip. The chip or ability appears at the owner in the appropriate window.

<a id="u07--karty"></a>
## U07 — cards

Click or Enter/Space on Cards expands your hand and on the card opens its compact preview. The cursor's departure does not close these windows; ×/Esc retains the unsent selection. Hover/fokus can only show available information text, without loading full card. Drag keeps the source and handheld pinned remains independent. Both full-use Event and Combat have separate blocks; legal application and miniatures get the distinction. Small Play, double-click or drag starts playing. The thumbnail click itself taps the preview; the check to discard concerns the cost of effect, not the excess hand. No hover does not send command.

Play uses the selected dice and server options; the target still points to the player. For Will, the usual alternative costs of card are direct Play buttons with the icon of use. Event is the first shortcut of the second click of the armed card and drop, keeping the optional Gandalf/Anárion reactions. Tooltip explains the cost; we do not select the replacement dice, the ability or response. Combat uses the same Play without the cost of dice and retains confidentiality to disclose.

The selection is done by two decks presented. Overcharged copy: from UI-142 one click card (miniatures or preview content) saves one discard without Confirm, to reach the limit of six (UI-31). The foreign hand and covered card remain covered, also in tooltips. Full text and source of effect are available without a fixed text wall on board.

<a id="u08--bitwa"></a>
## U08 — battle

The panel anchored to the battle region: participants, distinguished garrison, card, throws and the current decision. Opens it by clicking the region, shortcut or notification or Enter/Space on the control, without taking over the camera. Hover/fokus does not open battle or preview card Combat; can only show secure information text.

Losses are chosen figure And we approve one ✓ Regular switch none/usuni hook; elite none/obni The sequence of operations remains public because it affects the availability of replacements. The preview counts the actual hits, the removal of the elite in the absence of a replacement and the consequences for the commanders.

We choose the transfers on the eligible ones. dice, with a visible limit and one ✓.  Continue/Stop are direct. Retreat and entry are indicated on the map; extension siege The choice of the elite. card shall be disclosed only at the time established by the rules.

The agreed window development, cards , kicks, losses and four modes describes [BATTLE_ACTIVITY_UX](BATTLE_ACTIVITY_UX.md) . UI-40 provide permanent reports, and UI-44 – UI-46 interface; status implementation specify the register. Open public, pinned and card last action use common navigation and notifications. Former hower 200/150 ms panel was replaced in UI-74 ; UI-80 restores only an inactive description, without automatic panel. Illustrated window frame, cards , dice , losses, end battle and report and portraits describes [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md) ( UI-113 – UI-121 ).

<a id="u09--drużyna-polowanie-i-mordor"></a>
## U09 — Fellowship, Hunt and Mordor

UI-79: Fellowship panel is built around two parallel graphic travel tracks and warps. Accurate values with separate tags; Hidden/Revealed distinguishes kaptur/ods. Mordor replaces only the top track to0–5. The fields are informative, action retain separate legal gestures. The full name Guide and explanations remain available. Graphics have interchangeable roles described in [Contract art](../../graphicalassets/fellowship/README.md). Sources, layout and limit of the drawn range are described by BOARD_INTERACTION_UX.

Panel at the Fellowship tag; a small summary of progress, hiding and warping constantly visible. Normal hidden movement increases abstract progress, without selecting regions. Hidden uses state tag. Declaration/disclosure and separations show actual routes and portraits.

Hunt chips and throws and legal reactions appear in this context. Guide/Random/Ring are separated: Random does not allow to indicate a specific victim. Mordor route does not promise a shift before settling the tile; stop, disclosure, additional tiles and abilities retain separate decisions and checkpoints.

Click or Enter/Space at short/marker opens the panel; × Esc closes without losing his choice, and the cursor's departure retains its openness. Hover/fokus gives only information about the open state. Open does not center the map; a separate Location button knowingly moves the camera. With the selected legal dice   → performs a movement test, ◆ hides, ⁇ begins the declaration. Portrait Guide In his window, it's a complete decision. Ring/Guide/Random selects the answer first, and click the tile, two-click answers or drop on the tile performs it according to U09 below. Metastatic set still requires one ✓, also when selecting zeros dice Forced separation keeps the required portraits even after they leave the lineup Fellowship.

<a id="rozwinięcie-u09--plan-ui-34"></a>
### Development U09 — UI-34 plan

[FELLOWSHIP_UX](FELLOWSHIP_UX.md) describes the target two windows: Fellowship and Eye, short icon labels, declaration/disclosure on the map, guides, separation, responses to Hunt and Mordor track. Saved in UI-36 division by UI-34-PANEL/ROUTES/GUIDES/HUNT/RESPONSES/MORDOR defines implementation owners; UI-34 receives the connection, UI-35 complete regression and build. The report remains within the scope of UI-33. The following contracts retain the costs and order of the decision; the way of opening is up to U03. The results of specific previous implementations remain in their task documents.

UI-33: A typical report is also saved with an automatic throw without a decision and after the end of the game. It contains only known public facts; the mentioned tile does not receive an unknown result. An older record may not have a report. The new interface will use these data without parsing the history; legal options remain a separate, current server contract.

UI-34-PANEL implements the described status, Move/Hide and Location. No selected dice explains and distinguishes the available costs. The selected Will is not replaced by Character. The usual move changes progress, hide does not add dice to Hunt. The panel opens once for its new decision and retains Close at GET and reconnect. The click of Your decision only opens; the Location center openly. The other F03–F07 have separate tasks and are still not acceptance of this panel.

UI-34-ROUTES: F03 uses the highlighted terrain according to UI-73. The current region click prepares to remain. From UI-219 (2026-10-17) the route has no second click: the step that uses all of the Progress moves at once (with no Progress, the click on the Fellowship's region stays at once), and a shorter legal route or a stay with Progress left is sent with ✓ in the Fellowship's decision panel (or Enter) once the server preview allows it. Undo step and Cancel only applies to the local route; mandatory disclosure remains pending decision. Preview shows progress reset, legal treatment/activation or known additional fortresses, without the result of the tiles. Change of draft, projection and panel opening does not move the camera.

UI-34-GUIDES (UI-249): a voluntary Guide is chosen directly on its glowing portrait, whose tooltip says before the click that it ends the phase; there is no Change guide mode. Forced choice remains direct. Guide has the badge and hint of the correct, visible version. Other portraits can be viewed without changing the decision. Separation: selected die → portraits → route → re-click the end of the route. Undo/Cancel does not remove the group of mandatory Hobbit return. Forced separation in Mordor shows Remove from play. Before approval, it is shown by the Separated associations cannot rejoin.

UI-34-HUNT: click or Enter/Space on the Hunt with Eye page opens the moving window. Hover/fokus shows only a public description available. One tray supports the decision: allocation, reroll, injury and Guide in Hunt belong to the Eye, and a regional route to Fellowship/map. dice (from UI-88 without a step Eye) or drop on the Eye prepares the set; click the assigned one withdraws it. Allocate & roll/Keep roll/Reroll selected approves the entire set selected. The meshes, hits, bonuses and the limit of the spreads are separate. The cable shows Damage ?, Reveal and Stop independently. The public report retains the known results at the end of the procedure; the previous entries are available without scrolling. The lock survives GET/reconnect of the same decision, and the opening does not move the map.

UI-34-RESPONSES replaces the historical single click Ring/Guide/Random selection → tile. From UI-142 click answers again ends step right away (the selection is complete); drop on the tile remains a shortcut. The first click shows the known preview, draws no prey. No Confirm; cards reactions and guides retain their own windows and legal Skip. The tile replaces the throw as the dominant element, with access to the earlier results in the public report. The limits of the decision and reconnect protect DraftInput.

UI-34-MORDOR shows the movement/hide test in the current turn without reading the story: done, required before the end of action (+1 Corruption), information not known in the older view. The entry is not a trial, Stop is it. After the game result, the reminder disappears. Hunt clearly describes the lack of a throw on the track, and Locate sets the focus on the track without moving the camera.

Recover answers in UI-34 opens the right window once for uncertain command. `Check result` remains achievable when the phase or decision owner has changed in the meantime. The shutdown is still respected.

<a id="rozwinięcie-u09--plan-ui-49ui-54"></a>
### Development U09 — UI-49-UI-54 plan

UI-48 saves a new layout in [HUNT_UX](HUNT_UX.md): header and decision owner, short run, main object and own responses with visible effect. UI-49–UI-53 implements the layout/dice/folder, consequences/Mordor and integration with existing log/notifications; UI-54 receives H01–H14. This is a new plan, not yet proof of acceptance UI-35.

The automatic result goes to the log and the proper existing notifications; it does not open Hunt or add text to the Eye. The new decision still opens once without changing the camera. The own automatic effect does not create a red alert. The manual Eye retains the last public report. Ring/Guide/Random shows the primary effect without the hive, with the unknown Random result and the gesture-preserved choice of → tile. The options of the other side are not controls; the public run remains available. Mordor uses the same window without a throw and without the promise to move before the result.

<a id="u10--dodatki-i-setup"></a>
## U10 — expansions and setup

All eight combinations and required variants use the same language. Council shows comparison of version and arrangement of portraits, with privacy. Kings: Eye → sovereign → legal tiles, and the sequential variant retains the public Draw/Stop. Capability and reactions are with the right owner. Ruler token, special ability and simple wall remain different costs. We do not add dice to free reactions nor overlook the exceptions of card for convenience.

<a id="u11--powiadomienia-i-powrót"></a>
## U11 — notifications and return

The incoming decision shows the notification in the application and backlights the location. Only a click of the notifications moves the camera to the location and opens the panel, e.g. the battle region. No automatic view acquisition. The information can be read during waiting/offline; execution requires a current decision.

Reconct restores the current state and the sequence started from the server. Unsent local selection retains legal elements without changing the cost. Timeout recovers the same commandId/receipt. Ongoing upload and revision changes do not allow you to double click on the next decision. We do not force the animation to be played.

Plan  UI-38 – UI-43 with [BATTLE_ACTIVITY_UX](BATTLE_ACTIVITY_UX.md) adds the switched Log of the entire batch, common card   action and the red tabs of the opponent and the gold of the current decision. The red click immediately shows the cost, participants, course and result when appropriate UI with static map markings; does not force the opening of the log. The public opening only removes the shown version of the red message. Gold remains to decide the selection. Auto-open Fellowship / Hunt maintain the existing behaviour; battle It does not open itself. Reading alerts is separate from match seen. Details, legacies and BA01 – BA16 are a specification for the future implementation of which acceptance belongs to UI-47.

<a id="u12--jakość-i-odbiór"></a>
## U12 — Quality and acceptance

1366×768 and 1920×1080, scale 100/125%, mouse, plaice and focus. The color is symbol/kszta Local response to selection to 100 ms; purpose 60   FPS on an open machine. Delaying text tooltipu (hover 200 ms, focus right away) we measure separately from the response of the selection and opening of the interactive panel at the click. Reduced motion and ominous short animations.

The previews read the safe PlayerView, indexed according to regionu/ID. PointerMove does not ask the server. The field update does not play the entire UI; stable controls maintain focus and pointer capture. Tests also check dense stacks, screen edges, quick cursor crossings, no panel or API after hover/focus, unscathed double-click/drop, one Esc for the active window and no secret leak.

Build Windows is required, runtime Windows and manual acceptance the user is not performed according to the disposition. We do not declare readiness on the basis of the fixture or compilation itself. Each task updates the documentation with actual behavior and results.

<a id="wdrożony-wspólny-kontekst-ui-15"></a>
## Implementing Common Context UI-15

**History of UI-15-UI-30:** The following descriptions until U13 document these versions and their acceptance. Times of hover panels, former selection of categories of dice, target holders and disclosure of figures after invading are not the current standard; the U01–U17 and UI-73–UI-80 apply. The numbers, sources and results of previous tests remain historical.

Runtime has a choice of the symbol with a specific dice, neutral icons and interactive preview of the 200/150 region, common to units and characters. The output from the preview retains the local choice. Two-click and drag check the current decision range. Hover reads the safe view index and does not send commands; it does not replace the attached card. Integrating full direct action and the target compact tray layout provide the next tasks. Evidence and limitations: UI-15.

UI-16 adds server steps to march, recruit and travel groups. After reconnecting their cost Resolving, the steps and the remaining budget are made from `ActionSequenceView`, never from the restored local draft. die continuation is already paid for. `FinishActionSequence` ends the budget and the required excess/reaction remains a separate window. The gesture views are implemented by UI-17/UI-20/UI-21; the server contract itself does not mean them acceptance.

UI-17 connect normal costs Army/Character , symbol selected for Army/Muster or Will and the continuation of paid march. Own flag opens the lineup, two click selects available figure The field click or legal opponent and drop of the selected lineup use a sender without Confirm. The preview does not obscure the target labels: the map takes into account its rectangle when they are deployed. Two progress symbols and Finish are at dice Capacity costs and tokens retain existing public integration controls UI-22 ; UI-17 does not give them the usual cost. Results acceptance : UI-17 .


UI-18 replaces the hand lists with permanent miniatures and two decks. From UI-217 a card name that needs more than two lines in the collapsed tile gets just enough smaller type for two lines (not below 8 px, the longest word on one line); shorter names keep the normal size and the unrolled card keeps its full title. Single click clips the text. In the excess hand twoclick rejects one card; selection card the cost is still subject to a separate decision. double-click/drop use exactly indicated dice and the symbol, and Combat and the free chain of events do not add cost. The required purpose of the nation is to highlight its emblem. Selection by clicking the deck and the single choice of effect perform a step without Confirm; discards as cost and sets figures have ✓; the excess hand has no additional Confirm. Jerkír has a separate conversion symbol at dice The full text of both applications, table previews, pin and public reverses retain the existing sources/projections. acceptance and its scope: UI-18.

UI-19 Move battle and loss of event to the panel at the region. The button ⁇ in the bottom bar or indication of the region opens the context; × hides the panel and retains an unsent selection. Another decision of the same battle keeps the opening, new battle He's waiting for the player. none/ ↓/ ×, regular none/ ×; symbols and order are on figure, a complete explanation in the tooltip. A set of losses and reroll has one ✓ The retreat ends with a click of the target, entering: figure   →   cel/drop. The extension elite click shows the actual cost preview, the two-click performs it without an additional button. card, ordinary and special throws, garrison and round report retain existing projection and rules.

UI-20 connects Muster to settlements and emblems of nations. Palette in the preview of the region shows legal figure, reserve size and inaccessible types. The recruit click prepares one step and preview the effects, click the target executes it; a double click in the pallet or drag figure They're shortcuts. figure They go to different settlements, the elite uses two points, and ✓ on paid dice The removal of a lone commander is a clear result of a legal recruit, not a blockade. An excess is chosen figure after completion action The track appears after invading the emblem, without the choice of the nation. → next field or drag emblem performs diplomacy. command four positions distinguish the current state, legal next step and At War; tooltip explains the nation's activity. This single track replaces in UI-25 common panel described in U05.

UI-22: Action phase has no general command list. The selected die reveals legal components: ability in interactive preview of its region, entry of new character at resources, removal of effect at the tab on the table, Ring and Ruler token in the pool. The sovereign preview opens after 200 ms and disappears 150 ms after leaving the portrait and panel; click can pin it. Camera location and game status remain unchanged. Selecting the ability to set its apparent special cost and retains the exact die. The required commander must be in the group, Balroga symbol only makes his journey available, and Far-sighted retains the token and range from the server option. Selecting a specific cost/symbol does not run another capability automatically.

Single Choice dice special, rejected dice Ruler, received token Council or tile Corruption ends the decision by clicking. These are individual objects, so they do not require additional ✓. Comparison of Council versions, deployment of starting portraits, cost of two cards Protection and an optional set of capacity recruits retain one ✓ The Council's location works with clicks and drop of a portrait on the region; completed steps of a paid trip saves the server, and the free multigroup journey of the Token Council approves the entire selected set.

Travels Gwaihir/We Prove the Swifter: a portrait in Fellowship begins the separation, figure in the region preview begins the external journey. Both of them belong to the already paid card, retain its limit of one group and end in gesture at the end of the actual route. We do not select another dice or mode from the list. Tooltip target shows a well-known description of the effect from the server option.

UI-23: button ⁇ Your decision and character on the map show the current context. Click centred the known region and opens the panel; Council, normal action or Mordor without the region do not cause an imaginary shift. Each click started must end in the same range of decisions and connections. Return from the reconnet does not revive the previously pressed deck. Offline maintains editing the unsent composition and inspection, and the reconnection itself does not approve anything.

UI-24: during declaration and disclosure, the Fellowship panel narrows down to the context of trasy/Polowania. Portraits return after completion or withdrawal of draft; we do not reject or change anything Guide. All the adjacent steps of the regular route have handles, even when they are finished with more than eight. The flight retains the lighting of the regions and the end handle. Eight portraits with expansions are in one row, with full information in the tooltips. The base pool houses five symbols of Will without an additional row. The directory describes the responsive layout instead of the old coordinates of the UI-01 mock.

The map bar uses ⁇ to centre Fellowship and ⁇ for the report of the last battle; tooltip and focus explain the meaning. The click goal has at least 32 points also at 125%, and the bar remains next to the attached card. The message ⁇ Your decision retains text. Long portrait labels end with a multidot instead of coming onto the neighbor; full name and role remain in preview.

Short hints are rendered by the client outside the Editor: after 200 ms howera or after focus setting. They disappear at the output, start gesture, Esc, change scale and remove the control. They do not block the click goal, do not create a second layer over the interactive preview of cards/region and do not show the value of the fields entered. The inactive object can still explain its state. The content comes solely from the existing view of the player.

UI-29 keeps its hand in two rows, also for excess at 125%. The usual preview is 336/400-wide; long text in the low area of the map uses 440/500 and smaller spaces. The rule text remains full, 14-point, without scroll and without hiding under the tray. Your decision opens Cards for the required karty/odrzutu/talii. End Evidence and build: UI-29.

UI-30 : figure They're hidden in the distance on the map. 160% matches reveal tags; drop below 145% hides. Hover region and its panel unveils only this region, the selected group remains until the end of the selection. Sources action have a outline also without figures . garrison/field they get local places without placing labels on the screen; status Fellowship He's still available. figures in the remote area replaces the user’s decision with 2026-10-13 ( UI-123 , [ARMY_MAP_UX](ARMY_MAP_UX.md) ) armies always visible in two levels of detail; implementation UI-126 .

In UI-30, during the drag, the composition of the region extinguishs and passes drop to the area under the panel; its controls do not change position. Return from drag/cancel restores hit-test. The hidden tag returns focus of the map, so further Esc still reverses local selection.

<a id="u13--widoczność-figur-na-mapie"></a>
## U13 — figures visibility on the map

In the distance figure are hidden; zoom from 160% matches reveal them, and drop below 145% hides. The region or figure opens its composition; the invading of the area never opens the description or composition. The selected group and the revealed result of the approved movement retain a clear presentation according to UI-73/UI-76. We do not show the supposed position of the hidden Fellowship. Legal sources and targets have a outline also with the hidden figure. During the dredge, the open composition of the drop stalls and passes to the area while retaining the geometry; cancel restores the hit-test.

From UI-126 the 160%/145% threshold no longer hide the army. Below 1,3× match (return below 1,2×) each force has a token in the hand enamel with the coat of arms of the most numerous nation and number; above, and for the region open-click, selected strength and fresh arrival, figurines on the stand with coat of arms and number. The garrison has blanks. The power government stands over the symbol settlement and does not obscure it; the tooltip gives the composition per nation. The Fellowship tag uses the same threshold 1,3×. Details: [ARMY_MAP_UX](ARMY_MAP_UX.md).

<a id="u14--przesuwane-okna-ui-32"></a>
## U14 — Sliding windows (UI-32)

Information windows, resources, report and panel battle, Fellowship, pinned card, sovereign and open click Cards/Politics have a handle in the header. Drag preview button attaches it; hower does not change the position/choice itself. Buttons, checkbox and game objects are not handles. The header also supports keyboard focuss. Location remains by zoom, refresh the projection, close and reopen, and return from the list of parties.

The memory only lasts until the client restarts. The size/scale change brings the window to the map area. Settings → Reset window positions restores anchors. The fixed bookmarks, statuses and trays of dice are not moved. The layout of the windows is not command, it does not change revisions and does not select the game components.

The acceptance UI-35 precision: the default hand and the view card retain the anchor at the right edge when changing the text, resolution or scale. Only the clear shift remembers the player's own position. The preview in the default layout cannot obscure the cards intended for discard. The focus outline remains in the layer of the window; it does not move over the second window open above and does not change the geometry of the controls. From UI-122 the outline appears only after using the keyboard or navigation; clicking gives the focus without a white outline (BOARD_INTERACTION_UX, UI-122).

The hand with the click remains open also after leaving the entire application window and losing focus. These events remove the information tooltip; the focus loss also cancels the ongoing gesture. The ×/Esc public closes the panel. Tooltip is not a sliding window and does not capture the first Esc.

<a id="u15--pozostały-interfejs-partii-plan-ui-56ui-63"></a>
## U15 — Other Party Interface (Plan UI-56-UI-63)

[IN_GAME_UX](IN_GAME_UX.md) develops U06/U07/U10/U11 with Effects, Resources, Sovereigns, Victory and Help. Click or Enter/Space on Effects develops public sources of both sides, coverage/conditions of duration and legal removal at cost. Hover/fokus source shows its own text, without opening a full card. Local marker does not open the region; global effects are with narodzie/parametrze.

Resources show a specific source, cost and proper Recovery. Own new choices at the pool open once without camera, and Nenya/Vilya retain the right host. Kings has a portrait bar at Politics and a test view at sovereignty: a simple selection or Sequential Draw/Stop. Visible does not reveal a private selection; Corrupted does not become a normal figure combat.

Victory retains a fixed shortcut of points with a break-up after clicking or Enter/Space. Military threshold is a pending sign of the 6 phase, not earlier Winner. The final message remains on board with an inspection without revealing secrets. Tooltip explains the condition on hover/focus, and why unavailable?, ? and Help opened interactive help after clicking or Enter/Space (disabled in UI-173). Reading gestures, legends and configuration does not change draft or the rules.

UI-56 provides safe data, UI-57-UI-61 panels, UI-62 integration with existing card action/log and two types of alerts, UI-63 acceptance G01–G18. Current opening requirements result from U03; status determines the register. The scope does not include a lobby, a batch list or a separate summary screen.

<a id="u16--characters-przygotowanie-i-menu-plan-ui-65ui-72"></a>
## U16 — Characters, preparation and Menu (Plan UI-65-UI-72)

[CHARACTERS_SETUP_MENU_UX](CHARACTERS_SETUP_MENU_UX.md) develops U05/U10–U14. Characters are the own rolled-up list of only name characters outside Fellowship and possible inputs. The usual Nazgûle remain on the map. He board/Not in play/ Removed is due to a safe projection, and one figure retains one entry during the transformation. The preview is shared with the map and awakened sovereign.

Click or Enter/Space opens Characters and shared card characters. Hover/fokus gives only a description, without changing your choice, camera or private version. dice click the portrait selects the trip unless it is a participant in the army being elected; without context it opens information. Enter/change/zdolno The exact cost, purpose and proper recovery are the exact ones. Hunt/battle/Dru The rye remains the hosts of the reaction.

From UI-139 card the character says with symbols: the list line has a level star on the portrait and a status symbol with the region; card has a status symbol, pass symbols statistics (level, movement or flight, combat level, Leadership emblem, nation emblem) and full skill text develops click on the token. Each symbol has a tooltip ([CHARACTERS_SETUP_MENU_UX](CHARACTERS_SETUP_MENU_UX.md), “card characters in symbols — UI-139).

Lords/Council: one private version panel and Guide with one ✓ Homeland is a separate decision in the first Fellowship Phase: portrait → region or drop, Fellowship withdraws allocation, one ✓ Approves an empty set as well. Window without candidates protects privacy; tokens go to resources Shadow.

The menu includes Resume/Victory/Settings/Games (Help removed in UI-173) and does not pause the batch. The 100/125% and Reduced Motion preferences are local, window positions are only in session. Esc has one receiver; tooltip extinguishs its text and passes the same key to the window or draft. Games waits for local draft/journal recording and checks the Error without exception. Error leaves board and Retry. Return retrieves the existing receipt, does not perform draft automatically.

UI-65-UI-71 implement data, panels and integration; UI-72 receives CM01-CM18 after UI-63. The current way of opening determines U03 and the state of acceptance register.


<a id="u17--mapa-z-konturów-plan-map-12map-17"></a>
## U17 — contour map (Plan MAP-12-MAP-17)

[MAP_CONTOURS_UX](MAP_CONTOURS_UX.md) describes the swapping of approximate geometry to existing 105 fields and basic renderer. Non-passive sections are black; normal white transitions, and bands of nations on the right side of the border according to BASE-2.4.1 p.9–10. Two nations have two colors separated by white. Military control has a separate tag; Fate/White Hand use public RegionOverrides. The color of the border does not replace legal options.

The ground floor does not contain burnt contours. Drawing, hit-test, markers, targets and panels divide the transformation; water/holes are not regions. Raster covers the screen without an external background after zoom/pan/resize/Locate. Change Map Keeps U02/U03: each region opens the information by clicking and the mountain of the site only distinguishes it. Non-interactive tooltip figure uses its public metadata, does not open the region or choose an army. Legal final click/drop uses a common sender without arrows and handles. New view does not show controls action accessible only on the other side or hidden route Fellowship The plan requires MC01 – MC18 Both roles, expansions , player recovery and measurement. MAP-11 completes the documentation, does not mean implementation A new map at Unity.

<a id="doprecyzowanie-log--ui-41"></a>
## Log precision — UI-41

**History of implementations of UI-41–UI-72:** Next sections up to “Comprehensive panels — UI-78” record the solutions and results of their versions at the time. They are not a repeat acceptance UI-73–UI-80. The former opening of panels by hover/focus, arrows, handles and container-fit describe that runtime; the current requirements are in U01–U17. Sources and actual measurements were preserved.

History replaces Log, with All/Opponent/You filters and a full paged history. The reading does not disable Inspect, does not change draft, dice or camera. card has historical facts and cost of the performer; the legacy entry means no information. Log and pinned card remain separate moving windows; the update retains the reading fragment and focus. Scrolling refers to the optional history, not the current decision panel. UI-42 moves the full preview to the object context.

<a id="kontekst-zapisanej-akcji--ui-42"></a>
## Background of the saved action — UI-42

card action appears at the right mechanics and has the signature Recorded action. It uses the regions, composition and version saved, without re-election of dice or target. A separate non-interactive layer of the map shows the route and attack with stages numbers. Current goals, drag and master have priority; Close removes the markings.

The battle report and the facts of Hunt are identified independently of the current mechanics. The earlier step does not receive a later round. Call outside Log keeps the closed log. Details of BA06 and check: UI-42.

<a id="powiadomienia--ui-43"></a>
## Notifications — UI-43

The fixed bar reserves space for the current decision and the new actions of the opponent. Gold leads to the current controls and remains until the resolution; red leads to the saved version of action. Hover/fokus gives the preview without reading and without changing the camera. +N shows the remaining groups in the fastened, sliding window. The clearly shown complete version disappears; the later update remains new. Local recording does not transfer reading between devices or accounts. Description, privacy and checking: BA07–BA08 and UI-43.

<a id="regionalne-okno-bitwy--ui-44"></a>
## Regional window battle — UI-44

U08 receives the Hover 200/150 ms, pinned, moving and behavior Close on the GET again. The region and window form a common area; the normal RegionHover does not duplicate the fighting context. The new battle does not open the panel or move the camera. The public composition, rear guard/garnison, server statistics and the current decision owner remain clearly separated.

Combat is selected inside battle through its own thumbnails and shared full preview with Play, double click or drop. No card is separate. The hand remains clear without the second track of the same command. The already mandatory or archived preview of card is read-only. Details and evidence: UI-44; BA10–BA13 develop UI-45/UI-46.

<a id="ui-45--kostki-walki-i-zestaw-strat"></a>
## UI-45 — Fight cubes and set of losses

The natural meshes and the result of the reroll are side by side; the bonus, hit and source remain separate information. The check failed die does not send command, and Keep roll/Reroll selected completes the entire selection. The opponent does not receive prior access to the selected dice.

The loss set shows order and budget; tooltip figure describes the effect of a legal server preview. Clear/reselect moves figure at the end of the operation. Apply loses is one approval without entering a number or scroll. Separate history panels can be scrolled.

<a id="ui-46--decyzje-końca-bitwy-i-historia-rund"></a>
## UI-46 — End Decisions battle and the history of rounds

The cost preview siege describes figure and its actual effect; it does not use the injury counter. Click selects, doubleclick approves, Stop ends battle. The illegal retreat target does not change the state. Partial entry requires the selection of participants and region; control/points updates with the result of the server.

Battle report window has fixed header, status and arrow rounds. It is a moving read: new event does not replace the report, and closing invalidates the delayed GET The Last Combat round shortcut leads to battle last saved throw; Log also shares battle We don't create a fictional round.

<a id="poprawki-dostępności-wspólnego-przebiegu--ui-47"></a>
## Joint accessibility improvements — UI-47

At low logical height, the dock is ordered by dice in two columns and status with the final buttons in the common row. The Council and Fellowship in Mordor have smaller spaces, without mandatory decision scrolling. The region preview also bypasses the Fellowship tag. The inspection by clicking hits the top; the same Hower does not change the selection. card Combat automatically divides the space with the battle window, retaining manual window positions.

The focus from the keyboard still shows card; the focus created during PointerDown waits for the click to complete, not to obscure the button. The decision alert also contains the automatic result of the same revision. The range of CONTEXT remains: action only the right side, public information of both players, an empty region opened by clicking. Evidence and boundaries of acceptance collects UI-47.

<a id="układ-hunt--ui-49"></a>
## Hunt layout — UI-49

Hunt allocation / Current Hunt / Last Hunt have separate headers and a clear state of lack of report. The current object is separated from three steps of history and from its own answers. Guide replaces the tile in the main area; the assignment does not display the result of the previous action. The story is focussable and retains the older page at the new entry, without changing the game. The owner comes from projections, and other controls remain hidden. acceptance: UI-49.

<a id="ui-56--informacje-potrzebne-przed-gestem"></a>
## UI-56 — information needed before gesture

The self-contained reason why unavailable? receives separate costs, legal objectives, conditions, limit used and appropriate timing from the server. The fulfilled conditions outside their own window do not disable the inspection and do not enable the sending of command. UI does not calculate the rules for the absence of ActionOption. The results are preserved by different terms and ranges, and Victory shows the origin of the points and the timing of the evaluation without premature announcement of victory. Neutral deficiencies of older data are not a zero of secret sum or a right to act. implementation data is described by UI-56; the panel design: [IN_GAME_UX](IN_GAME_UX.md).

<a id="ui-51--skutki-odpowiedzi-bez-hovera"></a>
## UI-51 — the effects of a non-hover response

In Hunt, their own legal responses show a known effect without choice and without tooltip. Objects retain identity when selection is selected. Short labels under Ring, Guide and Random company do not predict RNG or later Guide capability. The response shows cost and source without additional menu; inspection develops details. The gestures and order of the server are described by [HUNT_UX](HUNT_UX.md) and UI-51.


<a id="ui-57--effects-przy-źródłach"></a>
## UI-57 — Effects at sources

The Effects · n tab is at the left edge under Politics and also works at zero. The counter groupes the sources of both sides, including public capabilities and card on the table without a separate modifier. The larger list has four pages without scrolling. The source click shows under the list its details, and for card also full text (from UI-94 without a separate window card); Locate is an open navigation to the selected region. Effects/time limits they are not glued together. On the low screen, the range group are side by side. Own legal removal of the alien effect is at the source and shows the exact cost. Two discards as protection cost have one ✓; the selected Ring ends the Jerkíra cost without additional Confirm. The incomplete cost does not spend resources. The viewer's lack of permission hides the removal. The expire removes the button and the tags, but pinned text can be left with Effect ended. Close does not delete the starting cost. Local tags group sources and do not take over clicks/drops; legitimate target action The region with the effect itself is still empty for the hover and requires a click. The overall/national impact does not multiply icons in each region.

<a id="ui-52--kontynuacja-bez-dodatkowych-paneli"></a>
## UI-52 — continuation without additional panels

Guide, its ability, return and Reveal have one tray and a clear cost of the ongoing action. In Mordor, the tile does not promise a move before settlement; Reveal and Stop are independent. Forced separation says Remove from play. Hunt and passive preview Fellowship contain decisions without scroll also at 125%. Default positions respond to host and size changes, and manual shifts remain remembered in the session. Full matrix: UI-52.

<a id="ui-58--wybór-zasobu-bez-kopii-obiektów"></a>
## UI-58 — selection of a resource without a copy of objects

dice Keeper/Lesser and Ruler discards are selected in the pool; the panel gives a short effect before gesture. The new decision opens Dice & tokens once, Close retains the choice. The council does not choose strategic application or cost per player; its own applications and accurate dice inputs are highlighted. The change of the wall The Ring does not start using dice. Objects not available retain the source inspection, conditions and appropriate recovery time. The enemies of dice may be the target of their own Dismay according to server options.

<a id="ui-53--h05h11h12-w-historii"></a>
## UI-53 — H05/H11/H12 in history

The automatic result of your own action remains in Log without a new red alert and without opening Hunt. card red tab shows saved meshes, hits, tiles or its missing and the result of the right string; no additional click in Log is required. The reading only includes the indicated step and its automatic consequences from the same revision. The later step remains unread, and the gold tab continues to open its current decision.

<a id="ui-60--wynik-na-planszy-i-dostępność-podglądów"></a>
## UI-60 — score on board and preview availability

The fixed FP n/4 · SH n/10 opens the hoverem/fokusem/klikiem panel of both sides. The button, source, preview and result remain available while waiting, reconnect and end of the game. Columns show city/fortress, region, scoring and control page; at over three regions have pages (low view of two lines), without scroll. The threshold achieves the right page and shows Threshold readed · Checked at turn end, not calculated by the client. The final score is a small banner over a shortcut; both sides retain an inspection without new secrets. The conditions of the Ring lead to an existing Fellowship without changing the camera and without copying the track. The location is an open navigation and leaves no unreached destination, even after the previously moving the clip. The fixed shortcut retains its own area, and the banner omitting inspector. The header uses the window memory; the location is not re-opening, or re-resing the selection of the panel.

The Fellowship system's regression revealed the cover of the region's portraits by pinned window. RegionHover raises the new/re to the top of the open, without changing the state of the game or the order of the windows at the same preview. The FellowshipGroupsHuntAndMordorFitBothSizesAndScalesWithoutScrolling test uses a real hit-test and checks the selection of all seven characters in four layouts. Results and previous failures remain in UI-60.

<a id="ui-65--własne-figury-i-kandydatury"></a>
## UI-65 — own figure and candidacy

UI-65 provides real data CM01/CM02 And then they came UI-66 uses them without a local planner. Own candidatures also exist in someone else’s turn but WindowOpen/CanAct They're false. Gandalf/Strider may have their own entry InFellowship where it contains a future transformation; UI-66 does not need to repeat the composition by this Fellowship The usual Nazgûle, Gollum and Sméagol do not go to the Characters. Awakened sovereigns and their elimination retain them figure ; the early and corrupt remain in UI-59 . Future version of the living figure is a description in the same entry. UI-66/UI-67 They still have their own acceptance interactions.

<a id="map-14--czytelność-podstawowej-mapy"></a>
## MAP-14 — the readability of the basic map

The view of the regions comes from accurate contours, and the interchangeable paper/area remains a simple foundation. One common white border means transition; adjacent colors mean Nation on the right side. The black barrier has greater weight and remains over each interaction filling. The control of settlement uses its own Free Peoples/Shadow symbol, without changing the colour of the nation. The fixed sediment symbols do not tolerate the rules of concealing units to zoomu/najechania. Water remains outside the gameable contours.

Legend “ ⁇ ” is available on request in English; shows a white line, barrier, eight colors, sediment and control symbols. The layout above map controls houses full content without scrolling. Opening a legend or moving a camera does not select a region and do not perform command. Selection, source, target, attack and protection have delicate fillings under borders; attention remains a separate tag. Verification of the actual image, response time and texture budget is described in MAP-14 and [MC07–MC08](MAP_CONTOURS_UX.md).

<a id="ui-59--decyzje-przy-portrecie-suwerena"></a>
## UI-59 — decisions on the sovereign portrait

The Politics strip shows three or five portraits according to the projection. Hover/fokus opens a public preview, click it pin. The preview shows the state, region, immunity, overt tiles or reverses and safe conditions; Location is an open navigation. The inspection itself does not move the camera or draft.

The new decision opens one host at the right portrait. die and the selected target remain tied after Close and reconnet. Stay here ends legal awakening; the route retains the existing gestures of the map. Before the end you can see the return of the tile, the identity of the token and conditional subsequent Recovery Ruler. Back adjacent to Stay, the repetitive gesture instruction is in the tooltip of the route; the error and the result of the preview remain visible. No additional Confirm or Place til switch after the selection of the legal tile. The attempt has a cost and limit, public omissions and own legal Draw/Stop or the tiles. Sequential does not restore the previous result to the selection, and Visible does not release FP's private selection of the Shadow. The random return after the policy is signed separately, without the value selector or additional dice.

Close closes the preview and the gold tab returns to the current host; the inspection of another portrait does not change the desired purpose of the required decision. The window can be moved. Automatic position bypasses the fixed shortcuts and other visible panels, and the compact dock leaves more space for map and decision without scrolling. Public counter and omitted tiles share a row; private Stop and Draw remain directly next to each other. Cleaning labels do not display technical ID target.

<a id="map-15--inspekcja-i-gesty-na-rzeczywistym-obrysie"></a>
## MAP-15 — inspection and gestures on the real scratch

`contours-2` contours retain the die → object → highlighted region. Click/drop ends legal step without Confirm; hower and transition between map and panel do not change the selected resource or army. The empty region only opens the information after clicking or seal; forces and public Fellowship remain accessible by the hiver. SH sees the public Fellowship without the controls FP.

pinned card retains its location when moving the map. If the Focus covers a small region, the public master discovers the area next to the panel without unscrewing the card. The trials in both roles and four layouts require the actual `panel.Pick` on the map; they do not replace the area with a distant button or a moved hit field. With 125%, information panels may partially overlap with card; the evidence relates to the availability of the site, correct target, unchanged selection and lack of command during the inspection.

The location and the open of the notice/log are using the current `regionId` anchor. The arrival of the state, event or reconnect does not center the map. Evidence, trial limits and the family matrix: [MAP15_INTERACTION_COVERAGE](MAP15_INTERACTION_COVERAGE.md).

<a id="ui-54--odbiór-polowania-i-integracji"></a>
## UI-54 — acceptance Hunt and integration

UI-54 is a collective acceptance H01–H14: the cost and RNG still on the server side, the inspection without changing the selection, one host of the current decision, readable Effects/Skip, no enemy controls and four systems. The results of the domain, network, drop, R23 and separate Player are separated. Results: .NET3723/3723, Editor449/449, Play99/99 unique methods; 56 measurements of the Player and final dragons of the PASS login screen.

<a id="ui-66--lista-characters-i-jedna-karta-postaci"></a>
## UI-66 — Charters list and one card character

The Charters tab next to the Politics shares the rules of hower and pin with other previews. Own roster and public portrait on the map lead to one card; the same works Awakened with the sovereign bar. Short signatures do not replace the full inspection. The capacity name is focussable and has a full description of the action and the sources in the tooltip. cost/conditions future version is read from the server; the inspection does not pay itself or selects action.

pinned card is protected from swapping by the hiver. The disappearance of the future version button after the change retains focus in the common inspector. The location appears only for the real region; the opening or update itself does not move the map. UI-66.

<a id="ui-61--pomoc-kontekstowa-i-wyjaśnienia-dostępności"></a>
## UI-61 — Contextual assistance and accessibility clarifications

Replaced by UI-173: the game no longer has a Help panel or links to it.

Global Help replaces Rules; the preserved internal BoardInformation. Rules still serves the old configuration preview. Small ? with dice, card, politics, Fellowship, Hunt, fighting, effects, resources, sovereigns, characters and Victory opens the right subject. Why unavailable? presents costs, conditions, targets, limits and timing of the server; no explanation remains an obvious lack of data. The themes of expansions/variants are filtered according to configuration; Fate explicitly omits Dáin/Brand as sovereigns and leaves the remaining three; the modified card form of Fate concerns two Gimli's obverses.

The help is optional to read scrolling; decisions still use their unscrewed hosts. Esc closes Help only and restores the existing visible source focus (fallback: global Help). Enter in Help does not go to action approval. The click outside the unsealed window closes the help itself; pin keeps it open, and the moves are remembered by FloatingWindow. Reconct retains draft and the subject and indicates the last item received. The changes to the decision refresh actor/timing, without the action and without the camera motion.

<a id="panel-przygotowania-drużyny--ui-68"></a>
## Fellowship Preparation Panel — UI-68

Custom CouncilVersions opens one sliding panel with portraits, two card comparison and a separate starting guide selection. The portrait changes only the view; click card choose the version. Names of the ability give a full description and source on hover/focus. The markings for portraits show current versions. No Gandalf or Guide required has a short local clue and blocks ✓.

Esc/ × closes the panel without resetting the election; the tab or the gold decision returns to it. Enter in comparison does not accidentally approve the entire setup. One final ✓ send the whole set; double click does not execute another decision. Shadow does not get a panel or progress of private choice. Four layouts, privacy and reboot confirm acceptance UI-68:  3 new network trials and 7 regression, without scrolling or clipping resources.

<a id="akcje-characters--ui-67"></a>
## action Characters — UI-67

The Characters list and map edit one choice figur/trasy. Legal portrait responds to the current specific die; click does not automatically turn on Balrog, Far-sighted or any other special ability. Hover and Pin continue to serve the inspection itself. The change of origin only replaces the unsent group, and the costs and other steps after the recording return from the server.

The entry, transformation and active ability have the same tray in the character inspector. The re-election button of the already active capacity is missing; the full conditions and the additional date of dice remain available. Close/reconnect do not perform the prepared selection. The golden shortcut returns to the host, the view of another figure does not change the payload. The opponent retains public inspection without performance controls. The state of the tests and limitations: UI-67.

<a id="historia-i-powrót-do-decyzji--ui-62"></a>
## History and Back to Decision — UI-62

Effects, resources, sovereign and Victory have their own anchors together card action . The cost and result are historical, also when the source has disappeared. Gold tab opens the actual host of one current tray; history preview and Help do not replace the selection. Manual opening battle indicates region, automatic alert only points to attention. The unknown long-standing value remains unknown. Common sources show public sovereign names and Council applications instead of their technical ID Restart tests, privacy, separate proof TLS and actual results and re-executed are recorded in UI-62 . Final range: 39/39 Play, 26/26 appropriate Editor and Review 17 PNG ; profiling and four systems of the entire new interface receives UI-63 .

<a id="menu-przy-planszy--ui-70"></a>
## Menu at board — UI-70

The menu in the top right corner provides four items. The click outside the panel closes it when you release the mouse and does not run the object under the bottom. Esc performs one operation, also when the key remains pressed. Closes the inspection does not reverse draft. Settings and Help return to the visible focus source. The menu remains offline and after the end of the batch.

Games wait for local record and check out the public errors of both logs and draft. Error leaves board with Retry. Resume/Settings/Help can stop waiting for the exit of board; completion of late write does not then move to Games. Uncertain command returns by the previous commandId and receipt, without repeating the cost or automatic approval of the unfinished draft.

<a id="ui-70--granica-esc-przy-settings"></a>
### UI-70 — Esc border at Settings

Return focus after closing Settings retains the owner of the same physical pressing Esc. Reruns without KeyUp do not close another window or open Menu. Passive keyboard observation ends when board is disconnected; fields and other screens retain their own support.

The final 20/20 Play also checked the actual window drag and map master, the local Retra I/O, the return after the lost receipt and four Menu layouts. The editor includes the 15/15 required cases; full evidence and boundaries of acceptance: UI-70. UI-71 retains the same focus and write policy when the navigation between decision hosts.

<a id="map-17--zweryfikowane-kontury-gęste-sceny-i-pełna-karta"></a>
## MAP-17 — verified contours, dense scenes and full card

[acceptance MC01-MC18](MAP17_ACCEPTANCE.md) includes a common drawing and aiming geometry, all 105 anchors and four 1366×768/1920×1080 with 100/125%. National bands remain separate from military control; the highlight does not cover black barriers or white crossings. Separate field/garrison groups feature 23 figure without mandatory scrolling. Empty regions retain information after clicking.

The long card with the visible action bar is evaluated on the full measured text of both effects. When the height is missing, it reduces padding and increases width in the available space; it does not require scroll or downsize of the rule text. pinned position and size remain stable after refresh. Victory page bypasses the visible card, and both texts and the page itself have a real hit test after refresh.

Fit shows the entire source atlas, does not cut east or south. Permanent Fellowship HUD can at 125% cover the northeast that you unveil. The temporary region hierarch can cover the part manually attached to card; acceptance does not promise full simultaneous visibility of both such windows. Profiles and gestures pointer/wheel do not replace the manual user party.

<a id="ojczyzny-council--ui-69"></a>
## Country Council — UI-69

Private Homelands mode shows portraits and current assignments. Portrait → the highlighted region or drop changes the local set; Fellowship undoes the selected figure. Legal regions are on the map, without the second list of buttons. One ✓ approve the set also when everyone stays in Fellowship. Token announcement distinguishes no, separate choice SH and automatic both; does not grant resources before the server responds. Privacy details, click/drop, four systems and reboots: UI-69.

UI-69 automatically received: click/drop, undo, empty choice, privacy and four layouts of both roles; full results and trial limits are in the to-do document.

<a id="ui-71--historia-wersji-i-jedna-bieżąca-decyzja"></a>
### UI-71 — Version history and one current decision

The historical entry/change entry entry record shows both saved obverses and cost. The guide and setup choices have a context of Fellowship; private election sees only the owner, and public disclosure does not hint the rest of the version. Red story does not replace the current version of the character or its command. Gold shortcut returns to the current host and the exact cost. Menu, Settings and Help retain batch updates, legal draft and focus after closure. The reaction does not create a second tray in Characters; Awakened has one joint inspector. Details acceptance: UI-71.

acceptance UI-71 : 33/33 the required Play methods, 13/13 Full Editor cases 542/542 , 7/7 PNG and 399/399 unchanged domain cases in full 3726/3726 PASS . Separate fresh HTTPS/WSS dragons are credited with trusted CA ; Unity tests benefit from HTTP/WS/PostgreSQL. Accurate filters, reusing identical methods and limits: UI-71 . .


<a id="ui-63--odbiór-gęstych-paneli"></a>
## UI-63 — acceptance dense panels

Effects, full resources, five portraits and help are checked in both resolutions and scales. The very existence of the button is not enough: full boundaries and actual hits are required in the corners and middle. Help remains an optional reader with acceptable scrolling; this does not loosen the scroll decision ban. The manual position of the window is the priority and the automatic layout is to avoid blocking the available targets.

The measurement preparation does not determine the pending decision. Automatically open Fellowship can be closed by a simple Close, maintaining the golden shortcut, phase and cost. Profiles check gesture and result after rendering, while the command and secrecy have separate actual server tests. The current condition of acceptance and the collisions found is described by UI-63.

<a id="końcowy-odbiór-ui-63"></a>
### Final acceptance UI-63

UI-63 confirmed direct gestures and separation of its decisions from the inspection on four layouts. Effects and Help can be opened simultaneously, and five portraits and shortcuts remain available. FloatingWindow omits change of already pixel position smaller than 0.1 px to prevent the clamp from looping the height rounding of card; manual positions and window limits remain preserved. Full editor 551 and final 44 tests PASS. Player measurements 56+32 have passed the thresholds; 52 final PNG have confirmed the viewing in UI-63.

<a id="stabilność-podglądu-i-pomiary--ui-72"></a>
## Preview and measurement stability — UI-72

The location of the Characters preview cannot be looped by rounding its own size. Opening and material change of content still determines the position and manual setting remains superior. UI-72 include 56 actual views of both sides and 40 driver's runs.

The readability tests take into account the way Unity draws the text: padding the button is not a cut-off limit, and the wrapping measurement requires taking into account the physical pixel. The dice objective still has the 32 minimum; control also requires a physical minimum after rasterization, without changing the size or style of the product. Panel boundaries, no collision, availability of targets and full descriptions remain required. Results: UI-72.

<a id="zwarte-panele--ui-78"></a>
## Compact panels — UI-78

The lower HUD adjusts the height to the dice and the current decision; it does not reserve an empty block by action mode. The empty notification line does not take place. Inspection of the empty region is narrower than the full army panel, and the reserves and expanded decisions increase the area according to the content. We do not reduce fonts or hitboxes to hide excess. The graphics of the two Fellowship tracks in UI-79 will replace textual indicators; the status of implementation and the actual results remain in the register.

The own dice and the reserves in the short frame occupy two columns, the actual content of which determines the height. Smaller margins do not change the size of interactive elements or symbols of the remaining budget. UI-78 saves tests and previews of four layouts; the final appearance of tracks and information will be received after UI-79/UI-80.

Open the context of the written action staffing familiar regions in a free place next to her card and the log, maintaining the screen cover with a map. When both conditions cannot be met, it does not change the camera or the position of the window. Normal refresh and re-receive the same inspection do not frame. Cancellation, loss of focus and detas also end right/ middle drug.

<a id="odbiór-interakcji-planszy--ui-77"></a>
## acceptance Interaction board — UI-77

The final version combines information on hover/focus with the panel-based, full map frame and readable figures traffic. The site remains the exception: hower only lightens the region, description requires clicking. Tooltip does not capture the input or Esc and does not perform network reading. Two tracks Fellowship and neighboring Hunt retain their own decisions and manual positions. The stable budget geometry after selecting dice prevents moving the first click target. The actual results and environmental limits of acceptance are stored in UI-77.

<a id="korekty-krawędzi-i-odrzutu--ui-81ui-82"></a>
## Edge and discard corrections — UI-81/UI-82

With constant HUD navigation remains available in the external 4 physical pixels of each edge. Interiors of the controller and sliding windows and modal Menu/Settings block movement under each other. The cursor may remain motionless; moving figure They don't stop the cameras, click, capture and focus loss end up scrolling, and the cursor's return can resume them.

The first click of the item is not a part of this pair. The first click only shows the information; the second click ends one discard. The slight mouse movement is tolerated, drag cancels the two-click. The change of decision or removal of the card does not transform the third click into the selection of the next card. The header, Pin, Close and Play preview retain their own features. The reproduction results and acceptance are in UI-81/UI-82..

<a id="zamykanie-ręki-podczas-odrzutu--ui-83"></a>
## Closing your hand during discard — UI-83

The new decision to discard opens the Cards once. Close and Esc closes their hand also with excess; Discard N remains on the right tab and reopens the hand by clicking. Refreshing the same decision does not open it again. The next decision, for example, after rejecting the first card, opens the hand with the current excess. Separately pinned preview card retains its condition.

The two-click thumbnail or preview content still rejects exactly one card. If the old server requires a bulk discard, the tray explains the need for an upgrade of the server. The gesture does not change the selection, does not send an illegal command and does not add a Confirm. Build Unity does not update the backend.

<a id="bezpośrednie-dotknięcie-brzegu--ui-84"></a>
## Direct touch of the shore — UI-84

Scrolling involves the last pixel and the exact limit of the game surface, also when Unity does not find a to-use item for hower. Getting the mouse to the edge does not stop the motion and does not require the cursor to be withdrawn. At the corner, the movement continues in the available axis, and when the map borders are reached, it stops. The inactive client and cursor outside the window do not scroll the map; windows, menus and active drags still have priority.

<a id="panele-przy-krawędziach--ui-85"></a>
## Edge panels — UI-85

The final reference of the layout is the screenshot user with 2026-10-09 17:11:43: left panel Politics/Effects/Characters and a single panel at it, full Fellowship over Hunt in the right column, Cards at the bottom of this column. Opening the left tab closes the previous one also after pinned. Fellowship and Hunt are constantly open; inspections, Esc and reset positions do not remove or move them. The decision state still present the current controls from the safe projection. Victory opens in Menu; the final result still has its own visible message. card have two decks over the miniature grid; they retain full preview, Play, draw and two-clip recoil.

With a short screen, the layout reduces the spacing, track height and portraits. Inaccessible shortcuts Move/Hide and redundant general text give way to the locations of the map; full explanations remain in help and inspections. This arrangement replaces the free windows of these panels in U14 and the horizontal pair of UI-75.

UI-85: the right column reaches the physical bottom of the screen, and HUD dice reserves its width. The extended Hunt response expands the column; in the absence of height the cards net extends to the left of Cards, above HUD. Geometry changes the presentation, without closing the hand and without changing the draft.

<a id="ilustrowany-górny-pasek--ui-86"></a>
## Illustrated upper bar — UI-86

The reference is a screenshot user with 2026-10-09 17:51:21, subject to its being thinner: 56 pts instead of the previous 60. Dark lacquered plate, gold bottom rail and filigree corners combine a strip with a parchment map; gentle shade falls on the map under docked panels. From left: phase turn, separator, signature of the opponent's side and its dice as gold medallions (special as framed tiles with a designation of origin), to the right of the opponent's counters Cards/Rings/Spent/Hunt/Out with icons, connection status and decorative menu. The title title has disappeared from the strip.

The counters show the same public values as the former text line, also Out. Chip has tooltip with a page and description (e.g. "Shadow · Dice in the Hunt Box: 2 ), is not a button and does not open panels. dice the opponent retains the previous clicks, tooltips and states choice/target/candidate as a coloured ring around the medallion; the hover brightens the frame. Below 1340 pt width (at 125% on screens 16:9 ) the signatures of the counters and the signatures of the pages are given to icons, values and tooltipom, and medallions have 28 The Ornaments and Shadows do not capture the indicator or edge master. From UI-98 The belt has 44 pt, meters are an icon with a number and Hunt counter went to the bookmark Fellowship and Hunt in the top right corner of the map.

<a id="lewa-listwa-i-górna-oprawa--ui-87"></a>
## Left strip and upper frame — UI-87

Reference: screenshot user with 2026-10-09 19:40:11. The strap has a gold frame also at the top, mirrored to the bottom, with hardware in the corners; the content remains centered outside pass edge pan. The Politics/Effects/ Characters tabs are framed buttons with an icon (corona, star, team) over the label; the selected tab has red paint, not only the border. The panels at the slat have a gold frame, a red header with panel icon and small square buttons. We keep UI-85: one open tab, without accordion of all three panels visible on the reference.

Politics: lines in the colors of the page (Free Peoples blue, Shadow red), the coat of arms of the nation in the field of name and token as a medallion of the page with the ⁇ /● activity glyph. Column names and alignment of the Peace/War axis do not change. The badge of the effect range temporarily covers the coat of arms to not obscure the name. Effects and Characters: entries in the frame of the page enamel with the variant of the hiver; selection, legal purpose, inspection, Play and removal work so far.

<a id="przydział-polowania-jednym-zatwierdzeniem--ui-88"></a>
## Allocation Hunt one approval — UI-88

Notification 2026-10-10: Player Shadow selects several dice and approve them together, instead of repeating them die   → Hunt. Click Enter/Space) legal dice in the pool it adds it immediately to the set: die In the pool extinguishs, and in the Hunt window appears her Eye. Click again in the pool or click the Eye withdraws it, and drop on the Eye continues to add. With the lime remaining dice lose the backlight, click does not change anything, and the counter shows “Limit reached”. One Allocate & roll sends the whole set; legal zero still requires it ✓ The eye is a set header and drop target, not a button: its click does not approve anything, so the old habit will not perform the throw by accident.

<a id="podokna-zamykane-z-rodzicem--ui-89"></a>
## Doors closed with parent — UI-89

Notification 2026-10-10 : closing the window also closes the window open from it. Preview card open from hand (from UI-94 the effect entry does not open preview), report battle open with log entry and help opened with “?” panel, preview card or “Why?” card characters close when the window in which the button was used is missing. It does not matter if the window was closed by ×, reclicked on the tab, switched on the tab, Esc or another panel opened. The closing works cascading: window → card → help. Open window with map, trays, top bar, map buttons, pool dice or Menu has no parent and stays. Help pinned The reopening from another location moves the connection.

<a id="ilustrowane-fellowship-i-hunt--ui-90"></a>
## Illustrated Fellowship and Hunt — UI-90

The 2026-10-10 application: right column in the style of the left strip, with the primary rule of "as little useless place as possible". Fellowship and Hunt have a golden frame, a red header with an icon (One Ring, Eye) and square buttons. The header has a 28 pt (26 in compact circuit), less than 32 pt on the left, because the column is fixed. The titles are versed as on the left.

Fellowship: state (Hidden/Revealed) and last known place are in the header as a button that centers the map (in Mordor the Mordor the Mordor track). It replaces a separate state line and the "Locate" button, doubled by map buttons. The poem "Guide · ..." disappears when the composition shows the portrait of Guide with its name and ability in a balloon; without such a portrait (e.g. Gollum) stays. The portraits are in one row and if necessary they narrow down.

Hunt: without Hunt and report number of dice in Hunt is in the header and the empty row disappears; the panel is then the header itself. Allocation, throw, tile, reactions and report have the content under the header so far. As measured in four layouts of both sides: Fellowship lower by 45–99, empty Hunt by 26–69 entitled From UI-98 both panels are not in the column: they develop from a small tab in the top right corner of the map above the hand, and the empty Hunt shows the contents of Hunt Box.

<a id="karty-na-pergaminie--ui-91"></a>
## card on parchment — UI-91

Notification 2026-10-10 . card in their hands lie on a yellowish parchment with ink frame: an icon of the type of event in the ink and title; playable card has a gold border. The header “Cards N” and “ ⁇ ” has been removed; the hand closes the right column permanently (Esc does not close it), and the matching decks are leather reverses with a number. UI-124 card selected die can play, has green glow in the color of legal goals, and the others card extinguish to 50% opacity (developed) card read in full cover). Without the selected dice No distinction; gold remains for the armed card ( [ARMY_MAP_UX](ARMY_MAP_UX.md) ).

Hover or focus develops card up to the full width of the hand in approx. 140 ms: title, type, full text of the event, combat section (initiative, title, text) and footer with hint. The card bottom is in place, card is in the upper position, card, no change the position. As long as the cursor is over the developed card, also over the covered card, it remains developed and click on any of its locations it is the one that applies to it. After driving the card cursor it is rolled; visible card under the cursor develops in its place. Limited traffic disables animation. This is a user decision amending the UI-80 rule for the cards preview: the development serves reading and does not perform any action.

General rule: the first click, after which the next click is final, must show the state of arms. In hand: the first click of the gameplay card (playing) or card to discard it is armed (golden glow, wax seal, footer “Click again to play/discard ) The second click, fast or slow, does the same as the two click. Click beyond card Esc, change dice or decision or weaponisation of another card Disarms her without command Choice card as cost ( DiscardProtection ) remains a simple selection. Click no longer opens a floating preview, unless card you can pay in two ways: then the preview shows both Play buttons, and the second click card He's using the first.

From UI-194 (user decision with 2026-10-16) card do not arm: one click card, which can be paid in one way, immediately plays it. Pressing the mouse button shows `pressed` lica, and the play follows the release over card (UI-113). card with two ways of paying it opens the preview with the Play button for each way and chooses nothing. There is no wax seal or a footer "Click to play" or "Click to choose how to pay".

<a id="podgląd-lewej-listwy--ui-92"></a>
## Left strip preview — UI-92

Notification 2026-10-10 with the user's choice "View on the how card “ Hover or focus on the Politics, Effects or Characters tab develops its panel from the bookmark in approx. 140 ms as read-only preview: transparent curtain takes every push, and the sign “Click to keep open” says the first click clips the panel. The cursor over the panel keeps the preview; the exit from the tab and panel rolls it up after a short tail and restores the panel pinned before. The tab or preview click attaches the panel; then it works so far and stays after the cursor is moved. Each left panel opens with the same animation; the reduced motion turns it off, drags it off. This changes the rule UI-80 for previewing the panels from the user's decision; the action still requires clicking. UI-98 removes the curtain, inscription and fastening: the developed panel works immediately, and click on it does not pin anything.

<a id="efekty-na-rozwiniętej-karcie--ui-93"></a>
## Effects on the developed card — UI-93

2026-10-11: when card is developed, all its graphic effects are related to the developed face. Focus circuit (e.g. after first clicking at the rejection) surrounds the lico and follows development and roll-down; the light, seal and tags were already on the lico, and the miniature does not draw its own states (hover, push, focus, selection). The lico coils immediately stop capturing the indicator, so it does not obscure the undisclosed card.

<a id="treść-karty-w-panelu-effects--ui-94"></a>
## Content of card in the Effects Panel — UI-94

Notification 2026-10-11 with screen: all information from a separate window card display at the bottom of the Effects panel. Source click- card selects them, and under the ranges and term appears a small parchment as developed card in the hands: the icon of the type of event, type (e.g. " STRATEGY CARD · MUSTER EVENT ), full text of the event and the fight section (initiative, title, text). The title is once, in the heading of the detail; card (Free Peoples · In play or "Effect ended", then parchment is extinguished) replaces the window footer. The Effects entry no longer opens a floating preview card ; the capacity of the form and source without card They don't have parchment. card range does not repeat the shortcut of effect (says it card ) Location buttons and expiration date are in one row. When text would push the panel under the bottom pass (e.g. 768 px at 125% ), panel expands like an old window card at most to 55% width board , and the sources entries have one line; the new source or change of window returns to the width of the bar. When paying for the removal of parchment is hidden, because the place is the decision. The content is located above the bottom pass in four systems without changing.

<a id="płynny-ruch-drużyny--ui-95"></a>
## Liquid movement Fellowship — UI-95

Notification 2026-10-11 : “The ring team does not have smooth animations of movement”. Fellowship is hidden, so the visible motion is the progress mark on the track: it moves smoothly now (approx. 260 ms) instead of jumping, same tag Corruption Declaration or disclosure shall be carried out figure Fellowship by a flight that is not interrupted by another motionless projection figures Reduced motion disables both animations; the hidden route is still not shown.

<a id="panel-kości-w-stylu-planszy--ui-97"></a>
## dice-style panel board — UI-97-style panel

Notification 2026-10-11 with screen of the bottom panel dice The panel has a golden frame and pass The bottom strip was given a top strip plate. dice are medallions in the enamel of the page ( FP blue, Shadow red) with a clear wall symbol; marked die has a brighter gild and brighter, and the states of purpose, cost and Hunt The border. dice the opponent in the top bar uses the same medallions and symbols. Spent/Hunt/ Out is an icon with a number (name in the description), Ring of elves — icons. Description dice is short: the name of the wall, 1–4 options with BASE p. 20–21 , ‘Now’ with legal uses at the moment and one line of state; without identifier dice , paragraphs and references to instructions. Full conditions remain under “Why?”.

<a id="drużyna-i-polowanie-w-rogu-mapy--ui-98"></a>
## Fellowship and Hunt at the map corner — UI-98

The 2026-10-11 application: the status and place of the Fellowship and the "Hunt: [number of Eyes]" are to be continuously visible, and the rest of the information develops after invading like a left bookmark; from all places, "Click to keep open"; the bar is to be thinner, graphics are still nice. Clarification of the same day: instead of the tablets in the bar, there is a small basic tab Fellowship and Hunt, which obscures the empty top right corner of the map and on the hover it develops to the full current form.

The corner is under the belt at the right edge, with a golden frame of panels, wide as a column of the hand and high on 46 "To the left: a painted state symbol (a hood with a ring for a hidden, glowing Ring for the revealed), the last known place above the inscription Hidden/Revealed and feet of Progress and cracked Ring of Corruption; on the Mordor track, the gate and `n/5` . Right: icon Hunt , Eye medallion in enamel Shadow with number dice Shadow in Hunt Box and the inscription Hunt; dice Free Peoples traffic Fellowship They have a foot with a number next to it, only when they're in Hunt Box.

The keyboard's technique or focus develops the full format: panel Fellowship over panel Hunt , as long as the column has stood, covering the tab and lying over the hand that it does not move; the panels work immediately. Drives off to roll them up after approx. 140 ms; click on the button does not pin anything. Panel leading the current decision (phase and route Fellowship , Guide , separation, selected move Fellowship , allocation and Hunt is kept by both players and there is no ×; Esc then goes to cancel the selection, and after the decision the panel wraps itself up. The held or open with the code the panels occupy the column as before, so the full hand moves left; the preview from the invasion only temporarily lies above it. Opening from the code (marker Fellowship on the map, Location, victory screen) leaves the panel with ×; Esc closes. Fellowship has a green border when chosen die allows movement Fellowship . Empty panel Hunt shows “Hunt Box”: Eyes and dice Free Peoples , under it last report. Window opened from inside the panel (help, preview card , the character inspector) holds the panel while it is visible.

Esc does not re-expend the tab by refocused or unveiling it under the still cursor. Fresh movement, click and new focus keyboards continue to expand the panel, also when crossing between the plates in the corner.

The drag started inside the preview keeps its source and targets to be completed or canceled. The cursor itself does not wrap the panel; after release, normal hover and focus conditions return.

The left panel loses the curtain and the fastening: the developed panel works immediately, click on the tab only develops it, the focus on the click does not hold the panel, and the focus on the keyboard also keeps it inside. The panel to which the decision tray will be placed, is as open as context. This extension of the change UI-92 rules UI-80 from the user's decision: the edge tab panels are interactive after the raid; other panels, card and options still open click or Enter/Space.

The bar has 44 pt instead of 56 (new 44 pt frame) and is a border control: edge pan works only on the outer edge, so reading the bar does not move the map. Counter counters are an icon with a number (a name in a balloon), without a Hunt counter; Menu has 64 pt (56 in a narrow bar). pass notifications end at column, like the bottom bar, next to a tab in the corner.

<a id="powiadomienia-jako-stos-banerów--ui-99"></a>
## Notifications as a stack of banners — UI-99

2026-10-11: notifications are shaped like small banners, from which stacks are created when there are more than one; click stack reads getting older; click new closes old; open notification disappears; the window with content is to fit into parchment style.

Unread action The opponent is one stack in the notification bar: at the front of the newest banner (a ribbon with a swallow tail in the opponent's hand enamel, a gold lamb, text in one row), behind it tails to two older banners, and at the wrapped end a wax seal with a number when notifications are at least two. Click (or Enter/Space) opens card the latest and immediately removes its banner, so the next click reads another, older. Opening another notification replaces card the previous one. A step that comes later in the same action will come back as a new banner. Reading is local: not sending command and does not change history; the stake smoke shows full text and number of older. The overflow list and the button “+N” disappear. The notification bar does not have its own CD, so the map sweeps between its elements; the decision rate and reconnection becomes.

card action (from notifications and history) lies on parchment cards hand, with lattice panel header and ×: title and content in ink, status in sepia, headstones in red, ink cost symbols, breakers in sepia. Height is due to content, at most to date 460 The long-term steps have been revised so far.

<a id="ikonki-powiadomień-karta-na-środku-bez-rzędu-kontrolek--ui-100"></a>
## Notification icons, card in the middle, without the control order — UI-100

2026-10-12: The message window appeared once to the right, once to the left; it is always to stand in the middle of the screen. Bookmarks (notice banners) are to look like small icons, not like long ribbons. The `+`/`−`/... icon row is gone. Log opens a small icon at the stack (user selection).

- [Man] Oh, my God.
- Each unread notification is a small flag on a gold rod (22 × 28 pt) in the enamel of the
  opponent's side. Behind it look up to two older flags, and the wax seal shows the number.
- The stack occupies about 42 entitled Endorsitis does not have a string: short tooltip (UI-80)
  gives the text of the latest action and the number of older ones.
- Reading works like in UI-99: click opens the latest, icon disappears, new card replaces the
  previous.

**card action.** It always opens in the middle of board, regardless of the type of action and whatever opened it (stash, Log, context).
- It does not cover the top bar or notification bar.
- The drag moves only the current card; the next one starts in the middle again.
- The highlight of the context object is left. The map captures the action regions in the largest
  free pass around card (on the side, above or below it) which houses them all. The open Log list
  cuts pass, which blocks.

The government of the map controls is disappearing:
- zoom stays on the mouse wheel;
- the Fellowship position shows the row in the Fellowship panel;
- Help was in Menu and contextual “?” (removed in UI-173).

In the notification bar, in front of the stack, there are two small square icons:
- the scroll opens and closes Log, all the time during the batch;
- The sword with the shield opens the report of the final round of fighting when it is (from UI-112
  only to read it).

**Reading the region by right click.**Inspect switch replaces right click without dragging on the map.
- Opens a region information window (forces, characters, state) and on an army tag about its region.
- draft, dice, selected targets and camera remain unchanged.
- Right-click drag continues to move the map; the tolerance is 6 pt.
- Back to decision through decision indicator closes windows only for reading, like the old output
  from Inspection.

<a id="władcy-w-zakładce-characters--ui-101"></a>
## Overlords in Characters tab — UI-101

2026-10-12: “We remove buttons to hang in the middle of the map (theoden, denethor, thranduil)”. User selection: We open the rulers from the section on the Charters page.

- There are no more rulers' buttons over the map.
- The Characters panel has a “Sovereigns” section under the lists of characters with the same
  buttons: name, state, tooltip, mark and mark of the corrupt ruler. The buttons stand in one
  compact row. Under Fate there are three, with expansions Kings without Fate five, without
  expansions section is not there.
- Awakened ruler whose player's list already shows, has only this entry (opens the same inspector),
  so the full lineup is located in 1366 × 768. Awakened ruler of the other side stays in the
  section. Section without buttons disappears.
- The button click has been working so far: the legal gesture takes precedence, or it opens the
  master window (or the inspector awakened).
- The panel is developed until the window opened from it the ruler is visible.
- The master window stands next to the button. When the panel is rolled up, for example, the
  decision opened the window itself, it stands next to the Characters tab.
- When the ruler is a legitimate goal (e.g. waking up after the dice selection), the Characters tab
  has an award for the target also in a rolled-up state.
- The context of the saved action ruler and Esc indicate the Characters tab.

<a id="powiadomienia-bez-pasa-ponowny-klik-regionu--ui-103"></a>
## Notifications without pass, reclick of region — UI-103

2026-10-12 applications: ‘to eliminate the visual representation of this strip under the bar [...], the notifications are to be hung under the top bar themselves’ and ‘to disable the tabs of the reclick on the region’.

**Notifications.**
- There is no longer pass or shadow line under the top stripe over the width of the map.
- The decision indicator, Log icons and the final fight, the stack of flags and re-hanging
  themselves just under the stripe, above the map.
- Next to them, the map is visible and reacts to the indicator.
- Shadow strap falls on the map directly below it, and edge panels begin from its lower edge.

**Panel region.**
- A click in the region or in the army tag, when its panel is open, closes this panel.
- Click in another region switches the panel. The third click opens again.
- Program opening (e.g. decision indicator) does not switch.
- The legal target has priority so far.

<a id="papier-i-morze-poza-mapą-grafika-wody--ui-102"></a>
## Paper and sea outside the map, water graphics — UI-102

2026-10-12 application: extend the map graphics to the north, south and [east] with paper and west by sea. It is to be scrolled further to click each region. Added graphics are not part of the map. Water tanks in the middle of the map are to have graphics.

The canvas.
- Around the map lies a canvas: paper in the north, east and south, and in the north-east cut, and
  the sea in the west, in the bays and beyond.
- The paper passes without visible suture, and at the edge of the canvas darkens like old parchment.
- The sea, Rhûn, Nurnen and Evendim have lavender, pencil wave lines at the shore and fine waves at
  open water instead of flat color.

**Camera.**
- `Fit` and the first layout show the map so far.
- Moving (right button, edge of screen, history frame and decision) reaches the edge of the canvas,
  so any region at the edge can be moved from under the stripes, tabs, trays and hand.
- The distance below `Fit` shows more canvas. Background behind the map never reveals itself.

**Margin.**It is not a region: it does not highlight, does not open a description, selects the target or accepts drop.

<a id="pergamin-przy-świecy--map-18"></a>
## Candle-lit parchment — MAP-18

The 2026-10-14 application: shape, boundaries, mountains and forests are changed, colors and texture are changed. The map is to resemble old yellow paper, papyrus or parchment read at candlelights.

**Paper.**
- Warmer and more saturated ochre tone.
- Irregular discolorations, grain and short fibers.
- Foxing stains and a few dried up leaks with a darker edge.
- Candlelight: heat in the middle of board, darker brown in the corners of the canvas.
- Everything goes seamless through the map and the margins.
- The paper bend is gone. A straight line through board could pretend to be border or road.

*The drawing does not change. The colour is placed on it like watercolor lawing:
- The Dark Forest is dark green;
- Fangorn, Lothlórien, Old Forest and Lindon Coast trees are brighter green;
- the mountains have a clear grey line;
- Mordor (equals in the ring of mountains and mountains themselves) is ashtrayed and clearly darker,
  darkest in the middle.

Water, regions, Mordor track and markers remain unchanged. Export details: [MAP_CONTOURS_UX](MAP_CONTOURS_UX.md#kolory-i-faktura-płótna--map-18).

**Paper as parchment (MAP-19, user revision).**
- The paper is clearly saturated, gold-ochre.
- It has cloudy cream and amber lobes, soft skin bands, fine spots and stronger grain.
- Forests, sea, Mordor and mountains look like MAP-18.
- Details: [MAP_CONTOURS_UX](MAP_CONTOURS_UX.md#skóra-pergaminu--map-19).

<a id="figurki-narodów--art-01"></a>
## Figures of Nations — ART-01

The application 2026-10-15: figures of higher quality and more distinctive, so that it is immediately known which unit is regular and which is elite. A design from the original game, e.g. the Haradu elite is Mûmak.

- Each nation of base game has its own Regular, Elite and (Free Peoples) Leader figures in the
  colors of its pass nation. Shadow also has Nazgûla.
- The elite is larger and heavier either horse or beast:
  - knight of Gondor and Horseman of Rohan horse;
  - Gold guard of elves and dwarf in full board;
  - Mordor's troll, Isengard's lip rider and Mûmak Haradu.
- Regular stands below the elite also when both have the same frame.
- The army shows the figures of the nation, which has the most figures of a given kind. The region
  panel, battle, losses and meals show the nation of each figure.
- Origin, Prompty and Atlas: [graphicalassets/figures](../../graphicalassets/figures/README.md).

<a id="figurki-i-portrety-postaci--art-02"></a>
## Character figures and portraits — ART-02

The 2026-10-15 application: each character has its own, nice graphics matching the style of the game, very loosely inspired by films.

- 14 character base game has its own figure in the style of national figures:
  - Fellowship, Gollum, Gandalf White and Aragorn;
  - Saruman, Sauron's mouth, and the Wizard;
  - Fellowship tag is Ring-bearers.
- Hobbits, Gimli, and Gollum are lower than humans and wizards.
- The battle medal shows a painted portrait of the bust at the candle. The designs are original, not
  like the actors.
- The expansions characters retain their existing portraits and silhouettes.

<a id="bez-granicy-mapy--ui-104"></a>
## Without map boundary — UI-104

2026-10-12: "Let's remove this brown border separating the real map from the rest of the graphics".

- The board edge (north, east, south) and the north-east cut arch no longer have lines: map paper
  turns to canvas paper, and white transitions between regions end at the edge.
- The sea shore, lakes, rivers, barriers and passageways are so far drawn.

<a id="ruch-wyłącznie-na-mapie--ui-105"></a>
## Movement exclusively on the map — UI-105

The 2026-10-12 application: "Moving is only to be done on the map, click on the region, select selected units, characters, nudges etc. and select only by clicking the selected region, move is done immediately". Elections from the conversation: die first; the character's route with a range greater than 1 arranges the game.

Flow. die   → click region → click figures in the region panel → click the highlighted target. The move is executed immediately, without Confirm. Another army or group in the budget dice begins the same way; ✓ with dice He's done early.

**Armies.**
- figure Army select only in the region panel. The two-click banner continues to mark the entire
  available stack, and click the legal enemy region begins the attack.
- Nazgûl, Minion or commander elected before a soldier in this region joins the army when a soldier
  arrives; he does not lose the mark (user notification from 2026-10-12). Nazgûl himself at dice
  Character flies separately.
- With dice tray shows only a hint. A list of movements and a figures grid in tray remain only for
  movements of several armies with cards, which have one common approval.

"The Men and Nazgûle."
- The map highlightes every target available to the selected group.
- The target click sets the shortest legal route. The client counts it from the server data:
  neighborhood, range, regions of detention and blocked; flight is a start and a goal.
- The length draw settles the order of identifier Neighbours, because the player's choice is only
  the goal.
- The start region click means to stay. The usual journey and separation send command immediately.
- Travel of several groups with card the group's objective shall be recorded by one instruction and
  shall retain joint approval.
- Undo step is only left at the declaration and disclosure route Fellowship. Cancel local separation
  draft.

No gestures.
- Drag of Army, figures, Charters list lines and Fellowship portraits does not capture anything and
  is not a move.
- The list of Characters only shows the inspector. It replaces the UI-67 paragraph with a joint list
  and map selection.
- Drag remains for reserves (Muster), tokens of nations, cards and portraits of homelands on the
  deployment.

**The target click on the open panel.** The landslide closes the region panel, so the tags are re-stitched. The release over the tag of the same region still selects the target; the panel above the map is not the target.

The entry and movement in battle (entering, retreating) retain the target click.

<a id="kolejność-lewej-listwy--ui-107"></a>
## Left strip order — UI-107

2026-10-12 application: "change tab order to the left to the policy, character and only then effects". From top of the rail: Politics, Characters (at the time of the deployment also Setup), Effects. Panels, development after invading and decisions in the panels unchanged. Replaces the order Politics/Effects/Characters with UI-85, UI-87 and UI-92.

<a id="wejście-postaci-dwuklikiem-w-characters--ui-108"></a>
## Double-click entry in Characters — UI-108

2026-10-12: “When you choose die, you click on the character twice and it introduces it to board, you don’t need a new window with a separate button”.

**Gest.**
- After selecting the dice (or Council token) the character lines that this cost can introduce are
  distinguished (`legal-target`, `entry-target`).
- Two-click line or Enter/Space by seal selects input:
  - at one place command goes immediately;
  - on several maps, it highlightes regions, and a click of the region introduces a character.
- A single click of such a line does not open the inspector window. Outside the input context, click
  still shows card characters.

No buttons.
- Inspector, government action in tray (`board.Components`) and board of Council tokens do not have
  an entry button.
- Exception: a character with several possible inputs for the same cost. Such a poem opens the
  inspector with the selection buttons.
- The prepared entrance is in the usual place of the tray: the character name, cost and "Choose a
  highlighted region". It does not enter the character window.

This replaces UI-67 in the entry part. Active character capabilities and recruitment of rulers remain in the inspector.

<a id="zwarty-panel-polowania--ui-109"></a>
## Hunt — UI-109

The 2026-10-12 application with the "LAST HUNT" drop: the panel is to show what matters now, mainly the number of dice in the Hunt field. The larger one is to be only with action Hunt.

**Retired.** Hunt does not last: except for the allocation, without the current Hunt and with the completed report.
- Under the heading ‘HUNT’ there is one row of the field Hunt: Eyes Shadow and dice Free Peoples,
  the latter only when they are.
- Without the last Hunt report bar, dice with hits, pool, warp and metastatic sources. The report
  remains in history and in card action.

**action Hunt.**dice allocation, throw, tile, response and disclosure show the full panel with the controls to date.

<a id="ruch-drużyny-ikoną-toru--ui-110"></a>
## Move Fellowship track icon — UI-110

The 2026-10-12 application: “Do not add a new move button when moving the ring team on track, give the option to click on the icon next to the track and graphically mark that it is clickable, let it shine nicely”.

- There is no “Move →” or “Attempt move →” button.
- The Fellowship movement is a click (or Enter/Space at the seal) of the icons at the beginning of
  the visible track: progress outside Mordor, Mordor track in Mordor.
- The icon shines with a golden glow (`ornaments/move-glow`) only when the selected die allows
  movement. When the motion is activated, the glow pulsates gently; the reduced motion leaves a
  constant glow.
- Tooltip gives the effect, and the hint of the panel indicates “Click the shining track symbol”.
- Without legal movement, the icon is a simple, non-clip symbol. Hiding is the Ring icon (UI-232), the
  declaration the Fellowship's map figure and entry to Mordor the track's gate (UI-236), and the Guide its glowing
  portrait (UI-249); none of them is a panel button.

<a id="proste-powiadomienia-i-pergaminowe-karty--ui-106"></a>
## Simple notifications and parchment card — UI-106

2026-10-12: "Quick notifications...more parchment... no need for sources, or the cube used, information about free peoples in three different places".

** Tooltip flags are one sentence about what the opponent did, without the name of the page, because the enamel of the flag already shows it. There is no list of facts, number of steps or "Click to Read". From UI-166 tooltip is the header of the result and its first line (e.g. "Army move · Osgiliath" / "3 Regular · Minas Tirith → Osgiliaath").

**Action Tab.**
- The page appears once, in the header card.
- Status only if action continues or history is incomplete.
- Steps show effects without numbers, page names and command.
- They're gone:
  - row dice and costs;
  - sources;
  - changes to dice;
  - the remaining budget;
  - system steps without effect.
- dice assigned to the Hunt field is the result (‘Hunt Box +N’).
- Played card has a parchment insert with the title and text of the part used: event or fight.
- action shows its saved text without any recognized effect.
- Full record (sources, dice) stays in the history list.
- From UI-166 card opened from the banner shows the result in the highest 3 lines instead of the
  list of steps, and played card as a line opening its preview instead of a parchment insert.
  Inserts, steps and archives battle and Hunt remain in full writing: in Log and after clicking
  "Full record" ([SIGNALS_UX](SIGNALS_UX.md), "implementation UI-166").

Open card of the hand.** Same parchment as card hand: dark ink, red headers Event and Combat, square header buttons. No sources; decision context with one row.

<a id="niedostępne-karty-walki-z-warunkiem--ui-111"></a>
## Inaccessible card combating condition — UI-111

The 2026-10-12 report: "In battle I could only choose one card with 5 that I have." The choice was in accordance with the rules: each part of the fight has a condition, and the server only allows card that meets it.

The card Choice Window.
- card are allowed at the beginning and they work so far.
- Others card The hands are behind them, grey and inconsistent, with the condition of the text of
  part of the fight, e.g. "Requires a Shadow Elite in the battle." The two-click or dropping them
  does not play.

**Hand.** When selecting card card combats, the hint is not available and the same condition is given. Client does not evaluate the rules: takes the list of accepted cards from the server and the condition from the text card.

<a id="ikona-ostatniej-walki--ui-112"></a>
## Last fight icon — UI-112

The 2026-10-13 notification: ‘combat report is to disappear after opening it’. It is a sword with a shield on the notification bar that from the first fight stood there until the end of the party as unread notification.

- The icon means unread battle round. Click or Enter/Space opens the report so far and the icon
  disappears immediately.
- Report of the same round opened from history ( card action fighting, archive battle He's hiding it
  too.
- The next finished round or the new battle shows the icon again.
- The read round does not return after the game is restarted; the tag is local, as with flags.
- The report of the final fight is available from card action of the fight in Log.
- After the focus icon opens, it goes to the report window, so Esc continues to close it, although
  the icon under the cursor disappeared.

<a id="menu-wejściowe--menu-01menu-08"></a>
## Input menu — MENU-01-MENU-08

[MAIN_MENU_UX](MAIN_MENU_UX.md) replaces the old logflow and Tables. From AUTH-03, the login screen only log in (with the account name since AUTH-05); the account and password reset open in the browser on the game page. board and the safe return of UI-70 retain their mechanisms.

<a id="scenariusze-testowe-na-planszy--test-03"></a>
## Test scenarios for board — TEST-03

The test party has an amber bar “TEST · < script title>” under notifications, centered over the map next to the right column. Tooltip title gives a description of the script. The bar has three buttons:
- **Play Shadow / Play Free Peoples** switches view and command to the other side of the same party
  (hot-seat). action pages are changed, so the switch is always visible and works with one click.
- **Restart** launches the script again, in the same situation, on the side that starts.
- **Exit** completes the test without writing and returns to the menu; the same does Menu → Games.

The test party does not save the draft locally, recovery or read tags. The game is played normally: the same rules, windows and history.

<a id="ilustrowane-okno-bitwy-i-stan-naciśnięcia--ui-113"></a>
## Illustrated battle window and press state — UI-113

2026-10-13: "Now everything is very tabular, there is no place for nice graphics" and "I want to work on the response". [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md) saves the agreed project:

- General rule: Clicking graphics suggests clickability, PointerDown shows the status of `pressed`,
  action enters after release inside the item; cursor and drag outputs remove the status without
  action (UI-114, the whole board);
- window battle in the frame board : pass combat mode scenes, strength framework in the enamel of
  the sides, figure of an atlas, medallions of statistics and portraits, parchment card , dice d6 of
  dice elephant, loss tokens with overlays, decision buttons with icons, report as parchment
  chronicle ( UI-115 – UI-119 );
- hybrid graphics: SVG in `graphicalassets/ui/src/battle`, portraits and backgrounds as generated
  atlases (UI-120) with placeholderes until delivery;
- size similar to the current, decisions without scrulla in four layouts; acceptance agile in UI-121
  Status implementation specify the register.

<a id="stan-naciśnięcia-na-planszy--ui-114"></a>
## Pressing state on board — UI-114

implementation The UI-113 . `PressFeedback` on the interface root (next to `RuntimeTooltip` ) means class `pressed` nearest button or class item enabled `pressable` under the indicator from PointerDown to release; cursor output outside the control, drag the control from `DraftToken` ≥ 6 pt, cancel the indicator and unplug the item off the state without action . Enter/Space hold the condition on the focus control by pressing the key. action still belongs to `clicked` controls which UI Toolkit triggers when released inside her. `Styles/Interaction.uss` The last one is the one that lowers the push-in control by 1 pt, dims the paint and frame, and for the frame with graphics replaces variants `pressed` ( `frames/button` , `button-square` , `dice/medallion` in the enamel of both sides; card He's pushing a lico of parchment with tint. 70 ms only at `motion-enabled` The map area under the pressed indicator darkens ( `MapRegionEmphasis.Pressed` ) to be released; shift by ≥ 6 You can cancel pressing without choosing the region.

<a id="oprawa-okna-bitwy-i-ilustrowany-układ-sił--ui-115"></a>
## battle window mounting and illustrated power arrangement — UI-115

Window battle uses frame `frames/panel` , header in red lacquer (region · mode · round, square buttons) and pass combat mode scenes ( `battle/scene-field|siege|sortie|relief` ) with the coats of arms of the sides and the current decision in the middle. Each force stands in the frame of the enamel of its side ( `force-frame-free|shadow` ): header ATTACKER/DEFENDER , medallions of named characters (atlas silkworm figures and initials until portraits UI-120 ; red ring at the abolished ability) with name line, one figure from atlas to nation and type with the number ‘×N’ behind the coat of arms of the nation, four medallions of statistics (power, Leadership , dice , threshold) with server values and description of the modifiers in the tooltip, card in a straw, dice , and under them the shelf “not fighting” only when the garrison or rear guard exist. The ribbon of stages has icons and golden tag of the current stage. Rules battle moved to `Styles/Battle.uss` with one window width ( 760 pt, loss 820 pt). dice loss, hand cards Fights and report keep appearance UI-45/UI-46 ed UI-116 – UI-119 Gestures, command and projections unchanged.

<a id="karty-walki-w-ilustrowanym-oknie-bitwy--ui-116"></a>
## card battles in illustrated window battle — UI-116

The hand of the fight in the window is parchment tiles ( `cards/parchment` , variants `playable` and `armed` Like in the hand UI-91 ): waist symbol in ink, initiative and title of part of the fight, combat text, wax seal after weaponry and one line of clues. First click opens full preview and arms the tile (state `combat-armed` ), second click game card covered in the same path as a two-click and a drop ( `CardFinalized` → `CardContextInteraction.Play` ); the reinforcement of another tile carries the seal, the new projection deletes it. card without the condition fulfilled ( UI-111 ) stand behind accessible, extinguished, with the condition instead of text and without token. card pages: after commitment and before disclosure, the leather reverse ( `cards/back` , ‘Face down’; after the title and initiative parchment has been revealed, readable. ‘No card’, ‘Stay’ and other direct elections have a framework `frames/button` ; No card and “Skip cards this battle” carry icons ( `battle/no-card` , `battle/skip-cards` Legality still comes from the list of permitted cards server; no command changes.

From UI-137 the tile overlay shows the whole card (initiative, title and full combat text, name card) on the parchment with the glow, just as the hand develops card at the invasion. The preview does not take the cursor, changes the choice and disappears at the exit or pressing; the battle window has 12 space from the sides and bottom, so the gold frames corners do not collide with the content.

<a id="ilustrowane-kości-walki--ui-117"></a>
## Illustrated dice combat — UI-117

dice Fight is d6 with dice ivory with ink eyes (`battle/die-1`  …  `die-6`).  `DiePips` in battle is illustrated: it carries a class of walls `die-pips-N`, and states choose a variant of graphics: hit — amber glow and gold border (`hit`), first flip after roll — extinguished, smaller ankle (`rerolled`) with a gold arrow to the result of reroll, die marked for reroll — gold ring (`die-selected`, pressed — darker (`die-pressed`,  UI-114 Which of the two results counts as a hit, says the server (`Hit`,  `RerollHit`,  `ConvertedHit`; the client does not count the thresholds. Result sign (✓ / × /↗ ✓), signed modifier and tooltip `BattleRollText` No change. dice   Hunt keep drawn meshes. Button dice is clear, 52×40 pt, select by pressing and releasing; the special attack page has a square button; Keep roll / Reroll selected stand in the frame `frames/button` in a footer.

<a id="ilustrowany-zestaw-strat--ui-118"></a>
## Illustrated loss set — UI-118

figure to loss is tokens from the atlas figures ( `FigureArtwork` ) with the coat of arms of the nation in the corner. Click or press and release ( UI-114 ) cyclically selects keep → downgrade → remove (regular: keep ↔ remove) by the previous `BattleDraft.CycleLoss` ; token shows the cover `battle/loss-downgrade` (gunshot down) or `battle/loss-remove` (cross) and badge number of glyph operation (“ 1 .↓’, ‘ 2 .×). Above the tokens legends with the cost of hits, wax seals `battle/hit-seal` one to be assigned (flated allocated, `spent` ) and the current budget line “Assigned N / M · Remaining K” with voluntary losses without seals. Preview server (no substitute, excess hits, death of commanders) stays in tooltips and budget line; Apply losses and Keep all stand in the frame of the footer. Losses from events use the same tray and appearance.

<a id="decyzje-końca-bitwy-i-kronika-raportu--ui-119"></a>
## End Decisions of battle and Chronicle of Report — UI-119

Direct Elections of the End battle (Continue battle, Stop battle, Hold position, Retreat, Advance, Fight in field, Retreat inside, Extend Siege, Stay) are buttons in the frame `frames/button` with press and icon state ( `battle/continue` , `stop` , `hold` , `retreat` , `advance` , `extend-siege` ); text remains a control and each icon button has its own width (button with a child item loses text measurement). Reverse still indicates region on the map, entry and cost of extension siege choose figure of an atlas ( `FigureGrid` ), two-click approves a single legal choice.

The battle report is a parchment chronicle: the `frames/panel` frame, the header in red lacquer with region and mode, status and Retry, the navigation of rounds ‹ › square buttons with parchment-only tab `battle/round-tab` (Round 2 · Completed), "No recorded Combat roll", pages on `cards/parchment` with ink text: costs, power layout with UI-115 read only, control and points, steps as red boxes. Versioning, Retry and round selection after reconnect unchanged. "View battle rounds" in the written action card has the same frame and icon.

<a id="portrety-postaci-i-tła-scen--ui-120"></a>
## Portraits of characters and backgrounds of scenes — UI-120

Character Medallion and pass The scenes have optional bitmaps: atlas `Resources/Wotr/Art/battle-portraits-v1.png` ed JSON cell (key = last term) `DefinitionId` , common to base game and Lords) and `Resources/Wotr/Art/battle-scenes/<tryb>.png` ( 168×44 pt, repeated horizontally). `PortraitArtwork` and `BattleSceneArtwork` They load them once; when a file or cell is missing, the medallion retains a figure from the atlas figures and initial, a pass vector scenes pass from the set UI The swap does not change the layout, hitboxes or rules. Propts, cell names and procedure acceptance : [graphicalassets/portraits](../../graphicalassets/portraits/README.md) . Delivered 2026-10-13 as manually drawn vectors drawn to PNG ( 19 medallions in the atlas 1000×800 , four lanes 336×88 ); the propts are replaced for later on with painted images without changing the system.

<a id="odbiór-armii-na-mapie-ruchu-i-posiłków--ui-132"></a>
## acceptance Army on map, movement and meals — UI-132

acceptance Collective UI-124 – UI-131  (AM01 – AM21) according to [ARMY_MAP_UX](ARMY_MAP_UX.md), „ acceptance   UI-132 ”: evidence matrix, regression PlayMode, drops of four layouts of both sides and Player profiles ([map17](profiles/ui132-macos-map17.json),  [board](profiles/ui132-macos-board.json),  [index](profiles/ui132-validation.json) Derogation. UI-130  (settlement outside the targets without separate extinguishing and tooltipu) adopted in accordance with the principle that the map area does not have a tooltipu. Window battle Got the targets. 33  px ([BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md) ‘Objectives 33 px in window battle  —  UI-132 ”).

<a id="konsola-tury-okno-polowania-i-pierścienie--plan-ui-158"></a>
## turn console, Hunt window and Rings — UI-158 plan

User decisions with 2026-10-14: the bottom bar is turn (status, Hunt field, Elf rings of both sides, Pass and rejection dice), Hunt has an illustrated window like battle with auto-open at both players, dice allocation to Hunt takes place in the console, and the Ring uses a gesture ring → die → result without Confirm. Hunt tab disappears from the map corner. The console has constant height and fixed zones; decisions choices take place at objects and those without an object in one decision window over the console. Design, inventory, TC/ER/HW/OB scripts and owners UI-159–UI-164: [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md).

implementation   UI-160: the bottom row has a constant 140  pt (panel  dice   212   ×   132 pt and consoles. The console has a status with hint, resource column (table Hunt over the three nests of the Ring) and the fixed grid of the ⁇ ✓ ⁇ ⁇. Progress action is in the status header, Pass and rejection dice are in the console, and private setting cards   battle is in the window header battle. The decision window has a fixed place above the console, grows up and shows itself only at the election without the object. The status click opens the site of the current decision without moving the camera. Details: [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md), „ implementation   UI-160 ”.

<a id="sygnały-akcji-i-tury-okna-otwierane-same-koniec-gry--plan-ui-165"></a>
## action and turn signals, windows opened alone, game end — UI-165 plan

User decisions with 2026-10-14:
- action the opponent only signals a stack of flags, but its card is compact and describes the
  result (highest 3 lines) and the full record stays in Log;
- the action switch to the player shows the "YOUR ACTION" ribbon and the console's glowing badge;
- only windows in which you need to operate or look (battle and Hunt in both, your own decisions,
  the game end screen) and card action in the opponent never; the camera does not move alone;
- The game's end is a full screen victory for both players.

Design, window table and scenarios SG/AO/GE/TE with owners UI-166 – UI-170 and TEST-07 : [SIGNALS_UX](SIGNALS_UX.md) From UI-166 compact card replaces the list of steps with UI-106 in a card opened with a flag. UI-169 The end of the game is a full screen for both players (title from player perspective, reason, scene, summary, View board buttons, Game log and Main menu); label `victory-reason` And the Victory panel in the Menu stays.

<a id="bez-help--ui-173"></a>
## Without Help — UI-173

User decision from 2026-10-15 : The game has no Help panel or any link to it. The game menu has Resume, Victory, Settings and Games. The buttons “?” have disappeared ( `help-for-*` ) from panels headers, cards , the hand, dice and resources and “Why unavailable?” buttons in the pool action , characters and resources. Not available action sovereign (Kings) shows the inconsistible label "Unavailable", and its reason is in the tooltip. The header of its own pool action ( `dice-header` ) is left without a link. The button “?” in the corner of the map opens the map legend and stays. Earlier descriptions of Help (U15, UI-61 , UI-100 Sections UI-56 – UI-63 ) are historical.

<a id="wyjście-z-gry--menu-14"></a>
## Out of the game — MENU-14

The user's decision with 2026-10-15: the board menu has the “Quit game” item after “Games” with a confirmation question (Quit/Cancel, focus on Cancel; Esc, Cancel and click next to close the question). Confirmed exit in the batch first saves recovery data like “Games”. Menu board: Resume, Victory, Settings, Games, Quit game. Details: [MAIN_MENU_UX](MAIN_MENU_UX.md#wyjście-z-gry--menu-14-2026-10-15).

<a id="tor-polityki-i-zakładka-posiłków--plan-ui-178-ui-179"></a>
## Policy track and meal tab — plan UI-178, UI-179

User decision with 2026-10-15:
- Politics is a track (rail with stations, no buttons frames) with a small right arrow in each row,
  extinguished when the step is impossible, and without an empty column;
- The separate Reinforcements window replaces the left strip tab under Politics. Poems show only
  reserves, without a political state. Recruiting nations are highlighted, the remaining
  inconsistency.

Project and scenarios PR01–PR05: [POLITICS_REINFORCEMENTS_UX](POLITICS_REINFORCEMENTS_UX.md).

<a id="dyskretne-dźwięki--plan-audio-00audio-08"></a>
## Discrete Sounds — AUDIO-00-AUDIO-08 Plan

Plan with 2026-10-15: short effects of paper, wood and metal, signals of the required decision, start of battle and end of game, and steps Fellowship and metallic slide Ring on the track Corruption both ways. The latest decision by the user: a common change of progress and Corruption has two more animations and both sounds instead of the earlier Ring itself. In the background you can only hear the required decision and end of the batch; hower, automatic bookmarks and playback of past events remain silent.

Directory, settings, priorities, track sequence exception, data limits and scenarios AU01 – AU18 : [AUDIO_UX](AUDIO_UX.md) . implementation AUDIO-01 – AUDIO-07 (order from 2026-10-15 ) is completed and the war horn replaced the jaws battle at the request of the user. acceptance AUDIO-08 has an automatic proof matrix in AUDIO_UX . Open: re-hearsing of improved sounds by the user and actual attempts to focus and minimize on Windows and macOS.

<a id="aktualizacje-w-grze--dist-07"></a>
## Updates in the game — DIST-07

Decisions D13 (U1–U8) with 2026-10-14; behaviour and states describes `wotr-server:docs/release/DISTRIBUTION_PLAN.md` (Section ‘D13 — Changes with 2026-10-14’). Three elements, all in English:

- **Toolbar `update-notice`** over each screen (root layer, under notifications and test strip, 116
  px from the top, centered). pass   `update-lane` does not capture the indicator; gestures on the
  map pass by the bar. The bar shows one of: optional version (`Download update`  /  `Later`, size
  in content, description in tooltip), download with a percentage (`Cancel`; missing for the
  required version), countdown to the required version date (Update required at 20:00  (in  1  h  12
  min) · Downloading  45%), ready-to-use parcel (`Restart and update`  /  `Later`; repair: `Restart
  and repair`) or the download error requested by the player (`Retry`  /  `Later` The bar does not
  appear or disappear itself during the decision beyond the change of status of the update.
- **`update-required`** window (scrim like Settings, window like `quit-window`) when the server
  rejects the version (409 or the status of the directory) or the date expired. The screen
  underneath is visible, but disabled (`InterfaceController.Hold`). action in fixed places: `Retry`
  (only after error or lack of release), `Restart and update` (active when the package is ready;
  copy without installer: `Game page`), `Decline and quit` (always). Esc and click next to do
  nothing. Focus: `Restart and update` when active, or `Decline and quit`.
- **`update-settings`** section in Settings, under two columns: version, platform, commit shortcut
  and type of copy; status in one sentence; `Check now`, button of the moment (`Download update`,
  `Cancel download`, `Restart and update`, `Game page`) and `Repair installation` with confirmation
  in place (`Repair` / `Cancel`, focus on Cancel). The withdrawn version disables the repair and
  suggests an update.

`Restart and update` and `Decline and quit` follow the same path as `Quit game` (`ClientFlow.SaveForExit`): the write error stays in the bar or window and the reclick re-writes. The second release copy shows “War of the Ring is already running” with `Quit`. The editor and development builds do not run the updater or block one copy.

<a id="obserwatorzy--obs-03obs-05-2026-10-16"></a>
## Observers — OBS-03-OBS-05 (2026-10-16)

- The menu and waiting room have Play and Watch and the audience to 16 people
  ([MAIN_MENU_UX](MAIN_MENU_UX.md), [WAITING_ROOM_UX](WAITING_ROOM_UX.md)).
- board viewer has a "Observing" bar with Free Peoples/Shadow switch, information only clicks,
  neutral console and neutral end screen ([OBSERVER_UX](OBSERVER_UX.md)). Without commands, draft,
  automatic decision-makers and read tags.

<a id="odtwarzacz-replayów--replay-05-2026-10-16"></a>
## Playback player — REPLAY-05 (2026-10-16)

- board replaya uses board viewer: the “Observing” bar with Free Peoples / Shadow, information only
  clicks, neutral console and end screen. The status of the connection is “Replay”.
- Under the “Observing” bar is the playbar (`replay-bar`). It has fixed controls:
  - ‘ ⁇ Turn’, ‘ ⁇ Step’, ‘Play’/‘Pause’, ‘Step ▶’, ‘Turn ⁇ ’;
  - Step axis (`replay-timeline`);
  - the entry ‘Step n/N · Turn t · phase’;
  - tempo „Slow / Normal / Fast”.
- Navigation:
  - the play opens at the beginning, in pause;
  - manual step, turn or axle motion pauses playback;
  - The ‘Turn’ goes to the beginning of the closely previous or next turn; at the ends of the
    control is razed.
- Playout skips the time of the mind: the break of the 2 / 1 / 0,25 is at the end of the
  presentation step (default Normal). The player checks the flight figures and the second stage of
  the track which 0,05 s, at the most 3 s (REPLAY-06), so the mute and reduced motion do not block
  anything. It stands at the last step and the loss of focus of the window pauses. The end screen
  opens once on board, also when it goes back through the final.
- REPLAY-06: a playback step and new live facts have flights figures, two-stage tracks and sounds.
  First view, hand step, jump, page change and reconnect show state without animation and sound
  (`BoardScreen.ApplyQuietly`) and stop what still goes on.
- The change of page shows the same step on the other hand: hand, Log and reports. Camera and
  bookmarks remain.
- Log contains only steps saved to the moment shown. The battle report appears when battle was full;
  previously battle is only visible in its window.
- The frames are read in portions after 25 except for the main thread; they are held at most 3
  portions. Later reading wins with earlier, so fast jumps do not show a step out of order.
- Code: `ReplaySession`, `ReplaySource`, `ReplayHistory`, `ReplayReports`,
  `UI/Replays/ReplayBar`.

## Political view — UI-218 (2026-10-17)

A folded-map button above the map legend and the P key switch the political view: regions in their nations' colours over a subdued illustration, captured settlements striped in the captor's colour; play continues in the view and the choice is kept on the device. Details: [MAP_CONTOURS_UX](MAP_CONTOURS_UX.md#political-view--ui-218-2026-10-17).

## Hide from the Fellowship plaque — UI-220 (2026-10-17)

The Hide button is gone: with a die that can hide a revealed Fellowship, the corner status plaque glows and one click hides it. Details: [FELLOWSHIP_UX](FELLOWSHIP_UX.md#hide-from-the-status-plaque--ui-220-2026-10-17).

## Table effects unroll like the hand — UI-221 (2026-10-17)

The owner's request: the cards in play on the table unroll on hover like the hand's cards. Every source of the Effects tab is a tile in view at once (no pages); hovering or focusing a tile unrolls its complete text on parchment under the tile, over the other tiles, and leaving it rolls it back; the unrolled text takes no pointer and sends nothing (a user-approved hover exception like UI-91). A click still pins the source's details (scopes, Locate), and × still removes or chooses a source. The panel's header has only its close button, on the right.

## Discard from the hand by clicking — UI-222 (2026-10-17)

The owner's request: whenever cards must be discarded from the hand — an excess hand, the cost of removing A Power too Great or Tom Bombadil (one Shadow die of any result, one Army and one Character card), or an effect's choice of a card from this hand (We Won't Go Back) — the player clicks the cards in the hand on the right. Cards that may be discarded glow red (`parchment-discard`), the counterpart of the green playable glow; the discard is sent as soon as the selection is complete (the second cost card removes the protection), with no extra confirmation. The server keeps validating the cost.

## Pulsing target regions — UI-224 (2026-10-17)

Enterable regions glow green and attackable enemy regions red along their edges, with a gentle pulse (still with Reduced motion). Details: [MAP_CONTOURS_UX](MAP_CONTOURS_UX.md#pulsing-glow-of-enterable-and-enemy-regions--ui-224-2026-10-17).

## Left tabs stand out from the map — UI-223 (2026-10-17)

The owner's request: over the map the left tabs (Politics, Effects, Characters, …) were barely visible. Each tab now sits on a dark lacquer plate with a fine gilt rim (`LeftRail.uss`), so the medallion and its label read over light and dark terrain. A usable tab (a legal target or usable by the chosen die, UI-136/UI-193) gets a bright gold rim, a warmer plate and a gold label, and pulses gently with the map targets' rhythm (750 ms); Reduced motion keeps it bright and still. Sizes and places stay fixed.

## Object-choice routing — UI-244 (2026-10-17)

Every own decision has one host object family (`DecisionRoutes`: map, region panel, reserve, Effects, dice, Rings,
hand, Battle, Hunt, Fellowship, characters, console, or the generic window as a fallback), read only from the
decision's kind and presentation. The console status opens that host; UI-245–UI-251 move the remaining
presentations from the generic window onto their objects. Test: `DecisionRoutesTests`.

## Required decisions reach their own window — UI-254 (2026-10-17)

The owner's request: a decision the game waits on — excess units, battle and event losses, Hunt damage, excess hand
cards (`RequiredDecisions`) — turns the decision notice at the top into a red enamel notice with a "!" that pulses in
the plaque's rhythm (still with Reduced motion) and goes when the decision is resolved. The notice and the console's
plaque open the window that already handles the decision — the region panel for excess units (the map focuses that
region), the battle window for losses, the Hunt panel for its damage, the hand for excess cards — and a second click on
the plaque closes it. No new window is made. Details: [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md),
[CARD_CHOICES_UX](CARD_CHOICES_UX.md). Tests: `RequiredDecisionTests`,
`BoardInteractionTests.RequiredExcessOpensItsRegionPanelFromThePlaqueAndTheNotice`.
