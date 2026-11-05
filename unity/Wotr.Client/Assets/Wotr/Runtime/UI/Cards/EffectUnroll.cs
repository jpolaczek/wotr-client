using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-221: the text of a hovered table source unrolls on parchment under its tile, over the other sources, like a
    // hand card (UI-91); UI-446: a card's only in the part in effect. It only shows text: it takes no pointer and sends
    // nothing.
    internal sealed class EffectUnroll
    {
        public VisualElement Root { get; } = new VisualElement { name = "effect-unroll", pickingMode = PickingMode.Ignore };
        public EffectEntry Entry { get; private set; }
        // Only a shown card's text is in the panel, so an ability never carries a card text block.
        private readonly EffectCardText card = new EffectCardText();
        private readonly Label text = new Label { name = "effect-unroll-text" };

        public EffectUnroll()
        {
            Root.AddToClassList("effect-unroll");
            Root.style.position = Position.Absolute;
            Root.style.display = DisplayStyle.None;
            Root.Add(text);
        }

        public void Show(EffectEntry entry, VisualElement list, VisualElement host, MatchConfiguration configuration)
        {
            Entry = entry;
            var source = entry.Source;
            var isCard = CardLibrary.Cards.TryGetValue(source.SourceId, out var printed);
            if (isCard)
            {
                card.Show(printed, CardLibrary.EventText(printed, configuration), false, EffectsPresentation.CombatPart(source));
                if (card.Root.parent != Root)
                    Root.Insert(0, card.Root);
            }
            else
                card.Root.RemoveFromHierarchy();
            text.text = isCard ? "" : string.Join("\n", source.Effects.Select(e => e.Description).Where(d => !string.IsNullOrEmpty(d)).Distinct());
            text.style.display = isCard ? DisplayStyle.None : DisplayStyle.Flex;
            var area = host.WorldToLocal(list.worldBound);
            Root.style.left = area.xMin;
            Root.style.width = area.width;
            Place(entry, host);
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            Root.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);
        }

        // UI-424: a tile in the board's upper half unrolls its text downward, one in the lower half upward from above the
        // tile, so the text never runs under the bottom row of a panel that stands low or is full.
        private void Place(EffectEntry entry, VisualElement host)
        {
            var tile = host.WorldToLocal(entry.Root.worldBound);
            var stage = host.parent;
            if (stage == null || entry.Root.worldBound.center.y < stage.worldBound.center.y)
            {
                Root.style.top = tile.yMax + 2;
                Root.style.bottom = StyleKeyword.Null;
                return;
            }
            Root.style.top = StyleKeyword.Null;
            Root.style.bottom = host.layout.height - tile.yMin + 2;
        }

        public void Hide()
        {
            Entry = null;
            card.Root.RemoveFromHierarchy();
            Root.style.display = DisplayStyle.None;
        }
    }
}
