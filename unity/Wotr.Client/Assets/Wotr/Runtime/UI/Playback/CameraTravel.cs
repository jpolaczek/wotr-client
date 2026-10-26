using UnityEngine;

namespace Wotr.Client
{
    // UI-305 (OPPONENT_ACTIONS_UX §6.5–§6.6): one eased camera move between two framings. The board point at the focus
    // (the safe area's centre) travels on a straight line while the zoom changes geometrically, so the map neither
    // swings nor jumps in scale. A longer travel or a larger zoom change takes longer, from 450 to 750 ms.
    public sealed class CameraTravel
    {
        public const double Shortest = .45;
        public const double Longest = .75;
        private const float FarDistance = 900;
        private readonly float zoomFrom;
        private readonly float zoomTo;
        private readonly Vector2 centreFrom;
        private readonly Vector2 centreTo;
        private readonly Vector2 focus;
        public double Duration { get; }

        public CameraTravel(float zoomFrom, Vector2 panFrom, float zoomTo, Vector2 panTo, Vector2 focus)
        {
            this.zoomFrom = zoomFrom;
            this.zoomTo = zoomTo;
            this.focus = focus;
            centreFrom = (focus - panFrom) / zoomFrom;
            centreTo = (focus - panTo) / zoomTo;
            var distance = (centreTo * zoomFrom + panFrom - focus).magnitude;
            var doublings = Mathf.Abs(Mathf.Log(zoomTo / zoomFrom, 2));
            Duration = Shortest + (Longest - Shortest) * Mathf.Clamp01(Mathf.Max(distance / FarDistance, doublings));
        }

        public (float Zoom, Vector2 Pan) At(double fraction)
        {
            var t = Mathf.Clamp01((float)fraction);
            var eased = t * t * (3 - 2 * t);
            var zoom = zoomFrom * Mathf.Pow(zoomTo / zoomFrom, eased);
            var centre = Vector2.Lerp(centreFrom, centreTo, eased);
            return (zoom, focus - centre * zoom);
        }
    }
}
