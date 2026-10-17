<a id="characters-przygotowanie-i-menu--plan-ui-65ui-72"></a>
# Characters, Preparation and Menu — UI-65-UI-72 plan

The project adopted in the 2026-10-08 interview and written down in the documentary UI-64. [Registry](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/tasks/INDEX.md) defines the statuses. Description and matrix are requirements for future implementation, not evidence of acceptance.

<a id="zakres-i-odpowiedzialność"></a>
## Scope and responsibility

User Elections: only UI at board; Characters only includes name characters; one panel version/Guide; small Menu. The usual Nazgûle are selected from the map. We do not rebuild the login, lobby or party list; Games leads to the existing screen. We do not add new rules, strategy slots or the next notification system.

| Task | Liability |
| --- | --- |
| UI-65 | Own list of characters, life cycle, versions and safe entry conditions. |
| UI-66 | Characters tab and common lists/maps/sovereign view. |
| UI-67 | Movement, inputs, transformations and capabilities by existing controllers. |
| UI-68 | One private version panel and the initial Guide. |
| UI-69 | A separate decision of the Council's homeland and transfer to the resources of Shadow. |
| UI-70 | Menu, settings, fokus/Esc and safe return to Games. |
| UI-71 | Appropriate decision windows, log/alerts, reconnect and regression of common panels. |
| UI-72 | acceptance CM01–CM18, both roles, expansions, systems, performance and builds. |

The foundation is UI-21/22/23/35 and UI-47-CONTEXT The new projection benefits from availability UI-56 ; panel from sovereign preview UI-59 ; costs and tokens from UI-58 ; Menu with Help UI-61 . UI-71 Waiting for integration UI-62 , UI-72 also acceptance UI-63 Older tasks do not depend back on this package.

Sources: [UX_SPEC](UX_SPEC.md) , [IN_GAME_UX](IN_GAME_UX.md) , [LOGIC](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/LOGIC.md) B01– B03/B10/B15/B16 and [EXPANSIONS](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/EXPANSIONS.md) L01– L03/K01 – K03/F01/X03. Inputs/ wersje/ruch: [T20](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T20-CHARACTERS.md) , BASE p. 19 – 21/24 – 25/29 – 32/36 – 41 ; [T25](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T25-LORDS-CHARACTERS.md) , LORDS s.6/16 – 29 ; [T26](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T26-COUNCIL.md) , LORDS p. 30–31 and FAQ 1.2 p. 3 Kings: [T28](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T28-SOVEREIGNS.md) and [T29](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T29-RULER-CHIEFTAINS.md) Fate and the submissions: [T32](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T32-FATE-SETUP.md) , [T33](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T33-COMPOSITION.md) Full content cards come from existing verified directories.

<a id="characters-i-podgląd"></a>
## Characters and Preview

The folder is at the left edge next to Politics. The list and interactive preview opens either the Enter/Space click; the hover itself or the focus does not open them. The cursor leaves does not close the open panel. The header allows you to move the window, the position remains in the session, and Close/Esc closes it without changing the legal draft. The opening does not change the camera, cost, draft or the game.

From UI-80 hover after 200 ms or focus immediately shows only passive description of the object and the full legally known ability. Own temporarily unavailable controls can explain the reason; other controls still do not exist. Description does not download data or capture gestures. Departing, losing focus and starting gestures will extinguish it; Esc extinguish the description and still close the active window. [BOARD_INTERACTION_UX](BOARD_INTERACTION_UX.md).

The list includes your own name figure outside Fellowship and you can enter in this configuration. Sections: On board, Not in play, rolled Removed. Absence on the map does not mean death. Removing the version does not block the exception of return or transformation; Gandalf White remains to be found in Not in play after Grey's death, with conditions from the server sources.

