using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ArmyActionTargets
    {
        public static IEnumerable<string> For(TurnDraft draft, PlayerView view, ActionDieContext context, CommandKind? kind = null) =>
            Options(draft, view, context, kind).Select(t => t.RegionId).Distinct();

        // The server's targets that accept every selected figure, with their battle mode for an attack.
        public static IEnumerable<ArmyTargetOption> Options(TurnDraft draft, PlayerView view, ActionDieContext context,
            CommandKind? kind = null) =>
            ContextArmySelection.Options(view, context).Where(o => !kind.HasValue || o.Kind == kind)
                .SelectMany(o => ForOption(draft, view, o));

        public static CommandKind? Kind(TurnDraft draft, PlayerView view, ActionDieContext context, string region) =>
            ContextArmySelection.Options(view, context).FirstOrDefault(o => ForOption(draft, view, o).Any(t => t.RegionId == region))?.Kind;

        private static IEnumerable<ArmyTargetOption> ForOption(TurnDraft draft, PlayerView view, ActionOption option)
        {
            var move = ArmyDraft.Current(draft);
            if (move == null || move.To.Length > 0)
                return Enumerable.Empty<ArmyTargetOption>();
            var ids = move.UnitIds.Concat(move.CharacterIds).ToArray();
            return option.Armies.Where(a => a.RegionId == move.From && ids.All(id => ContextArmySelection.Contains(a, id)) &&
                    Problem(draft, view, option, a).Length == 0)
                .SelectMany(a => a.Targets).Where(t => ids.All(t.EligibleIds.Contains));
        }

        // UI-127: figures that keep the selection out of some region the rest of it could reach, by the server's
        // per-target EligibleIds; no rule is recomputed here.
        public static List<string> Blocking(TurnDraft draft, PlayerView view, ActionDieContext context)
        {
            var move = ArmyDraft.Current(draft);
            if (move == null || move.To.Length > 0)
                return new List<string>();
            var ids = move.UnitIds.Concat(move.CharacterIds).ToList();
            return ContextArmySelection.Sources(view, context)
                .Where(a => a.RegionId == move.From && ids.All(id => ContextArmySelection.Contains(a, id)))
                .SelectMany(a => a.Targets).SelectMany(t => ids.Where(id => !t.EligibleIds.Contains(id)))
                .Distinct().ToList();
        }

        public static string Hint(TurnDraft draft, PlayerView view, ActionDieContext context)
        {
            var move = ArmyDraft.Current(draft);
            if (move == null)
                return ContextArmySelection.RequiresCommander(view, context)
                    ? "Click an army with a commander to select all of it" : "Click your army to select all of it";
            var blocking = Blocking(draft, view, context);
            if (For(draft, view, context).Any())
                return "Click a highlighted region to move · click figures in the panel to leave them behind" +
                    (blocking.Count > 0 ? " · Leave out " + Names(view, blocking) + " to reach more regions" : "");
            foreach (var option in ContextArmySelection.Options(view, context))
            {
                var army = option.Armies.FirstOrDefault(a => a.RegionId == move.From &&
                    move.UnitIds.Concat(move.CharacterIds).All(id => ContextArmySelection.Contains(a, id)));
                if (army == null)
                    continue;
                var problem = Problem(draft, view, option, army);
                if (problem.Length > 0)
                    return problem;
            }
            return blocking.Count > 0 ? "No region takes every selected figure · leave out " + Names(view, blocking)
                : "No legal destination for these figures";
        }

        private static string Names(PlayerView view, IEnumerable<string> ids) => string.Join(", ", ids.Select(id =>
            view.Units.FirstOrDefault(u => u.Id == id) is { } unit ? FigureGrid.Name(unit.Nation) + " " + unit.Kind
                : CharacterLibrary.Name(view, id)).GroupBy(name => name)
            .Select(group => group.Count() > 1 ? group.Key + " ×" + group.Count() : group.Key));

        private static string Problem(TurnDraft draft, PlayerView view, ActionOption option, ArmyOption army)
        {
            var move = ArmyDraft.Current(draft);
            var selected = move.UnitIds.Concat(move.CharacterIds).ToHashSet();
            var cost = option.MovementCosts.FirstOrDefault(c => c.DieId == draft.Payload.DieId &&
                c.AbilityId == draft.Payload.AbilityId && c.Face == draft.Payload.MovementCost);
            if (cost?.RequiredCharacterId.Length > 0 && !selected.Contains(cost.RequiredCharacterId))
                return "Include " + CharacterLibrary.Name(cost.RequiredCharacterId);
            if (!view.Units.Any(u => selected.Contains(u.Id) && ArmyDraft.IsCombat(u)))
                return "Include an army unit";
            if (army.RequiredAnyIds.Count > 0 && !army.RequiredAnyIds.Any(selected.Contains) ||
                draft.Payload.MovementCost == DieFace.Character && !army.CommanderIds.Any(selected.Contains))
                return "Include a commander in the selected force";
            var left = view.Units.Where(u => u.Side == view.You && u.Location == army.RegionId && u.Garrison == army.Garrison && !selected.Contains(u.Id)).ToList();
            if (option.Kind == CommandKind.MoveArmies)
                return left.Any(u => u.Kind == UnitKind.Leader) && !left.Any(ArmyDraft.IsCombat)
                    ? "A leader cannot stay without an army unit" : "";
            var charactersLeft = view.Characters.Any(c => c.Side == view.You && c.Location == army.RegionId && c.Garrison == army.Garrison && !selected.Contains(c.Id));
            return (left.Count > 0 || charactersLeft) && !left.Any(ArmyDraft.IsCombat)
                ? "Include an army unit in the rearguard" : "";
        }
    }
}
