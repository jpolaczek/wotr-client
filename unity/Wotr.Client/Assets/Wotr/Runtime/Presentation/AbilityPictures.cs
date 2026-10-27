using System.Collections.Generic;
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
        public const string Draw = "Wotr/Art/UI/dice/faces/card-draw";

        // UI-348: the Kings abilities — the Ruler die's recruitments, the sovereigns' upgrades, the Palantír of Anárion and
        // Far-sighted — by what they do.
        private static readonly IReadOnlyDictionary<string, string> Kings = new Dictionary<string, string>
        {
            ["kings.call-to-arms"] = Muster,
            ["kings.iron-guard"] = Muster,
            ["kings.mustering-of-the-mark"] = Muster,
            ["kings.dwarvenking"] = Upgrade,
            ["kings.stewardship"] = Upgrade,
            ["kings.elvenking"] = Upgrade,
            ["kings.palantir-of-anarion"] = Draw,
            ["kings.far-sighted"] = Move
        };

        public static string For(CommandKind kind, string ability)
        {
            if (kind == CommandKind.EnterCharacter)
                return Entry;
            if (kind == CommandKind.AttackArmy)
                return Attack;
            if (kind is CommandKind.MoveArmies or CommandKind.MoveShadowCharacters or CommandKind.MoveCompanions)
                return Move;
            if (Kings.TryGetValue(ability, out var kings))
                return kings;
            // UI-349: a messenger (Messenger of the Dwarves, the Council's messengers) activates and advances a nation.
            if (ability.StartsWith("politics-") || ability.StartsWith("messenger-"))
                return Politics;
            if (ability.EndsWith("-recruit"))
                return Muster;
            if (ability.EndsWith("-upgrade"))
                return Upgrade;
            return Ability;
        }
    }
}
