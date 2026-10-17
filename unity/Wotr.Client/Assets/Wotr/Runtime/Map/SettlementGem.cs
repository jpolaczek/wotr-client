using System.Linq;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-195: a settlement on the map is a cut gemstone in the colour of the side that controls it, sapphire for the
    // Free Peoples and ruby for the Shadow. The cut tells its kind: a round brilliant for a Town, an emerald cut for a
    // City, a cushion in a gold setting for a Stronghold and a trillion for a Fortification (the ruins of Osgiliath,
    // the Fords of Isen). Shapes are unit polygons with y pointing down, scaled to points on the screen.
    public sealed class SettlementGem
    {
        public static readonly GemPalette Sapphire = new GemPalette(new Color(.62f, .80f, 1f), new Color(.17f, .42f, .86f), new Color(.04f, .13f, .40f));
        public static readonly GemPalette Ruby = new GemPalette(new Color(1f, .58f, .58f), new Color(.80f, .10f, .17f), new Color(.33f, .02f, .06f));
        public static readonly GemPalette Quartz = new GemPalette(new Color(.97f, .95f, .90f), new Color(.78f, .75f, .68f), new Color(.42f, .40f, .35f));
        public static readonly GemPalette Gold = new GemPalette(new Color(1f, .88f, .52f), new Color(.80f, .60f, .24f), new Color(.42f, .28f, .08f));
        public static readonly Color Edge = new Color(.07f, .06f, .05f, .9f);
        public const float SettingScale = 1.3f;
        // Light falls from the upper left, as on the rest of the board's art.
        private static readonly Vector2 Light = new Vector2(-.6f, -.8f);

        private static readonly Vector2[] Brilliant = Enumerable.Range(0, 8)
            .Select(i => new Vector2(Mathf.Cos((22.5f + 45 * i) * Mathf.Deg2Rad), Mathf.Sin((22.5f + 45 * i) * Mathf.Deg2Rad)))
            .ToArray();
        private static readonly Vector2[] Emerald =
        {
            new Vector2(-1, -.42f), new Vector2(-.72f, -.7f), new Vector2(.72f, -.7f), new Vector2(1, -.42f),
            new Vector2(1, .42f), new Vector2(.72f, .7f), new Vector2(-.72f, .7f), new Vector2(-1, .42f)
        };
        private static readonly Vector2[] Cushion =
        {
            new Vector2(-1, -.72f), new Vector2(-.72f, -1), new Vector2(.72f, -1), new Vector2(1, -.72f),
            new Vector2(1, .72f), new Vector2(.72f, 1), new Vector2(-.72f, 1), new Vector2(-1, .72f)
        };
        private static readonly Vector2[] Trillion =
        {
            new Vector2(-.14f, -.98f), new Vector2(.14f, -.98f), new Vector2(.95f, .42f),
            new Vector2(.8f, .66f), new Vector2(-.8f, .66f), new Vector2(-.95f, .42f)
        };

        public Vector2[] Girdle { get; }
        public float Size { get; }
        public float TableScale { get; }
        public bool Setting { get; }
        public GemPalette Colours { get; }

        private SettlementGem(Vector2[] girdle, float size, float table, bool setting, GemPalette colours)
        {
            Girdle = girdle;
            Size = size;
            TableScale = table;
            Setting = setting;
            Colours = colours;
        }

        public static SettlementGem For(string kind, Side? controller)
        {
            var colours = Palette(controller);
            return kind switch
            {
                "Town" => new SettlementGem(Brilliant, 6f, .52f, false, colours),
                "Stronghold" => new SettlementGem(Cushion, 7.5f, .52f, true, colours),
                "Fortification" => new SettlementGem(Trillion, 8f, .45f, false, colours),
                _ => new SettlementGem(Emerald, 7.5f, .52f, false, colours)
            };
        }

        public static GemPalette Palette(Side? controller) => controller switch
        {
            Side.FreePeoples => Sapphire,
            Side.Shadow => Ruby,
            _ => Quartz
        };

        // Half of the drawn height, the gold setting included.
        public float Extent => Size * (Setting ? SettingScale : 1) * Girdle.Max(point => Mathf.Abs(point.y));

        public Vector2[] Outline(Vector2 at) => Scaled(at, Size);

        public Vector2[] SettingOutline(Vector2 at) => Scaled(at, Size * SettingScale);

        public Vector2[] Table(Vector2 at) => Scaled(at, Size * TableScale);

        // The facet between girdle points i and i + 1: lighter the more it faces the light.
        public Color Facet(int edge, GemPalette palette)
        {
            var middle = (Girdle[edge] + Girdle[(edge + 1) % Girdle.Length]) / 2;
            var facing = (Vector2.Dot(middle.normalized, Light.normalized) + 1) / 2;
            return Color.Lerp(palette.Dark, palette.Light, facing);
        }

        public Color TableColour => Color.Lerp(Colours.Mid, Colours.Light, .35f);

        private Vector2[] Scaled(Vector2 at, float scale) => Girdle.Select(point => at + point * scale).ToArray();
    }

    public readonly struct GemPalette
    {
        public Color Light { get; }
        public Color Mid { get; }
        public Color Dark { get; }

        public GemPalette(Color light, Color mid, Color dark)
        {
            Light = light;
            Mid = mid;
            Dark = dark;
        }
    }
}
