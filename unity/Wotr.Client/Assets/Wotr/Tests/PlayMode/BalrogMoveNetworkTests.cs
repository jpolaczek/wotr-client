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
    // UI-459 (owner, 2026-11-03): "Moving the Balrog alone or with the army should not be chosen by clicking the skill icon
    // in different versions; when we click the units a window with them opens where we mark who goes and who doesn't, then
    // the move is done as always, by click or drag." lords-sh-use-balrog-active: the active Balrog stands in Moria with his
    // Orcs beside an Elven army in Dimrill Dale, and the Balrog die shows Use Balrog.
    public sealed class BalrogMoveNetworkTests
    {
        private const string Scenario = "lords-sh-use-balrog-active";
        private const string Balrog = "lords.character.balrog";

        [UnityTest] public IEnumerator TheBalrogsPanelLeavesHisArmyBehindAndHeMovesAlone() => Wait(Alone(false));
        [UnityTest] public IEnumerator TheBalrogIsCarriedAloneToHisTarget() => Wait(Alone(true));
        [UnityTest] public IEnumerator TheBalrogAttacksWithHisWholeArmy() => Wait(Army());

        private static async Task Alone(bool drag)
        {
            using var room = await ConnectCatalog(Scenario);
            var client = room.Sh;
            var board = client.Board;
            var die = await Choose(room);
            var orcs = board.View.Units.Where(u => u.Location == "moria" && u.Side == Side.Shadow).Select(u => u.Id).ToList();
            var target = board.View.Options.Single(o => o.Kind == CommandKind.MoveShadowCharacters).Travel
                .Single(t => t.Id == Balrog).Targets.First(t => t != "moria" && t != "dimrill-dale");
            if (drag)
            {
                MapContourProbe.ReachRegion(board.Map, target);
                await room.Settle();
                var revision = board.View.Revision;
                MapContourProbe.DragFigure(board, board.Map.Q<Button>("character-" + Balrog), MapContourProbe.Terrain(board.Map, target));
                await room.At(revision + 1);
            }
            else
            {
                await ArmyGestures.All(client, "moria");
                await room.Settle();
                foreach (var id in orcs.Append(Balrog))
                    Assert.IsTrue(Figure(board, id).ClassListContains("selected"), "His army's click marks every figure: " + id);
                LayoutTests.Save(client.Viewport, "ui459-army-marked.png");
                foreach (var id in orcs)
                {
                    CardNetworkTests.Click(Figure(board, id));
                    await room.Settle();
                }
                Assert.IsTrue(Figure(board, Balrog).ClassListContains("selected"), "The Balrog alone stays marked.");
                Assert.IsFalse(orcs.Any(id => Figure(board, id).ClassListContains("selected")));
                LayoutTests.Save(client.Viewport, "ui459-alone-marked.png");
                await ArmyGestures.Target(room, client, target);
            }
            var latest = room.Fp.Board.View;
            Assert.AreEqual(target, latest.Characters.Single(c => c.Id == Balrog).Location);
            Assert.IsTrue(orcs.All(id => latest.Units.Single(u => u.Id == id).Location == "moria"), "His army stays behind.");
            Assert.AreEqual(DieLocation.Spent, latest.Dice.Single(d => d.Id == die).Location);
        }

        private static async Task Army()
        {
            using var room = await ConnectCatalog(Scenario);
            var client = room.Sh;
            var board = client.Board;
            var die = await Choose(room);
            await ArmyGestures.All(client, "moria");
            await room.Settle();
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains("dimrill-dale"), "The Balrog's army may attack the Elves.");
            CardNetworkTests.Click(Figure(board, Balrog));
            await room.Settle();
            Assert.IsFalse(board.Map.DraftOverlay.Targets.Contains("dimrill-dale"), "His army moves only with him.");
            CardNetworkTests.Click(Figure(board, Balrog));
            await room.Settle();
            await ArmyGestures.Target(room, client, "dimrill-dale");
            var latest = room.Fp.Board.View;
            Assert.AreEqual("dimrill-dale", latest.Battle?.RegionId, "The whole force attacks the Elves.");
            Assert.AreNotEqual(DieLocation.Available, latest.Dice.Single(d => d.Id == die).Location);
        }

        // The Balrog die lights his force on the map, with no icon to choose first and the Characters tab dark.
        private static async Task<string> Choose(MatchTestRoom room)
        {
            var board = room.Sh.Board;
            var die = board.View.Dice.Single(d => d.Owner == Side.Shadow && d.Face == DieFace.UseBalrog && d.Location == DieLocation.Available).Id;
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            await room.Settle();
            Assert.AreEqual(die, board.ActionDie.DieId);
            Assert.IsTrue(board.ActionDie.Implied, "The Balrog die is his Use Balrog.");
            foreach (var kind in new[] { "MoveArmies", "AttackArmy", "MoveShadowCharacters" })
                Assert.IsEmpty(board.Hover.Panel.ActionRegion(kind + "-use-balrog"), "No icon for " + kind);
            Assert.IsFalse(board.Characters.Tab.ClassListContains(DieOptionGlow.LitClass), "His moves are made on the map.");
            Assert.IsTrue(board.Map.Q<Button>("army-moria-Shadow").ClassListContains("eligible-army"), "His force lights.");
            StringAssert.Contains("alone moves by himself", board.Console.Status.Hint.text);
            return die;
        }

        private static Button Figure(BoardScreen board, string id)
        {
            board.Hover.Open("moria");
            return board.Hover.Panel.Root.Q<Button>("hover-figure-" + id);
        }
    }
}
