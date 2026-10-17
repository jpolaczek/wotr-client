using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-105: movement happens on the map only. The die comes first, then the region's panel picks the figures,
    // and a click on a highlighted destination moves them at once, without a drag or a confirmation.
    public sealed class MapClickMoveNetworkTests
    {
        private const string From = "lossarnach";

        [UnityTest]
        public IEnumerator ANazgulPickedBeforeASoldierStaysInTheArmyThatMovesOnTheDestinationClick() => Wait(Army());

        [UnityTest]
        public IEnumerator ANazgulPickedAloneFliesOnTheDestinationClick() => Wait(Alone());

        private static async Task Army()
        {
            using var room = await Connect("ui105-nazgul-army", ScenarioKind.Interaction, openPanels: false);
            var board = room.Sh.Board;
            var nazgul = Nazgul(board);
            var soldier = board.View.Units.First(u => u.Location == From && u.Kind == UnitKind.Regular).Id;
            await CharacterDie(room);
            await Pick(room, nazgul);
            Assert.IsTrue(Selected(board, nazgul));
            await Pick(room, soldier);
            Assert.IsTrue(Selected(board, nazgul), "Adding a soldier keeps the Nazgûl picked first.");
            Assert.IsTrue(Selected(board, soldier));
            Assert.AreEqual(2, board.Hover.Panel.Root.Query<Button>(className: "selected").ToList().Count);
            var revision = board.View.Revision;
            await ArmyGestures.Target(room, room.Sh, "pelargir");
            Assert.AreEqual(revision + 1, room.Fp.Board.View.Revision);
            var units = room.Fp.Board.View.Units;
            Assert.AreEqual("pelargir", units.Single(u => u.Id == nazgul).Location);
            Assert.AreEqual("pelargir", units.Single(u => u.Id == soldier).Location);
            Assert.AreEqual(2, units.Count(u => u.Location == From && u.Kind == UnitKind.Regular), "Unpicked soldiers stay.");
        }

        private static async Task Alone()
        {
            using var room = await Connect("ui105-nazgul-alone", ScenarioKind.Interaction, openPanels: false);
            var board = room.Sh.Board;
            var nazgul = Nazgul(board);
            await CharacterDie(room);
            await Pick(room, nazgul);
            Assert.IsTrue(Selected(board, nazgul));
            await room.MoveTo(room.Sh, "osgiliath");
            var units = room.Fp.Board.View.Units;
            Assert.AreEqual("osgiliath", units.Single(u => u.Id == nazgul).Location);
            Assert.AreEqual(3, units.Count(u => u.Location == From && u.Kind == UnitKind.Regular), "The army stays.");
        }

        private static async Task CharacterDie(MatchTestRoom room)
        {
            var die = room.Sh.Board.View.Dice.Single(d => d.Owner == Side.Shadow && d.Face == DieFace.Character).Id;
            ArmyGestures.Die(room.Sh, die);
            await room.Settle();
            Assert.AreEqual(die, room.Sh.Board.ActionDie.DieId);
        }

        private static async Task Pick(MatchTestRoom room, string id)
        {
            FellowshipGestures.Figure(room.Sh, From, id);
            await room.Settle();
        }

        private static string Nazgul(BoardScreen board) =>
            board.View.Units.Single(u => u.Location == From && u.Kind == UnitKind.Nazgul).Id;

        private static bool Selected(BoardScreen board, string id)
        {
            board.Hover.Open(From);
            return board.Hover.Panel.Root.Q<Button>("hover-figure-" + id).ClassListContains("selected");
        }
    }
}
