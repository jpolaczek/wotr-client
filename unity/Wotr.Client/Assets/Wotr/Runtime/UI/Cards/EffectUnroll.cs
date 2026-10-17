using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-221: the complete text of a hovered table source unrolls on parchment under its tile, over the other
    // sources, like a hand card (UI-91). It only shows text: it takes no pointer and sends nothing.
    internal sealed class EffectUnroll
    {
        public VisualElement Root { get; } = new VisualElement { name = "effect-unroll", pickingMode = PickingMode.Ignore };
        public EffectEntry Entry { get; private set; }
        private readonly EffectCardText card = new EffectCardText();
        private readonly Label text = new Label { name = "effect-unroll-text" };

        public EffectUnroll()
        {
            Root.AddToClassList("effect-unroll");
            Root.style.position = Position.Absolute;
            Root.style.display = DisplayStyle.None;
            Root.Add(card.Root);
            Root.Add(text);
        }

        public void Show(EffectEntry entry, VisualElement list, VisualElement host, MatchConfiguration configuration)
        {
            Entry = entry;
            var source = entry.Source;
            var isCard = CardLibrary.Cards.TryGetValue(source.SourceId, out var printed);
            if (isCard)
                card.Show(printed, CardLibrary.EventText(printed, configuration), false);
            text.text = isCard ? "" : string.Join("\n", source.Effects.Select(e => e.Description).Where(d => !string.IsNullOrEmpty(d)).Distinct());
            card.Root.style.display = isCard ? DisplayStyle.Flex : DisplayStyle.None;
            text.style.display = isCard ? DisplayStyle.None : DisplayStyle.Flex;
            var tile = host.WorldToLocal(entry.Root.worldBound);
            var area = host.WorldToLocal(list.worldBound);
            Root.style.left = area.xMin;
            Root.style.width = area.width;
            Root.style.top = tile.yMax + 2;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            Root.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);
        }

        public void Hide()
        {
            Entry = null;
            Root.style.display = DisplayStyle.None;
        }
    }
}
