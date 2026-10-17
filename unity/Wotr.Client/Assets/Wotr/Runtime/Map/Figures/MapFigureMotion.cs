using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Animations consume accepted public projections. They cannot send commands or hold up play.
    public sealed class MapFigureMotion : IDisposable
    {
        // UI-185/UI-192: the point under the feet of a flight's middle figure (Styles: .moving-figures).
        public static readonly Vector2 FlightFeet = new Vector2(32, 60);
        public VisualElement Root { get; } = new VisualElement { name = "map-figure-motion", pickingMode = PickingMode.Ignore };
        public int ActiveCount => flights.Count;
        public bool FellowshipArriving { get; private set; }
        public IReadOnlyList<BoardFigureMove> Moves { get; private set; } = Array.Empty<BoardFigureMove>();
        private readonly MapCanvas map;
        private readonly BoardFigureChanges changes = new BoardFigureChanges();
        private readonly List<Flight> flights = new List<Flight>();
        private readonly HashSet<(string, Side, bool)> arrivals = new HashSet<(string, Side, bool)>();
        private readonly HashSet<string> revealedRegions = new HashSet<string>();
        private readonly IVisualElementScheduledItem timer;
        private double started;
        private bool disposed;
        private bool observingFocus;

        public MapFigureMotion(MapCanvas map)
        {
            this.map = map;
            Root.AddToClassList("figure-motion-layer");
            Root.StretchToParentSize();
            map.Add(Root);
            timer = Root.schedule.Execute(Tick).Every(16);
            timer.Pause();
            Root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (!observingFocus && !disposed)
                {
                    Application.focusChanged += FocusChanged;
                    observingFocus = true;
                }
            });
        }

        public void SetConnected(bool connected)
        {
            changes.SetConnected(connected);
            if (!connected)
                Clear();
        }

        public void Apply(PlayerView view)
        {
            if (disposed)
                return;
            var current = changes.Current;
            var sameGame = current?.MatchId == view.MatchId && current.Viewer == view.You;
            if (sameGame && view.Revision <= current.Revision)
                return;
            var moves = changes.Apply(view, map.Data.Regions.ContainsKey);
            // UI-95: a projection that leaves every flying figure at its destination lets the flights finish.
            if (moves.Count == 0 && sameGame && FlightsHold(changes.Current))
                return;
            Present(moves);
        }

        private bool FlightsHold(BoardFigureSnapshot snapshot)
        {
            if (flights.Count == 0 || snapshot == null)
                return false;
            foreach (var flight in flights)
                if (!flight.Holds(snapshot))
                    return false;
            return true;
        }

        private void Present(IReadOnlyList<BoardFigureMove> moves)
        {
            Clear();
            Moves = moves;
            if (moves.Count == 0)
                return;
            started = Time.realtimeSinceStartupAsDouble;
            AddFlights(moves, ReducedMotion());
            map.Refresh();
            timer.Resume();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Explicit offline playback of recorded public views for rendering measurements.
        // This never changes a connection status or enables the command sender.
        public void PlayRecordedTransition(PlayerView before, PlayerView after)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(MapFigureMotion));
            if (before.MatchId != after.MatchId || before.You != after.You || after.Revision != before.Revision + 1)
                throw new ArgumentException("Figure playback requires consecutive public projections of one viewer.");
            Present(BoardFigureChanges.Difference(new BoardFigureSnapshot(before, map.Data.Regions.ContainsKey),
                new BoardFigureSnapshot(after, map.Data.Regions.ContainsKey)));
        }
