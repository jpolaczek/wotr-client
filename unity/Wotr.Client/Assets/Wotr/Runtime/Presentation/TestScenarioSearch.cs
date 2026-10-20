using System.Globalization;
using System.Linq;
using System.Text;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-295 (owner's request): the test scenarios are found by name. Every word of the query must appear in the
    // scenario's title or id, ignoring case and accents ("smeagol" finds "Sméagol", "nazgul strike" finds "The Nazgûl
    // Strike!").
    public static class TestScenarioSearch
    {
        public static bool Active(string query) => Words(query).Length > 0;

        public static bool Matches(TestScenarioView scenario, string query)
        {
            var words = Words(query);
            if (words.Length == 0)
                return true;
            var name = Fold(scenario.Title) + " " + Fold(scenario.Id);
            return words.All(name.Contains);
        }

        private static string[] Words(string query) =>
            Fold(query ?? "").Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Lower case without diacritics, so a player types names as they come to mind.
        private static string Fold(string text)
        {
            var decomposed = text.Normalize(NormalizationForm.FormD);
            var plain = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    plain.Append(character);
            return plain.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }
    }
}
