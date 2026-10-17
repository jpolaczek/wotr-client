using UnityEngine;

namespace Wotr.Client
{
    public static class EffectsPanelPlacement
    {
        private const float Gap = 8;

        public static Rect Place(Vector2 viewport, Rect preferred, Rect? obstacle)
        {
            var bounds = new Rect(Gap, Gap, Mathf.Max(0, viewport.x - 2 * Gap), Mathf.Max(0, viewport.y - 2 * Gap));
            var current = Fit(preferred.position, preferred.size, bounds);
            if (!obstacle.HasValue || !current.Overlaps(obstacle.Value))
                return current;
            var blocked = obstacle.Value;
            var positions = new[]
            {
                new Vector2(current.x, blocked.yMax + Gap),
                new Vector2(blocked.xMax + Gap, current.y),
                new Vector2(current.x, blocked.yMin - Gap - current.height),
                new Vector2(blocked.xMin - Gap - current.width, current.y)
            };
            foreach (var position in positions)
            {
                var candidate = Fit(position, current.size, bounds);
                if (!candidate.Overlaps(blocked))
                    return candidate;
            }
            return current;
        }

        private static Rect Fit(Vector2 position, Vector2 size, Rect bounds) => new Rect(
            Mathf.Clamp(position.x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - size.x)),
            Mathf.Clamp(position.y, bounds.yMin, Mathf.Max(bounds.yMin, bounds.yMax - size.y)), size.x, size.y);
    }
}
