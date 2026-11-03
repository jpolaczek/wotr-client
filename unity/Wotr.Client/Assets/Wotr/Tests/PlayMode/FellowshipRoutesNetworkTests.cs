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
            // UI-448 (owner): Declare stands beside Finish phase, in one row.
            var declare = board.Fellowship.Root.Q<Button>("fellowship-declare");
            Assert.IsNotNull(declare, "Declare beside Finish phase.");
            Assert.AreSame(board.Fellowship.Root.Q<Button>("fellowship-continue").parent, declare.parent, "In Finish phase's row.");
            Assert.IsTrue(declare.enabledSelf, "The hidden Fellowship may declare.");
            // Its click selects the Fellowship for the declaration and brings the camera to its region.
            board.Map.FocusOn(UnityEngine.Vector2.zero, board.Map.FitZoom * 3);
            await room.Settle();
            PointerGestureEvents.ClickAt(declare);
            await room.Settle();
            StringAssert.Contains("Choose a route", board.Tray.Q<Label>("travel-summary").text, "The declaration started.");
            var region = board.View.Fellowship.RegionId;
            var anchor = board.Map.Data.Shapes[region].Anchor;
            Assert.IsTrue(board.Map.contentRect.Contains(board.Map.Camera.ToScreen(new UnityEngine.Vector2(anchor[0], anchor[1]))),
                "The camera came to the Fellowship's region.");
            Assert.AreSame(board.Fellowship.Root.Q<Button>("fellowship-continue").parent, declare.parent, "Declare keeps its row while declaring.");
            MapContourProbe.ReachRegion(board.Map, "minas-tirith");
            await room.Settle();
            var pan = board.Map.Camera.Pan;
            var zoom = board.Map.Camera.Zoom;
            // UI-448: no effect line, no Undo step or Cancel in the panel; Finish phase stays beside Declare, inactive.
            Assert.IsNull(board.Tray.Q<Label>("fellowship-route-effect"));
            Assert.IsNull(board.Tray.Q<Button>("route-undo"));
            Assert.IsNull(board.Tray.Q<Button>("route-cancel"));
            var finish = board.Fellowship.Root.Q<Button>("fellowship-continue");
            Assert.AreEqual(DisplayStyle.Flex, finish.resolvedStyle.display, "Finish phase keeps its place.");
            Assert.IsFalse(finish.enabledSelf, "Finish phase waits while the declaration is drawn.");
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

        private static Button Recovery(BoardScreen board) => PendingRecovery.Control(board);
    }
}
