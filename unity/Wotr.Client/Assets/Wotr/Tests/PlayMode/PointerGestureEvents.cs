using UnityEngine;
using UnityEngine.UIElements;
using NUnit.Framework;

namespace Wotr.PlayTests
{
    internal static class PointerGestureEvents
    {
        public static void ClickAt(VisualElement expected, int count = 1)
        {
            Assert.IsNotNull(expected);
            var point = expected.worldBound.center;
            var root = expected.panel.visualTree;
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point }))
                root.SendEvent(move);
            var picked = expected.panel.Pick(point);
            Assert.IsTrue(picked == expected || expected.Contains(picked),
                $"{expected.name} is covered by {picked?.name} ({picked?.GetType().Name}).");
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point, clickCount = count }))
                picked.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point, clickCount = count }))
                picked.SendEvent(up);
        }

        public static void Press(VisualElement target)
        {
            var root = target.panel.visualTree;
            foreach (var point in new[] { root.worldBound.min + Vector2.one, target.worldBound.center })
                using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point }))
                    root.SendEvent(move);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0,
                mousePosition = target.worldBound.center, clickCount = 1 }))
                target.SendEvent(down);
        }

        public static void Release(VisualElement target)
        {
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0,
                mousePosition = target.worldBound.center, clickCount = 1 }))
                target.SendEvent(up);
        }
    }
}
