using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-432 (owner's rule, UI-430): Scouts' Withdraw into siege has no button. Its object is the battle's own region — the
    // stronghold the army withdraws into — lit on the map beside the regions it may retreat to, and its click sends the
    // withdrawal, as a click on the battle region advances after a won battle (BattleContextInteraction). Map terrain has no
    // tooltip (UI-80), so the forces standing there say what the click does and the console's hint names the region.
    public static class ScoutsShelter
    {
        public const string Presentation = "RegionOrShelter";
        public const string Shelter = "scouts.shelter";
        private static readonly IReadOnlyDictionary<string, string> NoOffers = new Dictionary<string, string>();

        // The battle region whose click withdraws into siege now, or "".
        public static string Region(PlayerView view, TurnDraft draft) =>
            Option(view, draft)?.EligibleIds.Contains(Shelter) == true ? view.Battle.RegionId : "";

        // Region → the tooltip its forces lead with (TOOLTIP_AUDIT rule 1): "Withdraw into siege · Click to withdraw into
        // the stronghold because of Scouts · End this battle before the Combat roll."
        public static IReadOnlyDictionary<string, string> Offers(PlayerView view, TurnDraft draft)
        {
            var region = Region(view, draft);
            return region.Length == 0 ? NoOffers : new Dictionary<string, string> { [region] = Tooltip(view, draft) };
        }

        public static string Tooltip(PlayerView view, TurnDraft draft)
        {
            var choice = Option(view, draft)?.EffectChoices.FirstOrDefault(c => c.Id == Shelter);
            return TooltipText.Parts(choice?.Title.Length > 0 ? choice.Title : "Withdraw into siege",
                "Click to withdraw into the stronghold because of " + CombatCard(view), choice?.Detail ?? "");
        }

        // The card's combat side ("Scouts"), as the battle window names it.
        private static string CombatCard(PlayerView view)
        {
            var source = view?.Decision?.SourceId ?? "";
            return CardLibrary.Cards.TryGetValue(source, out var card) && card.CombatTitle.Length > 0 ? card.CombatTitle : "this effect";
        }

        // The console's hint names the battle's region, since its terrain says nothing on hover.
        public static string Hint(PlayerView view, TurnDraft draft, string regionName)
        {
            var option = Option(view, draft);
            if (option == null || !option.EligibleIds.Contains(Shelter))
                return "";
            var withdraw = "Click " + regionName + " to withdraw into its stronghold";
            return option.EligibleIds.Any(id => id != Shelter) ? withdraw + ", or a lit region to retreat" : withdraw;
        }

        private static ActionOption Option(PlayerView view, TurnDraft draft) =>
            view?.Battle != null && view.Decision?.Presentation == Presentation && view.DecisionOwner == view.You &&
            !view.Winner.HasValue && draft?.Kind == CommandKind.ChooseEffect
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect)
                : null;
    }
}
