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
            Assert.AreEqual(PickingMode.Ignore, status.Ring.pickingMode, "A plain picture lets the plaque open the panel.");
            Assert.IsFalse(status.Ring.ClassListContains(ChoiceGlow.ClassName));
            status.OfferHide(true, FellowshipControls.HideDescription);
            foreach (var ring in new[] { status.Ring, status.PanelRing })
            {
                Assert.AreEqual(PickingMode.Position, ring.pickingMode);
                Assert.IsTrue(ring.ClassListContains(ChoiceGlow.ClassName));
                StringAssert.StartsWith("Click the Ring to hide", ring.tooltip);
            }
            Assert.IsFalse(status.Summary.ClassListContains(FellowshipStatus.HideClass), "No separate Hide control.");
            status.OfferHide(false, "");
            Assert.AreEqual(PickingMode.Ignore, status.PanelRing.pickingMode);
            Assert.AreEqual("", status.Ring.tooltip);
        }
    }
}
