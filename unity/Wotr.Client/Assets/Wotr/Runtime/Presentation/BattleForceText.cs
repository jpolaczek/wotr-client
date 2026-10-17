using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class BattleForceText
    {
        public static string Modifiers(BattleForceView force, BattleView battle, PlayerView view) =>
            "Combat modifier " + Signed(force.CombatModifier) + " · Leader re-roll modifier " + Signed(force.RerollModifier) +
            (force.CaptainBonus > 0 ? "\nCaptain bonus +" + force.CaptainBonus : "") +
            (force.CombatDiceReduction > 0 ? "\nHelp Unlooked For: −" + force.CombatDiceReduction + " Combat dice after the limit, minimum one, for this battle." : "") +
            (battle.WhiteRiderActive ? "\nWhite Rider: Gandalf and Nazgûl forfeit Leadership for this battle." : "") +
            (force == battle.Attacker && battle.ArrivalBeforeBattle ? "\nLanded before battle · No advance follows." : "") +
            (force.SpentHits > 0 ? "\n" + force.SpentHits + " hits spent on a Combat card effect." : "") +
            string.Concat(force.CancelledAbilities.Select(id => "\n" + CharacterLibrary.ShortName(view, id) + " · Leadership and abilities cancelled this round")) +
            string.Concat(force.ForfeitedLeadership.Select(p => "\n" + LeadershipSource(view, p.Key) + " · " + p.Value + " Leadership forfeited"));

        public static string Nonparticipants(BattleForceView force, BattleView battle, PlayerView view)
        {
            var participants = battle.Attacker.ParticipantIds.Concat(battle.Defender.ParticipantIds).ToHashSet();
            var units = view.Units.Where(u => u.Side == force.Side && !participants.Contains(u.Id) &&
                (u.Location == force.RegionId || u.Location == battle.RegionId)).ToArray();
            var characters = view.Characters.Where(c => c.Side == force.Side && !participants.Contains(c.Id) &&
                (c.Location == force.RegionId || c.Location == battle.RegionId)).ToArray();
            var groups = units.GroupBy(u => u.Garrison).Select(g =>
                (g.Key ? "Garrison · not fighting: " : "Rear guard: ") + string.Join(" ", g.GroupBy(u => (u.Nation, u.Kind))
                    .Select(n => FigureGrid.Name(n.Key.Nation) + " " + FigureGrid.Symbol(n.Key.Kind) + n.Count())));
            if (characters.Length > 0)
                groups = groups.Concat(new[] { "Not fighting: " + string.Join(", ", characters.Select(c => CharacterLibrary.Name(c.DefinitionId))) });
            return string.Join(" · ", groups);
        }

        private static string LeadershipSource(PlayerView view, string id)
        {
            if (view.Characters.Any(c => c.Id == id)) return CharacterLibrary.Name(view, id);
            var unit = view.Units.FirstOrDefault(u => u.Id == id);
            return unit == null ? "Leader" : unit.Kind == UnitKind.Nazgul ? "Nazgûl" : FigureGrid.Name(unit.Nation) + " " + unit.Kind;
        }

        private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
    }
}
