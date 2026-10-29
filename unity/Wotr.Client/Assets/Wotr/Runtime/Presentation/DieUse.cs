using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-373 (owner): a button that spends a die says which die and what it does — "Use your Muster die: Recruit the
    // marked figures." The effect is the server preview's summary (or the action's name before it); its own generic
    // "Spend the selected die" goes, as the die is named in front.
    public static class DieUse
    {
        private static readonly string[] GenericSpends =
        {
            "Spend the selected die · ", "Spend this die · ", "Spend the selected die and ", "Spend this die and "
        };

        // "Muster die", "Will of the West die", "Ruler die (Muster)"; empty without a die.
        public static string Name(PlayerView view, string dieId)
        {
            var die = string.IsNullOrEmpty(dieId) ? null : view?.Dice.FirstOrDefault(d => d.Id == dieId);
            if (die == null)
                return "";
            var face = DieDescriptions.Face(die.Face);
            return die.Source == "ruler" ? "Ruler die (" + face + ")" : face + " die";
        }

        public static string Tooltip(PlayerView view, TurnDraft draft, string effect) =>
            Tooltip(Name(view, draft?.Payload.DieId), effect);

        public static string Tooltip(string die, string effect)
        {
            var rest = WithoutGenericSpend(effect ?? "");
            if (die.Length == 0)
                return rest;
            return rest.Length == 0 ? "Use your " + die + "." : "Use your " + die + ": " + Capital(rest);
        }

        private static string WithoutGenericSpend(string effect)
        {
            foreach (var spend in GenericSpends)
                if (effect.StartsWith(spend, StringComparison.Ordinal))
                    return effect.Substring(spend.Length);
            return effect;
        }

        private static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
