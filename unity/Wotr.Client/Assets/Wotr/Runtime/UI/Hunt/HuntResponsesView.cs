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
        // UI-270: an alternate companion's Guide response (HuntGuideResponses), by its reaction id.
        public event Action<string> GuideResponse;
        private readonly VisualElement guideRow = BoardScreen.Row("hunt-guide-responses");
        private string guideKey = "";
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
            Root.Add(guideRow);
            Root.Add(guideOrder);
            // UI-235: only a damage decision fills and shows the row (Context); a replay or an observer never calls it.
            Root.style.display = DisplayStyle.None;
        }

        // UI-270: the alternate companions' Guide responses as response objects: the companion's portrait, its name and
        // what the response does; a click uses it.
        private void GuideResponses(PlayerView view, IReadOnlyList<EffectChoiceView> offered, bool enabled)
        {
            var key = string.Join("|", offered.Select(c => c.Id));
            if (key != guideKey)
            {
                guideKey = key;
                guideRow.Clear();
                foreach (var choice in offered)
                    guideRow.Add(GuideButton(view, choice));
            }
            guideRow.style.display = offered.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            foreach (var button in guideRow.Query<Button>().ToList())
            {
                button.SetEnabled(enabled);
                button.EnableInClassList("legal-target", enabled);
            }
        }

        private Button GuideButton(PlayerView view, EffectChoiceView choice)
        {
            var id = choice.Id;
            var button = BoardScreen.Button("", () => GuideResponse?.Invoke(id));
            button.name = "effect-choice-" + id;
            button.AddToClassList("hunt-response-object");
            var header = BoardScreen.Row("hunt-response-heading");
            header.pickingMode = PickingMode.Ignore;
            var portrait = new Image
            {
                pickingMode = PickingMode.Ignore, scaleMode = UnityEngine.ScaleMode.ScaleToFit,
                sprite = FigureArtwork.Sprite(FigureAppearances.Character(
                    CharacterLibrary.Version(view, HuntGuideResponses.Figure(view, choice)), Side.FreePeoples))
            };
            portrait.AddToClassList("hunt-response-icon");
            portrait.AddToClassList("hunt-guide-portrait");
            header.Add(portrait);
            header.Add(new Label(CharacterLibrary.ShortName(view, HuntGuideResponses.Figure(view, choice)))
                { name = "hunt-response-name", pickingMode = PickingMode.Ignore });
            button.Add(header);
            button.Add(new Label(choice.Title.Length > 0 ? choice.Title : "Guide response")
                { name = "hunt-response-consequence", pickingMode = PickingMode.Ignore });
            button.tooltip = HuntGuideResponses.Tooltip(view, choice);
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
            var guides = HuntGuideResponses.Offered(view);
            GuideResponses(view, guides, enabled);
            Root.style.display = option == null && guides.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
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
