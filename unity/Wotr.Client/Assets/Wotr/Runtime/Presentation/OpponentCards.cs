using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-361 (owner, 2026-10-26): what the rules make public about a side's cards, for its card counter's tooltip — the
    // hand's size and its split by deck (the Character and Strategy decks' backs differ), the decks' remaining cards and
    // the discard pile. No card is named.
    public static class OpponentCards
    {
        private static readonly string[] StandardDecks = { "character", "strategy" };

        public static string Tooltip(PlayerSummary player, string owner)
        {
            var lines = new List<string> { owner + " · " + player.HandCount + (player.HandCount == 1 ? " card" : " cards") + " in hand" };
            // A server older than Contracts 0.12.0 sends no split; the total stands alone then.
            if (player.HandCount > 0 && player.HandDeckCounts.Count > 0)
                lines.Add(Split(player.HandDeckCounts));
            if (player.DeckCounts.Count > 0)
                lines.Add("Decks: " + Split(player.DeckCounts));
            lines.Add("Discard pile: " + player.DiscardCount);
            return string.Join("\n", lines);
        }

        // The standard decks first, a zero included, then any other deck of an expansion.
        private static string Split(IReadOnlyDictionary<string, int> counts)
        {
            var decks = StandardDecks.Concat(counts.Keys.Where(key => !StandardDecks.Contains(key)).OrderBy(key => key));
            return string.Join(" · ", decks.Select(deck => Name(deck) + " " + (counts.TryGetValue(deck, out var count) ? count : 0)));
        }

        private static string Name(string deck) => deck.Length == 0 ? deck : char.ToUpperInvariant(deck[0]) + deck.Substring(1);
    }
}
