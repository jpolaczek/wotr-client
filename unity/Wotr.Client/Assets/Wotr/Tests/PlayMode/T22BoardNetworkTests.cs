using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class T22BoardNetworkTests
    {
        [UnityTest]
        public IEnumerator MixedArmySplitPreservesExactFiguresAndExplicitWestCost() => Wait(Split());

        [UnityTest]
        public IEnumerator TwoDistantMovesPersistOneAtATime() => Wait(TwoMoves());

        [UnityTest]
        public IEnumerator TemporaryExcessDoesNotAllowArrivalsToMoveTwice() => Wait(TemporaryExcess());

        [UnityTest]
        public IEnumerator FinalExcessReturnsChosenFiguresBeforeReleasingThePaidDie() => Wait(FinalExcess());

        [UnityTest]
        public IEnumerator TwoNationsRecruitAtDifferentSettlementsWithOneRemainingRegular() => Wait(Recruit());

        private static async Task Split()
        {
            using var room = await Connect("board-split", ScenarioKind.BaseAcceptance);
            var before = room.Fp.Board.View;
            var origin = before.Units.Where(u => u.Location == "minas-tirith").ToArray();
            Assert.AreEqual(3, origin.Select(u => u.Nation).Distinct().Count());
            var stay = new[]
            {
                origin.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular).Id,
                origin.Single(u => u.Nation == "gondor" && u.Kind == UnitKind.Leader).Id
            };
            CardNetworkTests.Click(room.Fp.Board.Map.Q<Button>("army-minas-tirith-FreePeoples"));
            await room.Settle();
            Assert.IsEmpty(room.Fp.Board.OwnDice.Query<Button>(className: "selected").ToList(),
                "Army and Will of the West are strategically different costs.");
            var die = before.Dice.Single(d => d.Owner == Side.FreePeoples && d.Face == DieFace.WillOfTheWest).Id;
            ArmyGestures.Die(room.Fp, die, DieFace.Character);
            await ArmyGestures.All(room.Fp, "minas-tirith");
            await room.Settle();
            foreach (var id in stay)
                CardNetworkTests.Click(room.Fp.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + id));
            await ArmyGestures.Target(room, room.Fp, "osgiliath");
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                foreach (var unit in origin)
                    Assert.AreEqual(stay.Contains(unit.Id) ? "minas-tirith" : "osgiliath",
                        client.Board.View.Units.Single(u => u.Id == unit.Id).Location);
                Assert.AreEqual("osgiliath", client.Board.View.Characters.Single(c => c.Id == "base.character.strider").Location);
            }
            Assert.AreEqual(1, room.Fp.Board.View.ActionSequence.Remaining, "Will of the West uses Army for an ordinary march.");
            await ArmyGestures.Finish(room, room.Fp);
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static async Task TwoMoves()
        {
            using var room = await Connect("board-two-moves", ScenarioKind.BaseAcceptance);
            var before = room.Fp.Board.View;
            var die = await BeginArmy(room, "minas-tirith");
            await ArmyGestures.Target(room, room.Fp, "lossarnach");
            Assert.AreEqual(1, room.Fp.Board.View.Revision);
            Assert.IsNull(room.Fp.Board.Tray.Q<Button>("edit-move-0"));
            await ArmyGestures.All(room.Fp, "dale");
            await ArmyGestures.Target(room, room.Fp, "erebor");
            AssertMoved(room, before, "minas-tirith", "lossarnach");
            AssertMoved(room, before, "dale", "erebor");
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static async Task TemporaryExcess()
        {
            using var room = await Connect("board-temporary-excess", ScenarioKind.BaseAcceptance);
            var before = room.Fp.Board.View;
            var die = await BeginArmy(room, "minas-tirith");
            await ArmyGestures.Target(room, room.Fp, "osgiliath");
            await ArmyGestures.All(room.Fp, "osgiliath");
            await room.Settle();
            foreach (var arrival in before.Units.Where(u => u.Location == "minas-tirith"))
                Assert.IsFalse(room.Fp.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + arrival.Id).ClassListContains("eligible-figure"),
                    "The same physical figure cannot move again in this action.");
            await ArmyGestures.Target(room, room.Fp, "south-ithilien");
            AssertMoved(room, before, "minas-tirith", "osgiliath");
            AssertMoved(room, before, "osgiliath", "south-ithilien");
            Assert.AreEqual(Side.Shadow, room.Fp.Board.View.DecisionOwner);
            Assert.AreEqual(DecisionKind.Action, room.Sh.Board.View.Decision.Kind);
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static async Task FinalExcess()
        {
            using var room = await Connect("board-final-excess", ScenarioKind.BaseAcceptance);
            var before = room.Fp.Board.View;
            var die = await BeginArmy(room, "minas-tirith");
            await ArmyGestures.Target(room, room.Fp, "osgiliath");
            await ArmyGestures.Finish(room, room.Fp);
            Assert.AreEqual(DecisionKind.RemoveExcess, room.Fp.Board.View.Decision.Kind);
            Assert.AreEqual(2, room.Fp.Board.View.Decision.RequiredCount);
            Assert.AreEqual(DieLocation.Resolving, room.Fp.Board.View.Dice.Single(d => d.Id == die).Location);
            Assert.AreEqual(12, CombatCount(room.Fp.Board.View, "osgiliath"));
            var returned = before.Units.Where(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular).Take(2).ToArray();
            foreach (var unit in returned)
                await room.ChooseFigure(room.Fp, unit.Id);
            await Commit(room);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                Assert.AreEqual(10, CombatCount(client.Board.View, "osgiliath"));
                foreach (var unit in returned)
                    Assert.AreEqual("reserve", client.Board.View.Units.Single(u => u.Id == unit.Id).Location);
            }
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
            Assert.AreEqual(Side.Shadow, room.Fp.Board.View.DecisionOwner);
        }

        private static async Task Recruit()
        {
            using var room = await Connect("board-recruit", ScenarioKind.BaseAcceptance);
            var before = room.Fp.Board.View;
            var last = before.Units.Single(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve");
            var die = before.Dice.Single(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Muster).Id;
            ArmyGestures.Die(room.Fp, die, DieFace.Muster);
            await room.Settle();
            // UI-135: the reserve is chosen in the Reserve tab and the settlement click commits the step; UI-181: the
            // player opens the tab the Muster die lit.
            room.Fp.Board.Reinforcements.Open();
            await room.Settle();
            var panel = room.Fp.Board.Reinforcements.Root;
            var regular = panel.Q<Button>("reserve-token-gondor-Regular");
            CardNetworkTests.Click(regular);
            await room.Settle();
            AssertBeforeCommit(room, before);
            Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"));
            await ArmyGestures.Target(room, room.Fp, "pelargir");
            Assert.AreEqual(1, room.Fp.Board.View.ActionSequence.Remaining);
            Assert.AreEqual(DieLocation.Resolving, room.Fp.Board.View.Dice.Single(d => d.Id == die).Location);
            var connected = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > connected);
            await room.Settle();
            Assert.AreEqual(die, room.Fp.Board.ActionDie.DieId);
            Assert.IsFalse(room.Fp.Board.Map.DraftOverlay.Targets.Contains("pelargir"));
            Assert.IsFalse(panel.Q<Button>("reserve-token-gondor-Regular").enabledSelf, "Gondor's last Regular was recruited.");
            CardNetworkTests.Click(panel.Q<Button>("reserve-token-rohan-Regular"));
            await ArmyGestures.Target(room, room.Fp, "edoras");
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                Assert.AreEqual("pelargir", client.Board.View.Units.Single(u => u.Id == last.Id).Location);
                Assert.AreEqual(1, client.Board.View.Units.Count(u => u.Nation == "rohan" && u.Location == "edoras"));
                Assert.IsFalse(client.Board.View.Units.Any(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve"));
            }
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static async Task<string> BeginArmy(MatchTestRoom room, string region)
        {
            var die = room.Fp.Board.View.Dice.Single(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army).Id;
            ArmyGestures.Die(room.Fp, die);
            await ArmyGestures.All(room.Fp, region);
            await room.Settle();
            return die;
        }

        private static Task Target(MatchTestRoom room, string region) => T22NetworkActions.Region(room, room.Fp, region);

        private static async Task Commit(MatchTestRoom room)
        {
            await T22NetworkActions.Ready(room, room.Fp);
            await room.Settle();
            await room.Confirm(room.Fp);
        }

        private static void AssertBeforeCommit(MatchTestRoom room, PlayerView before)
        {
            Assert.AreEqual(before.Revision, room.Sh.Board.View.Revision);
            Assert.AreEqual(JsonConvert.SerializeObject(before.Units), JsonConvert.SerializeObject(room.Sh.Board.View.Units));
            Assert.IsEmpty(room.Fp.Board.Tray.Query<ScrollView>().ToList());
            Assert.IsEmpty(room.Fp.Board.Tray.Query<TextField>().ToList());
        }

        private static void AssertMoved(MatchTestRoom room, PlayerView before, string origin, string destination)
        {
            foreach (var client in new[] { room.Fp, room.Sh })
                foreach (var unit in before.Units.Where(u => u.Location == origin))
                    Assert.AreEqual(destination, client.Board.View.Units.Single(u => u.Id == unit.Id).Location, unit.Id);
        }

        private static int CombatCount(PlayerView view, string region) =>
            view.Units.Count(u => u.Location == region && (u.Kind == UnitKind.Regular || u.Kind == UnitKind.Elite));
    }
}
