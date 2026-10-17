using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MapBorderPalette
    {
        // UI-143: a nation band is a two-tone wash of its nation's hue: a light, translucent interior that lets the
        // map's paper through and a darker fringe of the same hue at the outer edge. Ink stays on the passage line.
        public const float WashTowardLand = .12f;
        public const float WashAlpha = .88f;
        public const float EdgeShade = .58f;
        public const float EdgeAlpha = 1f;
        // The darker edge is this many keylines wide, straddling the band's outline.
        public const float EdgeKeylines = 1.6f;
        // UI-143: an impassable border is a pen line, not a marker stroke: a thin core of warm dark ink over a soft,
        // translucent shadow, both inside the printed barrier width.
        public const float BarrierInkTowardLand = .2f;
        public const float BarrierInkAlpha = .9f;
        public const float BarrierCore = .4f;
        public const float BarrierShadow = .9f;
        public const float BarrierShadowAlpha = .26f;
        public MapStyle Style { get; }
        public Color Land { get; }
        public Color Water { get; }
        public Color Passage { get; }
        public Color Barrier { get; }
        public Color Keyline { get; }
        public Color BarrierInk { get; }
        public Color BarrierShade { get; }
        public float BarrierCoreWidth => Style.BarrierWidth * BarrierCore;
        public float BarrierShadowWidth => Style.BarrierWidth * BarrierShadow;
        public IReadOnlyDictionary<string, Color> Nations { get; }
        public IReadOnlyDictionary<string, Color> NationWashes { get; }
        public IReadOnlyDictionary<string, Color> NationEdges { get; }

        public MapBorderPalette(MapStyle style)
        {
            MapContourValidation.Require(style != null && style.Version == "map-style-1", "Unsupported map palette.");
            MapContourValidation.Require(style.Nations != null && style.Nations.Count == 8 &&
                style.Nations.Select(n => n.Id).ToHashSet().SetEquals(new[]
                    { "dwarves", "elves", "gondor", "north", "rohan", "isengard", "sauron", "southrons" }),
                "Map palette must contain eight nations.");
            MapContourValidation.Require(style.PassageWidth > 0 && style.BarrierWidth > style.PassageWidth &&
                style.NationWidth > 0, "Invalid map stroke widths.");
            Style = style;
            Land = Parse(style.Land);
            Water = Parse(style.Water);
            Passage = Parse(style.Passage);
            Barrier = Parse(style.Barrier);
            Keyline = Parse(style.Keyline);
            BarrierInk = Ink(Barrier, Land, BarrierInkAlpha);
            BarrierShade = Ink(Barrier, Land, BarrierShadowAlpha);
            Nations = style.Nations.ToDictionary(n => n.Id, n => Parse(n.Color));
            NationWashes = Nations.ToDictionary(n => n.Key, n => Wash(n.Value, Land));
            NationEdges = Nations.ToDictionary(n => n.Key, n => Edge(n.Value));
        }

        public static Color Wash(Color nation, Color land)
        {
            var wash = Color.Lerp(nation, land, WashTowardLand);
            wash.a = WashAlpha;
            return wash;
        }

        public static Color Ink(Color barrier, Color land, float alpha)
        {
            var ink = Color.Lerp(barrier, land, BarrierInkTowardLand);
            ink.a = alpha;
            return ink;
        }

        public static Color Edge(Color nation) => new Color(nation.r * EdgeShade, nation.g * EdgeShade, nation.b * EdgeShade, EdgeAlpha);

        private static Color Parse(string value)
        {
            MapContourValidation.Require(value != null && value.Length == 7 && value[0] == '#' &&
                ColorUtility.TryParseHtmlString(value, out _), "Invalid map palette color.");
            ColorUtility.TryParseHtmlString(value, out var color);
            return color;
        }
    }
}
