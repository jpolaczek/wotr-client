using Newtonsoft.Json;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-102: paper and sea beyond the map's edge (Resources/MapCanvas/map-canvas.json, scripts/export_map_canvas.py).
    // The margins are only artwork: hit testing still follows the region contours inside the map.
    public sealed class MapCanvasLayout
    {
        public const string Resource = "MapCanvas/map-canvas";
        public const string Version = "map-canvas-1";

        [JsonProperty("version")] public string FormatVersion { get; set; }
        [JsonProperty("map")] public float[] Map { get; set; }
        [JsonProperty("margins")] public CanvasMargins Margins { get; set; }
        [JsonProperty("margins_texture")] public string MarginsTexture { get; set; }
        [JsonProperty("terrain_texture")] public string TerrainTexture { get; set; }

        public sealed class CanvasMargins
        {
            [JsonProperty("west")] public float West { get; set; }
            [JsonProperty("north")] public float North { get; set; }
            [JsonProperty("east")] public float East { get; set; }
            [JsonProperty("south")] public float South { get; set; }
        }

        public static MapCanvasLayout Load(MapGeometry geometry)
        {
            var layout = JsonConvert.DeserializeObject<MapCanvasLayout>(Resources.Load<TextAsset>(Resource).text);
            MapContourValidation.Require(layout?.FormatVersion == Version && layout.Margins != null, "Unsupported map canvas.");
            MapContourValidation.Require(layout.Map != null && layout.Map.Length == 2 &&
                Mathf.Approximately(layout.Map[0], geometry.Width) && Mathf.Approximately(layout.Map[1], geometry.Height),
                "The map canvas was exported for another map size.");
            MapContourValidation.Require(layout.Margins.West >= 0 && layout.Margins.North >= 0 &&
                layout.Margins.East >= 0 && layout.Margins.South >= 0, "Invalid map canvas margins.");
            return layout;
        }

        public Rect Around(Rect map) => Rect.MinMaxRect(map.xMin - Margins.West, map.yMin - Margins.North,
            map.xMax + Margins.East, map.yMax + Margins.South);
    }
}
