using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Expand only when the actual laid-out rules exceed the available height.
    // Retain the settled width across projections, hover and pin changes.
    internal sealed class CardViewportLayout
    {
        internal const float MaximumInitialWidth = 500;
        private readonly VisualElement root;
        private readonly Label eventText;
        private readonly Label combatText;
        private readonly VisualElement toolbar;
        private readonly IVisualElementScheduledItem reflow;
        private string previousEvent;
        private string previousCombat;
        private DisplayStyle previousToolbar;
        private Vector2 previousViewport;

        public CardViewportLayout(VisualElement root, Label eventText, Label combatText, VisualElement toolbar)
        {
            this.root = root;
            this.eventText = eventText;
            this.combatText = combatText;
            this.toolbar = toolbar;
            reflow = root.schedule.Execute(RefitOverflow);
            reflow.Pause();
        }

        public void Fit()
        {
            if (root.parent == null)
                return;
            var viewport = root.parent.contentRect.size;
            if (viewport.x <= 0 || viewport.y <= 0)
                return;
            if (previousViewport != viewport || previousEvent != eventText.text || previousCombat != combatText.text ||
                previousToolbar != toolbar.style.display.value)
            {
                previousViewport = viewport;
                previousEvent = eventText.text;
                previousCombat = combatText.text;
                previousToolbar = toolbar.style.display.value;
                var length = eventText.text.Length + combatText.text.Length;
                var compact = viewport.y < 360;
                root.EnableInClassList("compact-card", compact);
                var width = length > 450 ? 400f : 336f;
                if (compact && length > 300)
                    width = length > 450 ? MaximumInitialWidth : 440f;
                SetWidth(Mathf.Min(width, viewport.x - 16));
            }
            if (SizeRules())
                LayoutChanged();
        }

        public void LayoutChanged() => reflow.ExecuteLater(0);

        private void RefitOverflow()
        {
            // minHeight is measured at the new width. Allow the resulting flow
            // layout to settle before deciding whether another expansion is needed.
            if (root.parent == null || root.resolvedStyle.display == DisplayStyle.None)
                return;
            if (SizeRules())
            {
                LayoutChanged();
                return;
            }
            var viewport = root.parent.contentRect;
            if (root.layout.height <= viewport.height - 8 || viewport.width <= 16)
                return;
            if (!root.ClassListContains("compact-card"))
            {
                root.AddToClassList("compact-card");
                return;
            }
            var estimated = root.layout.width * root.layout.height / Mathf.Max(1, viewport.height - 8);
            var width = Mathf.Min(Mathf.Max(root.layout.width + 48, estimated), viewport.width - 16);
            SetWidth(width);
        }

        private void SetWidth(float width)
        {
            if (width > 0 && Mathf.Abs(root.style.width.value.value - width) > .5f)
                root.style.width = width;
        }

        private bool SizeRules()
        {
            if (root.resolvedStyle.display == DisplayStyle.None)
                return false;
            var changed = FitText(eventText);
            if (combatText.parent.resolvedStyle.display != DisplayStyle.None)
                changed |= FitText(combatText);
            return changed;
        }

        private static bool FitText(Label label)
        {
            var width = label.contentRect.width;
            if (label.panel == null || width <= 0 || float.IsNaN(width))
                return false;
            var measured = label.MeasureTextSize(label.text, width, VisualElement.MeasureMode.Exactly,
                0, VisualElement.MeasureMode.Undefined);
            var height = Mathf.Ceil(measured.y + label.resolvedStyle.paddingTop + label.resolvedStyle.paddingBottom);
            if (float.IsNaN(height) || Mathf.Abs(label.style.minHeight.value.value - height) <= .5f)
                return false;
            label.style.minHeight = height;
            return true;
        }
    }
}
