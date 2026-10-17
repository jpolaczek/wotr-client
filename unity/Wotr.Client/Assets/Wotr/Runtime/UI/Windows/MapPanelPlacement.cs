using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // The map fills the screen; decision windows use the safe area between HUDs.
    public static class MapPanelPlacement
    {
        public static Vector2 Anchor(MapCanvas map, VisualElement panel, Vector2 mapPoint) =>
            panel.parent.WorldToLocal(map.LocalToWorld(mapPoint));

        // UI-225: whether a placed panel can stay where it is: inside its parent and clear of the HUD panels.
        public static bool Fits(MapCanvas map, VisualElement panel, Rect rect)
        {
            var parent = panel.parent;
            if (parent == null || float.IsNaN(rect.width) || rect.width <= 0 || float.IsNaN(rect.height) || rect.height <= 0)
                return false;
            var area = parent.contentRect;
            if (rect.xMin < area.xMin || rect.yMin < area.yMin || rect.xMax > area.xMax || rect.yMax > area.yMax)
                return false;
            if (map.Occlusions == null)
                return true;
            var own = new Rect(map.WorldToLocal(panel.worldBound.position), panel.worldBound.size);
            foreach (var rectangle in map.Occlusions())
                if (rectangle != own && new Rect(parent.WorldToLocal(map.LocalToWorld(rectangle.position)), rectangle.size).Overlaps(rect))
                    return false;
            return true;
        }

        // markers: map-local rectangles the panel must not cover, e.g. the inspected region's own forces (UI-126).
        public static Rect Near(MapCanvas map, VisualElement panel, Vector2 mapPoint, float reservedRight = 0,
            IEnumerable<Rect> markers = null)
        {
            var parent = panel.parent;
            var anchor = Anchor(map, panel, mapPoint);
            var own = new Rect(map.WorldToLocal(panel.worldBound.position), panel.worldBound.size);
            var blockers = new List<Rect>();
            if (map.Occlusions != null)
                foreach (var rectangle in map.Occlusions())
                {
                    if (rectangle == own)
                        continue;
                    blockers.Add(new Rect(parent.WorldToLocal(map.LocalToWorld(rectangle.position)), rectangle.size));
                }
            if (markers != null)
                foreach (var rectangle in markers)
                    blockers.Add(new Rect(parent.WorldToLocal(map.LocalToWorld(rectangle.position)), rectangle.size));
            var available = parent.contentRect;
            if (reservedRight > 0)
                available.width = Mathf.Max(0, available.width - reservedRight);
            return PopoverPlacement.Near(anchor, panel.layout.size, available, blockers);
        }
    }
}
