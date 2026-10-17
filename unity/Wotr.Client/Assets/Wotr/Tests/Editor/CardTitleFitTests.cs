using System.Linq;
using NUnit.Framework;
using Wotr.Client;

namespace Wotr.Tests
{
    // UI-217: the collapsed hand tile fits a card name into two lines with the smallest needed reduction.
    public class CardTitleFitTests
    {
        // Word widths in px at 12 px, roughly Cinzel SemiBold: "The Black Captain Commands" on a 130 px title.
        private static readonly float[] BlackCaptain = { 30, 46, 64, 77 };
        private const float Space = 4;

        [Test]
        public void ANameThatFitsTwoLinesKeepsItsSize()
        {
            Assert.AreEqual(12f, CardTitleFit.Size(new float[] { 30, 46 }, Space, 12, 12, 130));
            Assert.AreEqual(12f, CardTitleFit.Size(new float[] { 60, 50, 70 }, Space, 12, 12, 130));
        }

        [Test]
        public void ALongNameShrinksJustEnoughForTwoLines()
        {
            Assert.AreEqual(3, CardTitleFit.LineCount(BlackCaptain, Space, 1, 130 * .97f));
            var size = CardTitleFit.Size(BlackCaptain, Space, 12, 12, 130);
            Assert.Less(size, 12f);
            Assert.GreaterOrEqual(size, CardTitleFit.MinimumSize);
            Assert.LessOrEqual(CardTitleFit.LineCount(BlackCaptain, Space, size / 12, 130 * .97f), CardTitleFit.Lines);
            Assert.Greater(CardTitleFit.LineCount(BlackCaptain, Space, (size + .25f) / 12, 130 * .97f), CardTitleFit.Lines);
        }

        [Test]
        public void TheResultDoesNotDependOnTheSizeTheWidthsWereMeasuredAt()
        {
            var atTwelve = CardTitleFit.Size(BlackCaptain, Space, 12, 12, 130);
            var atTen = CardTitleFit.Size(BlackCaptain.Select(width => width * 10 / 12).ToArray(), Space * 10 / 12, 10, 12, 130);
            Assert.AreEqual(atTwelve, atTen);
        }

        [Test]
        public void TheLongestWordFitsOneLineAndTheSizeStopsAtTheMinimum()
        {
            var size = CardTitleFit.Size(new float[] { 150 }, Space, 12, 12, 130);
            Assert.LessOrEqual(150 * size / 12, 130 * .97f);
            Assert.AreEqual(CardTitleFit.MinimumSize, CardTitleFit.Size(Enumerable.Repeat(100f, 8).ToArray(), Space, 12, 12, 130));
            Assert.AreEqual(int.MaxValue, CardTitleFit.LineCount(new float[] { 150 }, Space, 1, 130));
        }

        [Test]
        public void NothingToMeasureKeepsTheNormalSize()
        {
            Assert.AreEqual(11f, CardTitleFit.Size(new float[0], Space, 11, 11, 115));
            Assert.AreEqual(11f, CardTitleFit.Size(BlackCaptain, Space, 11, 11, 0));
        }
    }
}
