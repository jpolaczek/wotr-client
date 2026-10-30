using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-404 (TABLETOP_FIGURES_UX, "Flight profile"): one flight — the figures one destination marker receives from one
    // source, drawn as that marker's miniature (FlightMiniature). Its holder (a MapStand) stands on the ground under the
    // figures, between the two stand points, so the shadow inside it stays on the table; the pose inside lifts, arcs and
    // lands the miniature (FlightProfile). Each frame writes only the holder's translate and scale, the pose's translate
    // and scale and the shadow's scale and opacity (UI-427: and the Fellowship plaque's scale), each only when it changes. An
    // arrival in place (a recruit) and every flight under Reduced motion stand at the destination and fade a little, as
    // before.
    internal sealed class FigureFlight
    {
        public const string FlightClass = "figure-flight";
        public const string ArrivalClass = "figure-arrival";

        public VisualElement Root => stand.Root;
        public IReadOnlyList<BoardFigureMove> Moves => group;
        public float Duration { get; private set; }
        public bool Arrival { get; private set; }
        public Vector2 From => start.Ground;
        public Vector2 To { get; }
        // Where it was drawn last (map-local): the ground under the feet, and how high the feet stand above it.
        public Vector2 Ground { get; private set; }
        public float Height { get; private set; }
        public FlightFrame Frame { get; private set; }
        private readonly List<BoardFigureMove> group;
        private readonly FlightStart start;
        private readonly FlightMiniature miniature;
        private readonly MapStand stand;
        private readonly VisualElement pose;
        private readonly VisualElement shadow;
        private readonly double began;
        private readonly float skipped;
        private readonly HashSet<string> keys = new HashSet<string>();
        private float shownRaise = float.NaN;
        private float shownLift = float.NaN;
        private float shownShadowSize = float.NaN;
        private float shownShadowAlpha = float.NaN;
        private float shownOpacity = float.NaN;
        private bool laidOut;

        public FigureFlight(List<BoardFigureMove> group, FlightMiniature miniature, FlightStart start, Vector2 to,
            bool arrival, double now)
        {
            this.group = group;
            this.miniature = miniature;
            this.start = start;
            To = to;
            Arrival = arrival;
            began = now;
            foreach (var move in group)
                keys.Add(move.To.Key);
            Duration = arrival ? FlightProfile.ArrivalSeconds : FlightProfile.Duration(Vector2.Distance(start.Ground, to));
            skipped = arrival ? 0 : FlightProfile.StartsAt(start) * Duration;
            shadow = Part("flight-shadow");
            shadow.AddToClassList(miniature.ShadowClass);
            pose = Part("flight-lift");
            pose.style.transformOrigin = new TransformOrigin(miniature.Feet.x, miniature.Feet.y, 0);
            stand = Holder();
            Root.name = "moving-" + group[0].To.Key;
            Root.AddToClassList(FlightClass);
            Root.EnableInClassList(ArrivalClass, arrival);
        }

        // The miniature in its pose over its shadow, in a holder of its design size.
        private MapStand Holder()
        {
            miniature.Element.style.left = 0;
            miniature.Element.style.top = 0;
            pose.Add(miniature.Element);
            var content = new VisualElement { name = "flight-figure", pickingMode = PickingMode.Ignore };
            content.style.position = Position.Absolute;
            content.style.width = miniature.Size.x;
            content.style.height = miniature.Size.y;
            content.Add(shadow);
            content.Add(pose);
            var holder = new MapStand(content, miniature.Size);
            holder.Design(miniature.Feet, miniature.Visible);
            return holder;
        }

        // A part of the pose: only its transform and opacity change. The name doubles as its class (Styles/Armies.uss).
        private static VisualElement Part(string name)
        {
            var element = new VisualElement
            {
                name = name,
                pickingMode = PickingMode.Ignore,
                usageHints = UsageHints.DynamicTransform
            };
            element.AddToClassList(name);
            return element;
        }

        public bool Carries(string key) => keys.Contains(key);

        // Whether a new projection moves any of its figures again.
        public bool Takes(ISet<string> moving)
        {
            foreach (var key in keys)
                if (moving.Contains(key))
                    return true;
            return false;
        }

        // UI-95: whether a projection still has every flying figure where this flight takes it.
        public bool Holds(BoardFigureSnapshot snapshot)
        {
            foreach (var move in group)
                if (!snapshot.Figures.TryGetValue(move.To.Key, out var position) ||
                    position.Region != move.To.Region || position.Garrison != move.To.Garrison)
                    return false;
            return true;
        }

        // The share of the profile shown at `now`.
        public float Progress(double now) => Mathf.Clamp01((float)(now - began + skipped) / Duration);

        // Its last frame, the figures standing on their place, has been laid out.
        public bool Settled { get; private set; }

        // Reduced motion while it flies: the figures stand at their destination for what is left of an arrival.
        public void Reduce()
        {
            if (Arrival)
                return;
            Arrival = true;
            Duration = Mathf.Min(Duration, FlightProfile.ArrivalSeconds);
            Root.AddToClassList(ArrivalClass);
        }

        // Where the figures were last drawn, on the board: a new projection's flight of them continues from there (UI-95).
        public FlightStart Current()
        {
            if (Arrival)
                return FlightStart.Of(To, FlightPose.Standing);
            if (!laidOut)
                return start;
            return new FlightStart(Vector2.Lerp(start.Ground, To, Frame.Progress), Frame.Lift, Frame.Height);
        }

        // The pass of every frame and every camera change.
        public void Layout(MapCanvas map, double now)
        {
            var t = Progress(now);
            var from = map.Camera.ToScreen(start.Ground);
            var to = map.Camera.ToScreen(To);
            Frame = Arrival ? new FlightFrame(1, 0, 0, 0)
                : FlightProfile.At(t, start, FlightProfile.ArcHeight(Vector2.Distance(from, to)));
            laidOut = true;
            var scale = map.Stands.Scale.Of(miniature.DesignUnit);
            Ground = Vector2.Lerp(from, to, Frame.Progress);
            Height = FlightProfile.Raise(Frame, scale) * scale;
            stand.Place(Ground, scale);
            miniature.Scaled?.Invoke(scale);
            ShowPose(FlightProfile.Raise(Frame, scale), FlightProfile.Scale(Frame));
            ShowShadow(FlightProfile.ShadowSize(Frame), FlightProfile.ShadowAlpha(Frame, miniature.StandingShadow));
            ShowOpacity(Arrival ? 1 - t * .6f : 1);
            Settled = t >= 1;
        }

        private void ShowPose(float raise, float lift)
        {
            if (raise != shownRaise)
            {
                shownRaise = raise;
                pose.style.translate = new Translate(0, -raise);
            }
            if (lift != shownLift)
            {
                shownLift = lift;
                pose.style.scale = new Scale(new Vector2(lift, lift));
            }
        }

        private void ShowShadow(float size, float alpha)
        {
            if (size != shownShadowSize)
            {
                shownShadowSize = size;
                shadow.style.scale = new Scale(new Vector2(size, size));
            }
            if (alpha != shownShadowAlpha)
            {
                shownShadowAlpha = alpha;
                shadow.style.opacity = alpha;
            }
        }

        private void ShowOpacity(float opacity)
        {
            if (opacity == shownOpacity)
                return;
            shownOpacity = opacity;
            Root.style.opacity = opacity;
        }

        public void Remove() => stand.Remove();
    }
}
