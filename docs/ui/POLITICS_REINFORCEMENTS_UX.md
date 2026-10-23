<a id="tor-polityki-i-zakładka-posiłków--ui-178-ui-179"></a>
# Policy Track and Meals Bookmark — UI-178, UI-179

User application from 2026-10-15 (Politics panel discharge):

> Let us get rid of the separate reinforcements window and convert it to another tab under politics.
> It is not to directly display information about whether the nation is activated or at what stage;
> if it is not recruitable, its government is to be unlit and unclip. Each row, like now in
> reinforcements, is to show units that can be used as reinforcements. Let's improve the current
> design of the policy tab, because it now looks like it is being re-split, looks like a series of
> buttons, not a track. this is one and the other that the place on the right is wasted — there
> should be a small button of the arrow icon right that is extinguished when it is not being used.
> The rest of the free, useless place should be liquidated.

<a id="stan-przed-zmianą-klient-6278b00"></a>
## Pre-change Status (`6278b00` Client)

- Politics (`PoliticalPanel`, `NationTrackRow`): The line is a name, four steps and a hidden
  “Activate” button (78 pt reserved also when it is not there). The steps are `.action` buttons with
  `frames/button` frame, so they look like a row of buttons. The small 9-slice frame looks like a
  stacked one. The step forward shows “→” in the next step.
- Meals (`ReinforcementsPanel`, UI-130–UI-135): separate window `FloatingWindow` opened alone at its
  own dice Muster or recruitment plan. Nation line: coat of arms, name, mini-tor of politics
  (`ReinforcementTrackView`), reserve chips and action Muster (icon Politics: step on track, ring
  icon: Narya activation).

<a id="a-tor-polityki--ui-178"></a>
## A. Policy Track — UI-178

- The poem has three columns and nothing more:
  - coat of arms with a name;
  - tor;
  - a small right arrow button (`nation-advance-<id>`, `next` icon).
The panel has a width of this content (about 290 pt instead of 342); the place after the "Activate" disappears.
- Track is a continuous gold rail with four stations: three steps as small rings and War as a
  medallion with crossed swords. Stations are not buttons, they have no frame or button background.
  Draws them USS (frames and roundings), so they are sharp on every scale. Nation's token (modalion
  side, ● active, ⁇ passive) stands at the station of its step.
- The right arrow is in each row in the same place:
  - enabled and lit (`legal-target`) when selected die can move this nation a step; click moves the
    nation without Confirm;
  - extinguished (inactive but visible) in other cases, including in enemy lines.
- When the step is legal, the next station on the rail also shines. Click on the token or drag it to
  this station is still shorthand (UI-144). The sign ‘→’ in the field disappears.
- activation (only expansions, e.g. Narya): when the server offers it, the same button shows the
  icon ✓ with the tooltip ‘Activate...’. base game She doesn't offer it, so the button is always an
  arrow.
- The axis above the track (Peace · · War) stands above the stations; legend ⁇ / ● above the names
  remains.

<a id="wdrożenie-ui-178-2026-10-15"></a>
### implementation UI-178 (2026-10-15)

- `NationTrackRow` has columns: name with coat of arms (it grows by the rest of width), track
  `nation-slots` (144: `nation-rail` rail and four stations after 36 pt) and column `nation-actions`
  with arrow `nation-advance-<id>` (icon `next`, 26 pt).
- `political-slot-<id>-<krok>` stations are buttons without `.action` class. They have a clear
  background in all states, and their mark (`nation-station`) is the 10 ring entitled "The stations
  have been travelled with gold." War is a medallion with swords (`actions/battle`) in the red
  border. The station of legal step grows to 16 called and shines gold.
