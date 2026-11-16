using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-131: recruitment by a character: a Kings ruler's muster (RulerRecruitment) or the optional figure that
    // joins the Balrog on entry.
    public sealed class CharacterReinforcements : IReinforcementSource
    {
        public bool Owns(PlayerView view, TurnDraft draft) => Choice(view, draft) != null;

        public ReinforcementPlan Build(PlayerView view, TurnDraft draft, BoardPresentation data)
        {
            var choice = Choice(view, draft);
            var plan = new ReinforcementPlan
            {
                Prefix = "character",
                Title = choice.Title,
                Slots = choice.RulerRecruitment == null ? 1 : choice.TargetCount,
                Optional = choice.RulerRecruitment == null,
                Ready = CharacterRecruitDraft.Ready(draft, choice),
                Placements = ReinforcementPlans.Placements(view, data, draft.Payload.Placements)
            };
            // UI-539 (owner, 2026-11-16): a ruler's recruitment is figure first like every other.
            if (choice.RulerRecruitment != null)
                plan.Hint = RulerHint;
            foreach (var nation in ReinforcementRows.InOrder(choice.Recruitment.Select(g => g.Nation)))
            {
                var groups = choice.Recruitment.Where(g => g.Nation == nation).ToList();
                var settlements = choice.RulerRecruitment?.Settlements ?? groups.SelectMany(g => g.Targets).ToList();
                var row = ReinforcementPlans.Row(view, nation, (plan.Optional ? "Optional · " : "") +
                    ReinforcementPlans.Regions(data, settlements));
                foreach (var group in groups)
                    row.Figures.Add(Figure(view, draft, choice, plan, group));
                ReinforcementPlans.Close(row);
                plan.Rows.Add(row);
            }
            return plan;
        }

        public const string RulerHint = "Click, then click a highlighted settlement · Or drag it to one";

        // A token is chosen through its first free figure that still has a settlement.
        private static ReinforcementFigure Figure(PlayerView view, TurnDraft draft, CharacterActionOption choice, ReinforcementPlan plan,
            RecruitmentOption group)
        {
            var free = group.ReserveIds.FirstOrDefault(unit =>
                group.Targets.Any(region => CharacterRecruitDraft.CanPlace(draft, choice, unit, region)));
            return ReinforcementPlans.Figure(view, plan, group.Nation, group.Kind, free,
                plan.Placements.Count >= plan.Slots ? "All reinforcements of this ability are planned" : "No settlement left for this figure");
        }

        public bool Choose(TurnDraft draft, PlayerView view, string id) =>
            CharacterRecruitDraft.Select(draft, Choice(view, draft), id);

        public bool Arm(TurnDraft draft, PlayerView view, string id) =>
            CharacterRecruitDraft.Arm(draft, Choice(view, draft), id);

        public bool Remove(TurnDraft draft, PlayerView view, string id)
        {
            if (draft.Payload.Placements.All(p => p.UnitId != id))
                return false;
            EventRecruitDraft.Remove(draft, id);
            return true;
        }

        private static CharacterActionOption Choice(PlayerView view, TurnDraft draft)
        {
            if (!CharacterDraft.UsesCharacters(draft.Kind))
                return null;
            var option = DraftOptions.For(view, draft);
            var choice = option == null ? null : CharacterDraft.Selected(draft, option);
            return choice?.Recruitment.Count > 0 ? choice : null;
        }
    }
}
