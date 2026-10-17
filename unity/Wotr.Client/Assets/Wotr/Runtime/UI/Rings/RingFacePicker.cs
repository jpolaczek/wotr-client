using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-161: the results an Elven Ring can give the chosen die, in a small popover over the die's column (art:
    // console/picker and console/picker-tail). It stands above the whole dice panel, so it covers neither the die
    // nor the panel's header and help links; the tail points at the die. A click on a result completes the gesture.
    public sealed class RingFacePicker
    {
        private const float Gap = 10f;
        private const float Margin = 4f;
        public VisualElement Root { get; } = new VisualElement { name = "ring-face-picker" };
        public event Action<DieFace> Chosen;
        public bool IsOpen => Root.style.display.value == DisplayStyle.Flex;
        private readonly VisualElement row = new VisualElement { name = "ring-face-row" };
        private readonly VisualElement tail = new VisualElement { name = "ring-face-tail", pickingMode = PickingMode.Ignore };
        private VisualElement anchor;
        private VisualElement ceiling;
        private string shown = "";

        public RingFacePicker()
        {
            Root.AddToClassList("ring-face-picker");
            row.AddToClassList("ring-face-row");
            tail.AddToClassList("ring-face-tail");
            Root.Add(row);
            Root.Add(tail);
            Root.style.display = DisplayStyle.None;
            Root.RegisterCallback<GeometryChangedEvent>(_ => Position());
        }

        // key names the die and its results: the same key keeps the buttons and only follows the die. The popover
        // stays above ceiling, the panel that holds the die.
        public void Show(VisualElement die, VisualElement panel, Side side, IReadOnlyList<RingFace> faces, string key)
        {
            anchor = die;
            ceiling = panel;
            if (key != shown)
            {
                shown = key;
                Build(side, faces);
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

        // Centred on the die above its panel, kept inside the board; the tail points at the die's column.
        public void Position()
        {
            if (anchor?.panel == null || Root.parent == null || !IsOpen)
                return;
            var width = Root.resolvedStyle.width;
            var height = Root.resolvedStyle.height;
            if (float.IsNaN(width) || float.IsNaN(height))
                return;
            var die = Root.parent.WorldToLocal(anchor.worldBound);
            var top = ceiling?.panel == null ? die.yMin : Mathf.Min(die.yMin, Root.parent.WorldToLocal(ceiling.worldBound).yMin);
            var left = Mathf.Clamp(die.center.x - width / 2, Margin, Mathf.Max(Margin, Root.parent.contentRect.width - width - Margin));
            Root.style.left = left;
            Root.style.top = top - height - Gap;
            tail.style.left = die.center.x - left - tail.resolvedStyle.width / 2;
        }

        private void Build(Side side, IReadOnlyList<RingFace> faces)
        {
            row.Clear();
            foreach (var option in faces)
                row.Add(FaceButton(side, option));
        }

        private Button FaceButton(Side side, RingFace option)
        {
            var face = option.Face;
            var button = new Button(() => Chosen?.Invoke(face)) { name = "ring-face-" + DieFaceIcon.Key(face) };
            button.AddToClassList("ring-face");
            button.AddToClassList(side == Side.FreePeoples ? "die-free" : "die-shadow");
            button.Add(new DieFaceIcon(face));
            button.tooltip = ActionCardText.Face(face) + (option.Enabled ? "" : " · Current result");
            button.SetEnabled(option.Enabled);
            return button;
        }
    }
}
