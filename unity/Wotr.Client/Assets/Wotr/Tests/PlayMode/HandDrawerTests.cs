using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public sealed class HandDrawerTests
    {
        [UnityTest]
        public IEnumerator KeyboardEscapeAndPointerExitKeepSelectionAndPinnedPreviewIndependent()
        {
            var view = new BoardFixture().View;
            const string id = "base.fp.strategy.01";
            view.Hand.Add(id);
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Apply(view);
            yield return Settle();
            var board = client.Board;
            var card = board.Hand.Q<Button>(id);
            Assert.IsTrue(board.HandDrawer.Docked && board.HandDrawer.IsOpen);
            Assert.AreEqual(DisplayStyle.None, board.HandDrawer.Tab.resolvedStyle.display);
            card.Focus();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(id, board.ExpandedCard);
            Assert.IsEmpty(board.Cards.CardId);
            using (var submit = NavigationSubmitEvent.GetPooled())
                card.SendEvent(submit);
            yield return Settle();
            Assert.IsFalse(card.ClassListContains("playable-card"), "No die was selected for this card.");
            Assert.IsEmpty(board.Cards.CardId);

            // A separately opened card remains independent of the permanent hand.
            board.Cards.Show(id, true);
            yield return Settle();
            var close = board.Cards.Root.Q<Button>("close-card");
            close.Focus();
            yield return Settle();
            Assert.AreSame(close, close.panel.focusController.focusedElement);
            BoardMenuLayoutTests.Key(close, true);
            BoardMenuLayoutTests.Key(board.Root, false);
            yield return Settle();
            Assert.IsTrue(board.HandDrawer.IsOpen);
            Assert.IsEmpty(board.Cards.CardId);
            Assert.IsFalse(board.Menu.IsOpen);
            Assert.AreSame(card, board.Hand.Q<Button>(id));
            client.Apply(view);
            yield return Settle();
            Assert.IsTrue(board.HandDrawer.IsOpen);
            Assert.AreSame(card, board.Hand.Q<Button>(id));
            CardGestures.Move(board.Phase);
            yield return new WaitForSecondsRealtime(.25f);
            LeaveSurface(board.Root);
            yield return Settle();
            Assert.IsTrue(board.HandDrawer.IsOpen, "Leaving the application keeps the permanent hand visible.");
            board.HandDrawer.Close();
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Hand.resolvedStyle.display);
            Assert.AreEqual(0, board.View.Revision);
            Assert.IsEmpty(board.ActionDie.DieId);
        }

        private static void LeaveSurface(VisualElement surface)
        {
            using var move = PointerMoveEvent.GetPooled(new Event
            {
                type = EventType.MouseMove,
                mousePosition = surface.worldBound.max + Vector2.one * 20
            });
            surface.panel.visualTree.SendEvent(move);
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 4; i++)
                yield return null;
        }
    }
}
