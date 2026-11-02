using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-439: an ability whose commands stand side by side and do different things — Saruman's Voice and Gothmog's Fling
    // Them into the Fray recruit or upgrade, the Mouth of Sauron's Muster die (and Great Sorcery's) moves an army or
    // attacks — keeps its one icon (AbilityArt) on each button, with a small badge of what that command does. UI-440
    // (owner): every variant is badged, so Far-sighted's three moves (the Fellowship, companions, a separation) and the
    // Balrog's moves (an army, an attack, alone) are told apart too; one Balrog activation command at a time needs none.
    public static class AbilityBadges
    {
        public const string Muster = "Wotr/Art/UI/dice/faces/muster";
        public const string Upgrade = "Wotr/Art/UI/characters/level";
        public const string Move = "Wotr/Art/UI/characters/movement";
        public const string Attack = "Wotr/Art/UI/actions/battle";
        public const string Fellowship = "Wotr/Art/UI/icons/fellowship";
        public const string Companions = "Wotr/Art/UI/icons/characters";
        public const string Alone = "Wotr/Art/UI/characters/on-board";

        // The badge of a command that uses an ability, or "" when nothing beside it uses the ability differently.
        public static string For(CommandKind kind, string commandId)
        {
            var badge = ForKind(kind);
            if (badge.Length > 0)
                return badge;
            var id = commandId ?? "";
            if (id.EndsWith("-recruit", System.StringComparison.Ordinal))
                return Muster;
            return id.EndsWith("-upgrade", System.StringComparison.Ordinal) ? Upgrade : "";
        }

        private static string ForKind(CommandKind kind) => kind switch
        {
            CommandKind.AttackArmy => Attack,
            CommandKind.MoveArmies => Move,
            CommandKind.MoveFellowship => Fellowship,
            CommandKind.MoveCompanions => Companions,
            CommandKind.SeparateCompanions or CommandKind.MoveShadowCharacters => Alone,
            _ => ""
        };
    }
}
