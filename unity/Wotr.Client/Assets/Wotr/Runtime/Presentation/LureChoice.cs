using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-431 (owner's rule, UI-430): Lure of the Ring's Free Peoples choice stands on its objects instead of two console
    // buttons. The companion the server drew glows red on its Fellowship portrait (a click eliminates it) and the
    // Fellowship's marker on the corruption track is offered (a click takes the companion's Level in Corruption). The
    // Fellowship panel opens for the choice. Server: LureChoiceStep (lure.eliminate, lure.corruption = +Level).
    public static class LureChoice
    {
        public const string Presentation = "LureCompanion";
        public const string Eliminate = "lure.eliminate";
        public const string Corruption = "lure.corruption";
        // The red rim of a portrait whose click eliminates its companion (Fellowship.uss).
        public const string EliminateClass = "eliminate-target";

        public static bool Active(PlayerView view) => Option(view) != null;

        // Both choices stand on the Fellowship panel's objects while the companion's portrait is there.
        public static bool OnObject(PlayerView view, string id) => (id is Eliminate or Corruption) && Offered(view, id) &&
            view.OwnCompanions.Any(c => c.Id == Companion(view));

        // The companion the Lure concerns, or "".
        public static string Companion(PlayerView view) => Option(view)?.EffectChoices.FirstOrDefault(c => c.SourceId.Length > 0)?.SourceId ?? "";

        public static bool Eliminates(PlayerView view, string companion) => Offered(view, Eliminate) && companion.Length > 0 && companion == Companion(view);

        public static bool CorruptionOffered(PlayerView view) => Offered(view, Corruption);

        // TOOLTIP_AUDIT rule 1: "Eliminate · Click to eliminate Gimli because of Lure of the Ring · Gimli: eliminate this Companion; …".
        public static string EliminateTooltip(PlayerView view) => TooltipText.Parts(Title(view, Eliminate, "Eliminate"),
            "Click to eliminate " + Name(view) + " because of " + Card(view), Detail(view, Eliminate));

        // "Corruption +2 · Click to keep Gimli and take 2 Corruption because of Lure of the Ring · Gimli: keep this Companion …".
        public static string CorruptionTooltip(PlayerView view)
        {
            var title = Title(view, Corruption, "Corruption");
            var amount = title.Contains("+") ? title.Substring(title.IndexOf('+') + 1).Trim() : "";
            var take = amount.Length > 0 ? "take " + amount + " Corruption" : "take Corruption";
            return TooltipText.Parts(title, "Click to keep " + Name(view) + " and " + take + " because of " + Card(view), Detail(view, Corruption));
        }

        // The console's hint names the two objects.
        public static string Hint(PlayerView view)
        {
            if (!Active(view) || Companion(view).Length == 0)
                return "";
            var name = Name(view);
            return "Click " + name + "'s red portrait to eliminate " + name + ", or the Fellowship's corruption marker to take the Corruption";
        }

        private static string Name(PlayerView view) => CharacterLibrary.ShortName(view, Companion(view));

        private static string Title(PlayerView view, string id, string fallback)
        {
            var title = Option(view)?.EffectChoices.FirstOrDefault(c => c.Id == id)?.Title ?? "";
            return title.Length > 0 ? title : fallback;
        }

        private static string Detail(PlayerView view, string id) => Option(view)?.EffectChoices.FirstOrDefault(c => c.Id == id)?.Detail ?? "";

        private static string Card(PlayerView view)
        {
            var card = ConfirmSummary.Source(view);
            return card.Length > 0 ? card : "this effect";
        }

        private static bool Offered(PlayerView view, string id) => Option(view)?.EligibleIds.Contains(id) == true;

        private static ActionOption Option(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == Presentation &&
            view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect) : null;
    }
}
