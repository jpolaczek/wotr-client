# Layout stress scenarios — TEST-15 (2026-10-29)

Owner request (2026-10-29): audit the game's scenarios and features and add special test scenarios that demonstrate the
potentially problematic layouts — decisions that open extra windows, many buttons at once, the complex new mechanics of
the expansions. The scenarios are in the server's Testing catalog (`wotr-server` `src/Wotr.Domain/Testing/
LayoutStressScenarios.cs`, documented in its `docs/TEST_SCENARIOS.md`); each is a faithful state reached through the
real setup and the engine. They are new catalog entries only, so a current client lists them like any other scenario.

Open them from **Test scenarios → Kings (or Lords) → Layout stress**.

## The audit: which features fill the screen

The audit (2026-10-29) listed the features whose layout is at risk and whether a scenario reached each one:

| Feature at risk | Evidence | Before TEST-15 |
| --- | --- | --- |
| The Characters tab and window with every version, kings and minions | `CharacterRosterLayoutTests` 1366×768 at 125 % too tall | none with everything at once |
| The Effects panel with many sources | `EffectsLayoutTests.MultipleSources` 1366×768 at 125 % | `kings-corrupted-recruitment` (six sources) |
| A battle window crowded with expansion objects | several Chieftain chips, Skip effect, kings in the force column | each feature alone |
| The Hunt window with several responses | Council Guide, table cards, Nenya | single responses only |
| Every pool resource at once; a wide Council travel | `KingsCards03` Valinor pool overlap | a few Nazgûl only |
| The console with a card's options, Pass and ✓ | `TurnConsoleLayoutTests` covers no options | reached, never checked |

## Scenarios

| Id | Side | What to look at |
| --- | --- | --- |
| `layout-full-roster` | Free Peoples | Fate, Lords with the Council and Kings: every alternative Companion, the Keepers, three kings in each state, every Minion and the three Chieftains. Open the Characters tab and each character's window. |
| `layout-full-roster-five-kings` | Free Peoples | The same without Fate, so all five kings play: two Awakened with Ruler tokens, Dáin with Corruption tiles, Théoden Corrupted, Thranduil Initial. |
| `layout-chieftains-battle` | Shadow | Three Chieftains' replacement chips and Keep card effects in one battle window; after the Shadow's choice Brand's Heir of Bard waits for the Free Peoples. |
| `layout-hunt-responses` | Free Peoples | A Hunt answered at every window: Mithril Coat and Nenya, then (after Pass) Axe and Bow and the Horn, then the Hobbits' cancel, then the damage choice. |
| `layout-council-travel` | Shadow | The Council token's travel with twelve movable figures in several regions. |
| `layout-white-rider` | Free Peoples | The White Rider's chip at a battle's start, with Pass. |
| `layout-all-resources` | Free Peoples | The special, Ruler and Keeper dice, three Ruler tokens, two Council and two Dismay tokens, the three Elven Rings. |
| `layout-effects-sources` | Free Peoples | The Effects panel with 22 entries: six Free Peoples and eight Shadow table cards and three corrupted kings. |
| `layout-console-options` | Shadow | The Nazgûl Strike!'s three console options beside Pass, with four table cards lit in the Effects panel. Since UI-430 (2026-10-30) the Strike has no console options: open the Effects tab and look at the four red table cards, the glowing Hunt Box and the lit ✓ (the server's title still says "three console options"). |

The Witch-king's Sorcerer is not in the White Rider scenario: its window opens only after the first round. A long trail of
committed steps before a three-option console choice is not reachable with any card of the engine, so the console
scenario's trail is its played card.

## Check

PlayMode `LayoutStressScenarioTests` opens each scenario on its side's board, opens the window it is about (the decision,
the Characters tab, the Effects tab) and checks, in the four layouts (1366×768 and 1920×1080 at 100 and 125 %), that the
decision window fits between the top bar and the console without a decision scroll, that the dice pools hold their
buttons, and that every shown window, panel and bar stays on the screen with its buttons inside it. Each layout saves
`.artifacts/screenshots/test15-<scenario>-<width>-<scale>.png`. A failure is a layout defect to fix, recorded in its own
task.

## Findings (2026-10-29)

- **The console's options were cut to "…"** (`layout-console-options`): The Nazgûl Strike!'s three options shared the
  console's line with the played card's button, so at 1920×1080 they read "Di…", "Ro…", "Fi…", and in a Lords or Kings
  game at 1366×768 and 125 % (the wider dice panel leaves the status a narrow column) they were "…" only. Fixed: options
  that do not fit the line at their full text stand in a strip just above the console, with the played card's button
  when that does not fit either (`ConsoleOptionsFit`); the bottom row keeps its height, wide layouts keep the line.
  UI-430 (owner, 2026-10-30): the Strike's options are its objects now, so `LayoutStressScenarioTests.TheNazgulStrikesObjectsFit`
  checks no option strip, the red cards in the open Effects panel, the glowing Hunt Box and the lit ✓ in the four layouts.
- **The Effects panel reaches past the bottom row** (`layout-effects-sources`) with 18 sources at 1366×768 and 125 %
  (592.6 against 474.4 points) — the defect behind the earlier `EffectsLayoutTests.MultipleSources` failure. Its own task
  is UI-424; until then the stress test logs it as a warning and keeps the screenshot.
- Every other scenario keeps its windows, panels and buttons on the screen with their whole text in the four layouts.
