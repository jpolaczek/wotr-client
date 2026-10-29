using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-420 (owner): each statistic medallion explains only its own number, and a modifier of zero is left out; the
    // force's heading keeps what concerns the whole force and the roll's heading what changes its hits.
    public static class BattleForceText
    {
        public static string Strength(BattleForceView force) => "Strength " + force.Strength;

        public static string Leadership(BattleForceView force, BattleView battle, PlayerView view) => TooltipText.Lines(
            "Leadership " + force.Leadership,
            battle.WhiteRiderActive ? "White Rider: Gandalf and Nazgûl forfeit Leadership for this battle." : "",
            string.Join("\n", force.CancelledAbilities.Select(id => CharacterLibrary.ShortName(view, id) + " · Leadership and abilities cancelled this round")),
            string.Join("\n", force.ForfeitedLeadership.Select(p => LeadershipSource(view, p.Key) + " · " + p.Value + " Leadership forfeited")));

        public static string CombatDice(BattleForceView force) => TooltipText.Lines(
            "Combat dice " + force.CombatDice,
            force.CaptainBonus > 0 ? "Captain bonus +" + force.CaptainBonus : "",
            force.CombatDiceReduction > 0 ? "Help Unlooked For: −" + force.CombatDiceReduction + " Combat dice after the limit, minimum one, for this battle." : "");

        public static string HitTarget(BattleForceView force) => TooltipText.Lines("Hits on " + force.HitTarget + "+", RollModifiers(force));

        // The force's own notes beside its heading; empty when there are none.
        public static string Notes(BattleForceView force, BattleView battle) =>
            force == battle.Attacker && battle.ArrivalBeforeBattle ? "Landed before battle · No advance follows." : "";

        // What changes the hits of the force's Combat roll: its non-zero modifiers and hits spent on a card.
        public static string Roll(BattleForceView force) => TooltipText.Lines(
            RollModifiers(force),
            force.SpentHits > 0 ? TooltipText.Count(force.SpentHits, "hit") + " spent on a Combat card effect." : "");

        private static string RollModifiers(BattleForceView force) => TooltipText.Parts(
            force.CombatModifier != 0 ? "Combat modifier " + Signed(force.CombatModifier) : "",
            force.RerollModifier != 0 ? "Leader reroll modifier " + Signed(force.RerollModifier) : "");

        // UI-420 (owner): one nation's figures in a force read "Gondor · 3 Regular · 1 Elite · 1 Leader".
        public static string Composition(string nation, IEnumerable<UnitKind> kinds) => ArmyMarkerText.Composition(nation, kinds.ToList());

        public static string Nonparticipants(BattleForceView force, BattleView battle, PlayerView view)
        {
            var units = Outside(force, battle, view);
            var groups = units.GroupBy(u => u.Garrison).Select(g =>
                (g.Key ? "Garrison · not fighting: " : "Rear guard: ") + string.Join(" ", g.GroupBy(u => (u.Nation, u.Kind))
                    .Select(n => FigureGrid.Name(n.Key.Nation) + " " + FigureGrid.Symbol(n.Key.Kind) + n.Count())));
            var characters = OutsideCharacters(force, battle, view);
            if (characters.Length > 0)
                groups = groups.Concat(new[] { "Not fighting: " + string.Join(", ", characters.Select(c => CharacterLibrary.Name(c.DefinitionId))) });
            return string.Join(" · ", groups);
        }

        // UI-420 (owner): the ledge's tooltip spells out its symbols, one army per line, rather than repeating its text.
        public static string NonparticipantDetails(BattleForceView force, BattleView battle, PlayerView view)
        {
            var armies = Outside(force, battle, view).GroupBy(u => (u.Garrison, u.Nation))
                .Select(g => (g.Key.Garrison ? "Garrison" : "Rear guard") + TooltipText.Separator + Composition(g.Key.Nation, g.Select(u => u.Kind)));
            var characters = OutsideCharacters(force, battle, view).Select(c => CharacterLibrary.Name(c.DefinitionId)).ToArray();
            return TooltipText.Lines("Not fighting", string.Join("\n", armies),
                characters.Length == 0 ? "" : "Characters" + TooltipText.Separator + string.Join(", ", characters));
        }

        private static UnitView[] Outside(BattleForceView force, BattleView battle, PlayerView view)
        {
            var participants = Participants(battle);
            return view.Units.Where(u => u.Side == force.Side && !participants.Contains(u.Id) &&
                (u.Location == force.RegionId || u.Location == battle.RegionId)).ToArray();
        }

        private static CharacterView[] OutsideCharacters(BattleForceView force, BattleView battle, PlayerView view)
        {
            var participants = Participants(battle);
            return view.Characters.Where(c => c.Side == force.Side && !participants.Contains(c.Id) &&
                (c.Location == force.RegionId || c.Location == battle.RegionId)).ToArray();
        }

        private static HashSet<string> Participants(BattleView battle) =>
            battle.Attacker.ParticipantIds.Concat(battle.Defender.ParticipantIds).ToHashSet();

        private static string LeadershipSource(PlayerView view, string id)
        {
            if (view.Characters.Any(c => c.Id == id)) return CharacterLibrary.Name(view, id);
            var unit = view.Units.FirstOrDefault(u => u.Id == id);
            return unit == null ? "Leader" : TooltipText.Unit(unit.Nation, unit.Kind);
        }

        private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
    }
}
