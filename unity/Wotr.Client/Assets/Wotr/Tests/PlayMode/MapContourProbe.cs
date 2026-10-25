using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    internal static class MapContourProbe
    {
        public static Vector2 Anchor(MapCanvas map, string region)
        {
            var anchor = map.Data.Shapes[region].Anchor;
            return map.LocalToWorld(map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])));
        }

        // Find actual uncovered terrain, not a displaced target button. Every sampled
        // point must belong to the source contour and be picked as the live canvas.
        public static Vector2 Terrain(MapCanvas map, string region)
        {
            if (TryTerrain(map, region, out var point))
                return point;
            var anchor = Anchor(map, region);
            throw new AssertionException("No exposed terrain for " + region + "; zoom=" + map.Camera.Zoom +
                ", pan=" + map.Camera.Pan + ", anchor=" + anchor + ", covered by=" + map.panel.Pick(anchor)?.name);
        }

        // Any uncovered terrain of any region, without moving the camera.
        public static Vector2 ExposedTerrain(MapCanvas map, out string region)
        {
            for (var row = 1; row < 20; row++)
                for (var column = 1; column < 20; column++)
                {
                    var point = map.worldBound.min + Vector2.Scale(map.worldBound.size, new Vector2(column, row) / 20f);
                    region = map.HitTest(map.WorldToLocal(point));
                    if (region != null && map.panel.Pick(point) == map)
                        return point;
                }
            throw new AssertionException("No uncovered terrain on the map.");
        }

        private static bool TryTerrain(MapCanvas map, string region, out Vector2 result)
        {
            var center = Anchor(map, region);
            for (var radius = 0; radius <= 200; radius += 4)
                for (var angle = 0; angle < 360; angle += 15)
                {
                    var radians = angle * Mathf.Deg2Rad;
                    var point = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
                    if (map.worldBound.Contains(point) && map.HitTest(map.WorldToLocal(point)) == region &&
                        map.panel.Pick(point) == map)
                    {
                        result = point;
                        return true;
                    }
                }
            result = Vector2.zero;
            return false;
        }

        // Legacy map buttons could be clamped far away from their actual region.
        // Reach the real contour using player navigation before clicking or dropping.
        public static Vector2 ReachRegion(MapCanvas map, string region, Action<string> trace = null)
        {
            var point = Vector2.zero;
            var anchor = map.Data.Shapes[region].Anchor;
            Navigate(map, new Vector2(anchor[0], anchor[1]),
                () => TryTerrain(map, region, out point), region, trace);
            return point;
        }

        public static void ReachPoints(MapCanvas map, Vector2[] coordinates, Action<string> trace = null)
        {
            Assert.IsNotEmpty(coordinates);
            var center = Vector2.zero;
            foreach (var coordinate in coordinates)
                center += coordinate;
            center /= coordinates.Length;
            Navigate(map, center, () =>
            {
                foreach (var coordinate in coordinates)
                {
                    var local = map.Camera.ToScreen(coordinate);
                    if (!map.contentRect.Contains(local) || map.panel.Pick(map.LocalToWorld(local)) != map)
                        return false;
                }
                return true;
            }, "exact contour boundary points", trace);
        }

        private static void Navigate(MapCanvas map, Vector2 coordinate, Func<bool> exposed, string target,
            Action<string> trace)
        {
            if (exposed())
                return;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                foreach (var destination in NavigationPoints(map))
                {
                    var delta = destination - map.LocalToWorld(map.Camera.ToScreen(coordinate));
                    var before = map.Camera.Pan;
                    PanAt(map, ExposedMap(map), delta);
                    trace?.Invoke("requested=" + delta.ToString("F3") + ", actual=" +
                        (map.Camera.Pan - before).ToString("F3") + ", pan=" + map.Camera.Pan.ToString("F3"));
                    if (exposed())
                        return;
                }
                var zoom = map.Camera.Zoom;
                ZoomIn(map);
                trace?.Invoke("zoom=" + zoom.ToString("R") + " -> " + map.Camera.Zoom.ToString("R"));
                if (exposed())
                    return;
            }
            throw new AssertionException("No exposed terrain for " + target + "; zoom=" + map.Camera.Zoom +
                ", pan=" + map.Camera.Pan + ", viewport=" + map.worldBound);
        }

        public static async Task ReachMarker(MapCanvas map, VisualElement marker, string region)
        {
            Assert.IsNotNull(marker, "Missing marker for " + region);
            if (!map.MarkerVisibility.Detail)
            {
                ZoomIn(map);
                if (!map.MarkerVisibility.Detail)
                    ZoomIn(map);
                await Task.Delay(80);
            }
            if (Exposed(marker))
                return;
            foreach (var destination in NavigationPoints(map))
            {
                PanAt(map, ExposedMap(map), destination - Anchor(map, region));
                await Task.Delay(80);
                if (Exposed(marker))
                    return;
            }
            Assert.IsTrue(Exposed(marker), "Unable to expose marker " + marker.name + " using map navigation.");
        }

        private static bool Exposed(VisualElement element)
        {
            if (element.resolvedStyle.display == DisplayStyle.None || element.worldBound.width <= 0)
                return false;
            var picked = element.panel.Pick(element.worldBound.center);
            return picked == element || element.Contains(picked);
        }

        private static void ZoomIn(MapCanvas map)
        {
            using var wheel = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel,
                mousePosition = ExposedMap(map), delta = new Vector2(0, -7) });
            map.SendEvent(wheel);
        }

        private static IEnumerable<Vector2> NavigationPoints(MapCanvas map)
        {
            foreach (var row in new[] { .45f, .3f, .6f, .2f, .7f })
                foreach (var column in new[] { .5f, .25f, .75f, .12f, .88f })
                {
                    var point = map.worldBound.min + Vector2.Scale(map.worldBound.size, new Vector2(column, row));
                    if (map.panel.Pick(point) == map)
                        yield return point;
                }
        }

        public static void ClickRegion(MapCanvas map, string region) => Click(map, ReachRegion(map, region));

        // UI-105: armies, figures and characters are never dragged. Pressing one and moving away captures nothing,
        // and releasing over a region is not a step.
        public static void AttemptDrag(BoardScreen board, VisualElement source, Vector2 point)
        {
            Assert.IsNotNull(source);
            PointerGestureEvents.Press(source);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag,
                button = 0, mousePosition = source.worldBound.center + new Vector2(9, 0) }))
                source.SendEvent(move);
            Assert.IsFalse(board.Root.HasPointerCapture(PointerId.mousePointerId), "A figure is never a drag source.");
            Assert.AreEqual(DisplayStyle.None, board.Root.Q(className: "drag-ghost").resolvedStyle.display);
            Drop(board, point);
        }

        internal static Vector2 ExposedMap(MapCanvas map)
        {
            for (var row = 1; row < 10; row++)
                for (var column = 1; column < 10; column++)
                {
                    var point = map.worldBound.min + Vector2.Scale(map.worldBound.size, new Vector2(column * .1f, row * .1f));
                    if (map.panel.Pick(point) == map)
                        return point;
                }
            throw new AssertionException("No exposed map available for navigation.");
        }

        public static void Hover(VisualElement root, Vector2 point)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point });
            root.panel.visualTree.SendEvent(move);
        }

        public static void Click(MapCanvas map, Vector2 point, int count = 1)
        {
            var picked = map.panel.Pick(point);
            Assert.AreSame(map, picked, "Terrain is occluded by " + picked?.name);
            Press(map, point, count);
            Release(map, point, count);
        }

        public static void Press(MapCanvas map, Vector2 point, int count = 1)
        {
            Assert.AreSame(map, map.panel.Pick(point), "Terrain press must begin on the exposed map.");
            using var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown,
                button = 0, mousePosition = point, clickCount = count });
            map.SendEvent(down);
        }

        public static void Release(MapCanvas map, Vector2 point, int count = 1)
        {
            using var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                button = 0, mousePosition = point, clickCount = count });
            map.SendEvent(up);
        }

        // UI-100: a right click without a drag reads the region under the pointer (read-only information).
        // The target is the map, or an army marker on it.
        public static void Inspect(VisualElement target, Vector2 point)
        {
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown,
                button = 1, mousePosition = point, clickCount = 1 }))
                target.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                button = 1, mousePosition = point, clickCount = 1 }))
                target.SendEvent(up);
        }

        public static void BeginDrag(BoardScreen board, VisualElement source)
        {
            Assert.IsNotNull(source);
            PointerGestureEvents.Press(source);
            var point = source.worldBound.center + new Vector2(9, 0);
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag,
                button = 0, mousePosition = point });
            source.SendEvent(move);
            Assert.IsTrue(board.Root.HasPointerCapture(PointerId.mousePointerId));
        }

        public static void Drop(BoardScreen board, Vector2 point)
        {
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag,
                button = 0, mousePosition = point }))
                board.Root.SendEvent(move);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                button = 0, mousePosition = point, clickCount = 1 }))
                board.Root.SendEvent(up);
        }

        public static void Pan(MapCanvas map, Vector2 delta)
        {
            var point = map.worldBound.center;
            PanAt(map, point, delta);
        }

        public static void PanAt(MapCanvas map, Vector2 point, Vector2 delta)
        {
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown,
                button = 1, mousePosition = point }))
                map.SendEvent(down);
            // UI-100: a right press without a drag reads the region; a short pan first passes the drag tolerance.
            if (delta.magnitude < 8)
                using (var reach = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag,
                    button = 1, mousePosition = point + new Vector2(0, 12) }))
                    map.SendEvent(reach);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag,
                button = 1, mousePosition = point + delta }))
                map.SendEvent(move);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                button = 1, mousePosition = point + delta }))
                map.SendEvent(up);
        }
    }
}
