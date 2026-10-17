<a id="map-15--pokrycie-interakcji-na-konturach"></a>
# MAP-15 — Covering Contour Interaction

<a id="kontrakt-i-granice"></a>
## Contract and boundaries

The geometry of `contours-2` and the renderer MAP-14 are common to all families below. `MapCanvas.HitTest` converts the screen point by `CameraState.ToBoard` to the index `MapContourData`. `MapDraftOverlay` receives identifier from existing drafts/options of the server. `TurnInteraction.ChooseRegion` transfers ID to the proper controller; `DraftInput.Token` uses the same hit test for the drop on the site. No new authorization, recalculation of the legality from the neighbourhood of polygons or sending hidden status.

On the exact common edge wins the first `regionId` in the original order; the interior of holes and the ocean do not give the region. Tests of both sides of the edges include Old Forest/Buckland, Minas Tirith/Osgiliath, Dagorlad/Morannon and the bank of Dol Amroth. Direct samples on both sides are removed by one unit of geometry so that the floating point transformation of the screen does not fake a mathematically accurate point on the edge.

<a id="macierz-rodzin-z-interaction-inventory"></a>
## Family Matrix with interaction-inventory

The Matrix indicates specific attempts of the actual server and equivalent common paths. The results of all the specified attempts are recorded below. The new MAP-15 classes use their own `map15-*` matches; no fixture-only test is used as a substitute for command by HTTP/WSS/PostgreSQL.

| Family | Shared path / case | Test and equivalent proof |
|---|---|---|
| ObjectIntentCost | die → object → region; one specific cost | `MapContourInteractionTests.BothSidesClickAndDropOnContoursCommitTheSameArmyStep` and `MultiUseDiceNetworkTests.OneDieClickExposesArmiesRecruitmentAndPolitics` |
| MapUnits | army/figures → move or attack; last click/drop | New interaction class in both roles; `ArmyContextNetworkTests.FirstMarchPersistsAndMergedFiguresCannotMoveTwice` and `GarrisonCanSortieButHasNoMarchTargets` |
| PaidActionSteps | Common record of a single step and of the remaining budget | New Class of Interaction; `MovementNetworkTests.TwoMountedMatchClientsMoveRecruitAndRecoverWithoutPayingTwice` checks the receipt, second step, recruiting FP/SH and one fee |
| ReservePlacement | Region with recruitment options; `MusterContextInteraction.Target` | `MovementNetworkTests` (both roles, click/drop); `MusterNetworkTests.EliteDropSpendsBothRecruitmentPoints` |
| MapStayShelter | `BattleContextInteraction.Target`; Legal retreat/enter | `BattleNetworkTests.ContinuingRetreatingAndAdvancingUseOneTrayAndKeepTheCamera`; `StrongholdNetworkTests.ShelterRequiresFiveOnlyWhenTheAttackerActuallyEnters` |
| CardMapTargets | ID maps → `EffectChoiceDraft`/`EventRecruitDraft` | `LordsFinalCardsNetworkTests.MadnessHoverPreservesChoiceAndFinalMapClickCommits` and `MadnessLossesAndRecruitmentPassControlBetweenRealClients`; status of selection and two targets plus `CardInteractionTests.TwoMapChoicesKeepThePinnedCardAndCommittedStepsDuringAReaction` (the latter is a client test, not a server proof) |
| MapPortrait | Public anchor Fellowship, route declaration | `MapContourNavigationTests` (Locate and privacy of both roles); `FellowshipNetworkTests.DeclarationMovementSeparationAndIndependentGroupsUseTheServer`; `FellowshipRoutesNetworkTests.ZeroProgressDeclarationPreviewsHealingAndActivationAndRecoversReceipt` |
| MapRoute | `TravelDraft.Step` and the public completion of the route | `FellowshipNetworkTests` as above; `TravelGestureNetworkTests.FigureDropSavesOneGroupAndFinishKeepsTheExactPaidDie` and `FellowshipPortraitDropUsesTheSelectedCostAndKeepsTheGuideWindow` |
| PortraitDestination | `CouncilDraft.Region` and legal homeland; same token region | `ExpansionDecisionNetworkTests.CouncilVersionsHomelandsAndShadowTokenSurviveReconnect` |
| PortraitRoute | Flights/dr th groups on `TravelDraft` | `Shadow02NetworkTests.SearchUsesMapFlightThenTransfersTheRevealDecisionToFp` (flight Shadow); `FellowshipNetworkTests.DeclarationMovementSeparationAndIndependentGroupsUseTheServer` and `TravelGestureNetworkTests.FigureDropSavesOneGroupAndFinishKeepsTheExactPaidDie` (group FP); character type does not change the common `TravelDraft.Step` or `DraftInput.Token` |
| MapArmyGroupRoute | Marching events: another steps/Groups; `TravelDraft` and a public tour | `Strategy02NetworkTests.MarchUsesTwoMapStepsAndRetainsRouteOnReconnect`; `ShadowStrategy02NetworkTests.MovingCommitsFourArmiesWithOnePaidDie` |
| PortraitTargetCost | Region characters/In the the rulers by `CharacterDraft.Region` ; legal objectives and costs | `ExpansionAbilityNetworkTests.BrandChoosesTwoSettlementsAndHisLocalRecruit`, `DainChoosesAnEliteAndARecruitInAnotherSettlement`; `ExpansionDecisionNetworkTests.AwakeningSelectsAmongAllFiveSovereignsAndTracesARoute` |
| DieTokenTarget | die source/capacity → existing army or journey | `ExpansionCostNetworkTests.MouthConvertsTheSelectedMusterIntoTwoArmySteps`, `BalrogSymbolTravelsWithoutGrantingNazgulAnAction`; other capabilities with cost and indication army/routes use the same `ArmyDraft`/`TravelDraft`, and the allowed options still calculates the server |

