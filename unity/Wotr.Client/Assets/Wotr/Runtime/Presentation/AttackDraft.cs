using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class AttackDraft
    {
        public static bool SelectRegion(TurnDraft draft, PlayerView view, string region)
        {
            if (Targets(draft, view).Contains(region))
            {
                draft.Change(p => p.Moves[0].To = region);
                return true;
            }
            var army = view.Options.FirstOrDefault(o => o.Kind == CommandKind.AttackArmy && o.Enabled)?.Armies
                .FirstOrDefault(a => a.RegionId == region);
            if (army == null)
                return false;
            draft.Change(p =>
            {
                p.Moves.Clear();
                p.Moves.Add(new ArmyMoveChoice { From = region, UnitIds = new List<string>(army.UnitIds), CharacterIds = new List<string>(army.CharacterIds) });
                draft.MoveFocus = 0;
            });
            return true;
        }

        public static IEnumerable<string> Targets(TurnDraft draft, PlayerView view)
        {
            var move = ArmyDraft.Current(draft);
            if (move == null || Problem(draft, view).Length > 0)
                return Enumerable.Empty<string>();
            return view.Options.First(o => o.Kind == CommandKind.AttackArmy).Armies.First(a => a.RegionId == move.From).Targets
                .Where(t => move.UnitIds.Concat(move.CharacterIds).All(t.EligibleIds.Contains)).Select(t => t.RegionId);
        }

        public static string Problem(TurnDraft draft, PlayerView view)
        {
            var move = ArmyDraft.Current(draft);
            if (move == null)
                return "Choose an attacking army on the map.";
            var army = view.Options.First(o => o.Kind == CommandKind.AttackArmy).Armies.FirstOrDefault(a => a.RegionId == move.From);
            if (army == null || !view.Units.Any(u => move.UnitIds.Contains(u.Id) && ArmyDraft.IsCombat(u)))
                return "Include an army unit in the attack.";
            if (army.RequiredAnyIds.Count > 0 && !move.UnitIds.Concat(move.CharacterIds).Any(army.RequiredAnyIds.Contains))
                return "Include a ◇ commander in the attacking army.";
            var left = view.Units.Where(u => u.Side == view.You && u.Location == move.From && u.Garrison == army.Garrison && !move.UnitIds.Contains(u.Id)).ToList();
            var charactersLeft = view.Characters.Any(c => c.Side == view.You && c.Location == move.From && c.Garrison == army.Garrison && !move.CharacterIds.Contains(c.Id));
            if ((left.Count > 0 || charactersLeft) && !left.Any(ArmyDraft.IsCombat))
                return "Include an army unit in the rearguard.";
            if (draft.Payload.DieId.Length > 0 && draft.Payload.MovementCost == DieFace.Character &&
                !move.UnitIds.Concat(move.CharacterIds).Any(army.CommanderIds.Contains))
                return "A Character attack needs a participating commander.";
            return "";
        }

        public static BattleMode? Mode(TurnDraft draft, PlayerView view)
        {
            var move = ArmyDraft.Current(draft);
            return view.Options.FirstOrDefault(o => o.Kind == CommandKind.AttackArmy)?.Armies.FirstOrDefault(a => a.RegionId == move?.From)?
                .Targets.FirstOrDefault(t => t.RegionId == move.To)?.BattleMode;
        }
    }
}
