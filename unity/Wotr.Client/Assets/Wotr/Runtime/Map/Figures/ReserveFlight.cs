using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-454: where a recruit's flight begins, map-local: the point under its feet, the scale it is drawn at there and its
    // pose (FlightStart's lift and height above the raised pose; its board point is not used).
    public readonly struct ReserveFlightStart
    {
        public readonly Vector2 Feet;
        public readonly float Scale;
        public readonly FlightStart Pose;

        public ReserveFlightStart(Vector2 feet, float scale, FlightStart pose)
        {
            Feet = feet;
            Scale = Mathf.Max(.05f, scale);
            Pose = pose;
        }

        // Standing on the slot's own picture of the figure: the lone figure's box on its base (`figure`, in the miniature's
        // design points, its feet at `feet`) laid over the picture's drawn part (`slot`, panel coordinates, drawn to fit), so
        // the miniature first stands exactly where the slot shows it. `map` turns panel points into map-local ones.
        public static ReserveFlightStart OnSlot(VisualElement map, Rect slot, float aspect, Rect figure, Vector2 feet)
        {
            var height = Mathf.Min(slot.height, slot.width / Mathf.Max(.01f, aspect));
            var scale = height / Mathf.Max(1, figure.height);
            var bottom = map.WorldToLocal(new Vector2(slot.center.x, slot.center.y + height / 2));
            var offset = new Vector2(figure.center.x - feet.x, figure.yMax - feet.y) * scale;
            return new ReserveFlightStart(bottom - offset, scale, FlightStart.Of(Vector2.zero, FlightPose.Standing));
        }
    }

    // UI-454: one recruit's flight as it was drawn last (map-local), for the board's checks and tests.
    public readonly struct ReserveFlightSnapshot
    {
        public readonly string UnitId;
        public readonly bool Planned;
        // Where its feet stood and at what scale; the frame of the profile; the share shown; its root's opacity.
        public readonly Vector2 Feet;
        public readonly float Scale;
        public readonly FlightFrame Frame;
        public readonly float Progress;
        public readonly float Opacity;
        public readonly bool Landed;
        // Where it began, and the point (map-local) it lands on now.
        public readonly Vector2 From;
        public readonly Vector2 To;

        internal ReserveFlightSnapshot(ReserveFlight flight)
        {
            UnitId = flight.UnitId;
            Planned = flight.Planned;
            Feet = flight.Feet;
            Scale = flight.Scale;
            Frame = flight.Frame;
            Progress = flight.Shown;
            Opacity = flight.Opacity;
            Landed = flight.Landed;
            From = flight.From;
            To = flight.To;
        }
    }

    // UI-454 (owner, 2026-11-03: "when the user just picks with a click, the unit should glide smoothly from its Reserve slot
    // to its proper place on the board, like drawn cards"): one recruit flying from its Reserve slot — or from where its
    // carried miniature was dropped — to the place it will stand on. It is the miniature it will be (FlightMiniature.Recruit),
    // first laid over the slot's picture of it, growing to the figures' scale on the map while it rises, arcs and lands
    // (FlightProfile, UI-404); from a slot it fades in as it rises out of it. It stands in the map's carry layer, above the
    // HUD, and its destination is read again on every layout, so a camera move is followed. A planned recruit fades to its
    // ghost's opacity as it lands. Each frame writes only the holder's translate and scale, the pose's translate and scale,
    // the shadow's scale and opacity and the root's opacity, each only when it changes.
    internal sealed class ReserveFlight
    {
        public const string NamePrefix = "reserve-flight-";
        public const float FadeSeconds = .2f;
        public string UnitId { get; }
        public string Region { get; }
        public string Key => BoardFigurePosition.UnitPrefix + UnitId;
        public bool Planned { get; private set; }
        public VisualElement Root => stand.Root;
        public Vector2 From => start.Feet;
        public Vector2 To { get; private set; }
        public Vector2 Feet { get; private set; }
        public float Scale { get; private set; }
        public FlightFrame Frame { get; private set; }
        public float Shown { get; private set; }
        public float Opacity { get; private set; } = float.NaN;
        public bool Landed { get; private set; }
        public float Duration { get; }
        private readonly ReserveFlightStart start;
        private Func<Vector2> destination;
        private float landedOpacity;
        private readonly float arc;
        private readonly FlightMiniature miniature;
        private readonly MapStand stand;
        private readonly VisualElement pose;
        private readonly VisualElement shadow;
        private readonly double began;
        private readonly float skipped;
        private double? fading;
        private float fadeFrom = 1;
        private int seen;
        private float shownRaise = float.NaN;
        private float shownLift = float.NaN;
        private float shownShadowSize = float.NaN;
        private float shownShadowAlpha = float.NaN;

        // `destination`: the board point it lands on, read on every layout; `landedOpacity`: its opacity once landed.
        public ReserveFlight(string unitId, string region, bool planned, FlightMiniature miniature, ReserveFlightStart start,
            Func<Vector2> destination, float landedOpacity, MapCanvas map, double now)
        {
            UnitId = unitId;
            Region = region;
            Planned = planned;
            this.miniature = miniature;
            this.start = start;
            this.destination = destination;
            this.landedOpacity = landedOpacity;
            began = now;
            var to = map.Camera.ToScreen(destination());
            var distance = Vector2.Distance(start.Feet, to);
            Duration = FlightProfile.Duration(distance / Mathf.Max(.01f, map.Camera.Zoom));
            arc = FlightProfile.ArcHeight(distance);
            skipped = FlightProfile.StartsAt(start.Pose) * Duration;
            shadow = Part("flight-shadow");
            shadow.AddToClassList(miniature.ShadowClass);
            pose = Part("flight-lift");
            pose.style.transformOrigin = new TransformOrigin(miniature.Feet.x, miniature.Feet.y, 0);
            stand = Holder();
            Root.name = NamePrefix + unitId;
            Root.AddToClassList(FigureFlight.FlightClass);
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

        public ReserveFlightSnapshot Snapshot => new ReserveFlightSnapshot(this);

        // The share of the profile shown at `now`.
        public float Progress(double now) => Mathf.Clamp01((float)(now - began + skipped) / Duration);

        // The pass of every frame and every camera change: along the profile, from the start to the destination's place on
        // the screen now, from the start's scale to the figures' scale.
        public void Layout(MapCanvas map, double now)
        {
            var t = Progress(now);
            Shown = t;
            To = map.Camera.ToScreen(destination());
            Frame = FlightProfile.At(t, start.Pose, arc);
            Scale = Mathf.Lerp(start.Scale, map.Stands.FigureScale, Frame.Progress);
            Feet = Vector2.Lerp(start.Feet, To, Frame.Progress);
            stand.Place(Feet, Scale);
            ShowPose(FlightProfile.Raise(Frame, Scale), FlightProfile.Scale(Frame));
            ShowShadow(FlightProfile.ShadowSize(Frame), FlightProfile.ShadowAlpha(Frame, miniature.StandingShadow));
            ShowOpacity(fading.HasValue ? Fade(now) : Appear(t));
            Landed = t >= 1;
        }

        // From a slot it rises out of the picture there; a planned recruit fades to its ghost's opacity as it lands.
        private float Appear(float t)
        {
            var rising = start.Pose.Lift >= 1 ? 1 : Mathf.Clamp01(t / FlightProfile.LiftEnd);
            var landing = Mathf.Clamp01((t - FlightProfile.LandingStart) / (1 - FlightProfile.LandingStart));
            return rising * Mathf.Lerp(1, landedOpacity, landing);
        }

        // A planned recruit whose plan was sent while it flies: it lands in full on the place the view gives it.
        public void Send(Func<Vector2> place)
        {
            Planned = false;
            destination = place;
            landedOpacity = 1;
        }

        // A recruit taken back, refused or not brought fades out where it is.
        public void End(double now)
        {
            if (fading.HasValue)
                return;
            fadeFrom = float.IsNaN(Opacity) ? 1 : Opacity;
            fading = now;
        }

        // Fading out: from the opacity it had when it began to fade, to nothing.
        private float Fade(double now) => fadeFrom * (1 - Mathf.Clamp01((float)(now - fading.Value) / FadeSeconds));

        public bool Ending => fading.HasValue;

        public bool Faded(double now) => fading.HasValue && now - fading.Value >= FadeSeconds;

        // A landed Muster recruit waits on its place until the map has shown it for a whole frame, so the stand that takes
        // over has been laid out before it goes.
        public bool Seen(bool shown)
        {
            seen = shown ? seen + 1 : 0;
            return seen >= 2;
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
            if (opacity == Opacity)
                return;
            Opacity = opacity;
            Root.style.opacity = opacity;
        }

        public void Remove() => stand.Remove();
    }
}
