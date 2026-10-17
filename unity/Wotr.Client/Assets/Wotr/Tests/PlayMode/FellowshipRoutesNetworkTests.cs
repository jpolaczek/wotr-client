using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed class FellowshipRoutesNetworkTests
    {
        [UnityTest]
        public IEnumerator ZeroProgressDeclarationPreviewsHealingAndActivationAndRecoversReceipt() => MatchTestRoom.Wait(Stay());

        private static async Task Stay()
        {
            using var room = await UI10TestRoom.Connect("ui34-route-stay");
            var board = room.Fp.Board;
            var dice = board.View.Dice.Select(d => d.Location).ToArray();
            Assert.IsTrue(board.Fellowship.IsOpen);
            Assert.IsNull(board.Fellowship.Root.Q<Button>("fellowship-declare"), "UI-236: no panel button declares.");
            FellowshipGestures.Declare(room.Fp);
            await room.Settle();
            MapContourProbe.ReachRegion(board.Map, "minas-tirith");
            await room.Settle();
            var pan = board.Map.Camera.Pan;
            var zoom = board.Map.Camera.Zoom;
            StringAssert.Contains("Remain hidden", board.Tray.Q<Label>("fellowship-route-effect").text);
            Assert.AreEqual(0, room.Boundary.Commands);
            Assert.IsNull(room.Sh.Board.View.Decision);
            Assert.IsEmpty(room.Sh.Board.View.Options);
            Assert.IsEmpty(room.Sh.Board.Map.DraftOverlay.Targets);
            Assert.AreEqual(1, room.Sh.Board.View.Fellowship.Corruption);

            // UI-219: with no Progress, one click on the Fellowship's region stays and declares at once.
            room.Boundary.HoldNextCommand = true;
            MapContourProbe.Click(board.Map, MapContourProbe.Terrain(board.Map, "minas-tirith"));
            await room.Until(() => room.Boundary.CommandHeld);
            await room.At(1);
            CollectionAssert.AreEqual(new[] { "minas-tirith" }, room.Boundary.LastCommand.Payload.Route);
            Assert.AreEqual(CommandKind.DeclareFellowship, room.Boundary.LastCommand.Kind);
            Assert.IsTrue(board.View.Fellowship.Hidden);
            Assert.AreEqual(0, board.View.Fellowship.Corruption);
            Assert.IsTrue(board.View.Nations.Single(n => n.Id == "gondor").Active);
            CollectionAssert.AreEqual(dice, board.View.Dice.Select(d => d.Location));
            room.Boundary.ReleaseCommand();
            await room.Until(() => Recovery(board) != null);
            await room.Settle();
            PointerGestureEvents.ClickAt(Recovery(board));
            await room.Until(() => Recovery(board) == null);
            Assert.Greater(room.Boundary.ReceiptLookups, 0);
            Assert.AreEqual(1, room.Boundary.Commands);
            await room.Fp.Connection.Refresh();
            await room.Settle();
            Assert.AreEqual(1, board.View.Revision);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(zoom, board.Map.Camera.Zoom);
        }

        private static Button Recovery(BoardScreen board) => board.Tray.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result");
    }
}
