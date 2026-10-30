using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Animations consume accepted public projections. They cannot send commands or hold up play.
    // UI-404 (TABLETOP_FIGURES_UX, "Flight profile"): every figure that changes its place flies as a miniature from the
    // place it stood on to the place it stands on now (FlightPlaces) — one flight per destination marker and source,
    // drawn as that marker's miniature at the figures' scale (FigureFlight) — rising, arcing and landing (FlightProfile).
    // A figure the player holds off the table (UI-403's hand, a planned ghost) is expected (Expect): its flight starts
    // where it is and as it is held, never from its source, and a projection that moves a flying figure again takes it
    // over where it is in the air (UI-95). Reduced motion and Skip are unchanged: an arrival in place, and the result at
    // once.
    public sealed class MapFigureMotion : IDisposable
    {
        // UI-404: how long a held figure waits for the view that flies it before it is put back on its base.
        public const double ExpectSeconds = 5;
        public VisualElement Root { get; } = new VisualElement { name = "map-figure-motion", pickingMode = PickingMode.Ignore };
        public int ActiveCount => flights.Count;
        public bool FellowshipArriving { get; private set; }
        // The moves of the flights in the air.
        public IReadOnlyList<BoardFigureMove> Moves { get; private set; } = Array.Empty<BoardFigureMove>();
        // UI-404: expected figures (by figure key) the view did not fly — a rejection, a disconnect, a reset or the expiry;
        // whoever held them puts them back on their base.
        public event Action<IReadOnlyCollection<string>> Lapsed;
        private readonly MapCanvas map;
        private readonly BoardFigureChanges changes = new BoardFigureChanges();
        private readonly List<FigureFlight> flights = new List<FigureFlight>();
        private readonly HashSet<(string, Side, bool)> arrivals = new HashSet<(string, Side, bool)>();
        // UI-402: the characters a flight brings, by id; they stand on their own, so their force's army does not wait.
        private readonly HashSet<string> characterArrivals = new HashSet<string>();
        // UI-338: the forces figures fly away from; their marker needs no fade of its own.
        private readonly HashSet<(string, Side, bool)> departures = new HashSet<(string, Side, bool)>();
        private readonly HashSet<string> revealedRegions = new HashSet<string>();
        // UI-404: where each expected figure starts, and until when it is expected.
        private readonly Dictionary<string, (FlightStart Start, double Until)> expected =
            new Dictionary<string, (FlightStart, double)>();
        private readonly IVisualElementScheduledItem timer;
        private readonly IVisualElementScheduledItem expiry;
        private bool disposed;
        // UI-306: a presentation flew moves and rebased on its staged view; the release that brings nothing new keeps
        // the presented result (its revealed destinations) instead of clearing it.
        private bool presented;
        private bool observingFocus;

        public MapFigureMotion(MapCanvas map)
        {
            this.map = map;
            Root.AddToClassList("figure-motion-layer");
            Root.StretchToParentSize();
            map.Add(Root);
            timer = Root.schedule.Execute(Tick).Every(16);
            timer.Pause();
            expiry = Root.schedule.Execute(() => Expire(Now)).Every(100);
            expiry.Pause();
            Root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (!observingFocus && !disposed)
                {
                    Application.focusChanged += FocusChanged;
                    observingFocus = true;
                }
            });
        }

        private static double Now => Time.realtimeSinceStartupAsDouble;

        public void SetConnected(bool connected)
        {
            changes.SetConnected(connected);
            if (connected)
                return;
            Clear();
            LapseAll();
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
            var released = presented;
            presented = false;
            // UI-95: a projection that leaves every flying figure at its destination lets the flights finish.
            if (moves.Count == 0 && sameGame && (released || FlightsHold(changes.Current)))
                return;
            var snapshot = changes.Current;
            Present(moves, flight => sameGame && flight.Holds(snapshot));
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
                new BoardFigureSnapshot(after, map.Data.Regions.ContainsKey)), _ => false);
        }
