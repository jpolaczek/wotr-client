using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CardText
    {
        public string Id = "";
        public string Title = "";
        public string Deck = "";
        public string EventType = "";
        public string Event = "";
        public string CombatTitle = "";
        public string Combat = "";
        public int[] Initiative = new int[0];
        public bool HasCombat => Combat.Length > 0 && Initiative.Length > 0;
    }

    // Public definitions are cached once; they contain no hand/deck membership.
    public static class CardLibrary
    {
        private static IReadOnlyDictionary<string, CardText> cards;
        public static IReadOnlyDictionary<string, CardText> Cards => cards ??= Load();
        public static string Title(string id) => Cards.TryGetValue(id, out var card) ? card.Title : FigureGrid.Name(id);

        public static string EventText(CardText card, MatchConfiguration configuration) =>
            configuration?.Kings == true && configuration.VisibleCorruption && card.Id.StartsWith("kings.")
                ? card.Event.Replace("hidden Corruption tile", "non-Kings Corruption tile") : card.Event;

        private static IReadOnlyDictionary<string, CardText> Load()
        {
            return LoadModule("base").Concat(LoadModule("lords")).Concat(LoadModule("kings"))
                .Concat(LoadModule("fate")).ToDictionary(c => c.Id);
        }

        private static IEnumerable<CardText> LoadModule(string module)
        {
            var metadata = JsonConvert.DeserializeObject<List<CardText>>(Resources.Load<TextAsset>(module + "-cards").text);
            var texts = JsonConvert.DeserializeObject<List<CardText>>(Resources.Load<TextAsset>(module + "-card-text").text).ToDictionary(c => c.Id);
            foreach (var card in metadata)
            {
                if (!texts.TryGetValue(card.Id, out var text))
                    continue;
                card.Event = text.Event;
                card.Combat = text.Combat;
                card.CombatTitle = text.CombatTitle;
                card.Initiative = text.Initiative;
                yield return card;
            }
        }
    }
}
