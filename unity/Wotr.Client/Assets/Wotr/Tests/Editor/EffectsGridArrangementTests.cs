using System.Linq;
using NUnit.Framework;
using Wotr.Client;

namespace Wotr.Tests
{
    // UI-424: the Effects grid is chosen from the source count and the docked panel's fixed height, before layout.
    public class EffectsGridArrangementTests
    {
        // The height between the rail's top and the bottom row: 1366×768 (and 1920×1080, the same in points) at 100 and 125 %.
        private const float Regular = 584 - 8 - 58;
        private const float Scaled = 430.4f - 8 - 58.4f;

        [Test]
        public void TheOwnersScenarioFitsAboveTheBottomRowInBothScales()
        {
            Assert.AreEqual("effects-columns-2", EffectsGridArrangement.Choose(18, Regular).ClassName);
            Assert.AreEqual("effects-columns-3", EffectsGridArrangement.Choose(18, Scaled).ClassName);
            Assert.AreEqual("effects-columns-3", EffectsGridArrangement.Choose(22, Regular).ClassName);
            Assert.AreEqual("effects-columns-4", EffectsGridArrangement.Choose(22, Scaled).ClassName);
            Assert.AreEqual("effects-columns-2", EffectsGridArrangement.Choose(6, Scaled).ClassName);
            foreach (var sources in Enumerable.Range(0, 29))
                foreach (var height in new[] { Regular, Scaled })
                    Assert.LessOrEqual(EffectsGridArrangement.Choose(sources, height).PanelHeight(sources), height, sources + " at " + height);
        }

        [Test]
        public void TheNarrowestArrangementThatFitsWins()
        {
            foreach (var sources in Enumerable.Range(0, 29))
            {
                var chosen = EffectsGridArrangement.Choose(sources, Scaled);
                foreach (var narrower in EffectsGridArrangement.All.TakeWhile(arrangement => arrangement != chosen))
                    Assert.Greater(narrower.PanelHeight(sources), Scaled, sources + " sources: " + narrower + " before " + chosen);
            }
        }

        [Test]
        public void EveryArrangementKeepsThirtyTwoPointTargetsAndOneWidthPerColumnCount()
        {
            foreach (var arrangement in EffectsGridArrangement.All)
            {
                Assert.GreaterOrEqual(arrangement.TitleHeight, 32, arrangement.ToString());
                Assert.GreaterOrEqual(arrangement.TitleTextHeight, 28, "two lines of about ten points · " + arrangement);
                Assert.Greater(arrangement.TitleTextWidth(true, true), 60, arrangement.ToString());
                Assert.Greater(arrangement.PanelWidth, arrangement.Columns * arrangement.EntryWidth, arrangement.ToString());
                var sameColumns = EffectsGridArrangement.All.Where(other => other.Columns == arrangement.Columns);
                Assert.IsTrue(sameColumns.All(other => other.PanelWidth == arrangement.PanelWidth), arrangement.ToString());
            }
            Assert.AreEqual(1, EffectsGridArrangement.Choose(0, Regular).Rows(0));
        }

        [Test]
        public void AnUnknownHeightKeepsTheRegularGridAndTooManySourcesTakeTheDensest()
        {
            Assert.AreSame(EffectsGridArrangement.Regular, EffectsGridArrangement.Choose(22, float.NaN));
            Assert.AreSame(EffectsGridArrangement.Regular, EffectsGridArrangement.Choose(22, 0));
            Assert.AreSame(EffectsGridArrangement.All.Last(), EffectsGridArrangement.Choose(80, Scaled));
        }

        [Test]
        public void TheDetailsCardStandsLevelWithThePanelAndRisesOnlyAsFarAsItMust()
        {
            Assert.AreEqual(0, EffectDetailsPlace.Offset(58, 300, 584));
            Assert.AreEqual(-110, EffectDetailsPlace.Offset(232, 300, 430));
            Assert.AreEqual(8 - 200f, EffectDetailsPlace.Offset(200, 500, 430), "never above the top of the board");
        }
    }
}
