using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wotr.Client
{
    // UI-522: how one figure of each kind stands in a row. UI-526 (owner, 2026-11-08: "bigger still; some are very, very
    // small — not all are the same"): every figure stands at its full stature, as a single army's does, wherever it
    // stands. They stand apart while the row holds them; otherwise each overlaps its neighbour up to OverlapShare of the
    // narrower one's width — the counts at their feet still apart — and where even that does not fit the row is wider
    // than its place (MapArmyArtwork lets it reach past it on both sides). In design points, from the row's left edge.
    public static class MapArmyRow
    {
        public const float OverlapShare = .4f;
        public const float Gap = 2;
        public const float CountWidth = 16;
        private const float CountSpacing = CountWidth + 1;
        private const float ScaleStep = .005f;
        private const float Tolerance = .01f;
        private const float SmallestScale = .2f;

        // Each figure's box (its left edge, top 0, width, height) and the row's width, for figures of these full sizes in
        // a place `available` points wide.
        public static (Rect[] Boxes, float Width) Lay(IReadOnlyList<Vector2> full, float available)
        {
            if (full.Count == 0)
                return (new Rect[0], 0);
            var rest = Overlaps(full, false);
            var most = Overlaps(full, true);
            var width = full.Sum(size => size.x) - rest.Sum();
            if (width <= available)
                return Place(full, rest);
            var slack = most.Zip(rest, (limit, apart) => limit - apart).Sum();
            var needed = width - available;
            return Place(full, needed <= slack ? Shared(rest, most, needed, slack) : most);
        }

        // UI-526: the row within `available` points even at its most overlapping: its figures shrink alike, as little as
        // that takes. Only where a row may not reach past its place far enough (MapArmyRowRoom.Limit).
        public static (Rect[] Boxes, float Width) Fit(IReadOnlyList<Vector2> full, float available)
        {
            var laid = Lay(full, available);
            var scale = 1f;
            // At its most overlapping a row is as much narrower as its figures are smaller, so the first guess is the
            // proportion; a small step takes off what the counts' spacing keeps.
            while (laid.Width > available + Tolerance && scale > SmallestScale)
            {
                scale *= available / laid.Width;
                laid = Lay(full.Select(size => size * scale).ToList(), available);
                if (laid.Width > available + Tolerance)
                    scale -= ScaleStep;
            }
            return laid;
        }

        // How far each neighbour overlaps the next (a negative overlap is a gap): standing apart, a gap; at most, the share
        // of the narrower, the counts still apart.
        private static float[] Overlaps(IReadOnlyList<Vector2> full, bool most)
        {
            var overlaps = new float[full.Count - 1];
            for (var i = 0; i < overlaps.Length; i++)
            {
                var left = full[i].x;
                var right = full[i + 1].x;
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

        private static (Rect[] Boxes, float Width) Place(IReadOnlyList<Vector2> full, float[] overlaps)
        {
            var boxes = new Rect[full.Count];
            var left = 0f;
            for (var i = 0; i < full.Count; i++)
            {
                boxes[i] = new Rect(left, 0, full[i].x, full[i].y);
                if (i < overlaps.Length)
                    left += full[i].x - overlaps[i];
            }
            return (boxes, boxes[full.Count - 1].xMax);
        }
    }
}
