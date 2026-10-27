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
        private readonly Action<ComponentAction> ability;
        private readonly Action<Action> complete;
        private readonly BranchCarry carry;
        private PlayerView view;
        private TurnDraft draft;
        private List<ComponentAction> abilities = new List<ComponentAction>();

        public bool Planning => view != null && ReinforcementPlans.Active(view, draft);

        public ReinforcementsInteraction(BoardScreen board, Action<Action> edit, Func<bool> busy, Func<bool> muster, Func<string, bool> recruitAtOnly,
            Action confirm, Action finish, Action<ComponentAction> ability, Action<Action> complete, BranchCarry carry)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            this.muster = muster;
            this.recruitAtOnly = recruitAtOnly;
            this.confirm = confirm;
            this.finish = finish;
            this.ability = ability;
            this.complete = complete;
            this.carry = carry;
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
            // UI-271: Gothmog's ability stands beside the Muster rows while the die chosen can pay it.
            abilities = plan == null && muster() && draft.Kind == null
                ? ComponentActions.For(view, draft).Where(CharacterAbilityPlaces.InReserve).ToList() : new List<ComponentAction>();
            board.Reinforcements.Apply(State(plan));
            board.Map.PlannedRecruits.Apply(plan?.Placements ?? Nothing, view.You, !busy());
            if (plan != null && !busy() && carry.Take(view, BranchCarry.Reserve) is { Length: > 0 } figure)
                board.Root.schedule.Execute(() => PlanCarried(figure));
        }

        // UI-278 (D15): the figure whose click chose a Kings card's recruit is planned in its next step, when offered; a
        // card's single figure is then sent at once.
        private void PlanCarried(string figure)
        {
            var plan = ReinforcementPlans.For(view, draft, board.Presentation);
            var unit = view.Units.FirstOrDefault(u => u.Id == figure);
            var offered = plan?.Rows.Where(r => r.Nation == unit?.Nation).SelectMany(r => r.Figures)
                .FirstOrDefault(f => f.Kind == unit?.Kind && f.ReserveId != null)?.ReserveId;
            if (offered != null && !busy())
                edit(() => ReinforcementPlans.Choose(draft, view, offered));
        }

        // UI-278 (D15): a Kings card's recruit branch waits on its king's nation in the Reserve.
        private string BranchNation() => view == null || ReinforcementPlans.Active(view, draft) ? "" : KingsCardChoices.RecruitNation(view);

        private ReinforcementsState State(ReinforcementPlan plan)
        {
            if (plan != null)
                return ReinforcementsState.For(view, plan, draft.ReserveId, !busy());
            if (BranchNation() is { Length: > 0 } nation)
                return ReinforcementsState.Branch(view, nation, !busy());
            if (!muster() || draft.Kind is not (null or CommandKind.Recruit))
                return ReinforcementsState.Browse(view);
            var recruitment = MusterActions.Option(view, board.ActionDie, CommandKind.Recruit);
            var state = ReinforcementsState.For(view, recruitment, draft.ReserveId, !busy());
            state.Rows = state.Rows.Concat(GothmogReinforcements.Rows(view, abilities, board.Presentation)).ToList();
            return state;
        }

        // A card or ability plans the figure; a Muster reserve lights the settlements it can take.
        private void Choose(string id)
        {
            if (view == null || busy())
                return;
            if (GothmogReinforcements.Ability(abilities, id) is { } chosen)
            {
                ability(chosen);
                return;
            }
            if (BranchNation().Length > 0)
            {
                Recruit(id);
                return;
            }
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

        private void Recruit(string figure)
        {
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect);
            if (option == null)
                return;
            carry.Set(view, BranchCarry.Reserve, figure);
            complete(() => EffectChoiceSelection.Choose(view, draft, option, KingsCardChoices.Recruit));
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
