using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-195: each settlement is drawn as a cut gemstone (SettlementGem) in the colour of the side that controls it;
    // a capture changes the stone's colour, so there is no separate control mark beside it. UI-406 (TABLETOP_FIGURES_UX,
    // "Settlements"): the stone lies on the table — a soft shadow falls from it down and to the right, and a band of side
    // facets below its girdle (and below a Stronghold's gold setting) gives it thickness. Painter2D only, no filter: the
    // shadow's softness is two translucent fills, the outer one a little larger.
    internal static class MapSettlementVisual
    {
        private const float EdgeWidth = 1f;
        private static readonly Color Gleam = new Color(1, 1, 1, .8f);

        public static void Draw(Painter2D painter, Vector2 at, SettlementGem gem)
        {
            Shadow(painter, at, gem);
            if (gem.Setting)
            {
                Band(painter, gem.SettingOutline(at), at, gem.Thickness, SettlementGem.Gold);
                Cut(painter, gem, gem.SettingOutline(at), gem.Outline(at), SettlementGem.Gold);
            }
            else
                Band(painter, gem.Outline(at), at, gem.Thickness, gem.Colours);
            var outline = gem.Outline(at);
            var table = gem.Table(at);
            Cut(painter, gem, outline, table, gem.Colours);
            Fill(painter, table, gem.TableColour);
            Gleaming(painter, at, gem);
        }

        // The soft shadow on the table under the whole stone: a faint fringe a little larger, then the shadow itself.
        private static void Shadow(Painter2D painter, Vector2 at, SettlementGem gem)
        {
            var silhouette = gem.Silhouette(at);
            var middle = at + new Vector2(0, gem.Thickness / 2);
            Fill(painter, Spread(silhouette, middle, SettlementGem.ShadowSpread), SettlementGem.ShadowFringe);
            Fill(painter, Spread(silhouette, middle, 1), SettlementGem.Shadow);
        }

        // The silhouette moved by the shadow's offset and grown round its middle.
        private static Vector2[] Spread(Vector2[] silhouette, Vector2 middle, float scale)
        {
            var result = new Vector2[silhouette.Length];
            for (var index = 0; index < silhouette.Length; index++)
                result[index] = middle + SettlementGem.ShadowOffset + (silhouette[index] - middle) * scale;
            return result;
        }

        // The side facets below the ring's lower edges, each shaded by the way it faces the light, outlined as one body.
        private static void Band(Painter2D painter, Vector2[] ring, Vector2 at, float thickness, GemPalette palette)
        {
            var down = new Vector2(0, thickness);
            for (var edge = 0; edge < ring.Length; edge++)
            {
                if (!SettlementGem.FacesDown(ring, edge, at))
                    continue;
                var next = (edge + 1) % ring.Length;
                Fill(painter, new[] { ring[edge], ring[next], ring[next] + down, ring[edge] + down },
                    SettlementGem.SideFacet(palette, ring, edge, at));
            }
            var lowered = new Vector2[ring.Length];
            for (var index = 0; index < ring.Length; index++)
                lowered[index] = ring[index] + down;
            Stroke(painter, Hull(ring, lowered), SettlementGem.Edge, EdgeWidth);
        }

        private static Vector2[] Hull(Vector2[] ring, Vector2[] lowered)
        {
            var points = new List<Vector2>(ring);
            points.AddRange(lowered);
            return SettlementGem.ConvexHull(points);
        }

        // The facets between an outer and an inner ring, each shaded by the way it faces the light.
        private static void Cut(Painter2D painter, SettlementGem gem, Vector2[] outer, Vector2[] inner, GemPalette palette)
        {
            for (var edge = 0; edge < outer.Length; edge++)
            {
                var next = (edge + 1) % outer.Length;
                Fill(painter, new[] { outer[edge], outer[next], inner[next], inner[edge] }, gem.Facet(edge, palette));
            }
            Stroke(painter, outer, SettlementGem.Edge, EdgeWidth);
        }

        // A small spark of light on the upper left of the table.
        private static void Gleaming(Painter2D painter, Vector2 at, SettlementGem gem)
        {
            var reach = gem.Size * gem.TableScale;
            painter.fillColor = Gleam;
            painter.BeginPath();
            painter.Arc(at + new Vector2(-.4f, -.4f) * reach, Mathf.Max(.8f, reach * .32f), new Angle(0), new Angle(360));
            painter.ClosePath();
            painter.Fill();
        }

        private static void Fill(Painter2D painter, IReadOnlyList<Vector2> points, Color colour)
        {
            painter.fillColor = colour;
            Path(painter, points);
            painter.Fill();
        }

        private static void Stroke(Painter2D painter, IReadOnlyList<Vector2> points, Color colour, float width)
        {
            painter.strokeColor = colour;
            painter.lineWidth = width;
            painter.lineJoin = LineJoin.Round;
            Path(painter, points);
            painter.Stroke();
        }

        private static void Path(Painter2D painter, IReadOnlyList<Vector2> points)
        {
            painter.BeginPath();
            painter.MoveTo(points[0]);
            for (var index = 1; index < points.Count; index++)
                painter.LineTo(points[index]);
            painter.ClosePath();
        }
    }
}
