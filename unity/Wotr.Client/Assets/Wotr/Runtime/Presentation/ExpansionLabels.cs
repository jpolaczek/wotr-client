using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ExpansionLabels
    {
        public static bool Special(DieView die) => die.Source.Length > 0 && die.Source != "standard";
        public static string DieSource(DieView die) => die.Source == "ruler" ? "Ruler"
            : Special(die) ? DieOrigin(die.Source.Split('.').Last()) : "Action die";

        private static string DieOrigin(string key) => key switch
        {
            "narya" => "Narya", "nenya" => "Nenya", "vilya" => "Vilya",
            "balrog" => "Balrog", "gothmog" => "Gothmog",
            _ => FigureGrid.Name(key)
        };

        // UI-346: a die, a token or an ability id as a recorded text names it.
        public static string StandardDie(string id) => id.StartsWith("FreePeoples.", System.StringComparison.Ordinal)
            ? "a Free Peoples action die" : "a Shadow action die";

        public static string DieName(string id) => id switch
        {
            "kings.die.fp.ruler" => "the Free Peoples Ruler die",
            "kings.die.sh.ruler" => "the Shadow Ruler die",
            _ => id.StartsWith("lords.die.", System.StringComparison.Ordinal) ? "the " + DieOrigin(id.Split('.').Last()) + " die" : "a die"
        };

        public static string RulerToken(string id) => ActionCardText.Source("kings.sovereign." + id.Split('.').Last()) + "'s Ruler token";

        // The commands' ability ids that no printed card titles; the others read as their card's title.
        private static readonly System.Collections.Generic.IReadOnlyDictionary<string, string> CommandAbilities =
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["use-balrog"] = "Use the Balrog",
                ["activate-balrog"] = "Activate the Balrog",
                ["deactivate-balrog"] = "Deactivate the Balrog",
                ["gothmog-recruit"] = "Fling into the fray (recruit)",
                ["gothmog-upgrade"] = "Fling into the fray (upgrade)"
            };

        public static string Ability(string id) => CommandAbilities.TryGetValue(id, out var name) ? name : CharacterAbilityLibrary.Title(id);
        public static string DieFace(DieView die) => AssetCatalog.Current.Die(die.Face) + (die.RemoveSymbol ? " ×" : "");
        public static string DieCost(PlayerView view, TurnDraft draft, DieView die)
        {
            var token = view.RulerTokens.FirstOrDefault(t => t.Id == draft.Payload.RulerTokenId);
            return DieFace(die) + (die.Source == "ruler" && token != null && die.Face != token.Face
                ? " → " + AssetCatalog.Current.Die(token.Face) : "");
        }
        public static string TokenCost(PlayerView view, TurnDraft draft)
        {
            if (draft.Payload.RulerTokenId.Length == 0)
                return "";
            var die = view.Dice.FirstOrDefault(d => d.Id == draft.Payload.DieId);
            return Token(view, draft.Payload.RulerTokenId) + "'s Ruler token + Ruler die" +
                (die == null ? "" : " " + DieCost(view, draft, die)) + " · ";
        }
        // UI-97: one short line (source, face and state); recovery rules stay in the Why? and help panels.
        public static string DieDetail(DieView die) => DieDescriptions.Summary(die);
        public static string Ring(string id) => id.StartsWith("lords.") ? FigureGrid.Name(id.Split('.').Last()) : "Elven Ring";
        public static string CouncilToken(string id) => id == ExpansionDraft.CouncilPolitics ? "Council · Politics" : "Council · Travel";
        public static string Token(PlayerView view, string id) => view.Sovereigns.FirstOrDefault(s =>
            view.RulerTokens.Any(t => t.Id == id && t.SovereignId == s.Id))?.Title ?? FigureGrid.Name(id.Split('.').Last());
        public static string Tile(CorruptionTileView tile) => tile.Kings ? "♛" : tile.Value.HasValue
            ? tile.Value.Value + (tile.Reveal ? " ◉" : "") : "▧";
    }
}