Entry: replacement portrait, name, current version and region/garrison Or entry conditions. figure has one entry, also when changing the version. Future live version figure is in her eye, not as her second portrait. Fellowship Gollum and Sméagol retain the right views Fellowship Hunt, the awakening of sovereigns is figure also in Characters; other states and procedures belong to the bar UI-59 Both entrances open the same preview.

card distributes level, movement, Leadership, nation icon, capabilities, limits used and conditions/cost input or change. The level to move does not replace level to Hunt/bitwy. Location openly center map to figure; no region gives no fictional target. List changes retain fokus/choice.

We do not show anyone else's controls. Public hostile characters can still be viewed on the map, and the legitimate enemy target action is interactive. Hover of each site only gently highlight the region. Information about the empty or busy region opens click or Enter/Space, with priority of the existing legal action.

<a id="gesty-i-komendy-postaci"></a>
<a id="gesty-and-command-characters"></a>
## Gestures and command characters

The specific die or the already paid sequence determines the context. The legitimate portrait click selects the journey; without legal context opens the information. With selected units of the military, the portrait retains the role of a participant in this army. We do not replace the selected dice or the active ability with a simple move. The list and map edit the same draft.

The next clicks of the highlighted regions create a route; reclick the selected end or its two-click ends the selection. The map shows no arrows or hanging button ✓ Flight distinguishes legitimate targets without artificial path. Complete drop sends the same step. Change of source only replaces the unsent group. Budget and already moved figure come from ActionSequenceView;  ✓ with dice Ends the sequence.

Enter, transformation and ability are in the right form. They show the exact cost, objectives and known effects, including the additional term dice at Recover. Final complete click/drop takes a step; aggregate recruitment/cost saves one ✓ Ruler token, Council, Ring and ability remain separate costs. The reaction does not get additional dice.

battle, Hunt and Fellowship remain the hosts of their decisions. Characters can lead to them through a link, but does not create a second loss form, Guide or reaction. This also applies to Hobbits' return and refusals.

<a id="przygotowanie-lords-i-council"></a>
## Preparation of Lords and Council

ChooseCompanionVersions It only opens FP, once for the new ID one panel shows a row of portraits, two versions of the indicated character and a separate selection Guide Comparison includes statistics and differences of ability from the catalogue; full card are in the preview. Selected versions are marked on portraits; one ✓ approve the whole.

The options specify the required choices and the base values of the optional versions; we do not automatically select Gandalf or Guide. Lords without Council only shows the right choices, the database does not receive an unnecessary wizard. Fate uses the correct versions of cards and configuration of the server. Automatic changes of Fate/Kings do not become additional player decisions.

Shadow sees the expectation and only allowed public context, without version, selections, private terms, number of candidates or progress. Guide reveals itself according to existing rules. Comparison of cards does not execute commands or log in private draft on the other side.

ChooseCouncilHomelands occurs separately in the first Fellowship Phase, after corresponding earlier steps of the server. Portrait → legal region or drop changes local location; Fellowship withdraws allocation figure The set can be improved, one ✓ saves the whole, also an empty list. The panel also opens without authorized figures, according to the protection of the version.

Preview shows effect: zero figures → no token; one → separate choice SH; two or more → both tokens. The SH selection goes to the UI-58 resource panel. We do not move the homelands before the first Recover or connect these decisions into one command. Once approved, the disclosure, policy and further phase according to the server, without the Unity's self-predictation.

<a id="menu-i-zachowanie-przy-wyjściu"></a>
## Menu and output behavior

The menu in the top right corner has Resume, Settings, Help and Games. Resume closes the menu; click outside the panel also closes them without passing a click on the map. We do not pause the batch or notifications. dice, mechanics, Characters, Log and the basic map tools remain directly available.

Settings uses existing preferences: 100/125%, Reduced motion, Reset window positions. Changes work immediately, do not delete draft. Scale/movement are saved locally; window positions remain only in session. Help opens existing UI-61 help, including read-only configuration.

Esc first supports the current gesture, active window or local selection; only unsupported pressing opens the Menu. One pressing does not close the window and does not open the menu simultaneously. Closes the focus.