#endif

        public bool Arriving(string region, Side side, bool garrison) => arrivals.Contains((region, side, garrison));
        public bool Reveals(string region) => revealedRegions.Contains(region);

        public void Reset()
        {
            changes.Reset();
            Clear();
        }

        private void AddFlights(IReadOnlyList<BoardFigureMove> moves, bool reduced)
        {
            var groups = new Dictionary<(string, string, Side, bool, bool), List<BoardFigureMove>>();
            foreach (var move in moves)
            {
                revealedRegions.Add(move.To.Region);
                if (move.To.Key == "fellowship")
                    FellowshipArriving = true;
                else
                    arrivals.Add((move.To.Region, move.To.Side, move.To.Garrison));
                var key = (move.From.Region, move.To.Region, move.To.Side, move.From.Garrison, move.To.Garrison);
                if (!groups.TryGetValue(key, out var group))
                    groups.Add(key, group = new List<BoardFigureMove>());
                group.Add(move);
            }
            foreach (var group in groups.Values)
            {
                var first = group[0];
                var from = BoardPoint(first.From);
                var to = BoardPoint(first.To);
                var arrival = reduced || first.IsArrival;
                var duration = arrival ? .45f : Mathf.Clamp(.65f + Vector2.Distance(from, to) / 1600f, .65f, 1.05f);
                var flight = new Flight(group, from, to, duration, arrival);
                flights.Add(flight);
                Root.Add(flight.Root);
            }
        }

        public void Layout()
        {
            var elapsed = (float)(Time.realtimeSinceStartupAsDouble - started);
            foreach (var flight in flights)
                flight.Layout(map.Camera, elapsed);
        }

        private void Tick()
        {
            if (ReducedMotion())
                foreach (var flight in flights)
                    flight.Reduce();
            Layout();
            var elapsed = Time.realtimeSinceStartupAsDouble - started;
            for (var i = flights.Count - 1; i >= 0; i--)
                if (elapsed >= flights[i].Duration)
                {
                    flights[i].Root.RemoveFromHierarchy();
                    flights.RemoveAt(i);
                }
            if (flights.Count == 0)
            {
                Moves = Array.Empty<BoardFigureMove>();
                arrivals.Clear();
                FellowshipArriving = false;
                timer.Pause();
                map.Refresh();
            }
        }

        private Vector2 BoardPoint(BoardFigurePosition position)
        {
            var anchor = map.Data.Shapes[position.Region].Anchor;
            return new Vector2(anchor[0], anchor[1]);
        }

        private bool ReducedMotion()
        {
            for (VisualElement element = map; element != null; element = element.parent)
                if (element.ClassListContains("reduced-motion"))
                    return true;
            return false;
        }

        private void FocusChanged(bool focused)
        {
            if (!focused)
                Reset();
        }

        private void Clear()
        {
            var hadArrivals = arrivals.Count > 0 || FellowshipArriving || revealedRegions.Count > 0;
            timer.Pause();
            Root.Clear();
            flights.Clear();
            arrivals.Clear();
            revealedRegions.Clear();
            FellowshipArriving = false;
            Moves = Array.Empty<BoardFigureMove>();
            if (hadArrivals && !disposed)
                map.Refresh();
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (observingFocus)
                Application.focusChanged -= FocusChanged;
            Reset();
            Root.RemoveFromHierarchy();
        }

        private sealed class Flight
        {
            public VisualElement Root { get; } = new VisualElement { pickingMode = PickingMode.Ignore };
            public float Duration { get; private set; }
            private readonly List<BoardFigureMove> group;
            private readonly Vector2 from;
            private readonly Vector2 to;
            private readonly Vector2 fromOffset;
            private readonly Vector2 toOffset;
            private bool reduced;
            // UI-185/UI-192: the flying figures match the map's larger figures (Styles: .moving-figures).
            private const float FigureStep = 20;
            private static readonly Vector2 GarrisonOffset = new Vector2(21, -12);

            public Flight(List<BoardFigureMove> group, Vector2 from, Vector2 to, float duration, bool reduced)
            {
                this.group = group;
                this.from = from;
                this.to = to;
                this.reduced = reduced;
                fromOffset = group[0].From.Garrison ? GarrisonOffset : Vector2.zero;
                toOffset = group[0].To.Garrison ? GarrisonOffset : Vector2.zero;
                Duration = duration;
                Root.name = "moving-" + group[0].To.Key;
                Root.AddToClassList("moving-figures");
                Root.EnableInClassList("figure-free-peoples", group[0].To.Side == Side.FreePeoples);
                Root.EnableInClassList("figure-shadow", group[0].To.Side == Side.Shadow);
                Root.EnableInClassList("figure-garrison", group[0].To.Garrison);
                var appearances = RepresentativeFigures(group);
                var visible = appearances.Count;
                for (var i = 0; i < visible; i++)
                {
                    var image = FigureArtwork.Image(appearances[i]);
                    image.style.left = (i - (visible - 1) * .5f) * FigureStep;
                    Root.Add(image);
                }
                if (group.Count > 1)
                {
                    var count = new Label(group.Count.ToString()) { pickingMode = PickingMode.Ignore };
                    count.AddToClassList("moving-figure-count");
                    Root.Add(count);
                }
                Root.EnableInClassList("figure-arrival", reduced);
            }

            private static List<FigureArt> RepresentativeFigures(List<BoardFigureMove> group)
            {
                var result = new List<FigureArt>(5);
                foreach (var move in group)
                    if (result.Count < 5 && !result.Contains(move.To.Appearance))
                        result.Add(move.To.Appearance);
                for (var i = 0; result.Count < 5 && result.Count < group.Count; i++)
                    result.Add(group[i].To.Appearance);
                return result;
            }

            public bool Holds(BoardFigureSnapshot snapshot)
            {
                foreach (var move in group)
                    if (!snapshot.Figures.TryGetValue(move.To.Key, out var position) ||
                        position.Region != move.To.Region || position.Garrison != move.To.Garrison)
                        return false;
                return true;
            }

            public void Reduce()
            {
                reduced = true;
                Root.AddToClassList("figure-arrival");
                Duration = Mathf.Min(Duration, .45f);
            }

            public void Layout(CameraState camera, float elapsed)
            {
                var t = Mathf.Clamp01(elapsed / Duration);
                var smooth = t * t * (3 - 2 * t);
                var position = camera.ToScreen(reduced ? to : Vector2.Lerp(from, to, smooth));
                position += reduced ? toOffset : Vector2.Lerp(fromOffset, toOffset, smooth);
                Root.transform.position = new Vector3(position.x - FlightFeet.x, position.y - FlightFeet.y, 0);
                Root.style.opacity = reduced ? 1 - t * .6f : 1;
            }
        }
    }
}
