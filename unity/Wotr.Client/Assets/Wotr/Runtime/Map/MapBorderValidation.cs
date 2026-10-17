using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class MapBorderValidation
    {
        public static void Validate(MapGeometry geometry, MapBorderCatalog catalog, BoardData board, byte[] geometryBytes)
        {
            MapContourValidation.Require(catalog != null && catalog.Version == "borders-1" &&
                catalog.GeometryVersion == geometry.Version && catalog.GeometryHash == Hash(geometryBytes),
                "Map border geometry/version mismatch.");
            MapContourValidation.Require(catalog.AuditHash != null && catalog.AuditHash.Length == 64 && catalog.AuditHash.All(Uri.IsHexDigit),
                "Missing border audit provenance.");
            MapContourValidation.Require(catalog.Paths != null && catalog.Paths.Count == geometry.Boundaries.Count,
                "Missing classified border paths.");
            var paths = geometry.Boundaries.ToDictionary(p => p.Id);
            var orientation = new MapBorderOrientation(geometry);
            var ids = new HashSet<string>();
            var expectedPairs = board.Regions.SelectMany(r => r.Neighbors.Select(n => Pair(r.Id, n))).ToHashSet();
            var passagePairs = new HashSet<string>();
            foreach (var path in catalog.Paths)
            {
                MapContourValidation.Require(path != null && paths.ContainsKey(path.Id) && ids.Add(path.Id),
                    "Unknown or repeated classified border.");
                ValidatePath(path, paths[path.Id], orientation, expectedPairs, passagePairs);
            }
            MapContourValidation.Require(passagePairs.SetEquals(expectedPairs), "Missing printed movement passage.");
        }

        private static void ValidatePath(MapBorderPath path, MapBoundaryGeometry geometry, MapBorderOrientation orientation,
            HashSet<string> expectedPairs, HashSet<string> passagePairs)
        {
            var sides = orientation.Sides(geometry);
            MapContourValidation.Require(path.PositiveNormalOwner == sides.Positive && path.NegativeNormalOwner == sides.Negative,
                "Border normal ownership mismatch: " + path.Id);
            MapContourValidation.Require(path.SourceReference == "MAP-13-BORDER-AUDIT/" + path.Id &&
                path.Parts != null && path.Parts.Count > 0, "Missing border classification source: " + path.Id);
            var land = sides.Positive != null && sides.Negative != null;
            var pair = land ? Pair(sides.Positive, sides.Negative) : "";
            var end = 0;
            foreach (var part in path.Parts)
            {
                MapContourValidation.Require(part != null && Enum.IsDefined(typeof(MapBorderKind), part.Kind) &&
                    part.FromSegment == end && part.ToSegment > end && part.ToSegment < geometry.Points.Length,
                    "Gap, overlap, or invalid border range: " + path.Id);
                var water = part.Kind == MapBorderKind.Coast || part.Kind == MapBorderKind.Lake || part.Kind == MapBorderKind.Frame;
                MapContourValidation.Require(water != land, "Border land/water mismatch: " + path.Id);
                if (MapBorderData.Passable(part.Kind))
                {
                    MapContourValidation.Require(expectedPairs.Contains(pair), "Border contradicts the movement catalog: " + path.Id);
                    passagePairs.Add(pair);
                }
                end = part.ToSegment;
            }
            MapContourValidation.Require(end == geometry.Points.Length - 1, "Unclassified border end: " + path.Id);
        }

        private static string Pair(string a, string b) => string.CompareOrdinal(a, b) < 0 ? a + ":" + b : b + ":" + a;

        private static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
