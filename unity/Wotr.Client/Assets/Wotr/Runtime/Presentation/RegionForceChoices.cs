using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-251: when a card's region holds two eligible forces — Dreadful Spells' field army and garrison — the force is
    // chosen on its own section of the region panel, so the region alone never collapses the alternatives. The
    // server's ids name the force ("region:field", "region:garrison"); a single force is the region click itself.
    public static class RegionForceChoices
    {
        public const string Presentation = "Region";

        public static string For(PlayerView view, TurnDraft draft, string region, bool garrison)
        {
            var option = Option(view, draft);
            if (option == null || string.IsNullOrEmpty(region))
                return "";
            var choices = option.EffectChoices.Where(c => c.TargetId == region && option.EligibleIds.Contains(c.Id)).ToList();
            if (choices.Count < 2)
                return "";
            var id = region + (garrison ? ":garrison" : ":field");
            return choices.Any(c => c.Id == id) ? id : "";
        }

        private static ActionOption Option(PlayerView view, TurnDraft draft) =>
            view?.Decision?.Presentation == Presentation && view.DecisionOwner == view.You && draft?.Kind == CommandKind.ChooseEffect
                ? DraftOptions.For(view, draft) : null;
    }
}
