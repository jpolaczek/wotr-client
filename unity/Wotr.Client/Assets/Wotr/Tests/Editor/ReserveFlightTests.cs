using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-454 (owner, 2026-11-03): a recruit placed by a click flies from its Reserve slot to its place. A Muster recruit is
    // sent at once: the view that brings it lets its Reserve flight show the arrival (no second arrival in place, its force
    // waiting faintly as for any flight) until the flight reports it brought; a view without it lapses it. A planned recruit's
    // ghost leaves it out while it flies and shows it once it lands.
    public sealed class ReserveFlightTests
    {
        private const string Region = "minas-tirith";

        [Test]
        public void AMusterRecruitArrivesWithItsReserveFlightNeverAgainInPlace()
        {
            var (view, map) = Board();
            var recruit = Recruit(view, "gondor", UnitKind.Elite);
            map.ReserveFlights.Launch(recruit, Side.FreePeoples, false, Slot());
            Assert.AreEqual(1, map.ReserveFlights.Count, "It flies from its slot.");
            var flight = map.ReserveFlights.Flight(recruit.UnitId).Value;
            Assert.IsFalse(flight.Planned);
            Assert.AreEqual(map.Camera.ToScreen(map.Q<Button>("army-minas-tirith-FreePeoples") is { } force
                ? ((MapStand)force.parent.userData).Feet : Vector2.zero), flight.To, "Towards Minas Tirith's force.");

            Place(view, map, recruit.UnitId);
            Assert.AreEqual(0, map.Figures.ActiveCount, "No arrival in place: the Reserve flight shows it.");
            Assert.IsTrue(map.Figures.Arriving(Region, Side.FreePeoples, false), "Its force waits faintly for the flight.");

            map.Figures.Brought(BoardFigurePosition.UnitPrefix + recruit.UnitId);
            Assert.IsFalse(map.Figures.Arriving(Region, Side.FreePeoples, false), "Landed: its force shows in full.");
        }

        [Test]
        public void AViewThatDoesNotBringTheRecruitLapsesIt()
        {
            var (view, map) = Board();
            var recruit = Recruit(view, "gondor", UnitKind.Leader);
            var lapsed = new List<string>();
            map.Figures.Lapsed += keys => lapsed.AddRange(keys);
            map.ReserveFlights.Launch(recruit, Side.FreePeoples, false, Slot());
            view.Units.First(u => u.Id == recruit.UnitId).Location = "casualties";
            view.Revision++;
            map.Data.Apply(view);
            map.UpdateData(map.Data);
            map.Figures.Apply(view);
            CollectionAssert.AreEqual(new[] { BoardFigurePosition.UnitPrefix + recruit.UnitId }, lapsed,
                "A lone leader removed at once never stands there: its flight fades out.");
            Assert.IsFalse(map.Figures.Arriving(Region, Side.FreePeoples, false));
        }

        [Test]
        public void APlannedRecruitsGhostShowsItOnlyOnceItsFlightLands()
        {
            var (view, map) = Board();
            var recruit = Recruit(view, "gondor", UnitKind.Regular);
            var placements = new List<PlannedReinforcement> { recruit };
            map.ReserveFlights.Launch(recruit, Side.FreePeoples, true, Slot());
            map.PlannedRecruits.Apply(placements, Side.FreePeoples, true);
            Assert.IsNull(map.Q<Button>("planned-recruit-" + Region), "No ghost while its only figure flies there.");
            var flight = map.ReserveFlights.Flight(recruit.UnitId).Value;
            Assert.IsTrue(flight.Planned);
            Assert.AreEqual(map.Camera.ToScreen(map.Stands.Stands(Region).Planned), flight.To, "Towards the ghost's place.");

            map.PlannedRecruits.Release(recruit.UnitId);
            Assert.IsNotNull(map.Q<Button>("planned-recruit-" + Region), "Landed: the ghost shows it.");
            map.ReserveFlights.End(recruit.UnitId);
        }

        [Test]
        public void TheFlightStartsOnTheSlotsPictureAndGrowsToTheFiguresScale()
        {
            var (view, map) = Board();
            var recruit = Recruit(view, "gondor", UnitKind.Elite);
            var figure = MapArmyArtwork.LoneFigure(FigureAppearances.Unit("gondor", UnitKind.Elite, Side.FreePeoples));
            var slot = new Rect(40, 300, 30, 30);
            var start = ReserveFlightStart.OnSlot(map, slot, figure.width / figure.height, figure, MapArmyArtwork.Feet(false, true));
            var drawn = Mathf.Min(slot.height, slot.width * figure.height / figure.width);
            var bottom = map.WorldToLocal(new Vector2(slot.center.x, slot.center.y + drawn / 2));
            Assert.AreEqual(drawn / figure.height, start.Scale, 1e-4f, "The miniature's figure is as tall as the slot's picture.");
            Assert.AreEqual(bottom.x, start.Feet.x, 1e-3f, "It stands on the picture's middle…");
            Assert.AreEqual(bottom.y, start.Feet.y + (figure.yMax - MapArmyArtwork.Feet(false, true).y) * start.Scale,
                1e-3f, "…its figure's feet on the picture's lower edge.");
            map.ReserveFlights.Launch(recruit, Side.FreePeoples, false, start);
            var flight = map.ReserveFlights.Flight(recruit.UnitId).Value;
            Assert.AreEqual(start.Feet, flight.Feet, "Its first frame stands on the slot.");
            Assert.AreEqual(start.Scale, flight.Scale, 1e-4f);
            Assert.AreEqual(0, flight.Opacity, .05f, "It rises out of the slot's picture.");
        }

        private static PlannedReinforcement Recruit(PlayerView view, string nation, UnitKind kind)
        {
            var unit = view.Units.First(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve");
            return new PlannedReinforcement { UnitId = unit.Id, Nation = nation, Kind = kind, RegionId = Region, RegionName = "Minas Tirith" };
        }

        private static ReserveFlightStart Slot() =>
            new ReserveFlightStart(new Vector2(40, 300), .5f, FlightStart.Of(Vector2.zero, FlightPose.Standing));

        private static void Place(PlayerView view, MapCanvas map, string unit)
        {
            view.Units.First(u => u.Id == unit).Location = Region;
            view.Revision++;
            map.Data.Apply(view);
            map.UpdateData(map.Data);
            map.Figures.Apply(view);
        }

        private static (PlayerView View, MapCanvas Map) Board()
        {
            var fixture = new BoardFixture();
            var data = BoardPresentation.Load();
            data.Apply(fixture.View);
            var map = new MapCanvas(data);
            map.Figures.SetConnected(true);
            map.Figures.Apply(fixture.View);
            return (fixture.View, map);
        }
    }
}
