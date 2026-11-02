using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Wotr.Client
{
    public sealed class CharacterAbilityText
    {
        public string Id = "";
        public string Title = "";
        public string Text = "";
        public string Source = "";
    }

    // Printed public information only. The selected version must come from this player's view.
    public static class CharacterAbilityLibrary
    {
        private sealed class Card
        {
            public string Id = "";
            public List<CharacterAbilityText> Abilities = new List<CharacterAbilityText>();
        }

        private sealed class Catalog
        {
            public List<Card> Cards = new List<Card>();
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<CharacterAbilityText>> cards;
        private static IReadOnlyDictionary<string, CharacterAbilityText> first;

        public static IReadOnlyList<CharacterAbilityText> For(string definitionId)
        {
            Load();
            return cards.TryGetValue(definitionId, out var card) ? card : Array.Empty<CharacterAbilityText>();
        }

        // UI-346: the printed title of an ability id, from the first card that prints it, or null.
        public static string Title(string abilityId) => Find(abilityId)?.Title;

        // UI-440: an ability as one card prints it, or as the first card that prints it when that card does not; null for
        // an id no card prints.
        public static CharacterAbilityText Find(string definitionId, string abilityId) =>
            For(definitionId ?? "").FirstOrDefault(ability => ability.Id == abilityId) ?? Find(abilityId);

        public static CharacterAbilityText Find(string abilityId) => First.TryGetValue(abilityId ?? "", out var ability) ? ability : null;

        private static IReadOnlyDictionary<string, CharacterAbilityText> First
        {
            get
            {
                Load();
                return first ??= cards.Values.SelectMany(abilities => abilities).GroupBy(ability => ability.Id)
                    .ToDictionary(group => group.Key, group => group.First());
            }
        }

        // UI-439: every card of the catalog (base game and expansions), and every printed ability id once.
        public static IReadOnlyCollection<string> CardIds
        {
            get
            {
                Load();
                return cards.Keys.ToList();
            }
        }

        public static IReadOnlyCollection<string> AbilityIds => First.Keys.ToList();

        private static void Load() =>
            cards ??= JsonConvert.DeserializeObject<Catalog>(Resources.Load<TextAsset>("character-ability-text").text)
                .Cards.ToDictionary(c => c.Id, c => (IReadOnlyList<CharacterAbilityText>)c.Abilities);
    }
}
