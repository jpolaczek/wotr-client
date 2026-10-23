using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-305: the presentation camera's geometry — the safe map area around the HUD and open panels, and the eased travel
    // between two framings.
    public sealed class PresentationCameraTests
    {
        private static readonly Rect Map = new Rect(0, 0, 1366, 768);

        [Test]
        public void TheSafeAreaLeavesTheTopBarTheConsoleAndTheRightColumnOut()
        {
            var top = new Rect(0, 0, 1366, 44);
            var console = new Rect(0, 628, 986, 140);
            var right = new Rect(986, 0, 380, 768);
            var safe = SafeMapArea.Largest(Map, new[] { top, console, right });
            Assert.AreEqual(Rect.MinMaxRect(0, 44, 986, 628), safe);
        }

        [Test]
        public void AnOpenPanelInTheMiddleLeavesTheLargerBandBesideIt()
        {
            var panel = new Rect(300, 100, 400, 500);
            var safe = SafeMapArea.Largest(Map, new[] { panel });
            Assert.AreEqual(Rect.MinMaxRect(700, 0, 1366, 768), safe, "The right band is larger than the left one.");
        }

        [Test]
        public void ASmallControlInACornerCostsOnlyItsCheaperStrip()
        {
            var legend = new Rect(8, 720, 32, 32);
            var safe = SafeMapArea.Largest(Map, new[] { legend });
            Assert.AreEqual(Rect.MinMaxRect(40, 0, 1366, 768), safe, "A 40 pt strip on the left loses less than 48 pt below.");
        }

        [Test]
        public void ATravelStartsAndEndsAtItsFramingsAndKeepsTheFocusLine()
        {
            var focus = new Vector2(500, 330);
            var travel = new CameraTravel(.5f, new Vector2(10, 20), 1f, new Vector2(-300, -100), focus);
            var start = travel.At(0);
            var end = travel.At(1);
            Assert.AreEqual(.5f, start.Zoom, 1e-5f);
            Assert.AreEqual(10, start.Pan.x, 1e-3f);
            Assert.AreEqual(1f, end.Zoom, 1e-5f);
            Assert.AreEqual(-300, end.Pan.x, 1e-3f);
            Assert.AreEqual(-100, end.Pan.y, 1e-3f);
            var middle = travel.At(.5);
            Assert.Greater(middle.Zoom, .5f);
            Assert.Less(middle.Zoom, 1f);
        }

        [Test]
        public void ANearMoveTakesTheShortestTimeAndAFarOrDeepOneTheLongest()
        {
            var focus = new Vector2(500, 330);
            var near = new CameraTravel(1, Vector2.zero, 1, new Vector2(-20, 0), focus);
            var far = new CameraTravel(1, Vector2.zero, 1, new Vector2(-2000, 0), focus);
            var deep = new CameraTravel(.5f, Vector2.zero, 1.5f, Vector2.zero, focus);
            Assert.AreEqual(CameraTravel.Shortest, near.Duration, .02);
            Assert.AreEqual(CameraTravel.Longest, far.Duration, 1e-6);
            Assert.AreEqual(CameraTravel.Longest, deep.Duration, 1e-6);
        }
    }
}