- The arrow is out (35% and inactive when die cannot move the nation, also in enemy lines. It has
  then tooltip with a reason. Activation with expansions  (`nation-activate-<id>`, icon ✓) stands in
  the same place and hides the arrow on offer.
- The Politics panel at the slat has 290 pt (previously 350, with the column "Activate"). The axis
  "Peace · · War" has an arrow column spacing, so the headers are above the stations.

<a id="b-zakładka-posiłków--ui-179"></a>
## B. Meals tab — UI-179

- The separate Reinforcements window disappears. On the left panel under Politics is the
  “Reinforcements” tab (Icon `reinforcements`). The order of the strip: Politics, Reinforcements,
  Characters (Setup), Effects. The panel is docked at the strip like the others and develops after
  invading (UI-92).
- The poem is only a coat of arms, name and tokens of reserves that the nation can give as meals
  (regulars, elites, leaders and nazgûle with meters, so far). The poem does not show a path of
  politics, activation nor step into war and there is no action Politics or Narya. Step on the track
  is taken in Politics (A).
- During recruitment (die Buster, card plan or capabilities):
  - the nation that can recruit has an illuminated line and its chips are clickable (choice of the
    token, highlighted settlement, drag like in UI-130);
  - The nation that cannot recruit has a line unlighted, and the poem and its chips are
    inconsequential. The reason for the server is only in the line tooltip.
- Besides recruiting, the tab shows reserves for viewing: all lines unexposed and inconsequential.
- Panel header: icon and “ REINFORCEMENTS ’ (with the title of the source of the plan, e.g. “·
  Muster”), budget slots, ✓ Finish or ✓ Confirm and cross. Scheduled figure plan (from ×) are under
  the lines.
- Opening (change of UI-168, AO03): Own die Muster or recruitment plan develops the tab and holds
  it, just like the panel holding the decision. The panel closed by the player is closed for next
  use. The settlement click on the recruitment will re-open it (UI-135). The opponent can see
  nothing. End of use will close the tab.

<a id="wdrożenie-ui-179-2026-10-15"></a>
### implementation UI-179 (2026-10-15)

- `ReinforcementsPanel` has a `reinforcements-tab` tab (`actions/reinforcements` icon) under
  Politics. The panel is docked at the slat (`edge-left-panel`, 320 pt), and the frame and header
  takes from `LeftRail.uss`. The `FloatingWindow` window does not exist.
- The use (`ReinforcementsState.Open`) develops the panel and sets the `Held`, so the preview of the
  strip (`LeftRailPreview`) holds it. Crossword and Esc (`Close`) reject it for next use. Selecting
  another tab (`Collapse`) just rolls the panel and does not reject anything. The console and click
  settlement restore the closed panel (`Show`, `Reopen`).
- In addition to recruitment `ReinforcementsState.Browse` gives rows with reserves: no budget and ✓
  inactive but full colour chips (`reserve-readonly`).
- A poem (`ReinforcementRowView`): coat of arms, name, chips. Class `recruitable` (green background
  and left edge) means a nation that can now recruit. Nation without recruitment right has a
  extinguished name and inactive chips. Tooltip row: name, card plan limits and server reason.
- `ReinforcementRow` no longer has track fields (`StepsToWar`, `Active`) and the status of war does
  not go to Muster lines or Muster plan. `ReinforcementTrackView` removed. Step on track with dice
  Muster makes an arrow in Politics (UI-178).

<a id="zakładka-reserve-i-grafika-zakładek--ui-181-2026-10-15"></a>
## Reserve tab and bookmark graphics — UI-181 (2026-10-15)

User applications: "change the reinforcements to reserve, improve the design of all three tabs, because now they look a bit basic, have little detail and look like a pattern/placeholder" and "let tab reinforcements not open automatically to the dice selection, let them be graphically as the rest of the tabs".

- The tab and the header of the panel is “Reserve” (Plan card: “Reserve · source”). Names in the
  code (`ReinforcementsPanel`, `reinforcements-*`) remain.
- die muster does not develop the tab. It shines with green light dice (`DieOptionGlow`, like
  Politics and Characters) and `eligible` variant (`legal-target`). The player develops it by
  driving, clicking, clicking settlement (UI-135) or by selecting the console. When he selects the
  chip, the panel stays (`Held`) for the time of deployment of figures. It also stays with the next
  figure (open recruitment sequence).
- Recruitment plan with card or capabilities is a decision that only this panel ends (✓ Confirm). It
  expands the tab and keeps it, just as the Characters and Effects keep their decisions.
- In both cases, the cross closes the panel for next use and the end of use wraps it.
- Bookmark graphics (`rail/edge-tab`, 94×66 pt) replaces the general button frame in Politics,
  Reserve, Characters (Setup) and Effects:
  - a lacquered plate in a carved gold frame with a pin towards the panel;
  - enamelled medallion in a pearly ring with a filigree per icon (24 pt.);
  - a gold ribbon with swallow tails on thickened signature;
  - states: hover (lighter gilt), `eligible` (green glow), selected (red lake), pressed.

<a id="zakładki-bez-wstęg-i-ramy--ui-182-2026-10-15"></a>
## Tabs without ribbons and frames — UI-182 (2026-10-15)

User application (UI-181 bookmarkshot): "Delete these ribbons added to each icon and correct the titles, now they are terribly crooked, remove all this frame around".

- `rail/edge-tab` is only an enameled medallion in a pearly ring: without a tile, carved frame with
  a pin, filigree and ribbons under the signature. The state shows the border of the medallion: red
  lake at the selected green glow at the legal goal, brighter gold at the landing.
- The signature is a simple label 13 pt (without thickening), one line, centered under the
  medallion, with a strong shadow, because there is a map underneath it. `LeftRailLayoutTests`
  checks that it fits in the tab.

## Lit tokens and one armed token for a card — UI-213 (2026-10-16)

User request of 2026-10-16 ([CARD_CHOICES_UX](CARD_CHOICES_UX.md)): one click on a highlighted figure, then
distribute it on the board while the rules allow.

- A token the player may choose now (an open, interactive use; Muster or a plan) is lit with
  `eligible-token`: since UI-301 the shared ChoiceGlow rim and plate over its slot, pulsing with its row, and a full
  glow, without changing its size or moving its figure. The
  tab keeps its `legal-target` glow during the use.
- A card's recruitment (`EventRecruitDraft`): an armed token stays armed after each placement, with its group's
  next free figure, while the card's limits (per region, per nation, in total) leave that figure a region; at a
  limit it is released. A click on another token switches the armed token; a click on the armed token releases
  it. Region first is unchanged when no token is armed: a token click fills the chosen region.
- ↶ removes the last placement and re-arms the token. The console counts "Recruited n of m"; its ✓ and the
  panel's ✓ send the same plan. A `ReserveFigures` choice (each figure has its own region) keeps its one click.

<a id="scenariusze-i-właściciele"></a>
## Screenplays and owners

| ID | Owner | Scenario |
| --- | --- | --- |
| PR01 | UI-178 | Politics row: name, track without buttons frames, arrow; panel width without empty column in four layouts. |
| PR02 | UI-178 | die Muster: The arrow of legal nation shines and moves it one step; other arrow and enemy lines extinguished. |
| PR03 | UI-179 | die Muster: Reinformments page developed and held; recruiting highlighted lines, other non-clip; track and status missing. |
| PR04 | UI-179 | Plan  card: source title, slots, ✓  Confirm,  figure with × in the tab; when closed, it is closed for next use. |
| PR05 | UI-179 | No recruitment: tab shows reserves, all non-climatic lines; no separate window. |

<a id="tor-przeciwnika--ui-307"></a>
## The opponent's political steps — UI-307 (2026-10-23)

An opponent's recorded political change opens this panel read-only during its presentation
([OPPONENT_ACTIONS_UX §6.11](OPPONENT_ACTIONS_UX.md#wdrozone-307)): the nation's token is traced, an activation lights
its crest first, then the token travels its recorded steps along the rail, and reaching the War medallion adds the
short caption "At war". The panel is never opened while it is live for the player's own decision; a panel the player
had open stays open, one the presentation opened closes again, and one the player closes is not reopened by the same
presentation. The track's stations, arrow and activation controls keep their rules above; nothing is added to the row.
The Reserve panel does not open for an opponent's recruitment: recruits appear on the map with their delta badge.
