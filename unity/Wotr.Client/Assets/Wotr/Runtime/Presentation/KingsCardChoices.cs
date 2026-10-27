using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-278 (EXP-15 D15, owner's decision): the Kings cards' branches and their Nation step stand on their objects. The
    // nation tokens in the Politics panel take the Nation step (It Is Not So Dark Here, Wisdom's politics) and Wisdom's
    // politics branch; the kings' portraits take Wisdom's inspection and a recruit-or-cleanse card's cleanse; the Reserve's
    // figures of the king's nation take its recruit. A branch's click carries its object into the card's next step
    // (BranchCarry): one click on Gondor's token activates and advances Gondor. The console offers only a branch whose
    // object is not on the board.
    public static class KingsCardChoices
    {
        public const string Branch = "SovereignCardBranch";
        public const string NationStep = "Nation";
        public const string Politics = "kings.wisdom.politics";
        public const string Inspect = "kings.wisdom.inspect";
        public const string Recruit = "recruit";
        public const string Cleanse = "corruption";
        private static readonly IReadOnlyDictionary<string, string> NoNations = new Dictionary<string, string>();
        private static readonly IReadOnlyDictionary<string, SovereignChoice> NoKings = new Dictionary<string, SovereignChoice>();

        // Nation id → what its token's click does now: the Nation step's choice or Wisdom's politics branch.
        public static IReadOnlyDictionary<string, string> Nations(PlayerView view)
        {
            var option = Option(view, NationStep);
            if (option != null)
                return option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id))
                    .ToDictionary(c => c.Id, c => "Click to choose " + c.Title + " for " + Card(view) + " · " + c.Detail);
            option = Option(view, Branch);
            if (option?.EligibleIds.Contains(Politics) != true)
                return NoNations;
            // Under Kings each Free Peoples nation has its king, so the kings name the nations the Nation step offers.
            return view.Sovereigns.Select(s => s.Nation).Where(n => view.Nations.Any(nation => nation.Id == n)).Distinct()
                .ToDictionary(n => n, n => "Click to activate and advance " + FigureGrid.Name(n) + " · " + Card(view));
        }

        // The effect a nation token's click sends now, or "".
        public static string NationEffect(PlayerView view, string nation)
        {
            if (!Nations(view).ContainsKey(nation))
                return "";
            return Option(view, NationStep) != null ? nation : Politics;
        }

        // King id → the branch his portrait's click chooses: Wisdom's inspection or a card's cleanse.
        public static IReadOnlyDictionary<string, SovereignChoice> Kings(PlayerView view)
        {
            var option = Option(view, Branch);
            if (option == null)
                return NoKings;
            var kings = new Dictionary<string, SovereignChoice>();
            if (option.EligibleIds.Contains(Inspect))
                foreach (var king in Inspectable(view))
                    kings[king.Id] = new SovereignChoice
                    {
                        EffectId = Inspect, Tooltip = "Click to look at " + king.Title + "'s Corruption tiles · " + Card(view)
                    };
            var cleanse = option.EligibleIds.Contains(Cleanse) ? option.EffectChoices.FirstOrDefault(c => c.Id == Cleanse) : null;
            if (cleanse != null && view.Sovereigns.FirstOrDefault(s => s.Id == cleanse.TargetId) is { } owner)
                kings[owner.Id] = new SovereignChoice
                {
                    EffectId = Cleanse, Tooltip = "Click to remove corruption from " + owner.Title + " · " + cleanse.Detail
                };
            return kings;
        }

        // The nation whose Reserve figures choose the recruit branch, or "" when its Reserve is empty.
        public static string RecruitNation(PlayerView view)
        {
            var option = Option(view, Branch);
            var recruit = option?.EligibleIds.Contains(Recruit) == true ? option.EffectChoices.FirstOrDefault(c => c.Id == Recruit) : null;
            if (recruit == null || recruit.TargetId.Length == 0)
                return "";
            return view.Units.Any(u => u.Nation == recruit.TargetId && u.Location == "reserve") ? recruit.TargetId : "";
        }

        public static string RecruitHint(PlayerView view) =>
            "Click to recruit with " + Card(view) + " · " + (Option(view, Branch)?.EffectChoices.FirstOrDefault(c => c.Id == Recruit)?.Detail ?? "");

        // This branch stands on its object, so the console leaves it out.
        public static bool OnObject(PlayerView view, string id)
        {
            if (Option(view, Branch) == null)
                return false;
            return id switch
            {
                Politics => Nations(view).Count > 0,
                Inspect or Cleanse => Kings(view).Values.Any(k => k.EffectId == id),
                Recruit => RecruitNation(view).Length > 0,
                _ => false
            };
        }

        // The console's guidance while a branch or the Nation step waits on the objects.
        public static string Hint(PlayerView view)
        {
            if (Option(view, NationStep) != null)
                return "Click a glowing nation token in Politics";
            if (Option(view, Branch) == null)
                return "";
            var parts = new List<string>();
            if (OnObject(view, Recruit))
                parts.Add("a glowing figure of " + FigureGrid.Name(RecruitNation(view)) + " in the Reserve to recruit");
            if (OnObject(view, Politics))
                parts.Add("a glowing nation token to activate and advance it");
            if (OnObject(view, Inspect))
                parts.Add("a glowing king to look at his tiles");
            var cleanse = Kings(view).FirstOrDefault(k => k.Value.EffectId == Cleanse);
            if (cleanse.Value != null)
                parts.Add("the glowing " + view.Sovereigns.First(s => s.Id == cleanse.Key).Title + " to remove his corruption");
            return parts.Count == 0 ? "" : "Click " + string.Join(", or ", parts);
        }

        // Wisdom inspects an Initial king who carries tiles (SelectInspectedSovereignStep).
        private static IEnumerable<SovereignView> Inspectable(PlayerView view) =>
            view.Sovereigns.Where(s => s.Status == SovereignStatus.Initial && s.Tiles.Count > 0);

        private static string Card(PlayerView view) => CardLibrary.Title(view.Decision.SourceId);

        private static ActionOption Option(PlayerView view, string presentation) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == presentation &&
            view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect) : null;
    }
}
