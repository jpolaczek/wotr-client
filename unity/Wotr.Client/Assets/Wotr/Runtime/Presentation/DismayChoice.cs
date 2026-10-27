using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-276 (EXP-15 D20, owner's decision): after the roll the Shadow spends Dismay by clicking a glowing Ruler die alone;
    // which Dismay token is spent makes no difference (KINGS, Kings/KingsDismay.cs), so the first one offered is picked and
    // named in the die's tooltip and the log. Keeping the tokens is the console's Pass (OptionalDecline, D7).
    public static class DismayChoice
    {
        public static ActionOption Option(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.Dismay && view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.UseDismay) : null;

        // The Dismay token a click on this Ruler die spends, or "".
        public static string Token(PlayerView view, string dieId)
        {
            var option = Option(view);
            return option != null && option.EligibleDice.Contains(dieId) ? option.EligibleIds.FirstOrDefault() ?? "" : "";
        }

        public static string Offer(PlayerView view, string dieId) =>
            "Click to spend " + ExpansionLabels.Token(view, Token(view, dieId)) + "'s Dismay token and re-roll this Ruler die";

        public static void Choose(TurnDraft draft, string dieId, string token)
        {
            draft.Choose(CommandKind.UseDismay, dieId);
            draft.Change(p =>
            {
                p.Ids.Clear();
                p.Ids.Add(token);
            });
        }
    }
}
