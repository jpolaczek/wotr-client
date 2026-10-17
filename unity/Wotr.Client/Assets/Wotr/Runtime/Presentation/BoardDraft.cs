using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class BoardDraft
    {
        public static bool UsesBoard(CommandKind? kind) => kind is CommandKind.AttackArmy or CommandKind.MoveArmies or CommandKind.Recruit or CommandKind.AdvancePolitics or CommandKind.RemoveExcess;

        public static string Hint(TurnDraft draft, PlayerView view)
        {
            if (!UsesBoard(draft.Kind))
                return "";
            if (draft.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy)
            {
                var problem = draft.Kind == CommandKind.AttackArmy ? AttackDraft.Problem(draft, view) : ArmyDraft.Problem(draft, view);
                if (problem.Length > 0)
                    return problem;
                if (draft.Payload.Moves.Any(m => m.To.Length == 0))
                    return "Choose a highlighted destination.";
            }
            if (draft.Kind == CommandKind.RemoveExcess)
                return "Choose " + view.Decision.RequiredCount + " units to return · Selected " + draft.Payload.Ids.Count;
            var armyEvent = view.Options.FirstOrDefault(o => o.Kind == draft.Kind)?.ArmyEvent;
            if (armyEvent != null)
            {
                var option = view.Options.First(o => o.Kind == draft.Kind);
                if (armyEvent.RouteMovement && draft.Kind == CommandKind.MoveArmies)
                {
                    if (!EventArmyDraft.Ready(draft, option))
                        return draft.Payload.Moves.Count < option.Minimum ? "Plan another army on the map." : "Continue to a highlighted final destination.";
                    return "Review the routes · No extra die.";
                }
                return "Review the army and destination · No extra die.";
            }
            if (draft.Payload.DieId.Length == 0)
                return "Choose an action die from your pool.";
            if (draft.Kind == CommandKind.Recruit)
                return draft.ReserveId.Length > 0 ? "Place the selected reinforcement in a highlighted settlement." : "Choose a reserve figure, or confirm the planned reinforcements.";
            return "";
        }

        public static bool SelectRegion(TurnDraft draft, PlayerView view, string region)
        {
            if (draft.Kind == CommandKind.AttackArmy)
                return AttackDraft.SelectRegion(draft, view, region);
            if (draft.Kind == CommandKind.Recruit)
                return RecruitDraft.Place(draft, view, region);
            if (draft.Kind == CommandKind.MoveArmies && draft.MoveFocus >= 0 && ArmyDraft.Target(draft, view, region))
                return true;
            if (!draft.Kind.HasValue || draft.Kind == CommandKind.MoveArmies)
                return ArmyDraft.Start(draft, view, region);
            return false;
        }

        // A reserve figure dropped on a settlement or its marker. Armies are never dropped (UI-105, UI-128).
        public static bool Drop(TurnDraft draft, PlayerView view, DraftToken source, DraftToken target)
        {
            if (source.Object != DraftObject.Reserve || target.Object is not (DraftObject.Region or DraftObject.Army))
                return false;
            var trial = draft.Fork();
            var success = RecruitDraft.Select(trial, view, source.Id) && RecruitDraft.Place(trial, view, target.Id);
            if (success)
                draft.Adopt(trial);
            return success;
        }

        public static void Reconcile(TurnDraft draft, ActionOption option)
        {
            var payload = draft.Payload;
            var focused = draft.MoveFocus >= 0 && draft.MoveFocus < payload.Moves.Count ? payload.Moves[draft.MoveFocus] : null;
            if (!option.Targets.Contains(payload.NationId))
                payload.NationId = "";
            payload.Placements.RemoveAll(p => !option.Recruitment.Any(g => g.ReserveIds.Contains(p.UnitId) && g.Targets.Contains(p.RegionId)));
            if (!option.Recruitment.Any(g => g.ReserveIds.Contains(draft.ReserveId)))
                draft.ReserveId = "";
            payload.Moves.RemoveAll(m => !option.Armies.Any(a => a.RegionId == m.From));
            foreach (var move in payload.Moves)
            {
                var army = option.Armies.First(a => a.RegionId == move.From);
                move.UnitIds.RemoveAll(id => !army.UnitIds.Contains(id));
                move.CharacterIds.RemoveAll(id => !army.CharacterIds.Contains(id));
                if (!army.Targets.Any(t => t.RegionId == move.To && move.UnitIds.Concat(move.CharacterIds).All(t.EligibleIds.Contains)))
                    move.To = "";
            }
            draft.MoveFocus = focused == null ? -1 : payload.Moves.IndexOf(focused);
            EventArmyDraft.Reconcile(draft, option);
        }
    }
}
