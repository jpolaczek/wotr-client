using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Recorded facts only. This layer never creates targets or changes a map selection.
    public sealed class MapActionOverlay
    {
        public VisualElement Root { get; } = new VisualElement { name = "action-map-history", pickingMode = PickingMode.Ignore };
        public ActionContextModel Context { get; private set; }
        public bool Suppressed => dragging || map.Panning || map.DraftOverlay?.Targets.Count > 0;
        private readonly MapCanvas map;
        private readonly List<RegionGeometry> regions = new List<RegionGeometry>();
        private bool dragging;

        public MapActionOverlay(MapCanvas map)
        {
            this.map = map;
            Root.AddToClassList("map-action-layer");
            map.Add(Root);
        }

        public void Apply(ActionContextModel context)
        {
            Context = context;
            regions.Clear();
            if (context != null)
                foreach (var region in context.Regions.Distinct())
                    if (map.Data.Shapes.TryGetValue(region, out var shape))
                        regions.Add(shape);
            map.Refresh();
        }

        public void PreviewDrag(DraftToken token)
        {
            dragging = token != null;
            Layout();
            map.Redraw();
        }

        public void Layout()
        {
            Root.style.display = Context == null ? DisplayStyle.None : DisplayStyle.Flex;
            Root.style.opacity = Suppressed ? 0 : 1;
        }

        public void Draw(Painter2D painter)
        {
            if (Context == null || Suppressed)
                return;
            painter.strokeColor = new Color(.57f, .83f, .95f, .85f);
            painter.lineWidth = 2;
            foreach (var region in regions)
                foreach (var ring in region.Rings)
                {
                    painter.BeginPath();
                    for (var index = 0; index < ring.Points.Length; index++)
                    {
                        var point = ring.Points[index];
                        var screen = map.Camera.ToScreen(new Vector2(point[0], point[1]));
                        if (index == 0)
                            painter.MoveTo(screen);
                        else
                            painter.LineTo(screen);
                    }
                    painter.ClosePath();
                    painter.Stroke();
                }
        }

        public bool Frame(Rect viewport)
        {
            if (Context == null) return false;
            var points = Context.Regions.Where(map.Data.Shapes.ContainsKey).Select(id =>
                new Vector2(map.Data.Shapes[id].Anchor[0], map.Data.Shapes[id].Anchor[1])).ToArray();
            if (points.Length == 0) return false;
            var min = new Vector2(points.Min(p => p.x), points.Min(p => p.y));
            var max = new Vector2(points.Max(p => p.x), points.Max(p => p.y));
            var bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            var preferredZoom = Mathf.Min(1.5f, viewport.width / (bounds.width + 200), viewport.height / (bounds.height + 150));
            map.Camera.Constrain(map.CanvasBounds, map.contentRect);
            if (!map.Camera.TryFrame(bounds, viewport, preferredZoom))
                return false;
            map.Refresh();
            return true;
        }

    }
}
