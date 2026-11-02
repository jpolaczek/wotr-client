using System.Collections.Generic;

namespace Wotr.Client
{
    // UI-440 (owner): an ability that is not used from its icon says in the icon's tooltip where it is used — a reaction
    // on the object that offers it (board-first rule), a triggered ability by itself, a change of version from the
    // Characters row. Passive abilities and the ones the icon uses have no line here.
    public static class AbilityUsePlaces
    {
        private const string Portrait = "Offered in battle: click his portrait";
        private const string BesideCard = "Offered in battle: click it beside your Combat card";
        private const string Siege = "Offered when the siege may go on: click a lit Shadow Regular";
        private const string HuntResponse = "Offered in the Hunt window: click this Guide's response";
        private const string Version = "Used from his Characters row with a Will of the West die";

        private static readonly IReadOnlyDictionary<string, string> Places = new Dictionary<string, string>
        {
            ["guide-draw-after-event-die-card"] =
                "Offered after you play an Event card with an Event die: click his glowing Guide portrait in the Fellowship panel",
            ["guide-separate-to-reduce-hunt-one"] = HuntResponse,
            ["guide-cancel-standard-hunt"] = HuntResponse,
            ["voluntary-reveal-to-reduce-hunt-one"] = "Offered in the Hunt window: click Gollum's response",
            ["forfeit-leadership-to-cancel-nazgul-for-battle"] = Portrait,
            ["draw-after-first-round-combat-card"] = Portrait,
            ["evil-knowledge"] = "Offered in a siege: click his portrait",
            ["spear-of-terror"] = "Offered after a Character Event card: click the lit Character deck",
            ["he-sees-he-knows"] = "Offered when the Fellowship is declared: click his figure",
            ["vilya"] = "Offered after your action: click Vilya on the console",
            ["nenya"] = "Offered when a standard Eye is drawn: click Nenya on the console",
            ["heir-of-bard"] = BesideCard,
            ["shadows-in-the-north"] = BesideCard,
            ["red-wrath"] = BesideCard,
            ["lord-of-the-bats"] = BesideCard,
            ["i-command"] = BesideCard,
            ["palantir-of-anarion"] = "Offered after the Ruler die's Event: click the lit deck",
            ["fate-of-the-lonely-mountain"] = Siege,
            ["fate-of-the-white-tower"] = Siege,
            ["fate-of-mirkwood"] = Siege,
            ["return-after-fellowship-elimination-except-mordor"] = "Happens by itself; then choose his region on the map",
            ["art-of-disappearing"] = "Happens by itself; then choose his region on the map",
            ["may-transform-to-white"] = Version,
            ["may-transform-to-aragorn"] = Version
        };

        // Where this printed ability is used, or "" when its icon (or nothing) uses it.
        public static string For(string abilityId) => Places.TryGetValue(abilityId ?? "", out var place) ? place : "";

        public static IEnumerable<string> Abilities => Places.Keys;
    }
}
