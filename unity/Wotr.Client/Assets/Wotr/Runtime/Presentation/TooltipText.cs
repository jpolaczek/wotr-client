using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-420 (owner): a tooltip joins only the parts that have text, so no line ends in a dangling " · " and no blank
    // line stands between its parts; units and nations read as printed ("Nazgûl", "Southrons & Easterlings").
    public static class TooltipText
    {
        public const string Separator = " · ";

        // "Title · part · part", skipping empty parts.
        public static string Parts(params string[] parts) =>
            string.Join(Separator, parts.Where(HasText).Select(p => p.Trim()));

        // One line per non-empty part, without blank lines.
        public static string Lines(params string[] lines) =>
            string.Join("\n", lines.Where(HasText).Select(l => l.Trim('\n')));

        public static string Kind(UnitKind kind) => kind == UnitKind.Nazgul ? "Nazgûl" : kind.ToString();

        // "Rohan Elite", "Nazgûl".
        public static string Unit(string nation, UnitKind kind) =>
            kind == UnitKind.Nazgul ? Kind(kind) : Nation(nation) + " " + Kind(kind);

        public static string Nation(string id) => id switch
        {
            "southrons" => "Southrons & Easterlings",
            CharacterCardFacts.AllFreePeoples => "All Free Peoples",
            _ => FigureGrid.Name(id)
        };

        // "1 card", "3 cards".
        public static string Count(int count, string noun) => count + " " + noun + (count == 1 ? "" : "s");

        private static bool HasText(string part) => !string.IsNullOrWhiteSpace(part);
    }
}
