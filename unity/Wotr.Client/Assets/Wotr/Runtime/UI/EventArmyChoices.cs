using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class EventArmyChoices
    {
        private readonly List<Action> refresh = new List<Action>();

        public EventArmyChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit, BoardPresentation data)
        {
            var option = EventArmyDraft.Option(view);
            if (option.ArmyEvent.SeparateRoutes)
                refresh.Add(new EventArmyPlans(root, draft, option, edit, data).Refresh);
            var move = ArmyDraft.Current(draft);
            if (move == null)
            {
                root.Add(new Label("Choose an army on the map."));
                return;
            }
            var army = option.Armies.First(a => a.RegionId == move.From);
            var route = new Label { name = "event-army-route" };
            var row = BoardScreen.Row();
            row.Add(route);
            var reset = BoardScreen.Button("Reset route", () => edit(() => EventArmyDraft.ResetRoute(draft)));
            reset.name = "event-army-reset";
            row.Add(reset);
            root.Add(row);
            refresh.Add(() =>
            {
                var selectedRoute = EventArmyDraft.Route(draft, option, ArmyDraft.Current(draft));
                route.text = data.Regions[move.From].Name + (army.Garrison ? " · Garrison" : "") +
                    (selectedRoute.Count == 0 ? " → Choose destination" : " → " + string.Join(" → ", selectedRoute.Select(r => data.Regions[r].Name)));
                reset.SetEnabled(selectedRoute.Count > 0);
            });
            var grid = new FigureGrid(view.Units.Where(u => u.Location == move.From && u.Garrison == army.Garrison && u.Side == view.You),
                id => ArmyDraft.Current(draft).UnitIds.Contains(id) || ArmyDraft.Current(draft).CharacterIds.Contains(id),
                id => !army.UnitIds.Contains(id) && !army.CharacterIds.Contains(id),
                ids => edit(() => EventArmyDraft.Toggle(draft, ids.Where(army.UnitIds.Contains))),
                view.Characters.Where(c => c.Location == move.From && c.Garrison == army.Garrison && c.Side == view.You),
                ids => edit(() => EventArmyDraft.Toggle(draft, ids.Where(army.CharacterIds.Contains), true)));
            root.Add(grid.Root);
            refresh.Add(grid.Refresh);
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
