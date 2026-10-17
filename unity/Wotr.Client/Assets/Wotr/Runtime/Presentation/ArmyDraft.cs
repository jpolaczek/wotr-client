using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Local constraints guide the player; the server remains authoritative for the whole action.
    public static class ArmyDraft
    {
        public static int VisibleIndex(TurnDraft draft) => draft.MoveFocus >= 0 ? draft.MoveFocus : draft.Payload.Moves.Count - 1;
        public static ArmyMoveChoice Current(TurnDraft draft) => VisibleIndex(draft) >= 0 ? draft.Payload.Moves[VisibleIndex(draft)] : null;
        public static HashSet<string> UsedElsewhere(TurnDraft draft, int index) => new HashSet<string>(draft.Payload.Moves
            .Where((_, i) => i != index).SelectMany(m => m.UnitIds.Concat(m.CharacterIds)));

        public static void MatchCost(TurnDraft draft, ActionOption option)
        {
            var costs = option.MovementCosts.Where(c => c.DieId == draft.Payload.DieId).ToList();
            draft.Payload.AbilityId = "";
            if (costs.Count == 1)
            {
                draft.Payload.MovementCost = costs[0].Face;
                draft.Payload.AbilityId = costs[0].AbilityId;
            }
        }

        public static bool Start(TurnDraft draft, PlayerView view, string region)
        {
            if (EventArmyDraft.Option(view) != null)
                return EventArmyDraft.Start(draft, view, region);
            var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.MoveArmies && o.Enabled);
            var army = option?.Armies.FirstOrDefault(a => a.RegionId == region);
            if (army == null)
                return false;
            if (draft.Kind != CommandKind.MoveArmies)
                DraftGesture.Choose(draft, view, CommandKind.MoveArmies);
            var current = Current(draft);
            var replace = draft.MoveFocus >= 0 && current != null && current.To.Length == 0;
            var index = replace ? draft.MoveFocus : draft.Payload.Moves.Count;
            var cost = option.MovementCosts.FirstOrDefault(c => c.DieId == draft.Payload.DieId && c.Face == draft.Payload.MovementCost);
            if (!replace && index >= (cost?.MaximumMoves ?? option.Maximum))
                return false;
            var used = UsedElsewhere(draft, replace ? index : -1);
            var ids = army.UnitIds.Where(id => !used.Contains(id)).ToList();
            if (!view.Units.Any(u => ids.Contains(u.Id) && IsCombat(u)))
                return false;
            draft.Change(p =>
            {
                var move = new ArmyMoveChoice { From = region, UnitIds = ids, CharacterIds = army.CharacterIds.Where(id => !used.Contains(id)).ToList() };
                if (replace)
                    p.Moves[index] = move;
                else
                    p.Moves.Add(move);
                draft.MoveFocus = index;
            });
            return true;
        }

        public static IEnumerable<string> Targets(TurnDraft draft, PlayerView view)
        {
            if (EventArmyDraft.Option(view) != null)
                return EventArmyDraft.Targets(draft, view);
            var move = Current(draft);
            if (move == null || Problem(draft, view).Length > 0)
                return Enumerable.Empty<string>();
            var army = view.Options.First(o => o.Kind == CommandKind.MoveArmies).Armies.FirstOrDefault(a => a.RegionId == move.From);
            var ids = move.UnitIds.Concat(move.CharacterIds).ToArray();
            return army?.Targets.Where(t => ids.All(t.EligibleIds.Contains)).Select(t => t.RegionId) ?? Enumerable.Empty<string>();
        }

        public static bool Target(TurnDraft draft, PlayerView view, string region)
        {
            if (EventArmyDraft.Option(view) != null)
                return EventArmyDraft.Target(draft, view, region);
            if (draft.MoveFocus < 0 || !Targets(draft, view).Contains(region))
                return false;
            draft.Change(p => { p.Moves[draft.MoveFocus].To = region; draft.MoveFocus = -1; });
            return true;
        }

        public static void Edit(TurnDraft draft, int index)
        {
            if (index >= 0 && index < draft.Payload.Moves.Count)
                draft.Change(_ => draft.MoveFocus = index);
        }

        public static void Remove(TurnDraft draft, int index)
        {
            if (index >= 0 && index < draft.Payload.Moves.Count)
                draft.Change(p => { p.Moves.RemoveAt(index); draft.MoveFocus = -1; });
        }

        public static void Toggle(TurnDraft draft, IEnumerable<string> ids, bool character = false)
        {
            var move = Current(draft);
            if (move == null)
                return;
            var used = UsedElsewhere(draft, VisibleIndex(draft));
            var eligible = ids.Where(id => !used.Contains(id)).ToArray();
            draft.Change(_ =>
            {
                var selected = character ? move.CharacterIds : move.UnitIds;
                var remove = eligible.All(selected.Contains);
                foreach (var id in eligible)
                {
                    if (remove)
                        selected.Remove(id);
                    else if (!selected.Contains(id))
                        selected.Add(id);
                }
                draft.MoveFocus = VisibleIndex(draft);
            });
        }

        public static string Problem(TurnDraft draft, PlayerView view)
        {
            if (EventArmyDraft.Option(view) != null)
                return EventArmyDraft.Problem(draft, view);
            var move = Current(draft);
            if (move == null)
                return "Choose an army on the map.";
            var cost = view.Options.First(o => o.Kind == CommandKind.MoveArmies).MovementCosts
                .FirstOrDefault(c => c.DieId == draft.Payload.DieId && c.Face == draft.Payload.MovementCost);
            if (cost != null && draft.Payload.Moves.Count > cost.MaximumMoves)
                return "This die allows one army. Remove a move or choose another die.";
            var units = view.Units.Where(u => move.UnitIds.Contains(u.Id)).ToList();
            if (!units.Any(IsCombat))
                return "Include at least one army unit.";
            var army = view.Options.First(o => o.Kind == CommandKind.MoveArmies).Armies.First(a => a.RegionId == move.From);
            if (army.RequiredAnyIds.Count > 0 && !move.UnitIds.Concat(move.CharacterIds).Any(army.RequiredAnyIds.Contains))
                return "Include a ◇ commander in this army.";
            var positions = view.Units.ToDictionary(u => u.Id, u => u.Location);
            foreach (var earlier in draft.Payload.Moves.Take(VisibleIndex(draft)))
                foreach (var id in earlier.UnitIds)
                    positions[id] = earlier.To;
            var left = view.Units.Where(u => positions[u.Id] == move.From && u.Side == view.You && !u.Garrison &&
                !move.UnitIds.Contains(u.Id)).ToList();
            if (left.Any(u => u.Kind == UnitKind.Leader) && !left.Any(IsCombat))
                return "A leader cannot stay without an army unit.";
            if (draft.Payload.DieId.Length > 0 && draft.Payload.MovementCost == DieFace.Character &&
                !move.UnitIds.Concat(move.CharacterIds).Any(view.Options.First(o => o.Kind == CommandKind.MoveArmies).Armies
                    .First(a => a.RegionId == move.From).CommanderIds.Contains))
                return "A Character move needs a commander in the moving group.";
            return "";
        }

        public static bool IsCombat(UnitView unit) => unit.Kind is UnitKind.Regular or UnitKind.Elite;
    }
}
