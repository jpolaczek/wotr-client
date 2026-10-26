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
        private static IReadOnlyDictionary<string, string> titles;

        public static IReadOnlyList<CharacterAbilityText> For(string definitionId)
        {
            Load();
            return cards.TryGetValue(definitionId, out var card) ? card : Array.Empty<CharacterAbilityText>();
        }

        // UI-346: the printed title of an ability id, from the first card that prints it, or null.
        public static string Title(string abilityId)
        {
            Load();
            titles ??= cards.Values.SelectMany(abilities => abilities).GroupBy(ability => ability.Id)
                .ToDictionary(group => group.Key, group => group.First().Title);
            return titles.TryGetValue(abilityId, out var title) ? title : null;
        }

        private static void Load() =>
            cards ??= JsonConvert.DeserializeObject<Catalog>(Resources.Load<TextAsset>("character-ability-text").text)
                .Cards.ToDictionary(c => c.Id, c => (IReadOnlyList<CharacterAbilityText>)c.Abilities);
    }
}
