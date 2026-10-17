using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-154: what the track shows comes from the public Fellowship view only; the figure walks the drawn path.
    public sealed class MordorTrackStateTests
    {
        [Test]
        public void OutsideMordorEveryStepWaitsAndTheCrackExplainsTheEnd()
        {
            var state = MordorTrackState.For(new FellowshipView { RegionId = "minas-morgul" });
            Assert.IsFalse(state.Active);
            Assert.IsTrue(Enumerable.Range(0, 6).All(step => state.Slab(step) == MordorSlabState.Ahead));
            StringAssert.StartsWith("Crack of Doom", state.SlabTooltip(5));
            StringAssert.StartsWith("Mordor · Step 2 of 5", state.SlabTooltip(2));
            // UI-157: a Fellowship off the track (in Gorgoroth too) learns where the track is entered.
            StringAssert.Contains(MordorTrackState.EntryHint, state.SlabTooltip(0));
            StringAssert.Contains(MordorTrackState.EntryHint, MordorTrackState.GateTooltip("morannon"));
            Assert.AreSame(MordorTrackState.Outside, MordorTrackState.For(null));
        }

        [Test]
        public void OnTheTrackStepsBehindArePassedAndTheFigureCarriesItsCorruption()
        {
            var state = MordorTrackState.For(new FellowshipView
            {
                RegionId = "mordor-track", MordorStep = 3, Hidden = true, Corruption = 7, MordorAttemptedThisTurn = false
            });
            Assert.IsTrue(state.Active);
            CollectionAssert.AreEqual(new[] { MordorSlabState.Passed, MordorSlabState.Passed, MordorSlabState.Passed,
                MordorSlabState.Current, MordorSlabState.Ahead, MordorSlabState.Ahead }, Enumerable.Range(0, 6).Select(state.Slab));
            StringAssert.Contains("The Fellowship is here", state.SlabTooltip(3));
            StringAssert.Contains("Passed", state.SlabTooltip(1));
            StringAssert.DoesNotContain(MordorTrackState.EntryHint, state.SlabTooltip(4));
            StringAssert.Contains("Hidden · Mordor step 3 of 5", state.FigureTooltip);
            StringAssert.Contains("Corruption 7/12", state.FigureTooltip);
            StringAssert.Contains("No move or hide attempted", state.FigureTooltip);
        }

        [Test]
        public void TheFigureWalksThePathBetweenSteps()
        {
            var track = BoardPresentation.Load().MordorTrack;
            for (var step = 0; step <= MordorTrackGeometry.LastStep; step++)
                Assert.Less(Vector2.Distance(track.PointAt(step), track.Step(step)), .01f, "Step " + step);
            var halfway = track.PointAt(4.5f);
            Assert.Greater(Vector2.Distance(halfway, track.Step(4)), 5f);
            Assert.Greater(Vector2.Distance(halfway, track.Step(5)), 5f);
            var walked = Enumerable.Range(0, 11).Select(i => track.PointAt(2 + i / 10f)).ToList();
            for (var i = 1; i < walked.Count; i++)
                Assert.Greater(Vector2.Distance(walked[i], track.Step(2)), Vector2.Distance(walked[i - 1], track.Step(2)) - .01f,
                    "The walk leaves step 2 steadily.");
        }
    }
}
