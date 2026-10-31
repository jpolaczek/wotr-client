using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Opponent counters in the top bar cartouche. Chips are built once; Apply rewrites only changed values. UI-432 (owner's
    // rule, UI-430): while Worn with Sorrow and Toil may discard a random card from the Free Peoples hand, the card counter
    // is that choice.
    public sealed class OpponentSummary
    {
        public const string DiscardTargetClass = "discard-target";
        public VisualElement Root { get; } = new VisualElement { name = "opponent-summary" };
        // UI-432: the click on the offered card counter.
        public event Action CardsChosen;
        public VisualElement Cards => cards.Root;
        public bool CardsOffered { get; private set; }
        private readonly SummaryChip cards = new SummaryChip("cards", "Cards", "Cards in hand");
        private readonly SummaryChip rings = new SummaryChip("rings", "Rings", "Elven Rings");
        private readonly SummaryChip spent = new SummaryChip("spent", "Spent", "Action dice used this turn");
        private readonly SummaryChip removed = new SummaryChip("out", "Out", "Dice out of play");
        private readonly ChoicePulse pulse;

        public OpponentSummary()
        {
            Root.AddToClassList("opponent-summary");
            pulse = new ChoicePulse(Root);
            cards.Root.AddManipulator(new Clickable(() =>
            {
                if (CardsOffered)
                    CardsChosen?.Invoke();
            }));
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
            // UI-361: the card counter's tooltip says what the rules make public about the cards.
            var player = view.Players.FirstOrDefault(p => p.Side == side);
            cards.Show(counts.Cards, player != null ? OpponentCards.Tooltip(player, owner) : cards.Describe(owner, counts.Cards));
            rings.Show(counts.Rings, rings.Describe(owner, counts.Rings));
            spent.Show(counts.Spent, spent.Describe(owner, counts.Spent));
            removed.Show(counts.Out, removed.Describe(owner, counts.Out));
            Root.style.display = DisplayStyle.Flex;
        }

        // UI-432 (owner's rule, UI-430): the offered counter wears the red of a card to discard, breathes in the shared pulse,
        // presses like a button and leads its tooltip with what its click does; "" withdraws the offer.
        public void OfferCards(string offer)
        {
            var offered = offer.Length > 0;
            if (offered == CardsOffered && cards.Lead == offer)
                return;
            CardsOffered = offered;
            cards.Root.EnableInClassList(DiscardTargetClass, offered);
            cards.Root.EnableInClassList(PressFeedback.PressableClass, offered);
            cards.Root.focusable = offered;
            cards.Lead = offer;
            pulse.Run(offered);
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
            private string lead = "";

            // UI-432: what a click on the counter does now, before its description; "" when it is a picture only.
            public string Lead
            {
                get => lead;
                set
                {
                    lead = value;
                    Root.tooltip = TooltipText.Lines(lead, shown ?? "");
                }
            }

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

            public string Describe(string owner, int count) => owner + " · " + description + ": " + count;

            public void Show(int count, string tooltip)
            {
                if (shown == tooltip)
                    return;
                shown = tooltip;
                ValuePulse.Set(value, count.ToString());
                Root.tooltip = TooltipText.Lines(lead, tooltip);
            }
        }
    }
}
