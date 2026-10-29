using UnityEngine;

namespace Wotr.Client
{
    // UI-401 (TABLETOP_FIGURES_UX, "Scale"): how large the map draws what stands on it. A figure is sized in board units:
    // the zoom, clamped between the worst-case Detail zoom (MapStandSlots.WorstCaseZoom, where the footprints are at
    // scale 1, the readable minimum) and the zoom at which the miniature reaches its design size, is the number of points
    // per board unit it is drawn at. Below the minimum a figure keeps its readable size, so its footprint grows past
    // scale 1; above the maximum it stops growing, so its footprint shrinks and a tight region's figures find room.
    public readonly struct MapStandScale
    {
        // The token is a puck about as wide as an army's base, kept between a readable size and its design size.
        public const float TokenUnits = 40;
        public const float TokenMinimum = 32;
        public const float TokenDesign = 44;
        // Figures come back to tokens only once the footprint exceeds the capacity by the Detail level's hysteresis.
        public const float CapacityHysteresis = MapMarkerVisibility.FiguresFrom / MapMarkerVisibility.TokensBelow;
        private const float Tolerance = 1e-4f;

        public readonly float Zoom;
        public readonly float PointsPerUnit;

        public MapStandScale(float zoom, float minimum, float maximum)
        {
            Zoom = zoom;
            PointsPerUnit = Mathf.Clamp(zoom, minimum, maximum);
        }

        // The scale of the stand footprints on the board: 1 between the clamps.
        public float Footprint => Zoom > 0 ? PointsPerUnit / Zoom : float.PositiveInfinity;

        // The element scale of something designed at `designUnit` points per board unit.
        public float Of(float designUnit) => PointsPerUnit / designUnit;

        // The element scale of an army's token.
        public float Token => Mathf.Clamp(TokenUnits * Zoom, TokenMinimum, TokenDesign) / TokenDesign;

        // Whether a region's group fits its capacity at this footprint; `shown` keeps figures until the hysteresis.
        public bool Fits(float capacity, bool shown) =>
            Footprint <= capacity * (shown ? CapacityHysteresis : 1) + Tolerance;
    }
}
