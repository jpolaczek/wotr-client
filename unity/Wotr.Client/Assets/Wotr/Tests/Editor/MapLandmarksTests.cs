using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-234: the fortress drawings stand in their regions, clear of the forces' row and the region's name. UI-256: every
    // stronghold of the base board has one.
    public sealed class MapLandmarksTests
    {
        private const float NameHalfWidth = 36;
        private const float NameHalfHeight = 9;

        [Test]
        public void FortressDrawingsStandInTheirRegionsClearOfForcesAndNames()
        {
            var data = BoardPresentation.Load();
            var landmarks = MapLandmarks.Load();
            var strongholds = data.Regions.Values.Where(r => r.Settlement == "Stronghold").Select(r => r.Id);
            CollectionAssert.AreEquivalent(strongholds, landmarks.Items.Select(l => l.Id), "Every stronghold has its drawing.");
            Assert.AreEqual(16, landmarks.Items.Count);
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
                // UI-258: a drawing stands mostly on its own region; the two cities the owner placed by eye stand at least three quarters on it.
                var inside = InsideShare(shape, rect);
                Assert.GreaterOrEqual(inside, landmark.Id is "minas-tirith" or "minas-morgul" ? 0.75f : 0.5f, landmark.Id + " stands on its region");
            }
        }

        // UI-265: in a Fate of Erebor game Erebor's drawing is the Shadow-held one, in the same place; without Fate, and
        // for every other module, the base drawing stays. The board presentation names the shown game's modules.
        [Test]
        public void AFateGameDrawsEreborHeldByTheShadowInItsPlace()
        {
            var landmarks = MapLandmarks.Load();
            var erebor = landmarks.Items.Single(l => l.Id == "erebor");
            var shadow = erebor.VariantTextures["fate"];
            Assert.AreNotSame(erebor.Texture, shadow);
            Assert.AreSame(shadow, erebor.TextureFor(new[] { "fate" }));
            Assert.AreSame(erebor.Texture, erebor.TextureFor(new[] { "lords", "kings" }));
            Assert.AreSame(erebor.Texture, erebor.TextureFor(new string[0]));
            Assert.IsTrue(landmarks.Items.Where(l => l.Id != "erebor").All(l => l.VariantTextures.Count == 0));
            var data = BoardPresentation.Load();
            var rect = erebor.RectOf(shadow);
            Assert.AreEqual(erebor.Rect.center, rect.center);
            Assert.GreaterOrEqual(InsideShare(data.Shapes["erebor"], rect), 0.5f, "The Shadow's Erebor stands on its region");
            data.Apply(new PlayerView { Configuration = new MatchConfiguration { Fate = true, Kings = true } });
            CollectionAssert.AreEqual(new[] { "fate", "kings" }, data.Modules);
            data.ClearGameState();
            CollectionAssert.IsEmpty(data.Modules);
        }

        private static float InsideShare(RegionGeometry shape, Rect rect)
        {
            var inside = 0;
            for (var i = 0; i <= 8; i++)
            for (var j = 0; j <= 8; j++)
                if (MapContourMath.Contains(shape, new Vector2(rect.xMin + rect.width * i / 8, rect.yMin + rect.height * j / 8)))
                    inside++;
            return inside / 81f;
        }
    }
}
