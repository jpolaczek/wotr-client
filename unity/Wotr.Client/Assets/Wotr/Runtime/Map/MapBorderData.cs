using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    public readonly struct MapNationBands
    {
        public readonly string Positive;
        public readonly string Negative;
        public MapNationBands(string positive, string negative)
        {
            Positive = positive;
            Negative = negative;
        }
    }

    public sealed class MapBorderData
    {
        public MapBorderCatalog Catalog { get; }
        public MapBorderPalette Palette { get; }
        public IReadOnlyDictionary<string, MapBorderPath> Paths { get; }
        public IReadOnlyDictionary<string, MapBoundaryGeometry> Geometry { get; }

        public MapBorderData(MapGeometry geometry, MapBorderCatalog catalog, MapStyle style, BoardData board, byte[] geometryBytes)
        {
            MapBorderValidation.Validate(geometry, catalog, board, geometryBytes);
            Catalog = catalog;
            Palette = new MapBorderPalette(style);
            Paths = catalog.Paths.ToDictionary(p => p.Id);
            Geometry = geometry.Boundaries.ToDictionary(p => p.Id);
        }

        public static MapBorderData Load(MapGeometry geometry, BoardData board, byte[] geometryBytes)
        {
            var catalog = JsonConvert.DeserializeObject<MapBorderCatalog>(Resources.Load<TextAsset>("map-boundaries").text);
            var style = JsonConvert.DeserializeObject<MapStyle>(Resources.Load<TextAsset>("map-style").text);
            return new MapBorderData(geometry, catalog, style, board, geometryBytes);
        }

        public static bool Passable(MapBorderKind kind) => kind == MapBorderKind.Passage || kind == MapBorderKind.River;

        public static MapNationBands Bands(MapBorderPath path, MapBorderKind kind, IReadOnlyDictionary<string, RegionDefinition> regions)
        {
            if (!Passable(kind))
                return default;
            var positive = Nation(path.PositiveNormalOwner, regions);
            var negative = Nation(path.NegativeNormalOwner, regions);
            return positive == negative ? default : new MapNationBands(positive, negative);
        }

        private static string Nation(string regionId, IReadOnlyDictionary<string, RegionDefinition> regions) =>
            regionId != null && regions.TryGetValue(regionId, out var region) ? region.Nation : null;
    }
}
