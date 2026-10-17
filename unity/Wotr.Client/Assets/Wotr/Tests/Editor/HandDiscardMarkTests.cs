using NUnit.Framework;
using Wotr.Client;

namespace Wotr.Tests
{
    // UI-222: a card the player may discard is marked red, a card they may play green, never both.
    public class HandDiscardMarkTests
    {
        [Test]
        public void ADiscardableCardIsRedAndAPlayableCardIsGreen()
        {
            var card = new HandCard("base.fp.strategy.02", () => { });
            card.State(true, false, "Click to play");
            Assert.IsTrue(card.Slot.ClassListContains("playable-card"));
            Assert.IsFalse(card.Slot.ClassListContains(HandCard.DiscardClass));
            card.State(true, true, "Click to discard");
            Assert.IsFalse(card.Slot.ClassListContains("playable-card"));
            Assert.IsTrue(card.Slot.ClassListContains(HandCard.DiscardClass));
            card.State(false, true, "");
            Assert.IsFalse(card.Slot.ClassListContains("playable-card"));
            Assert.IsFalse(card.Slot.ClassListContains(HandCard.DiscardClass));
        }
    }
}
