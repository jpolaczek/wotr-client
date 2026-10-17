<a id="ui-09--decyzje-i-zasoby-dodatków"></a>
# UI-09 — decisions and resources expansions

<a id="zakres-i-stan"></a>
## Scope and state

Work started with 2026-10-06. The status of the task is led by `tasks.json`. The source of the election is the server projection with T33; the client does not calculate the legality of the rules nor does he discover identifier hidden tiles. acceptance T33 describes [93 matrix](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T33-COMPOSITION.md).

<a id="przepływ"></a>
## Flow

- Council: seven pairs of versions in a compact grid, separate open-label selection of Guide;
  mandatory Gandalf without automatic decision. Country: portrait, allowed region on the map or
  button, preview of all disconnects and one commit. Leaving the Fellowship is a visible
  alternative. Data remains private.
- Keeper/Lesser/Ruler: common tiles dice with designation of origin, result symbol and deletion.
  Selection preserved/odrzucanej dice shows effect before approval. Hunt shows origin of the eye.
- Tokens: a clear choice of token, then legal action and its components. Token Ruler remains a
  component of the same command as die. The Council and Dismay have separate designations, state of
  use and legal targets.
- Sovereigns: portraits with state and region, tiles exactly according to the projection; covered
  tile has no value nor physical ID in tooltip. Sequential trial has the Draw/Stop election without
  automatic risk selection.
- Special abilities use common character selectors, regions, routes and figures. Fate versions and
  the absence of Daina/Branda are due to projections.
- The match configuration is read-only after start-up. Opening the expansions configuration in the
  lobby belongs to T34 after acceptance UI-09.

<a id="podział-kodu"></a>
## Code breakdown

The local logic of draft and its recovery goes to `Presentation`, the components of expansions to `UI/Expansions`, resources for a separate pool view. `DecisionTray` and `TurnInteraction` coordinate flow. The change of the selection refreshes existing controls without re-createing the entire tray. Clicking is sufficient for each operation; there are no required numerical fields or scrolling the decision panel.

<a id="plan-sprawdzenia"></a>
## Verification plan

1. EditMode: sketches, token cost + die, private data, playback after reconnect, outdated options
   and character directory.
2. PlayMode on real HTTP/PostgreSQL: new decision types, both perspectives, Council, dice, Dismay,
   Ruler cost and sequence Draw/Stop.
3. Layout with 1366×768 and 1920×1080, scale 100%/125%: no cut of controls and required scroll. View
   of the screens representative discharges.
4. Regression .NET and validators T33; `tasks.py validate`, `git diff --check`.

## Checkpoint 2026-10-06, 09:38 UTC

The Council, sovereigns, special dice and resource pool components have been implemented, the Ruler cost sketches and the integration of Draw/Stop. The character catalog includes Lords and Kings, including situation-dependent statistics (`null`). The current code requires also acceptance by network scenarios and layout checking; this checkpoint is not a complete UI-09.

- `scripts/test.sh ... --filter 'FullyQualifiedName~ExpansionCompositionTests|FullyQualifiedName~SovereignAwakening|FullyQualifiedName~RulerRecruit'`:
  **104/104 PASS**, `.artifacts/test-results/ui09-domain.trx`.
- First EditMode: **156/158**, two read failures `leadership: null` after the folder extension.
  Fixed the read type, without changing the rules.
- The second EditMode (`scripts/unity.sh -runTests -testPlatform EditMode`): **171/171 PASS**, 0
  skip, `.artifacts/ui09-editor-second.xml`, including new draft tests and presentation privacy.
- The new domain test was initially not compiled by the wrong name of the `StateCodec.Serialize`
  method; it was improved to existing `Write`.
- Next start of new tests: 5/6; test mistreated Westemnet as a region without settlement. Source
  `base-board.json` has Town there. The script changed to Eastemnet. Re-release: **6/6 PASS**, 0
  skip, `.artifacts/test-results/ui09-projection-final.trx`.
- `python3 scripts/tasks.py validate`: **88 PASS**; `git diff --check`: **PASS**.

Further results will be added after the actual startup.

<a id="scenariusze-sieciowe-i-korekty-układu"></a>
## Network scenarios and system adjustments

`ui09-*` scripts use the real commands HTTP, WebSocket and PostgreSQL. Each approved decision increases the revision exactly once; the reconconnect plays the projection and the legal draft before approval. The matrix exports 186 separately from the projection (93 configurations × 2) from the same engine and providers as backend.

