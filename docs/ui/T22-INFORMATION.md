<a id="t22--informacje-o-bieżącej-partii"></a>
# T22 — information on the current batch

<a id="kontrakt-przed-implementacją"></a>
## Contract before implementation

The range is complemented by R15–R21 and U01/U03/U11, without changing the rules or server contracts. The gameplay data comes exclusively from `PlayerView`; the local directory serves names, symbols and geometry.

- Before the first projection, the client shows the `Loading match…`, an empty map without figures
  and the players' property and the status of the connection. Delayed or unsuccessful GET does not
  reveal the local setup. After reconnect, it retains the last real projection, marked as out of
  date.
- At the edge of the map, a compact bar of eight nations is available: short name, position for war
  and distinction aktywny/pasywny. Click shows full explanation; it does not select action
  political, dice or draft. The bar remains available when the opponent decides and reconnects.
- The visible `Inspect` button switches the map clicks to the viewing. A separate map event does not
  call `RegionSelected`, does not change the indicated target/draft and does not send command.
  `Resume planning` restores the usual selection. The viewing shows separately the field army and
  garrison of each page, the exact composition according to nation/type and the character names
  and their public parameters. Bookmarks switch only the viewed group; they do not select figures.
  pinned card and the camera remain unchanged.
- `History` opens the text only with `PlayerView.History`, already filtered through the server. It
  does not read the opponent's hand, full state or local log. History works in anticipation and
  reconnect. It can be scrolled as an optional preview; the decision tray does not get a scroll.

The policy details, region and history share one closed panel over the map. The constant remains only a small bar of nations and viewing buttons. The panel does not replace the tray or obscure the attached card. We refresh the data after projection, not every frame; the simple change of draft does not rebuild the information. The new controls retain at least 32 logical px also at 125%.

<a id="weryfikacja"></a>
## Verification

New PlayMode `T22InformationTests`: delayed and failed first response HTTP from the actual server; missing figures before the projection; politics and history on the real `PlayerView`; inspection of the opponent during the prepared action without changing cost/target; waiting/reconnect; private text of both projections. Separate matrix 1366×768 and 1920×1080 at 100/125% will cover full hand, dense composition and open information, with control of geometry/click and screenshots. Tests with synthetic composition only check the layout, not the rules.

Buildings, Unity and commit coordinate root. New checks have not been launched at the contract signing stage.

<a id="przekazanie-implementacji"></a>
## Implementation handoff

Added `BoardInformation`, `RegionInformation` and `NationStatusStrip`, integration with `BoardScreen`, local setup cleaning and separate map mode. Regional details preserve settlement, nation and current control, and bookmarks separate both sides and garrison. Root completed the `TurnInteraction.Drop` border to prevent the drag of the reserve during the inspection from preparing the recruitment.

Three new PlayMode tests include: controlled loss/delay of the first actual projection; public and private history entries of two clients, traffic plan, separate viewing mode, navigation click Fellowship, reserve drag and reconnect; full matrix of two resolutions and scales with eight nations, three armies/garnisone, the character names, six card and 50 history entries. The layout tests are explicitly synthetic; networkes use isolated `information-loading` and `information-readonly` seeds and the actual server.

Before the central start: `git diff --check` **PASS**, `python3 scripts/tasks.py validate` **PASS: 88 performance**. Units and new tests have not yet been launched; results need to be replenished after the coordinator runs. No build or commit has been performed.

<a id="wynik-integracji"></a>
## Integration result

Final full PlayMode: **172/172   PASS** including **3/3   `T22InformationTests`**, no skip; `.artifacts/t22-play-full.xml`, end 2026-10-05   22:20:03   UTC After the first attempts, the level direction of the controls, the margins of the character names, the space under navigation and the mutual closure were improved. information/report   battle The dense fixture contains a visible button `Last battle`; the opening of the report, transition to Inspection and chained behaviour was checked card. Personnel `t22-information-*` / `t22-history-*` include four settings; the performer looked at both at 1366 × 768/125%. History remains the only optionally scrolled preview in these attempts. [protocol](T22-ACCEPTANCE.md) preserves past failures and their causes.
