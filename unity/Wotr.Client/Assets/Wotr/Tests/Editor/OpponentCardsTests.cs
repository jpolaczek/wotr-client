using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-361 (owner, 2026-10-26): the opponent's card counter says what the rules make public about the cards — the hand's
    // size and split by deck, the decks and the discard pile — and never names a card.
    public sealed class OpponentCardsTests
    {
        [Test]
        public void TheTooltipNamesTheHandsDecksTheDecksAndTheDiscardPile()
        {
            var player = Shadow();
            Assert.AreEqual("Shadow · 5 cards in hand\nCharacter 3 · Strategy 2\nDecks: Character 18 · Strategy 21\nDiscard pile: 4",
                OpponentCards.Tooltip(player, "Shadow"));
        }

        [Test]
        public void AnOlderServersTotalStandsAloneAndAnEmptyDeckReadsZero()
        {
            var player = new PlayerSummary { Side = Side.FreePeoples, HandCount = 1, DeckCounts = { ["character"] = 20, ["strategy"] = 0 } };
            Assert.AreEqual("Free Peoples · 1 card in hand\nDecks: Character 20 · Strategy 0\nDiscard pile: 0",
                OpponentCards.Tooltip(player, "Free Peoples"));
            player.HandDeckCounts["strategy"] = 1;
            StringAssert.Contains("\nCharacter 0 · Strategy 1\n", OpponentCards.Tooltip(player, "Free Peoples"));
        }

        [Test]
        public void TheTopBarsCardCounterCarriesTheTooltip()
        {
            var view = new PlayerView { You = Side.FreePeoples, Players = { Shadow() } };
            var summary = new OpponentSummary();
            summary.Apply(view, Side.Shadow);
            var chip = summary.Root.Q("opponent-cards");
            Assert.AreEqual(OpponentCards.Tooltip(view.Players[0], "Shadow"), chip.tooltip);
            Assert.AreEqual("5", chip.Q<Label>(className: "summary-value").text);
            StringAssert.StartsWith("Shadow · Elven Rings: ", summary.Root.Q("opponent-rings").tooltip);
        }

        private static PlayerSummary Shadow() => new PlayerSummary
        {
            Side = Side.Shadow, HandCount = 5, DiscardCount = 4,
            HandDeckCounts = { ["character"] = 3, ["strategy"] = 2 },
            DeckCounts = { ["character"] = 18, ["strategy"] = 21 }
        };
    }
}
