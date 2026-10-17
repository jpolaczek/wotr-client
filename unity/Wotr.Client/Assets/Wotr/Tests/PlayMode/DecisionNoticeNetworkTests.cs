using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed class DecisionNoticeNetworkTests
    {
        [UnityTest]
        public IEnumerator IncomingBattleHighlightsWithoutPanningUntilThePlayerOpensItsNotice() => MatchTestRoom.Wait(Battle());

        [UnityTest]
        public IEnumerator ReconnectAtSameRevisionInvalidatesCapturedDeckPress() => MatchTestRoom.Wait(Reconnect());

        private static async Task Battle()
        {
            using var room = await UI10TestRoom.Connect("notice-battle");
            var board = room.Fp.Board;
            var shadow = room.Sh.Board;
            board.Map.Focus("minas-tirith");
            board.Map.Camera.Zoom = .8f;
            board.Map.Refresh();
            var shadowPan = shadow.Map.Camera.Pan;
            ArmyGestures.Die(room.Fp, ArmyGestures.ArmyDie(room.Fp));
            await ArmyGestures.All(room.Fp, "minas-tirith");
            board.Hover.Close();
            MapContourProbe.ReachRegion(board.Map, "lossarnach");
            await room.Settle();
            var pan = board.Map.Camera.Pan;
            var zoom = board.Map.Camera.Zoom;
            MapContourProbe.Click(board.Map, MapContourProbe.Terrain(board.Map, "lossarnach"));
            await room.At(1);
            Assert.AreEqual(DecisionKind.CombatCard, board.View.Decision.Kind);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(zoom, board.Map.Camera.Zoom);
            Assert.IsTrue(board.Battles.IsOpen, "UI-149: a new battle opens its window without moving the camera.");
            Assert.AreEqual("lossarnach", board.Map.AttentionRegion);
            Assert.IsEmpty(shadow.Map.AttentionRegion);
            Assert.AreEqual(DisplayStyle.None, shadow.Notice.Button.style.display.value);
            CardNetworkTests.Click(board.Notice.Button);
            await room.Settle();
            Assert.AreEqual("lossarnach", board.Map.Selected);
            Assert.IsTrue(board.Battles.IsOpen);
            Assert.AreEqual(1, board.View.Revision, "Navigation is not a command.");
            // UI-100: a right click reads a region; returning to the decision closes that read-only window.
            MapContourProbe.Inspect(shadow.Map, MapContourProbe.ExposedTerrain(shadow.Map, out _));
            Assert.AreEqual(DisplayStyle.Flex, shadow.Information.Root.style.display.value);
            CardNetworkTests.Click(board.Tray.Q<Button>("battle-no-card"));
            await room.At(2);
            Assert.AreEqual(shadowPan, shadow.Map.Camera.Pan);
            Assert.IsTrue(shadow.Battles.IsOpen, "UI-149: the defender's window opened with the battle.");
            Assert.AreEqual("lossarnach", shadow.Map.AttentionRegion);
            CardNetworkTests.Click(shadow.Notice.Button);
            Assert.AreEqual("lossarnach", shadow.Map.Selected);
            Assert.IsTrue(shadow.Battles.IsOpen);
            Assert.AreEqual(DisplayStyle.None, shadow.Information.Root.style.display.value);
            Assert.IsNull(shadow.View.Battle.Attacker.CardId);
        }

        private static async Task Reconnect()
        {
            using var room = await UI10TestRoom.Connect("reconnect-press");
            await room.SelectDrawCost(room.Fp);
            var die = room.Fp.Board.ActionDie.DieId;
            var deck = room.Fp.Board.Decks.Root.Q<Button>("deck-character");
            PointerGestureEvents.Press(deck);
            Assert.IsTrue(deck.HasPointerCapture(PointerId.mousePointerId));
            var connected = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > connected);
            await room.Settle();
            Assert.AreEqual(0, room.Fp.Board.View.Revision);
            Assert.AreEqual(die, room.Fp.Board.ActionDie.DieId);
            PointerGestureEvents.Release(deck);
            await room.Settle();
            Assert.AreEqual(0, room.Boundary.Commands);
            Assert.IsFalse(deck.HasPointerCapture(PointerId.mousePointerId));
            room.Draw(room.Fp);
            await room.At(1);
            Assert.AreEqual(1, room.Boundary.Commands);
            Assert.AreEqual(die, room.Boundary.LastCommand.Payload.DieId);
        }
    }
}
