using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ArmyChoices
    {
        private readonly List<Action> refresh = new List<Action>();
        public ArmyChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit, BoardPresentation data)
        {
            var paths = BoardScreen.Row();
            root.Add(paths);
            for (var i = 0; i < draft.Payload.Moves.Count; i++)
            {
                var index = i;
                var button = BoardScreen.Button("", () => edit(() => ArmyDraft.Edit(draft, index)));
                button.name = "edit-move-" + index;
                paths.Add(button);
                paths.Add(IconButton.Create("close", "Close", () => edit(() => ArmyDraft.Remove(draft, index))));
                refresh.Add(() =>
                {
                    var move = draft.Payload.Moves[index];
                    button.text = (index + 1) + " · " + data.Regions[move.From].Name + " → " + (move.To.Length > 0 ? data.Regions[move.To].Name : "Choose destination");
                    button.EnableInClassList("selected", ArmyDraft.VisibleIndex(draft) == index);
                });
            }
            var current = ArmyDraft.Current(draft);
            if (current == null)
            {
                root.Add(new Label("Choose an army on the map."));
                return;
            }
            var arrivals = draft.Payload.Moves.Take(ArmyDraft.VisibleIndex(draft)).Where(m => m.To == current.From)
                .SelectMany(m => m.UnitIds.Concat(m.CharacterIds)).ToHashSet();
            var figures = view.Units.Where(u => (u.Location == current.From || arrivals.Contains(u.Id)) && u.Side == view.You && !u.Garrison);
            var characters = view.Characters.Where(c => (c.Location == current.From || arrivals.Contains(c.Id)) && c.Side == view.You && !c.Garrison).ToList();
            var used = new HashSet<string>();
            refresh.Add(() => used = ArmyDraft.UsedElsewhere(draft, ArmyDraft.VisibleIndex(draft)));
            var army = view.Options.First(o => o.Kind == CommandKind.MoveArmies).Armies.First(a => a.RegionId == current.From);
            var eligible = army.CharacterIds;
            var fixedCharacters = characters.Where(c => !eligible.Contains(c.Id)).Select(c => c.Id).ToHashSet();
            var grid = new FigureGrid(figures,
                id => ArmyDraft.Current(draft).UnitIds.Contains(id) || ArmyDraft.Current(draft).CharacterIds.Contains(id),
                id => used.Contains(id) || fixedCharacters.Contains(id),
                ids => edit(() => ArmyDraft.Toggle(draft, ids)), characters,
                ids => edit(() => ArmyDraft.Toggle(draft, ids, true)), requiredAnyIds: army.RequiredAnyIds);
            root.Add(grid.Root);
            refresh.Add(grid.Refresh);
            var modes = BoardScreen.Row();
            root.Add(modes);
            foreach (var face in new[] { DieFace.Army, DieFace.Character })
            {
                var button = BoardScreen.Button(face == DieFace.Army ? "Army · up to two moves" : "Character · one led army",
                    () => edit(() => draft.Change(p => { p.MovementCost = face; p.AbilityId = ""; })));
                button.name = "move-cost-" + face;
                modes.Add(button);
                refresh.Add(() =>
                {
                    var costs = view.Options.First(o => o.Kind == CommandKind.MoveArmies).MovementCosts.Where(c => c.DieId == draft.Payload.DieId).ToList();
                    button.style.display = costs.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
                    button.SetEnabled(costs.Any(c => c.Face == face && c.MaximumMoves >= draft.Payload.Moves.Count));
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