- `ExpansionDecisionNetworkTests` : first start **0/7**, second **3/7**. Vertical containers
  were detected without open `flex-direction` , cut off two-line description dice and round the
  button below 32 px at 125% . Improved styles and place of par Council, not weakened assertion.
  Third launch: **7/7 PASS**, 0 a skip, `.artifacts/ui09-decisions-third.xml` ( 09:46 UTC Includes
  private versions and starting places. Council/Fate , token Council, both special elections dice
  And the Ruler, Dismay, Narya, Vilya, awakening and removal of the sovereign.
- `ExpansionTokenNetworkTests;ExpansionCorruptionNetworkTests`: first launch **5/13 PASS**, 8 FAIL,
  `.artifacts/ui09-tokens-first.xml`. Four variants of corruption and political token Council
  passed, along with the privacy of the tiles and the reconnect. Others revealed the overflow of the
  recruitment/dr tray and the premature waiting of `Spent` after Denethor's token. Its mandatory
  additional selection (KINGS p. 10, T29 contract) requires a separate decision; the test was
  supplemented with the selection of the deck, the reconnect and the end of the cost.
- After selecting the tray character, it focuses on the selected action and cost; `Change` restores
  the list. Optional meals and all targets remain public. Recruitment does not repeat unnecessary
  description of the region, and the Far-sighted and group mode uses a common line. The choice of
  the leader entry region remains the player's decision, also when the test scenario provides for a
  specific region.

The subsequent launch includes tokens, corruption, matrix and regression of travel Fellowship and characters; its result will be saved after completion.

Second start: **19/20 PASS**, 0 a skip, `.artifacts/ui09-tokens-second.xml` ( 09:59–10:00 UTC All **744 System(s)** 93 configuration × 2 pages × 2 resolution × 2 scales have passed the control of the lack scrolla/dic . All costs except one Thranduil (footing o 1,6 px per screen); a repeated summary was shortened. 4 the scenarios of the forms and 2 travel scenarios/Hunt.

Next set: **17/17 PASS**, 0 skip, `.artifacts/ui09-resources.xml` (10:03–10:04 UTC): 7 kombek, 9 tokens and a test test full of resources. The screening of the dumps showed the application of the Shadow pool despite the correct external borders. This result does not constitute acceptance pool: the paired anti-impossement, the rows were forced to lack shrink, the tokens' hitboxes were increased, and the excess interval removed. Fixture also includes Narya recovered after Fellowship move, five sovereigns and five tokens.

The unchanged resource projection preserves control objects and focus, which checks additional assertion; cache compares only a small, public set of presentation fields, without full party status. It does not buffer the legality of the commands on the server side. The outdated draft Council disappears if its specific token has lost its legality instead of using the goals of another token.

Extended Renew: **22/22 PASS**, 0 skip, `.artifacts/ui09-layout-final.xml` (10:08–10:10 UTC). The Matrix has again passed 744 layouts; full pool (also Narya and five Dismay) has passed border check and button-applicability. Past decisions, nine token scenarios and regression of the battle/politics cards circuit. The smallest full pool of SH and Ruler recruitments are available.

The cost preview is precise: die in the pool still shows the actual result, and the tray after selecting the token shows conversion, e.g. `C → M`. Re-clicking on the already selected ability does not delete the planned meals. The recruitment test checks both behaviors before single approval. Full regression is underway .NET and final Unity checks.

Final EditMode: **172/172 PASS**, 0 skip, `.artifacts/ui09-editor-final.xml` (10:12 UTC), after specifying the conversion of the cost and the extinction of the outdated Council. The complete regression of the server and the client remains in progress at the time of this checkpoint.

The interaction review has also detected that the token Dismay was available in the tray, but remained inactive in its own pool. The legality of the pool button and drag shortcut with the server option UseDismay has been unified. The token selection does not select any dice Ruler as player; both costs remain distinguished. The network script starts now by clicking the token in the pool, and the new EditMode test checks the same path by drag.

Full `scripts/test.sh --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger trx;LogFileName=ui09-full.trx --results-directory .artifacts/test-results`: **3440/3440 PASS**, 0 skip, 3 min 26 p. `expansion_acceptance.py --results` for this TRX: **1107 methods / 2560 cases PASS**, eight source compositions, event/form manifests and integration matrix.

EditMode after completing Dismay gesture: **173/173 PASS**, 0 skip, `.artifacts/ui09-editor-dismay.xml` (10:15 UTC). Full `scripts/test-client.sh` without filter was launched, with real HTTP/PostgreSQL; its score remains to be saved after completion.

Full PlayMode : **236/237 PASS**, 0 a skip, 10:17–10:28 UTC , `.artifacts/ui09-client-full.xml` All new scenarios expansions The only failure: `TemporaryExcessLocksArrivalsAndMandatoryRemovalUsesIndividualFigures` in the existing screen of the second Army movement at 125% ; the footer crossed the limit by a fraction of the pixel after increasing the buttons to 33 px. Only upper interval of army planning trays was reduced by 2 px. No buttons size or Assertion changed. Re-incorporation includes the entire class of interaction board new decisions/tokens and regression of travel/figure.

The final review also brought together the conversion of the Ruler result in the summary of all action (including Denethor events) and the short names of both versions of Mouth/Witch-king so that two figures do not have the label The.

<a id="odbiór-ui-09"></a>
## acceptance UI-09

Final purposeed re-up `scripts/test-client.sh -testFilter 'BoardInteractionTests;ExpansionTokenNetworkTests;ExpansionDecisionNetworkTests;FellowshipLayoutTests;CharacterNetworkTests'`: **28/28 PASS**, 0 skip, 10:31–10:32 UTC, `.artifacts/ui09-regression-fix.xml`. Includes a repaired class board, conversion Ruler, recruitment plan behavior, click Dismay from the pool, new decisions and travel/form. This is a full regression **236/237 with a purposeed repair recovery 28/28**, not a single report 237/237.

The S17–S19 implementation was closed: new decisions use a real basend, one step-by-step approval, the projection of the right page and the reconnect. 744 matrix layouts and a difficult resource pool without scrolling, cutting or pressing buttons. The viewing of the dumps resulted in an improvement in the problem that the first border placement did not detect. `tasks.py validate` and `git diff --check` included at the closing.

Main changes: `Presentation` (drafts, costs, form directory), `UI/Expansions`, common views characters/podry and stylesheet; server options Travel/Settlements in Contracts/Domain; scripts and regression in test projects. Overview of the structure and cost of refresh is in CODE_QUALITY. Statuses are not maintained in this report in parallel with task registry.

The next task of T34 receives the full list of expansions windows, difficult configurations and normal start with configuration. Lobby still has gate with the database; UI-09 does not open it before this acceptance. New Windows/macOS builds, FPS measurements and hand-made user tests were not made in this work; they remain in the release and availability stage.
