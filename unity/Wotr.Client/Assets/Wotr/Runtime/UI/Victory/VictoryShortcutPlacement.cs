using System.Collections.Generic;
using UnityEngine;

namespace Wotr.Client
{
    public static class VictoryShortcutPlacement
    {
        private const float Gap = 8;

        public static Rect Place(Rect preferred, Vector2 minimum, IReadOnlyList<Rect> obstacles)
        {
            if (TryRow(preferred, minimum.x, obstacles, out var bottom))
                return bottom;
            var best = preferred;
            var distance = float.PositiveInfinity;
            foreach (var obstacle in obstacles)
            {
                Consider(obstacle.yMin - preferred.height - Gap);
                Consider(obstacle.yMax + Gap);
            }
            return best;

            void Consider(float y)
            {
                if (y < minimum.y || y > preferred.y)
                    return;
                var row = new Rect(preferred.x, y, preferred.width, preferred.height);
                if (!TryRow(row, minimum.x, obstacles, out var candidate))
                    return;
                var candidateDistance = Vector2.SqrMagnitude(candidate.position - preferred.position);
                if (candidateDistance >= distance)
                    return;
                best = candidate;
                distance = candidateDistance;
            }
        }

        private static bool TryRow(Rect preferred, float minimumX, IReadOnlyList<Rect> obstacles, out Rect result)
        {
            result = preferred;
            if (Clear(preferred, obstacles))
                return true;
            var bestX = float.NegativeInfinity;
            foreach (var obstacle in obstacles)
            {
                Consider(obstacle.xMin - preferred.width - Gap);
                Consider(obstacle.xMax + Gap);
            }
            if (float.IsNegativeInfinity(bestX))
                return false;
            result = new Rect(bestX, preferred.y, preferred.width, preferred.height);
            return true;

            void Consider(float x)
            {
                if (x < minimumX || x > preferred.x || x <= bestX)
                    return;
                if (Clear(new Rect(x, preferred.y, preferred.width, preferred.height), obstacles))
                    bestX = x;
            }
        }

        private static bool Clear(Rect candidate, IReadOnlyList<Rect> obstacles)
        {
            foreach (var obstacle in obstacles)
                if (candidate.Overlaps(obstacle))
                    return false;
            return true;
        }
    }
}
