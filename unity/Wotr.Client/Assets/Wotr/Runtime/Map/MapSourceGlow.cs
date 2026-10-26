using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-323 (owner request 2026-10-24: "we must also clearly mark the region where that unit or character stands"): the
    // regions the player's own draft moves from have a steady gold edge — the selected force's gold, a soft wide halo and a
    // bright line — in their own retained layer just above the target glow and under the bands and barriers. It never
    // pulses, so it never reads as a choice, and it repaints only when its regions change.
    public sealed class MapSourceGlow
    {
        public static readonly Color Gold = new Color(1f, .84f, .44f, 1f);
        internal MapVectorLayer Layer { get; }
        private readonly Dictionary<string, RegionGeometry> shapes = new Dictionary<string, RegionGeometry>();
        private readonly HashSet<string> regions = new HashSet<string>();

        internal MapSourceGlow(MapGeometry geometry, Vector2 size)
        {
            foreach (var shape in geometry.Regions)
                shapes[shape.Id] = shape;
            Layer = new MapVectorLayer("map-source-glow", size.x, size.y, context => Draw(context.painter2D));
            Layer.AddToClassList("map-source-glow");
        }

        public bool Glowing => regions.Count > 0;
        public IReadOnlyCollection<string> Regions => regions;

        internal void Update(IReadOnlyList<string> sources)
        {
            if (MapTargetGlow.Changed(regions, sources))
                Layer.MarkDirtyRepaint();
        }

        private void Draw(Painter2D painter)
        {
            foreach (var id in regions)
            {
                if (!shapes.TryGetValue(id, out var shape))
                    continue;
                // The halo is wider than the region's own border band, so it shows on both sides of it.
                MapVectorDrawing.Outline(painter, shape.Rings, new Color(Gold.r, Gold.g, Gold.b, .45f), 14f);
                MapVectorDrawing.Outline(painter, shape.Rings, new Color(Gold.r, Gold.g, Gold.b, .95f), 3.2f);
            }
        }
    }
}
