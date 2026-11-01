using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-439 (owner's playtest, 2026-11-01): every printed ability of every character card has its own small icon
    // (graphicalassets/abilities), and "the same icons every time a button stands for the skill". This is the one map
    // from an ability to its icon: the roster row's icons, the character window's lines, the component action buttons,
    // Narya's activation, the battle's Evil Knowledge, reactions and combat replacements, and the Hunt's Guide responses
    // all ask it. An ability's icon is Art/UI/abilities/<printed ability id>; a command, a reaction or a decision that
    // carries its own id names the printed ability it uses first; one card prints an ability in its own form
    // ("<card id>#<ability id>", as character-ability-summaries.json).
    public static class AbilityArt
    {
        public const string Folder = "Wotr/Art/UI/abilities/";
        // An id that names no printed ability shows the ability scroll (every id of the content has its own icon).
        public const string Unknown = "Wotr/Art/UI/characters/ability";
        public const string DarkChieftain = "dark-chieftain";
        private const string GandalfWhiteEntry = "not-minion-for-gandalf-white";

        // The ids a command or a reaction carries for the printed ability it uses. The Kings' Ruler abilities drop
        // their prefix ("kings.call-to-arms"; Printed).
        private static readonly IReadOnlyDictionary<string, string> Commands = new Dictionary<string, string>
        {
            ["activate-balrog"] = "balrog-activation",
            ["deactivate-balrog"] = "balrog-activation",
            ["use-balrog"] = "balrog-activation",
            ["gothmog-recruit"] = "fling-them-into-the-fray",
            ["gothmog-upgrade"] = "fling-them-into-the-fray",
            ["voice-recruit"] = "muster-each-isengard-settlement-or-upgrade-two",
            ["voice-upgrade"] = "muster-each-isengard-settlement-or-upgrade-two",
            [NamedRings.NenyaReaction] = "nenya",
            ["lords.chief.spear-of-terror"] = "spear-of-terror",
            ["lords.chief.he-sees-he-knows"] = "he-sees-he-knows",
            // The characters' reactions (the server's CharacterReactions, CouncilGuideReactions and FateGimli).
            ["character.grey-draw"] = "guide-draw-after-event-die-card",
            ["character.white-rider"] = "forfeit-leadership-to-cancel-nazgul-for-battle",
            ["character.sorcerer"] = "draw-after-first-round-combat-card",
            ["character.merry-guide"] = "guide-separate-to-reduce-hunt-one",
            ["character.pippin-guide"] = "guide-separate-to-reduce-hunt-one",
            ["character.gollum-reveal"] = "voluntary-reveal-to-reduce-hunt-one",
            ["council.boromir-guide"] = "guide-separate-to-reduce-hunt-one",
            ["council.gimli-guide"] = "guide-separate-to-reduce-hunt-one",
            ["council.legolas-guide"] = "guide-separate-to-reduce-hunt-one",
            ["fate.gimli-guide"] = "guide-separate-to-reduce-hunt-one",
            ["council.hobbits-cancel-hunt.lords.character.merry"] = "guide-cancel-standard-hunt",
            ["council.hobbits-cancel-hunt.lords.character.pippin"] = "guide-cancel-standard-hunt"
        };

        // The commands that stand for an ability by their kind alone.
        private static readonly IReadOnlyDictionary<CommandKind, string> Kinds = new Dictionary<CommandKind, string>
        {
            [CommandKind.UseNarya] = "narya",
            [CommandKind.UseVilya] = "vilya",
            [CommandKind.DeclineVilya] = "vilya",
            [CommandKind.UseEvilKnowledge] = "evil-knowledge"
        };

        // The Dark Chieftains print "Gandalf the White entry" as Dark Chieftain, with the shared Ruler die.
        private static readonly IReadOnlyDictionary<string, string> CardForms = new Dictionary<string, string>
        {
            ["kings.character.black-serpent#" + GandalfWhiteEntry] = DarkChieftain,
            ["kings.character.shadow-of-mirkwood#" + GandalfWhiteEntry] = DarkChieftain,
            ["kings.character.ugluk#" + GandalfWhiteEntry] = DarkChieftain
        };

        // The printed ability an id names: a command's or reaction's own, else the id without an expansion's prefix.
        public static string Printed(string abilityOrCommandId)
        {
            var id = abilityOrCommandId ?? "";
            if (Commands.TryGetValue(id, out var printed))
                return printed;
            var bare = id.Substring(id.LastIndexOf('.') + 1);
            return Commands.TryGetValue(bare, out printed) ? printed : bare;
        }

        // The printed ability a command kind stands for ("narya" for Use Narya), or "".
        public static string Printed(CommandKind kind) => Kinds.TryGetValue(kind, out var printed) ? printed : "";

        // The icon's key: the card's own form of the ability, or the printed ability itself.
        public static string Key(string cardId, string abilityOrCommandId)
        {
            var printed = Printed(abilityOrCommandId);
            return CardForms.TryGetValue((cardId ?? "") + "#" + printed, out var own) ? own : printed;
        }

        // The icon of an ability on a card (a Resources path); the scroll for an id that names no printed ability.
        public static string For(string cardId, string abilityOrCommandId)
        {
            var icon = Find(cardId, abilityOrCommandId);
            return icon.Length > 0 ? icon : Unknown;
        }

        // The icon, or "" for an id that names no printed ability (a badge for it is then left out).
        public static string Find(string cardId, string abilityOrCommandId)
        {
            var key = Key(cardId, abilityOrCommandId);
            return Known(key) ? Folder + key : "";
        }

        public static string For(string abilityOrCommandId) => For("", abilityOrCommandId);

        // The icon of the ability a command kind stands for, or "" when the kind stands for none.
        public static string For(CommandKind kind) => Printed(kind) is { Length: > 0 } printed ? For(printed) : "";

        // Every icon key the content needs: each printed ability id, and each card's own form.
        public static IReadOnlyCollection<string> Keys => CharacterAbilityLibrary.AbilityIds.Concat(CardForms.Values).Distinct().ToList();

        private static bool Known(string key) => CharacterAbilityLibrary.Title(key) != null || CardForms.Values.Contains(key);
    }
}
