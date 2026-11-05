using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-186: every region of the board has a point for its name inside it, clear of its borders. UI-422: the name keeps
    // clear of the stand places, its own region's and those of the neighbours whose figures reach over it.
    public sealed class MapRegionNamePointsTests
    {
        [Test]
        public void EveryRegionNameStandsInsideItsRegionClearOfTheStands()
        {
            var data = BoardPresentation.Load();
            var stands = MapStandSlots.Load(data);
            var watch = Stopwatch.StartNew();
            var points = data.Regions.Values.Where(r => data.Shapes.ContainsKey(r.Id))
                .ToDictionary(r => r.Id, r => MapRegionNames.Point(r, data.Shapes[r.Id], stands));
            watch.Stop();
            Assert.AreEqual(data.Shapes.Count, points.Count, "Every region of the map has a name point.");
            // Run once per session for the whole board (MapRegionNames caches it); measured here, recorded in TESTING.
            TestContext.WriteLine($"Name points for {points.Count} regions: {watch.ElapsedMilliseconds} ms");
            Assert.Less(watch.ElapsedMilliseconds, 500, "The one-off search stays short (about 150 ms when measured).");
            var figures = stands.Regions.Values.SelectMany(s => s.Places.Select(p => p.FigureRect(s.ShownScale))).ToList();
            foreach (var entry in points)
            {
                var shape = data.Shapes[entry.Key];
                var point = entry.Value;
                Assert.IsTrue(MapContourMath.Contains(shape, point.Point), entry.Key + " lies inside its region");
                Assert.GreaterOrEqual(MapRegionNamePoints.Clearance(shape, point.Point), 5, entry.Key + " keeps clear of its borders");
                Assert.Greater(point.HalfWidth, 0, entry.Key);
                Assert.Greater(point.HalfHeight, 0, entry.Key);
                var region = data.Regions[entry.Key];
                var name = MapRegionNames.BoardRect(point, MapRegionNames.BoardHalfSize(region.Name, MapRegionNames.LetterHeightOf(region)));
                Assert.IsFalse(figures.Any(rect => rect.Overlaps(name)), entry.Key + "'s name keeps clear of every stand's figures");
            }
        }

        // UI-422: a name keeps its point on the empty board unless an obstacle covers it there; a covered name searches
        // again around the obstacles.
        [Test]
        public void ANameMovesOnlyOffAnObstacle()
        {
            var shape = BoardPresentation.Load().Shapes["eastern-mirkwood"];
            var empty = MapRegionNamePoints.For(shape);
            var half = new Vector2(20, 6);
            Assert.AreEqual(empty.Point, MapRegionNamePoints.For(shape, half, _ => false).Point, "Nothing in the way: the point stays.");
            var block = new Rect(empty.Point - half, half * 2);
            var moved = MapRegionNamePoints.For(shape, half, rect => rect.Overlaps(block));
            Assert.AreNotEqual(empty.Point, moved.Point, "A covered point gives way.");
            Assert.IsFalse(MapRegionNames.BoardRect(moved, half).Overlaps(block), "The name stands clear of the obstacle.");
            Assert.IsTrue(MapContourMath.Contains(shape, moved.Point));
        }

        // UI-363 (owner report): a small region still shows its name at a reading zoom; a name that fits beside the
        // stands stays at its own point.
        [Test]
        public void ASmallRegionsNameCanBeReadAtTheClosestZoom()
        {
            var data = BoardPresentation.Load();
            var stands = MapStandSlots.Load(data);
            foreach (var id in new[] { "western-mirkwood", "rhosgobel", "narrows-of-the-forest" })
            {
                var region = data.Regions[id];
                var point = MapRegionNames.Point(region, data.Shapes[id], stands);
                Assert.IsTrue(MapRegionNames.Readable(region.Name, point, 11), id + " can be read at its point");
            }
            var eastern = data.Regions["eastern-mirkwood"];
            Assert.AreEqual(MapRegionNamePoints.For(data.Shapes[eastern.Id]).Point,
                MapRegionNames.Point(eastern, data.Shapes[eastern.Id], stands).Point, "A name that fits beside the stands stays there.");
            var unreadable = data.Regions.Values.Where(r => data.Shapes.ContainsKey(r.Id) &&
                !MapRegionNames.Readable(r.Name, MapRegionNames.Point(r, data.Shapes[r.Id], stands), MapRegionNames.LetterHeightOf(r)));
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

        // UI-422: a name's largest extent is its single line's width and, with a space to break at, two lines' height;
        // where it is drawn, its room bounds it.
        [Test]
        public void ANamesBoardRectangleIsItsLargestExtentWithinItsRoom()
        {
            var single = MapRegionNames.BoardHalfSize("Moria", 11);
            var broken = MapRegionNames.BoardHalfSize("Old Ford", 11);
            Assert.AreEqual(single.y * 2, broken.y, 1e-4f, "A name with a space may take two lines.");
            Assert.AreEqual(single.x * 8 / 5, broken.x, 1e-4f, "The width follows the letters on one line.");
            var room = new MapNamePoint(new Vector2(100, 50), 10, 40);
            var rect = MapRegionNames.BoardRect(room, broken);
            Assert.AreEqual(20, rect.width, 1e-4f, "The room bounds the width.");
            Assert.AreEqual(broken.y * 2, rect.height, 1e-4f);
            Assert.AreEqual(room.Point, rect.center);
        }
    }
}
