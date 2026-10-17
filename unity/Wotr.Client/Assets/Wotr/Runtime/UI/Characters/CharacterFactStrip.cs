using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-139: the statistics of a character card as a row of symbols with their numbers (level, move or flight,
    // combat level, leadership, nation). Each symbol explains itself in its tooltip; the row's tooltip reads them
    // all in words. The items are made once and only their values change.
    public sealed class CharacterFactStrip
    {
        public const string ItemClass = "character-fact";
        private static readonly string[] Keys = { "level", "movement", "flight", "combat", "leadership", "nation" };
        public VisualElement Root { get; } = new VisualElement { name = "character-statistics" };
        private readonly Dictionary<string, Item> items = new Dictionary<string, Item>();
        private string nation = "";

        public CharacterFactStrip()
        {
            Root.AddToClassList("character-statistics");
            foreach (var key in Keys)
            {
                var item = new Item(key);
                items.Add(key, item);
                Root.Add(item.Root);
            }
        }

        public void Apply(IReadOnlyList<CharacterFact> facts)
        {
            var shown = facts.ToDictionary(f => f.Key);
            foreach (var pair in items)
                pair.Value.Apply(shown.TryGetValue(pair.Key, out var fact) ? fact : null);
            var emblem = shown.TryGetValue("nation", out var nationFact) ? nationFact.Nation : "";
            if (emblem != nation)
            {
                items["nation"].Icon.RemoveFromClassList("nation-emblem-" + nation);
                items["nation"].Icon.AddToClassList("nation-emblem-" + emblem);
                nation = emblem;
            }
            Root.tooltip = CharacterCardFacts.Summary(facts);
        }

        private sealed class Item
        {
            public VisualElement Root { get; } = new VisualElement();
            public VisualElement Icon { get; } = new VisualElement { pickingMode = PickingMode.Ignore };
            private readonly Label value = new Label { pickingMode = PickingMode.Ignore, enableRichText = false };

            public Item(string key)
            {
                Root.name = "character-fact-" + key;
                Root.AddToClassList(ItemClass);
                Root.AddToClassList(ItemClass + "-" + key);
                Icon.AddToClassList("character-fact-icon");
                value.AddToClassList("character-fact-value");
                Root.Add(Icon);
                Root.Add(value);
            }

            public void Apply(CharacterFact fact)
            {
                Root.style.display = fact == null ? DisplayStyle.None : DisplayStyle.Flex;
                if (fact == null)
                    return;
                Root.tooltip = fact.Tooltip;
                value.text = fact.Value;
                value.style.display = fact.Value.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