Games leads to an existing list. Before leaving board we finish the local record of draft and the command logs. We check both the end of the operation and the saved error: LocalMatchPersistence can report Error without throwing an exception with Flushed. At the error we remain on board, keep draft and offer a re-entry; we do not show successful recording. We do not block the inspection or create another type of notification.

Uncertain upload retains commandId/payload in the log. Output does not cancel the accepted action nor does it create a new command; return recovers the receipt before reexecution. Save draft does not approve it in the game. The completed batch still allows you to use Menu and read the allowed information.

<a id="dane-i-granice-integracji"></a>
## Data and integration limits

UI-65 adds its own character list to secure projection: stable figure, version, life cycle, future versions and conditions based on the UI-56 model and the same planners as validation. No current option in someone else turn does not mean an unfulfilled entry condition. No new fields in older view give no false information about death or legal action. New DTO does not require a new command or a full MatchState at the client. The reading remains clean, without changing rewizji/RNG. CONTRACTS/ARCHITECTURE will describe the actual contract with the implementation, not the apparent ready API.

UI-71 binds new views to UI-62: the red tab opens a full historical summary, the gold of its own decision. History does not override draft. No third type, additional sender and double decision hosts. Reconnect/two devices retain cost identity, range of gesture and secrecy.

<a id="macierz-cm01cm18"></a>
## Matrix CM01–CM18

Each row requires real testing of the owner. UI-72 takes the whole.

| ID | Screenplay and required result | Owner |
| --- | --- | --- |
| CM01 | Custom list and availability: both views, old records, no Council leak, read without mutacji/RNG. | UI-65 |
| CM02 | figure and version: before entering, transforming, death and exception of return; no false elimination in absence. | UI-65 |
| CM03 | Characters: own name figure, sections and conditions, without Nazgûla/Fellowship, one ID after transformation and consistent sovereigns. | UI-66 |
| CM04 | Hover/fokus only passive description, click/Enter/Space opens panel, public Location; stable choice, no auto-camera, other people's controls and region panel on the hover. | UI-66, UI-80 |
| CM05 | List ↔ map: travel, participation in the army, several groups, flight, siege and replacing only the unsent source. | UI-67 |
| CM06 | Entry/change, specific die, full costs and additional die only in the appropriate Recovery. | UI-67 |
| CM07 | Capability, Ruler/Council/Pier a wall and a limitation; no automatic ability or cost, one host reaction. | UI-67 |
| CM08 | One panel wersji/przewodnika: Lords without Council, Council, Fate and no unnecessary choice on the base. | UI-68 |
| CM09 | Privacy version/draft/Progress, open Guide and one approval; restart before/after selection. | UI-68 |
| CM10 | Home 0/1/2/many figures: click/drop/Fellowship, change and empty set, correct Fate targets. | UI-69 |
| CM11 | Separate moments of setup, window without candidates, disclosure/politics, choice of one token or award of both. | UI-69 |
| CM12 | Resume/Settings/Help/Games, no pause, no hierarchy Esc/fokusu and no pass of a click to the map. | UI-70 |
| CM13 | Scale 100/125%, Reduced motion and reset windows during draft; permanent preferences, positions only in session. | UI-70 |
| CM14 | Games: Completed recording and saved Error, Retry, delayed upload, same journal/commandId upon return. | UI-70 |
| CM15 | Red history and gold decision open the right context without double form and losing draft. | UI-71 |
| CM16 | Restart/timeout/two device in input, versions, homelands and reactions: one cost/RNG, proper receipt and secrecy. | UI-71 |
| CM17 | Common Preview/Abbreviation with Fellowship, Hunt, Fight, Sovereigns and Help; new decisions during Menu and final inspection. | UI-71 |
| CM18 | Four layouts, both roles and configurations, click/drop without scroll decision, feedback ≤100 ms, 60 FPS target, build and dragons. | UI-72 |

