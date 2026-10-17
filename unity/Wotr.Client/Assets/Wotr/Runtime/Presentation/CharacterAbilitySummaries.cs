using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Wotr.Client
{
    // UI-187: a character's abilities for its tooltips, one short line each: the printed title and the gist of the
    // printed text (Resources/character-ability-summaries.json). The card keeps the full text (UI-139).
    public static class CharacterAbilitySummaries
    {
        private sealed class Catalog
        {
            public Dictionary<string, string> Summaries = new Dictionary<string, string>();
        }

        private static IReadOnlyDictionary<string, string> summaries;

        private static IReadOnlyDictionary<string, string> All => summaries ??=
            JsonConvert.DeserializeObject<Catalog>(Resources.Load<TextAsset>("character-ability-summaries").text).Summaries;

        public static bool Has(string abilityId) => All.ContainsKey(abilityId);

        public static string Summary(string abilityId) => All.TryGetValue(abilityId, out var text) ? text : "";

        public static IReadOnlyList<string> Lines(string definitionId) => CharacterAbilityLibrary.For(definitionId)
            .Where(ability => Has(ability.Id)).Select(ability => ability.Title + " · " + All[ability.Id]).ToList();

        // The tooltip's own lines first, then one line per ability.
        public static string Append(string tooltip, string definitionId)
        {
            var lines = Lines(definitionId);
            return lines.Count == 0 ? tooltip : tooltip + "\n" + string.Join("\n", lines);
        }
    }
}
