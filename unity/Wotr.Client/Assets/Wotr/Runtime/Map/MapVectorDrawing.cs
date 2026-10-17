using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal static class MapVectorDrawing
    {
        public static void Fill(Painter2D painter, IReadOnlyList<MapContourRing> rings, Color color)
        {
            if (rings.Count == 0)
                return;
            painter.fillColor = color;
            painter.BeginPath();
            foreach (var ring in rings)
            {
                var points = ring.Points;
                painter.MoveTo(new Vector2(points[0][0], points[0][1]));
                for (var index = 1; index < points.Length - 1; index++)
                    painter.LineTo(new Vector2(points[index][0], points[index][1]));
                painter.ClosePath();
            }
            painter.Fill(FillRule.NonZero);
        }

        // UI-143: the edge of each ring as a line, for a fringe that does not darken the area it surrounds.
        public static void Outline(Painter2D painter, IReadOnlyList<MapContourRing> rings, Color color, float width)
        {
            if (rings.Count == 0)
                return;
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            foreach (var ring in rings)
            {
                var points = ring.Points;
                painter.MoveTo(new Vector2(points[0][0], points[0][1]));
                for (var index = 1; index < points.Length - 1; index++)
                    painter.LineTo(new Vector2(points[index][0], points[index][1]));
                painter.ClosePath();
            }
            painter.Stroke();
        }

        public static void Stroke(Painter2D painter, float[][] points, MapBorderPart part, Color color, float width)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Butt;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(new Vector2(points[part.FromSegment][0], points[part.FromSegment][1]));
            for (var index = part.FromSegment + 1; index <= part.ToSegment; index++)
                painter.LineTo(new Vector2(points[index][0], points[index][1]));
            painter.Stroke();
        }

    }
}
