using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-102: paper and sea beyond the map let the camera bring every edge region clear of the HUD at the Fit
    // zoom; the canvas always covers the viewport, and the margins are never a region.
    public sealed class MapCanvasLayoutTests
    {
        [UnityTest]
        public IEnumerator EveryRegionComesClearOfTheHudAndTheMarginsStayInertInFourLayouts()
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var fixture = new BoardFixture();
                    fixture.View.Decision = null;
                    fixture.View.Options.Clear();
                    using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    try
                    {
                        client.Panel.targetTexture = texture;
                        client.Panel.scale = scale;
                        client.Apply(fixture.View);
                        yield return Settle();
                        var label = $"{width}-{scale:0.00}";
                        var map = client.Board.Map;
                        map.Fit();
                        yield return Settle();
                        CheckReach(client.Board, label);
                        CheckCover(map, label);
                        yield return CheckMargins(client.Board, texture, label);
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        texture.Release();
                        Object.Destroy(texture);
                    }
                }
        }

        // At the Fit zoom each region can be panned until some of its terrain is the map, not the HUD.
        private static void CheckReach(BoardScreen board, string label)
        {
            var map = board.Map;
            var hidden = new List<string>();
            foreach (var shape in map.Data.Shapes.Values)
            {
                map.Camera.Zoom = map.FitZoom;
                var anchor = new Vector2(shape.Anchor[0], shape.Anchor[1]);
                map.Camera.Pan = map.contentRect.center - anchor * map.Camera.Zoom;
                map.Refresh();
                if (!Exposed(board, shape.Id))
                    hidden.Add(shape.Id);
            }
            Assert.AreEqual(105, map.Data.Shapes.Count);
            CollectionAssert.IsEmpty(hidden, "Regions that stay under the HUD · " + label);
        }

        private static bool Exposed(BoardScreen board, string region)
        {
            var map = board.Map;
            var shape = map.Data.Shapes[region];
            var center = map.LocalToWorld(map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1])));
            for (var radius = 0; radius <= 160; radius += 4)
                for (var angle = 0; angle < 360; angle += 20)
                {
                    var radians = angle * Mathf.Deg2Rad;
                    var point = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
                    if (map.worldBound.Contains(point) && map.HitTest(map.WorldToLocal(point)) == region &&
                        board.Root.panel.Pick(point) == map)
                        return true;
                }
            return false;
        }

        // Even zoomed out and pushed to a corner, the canvas covers the whole viewport: no background shows.
        private static void CheckCover(MapCanvas map, string label)
        {
            foreach (var corner in new[] { new Vector2(-1e5f, -1e5f), new Vector2(1e5f, 1e5f), new Vector2(-1e5f, 1e5f), new Vector2(1e5f, -1e5f) })
            {
                map.Camera.Zoom = map.Camera.MinimumZoom;
                map.Camera.Pan = corner;
                map.Refresh();
                var min = map.Camera.ToScreen(map.CanvasBounds.min);
                var max = map.Camera.ToScreen(map.CanvasBounds.max);
                Assert.LessOrEqual(min.x, map.contentRect.xMin + .01f, label);
                Assert.LessOrEqual(min.y, map.contentRect.yMin + .01f, label);
                Assert.GreaterOrEqual(max.x, map.contentRect.xMax - .01f, label);
                Assert.GreaterOrEqual(max.y, map.contentRect.yMax - .01f, label);
            }
            Assert.Less(map.Camera.MinimumZoom, map.FitZoom, "The canvas lets the camera step back beyond the map · " + label);
        }

        // The paper and the sea beyond the map neither highlight nor open nor select anything.
        private static IEnumerator CheckMargins(BoardScreen board, RenderTexture texture, string label)
        {
            var map = board.Map;
            map.Camera.Zoom = map.FitZoom;
            map.Camera.Pan = new Vector2(1e5f, 1e5f);
            map.Refresh();
            yield return Settle();
            LayoutTests.Save(texture, $"ui102-margins-{label}.png");
            var point = Margin(board);
            Assert.IsNull(map.HitTest(map.WorldToLocal(point)), label);
            RegionInspectionTests.Move(board, point);
            yield return Settle();
            Assert.IsEmpty(map.Hovered, "The margin never highlights · " + label);
            var selected = map.Selected;
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point, clickCount = 1 }))
                map.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point, clickCount = 1 }))
                map.SendEvent(up);
            yield return Settle();
            Assert.AreEqual(selected, map.Selected, "A margin click selects nothing · " + label);
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display, "A margin click opens nothing · " + label);
            Assert.AreEqual(0, board.View.Revision, label);
        }

        private static Vector2 Margin(BoardScreen board)
        {
            var map = board.Map;
            var bounds = map.worldBound;
            for (var row = 1; row < 30; row++)
                for (var column = 1; column < 30; column++)
                {
                    var point = bounds.min + Vector2.Scale(bounds.size, new Vector2(column, row) / 30f);
                    var local = map.Camera.ToBoard(map.WorldToLocal(point));
                    if (!map.TerrainBounds.Contains(local) && board.Root.panel.Pick(point) == map)
                        return point;
                }
            throw new AssertionException("No uncovered margin at the canvas corner.");
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }
    }
}
