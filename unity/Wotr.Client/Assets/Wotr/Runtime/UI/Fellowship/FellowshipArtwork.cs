using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public static class FellowshipArtwork
    {
        public const string ResourcePath = "Wotr/Art/fellowship-symbols-v1";
        public static readonly IReadOnlyList<string> Keys = Array.AsReadOnly(new[]
        {
            "journey", "corruption", "hidden", "revealed", "mordor-gate", "crack-of-doom", "guide", "fellowship"
        });
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private static Dictionary<string, Rect> cells;
        private static Texture2D atlas;

        public static Sprite Sprite(string key)
        {
            if (sprites.TryGetValue(key, out var sprite))
                return sprite;
            atlas ??= Resources.Load<Texture2D>(ResourcePath);
            if (atlas == null)
                return null;
            var cell = NormalizedCell(key);
            sprite = UnityEngine.Sprite.Create(atlas,
                new Rect(Mathf.Round(cell.x * atlas.width), Mathf.Round(cell.y * atlas.height),
                    Mathf.Round(cell.width * atlas.width), Mathf.Round(cell.height * atlas.height)),
                new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = "fellowship-symbol-" + key;
            sprites.Add(key, sprite);
            return sprite;
        }

        public static Rect NormalizedCell(string key)
        {
            cells ??= LoadCells();
            return cells[key];
        }

        private static Dictionary<string, Rect> LoadCells()
        {
            var resource = Resources.Load<TextAsset>(ResourcePath);
            var definition = resource == null ? null : JsonUtility.FromJson<AtlasDefinition>(resource.text);
            if (definition?.cells?.Length != Keys.Count)
                throw new InvalidOperationException("The Fellowship atlas needs eight named sprite bounds.");
            var result = new Dictionary<string, Rect>();
            foreach (var key in Keys)
            {
                var cell = Array.Find(definition.cells, candidate => candidate.name == key);
                if (cell == null || cell.width <= 0 || cell.height <= 0 || cell.x < 0 || cell.y < 0 ||
                    cell.x + cell.width > 1.00001f || cell.y + cell.height > 1.00001f)
                    throw new InvalidOperationException("Invalid Fellowship symbol bounds: " + key);
                result.Add(key, new Rect(cell.x, cell.y, cell.width, cell.height));
            }
            return result;
        }

        public static Image Image(string key)
        {
            var image = new Image { sprite = Sprite(key), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.AddToClassList("fellowship-symbol");
            return image;
        }

        [Serializable]
        private sealed class AtlasDefinition { public AtlasCell[] cells; }

        [Serializable]
        private sealed class AtlasCell
        {
            public string name;
            public float x, y, width, height;
        }
    }
}
