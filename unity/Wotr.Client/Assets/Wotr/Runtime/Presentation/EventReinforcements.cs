using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-131: a card's EventRecruitment (limits per nation and region, MaximumRegions, DistinctNations).
    public sealed class EventReinforcements : IReinforcementSource
    {
        public bool Owns(PlayerView view, TurnDraft draft) =>
            draft.Kind == CommandKind.ChooseEffect && EventRecruitDraft.Option(view) != null;

        public ReinforcementPlan Build(PlayerView view, TurnDraft draft, BoardPresentation data)
        {
            var option = EventRecruitDraft.Option(view);
            var plan = new ReinforcementPlan
            {
                Prefix = "event",
                Title = ReinforcementPlans.SourceTitle(view),
                Slots = option.Maximum,
                Optional = option.Minimum == 0,
                Ready = EventRecruitDraft.Ready(draft, option),
                Placements = ReinforcementPlans.Placements(view, data, draft.Payload.Placements)
            };
            foreach (var nation in ReinforcementRows.InOrder(option.Recruitment.Select(g => g.Nation)))
            {
                var row = ReinforcementPlans.Row(view, nation, Limits(option, nation, data));
                foreach (var group in option.Recruitment.Where(g => g.Nation == nation))
                    row.Figures.Add(ReinforcementPlans.Figure(view, plan, nation, group.Kind, Free(draft, option, group),
                        Blocked(option, plan, nation)));
                ReinforcementPlans.Close(row);
                plan.Rows.Add(row);
            }
            return plan;
        }

        public bool Choose(TurnDraft draft, PlayerView view, string id) =>
            EventRecruitDraft.Select(draft, EventRecruitDraft.Option(view), id);

        public bool Remove(TurnDraft draft, PlayerView view, string id)
        {
            if (draft.Payload.Placements.All(p => p.UnitId != id))
                return false;
            EventRecruitDraft.Remove(draft, id);
            return true;
        }

        // The first figure of the group that still has a legal region under the card's limits.
        private static string Free(TurnDraft draft, ActionOption option, RecruitmentOption group)
        {
            var id = EventRecruitDraft.Available(draft, group).FirstOrDefault();
            if (id == null)
                return null;
            var probe = draft.Fork();
            probe.ReserveId = id;
            return EventRecruitDraft.Targets(probe, option).Any() ? id : null;
        }

        private static string Blocked(ActionOption option, ReinforcementPlan plan, string nation)
        {
            if (plan.Placements.Count >= option.Maximum)
                return "All reinforcements of this card are planned";
            if (option.EventRecruitment.DistinctNations > 0 && plan.Placements.Any(p => p.Nation == nation))
                return "One figure per nation · " + FigureGrid.Name(nation) + " is planned";
            return "No region left within this card's limits";
        }

        private static string Limits(ActionOption option, string nation, BoardPresentation data)
        {
            var limits = option.EventRecruitment.Limits.Where(l => l.Nation == nation && l.Maximum > 0 && data.Regions.ContainsKey(l.RegionId))
                .GroupBy(l => l.RegionId).Select(g => data.Regions[g.Key].Name + " " + g.Max(l => l.Maximum)).Take(3).ToList();
            var text = limits.Count == 0 ? "" : "Up to " + string.Join(" · ", limits);
            return option.EventRecruitment.DistinctNations > 0 ? "One figure · " + text : text;
        }
    }
}
