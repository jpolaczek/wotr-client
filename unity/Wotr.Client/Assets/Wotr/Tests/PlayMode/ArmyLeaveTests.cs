using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-338 (owner 2026-10-25): the counterpart of UI-334 — a marker whose figures all leave the map fades out where it
    // stood instead of vanishing (it counts as gone at once), a shrinking army's figures dim as its number steps down, and
    // figures a flight carries away leave with the flight. A quiet view and Skip animations show the state at once.
    public sealed class ArmyLeaveTests
    {
        private const string Region = "lossarnach";
        private const string Marker = "army-lossarnach-FreePeoples";

        [UnityTest]
        public IEnumerator ARemovedArmyFadesOutWhereItStoodThenIsGone()
        {
            using var client = new MountedMatchClient("http://localhost", "army-leave");
            var recruited = Recruited(client, out var view);
            yield return Settled(client, recruited);
            var map = client.Board.Map;
            map.Focus(Region);
            map.Refresh();
            yield return null;
            var standing = map.Q<Button>(Marker).worldBound.position - Anchor(map);
            LayoutTests.Save(client.Viewport, "ui338-standing.png");
            client.Apply(Remove(view, Region));
            yield return null;
            yield return null;
            Assert.IsNull(map.Q<Button>(Marker), "It counts as gone at once.");
            var leaving = map.Q<Button>("leaving-" + Marker);
            Assert.IsNotNull(leaving, "Its figures are still fading.");
            Assert.IsTrue(leaving.ClassListContains(MapArmyArtwork.LeavingClass));
            Assert.AreEqual(PickingMode.Ignore, leaving.pickingMode, "A fading marker takes no pointer.");
            yield return new WaitForSecondsRealtime(MapArmyArtwork.LeaveMilliseconds / 2000f);
            var halfway = leaving.Q("army-artwork").resolvedStyle.opacity;
            Assert.Greater(halfway, .25f, "It fades slowly…");
            Assert.Less(halfway, .75f, "…and smoothly.");
            LayoutTests.Save(client.Viewport, "ui338-leaving.png");
            map.Camera.Pan += new Vector2(60, 0);
            map.Refresh();
            yield return null;
            Assert.AreEqual(standing.x, (leaving.worldBound.position - Anchor(map)).x, 1, "It stays where it stood as the view moves.");
            yield return new WaitForSecondsRealtime(MapArmyArtwork.LeaveMilliseconds / 1000f);
            Assert.IsNull(leaving.parent, "Then it is gone.");
        }

        [UnityTest]
        public IEnumerator AShrinkingArmyDimsItsFiguresAsItsNumberStepsDown()
        {
            using var client = new MountedMatchClient("http://localhost", "army-shrink");
            var recruited = Recruited(client, out var view);
            yield return Settled(client, recruited);
            client.Apply(Remove(view, Region, 1));
            yield return null;
            yield return null;
            var marker = client.Board.Map.Q<Button>(Marker);
            Assert.IsTrue(marker.ClassListContains(MapArmyArtwork.ShrinkingClass), "Its figures dim.");
            Assert.AreEqual("1", marker.Q<Label>("army-count").text, "Its number steps down.");
            // The dip, then the figures' usual 0.5 s ease back to clear.
            yield return new WaitForSecondsRealtime((MapArmyArtwork.ShrinkMilliseconds + MapArmyArtwork.EmergeMilliseconds) / 1000f + .15f);
            Assert.IsFalse(marker.ClassListContains(MapArmyArtwork.ShrinkingClass));
            Assert.AreEqual(1f, marker.Q("army-artwork").resolvedStyle.opacity, .01f, "Clear again.");
        }

        [UnityTest]
        public IEnumerator FiguresAFlightCarriesAwayLeaveWithTheFlight()
        {
            using var client = new MountedMatchClient("http://localhost", "army-fly");
            var recruited = Recruited(client, out var view);
            yield return Settled(client, recruited);
            client.Board.SetConnection("Connected");
            var moved = Copy(view);
            moved.Revision++;
            foreach (var unit in moved.Units.Where(u => u.Location == Region))
                unit.Location = "minas-tirith";
            client.Refresh(moved);
            yield return null;
            yield return null;
            Assert.AreEqual(1, client.Board.Map.Figures.ActiveCount, "A flight carries them.");
            Assert.IsFalse(Fading(client.Board.Map), "The flight is their departure.");
        }

        [UnityTest]
        public IEnumerator AQuietViewAndSkipAnimationsRemoveTheArmyAtOnce()
        {
            using (var client = new MountedMatchClient("http://localhost", "army-leave-quiet"))
            {
                var recruited = Recruited(client, out var view);
                yield return Settled(client, recruited);
                client.Board.ApplyQuietly(Remove(view, Region));
                yield return null;
                yield return null;
                Assert.IsNull(client.Board.Map.Q<Button>(Marker));
                Assert.IsFalse(Fading(client.Board.Map), "A quiet view shows the state.");
            }
            using var stage = new PresentedBoardStage(1366, 1f, true);
            yield return stage.Ready();
            var latest = Copy(stage.Latest());
            var army = latest.Units.First(u => u.Side == Side.FreePeoples && u.Location != "reserve" && stage.Board.Map.Data.Regions.ContainsKey(u.Location));
            var region = army.Location;
            foreach (var unit in latest.Units.Where(u => u.Location == region))
                unit.Location = "reserve";
            latest.Characters.RemoveAll(c => c.Location == region);
            latest.Revision++;
            stage.Board.Apply(latest);
            yield return null;
            yield return null;
            Assert.IsFalse(stage.Board.Map.Query<Button>().ToList().Any(button => button.name == "army-" + region + "-FreePeoples"));
            Assert.IsFalse(Fading(stage.Board.Map), "Skip animations shows it at once.");
        }

        // A first view, then two Gondor Regulars recruited to Lossarnach (they emerge, UI-334).
        private static PlayerView Recruited(MountedMatchClient client, out PlayerView view)
        {
            var first = new BoardFixture().View;
            client.Apply(first);
            view = Copy(first);
            view.Revision++;
            foreach (var unit in view.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve").Take(2))
                unit.Location = Region;
            return view;
        }

        // The first view only sets the state; the recruits then emerge and settle.
        private static IEnumerator Settled(MountedMatchClient client, PlayerView recruited)
        {
            yield return new WaitForSecondsRealtime(.3f);
            client.Apply(recruited);
            yield return new WaitForSecondsRealtime((MapArmyArtwork.FlareMilliseconds + MapArmyArtwork.EmergeMilliseconds) / 1000f + .2f);
        }

        private static Vector2 Anchor(MapCanvas map)
        {
            var anchor = map.Data.Shapes[Region].Anchor;
            return map.LocalToWorld(map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])));
        }

        private static bool Fading(VisualElement map) => map.Query<Button>().ToList().Any(button => button.name.StartsWith("leaving-"));

        // The region's figures back to the reserve (`count` of them, or all), the next revision.
        private static PlayerView Remove(PlayerView view, string region, int count = int.MaxValue)
        {
            var next = Copy(view);
            next.Revision++;
            foreach (var unit in next.Units.Where(u => u.Location == region).Take(count))
                unit.Location = "reserve";
            return next;
        }

        private static PlayerView Copy(PlayerView view) => JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
    }
}
