using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-234: the fortress drawings stand in their regions, clear of the forces' row and the region's name.
    public sealed class MapLandmarksTests
    {
        private const float NameHalfWidth = 36;
        private const float NameHalfHeight = 9;

        [Test]
        public void FortressDrawingsStandInTheirRegionsClearOfForcesAndNames()
        {
            var data = BoardPresentation.Load();
            var landmarks = MapLandmarks.Load();
            CollectionAssert.AreEquivalent(new[] { "minas-tirith", "minas-morgul", "helms-deep" }, landmarks.Items.Select(l => l.Id));
            foreach (var landmark in landmarks.Items)
            {
                var shape = data.Shapes[landmark.Id];
                var rect = landmark.Rect;
                Assert.IsTrue(MapContourMath.Contains(shape, rect.center), landmark.Id + " stands in its region");
                var anchor = new Vector2(shape.Anchor[0], shape.Anchor[1]);
                var settled = data.Regions[landmark.Id].Settlement != null;
                foreach (var corner in new[] { rect.min, rect.max, new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMax, rect.yMin), rect.center })
                    Assert.IsFalse(MapRegionNamePoints.InForcesZone(anchor, settled, corner), landmark.Id + " keeps clear of the forces at " + corner);
                var name = MapRegionNamePoints.For(shape, settled).Point;
                var label = new Rect(name.x - NameHalfWidth, name.y - NameHalfHeight, NameHalfWidth * 2, NameHalfHeight * 2);
                Assert.IsFalse(rect.Overlaps(label), landmark.Id + " keeps clear of its name at " + name);
                Assert.GreaterOrEqual(rect.width, 30, landmark.Id + " is large enough to read");
            }
        }
    }
}