<a id="ui-65--dane-cm01cm02"></a>
## UI-65 — CM01/CM02 data

Implementation descriptions and results below UI-65 – UI-72 come from 2026-10-08 UTC before UI-80 . The records of the former hiver and pinning document those versions; the current opening standards are defined by the section “Characters and preview” above. Historical trials are not counted as acceptance new gestures UI-80.

UI-65 provides real data CM01/CM02 And then they came UI-66 uses them without a local planner. Own candidatures also exist in someone else’s turn but WindowOpen/CanAct They're false. Gandalf/Strider may have their own entry InFellowship where it contains a future transformation; UI-66 does not need to repeat the composition by this Fellowship The usual Nazgûle, Gollum and Sméagol do not go to the Characters. Awakened sovereigns and their elimination retain them figure ; the early and corrupt remain in UI-59 . Future version of the living figure is a description in the same entry. UI-66/UI-67 They still have their own acceptance interactions.

<a id="ui-66--wspólna-inspekcja-cm03cm04"></a>
## UI-66 — Joint inspection CM03/CM04

Characters group their own figure On board / Not in play / rolled up Removed. The replacement portrait, short name and region facilitates finding the entry; the inspector shows the full title, edit and separate statistics. The entry is possible in the same figure entry; Grey in Fellowship does not create a separate, competitive entry of the current lineup. After Grey's death, candidacy White remains in Not in play. Legality and conditions returns the server.

Hover/fokus retains the camera and draft. A clear click attaches the information; the hiver of another awakened sovereign does not replace the attached character. Awakened opens the same inspector as the map and list. Public hostile figure have information and Location, without own entry and other action. Full capacity texts are available after invading or seal on the name, without technical identifier and without expanding the permanent panel.

Accurate results, corrections and border acceptance describes UI-66; action and the version/Guide elections still have their own tasks UI67/UI68.

<a id="integracja-cm08cm09--ui-68"></a>
### Integration of CM08/CM09 — UI-68

Panel `companion-setup-panel` takes over the existing tray for his own CouncilVersions . `setup-portrait-*` serves only the comparison; `version-*` selects the version, `starting-guide-*` Guide and existing `confirm-decision` approves the set. Only two currently compared variants are controls. Full capacity texts are a resource UI-66 on a hower or seal. The required choices do not have the default answer. The state Close is remembered for ID decisions in session and himself draft is permanently recorded by the existing mechanism. Shadow has only public expectations. acceptance In UI-68 save 3 new network trials, 7 regression, five Editor, required 29 domain cases and four PNG The boundaries of builds and hand tests remain public in the task.

<a id="integracja-cm05cm07--ui-67"></a>
### Integration of CM05-CM07 — UI-67

With the selected legal dice or the paid sequence, the portrait selects figure for the same trip as the map. With the selected combat units, it retains the role of an Army participant. Without legal context, click on the clip; the hiver and the explicit Pin remain available unchanged draft. Available figure and the already marked portraits have separate designations.

Enter/change and abilities are open buttons on the viewed obverse. Illegal cost does not automatically become different dice Active action carries the same tray to card characters, without re-election of abilities. Collective recruitment retains one ✓; the target or last cost shall be completed by the remaining complete action. Another viewed character does not take over the started action. Close preserves draft, and the golden decision or Resume goes back to the right one figure and its intended version.

Ruler/Council and its special abilities require your own open choice. White Rider and other reactions have a link to the current host, not a new form in Characters. Cases, manifesto and actual state of the check are described by UI-67: 37 methods Play, 10 cases Editor and the domain filter 168 cases have passed. Full control geometry is also checked with 1366×768/125%, without reducing fonts or the decision scrolling requirements.

<a id="menu-przy-planszy--ui-70"></a>
## Menu at board — UI-70

The menu in the top right corner provides four items. The click outside the panel closes it when you release the mouse and does not run the object under the bottom. Esc performs one operation, also when the key remains pressed. Closes the inspection does not reverse draft. Settings and Help return to the visible focus source. The menu remains offline and after the end of the batch.