Others 18 families do not choose the field goal. `CardOrDecline` , `HitsUnits` , `UnitsToReserve` and `ContinueStopElite` operate in a panel anchored at the region; placement and separate field/garrison check BoardInteraction , Battle/Stronghold and MapVisibility Configuration, PortraitVersions , TokenChoice , CardGrid , DiceTransfer , DiceComparison , TokenDie , RingDieFace , NationTrack , GuideRandomCorruption , PortraitChoice , TileCardOrDecline , EyePortraitTile and TileDrawStop choose objects/tor/reakcj , without the target region. Moving these data markers does not extend the visibility of projections or the list of legal action .

<a id="nowe-próby-mc09mc12"></a>
## New MC09-MC12 trials

- MC09 : two roles × four systems ( 1366 × 768/1920 × 1080 , 100/125% ); Hover empty regions opens
  nothing, click and focus keyboard opens the right region; hover army/Dru the rye does not change
  the condition or camera, the controls Fellowship only FP . pinned card it's open.
- MC10: actual `panel.Pick` in the outdoor area, without calling `Map.Select` or replacing the
  target with a button. Click/drop both sides send the same figure / cost/step; separate attacks in
  both roles. Mountain contact Minas Tirith/Lamedon, the ocean and remote region do not consume the
  resource.
- MC11: all 105 anchors at three zoos and master, concave Old Forest, small Osgiliath and the coast
  of Dol Amroth; both sides of the four edges and a hole Gorgoroth. Exit from the power panel
  retains the chosen army and its targets.
- MC12: intercepted drag discarded after reconnect on the same revision and after traffic approved
  by the second client of the same player. The new revision does not center the map; the explicit
  notice/log/Locate uses current anchors, the log remains a context only readable. The receiver
  includes the existing test MovementNetwork and FellowshipRoutes; it does not replace their mockup.

<a id="wyniki"></a>
## Results

- `.artifacts/map15-interaction-final.xml`: **5/5 PASS**, without omission, actual commandOn both
  sides, no command on the wrong/old arrow.
- `.artifacts/map15-navigation-final.xml`: **2/2 PASS**, without omission, 2026-10-08
  10:56:12–10:56:53Z. The first 1/2 score showed that pinned card can obscure the region after
  Focus. The final attempt is made by a clear right map dredge, retains card and checks the actual
  `panel.Pick` area.
- `.artifacts/ui54-fellowship.xml`: **10/10 PASS**; includes declaration, separation, group travel,
  receipt and all eight route targets at the attached card. Renderer and geometry are the same as in
  MAP-15.
- `.artifacts/ui54-cards.xml`: **18/18 PASS**; both Madness methods from the table, four
  CharacterNetwork and five FellowshipGuide. These are existing attempts of the actual server
  restarted by root on contours-2.
- `.artifacts/ui54-activity.xml`: **14/14 PASS**, including ActionContextNetwork, ActionMapLayout
  and both ActionNoticeNetwork. Reading/navigation are not command.
