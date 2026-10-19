using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-293 (owner's decision, after UI-232): the Fellowship's marker on its track is the Hide control; the Ring on the
    // plaque and in the panel's header is a picture of the condition only.
    public sealed class FellowshipTrackMarkerTests
    {
        [Test]
        public void TheTrackMarkerHidesOnlyWhileHidingIsOfferedAndTheRingsArePictures()
        {
            var status = new FellowshipStatus(() => { });
            var marker = status.HideMarker;
            Assert.AreEqual("journey-track-marker", marker.name);
            Assert.AreEqual(PickingMode.Ignore, marker.pickingMode, "A plain picture until hiding is legal.");
            Assert.IsFalse(marker.ClassListContains(ChoiceGlow.ClassName));

            status.OfferHide(true, FellowshipControls.HideDescription);
            Assert.AreEqual(PickingMode.Position, marker.pickingMode);
            Assert.IsTrue(marker.focusable);
            Assert.IsTrue(marker.ClassListContains(ChoiceGlow.ClassName));
            Assert.IsTrue(marker.ClassListContains(PressFeedback.PressableClass), "It presses in like every control.");
            StringAssert.StartsWith("Click the Fellowship's marker on its track to hide", marker.tooltip);
            foreach (var name in new[] { "fellowship-ring", "fellowship-panel-ring" })
            {
                var ring = status.Summary.Q(name) ?? status.StateRow.Q(name);
                Assert.IsNotNull(ring, name);
                Assert.IsNotInstanceOf<Button>(ring, "The Ring is a picture, not a control.");
                Assert.AreEqual(PickingMode.Ignore, ring.pickingMode, "A click on it reaches its host.");
                Assert.IsFalse(ring.ClassListContains(ChoiceGlow.ClassName));
            }
            Assert.IsFalse(status.Summary.ClassListContains(FellowshipStatus.HideClass), "The plaque does not hide.");

            status.OfferHide(false, "");
            Assert.AreEqual(PickingMode.Ignore, marker.pickingMode);
            Assert.IsFalse(marker.ClassListContains(ChoiceGlow.ClassName));
            Assert.AreEqual("", marker.tooltip);
        }
    }
}
