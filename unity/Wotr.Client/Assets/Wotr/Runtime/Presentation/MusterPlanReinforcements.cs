using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-135: a Muster paid with a Kings ruler token or an ability sends its whole plan in one command, so it is
    // planned in the panel like a card: one Elite or two figures in different settlements, x to return one, one ✓.
    public sealed class MusterPlanReinforcements : IReinforcementSource
    {
        public bool Owns(PlayerView view, TurnDraft draft) =>
            draft.Kind == CommandKind.Recruit && !draft.Payload.Stepwise && Option(view) != null;

        public ReinforcementPlan Build(PlayerView view, TurnDraft draft, BoardPresentation data)
        {
            var option = Option(view);
            var plan = new ReinforcementPlan
            {
                Prefix = "muster",
                Title = "Muster",
                Hint = "Click, then click a highlighted settlement · Or drag to it",
                Slots = 2,
                Ready = draft.Ready(option),
                Placements = ReinforcementPlans.Placements(view, data, draft.Payload.Placements)
            };
            foreach (var nation in ReinforcementRows.InOrder(view.Units.Where(u => u.Side == view.You).Select(u => u.Nation)))
            {
                // UI-179: a Muster plan's row carries no political status, like the Muster rows.
                var row = ReinforcementPlans.Row(view, nation, "");
                foreach (var kind in ReinforcementRows.Kinds.Where(k => view.Units.Any(u => u.Nation == nation && u.Kind == k)))
                {
                    var group = option.Recruitment.FirstOrDefault(g => g.Nation == nation && g.Kind == kind);
                    row.Figures.Add(ReinforcementPlans.Figure(view, plan, nation, kind, Free(draft, view, group),
                        Blocked(draft, view, group, nation)));
                }
                ReinforcementPlans.Close(row);
                plan.Rows.Add(row);
            }
            return plan;
        }

        public bool Choose(TurnDraft draft, PlayerView view, string id) => RecruitDraft.Select(draft, view, id);

        public bool Remove(TurnDraft draft, PlayerView view, string id)
        {
            if (draft.Payload.Placements.All(p => p.UnitId != id))
                return false;
            RecruitDraft.Remove(draft, id);
            return true;
        }

        // The first unplanned figure of the group, while a recruitment point and a free settlement remain.
        private static string Free(TurnDraft draft, PlayerView view, RecruitmentOption group)
        {
            var id = group?.ReserveIds.FirstOrDefault(unit => draft.Payload.Placements.All(p => p.UnitId != unit));
            if (id == null || !RecruitDraft.CanAdd(draft, view, group.Kind))
                return null;
            return group.Targets.Any(region => draft.Payload.Placements.All(p => p.RegionId != region)) ? id : null;
        }

        private static string Blocked(TurnDraft draft, PlayerView view, RecruitmentOption group, string nation)
        {
            if (group == null)
                return ReinforcementRows.NationReason(view, nation) is var reason && reason.Length > 0 ? reason : "Not available now";
            if (!RecruitDraft.CanAdd(draft, view, group.Kind))
                return group.Kind == UnitKind.Elite ? "An Elite needs both recruitment points" : "Both recruitment points are planned";
            return "No free settlement left";
        }

        private static ActionOption Option(PlayerView view) =>
            view.Options.FirstOrDefault(o => o.Kind == CommandKind.Recruit && o.Enabled);
    }
}
