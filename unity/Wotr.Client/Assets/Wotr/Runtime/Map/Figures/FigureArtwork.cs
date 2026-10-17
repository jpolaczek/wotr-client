using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class FigureArtwork
    {
        public const string ResourcePath = "Wotr/Art/board-figures-v2";
        // ART-01/ART-02: figures of each nation's units and of each character; UI-76 silhouettes stay the fallback.
        public static readonly NamedAtlas Units = new NamedAtlas("Wotr/Art/units-v3");
        public static readonly NamedAtlas Characters = new NamedAtlas("Wotr/Art/characters-v1");
        private static Texture2D atlas;
        private static readonly Sprite[] sprites = new Sprite[12];
        private static Rect[] cells;
        private static readonly string[] names =
        {
            "fp-regular", "fp-elite", "fp-leader", "companion",
            "sh-regular", "sh-elite", "sh-leader", "nazgul",
            "wizard", "ruler", "minion", "fellowship"
        };

        public static Sprite Sprite(FigureAppearance appearance)
        {
            var index = (int)appearance;
            if (sprites[index] != null)
                return sprites[index];
            atlas ??= Resources.Load<Texture2D>(ResourcePath);
            if (atlas == null)
                return null;
            var cell = NormalizedCell(appearance);
            sprites[index] = UnityEngine.Sprite.Create(atlas,
                new Rect(Mathf.Round(cell.x * atlas.width), Mathf.Round(cell.y * atlas.height),
                    Mathf.Round(cell.width * atlas.width), Mathf.Round(cell.height * atlas.height)),
                new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            sprites[index].name = "board-figure-" + appearance;
            return sprites[index];
        }

        public static Sprite Sprite(FigureArt art) => Named(art.Key) ?? Sprite(art.Fallback);

        private static Sprite Named(string key) => key.Length == 0 ? null : Units.Sprite(key) ?? Characters.Sprite(key);

        public static Rect NormalizedCell(FigureAppearance appearance)
        {
            cells ??= LoadCells();
            return cells[(int)appearance];
        }

        private static Rect[] LoadCells()
        {
            var resource = Resources.Load<TextAsset>(ResourcePath);
            if (resource == null)
                throw new InvalidOperationException("The board figure atlas needs its named sprite bounds.");
            var definition = JsonUtility.FromJson<AtlasDefinition>(resource.text);
            if (definition?.cells?.Length != names.Length)
                throw new InvalidOperationException("The board figure atlas must contain twelve named sprites.");
            var result = new Rect[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                var cell = Array.Find(definition.cells, candidate => candidate.name == names[i]);
                if (cell == null || cell.width <= 0 || cell.height <= 0 || cell.x < 0 || cell.y < 0 ||
                    cell.x + cell.width > 1.00001f || cell.y + cell.height > 1.00001f)
                    throw new InvalidOperationException("Invalid board figure bounds: " + names[i]);
                result[i] = new Rect(cell.x, cell.y, cell.width, cell.height);
            }
            return result;
        }

        public static Image Image(FigureArt art)
        {
            var image = new Image { sprite = Sprite(art), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.AddToClassList("figure-art-image");
            return image;
        }

        public static void Apply(Button button, UnitView unit) => Apply(button, FigureAppearances.Art(unit), unit.Side);
        public static void Apply(Button button, CharacterView character) => Apply(button, FigureAppearances.Art(character), character.Side);

        // UI-201: a figure in a panel stands in a soft gilt glow (IconGlow), so it does not sink into the dark plate;
        // the tile itself is no more than its state needs (Styles: .illustrated-figure).
        public static void Apply(Button button, FigureArt art, Side side, bool glow = true)
        {
            button.AddToClassList("illustrated-figure");
            button.EnableInClassList("figure-free-peoples", side == Side.FreePeoples);
            button.EnableInClassList("figure-shadow", side == Side.Shadow);
            var image = button.Q<Image>("figure-art");
            if (image == null)
            {
                image = Image(art);
                image.name = "figure-art";
                button.Insert(0, image);
                if (glow)
                    button.Insert(0, IconGlow.Create("figure-glow"));
            }
            else
                image.sprite = Sprite(art);
            // The existing text remains a small independent type/identity badge.
        }

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
