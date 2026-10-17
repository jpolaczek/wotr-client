using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class ShadowStrategy02NetworkTests
    {
        [UnityTest] public IEnumerator GatherKeepsRearGuardAndConfirmsThreeExplicitEdges() => Wait(Gather());
        [UnityTest] public IEnumerator LengthensRequiresTwoIndependentRoutesAndRestoresDraft() => Wait(Lengthens());
        [UnityTest] public IEnumerator MovingCommitsFourArmiesWithOnePaidDie() => Wait(Moving());
        [UnityTest] public IEnumerator RageSelectsRecruitmentRegionBeforeOptionalMovement() => Wait(RageRegion());
        [UnityTest] public IEnumerator RageKeepsFigureSelectionsFromBothDunlands() => Wait(RageMove());
        [UnityTest] public IEnumerator ReturnMovesWitchKingBeforeChoosingStackExcess() => Wait(ReturnExcess());
        [UnityTest] public IEnumerator CorsairsCanLandOnThePrintedLamedonCoast() => Wait(CorsairsLanding());
        [UnityTest] public IEnumerator CorsairsLetsDefenderShelterAfterSeeingTheLanding() => Wait(CorsairsDefense());
        [UnityTest] public IEnumerator CorsairsReliefKeepsGarrisonSeparateUntilRetreatThenCombines() => Wait(CorsairsRelief());
        [UnityTest] public IEnumerator CorsairsRemovesExcessBeforeTheFirstSiegeCard() => Wait(CorsairsExcess());
        [UnityTest] public IEnumerator CorsairsRequiresAnotherRoundWhenDefenderStays() => Wait(CorsairsForced());
        [UnityTest] public IEnumerator MumakilAddsItsHitAfterTheCombatComparison() => Wait(Mumakil());
        [UnityTest] public IEnumerator SwarmShowsCancelledOpponentCardWithoutNoCardBonus() => Wait(Swarm());

        private static async Task Gather()
        {
            using (var room = await Connect("sh-str02-gather", ScenarioKind.CardPack))
            {
                room.Sh.Board.Map.Select("moria");
                await room.Settle();
                var elite = room.Sh.Board.View.Units.Single(u => u.Location == "moria" && u.Kind == UnitKind.Elite).Id;
                Click(room.Sh, "figure-" + elite);
                await Route(room, "hollin", "north-dunland", "south-dunland");
                Assert.AreEqual(Side.FreePeoples, room.Fp.Board.View.Control["north-dunland"]);
                await room.Confirm(room.Sh);
                Assert.AreEqual("moria", room.Fp.Board.View.Units.Single(u => u.Id == elite).Location);
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Location == "south-dunland"));
                Assert.AreEqual(Side.Shadow, room.Fp.Board.View.Control["north-dunland"]);
            }
        }

        private static async Task Lengthens()
        {
            using (var room = await Connect("sh-str02-lengthens", ScenarioKind.CardPack))
            {
                await March(room, "moria", "north-dunland", "south-dunland");
                Assert.IsFalse(room.Sh.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                await March(room, "south-dunland", "north-dunland", "moria");
                // UI-216: both armies are planned on the map; the console counts them and no window opens.
                Assert.That(room.Sh.Board.Console.Status.Hint.text, Does.StartWith("Armies moved 2 of 2"));
                Assert.IsTrue(room.Sh.Board.Map.DraftOverlay.PlannedRegions.Contains("moria"));
                Assert.IsFalse(room.Sh.Board.DecisionWindow.IsOpen);
                await room.Confirm(room.Sh);
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Location == "south-dunland"));
                Assert.AreEqual(1, room.Fp.Board.View.Units.Count(u => u.Location == "moria"));
            }
        }

        private static async Task Moving()
        {
            using (var room = await Connect("sh-str02-moving", ScenarioKind.CardPack))
            {
                await March(room, "moria", "north-dunland");
                await March(room, "north-dunland", "hollin");
                await March(room, "hollin", "moria");
                await March(room, "south-dunland", "north-dunland");
                Assert.That(room.Sh.Board.Console.Status.Hint.text, Does.StartWith("Armies moved 4"));
                Assert.IsFalse(room.Sh.Board.DecisionWindow.IsOpen);
                await room.Confirm(room.Sh);
                Assert.AreEqual(3, room.Fp.Board.View.Units.Count(u => u.Location == "north-dunland"));
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Spent));
            }
        }

        private static async Task RageRegion()
        {
            using (var room = await Connect("sh-str02-rage-region", ScenarioKind.CardPack))
            {
                Assert.IsFalse(room.Sh.Board.View.Decision.Choices.Contains("orthanc"));
                room.Sh.Board.Map.Select("hollin");
                await room.Confirm(room.Sh);
                Assert.AreEqual("ArmyFigures", room.Sh.Board.View.Decision.Presentation);
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Nation == "isengard" && u.Location == "hollin"));
                await room.Confirm(room.Sh);
                Assert.AreEqual(4, room.Fp.Board.View.Units.Count(u => u.Location == "north-dunland" || u.Location == "south-dunland"));
            }
        }

        private static async Task RageMove()
        {
            using (var room = await Connect("sh-str02-rage-move", ScenarioKind.CardPack))
            {
                foreach (var region in new[] { "north-dunland", "south-dunland" })
                {
                    room.Sh.Board.Map.Select(region);
                    await room.Settle();
                    foreach (var unit in room.Sh.Board.View.Units.Where(u => u.Location == region && u.Nation == "isengard"))
                        Click(room.Sh, "effect-choice-" + unit.Id);
                }
                await room.Confirm(room.Sh);
                Assert.AreEqual(6, room.Fp.Board.View.Units.Count(u => u.Nation == "isengard" && u.Location == "hollin"));
            }
        }

        private static async Task ReturnExcess()
        {
            using (var room = await Connect("sh-str02-return-excess", ScenarioKind.CardPack))
            {
                Assert.IsTrue(room.Fp.Board.View.Characters.Any(c => c.DefinitionId == "base.character.witch-king" && c.Location == "angmar"));
                Assert.AreEqual(12, room.Sh.Board.View.Units.Count(u => u.Location == "angmar"));
                await room.RemoveExcess(room.Sh);
                Assert.AreEqual(10, room.Fp.Board.View.Units.Count(u => u.Location == "angmar"));
            }
        }

        private static async Task CorsairsLanding()
        {
            using (var room = await Connect("sh-str02-corsairs-landing", ScenarioKind.CardPack))
            {
                room.Sh.Board.Map.Select("umbar");
                await room.Settle();
                MapContourProbe.ClickRegion(room.Sh.Board.Map, "lamedon");
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Location == "umbar"));
                await room.Confirm(room.Sh);
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Location == "lamedon"));
                Assert.AreEqual(Side.Shadow, room.Fp.Board.View.Control["lamedon"]);
            }
        }

        private static async Task CorsairsDefense()
        {
            using (var room = await Connect("sh-str02-corsairs-defense", ScenarioKind.CardPack))
            {
                Assert.IsTrue(room.Fp.Board.View.Battle.ArrivalBeforeBattle);
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Side == Side.Shadow && u.Location == "dol-amroth"));
                Assert.IsFalse(room.Fp.Board.View.Units.Any(u => u.Location == "umbar"));
                await room.Direct(room.Fp, "action-RetreatIntoSiege");
                Assert.IsNull(room.Sh.Board.View.Battle);
                Assert.AreEqual(2, room.Sh.Board.View.Units.Count(u => u.Side == Side.FreePeoples && u.Location == "dol-amroth" && u.Garrison));
                Assert.AreEqual(2, room.Sh.Board.View.Units.Count(u => u.Side == Side.Shadow && u.Location == "dol-amroth" && !u.Garrison));
            }
        }

        private static async Task CorsairsRelief()
        {
            using (var room = await Connect("sh-str02-corsairs-relief", ScenarioKind.CardPack))
            {
                Assert.AreEqual(BattleMode.Relief, room.Fp.Board.View.Battle.Mode);
                Assert.AreEqual(7, room.Fp.Board.View.Battle.Attacker.Units.Count);
                Assert.AreEqual(5, room.Fp.Board.View.Units.Count(u => u.Side == Side.Shadow && u.Location == "dol-amroth" && u.Garrison));
                await ArmyGestures.Target(room, room.Fp, "erech");
                Assert.AreEqual(DecisionKind.RemoveExcess, room.Sh.Board.View.Decision.Kind);
                Assert.AreEqual("Complete", room.Sh.Board.View.Battle.Stage);
                Assert.IsFalse(room.Sh.Board.View.Units.Any(u => u.Location == "dol-amroth" && u.Garrison));
                var excess = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.RemoveExcess);
                foreach (var id in excess.EligibleIds.Take(excess.Minimum))
                    Click(room.Sh, "figure-" + id);
                await room.Until(() => room.Sh.Board.Tray.Q<Button>("confirm-decision")?.enabledSelf == true);
                await room.Settle();
                LayoutTests.Save(room.Sh.Viewport, "shadow-strategy02-relief-network-before-excess-confirm.png");
                FellowshipLayoutTests.Check(room.Sh.Board);
                await room.Confirm(room.Sh);
                Assert.AreEqual(10, room.Fp.Board.View.Units.Count(u => u.Location == "dol-amroth"));
                Assert.IsNull(room.Sh.Board.View.Battle);
            }
        }

        private static async Task CorsairsExcess()
        {
            using (var room = await Connect("sh-str02-corsairs-excess", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Sh.Board.View.Battle);
                Assert.AreEqual(12, room.Sh.Board.View.Units.Count(u => u.Side == Side.Shadow && u.Location == "dol-amroth"));
                await room.RemoveExcess(room.Sh);
                Assert.AreEqual(BattleMode.Siege, room.Fp.Board.View.Battle.Mode);
                Assert.AreEqual(10, room.Sh.Board.View.Battle.Attacker.Units.Count);
                Assert.AreEqual(DecisionKind.CombatCard, room.Sh.Board.View.Decision.Kind);
            }
        }

        private static async Task CorsairsForced()
        {
            using (var room = await Connect("sh-str02-corsairs-forced", ScenarioKind.CardPack))
            {
                Assert.AreEqual(DecisionKind.BattleRetreat, room.Fp.Board.View.Decision.Kind);
                Assert.IsNull(room.Sh.Board.Tray.Q<Button>("action-StopBattle"));
                await room.Direct(room.Fp, "action-HoldBattle");
                // QUAL-19: another round is what this test guards. Without a legal Combat card each side's round-2 card
                // choice is sent as No card at once (UI-151), so the decision itself may already be past.
                Assert.AreEqual(2, room.Sh.Board.View.Battle.Round);
                Assert.AreEqual(2, room.Fp.Board.View.Battle.Round);
            }
        }

        private static async Task Mumakil()
        {
            using (var room = await Connect("sh-str02-mumakil", ScenarioKind.CardPack))
            {
                Assert.AreEqual(2, room.Fp.Board.View.Battle.PendingHits);
                Assert.AreEqual(1, room.Fp.Board.View.Battle.Attacker.CombatModifier);
                Assert.AreEqual(0, room.Fp.Board.View.Battle.Attacker.RerollModifier);
                foreach (var id in room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.TakeBattleLosses).EligibleIds.Take(2))
                    Click(room.Fp, "loss-" + id);
                await room.Confirm(room.Fp);
                Assert.AreEqual(1, room.Sh.Board.View.Units.Count(u => u.Side == Side.FreePeoples && u.Location == "north-ithilien"));
            }
        }

        private static async Task Swarm()
        {
            using (var room = await Connect("sh-str02-swarm", ScenarioKind.CardPack))
            {
                Assert.IsTrue(room.Fp.Board.View.Battle.Defender.CardCancelled);
                Assert.AreEqual(0, room.Sh.Board.View.Battle.Attacker.RerollModifier);
                var id = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.RerollBattle).EligibleIds[0];
                Click(room.Sh, "battle-die-" + id);
                await room.Confirm(room.Sh);
                Assert.IsTrue(room.Fp.Board.View.Battle.Defender.CardCancelled);
            }
        }

        private static async Task March(MatchTestRoom room, string from, params string[] route)
        {
            room.Sh.Board.Map.Select(from);
            await room.Settle();
            await Route(room, route);
        }

        private static async Task Route(MatchTestRoom room, params string[] route)
        {
            foreach (var region in route)
            {
                MapContourProbe.ClickRegion(room.Sh.Board.Map, region);
                await room.Settle();
            }
        }
    }
}
