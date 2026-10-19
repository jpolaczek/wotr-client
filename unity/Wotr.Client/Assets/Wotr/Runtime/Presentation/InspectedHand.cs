using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-272 (CARD_CHOICES_UX): Wind from the West shows the Free Peoples the Shadow's hand. Its cards stand in the
    // player's hand drawer, marked to discard (UI-222), and the click on one discards it from the Shadow's hand.
    public static class InspectedHand
    {
        public const string Presentation = "InspectedHand";
        private static readonly IReadOnlyList<string> None = new string[0];

        public static IReadOnlyList<string> Cards(PlayerView view)
        {
            if (view?.Decision?.Kind != DecisionKind.EffectChoice || view.Decision.Presentation != Presentation ||
                view.DecisionOwner != view.You || view.Winner.HasValue)
                return None;
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect);
            return option?.EffectChoices.Select(c => c.Id).Where(option.EligibleIds.Contains).ToList() ?? None;
        }

        public static bool Contains(PlayerView view, string id) => Cards(view).Contains(id);
    }
}