#endif

        // UI-306: recorded moves of a presented opponent action, flown on the presented board.
        public void Fly(IReadOnlyList<BoardFigureMove> moves)
        {
            if (!disposed)
                Present(moves, _ => true);
        }

        // UI-306: the staged view of a presentation is the baseline of the next projection, so its moves never replay.
        public void Rebase(PlayerView staged)
        {
            changes.Rebase(staged, map.Data.Regions.ContainsKey);
            presented = true;
        }

        // UI-306: Skip shows the latest state; the next projection moves nothing.
        public void Quiet() => changes.Quiet();

        // UI-306: a skipped presentation's flights end at once, at their destinations.
        public void Land() => Clear();

        public bool Arriving(string region, Side side, bool garrison) => arrivals.Contains((region, side, garrison));
        // UI-402: a character's own miniature waits, dimmed, for the flight that brings it.
        public bool ArrivingCharacter(string id) => characterArrivals.Contains(id);
        public bool Departing(string region, Side side, bool garrison) => departures.Contains((region, side, garrison));
        public bool Reveals(string region) => revealedRegions.Contains(region);

        // Whether a stand waits for a flight or a region shows figures for one, so the map lays out again when it ends.
        private bool ShowsArrivals =>
            arrivals.Count > 0 || characterArrivals.Count > 0 || FellowshipArriving || revealedRegions.Count > 0;

        // UI-404: where the flight carrying a figure (by its figure key) was drawn last; null while none carries it.
        public FlightSnapshot? Flight(string key)
        {
            var flight = flights.FirstOrDefault(candidate => candidate.Carries(key));
            return flight == null ? (FlightSnapshot?)null : new FlightSnapshot(flight);
        }

        public void Reset()
        {
            changes.Reset();
            Clear();
            LapseAll();
        }

        // UI-404: figures (by figure key) held off the table at a board point — lifted in place, or dropped on their
        // target — whose next flight starts there, in that pose, never from their source. Kept for ExpectSeconds.
        public void Expect(IEnumerable<string> keys, Vector2 point, FlightPose pose) => Expect(keys, FlightStart.Of(point, pose));

        // UI-428: a start with its own height above the raised pose (a dropped figure held higher, MapHandPiece).
        public void Expect(IEnumerable<string> keys, FlightStart start)
        {
            var until = Now + ExpectSeconds;
            foreach (var key in keys)
                expected[key] = (start, until);
            if (expected.Count > 0)
                expiry.Resume();
        }

        public bool Expects(string key) => expected.ContainsKey(key);

        // UI-404: the step that sent the expected figures was rejected: none of them will fly.
        public void Lapse() => LapseAll();

        // The expiry check; the map's schedule runs it with the real time.
        public void Expire(double now)
        {
            var due = expected.Where(pair => pair.Value.Until <= now).Select(pair => pair.Key).ToList();
            foreach (var key in due)
                expected.Remove(key);
            Ended(due);
        }

        private void LapseAll()
        {
            var keys = expected.Keys.ToList();
            expected.Clear();
            Ended(keys);
        }

        private void Ended(IReadOnlyCollection<string> keys)
        {
            if (expected.Count == 0)
                expiry.Pause();
            if (keys.Count > 0 && !disposed)
                Lapsed?.Invoke(keys);
        }

        // A projection's moves: the flights of figures it moves again end where they are and new ones take over from there;
        // the others go on while `keep` holds them. Nothing is laid out again for a projection that changes nothing.
        private void Present(IReadOnlyList<BoardFigureMove> moves, Func<FigureFlight, bool> keep)
        {
            var hadArrivals = ShowsArrivals;
            var inAir = TakeOver(moves, keep);
            revealedRegions.Clear();
            foreach (var flight in flights)
                revealedRegions.Add(flight.Moves[0].To.Region);
            AddFlights(moves, inAir, ReducedMotion());
            Track();
            if (flights.Count > 0)
                timer.Resume();
            else
                timer.Pause();
            if (moves.Count > 0 || hadArrivals)
                map.Refresh();
        }

        // Ends every flight a new move takes over (returning where its figures are) and every flight `keep` lets go.
        private Dictionary<string, FlightStart> TakeOver(IReadOnlyList<BoardFigureMove> moves, Func<FigureFlight, bool> keep)
        {
            var moving = new HashSet<string>(moves.Select(move => move.To.Key));
            var inAir = new Dictionary<string, FlightStart>();
            for (var i = flights.Count - 1; i >= 0; i--)
            {
                var flight = flights[i];
                if (flight.Takes(moving))
                    foreach (var move in flight.Moves)
                        inAir[move.To.Key] = flight.Current();
                else if (keep(flight))
                    continue;
                flight.Remove();
                flights.RemoveAt(i);
            }
            return inAir;
        }

        private void AddFlights(IReadOnlyList<BoardFigureMove> moves, Dictionary<string, FlightStart> inAir, bool reduced)
        {
            var now = Now;
            foreach (var group in Groups(moves))
            {
                revealedRegions.Add(group[0].To.Region);
                var start = Start(group, inAir, out var chosen);
                var to = map.FlightPlaces.Destination(group[0].To);
                var flight = new FigureFlight(group, Miniature(group, chosen), start, to, reduced || group[0].IsArrival, now);
                flights.Add(flight);
                Root.Add(flight.Root);
                flight.Layout(map, now);
            }
        }

        // One flight per destination marker and source: a force's units together, each character and the Fellowship alone.
        private static IEnumerable<List<BoardFigureMove>> Groups(IEnumerable<BoardFigureMove> moves)
        {
            var groups = new Dictionary<string, List<BoardFigureMove>>();
            foreach (var move in moves)
            {
                var key = move.To.IsUnit
                    ? string.Join("|", move.From.Region, move.From.Garrison, move.To.Region, move.To.Side, move.To.Garrison)
                    : move.To.Key;
                if (!groups.TryGetValue(key, out var group))
                    groups.Add(key, group = new List<BoardFigureMove>());
                group.Add(move);
            }
            return groups.Values;
        }

        // Where a flight begins: where a flight it takes over left its figures, where the player holds them, or standing
        // on their source's place. `chosen`: the hand held them, so they keep its selected rim.
        private FlightStart Start(List<BoardFigureMove> group, Dictionary<string, FlightStart> inAir, out bool chosen)
        {
            chosen = false;
            FlightStart? start = null;
            foreach (var move in group)
            {
                if (start == null && inAir.TryGetValue(move.To.Key, out var air))
                    start = air;
                if (expected.TryGetValue(move.To.Key, out var held))
                {
                    expected.Remove(move.To.Key);
                    chosen |= held.Start.Lift > 0;
                    start ??= held.Start;
                }
            }
            if (expected.Count == 0)
                expiry.Pause();
            return start ?? FlightStart.Of(map.FlightPlaces.Source(group[0].From), FlightPose.Standing);
        }

        private FlightMiniature Miniature(List<BoardFigureMove> group, bool chosen)
        {
            var to = group[0].To;
            if (to.IsFellowship)
                return FlightMiniature.Fellowship(map.FellowshipMarker?.Hidden ?? true);
            if (to.IsCharacter)
                return FlightMiniature.Character(to, chosen);
            var joined = map.Data.Armies.FirstOrDefault(army => army.Is(to.Region, to.Side, to.Garrison));
            return FlightMiniature.Army(group, chosen, joined);
        }

        // What the stands read while flights are in the air: which markers wait for a flight and which it leaves.
        private void Track()
        {
            arrivals.Clear();
            characterArrivals.Clear();
            departures.Clear();
            FellowshipArriving = false;
            var moves = new List<BoardFigureMove>();
            foreach (var flight in flights)
                foreach (var move in flight.Moves)
                {
                    moves.Add(move);
                    Track(move);
                }
            Moves = moves;
        }

        private void Track(BoardFigureMove move)
        {
            if (move.To.IsFellowship)
                FellowshipArriving = true;
            else if (move.To.IsCharacter)
                characterArrivals.Add(move.To.FigureId);
            else
                arrivals.Add((move.To.Region, move.To.Side, move.To.Garrison));
            if (!move.IsArrival && !move.To.IsCharacter)
                departures.Add((move.From.Region, move.From.Side, move.From.Garrison));
        }

        public void Layout()
        {
            var now = Now;
            foreach (var flight in flights)
                flight.Layout(map, now);
        }

        private void Tick()
        {
            if (ReducedMotion())
                foreach (var flight in flights)
                    flight.Reduce();
            var landed = RemoveSettled();
            Layout();
            if (!landed)
                return;
            Track();
            if (flights.Count == 0)
                timer.Pause();
            map.Refresh();
        }

        // UI-404: a flight leaves once its last frame — the figures standing on their place — has been drawn, so the stand
        // that takes over shows the same picture whatever the frame rate.
        private bool RemoveSettled()
        {
            var landed = false;
            for (var i = flights.Count - 1; i >= 0; i--)
                if (flights[i].Settled)
                {
                    flights[i].Remove();
                    flights.RemoveAt(i);
                    landed = true;
                }
            return landed;
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
            var hadArrivals = ShowsArrivals;
            timer.Pause();
            foreach (var flight in flights)
                flight.Remove();
            Root.Clear();
            flights.Clear();
            revealedRegions.Clear();
            Track();
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
    }

    // UI-404: one flight as it was drawn last (map-local points, px), for the board's checks and tests.
    public readonly struct FlightSnapshot
    {
        // The ground under the figures' feet, where their shadow lies, and how high their feet stand above it.
        public readonly Vector2 Ground;
        public readonly float Height;
        // 0 standing, 1 the raised pose; the arc and the landing's settle; the share of the profile shown.
        public readonly FlightFrame Frame;
        // The board points it flies between: its start (a stand point, or where a held or flying figure was) and the
        // destination's stand point.
        public readonly Vector2 From;
        public readonly Vector2 To;
        public readonly bool Arrival;
        public readonly float Duration;

        internal FlightSnapshot(FigureFlight flight)
        {
            Ground = flight.Ground;
            Height = flight.Height;
            Frame = flight.Frame;
            From = flight.From;
            To = flight.To;
            Arrival = flight.Arrival;
            Duration = flight.Duration;
        }
    }
}
