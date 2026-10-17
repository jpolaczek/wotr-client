using System.Collections.Generic;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-218: diagonal stripes inside a region. Painter2D cannot clip, so each stripe (a line x + y = c) is cut into
    // the pieces that lie inside the region's rings, with the even-odd rule, so holes stay unstriped.
    public static class MapHatching
    {
        private const float MinimumLength = .01f;

        public static List<(Vector2 From, Vector2 To)> Segments(IReadOnlyList<MapContourRing> rings, float spacing)
        {
            var segments = new List<(Vector2, Vector2)>();
            if (rings.Count == 0 || spacing <= 0)
                return segments;
            var (low, high) = Range(rings);
            var crossings = new List<Vector2>();
            for (var line = Mathf.Ceil(low / spacing) * spacing; line <= high; line += spacing)
            {
                crossings.Clear();
                foreach (var ring in rings)
                    AddCrossings(ring.Points, line, crossings);
                crossings.Sort((a, b) => a.x.CompareTo(b.x));
                for (var index = 0; index + 1 < crossings.Count; index += 2)
                    // A line through a corner only touches the region.
                    if ((crossings[index + 1] - crossings[index]).sqrMagnitude > MinimumLength * MinimumLength)
                        segments.Add((crossings[index], crossings[index + 1]));
            }
            return segments;
        }

        private static (float Low, float High) Range(IReadOnlyList<MapContourRing> rings)
        {
            var low = float.MaxValue;
            var high = float.MinValue;
            foreach (var ring in rings)
                foreach (var point in ring.Points)
                {
                    low = Mathf.Min(low, point[0] + point[1]);
                    high = Mathf.Max(high, point[0] + point[1]);
                }
            return (low, high);
        }

        // A half-open rule (one end inclusive) counts a vertex lying on the line once.
        private static void AddCrossings(float[][] points, float line, List<Vector2> crossings)
        {
            for (var index = 0; index < points.Length; index++)
            {
                var from = points[index];
                var to = points[(index + 1) % points.Length];
                var start = from[0] + from[1];
                var end = to[0] + to[1];
                if ((start <= line) == (end <= line))
                    continue;
                var t = (line - start) / (end - start);
                crossings.Add(new Vector2(from[0] + t * (to[0] - from[0]), from[1] + t * (to[1] - from[1])));
            }
        }
    }
}
