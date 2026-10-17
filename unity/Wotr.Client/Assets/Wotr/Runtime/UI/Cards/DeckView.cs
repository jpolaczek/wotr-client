using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class DeckView
    {
        public VisualElement Root { get; } = BoardScreen.Row("card-decks");
        public event Action<string> Selected;
        private readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();

        public DeckView()
        {
            foreach (var id in new[] { "character", "strategy" })
            {
                var button = BoardScreen.Button("", () => Selected?.Invoke(id));
                button.name = "deck-" + id;
                button.userData = new DraftToken(DraftObject.Deck, id, CommandKind.DrawCard);
                button.AddToClassList("card-deck");
                button.AddToClassList(id + "-card");
                button.Add(new ActionSymbol(id == "character" ? DieFace.Character : DieFace.Army));
                button.Add(new Label(id == "character" ? "Character" : "Strategy") { pickingMode = PickingMode.Ignore });
                button.Add(new Label { name = "deck-count", pickingMode = PickingMode.Ignore });
                buttons.Add(id, button);
                Root.Add(button);
            }
        }

        public void Refresh(PlayerView view, TurnDraft draft, ActionDieContext context, bool busy)
        {
            var counts = view.Players.FirstOrDefault(p => p.Side == view.You)?.DeckCounts;
            foreach (var entry in buttons)
            {
                var count = counts != null && counts.TryGetValue(entry.Key, out var n) ? n : 0;
                var available = CardActions.CanDraw(view, draft, context, entry.Key);
                entry.Value.EnableInClassList("legal-target", available);
                entry.Value.EnableInClassList("empty-deck", count == 0);
                entry.Value.Q<Label>("deck-count").text = count.ToString();
                entry.Value.SetEnabled(available && !busy);
                entry.Value.tooltip = FigureGrid.Name(entry.Key) + " · " + count + " cards" +
                    (count == 0 ? " · Empty" : available ? " · Draw" : " · Choose an Event die to draw");
            }
        }
    }
}
