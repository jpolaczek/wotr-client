using System.Linq;
using NUnit.Framework;
using Wotr.Client;

namespace Wotr.EditorTests
{
    public class FateCardLibraryTests
    {
        [Test]
        public void SixPublicFateCardsHaveTheirOwnFullEventAndCombatTextAndPrintedInitiatives()
        {
            var cards = CardLibrary.Cards.Values.Where(c => c.Id.StartsWith("fate.")).ToArray();
            Assert.AreEqual(6, cards.Length);
            Assert.IsTrue(cards.All(c => c.Event.Length > 40 && c.HasCombat));
            Assert.AreEqual("Bardings", CardLibrary.Title("fate.fp.strategy.19"));
            Assert.AreEqual("Dwarves of the Iron Hills", CardLibrary.Title("fate.fp.strategy.22"));
            Assert.That(CardLibrary.Cards["fate.fp.strategy.04"].Event, Does.Contain("Iron Hills"));
            Assert.That(CardLibrary.Cards["fate.sh.strategy.20"].Event, Does.Contain("Erebor"));
            Assert.AreEqual(6, CardLibrary.Cards["fate.fp.strategy.19"].Initiative.Single());
            Assert.AreEqual(7, CardLibrary.Cards["fate.sh.strategy.20"].Initiative.Single());
            Assert.IsTrue(cards.Where(c => c.Id != "fate.fp.strategy.19" && c.Id != "fate.sh.strategy.20")
                .All(c => c.Initiative.Single() == 3));
        }
    }
}
