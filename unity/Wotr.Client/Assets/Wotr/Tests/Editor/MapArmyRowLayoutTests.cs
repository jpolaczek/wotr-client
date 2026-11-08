using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-523, UI-526 (owner, 2026-11-08: "bigger still; some are very, very small — not all are the same"): every figure
    // of a row stands at its full size; they stand apart while the place holds them, else overlap their neighbours at most
    // 40 % of the narrower one with their counts still apart, and where even that does not fit the row is wider than its
    // place rather than smaller.
    public sealed class MapArmyRowLayoutTests
    {
        private static readonly Vector2 Free = new Vector2(38, 54);
        private static readonly Vector2 Rider = new Vector2(54, 54);

        [Test]
        public void WithRoomEveryFigureStandsApart()
        {
            var (boxes, width) = MapArmyRow.Lay(new[] { Free, Free, Free }, 127);
            AssertFullSize(boxes, Free);
            for (var i = 1; i < boxes.Length; i++)
                Assert.AreEqual(MapArmyRow.Gap, boxes[i].xMin - boxes[i - 1].xMax, .01f, "A gap between neighbours");
            Assert.AreEqual(3 * 38 + 2 * MapArmyRow.Gap, width, .01f);
        }

        // The owner's Barad-dûr army: three wide riders keep their size and overlap a little to fit the place.
        [Test]
        public void AShortPlaceOverlapsTheFiguresWithinTheirShare()
        {
            var (boxes, width) = MapArmyRow.Lay(new[] { Rider, Rider, Rider }, 127);
            AssertFullSize(boxes, Rider);
            AssertNeighbours(boxes);
            Assert.AreEqual(127, width, .01f);
        }

        [Test]
        public void ATightPlaceGetsAWiderRowNotSmallerFigures()
        {
            var (boxes, width) = MapArmyRow.Lay(new[] { Rider, Free, Rider }, 46);
            Assert.AreEqual(54, boxes[0].height, .01f);
            Assert.AreEqual(54, boxes[1].height, .01f);
            AssertNeighbours(boxes);
            Assert.Greater(width, 46, "The row reaches past its place");
        }

        // UI-526: where a row may not reach past its place far enough, its figures shrink alike just enough to fit.
        [Test]
        public void ARowThatMayNotReachFurtherShrinksJustEnough()
        {
            var (boxes, width) = MapArmyRow.Fit(new[] { Rider, Rider, Rider }, 90);
            Assert.LessOrEqual(width, 90 + .01f);
            Assert.Less(boxes[0].height, 54);
            Assert.Greater(boxes[0].height, 40, "No smaller than it takes");
            AssertNeighbours(boxes);
        }

        private static void AssertFullSize(Rect[] boxes, Vector2 size)
        {
            foreach (var box in boxes)
                Assert.AreEqual(size, box.size);
        }

        private static void AssertNeighbours(Rect[] boxes)
        {
            for (var i = 1; i < boxes.Length; i++)
            {
                var overlap = boxes[i - 1].xMax - boxes[i].xMin;
                Assert.LessOrEqual(overlap, MapArmyRow.OverlapShare * Mathf.Min(boxes[i].width, boxes[i - 1].width) + .01f, "At most its share");
                Assert.GreaterOrEqual(boxes[i].center.x - boxes[i - 1].center.x, MapArmyRow.CountWidth, "Their counts apart");
            }
        }
    }
}
