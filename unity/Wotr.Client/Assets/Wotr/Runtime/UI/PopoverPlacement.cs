using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wotr.Client
{
    public static class PopoverPlacement
    {
        // Moving a panel at a fractional scale can change its rounded measured size
        // by a pixel. That must not start another placement search and move it back.
        public static bool SizeChanged(Rect previous, Rect current) =>
            Mathf.Abs(previous.width - current.width) > 1 || Mathf.Abs(previous.height - current.height) > 1;

        public static Rect Near(Vector2 anchor, Vector2 size, Rect viewport, IEnumerable<Rect> occupied)
        {
            var blockers = occupied?.ToArray() ?? System.Array.Empty<Rect>();
            var best = Clamp(anchor + new Vector2(64, -size.y / 2), size, viewport);
            var score = Score(best, anchor, blockers);
            // Evaluated only on opening/geometry changes, never on every pointer move.
            for (var y = 8f; y <= Mathf.Max(8, viewport.height - size.y - 8); y += 24)
                for (var x = 8f; x <= Mathf.Max(8, viewport.width - size.x - 8); x += 24)
                {
                    var candidate = new Rect(new Vector2(x, y), size);
                    var value = Score(candidate, anchor, blockers);
                    if (value >= score)
                        continue;
                    best = candidate;
                    score = value;
                }
            return best;
        }

        private static Rect Clamp(Vector2 position, Vector2 size, Rect viewport) => new Rect(
            Mathf.Clamp(position.x, 8, Mathf.Max(8, viewport.width - size.x - 8)),
            Mathf.Clamp(position.y, 8, Mathf.Max(8, viewport.height - size.y - 8)), size.x, size.y);

        private static float Score(Rect rect, Vector2 anchor, Rect[] blockers)
        {
            var score = (rect.center - anchor).sqrMagnitude;
            score += Overlap(rect, new Rect(anchor - Vector2.one * 24, Vector2.one * 48)) * 10000;
            foreach (var blocker in blockers)
                score += Overlap(rect, blocker) * 1000;
            return score;
        }

        private static float Overlap(Rect a, Rect b) => Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) *
            Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
    }
}
