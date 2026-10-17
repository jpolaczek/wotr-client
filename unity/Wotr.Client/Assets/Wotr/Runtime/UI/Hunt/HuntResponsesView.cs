using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class HuntResponsesView
    {
        public VisualElement Root { get; } = new VisualElement { name = "hunt-responses" };
        public event Action<HuntResponse> Selected;
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
            Root.Add(guideOrder);
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
            Root.style.display = option == null ? DisplayStyle.None : DisplayStyle.Flex;
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
