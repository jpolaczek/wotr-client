using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class ArmyContextNetworkTests
    {
        [UnityTest]
        public IEnumerator FirstMarchPersistsAndMergedFiguresCannotMoveTwice() => Wait(TwoSteps());

        [UnityTest]
        public IEnumerator SourceCorrectionAndFinishDoNotUndoTheSavedMove() => Wait(Correction());

        [UnityTest]
        public IEnumerator FinishOpensRealExcessDecision() => Wait(Excess());

        [UnityTest]
        public IEnumerator EnemyClickAttacksWithFreePeoples() => Wait(Attack("ui17-attack-fp", "minas-tirith", "lossarnach", false));

        [UnityTest]
        public IEnumerator EnemyClickAttacksWithShadow() => Wait(Attack("ui17-attack-sh", "osgiliath", "minas-tirith", false));

        [UnityTest]
        public IEnumerator GarrisonCanSortieButHasNoMarchTargets() => Wait(Attack("ui17-sortie", "minas-tirith", "minas-tirith", true));

        private static async Task TwoSteps()
        {
            using var room = await Connect("ui17-board-temporary-excess", ScenarioKind.BaseAcceptance);
            var client = room.Fp;
            var board = client.Board;
            var arrivals = board.View.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToArray();
            var residents = board.View.Units.Where(u => u.Location == "osgiliath" && u.Side == Side.FreePeoples).Select(u => u.Id).ToArray();
            var die = ArmyGestures.ArmyDie(client);
            ArmyGestures.Die(client, die);
            await ArmyGestures.All(client, "minas-tirith");
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
            await ArmyGestures.Target(room, client, "osgiliath");
            Assert.AreEqual(DecisionKind.ActionSequence, board.View.Decision.Kind);
            Assert.AreEqual(1, board.View.ActionSequence.Remaining);
            Assert.AreEqual(DieLocation.Resolving, board.View.Dice.Single(d => d.Id == die).Location);
            Assert.IsNull(room.Sh.Board.View.Decision);
            Assert.AreEqual(1, room.Sh.Board.View.ActionSequence.CompletedSteps);
            var connected = client.Connected;
            client.Connection.Reconnect();
            await room.Until(() => client.Connected > connected);
            await room.Settle();
            Assert.AreEqual(die, board.ActionDie.DieId);
            Assert.AreEqual(1, board.View.ActionSequence.Remaining);
            await ArmyGestures.All(client, "osgiliath");
            await room.Settle();
            foreach (var id in arrivals)
            {
                var figure = board.Hover.Panel.Root.Q<Button>("hover-figure-" + id);
                Assert.IsFalse(figure.ClassListContains("eligible-figure"));
                CardNetworkTests.Click(figure);
                Assert.IsFalse(figure.ClassListContains("selected"));
            }
            foreach (var id in residents)
                Assert.IsTrue(board.Hover.Panel.Root.Q<Button>("hover-figure-" + id).ClassListContains("selected"));
            await ArmyGestures.Target(room, client, "south-ithilien");
            Assert.IsNull(board.View.ActionSequence);
            Assert.AreEqual(Side.Shadow, board.View.DecisionOwner);
            Assert.AreEqual(DieLocation.Spent, board.View.Dice.Single(d => d.Id == die).Location);
            Assert.IsTrue(board.View.Units.Where(u => arrivals.Contains(u.Id)).All(u => u.Location == "osgiliath"));
            Assert.IsTrue(board.View.Units.Where(u => residents.Contains(u.Id)).All(u => u.Location == "south-ithilien"));
            Assert.AreEqual(2, board.View.Revision);
        }

        private static async Task Correction()
        {
            using var room = await Connect("ui17-board-two-moves", ScenarioKind.BaseAcceptance);
            var client = room.Fp;
            var board = client.Board;
            ArmyGestures.Die(client, ArmyGestures.ArmyDie(client));
            await ArmyGestures.All(client, "minas-tirith");
            var first = board.View.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular).Id;
            CardNetworkTests.Click(board.Hover.Panel.Root.Q<Button>("hover-figure-" + first));
            Assert.IsFalse(board.Hover.Panel.Root.Q<Button>("hover-figure-" + first).ClassListContains("selected"));
            // A friendly banner is a source, while the highlighted terrain is a destination. UI-127: its click
            // selects that whole force, replacing the unfinished choice, and opens the region's panel.
            await MapContourProbe.ReachMarker(board.Map, board.Map.Q<Button>("army-osgiliath-FreePeoples"), "osgiliath");
            PointerGestureEvents.ClickAt(board.Map.Q<Button>("army-osgiliath-FreePeoples"));
            await room.Settle();
            Assert.AreEqual("osgiliath", board.Hover.Panel.Region);
            var other = board.View.Units.First(u => u.Location == "osgiliath" && u.Side == Side.FreePeoples).Id;
            Assert.AreEqual(0, board.View.Revision);
            Assert.IsTrue(board.Hover.Panel.Root.Q<Button>("hover-figure-" + other).ClassListContains("selected"),
                "One click selects the other force.");
            await ArmyGestures.Target(room, client, "south-ithilien");
            using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                board.Root.SendEvent(key);
            using (var key = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape }))
                board.Root.SendEvent(key);
            Assert.IsTrue(board.Menu.IsOpen, "With no unsaved choice, Escape opens Menu and cannot undo the saved march.");
            Assert.AreEqual(1, board.View.Revision);
            Assert.AreEqual("south-ithilien", board.View.Units.Single(u => u.Id == other).Location);
            Assert.AreEqual("minas-tirith", board.View.Units.Single(u => u.Id == first).Location);
            await room.Settle();
            PointerGestureEvents.ClickAt(board.Menu.Panel.Q<Button>("board-menu-resume"));
            Assert.IsFalse(board.Menu.IsOpen);
            await ArmyGestures.Finish(room, client);
            Assert.IsNull(board.View.ActionSequence);
            Assert.AreEqual(Side.Shadow, board.View.DecisionOwner);
        }

        private static async Task Excess()
        {
            using var room = await Connect("ui17-board-final-excess", ScenarioKind.BaseAcceptance);
            var client = room.Fp;
            var die = ArmyGestures.ArmyDie(client);
            ArmyGestures.Die(client, die);
            await ArmyGestures.All(client, "minas-tirith");
            await ArmyGestures.Target(room, client, "osgiliath");
            Assert.AreEqual(DecisionKind.ActionSequence, client.Board.View.Decision.Kind);
            await ArmyGestures.Finish(room, client);
            Assert.AreEqual(DecisionKind.RemoveExcess, client.Board.View.Decision.Kind);
            Assert.AreEqual(DieLocation.Resolving, client.Board.View.Dice.Single(d => d.Id == die).Location);
            await room.RemoveExcess(client);
            Assert.AreEqual(DieLocation.Spent, client.Board.View.Dice.Single(d => d.Id == die).Location);
        }

        private static async Task Attack(string scenario, string from, string to, bool garrison)
        {
            using var room = await Connect(scenario, ScenarioKind.BaseAcceptance);
            var client = room.Actor;
            var board = client.Board;
            var enemy = board.View.You == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            board.Hover.Open(to);
            await room.Settle();
            var defender = board.Map.Q<Button>("army-" + to + "-" + enemy);
            // Inspection reveals the army but does not frame it or clear covering HUD.
            await MapContourProbe.ReachMarker(board.Map, defender, to);
            PointerGestureEvents.ClickAt(defender);
            Assert.AreEqual(0, board.View.Revision);
            var attackers = board.View.Units.Where(u => u.Side == board.View.You && u.Location == from && u.Garrison == garrison).Select(u => u.Id).ToArray();
            ArmyGestures.Die(client, ArmyGestures.ArmyDie(client));
            await ArmyGestures.All(client, from, garrison);
            Assert.Contains(to, board.Map.DraftOverlay.Targets.ToList());
            if (garrison)
                CollectionAssert.AreEquivalent(new[] { to }, board.Map.DraftOverlay.Targets);
            await room.Settle();
            board.Hover.Open(to);
            await room.Settle();
            await MapContourProbe.ReachMarker(board.Map, defender, to);
            PointerGestureEvents.ClickAt(defender);
            await room.At(1);
            Assert.NotNull(board.View.Battle);
            Assert.IsTrue(board.View.Units.Where(u => attackers.Contains(u.Id)).All(u => u.Location == from));
            Assert.AreEqual(to, board.View.Battle.RegionId);
            Assert.AreEqual(1, board.View.Dice.Count(d => d.Owner == board.View.You && d.Location == DieLocation.Resolving));
        }
    }
}
