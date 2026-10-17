using NUnit.Framework;
using Wotr.Client;

namespace Wotr.EditorTests
{
    public sealed class MapMarkerVisibilityTests
    {
        // UI-126: markers are always shown; zoom picks tokens or figures with hysteresis, while inspection and
        // selection show figures for their own region only.
        [Test]
        public void ZoomPicksTheLevelWithHysteresisWhileInspectionAndSelectionStayLocal()
        {
            var visibility = new MapMarkerVisibility();
            visibility.UpdateZoom(1);
            Assert.IsFalse(visibility.Figures("minas-tirith"));
            visibility.InspectedRegion = "minas-tirith";
            Assert.IsTrue(visibility.Figures("minas-tirith"));
            Assert.IsFalse(visibility.Figures("osgiliath"));
            visibility.InspectedRegion = "osgiliath";
            Assert.IsTrue(visibility.Figures("minas-tirith", selected: true));
            Assert.IsFalse(visibility.Figures("minas-tirith"));
            visibility.UpdateZoom(1.29f);
            Assert.IsFalse(visibility.Figures("dale"));
            visibility.UpdateZoom(MapMarkerVisibility.FiguresFrom);
            Assert.IsTrue(visibility.Figures("dale"));
            visibility.UpdateZoom(1.25f);
            Assert.IsTrue(visibility.Figures("dale"), "Between the thresholds the level does not flicker.");
            visibility.UpdateZoom(1.19f);
            Assert.IsFalse(visibility.Figures("dale"));
            Assert.IsTrue(visibility.Figures("osgiliath"));
            visibility.InspectedRegion = "";
            Assert.IsFalse(visibility.Figures("osgiliath"));
        }
    }
}
