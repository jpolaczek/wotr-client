using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wotr.Client
{
    // UI-522, UI-523 (owner, 2026-11-08: the figures "must be decidedly bigger"): how one figure of each kind stands in a
    // row. Each stands at its full size while the row holds them apart; otherwise all shrink alike to fit, down to
    // MinimumScale; below that each overlaps its neighbour up to OverlapShare of the narrower one's width — the counts at
    // their feet still apart — and only then do they shrink further. In design points, from the row's left edge.
    public static class MapArmyRow
    {
        public const float MinimumScale = .85f;
        public const float OverlapShare = 1f / 3;
        public const float Gap = 2;
        public const float CountWidth = 16;
        private const float CountSpacing = CountWidth + 1;
        private const float SmallestScale = .2f;
        private const float ScaleStep = .01f;

        // Each figure's box (its left edge, top 0, width, height) and the row's width, for figures of these full sizes in
        // a row of at most `available` points.
        public static (Rect[] Boxes, float Width) Lay(IReadOnlyList<Vector2> full, float available)
        {
            var count = full.Count;
            if (count == 0)
                return (new Rect[0], 0);
            var widths = 0f;
            foreach (var size in full)
                widths += size.x;
            var gaps = Gap * (count - 1);
            if (widths + gaps <= available)
                return Place(full, 1, Spaced(count));
            var fit = (available - gaps) / widths;
            if (fit >= MinimumScale)
                return Place(full, fit, Spaced(count));
            for (var scale = MinimumScale; scale > SmallestScale; scale -= ScaleStep)
            {
                var rest = Overlaps(full, scale, false);
                var most = Overlaps(full, scale, true);
                var width = widths * scale - rest.Sum();
                if (width <= available)
                    return Place(full, scale, rest);
                var slack = most.Zip(rest, (limit, apart) => limit - apart).Sum();
                if (width - available <= slack)
                    return Place(full, scale, Shared(rest, most, width - available, slack));
            }
            return Place(full, SmallestScale, Overlaps(full, SmallestScale, true));
        }

        // How far each neighbour overlaps the next at a scale (a negative overlap is a gap): standing apart, a gap, wider
        // where their counts need it; at most, a third of the narrower, the counts still apart.
        private static float[] Overlaps(IReadOnlyList<Vector2> full, float scale, bool most)
        {
            var overlaps = new float[full.Count - 1];
            for (var i = 0; i < overlaps.Length; i++)
            {
                var left = full[i].x * scale;
                var right = full[i + 1].x * scale;
                var counts = (left + right) / 2 - CountSpacing;
                overlaps[i] = Mathf.Min(most ? OverlapShare * Mathf.Min(left, right) : -Gap, counts);
            }
            return overlaps;
        }

        // Each pair takes its share of the overlap needed, in proportion to how much more it may take.
        private static float[] Shared(float[] rest, float[] most, float needed, float slack)
        {
            var overlaps = new float[rest.Length];
            for (var i = 0; i < rest.Length; i++)
                overlaps[i] = rest[i] + (slack > 0 ? needed * (most[i] - rest[i]) / slack : 0);
            return overlaps;
        }

        // A gap between every pair: a negative overlap.
        private static float[] Spaced(int count)
        {
            var overlaps = new float[count - 1];
            for (var i = 0; i < overlaps.Length; i++)
                overlaps[i] = -Gap;
            return overlaps;
        }

        private static (Rect[] Boxes, float Width) Place(IReadOnlyList<Vector2> full, float scale, float[] overlaps)
        {
            var boxes = new Rect[full.Count];
            var left = 0f;
            for (var i = 0; i < full.Count; i++)
            {
                var size = full[i] * scale;
                boxes[i] = new Rect(left, 0, size.x, size.y);
                if (i < overlaps.Length)
                    left += size.x - overlaps[i];
            }
            return (boxes, boxes[full.Count - 1].xMax);
        }
    }
}
