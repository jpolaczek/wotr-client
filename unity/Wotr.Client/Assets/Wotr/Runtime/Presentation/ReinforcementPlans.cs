using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-131: card and ability recruitment in the Reinforcements panel. Each source keeps its draft rules; this
    // class only finds the source that owns the current draft.
    public static class ReinforcementPlans
    {
        private static readonly IReinforcementSource[] Sources =
        {
            new EventReinforcements(), new EffectReinforcements(), new CharacterReinforcements(), new MusterPlanReinforcements()
        };

        public static bool Active(PlayerView view, TurnDraft draft) => Source(view, draft) != null;

        public static ReinforcementPlan For(PlayerView view, TurnDraft draft, BoardPresentation data) =>
            Source(view, draft)?.Build(view, draft, data);

        public static bool Choose(TurnDraft draft, PlayerView view, string id) =>
            Source(view, draft)?.Choose(draft, view, id) == true;

        public static bool Remove(TurnDraft draft, PlayerView view, string id) =>
            Source(view, draft)?.Remove(draft, view, id) == true;

        // The × of a map ghost returns every figure planned for that region in one undo step.
        public static bool RemoveRegion(TurnDraft draft, PlayerView view, BoardPresentation data, string region)
        {
            var source = Source(view, draft);
            var planned = source?.Build(view, draft, data).Placements.Where(p => p.RegionId == region).ToList();
            if (planned == null || planned.Count == 0)
                return false;
            draft.Gesture(() =>
            {
                foreach (var placement in planned)
                    source.Remove(draft, view, placement.UnitId);
            });
            return true;
        }

        private static IReinforcementSource Source(PlayerView view, TurnDraft draft) =>
            view?.Decision == null ? null : Sources.FirstOrDefault(s => s.Owns(view, draft));

        internal static string SourceTitle(PlayerView view) =>
            view.Decision?.SourceId.Length > 0 ? CardLibrary.Title(view.Decision.SourceId) : "";

        internal static ReinforcementRow Row(PlayerView view, string nation, string status) =>
            new ReinforcementRow { Nation = nation, Name = FigureGrid.Name(nation), Status = status };

        // A token shows the figures still in reserve after the plan; it is chosen through its first free figure.
        internal static ReinforcementFigure Figure(PlayerView view, ReinforcementPlan plan, string nation, UnitKind kind,
            string id, string blocked)
        {
            var planned = plan.Placements.Count(p => p.Nation == nation && p.Kind == kind);
            var reserve = System.Math.Max(0, ReinforcementRows.InReserve(view, nation, kind) - planned);
            return new ReinforcementFigure
            {
                Kind = kind,
                Reserve = reserve,
                ReserveId = id,
                Reason = id != null ? "" : reserve == 0 ? "No figures left in reserve" : blocked
            };
        }

        // A row whose every token is closed explains why with its first token's reason.
        internal static void Close(ReinforcementRow row)
        {
            row.CanRecruit = row.Figures.Any(f => f.ReserveId != null);
            row.Reason = row.CanRecruit ? "" : row.Figures.Select(f => f.Reason).FirstOrDefault(r => r.Length > 0) ?? "";
        }

        internal static List<PlannedReinforcement> Placements(PlayerView view, BoardPresentation data,
            IEnumerable<UnitPlacementChoice> placements) =>
            placements.Select(p => (Placement: p, Unit: view.Units.FirstOrDefault(u => u.Id == p.UnitId)))
                .Where(p => p.Unit != null)
                .Select(p => new PlannedReinforcement
                {
                    UnitId = p.Placement.UnitId,
                    Nation = p.Unit.Nation,
                    Kind = p.Unit.Kind,
                    RegionId = p.Placement.RegionId,
                    RegionName = data.Regions.TryGetValue(p.Placement.RegionId, out var region) ? region.Name : p.Placement.RegionId
                }).ToList();

        internal static string Regions(BoardPresentation data, IEnumerable<string> regions) =>
            string.Join(", ", regions.Distinct().Where(data.Regions.ContainsKey).Take(3).Select(r => data.Regions[r].Name));
    }
}
