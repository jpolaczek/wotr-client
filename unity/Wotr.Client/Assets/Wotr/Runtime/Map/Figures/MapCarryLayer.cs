using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-444 (owner, 2026-11-02): a pointer-ignoring layer laid over the map in the board's root, in front of the HUD and the
    // windows, so neither the map's edge nor the top bar, the console or a rail cuts what stands in it. Its points are the
    // map's own (map-local), so a piece placed for the map stands at the same spot here. Made on first use.
    // UI-454 (owner, 2026-11-03): shared by the carried pieces of the hand (MapFigureHand), the Reserve figure carried onto
    // the map (MapRecruitCarry) and the recruits flying from their Reserve slot (ReserveFlights).
    internal sealed class MapCarryLayer
    {
        public const string LayerName = "map-hand-carried";
        // How far a carried piece keeps from the screen's edge; a gold rim and a plaque reach a little past the visible part
        // a stand measures.
        public const float ScreenMargin = 6;
        private readonly MapCanvas map;
        private VisualElement layer;

        public MapCarryLayer(MapCanvas map)
        {
            this.map = map;
        }

        // Whether an element stands in this layer now.
        public bool Holds(VisualElement element) => layer != null && element.parent == layer;

        // The element moves into the layer, which is laid over the map and brought in front of everything in its host.
        public void Take(VisualElement element)
        {
            var host = map.parent;
            if (host == null)
                return;
            if (layer == null)
            {
                layer = new VisualElement { name = LayerName, pickingMode = PickingMode.Ignore };
                layer.AddToClassList(LayerName);
                layer.style.position = Position.Absolute;
            }
            if (layer.parent != host)
                host.Add(layer);
            layer.BringToFront();
            var frame = map.layout;
            layer.style.left = frame.x;
            layer.style.top = frame.y;
            layer.style.width = frame.width;
            layer.style.height = frame.height;
            layer.Add(element);
        }

        // What the screen shows of the map (map-local), ScreenMargin inside its edges: the map within its host; null before
        // layout.
        public Rect? Screen()
        {
            var frame = map.layout;
            if (float.IsNaN(frame.width) || frame.width <= 0 || frame.height <= 0)
                return null;
            var area = new Rect(0, 0, frame.width, frame.height);
            if (map.parent?.layout is Rect host && !float.IsNaN(host.width))
                area = Rect.MinMaxRect(Mathf.Max(0, -frame.x), Mathf.Max(0, -frame.y),
                    Mathf.Min(frame.width, host.width - frame.x), Mathf.Min(frame.height, host.height - frame.y));
            return Rect.MinMaxRect(area.xMin + ScreenMargin, area.yMin + ScreenMargin, area.xMax - ScreenMargin, area.yMax - ScreenMargin);
        }

        // How far what shows (map-local) moves to lie whole on the screen: nothing when it does, otherwise just enough inward.
        public Vector2 Inward(Rect shown)
        {
            if (!(Screen() is Rect screen))
                return Vector2.zero;
            return new Vector2(Inward(shown.xMin, shown.xMax, screen.xMin, screen.xMax),
                Inward(shown.yMin, shown.yMax, screen.yMin, screen.yMax));
        }

        // How far an extent moves to lie inside a range: none when it does; one too large starts where the range starts.
        private static float Inward(float min, float max, float low, float high)
        {
            if (max - min >= high - low || min < low)
                return low - min;
            return max > high ? high - max : 0;
        }
    }
}
