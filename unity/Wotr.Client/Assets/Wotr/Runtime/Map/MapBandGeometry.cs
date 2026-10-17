using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MapBandPart
    {
        public int FromSegment { get; set; }
        public int ToSegment { get; set; }
        public List<MapContourRing> Positive { get; set; } = new List<MapContourRing>();
        public List<MapContourRing> Negative { get; set; } = new List<MapContourRing>();
        public List<MapContourRing> PositiveOutline { get; set; } = new List<MapContourRing>();
        public List<MapContourRing> NegativeOutline { get; set; } = new List<MapContourRing>();
    }

    public sealed class MapBandPath
    {
        public string Id { get; set; }
        public List<MapBandPart> Parts { get; set; } = new List<MapBandPart>();
    }

    public sealed class MapBandGeometry
    {
        public string Version { get; set; }
        public Dictionary<string, string> SourceHashes { get; set; } = new Dictionary<string, string>();
        public float KeylineWidth { get; set; }
        public List<MapContourRing> Water { get; set; } = new List<MapContourRing>();
        public List<MapBandPath> Paths { get; set; } = new List<MapBandPath>();
        private static MapBandGeometry shared;

        public static MapBandGeometry Load(MapBorderData borders)
        {
            if (shared != null)
                return shared;
            var data = JsonConvert.DeserializeObject<MapBandGeometry>(Resources.Load<TextAsset>("map-border-bands").text);
            Validate(data, borders);
            shared = data;
            return data;
        }

        public static void Validate(MapBandGeometry data, MapBorderData borders)
        {
            MapContourValidation.Require(data != null && data.Version == "border-bands-1" && data.KeylineWidth > 0,
                "Unsupported map render bands.");
            foreach (var name in new[] { "map-geometry", "map-boundaries", "map-style" })
            {
                using var sha = SHA256.Create();
                var hash = BitConverter.ToString(sha.ComputeHash(Resources.Load<TextAsset>(name).bytes)).Replace("-", "").ToLowerInvariant();
                MapContourValidation.Require(data.SourceHashes.TryGetValue(name + ".json", out var expected) && hash == expected,
                    "Stale map render bands: " + name);
            }
            MapContourValidation.Require(data.Paths.Count == borders.Paths.Count &&
                data.Paths.Select(p => p.Id).ToHashSet().SetEquals(borders.Paths.Keys), "Incomplete map render paths.");
            foreach (var path in data.Paths)
            {
                var source = borders.Paths[path.Id];
                MapContourValidation.Require(path.Parts.Count == source.Parts.Count, "Incomplete map render parts.");
                for (var index = 0; index < path.Parts.Count; index++)
                {
                    var part = path.Parts[index];
                    MapContourValidation.Require(part.FromSegment == source.Parts[index].FromSegment &&
                        part.ToSegment == source.Parts[index].ToSegment, "Map render bands changed a passage.");
                }
            }
        }
    }
}
