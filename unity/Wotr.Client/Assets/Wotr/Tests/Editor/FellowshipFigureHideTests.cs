using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-280: the revealed Fellowship's figure is the hide's object on its region as on the Mordor track: it glows
    // while hiding is offered and its click hides; otherwise the click inspects the figure.
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
    }
}
