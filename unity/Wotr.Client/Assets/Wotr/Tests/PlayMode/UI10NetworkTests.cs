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
    public sealed class UI10NetworkTests
    {
        [UnityTest]
        public IEnumerator LateReceiptAndHeldEnterNeverSpendOrDrawTwice() => MatchTestRoom.Wait(Receipt());

        [UnityTest]
        public IEnumerator TwoDevicesInvalidateCapturedDeckPressAndNeverReplaceItsSpentCost() => MatchTestRoom.Wait(Race());

        [UnityTest]
        public IEnumerator DelayedViewCannotUndoOpponentActionOrClosePinnedCard() => MatchTestRoom.Wait(LateView());

        private static async Task Receipt()
        {
            using (var room = await UI10TestRoom.Connect("receipt"))
            {
                var firstHand = room.Fp.Board.View.Hand.Count;
                await room.SelectDrawCost(room.Fp);
                room.Boundary.HoldNextCommand = true;
                var deck = room.Fp.Board.Decks.Root.Q<Button>("deck-character");
                UI10TestRoom.Key(room.Fp.Board.Root, true);
                UI10TestRoom.Key(room.Fp.Board.Root, true);
                room.Draw(room.Fp);
                ArmyGestures.Click(deck, 2);
                await room.Until(() => room.Boundary.CommandHeld);
                await room.At(1);
                Assert.AreEqual(1, room.Boundary.Commands);
                await room.DiscardShadowDie();
                await room.At(2);
                UI10TestRoom.Key(room.Fp.Board.Root, true);
                room.Boundary.ReleaseCommand();
                await room.Until(() => Recovery(room.Fp) != null);
                await room.Settle();
                Assert.IsTrue(Recovery(room.Fp).enabledInHierarchy,
                    "A changed draft must not disable recovery of the pending command.");
                room.Boundary.HoldNextView = true;
                CardNetworkTests.Click(Recovery(room.Fp));
                await room.Until(() => room.Boundary.ViewHeld);
                Assert.IsFalse(room.Fp.Board.Tray.enabledInHierarchy,
                    "Receipt recovery stays busy until its final view refresh completes.");
                room.Boundary.ReleaseView();
                await room.Until(() => Recovery(room.Fp) == null && room.Fp.Board.Tray.enabledInHierarchy);
                await room.Settle();
                Assert.AreEqual(2, room.Fp.Board.View.Revision, "Receipt r1 cannot roll back the current r2 view.");
                Assert.AreEqual(1, room.Boundary.Commands, "Recovery must read the committed receipt.");
                Assert.AreEqual(firstHand + 1, room.Fp.Board.View.Hand.Count);
                Assert.AreEqual(1, Spent(room.Fp, Side.FreePeoples));

                await room.SelectDrawCost(room.Fp);
                UI10TestRoom.Key(room.Fp.Board.Root, true);
                await room.Settle();
                Assert.AreEqual(1, room.Boundary.Commands, "A held key cannot submit a different decision.");
                UI10TestRoom.Key(room.Fp.Board.Root, false);
                room.Draw(room.Fp);
                await room.At(3);
                Assert.AreEqual(2, room.Boundary.Commands);
                Assert.AreEqual(firstHand + 2, room.Fp.Board.View.Hand.Count);
                Assert.AreEqual(2, Spent(room.Fp, Side.FreePeoples));
            }
        }

        private static async Task Race()
        {
            using (var room = await UI10TestRoom.Connect("race", secondDevice: true))
            {
                var die = room.Fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id;
                await room.SelectDrawCost(room.Fp, die);
                var deck = room.Fp.Board.Decks.Root.Q<Button>("deck-character");
                PointerGestureEvents.Press(deck);
                room.Boundary.HoldNextView = true;
                await room.SelectDrawCost(room.OtherFp, die);
                room.Draw(room.OtherFp);
                await room.Until(() => room.Boundary.ViewHeld && room.Sh.Board.View?.Revision == 1);
                await room.DiscardShadowDie();
                await room.Until(() => room.OtherFp.Board.View?.Revision == 2 && room.Sh.Board.View?.Revision == 2);

                // An independent GET overtakes the GET held after the real WebSocket notification.
                await room.Fp.Connection.Refresh();
                await room.At(2);
                Assert.IsEmpty(room.Fp.Board.ActionDie.DieId, "A consumed cost must not be replaced.");
                room.Boundary.ReleaseView();
                await room.Settle();
                Assert.AreEqual(2, room.Fp.Board.View.Revision);
                PointerGestureEvents.Release(deck);
                await room.Settle();
                Assert.AreEqual(0, room.Boundary.Commands, "An old captured release must not draw.");
                Assert.IsFalse(deck.HasPointerCapture(PointerId.mousePointerId));

                var replacement = room.Fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id;
                CardNetworkTests.Click(room.Fp.Board.OwnDice.Q<Button>(replacement));
                await CardGestures.Open(room.Fp);
                room.Draw(room.Fp);
                await room.At(3);
                Assert.AreEqual(1, room.Boundary.Commands);
                Assert.AreEqual(replacement, room.Boundary.LastCommand.Payload.DieId);
                Assert.AreEqual("character", room.Boundary.LastCommand.Payload.Deck);
                Assert.AreEqual(2, Spent(room.Fp, Side.FreePeoples));
            }
        }

        private static async Task LateView()
        {
            using (var room = await UI10TestRoom.Connect("late-view"))
            {
                await room.SelectDrawCost(room.Fp);
                room.Draw(room.Fp);
                await room.At(1);
                var card = room.Fp.Board.View.Hand.First();
                room.Fp.Board.Cards.Show(card, true);
                var camera = room.Fp.Board.Map.Camera;
                camera.Pan = new Vector2(-260, -41);
                camera.Zoom = 1.35f;
                room.Fp.Board.Map.Refresh();
                room.Boundary.HoldNextView = true;
                var delayed = room.Fp.Connection.Refresh();
                await room.Until(() => room.Boundary.ViewHeld);
                await room.DiscardShadowDie();
                await room.At(2);
                var latest = room.Fp.Board.View;
                room.Boundary.ReleaseView();
                await delayed;
                await room.Settle();
                Assert.AreSame(latest, room.Fp.Board.View, "A late GET must not apply an obsolete projection.");
                var connected = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                Assert.AreEqual(2, room.Fp.Board.View.Revision);
                Assert.AreEqual(new Vector2(-260, -41), camera.Pan);
                Assert.AreEqual(1.35f, camera.Zoom);
                Assert.IsTrue(room.Fp.Board.Cards.Pinned);
                Assert.AreEqual(card, room.Fp.Board.Cards.CardId);
                Assert.AreEqual(DisplayStyle.Flex, room.Fp.Board.Cards.Root.style.display.value);
            }
        }

        private static int Spent(MountedMatchClient client, Side side) => client.Board.View.Dice
            .Count(d => d.Owner == side && d.Location == DieLocation.Spent);

        private static Button Recovery(MountedMatchClient client) => client.Board.Tray.Query<Button>().ToList()
            .FirstOrDefault(b => b.text == "Check result");
    }
}
