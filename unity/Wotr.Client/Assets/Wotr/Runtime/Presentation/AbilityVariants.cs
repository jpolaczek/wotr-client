using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-440 (owner): an ability whose commands do different things stands as one icon per variant, side by side in one
    // group, each badged with what it does (AbilityBadges); the group's slots are fixed, so nothing shifts while a variant
    // is not offered. An action names its variant by its command's ability id (Voice, Fling, the Balrog's activation) or
    // by its command kind (the moves and attacks of a Muster die, Great Sorcery, the Balrog and Far-sighted).
    public static class AbilityVariants
    {
        public sealed class Variant
        {
            public Variant(string key, string name, string badge)
            {
                Key = key;
                Name = name;
                Badge = badge;
            }

            public string Key { get; }
            public string Name { get; }
            public string Badge { get; }
        }

        // The Balrog activates or deactivates with one command at a time: one slot, no badge.
        public const string Activation = "activation";

        private static readonly Variant[] Single = { new Variant("", "", "") };

        private static readonly IReadOnlyDictionary<string, Variant[]> Groups = new Dictionary<string, Variant[]>
        {
            ["muster-each-isengard-settlement-or-upgrade-two"] = Recruitment("voice"),
            ["fling-them-into-the-fray"] = Recruitment("gothmog"),
            ["balrog-activation"] = new[]
            {
                new Variant(Activation, "Activate or deactivate", ""),
                Kind(CommandKind.MoveArmies, "Move an army"),
                Kind(CommandKind.AttackArmy, "Attack"),
                Kind(CommandKind.MoveShadowCharacters, "Move alone")
            },
            ["muster-as-army-once-per-turn"] = Armies(),
            ["great-sorcery"] = Armies(),
            ["far-sighted"] = new[]
            {
                Kind(CommandKind.MoveFellowship, "Move the Fellowship"),
                Kind(CommandKind.MoveCompanions, "Move companions"),
                Kind(CommandKind.SeparateCompanions, "Separate companions")
            }
        };

        // The variants of a printed ability, in their fixed order; an ability with one use has one unnamed variant.
        public static IReadOnlyList<Variant> For(string printedAbility) =>
            Groups.TryGetValue(printedAbility ?? "", out var group) ? group : Single;

        public static bool Grouped(string printedAbility) => Groups.ContainsKey(printedAbility ?? "");

        // The variant an action uses of its printed ability ("" for an ability with one use).
        public static string KeyOf(ComponentAction action, string printedAbility)
        {
            if (!Grouped(printedAbility))
                return "";
            var command = action.Character?.AbilityId ?? "";
            if (command is CharacterAbilityPlaces.ActivateBalrog or CharacterAbilityPlaces.DeactivateBalrog)
                return Activation;
            return command.Length > 0 ? command : action.Kind.ToString();
        }

        public static IEnumerable<string> GroupedAbilities => Groups.Keys;

        private static Variant[] Recruitment(string command) => new[]
        {
            new Variant(command + "-recruit", "Recruit", AbilityBadges.Muster),
            new Variant(command + "-upgrade", "Upgrade", AbilityBadges.Upgrade)
        };

        private static Variant[] Armies() => new[] { Kind(CommandKind.MoveArmies, "Move an army"), Kind(CommandKind.AttackArmy, "Attack") };

        private static Variant Kind(CommandKind kind, string name) => new Variant(kind.ToString(), name, AbilityBadges.For(kind, ""));
    }
}
