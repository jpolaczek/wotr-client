using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Move only the fixed shortcut. A player's saved window position is never changed.
    internal sealed class VictoryShortcut
    {
        private readonly VisualElement tab;
        private readonly List<VisualElement> windows = new List<VisualElement>();
        private readonly List<Rect> obstacles = new List<Rect>();

        public VictoryShortcut(VisualElement tab) => this.tab = tab;

        public void Avoid(VisualElement window)
        {
            if (windows.Contains(window))
                return;
            windows.Add(window);
            window.RegisterCallback<GeometryChangedEvent>(_ => Refresh());
            Refresh();
        }

        public void Refresh()
        {
            var viewport = tab.parent;
            if (viewport == null || tab.layout.width <= 0 || tab.layout.height <= 0 ||
                float.IsNaN(tab.layout.width) || viewport.contentRect.width <= 0)
                return;
            obstacles.Clear();
            foreach (var window in windows)
            {
                if (window.panel == null || window.resolvedStyle.display == DisplayStyle.None)
                    continue;
                var minimum = viewport.WorldToLocal(window.worldBound.min);
                obstacles.Add(new Rect(minimum, viewport.WorldToLocal(window.worldBound.max) - minimum));
            }
            var right = viewport.contentRect.xMax - tab.layout.width - tab.resolvedStyle.marginRight - 8;
            var bottom = viewport.contentRect.yMax - tab.layout.height - tab.resolvedStyle.marginBottom - 8;
            var preferred = new Rect(right, bottom, tab.layout.width, tab.layout.height);
            var placement = VictoryShortcutPlacement.Place(preferred, viewport.contentRect.min + Vector2.one * 8, obstacles);
            var current = viewport.WorldToLocal(tab.worldBound.min);
            if (Vector2.Distance(current, placement.position) < .1f)
                return;
            tab.style.right = StyleKeyword.Auto;
            tab.style.bottom = StyleKeyword.Auto;
            tab.style.left = placement.x - tab.resolvedStyle.marginLeft;
            tab.style.top = placement.y - tab.resolvedStyle.marginTop;
        }
    }
}
