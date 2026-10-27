using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-377 (owner's request): the Council token's travel in a small popover just above the token pool, pointing at the
    // token, as the Ring's faces are (RingFacePicker): only the icons of the Nazgûl it may move and the ✓ that ends the
    // choice. A lit icon adds its Nazgûl to the group being planned; a chosen one shows its selection, a planned one dims.
    public sealed class CouncilTravelPicker
    {
        private const float Gap = 10f;
        private const float Margin = 4f;
        public VisualElement Root { get; } = new VisualElement { name = "council-travel-picker" };
        public Button Confirm { get; }
        public event Action<string> FigureChosen;
        public event Action Confirmed;
        public bool IsOpen => Root.style.display.value == DisplayStyle.Flex;
        // The icons' tooltips name the figure's region.
        public Func<string, string> RegionName { get; set; } = id => id;
        private readonly VisualElement row = new VisualElement { name = "council-travel-figures" };
        private readonly VisualElement tail = new VisualElement { name = "council-travel-tail", pickingMode = PickingMode.Ignore };
        private readonly Dictionary<string, Button> figures = new Dictionary<string, Button>();
        private readonly ChoicePulse pulse;
        private VisualElement anchor;
        private VisualElement ceiling;
        private string shown = "";

        public CouncilTravelPicker()
        {
            Root.AddToClassList("ring-face-picker");
            Root.AddToClassList("council-travel-picker");
            row.AddToClassList("council-travel-figures");
            tail.AddToClassList("ring-face-tail");
            Confirm = IconButton.Create("confirm", "Use the Council token", () => Confirmed?.Invoke());
            Confirm.name = "council-travel-confirm";
            Confirm.AddToClassList("council-travel-confirm");
            Root.Add(row);
            Root.Add(Confirm);
            Root.Add(tail);
            Root.style.display = DisplayStyle.None;
            // Moving the popover at a fractional scale must not start another placement (PopoverPlacement.SizeChanged).
            Root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (PopoverPlacement.SizeChanged(e.oldRect, e.newRect))
                    Position();
            });
            pulse = new ChoicePulse(row);
        }

        // token is the Council token's medallion and pool the panel that holds it; the same figures keep their icons.
        public void Show(VisualElement token, VisualElement pool, PlayerView view, IReadOnlyList<string> ids)
        {
            anchor = token;
            ceiling = pool;
            var key = view.MatchId + "/" + view.Decision?.Id + "/" + string.Join(",", ids);
            if (key != shown)
            {
                shown = key;
                Build(view, ids);
            }
            if (!IsOpen)
            {
                Root.style.display = DisplayStyle.Flex;
                Root.BringToFront();
            }
            Position();
        }

        public void Hide()
        {
            anchor = null;
            ceiling = null;
            shown = "";
            Root.style.display = DisplayStyle.None;
        }

        // Each icon's state: chosen for the current group, planned in another, or lit while a click can still add it.
        public void Apply(Func<string, bool> chosen, Func<string, bool> planned, Func<string, bool> selectable, bool enabled)
        {
            var lit = false;
            foreach (var pair in figures)
            {
                var id = pair.Key;
                var button = pair.Value;
                var isChosen = chosen(id);
                var canChoose = enabled && selectable(id);
                button.EnableInClassList("selected", isChosen);
                button.EnableInClassList("planned-group", planned(id));
                button.SetEnabled(canChoose || isChosen);
                ChoiceGlow.Apply(button, canChoose && !isChosen);
                lit |= canChoose && !isChosen;
            }
            pulse.Run(lit);
        }

        public void SetConfirm(bool enabled, string tooltip)
        {
            Confirm.SetEnabled(enabled);
            Confirm.tooltip = tooltip;
            ChoiceGlow.Apply(Confirm, enabled);
        }

        // Centred on the token above its pool, kept inside the board; the tail points at the token.
        public void Position()
        {
            if (anchor?.panel == null || Root.parent == null || !IsOpen)
                return;
            var width = Root.resolvedStyle.width;
            var height = Root.resolvedStyle.height;
            if (float.IsNaN(width) || float.IsNaN(height))
                return;
            var token = Root.parent.WorldToLocal(anchor.worldBound);
            var top = ceiling?.panel == null ? token.yMin : Mathf.Min(token.yMin, Root.parent.WorldToLocal(ceiling.worldBound).yMin);
            var left = Mathf.Round(Mathf.Clamp(token.center.x - width / 2, Margin, Mathf.Max(Margin, Root.parent.contentRect.width - width - Margin)));
            Move(Root, left, Mathf.Round(top - height - Gap));
            Move(tail, Mathf.Round(token.center.x - left - tail.resolvedStyle.width / 2), float.NaN);
        }

        private static void Move(VisualElement element, float left, float top)
        {
            if (Mathf.Abs(element.resolvedStyle.left - left) > 0.5f)
                element.style.left = left;
            if (!float.IsNaN(top) && Mathf.Abs(element.resolvedStyle.top - top) > 0.5f)
                element.style.top = top;
        }

        private void Build(PlayerView view, IReadOnlyList<string> ids)
        {
            row.Clear();
            figures.Clear();
            foreach (var id in ids)
            {
                var button = Figure(view, id);
                figures[id] = button;
                row.Add(button);
            }
        }

        private Button Figure(PlayerView view, string id)
        {
            var button = BoardScreen.Button("", () => FigureChosen?.Invoke(id));
            button.name = "council-travel-" + id;
            button.AddToClassList("companion-portrait");
            button.AddToClassList("council-travel-figure");
            var unit = view.Units.Find(u => u.Id == id);
            var character = view.Characters.Find(c => c.Id == id);
            if (unit != null)
                FigureArtwork.Apply(button, unit);
            else if (character != null)
                FigureArtwork.Apply(button, character);
            var location = unit?.Location ?? character?.Location ?? "";
            button.tooltip = CharacterLibrary.Name(view, id) + (location.Length > 0 ? " · " + RegionName(location) : "") +
                " · Click, then its destination on the map";
            return button;
        }
    }
}
