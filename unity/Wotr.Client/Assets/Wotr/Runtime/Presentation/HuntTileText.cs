using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-141: what a printed Hunt tile means, from its face (HuntTileFaceView). Words for the tooltip; the artwork
    // shows the same face. Damage values the server resolves (Eye, die) are not guessed here.
    public static class HuntTileText
    {
        public const string Smeagol = "smeagol";

        public static string Title(HuntTileFaceView face) => face.Pool == Smeagol ? "Sméagol" :
            face.Value == "eye" ? "Eye" : face.Value == "die" ? "Die" : face.Value.Replace("-", "−");

        // UI-331: the tile in a sentence — its kind and face ("Shadow special tile 1").
        public static string Name(HuntTileFaceView face) => face.Pool == Smeagol ? "the Sméagol tile" : Kind(face) + " " + Title(face);

        // UI-420 (owner): the tile's kind and meaning without its value twice — "Standard tile · 3 Hunt damage" — then its
        // Reveal and Stop; an expansion's tile names its edition at the end of the first line.
        public static string Tooltip(HuntTileFaceView face) => TooltipText.Lines(
            Summary(face),
            face.Reveal ? "Reveal · The Fellowship is revealed, apart from the damage" : "",
            face.Stop ? "Stop · Stops the Fellowship's move on the Mordor track" : "");

        // "Standard tile · 3 Hunt damage", "Shadow special tile · Die · Damage equals a die roll".
        public static string Summary(HuntTileFaceView face) =>
            TooltipText.Parts(BaseKind(face), Symbol(face), Meaning(face), Edition(face));

        // A number is already in the tile's meaning; an Eye or a die is named before it.
        private static string Symbol(HuntTileFaceView face) =>
            face.Pool == Smeagol || int.TryParse(face.Value, out _) ? "" : Title(face);

        private static string Meaning(HuntTileFaceView face)
        {
            if (face.Pool == Smeagol)
                return "Sméagol joins when he can; otherwise another tile is drawn";
            if (face.Value == "eye")
                return "Damage equals the successful Hunt dice; on the Mordor track, the dice in the Hunt Box";
            if (face.Value == "die")
                return "Damage equals a die roll";
            if (!int.TryParse(face.Value, out var value))
                return "Printed value " + face.Value;
            return value > 0 ? value + " Hunt damage" : value == 0 ? "No damage" : "Heals " + -value + " Corruption";
        }

        private static string Kind(HuntTileFaceView face)
        {
            var kind = BaseKind(face);
            return face.Source == "kings" ? kind + " · Kings of Middle-earth" : kind;
        }

        private static string BaseKind(HuntTileFaceView face) => face.Pool switch
        {
            "special-fp" => "Free Peoples special tile",
            "special-sh" => "Shadow special tile",
            Smeagol => "Lords of Middle-earth tile",
            _ => "Standard tile"
        };

        private static string Edition(HuntTileFaceView face) => face.Source == "kings" ? "Kings of Middle-earth" : "";
    }
}
