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
        // UI-271: what this token's click does when it differs from the panel's hint (Gothmog's ability).
        public string Hint = "";
    }

    public sealed class ReinforcementRow
    {
        // UI-271: a row that is not a nation's own (Gothmog's) has its own key; a nation's row is keyed by the nation.
        public string Key = "";
        public string Id => Key.Length > 0 ? Key : Nation;
        public string Nation = "";
        public string Name = "";
        // A card plan's limits for the nation (UI-131); UI-179: never the nation's political status.
        public string Status = "";
        public bool CanRecruit;
        public string Reason = "";
        public List<ReinforcementFigure> Figures = new List<ReinforcementFigure>();
    }

    // UI-130: one row per own nation of the Reinforcements tab. Legality comes from the server's Recruit option;
    // the explanation of a closed nation, settlement or Elite from its "recruit" availability entry (UI-129).
    // Without that entry (an older server) the row stays silent about the reason. Without an option (UI-179)
    // the rows only show the reserves.
    public static class ReinforcementRows
    {
        private static readonly string[] Order = { "dwarves", "elves", "gondor", "north", "rohan", "isengard", "sauron", "southrons" };
        public static readonly UnitKind[] Kinds = { UnitKind.Regular, UnitKind.Elite, UnitKind.Leader, UnitKind.Nazgul };

        public static List<ReinforcementRow> For(PlayerView view, ActionOption recruit) =>
            InOrder(view.Units.Where(u => u.Side == view.You).Select(u => u.Nation)).Select(nation => Row(view, recruit, nation)).ToList();

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

        private static ReinforcementRow Row(PlayerView view, ActionOption recruit, string nation)
        {
            var entry = Entry(view, nation);
            var groups = recruit?.Recruitment.Where(g => g.Nation == nation).ToList() ?? new List<RecruitmentOption>();
            var row = new ReinforcementRow
            {
                Nation = nation,
                Name = FigureGrid.Name(nation),
                CanRecruit = groups.Count > 0
            };
            row.Reason = row.CanRecruit ? "" : Reason(entry);
            foreach (var kind in Kinds.Where(k => view.Units.Any(u => u.Side == view.You && u.Nation == nation && u.Kind == k)))
                row.Figures.Add(Figure(view, entry, groups, row, nation, kind));
            return row;
        }

        private static ReinforcementFigure Figure(PlayerView view, ActionAvailabilityView entry, List<RecruitmentOption> groups,
            ReinforcementRow row, string nation, UnitKind kind)
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
            if (figure.ReserveId != null && groups.Where(g => g.Kind == kind).SelectMany(g => g.Targets)
                    .Any(region => MusterActions.OrphanLeader(view, kind, region)))
                figure.Warning = OrphanWarning;
            return figure;
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
