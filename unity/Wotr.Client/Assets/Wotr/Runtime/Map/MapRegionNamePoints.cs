using System;
using System.Collections.Generic;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-186: where a region's name stands, in board units: the room to each side and above or below it bounds how
    // large the name may be drawn.
    public readonly struct MapNamePoint
    {
        public readonly Vector2 Point;
        public readonly float HalfWidth;
        public readonly float HalfHeight;

        public MapNamePoint(Vector2 point, float halfWidth, float halfHeight)
        {
            Point = point;
            HalfWidth = halfWidth;
            HalfHeight = halfHeight;
        }
    }

    // UI-186: a name stands inside its region, as far from the borders as it can, and clear of the settlement and of
    // the forces that stand above it (MapCanvas.RowCentre) or on the anchor of a region without one. Below the
    // settlement is preferred, as on the printed board. The search runs once per region on the static geometry; the
    // distances use the traced borders simplified to within one board unit (a quarter of the points): about 0.15 s for
    // the whole board instead of 0.6 s, measured in MapRegionNamePointsTests.
    public static class MapRegionNamePoints
    {
        private const int Grid = 14;
        private const float Simplification = 1;
        private const int Refinement = 6;
        private const float MinimumClearance = 5;
        private const float ClearanceCap = 26;
        private const float BelowBonus = 6;
        private const float ZonePenalty = 1000;
        // The forces' row at figure size, in board units at the zoom where figures appear.
        private const float ZoneHalfWidth = 42;
        private const float ZoneAbove = 64;
        private const float ZoneBelow = 12;
        private const float CentredZoneHalfHeight = 32;

        // clearOfForces: false finds the region's roomiest point wherever its forces stand (UI-363).
        public static MapNamePoint For(RegionGeometry region, bool settled, bool clearOfForces = true)
        {
            var anchor = new Vector2(region.Anchor[0], region.Anchor[1]);
            var borders = Borders(region);
            var bounds = Bounds(region);
            var step = Mathf.Max(2, Mathf.Min(bounds.width, bounds.height) / Grid);
            var best = Search(region, borders, anchor, settled, clearOfForces, bounds, step, out var score);
            var fine = new Rect(best - Vector2.one * step, Vector2.one * step * 2);
            best = Search(region, borders, anchor, settled, clearOfForces, fine, step * 2 / Refinement, out _, score, best);
            return new MapNamePoint(best, Chord(borders, best, Vector2.right), Chord(borders, best, Vector2.up));
        }

        private static Vector2 Search(RegionGeometry region, List<Vector2[]> borders, Vector2 anchor, bool settled, bool clearOfForces,
            Rect area, float step, out float bestScore, float startScore = float.MinValue, Vector2 start = default)
        {
            var best = start;
            bestScore = startScore;
            for (var y = area.yMin + step / 2; y < area.yMax; y += step)
                for (var x = area.xMin + step / 2; x < area.xMax; x += step)
                {
                    var point = new Vector2(x, y);
                    if (!MapContourMath.Contains(region, point))
                        continue;
                    var score = Score(borders, anchor, settled, clearOfForces, point);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = point;
                    }
                }
            if (bestScore == float.MinValue)
                best = anchor;
            return best;
        }

        private static float Score(List<Vector2[]> borders, Vector2 anchor, bool settled, bool clearOfForces, Vector2 point)
        {
            var clearance = Clearance(borders, point);
            if (clearance < MinimumClearance)
                return clearance - ZonePenalty * 2;
            var score = Mathf.Min(clearance, ClearanceCap);
            if (clearOfForces && InForcesZone(anchor, settled, point))
                score -= ZonePenalty;
            if (point.y > anchor.y + ZoneBelow)
                score += BelowBonus;
            return score;
        }

        public static bool InForcesZone(Vector2 anchor, bool settled, Vector2 point)
        {
            if (Mathf.Abs(point.x - anchor.x) > ZoneHalfWidth)
                return false;
            return settled
                ? point.y >= anchor.y - ZoneAbove && point.y <= anchor.y + ZoneBelow
                : Mathf.Abs(point.y - anchor.y) <= CentredZoneHalfHeight;
        }

        // The distance to the nearest border of the region, holes included.
        public static float Clearance(RegionGeometry region, Vector2 point) => Clearance(Borders(region, 0), point);

        private static float Clearance(List<Vector2[]> borders, Vector2 point)
        {
            var nearest = float.MaxValue;
            foreach (var ring in borders)
                for (var i = 1; i < ring.Length; i++)
                    nearest = Mathf.Min(nearest, SegmentDistance(point, ring[i - 1], ring[i]));
            return nearest;
        }

        // The free distance from the point along a direction and its opposite, whichever is shorter.
        private static float Chord(List<Vector2[]> borders, Vector2 point, Vector2 direction)
        {
            var forward = float.MaxValue;
            var backward = float.MaxValue;
            foreach (var ring in borders)
                for (var i = 1; i < ring.Length; i++)
                {
                    if (!Crossing(point, direction, ring[i - 1], ring[i], out var distance))
                        continue;
                    if (distance >= 0)
                        forward = Mathf.Min(forward, distance);
                    else
                        backward = Mathf.Min(backward, -distance);
                }
            return Mathf.Min(forward, backward);
        }

        // Where the line through the point along the direction crosses the segment, as a signed distance.
        private static bool Crossing(Vector2 point, Vector2 direction, Vector2 a, Vector2 b, out float distance)
        {
            distance = 0;
            var horizontal = direction.x != 0;
            var across = horizontal ? point.y : point.x;
            var a0 = horizontal ? a.y : a.x;
            var b0 = horizontal ? b.y : b.x;
            if ((a0 > across) == (b0 > across))
                return false;
            var t = (across - a0) / (b0 - a0);
            var a1 = horizontal ? a.x : a.y;
            var b1 = horizontal ? b.x : b.y;
            distance = a1 + t * (b1 - a1) - (horizontal ? point.x : point.y);
            return true;
        }

        private static float SegmentDistance(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var length = segment.sqrMagnitude;
            var t = length > 0 ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / length) : 0;
            return Vector2.Distance(point, start + segment * t);
        }

        // Every ring of the region as a closed polyline, simplified to within the tolerance (0 keeps every point).
        private static List<Vector2[]> Borders(RegionGeometry region, float tolerance = Simplification)
        {
            var borders = new List<Vector2[]>(region.Rings.Count);
            foreach (var ring in region.Rings)
            {
                var points = new Vector2[ring.Points.Length];
                for (var i = 0; i < points.Length; i++)
                    points[i] = new Vector2(ring.Points[i][0], ring.Points[i][1]);
                borders.Add(tolerance > 0 ? Simplify(points, tolerance) : points);
            }
            return borders;
        }

        // Douglas–Peucker without recursion: a point stays when it lies farther than the tolerance from the chord of
        // its span. A closed ring keeps its first and middle points, so no span starts and ends at one point.
        private static Vector2[] Simplify(Vector2[] points, float tolerance)
        {
            if (points.Length < 4)
                return points;
            var keep = new bool[points.Length];
            var middle = points.Length / 2;
            keep[0] = keep[middle] = keep[points.Length - 1] = true;
            var spans = new Stack<(int, int)>();
            spans.Push((0, middle));
            spans.Push((middle, points.Length - 1));
            while (spans.Count > 0)
            {
                var (first, last) = spans.Pop();
                var farthest = -1;
                var distance = tolerance;
                for (var i = first + 1; i < last; i++)
                {
                    var d = SegmentDistance(points[i], points[first], points[last]);
                    if (d > distance)
                    {
                        distance = d;
                        farthest = i;
                    }
                }
                if (farthest < 0)
                    continue;
                keep[farthest] = true;
                spans.Push((first, farthest));
                spans.Push((farthest, last));
            }
            var result = new List<Vector2>();
            for (var i = 0; i < points.Length; i++)
                if (keep[i])
                    result.Add(points[i]);
            return result.ToArray();
        }

        private static Rect Bounds(RegionGeometry region)
        {
            var points = region.Rings[0].Points;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                minX = Math.Min(minX, p[0]);
                minY = Math.Min(minY, p[1]);
                maxX = Math.Max(maxX, p[0]);
                maxY = Math.Max(maxY, p[1]);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}
