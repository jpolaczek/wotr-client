using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-360 (owner's decision D9 of EXP-15, 2026-10-26): every Guide response in the Hunt — Merry's and Pippin's
    // separation, Gollum's reveal, and the expansions' alternates (UI-270) — stands as a response object in the Hunt
    // window's response row, beside the damage responses: the Guide's portrait, its name and what the response does. A
    // click uses it at once (UI-142) and the console's Pass declines (UI-291); the tray lists none of them.
    public static class HuntGuideResponses
    {
        public const string Window = "hunt.guide";
        private static readonly IReadOnlyList<EffectChoiceView> None = new EffectChoiceView[0];

        public static IReadOnlyList<EffectChoiceView> Offered(PlayerView view)
        {
            if (view?.Hunt == null || view.Decision?.Kind != DecisionKind.Reaction || view.Decision.Window != Window ||
                view.DecisionOwner != view.You || view.Winner.HasValue)
                return None;
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.RespondEffect);
            return option == null ? None : option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id)).ToList();
        }

        public static bool InRow(PlayerView view, string id) => Offered(view).Any(c => c.Id == id);

        // The figure the response belongs to: its source companion, else the Guide.
        public static string Figure(PlayerView view, EffectChoiceView choice) =>
            choice.SourceId.Length > 0 ? choice.SourceId : view.Fellowship.Guide;

        public static string Consequence(EffectChoiceView choice) => choice.Title.Length > 0 ? choice.Title : "Guide response";

        public static string Tooltip(PlayerView view, EffectChoiceView choice) =>
            "Click to use " + Owner(view, choice) + "'s response · " + Consequence(choice) +
            (choice.Detail.Length > 0 ? "\n" + choice.Detail : "") + "\nOr Pass to skip it.";

        // The console's hint while the row offers the player's own Guide responses.
        public static string Guidance(PlayerView view, IReadOnlyList<EffectChoiceView> offered)
        {
            if (offered.Count == 1)
                return "Click " + Owner(view, offered[0]) + "'s glowing response in the Hunt window to use " +
                    Consequence(offered[0]) + " · or Pass to skip it.";
            return "Click a glowing Guide response in the Hunt window, or Pass to skip them.";
        }

        private static string Owner(PlayerView view, EffectChoiceView choice) =>
            CharacterLibrary.ShortName(view, Figure(view, choice));
    }
}