- `.artifacts/ui54-inspection.xml`: final **17/17 PASS**, including ContextHover **4/4**,
  RegionInspection **2/2**, FloatingWindow **4/4** and the indicated test of two goals. Previous
  16/17 with the error of the layout of full text cards was repeated after the amendment in UI-54;
  quoted XML is the final result, not the failure of the previous defeat.

- `.artifacts/ui59-checkpoint5.xml`: **22/22 PASS**; in particular `ExpansionDecisionNetworkTests`
  **7/7**, including Council/homelands/receipt and Awakening/route from matrix, as well as
  ExpansionMatrixLayout **2/2**. `.artifacts/ui59-checkpoint6-play.xml` **5/5** repeats
  SovereignPanel after adding a shortcut to the legend to areas bypassed by the ruler panel.
- `.artifacts/ui54-edit-final.xml`: **449/449 PASS**, including both SideControlTests for own
  Fellowship/Hunt controls. No map controls for the opponent also check new navigation in both
  roles.

- `.artifacts/map15-regression-core.xml/log` : **14/14 PASS**, 0 omissions, 11:12:49–11:13 :29Z.
  All BoardInteraction ( 7 ) and MapVisibility ( 2 ), MovementNetwork ( 1 ), ArmyContext ( 2 ),
  Muster/Elite ( 1 ), MultiUseDice ( 1 ).
- `.artifacts/map15-regression-routes.xml/log`: **7/7   PASS**,  0 omissions, 11:13:46–11:14: 00Z.
  Battle/retreat/advance  (1),  Stronghold/shelter  (1),  TravelGesture  (2),  Strategy02/March
  (1),  ShadowStrategy02/Moving  (1),  Shadow02/Search  (1).
- `.artifacts/map15-regression-abilities.xml/log` : **4/4 PASS**, 0 omissions, 11:14:17–11:14
  :29Z. Brand/Call It's Arms, Dáin/Iron Guard, Mouth/Messenger and Balrog/Travel from the matrix
  above.

All times with 2026-10-08 UTC Three XML is compared by machine to a list of full method names in `.artifacts/map15-regressions.json` : **25/25 exact methods, no additional and missing**. Manifesto serves to recreate filters; it is not a source of task status. New 7 and required 25 Regressions give 32/32 PASS ; independent existing evidence UI-54/UI-59 are listed separately.


<a id="oględziny-i-źródła-punktów"></a>
## Audits and sources of points

All of them were viewed 16   `map15-edge-*.png`:  Buckland/Old Forest on both sides of the common episode, FP/SH,  1366×768 and 1920×1080 with 100/125%. The names of the selected regions are legible and consistent with the test point. In addition, representative pairs were viewed. `before-pan` / `hit` for Old Forest and Dol Amroth and both views of small Osgiliath. In isolated checkout `.artifacts/screenshots` saved in total 64   PNG:  16 the edge, 24 the objectives and 24 State before the overt master. Drops include pinned self-view card and the right bar FP/SH. With a larger scale, the information panel may partially invade card; this is not evidence of the absence of all collisions of panels. Actual attempts require accessible terrain outside panels, unchanged position card and 0 commands during inspections.

Geometry with `content/map-geometry.json`: `contours-2`, SHA-256 `b1ed10e22091b22ea602df1d1b017387f7c5bf071d8ec5c1140a2c8721f4801a`, 105 regions and 106 rings; renderer MAP-14 in the commission of `c8a9e1c`. Examples are from source points and border owners, not from raster:

| Sample | Source/point in space board | Waiting |
|---|---|---|
| Buckland/Old Forest | `boundary-0113`, (408,328)→(424,328), centre (416,328) | Y−1 Buckland; Y+1 Old Forest; exact edge of the original `buckland` |
| Minas Tirith/Osgiliath | `boundary-0098`, middle of longest segment (1029.5,1006) | Both sides return the appropriate owner |
| Dagorlad/Morannon | `boundary-0208`, centre (1288,782.5) | Both sides return the appropriate owner |
| Dol Amroth/wybrze that | `boundary-0004`, centre (746,1015.5) | Interior of the region or no region on the sea side |
| Hole Gorgoroth | (1504.875,988.5), the region's inner ring | No region |
| Minas Tirith/Lamedon | Common physical boundary; no border of legal army movement | Drop does not send command, does not use dice and does not remove the choice |

Checker `python3 scripts/export_map_geometry.py --check --borders` confirms 105/106, 323 common paths, 360 classified parts and 232 legal contacts without changing exports. MAP-15 does not interpret the rules of movement again. Trials were made in macOS Unity; no Windows client or hand batch was launched. The final acceptance board/options remains a separate MAP-17.
