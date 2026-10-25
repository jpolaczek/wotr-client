using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-286 (owner's decision after the object controls audit): a character's ability is a small picture of what it
    // does — a muster, an upgrade, the Politics track, a move or an attack it pays, an entry — and the ability scroll
    // otherwise. The pictures are resources under Art/UI.
    public static class AbilityPictures
    {
        public const string Muster = "Wotr/Art/UI/dice/faces/muster";
        public const string Upgrade = "Wotr/Art/UI/characters/level";
        public const string Politics = "Wotr/Art/UI/actions/political";
        public const string Move = "Wotr/Art/UI/characters/movement";
        public const string Attack = "Wotr/Art/UI/actions/battle";
        public const string Entry = "Wotr/Art/UI/characters/on-board";
        public const string Ability = "Wotr/Art/UI/characters/ability";

        public static string For(CommandKind kind, string ability)
        {
            if (kind == CommandKind.EnterCharacter)
                return Entry;
            if (kind == CommandKind.AttackArmy)
                return Attack;
            if (kind is CommandKind.MoveArmies or CommandKind.MoveShadowCharacters or CommandKind.MoveCompanions)
                return Move;
            if (ability.StartsWith("politics-"))
                return Politics;
            if (ability.EndsWith("-recruit"))
                return Muster;
            if (ability.EndsWith("-upgrade"))
                return Upgrade;
            return Ability;
        }
    }
}
