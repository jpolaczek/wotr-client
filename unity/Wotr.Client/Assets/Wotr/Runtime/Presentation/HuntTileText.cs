using System.Collections.Generic;
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

        public static string Tooltip(HuntTileFaceView face)
        {
            var lines = new List<string> { Title(face) + " · " + Meaning(face) };
            if (face.Reveal)
                lines.Add("Reveal · The Fellowship is revealed, apart from the damage");
            if (face.Stop)
                lines.Add("Stop · Stops the Fellowship's move on the Mordor track");
            lines.Add(Kind(face));
            return string.Join("\n", lines);
        }

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
            var kind = face.Pool switch
            {
                "special-fp" => "Free Peoples special tile",
                "special-sh" => "Shadow special tile",
                Smeagol => "Lords of Middle-earth tile",
                _ => "Standard tile"
            };
            return face.Source == "kings" ? kind + " · Kings of Middle-earth" : kind;
        }
    }
}
