using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-439: an ability whose commands stand side by side and do different things — Saruman's Voice and Gothmog's Fling
    // Them into the Fray recruit or upgrade, the Mouth of Sauron's Muster die (and Great Sorcery's) moves an army or
    // attacks — keeps its one icon (AbilityArt) on each button, with a small badge of what that command does. Commands
    // that do the same (Far-sighted's moves, one Balrog command at a time) need none; their tooltips name them.
    public static class AbilityBadges
    {
        public const string Muster = "Wotr/Art/UI/dice/faces/muster";
        public const string Upgrade = "Wotr/Art/UI/characters/level";
        public const string Move = "Wotr/Art/UI/characters/movement";
        public const string Attack = "Wotr/Art/UI/actions/battle";

        // The badge of a command that uses an ability, or "" when nothing beside it uses the ability differently.
        public static string For(CommandKind kind, string commandId)
        {
            if (kind == CommandKind.AttackArmy)
                return Attack;
            if (kind == CommandKind.MoveArmies)
                return Move;
            var id = commandId ?? "";
            if (id.EndsWith("-recruit", System.StringComparison.Ordinal))
                return Muster;
            return id.EndsWith("-upgrade", System.StringComparison.Ordinal) ? Upgrade : "";
        }
    }
}
