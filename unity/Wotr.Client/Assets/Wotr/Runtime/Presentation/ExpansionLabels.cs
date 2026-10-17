using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ExpansionLabels
    {
        public static bool Special(DieView die) => die.Source.Length > 0 && die.Source != "standard";
        public static string DieSource(DieView die) => die.Source == "ruler" ? "Ruler" : die.Source.Split('.').Last() switch
        {
            "narya" => "Narya", "nenya" => "Nenya", "vilya" => "Vilya",
            "balrog" => "Balrog", "gothmog" => "Gothmog",
            _ => Special(die) ? FigureGrid.Name(die.Source) : "Action die"
        };
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
            return Token(view, draft.Payload.RulerTokenId) + " token + Ruler" +
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
