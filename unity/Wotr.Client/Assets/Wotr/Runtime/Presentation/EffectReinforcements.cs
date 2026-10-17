using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-131: a "ReserveFigures" effect choice: each offered figure already has its region, so choosing a token
    // plans it there at once.
    public sealed class EffectReinforcements : IReinforcementSource
    {
        public bool Owns(PlayerView view, TurnDraft draft) => draft.Kind == CommandKind.ChooseEffect &&
            view.Decision?.Presentation == "ReserveFigures" && Option(view) != null;

        public ReinforcementPlan Build(PlayerView view, TurnDraft draft, BoardPresentation data)
        {
            var option = Option(view);
            var plan = new ReinforcementPlan
            {
                Prefix = "effect",
                Hint = "Click to plan it in its region",
                Title = ReinforcementPlans.SourceTitle(view),
                Slots = option.Maximum,
                Optional = option.Minimum == 0,
                Ready = EffectChoiceDraft.Ready(draft, option),
                Placements = ReinforcementPlans.Placements(view, data, EffectChoiceDraft.Placements(view, draft))
            };
            var offered = option.EffectChoices.Select(c => (Choice: c, Unit: view.Units.FirstOrDefault(u => u.Id == c.Id)))
                .Where(c => c.Unit != null).ToList();
            foreach (var nation in ReinforcementRows.InOrder(offered.Select(c => c.Unit.Nation)))
            {
                var mine = offered.Where(c => c.Unit.Nation == nation).ToList();
                var row = ReinforcementPlans.Row(view, nation, "To " + ReinforcementPlans.Regions(data, mine.Select(c => c.Choice.TargetId)));
                foreach (var kind in mine.Select(c => c.Unit.Kind).Distinct())
                {
                    var free = mine.Where(c => c.Unit.Kind == kind && !draft.Payload.Ids.Contains(c.Choice.Id))
                        .Select(c => c.Choice.Id).FirstOrDefault(id => EffectChoiceDraft.CanChoose(draft, option, id));
                    row.Figures.Add(ReinforcementPlans.Figure(view, plan, nation, kind, free, "This card's limit is reached"));
                }
                ReinforcementPlans.Close(row);
                plan.Rows.Add(row);
            }
            return plan;
        }

        public bool Choose(TurnDraft draft, PlayerView view, string id)
        {
            var option = Option(view);
            if (draft.Payload.Ids.Contains(id) || !option.EligibleIds.Contains(id) || !EffectChoiceDraft.CanChoose(draft, option, id))
                return false;
            CardDraft.Choose(draft, option, id);
            return true;
        }

        public bool Remove(TurnDraft draft, PlayerView view, string id)
        {
            if (!draft.Payload.Ids.Contains(id))
                return false;
            draft.Change(p => p.Ids.Remove(id));
            return true;
        }

        private static ActionOption Option(PlayerView view) => view.Options.FirstOrDefault(o =>
            o.Kind == CommandKind.ChooseEffect && o.Enabled && o.EventRecruitment == null && o.EffectChoices.Count > 0);
    }
}
