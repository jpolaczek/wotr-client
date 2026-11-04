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

We choose a specific die. Army/Muster immediately highlights all legal applications (UI-28), without separate choice of categories. Since UI-461 (owner, 2026-11-03) a Will of the West die first lights only what it does as itself (Gandalf the White's or Aragorn's entry, the Balrog's deactivation, a removal paid with Will of the West, any-die uses); the console's face menu turns it into Character, Army, Muster or Event, and then it lights exactly what such a die does ([UI-461](#ui-461)). A gesture on army, reserve, track, character, Fellowship, card or deck determines the use of the same dice. Special abilities and valuable resources still require your own open choice. Tooltip dice provides applications without redundant Action die/Available. The paid sequence retains the type and budget saved; the unsent intent can be replaced by another object.

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

Army → click your own army → illuminated targets → click the illuminated terrain; traffic is done immediately ( UI-105 : no drag figures and the army, no choice on the Characters list; the drag part
is superseded for units and characters by UI-421, see [TABLETOP_FIGURES_UX](TABLETOP_FIGURES_UX.md)). From UI-127 click on your own army, which is a legal source, mark all of its figure can move and open the region panel; click figure in the panel it shows (dividing), another click of the same army retains division, and the two clicks again mark the whole. figures another region before the target replaces an unfinished selection. From UI-140 the power header in the region panel has a switch select/unselect all figure The following is the non-interactive spirit of the selected target. figures with the signature “Move n figures” or “Attack · mode” from the server option. The first Esc in the open region panel removes all unsent selection ( die stays), another one closes the panel. Click on the highlighted area selects the target, also with a busy friendly region.

Two symbols of the march at dice show the budget. The first move executed is permanent on the server and irreversible locally. Then the second army and the second goal; the second march ends action automatically. ✓ with dice lets you finish one. figure it cannot move again, even after the merger. We only settle the transitional surplus and the political effects in the right final window of rules.

After selecting dice and participants, a single click of the legal enemy region begins the attack. Without the prepared attack, the region serves for inspection. Attackers remain in the source for legal entry after battle. The availability of movement/attack depends on the specific composition, nations and Leadership.

Large stacks have stable groups and developed figures nets without scroll. The garrison remains visually separated. Map objectives cannot disappear under a label, tooltip or marker on another scale.

On the second march figure, which has already moved, are in the panel extinguished with a sign ✓ and non-selectable; force of which all figure They're moving, it's got this mark on the map (UI-127 When some of the selected figures cannot enter the region accessible to the rest, the tray lists them (‘Leave out...’). Traffic paid dice, also with the ability, goes this flow; movement from the master's token and card with a route plan or a rescue stay in a tray with one approval because the server accepts their entire plan in one command  (UI-128,  [ARMY_MAP_UX](ARMY_MAP_UX.md) The Army cannot be dropped; drag remains for the reserves and cards.

<a id="tor-mordoru-na-mapie--plan-ui-152-wdrożenie-ui-153ui-155"></a>
## Mordor track on map — plan UI-152, implementation UI-153–UI-155

Mordor's track is drawn on the map as a stone trail in ink from Morannon and Minas Morgul to the Doomsday Penetration in Ordruin; Fellowship stands at its step, and the entry, motion and hiding attempt work with one click. The camera moves only to the track at the player who entered. The Fellowship panel retains the line 0–5. Details: [FELLOWSHIP_UX](FELLOWSHIP_UX.md), F07a.

<a id="no-card-bez-kliknięcia--ui-151"></a>
## "No card" without clicking — UI-151

Superseded by [UI-279](#every-choice-waits-for-a-click--ui-279-2026-10-19): without a legal card the player clicks No card, which glows alone and says why. In the cards stage the button keeps its place in the footer, greyed while waiting. Details: [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md).

## Every choice waits for a click — UI-279 (2026-10-19)

Owner decision 2026-10-19: the client never sends a decision without a click, even when only one option is legal. This
rule replaces the expansions program's "a single legal choice is sent automatically" and UI-151's automatic No card.
Automatic single choices cost later work: UI-274's forced draw clashed with UI-12, because opening a saved game must
not submit it. The single option glows, and its tooltip says what the click does:

- a Combat choice without a legal card: No card glows and says "No legal Combat card · Click to fight this round
  without one" (UI-151);
- a special-die choice with a forced Eye: the Eye glows (UI-267);
- a Ruler discard with one eligible die: that die glows, and the hint reads "Click your only eligible die to discard
  it" (UI-273);
- a corruption draw that is the only legal step: the console's Draw tiles; a single remaining tile: that tile (UI-274).
  Since [UI-442](#a-standard-corruption-attempt-draws-all-its-tiles-with-its-click--ui-442) a standard attempt has no
  draw step (its own click draws every tile) and the server places a single drawn tile itself, a rule of the game (EXP-15
  D8), not a click the client sends; Sequential corruption keeps its Draw again.

A click that completes a choice still sends it at once (UI-142): recruiting at the only settlement, a card with one
payment method, and awakening a king to his only destination. The player's own "Skip cards this battle" policy (UI-11)
stays, because the player chose it. Tests: `BoardInteractionTests.UI279NoLegalCombatCardWaitsForTheNoCardClick…`,
`SpecialDiceNetworkTests.AnEyeForcedAmongTheDiceWaitsForItsClick`, `RulerDiceTests`, `ExpansionCorruptionNetworkTests`,
`SovereignPanelNetworkTests`, `KingsCards02NetworkTests`; `MatchTestRoom.NoCard` clicks No card for every own Combat
choice.

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
  `frontend/button` since UI-471, before it `frames/button-square` (hover, push), with mandatory tooltip. Replaces characters ×, ‹, ›,  ✓, ⁇
  /◆, ⁇, ⁇, ?, +/−, ⁇, ⁇ /☐ and words Back, Close, Retry, Refresh, Location, Why?, Pin, Older,
  Previous, Next and the button “Reinforcements” in the tray. pass and rejection dice have their own
  icons; the development of the log entry switches the icon more/less. cards version Companion and
  the country of the Council is an icon; reroll becomes a word ("Keep roll" / "Reroll
  selected"), because the label says what it will do to press.
- Label button (`.action`): `frontend/button` frame since UI-471, before it `frames/button`, with hover, selected and pressed variants (UI-114
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

from UI-130  ([ARMY_MAP_UX](ARMY_MAP_UX.md)) the Muster selection opens the Meals Panel at the left slat (since UI-181 the Muster die only lights the Reserve tab, since UI-317 only while it can recruit): the line of each own nation, budget slots and ✓ during the sequence. From UI-134, the line is one line with a graphical policy track (active or passive), the reserves and the move icons on the track and the activation of Narya when the server allows them; The status and reason for the lock are in the tooltip. The chip click moves the view to the legal settlement; from UI-150 card, which die can play, are only in hand. The settlement highlight button on this figure, click on settlement to recruit without Confirm; At one legal settlement click the token immediately (UI-142, formerly two-click), and drag is a shortcut. The settlement click on the map means the line of its nation and restores the closed panel; click settlement, which the selected figure cannot occupy, shows the reason for the server in the tray. from UI-135 Muster paid for with the master's token or skill is a plan in the same panel (slots, figure with × one ✓), and the warning about a lone leader is in the tooltip of the token. The closed panel returns with the “Reinforcements” button in the tray. from UI-131 the same panel supports recruitment with cards  („ EventRecruitment ”, „ ReserveFigures), Kings and Balrog's rulers: the header calls the source, slots count the planned figure,  figure with × return to the reserve in the panel and from the map (spirits in the regions of the plan), and one ✓ approve the whole together with the tray. The selected die distinguishes card from playable (UI-124) and from UI-136 also the Green Light tabs Characters and Politics, the Fellowship and decks when server options allow it to be used.

Policy: click or Enter/Space On the left, the Politics page opens all tracks on a common Peace → War scale. Hover/fokus shows only the text of the bookmark. Own nations are first; checkbox Show opponent (from UI-197 The signature “Opponent”) shows the opponent by default. The chip stands in the actual position. From UI-197 (user notification from 2026-10-16 The coat of arms of the nation in the side medallion, over a soft golden glow. The coat of arms of the active nation is fuller and shines more, and the passive coat of arms is dim; the legend in the column header shows both appearances. The column of names no longer has a coat of arms, so the panel has 256 pt 290 . The state is supplemented by a outline and tooltip. The panel remains after the cursor leaves; ×/Esc closes it with the choice and cost. No scroll and separate track to open at each nation.

Muster, Army/Muster or Will → arrow on the next field of the nation, all in the same panel as Activate ( UI-26 . from UI-144 (user decision) we do not mark the nation first: after choosing the cost of each own nation that die can move according to the server option, immediately shows the arrow “→” in the next field, and its click takes a step without Confirm. A click of a token such a nation takes the same step. Drag of the token into the field becomes a shortcut: distinguishes the target of the drag chip and drop sends a step; canceling or illegal drop does not change anything. The nations of the opponent do not have arrows. There is no separate inspector, the list action or the Confirm button. A chip click without legal context leaves the panel; the tooltip explains the state and requirements, does not choose as the player dice The restored draft distinguishes the arrow of its nation. From UI-148 played card who is waiting for the choice of the nation (e.g. the Wisdom of Elrond, `NationTargets` server), opens Politics with highlighted nations, once per play; closed by the player the panel does not return on another revision. The token click selects the nation so far. The policy council uses the same tracks after the open selection of its token action , without a replacement dice A passive nation can approach the field before the war.

UI-27 removes the former lower renderer of the narodu/toru. selection No complete cost or restore the draft does not switch the policy to the bottom panel: a short tip points to a specific dice. UI-28 removes an additional selection of its categories; UI-461 brings it back for Will of the West only, as the console's face menu (a Will die moves a nation as a Muster result). Track, token and target remain exclusively in the Politics. Recovery of the uncertain response of the server retains its own message and Check result.

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

> **Planned, not shipped (UI-302, 2026-10-23):** the live presentation of an important opponent action will show
> the played public card and move the camera without a click, before the resulting decision opens, with one
> transient Skip target. This limited exception and everything it supersedes are specified in
> [OPPONENT_ACTIONS_UX](OPPONENT_ACTIONS_UX.md); until UI-303–UI-313 ship, the rules below stay in force. Shipped so far:
> the cue model (UI-303), the playback coordinator and command gate (UI-304), and the die emphasis, camera and Skip
> target (UI-305, [§6.9](OPPONENT_ACTIONS_UX.md#wdrozone-305)), then the family presenters UI-306–UI-312 (§6.10–§6.16);
> UI-313's aggregate acceptance is in [§6.17](OPPONENT_ACTIONS_UX.md#akceptacja-313).

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

From UI-126 the 160%/145% threshold no longer hide the army. Below 1,3× match (return below 1,2×) each force has a token in the hand enamel with the coat of arms of the most numerous nation and number; above, and for the region open-click, selected strength and fresh arrival, figurines on the stand with coat of arms and number. The garrison has blanks. The power government stands over the symbol settlement and does not obscure it; the tooltip gives the composition per nation. The Fellowship tag uses the same threshold 1,3×. Details: [ARMY_MAP_UX](ARMY_MAP_UX.md). Since UI-401 (2026-10-30) every force, the Fellowship and the planned ghosts stand on their region's fixed places, sized with the map, and figures need room in the region's capacity besides the 1,3× level ([TABLETOP_FIGURES_UX](TABLETOP_FIGURES_UX.md)).

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

dice Keeper/Lesser and Ruler discards are selected in the pool; since UI-264 the console's hint gives the short effect before the gesture and Dice & tokens does not open for them. Its other decisions open Dice & tokens once, Close retains the choice. The council does not choose strategic application or cost per player; its own applications and accurate dice inputs are highlighted. The change of the wall The Ring does not start using dice. Objects not available retain the source inspection, conditions and appropriate recovery time. The enemies of dice may be the target of their own Dismay according to server options.

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

Superseded by [UI-274](#sovereigns-on-their-objects--ui-274-2026-10-19): the decisions are made on the portraits,
their attempt buttons and tiles, and the inspector hosts none.

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
Since [UI-436](#a-characters-window-is-compact-shows-its-place-as-a-picture-and-opens-in-one-place--ui-436) the
location is the picture of the figure's place, enabled only for a region.

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

The counters show the same public values as the former text line, also Out. Chip has tooltip with a page and description (e.g. "Shadow · Dice in the Hunt Box: 2 ), is not a button and does not open panels. Since UI-361 (owner, 2026-10-26) the card counter's tooltip says what the rules make public about the opponent's cards (the decks' backs differ, BASE 2.4.1 pp. 14, 29): "Shadow · 5 cards in hand", the split by deck ("Character 3 · Strategy 2", from Contracts 0.12.0 `PlayerSummary.HandDeckCounts`), the decks' remaining cards and the discard pile's size; no card is named (`OpponentCards`). During the opponent's playback the split steps with each drawn or discarded card of a known deck (`StagedBoard.Hand`). dice the opponent retains the previous clicks, tooltips and states choice/target/candidate as a coloured ring around the medallion; the hover brightens the frame. Below 1340 pt width (at 125% on screens 16:9 ) the signatures of the counters and the signatures of the pages are given to icons, values and tooltipom, and medallions have 28 The Ornaments and Shadows do not capture the indicator or edge master. From UI-98 The belt has 44 pt, meters are an icon with a number and Hunt counter went to the bookmark Fellowship and Hunt in the top right corner of the map.

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

Notification 2026-10-11 with screen: all information from a separate window card display at the bottom of the Effects panel. Source click- card selects them, and under the ranges and term appears a small parchment as developed card in the hands: the icon of the type of event, type (e.g. " STRATEGY CARD · MUSTER EVENT ), full text of the event and the fight section (initiative, title, text). The title is once, in the heading of the detail; card (Free Peoples · In play or "Effect ended", then parchment is extinguished) replaces the window footer. The Effects entry no longer opens a floating preview card ; the capacity of the form and source without card They don't have parchment. card range does not repeat the shortcut of effect (says it card ) Location buttons and expiration date are in one row. When text would push the panel under the bottom pass (e.g. 768 px at 125% ), panel expands like an old window card at most to 55% width board , and the sources entries have one line; the new source or change of window returns to the width of the bar. When paying for the removal of parchment is hidden, because the place is the decision. The content is located above the bottom pass in four systems without changing. Since UI-424 (2026-11-02) the panel never widens: the details open on their own card beside the panel ([UI-424](#the-effects-panel-keeps-every-source-and-its-width--ui-424)); since [UI-446](#a-card-effect-shows-its-active-part--ui-446) a click opens no details at all and the hover text shows a card's active part.

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

The bar has 44 pt instead of 56 (new 44 pt frame; 36 pt and flat since UI-479) and is a border control: edge pan works only on the outer edge, so reading the bar does not move the map. Counter counters are an icon with a number (a name in a balloon), without a Hunt counter; Menu has 64 pt (56 in a narrow bar). pass notifications end at column, like the bottom bar, next to a tab in the corner.

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

UI-274 replaces the hiding rules below: every sovereign keeps a fixed slot in every state.

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
  [UI-436](#a-characters-window-is-compact-shows-its-place-as-a-picture-and-opens-in-one-place--ui-436) replaces
  this: every ruler's window opens at one place beside the Characters tab, with one size.
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

<a id="ui-402"></a>
## Every character is its own miniature — UI-402

UI-421's decision 5 (owner, 2026-10-28), built 2026-10-30 ([TABLETOP_FIGURES_UX](TABLETOP_FIGURES_UX.md), "As built
(UI-402)"; [ARMY_MAP_UX](ARMY_MAP_UX.md#ui-402)):

- On the map every character stands as its own miniature on a small round base in its side's enamel, beside the army in
  its region, on one of the region's character places (a garrison's beside the gate); it is never a figure of the army's
  marker, and the army's number counts its units only. Gandalf and Strider standing together are two figures. A region
  with more than six characters (three at a gate) shows a stacked pair with "+n" on its last place. Far out a character
  is its portrait in its side's medallion.
- Its click chooses it exactly as its chip in the region panel does (UI-105): with a Character die it starts its
  travel, with an Army die it joins the army (the commander-first rule), and a card's choice, an ability or an order lit
  on the figure is used. The player's own character's click opens no region panel; an enemy character's click is its
  region's. The army's click still selects every figure that can move, its characters included.
- The chips and the miniatures show one set of choices: a figure lit, selected or marched in the panel is lit, selected
  or marched on the map, and the other way round.
- A character appears, flies in or leaves on its own (UI-334, UI-338); the opponent's traces light an army together with
  its characters.

<a id="ui-403"></a>
## A chosen figure lifts off the board — UI-403

UI-421's decisions 1 and 2 (owner, 2026-10-28: "the lifted thing is that object, not a square or its shadow"), built
2026-10-30 ([TABLETOP_FIGURES_UX](TABLETOP_FIGURES_UX.md), "As built (UI-403)"; [ARMY_MAP_UX](ARMY_MAP_UX.md#ui-403)):

- A click that chooses figures lifts exactly those off the table: a force's chosen units rise on a base of their own and
  each chosen character on its stand, drawn as on the table at the same scale, a little larger (1.10) and higher (12 px
  × the figure's scale), their shadow left on the table. The rest of the army stays on its base; a base none stays on
  disappears, though a click there is still the army's click. A figure left behind in the region panel drops back onto
  the base.
- What is lifted is what the draft has not sent: the army selection, the travelling group being chosen, the Nazgûl a
  card's next destination click flies. A sent step stays raised until the server's view arrives.
- On a hovered legal target a landing mark lies on the table where the figures would stand, green for a move and red for
  an attack, and the click's caption hangs above the lifted figures. The translucent army ghost (UI-127) is gone:
  nothing is drawn faintly at the target.
- Esc and undo drop the figures back onto their base, and so does an attack. A card's planned flight or journey
  dissolves the lifted figure into its faint planned figure at the destination. Reduced motion shows the raised pose at
  once.
- The lifted figures never take the pointer and are clipped to the map; clicks remain the whole gesture until UI-405
  adds carrying them (built 2026-10-30, below).

<a id="ui-405"></a>
## A unit or character is picked up and carried — UI-405

UI-421's decision 2 (owner, 2026-10-28: drag and click for units and characters only), built 2026-10-30
([TABLETOP_FIGURES_UX](TABLETOP_FIGURES_UX.md), "As built (UI-405)"; [ARMY_MAP_UX](ARMY_MAP_UX.md#ui-405);
[BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md#ui-405)):

- A press on the player's own army or character that is a legal source (or already chosen) and moves past the drag
  threshold lifts it as its click would, without opening a panel, and carries the lifted miniature itself under the
  pointer — the army together with the characters chosen with it. No text label or square follows the pointer. The
  opponent's figures, any figure while the board is busy and a figure with nowhere to go do not lift: their press stays
  a click.
- The region under the carried figure lights (a legal target more strongly in its own colour), and on a legal target the
  landing mark and the caption show as on a hover. Held at the map's edge for about a quarter second, the map pans.
- A drop on a lit target other than the figure's own region is that target's click: the same command, with no Confirm.
  A sent step leaves the figure raised where it was dropped until the view comes, and its flight goes on from there; a
  card's planned flight dissolves it where it was dropped; an attack returns its army to its base as the click does.
- A drop anywhere else (an unlit region, its own region, the HUD), Esc, focus loss, a reconnection or a new revision
  sends nothing and puts the draft back exactly as it was before the press; the figure glides home.
- Clicks and the keyboard remain the whole path (R16); dice, cards, tokens, the reserve, the region panel's figure chips,
  the Characters rows and the Fellowship portraits keep their gestures.

<a id="bez-granicy-mapy--ui-104"></a>
## Without map boundary — UI-104

2026-10-12: "Let's remove this brown border separating the real map from the rest of the graphics".

- The board edge (north, east, south) and the north-east cut arch no longer have lines: map paper
  turns to canvas paper, and white transitions between regions end at the edge.
- The sea shore, lakes, rivers, barriers and passageways are so far drawn.

<a id="ruch-wyłącznie-na-mapie--ui-105"></a>
## Movement exclusively on the map — UI-105

UI-421 (owner, 2026-10-28; written down 2026-10-29): for units and characters this is superseded by [TABLETOP_FIGURES_UX](TABLETOP_FIGURES_UX.md) — they become miniatures that a click lifts and a drag carries; a drop sends what the target click sends, and clicks stay the full path (R16). Until UI-405 ships, the gestures below stay as written; dice, cards, tokens and reserves keep theirs. UI-405 shipped on 2026-10-30 ([A unit or character is picked up and carried — UI-405](#ui-405)): the clicks below are unchanged, and the map's armies and characters can also be carried to their target.

The 2026-10-12 application: "Moving is only to be done on the map, click on the region, select selected units, characters, nudges etc. and select only by clicking the selected region, move is done immediately". Elections from the conversation: die first; the character's route with a range greater than 1 arranges the game.

Flow. die   → click region → click figures in the region panel → click the highlighted target. The move is executed immediately, without Confirm. Another army or group in the budget dice begins the same way; ✓ with dice He's done early. Since UI-402 (2026-10-30) a character's own miniature on the map is clicked as its chip in the panel is, without opening the panel ("Every character is its own miniature — UI-402" above).

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
  is not a move. Since UI-405 (2026-10-30) the map's own armies and characters are carried and their drop on a lit
  target is the target's click; the region panel's figure chips, the Characters rows and the Fellowship portraits still
  do not drag.
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
- Without legal movement, the icon is a simple, non-clip symbol. Hiding is the Ring icon (UI-232), the declaration the
  Fellowship's map figure and entry to Mordor the track's gate (UI-236), and the Guide its glowing portrait (UI-249);
  none of them is a panel button. Since [UI-434](#every-object-a-hint-asks-to-click-visibly-glows--ui-434) (2026-10-31)
  the offered gate wears the gold ChoiceGlow on the map and the hint names the map's gate.

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

- **No update bar (DIST-17, 2026-10-18).** The bar over every screen (DIST-07's `update-notice`, with DIST-15's What's
  new and "Updated to version") is gone. Only the first menus — the sign-in screen and the main menu — show an update:
  their **Settings** button glows gently (`update-glow`: golden tint and edge, nothing moves) and carries a small note
  that is not a control (`settings-update-note`, no pointer): `Update available`, `Downloading 45%`, `Update ready`,
  `Update required soon` or `Update required`; on the sign-in screen under the button, in the main menu inside it on
  the right. The button's tooltip says the full sentence. A repair, a failure or no release show nothing. Downloading,
  installing and `What's new` happen only in Settings' Updates row.
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

DIST-18 (owner 2026-10-25): from the click on `Restart and update` or `Decline and quit` until the game quits, the exit lock (`ExitLock`, `InterfaceController.LockForExit`) greys the whole screen — the game loses its colour under a grey veil over every screen, Settings and the update window — and names the step (`Saving your game…`, `Installing the update…`, `Restarting…` or `Closing…`). Nothing beneath takes a pick, the wheel or a key (Esc and shortcuts included; the lock takes the focus and the interface root stops the rest before any element sees it), and edge pan stops with the disabled content. The lock has no control of its own and fades in quickly (at once under Skip animations). A failed save or install lifts it, gives the focus back where it was and shows the error with the re-click, as before.

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
  the track which 0,05 s, at the most 20 s (REPLAY-06; since UI-328 also the step's presented actions, every side's from
  the side shown), so the mute and reduced motion do not block
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

## Fate board — UI-265 (2026-10-19)

The Fate map reads as the expansion sets it up: the region overrides from the server make Erebor a Shadow (Sauron)
Stronghold, Dale a neutral Fortification and the Iron Hills a Dwarven City (New Cities adds Ered Luin and South
Rhûn), so their gems take the right cut and colour (ruby, quartz trillion, sapphire by control). The map's landmark
drawings may have a variant per module (`landmarks.json` `"variants": {"fate": …}`): in a Fate game Erebor's drawing
is the Shadow-held one (ART-05) in the same place, chosen from `BoardPresentation.Modules` and repainted only when the
modules change. The region panel adds where its points come from (`RegionVictoryText`, `hover-region-points`): "Worth
2 VP to the Free Peoples if captured" or "Scores 2 VP for the Free Peoples"; the Victory panel lists each scoring region
with its control, settlement and points. Tests: `MapLandmarksTests.AFateGameDrawsEreborHeldByTheShadowInItsPlace`,
`RegionVictoryTextTests`, `VictoryPanelNetworkTests.FateNewCitiesAndKingsKeepTheirAuthoritativeScoringOrigins` (with a
screenshot of Erebor), `ConfigurationLayoutTests` (all 93 configurations in four layouts).

## Lords characters on their objects — UI-271 (2026-10-19)

Every Lords character ability is used on its object; no ability button stands in the region panel or the inspector:

- **Entries** start from the character's row in the Characters panel, as the base characters do (UI-240). The Mouth
  of Sauron and the Witch-king, whose Lords cards are alternatives to the base ones, listed each version not in play as
  a row of its own (`CharacterRosterVersions`, key `figure@version`). Superseded by
  [UI-438](#one-marker-per-character--ui-438) (2026-11-01): one row each, like Strider/Aragorn, its second line naming
  the version it shows (Lieutenant / Númenórean, Black Captain / Chief) and its window switching between them. With the Black Captain in play, his
  one row turns him in place: "Click to turn The Witch-king, the Black Captain into The Witch-king, Chief of the
  Ringwraiths" (before UI-438 the Chief's own row under Not in play did). An entry waiting for its region offers no Change button (which
  opened the generic window): another lit row changes it. Elrond, Galadriel, Gothmog and the Balrog enter from their
  rows; the Balrog's optional reserve figure is chosen in the Reserve panel, which now unrolls when that plan begins
  after the Muster die (`ReinforcementsPanel.Follow`).
- **The Balrog** is activated (Use Balrog die) or deactivated (Free Peoples' Will of the West, or a Character die with
  Gandalf beside him) by a click on his figure: with the paying die selected, his region glows on the map and his
  figure in its panel glows and says what the click does (`FigureAbilityInteraction`, `CharacterAbilityPlaces`).
- **Gothmog**'s "fling into the fray" stands in the Reserve panel while a Muster die is selected: his row
  ("Gothmog · Minas Morgul") offers a Sauron Regular (recruit beside him) and an Elite (replace a Regular there); the
  token's click uses the ability at once (`GothmogReinforcements`). Under UI-317 a die that pays him backs the Reserve
  even when it cannot recruit (`MusterActions.BacksReserve`).
- **Evil Knowledge** is the Mouth of Sauron's glowing portrait in the Battle window's besieging column; its click
  extends the siege without downgrading an Elite (UI-347, D12; [BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md)).
- **The Chief's reactions**: Spear of Terror lights the Shadow's Character deck ("Click to draw a Character card ·
  Spear of Terror…"); He Sees, He Knows lights the Chief's region and his figure, whose click moves him to the
  Fellowship (`ChiefReactionObjects`). The console's Pass declines them (UI-291).
- **One movement pattern**: a card that moves Shadow characters uses the Nazgûl flight gesture (UI-212) for all of
  them. A walker (the Balrog alone, the Lords Minions) goes along its shortest path within its level, alone in its
  group, its ghost is its own figure, and the console speaks of figures rather than Nazgûl. The Balrog card's order
  stands on his figure (alone) and his army's figures (with it), and Pass finishes (UI-347, D11;
  [CARD_CHOICES_UX](CARD_CHOICES_UX.md)).

Tests: `CharacterRosterVersionsTests`, `NazgulFlightDraftTests.AWalkingCharacterMovesAlongItsPathWithTheSameGesture`,
`CardConsoleOptionsTests.TheBalrogsOrderStandsOnHisFigureAndHisArmy`,
`CardConsoleOptionsTests.TheChiefsReactionsLeaveTheConsoleForTheirObjects`, `ExpansionAbilityNetworkTests` (Balrog
figure, Gothmog's Reserve row), `ExpansionWindowNetworkTests` (Evil Knowledge), `LordsCharacterNetworkTests`
(entries, the Witch-king's switch, Spear, He Sees, the Balrog alone), seeder scenarios `ui271-*`
(wotr-server `LordsCharacterScenarioTests`).

## Sovereigns on their objects — UI-274 (2026-10-19)

Owner decision 2026-10-19: the kings' decisions are made on their objects, and no window opens for them.

- **Characters panel.** Every sovereign keeps a fixed slot in every state (`SovereignPortrait`): his painted portrait
  (an Initial king shows his awakened portrait, dimmed), a status badge (initial, awakened, corrupted, removed), his
  name, a corruption meter with a pip per point of resistance (filled for known corruption, hatched for hidden tiles;
  only while Initial) and a row for the tiles of an attempt or an inspection. An unlit portrait's click opens the
  inspector, which only informs (since
  [UI-436](#a-characters-window-is-compact-shows-its-place-as-a-picture-and-opens-in-one-place--ui-436) a compact
  window at one place beside the Characters tab, his place as a picture).
- **Corruption attempt.** For the Shadow a graphic button stands at the top right of each Initial king's portrait. It
  keeps its place, disabled with its reason in the tooltip, when no attempt is possible. Its click pays an eligible Eye
  from the Hunt Box: a standard Eye, else a Ruler die; a Lords special Eye only when the player clicked it first. When
  only special Eyes remain, the button stays disabled and says "Click the special Eye that pays in the Hunt Box
  first." The drawn tiles stand beside the king: values for the Shadow, backs for the Free Peoples. The Shadow's
  click attaches a tile. In Sequential corruption the latest tile's click keeps it, and "Draw again" stays in the
  console; otherwise every draw is the console's Draw tiles. Since UI-279 nothing is sent without a click: a draw that is
  the only legal step waits for Draw tiles, and a single remaining tile waits for its click. Since
  [UI-442](#a-standard-corruption-attempt-draws-all-its-tiles-with-its-click--ui-442) a standard attempt's click draws
  all its tiles: two or three wait beside the king for the Shadow's click, a single one is placed with the attempt.
- **Awakening.** Die → glowing king (portrait or his force on the map) → destination. A single destination (his
  besieged home, for one) is sent with the king's click.
- **Cards and the cleanse.** A card's choice of a king lights his portrait ("Click to …"). The inspection's tiles stand
  beside the inspected king, and the cleanse returns a random tile from the glowing king; Keep is in the console.
- **Map.** An Initial king is his force's crowned leader: a crown over the base or token, and "Led by …" in the force's
  tooltip. An Awakened king is a character figure. A Corrupted king is no figure, so a dark marker
  (`MapSovereignMarkers`) stands beside the forces of his region. A force holding a king that a decision or the
  selected die lights glows like an eligible force, its tooltip says what the click does, and its click chooses the
  king. The exception: when the selected die can move the force itself, the click selects the force and the king's
  portrait chooses him. The corrupted marker glows and acts the same way. Otherwise its click opens the king's
  information.

- **Motion and glow (UI-345, 2026-10-26).** A king whose portrait changes (awakened, corrupted) crossfades to it, a
  changed status pulses on its badge and a changed corruption on the meter; the first values never pulse. The inspector
  opens and closes with a fade, its rows fade in and out, a value of the same king that changes pulses (another king's
  values do not), and a newly attached tile fades in. A lit portrait, attempt button or tile pulses with the Characters
  roster in its one rhythm (`CharacterRosterPanel.OfferSovereigns`); the attempt button and the tiles show their
  ChoiceGlow rim. The Dice & tokens panel (deleted in UI-276), its tab and the Council's choices fade; a new Ruler or
  Dismay token arrives in the pool and a Council medallion whose state changes flashes its art (it does not grow: the
  pool's row has no room beside it). The expansions' tray choices (`ExpansionChoices`) and the version card not chosen
  glow and pulse (the version card in its rim and plate only, as it fills the tray's width). Per D6 the lit art —
  special, Lore and Council dice, Keeper Ring sockets offered now, Council medallions, the Spear's deck — keeps its art
  and dims and back in the shared 750 ms rhythm (`ChoiceGlow.LitArtPulseClass`, a ticker per host that runs only while
  something is lit). Since [UI-434](#every-object-a-hint-asks-to-click-visibly-glows--ui-434) (2026-10-31) an offered
  die, a Keeper socket, a medallion a hint asks for and an asked deck also wear the gold ChoiceGlow and breathe with it,
  since the lit art alone read as unlit; only a usable medallion no hint names keeps the lit art's fading alone.

This supersedes UI-59's decision host and UI-101's hiding of portraits. Tests: `SovereignPanelTests`,
`SovereignMapTests`, `DecisionRoutesTests`, `SovereignPanelNetworkTests`, `ExpansionCorruptionNetworkTests`,
`KingsCards01–04NetworkTests`, `UI63SiegeNetworkTests`, `SovereignSectionLayoutTests`, `ReturnListTests`.

## Kings of Middle-earth art — ART-07 (2026-10-19, painted part)

Generated and built like ART-06: the awakened figures of Brand, Dáin, Denethor, Théoden and Thranduil, their awakened
and corrupted portraits (grey-faced and hollow-eyed under a creeping shadow), and the Chieftains — the Black Serpent,
the Shadow of Mirkwood and Uglúk — figure and portrait. `docs/sources/ART-FIGURES.json` lists nothing pending. The
UI pieces of the module (Ruler die body, Ruler and Dismay tokens, the corruption-attempt button, the Kings Hunt tile)
come with UI-273, UI-274 and UI-276.

UI part (2026-10-27): the Ruler die has its own round body, as a Lords special die — `dice/special.svg`'s
`ruler-free` and `ruler-shadow` variants (a royal blue enamel with an ivory band for the Free Peoples, crimson with an
iron band for the Shadow; hover, lit and pressed), its crown (UI-273) on its top edge — in place of UI-264's framed tile
with a text caption, in the pool and in the top bar. With the Ruler and Dismay coins (UI-276), the attempt button and
the tile back (UI-274) and the Kings mark on a Hunt tile, no Kings placeholder remains. Test: `UiArtKitTests`.

## Lords of Middle-earth art — ART-06 (2026-10-19)

Generated with `graphicalassets/figures/v3/generate.py` (prompts in `prompts.json`) and built with
`scripts/build_figure_art.py`: the painted figures and portraits of Elrond, Galadriel, the Balrog, Gothmog and Sméagol
(stature 0.55, as Gollum) join `characters-v1` and `battle-portraits-v2`; the Mouth's and the Witch-king's Lords
versions keep their base art, as every version does (PortraitArtwork.Key). The rest of the module's art came with its
UI tasks: the special dice bodies, origin emblems and Remove star (UI-267), the three named Rings (UI-268), the
versions badge and Guide crown (UI-269), the Council tokens, alternate mark and Fellowship Box icon (UI-270). No Lords
character shows the silhouette any more. Tests: `CharacterFigureArtTests`, `PortraitArtworkTests`, `UiArtKitTests`.

## Fate of Erebor art — ART-05 (2026-10-19)

Generated and built with the existing pipelines: the Dwarven Lord Gimli's figure and portrait (`gimli-dwarven-lord`
in `characters-v1` and `battle-portraits-v2`) and the Shadow-held Erebor landmark (`map-landmarks/erebor-shadow`),
which UI-265 draws in Erebor's place in a Fate game. The base Gimli keeps his cells for Fate's standard version.
Details: `graphicalassets/figures/README.md`, `graphicalassets/landmarks/README.md`.

## Expansion characters: portrait keys and ability lines — ART-04 (2026-10-19)

A character's portrait and figure cell is its definition id after "<module>.character.", dots as dashes
(`PortraitArtwork.Key`): the Lords versions share their base cells ("lords.character.gimli" → "gimli"), and every
sovereign state, Dark Chieftain and the Dwarven Lord has its own ("kings.character.brand.awakened" →
"brand-awakened"); before, all ten sovereign states collided on "awakened" or "corrupted". A cell without art keeps
the figure silhouette. The prompts for the expansion characters' figures and portraits (and the five corrupted
sovereigns' portraits) are in `graphicalassets/figures/v3/prompts.json` under these keys; ART-05–ART-07 generate them.
Every ability of the expansions' characters has a one-line tooltip form (`character-ability-summaries.json`, at most
72 characters — 85 since UI-365, so a condition can name its subject — no rule added); where one ability id reads
differently on another card, the card has its own form
("fate.character.gimli-dwarven-lord#messenger-dwarves"). Tests: `PortraitArtworkTests.EveryCharacterCardResolvesItsOwnPortraitKey`,
`CharacterAbilitySummariesTests.EveryAbilityOfEveryCharacterHasOneShortLine`.

## Expansion decisions routed to their objects — UI-264 (2026-10-19)

Every expansion decision kind and card presentation has a host other than the generic window
(`DecisionRoutes.Expansion`, `DecisionRoutes.Presented`):

| Decision | Host now | Moves to its object in |
|---|---|---|
| Special die (Lords), Ruler discard (Kings) | lit dice in the pool; the console hint says what the click does | UI-267, UI-273 (art, dimmed slots) |
| Vilya keep, Dismay, Council token | Vilya's socket and the console's Pass (UI-347); Dismay on the glowing Ruler dice and the Pass (UI-276); the Council medallions (UI-347) | UI-268, UI-276, UI-270 |
| Companion versions, homelands (Council) | setup panel | UI-269, UI-270 |
| Corruption tile, a card's choice of a sovereign | the kings' portraits, attempt buttons and tiles (UI-274); in a battle a king who takes part glows in its army (UI-278, D16) | UI-278 |
| Fate's Monsters branch, Kings cards' branches | Monsters: console buttons; Kings: the nation tokens, the kings and the Reserve (UI-278, D15) | — |

The special-die pick, the Ruler discard, the Council setup and token and the corruption tile are required decisions
(`RequiredDecisions`): the red notice at the top waits for them. Vilya's keep and Dismay can be declined and are not.
Dice & tokens no longer opens for the two pool decisions; UI-276, the last of UI-268, UI-270, UI-272 and UI-276, deleted it.
Tests: `DecisionRoutesTests.EveryDecisionKindHasAnObjectHost`, `ExpansionDecisionsHaveTheirHosts`,
`ExpansionCardStepsHaveObjectHosts`, `RequiredDecisionTests`, `ResourcePanelTests.PoolDecisionsStayOutOfThePanel`,
`ResourcePanelNetworkTests.SpecialChoicesAndBothDiscardsAreMadeOnThePoolDiceWithoutAPanel`.

## Rail icons stay on their medallions — UI-261 (2026-10-18)

After choosing a die the available left tabs' icons jumped off their medallions: the icon is placed from the tab's
inner edge, which moves with the border (1 px at rest, 3 px when available, UI-223), while the medallion art keeps its
centre (47, 22). Each state now places the 24 px icon on that centre (`LeftRail.uss`). Test:
`BoardInteractionTests.LeftRailIconsStayOnTheirMedallionsWhenATabIsAvailable` (within 0.75 px in both states).

## Cards among the effects are cards — UI-288 (2026-10-19)

The owner's decision after the object controls audit (OBJECT_CONTROLS_AUDIT, finding 7): a card among the Effects
tab's sources (a card kept on the table or a card whose effect lasts) is a small parchment card as in the hand, with
the symbol printed on its corner and its title in dark ink (`EffectEntry.CardTile`, `LeftRail.uss`); a source a click
can use now takes the hand's playable parchment. An ability or another effect keeps its plain tile. The Hunt's
responses show pictures as well (HUNT_UX, UI-288). Test:
`HuntAndEffectPicturesTests.TableCardsAmongTheEffectsAreParchmentCardsAndAbilitiesStayPlain`.

## Character abilities are small pictures — UI-286 (2026-10-19)

The owner's decision after the object controls audit (OBJECT_CONTROLS_AUDIT, finding 4): a character's ability in the
region panel, the Characters inspector and the decision dock is no longer a text button ("★ Saruman" twice for the
Voice) but a small picture on the square gilt frame of what it does (`AbilityPictures`): a muster flag for Voice ·
recruit, the upgrade star for Voice · upgrade, the Politics track for Gimli's, Boromir's and Legolas's political
abilities, the boot or the crossed swords for the Mouth of Sauron's move or attack, the board pin for an entry, and the
ability scroll otherwise. The picture glows and pulses with the shared ChoiceGlow only while a click can use it (its
die is chosen and the board is free); otherwise it stays in its place greyed out. Its tooltip names the owner and the
ability and says "Click to use" or "Select an eligible die to use it". The controls keep their names
(`component-<key>`), so a click works as before; Pass and Discard stay action icons and other components keep their
text. Tests: `ComponentActionTests.SarumansVoiceIsTwoPicturesThatGlowOnlyWhileTheirDieIsChosen`,
`ComponentActionTests.AbilityPicturesShowWhatTheAbilityDoes`, `T22AbilityNetworkTests`, `CharacterLayoutTests`.
Since [UI-439](#every-ability-has-its-own-icon--ui-439) the picture is the ability's own icon (`AbilityArt`; an entry
keeps the board pin), and one ability's several commands side by side (Voice · recruit and upgrade, the Mouth's move and
attack) differ by a small badge (`AbilityBadges`); `AbilityPictures` and its test are gone
(`ComponentActionTests.AbilityButtonsShowTheAbilitysOwnIcon`).

## Unreachable tray fallbacks removed — UI-289 (2026-10-19)

The owner's decision after the object controls audit (OBJECT_CONTROLS_AUDIT, finding 8): text-button fallbacks that
no decision reaches any more are removed, each after its reachability was traced in the client and the server
(base game, Lords, Kings and Fate content, the demo CPU):

- `CardChoices.Event` (the card title, "Use as" and nation rows): every Event is played from its hand card with the
  tray's `DirectCard`, so the tray returns before it; the use is the board's `CardCostPicker`, the nation the
  highlighted nation.
- The `ArmyFigure(s)` and `CompanionDestination` rows: those presentations come only from event steps outside any
  battle or Hunt, so `CardFigureDraft` always hands them to the region panel and the map (UI-215).
- `EffectFigurePicker` (figure tabs by nation): battle figure choices are the battle's own figures, chosen in the
  Battle window (UI-245); no other step offers six or more figures in the tray.
- `AttackChoices`, `ArmyChoices`, `EventArmyChoices` and `EventArmyPlans`: an army move or attack is always made on
  the map and in the region panel (`DirectArmy`, `CardArmyEvent`); ruler tokens no longer pay for armies.

`ExcessChoices` stays: an excess opened by a battle's retreat, advance or landing is chosen in the Battle window.
UI-432 (2026-10-31) removed the next fallbacks — the dice, decks, kings and the Hunt's dice of the decision window
([below](#declines-and-window-lists-follow-the-board-rule--ui-432)).
Tests: the layout fixtures of `CardPack02LayoutTests` and `Shadow02LayoutTests` now offer the battle's own figures as
the server does; `CardFigureDraftTests`, `DecisionRoutesTests` and the army and card network tests are unchanged.

<a id="plynne-przejscia--ui-321"></a>
## Smooth transitions; jumps only under Skip animations — UI-321 (2026-10-24)

The owner's rule for every animation: "we want smooth transitions, as few quick jumps as possible; the jumps are
available as an option, skip animations".

- **The setting.** The interface setting formerly "Reduced motion" reads **Skip animations**: "Every change happens at
  once, without animation. Choices always appear immediately." The preference and its element (`reduced-motion`) are
  unchanged. Without it every transition is smooth; with it every change happens at once.
- **Opening.** A panel, window or part of a window that comes into view fades in over 0.18 s (`Appearance.Display`,
  Wotr.uss `.appear-host`/`.appearing`). Only its opacity changes, so its place, size and picking are its own from the
  first frame. It applies to:
  - the left-rail panels (Politics, Reserve, Characters, Effects), the Fellowship panel, the Hunt window and its drawn
    tile;
  - the battle window, the decision window, the card view, the board menu, the confirmation dialog, the game-end
    screen, the Log, the chat, the resource and Companion setup panels, and the Victory panel.

  A panel's own slide (the left rail's drawer) stays.
- **New marks.** The presentation's plates, its "At war" caption and the Skip medallion fade in as they join
  (`Appearance.Arrive`).
- **Unchanged.** A choice the player can make is never hidden behind an animation (it is shown and pickable from the
  first frame).
- **Follow-up (UI-322).** Closing fades, changing numbers and die faces, and figures fading out of a stack
  ([below](#plynne-zamykanie--ui-322)).

<a id="plynne-zamykanie--ui-322"></a>
## Smooth closings, changing numbers and leaving figures — UI-322 (2026-10-25)

The rest of the owner's rule from UI-321. Without Skip animations none of these elements disappears or changes in one
frame; with it, and in a quiet view (reconnect, change of side, a replay's manual step), every change happens at once.

- **Closing.** A panel, window or part of a window that closes fades out where it stands over 0.15 s and only then
  leaves the layout (`Appearance.Display`, Wotr.uss `.appear-host.closing`). It counts as closed at once
  (`Appearance.Closed`, which the left rail's tabs, the window family, the placement of windows around occupied panels,
  the board's middle, the Politics presentation and the exit lock read instead of its display), its controls take no
  press, release or click while it fades, and opening it again during the fade keeps it. A left panel that starts to fade
  no longer narrows the middle, so the windows standing there glide back at once (`Appearance.ClosingEvent`). The exit lock's grey layer gives the screen its input
  back at once and fades out over it.
  An element holding a filtered one (a presentation glow, a hit's drop shadow) leaves at once, because Unity's renderer
  fails on a fading element with filtered children. It applies to every element UI-321 fades in.
- **Numbers.** A changed count swells and lights for 0.32 s while its new value already stands (`ValuePulse`, Wotr.uss
  `.value-pulse`): the opponent's counters, the deck counts, the Hunt Box's Eyes and free dice, the reserves, the
  Fellowship's progress and corruption, the hand and Effects tabs, the pool's spent, Hunt and out dice, and a battle
  stack's count. A first value never pulses.
- **Die faces.** A die whose face changes where it stands (rolled, turned by a Ring or a card, unrolled again) crossfades:
  the new face fades in over the old one, which fades out and goes, in the own dice tray and the opponent's dice
  (`ActionPoolView`, `Appearance.Crossfade`). A die that joins the pool shows its face at once.
- **Leaving figures.** On the map a marker whose figures all leave fades out (UI-338). In the live battle window a figure
  stack that leaves fades out where it stood (a renamed `leaving-…` copy, so no query finds it) and its neighbours close up
  after it; a stack whose count changed pulses (`BattleStackMemory`, `BattleContext`). The presented opponent round keeps
  UI-337's hits. A view drawn twice in a frame compares with what was on screen before that frame (`ShownBefore`).
- **The map's armies use no filter.** A blurred element on an army marker (UI-334's flare, the soft emergence of UI-334 and
  UI-338) made Unity's renderer fail (`RenderTreeCompositor`) when the panel's scale, size or target changed, so the flare
  is the soft glow texture and the figures emerge and leave in opacity only. The flare joins a marker only while it shows.

Tests: `AppearanceTests` (a closing panel fades out but is closed at once and takes no click; Skip closes at once; a
closing left panel no longer narrows the middle),
`ValuePulseTests`, `DieFaceCrossfadeTests`, `BattleStackFadeTests`, `ArmyEmergeTests`, `ArmyLeaveTests`,
`FellowshipTrackLayoutTests`, `Shadow02LayoutTests`; the "is closed" checks of `PoliticalPanelTests`,
`OpponentRecruitmentPoliticsTests`, `WindowFamilyNetworkTests`, the edge-panel tests and the profile drivers read
`Appearance.Closed`.

<a id="nazwy--ui-331"></a>
## Names, never codes — UI-331 (2026-10-25)

Owner rule for the whole application: "we should not show this kind of code in the logs or notifications, only
human-friendly names" (a Log entry read `base.sh.character.04: base.hunt.special-sh.1.1.1 is in play.`).

- No identifier is ever shown to a player. A recorded or server text is read through `ReadableText` wherever it is shown
  (the Log's rows and their tooltips, the opened action card, the battle report's details, the result lines of the
  pennants and their tooltips): a card reads as its title, a character or sovereign as its name, a Hunt tile as its kind
  and face ("Shadow special tile 1"), a unit as its nation and kind ("Gondor Regular"), a region or nation as its
  printed name, `FreePeoples` as "Free Peoples". The screenshot's entry now reads "Give it to Uss!: Shadow special
  tile 1 is in play."
- An identifier the client cannot name reads as what it is ("a card", "a Hunt tile", "a character"), never as its
  code; `CardLibrary.Title` and the Hunt report's source and region fall back the same way.
- UI-346 (expansions): a standard action die (`FreePeoples.standard.2`) reads "a Free Peoples action die"; the Ruler
  dice "the Free Peoples/Shadow Ruler die"; a Lords special die "the Balrog die"; a Keeper Ring by its name ("Vilya");
  a Ruler token by its sovereign ("Dáin Ironfoot's Ruler token"); an ability id by its printed title ("Great Sorcery",
  from `CharacterAbilityLibrary`) or, for the commands' own ids, by what it does ("Use the Balrog", "Fling into the fray
  (recruit)") (`ExpansionLabels`). Tests: `ReadableTextTests.TheExpansionsDiceRingsTokensAndAbilitiesReadAsTheirNames`,
  `ARulerTokenReadsAsItsSovereigns`.
- Guards: an EditMode test reads every recorded text of the wire fixtures and fails on any code; a network test scans the
  rendered Log and its opened card.
- Server follow-up QUAL-21: the server records its texts with names, so an installed client without UI-331 reads well too.

## Ruler tokens, Dismay and Far-sighted on their objects — UI-276 (2026-10-27)

The owner's decisions EXP-15 D7 and D18–D20 ([EXPANSIONS_UI_AUDIT](EXPANSIONS_UI_AUDIT.md)); the Dice & tokens panel is
deleted, since no resource decision is made in it any more (UI-264's plan).

- **Dismay (D20, D7).** After the roll the Shadow spends Dismay by clicking a glowing Ruler die alone — the Free
  Peoples' in the opponent's pool, or the Shadow's own: it pulses with the expansions' lit art (UI-433, 2026-10-31,
  owner's report "nothing is lit": in the top bar the lit art alone read as unlit, so the opponent's offered die also
  wears the shared gold ChoiceGlow and pulses, and the hint reads "Click the glowing Ruler die in the top bar to reroll
  it with Dismay."; [UI-434](#every-object-a-hint-asks-to-click-visibly-glows--ui-434), 2026-10-31: the Shadow's own die
  glows in its pool too, and the hint names where each offered die stands — "Click a glowing Ruler die, yours or the top
  bar's, to reroll it with Dismay." when both are offered), and its tooltip names the token it
  spends ("Click to spend Théoden's Dismay token and re-roll this Ruler die"). Which token is spent makes no difference
  (KINGS, `Kings/KingsDismay.cs`), so the first one offered is picked (`DismayChoice`); the log names it. The Dismay
  tokens in the Shadow's pool are information. Keeping the tokens is the console's Pass ("Pass · Keep your Dismay
  tokens"), and the console's hint ends with it; no button, panel or window remains.
- **Ruler tokens (D19).** A Ruler token is a crowned coin (`graphicalassets/ui/src/tokens/ruler-token.svg`, replaceable
  art) with its king's nation emblem: blue while the Free Peoples hold it and lit while a die can use it, red for the
  Shadow's Dismay tokens. The usable token stands in the dice pool; the king who holds it wears it as a small badge on
  his Characters portrait, whose tooltip says the token stands in the pool.
- **Far-sighted (D18).** Thranduil's ability is his move picture, offered while a die that can pay it is selected;
  the standard move simply does not use it — no toggle or mode chip.

Tests: `ResourcePanelTests.DismayIsTheRulerDiesClickAndKeepingIsThePass`, `RulerTokensAreCoinsWithTheirKingsEmblem`,
`ExpansionDraftTests.ADismayTokenIsPickedByTheRulerDiesClick`, `SovereignPanelTests.AKingHoldingHisRulerTokenWearsItsBadge`,
`ComponentActionTests.FarSightedIsThranduilsPicture`, `ResourcePanelNetworkTests` (Dismay on the opponent's die, Dismay
before Lore), `ExpansionDecisionNetworkTests` (Dismay), `ExpansionWindowNetworkTests` (Keep tokens through Pass),
`ExpansionTokenNetworkTests` (Far-sighted, Ruler tokens); test-menu scenarios `kings-dismay`, `kings-far-sighted`,
`kings-fp-ruler-token`.

## The Kings cards on their objects — UI-278 (2026-10-27)

The owner's decisions EXP-15 D15–D16 and decision 17 of 2026-10-19 ([CARD_CHOICES_UX](CARD_CHOICES_UX.md),
[BATTLE_WINDOW_UX](BATTLE_WINDOW_UX.md)): the Kings Nation step is the nation tokens in Politics; Wisdom of Elrond's and
the recruit-or-cleanse cards' branches stand on the nation tokens, the kings' portraits and the king's nation in the
Reserve, and a branch's object carries into the card's next step, so one click on Gondor's token activates and advances
Gondor; Keep tiles is the console's Pass; There Is Hope for Victory and His Power Waxes light a king who takes part in
the battle in its army, any other king by his portrait; Your Welcome Is Doubtful and the Palantír of Barad-dûr are paid
in the pool with the Ring on its socket; the Palantír's Muster-as-Event starts at its card on the table. No Kings card
decision opens the generic window.

## The expansions' texts name their subject — UI-365 (2026-10-27)

Owner's report: Gimli's Dwarf of Renown read "Any die in Iron Hills or Ered Luin moves the Dwarves a step toward war",
as if the die stood in the region. Every expansion text the client writes (ability lines, tooltips, console hints,
notices, the rules strip) now follows one rule: a condition names its subject and place ("With Gimli in Iron Hills or
Ered Luin, any die moves the Dwarves a step toward war"), a lit control says what its click does, the game's own terms
are used (Awakened sovereign, Council token, Ruler die, Hunt Pool) and no engine word ("server", "decision") reaches a
player. An expansion ability line may have 85 characters (the base game's keep 72). The audit also corrected texts
that did not match the rules: Narya's line and Keeper note (Gandalf outside the Fellowship in an unconquered Free
Peoples Settlement; the Ring is given and a die spent), Nenya's (a standard Eye only), Vilya's (Elrond may keep the
die), Strider's Guide (the Fellowship's first move), Sméagol's departure (the Fellowship declares), Evil Knowledge
(no Elite downgraded), Spear of Terror (it replaces any Palantír draw), the Sequential Corruption variant (the Shadow may
stop; the last tile is kept) and the Council variant (only alternate Companions start outside the Fellowship).
Tests: `CharacterAbilitySummariesTests`, `NamedRingsTests`, `SovereignPanelTests`, `BattleConclusionTests`,
`PoliticalPanelTests`.
<a id="one-window-one-glow-named-dice--ui-371-ui-373-2026-10-27"></a>
## One window, one glow, named dice — UI-371, UI-372, UI-373 (2026-10-27)

Owner requests of 2026-10-27:

- UI-371 — a character's window is one window in one place: it is placed beside its source when a figure is opened or
  the roster moves; switching an ability's text or a page keeps its top-left and only pulls it back inside the board
  (`CharacterRosterPanel.PositionInspector`, `KeepInspectorInView`). A version a decision selects is not the player's
  change: the window still moves clear of the panels that decision opens, such as a Ruler's Reserve. Since
  [UI-436](#a-characters-window-is-compact-shows-its-place-as-a-picture-and-opens-in-one-place--ui-436) this holds for
  a window opened from a region's window; one opened from the Characters tab, its panel or a ruler stands at one place
  beside the tab (`CharacterWindowPlace`), clear of every left panel. Since [UI-440](#skill-icons--ui-440) (2026-11-02)
  no click on an ability changes the window's content (no text unrolls), so the kept place and `KeepInspectorInView`
  are gone.
- UI-372 — every choice and legal target uses one highlight, the ChoiceGlow gold (the tabs, the Fellowship's portraits,
  the map's units): the old pale blue (mint) `.legal-target` border and its copies (playable hand cards and effects,
  the playable preview, Hunt candidates, decks, the Hunt tab, the opponent's dice, the roster grid, the Reserve rows)
  are gold, a framed button adds the warm plate, and inside a pulsing host it breathes with the other choices. A
  usable Click to use (UI-369) wears the ChoiceGlow. UI-378 (owner, 2026-10-27): the opponent's presentation and history
  marks (the observed trace, rings and light, a revealed card's used half, a presented die, the map's observed
  outlines, a planned group's portrait, the recorded map label and the history host) turn gold as well; they are not
  choices, so they never wear the plate or the pulse. Side colours (the Free Peoples' blue against the Shadow's red)
  stay.
- UI-373 — a button that spends a die names the die and what it does: the decision tray's ✓ (and the console's, which
  reads it) and Click to use read "Use your Muster die: <the server preview's summary>" (`DieUse`); before the
  preview the action's name stands for the effect, and without a die the effect stands alone.

<a id="balrog-order-and-separation-hints--ui-374-ui-375-2026-10-27"></a>
## The Balrog's glowing region, a separation's region hint — UI-374, UI-375 (2026-10-27)

Owner reports of 2026-10-27 (playtest of the expansions' test scenarios):

- UI-374 — A Balrog is Come! said "Click the Balrog's glowing region" while nothing glowed and a click on Moria did
  nothing (only Pass worked). The army of a figure whose ability or order a click uses (`ability-owner`: the Balrog's
  activation, the Chief, A Balrog is Come!) now wears the ChoiceGlow halo over its gold plinth and pulses with the
  other choices ([ARMY_MAP_UX states](ARMY_MAP_UX.md#stany)); a click on the Balrog's region (its contour or its
  marker) opens his panel also while the card's ChooseEffect draft stands (`FigureAbilityInteraction.Region`), where
  his figure moves him alone and his army's figures move it with him. Test:
  `LordsCharacterNetworkTests.TheBalrogsCardMovesHimAloneAlongHisPath` (glow, map click, lit figure).
- UI-375 — separating Gimli as the Guide said "Select the highlighted characters, then confirm." when only his region
  was left to choose. Once a separation's or a companion move's figures are chosen, the console names them and asks
  for the region, as the Fellowship panel does: "Gimli · Click a highlighted region to move there."
  (`TravelDraft.DestinationHint`). Tests: `CompanionJourneyMapTests.AChosenSeparationAsksForARegion`,
  `FateGimliNetworkTests.GuideLeavesDuringHuntAndMovesWithThePrintedRange`.
- AUDIO-12 — the political token's step sounds a deep bell ([AUDIO_UX](AUDIO_UX.md#dzwon-polityki--audio-12)).

<a id="council-token-uses-on-their-objects--ui-376-ui-377-2026-10-27"></a>
## A Council token's uses on their objects — UI-376, UI-377 (2026-10-27)

Owner requests of 2026-10-27; the token's printed use still starts on its medallion in the pool (UI-347 D13), and
neither use opens the decision window any longer (`ExpansionChoices` lists nothing for `UseCouncilToken`).

- UI-376 — the token that advances a Shadow nation opens the Politics panel as its use starts; the console says
  "Council token · Click a glowing step of a Shadow nation on the Politics track." (since UI-423: "Click a glowing
  Shadow nation's token or its → arrow in Politics", as UI-410 removed the glowing station). If the player chooses anything
  else (another token, Back, or one of the own action dice, whose click now leaves the token's use for that die
  instead of being ignored), the panel the token opened closes at once; a panel that was already
  open stays as it was; if the player closes it, it stays closed and its tab keeps glowing until the use ends. The
  step's own presentation keeps the panel (`PoliticalContextInteraction.FollowCouncil`).
- UI-377 — the token that moves Nazgûl shows a small popover just above the token pool, pointing at the token on the
  Ring picker's frame (`CouncilTravelPicker`): only the icons of the Nazgûl it may move and the ✓ that ends the
  choice (UI-379's Finish; since UI-381 the console's ✓). Its planning is UI-379's, below.
- UI-379 (owner, 2026-10-28) — the token's Nazgûl move group by group. Rules: the token is the Character die's Move
  Nazgûl and Minions (T26, LORDS p. 30–31): any or all of them, each to its own destination, in one action; the
  server's `ShadowTravel.Plan` accepts any subset, grouped by origin. A click on a lit icon, on the figure in its
  region's panel, or on a region holding only that figure chooses it for the group, and a second click releases it;
  the group is local and never sent. A click on a lit destination (a region every chosen figure reaches, not one's own)
  plans the group there: one planned group per origin, garrison and destination, drawn faintly with its count at the
  destination (`PlannedFlights`), and the next group can be chosen; a planned icon dims, and its click gives up its
  destination and chooses it again. The console's ✓ (UI-381; UI-379's Finish above the token is gone) sends every
  planned move as one command; planning every figure the token may move sends them at once. The console says "Council token · Click the
  Nazgûl to move (above the token or on the map), then their destination.", "<Nazgûl ×2> · Click a highlighted region
  to move there; click a chosen one to release it." and "… planned · Choose more and their destination, or click ✓ to
  finish." (`CouncilTravel`, `CouncilTravelInteraction`; the general travel path, `TravelActions`, no longer handles the
  token).
- Tests: `ExpansionTokenNetworkTests.CouncilPoliticsSpendsTheTokenWithoutADie` (opening, closing on a die's click,
  staying closed with a glowing tab, the step), `ExpansionTokenNetworkTests.CouncilTravelMovesTheChosenNazgulWithoutADie`
  (no decision window, the popover above the pool with only the Nazgûl and Finish, a group of two chosen and a third
  released, the planned ghost without a command, Finish's move of both), `CouncilObjectsTests.TheCouncilTokensTravelPlansGroupsUntilFinish`.

<a id="ruler-token-way-console-confirm--ui-380-ui-383-2026-10-28"></a>
## A Ruler token's way, the console's ✓ for board choices — UI-380 to UI-383 (2026-10-28)

Owner reports from the Kings test scenarios (Brand's Call to Arms, Dáin's Iron Guard):

- UI-380 — a Ruler token's click takes the Ruler die when it is the only die the token's actions use (the token's
  actions pay with it; before, the click selected nothing usable). The Characters tab glows while the roster offers
  something — an entry, an action on a row, a king's decision — so the way is lit before the panel opens; then the
  king's row glows and, in his window, the usable ability's chip. An expansion's command names its ability with a
  prefix ("kings.call-to-arms") while the chip has the printed id ("call-to-arms"): the action now belongs to its chip
  (UI-369), so Click to use appears only while that chip is open, and the other ability (Heir of Bard) shows no button
  under Limits. Test: `RulerTokenMenuTests` (Brand and Dáin from the test menu).
- UI-381 — the owner's rule: "in this and every analogous case the button in the bottom panel confirms, not an extra
  button in the window". A choice planned on the board's objects — a card's or ability's recruitment plan in
  Reinforcements, a Council token's Nazgûl (UI-379's Finish above the token is gone), an ability's decision in the
  Characters window — is confirmed with the console's ✓ once the server's preview allows (`ConsoleConfirm`); the
  Reserve has no ✓ or Finish of its own (the console's ✓ also finishes a recruitment sequence, as for every action of
  several steps), and the tray keeps its confirmation hidden there. The decision window, the battle and Hunt windows
  and the Council setup's ✓ beside Finish phase (UI-370) are unchanged. Tests: `ShadowStrategy04NetworkTests`,
  `ShadowStrategy04LayoutTests`, `BoardInteractionTests`, `ExpansionTokenNetworkTests`, `ExpansionAbilityNetworkTests`.
- UI-382 — a separation's (or journey's) portraits in the Fellowship panel take the size of its companion icons
  (54 × 44, 46 × 34 compact) instead of the tray's 66 × 48. Test: `FateGimliNetworkTests`.
- UI-383 — a corrupted king's tooltip and inspector say "Corrupted · His tiles are resolved; he now serves the Shadow"
  (T28, KINGS p. 12–13) and a removed one "Removed from play", instead of "Corruption 0 / 4". Test:
  `SovereignPanelTests.ACorruptedOrRemovedKingShowsNoCorruptionCount`.
- UI-384 — every rail tab glows alike: Politics and Reserve are framed buttons (`.action`), whose UI-372 choice plate
  repainted them lighter than the Characters tab; every tab keeps the tabs' warm plate and gold rim (UI-223), the look
  the owner named as the reference, and pulses the same way.
- UI-385 — a king's ability names him and the printed ability: its owner is his name from his title ("Dáin"), never
  his id ("kings.sovereign.dain"), and an ability's picture reads "Dáin · Dwarvenking · Upgrade a Regular · …"
  (`ComponentActions.AbilityTitle`). Test: `ComponentActionTests.AKingsAbilityNamesTheKingAndThePrintedAbility`.
- UI-386 (owner) — a second click on the chosen action die releases it, and with it the unsent step it began (the
  draft clears, as Back to the start); before, it chose the same die again. The Hunt's allocation, a reroll and the
  special dice's choices keep their own die clicks. Test: `RulerTokenMenuTests.EveryLitTabGlowsAlike` (the tabs the
  die lit go dark).
- UI-387 (owner) — a character's versions are tabs right under the window's title, not large framed buttons below its
  abilities: a low strip on a thin gilt line, the shown version with a gold underline and gold text, the others muted
  until hovered; each tab reads the version's name ("Strider", "Aragorn"), with its edition only where two names are
  alike ("Base · Boromir", "Lords · Boromir"); the tooltip names the card and its edition. UI-438 (2026-11-01): one
  character's alternative cards read their epithets ("Lieutenant", "Númenórean"; "Black Captain", "Chief"; Fate's
  "Dwarven Lord"), `CharacterLibrary.VersionName`.
- UI-388 (owner) — a token with several uses opens no panel and lights them all. A Council token a Kings Chieftain can
  enter with (UI-347 D13) lights, at its one click, the Chieftain's row (and the Characters tab) for the entry and its
  printed use's objects: the glowing Shadow nations' steps (and the Politics tab) for Politics, the Nazgûl above the
  token for Travel; a click on any lit option uses the token that way (a step sends Politics at once, a Nazgûl starts
  Travel), and a second click on the token releases it. The console names both ways. A token with one use keeps UI-376
  (Politics opens with it). Tests: `ResourcePanelTests`, `ExpansionTokenNetworkTests` (both tokens),
  `CharacterRosterActionNetworkTests`, `CouncilTokenMenuTests` (UI-376 with `lords-council-token`).


## Every use at one click, a deck choice on its decks — UI-389, UI-390 (2026-10-28)

Owner reports from the Kings Ruler token scenarios:

- UI-389 (owner's rule) — every die and token lights all of its available uses at its click, never one use per click.
  A Ruler token's uses are the server's actions for that token (`RulerTokenActions`), judged by the token's face
  rather than the face the Ruler die shows (`ActionDieContext.Sync` with the token): Thranduil's token lights the
  Fellowship's move (its plaque and its figure on the map) beside his own row, where before only his row lit; Denethor's
  token lights the decks and the Event cards its Event face pays for. None of the Ruler die's own uses light beside
  them (before, the Muster die's Politics glowed under both tokens). A separation lights the Fellowship as its move
  does. A second click on the chosen token releases it with the Ruler die it took, as a die's (UI-386); before, it
  dropped only the token and the die's own uses (another king's ability, Politics) lit in its place. Every path that
  starts a token's action — the Fellowship panel, the map, a chip — sends the token's ability (Far-sighted), which the
  server requires (`ExpansionDraft.TokenAbility`). Tests: `RulerTokenUsesTests`, `RulerTokenMenuTests` (Thranduil's and
  Denethor's tokens from the test menu; the Fellowship's move with Thranduil's token is taken by the server).
- UI-390 — a deck choice is made on its decks. After Denethor's token (or Ruler die) pays for an Event, the Palantír of
  Anárion's extra card asks for a deck, with no Skip while his token is spent. Before, the console said "Resolve the
  played card" beside the played card's chip, the decks beside the hand kept only a thin rim (an expanded hand card
  could cover it), the console's plaque and the notice opened the army window in Minas Tirith where Denethor stands, and
  his window showed an Open decision that led back there. Now the decks wear the same halo as a die's draw, the console
  and the notice read "Denethor · Choose a deck" and "Click a glowing deck beside your hand to draw its top card" (with
  Pass named when the draw may be skipped), the notice leads to the hand (`DecisionContext.Hand`) — the docked hand
  shows its decks already, so the plaque has nothing to open — a floating hand drawer opens once for the step, and the
  character keeps no Open decision link (`DeckChoice`). Tests: `DeckChoiceTests`, `DeckChoiceMenuTests` (Horn of Gondor
  with Denethor's token from the test menu). Since [UI-434](#every-object-a-hint-asks-to-click-visibly-glows--ui-434)
  (2026-10-31) the offered decks also wear the thick gold ChoiceGlow rim and breathe: the halo and the thin rim alone
  read as unlit.

## Abilities on their rows, recruitment from the map, a lifted figure — UI-391 to UI-397 (2026-10-29)

Owner requests and reports from the Kings and Lords test scenarios:

- UI-391 (owner) — a character's abilities are used straight from the Characters tab. The third line of each roster
  row holds a 15-point line of small thematic icons, one per printed ability, right-aligned away from the portrait's
  level star (`AbilityIcons`: a battle for a combat bonus, the Guide's crown, the Politics track for a political step or
  a messenger, a muster, a move, a card draw, the Hunt's eye or die, a Ring, the Balrog, Gollum, a plus for an upgrade,
  a level, a change of version, a bond to a place; the scroll otherwise). The line keeps its height whether a character
  prints two abilities or four, so every row keeps its size; on a short viewport the icons take the status line's
  place and the row's tooltip keeps the state and region (a strip under the rows made a full roster too tall for the
  panel). An icon's tooltip names the ability and its text. An ability usable now glows with the shared gold rim and
  pulses with the roster; its click starts it as the chip's Click to use does (UI-369), with the same die or token, so
  Dáin's Iron Guard starts from his row after his token. Two usable actions of one ability (Saruman's Voice), or an
  ability not usable now, open the character's window with that ability unrolled. The window keeps its chips
  (`CharacterAbilityStrip`). Since [UI-440](#skill-icons--ui-440) (2026-11-02) the icons are bare, their tooltip is the
  skill's formatted description, and the window opens with that ability's icon focused (its description shown) instead
  of unrolled. Tests: `CharacterAbilityStripTests`,
  `RulerTokenMenuTests.DainsIronGuardStartsFromItsIconInTheCharactersTab`.
- UI-392 (owner) — a king's recruitment (Iron Guard, Call to Arms, Mustering of the Mark) starts on the map: the
  Reserve does not open by itself; its tab glows and can be opened by hand, and the target settlements glow green. A
  click on a lit settlement remembers it, opens the Reserve for it and glides the camera there at the same zoom
  (`MapCanvas.Glide` over `CameraTravel`, 450–750 ms eased; at once under Reduced motion; the player's own pan or zoom
  stops it). The chosen settlement wears a wider gold edge that pulses with the green targets and its fill turns to the
  planned colour, until its figure is placed (`MapDraftOverlay.ChosenRegions`). The next figure's click places it
  there. The Balrog's optional figure keeps its panel. Tests: `ReinforcementPlansTests`, `RulerTokenMenuTests`.
- UI-393 (owner) — a reserve figure is dragged onto a lit settlement as its own icon: the pressed figure's art follows
  the pointer, centred and a little larger, and its slot keeps only a faint trace while it is lifted; a drop on a legal
  settlement (or on the army standing there) places it, a drop elsewhere returns it with no action (UI-113). Other drag
  sources keep their label. Test: `RulerTokenMenuTests` (the second Iron Guard figure).
- UI-395 (owner) — a response made only in the console lights only its choices. Lady Galadriel's Nenya after a Hunt
  Eye is its Ring socket and Pass: the Hunt window holds nothing then, so the Hunt plaque no longer glows, the decision
  belongs to no window or region, and the console's banner neither pulses nor opens the Hunt window
  (`HuntActions.ConsoleReaction`). A table card's or a Guide's response beside Nenya keeps the Hunt window. Tests:
  `DecisionAttentionTests.ANenyaResponseLeadsToNoWindow`, `ExpansionWindowNetworkTests` (t34-nenya).
- UI-394 (owner) — the battle window's two sides keep one width in every step; a long figure choice wraps inside its
  column (`docs/ui/BATTLE_WINDOW_UX.md`, UI-208). Test: `BattleLayoutTests.LossesAndRerollsStandOnTheDecidingPlayersSide`.
- UI-396 (owner) — a Combat card's replacement (Heir of Bard, Shadows in the North, a Dark Chieftain's) can be skipped
  from the battle window: Keep card effects stands in its footer on the deciding player's side and sends the decline at
  once, as the console's Pass still does; the footer's hint names the chip's click and the skip. Tests:
  `KingsCards02Tests`, `KingsDecisionNetworkTests.KeepingTheCardsEffectsIsThePass`.
- UI-397 (owner) — an ability shown as a marker or chip in the battle window is described: a made replacement's marker
  reads its source, the ability (Red Wrath, Lord of the Bats, I Command, Heir of Bard, Shadows in the North), its printed
  text and what it replaces this round; an offered chip ends with the same text (`CombatReplacementChoice.Ability`).
  Test: `KingsCards02Tests`.
- UI-398 (owner) — a corruption attempt starts from its Eye: before, the attempt button beside an Initial king paid a
  standard Shadow Eye by itself (UI-274), so the attempt and the Characters tab glowed before any die was chosen. Now
  the Eyes that may pay stand in the Shadow's dice row (UI-230); the click on one lights the kings it may target, their
  attempt buttons and the Characters tab, and the attempt's click sends it with that Eye. Until then the buttons stay
  dark and say "Click an Eye that pays in your dice row first". Tests: `SovereignPanelTests`,
  `ExpansionCorruptionNetworkTests`.
- UI-399 (owner's question) — a standard corruption attempt's Draw tiles is one click. Under T28 it draws min(3,
  remaining Eyes) tiles, an Eye or special tile is revealed, set aside and replaced, and a Kings tile ends the draw: no
  choice falls between the draws, so after the click the client sends each next draw by itself, about half a second
  after the previous tile shows, until the limit or a Kings tile; then the choice of a tile waits (`CorruptionDrawChain`).
  Each draw stays the server's own command, and opening the game still draws nothing (UI-12). The Sequential variant
  keeps one click per draw, as each is a choice. Tests: `ExpansionCorruptionNetworkTests`, `KingsCards02NetworkTests`
  (`KingsCardNetworkChecks.DrawTiles` clicks once), `SovereignPanelNetworkTests`. Superseded by
  [UI-442](#a-standard-corruption-attempt-draws-all-its-tiles-with-its-click--ui-442): the server draws every tile in
  the attempt's own command, so the standard attempt has no Draw tiles and `CorruptionDrawChain` is gone.

## Two characters a line, clear character tooltips, a dark Politics station — UI-408 to UI-410 (2026-10-29)

- UI-408 (owner) — each character in the Characters tab has more room: two rows a line, each about twice as wide,
  with the portrait, the name and region, and beside them a 2x2 block of 19-point ability icons (17 on a short
  viewport; UI-391's were 15 points on the text's third line). A roster that two a line would make taller than the
  board area — the full Kings roster on a 1366 x 768 screen — keeps three a line with UI-391's icons
  (`roster-compact`); the panel tries two a line again only when the roster or the room for it changes, so the form
  never flips back and forth. Since [UI-445](#the-full-expansions-roster-fits--ui-445) a roster too tall even then takes
  one more column (`roster-dense`). (First registered as UI-400, a number another session's registration took over.)
  The block holds two lines of two icons one above the other and never wraps, so its form does not depend on the UI
  scale; every icon stays inside its row (`CharacterRosterLayoutTests`).
  Tests: `CharacterRosterPanelNetworkTests` (two a line, icons of 17+ points; `RulerTokenMenuTests` with Kings),
  `CharacterRosterLayoutTests`, `LeftRailLayoutTests`, `CharacterAbilityStripTests.EveryAbilityIconsArtExists` (since
  [UI-439](#every-ability-has-its-own-icon--ui-439) each icon is the ability's own, `AbilityArt`, and `AbilityIcons` with
  that test is gone: `AbilityArtTests`).
- UI-409 (owner) — a roster row's tooltip reads in clear parts without growing (`CharacterRowTooltip`): the click's
  offer in bright gold, the name in bold gold with its edition, the state dim, the statistics plain, the entry in gold
  with its cost on the same line and its conditions dim under it (✓ met in green, ○ open), the source small and faint,
  each ability with its name in bold; two thin gaps part identity, entry and abilities, and the merged entry line keeps
  the height. Only a source with `RuntimeTooltip.FormattedClass` shows rich text; every other tooltip stays plain.
  Tests: `CharacterRowTooltipTests`, the roster tests reading `CharacterRowTooltip.Plain`.
- UI-410 (owner) — no station on a Politics track glows: the lit green circle on the next station misled. The
  nation's token and its arrow show the step; the next station stays the drop target of a dragged token and shows a
  white rim only while the drag points at it. Tests: `PoliticalGestureTests`, `PoliticalPanelTests`,
  `PoliticalRecoveryTests`, `MusterNetworkTests`, `MultiUseDiceNetworkTests`, `T22TurnNetworkTests`.
- UI-411 (owner) — a Corruption tile that can be chosen (an attempt's drawn tiles, an inspection's tiles) pulses
  visibly: besides its fading gold rim it grows a little in the shared 750 ms rhythm, as the roster's choices do; Reduced
  motion keeps it lit and still. Test: `SovereignPanelNetworkTests` (the tile's panel pulses during the choice).

## A settlement's own recruits, clear Politics names — UI-412 and UI-413 (2026-10-29)

- UI-412 (owner) — with a Muster die chosen, a clicked settlement lights in the Reserve only the figures it can take:
  the kinds whose recruitment targets (the server's `RecruitmentOption.Targets`) hold that settlement stay lit, and so
  do their nations' rows. The others lose their light but stay choosable, as before, for another settlement; their
  tooltip says why ("Cannot be recruited in Erebor now", or the server's words for a closed settlement) and that their
  click lights the settlements that take them. The header reads "Reserve · Erebor". Another settlement moves the focus,
  a region without a settlement clears it, and a committed recruit clears it too, so the next figure sees every
  settlement again. The narrowing follows the targets, not the region's nation: Helm's Deep under the Mark of the White Hand
  lights Isengard's figures. The click flow is unchanged (a figure's click still lights its settlements).
  Test: `RecruitFocusMenuTests` (the Corrupted kings scenario: Erebor, the Iron Hills, Dale, the Old Forest).
- UI-413 (owner) — the Politics rows carry no effect badges: the small diamond a nation-wide effect (Wormtongue on Rohan,
  a Corrupted Brand or Théoden) placed at the start of a row covered the nation's name. The Effects tab still lists
  every effect; the Fellowship's badge keeps its place (`EffectsScopeBadges`; the global one beside the Effects
  window's close button was removed by UI-429, 2026-10-30). Test:
  `EffectsNetworkTests` (no badge on the Politics rows while Wormtongue is in play).
- UI-414 (owner) — the Effects tab lists passive effects and what is happening now (tiles entering the Hunt Pool, a
  card's lasting effect); an ability a player uses is described and offered only in the Characters tab. Each titled
  ability of a source has its own button, named by the ability ("Corrupted ruler · Dáin", "Mark of the White Hand ·
  Théoden"); a card's effects keep one button. A place shows its locate button once per source, and none where the
  effect's text already names it. The server (`InGameEffectProjection`) projects each Corrupted sovereign's recruitment
  ban naming its place and Théoden's Mark of the White Hand; Brand's Shadows in the North and the Fate abilities are
  the Shadow's optional uses, offered in battle, and are no longer effects. Tests: `EffectsPresentationTests`, server
  `InGameProjectionTests.CorruptedSovereignsProjectOnlyTheirPassiveEffectsTitledByAbility`.
- UI-414 (owner) — the Effects tab lists passive effects and what is happening now; an ability a player uses is
  described and offered only in the Characters tab. Each titled ability of a source has its own button; a card's
  effects keep one. A place shows its locate button once per source, and none where the effect's text already names
  it (`EffectsPresentation`, `EffectSourceDetails`). Test: `EffectsPresentationTests`.

## A compact menu before the game — UI-415 (2026-10-29)

- UI-415 (owner) — every screen before the game (the sign-in card, the main menu's pages: Open games with the lobby, My
  games, Replays, Host a game, Demo, Test scenarios, Join by code; the waiting room; Settings) is compact: buttons of 32
  points (the project's click target) instead of 36–42, rows of 46 (Open games 40) instead of 60 with 3 points between
  them, text fields with 2 points of padding, and almost no empty space between frames, header plates, tabs and rows.
  The masthead (title 30 points, the sign-in line under it) gives its height to the window (600 points tall, shrinking
  on a smaller screen); the action column ends under Sign out and Quit game instead of stretching an empty frame. In
  Test scenarios, Back stands beside the hint and the page's second title is gone (the window's header names it). Every
  control keeps its place; no information was added or removed (`Frontend.uss`, `WaitingRoom.uss`, Settings in
  `Wotr.uss`). Tests: `MainMenuLayoutTests` (four layouts, the audience line now 48 points), `TestScenarioNetworkTests`,
  `MainMenuNetworkTests`, `InterfaceSettingsTests`.

## A move shows its own figure; A Balrog is Come! in fewer clicks — UI-416 and UI-417 (2026-10-29)

- UI-416 (owner) — a planned move shows and names the figure that moves: the map's planned-figure ghosts keep a
  character's own figure (`MapPlannedRecruits` used only nation and kind, so the Balrog's planned move showed a
  Nazgûl), and a card's character move names its figures in the console ("Move the Balrog", "Keep the Witch-king and
  3 Nazgûl in place") instead of "Nazgûl". Test: `NazgulFlightDraftTests.TheSummaryAndTheGhostShowTheFiguresThatMove`.
- UI-417 (owner) — A Balrog is Come! in fewer clicks: a click on the Balrog's glowing army moves that army with him at
  once (the card's army step opens with it chosen, UI-347); his figure in his region's panel moves him alone with him
  already armed, so the next click is his destination; a card's move of one figure sends with its destination's click
  (no ✓), as any complete final click does. Pass still finishes the card. The console's trail shows one committed
  button per card: an option of its effect joins the card's button ("✓ A Balrog is Come! · Balrog alone"); a chosen
  region, figure or die keeps its own. Tests: `LordsCharacterNetworkTests`, `EffectTrailTests`.

## One button for a card's one option; the battle window's choices pulse and skip — UI-418 and UI-419 (2026-10-29)

- UI-418 (owner) — a card decision with one option besides its decline (the console's Pass) shows one button, named
  after the card (Wind from the West), lit and pulsing, whose tooltip says what it does ("Inspect Shadow hand · …"); the
  console's trail leaves out that card's committed button, which said the same. A single reaction response is lit too.
  Tests: `CardConsoleOptionsTests`, the Lords Wind from the West scenario.
- UI-419 (owner) — the battle window's clickable pips (the −1 that cancels a hit, Heroic Death's amounts) glow brighter
  and pulse in the shared rhythm. Every optional effect of a battle can be skipped in the battle window: Skip effect
  stands in its footer whenever the console's Pass sends a decline, and its tooltip names the effect it skips and what
  that does ("Skip Orderly Withdrawal · Keep the hit and fight on", "Skip Heir of Bard · Keep your card's effects"). It
  replaces UI-396's Keep card effects. Tests: `KingsDecisionNetworkTests`, `KingsCards02Tests`.

## Tooltips say what they confirm or pass, once and cleanly — UI-420 (2026-10-29)

- UI-420 (owner) — an audit of every tooltip of the base game and the expansions ([TOOLTIP_AUDIT](TOOLTIP_AUDIT.md)). An
  action's tooltip names what it confirms or passes: the console's ✓ reads `Confirm · <card>: <chosen options>` or names
  the armies, routes, recruits and nation of the step; Pass names the card or ability it skips; Finish the action it ends;
  the battle window's direct steps and Apply losses name the region and the figures; Keep roll and Reroll selected the roll.
  Informational tooltips drop repeated facts (a Hunt tile's value, a king's state, equal Level/Move/Combat level, a status
  equal to its label), blank lines and dangling separators, raw ids and enum names; a force, a character, a deck and a
  played card read the same everywhere. Tests: `TooltipTextTests` and the tests listed in the audit document.

## Layout stress scenarios; console options above the console — TEST-15 (2026-10-29)

- TEST-15 (owner) — nine Testing-catalog scenarios fill the windows at once (the full roster with Fate and with the five
  kings, three Chieftains in one battle, a Hunt answered at every window, the Council token's twelve figures, the White
  Rider, every pool resource, the Effects panel with 22 entries, The Nazgûl Strike!'s three options), listed in
  [LAYOUT_STRESS_SCENARIOS](LAYOUT_STRESS_SCENARIOS.md) with the audit behind them. `LayoutStressScenarioTests` opens each
  in the four layouts with a screenshot.
- Console options that do not fit the console's line at their full text stand in a strip just above the console — with
  the played card's committed button when that does not fit the line either — instead of being cut to "…"; the bottom
  row keeps its height (UI-160) and wide layouts keep the options in the line (`ConsoleOptionsFit`).

## The owner's playtest fixes where the same defects remained — UI-423 (2026-10-29)

UI-423 (owner) — the expansion playtests of the last days (UI-296 … UI-419) were read as patterns of defects, and the
other places with the same defects fixed the way the first fix did:
- **A window has its own skip (as UI-419).** Skip effect also stands in the Hunt window (`hunt-skip-effect`) for its
  optional effects (We Won't Go Back's No Corruption) and reaction windows (table-card and Guide responses), and in the
  battle window for reactions (the White Rider, the Witch-king's Sorcerer); a reaction's tooltip lists "Skip <skill>
  (<owner>)" per response. The console's Pass still declines (D7); Nenya alone stays console-only (UI-395).
- **A live choice glows and pulses (as UI-411, UI-419).** Battle and Hunt reroll dice while the budget is not full; the
  Hunt's Ring, Guide and Random companion responses beside the glowing card and Guide rows; the Ring and Lore pickers'
  faces; a Politics token a click can move (a gold rim and the shared pulse, no glowing station, UI-410); a corruption
  attempt's lone Draw tiles / Draw again.
- **A highlight follows the legal context (as UI-412).** A king's region-first recruitment (Dáin's Iron Guard) lights only
  the figures the chosen settlement can take; the others stay choosable, unlit, and say why.
- **Texts name the real subject (as UI-416, UI-374, UI-375).** Politics hints name the token and its → arrow; the
  Council token's travel says "figures" when it offers Minions; A Balrog is Come!'s hint names the Balrog; card journeys
  name the companions and destinations; with two Chieftains the battle footer, Pass and Skip effect name every chip; the
  Dead Men of Dunharrow and several-figure choices name what is chosen and what is left.
- **A link leads somewhere (as UI-390).** A character's window offers no Open decision for a response made on its own
  object (Nenya at its socket, Spear of Terror at the deck, He Sees, He Knows at the Chief).
- **A drag shows the object (as UI-393).** A dragged Politics token, Council portrait, Ruler token, card or Hunt tile reads
  as its name, not as a code word.
Not changed: a single-figure card choice ("Click a lit region, then its lit figure") sends with the figure's click, so its
hint cannot advance; the corruption attempt lit on the king's portrait and his attempt button at once is an owner
question (one lit control, as UI-418?).

## The Effects window's header holds only its close button — UI-429 (2026-10-30)

The owner: the Effects tab's bar keeps only the button that closes the window; the second one, which listed the
effects, goes. That was the "◇ N" badge of game-wide effects (`EffectsScopeBadges`), placed beside the close button: its
tooltip listed what the window already shows and its click opened the window that was already open. Game-wide effects
now get no badge anywhere; the window still lists each of them as a source, and the Fellowship's badge for effects on
the Fellowship keeps its place in the Fellowship's status row. This completes UI-221 ("the header holds only the close
button") and follows UI-413 (no nation-wide badge on the Politics rows). Tests: `InGameDenseLayoutTests` (the header's
only button is `close-effects`, four layouts), `LayoutStressScenarioTests.TheFullestEffectsPanelFits` (the same with ten game-wide effects in play).

<a id="no-button-for-an-option-the-board-draws--ui-430"></a>
## No button for an option the board draws — UI-430 (2026-10-30)

The owner, about The Nazgûl Strike!'s console buttons ("✓ The Nazgûl Strike!", "Discard a table card", "Roll for the
Hunt", "Finish"): Finish disappears and the standard confirm choice replaces it; of the other buttons only the card's
name and the ones with no other picture stay; the Effects tab already lights, the discardable effects only lack a red
glow; the Hunt Box glows and pulses and its tooltip explains that a click starts a new Hunt because of this effect.
From now on this is the general rule of the design:

- **A new button only for an option with no other picture on the board.** Otherwise the map, the UI and tooltips signal
  the option — the lit object's tooltip names the action and the effect that offers it — and a click on the lit object
  sends exactly what the button sent. Every option keeps a click path (R16).
- **Finish → ✓, decline → Pass / Skip effect.** A Finish that ends a card's event is the console's ✓ (the standard
  confirm choice): enabled, lit and pulsing while nothing else is chosen, its tooltip `Finish · End <card> without …`
  (`FinishingOption`: The Nazgûl Strike!'s Finish, There is Another Way's Stay, and Wind from the West's and A Balrog
  is Come!'s Finish, which were the Pass since UI-347 D7). A decline of a response or of an optional effect stays the
  console's Pass and a window's Skip effect (UI-291, UI-419, UI-423; `OptionalDecline`, `ReactionSkip`).
- **The Nazgûl Strike!** has no console option and its decision window stays closed; the console keeps the card's
  committed chip. Its Free Peoples Character cards are red in the Effects panel (the tab lights as before), its Hunt
  roll is the console's Hunt Box — gold choice glow and the shared pulse, no filter and no size change — whose tooltip
  reads `Roll for the Hunt · Click to start a Hunt roll now because of The Nazgûl Strike!: a Nazgûl is in the
  Fellowship's region · The Fellowship does not move and no die is spent · <n> Eyes in the Hunt Box`, and its Finish the
  ✓. The hint names them within two lines of the status column at 1366×768: "Discard: red card in Effects · Roll: Hunt
  Box · ✓ finishes" ("Click the glowing Hunt Box to roll · ✓ finishes" when no Free Peoples card is on the table); each
  object's tooltip says the rest. In a Lords or Kings game at 125 % the status column is too narrow for any hint (an
  earlier limit of that layout, not of this decision). Outside this offer the Hunt Box behaves as before.
- **The card's name chip is never cut.** In the console's line the committed chips keep their share (up to 45 %) instead
  of yielding to a long hint; without options, chips that do not fit that share at their full text stand alone in the
  strip above the console (`ConsoleOptionsFit`, as TEST-15 lifts them with options) and the hint takes the whole line.
  Before, UI-297 shortened them with "…" (the Strike's chip read "✓ The Nazg…" in all four layouts).
- **A table card an effect lets the player discard** (The Nazgûl Strike!, Wind from the West, Safe Paths in the Dark, a
  table card's own reaction, UI-247) wears the hand's red discard parchment (UI-222) on its tile, and a click on the
  tile or on its × sends the same command (`ChooseEffect`, or `RespondEffect` for a reaction). Its tooltip reads
  `Discard <card> · Click to discard it because of <effect>[: reason]` (`Discard <card> · Click to discard it and use
  its reaction · …` for a reaction). The console's "Discard a table card" and "Use a table card" are gone.
- **Buttons that stay** (no other picture): the console's Back, ✓, Pass and Discard and the committed card-name chips;
  the battle window's Continue/Stop, Hold, Field/Retreat inside, No card, Stay, Select all, Skip cards this battle,
  Apply losses, Keep/Reroll and Skip effect; the Hunt window's Keep/Reroll and Skip effect; a Kings branch without a
  board object; the game end's reopen; the Elven Ring and Lore result pickers; a character's ability controls on its
  row; the Black Captain's recruit with an empty reserve; a card's one option without an object (Wind from the West's
  Inspect Shadow hand, UI-418). The other cases of the analysis (the Black Captain's orders, Lure of the Ring, Dreadful
  Spells, card army events, Worn with Sorrow and Toil, the Scouts, the Fellowship tray) are UI-431 and UI-432.

Playtest (owner's checklist):
- [ ] The Strike — Testing catalog `layout-console-options` (Lords group, play the Shadow). Expect: no option strip, the
  card's chip only; the Effects tab lit, its four Free Peoples cards red with "Discard … because of The Nazgûl Strike!";
  the Hunt Box glowing and pulsing with "Roll for the Hunt …"; the ✓ lit with "Finish · End The Nazgûl Strike! …".
  A red card's click discards it; in a new session the Hunt Box's click starts the Hunt roll, then the ✓ finishes.
- [ ] Wind from the West — `lords-card-wind-finish`. Expect: Inspect Shadow hand is the card's one lit button, the ✓
  lit "Finish · End Wind from the West without inspecting the Shadow hand", the Pass unlit; the ✓ ends the card.
- [ ] A table card's reaction — base Balrog of Moria when the Fellowship is revealed or declared beside Moria (no catalog
  scenario yet; PlayMode `Shadow03NetworkTests`). Expect: no "Use a table card"; the card red in Effects; its click uses the reaction.
- [ ] The Strike in a base game — seeded `shadow-strike` (PlayMode `Shadow02NetworkTests`, the Shadow). Expect: as in the
  catalog case; a red card's click discards it, in a new game the Hunt Box's click starts the Hunt roll, in another the
  ✓ finishes.
- [ ] There is Another Way — Gollum as Guide (`CardPack02NetworkTests`, match `another-way`). Expect: no Stay button,
  the ✓ lit "Finish · End There is Another Way without moving or hiding the Fellowship"; the ✓ ends the card.

Tests: `ObjectFirstChoicesTests` (the registry, the ✓'s, Hunt Box's and red cards' tooltips, the hints with and without
table cards, the tray's and the console's hints, the Hunt Box lit only during the offer, and the command the Hunt Box and
the ✓ build equal to the old buttons' and to the payload recorded from them on a real server; from the base-game line,
EXP-17), `CardConsoleOptionsTests`, `FellowshipCardActionsTests`, `LordsCardChoicesTests`; PlayMode
`NazgulStrikeObjectTests` (on a real server, a red table card's tile, the Hunt Box and the ✓ each send the `ChooseEffect`
payload the old button sent, recorded from the buttons at 7ddddb8e), `Shadow02NetworkTests` (on a real server the red
tile sends the payload the old × sent, recorded at c7d38fd5; EXP-17), `Shadow02LayoutTests`,
`CardPack02NetworkTests`, `Shadow03NetworkTests`, `LordsCardNetworkTests`, `LayoutStressScenarioTests` (the Strike's
scenario, `layout-console-options`, now checks the objects instead of three buttons; screenshots `ui430-strike-*`).

<a id="options-the-board-draws-are-chosen-on-the-board--ui-431"></a>
## Options the board draws are chosen on the board — UI-431 (2026-10-31)

UI-430's rule (owner, 2026-10-30) applied to the analysis's other cases: a card's option whose object is on the board is
lit there and chosen with a click; neither the console nor the tray keeps a button for it, and the click sends exactly the
payload the old button sent (JSON-equal on a real server, recorded from the buttons at 3da66214). Every option keeps a
click path (R16). Map terrain has no tooltip (UI-80), so where a region is the object the console's hint names it and the
forces standing there carry the tooltip.

- **A region's two forces** (Dreadful Spells, Work of the Enemy: "Target · Field / · Garrison"). The console's
  alternatives (`CardConsoleOptions.Alternatives`) and the tray's (`CardChoices.RegionAlternatives`) are gone. The lit
  region's click opens its panel as a decision panel (`RegionHover.OpenDecision`) and the region keeps the chosen edge
  (UI-392) until a force is chosen; both force sections glow and breathe with the panel's pulse (UI-251's
  `.hover-force.choice-glow`, rim and plate only), their headers press (UI-113), and each header's tooltip reads `<server
  title> · Click to choose the garrison because of <card> · <detail>` (`3 dice · 5+ · Garrison · Click to choose the
  garrison because of Work of the Enemy · The Free Peoples choose this Army's losses.`). The click sends the choice with
  its region (`ConsoleOption.Region`), as the region's click and the old button did. Hint: "Click the glowing field army
  or garrison in the region's panel" ("Click the lit region, then …" while a one-region choice waits).
- **Card army events** ("Move armies", "Attack", "Skip maneuver": The Black Captain Commands', The Ringwraiths Are
  Abroad's, A Balrog is Come!'s and Dol Guldur's maneuvers). No mode buttons. The armies of either step glow; once one is
  chosen, its move destinations light with its attack targets, which are red with the red landing mark and an `Attack ·
  Field` caption, as for the Army die (UI-389, UI-403; `CardArmyEvent.OtherTargets`, `AttackTargets`, `ArmyGhost`). A red
  target's click makes the planned army's step that attack with that target, in one undo step — its figures in the
  attack's own order, so the payload is the old Attack button's — and the ✓ confirms; ↶ takes it back. An army that may
  only attack starts the attack (and lights its moves, if any). An attack takes one army, so with two armies planned (The
  Ringwraiths Are Abroad) only moves light, and an army whose destination is chosen lights nothing more. Skip maneuver
  ends the event, so it is the console's ✓ while no army is chosen (`FinishingOption`: `Finish · End <card> without moving
  or attacking`, hint "Or ✓ to finish without moving or attacking."); with an army chosen the ✓ confirms its step. Hint:
  "Click a lit army, then where it moves or a red region it attacks", then "Click a lit destination to move or a red region
  to attack". **What stays:** the decision window's fallback while the console does not hold the event (nothing is lit
  then) keeps its buttons.
- **The Black Captain Commands** (`captain.recruit` / `captain.fly`). Recruit is the Reserve's Nazgûl: the Reinforcements
  panel shows the Shadow's Nazgûl token alone, lit (`eligible-token`, the tab `legal-target`), `Recruit · 2 Nazgûl · Click
  to recruit them with the Witch-king because of The Black Captain Commands`; its click recruits. Move Nazgûl is the map's
  Nazgûl: their forces and the Witch-king's miniature glow (`ability-owner`) and each force's tooltip leads with `Move
  Nazgûl · Click to move these Nazgûl and the others, the Witch-king among them, because of The Black Captain Commands`
  (`MapCanvas.OfferForces`); a click on one (force, miniature or region) chooses Move Nazgûl and arms that stack in the
  flight that follows, so the next click is its destination (`CaptainOrderInteraction`, as A Balrog is Come!'s "alone",
  UI-417). The view moves once to the lit Nazgûl when they lie off the map's view (`MapCanvas.Reveal`; a force under the
  console's bottom row still counts as in view, a limit of `Reveal` left as it is). **What stays:** with no Nazgûl in the reserve the recruit has no object, so it keeps its console button, lit
  and named after the card (UI-418). Hint: "Click a glowing Nazgûl in the Reserve to recruit, or a glowing Nazgûl on the
  map to move them".
- **Lure of the Ring** (`lure.eliminate` / `lure.corruption`; the Free Peoples choose). The Fellowship panel opens once for
  the choice. The companion's portrait glows red (`eliminate-target`: red rim on a red plate, the roster's pulse, no size
  change, no filter) — `Eliminate · Click to eliminate Gimli because of Lure of the Ring · <detail>` — and its click
  eliminates him. The Fellowship's marker on the corruption track, a picture until now, is offered as the hide marker is
  (UI-293: gold glow, pulse, pointer) — `Corruption +2 · Click to keep Gimli and take 2 Corruption because of Lure of the
  Ring · <detail>` (the companion's Level, `LureChoiceStep`) — and its click takes the Corruption. Hint: "Click Gimli's red
  portrait to eliminate Gimli, or the Fellowship's corruption marker to take the Corruption".
- **Monsters Roused** (Fate: "West / East of the mountains"). The regions of both halves light (Angmar, Ettenmoors, Weather
  Hills, Trollshaws; Western and Northern Mirkwood, Withered Heath — the server's program, `FateCards.cs` and
  `FateMonstersEliteStep.Regions`, since the branch choices carry no region); a force standing there leads its tooltip
  with `East of the mountains · Click to recruit east of the mountains because of Monsters Roused · <detail>`. A region's
  click chooses its half and carries the region into the card's next step (`BranchCarry`), where it is that step's region
  click: East's Elite destination is that region, sent at once as its own click was (one click makes both steps); West's
  recruitment plan starts with that region. The regions lie in the north, often off screen, so the view moves to them once
  for the decision (`MapCanvas.Reveal`). Hint, within two lines of the status column at 1366×768: "Click a lit region west
  or east of the Misty Mountains to recruit on that side".
- **A reaction's response whose object is drawn.** A response that draws a card is its deck, as Spear of Terror is the
  Character deck (UI-271): Gandalf the Grey's draw, whenever the Guide's portrait cannot hold it (UI-247 keeps it there
  when it can), lights the deck of the Event card just resolved (`ReactionDecks`) — `Draw matching card · Click to draw a
  Strategy card because of Gandalf the Grey` — and its click sends the response. **What stays:** a response without any
  object keeps its console button. No seeded match reaches this case (the server offers the draw only while Gandalf the
  Grey is the Guide, so his portrait holds it); EditMode proves that the deck sends the old button's reaction id.
- **The finish hint.** "Or ✓ to finish …" joins the console's hint only while the ✓ can act (online and idle), as on
  ability-chips; offline the ✓ is disabled and the sentence is gone.

Branches: the forces, the army events, the Black Captain, Lure of the Ring, the deck draw and the finish hint are base
game; Work of the Enemy (Kings), A Balrog is Come!'s maneuver and the Spear's deck (Lords) and Monsters Roused (Fate) are
expansions. The base port (ability-chips) lacks `BranchCarry` (only Monsters Roused uses it), the chosen-region edge
(UI-392), the markers' king hint (`ApplySovereignHint`, beside which `OfferForces` composes its line) and the decks'
lit-art pulse (UI-345), so there the drawn deck lights with `legal-target` alone.

Playtest (owner's checklist):
- [ ] Work of the Enemy's forces — seeded `kings-enemy-army`. Expect: no console or tray buttons; Minas Tirith lit; its
  click opens its panel with both Free Peoples sections glowing and pulsing, each header's tooltip "… · Click to choose
  the garrison because of Work of the Enemy · …"; the garrison's click sends.
- [ ] The Black Captain Commands — seeded `shadow-captain`. Expect: no console buttons; the Reserve's Nazgûl token lit
  with "Recruit · 2 Nazgûl · Click to recruit them …"; Osgiliath's force and the Witch-king glowing, "Move Nazgûl · Click to
  move …". The token recruits; then the Witch-king's maneuver: no buttons, the ✓ lit "Finish · End The Black Captain
  Commands without moving or attacking"; Osgiliath's army clicked lights its moves and Minas Tirith red ("Attack · Field"
  on hover); Minas Tirith's click and the ✓ attack. In a new session the force's click moves the Nazgûl with Osgiliath's
  stack armed.
- [ ] Lure of the Ring — seeded `shadow-lure` (the Free Peoples). Expect: the Fellowship panel open; Gimli's portrait red,
  "Eliminate · Click to eliminate Gimli …"; the corruption marker glowing, "Corruption +2 · Click to keep Gimli and take 2
  Corruption …"; either click sends.
- [ ] Monsters Roused — seeded `fate-monsters`. Expect: no buttons; the seven regions lit; Withered Heath's click recruits
  the east with its Elite there (two steps, one click); in a new session Trollshaws' click chooses the west and its plan
  follows.

Tests: EditMode `CardConsoleOptionsTests` (no buttons for the orders, Lure, the forces and a response's draw; the
recruit's button with an empty reserve), `CardArmyEventTests` (no buttons; the ✓'s Finish; the attack lit with the moves
and its click's step JSON-equal to the old Attack button's, in one undo step; an attack-only army; two planned armies),
`RegionForceChoicesTests` (the waiting region, the tooltips, the region kept in the payload), `BoardDrawnChoicesTests` (the
Black Captain's objects and Reserve, Lure's red portrait and offered marker, Monsters Roused's halves and the carry);
PlayMode `BoardDrawnOptionsTests` (one test per path on the seeded matches, each in its own run: the garrison; the
Reserve's Nazgûl, then the ✓; the map's Nazgûl and the armed stack; the red attack and the ✓; Gimli's portrait; the
corruption marker; Withered Heath; Trollshaws — each payload JSON-equal to the old button's; screenshots
`ui431-*-1366-1.00.png`), `Shadow03NetworkTests`, `Shadow04NetworkTests`, `Shadow04LayoutTests`, `KingsCards04NetworkTests`
(its garrison is the section now, so `EnemySeparatesFieldArmyFromGarrison` no longer expects the UI-214 buttons),
`FateCardsNetworkTests`.

<a id="declines-and-window-lists-follow-the-board-rule--ui-432"></a>
## Declines and window lists follow the board rule — UI-432 (2026-10-31)

UI-430's rule (owner, 2026-10-30) applied to the declines and window lists of the analysis (cases 11–16): a decline of an
optional effect is the window's Skip effect and the console's Pass (UI-419, UI-423, `OptionalDecline`), and a window
button that repeats an object always on screen goes. Each object sends a payload JSON-equal to its old button's (recorded
from the buttons at 7b924fb4 on a real server); every option keeps a click path (R16).

- **The base battle cards' declines.** Black Breath's and Heroic Death's "Keep all", Fateful Strike's "Keep Minions" and
  Andúril's "Keep misses" leave the battle window's list. The window's Skip effect sends them, as the console's Pass does,
  and both name the card's combat side and what passing does: `Skip Black Breath · Eliminate no Leader or Companion`,
  `Skip Heroic Death · Eliminate no figure and take every hit`, `Skip Fateful Strike · Eliminate no Minion`, `Skip
  Andúril · Convert no miss; Aragorn still forfeits his Leadership` (the server forfeits it when he converts none,
  `AndurilStep`), each with `Pass · ` in front on the console; the hint ends "Or Pass to …". Passing answers the whole
  step, so a figure already chosen is dropped, as the old exclusive button dropped it. **What stays:** Heroic Death's
  "Keep hits" (zero hits — a legal, costly choice: the Companion is still eliminated) is no decline and keeps its button
  (UI-246).
- **Worn with Sorrow and Toil** (the Shadow, during the Hunt). No list in the Hunt window. A Free Peoples Character card on
  the table is red in the Effects panel, whose tab lights (UI-430's look and path): `Discard Mithril Coat and Sting · Click
  to discard it because of Worn with Sorrow and Toil: a Companion was taken as a casualty`; a click on the tile or its ×
  discards it. The random card from the Free Peoples hand is the opponent's card counter in the top bar, a picture until
  now: it wears the red of a card to discard (a red rim on a red plate, breathing in the shared pulse, no filter, its size
  kept), presses like a button and leads its tooltip with `Discard a random card · Click to discard a random card from the
  Free Peoples hand because of Worn with Sorrow and Toil · <the server's detail>`; its click sends the choice. Keep cards
  is the Hunt window's Skip effect and the console's Pass: `Skip Worn with Sorrow and Toil · Discard no Free Peoples
  Character card`. Hint: "Discard: red card in Effects · Random card: red hand counter · Pass discards none" (the hint
  names the Pass itself, so no "Or Pass to …" sentence is glued after it).
- **Scouts' Withdraw into siege.** The battle's own region — the stronghold the army withdraws into — lights on the map
  beside the regions the army may retreat to (Retreat was already their click), and its click sends the withdrawal, as a
  click on the battle region advances after a won battle (`BattleContextInteraction`). Map terrain has no tooltip (UI-80),
  so the forces standing there lead their tooltip with `Withdraw into siege · Click to withdraw into the stronghold because
  of Scouts · End this battle before the Combat roll.` and the hint names the region: "Click Helm's Deep to withdraw into
  its stronghold, or a lit region to retreat". The battle window lists no button for it. The region panel's garrison
  section was not used: in a field battle no garrison stands there yet, so the region is the stronghold's only picture.
- **The Fellowship tray** (`FellowshipChoices`), each button checked against its object:
  - *Stay here* goes. Its object is the lit origin region of the group or route (`MapDraftOverlay` lights it exactly when
    the button was enabled), whose click is `TravelDraft.RouteTo` → `TravelDraft.Stay`, the button's own code; the click
    sends a single route at once instead of waiting for ✓ (same payload). The button stood only outside the Fellowship
    panel — a journey arriving during the Hunt or a battle, and the kings' awakening, whose tray the kings' portraits
    replaced (UI-274) — so no live table reached it any longer.
  - *Stay all* goes. With an empty plan the console's ✓ sends what Stay all + ✓ sent (an empty `CompanionMoves`); a
    planned group is given up by clicking its members off. Its journeys (the card journeys, the Nazgûl's and the Balrog's
    flights, the Council token's travel) are on the map with the ✓ since UI-212, UI-294 and UI-379, so the button no
    longer appeared.
  - *Standard* / *Far-sighted* go, as UI-276 (D18) already decided ("no toggle or mode chip"): Far-sighted is Thranduil's
    ability picture with the Ruler die (or his Ruler token, UI-389), the standard move its plain gesture. Both paths send
    the toggle's command (recorded on a real server at 7b924fb4: the separation chosen with the old buttons and with the
    objects, JSON-equal). The tray names a Far-sighted move with a label (`Far-sighted · Range +1`).
  - **What stays:** *Undo step* and *Cancel* of a drawn Fellowship route (Declare, Reveal) and of a separation. The console's
    ↶ is off while the tray sits in the Fellowship panel, and a step back is not an Undo step (the first map step adds the
    origin and the region together; after a server view the history is empty), Declare has no die whose second click
    clears it, and Esc on an empty route opens the menu. Making ↶ do both is a change of its own, not a removal.
- **The decision window's fallback lists** (`DecisionTray`, `CharacterChoices`, `ExpansionChoices`):
  - the dice `cost-<die>` go: the only list left was an unsent Discard die (offline or refused) and an unsent Narya, and
    the own pool — always beside the console, never under the window — sends the same `ChooseDie`; `CharacterChoices`'
    list was always empty (the pool lights the same dice, `CostDice.Offered`) and the kings' actions never reach the tray
    (`SovereignPresentation.OwnsDecision`);
  - the decks `deck-<deck>` go: a draw is the hand's decks (`DirectCard`), so the tray never built them;
  - the kings `sovereign-choice-<king>` go: a king is his portrait (UI-274), and the tray never built them;
  - the Hunt's `select-<die>` dice go: the Hunt allocation never reaches the tray (UI-162, UI-172) — dead code, deleted
    with the tray's `DieChosen`, `DeckChosen` and `CardOrDieToggled` events;
  - **what stays:** a character's actions `character-action-<id>`. They show when its Characters row and the board offer
    nothing — after the tray's Change between a king's upgrades, after a new revision took the chosen action away, or in a
    restored draft (the rows offer actions only before a draft starts) — so the list is the option's only picture then
    (UI-244's fallback, as UI-431 kept the army event's).
- **Open owner decisions**, not changed here:
  - *The Council token's travel picker* (UI-377, the owner's request): the popover above the token pool repeats the Nazgûl
    that stand lit on the map and in their region panels. Under UI-430's rule it would go; it was the owner's own request,
    so the owner decides.
  - *The Kings corruption draw* (UI-423): "Draw tiles" / "Draw again" is a console button while the king's attempt button
    on his portrait lights for the same attempt. One lit control (as UI-418), or both? The owner's question since UI-423.
    Since UI-442 only Sequential corruption's Draw again remains: a standard attempt draws all its tiles with its click.

Branches: the battle declines, Worn with Sorrow and Toil, the Scouts, Stay here, Undo step / Cancel and the fallback lists
are base game; Far-sighted, Narya and the kings' lists are expansions. The base port (ability-chips) has no
`OptionalDecline` and its Skip effect skips reactions only (`ReactionSkip`), so it ports the decline machinery with it.

Playtest (owner's checklist):
- [ ] Black Breath — seeded `shadow-black-breath` (the Shadow). Expect: no Keep all; the battle window's Skip effect "Skip
  Black Breath · Eliminate no Leader or Companion", the console's Pass the same with "Pass · " and lit; either sends.
- [ ] Heroic Death / Andúril — seeded `heroic`, `anduril` (the Free Peoples). Expect: Keep hits stays, Keep all / Keep misses
  gone; Skip effect names Heroic Death / Andúril (and that Aragorn still forfeits his Leadership).
- [ ] Worn with Sorrow and Toil — seeded `shadow-worn` (the Shadow). Expect: no list in the Hunt window; the Effects tab lit,
  the four Free Peoples cards red ("Discard … because of Worn with Sorrow and Toil: a Companion was taken as a casualty");
  the Free Peoples card counter in the top bar red and pulsing ("Discard a random card · …"); the Hunt window's Skip
  effect "Skip Worn with Sorrow and Toil · Discard no Free Peoples Character card". A red card's click discards it; in a new
  session the counter's click discards a random card; in another, Skip effect keeps the cards.
- [ ] Scouts — seeded `strategy-scouts` (the Free Peoples). Expect: no Withdraw into siege button; Helm's Deep and Fords of
  Isen lit; the Helm's Deep army's tooltip leads with "Withdraw into siege · Click to withdraw into the stronghold because
  of Scouts"; the hint "Click Helm's Deep to withdraw into its stronghold, or a lit region to retreat"; Helm's Deep's click
  withdraws (the next decision is the Shadow's advance).
- [ ] Far-sighted — test menu `kings-far-sighted` (or seeded `ui22-separate`). Expect: no Standard / Far-sighted buttons in
  the Fellowship panel; with the Ruler die, Thranduil's picture starts the Far-sighted separation, named "Far-sighted ·
  Range +1"; a companion's portrait with the die starts the standard one.

Tests: EditMode `BoardRuleDeclinesTests` (the four declines' Skip effect and Pass tooltips and their payload — Fateful
Strike's Keep Minions has no seeded match, as the server offers it only without a Nazgûl in the battle, so this is its
command-equality proof; Worn's offers, hint, Hunt window and counter; Scouts' region, tooltip, hint and the battle region's
payload), `WindowListsTests` (the Fellowship tray's remaining buttons and its Far-sighted label; the origin's click and an
empty plan against Stay here and Stay all; no die, deck or king lists), `BattleObjectChoicesTests`, `SovereignPanelTests`;
PlayMode `BoardRuleDeclineTests` (on a real server, each object's payload JSON-equal to its old button's: Black Breath's,
Heroic Death's and Andúril's Skip effect, Worn's red table card, red hand counter and Skip effect — three runs —, Scouts'
battle region; screenshots `ui432-*-1366-1.00.png`), `FarSightedObjectTests` (Thranduil's picture and the plain gesture
against the old Far-sighted and Standard buttons, each in its own run), `Shadow02LayoutTests`, `Shadow03NetworkTests`,
`Strategy01NetworkTests`, `Strategy01LayoutTests`, `CardPack02NetworkTests`, `ExpansionTokenNetworkTests`, the Fellowship
tray tests (`FellowshipNetworkTests`, `FellowshipLayoutTests`, `CharacterNetworkTests`, `CompanionJourneyMapTests`).

<a id="every-object-a-hint-asks-to-click-visibly-glows--ui-434"></a>
## Every object a hint asks to click visibly glows — UI-434 (2026-10-31)

The owner, after UI-433 (Dismay's Ruler die read as unlit in the top bar): audit every effect whose hint or tooltip asks
the player to click a glowing, lit, highlighted, red or pulsing object, and make sure that object visibly glows where it
stands (UI-430's rule: objects signal options by glowing and pulsing). An object is **strong** when it wears the shared
gold ChoiceGlow (a rim of at least 2 points on a warm plate) or the legal-target rim in a pulsing host, the red discard
look, or the map's choice glow, and stands on screen; **weak** with its lit art alone, a thin rim, a colour change only,
or no pulse where others pulse; **hidden** inside a closed panel whose tab does not light, under a window or the
console, off the map's view, or in a place the hint does not name.

- **The pools' dice.** Every die a resource decision offers wears the gold ChoiceGlow and breathes with its pool — the
  console's own pool as the top bar's (UI-433) — through one `PoolChoiceGlow` per pool (`BoardScreen.OwnDiceChoices`,
  `OpponentDiceChoices`): each interaction names its dice (an Elven Ring's die and a payment's, `RingInteraction`; the
  special-die choice, the Ruler discard, Dismay, Vilya's paid die, Lore and a Council entry's dice,
  `ResourceContextInteraction`), and the pool lights their union, so one interaction's refresh never puts out another's.
  The lit art stays under the rim; its own fading stops while the rim and plate breathe (no larger die: a pool's dice
  may not overlap, so the Elven Ring's die of UI-238 no longer grows either).
- **Dismay names the die's pool.** The Shadow may reroll either Ruler die (both seeded matches offer both), and the
  Shadow's own stands in its pool, not the top bar: "Click a glowing Ruler die, yours or the top bar's, to reroll it
  with Dismay." ("Click your glowing Ruler die …" or "Click the glowing Ruler die in the top bar …" when only one is
  offered).
- **The Council medallion a hint asks for** — the Council's offer ("Click the glowing Council token you want") and a
  Chieftain's entry cost ("· Click a glowing Council token to pay the entry", "· Or click a glowing Council token to
  enter with it"; `ResourcePresentation.CouncilAsked`) — wears a gold rim on a warm plate round the medallion and
  breathes with its pool, rim and plate only (the row has no room for a larger medallion). A usable medallion no hint
  asks for keeps UI-345's lit art and its fading. The marked token's hint keeps both places within the console's two
  lines (the longer one lost "in Politics" to its ellipsis at 1366×768): "Click a lit Chieftain's row in Characters to
  enter him, or a glowing Shadow nation's token or → in Politics." (Travel: "…, or the Nazgûl above the token, then
  their destination.")
- **The console's Elven Rings a hint asks for** — a Keeper Ring offered now (Nenya's tile, Vilya's keep, UI-268), a
  card's recovery (Three Rings for the Elven-Kings, UI-272) and a removal's Ring payment ("Click a lit Elven Ring …") —
  wear a round gold rim on a warm plate inside the fixed socket, the picture kept in place, and breathe (rim and plate:
  the console keeps its size). A Ring a card's step already picked is selected instead; a Ring the chosen die may merely
  use (the base Elven Ring use) keeps its bright picture. Lords also has a Vilya die, so Vilya's hint names the console:
  "Click the glowing Vilya in the console to keep the die that paid, or Pass to spend the die and keep Vilya."
- **The decks a hint asks for** — a deck choice (the Palantír of Anárion's extra card, UI-390), the Spear of Terror and
  Gandalf the Grey's draw (UI-431) — wear the thick gold rim round the back, breathing in rim only; the die's pale halo,
  drawn over the rim, steps aside, and UI-345's lit-art fading of the Spear's deck is gone with it.
- **The Mordor gate.** The gate the Fellowship may enter by wears the gold rim on a warm plate on the map and breathes;
  its "gate-next" art alone (a faint green halo at 34 points) read as unlit. The Fellowship panel's Mordor track symbol
  is a gate too, so the hint says the map's, in one clause with the declaration so that it keeps to two lines: "Declare
  or enter Mordor: click the glowing figure or gate on the map · or continue to the Hunt." ("Enter Mordor: click the
  glowing gate on the map" alone).
- **A siege extension's figures.** "Click a highlighted figure to extend the siege with it": one click extends, so no
  figure is chosen before it, and the battle window drew every figure dimmed at half opacity as one left behind. Each
  figure the extension may take now wears the gold ChoiceGlow and breathes with its grid (`BattleChoices`).
- **Off the map's view.** `MapCanvas.Reveal` (UI-134) counts as visible only the free map — the map minus what covers it
  (the top bar, the console, open panels and windows; `SafeMapArea`, as a presentation frames its cues) — so the Black
  Captain's Nazgûl no longer stay under the console (UI-431's noted limit), and the Reserve's settlements and Monsters
  Roused's halves come out from under the HUD too. Two more decisions bring their lit objects into view once, as those
  do: the Fellowship phase that offers Mordor's gate (its region, with the figure beside it,
  `FellowshipContextInteraction`) and a card's Nazgûl flight (the lit Nazgûl, `NazgulFlightInteraction`) — at 1920×1080
  at 125 % both lay beyond the screen's edge or under the hand.

Click paths and payloads are unchanged: every fix is a class, a pulse, a camera move or a hint's words; no CSS filter,
no size change beyond the shared pulse, no layout write in a geometry callback.

Inventory (verdict before UI-434; Audit = PlayMode `GlowingHintAuditTests` on the seeded match named, at 1366×768 at 100
% and 1920×1080 at 125 %, screenshots `ui434-<case>-<width>-<scale>.png`; Classes = EditMode `GlowingHintClassesTests`):

| Case (hint) | Branch | Object | Where | Verdict | Fix · test |
| --- | --- | --- | --- | --- | --- |
| "Click a highlighted die to keep it" / "An Eye must be kept: click a highlighted Eye" (`SpecialDice`) — special-die choice | expansions (Lords) | the special dice | own pool beside the console | WEAK: lit art alone, the candidate rim off | ChoiceGlow + the pool's pulse (`PoolChoiceGlow`) · Audit `ui267-tie` |
| "Click a glowing die to discard it" (`RulerDice`) — Ruler discard | expansions (Kings) | the standard dice and the Ruler die | own pool | WEAK: 2 pt candidate rim beside the dice's own rims; the Ruler die's lit art | ChoiceGlow + pulse · Audit `ui09-dice` |
| "Click … Ruler die … to reroll it with Dismay" (`ResourcePresentation.DismayHint`) | expansions (Kings) | the Free Peoples' Ruler die; the Shadow's own Ruler die | top bar; own pool | the top bar's STRONG since UI-433; the own pool's WEAK (lit art) and HIDDEN (the hint said "in the top bar") | ChoiceGlow + pulse; the hint names "your", "in the top bar" or both · Audit `ui09-dismay`, `ui58-dismay-before-lore`; Classes |
| "Click the glowing Vilya …" (`KeepActionDie`) | expansions (Lords) | Vilya's socket; the paid die | console; own pool | WEAK: the socket's lit art and fading; ambiguous with the Vilya die | socket and die ChoiceGlow + pulse; the hint says "in the console" · Audit `ui09-vilya`; Classes |
| "Nenya can replace this Eye tile: click the glowing Nenya in the console" (Hunt window tray) | expansions (Lords) | Nenya's socket | console | WEAK: lit art and fading | ChoiceGlow + pulse · Audit `t34-nenya`; Classes |
| "Click N glowing Rings in the console to recover them" (`RingRecovery`) — Three Rings for the Elven-Kings | expansions (Lords) | the Rings' sockets | console | WEAK: a brighter picture only | ChoiceGlow + pulse; a picked Ring selected · Audit `ui272-three-rings`; Classes |
| "Click a lit Elven Ring to give it to the Shadow with this die" (`CostDice.RingHint`) — Lore, Palantíri removals | expansions (Lords, Kings) | the Rings' sockets | console | WEAK: a brighter picture only | ChoiceGlow + pulse · Audit `lords-lore-remove`; Classes |
| "Click a lit die to change its result with Lore" (`LoreChoice`) | expansions (Lords) | the dice Lore may change | own pool's row | WEAK: 2 pt rim beside the dice's own rims | ChoiceGlow + pulse · Audit `lords-lore-choice` |
| "Click the glowing Council token you want" (Council's choice) | expansions (Lords) | a Council medallion | own pool | WEAK: lit art and fading | ChoiceGlow (rim and plate) + pulse · Classes (no one-step seed: `ui09-council` reaches it after the versions and homelands) |
| "… · Click a glowing Council token to pay the entry" / "· Or click a glowing Council token to enter with it" (`CharacterChoices.RegionHint`) | expansions (Lords + Kings) | a Council medallion | own pool | WEAK: lit art | ChoiceGlow + pulse (`ResourcePresentation.CouncilAsked`) · Classes (the seeded `ui09-entry` enters with the die or with the token, never both open) |
| "Click a lit Chieftain's row in Characters to enter him, or a glowing Shadow nation's token or → in Politics." (`CouncilHint`) | expansions (Lords + Kings) | the Chieftain's row; Shadow nations' tokens | Characters and Politics panels, closed; their tabs lit | STRONG objects, HIDDEN place: the long hint lost "in Politics" to the console's ellipsis at 1366×768 | shorter hint naming both panels · Audit `ui09-council-politics` (tabs lit, then each panel) |
| "Click a glowing deck beside your hand …" (`DeckChoice`), Spear of Terror, Gandalf the Grey's draw (`ReactionDecks`) | expansions (Kings, Lords); base (Grey) | the decks | hand drawer, opened once | WEAK: a 2 pt rim under the die's pale halo, no pulse; the Spear's lit art | ChoiceGlow (3 pt rim, the halo steps aside) + pulse · Audit `ui348-anarion`; Classes |
| "Enter Mordor: click the glowing gate" (`TurnConsoleState`) — Fellowship phase | base | the Mordor gate | map's Mordor track | WEAK: "gate-next" art alone, a faint halo at 34 pt; ambiguous with the panel's gate symbol; HIDDEN at 1920×1080 at 125 % (under the hand) | ChoiceGlow + pulse; "… gate on the map", one clause with Declare; the view comes to it once · Audit `ui34-mordor-morannon-True`; Classes |
| "Click a highlighted figure to extend the siege with it" (`BattleDraft`) | base | the figures that may extend it | battle window, the side's column | WEAK: drawn dimmed at half opacity, as figures left behind | ChoiceGlow + pulse, not dimmed · Audit `siege`; Classes |
| "Declare: click the glowing Fellowship figure on the map", "The glowing Fellowship figure moves/hides …", `HideHint`, `MoveHint` | base | the Fellowship figure; its track marker | map; Fellowship panel (corner tab lit) | STRONG (ChoiceGlow + pulse, UI-280/UI-293); beside Mordor off view at 1920×1080 at 125 % | the gate's reveal brings it too · Audit `ui34-mordor-morannon-True` (the figure) |
| "Click the glowing Mordor track symbol", "Move: click the glowing track" (There Is Another Way) | base | the track symbol | Fellowship panel (corner tab lit) | STRONG: the gilt halo breathing (UI-110) | — · `MordorNetworkTests`, `FellowshipCardActionsTests` |
| "Click a glowing companion in the Fellowship panel to make it the Guide", "Click the Guide's glowing portrait …", "Click a glowing companion …, then a lit homeland" | base; Council homelands expansions (Lords) | companions' portraits; homelands | Fellowship panel (opens for its decision, corner tab lit); map | STRONG | — · `FellowshipGuideNetworkTests`, `MultiUseDiceNetworkTests`, `CouncilHomelandPanelNetworkTests` |
| "Click Gimli's red portrait …, or the Fellowship's corruption marker" (`LureChoice`) | base | red portrait; marker | Fellowship panel, opened | STRONG (UI-431) | — · Audit `shadow-lure` |
| "Discard: red card in Effects · Roll: Hunt Box · ✓ finishes" (`NazgulStrikeChoice`), "Click a red table card in Effects" (`TableDiscardText`) | base | Hunt Box; red table cards | console; Effects panel (tab lit) | STRONG (UI-430) | — · Audit `shadow-strike` |
| "… Random card: red hand counter …" (`WornDiscardChoice`) | base | the Free Peoples' card counter | top bar | STRONG (UI-432) | — · Audit `shadow-worn` |
| "Click a red-marked card to discard it face down", inspected Shadow hand, We Won't Go Back's "glowing Character card in your hand" | base; We Won't Go Back expansions (Lords) | hand cards | hand drawer | STRONG: red parchment (UI-222) | — · Audit `ui31-discard` |
| "Click a glowing Nazgûl in the Reserve …, or a glowing Nazgûl on the map …" (`BlackCaptainOrder`), "a glowing figure of … in the Reserve" (`KingsCardChoices`), reserve tooltips "… a highlighted settlement" | base; Kings | Reserve tokens; the map's Nazgûl; settlements | Reinforcements panel (tab lit); map | STRONG objects; the map's Nazgûl HIDDEN under the console at the fitted view (UI-431's noted limit) | `MapCanvas.Reveal` counts only the free map · Audit `shadow-captain` (Reserve token, then the map's force) |
| "Click a lit Nazgûl, then where it flies" (`NazgulFlightDraft`) | base | the lit Nazgûl | map | STRONG glow; HIDDEN at 1920×1080 at 125 % (Barad-dûr beyond the screen's edge) | the view comes to them once (`NazgulFlightInteraction`) · Audit `shadow-strike` |
| The other map figures ("Balrog's glowing army", "a lit army", "a lit companion", "a lit figure") | base; Balrog expansions (Lords) | army and character stands | map | STRONG: the map's choice glow and pulse | — · `LordsCharacterNetworkTests`, `CompanionJourneyMapTests`, `BoardDrawnOptionsTests` |
| "Choose/click a highlighted/lit region, destination, settlement", red attack regions, Work of the Enemy's lit region, Monsters Roused's halves | base; Kings, Fate | regions | map | STRONG: the map's target glow pulsing | — · Audit `kings-enemy-army`, `fate-monsters` |
| "Click Helm's Deep to withdraw into its stronghold, or a lit region to retreat" (`ScoutsShelter`), "Choose a highlighted retreat region." | base | the battle region; the retreat regions | map, beside the battle window | STRONG at 1366×768; at 1920×1080 at 125 % HIDDEN until UI-435 (under the battle window and the console) | the window keeps to the middle's top and the view brings both regions below it ([UI-435](#a-battle-window-keeps-its-lit-regions-clear--ui-435)) · Audit `strategy-scouts` in both layouts |
| "Click the glowing field army or garrison in the region's panel", "Click a lit region, then its lit figure(s)" | base; Kings | force sections; figures | region panel | STRONG (UI-251, UI-431) | — · `BoardDrawnOptionsTests` |
| "Click a glowing tile beside the king", "Click the glowing king …", "Click a glowing king, by his portrait or on the map / in the battle window's army", "Click the attempt button beside a king in the Characters panel" | expansions (Kings) | portraits, attempt buttons, tiles; map markers; battle kings | Characters panel (tab lit, UI-380); map; battle window | STRONG (UI-274, UI-345, UI-411) | — · `SovereignPanelNetworkTests`, `ExpansionCorruptionNetworkTests` |
| "Click a glowing nation token in Politics", "a glowing nation token to activate and advance it" (`KingsCardChoices`) | expansions (Kings) | Shadow or Free nations' tokens | Politics panel (tab lit) | STRONG (UI-423) | — · `KingsBranchNetworkTests`; Audit `ui09-council-politics` (same tokens) |
| "Choose a highlighted card, or No card." (`BattleDraft`), combat card targets, replacement arms, kings in the army | base; arms expansions | combat cards; dice, portraits, stacks; chips | battle window | STRONG: the playable parchment (cards never pulse; No card glows when alone, UI-279); ChoiceGlow + pulse | — · `BattleWindowNetworkTests`, `LordsBattleNetworkTests` |
| "Click a glowing Guide response in the Hunt window …", "Use a glowing response, or Pass …" (`ReactionSkip`), Hunt card responses | base; Lords | response objects | Hunt window (opens for both) | STRONG (UI-285, UI-360, UI-423) | — · `HuntResponseNetworkTests` |
| "Click a highlighted die to change its result" (Elven Ring), a payment's dice, "Click, then a glowing Muster die" (Palantír of Barad-dûr) | base; Kings | dice | own pool | STRONG (UI-238, UI-248) | through `PoolChoiceGlow`, unchanged look · Audit `lords-lore-remove` (payment dice) |
| "Click the glowing ✓ in the console to check its result." | base | ✓ | console | STRONG (ChoiceGlow + pulse) | — |
| "Choose what this die acts on: the highlighted map, cards and panels", "Choose a highlighted object …", "Select the highlighted …, then confirm" (`DecisionObjects`) | base | a die's uses; a window's choices | each object's family | per family (UI-136, UI-389) | — |

Limits left as they are:
- *A floating window over a lit region* — resolved by [UI-435](#a-battle-window-keeps-its-lit-regions-clear--ui-435)
  for the battle window. At 1920×1080 at 125 % its middle place covered Helm's Deep, which Scouts asks to click, and the
  console the rest of it; the map cannot pan its west edge past the screen, so only the window's move to the middle's
  top together with a camera move into the strip below it clears the region.
- *A window's generic console line.* While a decision lives in a window (the siege extension, Nenya's tile), the console
  reads "Select the highlighted choices, then confirm." and the window's tray names the object; both objects glow.
- *A hand card over the decks.* A hand card that slid under the pointer stands expanded over the decks until the pointer
  leaves the hand (UI-390); the decks glow as soon as it does.
- *No one-step seed:* the Council's token offer (`ui09-council` reaches it after the versions and homelands) and a
  Chieftain's entry that may pay with the token or a die (the seeded `ui09-entry` offers them as separate entries) are
  checked by `GlowingHintClassesTests`.

Branches: the Mordor gate, its guidance and reveal, the siege extension's figures, the Nazgûl flight's reveal, the free
map of `MapCanvas.Reveal`, Gandalf the Grey's drawn deck and `PoolChoiceGlow` with `RingInteraction` (the Elven Ring's
die, a payment's dice) are base game; the special dice, the Ruler discard, Dismay, Vilya, Nenya, Three Rings, Lore, the
Ring payments, the Council medallions and hint, and the deck choice are expansions. The base port (ability-chips) takes
`PoolChoiceGlow` with `RingInteraction` only, the gate, its guidance and reveal, the flight's reveal, the free map and
the siege figures; there the drawn deck lights with `legal-target` and the gold ChoiceGlow (no Spear, no lit-art pulse).

Playtest (owner's checklist):
- [ ] Special dice — seeded `ui267-tie` (the Free Peoples). Expect: Vilya and Nenya in the pool beside the console with
  a thick gold rim, breathing; "Click a highlighted die to keep it"; a click keeps one.
- [ ] Dismay — seeded `ui09-dismay` (the Shadow). Expect: both Ruler dice glowing gold, the Free Peoples' in the top bar
  and the Shadow's in its own pool; "Click a glowing Ruler die, yours or the top bar's, to reroll it with Dismay."
- [ ] Vilya, Nenya, Three Rings — seeded `ui09-vilya`, `t34-nenya`, `ui272-three-rings`. Expect: the named Ring's socket
  in the console with a round gold rim, breathing; the hints name the console.
- [ ] A deck choice — seeded `ui348-anarion` (the Free Peoples), and the base game's `shadow-palantir-draw` (the Shadow).
  Expect: both decks beside the hand with a thick gold rim, breathing, no pale halo; a deck's click draws.
- [ ] A Nazgûl flight — seeded `shadow-strike` (the Shadow), at 1920×1080 at 125 %. Expect: the view comes to the lit
  Nazgûl of Barad-dûr.
- [ ] Mordor — seeded `ui34-mordor-morannon-True` (the Free Peoples), in a small window. Expect: the view comes to
  Morannon; its gate glowing gold and breathing, the figure glowing; "Declare or enter Mordor: click the glowing figure
  or gate on the map".
- [ ] A siege extension — seeded `siege` (the Shadow). Expect: the figures in the battle window's column glowing gold,
  not dimmed; one click extends.
- [ ] The Black Captain — seeded `shadow-captain` (the Shadow). Expect: Osgiliath's Nazgûl brought out from under the
  console, glowing.

Tests: EditMode `GlowingHintClassesTests` (the pool's union across interactions, Dismay's three hints, Vilya's hint, the
sockets a hint asks for and the base Ring use that does not glow, a Keeper Ring, the deck choice, the Council's offer
and entry cost, Gandalf the Grey's drawn deck (EXP-17), the Mordor gate, the siege figures), `TurnConsoleStateTests` (the Fellowship phase's clause),
`ResourcePanelTests` (the Council hint), `NamedRingsTests`; PlayMode `GlowingHintAuditTests` (22 cases, each on its own
seeded match, all in one run: the special dice, the Ruler discard, Dismay in both seeds, Vilya, Nenya, Three Rings,
Lore, a Ring payment, the decks of Anárion and of the Palantír of Orthanc (EXP-17), the Mordor gate and figure, the Council's Characters row and Politics tokens behind
their lit tabs, the Strike's flight, Hunt Box and red table cards, Worn's red hand counter, Lure's portrait and marker,
the Black Captain's Reserve token and map Nazgûl, Work of the Enemy's and Monsters Roused's regions, the hand's red
cards, Scouts' regions, the siege extension's figures) with the reusable checks
`GlowingTargetChecks.AssertVisiblyOffered` (shown, inside the screen, uncovered at its centre, a strong glow, its host
pulsing), `AssertTabLit` and `AssertRegionOffered`; `ResourcePanelNetworkTests` (UI-433's Dismay, now naming both dice),
`ExpansionTokenNetworkTests` (the Council hint); unchanged and still passing with the new glows, pulses and reveals:
`SpecialDiceNetworkTests`, `ExpansionDecisionNetworkTests` and the Council tests'
`ExpansionMatrixLayoutTests.ResourcesFit` (a pool's dice may not overlap, hence the rim-only pulse),
`ExpansionWindowNetworkTests`, `LordsCardNetworkTests`, `LordsShadowNetworkTests`, `KingsDecisionNetworkTests`,
`Shadow02NetworkTests`, `Shadow04NetworkTests`, `StrongholdNetworkTests`, `FateCardsNetworkTests`, `MordorNetworkTests`,
`DeckChoiceMenuTests`, `MusterNetworkTests`, `FellowshipNetworkTests`, `BattleLayoutTests` and `BoardDrawnOptionsTests`'
reveals (the map's Nazgûl, the eastern half, the Reserve's Nazgûl).

<a id="both-sides-rows-stand-level--ui-437"></a>
## Both sides' rows stand level in the battle window — UI-437 (2026-11-01)

The owner's playtest (expansions 80df28fd): after the Black Serpent's Red Wrath "the battle window fell apart" — the
defending Black Serpent's portrait stood alone in a row above his force (one figure ×1), the card row carried the gilt
replacement marker, and the Shadow's statistics, card, "Combat · Hits 1" and dice no longer lined up with the attacker's.

- **Cause.** The two columns were independent stacks: a force with characters added a row of medallions and a line of
  names above its figures, which no other ability's addition matched on the other side; so did the ledge of figures
  outside the fight, a special attack's pager (20 px against an 18 px heading) and the hit buttons Heroic Death and
  Orderly Withdrawal add to one side's roll heading (33 px). Red Wrath only made it visible: the Serpent is a character
  in his force, and the replacement marker sits beside the card. Reproduced on the seeded match `ui277-chieftain`
  (`ui277-ui277-chieftain-*.png` before, `ui437-*.png` after).
- **Characters stand in the force row** (`battle-force-row`, `BattleContext.ForceRow`): each medallion with its short
  name under it, as a figure has its count, beside the nation groups; a force of more than four characters keeps the
  medallions without names (`BattleContext.NamedCharacters`; the tooltips name them), and a column that chooses
  figures (losses, an advance, a siege extension; UI-208) hides the names, as UI-394 did on a short viewport. The
  portraits stay outside `battle-composition`, which a figure choice hides, so a choice made on a portrait (Evil
  Knowledge during the extension) keeps its object.
- **Both columns hold the same rows** (`BattleRowPlan`): heading, force row, a spacer, statistics, the ledge when either
  side has one, the card (a replacement's chip or marker in a row of the card's height, its top on the card's), the
  roll. A row or control only one side shows keeps an unseen, inert copy on the other (`battle-row-reserved`,
  `visibility: hidden`): the ledge, a roll with one die's room, the roll's pager, a hit button. The spacer under the
  shorter force row takes the difference (`flex-grow`), so everything under it stands level whatever either army holds.
  While a column chooses figures both keep their rows at the top (`battle-forces-choosing`), as before.
- Pure layout: no layout write in a geometry callback, no measuring; click paths, payloads and hit targets unchanged.

Playtest (owner's checklist):
- [ ] A battle with a rear guard — seeded `ui44-window` (the Free Peoples attack Lossarnach from Minas Tirith with a
  rear guard). Expect: the attacker's characters stand beside its figures, each name under its medallion; the ledge
  naming the rear guard on the attacker's side and an empty row of its height on the defender's; both sides' statistics,
  cards and rolls level.
- [ ] Red Wrath — seeded `ui277-chieftain` (the Shadow). Click the Black Serpent's chip, take Red Wrath's hit, roll both
  armies. Expect: both columns' rows stay level.

Tests: PlayMode `BattleRowsNetworkTests.RedWrathKeepsBothSidesRowsLevel` (seed `ui277-chieftain`: the Shadow clicks the
Black Serpent's chip, takes Red Wrath's hit, both armies roll; in 1366×768 and 1920×1080 at 100 % and 125 % the force
rows, statistics, cards, roll headings and dice rows of both sides start level, the marker is no taller than the card,
and the Serpent stands on his figures' line; screenshots `ui437-red-wrath-*`, `ui437-rolled-shadow-*`,
`ui437-rolled-free-peoples-*`), `BattleLayoutTests.WhatAnAbilityAddsToOneSideKeepsBothSidesRowsLevel` (nine Companions
whose figures wrap to a second line, a rear guard, Heroic Death's hit buttons, a special attack's pager and the Black
Serpent's marker on one side; four layouts; `ui437-abilities-*`) with the shared checks `BattleRowChecks.Level`;
`BattleLayoutTests` (all windows above the console, the losses' column), EditMode `BattleArtTests` (names under the
medallions, in the force row).
Port to `ability-chips` (base game): the force row, `BattleRowPlan`, the spacer, the reserved ledge, roll, pager and hit
button, the CSS in `Battle.uss`, `BattleRowChecks` and the fixture test without the replacement marker. Expansions only:
the card row's alignment (`Interaction.uss`, `.battle-card-row`), the replacement marker's checks and the Red Wrath test.

<a id="one-marker-per-character--ui-438"></a>
## One marker per character, its versions inside — UI-438 (2026-11-01)

The owner's playtest: "two different versions of the Mouth are shown separately, unlike Aragorn, who has one marker and
switches inside; fix it and make sure every character behaves like Aragorn/Strider".

- **Cause.** UI-271 split a Shadow character whose Lords card is an alternative to the base one into a row per version
  not in play (`CharacterRosterVersions`): "Mouth · Lieutenant" and "Mouth · Númenórean", the Witch-king's "Black
  Captain" and "Chief". The server already lists one row per character (identity) with its versions; the client split it.
- **Audit** of every character with more than one version in the content: Free Peoples — Gimli (base, Lords, Fate,
  Fate's Dwarven Lord), Boromir, Legolas, Merry, Pippin (base, Lords), Gandalf (the Grey base and Lords, the White),
  Strider/Aragorn (base and Lords Strider, Aragorn); Shadow — the Mouth of Sauron and the Witch-king (base, Lords). The
  kings' Awakened and Corrupted cards belong to different sides (the Corrupted ones are not roster characters), the
  Chieftains have one card each. Only the two Shadow characters were split; the others already had one row.
- **One row per character** in every section (On board, Not in play, Removed) — `CharacterRosterPresentation.Rows`
  keeps the server's rows. The row shows one version (`ShownVersion`): in play, the version in play; otherwise the
  version an offered entry brings in, or the first that may enter. Its second line names that version
  ("Lieutenant", "Númenórean") and changes to the version the chosen die's entry brings in; the tooltip opens with
  "Click to bring The Mouth of Sauron, Black Númenórean into play".
- **Its window switches** between the versions with the UI-387 tabs, which read the epithets of one character's
  alternative cards (`CharacterLibrary.VersionName`); a map figure (the Witch-king in play) opens the same window with
  both versions.
- **Click paths and payloads.** One version that may enter for the chosen cost enters at the row's click, as the
  version's own row did, with the same entry (`EnterCharacter` with that version). With the Black Captain in play, his
  one row turns him into the Chief in place. Two versions that may both enter for one die (both Mouths with a Muster die)
  are the existing explicit choice (UI-108): the row glows and says "Click to choose which version comes into play", its
  click opens the window, the other version's tab glows and the shown version's Enter sends it.

Tests: EditMode `CharacterRosterVersionsTests` (over the base, Lords, Kings and Fate content: every side's characters
not entered, on board, in the Fellowship and removed have one row each and one window tab per version, the tabs told
apart; the Mouth's row shows and sends the entry's version; two enterable versions are chosen in the window; the
Witch-king's row turns him in place), `CharacterRosterPanelTests` (eight Shadow rows); PlayMode
`LordsCharacterNetworkTests` (`ui271-entries-sh`: one Mouth row and one Witch-king row and their windows' tabs; every
Free Peoples nation is at war there, so one Muster die may bring in either version of each — the rows glow and open the
window, and the Lords Mouth enters from his glowing tab's Enter with the same entry as before; `ui271-witch-king-switch`:
the Black Captain's row turns him; screenshots `ui438-shadow-entries-*`, `ui438-witch-king-switch-*`,
`ui438-<figure>-window.png`, `ui438-mouth-choice-window.png`), `BoardCompletionLayoutTests`,
`CharacterRosterPanelNetworkTests`, `CharacterRosterLayoutTests`.
Port to `ability-chips`: nothing — it has no version split (`CharacterRosterVersions` is expansions-only); the tab names
apply only to the expansions' alternative cards.

<a id="a-characters-window-is-compact-shows-its-place-as-a-picture-and-opens-in-one-place--ui-436"></a>
## A character's window is compact, shows its place as a picture and opens in one place — UI-436 (2026-11-01)

The owner's playtest (expansions 80df28fd), about "Denethor · ♙ Initial" with its "Minas Tirith" button, "Corruption
0/3" and "After awakening · Abilities  Unavailable": "the window is too big, the information is vague; the Minas Tirith
button should be a picture of the place where he is; apply it to all characters and make sure every place any
character can be has its picture; the window opens in a different place for each ruler — always show it beside the
rulers' tab, in the same place."

- **One window form.** The king's window (`SovereignInspector`) is framed like a character's window
  (`.character-window`, 380 points wide in every layout, as the character window now is). From the top: the header —
  his name, his status badge (its icon and word, its tooltip what the state means: "Initial · Not yet awakened: he leads
  his force as a Leader"), and Close; the place row; his action in words; his abilities. An attempt's progress and a
  decision's consequence add their lines below, as before; the window still hosts no decision (UI-274).
- **The place is a picture** (`PlacePicture`, 128×80 points) captioned with the place's name, in place of the king's
  "Minas Tirith" text button and of the character window's status line with its Locate button. A region's picture is
  enabled, its tooltip "Minas Tirith · Click to show this region on the map", and its click sends what the old button
  sent (`Map.Focus(region)`). A place off the map keeps the picture's slot, disabled, its tooltip naming the place and
  what being there means ("The Fellowship · Travels with the Fellowship · Not on the map"). `PlacePresentation` maps
  every roster lifecycle and every king's status to a place: a region on the board; the Fellowship (in it, or leaving
  it to be placed); not in play (not entered, returning, unknown — a picture per side); removed (eliminated, or
  eliminated and able to return as Gandalf the White — per side); out of play (a removed king).
- **Every place has its picture** (`scripts/export_place_pictures.py`, deterministic with `--check`, the pinned
  Pillow/NumPy/SciPy of the map scripts; report `docs/sources/UI-436-PLACE-PICTURES.json`). 111 replaceable JPEGs of
  256×160 px (2 MB) in `Resources/Wotr/Art/Places/<place>.jpg`: each of the 105 regions is a crop of the map canvas art
  round it (the MAP-14/MAP-18 terrain over the UI-102 margins, the painted landmarks and the land borders), its outline
  in gold, the land round it dimmed and its own landmark kept bright; the six places off the map are painted from the
  board figures on the bare paper of the margins — the Fellowship's miniature, a ghosted Companion or Minion over an
  empty stand (not in play), a figure laid down (removed) and the Ruler figure laid down (a removed king). No character
  stands on the Mordor track (only the Fellowship does, its Companions inside it), and every king's home is a region.
- **Each line says what it means.** Beside the picture: "Corruption 1/4" on a small meter (a pip per point of
  resistance, filled for known corruption, hatched for hidden tiles), then "Corrupted when his tiles reach 4"
  (CorruptionRules: a king is corrupted when his tiles reach his resistance); a Corrupted king reads "Corruption
  resolved · He now serves the Shadow", a removed one "Out of play · Removed from play"; then "In the stronghold's
  garrison" or "Corruption attempted this turn", and his attached tiles. The bare "Unavailable" (and the character
  window's "Limits") is now the action in words (`AvailabilityLines`, `AvailabilityRows`): "Awaken · Muster or Ruler
  die", then "Available now" in gold or what it still needs ("His nation must be active", "No Muster or Ruler die
  available", "No region he can reach", "Needs 2 Eyes in the Hunt Box", "Already attempted this turn", "The Free Peoples
  have no unused die"; another condition as the server words it); the tooltip keeps the server's full cost, every
  condition with ✓ or ○, the timing and the rules page. An action of another state (Awaken for a Corrupted king) and the
  tile return outside its moment are left out; without an action of his own the line says what his state means ("Not
  yet awakened · The Free Peoples may awaken him, the Shadow corrupt him", "Serves the Shadow · His weaknesses below
  apply", "Out of play · Nothing more can happen to him"). "After awakening · Abilities" with a tooltip is now his
  abilities under "Abilities once awakened" or "Weaknesses while corrupted", as the character window's lines.
- **Abilities say what they do** — in every character's window too. An ability chip (`CharacterAbilityChips`) is a line
  of the window's width: the ability's icon (the roster row's; its own since UI-439, `AbilityArt`), its title in gold
  and the gist of its text ("Stewardship · With Denethor in Minas Tirith, a Muster die upgrades a Gondor Regular
  there"); its tooltip is the printed text and page, its click still unrolls the printed text (UI-139) and a usable one
  still glows and carries Click to use (UI-369). Since [UI-440](#skill-icons--ui-440) (2026-11-02) the lines, their
  pages, the unrolled parchment and Click to use are gone: each ability is its bare icon in one row, its gist and text
  in its formatted tooltip, and a lit icon's click uses it.
- **The character window** keeps every control (pin, close, version tabs, ability lines and pages, Click to use, the
  entry, the decision host); its place row is the picture and, beside it, a line saying what being there means ("On the
  board", "Travels with the Fellowship", "Not entered yet", "Eliminated; may return as Gandalf the White") over the
  statistics. An Awakened king's character window has the rows of every king's window: his action line ("Awakened · A
  Companion with the abilities below" when he has no action of his own) and "Abilities" over his ability lines.
- **One place** (`CharacterWindowPlace`). Every ruler's window, and every character's window opened from the Characters
  tab, its panel or a ruler, stands just right of where the Characters panel unrolls — a hidden slot with the panel's
  classes (`characters-panel-slot`), laid out while the panel is rolled up, so the panel never covers the window and the
  window never moves when the panel opens or closes — its top level with the Characters tab, raised only as far as it
  must to stay whole above the bottom of the board. Every left panel (Politics, Reserve, Effects) is narrower, so none
  covers it. A ruler's window has one size at rest (`.ruler-window`, 380×288 points: the same rows for an Initial, a
  Corrupted or an Awakened king, the shorter gists leaving at most a line free; 380×252 since
  [UI-440](#skill-icons--ui-440), the abilities a row of icons); an unrolled ability text or a decision makes it taller.
  A character opened from a region's window stands beside that window, its own panel, as
  before (UI-371); a window the player dragged keeps its place while pinned (`FloatingWindow.Restore`).

Tests: EditMode `PlacePictureTests` (every one of the 105 regions and every place off the map has its 256×160 picture;
every roster lifecycle of both sides and every king's status leads to one), `AvailabilityLinesTests`,
`CharacterWindowPlaceTests`, `SovereignPanelTests` (`TheWindowSaysWhatEachLineMeans`,
`AvailabilityExplainsOnlyProjectedOwnConditionsWithoutGrantingAnAction`), `CharacterRosterPanelTests`; PlayMode
`CharacterWindowLayoutTests` (the five Initial kings' sovereign windows of ui101-sovereigns and the five Awakened kings'
character windows of ui66-full, the Free Peoples at 1366×768 and 1920×1080 at 100 and 125 %, the Shadow at 1366×768 at
125 %: every king's window has the ruler size and one rect beside the Characters panel's place, level with the tab; a
king opened with the panel rolled up stands in the same rect; a character's window opened from his row stands at the
same place; the picture's click selects the king's region; nothing is sent; screenshots `ui436-initial-*`,
`ui436-awakened-*`, `ui436-rolled-up-*`, `ui436-character-*`), `SovereignSectionLayoutTests`,
`CharacterRosterPanelNetworkTests`, `CharacterRosterActionNetworkTests`, `LordsCharacterNetworkTests`,
`BoardCompletionLayoutTests` (each ability line equals `CharacterAbilityChips.Line` and shows its whole text in four
layouts; the short viewport's fixed 32-point height of an inspector's buttons no longer applies to a line), `ReturnListTests`
(the king's place picture locates his region).

Playtest (owner's checklist):
- [ ] The kings' window — Testing catalog `kings-fp-ruler-token` (play the Free Peoples). Open Characters and click each
  king's portrait in turn. Expect: one compact window, the same size at the same place beside the Characters panel for
  every king; his place as a picture named under it; "Corruption n/r" on a meter and when he is corrupted; his action
  in words ("Awaken · Muster or Ruler die" and what it still needs); his abilities in lines. Click the picture: the map
  shows his region. Roll the panel up and open a king from his force on the map: the window opens at the same place.
- [ ] A character's window — the same game: click a companion's row. Expect: the window at the same place, his region's
  picture (or the Fellowship's, "Not in play", "Removed") with what it means beside it, and his statistics.

Port to `ability-chips` (base game): `PlacePresentation` (characters), `PlacePicture`, `AvailabilityLines`,
`AvailabilityRows`, `CharacterWindowPlace` and its slot, the `CharacterInspector` and `CharacterRosterPanel` changes, the
ability lines (`CharacterAbilityChips`), the character-window styles, the export script and its pictures (the removed
king's picture is unused there), and their EditMode tests; the kings' window (`SovereignInspector`,
`SovereignPanel.Position`, `SovereignPresentation`, `CharacterInspector.RefreshRuler`) and `CharacterWindowLayoutTests`
are expansions-only.

<a id="every-ability-has-its-own-icon--ui-439"></a>
## Every ability has its own icon, wherever a button stands for it — UI-439 (2026-11-01)

The owner's playtest (expansions 80df28fd): "make better pictures for the characters' skills. Now they are very simple,
they repeat, they sometimes recycle existing icons. They are small and need not be very complicated, but they should
show the skill's character better. The Balrog has three skills with the same icon; each should be a little different.
Then use the same icons every time a button stands for that skill — for every character, for every skill."

- **The art.** One icon per printed ability of every character card — base game, Lords of Middle-earth (with the
  Council's companions), Kings of Middle-earth and The Fate of Erebor: 76 printed ability ids and one card form, the
  Dark Chieftains' *Dark Chieftain* (their print of `not-minion-for-gandalf-white`, which the Balrog, Gothmog and the
  Witch-king print as *Gandalf the White entry*), 77 icons in all, no two alike (the Balrog's: a horned head of shadow
  and flame, a sword of fire, a whip of fire, a crossed-out white staff). Codex art keyed to alpha, 28 points (56 × 56
  px, the UI kit's import settings) in `Resources/Wotr/Art/UI/abilities/<key>.png`; the pipeline, prompts, rounds and
  the twelve icons waiting for a later Codex round (same paths) are in
  [graphicalassets/abilities](../../graphicalassets/abilities/README.md), every ability and where it was shown in its
  [INVENTORY](../../graphicalassets/abilities/INVENTORY.md). They replace the 22 pictures borrowed from the rest of the
  kit (a battle's crossed swords for twelve abilities, the Politics map for nine…), the character window's one scroll
  for every ability, and the two maps that disagreed (`AbilityIcons` on the roster, `AbilityPictures` on the component
  buttons), both removed.
- **One map** (`AbilityArt`). An ability's icon is `Art/UI/abilities/<printed ability id>`. A command, reaction or
  decision that carries its own id names the printed ability it uses first: the Kings' Ruler abilities drop their
  prefix (`kings.call-to-arms`); `activate-balrog`, `deactivate-balrog` and `use-balrog` are Balrog Activation;
  `gothmog-recruit`/`-upgrade` Fling Them into the Fray; `voice-recruit`/`-upgrade` Saruman's Voice; `lords.nenya.replace`
  Nenya; `lords.chief.spear-of-terror` and `lords.chief.he-sees-he-knows` the Chief's own; the characters' reactions
  (`character.grey-draw`, `character.white-rider`, `character.sorcerer`, the Hobbits', the Council companions' and
  Fate's Gimli's Guide separations, Gollum's reveal, the Council Hobbits' cancel) their printed abilities; Use Narya,
  Use or Decline Vilya and Evil Knowledge by their command kinds. A card's own form comes before the shared one, keyed
  `"<card id>#<ability id>"` as `character-ability-summaries.json` keys the gists. An id that names no printed ability
  shows the ability scroll (a button) or no badge.
- **Where the icons stand** — every place a button, chip or marker stands for an ability (since
  [UI-440](#skill-icons--ui-440), 2026-11-02, every such button is the bare icon, without frame, plate or words, and a
  skill's variants each carry a badge, Far-sighted's and the Balrog's moves included):
  - the Characters roster row's ability icons (15–19 points, `CharacterAbilityStrip`), on the row's card;
  - the character window's ability lines (18 points, `CharacterAbilityChips`), the king's window's too;
  - the component action buttons (24-point glyph on the 32-point frame, `ComponentActions`/`ComponentActionView`) in
    the decision dock, a region's window and the character window: a character's ability, the Mouth of Sauron's Muster
    die moving or attacking, the Balrog's moves, Thranduil's Far-sighted, Narya in Gandalf's window. Where one ability
    has several commands side by side that do different things (Saruman's Voice and Gothmog's Fling recruit or upgrade,
    the Mouth's Muster die — or Great Sorcery's — moves an army or attacks), each button keeps the ability's icon and
    wears a 13-point badge on its lower right corner of what it does (`AbilityBadges`: the Muster face, the level star,
    the boot, the crossed swords), so no two side by side look alike. Far-sighted's buttons all move (the Fellowship, a
    separation, the companions) and keep no badge, as before their tooltips tell them apart. An entry keeps the board
    pin: it enters a character and uses no ability;
  - Narya's activation in the Politics panel (`NationTrackRow`, where the generic confirm stood);
  - in the battle window: Evil Knowledge's framed choice when it stands in the footer (`BattleChoices`), the combat
    replacement's chip and applied marker (Heir of Bard, Shadows in the North, Red Wrath, Lord of the Bats, I Command;
    the source's nation arms stood in for them, `CombatReplacementChoice.Icon`), and a portrait whose click uses an
    ability now — the Mouth of Sauron's Evil Knowledge, the White Rider, the Witch-king's Sorcerer — wears that
    ability's icon (18 points, 14 on a short viewport) on its upper right rim while the offer lasts;
  - the Hunt window's Guide responses (`HuntResponsesView`): the Guide stays recognisable by his figure, and his
    response's ability (Guide · separate, Gollum's reveal, the Council Hobbits' cancel) is a 17-point badge on the
    figure's lower right, outside the round portrait's clip (14 points on a short viewport). The response's name,
    consequence and tooltip are unchanged.

  The console's ring sockets and the starting Guide's crown keep their own art: they show the ring and the Guide role,
  not an ability; so does the Politics panel's advance arrow (the nation's step; a political ability is named in its
  tooltip). The "Click to use" button keeps its action icon. Text-only buttons (the tray's fallback lists, the
  character choice fallback, the Fellowship panel's glowing Guide portrait for Gandalf's draw, Gothmog's Reserve row)
  are unchanged. Hints that named the replacement chip's arms name its ability now: "Click Brand's Heir of Bard beside
  your Combat card's place to use the ability", "Click The Black Serpent's Red Wrath or The Shadow of Mirkwood's Lord of
  the Bats beside…". The battle kit's `battle/evil-knowledge` picture is no longer shown.

Tests: EditMode `AbilityArtTests` (every ability of every card of the four catalogues resolves to an existing icon of
its own, never the scroll; each card's icons differ; 77 icons, no two files alike; the Balrog's four; the card forms;
every command, reaction and command kind above resolves to its ability's icon; an unknown id; the character window's
lines and Narya's activation use the map), `UiArtKitTests` (the 77 icons are in the kit's contract at 28 points with
its import settings), `CharacterAbilityStripTests` (each roster icon is its ability's; a Dark Chieftain's form),
`ComponentActionTests` (Voice's two buttons share the icon and differ by badge; the Mouth's move and attack, the
Balrog's command, an entry, Narya; Far-sighted), `KingsCards02Tests` (the replacement chip and marker, the hints),
`BattleArtTests` (Evil Knowledge's framed choice), `ReactionSourcesTests` (the White Rider's portrait),
`HuntGuideResponsesTests` (Merry's and Gollum's badges); PlayMode `RulerTokenMenuTests` (Dáin's row, `ui439-roster-dain`),
`CharacterWindowLayoutTests` (every line's icon in four layouts, `ui439-character-*`), `FarSightedObjectTests`
(Thranduil's button, no badge, `ui439-component-far-sighted-*`), `KingsDecisionNetworkTests` (the chips and the applied
marker, `ui439-replacement-applied-*`), `LordsBattleNetworkTests` (the Mouth's portrait, `ui439-evil-knowledge`),
`HuntAftermathNetworkTests` (Merry's response, `ui439-hunt-merry-guide`), `T22AbilityNetworkTests` (Saruman's Voice
buttons and their badges, `ui439-voice-recruit`, `ui439-voice-upgrade`), `ExpansionDecisionNetworkTests` (Narya's
activation, `ui439-narya-activation`).

Playtest (owner's checklist):
- [ ] The roster and the window in a base game — any game (the Free Peoples). Open Characters. Expect: each row's
  ability icons its abilities' own (Strider's Guide, Captain of the West and Heir to Isildur differ). Click a
  companion's row: his window's ability lines show the same icons as the row.
- [ ] The roster and the window — Testing catalog `kings-ruler-token-dain` (Free Peoples). Click Dáin's Ruler token,
  open Characters. Expect: Dáin's row shows Iron Guard's and Dwarvenking's own icons, Iron Guard glowing. Click
  Dwarvenking: his window opens with each ability's line showing the same icon as the row.
- [ ] Saruman's Voice — a game with Saruman in Orthanc and a Muster die (the Shadow). Click the die, then Orthanc. Expect:
  two buttons with the same Voice icon, one badged with the Muster face (recruit), one with the level star (upgrade).
- [ ] The Balrog — Testing catalog with the Balrog (Lords). Open his window. Expect: four different icons; the
  activation's button on his figure's window shows the Activation icon.
- [ ] A replacement — `ui348-brand-heir`. Expect: beside your Combat card a gilt chip with Heir of Bard's icon (not the
  North's arms); after the click the marker shows it for both players.
- [ ] Evil Knowledge — `ui347-evil-knowledge` (the Shadow). Expect: the Mouth's glowing portrait wears Evil Knowledge's
  icon on its rim.
- [ ] A Guide's response — seeded `ui52-hunt-aftermath` (a Hunt with Merry as Guide taking damage). Expect: Merry's
  figure in the Hunt window with the Guide-separation icon as a badge.

Port to `ability-chips` (base game): `AbilityArt` (its base entries: the Voice's and the characters' reactions),
`AbilityBadges`, the `IconButton` badge, the roster, character window and component button changes, the battle
portrait badge and the Hunt Guide response badge, with their styles and tests (`AbilityArtTests` asserts the four
catalogues and 77 icons there; on base it holds for the base cards and their 22 icons); the 22 base icons are
`add-gollum-when-no-companions`, `cannot-leave-orthanc`, `combat-strength-plus-one`,
`draw-after-first-round-combat-card`, `enter-and-guide-when-no-companions`, `entry-activates-all-free-peoples`,
`forfeit-leadership-to-cancel-nazgul-for-battle`, `guide-draw-after-event-die-card`, `guide-hide-with-any-action-die`,
`guide-separate-to-reduce-hunt-one`, `ignore-standard-numbered-reveal`, `isengard-elites-have-leadership`,
`may-transform-to-aragorn`, `may-transform-to-white`, `move-four-alone-or-with-one-hobbit`,
`muster-as-army-once-per-turn`, `muster-each-isengard-settlement-or-upgrade-two`, `politics-dwarves-at-erebor`,
`politics-elves-at-stronghold`, `politics-gondor-at-city-or-stronghold`,
`return-after-fellowship-elimination-except-mordor`, `voluntary-reveal-to-reduce-hunt-one`. Narya's activation, Evil
Knowledge, the combat replacements, Far-sighted, the card forms and the other 55 icons are expansions-only.

<a id="skill-icons--ui-440"></a>
## A skill is its icon: a click uses it, the tooltip describes it — UI-440 (2026-11-02)

The owner, about the character window's ability lines ("[icon] Terror on entering play · Entering play activates…"):
"the skill buttons are still not right: a button must not be a rectangular button with text and a picture, it must be
ONLY THE ICON". Then, the same day, after a feasibility analysis of the 77 abilities: "I changed my mind: let's apply
your suggestions so that a click simply uses the skill, with the skill's variants where needed; the skill descriptions
in nicely formatted tooltips", and "make sure a skill's variants stand together, in one row". This supersedes UI-139's
and UI-369's "select the chip, then Click to use" for abilities, UI-436's ability lines and their pages, and UI-371's
kept place (nothing on the window changes size any more).

- **The look** (`SkillIcon`, `Styles/SkillIcons.uss`). A skill button is the skill's icon (`AbilityArt`) alone: no frame,
  plate or words in any state (resolved border 0, no picture of its own; the icon is its glyph, so
  `IconButton.SetPicture` and `AddBadge` serve it). Its box is its hit target. While the skill is usable (ChoiceGlow) a
  rimless gilt light (`ornaments/skill-glow`, a texture — no CSS filter, UI-322) glows behind it and breathes in its
  host's 750 ms ChoicePulse (Reduced motion keeps it bright and still); the icon keeps its size. Hover brightens it (a
  faint light); a press sinks and darkens it (UI-113); keyboard focus rings it (`KeyboardFocusRing`: a click focuses it
  too, but leaves no ring) and opens its tooltip at once. An icon a click does not use now is read only
  (`ReadOnlyControl`: no light, no press, still focusable for its tooltip). A variant not offered now is dimmed in its
  fixed slot. An applied replacement is the same icon with a short gold underline.
- **Where.** The character window's and the king's window's abilities (32-point icons, 28-point pictures); the roster
  row's icons (15–19 points); the component action buttons of the decision dock and a region's window (32 points; an
  entry keeps the board pin on its frame, as it uses no ability); Evil Knowledge's choice in the battle footer (lit and
  pulsing on its own while usable; its tooltip leads with its name); the combat replacement beside the own Combat card
  and its applied marker (Heir of Bard, Shadows in the North, Red Wrath, Lord of the Bats, I Command); Narya's
  activation in the Politics panel (26 points, lit while usable). Unchanged: the Hunt window's Guide responses (the Guide
  is the button, the ability a badge), the battle portraits' ability badges, the console's ring sockets, the tray's text
  fallbacks and the Council setup labels.
- **One row per card, variants together** (`CharacterAbilitySlots`, `AbilityVariants`). The window's icons stand in one
  row in printed order. An ability whose commands do different things shows one icon per variant, side by side as one
  group — 4 points apart over a thin shared gilt line, abilities 14 apart — each badged with what it does
  (`AbilityBadges`): Saruman's Voice (recruit: the Muster face; upgrade: the level star), Gothmog's Fling Them into the
  Fray (the same), the Mouth of Sauron's Muster die and Great Sorcery (move: the boot; attack: the crossed swords), the
  Balrog's Activation (activate or deactivate, one slot, no badge; move an army, attack, move alone: the board pin) and
  Thranduil's Far-sighted (the Fellowship: the Ring; companions: the group; a separation: the board pin — the component
  buttons carry these badges too). The slots are fixed per card, so nothing shifts while a variant comes or goes; a group
  never wraps. The Balrog's seven icons are the most any card shows; they fit the 380-point window in every layout. The
  window's former Actions row of the same abilities (Narya, the Muster die's move and attack, Great Sorcery, the Balrog's
  commands, Far-sighted) is merged into the row; only an entry (several versions) keeps its own button. A ruler's
  window at rest is now 380×252 points (`.ruler-window`, `SovereignPanel.WindowSize`; UI-436's 288 held the lines), every
  king's window still of one size.
- **A click uses the skill.** A lit icon (its ability usable now with the chosen die or token) uses it with one click,
  through the component path (`ComponentActions.Select`, `TurnInteraction.ChooseComponent`) exactly as Click to use did:
  a one-click ability (the political steps, the messengers, Dwarvenking, Stewardship, Elvenking, Narya, Voice's upgrade)
  sends at once; a click-then-choose one (Call to Arms, Iron Guard, Mustering of the Mark, a variant's move or attack)
  starts its choice on the board, confirmed with the console's ✓ (UI-381). Before any die is chosen, an icon whose one
  die (or identical dice) could pay was clickable and took that die (`ComponentActions.WithUniformDie`) until
  [UI-443](#nothing-lights-before-its-die--ui-443) retired it: before a die every icon with die-paid actions says "Choose
  an eligible die first". Every other icon is read only and its click does nothing: a
  passive ability, a triggered one (Take Them Alive!, Art of Disappearing), a reaction (used on the object that offers
  it, UI-247/UI-271) and a change of version (from the Characters row). A king's window shows his abilities read only (an
  Awakened king's character window uses them as above). Nothing unrolls: the parchment and Click to use are gone.
- **The roster row** keeps its click: one ready action of the ability starts it; otherwise the character's window opens
  with that ability's icon focused, so its description shows.
- **The description is the tooltip** (`SkillTooltip`, rich text, `RuntimeTooltip.FormattedClass`), one form wherever a
  skill's icon stands (the windows, the roster row, a component button): the name in bold gold (then its variant and, on
  a component button, its owner), the gist dim on the second line (`CharacterAbilitySummaries`), a thin gap, the printed
  text as a paragraph, the source small and faint, a thin gap, then the state — what a click does now in bright gold
  ("Click to use your Muster die: Move the Dwarves one step toward war"; `SkillUse`), or dim: "Choose an eligible die
  first", "Usable with an eligible die or token", where a reaction is used (`AbilityUsePlaces`: "Offered in battle: click
  his portrait", "Offered in the Hunt window: click this Guide's response", "Offered after your action: click Vilya on the
  console", "Used from his Characters row with a Will of the West die"…) or, on the roster, "Click to open the
  character's window". The tooltip keeps its 360-point width; the longest printed text (Emissary from the West, 381
  characters) fits 1366×768 at 125 %.

Tests: EditMode `SkillIconTests` (the parts, the name required, the glow and mark), `CharacterRosterPanelTests`
(`InspectorShowsStatusStatisticsAndAbilityChipsWithoutUnrolledText`: bare, read only, formatted, no unrolled text;
`AUsableAbilityGlowsAndItsClickUsesIt`: the click raises the very action Click to use raised;
`AnAbilityWithOneEligibleDieTakesItFromTheClick`; `AVariantAbilityShowsOneBadgedIconPerVariantSideBySide`;
`EveryCardsVariantsStandTogetherInOneRow`: for every card of the four catalogues each ability's icons stand together in
printed order and the widest card fits one row), `CharacterAbilityStripTests`, `ComponentActionTests` (bare icons, the
entry's frame, Narya's and Voice's formatted tooltips, Far-sighted's badge), `SovereignPanelTests`, `AbilityArtTests`,
`KingsCards02Tests` (the replacement icon and marker), `BattleArtTests` (Evil Knowledge's bare footer choice),
`UiArtKitTests` (`ornaments/skill-glow`); PlayMode (resolved styles through `SkillIconChecks`: border 0, no plate, no
words, the light exactly while usable, at least 28 points in the windows) `CharacterWindowLayoutTests` (the kings' and a
character's windows in four layouts: one row, a read-only click changes nothing and leaves no ring, keyboard focus rings
the icon and shows the formatted description whole, the longest — Emissary from the West, 381 characters — included;
`ui440-king-*`, `ui440-character-*`, `ui440-longest-tooltip-*`),
`CharacterRosterActionNetworkTests` (Fate Gimli's Messenger of the Dwarves sends at one click a payload JSON-equal to the
old Click to use path's; Théoden's Mustering of the Mark starts its choice; the Balrog's seven icons in one row in four
layouts and his move alone from its badged variant; `ui440-usable-*`, `ui440-widest-balrog-*`),
`CharacterRosterPanelNetworkTests` (the roster icon bare; Gimli's passive icon's formatted tooltip and read-only click;
`ui440-roster-*`), `RulerTokenMenuTests` (Iron Guard starts from its icon, Dwarvenking read only; Dáin's roster icons),
`T22AbilityNetworkTests` (Gimli's Dwarf of Erebor and Voice's upgrade variant sent at one click from the window's icons,
each payload JSON-equal to the old component path's on the same die; Voice's bare badged buttons, `ui440-component-*`),
`FarSightedObjectTests`,
`KingsDecisionNetworkTests`, `ExpansionDecisionNetworkTests` (Narya), `BoardCompletionLayoutTests` (every card's icons and
formatted tooltips in four layouts, inside the board; `MeasureTextSize` measures rich text a line taller than its layout
draws it, so the formatted descriptions are not measured against it).

Playtest (owner's checklist):
- [ ] A one-click skill — Testing catalog `ui67-fate` (Free Peoples): open Gimli's window. Expect: his abilities as bare
  icons in one row, no lines, no text. Hover each: a formatted description (gold name, gist, text, source, state). Click
  the die Messenger of the Dwarves needs: its icon glows. Click the icon: the Dwarves activate at once, no Click to use.
- [ ] A click-then-choose skill — `kings-ruler-token-dain`: click Dáin's Ruler token, open his window. Expect: Iron
  Guard glowing, Dwarvenking read only (no light under the pointer, the click does nothing). Click Iron Guard: his
  settlements light on the map; place the figures; confirm with the console's ✓.
- [ ] Variants together — the Balrog (`ui67-balrog`, Shadow): choose the Character die, open his window. Expect: seven
  icons in one row; Balrog Activation's four (activate, move an army, attack, move alone) side by side over a thin gilt
  line, each move badged; the move alone glows. Click it, then the Balrog's destination: he moves.
- [ ] Saruman's Voice — a game with Saruman in Orthanc (Shadow): open his window before choosing a die. Expect: Voice's
  recruit and upgrade side by side, dimmed; hover: "Choose an eligible die first" (or, with one Muster die only, "Click
  to use your Muster die: …" — a click then takes that die; since UI-441 both icons then glow, and Saruman with them).
- [ ] A reaction — open the Witch-king's or Galadriel's window. Expect: Sorcerer or Nenya read only, its tooltip saying
  where it is used ("Offered in battle: click his portrait", "… click Nenya on the console").
- [ ] Keyboard — Tab to an icon. Expect: a thin gold ring and the description at once; a mouse click leaves no ring.

Port to `ability-chips` (base game; UI-436/UI-439 at 164d87d9, where the kings' window kept its older form): `SkillIcon`,
`KeyboardFocusRing`, `Styles/SkillIcons.uss` (and its import), `ornaments/skill-glow` (svg, `assets.json`,
`manifest.json`, PNG and meta, `UiArtKitTests`), `IconButton.GlyphName`, `RichText` (and `CharacterRowTooltip` on it),
`SkillTooltip`, `SkillUse`, `AbilityUsePlaces` (its base entries: the Grey's Guide draw, the White Rider, the Sorcerer,
the Guides' separation, Gollum's reveal, Take Them Alive!, the two changes of version), `CharacterAbilityLibrary.Find`,
`CharacterAbilitySlots`, `AbilityVariants` (on base only Voice and the Mouth's Muster die appear; the table may move
whole), `AbilityBadges` (the new kinds), `ComponentAction.Definition`, `ComponentActions.WithUniformDie` and the
`Definition`s, `TurnInteraction.ChooseComponent`, `CharacterAbilityChips`, `CharacterAbilityStrip`, `CharacterInspector`,
`CharacterRosterContext`, `CharacterRosterPanel` (the kept place removed, `Present`), `ComponentActionView`, the
character-window and roster rules of `Characters.uss` and the component rules of `Wotr.uss`, with their tests
(`SkillIconTests`, both `SkillIconChecks`, `CharacterRosterPanelTests`, `CharacterAbilityStripTests`,
`ComponentActionTests` without Narya and Far-sighted, `AbilityArtTests`' window test on a base card,
`CharacterRosterPanelNetworkTests`, `BoardCompletionLayoutTests`, `T22AbilityNetworkTests`). Expansions-only: the king's
window (`SovereignInspector`), Evil Knowledge's footer choice (`BattleChoices`, `Battle.uss`), the combat replacement's
icon and marker (`BattleContext`, `Interaction.uss`), Narya's activation (`NationTrackRow`, its `Wotr.uss` rule), and
`KingsCards02Tests`, `KingsDecisionNetworkTests`, `FarSightedObjectTests`, `ExpansionDecisionNetworkTests`,
`RulerTokenMenuTests`, `CharacterWindowLayoutTests`, `BattleArtTests`' Evil Knowledge part and the Fate, Ruler and Balrog
parts of `CharacterRosterActionNetworkTests`.

<a id="usable-skills-light-their-character--ui-441"></a>
## A character whose skill can be used now lights up wherever it stands — UI-441 (2026-11-02)

The owner, with a screenshot of the Characters panel (Gothmog's row unlit under On board) beside Gothmog's window (his
two Fling Them into the Fray icons lit): "skill postaci jest mozliwy do uzycia ale nie powoduje to podswietlenia sie
postaci, tylko samego skilla. upewnij sie ze zawsze podswietla sie postac jesli mozna w danym momencie uzyc jej skilla"
— the character's skill could be used, yet only the skill lit, never the character; make sure the character always
lights up whenever one of its skills can be used at that moment. (Fling is used on Gothmog's Reserve tokens, UI-271, so
the roster, which left such abilities out, never lit him.)

- **One rule** (`UsableSkills.Characters`, built once per refresh in `ComponentContextInteraction` from the turn's
  `ComponentActions`). A character of the player's has a skill usable now exactly while one of its skill icons lights in
  its window (`CharacterAbilityChips`): an action of the turn that uses one of the printed abilities of its current card
  — its own command or a variant of it (`AbilityVariants`: Saruman's Voice, Gothmog's Fling, the Balrog's activation), a
  Muster die's move or attack, Far-sighted, Narya; an ability used on its own object included (Gothmog's Reserve tokens,
  the Balrog's figure) — offered with the chosen die or token (before
  [UI-443](#nothing-lights-before-its-die--ui-443) also, before any die, payable by the one die that could pay). A reaction of the
  character's offered to this player now counts too (a battle portrait's White Rider or Sorcerer, a Guide's Hunt
  response — its Guide's when it names no source —, Gandalf the Grey's draw on his Guide portrait or its deck, Spear of
  Terror, Nenya, He Sees, He Knows), named by the printed ability it uses (`AbilityArt.Printed`). Nothing counts while
  another action is drafted, while a command is being sent, for an entry, for another version's ability or for the
  opponent's characters.
- **The window's icons follow the same rule**, so the icon and its character light together. (UI-441 lit the one-die
  icon before any die; UI-443 put it out again.) The roster row's icons use the abilities used on their own objects, as
  the window's do. A
  reaction's icon stays read only (UI-440): its character lights while the reaction is offered.
- **Where the character lights**, each in its own pulse, with the shared light (no CSS filter; the pulses change
  colours and opacity only; nothing is laid out in a geometry callback):
  - its **Characters row** — the shared ChoiceGlow and the roster's pulse, as an offered entry (UI-240); its tooltip
    leads in bright gold with why: "Usable now: Fling Them into the Fray · Click its glowing icon" (a reaction: "Usable
    now: The White Rider · Click the glowing portrait in the battle window", `SkillReactionPlaces`);
  - the **Characters tab** while some row is lit (`legal-target`, the edge tabs' pulse, UI-380);
  - its **map miniature** (the ability-owner glow, UI-374/UI-402) — or, for an Initial king without a miniature, the force
    he leads — pulsing with the map's choices; its tooltip leads with "Usable now: … · Click its glowing icon in
    Characters" (`MapCanvas.LightSkills`, `MapCharacterFigures.LightSkills`; a figure is matched by its id or any of its
    versions', so Gandalf the White's miniature lights for his row);
  - its **Fellowship portrait** (a companion in the Fellowship) — ChoiceGlow with the roster's pulse; a portrait whose
    click makes another choice now keeps that choice's words, and a portrait lit only for a skill opens the companion's
    window on a click (`FellowshipContextInteraction`);
  - a **king's portrait** among the Sovereigns — lit as a decision lights it (UI-274); its click opens his character
    window, where the icon uses the skill, also for an Initial king (`SovereignPanel`);
  - its **window's header** — a gilt rim on a warm plate behind the plate art and title (`character-header-glow`), shown
    while the window shows the version in play, breathing in the window's one pulse with the lit icons (opacity only).
  The light goes out with the last usable skill; every other light and colour is unchanged.
- **Before any die is chosen** nothing that needs a die is lit (UI-443; UI-441 had lit a skill one die could pay, and
  the Characters tab with it): a released die puts out every tab it lit (UI-386), the Characters tab included — Brand's
  Call to Arms with the Ruler die as its only payer (`RulerTokenMenuTests.EveryLitTabGlowsAlike`).

Tests: EditMode `UsableSkillsTests` (a one-click skill with its die, a die that pays nothing, the one die before any die
is chosen vs two different dice, any variant of Voice lighting Saruman once, Gothmog's Fling on its Reserve tokens, the
window's icon lit exactly while the rule names Gothmog with the die and with the one die, nothing while another action
is drafted or a command is sent, entries, another version's ability and the opponent's characters, a reaction and its
withdrawal, a Guide's Hunt response without a source and a card's reaction), `CharacterRosterPanelTests` (the one-die
icon glows); PlayMode `RulerTokenMenuTests.EveryLitTabGlowsAlike` (the tab after the die's release) and
`UsableSkillLightNetworkTests` on real seeds — Gothmog's Fling Them into the Fray
(`t34-gothmog-upgrade`), Saruman's Voice (`ability-voice-recruit-full`), Gimli's Dwarf of Erebor (`ability-gimli`),
Théoden's Mustering of the Mark after his Ruler token (`ui67-ruler`), Gandalf the Grey's draw (`grey`) and the White
Rider (`white-rider`): before a die, with the skill's die, with a die that pays none of its skills and with the die
released (or before and after the reaction), the window's icon and header, the Characters row and its tooltip, the
rolled-up Characters tab (lit exactly while the panel holds a light), the map figure (`GlowingTargetChecks`: strong,
pulsing, uncovered on screen), the king's and the Fellowship portraits glow exactly while the icon does (for a reaction,
while it is offered); screenshots `ui441-<case>-row|tab|map|window|fellowship.png` and, for Gothmog under Reduced
motion at 1920×1080 at 125 %, `ui441-gothmog-row|window|map-1920-1.25.png`.

Playtest (owner's checklist):
- [ ] The owner's case — Testing catalog `t34-gothmog-upgrade` (Shadow): click the Muster die. Expect: Gothmog's row
  under On board glows and pulses (its tooltip: "Usable now: Fling Them into the Fray · Click its glowing icon"), the
  Characters tab glows while the panel is rolled up, his figure in Minas Morgul glows, and his window's header glows
  above his two lit Fling icons. Click a Character die instead: all of them go out together.
- [ ] Before a die — the same game, no die chosen (one Muster die only). Expect: Fling's icons and Gothmog glow already;
  a click on an icon takes the Muster die and uses it.
- [ ] A reaction — `white-rider` (Free Peoples, the battle starts). Expect: Gandalf's row and his figure glow, the row's
  tooltip saying "Click the glowing portrait in the battle window"; after the White Rider (or Pass) they go out.
- [ ] A king — `ui67-ruler`: click Théoden's Ruler token. Expect: his row and his portrait among the Sovereigns glow;
  a click on the portrait opens his window with Mustering of the Mark lit.

<a id="a-standard-corruption-attempt-draws-all-its-tiles-with-its-click--ui-442"></a>
## A standard corruption attempt draws all its tiles with its click — UI-442 (2026-11-02)

The owner: the Shadow picked an Eye, clicked the king's attempt, and the console then asked to draw ("Draw tiles"), though
nothing can come between the attempt and its draws (T28: a tile for each Eye left in the Hunt Box, up to three; an Eye
or special tile is revealed and replaced; a Kings tile ends the attempt). "dla sequential corruption zostawmy obecne
zachowanie, w innym przypadku klikniecie na ikone rulera od razu pobiera oba tokeny" — with Sequential Corruption keep
the current behaviour; otherwise the attempt's click draws the tiles at once.

- **The server draws** (server `CorruptionRuntime`, EXPANSIONS K02/X02; Kings rulebook pp. 12–13, 17–18): the standard
  `AttemptCorruption` (and The West Has Failed's choice of a king) draws every tile in its own command. Two or three
  numeric tiles open the private choice (`CorruptionChoice`, `ChooseCorruptionTile` only); a single one is the only
  possible choice, so the server places it (EXP-15 D8) and the attempt ends in that revision; a Kings tile ends it on the
  king. History, the Shadow-only `OwnCorruptionTile` events and the Free Peoples' backs are unchanged.
- **The client** shows no Draw tiles for a standard attempt: the attempt's click (the lit button beside the king, after
  the chosen Eye, UI-398, UI-430) leads straight to the glowing tiles beside the king and the hint "Click a glowing tile
  beside the king to attach it", or to the placed tile. The attempt's tooltip says what the click draws: "Draws a
  Corruption tile and places it on Dáin", "Draws 2 Corruption tiles at once · Then click the one to place on Dáin", or in
  Sequential corruption "Draws up to 2 Corruption tiles, one at a time · Keep a tile or draw the next"
  (`SovereignAttempt.Tooltip`). The king's window names the choice's consequence ("Attach one drawn tile; the others are
  revealed and return to the Hunt pool"). UI-399's `CorruptionDrawChain` is removed.
- **What stays:** Sequential corruption's console Draw again and the latest tile's click (UI-274, UI-423), and its
  `CorruptionDraw` step after an Eye tile was drawn. A standard draw step saved by an earlier build still shows Draw
  tiles; its one click now draws the remaining tiles at once.

Tests: EditMode `SovereignPanelTests` (the attempt's tooltip for one, two and Sequential draws; the choice without a draw,
its hint and consequence; a saved draw's Draw tiles); PlayMode on the pinned backend `ExpansionCorruptionNetworkTests`
(`ui09-corruption-hidden/visible`: one click, one revision, two own tiles at the choice; `kings-sh-corruption`: the
tile placed with the click; the Sequential cases unchanged), `SovereignPanelNetworkTests.Replacement`
(`ui59-replacement-kings`: the click reveals the Eye tile, the pool refills and the Kings tile ends the attempt),
`KingsCards02NetworkTests` (`kings-west`, `kings-west-draw`: the event's two tiles at the choice,
`KingsCardNetworkChecks.AtTileChoice`) and `OpponentExpansionPresentationNetworkTests.Corruption`.

Playtest (owner's checklist):
- [ ] Two tiles — `kings-corruption-three-tiles` (Shadow, four Eyes) or a Kings game with three Eyes in the Hunt Box:
  click an Eye in the dice row, then the lit attempt beside a king. Expect: no Draw tiles; the drawn tiles stand at once
  beside the king (three here, values for the Shadow, backs for the Free Peoples), and a click on one attaches it.
- [ ] One tile — `kings-sh-corruption` (Shadow, two Eyes): the same clicks. Expect: the one tile is placed on the king
  with the attempt; no choice and no Draw tiles.
- [ ] Sequential — `kings-variant-sequential-corruption`: the same clicks. Expect: one tile beside the king and the
  console's Draw again; the latest tile's click keeps it, as before.

<a id="the-effects-panel-keeps-every-source-and-its-width--ui-424"></a>
## The Effects panel keeps every source and its width — UI-424 (2026-11-02)

Found by TEST-15's `layout-effects-sources` (the panel with 18 sources reached 592.6 points against the bottom row at
474.4 at 1366×768 and 125 %; the same defect failed `EffectsLayoutTests.MultipleSources`), and reported by the owner with
two screenshots of that scenario: "czesc nazw efektow nie miesci sie w swoich okienkach" — long names (Mithril Coat and
Sting, The Power of Tom Bombadil, The Lore of the Elven Rings, The Palantír of Barad-dûr, Threats and Promises, Worn with
Sorrow and Toil) wrapped out of their parchment tiles — and "przy wybraniu pewnych skilli layout effects sie rozjezdza":
clicking a source (Captain of Despair) widened the whole panel to almost the screen's width (UI-94's reading width).

- **The grid's arrangement is chosen before layout** (`EffectsGridArrangement`) from the number of sources and the fixed
  height a docked panel has between the rail's top and the bottom row (518 points at 100 %, 364 at 125 %, the same at
  1366×768 and 1920×1080). The narrowest grid whose rows fit wins: two columns before three before four, regular rows
  (42-point tiles, type 12 points, 50-point pitch) before dense ones (38-point tiles, type 11 points, 45-point pitch);
  every title stays a 32-point target and nothing scrolls or hides behind "more". Each column count has one panel width
  (two 353, three 506, four 637 points), so a source's details, its × or a lit state never change the panel's width or
  columns. The arrangement changes only when the source count changes or when the stage takes another size (after
  layout); the panel's own geometry never changes it, which TEST-15's three-column attempt did ("Layout update failed to
  stabilize"). The owner's 18 sources: two columns at 100 %, three at 125 %; 22 sources: three at 100 %, four at 125 %.
- **Every name fits inside its tile** (`EffectTitleFit`): the parchment's frame is drawn finer (about 4 points) and a
  title's text keeps 4 points inside the tile's top and bottom, so two lines stand inside the frame. The title's runs of
  words are measured once per name, outside layout callbacks; it keeps the arrangement's size when its wrap at the tile's
  fixed text width (less the card symbol, the owner label and the ×) fits that height, and otherwise takes the largest
  smaller size that does, keeping two points of the height for the rounding of a large screen's pixels (not below 8
  points): a long two-line name takes 11.5 points in a regular tile; with a × in three or four columns the longest names
  of the catalogue take 9–10 points. A title measured before its style was resolved is measured again. An entry's border
  is always 2 points (gilt when selected), so selecting a source moves no title.
- **A source's details open on their own card beside the panel** — retired by
  [UI-446](#a-card-effect-shows-its-active-part--ui-446) (`EffectDetailsPlace`, 288 points wide on the panel's
  frame): the state ("Shadow · In play"), the scopes with their Locate buttons and expiry, and the card's text parchment
  wrap inside the card, which stands level with the panel's top and moves up only as far as it must to end above the
  bottom row. The grid neither moves nor widens; UI-94's reading width (up to 55 % of the board) and QUAL-19's compact
  panel are gone. While a removal is paid the details stand inside the panel above the payment, as before. The card's
  name and state each stand on one line; when the panel closes the card goes at once, so it covers nothing (a Hunt
  window's response) while the panel fades out. The tiles' tooltips say "beside the list" instead of "below".
- **The hover unroll (UI-221)** stays under its tile for a tile in the board's upper half; a tile in the lower half
  unrolls its text upward from above the tile, so the text no longer runs under the bottom row.
- **What stays:** the sources' order, their tooltips' actions, the click that shows a source (or discards a red tile,
  UI-430, or uses a lit card, UI-272), the header with only its close button (UI-429).

Tests: EditMode `EffectsGridArrangementTests` (the arrangement for 0–28 sources at both heights, the narrowest first,
one width per column count, the details card's place) and `EffectTitleFitTests`; PlayMode `EffectsLayoutTests`
(`MultipleSources…` and the new `ManySourcesAndTheLongestCardTextFitWithoutWideningThePanel`: 22 cards with the
catalogue's longest names, four with ×, and Dead Men of Dunharrow's text, four layouts, screenshots `ui424-many-*`),
`LayoutStressScenarioTests.TheFullestEffectsPanelFits` (the UI-424 warning is an assertion; titles inside their tiles,
the panel and the longest text's details above the bottom row, the same width and columns with the details open;
screenshots `ui424-effects-sources-*`, `ui424-effects-details-*`), `InGameDenseLayoutTests` (titles inside their tiles),
`EffectsNetworkTests` (the card text in the details beside the panel). Shared checks: `EffectsPanelChecks`.

Playtest (owner's checklist):
- [ ] Names — `layout-effects-sources` (Free Peoples), Interface scale 100 % and then 125 %: open the Effects tab.
  Expect: every source in view, two columns at 100 % and three at 125 %, every name (Mithril Coat and Sting, The Lore of
  the Elven Rings with its ×, Worn with Sorrow and Toil) inside its parchment, the panel's bottom above the dice row.
- [ ] Details — the same panel: click Captain of Despair, then The Gates are Closed. Expect: the details card opens to
  the right of the panel with "Shadow · In play", the expiry and the card's text; the panel keeps its width and its
  columns, no tile moves, and the card ends above the dice row.
- [ ] Few sources — `fp-army-event` (Free Peoples): play A Power too Great with the Army die, open Effects and click it.
  Expect: a two-column panel level with its tab, the details card beside it (moved up only if it would pass the dice
  row); hovering the tile unrolls its text above the tile.
- [ ] Payment — a game where the Shadow may remove A Power too Great: choose a die, open Effects and click its ×.
  Expect: the details stand inside the panel above the cost and the payment, as before.

<a id="one-release-line--exp-17"></a>
## One release line: the expansions and the base-game UI — EXP-17 (2026-11-02)

The owner: "let's merge everything into one branch, we want to make a release and work on everything at once —
expansions and the UI changes". `release/0.10.0-rc.1` is `expansions` with `ability-chips` merged in, recorded only:
every `ability-chips` change was already on `expansions`, equal or a superset (the base ports above, "Port to
`ability-chips`"), so the tree is the expansions one. What only the base-game line held is carried: the Palantír of
Orthanc's deck choice (`DeckChoiceTests`, `GlowingHintAuditTests`' `shadow-palantir-draw` case), Gandalf the Grey's drawn
deck (`GlowingHintClassesTests`), a reserve figure lifted as its own icon in `BoardInteractionTests`' drag (UI-393), the
Strike's payload and hint checks (`ObjectFirstChoicesTests`, `Shadow02NetworkTests`), and the base playtest items of
UI-430, UI-434, UI-437 and UI-439. The "Port to `ability-chips`" and "base parts" notes record history; new work branches
from the one line.

<a id="nothing-lights-before-its-die--ui-443"></a>
## Nothing lights before its die — UI-443 (2026-11-02)

The owner, reversing UI-440's decision 7 and UI-441's one-die light: "without a chosen die we do not light the options
available with only one die, which in theory makes choosing it unnecessary". Dice establish context: the player chooses
the die (or token), and its uses light (UI-389).

- **The rule** (`UsableSkills.Usable`): a character's ability lights — its icon in the character's window and on the
  roster row, the character's row, map miniature, Fellowship and king's portraits, the window's header and the
  Characters tab — and its icon's click uses it only while it is offered with the chosen die or token
  (`ComponentAction.Enabled`). Before a die is chosen an icon with die-paid actions is read only, unlit, and its tooltip
  ends "Choose an eligible die first"; its click does nothing. `ComponentActions.WithUniformDie` and
  `UsableSkills.UniformDie` are gone.
- **Unchanged:** a die's or token's click lights all its uses (UI-389); a Ruler token takes its only Ruler die (UI-380);
  Vilya's socket answers with its one die (UI-268); a chosen character action takes its cost from one die or identical
  dice (UI-239, `CharacterDraft.UniformDie`); reactions offered now need no die and still light their character (UI-441).

Playtest (owner's checklist):
- [ ] Gimli — seeded `ability-gimli` (the Free Peoples). Before any die: Gimli's row, miniature and the Characters tab are
  dark; his window's Dwarf of Erebor icon is unlit and says "Choose an eligible die first". Click a die that can pay:
  Gimli and the icon light; the icon's click moves the Dwarves. Release the die: everything goes dark.
- [ ] Brand — Testing catalog `kings-brand-call-to-arms` (the Free Peoples). Click the Ruler die: Politics, Reserve and
  Characters light. Click it again: all three go dark, the Characters tab too.
- [ ] Théoden — `kings-ruler-token-theoden`. Before the token nothing of his lights; his Ruler token's click lights
  Mustering of the Mark; a second click puts it out.

Tests: EditMode `UsableSkillsTests.BeforeAnyDieIsChosenNoSkillIsUsableEvenWhenOneDieCouldPay`,
`UsableSkillsTests.TheWindowsIconsLightExactlyWhileTheRuleNamesTheCharacter` (lit with the die, dark before it),
`CharacterRosterPanelTests.AnAbilityWithOneEligibleDieWaitsForTheDie`,
`ComponentActionTests.AnAbilityIsNotSelectedBeforeItsDieEvenWhenOneDieCouldPay`; PlayMode `UsableSkillLightNetworkTests`
(on real seeds nothing is lit before a die or token and after its release) and `RulerTokenMenuTests.EveryLitTabGlowsAlike`
(the Characters tab goes dark with the released Ruler die).

<a id="a-carried-figure-stands-whole-above-the-hud--ui-444"></a>
## A carried figure stands whole above the HUD — UI-444 (2026-11-02)

The owner (layout audit item 8): fix the carried army that the map's edge cut in half while the map panned under it
(UI-407's defect 4). While carried, a figure stands in a layer in front of the HUD and the windows and stays whole on
the screen, shifting inward only as far as the edge requires; set down it is back on the map, clipped as before.
Details and tests: [TABLETOP_FIGURES_UX](TABLETOP_FIGURES_UX.md), "As built (UI-444)".

Playtest (owner's checklist):
- [ ] `fp-army-move` (the Free Peoples). Choose an army, carry it to each edge of the screen and hold it there: the map
  pans, the army shows whole, a little inside the edge, above the top bar, the console and the rails; dropped on a lit
  region it moves as its click would; Esc puts it back.

<a id="the-full-expansions-roster-fits--ui-445"></a>
## The full expansions roster fits — UI-445 (2026-11-02)

The owner (layout audit item 2): fix the Characters panel that reached under the console at 1366 × 768 at 125 %. Since
the expansions joined (EXP-14) a Kings roster — 13 characters on board, one not in play, five Sovereigns — was 409
points tall in UI-408's compact form (four a line on a short viewport) with 374.6 points of room under the rail's top
(`CharacterRosterLayoutTests`: the panel's bottom at 508.8 against 475.4).

- **One more column** (`CharacterRosterPanel`, `roster-dense`): a roster still too tall in the compact form takes one
  more 110-point row a line — the panel 572 points wide on a short viewport (five a line), 462 otherwise (four) — the
  rows and their icons as in compact. The step follows UI-408's rule: taken when the panel reaches past the board area,
  tried again from two a line only when the roster's size or the room changes, so the form never flips. The left
  panels' 45 % cap does not apply to it.
- **The window still stands beside it**: the window's slot (`CharacterWindowPlace.Widen`) takes the same width, so a
  character's or ruler's window opens just right of the wider panel (UI-436).
- **The Sovereigns label** is 18 points on a short viewport, as "On board" and "Not in play" are.
- No scrolling, no smaller rows; nothing moves above the rail's top.

Playtest (owner's checklist):
- [ ] A Kings game with every character in play — 1366 × 768 at 125 % (Windowed, Interface size 125 %). Open Characters:
  five characters a line, the Sovereigns whole above the console; click a row: its window opens beside the panel.
  At 1920 × 1080 the panel keeps two a line.

Tests: PlayMode `CharacterRosterLayoutTests` (both seats, 1366 and 1920, 100 % and 125 %: the panel and its window
inside the board, apart, every button whole and uncovered; `ui66-roster-*`), `CharacterRosterPanelNetworkTests`,
`CharacterWindowLayoutTests`, `LeftRailLayoutTests`, `LayoutStressScenarioTests`, `RulerTokenMenuTests` (two a line
where they fit).

<a id="a-battle-window-keeps-its-lit-regions-clear--ui-435"></a>
## A battle window keeps its lit regions clear — UI-435 (2026-11-02)

Found by UI-434's audit, fixed at the owner's request (layout audit item 9): with Scouts the battle window's middle place
(UI-324) covered Helm's Deep — the region whose click withdraws into the siege — at 1920 × 1080 at 125 %, and the
console hid what the window left; the map cannot pan its west edge past the screen, so neither a camera move nor the
window's move alone cleared it.

- **The rule** (`RegionalBattlePanel.KeepClear`, after the draft's lights are drawn, `MapDraftOverlay.Applied`, and on a
  map size change): once per decision and map size, when a region the decision lights (`DraftOverlay.Targets` and
  `AttackTargets`) has its anchor under the window or the HUD (`MapCanvas.Uncovered`: outside the screen or inside one
  of the map's occlusions), the window keeps to the middle's top edge when the lit regions lie below the board's centre
  (otherwise its bottom edge), still centred across the middle (`FloatingWindow.Flush`), and once it stands there the
  view brings the lit regions into the free strip it leaves (`MapCanvas.Reveal`). While that decision lasts, a camera
  move keeps the window at that edge instead of re-centring it.
- **Unchanged:** a decision whose lit regions all show opens the window in the middle, as before; a window the player
  dragged keeps its place (only the view moves); a new decision returns to the middle; nothing moves for a battle
  without lit regions (UI-149's incoming battle neither opens nor pans).

Playtest (owner's checklist):
- [ ] Scouts — seeded `strategy-scouts` (the Free Peoples), Windowed 1920 × 1080 at 125 %. Expect: the battle window at
  the top of the board's middle, Helm's Deep and Fords of Isen lit in the strip below it, above the console; Helm's
  Deep's click withdraws into the siege. At 1366 × 768 the window opens in the middle as before.
- [ ] In the same battle drag the window by its header anywhere: it stays where you put it (the next decision too).

Tests: PlayMode `GlowingHintAuditTests.ScoutsBattleRegionAndRetreatAreLitOnScreen` (both layouts: the window open,
Helm's Deep and Fords of Isen lit with exposed terrain; `ui434-scouts-*`), `BattleLayoutTests`,
`BattleWindowNetworkTests`, `BoardRuleDeclineTests`, `Strategy01NetworkTests`, `FloatingWindowTests`.

<a id="a-card-effect-shows-its-active-part--ui-446"></a>
## A card effect shows its active part; no details beside the panel — UI-446 (2026-11-02)

The owner: "if an effect comes from a card, we should show only the effect that is active; the other one need not show
when we hover. The window that appears on the right after clicking an effect is to be removed." This supersedes UI-94's
click that shows a source and UI-424's details card beside the panel.

- **The hover text** (UI-221, `EffectUnroll`): a card shows its heading and only the part in effect — its Event text, or,
  for a source whose every effect lasts a battle or a combat round, its Combat text (`EffectsPresentation.CombatPart`,
  from the projected `Duration`). Every card effect the server projects today is an Event's (a table card, a lasting
  event), so the Combat part no longer shows in the panel. An ability or effect still unrolls its descriptions.
- **A click** opens nothing and selects nothing: it discards a red table card (UI-430), uses a card the decision uses
  (UI-272), or — the board rule of UI-430 — prepares the removal the tile's × offers when the tile is lit for it.
  Otherwise the tile is read only (`ReadOnlyControl`: no hover frame, no press, it keeps its plate and parchment) and its
  tooltip names it ("Wizard's Staff · A card in effect").
- **A removal being paid** keeps its details (state, scopes, expiry) inside the panel above the payment, as before; the
  details never stand beside the panel (`EffectDetailsPlace` is gone).
- `TableEffectsView.Inspect` opens the panel and unrolls the source's text as a hover does (`UnrolledSource`).

Playtest (owner's checklist):
- [ ] Testing catalog `layout-effects-sources`. Hover a card tile: only its Event text unrolls (no Combat part). Click a
  plain tile: nothing opens on the right and the tile does not press. In a game with The Nazgûl Strike! the red tiles'
  click still discards; with a removal offered, the lit tile's click starts the removal like its ×.

Tests: EditMode `EffectsPresentationTests`, `LordsFinalCardsTests`; PlayMode `EffectsLayoutTests`, `EffectsNetworkTests`,
`LayoutStressScenarioTests`, `ClickInspectionTests`, `CardPackNetworkTests`, `LordsFinalCardsNetworkTests`,
`CorruptedEffectsMenuTests`, `WindowFamilyNetworkTests`, with the shared `EffectsPanelChecks`.

<a id="a-characters-window-shows-its-portrait--ui-447"></a>
## A character's window shows its portrait — UI-447 (2026-11-02)

The owner, with a screenshot of Théoden's corrupted ruler window: "the character's window should also show its portrait
or icon".

- **A medallion at the head of the title bar** (`CharacterWindowPortrait`), before the name, in both window forms
  (`CharacterInspector`, `SovereignInspector`, UI-436): a round 30-point gilt medallion holding the painted portrait of
  the version the window shows (`PortraitArtwork`, the battle portraits; a version tab or another character changes
  it), a ruler's of his state as the Sovereigns strip shows him (`SovereignPortrait.ArtOf`: awakened, corrupted), or the
  figure's own art where no portrait exists (`FigureArtwork`). It takes no pointer, is written only when the version
  changes, and the header and the window keep their size.

Playtest (owner's checklist):
- [ ] Open Characters, click Gimli: his portrait heads the window's title bar; click Gandalf the Grey and his White tab:
  the medallion follows the version. In a Kings game click Théoden's portrait: his window shows his portrait (corrupted
  once he is).

Tests: EditMode `CharacterRosterPanelTests.TheCharacterWindowShowsThePortraitOfTheVersionItShows`; PlayMode
`CharacterWindowLayoutTests`, `CharacterRosterLayoutTests`, `CharacterRosterPanelNetworkTests`, `SovereignPanelNetworkTests`,
`SovereignSectionLayoutTests`, `RulerTokenMenuTests`.

<a id="the-fellowships-declaration-from-its-own-row--ui-448"></a>
## The Fellowship's declaration from its own row — UI-448 (2026-11-03)

The owner's playtest of the declaration ("Declare position · Remain hidden", "Hollin · Choose a route", Undo step and
Cancel, "Trace a route · Using all of the Progress moves at once.", Declare): besides Finish phase a Declare that selects
the Fellowship and brings the camera to it; no Undo step in the panel (the console's is used); Cancel to the console as a
general option at the bottom; Declare in one row with the inactive Finish phase; no "Remain hidden … Progress" lines; the
Fellowship must not disappear from this view. This supersedes UI-236's "no panel button declares".

- **Declare beside Finish phase** (`FellowshipControls`, `fellowship-declare`, in Finish phase's row): while the hidden
  Fellowship may declare, its click starts the declaration (as the figure on the map does) and brings the camera to the
  Fellowship (the panel's locate). While the route is drawn the row stays — Finish phase greyed, Declare lit once the
  declaration can be sent (the tray's ✓ state, `DecisionTray.ConfirmChanged`) and its click sends it.
- **The panel keeps the route's summary only** (`FellowshipChoices.Route`): no "Declare position · Remain hidden" /
  "Reveal position · Become revealed" line, no Undo step, no Cancel; while declaring, the tray's own Declare and its hint
  are hidden in the panel (the console shows the hint). The Fellowship's members stay in view while a position is declared
  or revealed.
- **The console takes Back and Cancel**: its ↶ undoes a step of any drawn Fellowship route (declared or revealed position,
  separation), wherever the route is drawn; Cancel is the console's last option while such a route is drawn
  (`RouteCancel`, `console-cancel-route`): it ends the declaration, or clears a mandatory route or the chosen companions.
  UI-455 (owner, 2026-11-03) replaced that option by the console's small fixed Cancel
  ([below](#the-consoles-fixed-cancel--ui-455)).

Playtest (owner's checklist):
- [ ] A Fellowship phase with the Fellowship hidden. Expect: Declare beside Finish phase; its click brings the camera to
  the Fellowship and starts the route; the members stay in the panel; ↶ undoes a step, the console's Cancel ends the
  declaration; Declare (lit) sends it.

Tests: EditMode `WindowListsTests.ADrawnRouteLeavesUndoAndCancelToTheConsole`; PlayMode `FellowshipRoutesNetworkTests`
(Declare in Finish phase's row, the camera at the Fellowship, Finish phase greyed), `FellowshipNetworkTests` (↶ and the
console's Cancel), `FellowshipLayoutTests` (↶ during a reveal), `CharacterNetworkTests` (a forced separation's Cancel).

<a id="a-characters-row-brings-the-camera-to-it--ui-449"></a>
## A character's row brings the camera to it — UI-449 (2026-11-03)

The owner: a click on a character in the Characters tab that is on the board moves the camera to it. A row's click now
also focuses the map on the character's region (`PlacePresentation.CameraRegion`: its region on the board; the
Fellowship's for a companion inside it, as this player sees it); a character off the board moves nothing, and an
inspection opened any other way (a hover, a window's Show) never moves the camera.

Tests: EditMode `CharacterRosterPanelTests.ARowsCameraRegionIsWhereItsCharacterStands`; PlayMode
`CharacterRosterPanelNetworkTests` (Gimli's and Dáin's rows bring their regions into view).

<a id="a-recruiting-ability-opens-the-reserve--ui-450"></a>
## A recruiting ability opens the Reserve at once — UI-450 (2026-11-03)

The owner, about Brand's Call to Arms (the window turned into "On the board", "Abilities", a flag and a back button; the
flag opened the Reserve; Confirm sent three figures at once): the Reserve opens right after the skill's click, the
character's window keeps its form with no new buttons, recruitment is instant once a region is chosen, no Undo step in
the window (the bottom panel's), and the same for every analogous case (Dáin's Iron Guard).

- **The Reserve opens at once** (`CharacterRecruitDraft.Ruler`): a ruler's recruiting ability (Call to Arms, Iron Guard,
  Mustering of the Mark — from its icon or its Ruler token) no longer moves the decision into the character's window
  (`CharacterRosterActions.OwnsDecision`), so the window keeps its form, and the Reserve opens by itself; the flag button
  is gone (the Balrog's entry keeps it).
- **Each figure with its click; the last one sends** (`ComponentActions.Immediate`): every figure placed stands on the map
  at once as planned, and the click that places the last figure the ability needs sends the recruitment — no ✓. The
  console's ↶ takes back a placed figure until then.
- **Not per-figure commits.** The server takes the ability as one command with all its figures (the rules need the
  ability's full count, KINGS; `SovereignRecruitPlan`), so a figure placed is a planned figure until the last one; a
  server-side step per figure (with no undo once placed) would be a change of its own.

Playtest (owner's checklist):
- [ ] `kings-brand-call-to-arms` (the Free Peoples). Click Brand's Call to Arms: the Reserve opens, his window stays as
  it was. Place the North figures one by one on the lit settlements: each stands at once; the last one sends without a
  ✓. ↶ before the last takes one back. The same with `kings-dain-iron-guard`.

Tests: PlayMode `RulerTokenMenuTests`, `CharacterRosterActionNetworkTests`, `ExpansionAbilityNetworkTests`,
`ExpansionTokenNetworkTests`, `UsableSkillLightNetworkTests`.

<a id="the-councils-homelands-with-one-click--ui-451"></a>
## The Council's homelands with one click — UI-451 (2026-11-03)

The owner: when assigning homelands at the start for the members we separate, a click on the homeland moves that
companion there, and the possible regions are marked more clearly.

- **Every homeland lights at once** (`CouncilDraft.OpenHomelands`, as a placement target, UI-452's strong fill), and the
  view comes to them once (UI-452's reveal); a companion's homeland where he is placed takes the gold edge.
- **A click on a lit homeland starts its companion there** (`CouncilDraft.Region`): the selected companion when it is
  his; else the one companion whose homeland it is and who is not there yet (Fate's Gimli moves between his two). The
  Shire, shared by Merry and Pippin while both may start there, waits with its gold edge for the portrait of the one to
  start there (the console names them). A portrait's click still selects, and its second click keeps a placed companion
  with the Fellowship (UI-347); a drag still places.
- **Still one ✓**: the draft collects the companions and the ✓ sends them together — how many leave decides the Shadow's
  Council tokens.

Playtest (owner's checklist):
- [ ] `lords-council-homelands`. Expect: every homeland lit strongly; click Erebor: Gimli starts there (gold edge); click
  Woodland Realm: Legolas. In `lords-council-setup` with both hobbits as alternates, click the Shire, then Merry: he
  starts there.

Tests: PlayMode `CouncilHomelandPanelNetworkTests`, `CouncilHomelandFateTests`, `CouncilHomelandLayoutTests`,
`CouncilHomelandRecoveryTests`, `ExpansionDecisionNetworkTests`.

<a id="every-placement-region-is-lit-clearly--ui-452"></a>
## Every placement region is lit clearly — UI-452 (2026-11-03)

The owner: with Monsters Roused mark clearly on the map every region where the recruitment may be made, and check the
analogous effects.

- **A placement target** (`MapDraftOverlay.PlacementTargets`, `MapRegionEmphasis.PlacementTarget`: green at .56 instead of
  a move's .34, the targets' glowing edge): a Muster die's recruitment, a card's recruitment plan (Monsters Roused,
  Misty Mountains, Orcs Multiplying, the Musterings, Kings and Fate recruitments), Monsters Roused's halves, a ruler's
  recruitment and the Council's homelands. A move's targets keep their lighter fill.
- **The view comes to them once per decision** (`TurnInteraction.RevealPlacements`): a card's, a ruler's or the Council's
  placement brings every region it may take into the free map (none under a panel or off the view); a die's own Muster
  in the player's action moves nothing, nor does its next step (the second recruitment point, `DecisionKind.ActionSequence`;
  fixed with UI-454, `T22BoardNetworkTests.TwoNationsRecruitAtDifferentSettlementsWithOneRemainingRegular`).
- **Left as they are** (to decide): a figure whose destination is fixed by its card (`ReserveFigures`: Glorfindel,
  Thranduil's Archers, Brand's Men…, Gothmog's Fling, the Black Captain) lights its Reserve token, not a region — its
  region's click would do nothing; a ruler-token Muster lights its regions once its figure is chosen.

Tests: EditMode `MapTests.APlacementTargetIsLitMoreStronglyThanAMovesTarget`; PlayMode `GlowingHintAuditTests` (Monsters
Roused's halves, the deck choices), `MusterNetworkTests`, `FateCardsNetworkTests`.

<a id="placement-targets-pulse--ui-453"></a>
## Placement targets pulse — UI-453 (2026-11-03)

The owner: "when choosing regions for Monsters Roused the regions are green but do not stand out enough; they should
pulse". Over UI-452's stronger fill, every placement target (a recruitment, Monsters Roused's halves and plan, a ruler's
recruitment, the Council's homelands) breathes: one retained layer above the region fills (`MapPlacementPulse`) paints
them a bright green whose opacity fades fully in and out in the targets' 750 ms rhythm, beside their pulsing edge
(UI-224). It is a class toggle with an opacity transition under .motion-enabled — Reduced motion keeps the regions lit
and still — and the layer repaints only when the set changes, never while the camera moves. A move's targets keep their
still fill. *Since UI-468 every target pulses this way.*

Playtest (owner's checklist):
- [ ] `fate-card-monsters-roused`. Expect: both halves' regions green and breathing, then the plan's regions likewise;
  with Skip animations on they stay brightly lit and still.

Tests: EditMode `MapTests.APlacementTargetIsLitMoreStronglyThanAMovesTarget` (a placement target pulses, a move's does
not, no repaint for the same set); PlayMode `GlowingHintAuditTests` (Monsters Roused), `MusterNetworkTests`,
`FateCardsNetworkTests`, `MapContourRenderingTests`, `CouncilHomelandPanelNetworkTests`.

<a id="a-recruit-flies-from-the-reserve-or-is-carried-there--ui-454"></a>
## A recruit flies from the Reserve, or is carried there — UI-454 (2026-11-03)

The owner (translated): "When recruiting units we want animation. If the user just picks with a click, the unit should
glide smoothly from its Reserve slot to its proper place on the board (like drawn cards), and there should also be the
option to drag the unit on exactly the same principle as moving it on the map." This supersedes UI-421's decision 2 ("the
reserve keeps its gestures", UI-393) for the Reserve's figures.

- **A click flies it.** A recruit placed by clicks (its Reserve figure and a lit settlement, a settlement and then the
  figure, or a figure with one legal settlement) flies as the miniature it will be: it rises out of its slot's picture
  (the Reserve tab while the panel is closed), grows to the figures' scale, arcs and lands with UI-404's profile on the
  place it will stand on, above the HUD, following the camera. A Muster recruit is sent at once and lands on its force's
  place, which shows faintly until it lands, as for any flight; no second arrival in place. A planned recruit (a card's or
  an ability's plan) lands on its faint planned figure, which shows it once it has landed. Taking it back, a refused
  step, a disconnect or a view without it ends the flight.
- **A drag carries it.** A press on a Reserve figure that crosses the drag threshold takes it up and arms it without
  placing it, so exactly its settlements light. Over the Reserve and the HUD it is its picture; over the map it is its
  miniature, held up like a carried army, the settlement under it lit as a carried figure's target, the landing mark
  with "Recruit Rohan Elite" on its place, and the map pans at its edge. A drop on a lit settlement is that settlement's
  click (the same step, the same payload) and the recruit flies on from where it was dropped; any other release, or Esc,
  puts the draft back and the figure glides back to its slot. A figure only a click chooses (a Kings card's branch, The
  Black Captain Commands, Gothmog's ability, a card's figure that already has its region) keeps UI-393's drag.
- **Skip animations** (Reduced motion): nothing flies; the recruit stands on its place at once, as before.

Details and tests: [TABLETOP_FIGURES_UX](TABLETOP_FIGURES_UX.md), "As built (UI-454)".

Playtest (owner's checklist):
- [ ] `fp-muster-recruit` (the Free Peoples). Choose the Muster die, open the Reserve, click a Regular, then a lit
  settlement. Expect: the figure rises out of its slot, flies in an arc above the panel and lands on the settlement's
  army, which shows faintly until it lands; nothing appears a second time. Then carry the next figure: over the Reserve
  it is its picture, over the map its miniature with the green ring and "Recruit …" under it; dropped on a lit
  settlement it recruits there and settles from where it was dropped; dropped elsewhere or with Esc it glides back.
- [ ] `kings-brand-call-to-arms`. Use Brand's Call to Arms; click a lit settlement, then a figure. Expect: it flies to the
  faint planned figure, which appears as it lands; carry the next one to another lit settlement the same way; its ×
  takes it back.
- [ ] `fate-card-monsters-roused`. Choose a half, then recruit by clicks and by carrying. Expect: each recruit flies or is
  carried to its region and its faint planned figure appears as it lands.
- [ ] Any of these with Skip animations on. Expect: no flight; the figure stands on its place (or its faint planned
  figure) at once.

<a id="the-consoles-fixed-cancel--ui-455"></a>
## The console's fixed Cancel — UI-455 (2026-11-03)

The owner's playtest of `fp-character-separate-companions` (translated): choosing the character to separate showed a long,
narrow Cancel in the console; instead the console needs a small, default, always present Cancel among its other buttons,
greyed when there is nothing to cancel. This replaces UI-448's Cancel option in the console's line.

- **A fifth console action** (`ConsoleActions.Cancel`, `console-cancel`): a small labelled button on its own line under
  ↶ ✓ ⇥ ⊠, always shown, greyed (with `Cancel · Nothing chosen to cancel`) while there is nothing to cancel. The bottom
  bar keeps its height and every control its place in every state.
- **One meaning everywhere** (`DraftCancel`): Cancel drops everything chosen since the last sent step — die or token,
  action, figures, regions, route — where ↶ undoes one step. A drawn route keeps UI-298's rule (a declaration ends; a
  reveal or a forced separation starts again), and every forced step starts again (`MandatoryDraft`), so Cancel is enabled
  only while its click would change the draft. It sends nothing.
- The console's line keeps its hint while a route is drawn. Details: [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md#ui-455).

Screenshots: `ui455-before-separation.png` (the long Cancel), `ui455-after-separation.png`, `ui455-after-cancelled.png`.

Tests: EditMode `DraftCancelTests`, `WindowListsTests`; PlayMode `ConsoleCancelNetworkTests`, `TurnConsoleLayoutTests`,
`FellowshipNetworkTests`, `CharacterNetworkTests`.

Playtest (owner's checklist):
- [ ] `fp-character-separate-companions`. Before choosing anything the small Cancel under the four console buttons is
  greyed. Click the Character die: Cancel lights. Click Legolas's glowing portrait in the Fellowship panel. Expect: no long
  Cancel in the console's line, its hint reads "Legolas · Click a highlighted region to move there.", and the small Cancel
  says "Clear the chosen companions and their route". Click it: the companion and the die are dropped, Cancel greys again.
- [ ] Any Fellowship phase with the Fellowship hidden. Declare, draw a step, then Cancel. Expect: the declaration ends, as
  UI-448's option did; ↶ still undoes one step.
- [ ] Any army move with an Army die: choose an army and a region, then Cancel. Expect: the die and the planned move are
  dropped at once.

<a id="far-sighted-moves-only-companions-outside--ui-456"></a>
## Far-sighted moves only companions outside the Fellowship; every instruction in the console — UI-456 (2026-11-03)

The owner's playtest of `kings-far-sighted` (translated): "Move companions" behaves like "Separate companions" when no
companion is outside the Fellowship — then it must be greyed. Remove "Separated companions cannot rejoin", "Choose a
group" and "Select figures…" wherever they show in panels and windows: every instruction belongs in the console, correct
for each step.

- **Root cause.** The server offers Move companions because Thranduil himself, the Awakened king who uses Far-sighted,
  counts as a Companion outside the Fellowship (KINGS pp. 8–13). Its click started a move whose only lit objects were the
  Fellowship's portraits, whose click separates — so it looked like Separate companions.
- **Greyed while no companion but Thranduil is outside** (`FarSightedMoves`): Far-sighted's Move companions variant is
  greyed in its slot (the Woodland Realm's window, Thranduil's row and window), its tooltip says "No companion is outside
  the Fellowship" (`ComponentAction.Reason`) and its click starts nothing. Move the Fellowship and Separate companions stay.
  Thranduil's own move remains his figure's plain move on the map. Open question for the owner: Far-sighted moving
  Thranduil himself one region further is no longer offered from the picture. Decided by
  [UI-465](#thranduils-figure-moves-with-far-sighted--ui-465) (owner, 2026-11-03): "both" — the variant stays greyed, its
  reason now reads "No companion is outside the Fellowship · Thranduil moves from his figure", and with the Ruler die his
  figure on the map moves him with Far-sighted.
- **No instruction in the Fellowship panel** (`FellowshipChoices`, `DecisionTray`): no "Separated companions cannot rejoin"
  line (the fact stays in the ✓'s summary); the route's summary states where the move stands — its origin, then its end and
  steps — without "Choose a group", "Choose a destination" or "Choose a route"; the tray's status line is hidden while a
  Fellowship move is drawn in the panel.
- **The console's hint for each step** (`FellowshipDraft.Hint`, now the console's hint for every move to a destination):
  a separation before its companions are chosen — "Click the glowing portrait of each companion to separate in the
  Fellowship panel."; a companions' move — "Click a glowing companion on the map to choose who moves."; another figures'
  move — "Click a glowing figure on the map to choose who moves."; once the figures are chosen — "‹names› · Click a
  highlighted region to move there." (UI-375).

Screenshots: `ui456-before-die.png`, `ui456-before-move-companions.png` (the lit portraits after Move companions),
`ui456-after-far-sighted.png`.

Tests: EditMode `ComponentActionTests.FarSightedMoveCompanionsWaitsForACompanionOutsideTheFellowship`,
`WindowListsTests.TheFellowshipTrayListsNoStayStayAllOrTravelMode`; PlayMode
`ConsoleCancelNetworkTests.FarSightedMovesNoCompanionWhileNoneIsOutsideTheFellowship` (`kings-far-sighted`),
`ConsoleCancelNetworkTests.ASeparationIsCancelledFromTheConsolesFixedCancel`, `FellowshipGuideNetworkTests`,
`FellowshipRoutesNetworkTests`.

Playtest (owner's checklist):
- [ ] `kings-far-sighted`. Click the Ruler die, open the Woodland Realm. Expect: Thranduil's Move companions picture is
  greyed and says "No companion is outside the Fellowship" (since UI-465 followed by "· Thranduil moves from his figure");
  Move the Fellowship and Separate companions are lit; the greyed picture's click does nothing.
- [ ] `fp-character-move-companions` with a Character die (Legolas and Gimli outside). Expect: Move companions works as
  before; the console says "Click a glowing companion on the map to choose who moves.", then names the chosen ones.
- [ ] `fp-character-separate-companions`. Choose the Character die, then a companion's portrait. Expect: the Fellowship
  panel shows the portraits and "Rivendell" only — no "Separated companions cannot rejoin", no "Choose …", no "Select
  figures…"; every instruction is in the console.

<a id="a-version-not-in-play-never-lights--ui-457"></a>
## A version not in play never lights its Characters row — UI-457 (2026-11-03)

The owner (translated): when a card is drawn whose mechanic concerns Gandalf as the Guide, the Characters tab and row of a
Gandalf version not yet on the board must not light up; check similar cases.

- **Root cause.** UI-441 lights a character wherever it is drawn while one of its skills is usable now — here Gandalf the
  Grey's Guide draw after an Event card. A companion in the Fellowship with a next version has its Characters row show that
  version (Gandalf the White, UI-438), so the row and the Characters tab lit for a version not on the board.
- **The rule** (`CharacterRosterContext.ShowsVersionInPlay`): a Characters row lights for a usable skill, and for a
  figure's action, only while it shows the version in play; the Characters tab follows its rows. An entry still lights the
  row of the version it brings in (UI-240, UI-438). The skill keeps lighting the version in play where it is drawn:
  Gandalf the Grey's Fellowship portrait, a figure on the map, a king's portrait.
- **Similar cases** covered by the same rule: Strider in the Fellowship (his row shows Aragorn), any companion with an
  alternate version, a reaction whose source is a version not in play.

Tests: EditMode `CharacterRosterPanelTests.ARowLightsForASkillOnlyWhileItShowsTheVersionInPlay`, `UsableSkillsTests`.

Playtest (owner's checklist):
- [ ] Test menu → Free Peoples cards → Book of Mazarbul (event), Gandalf the Grey the Guide. Play it with the Event die.
  Expect: Gandalf's Fellowship portrait glows for his draw; the Characters tab and Gandalf the White's row do not.
- [ ] Gandalf the White in play with a skill usable now. Expect: his row and the tab light as before.

<a id="the-hit-buttons-stand-on-their-users-side--ui-458"></a>
## The hit buttons stand on their user's side — UI-458 (2026-11-03)

The owner's playtest of `lords-card-orderly-withdrawal` (translated): the clickable "−1" must show on the side of the
faction that uses it, in the battle window. UI-246 and UI-347 had put it on the opponent's hits.

- **The rule** (`BattleDiceStrip.HitCancels`): the hit buttons a card adds — Orderly Withdrawal's −1 and Heroic Death's
  −1, −2… (one mechanism, so both follow the owner's rule) — stand on the roll heading of the deciding player's own
  column; the opposite column keeps their unseen room, so both rows stay level (UI-437). A side that cancels hits without
  a roll of its own shows them in its reserved roll row.

Screenshots: `ui458-before-orderly-withdrawal-1920-1.00.png`, `ui458-orderly-withdrawal-{1366,1920}-{1.00,1.25}.png`.

Tests: EditMode `BattleObjectChoicesTests` (Heroic Death, Orderly Withdrawal); PlayMode `LordsBattleNetworkTests`,
`BattleLayoutTests.WhatAnAbilityAddsToOneSideKeepsBothSidesRowsLevel`.

Playtest (owner's checklist):
- [ ] `lords-card-orderly-withdrawal` (the Free Peoples). Expect: the glowing, pulsing −1 stands in the Free Peoples'
  (defender's) column, on their Combat roll's heading; its click cancels a hit and withdraws; Pass keeps the hit.
- [ ] A battle where the Free Peoples play Heroic Death. Expect: the −1/−2 buttons stand in the Free Peoples' column.

<a id="a-characters-row-closes-its-window--ui-460"></a>
## A character's row closes its own window — UI-460 (2026-11-03)

The owner: in the Characters tab, while one character's window is open, a second click on that character's row closes the
window. A row's click now toggles the pinned window of its character (`CharacterRosterPanel.ShowsWindowOf`); a click on
another row still switches to that character, an entry row still enters, and the Characters panel stays open.

Screenshots: `ui460-window-open.png`, `ui460-window-closed.png`.

Tests: PlayMode `CharacterRosterPanelNetworkTests.ClickAndKeyboardInspectionPreserveTheDieWhileHoverAndPanStaySilent`.

Playtest (owner's checklist):
- [ ] Any game: open Characters, click Gimli's row (his window opens), click the row again. Expect: the window closes and
  the Characters panel stays; a third click opens it again.

<a id="an-events-losses-window-fits-its-content--ui-464"></a>
## An event's losses window fits its content — UI-464 (2026-11-03)

The owner's playtest of `kings-event-return-to-valinor` (translated): the window in which the Free Peoples eliminate units
is too big, with a lot of empty space; it must fit its content, and so must the same window for other events' losses.

- **Root cause.** Event losses (UI-251) reuse the battle window, whose loss planning is 820 pt wide for two columns.
- **The rule** (`Battle.uss`, `.event-loss-panel`): an event's losses window is as wide as its one column needs — 460 pt
  (at most 95 % of the screen) for the region's heading and hits, a row of figures and Apply losses, more figures wrapping
  onto the next row — for every event's losses (Return to Valinor, Madness and Horror, Shadow Spells…); battles keep
  their width. A definite width, not an auto one: an auto width measured the wrapping footer on one line and left Apply
  losses below the frame.

Screenshots: `ui464-before-valinor-{1366,1920}-1.00.png`, `ui464-after-valinor-{1366,1920}-1.00.png`,
`kings03-valinor-loss-*.png`.

Tests: PlayMode `KingsCards03NetworkTests` (Return to Valinor's losses at 1366 and 1920: narrower than 700 pt, its figures,
hit budget and Apply losses inside the frame, no scroll).

Playtest (owner's checklist):
- [ ] `kings-event-return-to-valinor` (the Shadow plays it; then the Free Peoples). Expect: the Lórien losses window is
  about as wide as its heading and figures, with no wide empty area; Apply losses works as before.
- [ ] `lords-card-madness-and-horror-event` (or any other event with losses). Expect: the same fitted window.

<a id="ui-461"></a>
## Will of the West is itself until the console turns it — UI-461 (2026-11-03)

The owner's playtest of `lords-fp-balrog-deactivate` (translated): "Will of the West can turn into another die but can also
be used by itself, e.g. for Gandalf's skill. Now we don't choose which die WotW turns into: all possible options are lit
and the die is chosen from what we do. That's a problem because there are different possibilities, e.g. an army move with
a leader can be done with more than one die. We need the previous small menu at the bottom (console) that let us choose the
new face WotW turns into, while the additional effects coming directly from WotW (like deactivating the Balrog) light on
the table; if the player picks one of them the die is spent and the player needn't choose anything." It replaces UI-28's
"all uses at once" for Will of the West only; Army/Muster keeps it.

- **As itself** (`WillOfTheWestUses`, presentation only — the server lists the dice of every use and validates every
  payment): a chosen Will die lights Gandalf the White's and Aragorn's entry, the Balrog's deactivation (LORDS pp. 23–25:
  a Will of the West result, or a Character result while Gandalf the Grey is in his region; the server pays Will of the
  West as itself), the removals of Denethor's Folly, The King's Welcome, the Gates, the Palantír cards and the Lore without
  a Ring, and what any die pays (a political ability, a Council messenger, Narya, a region's protection). One click on such
  an object spends the die; the console's hint says so.
- **The face menu** (`ConsoleFaceMenu`, `WillFaceChoices`): four fixed medallions — Character, Army, Muster, Event — at
  the start of the console's line, beside the hint (where the line is too narrow for both, as with Lords at 1366 × 768 and
  125 %, in the strip just above the console, as TEST-15's options); a result with no use now is disabled, not hidden. A click turns the die
  into that result: the board lights exactly what such a die does (UI-389) and the as-itself uses go dark. A second click
  on the chosen result, or ↶, makes it Will of the West itself again; a second click on the die releases it. Details:
  [TURN_CONSOLE_UX](TURN_CONSOLE_UX.md#ui-461).
- **The cost the server gets**: the chosen result is a local part of the draft (`TurnDraft.WillFace`, saved with it, never
  sent); the command carries it as its cost — an army move's `MovementCost` (Army: up to two moves; Character: one move of
  a led army), a card's `CardCost`, a draw, a recruitment or a political step. A started action names its result, so a
  restored draft needs no menu (`ActionDieContext.BindCost`). No contract or server change.

Screenshots: `ui461-as-itself-{1366,1920}-{1.00,1.25}.png` (the menu and the lit Balrog), `ui461-will-character.png`,
`ui461-will-army.png`, `ui136-will-options.png`.

Tests: EditMode `WillFaceTests`, `DieOptionsTests`, `ContextInteractionTests`, `CardActionTests`, `FellowshipDraftTests`,
`TravelActionTests`, `PoliticalActionTests`; PlayMode `WillOfTheWestFaceNetworkTests` (`lords-fp-balrog-deactivate`:
deactivation with one click, a Character and an Army march, Back, the menu's layout in four sizes),
`MultiUseDiceNetworkTests`, `BoardInteractionTests`, `T22BoardNetworkTests`, `FellowshipPanelNetworkTests`,
`ExpansionAbilityNetworkTests`.

Playtest (owner's checklist):
- [ ] `lords-fp-balrog-deactivate` (Free Peoples). Click the Will of the West die. Expect: four medallions at the start of
  the console's line, none pressed; no army glows; the Balrog's figure in Dimrill Dale glows with "Click to deactivate the
  Balrog". Click it: the die is spent and the Balrog is eliminated, with no menu choice.
- [ ] Same scenario, a fresh start: click the Will die, then Character in the menu. Expect: Character stays pressed;
  Gandalf, the Fellowship and led armies light; the Balrog's figure goes dark. Click Character again (or ↶): the die is
  Will of the West itself again.
- [ ] Any base-game action phase with a Will of the West die and an army with a leader (e.g. Minas Tirith): choose the
  die, then Army, click the army and a region — the move leaves one more army move (Finish ends it); start again with
  Character instead — one move of the led army spends the die.

<a id="ui-459"></a>
## The Balrog's force marks who goes — UI-459 (2026-11-03)

The owner's playtest of `lords-sh-use-balrog-active` (translated): "Moving the Balrog alone or with the army should not be
chosen by clicking the skill icon in different versions; when we click the units a window with them opens where we mark
who goes and who doesn't, then the move is done as always, by click or drag."

- **The die is its ability** (`ActionDieContext.Implied`): when every movement cost the server offers a die names one
  ability — the Balrog die's Use Balrog — choosing the die binds it, so his force lights on the map with no icon first.
  Balrog Activation's move, attack and move-alone variants are gone from the Characters window and the region panel
  (`AbilityVariants`, `ComponentActions`); its one slot keeps the activation (level 0) and the Free Peoples'
  deactivation. Great Sorcery and the Mouth of Sauron keep their icons.
- **One selection** (`BalrogSelection`): a click on his army marks every figure that can move (UI-127) and opens his
  region's panel, where a click leaves a figure behind or takes it along. With only the Balrog marked he moves alone (the
  server's travel of the Balrog, `LordsArmyCosts.ValidTravel`) and his route's regions light; with soldiers marked too he
  moves or attacks with them (an army move whose Character cost includes him, `ValidArmy`); soldiers without him have no
  target; the Nazgûl never travels with him (no army unit, no target). The move is then made as always: a click on a
  lit region, or the Balrog's miniature carried there (UI-405). Server unchanged.
- The console's hint: "Click the Balrog’s army to select all of it · mark who goes in its panel; the Balrog alone moves by
  himself".

Screenshots: `ui459-army-marked.png`, `ui459-alone-marked.png`.

Tests: EditMode `BalrogSelectionTests`, `ComponentActionTests`; PlayMode `BalrogMoveNetworkTests`
(`lords-sh-use-balrog-active`: alone by the panel and a click, alone carried, the whole army's attack and the army without
him), `ExpansionCostNetworkTests`, `CharacterRosterActionNetworkTests`.

Playtest (owner's checklist):
- [ ] `lords-sh-use-balrog-active` (Shadow). Click the Balrog die. Expect: no Balrog icon lights in the Characters tab or
  the region panel; the Moria army glows. Click it: the panel lists the Balrog and his Orcs, all marked. Click each Orc to
  leave it behind: the Balrog alone stays marked and his reachable regions light; click one — he moves alone, the Orcs stay.
- [ ] Same scenario, a fresh start: click the die, then carry the Balrog's miniature to a lit region. Expect: he moves
  alone with that drop.
- [ ] Same scenario, a fresh start: click the die and the Moria army, then Dimrill Dale. Expect: the whole force attacks
  the Elves. Unmarking the Balrog first darkens Dimrill Dale: his soldiers move only with him.

<a id="ui-462"></a>
## Sméagol flies into the Fellowship — UI-462 (2026-11-03)

The owner (translated): "we need a graphical animation presenting Sméagol joining the Fellowship."

- **The join.** Sméagol's Hunt tile brings him into the Fellowship as its Guide (LORDS pp. 21-22); the server records the
  drawn tile, then the Guide change, in the shared Hunt report. In the Hunt's presentation a Guide change whose Guide is
  no Companion of the staged roster yet is a join (`FellowshipJoin`): the Fellowship panel opens, the drawn tile glows, a
  portrait plate with his artwork rises out of the tile and flies, eased and shrinking, into his new place at the end of
  the roster (`DrawFlight` with a dress hook), which stands empty until it lands; then his portrait shows, takes the
  Guide's badge from the previous Guide and glows (`StagedBoard.Join`, `StagedBoard.Guide`). A Guide already in the
  roster keeps UI-310's badge step; Gollum, who has no portrait, never joins.
- **Both players.** A Hunt is a shared resolution (`PresentationSelection`), so the Free Peoples whose move drew the tile
  see the flight as the Shadow does; their roster holds him as their own Companion, the Shadow's by his identity.
- **Skip animations** (Reduced motion): no flight; once the panel has settled he stands in the roster with the badge and
  the mark at once. Skip or a rebase ends the flight: his portrait shows and the release draws the latest roster.
- Only translate, scale and opacity move, above the HUD, never taking the pointer. No server or contract change.

Screenshots: `ui462-join-flight.png`, `ui462-join-landed.png` (the presented board), `ui462-network-flight.png`,
`ui462-network-landed.png` (`lords-smeagol-joins`, the Shadow's board).

Tests: PlayMode `OpponentHuntPresentationTests` (`SmeagolsPortraitFliesFromHisTileIntoTheRosterAndTakesTheBadge`,
`TheFreePeoplesWhoseMoveDrewHisTileSeeHimJoinToo`, `UnderSkipAnimationsSmeagolStandsInTheRosterWithTheBadgeAtOnce`;
`ANewGuideTakesTheBadgeOnItsPortrait` unchanged), `SmeagolJoinNetworkTests` (`lords-smeagol-joins`: both boards fly him
in and end with him guiding), `OpponentResourcePresentationTests` and `OpponentExpansionPresentationTests` (the other
`DrawFlight` beats).

Playtest (owner's checklist):
- [ ] `lords-smeagol-joins` (Free Peoples). Click the Character die and move the Fellowship. Expect: the Hunt window rolls
  and turns up Sméagol's tile; the Fellowship panel opens, the tile glows and his portrait flies out of it into a new,
  empty place at the end of the roster; there it shows with the Guide badge and glows, and Strider loses the badge.
- [ ] Same scenario watched from the Shadow (switch side in the test menu, or a second client). Expect: the same flight
  on the Shadow's board.
- [ ] Same scenario with Skip animations on. Expect: no flight; once the panel opens Sméagol stands in the roster with the
  Guide badge, lit.
- [ ] Same scenario, Skip (or Esc) during the flight. Expect: the flight ends at once and Sméagol stands in the roster as
  the Guide.

<a id="ui-463"></a>
## A played card goes to the Effects tab — UI-463 (2026-11-03)

The owner (translated): "let's add an animation when playing a card makes it go to Effects."

- **The flight.** The opponent's table card already shrinks into the Effects tab, which glows (UI-316); the player's own
  play is never presented (`PresentationSelection`), so the card only vanished from the hand. Now `PlayedCardFlight`, on
  UI-326's mark-and-settle pattern, marks the hand card where it stands when its play is sent (`TurnInteraction` →
  `BoardScreen.MarkPlayedCard`: the card the command names, its unrolled face or its slot) and settles with the view in
  which it has left the hand and a new source of the player's own with its id stands among the effects
  (`EffectsPresentation.Sources`: a table card, or an Event's lasting effect). Its parchment stand-in with the card's
  title (UI-324) appears at that place and shrinks, eased as a presented card (`CardFlight`, 0.6 s), into the Effects
  tab, or into its tile while the panel is open; the tab then glows for 0.8 s.
- **Only a card that goes to Effects.** A card still in the hand at a newer revision is forgotten (a refused play); a card
  that left for anything else (an Event that does not stay, a combat card, a discard) is forgotten once its resolution no
  longer leads the effect trail; a quiet view (a reconnect, the first view) plays nothing.
- **Skip animations** (Reduced motion): no flight; the tab glows only.
- Only translate, scale and opacity move (the stand-in's place and size are set once), above the HUD, never taking the
  pointer; nothing waits for it. No server or contract change.

Screenshots: `ui463-flight.png`, `ui463-landed.png` (`PlayedCardFlightTests`), `ui463-network-flight.png`,
`ui463-network-landed.png` (`lords-card-we-wont-go-back-event`, the Shadow's board).

Tests: PlayMode `PlayedCardFlightTests` (the flight toward the tab, shrinking, the glow and its end; a kept, resolved,
opponent's or quiet card plays nothing; Skip animations' glow only), `PlayedCardFlightNetworkTests`
(`lords-card-we-wont-go-back-event`: the Character die, one click on the card, the flight and the Shadow's table),
`CardDissolveTests`, `OpponentCardPresentationTests`.

Playtest (owner's checklist):
- [ ] `lords-card-we-wont-go-back-event` (Shadow). Click the Character die, open the hand and click We Won't Go Back.
  Expect: the card leaves its place in the hand as a small parchment card with its title, which shrinks and glides into
  the Effects tab on the left; the tab then glows and counts one more source.
- [ ] Same scenario with the Effects panel pinned open. Expect: the card glides into its new tile in the panel and the tab
  glows.
- [ ] Same scenario with Skip animations on. Expect: no flight; the card leaves the hand and the Effects tab glows briefly.
- [ ] Any Event card that does not stay on the table (e.g. a recruiting Event). Expect: nothing flies to the Effects tab.

<a id="thranduils-figure-moves-with-far-sighted--ui-465"></a>
## Thranduil's figure moves with Far-sighted — UI-465 (2026-11-03)

The owner's decision on UI-456's open question (translated): "both" — while Awakened Thranduil is the only companion
outside the Fellowship, Far-sighted's "Move companions" stays greyed, and Thranduil's own figure on the map lights as
Far-sighted's mover, so his own move can use Far-sighted's +1 range.

- **Why his figure's move is Far-sighted's with the Ruler die.** Far-sighted is paid by the Free Peoples' Ruler die
  showing a Character (KINGS pp. 8–13, as UI-456 cites; the server's `KingsFarSighted.Valid`,
  `KingsRulerAbilities.CanPay`). For Move companions the server offers it as the option's `RulerTravel` (`AbilityId`
  `kings.far-sighted`, `RangeBonus` 1, one travel per Free Peoples character on the board, Thranduil included) and moves
  with `MoveCompanions`, `AbilityId` `kings.far-sighted` and `CompanionMoves` (`ActionStepRules.Companions`,
  `KingsFarSighted.Bonus`). With that die it gives Thranduil his plain move plus one region for the same die (a
  destination within his plain range stays reachable), so with the Ruler die his figure's move is Far-sighted's. With any
  other Character die his plain move stays as it is.
- **The figure** (`FarSightedFigure`, `TravelActions`): with the Ruler die chosen, no companions' move drafted and no
  Ruler token chosen, while he is the only companion outside the Fellowship, Thranduil's miniature glows as a choosable
  figure and its tooltip — and his chip in the Woodland Realm's panel — leads with "Far-sighted · Range +1 · Click to move
  Thranduil one region further" (`MapCanvas.OfferFigures`, `TravelContextInteraction.Offers`). Its click (or a drag past
  the threshold, UI-405, which makes the same click) starts `MoveCompanions` with `AbilityId` `kings.far-sighted`, his
  group and the Ruler die (`DraftOptions.WithRulerTravel`), and lights his Far-sighted range; a lit region's click (or the
  drop there) sends it. Back leaves him behind and his figure still offers the Far-sighted move; Esc and the console's
  Cancel undo as for any move.
- **The variant stays greyed** (`FarSightedMoves`): Far-sighted's Move companions keeps UI-456's greyed slot; its reason
  now points at the figure: "No companion is outside the Fellowship · Thranduil moves from his figure". Move the Fellowship
  and Separate companions are unchanged.
- **Another companion outside**: behaviour as before UI-465 — the variant is enabled and moves the chosen companions (him
  among them) one region further; every figure's own click, his included, starts a plain move. A chosen Ruler token keeps
  its own travel (UI-389).
- No server or contract change.

Screenshots: `ui465-lit-figure.png` (the Ruler die chosen, his lit figure), `ui465-far-sighted-range.png` (his Far-sighted
range after his click).

Tests: EditMode `FarSightedFigureTests` (the Ruler die starts Far-sighted from his figure with its range, Back and a
second click; another Character die moves him plainly; the variant stays greyed with the new reason; another companion
outside keeps the plain move and the enabled variant; a Ruler token keeps its travel),
`ComponentActionTests.FarSightedMoveCompanionsWaitsForACompanionOutsideTheFellowship`; PlayMode
`FarSightedFigureNetworkTests` (`kings-far-sighted`: the click path and the drag path send `MoveCompanions` with
`AbilityId` `kings.far-sighted` and Thranduil's move to a region one beyond his plain range; the variant is greyed with
its reason), `ConsoleCancelNetworkTests.FarSightedMovesNoCompanionWhileNoneIsOutsideTheFellowship`;
`ExpansionTokenNetworkTests.FarSightedIsAnExplicitModeForMovement` and `ThranduilsTokenPaysForFarSightedInTheSameAction`
(seeded `ui09-travel`, Thranduil alone outside) now move him from his figure — with the Ruler die, or with his token —
instead of clicking the variant UI-456 greyed.

Playtest (owner's checklist):
- [ ] `kings-far-sighted`. Click the Ruler die. Expect: Thranduil's figure in the Woodland Realm glows; its tooltip begins
  "Far-sighted · Range +1 · Click to move Thranduil one region further". Open the Woodland Realm: Far-sighted's Move
  companions picture is greyed and says "No companion is outside the Fellowship · Thranduil moves from his figure".
- [ ] Same scenario. Click Thranduil's figure. Expect: he lifts, the console says "Thranduil — King of the Woodland Realm ·
  Click a highlighted region to move there.", and the lit regions reach one region further than his plain move. Click
  Back: he is put back and still glows with the Far-sighted line. Click him again, then a lit region one beyond his plain
  range. Expect: he moves there at once.
- [ ] Same scenario, a new start. With the Ruler die chosen, drag Thranduil's figure onto a lit region one beyond his
  plain range. Expect: the drop moves him there with Far-sighted, as the click did.
- [ ] Same scenario, when the Free Peoples' pool also shows a standard Character die: choose it instead of the Ruler die.
  Expect: Thranduil's figure glows without the Far-sighted line and lights only his plain range.

<a id="the-free-peoples-band-is-blue--ui-466"></a>
## The Free Peoples' side band is blue — UI-466 (2026-11-03)

The owner: "the bar at the bottom left that shows Free Peoples should be blue for the Free Peoples, unlike the red one
for the Shadow". The own dice panel's header band (`ActionPoolView.Header`) takes `dice-header-free` for the Free
Peoples and shows `frames/panel-header-free-lacquer`: the same gilt-ruled band as `frames/panel-header` in blue lacquer
(the `free-lacquer` variant of `graphicalassets/ui`, which overrides only the lacquer tokens). The Shadow keeps the red
band; every other panel header is unchanged.

Playtest (owner's checklist):
- [ ] Any Free Peoples game. Expect: the band at the bottom left reading FREE PEOPLES is blue with its gilt rule.
- [ ] Any Shadow game. Expect: the band reading SHADOW stays red.

Tests: PlayMode `DicePanelLayoutTests` (both sides in four layouts: blue band for the Free Peoples, red for the Shadow).

<a id="a-compact-dice-panel-in-expansion-games--ui-467"></a>
## A compact dice panel in expansion games — UI-467 (2026-11-03)

The owner (with a screenshot of a Ruler token): "in some cases, e.g. when there is a token to spend, the panel on the left
widens a lot and the bar with instructions shrinks; the left part should be designed more compactly, with less free
space, which leaves room for a bigger bar with hints on what to click". In a Lords or Kings game the own dice panel was
432 px: the dice in a 210 px column and, beside them, a second column as wide holding the counters spread across it and
the tokens below. Now the counters (spent, Hunt, out, the Fellowship Box) stand under the dice as in the base game
(`ActionPoolView.OwnResources`, `own-dice-column`), and beside the dice only a 100 px column of tokens, three a row (Kings'
Ruler and Dismay tokens, the Shadow's Council tokens), and only in a game that has them (`token-column`). The panel is
230 px without tokens and 336 px with them (the fullest pool, five Dismay and two Council tokens, fits in three rows); its height is unchanged, the header band spans the panel, and the console
takes the width freed.

Playtest (owner's checklist):
- [ ] `kings-far-sighted` or any Kings game. Expect: the dice with the counters under them, the Ruler token beside the dice
  in a narrow column; the console's hint line clearly wider than before.
- [ ] A Lords game without the Council (e.g. `lords-sh-use-balrog-active`). Expect: no token column, the panel only as wide
  as the dice.

Tests: PlayMode `ExpansionMatrixLayoutTests` and `LayoutStressScenarioTests` (`ResourcesFit`: the own panel at most 336 px,
every die, counter and token inside it without overlap, in 186 matrix views and the stress scenarios, four layouts),
`DicePanelLayoutTests`, `TurnConsoleLayoutTests`, `RulerTokenMenuTests`, `CouncilTokenMenuTests`, `CompactBoardLayoutTests`,
`InGameDenseLayoutTests`.

<a id="every-target-region-pulses-clearly--ui-468"></a>
## Every target region pulses clearly — UI-468 (2026-11-03)

The owner: "the regions that can be clicked and are suggested by colour should pulse more clearly; now it is barely
noticeable, especially against similar colours all around". UI-453's breathing fill now covers every target region
(`MapTargetPulse`, was `MapPlacementPulse`): a move's or a placement's in a bright mint, an attack's in a bright red, both
lighter than any land of the map, over the region's still fill, fading fully in and out in the 750 ms rhythm. The target
edge (`MapTargetGlow`, UI-224) gets an ink line under its bright line, a wider halo, and fades to 20 % instead of 35 %
between beats, so it stands out against a neighbour of a similar colour. The chosen region (UI-392) keeps its gold edge
without the fill. Still one retained layer each, toggled by class under .motion-enabled (Reduced motion keeps them lit
and still) and repainted only when a set changes.

Playtest (owner's checklist):
- [ ] `fp-muster-recruit` and any army move. Expect: every lit settlement or region clearly breathes, also next to green
  regions such as Rohan or Mirkwood; an attack's region breathes red.
- [ ] With Skip animations on. Expect: the targets brightly lit and still.

Tests: EditMode `MapTests.APlacementTargetIsLitMoreStronglyThanAMovesTarget` (a move's, a placement's and an attack's
target pulse, a region that is no target does not, no repaint for the same set); PlayMode `GlowingHintAuditTests`,
`MusterNetworkTests`, `FateCardsNetworkTests`, `MovementNetworkTests`, `BoardInteractionTests`, `MapClickMoveNetworkTests`.

<a id="skills-beside-the-place-picture--ui-469"></a>
## Skills beside the place picture, no empty strip — UI-469 (2026-11-03)

The owner (Thranduil's window): "let's change the design of the window of a character with skills: move the row with the
skills to the right of the map or the Fellowship marker, and get rid of the empty strip at the bottom by making the whole
window smaller". In a king's window (`SovereignInspector`) and a character's (`CharacterInspector`) the ability heading
("Abilities once awakened", …) and the row of skill icons now stand in the facts column beside the place picture, under
the corruption meter or the card's statistics. The king's flags line and corruption tiles collapse when they show nothing,
and a ruler's window no longer keeps the 252-point minimum (UI-436): it is as tall as its content, about 208 points at
rest, and still opens at one place with one width (a king with an extra action line is a line taller).

Playtest (owner's checklist):
- [ ] `kings-far-sighted` (or any Kings game): open Thranduil, then Brand, from the Characters tab. Expect: the skill icons
  beside the place picture under the corruption meter, the Awaken line below, no empty strip at the bottom; every king's
  window opens at the same place.
- [ ] A companion with skills (e.g. Gandalf the Grey). Expect: the skill icons beside the place picture under the
  statistics.

Tests: PlayMode `CharacterWindowLayoutTests` (every king's window at one place and width, as tall as its content with no
empty strip, four layouts), `CharacterRosterLayoutTests`, `CharacterRosterPanelNetworkTests`,
`CharacterRosterActionNetworkTests`, `SovereignPanelNetworkTests`, `SovereignSectionLayoutTests`, `InGameDenseLayoutTests`,
`LayoutStressScenarioTests`, `ExpansionWindowNetworkTests`.

<a id="the-menus-in-thin-lines--ui-470"></a>
## The menus in thin lines — UI-470 (2026-11-03)

The owner (with a reference picture of a sign-in card): "let's change the design of the main menu, the settings, etc. —
all windows that are not part of the game's own interface: Test scenarios, Host a game and the rest. Everything less
thick and in-your-face; slick and elegant — now it is terribly coarse. Do not add right-pointing arrows on every button.
Do not add or change any functionality; we only care about the graphics." Only art and colours changed: every text,
control, name, order, place and size class is the one it was, and no button carries an arrow or a new icon.

One chrome for every menu window. `Styles/Menus.uss` holds it; `FrontendTheme.ApplyChrome` attaches the sheet and the
class `menu-chrome` to the sign-in, main menu, waiting room and replay library (through `FrontendTheme.Apply`), Settings
(`InterfaceSettingsPanel`), the questions (`ConfirmationDialog`: Quit, delete; `GameDeletedNotice`) and the board's Menu
panel (`BoardMenu`). Nothing reaches the board's HUD: the top bar's Menu toggle, the panels, the battle, card and
character windows keep their art.
- A window is a card (`frontend/card`): translucent charcoal under one thin muted-gilt rule with small concave corners, a
  tiny gilt lozenge in each cut and a hairline inner rule — no bevel, band or shadow.
- A button, tab or segment (`frontend/button`) is a dark field edged with one muted-gold line, its words in Alegreya
  Regular. Hover lightens the line and the field; a press darkens the field and sinks it a point (UI-113); an open page,
  a chosen segment or tab and a variant that is on wear `selected` (a warm field with a bright line) instead of red
  lacquer; a disabled control is greyed; keyboard focus brightens the words only, so a clicked button never looks held.
  The one main action of a screen — Login, Create game, Start game — is `primary`, a soft warm-gold gradient with a
  brighter line (`primary-hover`, `primary-pressed`).
- A text field is a dark field with one muted-gold line that brightens on hover and turns gold on focus, a muted
  placeholder, a gold caret and selection. Checkboxes are small boxes with a gold tick (`frontend/check`); a slider is a
  quiet gold track with a small dark dragger edged in gold; a scroll bar has no plate, a slim muted-gold thumb and quiet
  arrows, 10 points wide instead of 13.
- Titles are Cinzel capitals, letter-spaced, light gold, without the faux bold.

Per screen:
- **Sign-in**: the card instead of a flat plate with a border (one point more padding for the border it replaced); the
  title 21 points with wider spacing; a small line icon inside the account name and password fields
  (`frontend/icon-account`, `icon-password`); Login primary; Create account and Forgot password? thin; a hairline with a
  tiny lozenge (`frontend/divider`) over Settings and Replays, which now keep the 8-point gap between them that the old
  `:first-child` rule (unsupported in USS) never gave.
- **Main menu**: the window and the action column are cards; the red header band is gone — the page's name stands
  centred in spaced capitals over the divider, Refresh keeps its place at the header's right end. Open games, My games
  (own rooms and saved games), Replays and the test scenarios are quiet rows (`frontend/row`, `row-hover`) instead of
  gilt entry plates; a saved game waiting for your decision keeps its gold mark, 2 points instead of 4. The open page's
  action is `selected`. Join by code's rules, the lobby panel, the players-online strip and tab, a name's context tab
  and the challenge tab (now a warm charcoal plate under a bright gilt line instead of red) are in charcoal and muted
  gold.
- **Host a game / Demo vs CPU**: the side cards are rows and the chosen side is lit by `row-selected` (a thin gold line
  over a warm field; the 2-point ring is gone); Public and Private are segments; Create game is primary. The rules strip
  (demo page and waiting room): the expansions' medallions are the thin `frontend/medallion` (a dark enamel disc in one
  ring, lit in the module's enamel with a soft halo when on), the ticks are boxes, and a variant chip that is on is
  `selected`.
- **Test scenarios**: the tabs are thin buttons (the open one `selected`), a lens (`frontend/icon-search`) stands inside
  the search field, the scenarios are rows; the hint and descriptions are muted parchment instead of grey-teal.
- **Waiting room**: the seats are rows and your own seat is `row-selected`; the observers' chips and the chat log are
  edged in muted gold; Start game is primary.
- **Settings, the questions and the board's Menu**: the card instead of a flat plate (the Menu panel keeps its inner
  size: the 1-point border became a point of padding), thin buttons (the chosen size, window mode and resolution
  `selected`), boxes for the toggles, quiet sliders.
- The map behind the menus is a little dimmer (tint rgb(72, 66, 56) instead of rgb(88, 80, 66)), so the thin cards stand
  clear of it.

Art (`graphicalassets/ui/src/frontend`, `render.py --only frontend`; tokens `--fe-*` in `palette.css`; new variants
`primary`, `primary-hover`, `primary-pressed`, and `--fe-*` overrides in `hover`, `pressed`, `selected`, `fate-on`,
`lords-on`, `kings-on`): `card` (64 pt, slices 12), `button` with six variants (64×32 pt, slices 6), `row` with `hover`
and `selected` (96×40 pt, slices 8), `divider` (200×8 pt, unscaled), `medallion` with five variants (56 pt), `check` and
`check-selected` (14 pt), `icon-account`, `icon-password`, `icon-search` (16 pt). The board keeps `frames/panel`,
`panel-header`, `entry` and `button`; `rules/medallion*` and `rules/check-*` are no longer referenced but stay in the
kit.

Playtest (owner's checklist):
- [ ] Start the game. Expect: the sign-in card thin and dark over a dimmer map; small icons in the two fields; Login warm
  gold; the other buttons dark with a thin line; no arrows anywhere; Settings and Replays with a gap under a hairline
  with a tiny lozenge.
- [ ] Hover, press and release Create account; tab into a field. Expect: the line lightens on hover, the button darkens
  and sinks while held; the focused field's line turns gold.
- [ ] Sign in. Expect: the window and the column as thin cards; "OPEN GAMES" centred over the hairline; Open games
  marked in the column by a warm field; rows quiet with a dim line that brightens on hover; a disabled Observe clearly
  grey.
- [ ] Host a game. Expect: the chosen side card lit by a thin gold line; Public as a warm segment; Create game warm gold.
- [ ] Demo vs CPU, then Test scenarios. Expect: thin medallions and boxes in the rules row; thin tabs with the open one
  warm; a lens in the search field; scenario rows like the game rows.
- [ ] Open a waiting room (as host, with expansions). Expect: your seat lit by a gold line; the medallions lit in their
  enamel when on; the chips that are on warm; Start game warm gold once both are ready, grey before.
- [ ] Settings from the menu and from a game's Menu; Quit game. Expect: the same thin card, buttons, boxes and sliders;
  the chosen 100 %/125 % and window mode warm; the board's Menu panel as a small thin card while the top bar keeps its
  look.
- [ ] Repeat one screen at 1366×768 with 125 %. Expect: nothing clipped, every control in its usual place.

Tests: EditMode suite (`UiArtKitTests` lists the 23 new textures; `FrontendThemeTests`, `GatewayScreenTests`,
`MainMenuTests`, `WaitingRoomTests`, `ConfirmationDialogTests` unchanged and green); PlayMode `MainMenuLayoutTests` (two
new screenshots, `ui470-host-form-*` and `ui470-quit-*`; every page, the waiting room, Settings and the question in four
layouts), `TestScenarioNetworkTests`, `WaitingRoomRulesTests`, `ConfigurationLayoutTests`, `LayoutTests`,
`InterfaceSettingsTests`, `SoundSettingsTests`, `BoardMenuLayoutTests` and the menu network classes. Results
(2026-11-03): EditMode 1504/1504; every PlayMode class above passes except four failures already known before UI-470
(`MenuBrowserNetworkTests.Scrolling…`, `WaitingRoomRemovalNetworkTests.TheHostRemoves…`, `ReturnListTests.BothPlayers…`,
`BoardMenuNetworkTests.MenuReceives…`: status texts and timeouts, not styles). Screenshots:
`.artifacts/screenshots/ui470-before-*.png` and `ui470-after-*.png`.

## The game's buttons in the menus' thin line — UI-471 (2026-11-03)

First task of the game chrome migration (UI-471–UI-479). The owner: "update the game interface so it uses the same styles
and buttons as the main menu; we do not change the graphics of the map, characters, icons etc., only the tabs'
backgrounds, frames and buttons." This task changes every game button's chrome and creates the shared tokens. Panels,
windows, the rail, the console's frame, the battle's insides, notices and the top bar's plate keep their art until their
own tasks; their buttons change now. Sizes, paddings and positions are unchanged.

What changed:
- `.action`, icon buttons and every context override wear `frontend/button` (64×32 pt, 6-point slices, 12 texture pixels at
  scale 0.5) instead of `frames/button` and `frames/button-square`. Hover is `button-hover`; a press is `button-pressed`
  and still sinks a point (UI-113); a chosen button is `button-selected`, the warm field with a bright line, instead of
  red lacquer. Disabled is greyed. An icon on an icon button stays as it was.
- Words are the menus' muted parchment, not bright gold: `rgb(228, 216, 188)`, lighter on hover, warm when chosen, grey when
  disabled. Keyboard focus brightens the words only on a text button, so a clicked button does not look held; an icon
  button has no words, so focus lightens its line.
- A chosen button's old 2-point gold rim keeps its width in the layout and is transparent, so nothing moves and gold stays
  the sign of a legal target: a legal target keeps its thick gold rim and warm plate (UI-193, UI-223, UI-372, UI-384).
- The context overrides lost their copies of the art: they now only size and place the button, and the shared states apply.
- Buttons that stand directly on the bright map (the map legend and political view, chat, the Cards tab, the Victory tab,
  the notices' log and battle icons, the decision notice, Retry) have a nearly opaque dark field under the line
  (`--game-button-fill-over-map`, 0.92) and a 2-point radius matching the art's corners. `MapLegend` lost its inline colours.
- A figure or die on a `.action` that wore the old lacquer plate behind its picture (the battle's dice and losses, the
  Fellowship's portraits, the region panel's figures, Hunt responses) now stands on the thin plate. The picture itself is
  unchanged.

The shared tokens. `Styles/GameChrome.uss` is imported first by `Wotr.uss` and declares USS custom properties on
`.client-interface` and `.board-screen`; they are inherited by everything below. UI Toolkit in Unity 6000.6 resolves `var()`
for `background-image` with `resource()`, for the slices, for `-unity-slice-scale` and for colours (`GameButtonChromeTests`
reads the resolved values), so a later task changes one line:
- Art: `--game-button`, `--game-button-hover`, `--game-button-pressed`, `--game-button-selected`.
- Slices: `--game-button-slice` (12), `--game-button-slice-scale` (0.5px).
- Words: `--game-button-text`, `--game-button-text-hover`, `--game-button-text-focus`, `--game-button-text-selected`,
  `--game-button-text-disabled`.
- Over the map: `--game-button-fill-over-map`.

Rules for the next tasks (UI-472–UI-479):
- Add a surface's art and colours as `--game-<surface>…` tokens in `GameChrome.uss` and use `var()` in its rules. A token
  has no fallback outside those two roots.
- Never put `menu-chrome` on the board's root: `.menu-chrome .action` would repaint dice, cards and tokens.
- Do not give a context rule `background-image`, `color` or slices for a button: it would beat the states. Size it only.
- The states of `.action` (`:hover`, `:focus`, `.selected`, `.pressed`, `:disabled`) have two classes and stand in the first
  sheet on purpose, so that any art button's rule of two classes or more wins and a press never paints the chrome over
  its picture. The base `.action` stays in `Wotr.uss` at its old place in the cascade. `Interaction.uss` and `SkillIcons.uss`
  stay last. Static USS only, no CSS filter.

Art buttons. `docs/ui/BUTTONS.md` counts 70 `art` buttons, but an audit of every `.action` the tests below render showed that
few inherit `.action`'s slices: dice (`BoardScreen.Die`), Council and Ruler tokens, rings, skill icons and map figures are
not `.action` at all; cards, decks, battle cards, reserve slots, rail tabs and character rows already set their own slices.
Only two kinds inherited them: the Politics panel's `nation-token` and the region panel's `moved` tick. Both now set the
same 24 texture pixels beside their picture, so they render as before. `.battle-hit` gained a class so the hover's words
do not beat its green. `frames/button` and `frames/button-square` stay in the art kit, unreferenced.

Tests: `GameButtonChromeTests` (new) reads what UI Toolkit resolved for a text and an icon button in every state and for an
art button; `LeftRailLayoutTests` and `RightColumnLayoutTests` now expect the shared button art in panel headers. Results
(2026-11-04): EditMode 1504/1504. PlayMode, one class per run, passing: `GameButtonChromeTests`, `DicePanelLayoutTests`,
`TabletopObjectsTests`, `TurnConsoleLayoutTests`, `ConsoleCancelNetworkTests`, `BattleLayoutTests`, `LeftRailLayoutTests`,
`RightColumnLayoutTests`, `TopBarLayoutTests`, `ActionNoticeLayoutTests`, `BoardChromeLayoutTests`, `BoardMenuLayoutTests`,
`GlowingHintAuditTests`, `FateCardsNetworkTests`, `CharacterWindowLayoutTests`, `CharacterRosterPanelNetworkTests`,
`CharacterRosterActionNetworkTests`, `RulerTokenMenuTests`, `ExpansionDecisionNetworkTests`, `KingsDecisionNetworkTests`,
`T22AbilityNetworkTests`, `PressFeedbackClickTests`, `NoFocusOutlineTests`, `PoliticalPanelTests`. Two failures also occur
without this task's changes: `BoardCompletionLayoutTests.FullRosterVersions…` (same values before the change) and
`FarSightedObjectTests.ThranduilsPicture…` ("Missing component far-sighted-SeparateCompanions", also 2/2 with HEAD's
stylesheets). A temporary audit (removed) of what every `.action` resolved to while 26 classes ran, before and after, found
no art button with a changed picture or slices. Over the 18 classes with sizes recorded on both sides, 173 of 176 kinds of
button showed only known sizes; the other three (`pool-Hunt`, `pool-Spent`, a Kings roster row) were one-off samples that fit
a value pulse and a panel's slide. Screenshots: `.artifacts/screenshots/ui471-before-*.png` and `ui471-after-*.png`.

Playtest (owner's checklist):
- [ ] Start a game at 1366×768. Expect: Menu, the console's four squares and Cancel as dark fields with one thin muted-gold
  line, the words in muted parchment; the panels, the console's frame and the top bar's plate as they were.
- [ ] Hover, press and release a console square and Menu. Expect: the line lightens on hover; the field darkens and the
  button sinks a point while held; moving off while held does nothing.
- [ ] Open Politics, Characters and a card. Expect: thin header buttons in their old places; a pinned card's pin on the warm
  field.
- [ ] Fight a battle to its footer choices (Continue, Retreat…) and assign losses. Expect: thin footer buttons with their
  icons; figures and dice on thin plates; a chosen loss with its gold rim on the warm field, not red.
- [ ] Choose an action that offers a legal target among text buttons or tabs. Expect: the thick gold rim that pulses,
  clearly different from a chosen button's warm field and thin bright line.
- [ ] Look at the map legend, the political view, the chat icon, the Cards tab and the log icon over the brightest parchment.
  Expect: a dark field and a readable line and icon.
- [ ] Press a nation's token in Politics, a reserve figure and a hand card. Expect: their own pictures throughout.
- [ ] Repeat one screen at 125 %. Expect: nothing clipped, every control in its usual place.

## The docked panels in the menus' card — UI-472 (2026-11-04)

Second task of the game chrome migration. The owner (2026-11-03): "update the game interface so it uses the same styles and
buttons as the main menu; only the tabs' backgrounds, frames, buttons … remember to adapt elements such as the bars at the top
of the windows 'Denethor', 'Effects', 'Shadow' etc." UI-471 did the buttons; UI-472, UI-473 and UI-474 do the frames and header
bars of every panel and window. Only art and colours changed: every text, order, behaviour, size, padding and place is the one
it was.

What changed:
- The left panels (Politics, Reserve, Characters, Effects), the Fellowship dropdown, the corner tab, the card drawer and the dice
  panel's frame moved from `frames/panel` to the board's card (`frontend/card-board`). The deck buttons' card backs, the corner
  tab's gold rim for a usable Fellowship, the rail tabs (UI-475) and the dice panel's side band (UI-476) keep their art.
- The red lacquer headers are gone. Each is a quiet title (Cinzel, the menus' light gold, no faux bold) with the panel's icon, over
  a hairline fading at both ends. Heights are unchanged (32 points on the left, 28 on the right, 26 when compact), and so is each
  child's place. Politics' plain label takes the same Cinzel as the other titles.

Decisions:
- The board has its own card. The menus' `frontend/card` stands over a dimmed map, at 0.9. Over the bright parchment 0.9 let the
  map's white borders and tokens show through the log's words and mixed a pool list with the Hunt window under it, and the muted
  gilt line disappeared on parchment. `frontend/card-board` keeps the shape (the same concave corners, lozenges and inner rule) with a
  0.985 field and a dark hairline outside the gilt rule.
- The hairline is a picture (`frontend/title-rule`, 200×1 points, `background-size: 100% 1px`), not a border. A border would take a
  point from the row, moving each child half a point and blurring a thin button's line, and would grow the character window's
  30-point header, whose portrait fills it.
- Titles stay at their old left place rather than centred as in the menus' reference: the row must fit an icon, a locate button, a
  portrait, a status badge and buttons, and a centred lozenge cannot sit on a stretched line. A later task can centre the title-only
  ones if the owner prefers.
- Dropping the faux bold frees about 0.07 em per letter, which pays for wider letter-spacing, so no title takes more room than it
  did (`GameWindowChromeTests` measures this with a long battle title).

Tokens (`Styles/GameChrome.uss`, on `.client-interface` and `.board-screen`): `--game-card`, `--game-card-slice`,
`--game-card-slice-scale`, `--game-card-fill`, `--game-card-text`, `--game-card-text-muted`, `--game-hairline`,
`--game-hairline-quiet`, `--game-title-text`, `--game-title-rule`, `--game-title-ink`, `--game-title-rule-ink`. The frames' and the
headers' art stand in two shared rules of the same sheet; a surface's own sheet keeps its size, padding and transparent
`background-color`. A new frame joins the first rule. `Interaction.uss` still comes after the chrome; no CSS filter.

Art (`graphicalassets/ui/src/frontend`, `render.py --only frontend/card-board`, `--only frontend/title-rule`): `card-board` (64 pt, slices
12) and `title-rule` (200×1 pt), both in `UiArtKitTests`' contract. `frames/panel` stays in the kit, unreferenced;
`frames/panel-header` and `panel-header-free-lacquer` now hold only the dice band.

Playtest (owner's checklist):
- [ ] Start a game at 1366×768. Open Politics, Reserve, Characters and Effects over the bright map. Expect: a dark, thin card whose
  edge reads clearly against the parchment; a quiet gold title and a fine line instead of the red band; every button in its old place.
- [ ] Hover the corner tab. Expect: the Fellowship panel unrolled in the same card, its title beside the Hidden/Rivendell button.
- [ ] Look at the card drawer and the dice panel. Expect: both in the thin card; the card backs and the red or blue side band as they
  were.
- [ ] Make the Fellowship usable (a Guide's skill). Expect: the corner tab's gold rim still pulses over the thin card.
- [ ] Repeat at 125 %. Expect: nothing clipped, every control in its usual place.

## The floating windows in the menus' card — UI-473 (2026-11-04)

The same treatment for what floats over the board: the decision window, the battle window's frame and header, the chronicle's frame,
the Hunt window and its nested pool, the character and sovereign windows (the owner's "Denethor" bar), the end-of-game panel, and the
headers of the recorded-action card and the card inspector.

- Frame: `frontend/card-board`. Header: a quiet title over the hairline. Heights are unchanged (26 points; 24 in a short viewport;
  30 for a character; 34 for the recorded action), and the portrait medallion, title, status badge, pin and close keep their places.
- A skill's gold rim on a character's header (UI-441) lies over the row as before. No window header carried a side colour before this
  task, so none was lost; a character's side stays on his medallion's ring.
- On parchment the title is ink and the same line is tinted dark (`--game-title-ink`, `--game-title-rule-ink`). The parchment faces of
  the recorded action and the card inspector stay.
- Kept for UI-477: the force frames, the stage ribbon, card slots and round tabs. The end-of-game title plaques stay.

Playtest (owner's checklist):
- [ ] Open a king's window from Characters. Expect: the portrait medallion at the head of a quiet name over a fine line, the status
  badge and the close button in their old places, and no red bar.
- [ ] Fight a battle with the longest name you can (Dol Amroth, a siege). Expect: the whole title, the card setting and the close
  button on one row; forces, ribbon and footer as they were.
- [ ] Open a Hunt and its pool list from the header. Expect: the pool over the window in the same card, and the window's words not
  showing through it.
- [ ] Choose something that opens the decision window. Expect: the same card and header, anchored above the console.
- [ ] Click a notice and a hand card. Expect: parchment with a dark title and a dark fine line; the pin and the close button in place.
- [ ] Open a battle's report from a recorded action. Expect: the same card round its parchment pages.
- [ ] Finish a game. Expect: the thin card round the painting, the green or iron title plaque unchanged.

## The flat windows and scroll bars — UI-474 (2026-11-04)

The windows that had no art take the card, each old border staying in the layout as a transparent one so nothing moves: region
information, board information, the expansion inspector, the action log, the notice overflow list, the victory panel and the map legend.
Their rules also lost their old colours (a separator is `--game-hairline-quiet`, a muted line `--game-card-text-muted`), and a region's
name, like every title, is the quiet gold. Three stay flat with the card's colours and hairline: the tooltip, because it can be very
small; the undocked hand drawer, because the docked hand wears the same class and clears only its colour (`Cards.uss`); and the board's
chat, which was not listed but had the same old rule. `MapLegend` lost its last inline colours.

Scroll bars under the board are the menus' thin ones, by rules scoped to `#board-screen` in `GameChrome.uss`, never by putting
`menu-chrome` on the board's root (tokens `--game-scroll-track`, `--game-scroll-thumb`, `--game-scroll-thumb-hover`,
`--game-scroll-arrow`, `--game-scroll-arrow-hover`). Copying the menus' rules alone was not enough: they narrow the bar to 10 points but
leave the theme's thumb 18 points wide and its track and arrows 24, so those stand out of the bar. On the board every part is sized to
it: a 6-point thumb inside a 10-point bar. A view's content gains the 3 points the bar gave up.

Playtest (owner's checklist):
- [ ] Hover a console button over the parchment. Expect: a small dark tooltip with one fine gold line.
- [ ] Click a region with forces, then an empty one. Expect: the card round its figures; the name in quiet gold; the small window for the
  empty region sized to its text.
- [ ] Open the log and scroll it. Expect: a thin card, faint separators, a slim gold thumb inside a slim bar, and no map showing through
  the words.
- [ ] Open the map legend over the left rail, the game rules and the victory panel. Expect: the same card; the words readable; every
  button in its place.
- [ ] Open a chat. Expect: the same dark field and fine line.

Tests for the three tasks: `GameWindowChromeTests` (new) reads what UI Toolkit resolved for every frame and header, the ink headers,
scroll bars and flat surfaces, and measures the titles. `LeftRailLayoutTests` (the title fits, Politics included), `RightColumnLayoutTests`,
`EdgePlaqueTests`, `DicePanelLayoutTests` and `ActionNoticeLayoutTests` expect the new art where they named the old; what they check is
unchanged. `UiArtKitTests` lists the two new textures.

Results (2026-11-04): EditMode 1504/1504. PlayMode, one class per run, passing: `GameWindowChromeTests`, `LeftRailLayoutTests`,
`RightColumnLayoutTests`, `EdgePlaqueTests`, `DicePanelLayoutTests`, `BattleLayoutTests`, `HuntLayoutTests`, `CharacterWindowLayoutTests`,
`CharacterRosterLayoutTests`, `ActionNoticeLayoutTests`, `BoardChromeLayoutTests`, `TurnConsoleLayoutTests`, `GameButtonChromeTests`,
`InGameDenseLayoutTests`, `LayoutStressScenarioTests`, `ClickInspectionTests`, `GlowingHintAuditTests`, `SovereignSectionLayoutTests`,
`GameEndNetworkTests`, `VictoryPanelLayoutTests`, `RuntimeTooltipTests`, `RegionNameOnlyTests`, `ReinforcementsLayoutTests`,
`FloatingWindowTests`, `CardViewportLayoutTests`, `PoliticalPanelTests`. `PoliticalPanelTests` first failed in 2 of 3 runs with "Title and
toggle overlap" (2 of 2 passed on HEAD's stylesheets; a flake at that line has been on record since 2026-10-27): the growing title's edge
meets the toggle's and rounding can cross them. The title now stands a point apart, and the class passed 4 of 4 runs. One failure is not
from this work:
`BattleReportNetworkTests.ClosedReportRejects…` ("last-battle is covered by notification-bar") fails the same way on HEAD's stylesheets.
Screenshots: `.artifacts/screenshots/ui472-before-*.png`, `ui472-after-*.png`, and the same for `ui473` and `ui474`.


## The left rail in the menus' thin line — UI-475 (2026-11-04)

Next task of the game chrome migration (UI-471–UI-479). The owner (2026-11-03): "the game interface takes the main menu's styles and
buttons; only the tabs' backgrounds, frames and buttons change, not the map, characters, icons, dice or cards." Only art and colours
changed: every text, order, behaviour, size, padding and place is the one it was.

What changed:
- A rail tab's plate (UI-223's lacquer plate, radius 12) is one thin line on a nearly opaque dark fill, radius 2, like a menu button.
  Hover lightens the line and the field. The art `rail/edge-tab` keeps its name, 94×66 size, icon centre (47, 22) and six variants, and
  is re-drawn after `frontend/medallion`: a dark enamel disc in one thin ring and a hairline ring, without beads or bevel. The panel
  icons on it are unchanged.
- The selected tab (a panel held open) lost its red rim for the warm selected line, with a warm plate and disc.
- The usable tab keeps its 3-point gold rim on a warm plate, its gold label and its pulse (UI-193, UI-223, UI-384), and the art's gold
  halo round the disc. It stays clearly apart from the selected line.
- A press darkens disc, ring and plate and sinks a point (UI-113). Politics and Reserve are `.action`, whose press cleared the plate over
  the map; `Interaction.uss` now gives every pressed tab its dark plate.
- Effects and Characters entries are thin rows (`frontend/row`, hover `row-hover`); a Shadow effect stands on `row-shadow`. A roster row
  has a press of its own (`row-pressed`). A chosen roster row is `row-selected`, its 2-point rim staying in the layout and transparent,
  because a gold rim means a legal target (as for buttons, UI-471). A sovereign's portrait stands on the same row.
- Politics rows are the side rows (`row-free`, `row-shadow`) instead of translucent fills: a hairline in the side's colour keeps their
  meaning, and the old 1-point separator stays in the layout as a transparent border.

Tokens (`Styles/GameChrome.uss`): `--game-rail-tab`, `-hover`, `-pressed`, `-selected`, `-available`, `-available-hover`;
`--game-rail-fill`, `-hover`, `-pressed`, `-selected`; `--game-rail-line`, `-hover`, `-selected`; `--game-row`, `-hover`, `-pressed`,
`-selected`; `--game-row-slice` (16), `--game-row-slice-scale` (0.5px); `--game-row-free`, `--game-row-shadow` and their `-hover`. Every row
of the board shares one slices rule in that sheet; each surface's sheet names its picture.

Art (`render.py --only rail`, `--only frontend/row`): `rail/edge-tab` re-drawn; new `frontend/row-pressed`; `--tab-enamel-*` overrides in
the `selected` variant, `--fe-line` in `available` and `available-hover`. All in `UiArtKitTests`' contract. `frames/entry*` stay in the kit,
unreferenced.

Playtest (owner's checklist):
- [ ] Start a game at 1366×768. Expect: four tabs as dark plates with one thin line, a thin disc round each icon, labels readable over the
  map and the sea.
- [ ] Hover a tab, then press and release it. Expect: the line lightens; the plate and disc darken and sink a point while held, the map
  never showing through.
- [ ] Choose a Muster die. Expect: Politics and Reserve with a thick gold rim on a warm plate that pulses, clearly different from the rest.
- [ ] Open Politics. Expect: blue rows and red rows with a fine line in their colour; nation tokens as they were.
- [ ] Open Characters and Effects in an expansion game. Expect: quiet rows; a Shadow effect on a dark red row; a row darkens while held;
  portraits, ability icons and parchment card tiles unchanged.

## The console and the dice panel in the thin line, with their side's colour — UI-476 (2026-11-04)

Colour variants of the kit, without new SVG (`assets.json`): `frontend/row-free`, `row-shadow` (and `-hover`), `frontend/button-free`,
`button-shadow` (and `-hover`, `-pressed`). They override the `--fe-*` tokens with the side's enamel: the field `#0c2135` or `#2a0b09`, the line
`#2f5f82` or `#7c2b22`, at 0.96, so they read as blue and red over the dark card and the bright map alike (checked in the screenshots: the
force frames are the largest such areas). Only the `frontend` sources read `--fe-*`, so the other art of those variants is unaffected.

What changed; every height and control's place stays (bottom row 140, console 132, dice panel 132, `TurnConsoleLayoutTests`):
- The console and its options strip are the board's card (`frontend/card-board`, joining `GameChrome.uss`'s shared frames rule); the
  dividers are the quiet hairline.
- The status plaque is the button in the side's enamel (`button-free`, `button-shadow`), which gives it a hover and a pressed picture it did
  not have. Its halo (`console/status-glow`, "must act", UI-241) stays; it only toggles classes.
- The Hunt well (108×60) is a thin row; while eligible for Hunt dice it is `row-selected`, the warm field with a bright line. The plaque's gold
  rims inside it are unchanged.
- The small popover of the Elven Rings, Lore and the Council token's travel is the board's card; its tail is re-drawn thin under the same name
  and size (`console/picker-tail`).
- The dice band is the side row (`row-free`, `row-shadow`) with a quiet title in the menus' gold, without the faux bold. It keeps FREE PEOPLES
  blue and SHADOW red (UI-466), its 24-point height and the title's place. CHOOSE A DIE stays gold.
- The turn band over the map (440×52) is the side row with a gold Cinzel title. The side row has 8-point slices instead of 12 and 28.

Tokens: `--game-row-free`, `--game-row-free-hover`, `--game-row-shadow`, `--game-row-shadow-hover`, `--game-side-button-free`, `-free-hover`,
`-free-pressed`, `--game-side-button-shadow`, `-shadow-hover`, `-shadow-pressed`. `console/frame`, `status-plaque-*`, `hunt-box*`, `picker`, `ribbon-*`,
`frames/panel-header` and `panel-header-free-lacquer` stay in the kit, unreferenced.

Playtest (owner's checklist):
- [ ] Start a game as the Free Peoples. Expect: a thin console card; a dark blue status button with a thin blue line; a blue dice band with a
  quiet gold FREE PEOPLES; every control in its usual place.
- [ ] Hover and press the status button. Expect: it lightens, then darkens and sinks a point; the click opens the decision's window as before.
- [ ] Wait for the action to come to you. Expect: a thin dark blue band over the map for about a second and a half, the gold halo round the
  status button while the decision waits.
- [ ] Play as the Shadow. Expect: the same in dark red. Allocate Hunt dice. Expect: the Hunt well warm with a bright line, clearly lit.
- [ ] Use an Elven Ring on a die. Expect: the results in a small thin card with a thin tail over the die.
- [ ] Choose an option too long for the console's line. Expect: the strip above the console in the same card.
- [ ] Repeat at 125 % and in an expansion game. Expect: nothing clipped, every control in its usual place.

## The battle window's insides in the thin line — UI-477 (2026-11-04)

UI-473 did the battle window's frame and header; this task does what is inside it. The scene strip's illustration, dice, figures, portraits and
cards are unchanged.
- The force frames are the side rows: blue for the Free Peoples and red for the Shadow, one thin line, flat fill, in the battle and in the
  report. At their size the art stretches only flat fill and straight lines, on 8-point corners instead of 14.
- The stage ribbon (22 points; 20 in a short viewport), used by the battle and Hunt windows, is a thin row. Its gilt marker and icons stay.
- An empty card slot (33 points) is a thin row with `row-hover` and `row-pressed`. A committed card's back and a revealed card's parchment keep
  their pictures and their own slices, so the shared rule stays out of them: the slot sets its slices beside its picture.
- The report's round tab is a thin row with a card's words instead of a parchment tab.
- The ledge of figures that stay out of the fight (`battle/garrison-shelf`) was not listed, but it is the last frame of the window, so it is a thin
  row too.
- The policy control (138×22) has been `.action` since UI-471, so it was already a thin button; `GameWindowChromeTests` now reads that.

`battle/force-frame-*`, `stage-ribbon`, `card-slot`, `garrison-shelf` and `round-tab` stay in the kit, unreferenced.

Playtest (owner's checklist):
- [ ] Fight a battle. Expect: a blue force and a red force with flat fields and one thin line, no gilt corners; figures, dice and portraits as
  they were.
- [ ] Look at the stage ribbon as the stages advance. Expect: a thin row; the marker gliding as before.
- [ ] Choose a Combat card. Expect: the other side's empty slot as a thin row; a committed card with its leather back; a revealed one on parchment.
- [ ] Fight where figures stay out of the fight. Expect: a quiet row with the same words.
- [ ] Open the report of a battle with more than one round. Expect: the round's name on a thin row between the arrows.
- [ ] Repeat at 1366×768 with 125 %. Expect: nothing clipped, the rows level on both sides.

## The notices in the thin line — UI-478 (2026-11-04)

- The notice bar has no plate, and its plain decision notice and icon buttons have been thin since UI-471, on a nearly opaque field over the map.
- A required decision (UI-254) stays red as a signal: a clear red field under one thin warm line instead of a gold one, with the buttons' 2-point
  corner. Its pulse keeps the 750 ms rhythm and dims the same two colours; Reduced motion keeps it still. Tokens: `--game-warning-fill`, `-fill-hover`,
  `-fill-dim`, `--game-warning-line`, `-line-dim`, `--game-warning-text`.
- The pennants and the wax seal are unchanged, as the owner asked.

Playtest (owner's checklist):
- [ ] Let the opponent act. Expect: pennants and seal as they were, next to the thin log icon.
- [ ] Reach a decision the game waits on (excess units, losses). Expect: a red notice with "!" and a thin warm line, pulsing, clearly a warning next
  to the dark buttons; a click opens the decision's window as before.

Tests for the four tasks: `GameWindowChromeTests` gains checks of what UI Toolkit resolved: the console, its strip and the popover among the frames, the
rail tab's states, the side surfaces and their slices, the battle's rows, a card keeping its back and slices, and the required notice's red.
`DicePanelLayoutTests` and `LeftRailLayoutTests` expect the side rows and rows where they named the old art; what they check is unchanged. `UiArtKitTests` lists the
11 new textures.

Results (2026-11-04): EditMode 1504/1504. PlayMode, one class per run, all passing: `GameWindowChromeTests`, `LeftRailLayoutTests`,
`BoardInteractionTests`, `GlowingHintAuditTests`, `PoliticalPanelTests`, `ReinforcementsLayoutTests`, `TurnConsoleLayoutTests`, `TurnRibbonTests`,
`DicePanelLayoutTests`, `ConsoleCancelNetworkTests`, `WillOfTheWestFaceNetworkTests`, `HuntLayoutTests`, `BattleLayoutTests`, `LordsBattleNetworkTests`,
`ActionNoticeLayoutTests`, `BoardChromeLayoutTests`, `GameButtonChromeTests`, `InGameDenseLayoutTests`, `ExpansionMatrixLayoutTests`,
`LayoutStressScenarioTests`, `PressFeedbackClickTests`, `CharacterRosterLayoutTests`, `SovereignSectionLayoutTests`, `EffectsLayoutTests`,
`RightColumnLayoutTests`, `EdgePlaqueTests`, `CharacterRosterActionNetworkTests`, `BattleRowsNetworkTests`. The new required-notice check first read the colour
in the first frames of its 750 ms transition and now waits it out. The full PlayMode suite was not run.
Screenshots: `.artifacts/screenshots/ui475-*`, `ui476-*`, `ui477-*`, `ui478-*` (`before` and `after`), taken by a temporary test that was removed. States that the
fixtures do not reach (a tab's states, a pressed or chosen row, the Hunt well lit in the fixture, the options strip, the required notice, the round tab) were forced by
their classes. `ui475-before-roster-free` and `ui475-before-sovereigns` are copies of `ui472-after-characters` and `ui473-after-king`; the round tab has no before picture.

## The top bar in the thin line — UI-479 (2026-11-04)

The last task of the game chrome migration (UI-471–UI-479). The owner (2026-11-03): "the top bar we could try to slim a little, but without losing any
information"; he chose 36 points, with click targets staying at 32 points or more. Art and sizes only: no behaviour, text, order or control changed. Every
counter, die, state and tooltip stays.

| Part | Before | Since UI-479 |
| --- | --- | --- |
| Bar | 44 points, `surfaces/topbar-plate` under `frames/topbar-frame` | 36 points, the card's colour, opaque, with one 1-point muted gilt rule along its foot |
| Shadow on the map | 8 points | 4 points, the same `surfaces/cast-shadow` |
| Phase, side name, status | 15, 11, 11 px | unchanged; the status lost the theme's padding, so its longest text (two lines) is 30 points high instead of 38 |
| Crests | 18 points | 16, in the bar only |
| Divider after the crests | 10×34 ornament | 1×18 hairline |
| Opponent's dice | medallions 29 (28 compact), tiles 44×32 (42×32) | unchanged, with every rim |
| Counters | in a 32-point cartouche; 28 high, icon 22, value 16 | on the bar; 24 high, icon 18, value 15 |
| Between counters | 8×24 ornament | 1×18 hairline |
| Menu | 64×32 picture and target (56 wide compact) | a 28-point `frontend/button` picture on the same 64×32 target |

Width: Kings and Lords against the Shadow, six counters: 328 points become 252. The fullest pool in the screenshots (10 dice, Ruler, two Lords dice and a tile) squeezed the
phase at 1366×768 and 125 %; it now leaves 79 points free. `TopBarView.CompactBelow` (1340) is unchanged.

Decisions the numbers above do not explain:
- **The Menu's two heights.** UI Toolkit draws a sliced picture over the whole element, whatever its `background-size` (measured in the screenshot: 32 pixels), so a 28-point button on a
  32-point target needs the picture whole: `#open-board-menu` sets the slices to 0, an exception to UI-471's rule that a context rule sizes a button only. All its states take the same
  size, so no state is beaten. At 7/8 its lines stay one pixel, in the same colour (measured).
- **The offered card counter.** The only counter a click can reach (UI-432) was 28 points high. While offered it stands 32 high over -4-point margins, so nothing moves; a plain
  counter stays at 24.
- **An opaque fill.** The map's figures run under the bar and showed through at 0.96, as faint ghosts.
- **The Ruler die's crown** stands 2 points lower (`top: -5px`), or its tip would leave the screen. A Lords die's star and emblem already fit.

Everything that hung from the old height follows the new one. `Styles/Notices.uss` and the Menu panel read `--game-topbar-height` (36px) and `--game-topbar-menu-top` (38px) instead of 44 and 46.
The stage, the right column, the dropdown panels (`BoardEdgeLayout`), `BoardScreen.CardBounds` and `BoardMiddle` read the bar's own bounds. The left rail, the panels beside it and the turn band
are placed in the stage (`top: 58px`, `top: 48px`), so they rise with it, and the map's stage is 8 points taller. Nothing else in the sheets or code named 44 or 46 for the bar.

Tokens (`Styles/GameChrome.uss`): `--game-topbar-height`, `--game-topbar-menu-top`, `--game-topbar-fill`, `--game-topbar-rule`, `--game-topbar-hairline`. `surfaces/topbar-plate`, `frames/topbar-frame`, `frames/cartouche` and
`ornaments/divider` stay in the kit, unreferenced, and in `UiArtKitTests`' contract; `ornaments/divider-small` is still used by the right column. The element tree is unchanged: `TopBarView.Frame` now carries the rule, out of the bar's flow.

Known limit: at a fractional panel scale a 1-point hairline rounds to one pixel or two, by its position, as the console's dividers do.

Playtest (owner's checklist):
- [ ] Start a base game at 1366×768. Expect: a flat dark bar with one fine gilt line; phase, dice and four counters readable; the map 8 points taller; notices right under the bar.
- [ ] Hover and press Menu, then open it. Expect: a thin button that lightens, then darkens and sinks a point; the panel just under the bar. The top and bottom 2 points around the button still click.
- [ ] Start a Kings and Lords game as the Free Peoples. Expect: the Shadow's dice with the crown and emblems whole, six counters with Dismay and Council, crests after the phase.
- [ ] Reach Worn with Sorrow and Toil as the Shadow. Expect: the Cards counter with a taller red rim, pulsing, clicking as before, and nothing else moving.
- [ ] Repeat at 125 %. Expect: nothing cut, the side's name hidden as before, every tooltip as before.
- [ ] Lose the connection. Expect: the status beside Menu, in two lines at most, inside the bar.

Tests: `TopBarLayoutTests` expects 36 points, a 4-point shadow, flat chrome (no art on the bar, frame, summary or hairlines; a fill of at least 0.92; a 1-point rule), counters of 24, 18 and 15, 16-point crests, a die's
marks inside the bar and Menu's 32-point target with its 28-point picture. Its new `EveryTargetAndStateStaysInsideTheSlimBar` covers, in four layouts, a special tile at 32 points, the offered counter at 32 with nothing moved and
returning to 24, the three rims keeping a die's box, and the longest status. `EdgePlaqueTests` expects 36, `GameButtonChromeTests` reads the Menu's picture and an icon button's slices apart, and `BoardMenuLayoutTests` checks that the panel opens
just under the bar.

Results (2026-11-04): EditMode 1504/1504. PlayMode, one class per run, all passing: `TopBarLayoutTests` (2), `EdgePlaqueTests` (4), `ActionNoticeLayoutTests` (1), `BoardChromeLayoutTests` (3),
`BoardMenuLayoutTests` (7), `LeftRailLayoutTests` (1), `RightColumnLayoutTests` (3), `ExpansionMatrixLayoutTests` (2), `InGameDenseLayoutTests` (1), `LayoutStressScenarioTests` (9), `OpponentResourcePresentationTests` (8),
`CharacterWindowLayoutTests` (2), `GameButtonChromeTests` (2), `GameWindowChromeTests` (10), `GlowingHintAuditTests` (22), `NazgulStrikeObjectTests` (3), `InterfaceSettingsTests` (2), `PressFeedbackClickTests` (2), `BattleLayoutTests` (5),
`CardViewportLayoutTests` (2). `BoardRuleDeclineTests` passes 5 of 7 as a class: `WornsRedTableCard…` and `WornsSkipEffect…` fail in that order and pass each in its own run, and the same two failed as a class on 2026-11-02 (`ui435-3`), so they are
order-dependent and not this task's. The full PlayMode suite was not run.
Screenshots: `.artifacts/screenshots/ui479-before-*` and `ui479-after-*` (`base`, `kings-lords-shadow`, `kings-lords-free`, `discard`, `discard-pulse`, `rims-status`, `menu`, each at `1366-1.00`, `1366-1.25` and `1920-1.00`), taken by a temporary test that was removed.
Rims, the press and the status were forced by their classes and text; the before set has no `discard-pulse` or transition frame.
