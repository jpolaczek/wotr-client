using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Read-only historical facts; no draft tokens, current options, or gameplay callbacks.
    public sealed class HuntArchiveSummary
    {
        public VisualElement Root { get; } = new VisualElement { name = "hunt-archive" };
        public HuntArchivePresentation Displayed { get; private set; }
        private readonly BoardPresentation data;
        private readonly Label source = Text("hunt-archive-source");
        private readonly Label roll = Text("hunt-archive-roll");
        private readonly VisualElement dice = BoardScreen.Row("hunt-archive-dice");
        private readonly Label tile = Text("hunt-archive-tile");
        private readonly Label result = Text("hunt-archive-result");

        public HuntArchiveSummary(BoardPresentation data)
        {
            this.data = data;
            Root.AddToClassList("hunt-archive");
            Root.Add(source);
            Root.Add(roll);
            Root.Add(dice);
            Root.Add(tile);
            Root.Add(result);
            Root.style.display = DisplayStyle.None;
        }

        public void Apply(ActionContextModel context)
        {
            Displayed = HuntArchivePresentation.For(context);
            Root.style.display = Displayed == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (Displayed == null)
                return;
            source.text = "Recorded Hunt · " + HuntReportText.Source(Displayed.SourceId, data);
            roll.text = Displayed.RollText;
            tile.text = Displayed.TileText(data);
            tile.EnableInClassList("hunt-archive-tile", Displayed.Tile != null);
            result.text = Displayed.Result;
            roll.tooltip = "";
            dice.Clear();
            if (Displayed.Roll == null || Displayed.Mordor)
                return;
            roll.tooltip = "Roll bonus +" + Displayed.Roll.Bonus + " · Reroll bonus +" + Displayed.Roll.RerollBonus;
            foreach (var die in Displayed.Roll.Dice)
            {
                var item = BoardScreen.Row("hunt-archive-die");
                var face = new HuntDieFace();
                face.Apply(die.Result);
                item.Add(face);
                item.Add(new Label(die.Success ? "✓" : "×"));
                item.tooltip = (die.Rerolled ? "First " + die.First + " → Reroll " : "Natural ") + die.Result +
                    (die.Success ? " · Hit" : " · Miss");
                dice.Add(item);
            }
        }

        private static Label Text(string name) => new Label { name = name, enableRichText = false };
    }
}
