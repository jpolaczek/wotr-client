using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class AttackChoices
    {
        private readonly List<Action> refresh = new List<Action>();

        public AttackChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit, BoardPresentation data)
        {
            var move = ArmyDraft.Current(draft);
            if (move == null)
                return;
            var option = view.Options.First(o => o.Kind == CommandKind.AttackArmy);
            var army = option.Armies.First(a => a.RegionId == move.From);
            var target = new Label { name = "attack-target" };
            root.Add(target);
            refresh.Add(() => target.text = data.Regions[move.From].Name + (army.Garrison ? " · Garrison" : " · Field") +
                (ArmyDraft.Current(draft).To.Length > 0 ? " → " + data.Regions[ArmyDraft.Current(draft).To].Name + " · " + AttackDraft.Mode(draft, view) : " → Choose target"));
            var grid = new FigureGrid(view.Units.Where(u => u.Side == view.You && u.Location == move.From && u.Garrison == army.Garrison),
                id => ArmyDraft.Current(draft).UnitIds.Contains(id) || ArmyDraft.Current(draft).CharacterIds.Contains(id), id => !army.UnitIds.Contains(id) && !army.CharacterIds.Contains(id),
                ids => edit(() => ArmyDraft.Toggle(draft, ids.Where(army.UnitIds.Contains))),
                view.Characters.Where(c => c.Side == view.You && c.Location == move.From && c.Garrison == army.Garrison),
                ids => edit(() => ArmyDraft.Toggle(draft, ids.Where(army.CharacterIds.Contains), true)), requiredAnyIds: army.RequiredAnyIds);
            root.Add(grid.Root);
            refresh.Add(grid.Refresh);
            var costs = BoardScreen.Row();
            root.Add(costs);
            foreach (var face in new[] { DieFace.Army, DieFace.Character })
            {
                var button = BoardScreen.Button(face == DieFace.Army ? "Army attack" : "Led attack", () => edit(() => draft.Change(p => { p.MovementCost = face; p.AbilityId = ""; })));
                button.name = "attack-cost-" + face;
                costs.Add(button);
                refresh.Add(() =>
                {
                    var available = option.MovementCosts.Where(c => c.DieId == draft.Payload.DieId).ToList();
                    button.style.display = available.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
                    button.SetEnabled(available.Any(c => c.Face == face));
                    button.EnableInClassList("selected", draft.Payload.MovementCost == face);
                });
            }
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
