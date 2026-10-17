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

        public static IReadOnlyList<CharacterAbilityText> For(string definitionId)
        {
            cards ??= JsonConvert.DeserializeObject<Catalog>(Resources.Load<TextAsset>("character-ability-text").text)
                .Cards.ToDictionary(c => c.Id, c => (IReadOnlyList<CharacterAbilityText>)c.Abilities);
            return cards.TryGetValue(definitionId, out var card) ? card : Array.Empty<CharacterAbilityText>();
        }
    }
}
