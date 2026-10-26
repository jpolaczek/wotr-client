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
        // UI-360: a Guide's response (HuntGuideResponses), by its reaction id.
        public event Action<string> GuideResponse;
        // UI-285: a table card's response (HuntCardResponses), by its choice id.
        public event Action<string> CardResponse;
        private readonly VisualElement guideRow = BoardScreen.Row("hunt-guide-responses");
        private readonly ChoicePulse guidePulse;
        private string guideKey = "";
        private readonly VisualElement cardRow = BoardScreen.Row("hunt-card-responses");
        private readonly ChoicePulse cardPulse;
        private string cardKey = "";
        private readonly Dictionary<HuntResponse, Button> choices = new Dictionary<HuntResponse, Button>();
        // UI-288: the current Guide's figure on its response; Context keeps it on the Guide of the moment.
        private readonly Image guideArt = FigureArtwork.Image(FigureAppearances.Character("", Side.FreePeoples));
        private string guideShown;
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
            guideRow.name = "hunt-guide-responses";
            guidePulse = new ChoicePulse(guideRow);
            Root.Add(guideRow);
            cardRow.name = "hunt-card-responses";
            cardPulse = new ChoicePulse(cardRow);
            Root.Add(cardRow);
            Root.Add(guideOrder);
            // UI-235: only a damage decision fills and shows the row (Context); a replay or an observer never calls it.
            Root.style.display = DisplayStyle.None;
        }

        // UI-360: the Guides' responses as response objects: the Guide's portrait, its name and what the response does;
        // they glow while they can be used, and a click uses one.
        private void GuideResponses(PlayerView view, IReadOnlyList<EffectChoiceView> offered, bool enabled)
        {
            var key = string.Join("|", offered.Select(c => c.Id + ":" + Shown(view, HuntGuideResponses.Figure(view, c))));
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
                ChoiceGlow.Apply(button, enabled);
            }
            guidePulse.Run(enabled && offered.Count > 0);
        }

        private Button GuideButton(PlayerView view, EffectChoiceView choice)
        {
            var id = choice.Id;
            var figure = HuntGuideResponses.Figure(view, choice);
            var button = BoardScreen.Button("", () => GuideResponse?.Invoke(id));
            button.name = "effect-choice-" + id;
            button.AddToClassList("hunt-response-object");
            button.AddToClassList("hunt-guide-response");
            var header = BoardScreen.Row("hunt-response-heading");
            header.pickingMode = PickingMode.Ignore;
            var portrait = new VisualElement { name = "hunt-response-art", pickingMode = PickingMode.Ignore };
            portrait.AddToClassList("hunt-response-icon");
            portrait.AddToClassList("hunt-guide-portrait");
            portrait.Add(FigureArtwork.Image(FigureAppearances.Character(Shown(view, figure), Side.FreePeoples)));
            header.Add(portrait);
            header.Add(new Label(CharacterLibrary.ShortName(view, figure)) { name = "hunt-response-name", pickingMode = PickingMode.Ignore });
            button.Add(header);
            button.Add(new Label(HuntGuideResponses.Consequence(choice))
                { name = "hunt-response-consequence", pickingMode = PickingMode.Ignore });
            button.tooltip = HuntGuideResponses.Tooltip(view, choice);
            return button;
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
            // UI-288: a small parchment card with the symbol printed on the card's corner.
            var card = new VisualElement { name = "hunt-response-art", pickingMode = PickingMode.Ignore };
            card.AddToClassList("hunt-response-card");
            var symbol = new ActionSymbol(CardLibrary.Face(response.CardId));
            symbol.AddToClassList(ActionSymbol.InkClass);
            card.Add(symbol);
            header.Add(card);
            header.Add(new Label(response.Name) { name = "hunt-response-name", pickingMode = PickingMode.Ignore });
            button.Add(header);
            button.Add(new Label(response.Consequence) { name = "hunt-response-consequence", pickingMode = PickingMode.Ignore });
            button.tooltip = response.Tooltip;
            return button;
        }

        private void ShowGuide(PlayerView view)
        {
            var shown = Shown(view, view.Fellowship.Guide);
            if (shown == guideShown)
                return;
            guideShown = shown;
            guideArt.sprite = FigureArtwork.Sprite(FigureAppearances.Character(shown, Side.FreePeoples));
        }

        // A Fellowship character's figure as the Fellowship panel shows it: its own version when the Companion is the
        // player's, otherwise its public version.
        private static string Shown(PlayerView view, string character)
        {
            var version = view.OwnCompanions.Any(c => c.Id == character)
                ? CharacterLibrary.Version(view, character) : FellowshipGuideText.PublicVersion(view.Fellowship, character);
            return version.Length > 0 ? version : character;
        }

        // UI-288: each response shows its picture: the Ring with its drop of Corruption, the Guide's own figure, the
        // Companions' stack for a random casualty.
        private void AddFace(Button button, HuntResponse response)
        {
            var header = BoardScreen.Row("hunt-response-heading");
            header.pickingMode = PickingMode.Ignore;
            var portrait = new VisualElement { name = "hunt-response-art", pickingMode = PickingMode.Ignore };
            portrait.AddToClassList("hunt-response-icon");
            portrait.EnableInClassList("hunt-response-ring", response == HuntResponse.Ring);
            portrait.EnableInClassList("hunt-guide-portrait", response == HuntResponse.Guide);
            portrait.EnableInClassList("hunt-response-random", response == HuntResponse.RandomCompanion);
            if (response == HuntResponse.Guide)
                portrait.Add(guideArt);
            header.Add(portrait);
            header.Add(new Label { name = "hunt-response-name", pickingMode = PickingMode.Ignore });
            button.Add(header);
        }

        public void Context(PlayerView view, TurnDraft draft, bool enabled)
        {
            var option = HuntResponseActions.Option(view);
            var guides = HuntGuideResponses.Offered(view);
            GuideResponses(view, guides, enabled);
            var cards = HuntCardResponses.Offered(view);
            CardResponses(cards, enabled);
            Root.style.display = option == null && guides.Count == 0 && cards.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
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
                if (pair.Key == HuntResponse.Guide)
                    ShowGuide(view);
                button.Q<Label>("hunt-response-consequence").text = HuntResponseActions.Consequence(view, pair.Key);
                button.tooltip = HuntResponseActions.Describe(view, pair.Key) +
                    "\nClick to choose · Or drag onto the tile.";
            }
        }
    }
}
