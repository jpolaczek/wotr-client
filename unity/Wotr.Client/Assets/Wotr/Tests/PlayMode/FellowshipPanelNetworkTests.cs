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
    public sealed class FellowshipPanelNetworkTests
    {
        [UnityTest]
        public IEnumerator MoveKeepsTheMapRegionAndRecoversTheExactWillDieReceipt() => MatchTestRoom.Wait(Action(true));

        [UnityTest]
        public IEnumerator HideKeepsProgressAndSpendsTheExactDieOnce() => MatchTestRoom.Wait(Action(false));

        private static async Task Action(bool move)
        {
            using var room = await UI10TestRoom.Connect(move ? "ui34-panel-move" : "ui34-panel-hide");
            var board = room.Fp.Board;
            var before = board.View;
            var pan = board.Map.Camera.Pan;
            // UI-232: hiding is the Ring icon's click (on the plaque, or in the panel once it unrolls over the plaque);
            // moving is the track symbol in the panel.
            Button Control() => move ? board.Fellowship.Root.Q<Button>("fellowship-progress")
                : board.Fellowship.IsOpen ? board.Fellowship.PanelRing : board.Fellowship.Ring;
            Assert.IsFalse(board.Fellowship.IsOpen, "UI-98: no decision holds the panel; it unrolls from its plaque.");
            await room.Settle();
            StringAssert.Contains("Select a Character die", board.Fellowship.Root.Q<Label>("fellowship-action-hint").text);
            if (move)
                Assert.IsFalse(Control().enabledInHierarchy);
            else
                Assert.IsFalse(Control().ClassListContains(FellowshipStatus.HideClass), "No die chosen, no hide offer.");
            Assert.AreEqual(0, room.Boundary.Commands);
            var die = before.Dice.Single(d => d.Owner == Side.FreePeoples && d.Face == DieFace.WillOfTheWest).Id;
            Assert.IsTrue(board.OwnDice.Q<Button>(die).ClassListContains("fellowship-cost"));
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            await room.Settle();
            Assert.IsTrue(board.Fellowship.Status.ClassListContains("legal-target"), "The plaque offers the Fellowship action.");
            if (!move)
            {
                Assert.IsTrue(board.Fellowship.Ring.ClassListContains(FellowshipStatus.HideClass), "The Ring icon hides.");
                Assert.IsTrue(board.Fellowship.Ring.ClassListContains(ChoiceGlow.ClassName));
                StringAssert.StartsWith("Click the Ring to hide", board.Fellowship.Ring.tooltip);
                Assert.IsFalse(board.Fellowship.Status.ClassListContains(FellowshipStatus.HideClass), "No separate Hide control.");
                Assert.AreEqual("Revealed", board.Fellowship.Status.Q<Label>("fellowship-status-condition").text);
            }
            // Hovering the corner tab unrolls the full form over it, as a player reaches the panel.
            CardGestures.Move(board.Fellowship.Status);
            await room.Settle();
            Assert.IsTrue(board.Fellowship.IsOpen, "The corner tab unrolls the panel.");
            var action = Control();
            if (move)
            {
                // UI-110: the chosen die lights the progress track's symbol; a click on it moves the Fellowship.
                Assert.AreSame(board.Fellowship.Root.Q("journey-track"), action.parent);
                Assert.IsTrue(action.ClassListContains(FellowshipTrackRow.MoveClass), "The track symbol glows while the die can move.");
                Assert.AreEqual(DisplayStyle.Flex, action.Q(className: "track-move-glow").resolvedStyle.display);
                Assert.IsFalse(board.Fellowship.Root.Query<Button>().ToList().Any(b => b.text.StartsWith("Move")), "No separate Move button.");
                LayoutTests.Save(room.Fp.Viewport, "ui110-move-glow.png");
            }
            // Keyboard focus inside the unrolled panel holds it while the pointer is elsewhere.
            action.Focus();
            await room.Fp.Connection.Refresh();
            await room.Settle();
            Assert.AreSame(action, Control());
            Assert.AreSame(action, action.focusController.focusedElement);
            Assert.AreEqual(die, board.ActionDie.DieId);
            Assert.AreEqual(pan, board.Map.Camera.Pan);

            room.Boundary.HoldNextCommand = true;
            PointerGestureEvents.ClickAt(action);
            await room.Until(() => room.Boundary.CommandHeld);
            await room.At(1);
            Assert.AreEqual(move ? CommandKind.MoveFellowship : CommandKind.HideFellowship, room.Boundary.LastCommand.Kind);
            Assert.AreEqual(die, room.Boundary.LastCommand.Payload.DieId);
            Assert.AreEqual(before.Fellowship.RegionId, board.View.Fellowship.RegionId);
            Assert.AreEqual(before.Fellowship.Progress + (move ? 1 : 0), board.View.Fellowship.Progress);
            Assert.IsTrue(board.View.Fellowship.Hidden);
            Assert.AreEqual(move ? DieLocation.Hunt : DieLocation.Spent, board.View.Dice.Single(d => d.Id == die).Location);
            Assert.AreEqual(1, room.Boundary.Commands);
            if (move)
                Assert.IsFalse(action.enabledInHierarchy);
            Assert.IsFalse(action.ClassListContains(FellowshipTrackRow.MoveClass), "The glow ends with the sent move.");
            Assert.IsFalse(action.ClassListContains(FellowshipStatus.HideClass), "The hide offer ends with the sent hide.");
            room.Boundary.ReleaseCommand();
            await room.Until(() => Recovery(board) != null);
            await room.Settle();
            PointerGestureEvents.ClickAt(Recovery(board));
            await room.Until(() => Recovery(board) == null);
            Assert.Greater(room.Boundary.ReceiptLookups, 0);
            Assert.AreEqual(1, room.Boundary.Commands);
            Assert.AreEqual(1, board.View.Revision);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
        }

        private static Button Recovery(BoardScreen board) => board.Tray.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result");
    }
}
