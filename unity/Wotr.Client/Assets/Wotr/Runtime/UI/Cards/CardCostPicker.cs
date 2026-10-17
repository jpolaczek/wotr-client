using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CardCostPicker
    {
        public VisualElement Root { get; } = BoardScreen.Row("card-cost-picker");
        public event Action<EventCostOption> Selected;
        private readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();

        public void Refresh(PlayerView view, TurnDraft draft, ActionDieContext context, bool busy)
        {
            var costs = DraftOptions.All(view, draft).Where(o => o.Enabled).SelectMany(o => o.DrawCosts.Concat(o.Events.SelectMany(e => e.Costs)))
                .Where(c => c.DieId == context.DieId && c.AbilityId.Length > 0).GroupBy(c => c.AbilityId + "/" + c.Face)
                .ToDictionary(g => g.Key, g => g.First());
            foreach (var id in buttons.Keys.Where(id => !costs.ContainsKey(id)).ToArray())
            {
                buttons[id].RemoveFromHierarchy();
                buttons.Remove(id);
            }
            foreach (var entry in costs)
            {
                var cost = entry.Value;
                if (!buttons.TryGetValue(entry.Key, out var button))
                {
                    button = BoardScreen.Button("", () => Selected?.Invoke((EventCostOption)buttons[entry.Key].userData));
                    button.name = "card-cost-" + cost.Face;
                    button.Add(new ActionSymbol(cost.Face));
                    Root.Add(button);
                    buttons.Add(entry.Key, button);
                }
                button.userData = cost;
                button.tooltip = CardDrawDraft.Label(cost) + " · Once this turn";
                button.EnableInClassList("selected", draft.Payload.AbilityId == cost.AbilityId && draft.Payload.Face == cost.Face);
                button.SetEnabled(!busy);
            }
            Root.style.display = costs.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