Games wait for local record and check out the public errors of both logs and draft. Error leaves board with Retry. Resume/Settings/Help can stop waiting for the exit of board; completion of late write does not then move to Games. Uncertain command returns by the previous commandId and receipt, without repeating the cost or automatic approval of the unfinished draft.

CM12 – CM14 have the final 20/20 Play, 15/15 appropriate Editor in full 516/516 and viewing four Menu layouts. Cases include error without exception, lost receipt, delayed recording, live update, offline, sketches of all required families and the same focus on Settings. Accurate filters, previous failures and limits: UI-70.

<a id="ui-69--interakcje-cm10cm11"></a>
## UI-69 — interactions CM10/CM11

The Homelands tab opens a separate private panel mode in the first Fellowship Phase, also in the absence of candidates. The portrait selects figure; click the legal region or drop edits only its allocation. Fellowship undos the allocation of the selected figure. The panel shows the current version, the nation icon and the token announcement: no / selection of one by SH / both.

One ✓ approves the entire set, also empty. Shadow until then, he doesn't see any candidates, draft or a special empty form. Gimlie Fate's homeland comes from the server also without New Cities; the lack of an icon does not activate. After one departure, the token selection belongs to a separate resource panel Shadow; after two/five, both are automatically obtained. Close/Esc keep draft and camera; new ID opens the next context. Actual tests and limits acceptance save UI-69.

acceptance CM10/CM11: 19 target Play, two full parts, 14 Editor and eight watched PASS systems; results, previous failures and range of 88 unchanged domain tests describes UI-69.

<a id="integracja-cm15cm17--ui-71"></a>
### Integration of CM15-CM17 — UI-71

The change entry uses fixed Before/After obverses, even when the current figure has a different version. The own setup selection and public Guide have separate visible facts and an anchor Fellowship. The old story does not overwrite the current draft from the input, dice or the target version; the closed host returns through the gold shortcut, and the same ID after the reconnect does not reopen itself.

The White Rider's refusal to move back to battle, and the rejection of Gollum's proposal does not lose the Hobbits' common return to the map. The new nested decisions do not close Menu/Settings, do not move the camera and do not become the red move of the opponent. The entrance from the Council retains a clear token, accurate die and the final region. Games/restart first recovers the receipt, and does not perform the restored draft. Another client cancels the old capture; the entry from the Council does not re-enter or cost. Results and limits: UI-71.

acceptance UI-71 : 33/33 the required Play methods, 13/13 Full Editor cases 542/542 , 7/7 PNG and 399/399 unchanged domain cases in full 3726/3726 PASS . Separate fresh HTTPS/WSS dragons are credited with trusted CA ; Unity tests benefit from HTTP/WS/PostgreSQL. Accurate filters, reusing identical methods and limits: UI-71 . .

<a id="integracja-i-stabilność--ui-72"></a>
## Integration and stability — UI-72

New network trials have all passed 56 views: full of own rosters and abilities, Lords/Council/Fate with the choice of the Guide, the Motherland and Menu over the actual battle And Hunt. Inspection does not send command ; both projections remain identical and saved draft strat/reroll This keeps accurate ID Expecting Shadow does not receive a private preparation form.

The character preview responds to a significant change in size of existing `PopoverPlacement.SizeChanged`. This removes the confirmed loop of the subpixel height of Denethor at 1920×1080/125%, without changing the content, manual position or rules. Full driver 40 variants have been corrected. The matrix CM01–CM18, previous failures, measurements and limits of acceptance are in UI-72; this entry does not replace acceptance aggregate.

