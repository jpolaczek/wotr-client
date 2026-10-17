using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Wotr.Client
{
    // UI-234: fortress drawings (UI-256: every stronghold of the base board) lie on the terrain like its forests and
    // mountains: drawn in the terrain layer in board units, they scale with the map and never hide with the zoom.
    // Names, settlements and forces are drawn above them; their places keep clear of both (MapLandmarksTests).
    public sealed class MapLandmarks
    {
        public const string Resource = "MapCanvas/landmarks";
        public const string Version = "map-landmarks-1";

        [JsonProperty("version")] public string FormatVersion { get; set; }
        [JsonProperty("landmarks")] public List<Landmark> Items { get; set; }

        public sealed class Landmark
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("texture")] public string TexturePath { get; set; }
            [JsonProperty("centre")] public float[] Centre { get; set; }
            [JsonProperty("width")] public float Width { get; set; }
            [JsonIgnore] public Texture2D Texture { get; set; }

            // The drawing keeps its aspect ratio around its centre.
            public Rect Rect
            {
                get
                {
                    var height = Width * Texture.height / Texture.width;
                    return new Rect(Centre[0] - Width / 2, Centre[1] - height / 2, Width, height);
                }
            }
        }

        public static MapLandmarks Load()
        {
            var landmarks = JsonConvert.DeserializeObject<MapLandmarks>(Resources.Load<TextAsset>(Resource).text);
            MapContourValidation.Require(landmarks?.FormatVersion == Version && landmarks.Items != null, "Unsupported map landmarks.");
            foreach (var landmark in landmarks.Items)
            {
                landmark.Texture = Resources.Load<Texture2D>(landmark.TexturePath);
                MapContourValidation.Require(landmark.Texture != null && landmark.Centre?.Length == 2 && landmark.Width > 0,
                    "Invalid map landmark " + landmark.Id + ".");
            }
            return landmarks;
        }
    }
}
