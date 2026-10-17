using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal static class MapContourTopology
    {
        public static void Validate(MapGeometry geometry, HashSet<string> ids)
        {
            var segments = RingSegments(geometry);
            MapContourValidation.Require(geometry.Boundaries != null && geometry.Boundaries.Count == 323,
                "Expected 323 shared boundary paths.");
            var indices = new HashSet<int>();
            foreach (var boundary in geometry.Boundaries)
            {
                MapContourValidation.Require(boundary != null && boundary.SourceIndex >= 0 && boundary.SourceIndex < 323 &&
                    indices.Add(boundary.SourceIndex) && boundary.Id == $"boundary-{boundary.SourceIndex:0000}",
                    "Missing or repeated boundary provenance.");
                MapContourValidation.Require(boundary.Owners != null && boundary.Owners.Length == 2 &&
                    boundary.Owners[0] != boundary.Owners[1] &&
                    boundary.Owners.All(owner => owner == null || ids.Contains(owner)), "Invalid boundary owners.");
                MapContourValidation.Require(boundary.Points != null && boundary.Points.Length >= 2 &&
                    boundary.Points.All(MapContourValidation.ValidPoint), "Malformed boundary path.");
                foreach (var edge in Edges(boundary.Points))
                {
                    MapContourValidation.Require(segments.TryGetValue(edge, out var owners) &&
                        owners.SetEquals(boundary.Owners.Where(owner => owner != null)),
                        "Boundary differs from region rings: " + boundary.Id);
                    segments.Remove(edge);
                }
            }
            MapContourValidation.Require(segments.Count == 0, "Missing shared boundary segments.");
        }

        private static Dictionary<(Vector2, Vector2), HashSet<string>> RingSegments(MapGeometry geometry)
        {
            var result = new Dictionary<(Vector2, Vector2), HashSet<string>>();
            foreach (var region in geometry.Regions)
                foreach (var ring in region.Rings)
                    foreach (var edge in Edges(ring.Points))
                    {
                        if (!result.TryGetValue(edge, out var owners))
                        {
                            owners = new HashSet<string>();
                            result.Add(edge, owners);
                        }
                        MapContourValidation.Require(owners.Add(region.Id) && owners.Count <= 2, "Overlapping ring edges.");
                    }
            return result;
        }

        private static IEnumerable<(Vector2, Vector2)> Edges(float[][] points)
        {
            for (var i = 1; i < points.Length; i++)
            {
                var a = new Vector2(points[i - 1][0], points[i - 1][1]);
                var b = new Vector2(points[i][0], points[i][1]);
                yield return a.x < b.x || a.x == b.x && a.y < b.y ? (a, b) : (b, a);
            }
        }
    }
}
