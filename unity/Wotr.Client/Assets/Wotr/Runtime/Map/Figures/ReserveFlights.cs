using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-454 (owner, 2026-11-03): the recruits flying from the Reserve to the map (ReserveFlight), in the map's carry layer.
    // A Muster recruit is sent at once, so the view that brings it waits for its flight (MapFigureMotion.Bring): its place
    // shows faintly while it flies, as for any flight (UI-404), and the flight lands on that place and stays there until the
    // map shows the recruit standing; a rejection, a disconnect, a view that does not bring it or the 5 s expiry
    // (MapFigureMotion.Lapsed) fade it out. A planned recruit (a card's or an ability's plan) lands on its ghost's place and
    // its ghost shows it once it has landed (MapPlannedRecruits.Withhold); taking it back fades it out. Under Reduced motion
    // nothing flies, and a flight in the air lands at once.
    public sealed class ReserveFlights : IDisposable
    {
        private readonly MapCanvas map;
        private readonly MapCarryLayer layer;
        private readonly List<ReserveFlight> flights = new List<ReserveFlight>();
        private readonly IVisualElementScheduledItem timer;

        internal ReserveFlights(MapCanvas map, MapCarryLayer layer)
        {
            this.map = map;
            this.layer = layer;
            timer = map.schedule.Execute(Tick).Every(16);
            timer.Pause();
            map.Figures.Lapsed += Lapsed;
        }

        private static double Now => Time.realtimeSinceStartupAsDouble;

        public int Count => flights.Count;

        // A flight that landed and handed its recruit to the place it stands on, as its last frame was drawn.
        public event Action<ReserveFlightSnapshot> Landed;

        // The flight bringing this recruit (by unit id), as drawn last; null while none does.
        public ReserveFlightSnapshot? Flight(string unitId)
        {
            var flight = Find(unitId);
            return flight == null ? (ReserveFlightSnapshot?)null : flight.Snapshot;
        }

        // Whether figures fly now: not under Reduced motion (Skip animations).
        public bool Moving => !map.MotionReduced;

        // A recruit placed in `recruit.RegionId` flies from `start`: a planned one to its ghost's place, a sent one to the
        // place it will stand on. Nothing flies under Reduced motion, or for a recruit already flying.
        public void Launch(PlannedReinforcement recruit, Side side, bool planned, ReserveFlightStart start)
        {
            if (!Moving || Find(recruit.UnitId) != null || !map.Data.Shapes.ContainsKey(recruit.RegionId))
                return;
            var unit = new ArmyUnit(recruit.UnitId, recruit.Nation, recruit.Kind);
            var flight = new ReserveFlight(recruit.UnitId, recruit.RegionId, planned, FlightMiniature.Recruit(unit, side), start,
                Destination(recruit, side, planned), planned ? MapPlannedRecruits.GhostOpacity : 1, map, Now);
            flights.Add(flight);
            layer.Take(flight.Root);
            if (planned)
                map.PlannedRecruits.Withhold(recruit.UnitId);
            else
                map.Figures.Bring(flight.Key);
            flight.Layout(map, Now);
            timer.Resume();
        }

        // A planned recruit lands on its ghost's place; a sent one on its force's place once the map shows it there, and
        // until then where it will arrive.
        private Func<Vector2> Destination(PlannedReinforcement recruit, Side side, bool planned)
        {
            if (planned)
            {
                var ghost = map.Stands.Stands(recruit.RegionId).Planned;
                return () => ghost;
            }
            var arriving = map.RecruitPlace(recruit.RegionId, side);
            return () => map.UnitFeet(recruit.UnitId) ?? arriving;
        }

        // UI-454: the step that sends these recruits (by unit id) left while some of them still fly to their planned figure
        // (a ruler's last figure, a plan confirmed with ✓): they fly on to the place the view gives them, which waits for
        // them, as a Muster recruit's does.
        public void Send(IEnumerable<string> units)
        {
            foreach (var unit in units)
            {
                var flight = Find(unit);
                if (flight?.Planned != true || flight.Ending)
                    continue;
                var ghost = map.Stands.Stands(flight.Region).Planned;
                flight.Send(() => map.UnitFeet(unit) ?? ghost);
                map.Figures.Bring(flight.Key);
            }
        }

        // A planned recruit taken back: its flight fades out where it is.
        public void End(string unitId)
        {
            var flight = Find(unitId);
            if (flight?.Planned == true)
                flight.End(Now);
        }

        // The view will not bring these recruits (by figure key): their flights fade out.
        private void Lapsed(IReadOnlyCollection<string> keys)
        {
            var now = Now;
            foreach (var flight in flights)
                if (!flight.Planned && Contains(keys, flight.Key))
                    flight.End(now);
        }

        private static bool Contains(IReadOnlyCollection<string> keys, string key)
        {
            foreach (var candidate in keys)
                if (candidate == key)
                    return true;
            return false;
        }

        // Every camera change and every frame: each flight towards its place on the screen now.
        public void Layout()
        {
            var now = Now;
            foreach (var flight in flights)
                flight.Layout(map, now);
        }

        private void Tick()
        {
            var now = Now;
            var moving = Moving;
            for (var i = flights.Count - 1; i >= 0; i--)
            {
                var flight = flights[i];
                if (moving)
                    flight.Layout(map, now);
                if (moving && flight.Landed && !flight.Ending && Arrived(flight))
                {
                    var last = flight.Snapshot;
                    Finish(i);
                    Landed?.Invoke(last);
                }
                else if (!moving || flight.Faded(now))
                    Finish(i);
            }
            if (flights.Count == 0)
                timer.Pause();
        }

        // A planned recruit has arrived once it lands; a sent one once the map shows it standing there.
        private bool Arrived(ReserveFlight flight) => flight.Planned || flight.Seen(map.UnitFeet(flight.UnitId) != null);

        // The flight goes; the place it brought the recruit to shows it: its planned figure, or its force in full (each a
        // no-op when the flight was not the other kind).
        private void Finish(int index)
        {
            var flight = flights[index];
            flights.RemoveAt(index);
            flight.Remove();
            map.PlannedRecruits.Release(flight.UnitId);
            map.Figures.Brought(flight.Key);
        }

        private ReserveFlight Find(string unitId)
        {
            foreach (var flight in flights)
                if (flight.UnitId == unitId)
                    return flight;
            return null;
        }

        // Every flight lands at once (the board goes away, or the match is left).
        public void Clear()
        {
            for (var i = flights.Count - 1; i >= 0; i--)
                Finish(i);
            timer.Pause();
        }

        public void Dispose()
        {
            Clear();
            map.Figures.Lapsed -= Lapsed;
        }
    }
}
