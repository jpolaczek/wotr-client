using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Opponent counters in the top bar cartouche. Chips are built once; Apply rewrites only changed values.
    public sealed class OpponentSummary
    {
        public VisualElement Root { get; } = new VisualElement { name = "opponent-summary" };
        private readonly SummaryChip cards = new SummaryChip("cards", "Cards", "Cards in hand");
        private readonly SummaryChip rings = new SummaryChip("rings", "Rings", "Elven Rings");
        private readonly SummaryChip spent = new SummaryChip("spent", "Spent", "Action dice used this turn");
        private readonly SummaryChip removed = new SummaryChip("out", "Out", "Dice out of play");

        public OpponentSummary()
        {
            Root.AddToClassList("opponent-summary");
            // UI-98: the Hunt Box has its own plaque at the right end of the bar.
            var chips = new[] { cards, rings, spent, removed };
            for (var i = 0; i < chips.Length; i++)
            {
                if (i > 0)
                    Root.Add(Divider());
                Root.Add(chips[i].Root);
            }
        }

        public void Apply(PlayerView view, Side side)
        {
            var counts = SideResourceCounts.From(view, side);
            var owner = BoardScreen.SideName(side);
            cards.Show(counts.Cards, owner);
            rings.Show(counts.Rings, owner);
            spent.Show(counts.Spent, owner);
            removed.Show(counts.Out, owner);
            Root.style.display = DisplayStyle.Flex;
        }

        // The local fixture has no projection, so it must not show invented counts.
        public void Hide() => Root.style.display = DisplayStyle.None;

        private static VisualElement Divider()
        {
            var divider = new VisualElement { pickingMode = PickingMode.Ignore };
            divider.AddToClassList("summary-divider");
            return divider;
        }

        private sealed class SummaryChip
        {
            public VisualElement Root { get; }
            private readonly Label value = new Label { pickingMode = PickingMode.Ignore };
            private readonly string description;
            private string shown;

            public SummaryChip(string key, string caption, string description)
            {
                this.description = description;
                Root = new VisualElement { name = "opponent-" + key };
                Root.AddToClassList("summary-chip");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("summary-icon");
                icon.AddToClassList("summary-icon-" + key);
                Root.Add(icon);
                var text = new VisualElement { pickingMode = PickingMode.Ignore };
                text.AddToClassList("summary-text");
                var label = new Label(caption) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("summary-caption");
                text.Add(label);
                value.AddToClassList("summary-value");
                text.Add(value);
                Root.Add(text);
            }

            public void Show(int count, string owner)
            {
                var tooltip = owner + " · " + description + ": " + count;
                if (shown == tooltip)
                    return;
                shown = tooltip;
                value.text = count.ToString();
                Root.tooltip = tooltip;
            }
        }
    }
}
