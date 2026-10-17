using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wotr.Client
{
    // ART-01: a picture atlas with named cells — a PNG and a JSON of the same name in Resources, in the
    // board-figures schema (normalized bottom-left rects). A missing atlas or cell gives null, so callers keep
    // their fallback picture. The texture, the cells and each sprite are loaded once.
    public sealed class NamedAtlas
    {
        private readonly string path;
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private Texture2D texture;
        private Dictionary<string, Rect> cells;
        private bool probed;

        public NamedAtlas(string path)
        {
            this.path = path;
        }

        public string Path => path;

        public bool Available => Texture() != null && Cells().Count > 0;

        public IReadOnlyCollection<string> Names => Cells().Keys;

        public bool Has(string key) => !string.IsNullOrEmpty(key) && Cells().ContainsKey(key);

        public Rect NormalizedCell(string key) => Has(key) ? Cells()[key] : Rect.zero;

        public Sprite Sprite(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            if (sprites.TryGetValue(key, out var cached))
                return cached;
            var atlas = Texture();
            var sprite = atlas != null && Cells().TryGetValue(key, out var cell) ? Create(atlas, cell, key) : null;
            sprites[key] = sprite;
            return sprite;
        }

        private Sprite Create(Texture2D atlas, Rect cell, string key)
        {
            var pixels = new Rect(Mathf.Round(cell.x * atlas.width), Mathf.Round(cell.y * atlas.height),
                Mathf.Round(cell.width * atlas.width), Mathf.Round(cell.height * atlas.height));
            var sprite = UnityEngine.Sprite.Create(atlas, pixels, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = path.Substring(path.LastIndexOf('/') + 1) + "-" + key;
            return sprite;
        }

        private Texture2D Texture()
        {
            if (!probed)
            {
                probed = true;
                texture = Resources.Load<Texture2D>(path);
            }
            return texture;
        }

        private Dictionary<string, Rect> Cells()
        {
            if (cells != null)
                return cells;
            cells = new Dictionary<string, Rect>();
            var resource = Resources.Load<TextAsset>(path);
            var definition = resource == null ? null : JsonUtility.FromJson<AtlasDefinition>(resource.text);
            foreach (var cell in definition?.cells ?? Array.Empty<AtlasCell>())
                if (Valid(cell))
                    cells[cell.name] = new Rect(cell.x, cell.y, cell.width, cell.height);
            return cells;
        }

        private static bool Valid(AtlasCell cell) => !string.IsNullOrEmpty(cell.name) && cell.width > 0 && cell.height > 0 &&
            cell.x >= 0 && cell.y >= 0 && cell.x + cell.width <= 1.00001f && cell.y + cell.height <= 1.00001f;

        [Serializable]
        private sealed class AtlasDefinition
        {
            public AtlasCell[] cells;
        }

        [Serializable]
        private sealed class AtlasCell
        {
            public string name;
            public float x, y, width, height;
        }
    }
}
