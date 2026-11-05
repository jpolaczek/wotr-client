using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-329 (owner 2026-10-25: "when the opponent draws a card we need an animation that pulls a face-down card out of the
    // deck on the right and flies it into his lit card count, which flashes and changes"): the opponent's draw beat. The
    // hand count lights first; then, slightly staggered, each drawn card's back slides up out of its deck's plaque and flies,
    // eased and shrinking to the count's size, into the count — above the HUD, never taking the pointer and never showing an
    // identity. As a back lands the count flashes and its number steps; when the last has landed the beat holds the lit
    // count. Skip or a rebase ends it: the backs go and the release shows the latest state.
    // UI-487 (owner, 2026-11-04): the times of one flight and how far its piece rises as it leaves.
    public readonly struct FlightPace
    {
        public double Lead { get; }
        public double Pull { get; }
        public double Fly { get; }
        public double Hold { get; }
        public float Rise { get; }

        public FlightPace(double lead, double pull, double fly, double hold, float rise)
        {
            Lead = lead;
            Pull = pull;
            Fly = fly;
            Hold = hold;
            Rise = rise;
        }
    }

    public sealed class DrawFlight : IPresentationBeat
    {
        public const string BackClass = "presentation-draw-back";
        public const string FlashClass = "presentation-count-flash";
        public const double PullSeconds = .2;
        public const double FlySeconds = .6;
        public const double StaggerSeconds = .18;
        private const long FlashMilliseconds = 220;
        private const float Pull = 16;
        private static readonly Vector2 CardBack = new Vector2(34, 48);
        private readonly FlightPace pace;
        private readonly List<Flight> flights = new List<Flight>();
        // UI-344: a Corruption tile's back flies the same way, round and smaller.
        private readonly string backClass;
        private readonly Vector2 backSize;
        // UI-462 (owner, 2026-11-03): what a flying piece wears beyond its class (Sméagol's portrait, his artwork).
        private readonly Action<VisualElement> dress;
        private readonly VisualElement host;
        private readonly Func<VisualElement> count;
        private readonly Action<int> landed;
        private readonly Action done;
        private readonly Action end;
        private readonly double start;
        private double? last;
        private bool ended;

        private sealed class Flight
        {
            public VisualElement Origin;
            public VisualElement Back;
            public Vector2 Travel;
            public float Scale;
            public double Launch;
            public bool Landed;
        }

        // `origins` are the decks' plaques in the order of the cards; `landed` gets each landing's index, `done` follows the
        // last one, `end` the beat's end; `dress` fills each flying piece as it leaves.
        public DrawFlight(double now, VisualElement host, Func<VisualElement> count, IReadOnlyList<VisualElement> origins, Action light,
            Action<int> landed, Action done, Action end, string back = BackClass, Vector2? size = null, Action<VisualElement> dress = null,
            FlightPace? timing = null)
        {
            pace = timing ?? new FlightPace(PresentationPace.MarkSeconds, PullSeconds, FlySeconds, PresentationPace.HoldSeconds, Pull);
            backClass = back;
            backSize = size ?? CardBack;
            this.dress = dress;
            this.host = host;
            this.count = count;
            this.landed = landed;
            this.done = done;
            this.end = end;
            start = now;
            light();
            for (var index = 0; index < origins.Count; index++)
                flights.Add(new Flight { Origin = origins[index], Launch = pace.Lead + index * StaggerSeconds });
            Advance(now);
        }

        public bool Advance(double now)
        {
            if (ended)
                return true;
            var time = now - start;
            for (var index = 0; index < flights.Count; index++)
                Move(flights[index], index, time);
            if (last.HasValue && time >= last.Value + pace.Hold)
                Finish();
            return ended;
        }

        public void Finish()
        {
            if (ended)
                return;
            ended = true;
            foreach (var flight in flights)
                flight.Back?.RemoveFromHierarchy();
            end?.Invoke();
        }

        // The back appears at its deck when its card leaves, aimed at where the count stands then.
        private void Launch(Flight flight)
        {
            var target = count().worldBound;
            var from = Shown(flight.Origin) ? flight.Origin.worldBound : new Rect(target.center + new Vector2(0, 60), Vector2.zero);
            var back = new VisualElement { pickingMode = PickingMode.Ignore };
            back.AddToClassList(backClass);
            var centre = host.WorldToLocal(from.center);
            back.style.left = centre.x - backSize.x / 2;
            back.style.top = centre.y - backSize.y / 2;
            back.style.width = backSize.x;
            back.style.height = backSize.y;
            back.style.opacity = 0;
            dress?.Invoke(back);
            host.Add(back);
            host.BringToFront();
            flight.Back = back;
            flight.Travel = target.center - from.center;
            flight.Scale = Mathf.Clamp(target.height / backSize.y, .25f, 1);
        }

        // Out of the deck (a short slide up as it shows), then the eased flight into the count, shrinking; then it lands.
        private void Move(Flight flight, int index, double time)
        {
            if (flight.Landed)
                return;
            var local = time - flight.Launch;
            if (local < 0)
                return;
            if (flight.Back == null)
                Launch(flight);
            if (local < pace.Pull)
            {
                var pulled = (float)(local / pace.Pull);
                flight.Back.style.opacity = pulled;
                flight.Back.style.translate = new Translate(0, -pace.Rise * pulled);
                return;
            }
            var flown = Eased((local - pace.Pull) / pace.Fly);
            var lifted = new Vector2(0, -pace.Rise);
            var at = Vector2.Lerp(lifted, flight.Travel, flown);
            flight.Back.style.opacity = 1;
            flight.Back.style.translate = new Translate(at.x, at.y);
            flight.Back.style.scale = new Scale(Vector2.one * Mathf.Lerp(1, flight.Scale, flown));
            if (local < pace.Pull + pace.Fly)
                return;
            Land(flight, index, time);
        }

        private void Land(Flight flight, int index, double time)
        {
            flight.Landed = true;
            flight.Back.RemoveFromHierarchy();
            landed(index);
            Flash();
            if (flights.TrueForAll(item => item.Landed))
            {
                last = time;
                done();
            }
        }

        // The count the top bar shows now (it may have been redrawn with its new number).
        private void Flash()
        {
            var shown = count();
            shown.AddToClassList(FlashClass);
            shown.schedule.Execute(() => shown.RemoveFromClassList(FlashClass)).ExecuteLater(FlashMilliseconds);
        }

        private static float Eased(double t)
        {
            var x = Mathf.Clamp01((float)t);
            return x < .5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
        }

        private static bool Shown(VisualElement element) => element != null && element.panel != null &&
            element.resolvedStyle.display != DisplayStyle.None && element.worldBound.width > 0 && element.worldBound.height > 0;
    }
}
