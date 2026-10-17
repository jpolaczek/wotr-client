using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-166: the body of the compact card a notice opens — up to three result lines, the played card as one line
    // that opens its preview, and Full record, which opens the same action in the Log with every step.
    public sealed class ActionResultView
    {
        public VisualElement Root { get; } = new VisualElement { name = "action-result" };
        public event Action<string> CardRequested;
        public event Action FullRecordRequested;
        private readonly Label[] lines = new Label[ActionResultText.MaxLines];
        private readonly Button card;
        private string cardId = "";

        public ActionResultView()
        {
            Root.AddToClassList("action-result");
            for (var index = 0; index < lines.Length; index++)
            {
                lines[index] = new Label { name = "action-result-line-" + (index + 1), enableRichText = false };
                lines[index].AddToClassList("action-result-line");
                Root.Add(lines[index]);
            }
            card = BoardScreen.Button("", () => CardRequested?.Invoke(cardId));
            card.name = "action-result-card";
            card.AddToClassList("action-result-card");
            card.tooltip = "Show the card";
            Root.Add(card);
            var full = BoardScreen.Button("Full record", () => FullRecordRequested?.Invoke());
            full.name = "action-result-full";
            full.AddToClassList("action-result-full");
            full.tooltip = "Open this action in the Log with every step";
            Root.Add(full);
        }

        public void Apply(ActionResult result)
        {
            for (var index = 0; index < lines.Length; index++)
            {
                var shown = index < result.Lines.Count;
                lines[index].text = shown ? result.Lines[index] : "";
                lines[index].style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            }
            cardId = result.CardId;
            card.text = result.CardTitle;
            card.style.display = cardId.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
