using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wotr.Client
{
    // Fixed board controls take priority over proximity and floating windows.
    // Edge candidates cover narrow gaps that a coarse placement grid can miss.
    public static class SovereignPlacement
    {
        public static Rect Near(Vector2 anchor, Vector2 size, Rect viewport, IEnumerable<Rect> occupied,
            IEnumerable<Rect> fixedOccupied)
        {
            var blockers = occupied.ToArray();
            var fixedBlockers = fixedOccupied.ToArray();
            var edges = blockers.Concat(fixedBlockers).ToArray();
            var xs = Positions(viewport.xMin, viewport.xMax, size.x, anchor.x + 64, edges, true);
            var ys = Positions(viewport.yMin, viewport.yMax, size.y, anchor.y - size.y / 2, edges, false);
            var best = new Rect(xs[0], ys[0], size.x, size.y);
            var bestFixed = float.PositiveInfinity;
            var bestOverlap = float.PositiveInfinity;
            var bestDistance = float.PositiveInfinity;
            foreach (var y in ys)
                foreach (var x in xs)
                {
                    var candidate = new Rect(x, y, size.x, size.y);
                    var fixedOverlap = Overlap(candidate, fixedBlockers);
                    var overlap = Overlap(candidate, blockers);
                    var distance = (candidate.center - anchor).sqrMagnitude;
                    if (fixedOverlap > bestFixed || fixedOverlap == bestFixed &&
                        (overlap > bestOverlap || overlap == bestOverlap && distance >= bestDistance))
                        continue;
                    best = candidate;
                    bestFixed = fixedOverlap;
                    bestOverlap = overlap;
                    bestDistance = distance;
                }
            return best;
        }

        private static float[] Positions(float minimum, float maximum, float length, float preferred,
            Rect[] blockers, bool horizontal)
        {
            var low = minimum + 8;
            var high = Mathf.Max(low, maximum - length - 8);
            var positions = new HashSet<float> { low, high, Mathf.Clamp(preferred, low, high) };
            foreach (var blocker in blockers)
            {
                var start = horizontal ? blocker.xMin : blocker.yMin;
                var end = horizontal ? blocker.xMax : blocker.yMax;
                positions.Add(Mathf.Clamp(start - length - 4, low, high));
                positions.Add(Mathf.Clamp(end + 4, low, high));
            }
            return positions.OrderBy(value => value).ToArray();
        }

        private static float Overlap(Rect candidate, Rect[] blockers)
        {
            var overlap = 0f;
            foreach (var blocker in blockers)
                overlap += Mathf.Max(0, Mathf.Min(candidate.xMax, blocker.xMax) - Mathf.Max(candidate.xMin, blocker.xMin)) *
                    Mathf.Max(0, Mathf.Min(candidate.yMax, blocker.yMax) - Mathf.Max(candidate.yMin, blocker.yMin));
            return overlap;
        }
    }
}