Final acceptance : [matrix CM01–CM18](profiles/ui72-validation.json) , 41/41 required Play methods and separate driver 40 sequence PASS . [Player's actual profile](profiles/ui72-macos-board.json) He's gone all the way. 40 measurements: minimum 104,322 FPS maximum 24,1485 ms to end rendering. 56 network staff and 40 Player. Buildy macOS/Windows and macOS dragons passed; Windows runtime, Intel, physical devices and manual acceptance they are deferred.

<a id="karta-postaci-w-symbolach--ui-139"></a>
## card characters in symbols — UI-139

User decision with 2026-10-14: card characters are to fit the rest of the interface, contain a minimum text and provide information with symbols, concisely and clearly. Data come exclusively from public projection (current figure) and the reviewed directory (another version); the client does not calculate anything.

- List line: portrait with level star in the corner, name, state symbol (pine on board, ring in
  Fellowship, hourglass out of the game, broken sword removed, rising sun for a possible return)
  with region or short state. Tooltip line gives full state, statistics in words and entry
  conditions.
- card (inspector) is a window in the frame `frames/panel` with a header plate: version name and tag
  (Base, Lords, Fate...), state line with symbol and Location, pass symbol of statistics: level ★,
  movement (but) or flight (wing), ∞), level of combat (crossed swords), Leadership (the banner),
  the emblem of the nation (to Gandalf: the emblem of all Free Peoples Each symbol has tooltip;
  tooltip. pass reads the whole thing with words (“Level 1  · Move  2  · Combat level  1  ·
  Leadership  1 · Nation Dwarves). The level of movement and combat remain separate from the level
  (Fate of Erebor).
- The capabilities are tokens with scroll and title; the full text is in the tooltip, and click the
  token develops it under chips on parchment, the second click scroll. By default nothing is
  developed. Three chips per page, as in UI-66.
- Limitations action is a symbol ‘why’ with the word ‘Limits’ and tooltip; the entry cost becomes
  the server text, an additional die as ‘+n action die at Recovery’. Version buttons become labels
  (choice).
- action unchanged: entry with two-click line (UI-108), capabilities and costs in the inspector.

Graphics: `graphicalassets/ui/src/characters` (16 pt). Code: `Runtime/Presentation/CharacterCardFacts.cs` (symbols and state), `Runtime/UI/Characters/CharacterFactStrip.cs`, `CharacterAbilityChips.cs`, style `Resources/Wotr/Styles/Characters.uss`. The element with children is not measured by its own text (Yoga), so the limits ability token and line have a symbol and label as separate children.

<a id="krótkie-opisy-umiejętności-w-tooltipach--ui-187-2026-10-15"></a>
## Short skill descriptions in tooltips — UI-187 (2026-10-15)

User application from 2026-10-15: "Let's add descriptions in the tooltip of what skills they have (e.g. members of the ring team). The descriptions are to be short and compact, without repeating information and fit stylistically to the rest".

- The 22 skill shortcuts 14 cards base game are in `Resources/character-ability-summaries.json`,
  according to identifier skill. The common skill (e.g. "Captain of the West") has one shortcut.
  Each shortcut summarizes the printed text with `character-ability-text.json` and does not add
  rules. It has at most 72 characters, without a dot, one line.
- `CharacterAbilitySummaries` consists of tooltip: first lines that have already been (name, level,
  leadership, place), then one line per skill, “title · shortcut”. Example:

  ```
  Gimli · Level 2 · Leadership 1
  Captain of the West · +1 Combat Strength in battle (max 5 dice)
  Dwarf of Erebor · Any die in Erebor moves the Dwarves a step toward war
  ```

The abbreviation does not repeat the title (e.g. Gollum: ‘Guide · Joins the Fellowship when no Companions remain’).
- Where:
  - Characters tab rows (card, which the line shows);
  - portraits of the characters in the region panel (after hints of selection);
  - Fellowship portraits. There, skill lines replace the former description of the skill itself
    Guide, so nothing repeats. card without shortcuts (expansions) retains the description of Guide.
- The Army Tooltip on the map stays with the characters names; the skills are in the region panel,
  one click away. Full text stays on the character card (UI-139).

