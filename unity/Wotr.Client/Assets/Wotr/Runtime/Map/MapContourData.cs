using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MapContourData
    {
        public const string Version = "contours-2";
        private readonly List<(RegionGeometry Region, Rect Bounds)> indexed;
        public Rect Bounds { get; }

        public MapContourData(MapGeometry geometry, IEnumerable<string> regionIds)
        {
            MapContourValidation.Validate(geometry, regionIds);
            indexed = geometry.Regions.OrderBy(r => r.Id, StringComparer.Ordinal)
                .Select(region => (region, RegionBounds(region))).ToList();
            Bounds = Rect.MinMaxRect(indexed.Min(r => r.Bounds.xMin), indexed.Min(r => r.Bounds.yMin),
                indexed.Max(r => r.Bounds.xMax), indexed.Max(r => r.Bounds.yMax));
        }

        public string HitTest(Vector2 point)
        {
            foreach (var item in indexed)
            {
                var bounds = item.Bounds;
                if (point.x < bounds.xMin || point.x > bounds.xMax || point.y < bounds.yMin || point.y > bounds.yMax)
                    continue;
                if (MapContourMath.Contains(item.Region, point))
                    return item.Region.Id;
            }
            return null;
        }

        public static Vector2 FromSource(MapGeometry geometry, Vector2 point) =>
            point * geometry.Transform.SourceScale +
            new Vector2(geometry.Transform.SourceOffset[0], geometry.Transform.SourceOffset[1]);

        public static Vector2 ToPng(MapGeometry geometry, Vector2 point) =>
            point * geometry.Transform.PngScale +
            new Vector2(geometry.Transform.PngOffset[0], geometry.Transform.PngOffset[1]);

        private static Rect RegionBounds(RegionGeometry region)
        {
            var points = region.Rings[0].Points;
            return Rect.MinMaxRect(points.Min(p => p[0]), points.Min(p => p[1]),
                points.Max(p => p[0]), points.Max(p => p[1]));
        }
    }
}
