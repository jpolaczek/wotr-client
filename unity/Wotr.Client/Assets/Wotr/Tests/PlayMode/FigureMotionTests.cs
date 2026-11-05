using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public sealed class FigureMotionTests
    {
        [UnityTest]
        public IEnumerator AcceptedProjectionHasAnIntermediatePositionAndLeavesMapInputLive()
        {
            var fixture = new BoardFixture(true);
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            client.Board.SetConnection("Connected");
            yield return Settle();
            var board = client.Board;
            var map = board.Map;
            map.Focus("minas-tirith");
            map.Camera.Zoom = map.FitZoom;
            map.Refresh();
            var moved = fixture.View.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular);
            var remaining = fixture.View.Units.First(u => u.Location == "minas-tirith" && u.Id != moved.Id);
            var key = "unit:" + moved.Id;
            moved.Location = "osgiliath";
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            Assert.AreEqual(1, map.Figures.ActiveCount);
            var moving = map.Figures.Root.Q("moving-" + key);
            Assert.IsNotNull(moving);
            Assert.AreEqual(PickingMode.Ignore, moving.pickingMode);
            Assert.AreEqual("minas-tirith", remaining.Location);
            Assert.AreEqual("osgiliath", board.View.Units.First(u => u.Id == moved.Id).Location);
            var start = Ground(map, key);
            yield return Settle();
            LayoutTests.Save(client.Viewport, "ui76-motion-start.png");
            yield return new WaitForSecondsRealtime(.22f);
            var middle = Ground(map, key);
            var destination = map.Camera.ToScreen(map.Figures.Flight(key).Value.To);
            Assert.AreEqual(StandFeet(map, "army-osgiliath-FreePeoples"), map.Figures.Flight(key).Value.To,
                "UI-404: it lands on the destination force's place.");
            Assert.Greater(Vector2.Distance(start, middle), 2, "The figurine visibly leaves its source.");
            Assert.Greater(Vector2.Distance(middle, destination), 2, "The intermediate frame must not already be the result.");
            Assert.Less(Vector2.Distance(middle, destination), Vector2.Distance(start, destination));
            LayoutTests.Save(client.Viewport, "ui76-motion-middle.png");
            var over = middle - new Vector2(0, 20 + map.Figures.Flight(key).Value.Height);
            var picked = map.panel.Pick(map.LocalToWorld(over));
            Assert.IsFalse(picked == moving || moving.Contains(picked), "Motion overlays must never intercept input.");
            var publicRevision = board.View.Revision;
            map.Select("pelargir");
            Assert.AreEqual(publicRevision, board.View.Revision, "Inspecting while a figurine moves sends no action.");
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.AreEqual(0, map.Figures.ActiveCount);
            Assert.IsNull(map.Figures.Root.Q("moving-unit:" + moved.Id));
            Assert.IsFalse(map.Figures.Arriving("osgiliath", Side.FreePeoples, false));
            Assert.IsTrue(map.Figures.Reveals("osgiliath"));
            Assert.IsFalse(map.MarkerVisibility.Detail);
            Assert.AreEqual(DisplayStyle.Flex, map.Q<Button>("army-osgiliath-FreePeoples").resolvedStyle.display);
            LayoutTests.Save(client.Viewport, "ui76-motion-finished.png");
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            Assert.IsFalse(map.Figures.Reveals("osgiliath"), "The next projection clears arrival visibility without replay.");
        }

        [UnityTest]
        public IEnumerator ReducedMotionReconnectAndRevisionGapsShowResultsWithoutReplayingTravel()
        {
            var fixture = new BoardFixture();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            client.Board.SetConnection("Connected");
            client.Interface.Preferences.SetReducedMotion(true);
            yield return Settle();
            var map = client.Board.Map;
            var unit = fixture.View.Units.First(u => u.Location == "minas-tirith");
            var key = "unit:" + unit.Id;
            unit.Location = "osgiliath";
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            var accent = map.Figures.Root.Q("moving-" + key);
            Assert.IsNotNull(accent);
            Assert.IsTrue(accent.ClassListContains("figure-arrival"));
            var place = map.Camera.ToScreen(StandFeet(map, "army-osgiliath-FreePeoples"));
            Assert.Less(Vector2.Distance(Ground(map, key), place), .1f, "It shows on the destination's place at once.");
            Assert.AreEqual(0, map.Figures.Flight(key).Value.Height, .01f, "Nothing rises under Reduced motion.");
            yield return new WaitForSecondsRealtime(.12f);
            Assert.Less(Vector2.Distance(Ground(map, key), place), .1f);
            Assert.AreEqual(0, map.Figures.Flight(key).Value.Height, .01f);
            client.Board.SetConnection("Reconnecting");
            Assert.AreEqual(0, map.Figures.ActiveCount);
            unit.Location = "pelargir";
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            client.Board.SetConnection("Connected");
            client.Refresh(fixture.View);
            Assert.AreEqual(0, map.Figures.ActiveCount);
            fixture.View.Revision += 2;
            unit.Location = "minas-tirith";
            client.Refresh(fixture.View);
            Assert.AreEqual(0, map.Figures.ActiveCount);
        }

        [UnityTest]
        public IEnumerator PublicCharacterMovesButHiddenProgressDoesNotAndOnlyFigureChangesReplaceTheFlight()
        {
            var fixture = new BoardFixture(true);
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            client.Board.SetConnection("Connected");
            yield return Settle();
            var map = client.Board.Map;
            var character = fixture.View.Characters.First();
            character.Location = "osgiliath";
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            Assert.AreEqual(1, map.Figures.ActiveCount);
            Assert.IsNotNull(map.Figures.Root.Q("moving-character:" + character.Id));
            yield return new WaitForSecondsRealtime(.15f);
            var flight = map.Figures.Root.Q("moving-character:" + character.Id);
            fixture.View.Revision++;
            fixture.View.Fellowship.Progress++;
            client.Refresh(fixture.View);
            Assert.AreEqual(1, map.Figures.ActiveCount, "UI-95: hidden progress makes no flight and lets the running one finish.");
            Assert.AreSame(flight, map.Figures.Root.Q("moving-character:" + character.Id));
            yield return new WaitForSecondsRealtime(.2f);
            var key = "character:" + character.Id;
            var inAir = map.Figures.Flight(key).Value;
            Assert.Greater(inAir.Frame.Progress, 0, "Mid-air.");
            character.Location = "pelargir";
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            Assert.AreEqual(1, map.Figures.ActiveCount, "A new move of the figure replaces its flight.");
            Assert.AreNotSame(flight, map.Figures.Root.Q("moving-character:" + character.Id));
            var takenOver = map.Figures.Flight(key).Value;
            var oldFrom = map.Camera.ToScreen(inAir.From);
            var oldPath = map.Camera.ToScreen(inAir.To) - oldFrom;
            var from = map.Camera.ToScreen(takenOver.From);
            var along = Vector2.Dot(from - oldFrom, oldPath) / oldPath.sqrMagnitude;
            Assert.Less(Vector2.Distance(from, oldFrom + oldPath * along), 1,
                "UI-404: the new flight takes the figure over on its way, where it is in the air…");
            Assert.GreaterOrEqual(along, inAir.Frame.Progress - .01f, "…at least as far as it had come…");
            Assert.Greater(Vector2.Distance(inAir.To, takenOver.From), 5, "…not at its old destination.");
            Assert.AreEqual(1, takenOver.Frame.Lift, 1e-4f, "It stays up; it does not drop to the table first.");
            Assert.AreEqual(StandFeet(map, "character-" + character.Id), takenOver.To, "It lands on its new place.");
            fixture.View.Characters.Remove(character);
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            Assert.AreEqual(0, map.Figures.ActiveCount, "A figure that left the board ends its flight.");
            Assert.IsEmpty(map.Figures.Root.Children());
        }

        // UI-404 (TABLETOP_FIGURES_UX, "Flight profile"): the movers rise from their place as the army's own miniature at
        // its scale, fly in an arc with their shadow on the ground and land on the destination force's place with no snap;
        // the rear guard stays on its base.
        [UnityTest]
        public IEnumerator UI404AMarchRisesArcsAndLandsOnItsPlace()
        {
            var fixture = new BoardFixture();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            client.Board.SetConnection("Connected");
            yield return Settle();
            var map = client.Board.Map;
            client.Board.Hover.Close();
            var source = StandFeet(map, "army-minas-tirith-FreePeoples");
            var target = StandFeet(map, "army-osgiliath-FreePeoples");
            map.FocusOn((source + target) / 2, map.FitZoom * 2.4f);
            yield return Settle();
            var movers = fixture.View.Units.Where(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular).ToList();
            var key = "unit:" + movers[0].Id;
            foreach (var unit in movers)
                unit.Location = "osgiliath";
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            Assert.AreEqual(1, map.Figures.ActiveCount, "One flight for the force's movers.");
            var flight = map.Figures.Root.Q("moving-" + key);
            Assert.AreEqual("2", flight.Q<Label>("army-count").text, "The movers only; the leader stays behind.");
            Assert.AreEqual(source, map.Figures.Flight(key).Value.From, "It leaves from the force's place.");
            Assert.AreEqual(target, map.Figures.Flight(key).Value.To, "It lands on the destination force's place.");
            var frames = new FlightFrames(map, key, client, "ui404-march");
            while (map.Figures.ActiveCount > 0)
            {
                frames.Observe();
                yield return null;
            }
            frames.AssertProfile(map.Camera.ToScreen(source), map.Camera.ToScreen(target));
            var stand = map.Q<Button>("army-osgiliath-FreePeoples");
            Assert.Less(Vector2.Distance(frames.Last.Ground, map.Camera.ToScreen(target)), .5f, "Its last frame is on the place…");
            Assert.AreEqual(0, frames.Last.Height, .01f, "…standing on the table, so nothing snaps.");
            Assert.AreEqual(frames.Scale, stand.parent.worldBound.width / stand.parent.resolvedStyle.width, .002f,
                "The same miniature at the same scale as the stand it becomes.");
            Assert.AreEqual(1, stand.resolvedStyle.opacity, .001f, "The destination shows its force at once.");
            Assert.AreEqual(source, StandFeet(map, "army-minas-tirith-FreePeoples"), "The rear guard keeps its place.");
            LayoutTests.Save(client.Viewport, "ui404-landed.png");
        }

        // UI-404: a click-lifted army that is sent hands its raised pose to the flight: when the view takes it, the same
        // miniature goes on from where it was held — it never drops and rises again from its source.
        [UnityTest]
        public IEnumerator UI404AClickLiftedArmyFliesOnFromItsRaisedPose()
        {
            var fixture = new BoardFixture();
            fixture.View.Characters.Add(new CharacterView
            {
                Id = "strider", DefinitionId = "base.character.strider", Side = Side.FreePeoples, Location = "minas-tirith",
                Level = 3, Leadership = 1
            });
            fixture.Refresh();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            client.Board.SetConnection("Connected");
            yield return Settle();
            var map = client.Board.Map;
            client.Board.Hover.Close();
            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("army"));
            yield return Settle();
            var source = StandFeet(map, "army-minas-tirith-FreePeoples");
            map.FocusOn((source + StandFeet(map, "army-osgiliath-FreePeoples")) / 2, map.FitZoom * 2.4f);
            yield return Settle();
            PointerGestureEvents.ClickAt(map.Q<Button>("army-minas-tirith-FreePeoples"));
            yield return Settle();
            client.Board.Hover.Close();
            map.Select("osgiliath");
            yield return new WaitForSecondsRealtime(.45f);
            var piece = map.Q("hand-army-minas-tirith-FreePeoples");
            Assert.IsNotNull(piece, "Sent: the army stays raised over its place.");
            var raised = piece.Q("hand-lift").worldBound;
            LayoutTests.Save(client.Viewport, "ui404-handoff-raised.png");
            var ids = fixture.View.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).Append("strider").ToList();
            // What TurnInteraction does as the step leaves (this client is offline, so it sends nothing itself).
            map.ExpectFlights(ids);
            var key = "unit:" + ids[0];
            foreach (var unit in fixture.View.Units.Where(u => ids.Contains(u.Id)))
                unit.Location = "osgiliath";
            fixture.View.Characters.Single().Location = "osgiliath";
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            Assert.IsEmpty(map.Query(className: "figure-hand-piece").ToList(), "The hand gives the army to the flight.");
            Assert.AreEqual(2, map.Figures.ActiveCount, "The army and Strider, each once.");
            Assert.AreEqual(source, map.Figures.Flight(key).Value.From, "From where it was held…");
            Assert.AreEqual(1, map.Figures.Flight(key).Value.Frame.Lift, 1e-4f, "…in its raised pose.");
            Assert.AreEqual(1, map.Figures.Flight("character:strider").Value.Frame.Lift, 1e-4f);
            yield return null;
            var flying = map.Figures.Root.Q("moving-" + key).Q("flight-lift").worldBound;
            Assert.Less(Vector2.Distance(raised.center, flying.center), 6, "The same miniature goes on from the same spot.");
            Assert.AreEqual(raised.width, flying.width, 1, "At the same size: no drop to the table.");
            LayoutTests.Save(client.Viewport, "ui404-handoff-start.png");
            var frames = new FlightFrames(map, key, client, "ui404-handoff");
            var lowest = float.MaxValue;
            while (map.Figures.ActiveCount > 0)
            {
                if (map.Figures.Flight(key) is { } now && now.Frame.Progress < .5f)
                    lowest = Mathf.Min(lowest, now.Frame.Lift);
                frames.Observe();
                yield return null;
            }
            Assert.AreEqual(1, lowest, 1e-4f, "It never comes down before it lands.");
            frames.AssertProfile(map.Camera.ToScreen(source), map.Camera.ToScreen(StandFeet(map, "army-osgiliath-FreePeoples")));
            Assert.IsEmpty(map.Query(className: "figure-hand-piece").ToList());
            Assert.AreEqual(1, map.Q<Button>("army-osgiliath-FreePeoples").resolvedStyle.opacity, .001f);
            LayoutTests.Save(client.Viewport, "ui404-handoff-landed.png");
        }

        [UnityTest]
        public IEnumerator AllTwelveIllustrationsFitStableButtonTargetsAtBothScales()
        {
            using var client = new MountedMatchClient("http://localhost", "artwork-layout");
            var gallery = new VisualElement { name = "figure-gallery" };
            gallery.style.position = Position.Absolute;
            gallery.style.left = 160;
            gallery.style.top = 160;
            gallery.style.width = 420;
            gallery.style.flexDirection = FlexDirection.Row;
            gallery.style.flexWrap = Wrap.Wrap;
            foreach (FigureAppearance appearance in System.Enum.GetValues(typeof(FigureAppearance)))
            {
                var button = new Button { name = "art-" + appearance, text = FigureAppearances.Badge(appearance) };
                button.AddToClassList("figure");
                var side = appearance is FigureAppearance.ShadowRegular or FigureAppearance.ShadowElite or
                    FigureAppearance.ShadowLeader or FigureAppearance.Nazgul or FigureAppearance.Minion ? Side.Shadow : Side.FreePeoples;
                FigureArtwork.Apply(button, appearance, side);
                gallery.Add(button);
            }
            client.Board.Root.Add(gallery);
            foreach (var scale in new[] { 1f, 1.25f })
            {
                client.Panel.scale = scale;
                yield return Settle();
                foreach (var button in gallery.Query<Button>().ToList())
                {
                    var art = button.Q<Image>("figure-art");
                    Assert.IsNotNull(art.sprite);
                    Assert.GreaterOrEqual(button.worldBound.width, 32);
                    Assert.GreaterOrEqual(button.worldBound.height, 32);
                    Assert.Greater(art.worldBound.width, 16);
                    Assert.Greater(art.worldBound.height, 16);
                    var picked = button.panel.Pick(button.worldBound.center);
                    Assert.IsTrue(picked == button || button.Contains(picked), button.name);
                    Assert.AreEqual(PickingMode.Ignore, art.pickingMode);
                }
                LayoutTests.Save(client.Viewport, $"ui76-figure-atlas-{scale:0.00}.png");
            }
        }

        // UI-404: the map-local point under a flight's feet, on the ground.
        private static Vector2 Ground(MapCanvas map, string key) => map.Figures.Flight(key).Value.Ground;

        // The board point a marker (by its button's name) stands on.
        internal static Vector2 StandFeet(MapCanvas map, string marker) => ((MapStand)map.Q<Button>(marker).parent.userData).Feet;

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }

        // UI-404: an army's flight frame by frame as it was drawn — its ground, its height, its shadow on the ground and
        // its miniature above it — with the lift, the arc's top and the landing saved as pictures.
        private sealed class FlightFrames
        {
            // The army miniature's design box, its feet and its shadow's middle (MapArmyArtwork, Styles/Armies.uss).
            private static readonly Vector2 BoxMiddle = new Vector2(33, 43);
            private static readonly Vector2 Feet = new Vector2(33, 64);
            private static readonly Vector2 ShadowMiddle = new Vector2(37, 72.5f);
            private readonly MapCanvas map;
            private readonly string key;
            private readonly MountedMatchClient client;
            private readonly string prefix;
            private readonly List<FlightSnapshot> seen = new List<FlightSnapshot>();
            private readonly HashSet<string> saved = new HashSet<string>();

            public FlightFrames(MapCanvas map, string key, MountedMatchClient client, string prefix)
            {
                this.map = map;
                this.key = key;
                this.client = client;
                this.prefix = prefix;
            }

            public FlightSnapshot Last => seen[seen.Count - 1];
            public float Scale { get; private set; }

            public void Observe()
            {
                if (!(map.Figures.Flight(key) is { } flight))
                    return;
                seen.Add(flight);
                var root = map.Figures.Root.Q("moving-" + key);
                Scale = root.worldBound.width / root.resolvedStyle.width;
                AssertOnTheGround(root, flight);
                AssertAboveItsShadow(root, flight);
                var frame = flight.Frame;
                if (frame.Progress == 0 && frame.Lift > .2f && frame.Lift < 1)
                    Save("lift");
                if (frame.Progress > .4f && frame.Progress < .6f)
                    Save("arc-top");
                if (frame.Progress >= 1 && (frame.Lift < 1 || frame.Settle > 0))
                    Save("landing");
            }

            private void Save(string phase)
            {
                if (saved.Add(phase))
                    LayoutTests.Save(client.Viewport, prefix + "-" + phase + ".png");
            }

            // The shadow lies on the ground under the figures' feet, never in the air.
            private void AssertOnTheGround(VisualElement root, FlightSnapshot flight)
            {
                var shadow = root.Q("flight-shadow").worldBound.center;
                var expected = map.LocalToWorld(flight.Ground + (ShadowMiddle - Feet) * Scale);
                Assert.Less(Vector2.Distance(shadow, expected), 1.5f, "The shadow follows the ground at " + flight.Frame.Progress);
            }

            // The miniature stands its feet `Height` above that ground, grown round its feet.
            private void AssertAboveItsShadow(VisualElement root, FlightSnapshot flight)
            {
                var drawn = root.Q("flight-lift").worldBound.center;
                var grown = FlightProfile.Scale(flight.Frame);
                var feet = flight.Ground - new Vector2(0, flight.Height);
                var expected = map.LocalToWorld(feet + (BoxMiddle - Feet) * grown * Scale);
                Assert.Less(Vector2.Distance(drawn, expected), 1.5f, "The figure flies above its shadow at " + flight.Frame.Progress);
            }

            // Along the straight line between the two places, forwards only, in an arc of the profile's height.
            public void AssertProfile(Vector2 from, Vector2 to)
            {
                Assert.Greater(seen.Count, 5, "The flight was drawn frame by frame.");
                var progress = 0f;
                var highest = 0f;
                foreach (var flight in seen)
                {
                    Assert.Less(DistanceToSegment(flight.Ground, from, to), 1, "The ground runs straight between the places.");
                    Assert.GreaterOrEqual(flight.Frame.Progress, progress - 1e-5f, "It only moves forwards.");
                    progress = flight.Frame.Progress;
                    highest = Mathf.Max(highest, flight.Height);
                    if (flight.Frame.Progress == 0)
                        Assert.LessOrEqual(flight.Height, FlightProfile.Rise * Scale + .01f, "The lift is in place.");
                }
                var arc = FlightProfile.ArcHeight(Vector2.Distance(from, to)) + FlightProfile.Rise * Scale;
                Assert.LessOrEqual(highest, arc + .5f, "Never higher than the arc.");
                if (saved.Contains("arc-top"))
                    Assert.Greater(highest, arc * .9f, "Its top is the arc's height above the raised pose.");
                Assert.AreEqual(1, Last.Frame.Progress, 1e-5f, "It ends on the destination's place.");
            }

            private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
            {
                var along = b - a;
                var t = along.sqrMagnitude < 1e-6f ? 0 : Mathf.Clamp01(Vector2.Dot(point - a, along) / along.sqrMagnitude);
                return Vector2.Distance(point, a + along * t);
            }
        }
    }
}
