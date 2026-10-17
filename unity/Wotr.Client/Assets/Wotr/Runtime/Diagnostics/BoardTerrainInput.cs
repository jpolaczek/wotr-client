#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client.Diagnostics
{
    internal static class BoardTerrainInput
    {
        public static void Click(MapCanvas map, string region)
        {
            var point = ExposedRegion(map, region);
            Send(map, point, EventType.MouseDown, 0);
            Send(map, point, EventType.MouseUp, 0);
        }

        public static void Inspect(MapCanvas map, string region)
        {
            var point = ExposedRegion(map, region);
            Send(map, point, EventType.MouseDown, 1);
            Send(map, point, EventType.MouseUp, 1);
        }

        public static Vector2 PanTo(MapCanvas map, string region)
        {
            var anchor = map.Data.Shapes[region].Anchor;
            var coordinate = new Vector2(anchor[0], anchor[1]);
            for (var attempt = 0; attempt < 3; attempt++)
            {
                foreach (var destination in NavigationPoints(map))
                {
                    var current = map.LocalToWorld(map.Camera.ToScreen(coordinate));
                    var start = ExposedMap(map);
                    var end = start + destination - current;
                    Send(map, start, EventType.MouseDown, 1);
                    // A right press without a drag reads the region (UI-100); a short pan first passes the drag tolerance.
                    if (Vector2.Distance(start, end) < 8)
                        Send(map, start + new Vector2(0, 12), EventType.MouseDrag, 1);
                    Send(map, end, EventType.MouseDrag, 1);
                    Send(map, end, EventType.MouseUp, 1);
                    if (TryExposedRegion(map, region, out _))
                        return map.Camera.Pan;
                }
                using var wheel = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel,
                    mousePosition = ExposedMap(map), delta = new Vector2(0, -7) });
                map.SendEvent(wheel);
                if (TryExposedRegion(map, region, out _))
                    return map.Camera.Pan;
            }
            throw new InvalidOperationException("Unable to expose benchmark region through map navigation: " + region);
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

        private static Vector2 ExposedRegion(MapCanvas map, string region)
        {
            if (TryExposedRegion(map, region, out var point))
                return point;
            throw new InvalidOperationException("No exposed terrain for benchmark region " + region +
                " at zoom " + map.Camera.Zoom.ToString("0.00") + "; covered by " + Blockers(map, region));
        }

        private static bool TryExposedRegion(MapCanvas map, string region, out Vector2 result)
        {
            foreach (var point in RegionPoints(map, region))
                if (map.panel.Pick(point) == map)
                {
                    result = point;
                    return true;
                }
            result = Vector2.zero;
            return false;
        }

        private static IEnumerable<Vector2> RegionPoints(MapCanvas map, string region)
        {
            var anchor = map.Data.Shapes[region].Anchor;
            var center = map.LocalToWorld(map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])));
            for (var radius = 0; radius <= 220; radius += 4)
                for (var angle = 0; angle < 360; angle += 15)
                {
                    var radians = angle * Mathf.Deg2Rad;
                    var point = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
                    if (map.worldBound.Contains(point) && map.HitTest(map.WorldToLocal(point)) == region)
                        yield return point;
                }
        }

        private static string Blockers(MapCanvas map, string region)
        {
            var names = new SortedSet<string>();
            foreach (var point in RegionPoints(map, region))
                names.Add(Named(map.panel.Pick(point)));
            return names.Count == 0 ? "nothing (region outside the map view)" : string.Join(", ", names);
        }

        public static string Named(VisualElement element)
        {
            while (element != null && string.IsNullOrEmpty(element.name))
                element = element.parent;
            return element?.name ?? "unnamed";
        }

        private static Vector2 ExposedMap(MapCanvas map)
        {
            for (var row = 1; row < 10; row++)
                for (var column = 1; column < 10; column++)
                {
                    var point = map.worldBound.min + Vector2.Scale(map.worldBound.size, new Vector2(column * .1f, row * .1f));
                    if (map.panel.Pick(point) == map)
                        return point;
                }
            throw new InvalidOperationException("No exposed map surface for benchmark pan.");
        }

        private static void Send(MapCanvas map, Vector2 point, EventType type, int button)
        {
            var source = new Event { type = type, button = button, mousePosition = point, clickCount = 1 };
            if (type == EventType.MouseDown)
            {
                using var down = PointerDownEvent.GetPooled(source);
                map.SendEvent(down);
            }
            else if (type == EventType.MouseUp)
            {
                using var up = PointerUpEvent.GetPooled(source);
                map.SendEvent(up);
            }
            else
            {
                using var move = PointerMoveEvent.GetPooled(source);
                map.SendEvent(move);
            }
        }
    }
}
#endif
