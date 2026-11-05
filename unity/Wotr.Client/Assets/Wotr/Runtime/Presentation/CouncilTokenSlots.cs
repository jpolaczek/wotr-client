using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum CouncilTokenState { Missing, Offered, Ready, Usable, Spent }

    // UI-270: the Shadow's two Council of Rivendell Action Tokens (LORDS p. 31, rules T26) stand in fixed slots of its
    // dice pool in a Council game, Politics then Travel: not received, offered by the Council's choice, ready, usable now,
    // or spent. A click on an offered token takes it; a click on a usable token starts its use.
    public static class CouncilTokenSlots
    {
        public static readonly IReadOnlyList<string> Ids = new[] { ExpansionDraft.CouncilPolitics, ExpansionDraft.CouncilTravel };

        public static bool Shown(PlayerView view, Side side) =>
            side == Side.Shadow && (view.Configuration.Council || view.CouncilTokens.Count > 0);

        // The Council's token choice (one companion left for its homeland) offers this token now.
        public static bool Offered(PlayerView view, string id) => view?.Decision?.Kind == DecisionKind.CouncilToken &&
            view.DecisionOwner == view.You && !view.Winner.HasValue &&
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.ChooseCouncilToken && o.EligibleIds.Contains(id));

        public static CouncilTokenState State(PlayerView view, string id)
        {
            if (Offered(view, id))
                return CouncilTokenState.Offered;
            var token = view.CouncilTokens.FirstOrDefault(t => t.Id == id);
            if (token == null)
                return CouncilTokenState.Missing;
            if (token.Spent)
                return CouncilTokenState.Spent;
            return ResourcePresentation.CanSelectCouncil(view, id) ? CouncilTokenState.Usable : CouncilTokenState.Ready;
        }

        public static string Key(string id) => id == ExpansionDraft.CouncilPolitics ? "politics" : "travel";

        public static string Tooltip(PlayerView view, string id)
        {
            var name = ExpansionLabels.CouncilToken(id);
            var use = id == ExpansionDraft.CouncilPolitics ? "advance a Shadow nation politically, without a die"
                : "move Nazgûl and Minions, without a die";
            switch (State(view, id))
            {
                case CouncilTokenState.Offered:
                    return "Click to take " + name + " · Later, in an action, " + use + ".";
                case CouncilTokenState.Usable:
                    return ResourcePresentation.WithExplanation(view, id, "Click to use " + name + " · " + Capital(use) + ".");
                case CouncilTokenState.Ready:
                    var token = view.CouncilTokens.First(t => t.Id == id);
                    return ResourcePresentation.WithExplanation(view, id, name + " · " + Capital(use) + "." +
                        (token.UsedTokenThisTurn ? "\nA Council token was already used this turn." : ""));
                case CouncilTokenState.Spent:
                    return name + " · Spent";
                default:
                    return name + " · Not received in the Council of Rivendell";
            }
        }

        private static string Capital(string text) => char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
