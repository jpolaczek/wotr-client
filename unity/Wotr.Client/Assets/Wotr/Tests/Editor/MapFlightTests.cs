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
    // UI-404 (TABLETOP_FIGURES_UX, "Flight profile"): a flight runs between stand points — the place its figures stood on
    // before the view and the place they stand on now — one per destination marker, drawn as that marker's miniature. A
    // figure the player lifted hands its place and raised pose to the flight (Expect), so it never rises again from its
    // source; a rejection, a disconnect or the expiry puts it back on its base, where it stays while the draft still sends
    // it. A planned flight's figure flies from its faint figure at the destination. Outside a panel nothing animates.
    public sealed class MapFlightTests
    {
        private const string Region = "minas-tirith";
        private const string Target = "osgiliath";
        private const string Army = "army-minas-tirith-FreePeoples";

        [Test]
        public void AFlightRunsFromItsPreviousPlaceToItsPlaceNowOnePerDestinationMarker()
        {
            var (view, map, _) = Board();
            var source = Feet(map, Army);
            var striderSource = Feet(map, "character-strider");
            var rearGuard = view.Units.First(u => u.Location == Region && u.Kind == UnitKind.Leader);
            var movers = view.Units.Where(u => u.Location == Region && u.Id != rearGuard.Id).ToList();
            Move(view, map, movers.Select(u => u.Id).Append("strider"), Target);

            Assert.AreEqual(2, map.Figures.ActiveCount, "The army's units fly together, Strider on his own.");
            var army = map.Figures.Flight("unit:" + movers[0].Id).Value;
            Assert.AreEqual(army.From, map.Figures.Flight("unit:" + movers[1].Id).Value.From, "One flight for the force.");
            Assert.AreEqual(source, army.From, "It leaves from the force's place before the view.");
            Assert.AreEqual(Feet(map, "army-osgiliath-FreePeoples"), army.To, "It lands on Osgiliath's force's place.");
            Assert.AreEqual(source, Feet(map, Army), "The rear guard keeps its place.");
            var flight = map.Figures.Root.Q("moving-unit:" + movers[0].Id);
            Assert.AreEqual(movers.Count.ToString(), flight.Q<Label>("army-count").text, "Exactly the movers fly.");
            Assert.IsNotNull(flight.Q(className: "army-stand"), "The army's own miniature, on its base.");
            Assert.AreEqual(PickingMode.Ignore, flight.Q<Button>().pickingMode, "A flight never takes the pointer.");
            var strider = map.Figures.Flight("character:strider").Value;
            Assert.AreEqual(striderSource, strider.From, "Strider leaves from his own place…");
            Assert.AreEqual(Feet(map, "character-strider"), strider.To, "…and lands on his place beside the army.");
            Assert.IsNotNull(map.Figures.Root.Q("moving-character:strider").Q(className: "character-stand"));
            Assert.IsFalse(army.Arrival);
            Assert.AreEqual(FlightProfile.Duration(Vector2.Distance(army.From, army.To)), army.Duration, 1e-5f,
                "The duration bounds are unchanged.");
        }

        [Test]
        public void ALiftedArmyHandsItsRaisedPoseToTheFlightAndNeverRisesAgainFromItsSource()
        {
            var (view, map, context) = Board();
            var draft = Send(view, map, context);
            var source = Feet(map, Army);
            Assert.IsNotNull(map.Q("hand-" + Army), "Sent: it stays raised over its place.");
            var keys = view.Units.Where(u => u.Location == Region).Select(u => "unit:" + u.Id).ToList();
            Assert.IsTrue(keys.All(map.Figures.Expects), "Its flight is expected from the hand.");
            Assert.IsTrue(map.Figures.Expects("character:strider"));

            map.DraftOverlay.Apply(view, new TurnDraft(), context);
            Assert.IsNotNull(map.Q("hand-" + Army), "Accepted, the draft is done, but it waits raised for the view.");
            Move(view, map, view.Units.Where(u => u.Location == Region).Select(u => u.Id).Append("strider"), Target);

            Assert.IsEmpty(map.Query(className: "figure-hand-piece").ToList(), "The hand gives the figures to the flight.");
            var flight = map.Figures.Flight(keys[0]).Value;
            Assert.AreEqual(source, flight.From, "It goes on from where it was held…");
            Assert.AreEqual(1, flight.Frame.Lift, 1e-4f, "…in its raised pose: no second lift from the table.");
            Assert.IsTrue(map.Figures.Root.Q("moving-" + keys[0]).Q<Button>().ClassListContains("selected-force"),
                "It keeps the hand's gold rim.");
            Assert.AreEqual(1, map.Figures.Flight("character:strider").Value.Frame.Lift, 1e-4f, "Strider too.");
            Assert.IsFalse(keys.Any(map.Figures.Expects), "The flight took the expectation.");
            Assert.AreEqual(2, map.Figures.ActiveCount, "One flight each, never a second from the source.");
            Assert.IsNotNull(draft);
        }

        // UI-405: a carried army dropped on its target stays raised where it was dropped and its flight goes on from there.
        [Test]
        public void ADroppedArmyHandsTheDropPointToItsFlight()
        {
            var (view, map, context) = Board();
            var draft = new TurnDraft();
            Assert.IsTrue(ContextArmySelection.SelectAll(draft, view, context, Region, false));
            map.DraftOverlay.Apply(view, draft, context);
            var source = Feet(map, Army);
            Assert.IsTrue(map.GrabFigures(map.Camera.ToScreen(source)), "The raised army is taken up where its feet are.");
            var drop = Vector2.Lerp(source, Feet(map, "army-osgiliath-FreePeoples"), .8f);
            map.CarryFigures(map.Camera.ToScreen(drop));
            var movers = view.Units.Where(u => u.Location == Region).Select(u => u.Id).ToList();
            CollectionAssert.IsSubsetOf(movers, map.DropFigures(), "The drop releases what was carried.");
            Assert.IsTrue(ContextArmySelection.Target(draft, view, context, Target));
            map.DraftOverlay.Apply(view, draft, context);
            map.ExpectFlights(LiftedFigures.OnTheirWay(view, draft));
            map.HomeFigures();
            Assert.IsNotNull(map.Q("hand-" + Army), "Sent: it stays raised where it was dropped.");

            Move(view, map, movers.Append("strider"), Target);
            var flight = map.Figures.Flight("unit:" + movers[0]).Value;
            Assert.Less(Vector2.Distance(drop, flight.From), .01f, "It flies on from where it was dropped…");
            Assert.AreEqual(1, flight.Frame.Lift, 1e-4f, "…in its raised pose.");
            Assert.AreNotEqual(source, flight.From, "Never from its source.");
        }

        // UI-405: dropped where nothing takes it, a still-chosen army goes home over its place, so its step leaves from there.
        [Test]
        public void ACarriedArmyDroppedWhereNothingTakesItGoesHome()
        {
            var (view, map, context) = Board();
            var draft = new TurnDraft();
            Assert.IsTrue(ContextArmySelection.SelectAll(draft, view, context, Region, false));
            map.DraftOverlay.Apply(view, draft, context);
            var source = Feet(map, Army);
            Assert.IsTrue(map.GrabFigures(map.Camera.ToScreen(source)));
            map.CarryFigures(map.Camera.ToScreen(source) + new Vector2(140, 60));
            map.DropFigures();
            map.HomeFigures();
            Assert.IsNotNull(map.Q("hand-" + Army), "Still chosen, it stays raised.");
            Assert.IsTrue(ContextArmySelection.Target(draft, view, context, Target));
            map.DraftOverlay.Apply(view, draft, context);
            map.ExpectFlights(LiftedFigures.OnTheirWay(view, draft));
            var movers = view.Units.Where(u => u.Location == Region).Select(u => u.Id).ToList();
            Move(view, map, movers.Append("strider"), Target);
            var flight = map.Figures.Flight("unit:" + movers[0]).Value;
            Assert.AreEqual(source, flight.From, "Its step leaves from over its place, not from where it was dropped.");
            Assert.AreEqual(1, flight.Frame.Lift, 1e-4f, "Still raised: no second lift.");
        }

        [Test]
        public void ARejectionADisconnectOrTheExpiryPutsTheFiguresBackOnTheirBase()
        {
            var (view, map, context) = Board();
            var draft = Send(view, map, context);
            map.Figures.Lapse();
            AssertBack(map, "A rejection puts them back.");
            map.DraftOverlay.Apply(view, draft, context);
            AssertBack(map, "They stay down while the rejected draft still sends them.");

            (view, map, context) = Board();
            Send(view, map, context);
            map.Figures.SetConnected(false);
            AssertBack(map, "A disconnect puts them back.");

            (view, map, context) = Board();
            Send(view, map, context);
            map.Figures.Expire(Time.realtimeSinceStartupAsDouble + 1);
            Assert.IsNotNull(map.Q("hand-" + Army), "Within the expiry they wait.");
            map.Figures.Expire(Time.realtimeSinceStartupAsDouble + MapFigureMotion.ExpectSeconds + 1);
            AssertBack(map, "The expiry puts them back.");
            var movers = view.Units.Where(u => u.Location == Region).Select(u => u.Id).ToList();
            var source = Feet(map, Army);
            Move(view, map, movers, Target);
            var late = map.Figures.Flight("unit:" + movers[0]).Value;
            Assert.AreEqual(source, late.From, "A late view flies them from their base…");
            Assert.Less(late.Frame.Lift, .2f, "…rising from the table, as any flight.");
        }

        [Test]
        public void APlannedFlightsFigureFliesFromItsFaintFigureAtTheDestination()
        {
            var data = BoardPresentation.Load();
            var view = NazgulView();
            data.Apply(view);
            var map = new MapCanvas(data);
            map.Figures.SetConnected(true);
            map.Figures.Apply(view);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveShadowCharacters);
            Assert.IsTrue(NazgulFlightDraft.Arm(view, draft, "minas-morgul"));
            Assert.IsTrue(NazgulFlightDraft.Fly(view, draft, "moria", data));
            map.PlannedFlights.Apply(NazgulFlightDraft.Planned(view, draft), Side.Shadow, false);
            map.DraftOverlay.Apply(view, draft);
            var flown = draft.Payload.CompanionMoves.SelectMany(m => m.Ids).Single();
            map.ExpectFlights(LiftedFigures.OnTheirWay(view, draft));
            Assert.IsTrue(map.Figures.Expects("unit:" + flown));

            view.Units.First(u => u.Id == flown).Location = "moria";
            view.Revision++;
            data.Apply(view);
            map.UpdateData(data);
            map.Figures.Apply(view);
            var flight = map.Figures.Flight("unit:" + flown).Value;
            Assert.AreEqual(map.Stands.Stands("moria").Planned, flight.From, "It starts where its faint figure stood…");
            Assert.AreEqual(Feet(map, "army-moria-Shadow"), flight.To, "…and steps onto its place.");
            Assert.Less(flight.Frame.Lift, .2f, "It stood on the table: it rises from there.");
        }

        private static void AssertBack(MapCanvas map, string message)
        {
            Assert.IsEmpty(map.Query(className: "figure-hand-piece").ToList(), message);
            Assert.IsFalse(map.Q<Button>(Army).ClassListContains("lifted-away"), message);
            Assert.AreEqual(0, map.Figures.ActiveCount, message);
        }

        // The army chosen and sent to Osgiliath: it is raised in the hand and its flight expected.
        private static TurnDraft Send(PlayerView view, MapCanvas map, ActionDieContext context)
        {
            var draft = new TurnDraft();
            Assert.IsTrue(ContextArmySelection.SelectAll(draft, view, context, Region, false));
            map.DraftOverlay.Apply(view, draft, context);
            Assert.IsTrue(ContextArmySelection.Target(draft, view, context, Target));
            map.DraftOverlay.Apply(view, draft, context);
            map.ExpectFlights(LiftedFigures.OnTheirWay(view, draft));
            return draft;
        }

        // A view in which the given figures (unit or character ids) stand in `region`, applied as the next revision.
        private static void Move(PlayerView view, MapCanvas map, IEnumerable<string> ids, string region)
        {
            var moving = ids.ToHashSet();
            foreach (var unit in view.Units.Where(u => moving.Contains(u.Id)))
                unit.Location = region;
            foreach (var character in view.Characters.Where(c => moving.Contains(c.Id)))
                character.Location = region;
            view.Revision++;
            map.Data.Apply(view);
            map.UpdateData(map.Data);
            map.Figures.Apply(view);
        }

        // The board point a marker (by its button's name) stands on.
        private static Vector2 Feet(MapCanvas map, string marker) => ((MapStand)map.Q<Button>(marker).parent.userData).Feet;

        private static (PlayerView View, MapCanvas Map, ActionDieContext Context) Board()
        {
            var fixture = new BoardFixture();
            fixture.View.Characters.Add(new CharacterView
            {
                Id = "strider", DefinitionId = "base.character.strider", Side = Side.FreePeoples, Location = Region, Level = 3,
                Leadership = 1
            });
            fixture.Refresh();
            var data = BoardPresentation.Load();
            data.Apply(fixture.View);
            var context = new ActionDieContext();
            context.Sync(fixture.View, "army");
            var map = new MapCanvas(data);
            map.Figures.SetConnected(true);
            map.Figures.Apply(fixture.View);
            return (fixture.View, map, context);
        }

        private static PlayerView NazgulView() => new PlayerView
        {
            MatchId = "flights", You = Side.Shadow, DecisionOwner = Side.Shadow,
            Decision = new DecisionView { Id = "search", Kind = DecisionKind.CharacterEvent, SourceId = "base.sh.character.09" },
            Units =
            {
                new UnitView { Id = "nazgul-1", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "minas-morgul" },
                new UnitView { Id = "nazgul-2", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "minas-morgul" }
            },
            Options =
            {
                new ActionOption
                {
                    Kind = CommandKind.MoveShadowCharacters, Enabled = true, AllowEmptySelection = true,
                    EligibleIds = { "nazgul-1", "nazgul-2" },
                    Travel =
                    {
                        new TravelOption { Id = "nazgul-1", Origin = "minas-morgul", Flight = true, Targets = { "moria", "rohan" } },
                        new TravelOption { Id = "nazgul-2", Origin = "minas-morgul", Flight = true, Targets = { "moria", "rohan" } }
                    }
                }
            }
        };
    }
}
