using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Dense figure choices use tabs for presentation only. Switching tabs never selects a figure.
    public sealed class EffectFigurePicker
    {
        private readonly List<Action> refresh = new List<Action>();
        public HashSet<string> Rendered { get; } = new HashSet<string>();
        private string active;

        public EffectFigurePicker(VisualElement root, PlayerView view, ActionOption option, TurnDraft draft, Action<Action> edit)
        {
            var figures = option.EffectChoices.Where(c => (c.SelectionGroup.Length == 0 || c.SelectionGroup == "Sacrifice" || view.Decision?.Presentation == "LeaderOrCompanion") &&
                (view.Units.Any(u => u.Id == c.Id) || view.Characters.Any(u => u.Id == c.Id))).ToList();
            if (figures.Count < 6)
                return;
            var groups = figures.GroupBy(c => view.Units.FirstOrDefault(u => u.Id == c.Id)?.Nation ?? "companions").ToList();
            active = groups.FirstOrDefault(g => g.Any(c => draft.Payload.Ids.Contains(c.Id)))?.Key ?? groups[0].Key;
            var tabs = BoardScreen.Row("effect-figure-tabs");
            root.Add(tabs);
            foreach (var group in groups)
            {
                var key = group.Key;
                var tab = BoardScreen.Button(FigureGrid.Name(key), () => { active = key; Refresh(); });
                tab.name = "effect-tab-" + key;
                tabs.Add(tab);
                var row = BoardScreen.Row("effect-figure-row");
                root.Add(row);
                foreach (var choice in group)
                    AddFigure(row, view, option, draft, edit, choice);
                refresh.Add(() =>
                {
                    row.style.display = active == key ? DisplayStyle.Flex : DisplayStyle.None;
                    tab.EnableInClassList("selected", active == key);
                    tab.EnableInClassList("contains-selection", group.Any(c => draft.Payload.Ids.Contains(c.Id)));
                });
            }
            foreach (var choice in option.EffectChoices.Where(c => c.Exclusive))
                AddFigure(tabs, view, option, draft, edit, choice);
        }

        private void AddFigure(VisualElement root, PlayerView view, ActionOption option, TurnDraft draft, Action<Action> edit, EffectChoiceView choice)
        {
            Rendered.Add(choice.Id);
            var unit = view.Units.FirstOrDefault(u => u.Id == choice.Id);
            var label = unit != null ? FigureGrid.Symbol(unit.Kind) : choice.Title.Length > 0 ? choice.Title : CharacterLibrary.ShortName(view, choice.Id);
            var button = BoardScreen.Button(label, () => edit(() => CardDraft.Choose(draft, option, choice.Id)));
            button.name = "effect-choice-" + choice.Id;
            button.tooltip = (choice.Exclusive ? choice.Title : unit != null ? FigureGrid.Name(unit.Nation) + " " + unit.Kind : CharacterLibrary.Name(view, choice.Id)) + " · " + choice.Detail;
            button.AddToClassList(unit != null ? "effect-unit" : "effect-companion");
            root.Add(button);
            refresh.Add(() =>
            {
                button.EnableInClassList("selected", draft.Payload.Ids.Contains(choice.Id));
                button.SetEnabled(EffectChoiceDraft.CanChoose(draft, option, choice.Id));
            });
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
