using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-111: why a card in hand cannot be played in this battle. The server decides which cards can be committed
    // (the Combat card option); the reason shown is only the condition printed on the card's Combat part.
    public static class CombatCardCondition
    {
        private const string Fallback = "Not playable in this battle";

        public static HashSet<string> Eligible(PlayerView view) => view?.Decision?.Kind == DecisionKind.CombatCard &&
            view.DecisionOwner == view.You && view.Options.FirstOrDefault(o => o.Kind == CommandKind.CommitCombatCard) is { } option
                ? new HashSet<string>(option.EligibleIds) : null;

        public static string Text(string cardId)
        {
            if (!CardLibrary.Cards.TryGetValue(cardId, out var card) || !card.HasCombat)
                return Fallback;
            var end = card.Combat.IndexOf(". ", System.StringComparison.Ordinal);
            var first = end < 0 ? card.Combat : card.Combat.Substring(0, end + 1);
            return first.StartsWith("Requires", System.StringComparison.Ordinal) ? first : Fallback;
        }
    }
}
