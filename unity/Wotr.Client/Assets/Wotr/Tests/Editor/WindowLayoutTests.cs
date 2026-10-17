using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    public sealed class WindowLayoutTests
    {
        [TestCase(-40, -20, 10, 20)]
        [TestCase(1000, 1000, 710, 420)]
        [TestCase(110, 220, 110, 220)]
        public void WindowStaysInsideUsableViewport(float x, float y, float expectedX, float expectedY)
        {
            Assert.AreEqual(new Vector2(expectedX, expectedY), WindowLayout.Clamp(new Vector2(x, y),
                new Vector2(300, 200), new Rect(10, 20, 1000, 600)));
        }

        [Test]
        public void OversizedWindowKeepsItsHeaderReachableAndSessionsAreIndependent()
        {
            Assert.AreEqual(new Vector2(10, 20), WindowLayout.Clamp(Vector2.one * 800,
                new Vector2(1200, 900), new Rect(10, 20, 1000, 600)));
            var session = new WindowLayout();
            session.Remember("cards", new Vector2(100, 50));
            Assert.IsFalse(new WindowLayout().TryGet("cards", out _));
            var resets = 0;
            session.Reset += () => resets++;
            session.Clear();
            Assert.AreEqual(1, resets);
            Assert.IsFalse(session.TryGet("cards", out _));
        }
    }
}
