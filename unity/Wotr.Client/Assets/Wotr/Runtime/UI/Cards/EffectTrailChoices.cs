using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal static class EffectTrailChoices
    {
        public static void Render(VisualElement root, PlayerView view, BoardPresentation data, Action<string> inspect)
        {
            if (view.EffectTrail.Count == 0)
                return;
            var sovereigns = view.Sovereigns.Select(s => s.Id).ToHashSet();
            var units = view.Units.Where(unit => !sovereigns.Contains(unit.Id)).ToDictionary(unit => unit.Id);
            var groups = view.EffectTrail.Where(item => units.ContainsKey(item.ChoiceId) && data.Regions.ContainsKey(item.TargetId))
                .GroupBy(item => (item.SourceId, item.TargetId)).ToDictionary(group => group.Key, group => group.ToList());
            var displayed = new HashSet<(string SourceId, string TargetId)>();
            var trail = BoardScreen.Row("effect-trail");
            root.Add(trail);
            foreach (var item in view.EffectTrail)
            {
                var key = (item.SourceId, item.TargetId);
                if (units.ContainsKey(item.ChoiceId) && groups.TryGetValue(key, out var group))
                {
                    if (displayed.Add(key))
                        AddFigures(trail, group, units, data, inspect);
                }
                else
                    AddChoice(trail, item, view, data, inspect);
            }
        }

        private static void AddFigures(VisualElement trail, List<EffectTrailView> group,
            Dictionary<string, UnitView> units, BoardPresentation data, Action<string> inspect)
        {
            var first = group[0];
            var figures = group.Select(item => units[item.ChoiceId]).ToList();
            var symbols = string.Join(" ", figures.GroupBy(unit => unit.Kind)
                .Select(kind => FigureGrid.Symbol(kind.Key) + kind.Count()));
            var region = data.Regions[first.TargetId].Name;
            var details = string.Join("\n", figures.GroupBy(unit => (unit.Nation, unit.Kind))
                .Select(kind => $"{kind.Count()} × {FigureGrid.Name(kind.Key.Nation)} {kind.Key.Kind} → {region}"));
            var step = Add(trail, first.SourceId, symbols + " · " + region, inspect);
            step.name = "committed-figures-" + first.SourceId + "-" + first.TargetId;
            step.tooltip = details + "\n" + step.tooltip;
        }

        private static void AddChoice(VisualElement trail, EffectTrailView item, PlayerView view,
            BoardPresentation data, Action<string> inspect)
        {
            var label = item.ChoiceId.Length > 0 ? CardChoices.ChoiceName(view, data, item.ChoiceId)
                : CardLibrary.Cards.ContainsKey(item.SourceId) ? CardLibrary.Title(item.SourceId) : CharacterLibrary.Name(view, item.SourceId);
            if (item.ChoiceId != item.TargetId && data.Regions.TryGetValue(item.TargetId, out var region))
                label += " → " + region.Name;
            Add(trail, item.SourceId, label, inspect);
        }

        private static Button Add(VisualElement trail, string sourceId, string label, Action<string> inspect)
        {
            var cardSource = CardLibrary.Cards.ContainsKey(sourceId);
            var step = BoardScreen.Button("✓ " + label, () =>
            {
                if (cardSource)
                    inspect(sourceId);
            });
            step.AddToClassList("committed-choice");
            step.tooltip = cardSource ? "Committed · Inspect source" : "Committed";
            trail.Add(step);
            return step;
        }
    }
}
