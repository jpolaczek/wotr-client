using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public sealed class HandDiscardDrawerTests
    {
        [UnityTest]
        public IEnumerator FreePeoplesCanCloseAndReopenExcessHandAcrossRefreshes() =>
            CloseAndReopen(Side.FreePeoples);

        [UnityTest]
        public IEnumerator ShadowCanCloseAndReopenExcessHandAcrossRefreshes() =>
            CloseAndReopen(Side.Shadow);

        private static IEnumerator CloseAndReopen(Side side)
        {
            var view = DiscardView(side);
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Apply(view);
            yield return Settle();
            var board = client.Board;
            var drawer = board.HandDrawer;
            var card = board.Hand.Q<Button>(view.Hand[0]);
            AssertPermanent(board, "Discard 2", 8);
            Assert.IsNull(drawer.Window);
            drawer.Close();
            yield return Settle();
            AssertPermanent(board, "Discard 2", 8);
            client.Refresh(DiscardView(side));
            yield return Settle();
            AssertPermanent(board, "Discard 2", 8);
            client.Apply(DiscardView(side));
            yield return Settle();
            AssertPermanent(board, "Discard 2", 8);
            Assert.AreSame(card, board.Hand.Q<Button>(view.Hand[0]));

            Assert.IsEmpty(board.Cards.CardId);
            Assert.AreEqual(DisplayStyle.None, board.Cards.Root.resolvedStyle.display);
            client.Refresh(DiscardView(side));
            yield return Settle();
            AssertPermanent(board, "Discard 2", 8);

            var next = DiscardView(side, "next-discard", 7);
            next.Revision = 1;
            client.Refresh(next);
            yield return Settle();
            AssertPermanent(board, "Discard 1", 7);
            Assert.AreEqual("Discard 1", drawer.Tab.text);
            Assert.AreSame(card, board.Hand.Q<Button>(view.Hand[0]));
            Assert.AreEqual(7, board.Hand.Query<Button>(className: "hand-card").ToList().Count);
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
        }

        [UnityTest]
        public IEnumerator OpponentDecisionDoesNotOpenTheHandAndNewViewerResetsAttention()
        {
            var view = DiscardView(Side.FreePeoples);
            view.DecisionOwner = Side.Shadow;
            view.Decision = null;
            view.Options.Clear();
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Apply(view);
            yield return Settle();
            AssertPermanent(client.Board, "Cards 8", 8);
            Assert.IsEmpty(client.Board.Hand.Query<Button>(className: "playable-card").ToList(),
                "An opponent decision makes no card in the own hand playable.");
            Assert.AreEqual("Cards 8", client.Board.HandDrawer.Tab.text);

            client.Apply(DiscardView(Side.FreePeoples));
            yield return Settle();
            Assert.IsTrue(client.Board.HandDrawer.IsOpen);
            client.Board.HandDrawer.Close();
            yield return Settle();
            AssertPermanent(client.Board, "Discard 2", 8);
            client.Apply(DiscardView(Side.Shadow));
            yield return Settle();
            Assert.IsTrue(client.Board.HandDrawer.IsOpen, "The own hand remains available after changing viewer.");
            Assert.AreEqual("Discard 2", client.Board.HandDrawer.Tab.text);
            Assert.IsNull(client.Board.Hand.Q<Button>("base.fp.character.01"));
            Assert.IsNotNull(client.Board.Hand.Q<Button>("base.sh.character.01"));
        }

        private static PlayerView DiscardView(Side side, string decision = "discard", int count = 8)
        {
            var view = new BoardFixture().View;
            view.You = side;
            view.DecisionOwner = side;
            view.Decision = new DecisionView
            {
                Id = decision,
                Kind = DecisionKind.DiscardCards,
                RequiredCount = count - 6
            };
            var prefix = side == Side.FreePeoples ? "base.fp.character." : "base.sh.character.";
            view.Hand = Enumerable.Range(1, count).Select(index => prefix + index.ToString("D2")).ToList();
            view.Options.Clear();
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.DiscardCards,
                Minimum = 1,
                Maximum = count - 6,
                EligibleIds = view.Hand.ToList()
            });
            return view;
        }

        private static void AssertPermanent(Wotr.Client.BoardScreen board, string attention, int count)
        {
            Assert.IsTrue(board.HandDrawer.Docked);
            Assert.IsTrue(board.HandDrawer.IsOpen);
            Assert.AreEqual(DisplayStyle.Flex, board.Hand.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, board.HandDrawer.Tab.resolvedStyle.display);
            Assert.AreEqual(attention, board.HandDrawer.Tab.text);
            Assert.AreEqual(count, board.Hand.Query<Button>(className: "hand-card").ToList().Count);
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }
    }
}
