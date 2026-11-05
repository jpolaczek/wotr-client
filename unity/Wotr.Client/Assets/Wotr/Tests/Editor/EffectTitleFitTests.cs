using System.Linq;
using NUnit.Framework;
using Wotr.Client;

namespace Wotr.Tests
{
    // UI-424: an Effects tile's title fits inside its tile, from its words measured once at any size.
    public class EffectTitleFitTests
    {
        // Widths at 12 points, roughly the board's serif: one line is 17 points high and two are 33.
        private const float Space = 3;
        private static readonly float[] Mithril = { 46, 30, 22, 30 };
        private static readonly float[] Lore = { 20, 26, 12, 18, 31, 32 };

        private static EffectTitleFit Fit(float[] words, float measuredAt = 12, float scale = 1)
        {
            var runs = EffectTitleFit.Measure(Enumerable.Range(0, words.Length).Select(i => "w" + i).ToArray(), run =>
                run.Split(' ').Sum(word => words[int.Parse(word.Substring(1))] * scale) + (run.Split(' ').Length - 1) * Space * scale);
            return new EffectTitleFit(runs, 17 * scale, 33 * scale, measuredAt);
        }

        [Test]
        public void ANameOnTwoLinesThatTheTileHoldsKeepsItsSize()
        {
            Assert.AreEqual(2, Fit(Mithril).LineCount(1, 109 * .97f));
            // Two lines of 33 points with the 2-point allowance for pixel rounding.
            Assert.AreEqual(12f, Fit(Mithril).Size(12, 109, 35));
            Assert.Less(Fit(Mithril).Size(12, 109, 34), 12f);
            Assert.AreEqual(12f, Fit(new float[] { 60 }).Size(12, 109, 34));
        }

        [Test]
        public void ALongerNameShrinksJustEnoughToFitTheTile()
        {
            var fit = Fit(Lore);
            Assert.AreEqual(3, fit.LineCount(1, 79 * .97f));
            var size = fit.Size(11.5f, 79, 32);
            Assert.Less(size, 11.5f);
            Assert.IsTrue(fit.Fits(size, 79, 32));
            Assert.IsFalse(fit.Fits(size + .25f, 79, 32));
        }

        [Test]
        public void TheResultDoesNotDependOnTheSizeTheWordsWereMeasuredAt()
        {
            Assert.AreEqual(Fit(Lore).Size(12, 79, 32), Fit(Lore, 10, 10 / 12f).Size(12, 79, 32));
        }

        [Test]
        public void AWordWiderThanTheTileNeverFitsAndTheSmallestTypeIsTheFloor()
        {
            var fit = Fit(new float[] { 400 });
            Assert.AreEqual(int.MaxValue, fit.LineCount(1, 100));
            Assert.AreEqual(EffectTitleFit.MinimumSize, fit.Size(12, 100, 34));
            Assert.AreEqual(12f, Fit(new float[0]).Size(12, 100, 34));
        }

        [Test]
        public void WordsSplitAtSpacesAsTheLabelWrapsThem()
        {
            CollectionAssert.AreEqual(new[] { "The", "Palantír", "of", "Barad-dûr" }, EffectTitleFit.Split(" The  Palantír of Barad-dûr"));
            Assert.IsEmpty(EffectTitleFit.Split(null));
        }
    }
}
