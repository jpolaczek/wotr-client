using System.Collections.Generic;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MapBorderOrientation
    {
        private readonly Dictionary<(Vector2, Vector2), string> directed = new Dictionary<(Vector2, Vector2), string>();

        public MapBorderOrientation(MapGeometry geometry)
        {
            foreach (var region in geometry.Regions)
                foreach (var ring in region.Rings)
                    for (var i = 1; i < ring.Points.Length; i++)
                        directed.Add((Point(ring.Points[i - 1]), Point(ring.Points[i])), region.Id);
        }

        public (string Positive, string Negative) Sides(MapBoundaryGeometry path)
        {
            directed.TryGetValue((Point(path.Points[0]), Point(path.Points[1])), out var positive);
            directed.TryGetValue((Point(path.Points[1]), Point(path.Points[0])), out var negative);
            return (positive, negative);
        }

        // Positive is the algebraic perpendicular in screen coordinates. It is
        // derived from normalized shell/hole winding, not a guessed centroid.
        public static Vector2 PositiveNormal(Vector2 from, Vector2 to)
        {
            var direction = to - from;
            return new Vector2(-direction.y, direction.x).normalized;
        }

        private static Vector2 Point(float[] value) => new Vector2(value[0], value[1]);
    }
}
