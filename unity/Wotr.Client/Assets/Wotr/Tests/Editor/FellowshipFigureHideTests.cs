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
    }
}
