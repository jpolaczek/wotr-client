using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-523 (owner, 2026-11-08: the row's figures "must be decidedly bigger"): figures stand at full size while the row
    // holds them apart, shrink alike down to 85 %, then overlap their neighbours at most a third of the narrower one with
    // their counts still apart, and only then shrink further — never past the row's width.
    public sealed class MapArmyRowLayoutTests
    {
        private static readonly Vector2 Free = new Vector2(38, 54);
        private static readonly Vector2 Rider = new Vector2(54, 54);

        [Test]
        public void WithRoomEveryFigureStandsFullSizeApart()
        {
            var (boxes, width) = MapArmyRow.Lay(new[] { Free, Free, Free }, 127);
            Assert.That(boxes.Select(box => box.height), Is.All.EqualTo(54).Within(.01f));
            for (var i = 1; i < boxes.Length; i++)
                Assert.AreEqual(MapArmyRow.Gap, boxes[i].xMin - boxes[i - 1].xMax, .01f, "A gap between neighbours");
            Assert.AreEqual(3 * 38 + 2 * MapArmyRow.Gap, width, .01f);
        }

        [Test]
        public void ARowALittleShortShrinksTheFiguresAlikeWithoutOverlap()
        {
            var (boxes, width) = MapArmyRow.Lay(new[] { Rider, Rider, Rider }, 150);
            Assert.That(boxes[0].height, Is.GreaterThanOrEqualTo(MapArmyRow.MinimumScale * 54).And.LessThan(54));
            for (var i = 1; i < boxes.Length; i++)
                Assert.GreaterOrEqual(boxes[i].xMin - boxes[i - 1].xMax, MapArmyRow.Gap - .01f);
            Assert.LessOrEqual(width, 150 + .01f);
        }

        // The owner's Barad-dûr army on the widest row stand: three wide riders keep 85 % and overlap a little.
        [Test]
        public void ThreeRidersKeepEightyFivePercentAndOverlapAtMostAThird()
        {
            var (boxes, width) = MapArmyRow.Lay(new[] { Rider, Rider, Rider }, 127);
            Assert.That(boxes.Select(box => box.height), Is.All.EqualTo(MapArmyRow.MinimumScale * 54).Within(.01f));
            AssertNeighbours(boxes);
            Assert.LessOrEqual(width, 127 + .01f);
        }

        [Test]
        public void ATightRowShrinksFurtherButKeepsItsCountsApart()
        {
            var (boxes, width) = MapArmyRow.Lay(new[] { Rider, Free, Rider }, 46);
            Assert.Less(boxes[0].height, MapArmyRow.MinimumScale * 54);
            AssertNeighbours(boxes);
            Assert.LessOrEqual(width, 46 + .01f);
        }

        private static void AssertNeighbours(Rect[] boxes)
        {
            for (var i = 1; i < boxes.Length; i++)
            {
                var overlap = boxes[i - 1].xMax - boxes[i].xMin;
                Assert.LessOrEqual(overlap, MapArmyRow.OverlapShare * Mathf.Min(boxes[i].width, boxes[i - 1].width) + .01f, "At most a third");
                Assert.GreaterOrEqual(boxes[i].center.x - boxes[i - 1].center.x, MapArmyRow.CountWidth, "Their counts apart");
            }
        }
    }
}
