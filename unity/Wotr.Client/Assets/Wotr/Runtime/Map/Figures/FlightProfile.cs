using UnityEngine;

namespace Wotr.Client
{
    // UI-404: how a figure is held when its flight begins — standing on the table, or raised in the player's hand (UI-403).
    public enum FlightPose
    {
        Standing,
        Raised
    }

    // UI-404: where a flight begins: the board point under the figures' feet, how far they are lifted (0 standing, 1 the
    // raised pose) and, for a flight a new projection interrupts mid-air, how high above the raised pose they are (px).
    public readonly struct FlightStart
    {
        public readonly Vector2 Ground;
        public readonly float Lift;
        public readonly float Height;

        public FlightStart(Vector2 ground, float lift, float height)
        {
            Ground = ground;
            Lift = Mathf.Clamp01(lift);
            Height = Mathf.Max(0, height);
        }

        public static FlightStart Of(Vector2 ground, FlightPose pose) =>
            new FlightStart(ground, pose == FlightPose.Raised ? 1 : 0, 0);
    }

    // UI-404: one moment of a flight: how far along the ground it is (0 the source, 1 the destination), how far the
    // figures are lifted into the raised pose, how high the arc carries them above it (px) and the landing's settle (0..1).
    public readonly struct FlightFrame
    {
        public readonly float Progress;
        public readonly float Lift;
        public readonly float Height;
        public readonly float Settle;

        public FlightFrame(float progress, float lift, float height, float settle)
        {
            Progress = progress;
            Lift = lift;
            Height = height;
            Settle = settle;
        }
    }

    // UI-404 (TABLETOP_FIGURES_UX, "Flight profile"): a moving figure rises, flies in an arc and lands on its place.
    // 0–15 % of the flight lifts it in place into the raised pose (UI-403's: 1.10 × round its feet, 12 design points up,
    // its shadow left on the table at .8 and .35); 15–85 % carries it along the ground, eased, in an arc of
    // clamp(.12 × distance, 20, 80) px above that pose, its shadow following the ground under it; 85–100 % lands it: it
    // drops onto its place and settles with a short hop. The last frame is the figure standing on its place, so nothing
    // snaps when the stand takes over. A flight that starts raised (handed over by the hand) begins at the arc.
    public static class FlightProfile
    {
        public const float LiftEnd = .15f;
        public const float LandingStart = .85f;
        // Within the landing: the drop onto the place, then the settle.
        private const float DropShare = .6f;
        public const float ArcShare = .12f;
        public const float MinimumArc = 20;
        public const float MaximumArc = 80;
        // Unchanged since UI-76: 0.65 s plus a second per 1600 board units, at most 1.05 s; an arrival in place 0.45 s.
        public const float MinimumSeconds = .65f;
        public const float MaximumSeconds = 1.05f;
        public const float UnitsPerSecond = 1600;
        public const float ArrivalSeconds = .45f;
        // The settle's hop, in design points (so it scales with the figure).
        public const float SettleHop = 2;
        // The raised pose (MapHandPiece's, Styles/Armies.uss .figure-lifted).
        public const float LiftScale = MapHandPiece.LiftScale;
        public const float Rise = MapHandPiece.Rise;
        public const float ShadowScale = MapHandPiece.ShadowScale;
        public const float ShadowOpacity = MapHandPiece.ShadowOpacity;
        // High in the arc the shadow is a little smaller and fainter still.
        private const float HighShadowScale = .2f;
        private const float HighShadowFade = .4f;

        public static float Duration(float boardDistance) =>
            Mathf.Clamp(MinimumSeconds + boardDistance / UnitsPerSecond, MinimumSeconds, MaximumSeconds);

        public static float ArcHeight(float screenDistance) => Mathf.Clamp(ArcShare * screenDistance, MinimumArc, MaximumArc);

        // The share of the profile already behind a flight when it begins: a raised figure skips the lift.
        public static float StartsAt(FlightStart start) => start.Lift >= 1 ? LiftEnd : 0;

        public static FlightFrame At(float t, FlightStart start, float arc)
        {
            t = Mathf.Clamp01(t);
            if (t < LiftEnd)
                return Lifting(t / LiftEnd, start);
            if (t < LandingStart)
                return Arcing((t - LiftEnd) / (LandingStart - LiftEnd), start, arc);
            return Landing((t - LandingStart) / (1 - LandingStart));
        }

        private static FlightFrame Lifting(float v, FlightStart start) =>
            new FlightFrame(0, Mathf.Lerp(start.Lift, 1, EaseOut(v)), start.Height, 0);

        // Along the ground, eased; the arc's top is half way. A flight taken over mid-air leaves its old height behind
        // as it goes, so it continues from where it was.
        private static FlightFrame Arcing(float u, FlightStart start, float arc)
        {
            var progress = Smooth(u);
            var height = start.Height * (1 - progress) + arc * 4 * progress * (1 - progress);
            return new FlightFrame(progress, 1, height, 0);
        }

        private static FlightFrame Landing(float v)
        {
            if (v < DropShare)
            {
                var drop = v / DropShare;
                return new FlightFrame(1, 1 - drop * drop, 0, 0);
            }
            var settle = Mathf.Sin(Mathf.PI * (v - DropShare) / (1 - DropShare));
            return new FlightFrame(1, 0, 0, Mathf.Max(0, settle));
        }

        // The figures' scale round their feet.
        public static float Scale(FlightFrame frame) => Mathf.Lerp(1, LiftScale, frame.Lift);

        // How far the feet stand above the ground, in design points (the arc's pixels divided by the figures' scale).
        public static float Raise(FlightFrame frame, float figureScale) =>
            Rise * frame.Lift + SettleHop * frame.Settle + frame.Height / Mathf.Max(.01f, figureScale);

        // The shadow on the ground: the raised pose's, a little smaller and fainter high in the arc.
        public static float ShadowSize(FlightFrame frame) =>
            Mathf.Lerp(1, ShadowScale, frame.Lift) * (1 - HighShadowScale * High(frame));

        // `standing` is the shadow's opacity under a standing figure (the Fellowship's figure has none on the table).
        public static float ShadowAlpha(FlightFrame frame, float standing) =>
            Mathf.Lerp(standing, ShadowOpacity, frame.Lift) * (1 - HighShadowFade * High(frame));

        private static float High(FlightFrame frame) => Mathf.Clamp01(frame.Height / MaximumArc);

        private static float EaseOut(float v) => 1 - (1 - v) * (1 - v);

        private static float Smooth(float v) => v * v * (3 - 2 * v);
    }
}
