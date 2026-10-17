using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-232: the Ring icon is the only Hide control; it is a plain picture until hiding is legal.
    public sealed class FellowshipRingControlTests
    {
        [Test]
        public void TheRingIconHidesOnlyWhileHidingIsOffered()
        {
            var status = new FellowshipStatus(() => { });
            Assert.IsFalse(status.Ring.focusable, "A plain picture until hiding is legal.");
            Assert.IsFalse(status.Ring.ClassListContains(ChoiceGlow.ClassName));
            status.OfferHide(true, FellowshipControls.HideDescription);
            foreach (var ring in new[] { status.Ring, status.PanelRing })
            {
                Assert.IsTrue(ring.focusable);
                Assert.IsTrue(ring.ClassListContains(ChoiceGlow.ClassName));
                StringAssert.StartsWith("Click the Ring to hide", ring.tooltip);
            }
            Assert.IsFalse(status.Summary.ClassListContains(FellowshipStatus.HideClass), "No separate Hide control.");
            status.OfferHide(false, "");
            Assert.IsFalse(status.PanelRing.focusable);
            Assert.AreEqual("", status.Ring.tooltip, "Before any projection the icon has nothing to describe.");
        }
    }
}
