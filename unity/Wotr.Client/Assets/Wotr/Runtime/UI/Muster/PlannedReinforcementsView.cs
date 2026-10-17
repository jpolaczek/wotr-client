using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-131: the figures a card or ability plans to recruit, each with × to return it to the reserve.
    public sealed class PlannedReinforcementsView
    {
        public VisualElement Root { get; } = BoardScreen.Row("reinforcement-placements");
        private readonly Action<string> removed;
        private readonly List<Chip> chips = new List<Chip>();

        public PlannedReinforcementsView(Action<string> removed)
        {
            this.removed = removed;
            Root.AddToClassList("reinforcement-placements");
            Root.style.display = DisplayStyle.None;
        }

        public void Apply(ReinforcementsState state)
        {
            Root.style.display = state.Planning ? DisplayStyle.Flex : DisplayStyle.None;
            while (chips.Count < state.Placements.Count)
            {
                var chip = new Chip(removed);
                chips.Add(chip);
                Root.Add(chip.Button);
            }
            for (var i = 0; i < chips.Count; i++)
            {
                var shown = state.Planning && i < state.Placements.Count;
                chips[i].Button.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                if (shown)
                    chips[i].Apply(state.Placements[i], state, i);
                else
                    chips[i].Clear();
            }
        }

        private sealed class Chip
        {
            public Button Button { get; }
            private readonly Image image = FigureArtwork.Image(FigureAppearance.FreeRegular);
            private readonly Label region = new Label { pickingMode = PickingMode.Ignore };
            private string unit = "";

            public Chip(Action<string> removed)
            {
                Button = BoardScreen.Button("", () =>
                {
                    if (unit.Length > 0)
                        removed(unit);
                });
                Button.AddToClassList("planned-reinforcement");
                image.AddToClassList("planned-reinforcement-figure");
                region.AddToClassList("planned-reinforcement-region");
                Button.Add(image);
                Button.Add(region);
            }

            // A hidden chip names no figure, so nothing can return a figure that is no longer planned.
            public void Clear()
            {
                unit = "";
                Button.userData = null;
            }

            public void Apply(PlannedReinforcement placement, ReinforcementsState state, int index)
            {
                unit = placement.UnitId;
                Button.name = state.PlacementPrefix + index;
                Button.userData = placement.UnitId;
                Button.SetEnabled(state.Interactive);
                image.sprite = FigureArtwork.Sprite(FigureAppearances.Unit(placement.Nation, placement.Kind, state.Side));
                region.text = placement.RegionName + " ×";
                Button.tooltip = FigureGrid.Name(placement.Nation) + " " + placement.Kind + " → " + placement.RegionName +
                    " · Return it to the reserve";
            }
        }
    }
}
