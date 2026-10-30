using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-280: the revealed Fellowship's figure is the hide's object on its region as on the Mordor track: it glows
    // while hiding is offered and its click hides; otherwise the click inspects the figure. UI-427: it shows its state on
    // a plaque; UI-428: a drop on it lands in the region it stands in.
    public sealed class FellowshipFigureHideTests
    {
        [TestCase(null)]
        [TestCase(2)]
        public void TheRevealedFigureHidesOnItsRegionAndOnTheTrack(int? step)
        {
            var figure = new MapCanvas(BoardPresentation.Load()).FellowshipMarker;
            figure.Apply(new FellowshipView { RegionId = "rivendell", Hidden = false, MordorStep = step });
            var hides = 0;
            var inspections = 0;
            figure.HideRequested += () => hides++;
            figure.Clicked += () => inspections++;

            figure.Activate();
            Assert.AreEqual((0, 1), (hides, inspections), "Without the offer the click inspects the figure.");
            figure.OfferHide(true);
            Assert.IsTrue(figure.Root.ClassListContains(MapFellowshipMarker.HideClass));
            figure.Activate();
            Assert.AreEqual((1, 1), (hides, inspections), "With the offer the click hides.");
            figure.OfferHide(false);
            Assert.IsFalse(figure.Root.ClassListContains(MapFellowshipMarker.HideClass));
        }

        // UI-282: off the Mordor track the hidden figure moves with one click while the chosen die can move it; every
        // offer (move, declaration, hide) wears the shared ChoiceGlow; on the track the move is the next step's.
        [Test]
        public void TheHiddenFigureMovesOffTheTrackAndEveryOfferGlows()
        {
            var figure = new MapCanvas(BoardPresentation.Load()).FellowshipMarker;
            figure.Apply(new FellowshipView { RegionId = "rivendell", Hidden = true });
            var moves = 0;
            var declarations = 0;
            var inspections = 0;
            figure.MoveRequested += () => moves++;
            figure.DeclareRequested += () => declarations++;
            figure.Clicked += () => inspections++;
            Assert.IsFalse(figure.Root.ClassListContains(ChoiceGlow.ClassName), "Nothing offered, no glow.");

            figure.OfferMove(true);
            Assert.IsTrue(figure.Root.ClassListContains(MapFellowshipMarker.MoveClass));
            Assert.IsTrue(figure.Root.ClassListContains(ChoiceGlow.ClassName));
            figure.Activate();
            Assert.AreEqual((1, 0, 0), (moves, declarations, inspections), "The figure moves the Fellowship.");
            figure.OfferMove(false);
            figure.OfferDeclare(true);
            Assert.IsTrue(figure.Root.ClassListContains(ChoiceGlow.ClassName), "The declaration glows like the other offers.");
            figure.Activate();
            Assert.AreEqual((1, 1, 0), (moves, declarations, inspections), "The figure declares the position.");
            figure.OfferDeclare(false);
            Assert.IsFalse(figure.Root.ClassListContains(ChoiceGlow.ClassName));

            figure.Apply(new FellowshipView { RegionId = "rivendell", Hidden = true, MordorStep = 2 });
            figure.OfferMove(true);
            Assert.IsFalse(figure.Root.ClassListContains(ChoiceGlow.ClassName), "On the track the next step is the move.");
            figure.Activate();
            Assert.AreEqual((1, 1, 1), (moves, declarations, inspections));
        }

        // UI-427: the figure shows ◇ Hidden or ◆ Revealed on the plaque on its base's front — on its region and on the
        // Mordor track — and the plaque switches with the view; it is drawn over the figure and never takes the pointer.
        [TestCase(null)]
        [TestCase(3)]
        public void ThePlaqueOnTheBaseShowsHiddenOrRevealed(int? step)
        {
            var figure = new MapCanvas(BoardPresentation.Load()).FellowshipMarker;
            var plaque = figure.Root.Q(FellowshipStateBadge.ClassName);
            Assert.IsNotNull(plaque, "The figure carries its state plaque.");
            Assert.AreSame(plaque, figure.State);
            Assert.AreSame(plaque, figure.Root[figure.Root.childCount - 1], "Drawn last: nothing of the figure covers it.");
            Assert.AreEqual(PickingMode.Ignore, plaque.pickingMode, "It takes no click of its own (UI-280).");
            Assert.AreEqual(PickingMode.Ignore, plaque.Q(FellowshipStateBadge.SymbolClass).pickingMode);
            Assert.AreEqual("", ((Button)figure.Root).text, "No text drawn behind the base.");

            figure.Apply(new FellowshipView { RegionId = "rivendell", Hidden = true, MordorStep = step });
            AssertState(figure, true);
            figure.Apply(new FellowshipView { RegionId = "rivendell", Hidden = false, MordorStep = step });
            AssertState(figure, false);
            figure.Apply(new FellowshipView { RegionId = "rivendell", Hidden = true, MordorStep = step });
            AssertState(figure, true);
        }

        // UI-427: far out the plaque never draws smaller than on a figure at 0.62 of its design size; it grows down from the
        // feet line, so its lower edge goes below the figure's 80-point box only then.
        [Test]
        public void FarOutThePlaqueKeepsAReadableSize()
        {
            Assert.AreEqual(1, FellowshipStateBadge.Growth(.75f), "Zoomed in on a region it is its design size.");
            Assert.AreEqual(1, FellowshipStateBadge.Growth(80 / 72f), "On the track too.");
            Assert.AreEqual(.62f / .446f, FellowshipStateBadge.Growth(.446f), 1e-4f, "The whole map of the worst case.");
            Assert.AreEqual(80, FellowshipStateBadge.Bottom(.75f), 1e-4f);
            Assert.Greater(FellowshipStateBadge.Bottom(.446f), 80);
            Assert.AreEqual(.62f, FellowshipStateBadge.Growth(.446f) * .446f, 1e-4f, "Drawn as on a figure at 0.62.");
        }

        // UI-428: a figure carried onto the Fellowship's is dropped on the region it stands in: its region, or on the Mordor
        // track the region under its step.
        [Test]
        public void TheFiguresTokenStandsInItsRegionOrUnderItsStep()
        {
            var map = new MapCanvas(BoardPresentation.Load());
            var figure = map.FellowshipMarker;
            figure.Apply(new FellowshipView { RegionId = "rivendell", Hidden = true });
            var token = (DraftToken)figure.Root.userData;
            Assert.AreEqual(DraftObject.Fellowship, token.Object);
            Assert.AreEqual("rivendell", token.StandsIn);
            Assert.AreEqual(new DraftToken(DraftObject.Region, "rivendell").StandsIn, token.AsRegion().StandsIn);
            Assert.AreEqual(DraftObject.Region, token.AsRegion().Object, "A drop on it is a drop on its region.");

            figure.Apply(new FellowshipView { RegionId = "mordor-track", Hidden = true, MordorStep = 2 });
            token = (DraftToken)figure.Root.userData;
            var under = map.Data.Contours.HitTest(map.Data.MordorTrack.PointAt(2));
            Assert.IsNotNull(under, "The step lies in a region of the map.");
            Assert.AreEqual(under, token.StandsIn);
        }

        private static void AssertState(MapFellowshipMarker figure, bool hidden)
        {
            Assert.AreEqual(hidden, figure.Hidden);
            Assert.AreEqual(hidden ? "◇" : "◆", figure.Symbol);
            Assert.AreEqual(hidden, FellowshipStateBadge.ShowsHidden(figure.State));
            Assert.AreEqual(hidden, figure.State.ClassListContains(FellowshipStateBadge.HiddenClass));
            Assert.AreEqual(!hidden, figure.State.ClassListContains(FellowshipStateBadge.RevealedClass));
        }
    }
}
