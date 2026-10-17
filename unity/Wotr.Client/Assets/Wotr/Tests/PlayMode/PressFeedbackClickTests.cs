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
    // UI-114: a held control shows the pressed state; the action belongs to the release inside it.
    public sealed class PressFeedbackClickTests
    {
        [UnityTest]
        public IEnumerator ControlsPressWhileHeldAndActOnRelease()
        {
            using var client = new MountedMatchClient("http://localhost", "press-feedback");
            client.Apply(BattleScreenFixture.Create(CommandKind.TakeBattleLosses));
            yield return Settle();
            // The figures live in the battle window; a press reaches them only once it is open.
            client.Board.Battles.Open();
            yield return Settle();
            var figures = client.Board.Root.Query<Button>(className: "loss-figure").ToList();
            Assert.AreSame(figures[0], client.Board.Root.panel.Pick(figures[0].worldBound.center), "The first figure is under the pointer.");
            Assert.GreaterOrEqual(figures.Count, 2, "The losses fixture offers figures to press.");

            // BW01: pressed on the way down, no action; the release inside chooses the figure.
            var first = figures[0];
            // UI-201: a loss figure stands in a glow; at rest its tile has no frame or fill.
            Assert.IsNotNull(first.Q("figure-glow"));
            Assert.AreEqual(0, first.resolvedStyle.borderTopWidth);
            PointerGestureEvents.Press(first);
            yield return Settle();
            Assert.IsTrue(first.ClassListContains(PressFeedback.PressedClass), "The held figure sinks · last down: " + client.Interface.Presses.LastDown + " · last release: " + client.Interface.Presses.LastRelease);
            Assert.IsFalse(first.ClassListContains("selected"), "Nothing happens before the release.");
            LayoutTests.Save(client.Viewport, "ui114-pressed-loss-figure.png");
            PointerGestureEvents.Release(first);
            yield return Settle();
            Assert.IsFalse(first.ClassListContains(PressFeedback.PressedClass));
            Assert.IsTrue(first.ClassListContains("selected"), "The release inside the figure assigns the loss.");

            // Leaving the control releases the state and the later release outside is not a click.
            var second = figures[1];
            PointerGestureEvents.Press(second);
            yield return null;
            Assert.IsTrue(second.ClassListContains(PressFeedback.PressedClass));
            var outside = client.Board.Root.worldBound.min + Vector2.one;
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = outside }))
                second.SendEvent(move);
            yield return null;
            Assert.IsFalse(second.ClassListContains(PressFeedback.PressedClass), "Leaving the figure lifts it.");
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = outside, clickCount = 1 }))
                second.SendEvent(up);
            yield return Settle();
            Assert.IsFalse(second.ClassListContains("selected"), "A release outside the figure is not a click.");

            // Enter/Space hold the state on the focused control.
            var third = figures.Last();
            third.Focus();
            using (var down = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space }))
                third.SendEvent(down);
            yield return null;
            Assert.IsTrue(third.ClassListContains(PressFeedback.PressedClass), "A held key presses the focused control.");
            using (var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Space }))
                third.SendEvent(up);
            yield return null;
            Assert.IsFalse(third.ClassListContains(PressFeedback.PressedClass));
        }

        [UnityTest]
        public IEnumerator TerrainDarkensWhileHeldAndIsChosenOnRelease()
        {
            using var client = new MountedMatchClient("http://localhost", "press-feedback-map");
            client.Apply(BattleScreenFixture.Create(CommandKind.ContinueBattle));
            yield return Settle();
            var map = client.Board.Map;
            string region = null;
            var point = Vector2.zero;
            foreach (var shape in client.Board.Presentation.Shapes)
            {
                point = map.LocalToWorld(map.Camera.ToScreen(new Vector2(shape.Value.Anchor[0], shape.Value.Anchor[1])));
                if (map.worldBound.Contains(point) && map.panel.Pick(point) == map && map.HitTest(map.WorldToLocal(point)) == shape.Key)
                {
                    region = shape.Key;
                    break;
                }
            }
            Assert.IsNotNull(region, "Some region is reachable under the pointer.");

            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point, clickCount = 1 }))
                map.SendEvent(down);
            yield return null;
            Assert.AreEqual(region, map.PressedRegion);
            Assert.AreEqual(MapRegionEmphasis.Pressed, map.Renderer.CurrentEmphasis(region), "The held terrain darkens.");
            Assert.AreNotEqual(region, map.Selected, "Nothing is chosen before the release.");
            LayoutTests.Save(client.Viewport, "ui114-pressed-region.png");

            // A drag cancels the press without choosing the region.
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point + new Vector2(24, 0) }))
                map.SendEvent(move);
            yield return null;
            Assert.AreEqual("", map.PressedRegion, "Moving away lifts the press.");
            Assert.AreNotEqual(MapRegionEmphasis.Pressed, map.Renderer.CurrentEmphasis(region));
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point + new Vector2(24, 0), clickCount = 1 }))
                map.SendEvent(up);
            yield return Settle();
            Assert.AreNotEqual(region, map.Selected, "A release after a drag is not a click.");

            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point, clickCount = 1 }))
                map.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point, clickCount = 1 }))
                map.SendEvent(up);
            yield return Settle();
            Assert.AreEqual(region, map.Selected, "The release on the held terrain chooses it.");
            Assert.AreEqual("", map.PressedRegion);
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 8; frame++)
                yield return null;
        }
    }
}
