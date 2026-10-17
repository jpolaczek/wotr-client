using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-189 (user decision): a region window opened by a click stays while the pointer is on its region or on the
    // window, survives a quick crossing elsewhere, and closes shortly after the pointer has left both.
    public sealed class RegionPanelLeaveTests
    {
        [UnityTest]
        public IEnumerator TheWindowStaysOverItsRegionAndItselfAndClosesAfterThePointerLeavesBoth()
        {
            var fixture = new BoardFixture();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            yield return Settle();
            var hover = client.Board.Hover;
            var map = client.Board.Map;
            var region = VisibleRegionWithForces(map, fixture);
            hover.Open(region);
            yield return Settle();
            var panel = hover.Panel.Root;
            var root = panel.panel.visualTree;
            var away = AwayPoint(map, panel, region);

            Move(root, Point(map, region));
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(Open(panel), "On its region the window stays.");
            Move(root, panel.worldBound.center);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(Open(panel), "On the window it stays.");

            Move(root, away);
            yield return new WaitForSecondsRealtime(0.1f);
            Move(root, panel.worldBound.center);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(Open(panel), "A quick crossing elsewhere does not close it.");

            Move(root, away);
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.IsTrue(Open(panel), "Not at once: the pointer may still reach the window.");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(Open(panel), "Away from the region and the window it closes.");
        }

        private static bool Open(VisualElement panel) => panel.resolvedStyle.display != DisplayStyle.None;

        private static Vector2 Point(MapCanvas map, string region)
        {
            var anchor = map.Data.Shapes[region].Anchor;
            return map.LocalToWorld(map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])));
        }

        private static bool OnMap(MapCanvas map, Vector2 point)
        {
            var picked = map.panel.Pick(point);
            return map.worldBound.Contains(point) && picked != null && (picked == map || map.Contains(picked));
        }

        // A region with forces (the full window), whose anchor is on the visible map.
        private static string VisibleRegionWithForces(MapCanvas map, BoardFixture fixture)
        {
            var region = fixture.View.Units.Select(u => u.Location).Distinct()
                .FirstOrDefault(r => map.Data.Shapes.ContainsKey(r) && OnMap(map, Point(map, r)));
            Assert.IsNotNull(region, "No region with forces is visible.");
            return region;
        }

        // A point of another region, on the visible map and clear of the window.
        private static Vector2 AwayPoint(MapCanvas map, VisualElement panel, string open)
        {
            var clear = new Rect(panel.worldBound.position - Vector2.one * 40, panel.worldBound.size + Vector2.one * 80);
            foreach (var region in map.Data.Shapes.Keys.Where(r => r != open))
            {
                var point = Point(map, region);
                if (!clear.Contains(point) && OnMap(map, point))
                    return point;
            }
            Assert.Fail("No region clear of the window is visible.");
            return Vector2.zero;
        }

        private static void Move(VisualElement root, Vector2 point)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point });
            root.SendEvent(move);
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
