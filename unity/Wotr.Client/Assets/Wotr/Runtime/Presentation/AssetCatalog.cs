using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Resource paths can acquire licensed artwork without changing rule IDs or interaction geometry.
    public sealed class AssetCatalog
    {
        public Dictionary<string, string> Symbols = new Dictionary<string, string>();
        public Dictionary<string, string> Sprites = new Dictionary<string, string>();
        private static AssetCatalog current;
        public static AssetCatalog Current => current ?? (current = JsonConvert.DeserializeObject<AssetCatalog>(
            Resources.Load<TextAsset>("asset-manifest").text));
        public string Symbol(string key) => Symbols.TryGetValue(key, out var value) ? value : "?";
        public string Die(DieFace? face) => face.HasValue ? Symbol("die." + face) : "·";
        public Sprite Sprite(string id) => Sprites.TryGetValue(id, out var path) ? Resources.Load<Sprite>(path) : null;
    }
}
