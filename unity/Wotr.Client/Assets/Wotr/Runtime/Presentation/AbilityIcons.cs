using System.Collections.Generic;
using System.Linq;

namespace Wotr.Client
{
    // UI-391 (owner): every printed ability has a small thematic icon on its character's roster row — a battle for a
    // combat bonus, the Guide's crown, the Politics track, a muster, a move, a card draw, the Hunt's eye or die, a Ring,
    // the Balrog, Gollum, a level, a change of version, a bond to a place — and the ability scroll otherwise. The art is
    // under Art/UI and replaceable.
    public static class AbilityIcons
    {
        private const string Battle = "Wotr/Art/UI/actions/battle";
        private const string Leadership = "Wotr/Art/UI/battle/leadership";
        private const string Guide = "Wotr/Art/UI/fellowship/guide-crown";
        private const string Eye = "Wotr/Art/UI/hunt/eye";
        private const string HuntDie = "Wotr/Art/UI/hunt/die";
        private const string Ring = "Wotr/Art/UI/icons/ring";
        private const string Balrog = "Wotr/Art/UI/dice/faces/use-balrog";
        private const string Gollum = "Wotr/Art/UI/hunt/smeagol";
        private const string Versions = "Wotr/Art/UI/fellowship/versions";
        private const string Bound = "Wotr/Art/UI/actions/pinned";
        private const string Fate = "Wotr/Art/UI/icons/effects";
        private const string Corrupted = "Wotr/Art/UI/kings/status-corrupted";
        private const string Return = "Wotr/Art/UI/characters/may-return";
        private const string Flight = "Wotr/Art/UI/characters/flight";
        private const string Card = "Wotr/Art/UI/icons/cards";
        // A Regular turned into an Elite: a plus, so it is not read as the portrait's level star.
        private const string Upgrade = "Wotr/Art/UI/actions/expand";

        private static readonly IReadOnlyDictionary<string, string> Icons = new Dictionary<string, string>
        {
            ["add-gollum-when-no-companions"] = Gollum,
            ["smeagol-departure"] = Gollum,
            ["always-guide"] = Guide,
            ["enter-and-guide-when-no-companions"] = Guide,
            ["guide-conditional-narya-die"] = Guide,
            ["guide-draw-after-event-die-card"] = Guide,
            ["guide-first-move-to-fellowship-box"] = Guide,
            ["guide-hide-with-any-action-die"] = Guide,
            ["guide-cancel-standard-hunt"] = Eye,
            ["guide-separate-to-reduce-hunt-one"] = Eye,
            ["ignore-standard-numbered-reveal"] = Eye,
            ["voluntary-reveal-to-reduce-hunt-one"] = Eye,
            ["art-of-disappearing"] = Eye,
            ["he-sees-he-knows"] = Eye,
            ["evil-knowledge"] = HuntDie,
            ["lord-of-the-bats"] = HuntDie,
            ["balrog-activation"] = Balrog,
            ["fire-and-shadow"] = Balrog,
            ["flame-of-udun"] = Balrog,
            ["call-to-arms"] = AbilityPictures.Muster,
            ["iron-guard"] = AbilityPictures.Muster,
            ["mustering-of-the-mark"] = AbilityPictures.Muster,
            ["muster-as-army-once-per-turn"] = AbilityPictures.Muster,
            ["muster-each-isengard-settlement-or-upgrade-two"] = AbilityPictures.Muster,
            ["isengard-elites-have-leadership"] = Leadership,
            ["combat-strength-plus-one"] = Battle,
            ["champion-dwarves"] = Battle,
            ["champion-elves"] = Battle,
            ["champion-gondor"] = Battle,
            ["champion-north"] = Battle,
            ["combat-card-level-three"] = Battle,
            ["fling-them-into-the-fray"] = Battle,
            ["forth-eorlingas"] = Battle,
            ["i-command"] = Battle,
            ["red-wrath"] = Battle,
            ["valour-of-the-elven-people"] = Battle,
            ["spear-of-terror"] = Battle,
            ["forfeit-leadership-to-cancel-nazgul-for-battle"] = Leadership,
            ["draw-after-first-round-combat-card"] = AbilityPictures.Draw,
            ["palantir-of-anarion"] = AbilityPictures.Draw,
            ["household-of-elrond"] = Card,
            ["great-sorcery"] = Card,
            ["shadows-in-the-north"] = Card,
            ["far-sighted"] = AbilityPictures.Move,
            ["move-four-alone-or-with-one-hobbit"] = AbilityPictures.Move,
            ["movement-level-two"] = AbilityPictures.Move,
            ["we-march-day-and-night"] = AbilityPictures.Move,
            ["mark-of-the-white-hand"] = Flight,
            ["dwarvenking"] = Upgrade,
            ["elvenking"] = Upgrade,
            ["stewardship"] = Upgrade,
            ["level-of-highest-other-companion"] = AbilityPictures.Upgrade,
            ["heir-of-bard"] = AbilityPictures.Entry,
            ["entry-activates-all-free-peoples"] = AbilityPictures.Politics,
            ["messenger-dwarves"] = AbilityPictures.Politics,
            ["messenger-gondor"] = AbilityPictures.Politics,
            ["messenger-north"] = AbilityPictures.Politics,
            ["prince-of-mirkwood"] = AbilityPictures.Politics,
            ["politics-dwarves-at-erebor"] = AbilityPictures.Politics,
            ["politics-dwarves-at-fate-homelands"] = AbilityPictures.Politics,
            ["politics-elves-at-stronghold"] = AbilityPictures.Politics,
            ["politics-gondor-at-city-or-stronghold"] = AbilityPictures.Politics,
            ["narya"] = Ring,
            ["nenya"] = Ring,
            ["vilya"] = Ring,
            ["may-transform-to-aragorn"] = Versions,
            ["may-transform-to-white"] = Versions,
            ["not-minion-for-gandalf-white"] = Versions,
            ["cannot-leave-lorien"] = Bound,
            ["cannot-leave-orthanc"] = Bound,
            ["cannot-leave-rivendell"] = Bound,
            ["fate-of-mirkwood"] = Fate,
            ["fate-of-the-lonely-mountain"] = Fate,
            ["fate-of-the-white-tower"] = Fate,
            ["corrupted-ruler"] = Corrupted,
            ["return-after-fellowship-elimination-except-mordor"] = Return
        };

        // Every picture the map can name, the scroll included (a test loads each one's art).
        public static IEnumerable<string> Pictures => Icons.Values.Append(AbilityPictures.Ability).Distinct();

        // The icon of a printed ability (its id without an expansion's prefix).
        public static string For(string abilityId)
        {
            var id = abilityId ?? "";
            var printed = id.Substring(id.IndexOf('.') + 1);
            return Icons.TryGetValue(printed, out var icon) ? icon : AbilityPictures.Ability;
        }
    }
}
