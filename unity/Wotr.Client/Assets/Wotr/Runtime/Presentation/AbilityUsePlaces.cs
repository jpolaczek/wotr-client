using System.Collections.Generic;

namespace Wotr.Client
{
    // UI-440 (owner): an ability that is not used from its icon says in the icon's tooltip where it is used — a reaction
    // on the object that offers it (board-first rule), a triggered ability by itself, a change of version from the
    // Characters row. Passive abilities and the ones the icon uses have no line here. The base names only the places it
    // offers: the Guide's draw on its deck (ReactionDecks), the battle portraits' reactions (BattleReactions), the Hunt
    // window's responses, Vilya's console socket and Evil Knowledge's icon in the battle footer (BattleChoices). The
    // expansions' other objects (Spear of Terror's deck, a figure's He Sees, He Knows, Nenya's socket, the combat
    // replacements beside the Combat card, the Palantír of Anárion's decks, a siege's continuation) are not the base's, so
    // those abilities' icons only describe them.
    public static class AbilityUsePlaces
    {
        private const string Portrait = "Offered in battle: click his portrait";
        private const string HuntResponse = "Offered in the Hunt window: click this Guide's response";
        private const string Version = "Used from his Characters row with a Will of the West die";
        private const string Return = "Happens by itself; then choose his region on the map";

        private static readonly IReadOnlyDictionary<string, string> Places = new Dictionary<string, string>
        {
            ["guide-draw-after-event-die-card"] = "Offered after the Event card: click the lit deck",
            ["guide-separate-to-reduce-hunt-one"] = HuntResponse,
            ["guide-cancel-standard-hunt"] = HuntResponse,
            ["voluntary-reveal-to-reduce-hunt-one"] = "Offered in the Hunt window: click Gollum's response",
            ["forfeit-leadership-to-cancel-nazgul-for-battle"] = Portrait,
            ["draw-after-first-round-combat-card"] = Portrait,
            ["evil-knowledge"] = "Offered in a siege: click its icon at the foot of the battle window",
            ["vilya"] = "Offered after your action: click Vilya on the console",
            ["return-after-fellowship-elimination-except-mordor"] = Return,
            ["art-of-disappearing"] = Return,
            ["may-transform-to-white"] = Version,
            ["may-transform-to-aragorn"] = Version
        };

        // Where this printed ability is used, or "" when its icon (or nothing) uses it.
        public static string For(string abilityId) => Places.TryGetValue(abilityId ?? "", out var place) ? place : "";

        public static IEnumerable<string> Abilities => Places.Keys;
    }
}
