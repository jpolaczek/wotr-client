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
            var focus = Focus(draft, choice, data);
            var plan = new ReinforcementPlan
            {
                Prefix = "character",
                Title = choice.Title,
                Slots = choice.RulerRecruitment == null ? 1 : choice.TargetCount,
                Optional = choice.RulerRecruitment == null,
                Ready = CharacterRecruitDraft.Ready(draft, choice),
                RegionFirst = choice.RulerRecruitment != null,
                Placements = ReinforcementPlans.Placements(view, data, draft.Payload.Placements)
            };
            if (plan.RegionFirst)
                plan.Hint = RegionFirstHint(focus);
            foreach (var nation in ReinforcementRows.InOrder(choice.Recruitment.Select(g => g.Nation)))
            {
                var groups = choice.Recruitment.Where(g => g.Nation == nation).ToList();
                var settlements = choice.RulerRecruitment?.Settlements ?? groups.SelectMany(g => g.Targets).ToList();
                var row = ReinforcementPlans.Row(view, nation, (plan.Optional ? "Optional · " : "") +
                    ReinforcementPlans.Regions(data, settlements));
                foreach (var group in groups)
                    row.Figures.Add(Figure(view, draft, choice, plan, group, focus));
                ReinforcementPlans.Close(row);
                Narrow(row, focus);
                plan.Rows.Add(row);
            }
            return plan;
        }

        // UI-423 (owner's playtest pattern, like UI-412): the settlement clicked first (UI-392) narrows the Reserve while it
        // waits for its figure, as the map marks it (MapDraftOverlay.ChosenRegions); a figure chosen first, or a settlement
        // already full, narrows nothing.
        private static RecruitFocus Focus(TurnDraft draft, CharacterActionOption choice, BoardPresentation data)
        {
            var region = draft.Payload.RegionId;
            if (choice.RulerRecruitment == null || region.Length == 0 || !CharacterRecruitDraft.Targets(draft, choice).Contains(region))
                return RecruitFocus.None;
            return new RecruitFocus(region, data.Regions.TryGetValue(region, out var settlement) ? settlement.Name : region);
        }

        // UI-392: the token's click places it in the settlement chosen first; before one is chosen it lights its own.
        private static string RegionFirstHint(RecruitFocus focus) => focus.Active
            ? "Click to place it in " + focus.Name + " · Or drag it to a highlighted settlement"
            : "Click, then click a highlighted settlement · Or drag it to one";

        // A token is chosen through its first free figure, one the settlement in focus takes when there is one. UI-423: a
        // figure only another settlement takes stays choosable but unlit, and says why and what its click does (UI-412).
        private static ReinforcementFigure Figure(PlayerView view, TurnDraft draft, CharacterActionOption choice, ReinforcementPlan plan,
            RecruitmentOption group, RecruitFocus focus)
        {
            var here = focus.Active
                ? group.ReserveIds.FirstOrDefault(unit => CharacterRecruitDraft.CanPlace(draft, choice, unit, focus.Region)) : null;
            var free = here ?? group.ReserveIds.FirstOrDefault(unit =>
                group.Targets.Any(region => CharacterRecruitDraft.CanPlace(draft, choice, unit, region)));
            var figure = ReinforcementPlans.Figure(view, plan, group.Nation, group.Kind, free,
                plan.Placements.Count >= plan.Slots ? "All reinforcements of this ability are planned" : "No settlement left for this figure");
            figure.Elsewhere = focus.Active && free != null && here == null;
            if (figure.Elsewhere)
                figure.Hint = ReinforcementRows.ElsewhereHint(ReinforcementRows.NotIn(focus));
            return figure;
        }

        // UI-412: a nation none of whose free figures the settlement in focus takes is not lit either.
        private static void Narrow(ReinforcementRow row, RecruitFocus focus)
        {
            row.Elsewhere = row.CanRecruit && row.Figures.Where(f => f.ReserveId != null).All(f => f.Elsewhere);
            if (row.Elsewhere)
                row.Reason = ReinforcementRows.NotIn(focus);
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
