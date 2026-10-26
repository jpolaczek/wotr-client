using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-186: every region of the board has a point for its name inside it, clear of its borders and, where the
    // region has the room, clear of its settlement and of the forces that stand by it.
    public sealed class MapRegionNamePointsTests
    {
        [Test]
        public void EveryRegionNameStandsInsideItsRegionClearOfTheForces()
        {
            var data = BoardPresentation.Load();
            var watch = Stopwatch.StartNew();
            var points = data.Regions.Values.Where(r => data.Shapes.ContainsKey(r.Id))
                .ToDictionary(r => r.Id, r => MapRegionNamePoints.For(data.Shapes[r.Id], r.Settlement != null));
            watch.Stop();
            Assert.AreEqual(data.Shapes.Count, points.Count, "Every region of the map has a name point.");
            // Run once per session for the whole board (MapRegionNames caches it); measured here, recorded in TESTING.
            TestContext.WriteLine($"Name points for {points.Count} regions: {watch.ElapsedMilliseconds} ms");
            Assert.Less(watch.ElapsedMilliseconds, 500, "The one-off search stays short (about 150 ms when measured).");
            var crowded = 0;
            foreach (var entry in points)
            {
                var shape = data.Shapes[entry.Key];
                var point = entry.Value;
                Assert.IsTrue(MapContourMath.Contains(shape, point.Point), entry.Key + " lies inside its region");
                Assert.GreaterOrEqual(MapRegionNamePoints.Clearance(shape, point.Point), 5, entry.Key + " keeps clear of its borders");
                Assert.Greater(point.HalfWidth, 0, entry.Key);
                Assert.Greater(point.HalfHeight, 0, entry.Key);
                var anchor = new Vector2(shape.Anchor[0], shape.Anchor[1]);
                if (MapRegionNamePoints.InForcesZone(anchor, data.Regions[entry.Key].Settlement != null, point.Point))
                    crowded++;
            }
            TestContext.WriteLine($"Names left by the forces: {crowded} of {points.Count}");
            Assert.LessOrEqual(crowded, points.Count / 10, "Only small regions put their name where the forces stand.");
        }

        // UI-363 (owner report): a small region whose forces' zone nearly fills it still shows its name at a reading zoom;
        // a name that fits beside the forces stays there.
        [Test]
        public void ASmallRegionsNameCanBeReadAtTheClosestZoom()
        {
            var data = BoardPresentation.Load();
            foreach (var id in new[] { "western-mirkwood", "rhosgobel", "narrows-of-the-forest" })
            {
                var region = data.Regions[id];
                var point = MapRegionNames.Point(region, data.Shapes[id]);
                var beside = MapRegionNamePoints.For(data.Shapes[id], region.Settlement != null);
                Assert.IsFalse(MapRegionNames.Readable(region.Name, beside, 11), id + " has no room for its name beside the forces");
                Assert.IsTrue(MapRegionNames.Readable(region.Name, point, 11), id + " can be read at its roomiest point");
            }
            var eastern = data.Regions["eastern-mirkwood"];
            Assert.AreEqual(MapRegionNamePoints.For(data.Shapes[eastern.Id], eastern.Settlement != null).Point,
                MapRegionNames.Point(eastern, data.Shapes[eastern.Id]).Point, "A name that fits beside the forces stays there.");
            var unreadable = data.Regions.Values.Where(r => data.Shapes.ContainsKey(r.Id) &&
                !MapRegionNames.Readable(r.Name, MapRegionNames.Point(r, data.Shapes[r.Id]), r.Settlement is "Stronghold" or "City" ? 12.5f : 11));
            TestContext.WriteLine("Names without room anywhere: " + string.Join(", ", unreadable.Select(r => r.Id)));
        }

        [Test]
        public void ALongNameBreaksAtItsMiddleSpaceWhenItsRegionIsNarrow()
        {
            var narrow = new MapNamePoint(Vector2.zero, 40, 30);
            var wide = new MapNamePoint(Vector2.zero, 400, 30);
            Assert.AreEqual("Narrows of\nthe Forest", MapRegionNames.Lines("Narrows of the Forest", narrow, 11));
            Assert.AreEqual("Narrows of the Forest", MapRegionNames.Lines("Narrows of the Forest", wide, 11));
            Assert.AreEqual("Lothlórien", MapRegionNames.Lines("Lothlórien", narrow, 11), "One word never breaks.");
        }
    }
}
