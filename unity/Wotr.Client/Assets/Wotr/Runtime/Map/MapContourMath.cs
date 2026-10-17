using System;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class MapContourMath
    {
        public static bool Contains(RegionGeometry region, Vector2 point)
        {
            if (Location(region.Rings[0].Points, point) < 0)
                return false;
            for (var i = 1; i < region.Rings.Count; i++)
                if (Location(region.Rings[i].Points, point) >= 0)
                    return false;
            return true;
        }

        // A point on a land boundary belongs to both incident land polygons.
        // MapContourData resolves that tie by ordinal region ID, independent of
        // serialization order. Hole boundaries never hit their containing land.
        public static int Location(float[][] points, Vector2 point)
        {
            var inside = false;
            for (var i = 1; i < points.Length; i++)
            {
                var a = points[i - 1];
                var b = points[i];
                if (Cross(a, b, point.x, point.y) == 0 && Between(a, b, point.x, point.y))
                    return 0;
                if ((a[1] > point.y) != (b[1] > point.y) &&
                    point.x < (double)(b[0] - a[0]) * (point.y - a[1]) / (b[1] - a[1]) + a[0])
                    inside = !inside;
            }
            return inside ? 1 : -1;
        }

        public static double SignedArea(float[][] points)
        {
            double area = 0;
            for (var i = 1; i < points.Length; i++)
                area += (double)points[i - 1][0] * points[i][1] - (double)points[i][0] * points[i - 1][1];
            return area / 2;
        }

        public static bool Crosses(float[] a, float[] b, float[] c, float[] d)
        {
            var first = Cross(a, b, c[0], c[1]);
            var second = Cross(a, b, d[0], d[1]);
            var third = Cross(c, d, a[0], a[1]);
            var fourth = Cross(c, d, b[0], b[1]);
            if (first * second < 0 && third * fourth < 0)
                return true;
            return first == 0 && Between(a, b, c[0], c[1]) ||
                second == 0 && Between(a, b, d[0], d[1]) ||
                third == 0 && Between(c, d, a[0], a[1]) ||
                fourth == 0 && Between(c, d, b[0], b[1]);
        }

        private static double Cross(float[] a, float[] b, float x, float y) =>
            (double)(b[0] - a[0]) * (y - a[1]) - (double)(b[1] - a[1]) * (x - a[0]);

        private static bool Between(float[] a, float[] b, float x, float y) =>
            x >= Math.Min(a[0], b[0]) && x <= Math.Max(a[0], b[0]) &&
            y >= Math.Min(a[1], b[1]) && y <= Math.Max(a[1], b[1]);
    }
}
