using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-127: an unsent army selection drawn faintly over a legal target while the pointer rests on it, with a
    // caption from the server's options only (move, or attack and its battle mode); never a server query.
    public sealed class ArmyGhost
    {
        public ArmyMarker Army { get; private set; }
        public Dictionary<string, string> Captions { get; } = new Dictionary<string, string>();

        public static ArmyGhost Of(PlayerView view, TurnDraft draft, ActionDieContext context)
        {
            var move = ArmyDraft.Current(draft);
            if (move == null || move.To.Length > 0 || move.UnitIds.Count + move.CharacterIds.Count == 0)
                return null;
            var ghost = new ArmyGhost { Army = Figures(view, move) };
            var count = ghost.Army.Total;
            foreach (var region in ArmyActionTargets.For(draft, view, context, CommandKind.MoveArmies))
                ghost.Captions[region] = "Move " + count + (count == 1 ? " figure" : " figures");
            foreach (var target in ArmyActionTargets.Options(draft, view, context, CommandKind.AttackArmy))
                ghost.Captions[target.RegionId] = "Attack · " + (target.BattleMode ?? BattleMode.Field);
            return ghost.Captions.Count > 0 ? ghost : null;
        }

        private static ArmyMarker Figures(PlayerView view, ArmyMoveChoice move)
        {
            var units = view.Units.Where(u => move.UnitIds.Contains(u.Id)).Select(u => (u.Nation, u.Kind));
            var army = ArmyMarker.From("", view.You, false, units);
            var characters = view.Characters.Where(c => move.CharacterIds.Contains(c.Id)).ToList();
            army.Characters = characters.Count;
            if (characters.Count > 0)
                army.CharacterAppearance = FigureAppearances.Art(characters[0]);
            army.CharacterNames = characters.Select(c => CharacterLibrary.Name(c.DefinitionId)).ToList();
            return army;
        }
    }
}
