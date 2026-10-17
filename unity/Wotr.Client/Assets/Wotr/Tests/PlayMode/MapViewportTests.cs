using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed partial class MapViewportTests
    {
        [UnityTest]
        public IEnumerator ZoomPanLocateAndResizeCoverTheViewportForBothPlayers() =>
            MatchTestRoom.Wait(CoverLayouts());

        [UnityTest]
        public IEnumerator EdgesCornersControlsFocusAndDetachNeverChangeTheMatch() =>
            MatchTestRoom.Wait(EdgeNavigation());

        private static async Task CoverLayouts()
        {
            using var room = await UI10TestRoom.Connect("map15-navigation");
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var snapshot = JsonConvert.SerializeObject(board.View);
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        texture.Create();
                        try
                        {
                            client.Panel.targetTexture = texture;
                            client.Panel.scale = scale;
                            await room.Settle();
                            board.Map.Fit();
                            Covers(board.Map);
                            Assert.That(board.Map.worldBound.width, Is.EqualTo(board.Root.worldBound.width).Within(1));
                            Assert.That(board.Map.worldBound.height, Is.EqualTo(board.Root.worldBound.height).Within(1));
                            foreach (var factor in new[] { .001f, 12f, .8f })
                            {
                                board.Map.Camera.ZoomAt(board.Map.contentRect.center, factor);
                                board.Map.Refresh();
                                foreach (var delta in new[] { Vector2.one * 10000, Vector2.one * -10000 })
                                {
                                    MapContourProbe.Pan(board.Map, delta);
                                    Covers(board.Map);
                                }
                            }
                            foreach (var region in new[] { "grey-havens", "ered-luin", "erebor", "nurn", "umbar" })
                            {
                                board.Map.Focus(region);
                                Covers(board.Map);
                                var point = MapContourProbe.Anchor(board.Map, region);
                                Assert.IsTrue(board.Map.worldBound.Contains(point), "Locate must keep " + region + " on screen.");
                            }
                            LayoutTests.Save(texture, $"ui73-cover-{board.View.You}-{width}-{scale:0.00}.png");
                        }
                        finally
                        {
                            client.Panel.targetTexture = client.Viewport;
                            client.Panel.scale = 1;
                            texture.Release();
                            Object.Destroy(texture);
                        }
                    }
                Assert.AreEqual(snapshot, JsonConvert.SerializeObject(board.View));
            }
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        private static async Task EdgeNavigation()
        {
            using var room = await UI10TestRoom.Connect("map15-navigation");
            var board = room.Fp.Board;
            var map = board.Map;
            var snapshot = JsonConvert.SerializeObject(board.View);
            foreach (var direction in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down,
                new Vector2(-1, -1), new Vector2(1, 1) })
            {
                Center(map);
                await room.Settle();
                var before = map.Camera.Pan;
                var edge = EdgePoint(map, direction);
                MapContourProbe.Hover(board.Root, edge);
                Assert.IsTrue(map.EdgePan.Active, "An exposed edge must start navigation: " +
                    direction + " picked " + map.panel.Pick(edge)?.name);
                await Task.Delay(140, room.Token);
                var delta = map.Camera.Pan - before;
                if (direction.x != 0)
                    Assert.Greater(delta.x * -direction.x, 1, "Horizontal edge " + direction);
                if (direction.y != 0)
                    Assert.Greater(delta.y * -direction.y, 1, "Vertical edge " + direction);
                Covers(map);
                map.EdgePan.Stop();
            }
            Center(map);
            await room.Settle();
            var point = EdgePoint(map, Vector2.left);
            var blocker = new Button { name = "ui73-edge-control" };
            blocker.style.position = Position.Absolute;
            blocker.style.left = board.Root.WorldToLocal(point).x;
            blocker.style.top = board.Root.WorldToLocal(point).y - 20;
            blocker.style.width = 50;
            blocker.style.height = 40;
            board.Root.Add(blocker);
            await room.Settle();
            var still = map.Camera.Pan;
            var controlPoint = new Vector2(blocker.worldBound.xMin + 2, blocker.worldBound.center.y);
            var pickedControl = map.panel.Pick(controlPoint);
            Assert.IsTrue(pickedControl == blocker || blocker.Contains(pickedControl),
                "The control test must hit the rendered button, including its default margins.");
            Assert.Greater(MapEdgePan.VelocityAt(controlPoint, map.worldBound).x, 0,
                "The blocked pointer must remain inside the active left edge band.");
            MapContourProbe.Hover(board.Root, controlPoint);
            await Task.Delay(100, room.Token);
            Assert.AreEqual(still, map.Camera.Pan, "Controls at the edge block map navigation.");
            Assert.IsFalse(map.EdgePan.Active);
            blocker.RemoveFromHierarchy();
            await room.Settle();
            await ReparentedTrayBlocksNavigation(room, board);
            foreach (var button in new[] { 1, 2 })
            {
                CancelledPanCannotResume(board, button, focusLoss: false);
                CancelledPanCannotResume(board, button, focusLoss: true);
            }
            MapContourProbe.Hover(board.Root, point);
            Assert.IsTrue(map.EdgePan.Active);
            map.EdgePan.SetFocused(false);
            still = map.Camera.Pan;
            await Task.Delay(100, room.Token);
            Assert.AreEqual(still, map.Camera.Pan);
            Assert.IsFalse(map.EdgePan.Active);
            map.EdgePan.SetFocused(true);
            map.Camera.Pan = Vector2.one * 100000;
            map.Refresh();
            MapContourProbe.Hover(board.Root, point);
            await Task.Delay(100, room.Token);
            Assert.IsFalse(map.EdgePan.Active, "A reached raster edge stops the timer.");
            Center(map);
            MapContourProbe.Hover(board.Root, point);
            Assert.IsTrue(map.EdgePan.Active);
            map.RemoveFromHierarchy();
            Assert.IsFalse(map.EdgePan.Active);
            Assert.AreEqual(snapshot, JsonConvert.SerializeObject(board.View));
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        private static void CancelledPanCannotResume(BoardScreen board, int button, bool focusLoss)
        {
            var map = board.Map;
            var point = ExposedMapPoint(map);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown,
                button = button, mousePosition = point }))
            {
                map.SendEvent(down);
                Assert.IsTrue(map.Panning);
                Assert.IsTrue(map.HasPointerCapture(PointerId.mousePointerId));
                if (focusLoss)
                    map.EdgePan.SetFocused(false);
                else
                    using (var cancel = PointerCancelEvent.GetPooled(down))
                        map.SendEvent(cancel);
            }
            Assert.IsFalse(map.Panning, "Cancellation must clear the right/middle pan state.");
            Assert.IsFalse(map.HasPointerCapture(PointerId.mousePointerId));
            var pan = map.Camera.Pan;
            map.EdgePan.SetFocused(true);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove,
                mousePosition = point + new Vector2(37, 19) }))
                map.SendEvent(move);
            Assert.AreEqual(pan, map.Camera.Pan, "Moving without a new press must not resume cancelled pan.");
            // Cancellation releases this gesture's capture, while the physical
            // button may still be held. Release it before testing idle edge hover.
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                button = button, mousePosition = point + new Vector2(37, 19) }))
                map.SendEvent(up);
            Assert.IsFalse(map.Panning);
            Assert.AreEqual(pan, map.Camera.Pan);
            using (var idle = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove,
                mousePosition = point }))
                Assert.AreEqual(0, idle.pressedButtons, "The completed synthetic gesture must release the physical button.");
        }

        private static Vector2 ExposedMapPoint(MapCanvas map)
        {
            for (var row = 2; row < 8; row++)
                for (var column = 2; column < 8; column++)
                {
                    var point = map.worldBound.min + Vector2.Scale(map.worldBound.size,
                        new Vector2(column / 10f, row / 10f));
                    if (map.panel.Pick(point) == map)
                        return point;
                }
            throw new AssertionException("No exposed map available for the pan cancellation gesture.");
        }

        private static async Task ReparentedTrayBlocksNavigation(UI10TestRoom room, BoardScreen board)
        {
            var map = board.Map;
            var dock = board.Tray.parent;
            var position = dock.IndexOf(board.Tray);
            var panel = new VisualElement { name = "ui73-edge-panel" };
            panel.style.position = Position.Absolute;
            panel.style.left = 0;
            panel.style.top = board.Root.contentRect.center.y - 80;
            panel.style.width = 240;
            panel.style.height = 160;
            board.Root.Add(panel);
            panel.Add(board.Tray);
            try
            {
                await room.Settle();
                var point = new Vector2(board.Tray.worldBound.xMin + 2, board.Tray.worldBound.yMax - 2);
                var picked = map.panel.Pick(point);
                Assert.IsTrue(picked == board.Tray || board.Tray.Contains(picked),
                    "The floating-panel probe must hit the actual reparented decision tray.");
                Assert.Greater(MapEdgePan.VelocityAt(point, map.worldBound).x, 0);
                var before = map.Camera.Pan;
                MapContourProbe.Hover(board.Root, point);
                await Task.Delay(100, room.Token);
                Assert.AreEqual(before, map.Camera.Pan, "A tray inside a floating panel cannot expose the map edge.");
                Assert.IsFalse(map.EdgePan.Active);
            }
            finally
            {
                dock.Insert(position, board.Tray);
                panel.RemoveFromHierarchy();
            }
            await room.Settle();
        }

        private static void Center(MapCanvas map)
        {
            map.EdgePan.Stop();
            map.Camera.Zoom = Mathf.Max(1.6f, map.FitZoom * 1.5f);
            map.Camera.Pan = map.contentRect.center - map.TerrainBounds.center * map.Camera.Zoom;
            map.Refresh();
        }

        private static Vector2 EdgePoint(MapCanvas map, Vector2 direction)
        {
            var area = map.worldBound;
            var point = new Vector2(direction.x < 0 ? area.xMin + 2 : direction.x > 0 ? area.xMax - 2 : area.center.x,
                direction.y < 0 ? area.yMin + 2 : direction.y > 0 ? area.yMax - 2 : area.center.y);
            if (direction.x != 0 && direction.y != 0)
            {
                Assert.IsTrue(IsNavigationSurface(map, point),
                    "Corner is blocked by " + map.panel.Pick(point)?.name + ": " + direction);
                return point;
            }
            for (var offset = 0f; offset < .45f; offset += .04f)
                foreach (var sign in new[] { -1f, 1f })
                {
                    var candidate = point + (direction.x == 0 ? Vector2.right * area.width : Vector2.up * area.height) * offset * sign;
                    if (IsNavigationSurface(map, candidate))
                        return candidate;
                }
            throw new AssertionException("The HUD must leave a noninteractive edge reachable: " + direction);
        }

        private static bool IsNavigationSurface(MapCanvas map, Vector2 point)
        {
            var picked = map.panel.Pick(point);
            if (picked == map)
                return true;
            var area = map.worldBound;
            var rim = 4f / map.panel.scaledPixelsPerPoint;
            var onRim = point.x - area.xMin <= rim || area.xMax - point.x <= rim ||
                point.y - area.yMin <= rim || area.yMax - point.y <= rim;
            for (var current = picked; current != null; current = current.parent)
            {
                if (current.ClassListContains("floating-window"))
                    return false;
                if (onRim && current.ClassListContains("map-edge-control"))
                    return true;
            }
            for (var current = picked; current != null; current = current.parent)
            {
                if (current.ClassListContains("map-edge-surface"))
                    return true;
                if (current.focusable && !current.ClassListContains("map-edge-focus-anchor") ||
                    !string.IsNullOrEmpty(current.tooltip))
                    return false;
            }
            return false;
        }

        // UI-102: the canvas (map, paper and sea) always covers the viewport; the map alone may not.
        private static void Covers(MapCanvas map)
        {
            var min = map.Camera.ToScreen(map.CanvasBounds.min);
            var max = map.Camera.ToScreen(map.CanvasBounds.max);
            Assert.LessOrEqual(min.x, map.contentRect.xMin + .01f);
            Assert.LessOrEqual(min.y, map.contentRect.yMin + .01f);
            Assert.GreaterOrEqual(max.x, map.contentRect.xMax - .01f);
            Assert.GreaterOrEqual(max.y, map.contentRect.yMax - .01f);
        }
    }
}
