using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-180: the screen's edges hold the cursor in a full screen with focus; the native calls are not exercised here.
    public sealed class CursorBarrierTests
    {
        private static readonly Rect Screen = new Rect(-1920, 0, 1920, 1080);

        [Test]
        public void ACursorThatLeavesTheScreenReturnsToItsNearestEdge()
        {
            Assert.IsTrue(CursorBarrier.Clamp(new Vector2(40, 500), Screen, out var right));
            Assert.AreEqual(new Vector2(-1, 500), right, "Past the right edge, onto the last column.");
            Assert.IsTrue(CursorBarrier.Clamp(new Vector2(-2400, -30), Screen, out var corner));
            Assert.AreEqual(new Vector2(-1920, 0), corner);
            Assert.IsTrue(CursorBarrier.Clamp(new Vector2(-900, 1300), Screen, out var bottom));
            Assert.AreEqual(new Vector2(-900, 1079), bottom);
        }

        [Test]
        public void ACursorOnTheScreenStaysWhereItIs()
        {
            Assert.IsFalse(CursorBarrier.Clamp(new Vector2(-900, 500), Screen, out var inside));
            Assert.AreEqual(new Vector2(-900, 500), inside);
            Assert.IsFalse(CursorBarrier.Clamp(new Vector2(-1920, 0), Screen, out _), "The first column and row are on the screen.");
        }

        [Test]
        public void OnlyAFocusedFullScreenPlayerHoldsTheCursor()
        {
            Assert.IsTrue(CursorBarrier.Active(true, WindowMode.Fullscreen, true));
            Assert.IsTrue(CursorBarrier.Active(true, WindowMode.Borderless, true));
            Assert.IsFalse(CursorBarrier.Active(true, WindowMode.Windowed, true), "A window lets the cursor go.");
            Assert.IsFalse(CursorBarrier.Active(true, WindowMode.Fullscreen, false), "Losing focus frees the cursor.");
            Assert.IsFalse(CursorBarrier.Active(false, WindowMode.Fullscreen, true), "The editor's game view never holds it.");
        }
    }
}
