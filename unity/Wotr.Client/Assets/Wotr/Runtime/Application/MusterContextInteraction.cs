using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // A Muster action on the map: a reserve chosen in the Reinforcements panel goes to the settlement clicked or
    // dropped on, one sequence step at a time. UI-135: the panel is the only recruitment window.
    public sealed class MusterContextInteraction
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;
        public bool Active => view != null && MusterActions.Active(view, draft, board.ActionDie);

        public MusterContextInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
        }

        public bool Target(string region) => CommitFigure(draft?.ReserveId, region);

        // The server's reason why the chosen reserve cannot go to this settlement of its nation, or "".
        public string Refusal(string region)
        {
            var unit = view?.Units.FirstOrDefault(u => u.Id == draft?.ReserveId);
            if (unit == null || !board.Presentation.Regions.TryGetValue(region, out var definition) ||
                definition.Settlement == null || definition.Nation != unit.Nation)
                return "";
            return definition.Name + ": " + ReinforcementRows.Refusal(view, unit.Nation, region, unit.Kind);
        }

        private bool CommitFigure(string id, string region)
        {
            if (!Active || busy() || string.IsNullOrEmpty(id))
                return false;
            var next = draft.Fork();
            if (!MusterActions.Recruit(next, view, board.ActionDie, id, region))
                return false;
            edit(() => draft.Adopt(next), true);
            return true;
        }

        // UI-142: a reserve with exactly one legal settlement is a complete choice: its click recruits there.
        public bool RecruitAtOnly(string id)
        {
            if (!Active || string.IsNullOrEmpty(id))
                return false;
            var targets = MusterActions.FigureTargets(view, board.ActionDie, id);
            return targets.Count == 1 && CommitFigure(id, targets[0]);
        }

        public bool Drop(DraftToken source, DraftToken target)
        {
            if (!Active || busy() || source == null || target == null)
                return false;
            if (source.Object == DraftObject.Reserve && target.Object is DraftObject.Region or DraftObject.Army)
                return CommitFigure(source.Id, target.Id);
            return false;
        }
    }
}
