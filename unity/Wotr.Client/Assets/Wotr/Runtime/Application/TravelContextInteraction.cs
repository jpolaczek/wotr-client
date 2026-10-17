using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class TravelContextInteraction
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;
        public bool Active => draft != null && TravelActions.IsStep(draft);

        public TravelContextInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            var option = TravelActions.Option(view, draft, board.ActionDie);
            if (option == null)
                return;
            var eligible = option.EligibleIds.Where(id => TravelActions.CanSelect(view, draft, board.ActionDie, id)).ToHashSet();
            var selected = TravelActions.Selected(draft).ToHashSet();
            var armyEligible = board.Hover.Panel.Eligible;
            var armySelected = board.Hover.Panel.Selected;
            board.Hover.Panel.Eligible = id => armyEligible(id) || !busy() && eligible.Contains(id);
            board.Hover.Panel.Selected = id => armySelected(id) || selected.Contains(id);
            board.Hover.Panel.RefreshSelection();
            var sources = option.Travel.Select(t => new ArmyOption
            {
                RegionId = t.Origin,
                Garrison = view.Characters.FirstOrDefault(c => c.Id == t.Id)?.Garrison ??
                    view.Units.FirstOrDefault(u => u.Id == t.Id)?.Garrison ?? false
            });
            if (ContextArmySelection.Active(view, draft, board.ActionDie))
                sources = sources.Concat(ContextArmySelection.Sources(view, board.ActionDie));
            board.Map.HighlightSources(sources, view.You);
        }

        public bool Figure(string id)
        {
            if (view == null || busy() || !TravelActions.CanSelect(view, draft, board.ActionDie, id))
                return false;
            edit(() => TravelActions.Select(view, draft, board.ActionDie, id), false);
            return true;
        }

        // UI-105: a click on a reachable destination moves the selected group there at once. An event journey of
        // several groups sent as one command keeps its shared confirmation, so the click only sets that group's
        // destination.
        public bool Reach(string region)
        {
            if (view == null || busy() || !TravelDraft.TargetRoute(draft.Kind))
                return false;
            var next = draft.Fork();
            if (!TravelDraft.RouteTo(next, view, board.Presentation, region))
                return false;
            var option = DraftOptions.For(view, next);
            edit(() => draft.Adopt(next), option != null && !SharedJourney(next, option));
            return true;
        }

        // Sending at once would end the event before the player could move another of its figures.
        private bool SharedJourney(TurnDraft selection, ActionOption option) =>
            TravelDraft.IsGrouped(selection.Kind) && option.MaximumGroups != 1 && option.EligibleIds.Count > 1 &&
            view.Decision.Kind is not (DecisionKind.Action or DecisionKind.ActionSequence);

        public bool Finish(string region)
        {
            if (view == null || busy())
                return false;
            var next = draft.Fork();
            if (!TravelActions.End(next, view, region))
                return false;
            edit(() => draft.Adopt(next), true);
            return true;
        }
    }
}
