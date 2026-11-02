using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-440 (owner): the icons a card shows for its printed abilities, in printed order, an ability with variants
    // (AbilityVariants) as its variants side by side; fixed for the card, so nothing shifts while an action comes or goes.
    // Every figure action that uses one of the card's printed abilities — its own command, a Muster die's move or attack,
    // Far-sighted, Narya — belongs to one slot; an entry never does (it keeps its own control).
    public static class CharacterAbilitySlots
    {
        public sealed class Slot
        {
            public Slot(CharacterAbilityText ability, AbilityVariants.Variant variant)
            {
                Ability = ability;
                Variant = variant;
            }

            public CharacterAbilityText Ability { get; }
            public AbilityVariants.Variant Variant { get; }
            public string Key => SlotKey(Ability.Id, Variant.Key);
            public string Name => "character-ability-" + Ability.Id + (Variant.Key.Length > 0 ? "-" + Variant.Key : "");
        }

        public static IReadOnlyList<Slot> For(string definitionId) => CharacterAbilityLibrary.For(definitionId ?? "")
            .SelectMany(ability => AbilityVariants.For(ability.Id).Select(variant => new Slot(ability, variant)))
            .ToList();

        // The slot an action uses among the card's printed abilities, or "" when it uses none of them.
        public static string KeyOf(ComponentAction action, ISet<string> printed)
        {
            var ability = PrintedOf(action, printed);
            return ability.Length == 0 ? "" : SlotKey(ability, AbilityVariants.KeyOf(action, ability));
        }

        // UI-380/UI-439: the printed ability an action uses — its command's own id ("voice-recruit", "kings.call-to-arms"),
        // its movement cost's or Far-sighted's ability, or its command kind (Narya) — when the card prints it.
        public static string PrintedOf(ComponentAction action, ISet<string> printed)
        {
            var ability = Printed(action);
            return printed.Contains(ability) ? ability : "";
        }

        // The printed ability an action uses, whichever card prints it; "" for an entry.
        public static string Printed(ComponentAction action)
        {
            if (action.Kind == CommandKind.EnterCharacter)
                return "";
            var command = action.Character?.AbilityId ?? "";
            if (command.Length == 0)
                command = action.AbilityId;
            return command.Length > 0 ? AbilityArt.Printed(command) : AbilityArt.Printed(action.Kind);
        }

        private static string SlotKey(string ability, string variant) => ability + "#" + variant;
    }
}
