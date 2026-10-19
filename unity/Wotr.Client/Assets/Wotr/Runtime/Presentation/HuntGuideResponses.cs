using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-270: the alternate companions' Guide responses in the Hunt (Council of Rivendell: Boromir, Gimli and Legolas
    // separate and reduce the damage, both alternate Hobbits cancel the tile; Fate of Erebor: Gimli, Dwarven Lord, leaves
    // as the Guide; rules T26). They stand as response objects in the Hunt window's response row, beside the damage
    // responses; a click uses one, Skip declines. The base Guide responses keep their place.
    public static class HuntGuideResponses
    {
        public const string Window = "hunt.guide";

        public static IReadOnlyList<EffectChoiceView> Offered(PlayerView view)
        {
            if (view?.Hunt == null || view.Decision?.Kind != DecisionKind.Reaction || view.Decision.Window != Window ||
                view.DecisionOwner != view.You || view.Winner.HasValue)
                return new EffectChoiceView[0];
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.RespondEffect);
            return option == null ? new EffectChoiceView[0]
                : option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id) && Alternate(c.Id)).ToList();
        }

        public static bool InRow(PlayerView view, string id) => Alternate(id) && Offered(view).Any(c => c.Id == id);

        public static bool Alternate(string id) => id.StartsWith("council.") || id == "fate.gimli-guide";

        // The figure the response belongs to: its source companion, else the Guide.
        public static string Figure(PlayerView view, EffectChoiceView choice) =>
            choice.SourceId.Length > 0 ? choice.SourceId : view.Fellowship.Guide;

        public static string Tooltip(PlayerView view, EffectChoiceView choice) =>
            "Click to use " + CharacterLibrary.ShortName(view, Figure(view, choice)) + "'s response · " +
            (choice.Title.Length > 0 ? choice.Title : "Guide response") + (choice.Detail.Length > 0 ? "\n" + choice.Detail : "") +
            "\nOr Skip to continue without it.";
    }
}
