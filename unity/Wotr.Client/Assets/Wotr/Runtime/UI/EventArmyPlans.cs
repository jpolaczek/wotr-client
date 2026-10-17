using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class EventArmyPlans
    {
        private readonly List<Action> refresh = new List<Action>();

        public EventArmyPlans(VisualElement root, TurnDraft draft, ActionOption option, Action<Action> edit, BoardPresentation data)
        {
            var row = BoardScreen.Row();
            row.AddToClassList("event-army-plans");
            root.Add(row);
            for (var i = 0; i < draft.Payload.Moves.Count; i++)
            {
                var index = i;
                var group = BoardScreen.Row();
                group.AddToClassList("event-army-plan");
                var button = BoardScreen.Button("", () => edit(() => ArmyDraft.Edit(draft, index)));
                button.name = "edit-move-" + index;
                button.AddToClassList("event-army-tab");
                group.Add(button);
                var remove = IconButton.Create("close", "Close", () => edit(() => ArmyDraft.Remove(draft, index)));
                remove.name = "remove-move-" + index;
                remove.AddToClassList("event-army-remove");
                group.Add(remove);
                row.Add(group);
                refresh.Add(() =>
                {
                    var move = draft.Payload.Moves[index];
                    button.text = (index + 1) + " · " + data.Regions[move.From].Name;
                    button.tooltip = data.Regions[move.From].Name + " → " + (move.To.Length > 0 ? data.Regions[move.To].Name : "Choose destination");
                    button.EnableInClassList("selected", ArmyDraft.VisibleIndex(draft) == index);
                });
            }
            var next = BoardScreen.Button("Add army", () => edit(() => draft.Change(_ => draft.MoveFocus = -1)));
            next.name = "event-army-add";
            row.Add(next);
            refresh.Add(() => next.style.display = draft.MoveFocus >= 0 && draft.Payload.Moves.Count < option.Maximum &&
                ArmyDraft.Current(draft) is { } current && EventArmyDraft.Complete(draft, option, current) ? DisplayStyle.Flex : DisplayStyle.None);
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
