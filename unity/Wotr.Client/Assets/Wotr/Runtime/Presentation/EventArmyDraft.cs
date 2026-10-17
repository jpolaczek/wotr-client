using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class EventArmyDraft
    {
        public static ActionOption Option(PlayerView view) => view.Options.FirstOrDefault(o => o.Kind == CommandKind.MoveArmies && o.ArmyEvent?.RouteMovement == true);
        public static List<string> Route(TurnDraft draft, ActionOption option, ArmyMoveChoice move) =>
            option.ArmyEvent.SeparateRoutes ? move.Route : draft.Payload.Route;

        public static bool Start(TurnDraft draft, PlayerView view, string region)
        {
            var option = Option(view);
            var army = option?.Armies.FirstOrDefault(a => a.RegionId == region);
            if (army == null)
                return false;
            var separate = option.ArmyEvent.SeparateRoutes;
            var current = ArmyDraft.Current(draft);
            var replace = separate && draft.MoveFocus >= 0 && current != null && current.To.Length == 0;
            var index = separate ? replace ? draft.MoveFocus : draft.Payload.Moves.Count : 0;
            if (separate && (index >= option.Maximum || draft.Payload.Moves.Where((_, i) => i != index).Any(m => m.From == region)))
                return false;
            var used = ArmyDraft.UsedElsewhere(draft, index);
            draft.Change(p =>
            {
                var move = new ArmyMoveChoice
                {
                    From = region,
                    UnitIds = army.UnitIds.Where(id => !used.Contains(id)).ToList(),
                    CharacterIds = army.CharacterIds.Where(id => !used.Contains(id)).ToList()
                };
                if (!separate)
                {
                    p.Route.Clear();
                    p.Moves.Clear();
                }
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
            var move = ArmyDraft.Current(draft);
            if (move == null || Problem(draft, view).Length > 0)
                return Enumerable.Empty<string>();
            var option = Option(view);
            var route = Route(draft, option, move);
            var army = option.Armies.First(a => a.RegionId == move.From);
            return army.Targets.Where(t => t.Route.Count > route.Count && t.Route.Take(route.Count).SequenceEqual(route) &&
                move.UnitIds.Concat(move.CharacterIds).All(t.EligibleIds.Contains)).Select(t => t.Route[route.Count]).Distinct();
        }

        public static bool Target(TurnDraft draft, PlayerView view, string region)
        {
            if (!Targets(draft, view).Contains(region))
                return false;
            var option = Option(view);
            draft.Change(p =>
            {
                var index = ArmyDraft.VisibleIndex(draft);
                var move = p.Moves[index];
                var route = Route(draft, option, move);
                route.Add(region);
                move.To = region;
                draft.MoveFocus = option.ArmyEvent.SeparateRoutes && route.Count == option.ArmyEvent.MaximumSteps ? -1 : index;
            });
            return true;
        }

        public static void ResetRoute(TurnDraft draft)
        {
            draft.Change(p =>
            {
                var index = ArmyDraft.VisibleIndex(draft);
                p.Route.Clear();
                p.Moves[index].Route.Clear();
                p.Moves[index].To = "";
                draft.MoveFocus = index;
            });
        }

        public static void Toggle(TurnDraft draft, IEnumerable<string> ids, bool character = false)
        {
            var index = ArmyDraft.VisibleIndex(draft);
            var used = ArmyDraft.UsedElsewhere(draft, index);
            var figures = ids.Where(id => !used.Contains(id)).ToList();
            draft.Change(p =>
            {
                var move = p.Moves[index];
                var selected = character ? move.CharacterIds : move.UnitIds;
                var remove = figures.All(selected.Contains);
                foreach (var id in figures)
                    if (remove)
                        selected.Remove(id);
                    else if (!selected.Contains(id))
                        selected.Add(id);
                p.Route.Clear();
                move.Route.Clear();
                move.To = "";
                draft.MoveFocus = index;
            });
        }

        public static string Problem(TurnDraft draft, PlayerView view)
        {
            var move = ArmyDraft.Current(draft);
            if (move == null)
                return "Choose an army on the map.";
            var option = Option(view);
            var army = option.Armies.FirstOrDefault(a => a.RegionId == move.From);
            if (army == null || !view.Units.Any(u => move.UnitIds.Contains(u.Id) && ArmyDraft.IsCombat(u)))
                return "Include at least one army unit.";
            if (option.ArmyEvent.RequiresCompanion && move.CharacterIds.Count == 0)
                return "Include a Companion in the moving army.";
            var left = view.Units.Where(u => u.Location == move.From && u.Garrison == army.Garrison && u.Side == view.You && !move.UnitIds.Contains(u.Id)).ToList();
            if (!(option.ArmyEvent.AllowLeaderLoss && army.Garrison) && left.Any(u => u.Kind == UnitKind.Leader) && !left.Any(ArmyDraft.IsCombat))
                return "A leader cannot stay without an army unit.";
            return "";
        }

        public static bool Ready(TurnDraft draft, ActionOption option)
        {
            var moves = draft.Payload.Moves;
            if (moves.Count < option.Minimum || moves.Count > option.Maximum || moves.Count == 0 ||
                moves.Select(m => m.From).Distinct().Count() != moves.Count)
                return false;
            var ids = moves.SelectMany(m => m.UnitIds.Concat(m.CharacterIds)).ToArray();
            return ids.Distinct().Count() == ids.Length && moves.All(move => Complete(draft, option, move));
        }

        public static bool Complete(TurnDraft draft, ActionOption option, ArmyMoveChoice move) =>
            move.UnitIds.Count > 0 && (!option.ArmyEvent.RequiresCompanion || move.CharacterIds.Count > 0) &&
            option.Armies.Any(a => a.RegionId == move.From && a.Targets.Any(t => t.RegionId == move.To &&
                t.Route.SequenceEqual(Route(draft, option, move)) && move.UnitIds.Concat(move.CharacterIds).All(t.EligibleIds.Contains)));

        public static void Reconcile(TurnDraft draft, ActionOption option)
        {
            if (option.ArmyEvent?.RouteMovement != true || option.Kind != CommandKind.MoveArmies)
                return;
            foreach (var move in draft.Payload.Moves)
            {
                var route = Route(draft, option, move);
                var army = option.Armies.FirstOrDefault(a => a.RegionId == move.From);
                if (army == null || !army.Targets.Any(t => t.Route.Take(route.Count).SequenceEqual(route) &&
                    move.UnitIds.Concat(move.CharacterIds).All(t.EligibleIds.Contains)))
                {
                    route.Clear();
                    move.To = "";
                }
                else if (!army.Targets.Any(t => t.RegionId == move.To && t.Route.SequenceEqual(route) &&
                    move.UnitIds.Concat(move.CharacterIds).All(t.EligibleIds.Contains)))
                    move.To = "";
            }
        }
    }
}
