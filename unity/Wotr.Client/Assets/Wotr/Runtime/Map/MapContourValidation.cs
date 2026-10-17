using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class MapContourValidation
    {
        public static void Validate(MapGeometry geometry, IEnumerable<string> regionIds)
        {
            Require(geometry != null && geometry.Version == MapContourData.Version, "Unsupported contour version.");
            ValidateTransform(geometry);
            var expected = new HashSet<string>(regionIds);
            Require(geometry.Regions != null && geometry.Regions.Count == 105 && expected.Count == 105,
                "Expected 105 contour regions.");
            var ids = new HashSet<string>();
            var numbers = new HashSet<int>();
            foreach (var region in geometry.Regions)
            {
                Require(region != null && expected.Contains(region.Id) && ids.Add(region.Id), "Foreign or duplicate contour ID.");
                Require(region.MaskNumber > 0 && numbers.Add(region.MaskNumber), "Duplicate or invalid mask number.");
                ValidateRegion(region, geometry);
            }
            MapContourTopology.Validate(geometry, ids);
        }

        private static void ValidateTransform(MapGeometry geometry)
        {
            var transform = geometry.Transform;
            Require(transform != null && geometry.Width == 1632 && geometry.Height == 1338,
                "Unexpected contour dimensions.");
            Require(transform.SourceViewBox != null && transform.SourceViewBox.SequenceEqual(new float[] { 335, 40, 1632, 1338 }) &&
                transform.SourceOffset != null && transform.SourceOffset.SequenceEqual(new float[] { -335, -40 }) &&
                transform.SourceScale == 1, "Unexpected source coordinate transform.");
            Require(transform.PngSize != null && transform.PngSize.SequenceEqual(new[] { 6144, 5037 }) &&
                ValidPoint(transform.PngOffset), "Unexpected contour PNG dimensions.");
            var scale = Math.Min(6144f / geometry.Width, 5037f / geometry.Height);
            Require(Math.Abs(transform.PngScale - scale) < .00001f &&
                Math.Abs(transform.PngOffset[0] - (6144 - geometry.Width * scale) / 2) < .001f &&
                Math.Abs(transform.PngOffset[1] - (5037 - geometry.Height * scale) / 2) < .001f,
                "Unexpected contour PNG transform.");
            Require(geometry.SourceHashes != null && geometry.SourceHashes.Count == 6 &&
                geometry.SourceHashes.Values.All(hash => hash != null && hash.Length == 64 && hash.All(Uri.IsHexDigit)),
                "Missing contour source provenance.");
        }

        private static void ValidateRegion(RegionGeometry region, MapGeometry geometry)
        {
            Require(region.Rings != null && region.Rings.Count > 0, "Missing rings: " + region.Id);
            for (var index = 0; index < region.Rings.Count; index++)
            {
                var ring = region.Rings[index];
                Require(ring != null && ring.Hole == (index > 0), "Invalid shell/hole order: " + region.Id);
                ValidateRing(ring, geometry, region.Id);
                if (index > 0)
                    Require(ring.Points.All(p => MapContourMath.Location(region.Rings[0].Points, new Vector2(p[0], p[1])) >= 0),
                        "Hole outside shell: " + region.Id);
            }
            Require(ValidPoint(region.Anchor) &&
                MapContourMath.Location(region.Rings[0].Points, new Vector2(region.Anchor[0], region.Anchor[1])) == 1 &&
                MapContourMath.Contains(region, new Vector2(region.Anchor[0], region.Anchor[1])),
                "Anchor outside land: " + region.Id);
        }

        private static void ValidateRing(MapContourRing ring, MapGeometry geometry, string id)
        {
            var points = ring.Points;
            Require(points != null && points.Length >= 4 && points.All(ValidPoint), "Malformed ring: " + id);
            Require(points[0].SequenceEqual(points[points.Length - 1]), "Unclosed ring: " + id);
            var unique = new HashSet<Vector2>();
            for (var i = 0; i < points.Length - 1; i++)
            {
                var p = points[i];
                Require(p[0] >= 0 && p[0] <= geometry.Width && p[1] >= 0 && p[1] <= geometry.Height,
                    "Ring outside map: " + id);
                Require(unique.Add(new Vector2(p[0], p[1])), "Repeated ring vertex: " + id);
            }
            var area = MapContourMath.SignedArea(points);
            Require(ring.Hole ? area < 0 : area > 0, "Wrong winding or zero-area ring: " + id);
            ValidateSelfIntersections(points, id);
        }

        private static void ValidateSelfIntersections(float[][] points, string id)
        {
            var sorted = Enumerable.Range(0, points.Length - 1)
                .OrderBy(i => Math.Min(points[i][0], points[i + 1][0])).ToArray();
            for (var i = 0; i < sorted.Length; i++)
            {
                var a = sorted[i];
                var maxX = Math.Max(points[a][0], points[a + 1][0]);
                for (var j = i + 1; j < sorted.Length; j++)
                {
                    var b = sorted[j];
                    if (Math.Min(points[b][0], points[b + 1][0]) > maxX)
                        break;
                    if (Math.Abs(a - b) == 1 || Math.Abs(a - b) == sorted.Length - 1)
                        continue;
                    Require(!MapContourMath.Crosses(points[a], points[a + 1], points[b], points[b + 1]),
                        "Self-intersecting ring: " + id);
                }
            }
        }

        internal static bool ValidPoint(float[] point) => point != null && point.Length == 2 &&
            point.All(value => !float.IsNaN(value) && !float.IsInfinity(value));

        internal static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidDataException(message);
        }
    }
}
