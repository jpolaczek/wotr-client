using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Opponent counters in the top bar cartouche. Chips are built once; Apply rewrites only changed values. UI-344 (owner's
    // decision EXP-15 D3): the expansions' tokens have fixed chips too — the Free Peoples' Ruler tokens, the Shadow's
    // Dismay and Council tokens — shown for the whole game when its modules use them.
    public sealed class OpponentSummary
    {
        public VisualElement Root { get; } = new VisualElement { name = "opponent-summary" };
        private readonly SummaryChip cards = new SummaryChip("cards", "Cards", "Cards in hand");
        private readonly SummaryChip rings = new SummaryChip("rings", "Rings", "Elven Rings");
        private readonly SummaryChip spent = new SummaryChip("spent", "Spent", "Action dice used this turn");
        private readonly SummaryChip removed = new SummaryChip("out", "Out", "Dice out of play");
        private readonly SummaryChip ruler = new SummaryChip("ruler", "Ruler", "Ruler tokens");
        private readonly SummaryChip dismay = new SummaryChip("dismay", "Dismay", "Dismay tokens");
        private readonly SummaryChip council = new SummaryChip("council", "Council", "Council tokens");

        public OpponentSummary()
        {
            Root.AddToClassList("opponent-summary");
            // UI-98: the Hunt Box has its own plaque at the right end of the bar.
            var chips = new[] { cards, rings, spent, removed, ruler, dismay, council };
            for (var i = 0; i < chips.Length; i++)
            {
                if (i > 0)
                {
                    chips[i].Divider = Divider();
                    Root.Add(chips[i].Divider);
                }
                Root.Add(chips[i].Root);
            }
        }

        // The chip that shows a side's tokens or Rings: Ruler tokens are the Free Peoples', Dismay tokens the Shadow's.
        public static string ChipName(ActionCostKind kind, Side owner) => "opponent-" + kind switch
        {
            ActionCostKind.RulerToken => owner == Side.FreePeoples ? "ruler" : "dismay",
            ActionCostKind.CouncilToken => "council",
            _ => "rings"
        };

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
            Tokens(view, side, owner);
            Root.style.display = DisplayStyle.Flex;
        }

        private void Tokens(PlayerView view, Side side, string owner)
        {
            var configuration = view.Configuration;
            var kings = configuration.Kings && !configuration.RemovingRulerTokens;
            Token(ruler, kings && side == Side.FreePeoples, view.RulerTokens.Count(t => t.Location == "free-peoples"), owner);
            Token(dismay, kings && configuration.Lords && configuration.Dismay != false && side == Side.Shadow,
                view.RulerTokens.Count(t => t.Location == "dismay"), owner);
            Token(council, configuration.Council && side == Side.Shadow, view.CouncilTokens.Count(t => !t.Spent), owner);
        }

        private static void Token(SummaryChip chip, bool used, int count, string owner)
        {
            chip.Root.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
            if (chip.Divider != null)
                chip.Divider.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
            if (used)
                chip.Show(count, chip.Describe(owner, count));
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
            public VisualElement Divider { get; set; }
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

            public string Describe(string owner, int count) => owner + " · " + description + ": " + count;

            public void Show(int count, string tooltip)
            {
                if (shown == tooltip)
                    return;
                shown = tooltip;
                ValuePulse.Set(value, count.ToString());
                Root.tooltip = tooltip;
            }
        }
    }
}
