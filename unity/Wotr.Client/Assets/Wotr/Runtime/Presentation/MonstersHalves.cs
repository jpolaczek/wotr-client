using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-431 (owner's rule, UI-430): Monsters Roused's (Fate) two plans stand on their regions instead of two console
    // buttons. The regions of both halves light on the map; a region's click chooses its half and carries the region into
    // the card's next step (BranchCarry): East's Elite destination is that region, West's recruitment plan starts there.
    // The regions are the server's program (FateCards.cs: MonstersWest's quotas; FateMonstersEliteStep.Regions); the
    // branch choices carry no region of their own.
    public static class MonstersHalves
    {
        public const string West = "fate.monsters.west";
        public const string East = "fate.monsters.east";
        // The step each half opens next, which the carried region answers.
        public const string WestStep = "RecruitmentPlan";
        public const string EastStep = "Region";
        private static readonly string[] WestRegions = { "angmar", "ettenmoors", "weather-hills", "trollshaws" };
        private static readonly string[] EastRegions = { "western-mirkwood", "northern-mirkwood", "withered-heath" };
        private static readonly IReadOnlyDictionary<string, string> NoRegions = new Dictionary<string, string>();

        // This half stands on its regions, so the console leaves it out.
        public static bool OnObject(PlayerView view, string id) => (id is West or East) && Offered(view, id);

        // Region → the half its click chooses, for both offered halves.
        public static IReadOnlyDictionary<string, string> Halves(PlayerView view)
        {
            if (!Offered(view, West) && !Offered(view, East))
                return NoRegions;
            var halves = new Dictionary<string, string>();
            if (Offered(view, West))
                foreach (var region in WestRegions)
                    halves[region] = West;
            if (Offered(view, East))
                foreach (var region in EastRegions)
                    halves[region] = East;
            return halves;
        }

        public static IEnumerable<string> Regions(PlayerView view, TurnDraft draft) =>
            draft?.Kind == CommandKind.ChooseEffect ? Halves(view).Keys : Enumerable.Empty<string>();

        // The half a region's click chooses now, or "".
        public static string Half(PlayerView view, string region) => Halves(view).TryGetValue(region, out var half) ? half : "";

        public static string NextStep(string half) => half == East ? EastStep : WestStep;

        // TOOLTIP_AUDIT rule 1, on a lit region's forces: "West of the mountains · Click to recruit there because of Monsters
        // Roused · One Sauron Regular each in Angmar, …".
        public static IReadOnlyDictionary<string, string> Offers(PlayerView view) =>
            Halves(view).ToDictionary(pair => pair.Key, pair => Tooltip(view, pair.Value));

        public static string Tooltip(PlayerView view, string half)
        {
            var choice = Option(view)?.EffectChoices.FirstOrDefault(c => c.Id == half);
            var card = ConfirmSummary.Source(view);
            return TooltipText.Parts(choice?.Title.Length > 0 ? choice.Title : half == West ? "West of the mountains" : "East of the mountains",
                "Click to recruit " + (half == West ? "west" : "east") + " of the mountains because of " + (card.Length > 0 ? card : "this effect"),
                choice?.Detail ?? "");
        }

        // Map terrain has no tooltip (UI-80), so the console's hint says what the lit regions choose, within two lines of
        // the status column at 1366×768.
        public static string Hint(PlayerView view) => Halves(view).Count == 0 ? ""
            : "Click a lit region west or east of the Misty Mountains to recruit on that side";

        private static bool Offered(PlayerView view, string id) => Option(view)?.EligibleIds.Contains(id) == true;

        private static ActionOption Option(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect && o.EffectChoices.Any(c => c.Id is West or East))
                : null;
    }
}
