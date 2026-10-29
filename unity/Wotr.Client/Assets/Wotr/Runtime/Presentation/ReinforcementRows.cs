using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ReinforcementFigure
    {
        public UnitKind Kind;
        public int Reserve;
        // The first reserve figure of this kind the server lets recruit now; null when none can be.
        public string ReserveId;
        public string Reason = "";
        // UI-135: a legal recruit with a known consequence, such as a leader removed where no army stands.
        public string Warning = "";
        // UI-412: what this token's click does when it differs from the panel's hint.
        public string Hint = "";
        // UI-412: recruitable, but not in the settlement in focus: the token stays choosable and unlit.
        public bool Elsewhere;
    }

    public sealed class ReinforcementRow
    {
        public string Nation = "";
        public string Name = "";
        // A card plan's limits for the nation (UI-131); UI-179: never the nation's political status.
        public string Status = "";
        public bool CanRecruit;
        // UI-412: the nation recruits, but not in the settlement in focus: its row is not lit.
        public bool Elsewhere;
        public string Reason = "";
        public List<ReinforcementFigure> Figures = new List<ReinforcementFigure>();
    }

    // UI-412: the settlement whose click narrows a Muster recruitment's Reserve, with its name.
    public readonly struct RecruitFocus
    {
        public static readonly RecruitFocus None = new RecruitFocus("", "");
        public readonly string Region;
        public readonly string Name;

        public RecruitFocus(string region, string name)
        {
            Region = region ?? "";
            Name = name ?? "";
        }

        public bool Active => Region.Length > 0;
    }

    // UI-130: one row per own nation of the Reinforcements tab. Legality comes from the server's Recruit option;
    // the explanation of a closed nation, settlement or Elite from its "recruit" availability entry (UI-129).
    // Without that entry (an older server) the row stays silent about the reason. Without an option (UI-179)
    // the rows only show the reserves.
    public static class ReinforcementRows
    {
        private static readonly string[] Order = { "dwarves", "elves", "gondor", "north", "rohan", "isengard", "sauron", "southrons" };
        public static readonly UnitKind[] Kinds = { UnitKind.Regular, UnitKind.Elite, UnitKind.Leader, UnitKind.Nazgul };

        public static List<ReinforcementRow> For(PlayerView view, ActionOption recruit) => For(view, recruit, RecruitFocus.None);

        // UI-412 (owner): with a settlement in focus only the figures it can take are lit; the others stay choosable and
        // say why they are not lit.
        public static List<ReinforcementRow> For(PlayerView view, ActionOption recruit, RecruitFocus focus) =>
            InOrder(view.Units.Where(u => u.Side == view.You).Select(u => u.Nation)).Select(nation => Row(view, recruit, nation, focus)).ToList();

        // Free Peoples first in their printed order, then the Shadow nations.
        public static List<string> InOrder(IEnumerable<string> nations) => nations.Distinct()
            .OrderBy(n => System.Array.IndexOf(Order, n) is var index && index < 0 ? int.MaxValue : index).ToList();

        public static int InReserve(PlayerView view, string nation, UnitKind kind) =>
            view.Units.Count(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve");

        public static (int Spent, int Total) Budget(PlayerView view) =>
            view.ActionSequence is { Kind: CommandKind.Recruit } sequence ? (2 - sequence.Remaining, 2) : (0, 2);

        public const string OrphanWarning = "A leader recruited where no army stands is removed";

        public static ActionAvailabilityView Entry(PlayerView view, string nation) =>
            view.Availability.FirstOrDefault(a => a.ActionId == "recruit" && a.SubjectId == nation);

        // The server's words for a settlement that cannot take a reinforcement now, or "" when it is open or unknown.
        public static string SettlementReason(PlayerView view, string nation, string region)
        {
            var entry = Entry(view, nation);
            if (entry == null)
                return "";
            var war = Condition(entry, "at-war");
            if (war != null && !war.Met)
                return war.Description;
            var settlement = entry.Conditions.FirstOrDefault(c => c.Code == "settlement:" + region || c.Code.StartsWith("settlement:" + region + ":"));
            return settlement != null && !settlement.Met ? settlement.Description : "";
        }

        // UI-130: why a reserve of this kind cannot go to this settlement, in the server's words when it sent them.
        // UI-135: the tray says it when a chosen reserve is clicked onto such a settlement.
        public static string Refusal(PlayerView view, string nation, string region, UnitKind kind)
        {
            var entry = Entry(view, nation);
            var budget = entry == null ? null : Condition(entry, "elite-budget");
            if (kind == UnitKind.Elite && budget != null && !budget.Met)
                return budget.Description;
            var reason = SettlementReason(view, nation, region);
            return reason.Length > 0 ? reason : "Not available here now";
        }

        private static ReinforcementRow Row(PlayerView view, ActionOption recruit, string nation, RecruitFocus focus)
        {
            var entry = Entry(view, nation);
            var groups = recruit?.Recruitment.Where(g => g.Nation == nation).ToList() ?? new List<RecruitmentOption>();
            var here = focus.Active ? groups.Where(g => g.Targets.Contains(focus.Region)).ToList() : groups;
            var row = new ReinforcementRow
            {
                Nation = nation,
                Name = FigureGrid.Name(nation),
                CanRecruit = groups.Count > 0,
                Elsewhere = groups.Count > 0 && here.Count == 0
            };
            row.Reason = !row.CanRecruit ? Reason(entry) : row.Elsewhere ? Elsewhere(view, nation, focus) : "";
            foreach (var kind in Kinds.Where(k => view.Units.Any(u => u.Side == view.You && u.Nation == nation && u.Kind == k)))
                row.Figures.Add(Figure(view, entry, groups, here, row, nation, kind, focus));
            return row;
        }

        private static ReinforcementFigure Figure(PlayerView view, ActionAvailabilityView entry, List<RecruitmentOption> groups,
            List<RecruitmentOption> here, ReinforcementRow row, string nation, UnitKind kind, RecruitFocus focus)
        {
            var figure = new ReinforcementFigure
            {
                Kind = kind,
                Reserve = InReserve(view, nation, kind),
                ReserveId = groups.FirstOrDefault(g => g.Kind == kind)?.ReserveIds.FirstOrDefault()
            };
            var budget = entry == null ? null : Condition(entry, "elite-budget");
            figure.Reason = figure.Reserve == 0 ? "No figures left in reserve"
                : figure.ReserveId != null ? ""
                : kind == UnitKind.Elite && budget != null && !budget.Met ? budget.Description
                : row.Reason;
            figure.Elsewhere = figure.ReserveId != null && here.All(g => g.Kind != kind);
            if (figure.Elsewhere)
                figure.Hint = Elsewhere(view, nation, focus) + " · Click it to light the settlements that take it";
            if (figure.ReserveId != null && groups.Where(g => g.Kind == kind).SelectMany(g => g.Targets)
                    .Any(region => MusterActions.OrphanLeader(view, kind, region)))
                figure.Warning = OrphanWarning;
            return figure;
        }

        // A figure or nation the die can recruit elsewhere, but not in the settlement in focus: the server's words for a
        // closed settlement, otherwise that it cannot go there.
        private static string Elsewhere(PlayerView view, string nation, RecruitFocus focus)
        {
            var reason = SettlementReason(view, nation, focus.Region);
            return reason.Length > 0 ? reason : "Cannot be recruited in " + focus.Name + " now";
        }

        public static string NationReason(PlayerView view, string nation) => Reason(Entry(view, nation));

        // The rule the server named: war status first, then the reserve, then the closed settlements.
        private static string Reason(ActionAvailabilityView entry)
        {
            if (entry == null)
                return "";
            foreach (var code in new[] { "at-war", "reserves" })
            {
                var condition = Condition(entry, code);
                if (condition != null && !condition.Met)
                    return condition.Description;
            }
            return string.Join(" · ", entry.Conditions.Where(c => c.Code.StartsWith("settlement:") && !c.Met)
                .Select(c => c.Description).Take(3));
        }

        private static AvailabilityConditionView Condition(ActionAvailabilityView entry, string code) =>
            entry.Conditions.FirstOrDefault(c => c.Code == code);
    }
}
