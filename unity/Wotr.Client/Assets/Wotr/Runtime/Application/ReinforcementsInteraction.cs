using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-130/UI-131: owns the Reinforcements tab and the planned-figure ghosts on the map. A card or ability
    // plan takes precedence; otherwise the tab follows a Muster action, and (UI-179) without either it shows the
    // reserves. Steps on the political track belong to the Politics panel (UI-178).
    public sealed class ReinforcementsInteraction : IDisposable
    {
        private static readonly IReadOnlyList<PlannedReinforcement> Nothing = new List<PlannedReinforcement>();
        private readonly BoardScreen board;
        private readonly Action<Action> edit;
        private readonly Func<bool> busy;
        private readonly Func<bool> muster;
        private readonly Func<string, bool> recruitAtOnly;
        private readonly Action confirm;
        private readonly Action finish;
        private PlayerView view;
        private TurnDraft draft;

        public bool Planning => view != null && ReinforcementPlans.Active(view, draft);

        public ReinforcementsInteraction(BoardScreen board, Action<Action> edit, Func<bool> busy, Func<bool> muster, Func<string, bool> recruitAtOnly,
            Action confirm, Action finish)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            this.muster = muster;
            this.recruitAtOnly = recruitAtOnly;
            this.confirm = confirm;
            this.finish = finish;
            board.Reinforcements.FigureChosen += Choose;
            board.Reinforcements.PlacementRemoved += Remove;
            board.Reinforcements.Finish += Finish;
            board.Reinforcements.Confirm += Confirm;
            board.Hover.Panel.SettlementInspected += Inspect;
            board.Map.PlannedRecruits.Removed += RemoveRegion;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            var plan = ReinforcementPlans.For(view, draft, board.Presentation);
            board.Reinforcements.Apply(State(plan));
            board.Map.PlannedRecruits.Apply(plan?.Placements ?? Nothing, view.You, !busy());
        }

        private ReinforcementsState State(ReinforcementPlan plan)
        {
            if (plan != null)
                return ReinforcementsState.For(view, plan, draft.ReserveId, !busy());
            if (!muster() || draft.Kind is not (null or CommandKind.Recruit))
                return ReinforcementsState.Browse(view);
            var recruitment = MusterActions.Option(view, board.ActionDie, CommandKind.Recruit);
            return ReinforcementsState.For(view, recruitment, draft.ReserveId, !busy());
        }

        // A card or ability plans the figure; a Muster reserve lights the settlements it can take.
        private void Choose(string id)
        {
            if (view == null || busy())
                return;
            if (Planning)
                edit(() => ReinforcementPlans.Choose(draft, view, id));
            else if (muster())
            {
                // UI-142: one legal settlement makes the choice complete; otherwise its settlements light up.
                if (recruitAtOnly(id))
                    return;
                edit(() => MusterActions.Select(draft, view, board.ActionDie, id));
            }
            // The lit settlements may lie off screen: the view moves there.
            board.Map.Reveal(board.Map.DraftOverlay.Targets);
        }

        // UI-135: a settlement clicked during a recruitment brings the panel back and marks its nation's row.
        private void Inspect(string nation)
        {
            board.Reinforcements.Focus(nation);
            if (nation.Length > 0 && view?.Units.Any(u => u.Side == view.You && u.Nation == nation) == true)
                board.Reinforcements.Reopen();
        }

        private void Remove(string id)
        {
            if (Planning && !busy())
                edit(() => ReinforcementPlans.Remove(draft, view, id));
        }

        private void RemoveRegion(string region)
        {
            if (Planning && !busy())
                edit(() => ReinforcementPlans.RemoveRegion(draft, view, board.Presentation, region));
        }

        private void Finish()
        {
            if (!Planning && !busy())
                finish();
        }

        private void Confirm()
        {
            if (Planning && !busy())
                confirm();
        }

        public void Dispose()
        {
            board.Reinforcements.FigureChosen -= Choose;
            board.Reinforcements.PlacementRemoved -= Remove;
            board.Reinforcements.Finish -= Finish;
            board.Reinforcements.Confirm -= Confirm;
            board.Hover.Panel.SettlementInspected -= Inspect;
            board.Map.PlannedRecruits.Removed -= RemoveRegion;
            board.Reinforcements.Apply(ReinforcementsState.Closed);
            board.Map.PlannedRecruits.Apply(Nothing, Side.FreePeoples, false);
        }
    }
}
