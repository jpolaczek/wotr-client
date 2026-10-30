using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-404 (TABLETOP_FIGURES_UX, "Flight profile"): a moving figure lifts in place (0–15 %), flies in an arc of
    // clamp(.12 × distance, 20, 80) px over the raised pose with its shadow on the ground (15–85 %) and lands on its place
    // with a short settle (85–100 %); its last frame is the standing figure, so nothing snaps. A figure handed over raised
    // begins at the arc, and one taken over mid-air continues from its height. The duration bounds are unchanged.
    public sealed class FlightProfileTests
    {
        private const float Arc = 48;

        [Test]
        public void TheDurationAndTheArcKeepTheirBounds()
        {
            Assert.AreEqual(.65f, FlightProfile.Duration(0), 1e-5f);
            Assert.AreEqual(.85f, FlightProfile.Duration(320), 1e-5f);
            Assert.AreEqual(1.05f, FlightProfile.Duration(5000), 1e-5f);
            Assert.AreEqual(20, FlightProfile.ArcHeight(40), 1e-4f, "A short hop still clears the table.");
            Assert.AreEqual(48, FlightProfile.ArcHeight(400), 1e-4f, ".12 × the distance.");
            Assert.AreEqual(80, FlightProfile.ArcHeight(2000), 1e-4f, "A long flight never leaves the map's scale.");
        }

        [Test]
        public void AStandingFigureLiftsInPlaceArcsAndLandsWithoutASnap()
        {
            var start = FlightStart.Of(Vector2.zero, FlightPose.Standing);
            Assert.AreEqual(0, FlightProfile.StartsAt(start));
            AssertStanding(FlightProfile.At(0, start, Arc), 0, "It starts standing on its source's place.");

            var lifting = FlightProfile.At(.1f, start, Arc);
            Assert.AreEqual(0, lifting.Progress, 1e-5f, "The lift is in place.");
            Assert.Greater(lifting.Lift, .5f);
            Assert.AreEqual(0, lifting.Height, 1e-5f);
            var lifted = FlightProfile.At(FlightProfile.LiftEnd, start, Arc);
            Assert.AreEqual(1, lifted.Lift, 1e-5f, "At 15 % it is in the raised pose.");
            Assert.AreEqual(FlightProfile.LiftScale, FlightProfile.Scale(lifted), 1e-5f);
            Assert.AreEqual(FlightProfile.Rise, FlightProfile.Raise(lifted, 1), 1e-4f);
            Assert.AreEqual(FlightProfile.ShadowScale, FlightProfile.ShadowSize(lifted), 1e-5f, "Its shadow stays on the table.");
            Assert.AreEqual(FlightProfile.ShadowOpacity, FlightProfile.ShadowAlpha(lifted, 1), 1e-5f);

            var top = FlightProfile.At(.5f, start, Arc);
            Assert.AreEqual(.5f, top.Progress, 1e-4f, "Half way along the ground…");
            Assert.AreEqual(Arc, top.Height, 1e-3f, "…at the arc's top.");
            Assert.Less(FlightProfile.ShadowAlpha(top, 1), FlightProfile.ShadowOpacity, "High up the shadow is fainter.");
            AssertArcRisesThenFalls(start);

            var touchdown = FlightProfile.At(FlightProfile.LandingStart, start, Arc);
            Assert.AreEqual(1, touchdown.Progress, 1e-5f, "The arc ends over the destination's place.");
            Assert.AreEqual(0, touchdown.Height, 1e-4f);
            Assert.AreEqual(1, touchdown.Lift, 1e-5f, "Then it comes down from the raised pose.");
            var settling = FlightProfile.At(.96f, start, Arc);
            Assert.AreEqual(0, settling.Lift, 1e-5f, "Down on its place…");
            Assert.Greater(settling.Settle, 0, "…it settles with a short hop.");
            Assert.LessOrEqual(FlightProfile.Raise(settling, 1), FlightProfile.SettleHop + 1e-4f);
            AssertStanding(FlightProfile.At(1, start, Arc), 1, "The last frame is the figure standing on its place.");
        }

        [Test]
        public void AFigureHandedOverRaisedBeginsAtTheArcFromItsPose()
        {
            var start = FlightStart.Of(new Vector2(10, 20), FlightPose.Raised);
            Assert.AreEqual(FlightProfile.LiftEnd, FlightProfile.StartsAt(start), "No second lift: the flight skips it.");
            var first = FlightProfile.At(FlightProfile.StartsAt(start), start, Arc);
            Assert.AreEqual(0, first.Progress, 1e-5f, "It leaves from where it is held…");
            Assert.AreEqual(1, first.Lift, 1e-5f, "…in the hand's pose:");
            Assert.AreEqual(1.1f, FlightProfile.Scale(first), 1e-5f, "1.10 × round its feet,");
            Assert.AreEqual(12, FlightProfile.Raise(first, 1), 1e-4f, "12 design points up,");
            Assert.AreEqual(.8f, FlightProfile.ShadowSize(first), 1e-5f, "its shadow at .8");
            Assert.AreEqual(.35f, FlightProfile.ShadowAlpha(first, 1), 1e-5f, "and .35.");
            AssertStanding(FlightProfile.At(1, start, Arc), 1, "It lands like any flight.");
        }

        [Test]
        public void AFlightTakenOverMidAirContinuesFromItsHeight()
        {
            var start = new FlightStart(Vector2.zero, 1, 30);
            Assert.AreEqual(FlightProfile.LiftEnd, FlightProfile.StartsAt(start), "Already up: no lift.");
            var first = FlightProfile.At(FlightProfile.LiftEnd, start, Arc);
            Assert.AreEqual(30, first.Height, 1e-4f, "It goes on from the height it was at.");
            Assert.AreEqual(1, first.Lift, 1e-5f);
            AssertStanding(FlightProfile.At(1, start, Arc), 1, "And lands on its new place.");

            var landing = new FlightStart(Vector2.zero, .4f, 0);
            Assert.AreEqual(0, FlightProfile.StartsAt(landing), "Taken over while landing, it rises again first.");
            Assert.AreEqual(.4f, FlightProfile.At(0, landing, Arc).Lift, 1e-5f, "From the pose it was in.");
        }

        private static void AssertArcRisesThenFalls(FlightStart start)
        {
            var previous = 0f;
            for (var t = .2f; t <= .5f; t += .05f)
            {
                var frame = FlightProfile.At(t, start, Arc);
                Assert.GreaterOrEqual(frame.Height, previous - 1e-4f, "Rising to the top at " + t);
                Assert.GreaterOrEqual(frame.Progress, 0);
                previous = frame.Height;
            }
            for (var t = .55f; t <= .85f; t += .05f)
            {
                var frame = FlightProfile.At(t, start, Arc);
                Assert.LessOrEqual(frame.Height, previous + 1e-4f, "Falling from the top at " + t);
                previous = frame.Height;
            }
        }

        private static void AssertStanding(FlightFrame frame, float progress, string message)
        {
            Assert.AreEqual(progress, frame.Progress, 1e-5f, message);
            Assert.AreEqual(0, frame.Lift, 1e-5f, message);
            Assert.AreEqual(0, frame.Height, 1e-5f, message);
            Assert.AreEqual(0, frame.Settle, 1e-5f, message);
            Assert.AreEqual(1, FlightProfile.Scale(frame), 1e-5f, message);
            Assert.AreEqual(0, FlightProfile.Raise(frame, 1), 1e-5f, message);
            Assert.AreEqual(1, FlightProfile.ShadowSize(frame), 1e-5f, message);
            Assert.AreEqual(1, FlightProfile.ShadowAlpha(frame, 1), 1e-5f, message);
        }
    }
}
