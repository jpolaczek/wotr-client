using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    public sealed class VictoryShortcutPlacementTests
    {
        private static readonly Rect Preferred = new Rect(954.8f, 279, 130, 30);
        private static readonly Vector2 Minimum = new Vector2(8, 8);

        [Test]
        public void AFreeColumnKeepsTheShortcutOnItsOriginalBottomRow()
        {
            var card = new Rect(800, 60, 285, 249);
            var result = VictoryShortcutPlacement.Place(Preferred, Minimum, new[] { card });
            Assert.AreEqual(Preferred.y, result.y);
            Assert.AreEqual(card.xMin - Preferred.width - 8, result.x);
            Assert.IsFalse(result.Overlaps(card));
        }

        [Test]
        public void FullEffectsAndFivePortraitsLeaveAReachableShortcutOnAnotherRow()
        {
            var obstacles = new[]
            {
                new Rect(98, 160, 500, 144),
                new Rect(606, 44, 478.8f, 265),
                new Rect(110, 112, 480, 40),
                new Rect(648, 8, 330, 34),
                new Rect(986, 8, 98, 34)
            };
            var result = VictoryShortcutPlacement.Place(Preferred, Minimum, obstacles);
            Assert.Less(result.y, Preferred.y);
            Assert.AreEqual(Preferred.size, result.size);
            foreach (var obstacle in obstacles)
                Assert.IsFalse(result.Overlaps(obstacle), $"{result} must avoid {obstacle}");
            Inside(result);
            Assert.AreEqual(result, VictoryShortcutPlacement.Place(Preferred, Minimum, obstacles));
        }

        [Test]
        public void ClosingWindowsRestoresTheBottomRightWithoutUsingTheLastFallbackRow()
        {
            var blockedBottom = new Rect(0, 240, 1092.8f, 77);
            var shifted = VictoryShortcutPlacement.Place(Preferred, Minimum, new[] { blockedBottom });
            Assert.Less(shifted.y, Preferred.y);
            var restored = VictoryShortcutPlacement.Place(Preferred, Minimum, System.Array.Empty<Rect>());
            Assert.AreEqual(Preferred, restored);
        }

        [Test]
        public void AnEntirelyCoveredViewportDoesNotPushTheShortcutOutsideTheScreen()
        {
            var obstruction = new Rect(0, 0, 1092.8f, 317);
            var result = VictoryShortcutPlacement.Place(Preferred, Minimum, new[] { obstruction });
            Assert.AreEqual(Preferred, result);
            Inside(result);
        }

        private static void Inside(Rect rectangle)
        {
            Assert.GreaterOrEqual(rectangle.xMin, Minimum.x);
            Assert.GreaterOrEqual(rectangle.yMin, Minimum.y);
            Assert.LessOrEqual(rectangle.xMax, 1092.8f);
            Assert.LessOrEqual(rectangle.yMax, 317);
        }
    }
}
