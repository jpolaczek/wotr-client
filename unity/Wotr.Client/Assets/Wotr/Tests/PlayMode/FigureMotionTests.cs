using System.Collections;
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
            moved.Location = "osgiliath";
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            Assert.AreEqual(1, map.Figures.ActiveCount);
            var moving = map.Figures.Root.Q("moving-unit:" + moved.Id);
            Assert.IsNotNull(moving);
            Assert.AreEqual(PickingMode.Ignore, moving.pickingMode);
            Assert.AreEqual("minas-tirith", remaining.Location);
            Assert.AreEqual("osgiliath", board.View.Units.First(u => u.Id == moved.Id).Location);
            var start = moving.transform.position;
            yield return Settle();
            LayoutTests.Save(client.Viewport, "ui76-motion-start.png");
            yield return new WaitForSecondsRealtime(.22f);
            var middle = moving.transform.position;
            var destination = ScreenPosition(map, "osgiliath");
            Assert.Greater(Vector2.Distance(start, middle), 2, "The figurine visibly leaves its source.");
            Assert.Greater(Vector2.Distance(middle, destination), 2, "The intermediate frame must not already be the result.");
            Assert.Less(Vector2.Distance(middle, destination), Vector2.Distance(start, destination));
            LayoutTests.Save(client.Viewport, "ui76-motion-middle.png");
            var picked = map.panel.Pick(map.LocalToWorld((Vector2)middle + new Vector2(22, 20)));
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
            unit.Location = "osgiliath";
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            var accent = map.Figures.Root.Q("moving-unit:" + unit.Id);
            Assert.IsNotNull(accent);
            Assert.IsTrue(accent.ClassListContains("figure-arrival"));
            Assert.Less(Vector2.Distance(accent.transform.position, ScreenPosition(map, "osgiliath")), .1f);
            yield return new WaitForSecondsRealtime(.12f);
            Assert.Less(Vector2.Distance(accent.transform.position, ScreenPosition(map, "osgiliath")), .1f);
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
            character.Location = "pelargir";
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            Assert.AreEqual(1, map.Figures.ActiveCount, "A new move of the figure replaces its flight.");
            Assert.AreNotSame(flight, map.Figures.Root.Q("moving-character:" + character.Id));
            fixture.View.Characters.Remove(character);
            fixture.View.Revision++;
            client.Refresh(fixture.View);
            Assert.AreEqual(0, map.Figures.ActiveCount, "A figure that left the board ends its flight.");
            Assert.IsEmpty(map.Figures.Root.Children());
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

        private static Vector2 ScreenPosition(MapCanvas map, string region)
        {
            var anchor = map.Data.Shapes[region].Anchor;
            return map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])) - MapFigureMotion.FlightFeet;
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
