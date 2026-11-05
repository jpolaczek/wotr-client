using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-461 (owner, 2026-11-03): "Will of the West can turn into another die but can also be used by itself". A chosen Will
    // of the West die lights only what it does as itself until the console's face menu turns it into another result; then
    // it lights exactly what a die of that result would (UI-389). Rules: Will of the West may be used as any other result
    // (base rules, Action dice); Gandalf the White and Aragorn enter with Will of the West itself; the Balrog's
    // deactivation takes a Will of the West result, or a Character result while Gandalf the Grey is in his region (LORDS
    // pp. 23–25; the server pays Will of the West as itself there); the removals of Denethor's Folly, The King's Welcome,
    // the Gates, the Palantír cards and a Lore without a Ring take Will of the West itself. A character's political
    // ability (with the Council's messengers), Narya and a region's protection take any die. The table is presentation
    // only: the server lists the dice of every use and validates every payment.
    public static class WillOfTheWestUses
    {
        private const string DeactivateBalrog = "deactivate-balrog";

        private static readonly HashSet<string> ItselfEntries = new HashSet<string>
        {
            "base.character.gandalf-white", "base.character.aragorn"
        };

        private static readonly HashSet<CommandKind> ItselfRemovals = new HashSet<CommandKind>
        {
            CommandKind.DiscardPalantir, CommandKind.DiscardBaradPalantir, CommandKind.DiscardDenethor,
            CommandKind.DiscardLore, CommandKind.DiscardGates, CommandKind.DiscardWelcome
        };

        // The faces the menu offers, in their fixed slots.
        public static readonly IReadOnlyList<DieFace> Faces = new[] { DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event };

        // Whether a Will die turned into face (null: the die as itself) pays an option that is not a character's action.
        public static bool Pays(CommandKind kind, DieFace? face)
        {
            if (ItselfRemovals.Contains(kind))
                return !face.HasValue;
            if (kind == CommandKind.SeparateCompanions)
                return face == DieFace.Character;
            return true;
        }

        // Whether a Will die turned into face (null: as itself) pays a character's entry or ability.
        public static bool Pays(CommandKind kind, CharacterActionOption action, DieFace? face) =>
            CharacterPolitics.IsPolitical(action.AbilityId) || Needed(kind, action) == face;

        // The result a character's action takes from a Will die (null: Will of the West itself), by the server's costs:
        // CharacterEntry.RequiredCost, LordsCharacterAbilities.Plan, KingsUpgrades and SovereignCommands.
        private static DieFace? Needed(CommandKind kind, CharacterActionOption action) => kind switch
        {
            CommandKind.EnterCharacter => ItselfEntries.Contains(action.CharacterId) ? (DieFace?)null : DieFace.Muster,
            CommandKind.UseCharacterAbility => action.AbilityId == DeactivateBalrog ? (DieFace?)null : DieFace.Muster,
            CommandKind.RemoveCorruptedSovereign => DieFace.Character,
            _ => DieFace.Muster
        };
    }
}
