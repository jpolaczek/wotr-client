using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-174: the physical edges of a full-screen game bound edge panning.
    public sealed class MapScreenPointerTests
    {
        private static readonly Vector2 Screen = new Vector2(1920, 1200);

        [Test]
        public void AWindowLosesTheCursorOnePixelOutside()
        {
            Assert.IsTrue(MapScreenPointer.TryInside(new Vector2(1920, 1200), Screen, false, out var inside));
            Assert.AreEqual(new Vector2(1920, 1200), inside);
            Assert.IsFalse(MapScreenPointer.TryInside(new Vector2(-1, 600), Screen, false, out _));
            Assert.IsFalse(MapScreenPointer.TryInside(new Vector2(960, 1201), Screen, false, out _));
        }

        [Test]
        public void AFullScreenGameHoldsACursorBeyondThePictureAtItsEdge()
        {
            Assert.IsTrue(MapScreenPointer.TryInside(new Vector2(960, -60), Screen, true, out var bottom));
            Assert.AreEqual(new Vector2(960, 0), bottom, "A black bar below the picture holds at its bottom edge.");
            Assert.IsTrue(MapScreenPointer.TryInside(new Vector2(2400, 1300), Screen, true, out var corner));
            Assert.AreEqual(Screen, corner);
            Assert.IsTrue(MapScreenPointer.TryInside(new Vector2(700, 500), Screen, true, out var middle));
            Assert.AreEqual(new Vector2(700, 500), middle, "A cursor on the picture is not moved.");
        }

        [Test]
        public void AnEmptyScreenNeverPans()
        {
            Assert.IsFalse(MapScreenPointer.TryInside(Vector2.zero, Vector2.zero, true, out _));
            Assert.IsFalse(MapScreenPointer.TryInside(Vector2.zero, new Vector2(0, 1200), false, out _));
        }

        [Test]
        public void OnlyAFullScreenModeConfinesTheCursor()
        {
            Assert.AreEqual(CursorLockMode.Confined, DisplayController.CursorLock(WindowMode.Fullscreen));
            Assert.AreEqual(CursorLockMode.Confined, DisplayController.CursorLock(WindowMode.Borderless));
            Assert.AreEqual(CursorLockMode.None, DisplayController.CursorLock(WindowMode.Windowed));
        }
    }
}
