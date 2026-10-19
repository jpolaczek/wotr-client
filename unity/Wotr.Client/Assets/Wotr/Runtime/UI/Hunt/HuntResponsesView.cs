using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class HuntResponsesView
    {
        public VisualElement Root { get; } = new VisualElement { name = "hunt-responses" };
        public event Action<HuntResponse> Selected;
        // UI-285: a table card's response (HuntCardResponses), by its choice id.
        public event Action<string> CardResponse;
        private readonly VisualElement cardRow = BoardScreen.Row("hunt-card-responses");
        private readonly ChoicePulse cardPulse;
        private string cardKey = "";
        private readonly Dictionary<HuntResponse, Button> choices = new Dictionary<HuntResponse, Button>();
        private readonly Label guideOrder = new Label("Guide casualty → New Guide before Corruption")
            { name = "hunt-guide-order", pickingMode = PickingMode.Ignore };

        public HuntResponsesView()
        {
            var row = BoardScreen.Row("hunt-response-objects");
            foreach (var response in new[] { HuntResponse.Ring, HuntResponse.Guide, HuntResponse.RandomCompanion })
            {
                var button = BoardScreen.Button("", () => Selected?.Invoke(response));
                button.name = "hunt-response-" + response;
                button.userData = new DraftToken(DraftObject.HuntResponse, response.ToString());
                button.AddToClassList("hunt-response-object");
                AddFace(button, response);
                button.Add(new Label { name = "hunt-response-consequence", pickingMode = PickingMode.Ignore });
                row.Add(button);
                choices.Add(response, button);
            }
            Root.Add(row);
            cardRow.name = "hunt-card-responses";
            cardPulse = new ChoicePulse(cardRow);
            Root.Add(cardRow);
            Root.Add(guideOrder);
            // UI-235: only a damage decision fills and shows the row (Context); a replay or an observer never calls it.
            Root.style.display = DisplayStyle.None;
        }

        // UI-285: the table cards' responses as response objects: a small card, its name and what using it does; they
        // glow while they can be used, and a click uses one.
        private void CardResponses(IReadOnlyList<HuntCardResponse> offered, bool enabled)
        {
            var key = string.Join("|", offered.Select(r => r.Id + ":" + r.Consequence));
            if (key != cardKey)
            {
                cardKey = key;
                cardRow.Clear();
                foreach (var response in offered)
                    cardRow.Add(CardButton(response));
            }
            cardRow.style.display = offered.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            foreach (var button in cardRow.Query<Button>().ToList())
            {
                button.SetEnabled(enabled);
                button.EnableInClassList("legal-target", enabled);
                ChoiceGlow.Apply(button, enabled);
            }
            cardPulse.Run(enabled && offered.Count > 0);
        }

        private Button CardButton(HuntCardResponse response)
        {
            var id = response.Id;
            var button = BoardScreen.Button("", () => CardResponse?.Invoke(id));
            button.name = "effect-choice-" + id;
            button.AddToClassList("hunt-response-object");
            button.AddToClassList("hunt-card-response");
            var header = BoardScreen.Row("hunt-response-heading");
            header.pickingMode = PickingMode.Ignore;
            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.AddToClassList("hunt-response-card");
            header.Add(card);
            header.Add(new Label(response.Name) { name = "hunt-response-name", pickingMode = PickingMode.Ignore });
            button.Add(header);
            button.Add(new Label(response.Consequence) { name = "hunt-response-consequence", pickingMode = PickingMode.Ignore });
            button.tooltip = response.Tooltip;
            return button;
        }

        private static void AddFace(Button button, HuntResponse response)
        {
            var header = BoardScreen.Row("hunt-response-heading");
            header.pickingMode = PickingMode.Ignore;
            var portrait = new VisualElement { pickingMode = PickingMode.Ignore };
            portrait.AddToClassList("hunt-response-icon");
            portrait.EnableInClassList("hunt-guide-portrait", response == HuntResponse.Guide);
            portrait.EnableInClassList("hunt-random-stack", response == HuntResponse.RandomCompanion);
            if (response == HuntResponse.RandomCompanion)
            {
                var back = new VisualElement { pickingMode = PickingMode.Ignore };
                back.AddToClassList("hunt-card-back");
                portrait.Add(back);
            }
            portrait.Add(new Label(response == HuntResponse.Ring ? "○" : response == HuntResponse.Guide ? "★" : "?")
                { name = "hunt-response-symbol", pickingMode = PickingMode.Ignore });
            header.Add(portrait);
            header.Add(new Label { name = "hunt-response-name", pickingMode = PickingMode.Ignore });
            button.Add(header);
        }

        public void Context(PlayerView view, TurnDraft draft, bool enabled)
        {
            var option = HuntResponseActions.Option(view);
            var cards = HuntCardResponses.Offered(view);
            CardResponses(cards, enabled);
            Root.style.display = option == null && cards.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            guideOrder.style.display = option?.HuntResponses.Contains(HuntResponse.Guide) == true ||
                option?.HuntResponses.Contains(HuntResponse.RandomCompanion) == true ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var pair in choices)
            {
                var legal = option?.HuntResponses.Contains(pair.Key) == true;
                var button = pair.Value;
                button.style.display = legal ? DisplayStyle.Flex : DisplayStyle.None;
                button.SetEnabled(enabled && legal);
                button.EnableInClassList("legal-target", enabled && legal);
                button.EnableInClassList("selected", legal && draft.HuntChoice == pair.Key);
                if (!legal)
                    continue;
                button.Q<Label>("hunt-response-name").text = pair.Key == HuntResponse.Ring ? "Ring" :
                    pair.Key == HuntResponse.Guide ? CharacterLibrary.ShortName(view, view.Fellowship.Guide) : "Random companion";
                button.Q<Label>("hunt-response-consequence").text = HuntResponseActions.Consequence(view, pair.Key);
                button.tooltip = HuntResponseActions.Describe(view, pair.Key) +
                    "\nClick to choose · Or drag onto the tile.";
            }
        }
    }
}
