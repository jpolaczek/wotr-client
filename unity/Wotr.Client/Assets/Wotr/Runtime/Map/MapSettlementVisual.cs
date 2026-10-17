using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-195: each settlement is drawn as a cut gemstone (SettlementGem) in the colour of the side that controls it;
    // a capture changes the stone's colour, so there is no separate control mark beside it.
    internal static class MapSettlementVisual
    {
        private const float EdgeWidth = 1f;
        private static readonly Color Gleam = new Color(1, 1, 1, .8f);

        public static void Draw(Painter2D painter, BoardPresentation data, CameraState camera, Rect viewport)
        {
            foreach (var region in data.Regions.Values)
            {
                if (region.Settlement == null)
                    continue;
                var point = data.Shapes[region.Id].Anchor;
                var anchor = camera.ToScreen(new Vector2(point[0], point[1]));
                if (!viewport.Contains(anchor))
                    continue;
                data.Control.TryGetValue(region.Id, out var controller);
                Gem(painter, anchor, SettlementGem.For(region.Settlement, controller));
            }
        }

        private static void Gem(Painter2D painter, Vector2 at, SettlementGem gem)
        {
            if (gem.Setting)
                Cut(painter, gem, gem.SettingOutline(at), gem.Outline(at), SettlementGem.Gold);
            var outline = gem.Outline(at);
            var table = gem.Table(at);
            Cut(painter, gem, outline, table, gem.Colours);
            Fill(painter, table, gem.TableColour);
            Gleaming(painter, at, gem);
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
