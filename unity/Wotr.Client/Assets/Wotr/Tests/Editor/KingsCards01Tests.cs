using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class KingsCards01Tests
    {
        [Test]
        public void AllSixCardsHaveBothEnglishHalvesAndSourceInitiatives()
        {
            var ids = new[] { "character.23", "character.25", "character.26", "strategy.08", "strategy.16", "strategy.19" };
            var initiatives = new[] { 3, 4, 2, 4, 3, 6 };
            for (var i = 0; i < ids.Length; i++)
            {
                var card = CardLibrary.Cards["kings.fp." + ids[i]];
                Assert.IsNotEmpty(card.Title);
                Assert.IsNotEmpty(card.Event);
                Assert.IsNotEmpty(card.Combat);
                Assert.AreEqual(initiatives[i], card.Initiative.Single());
                Assert.IsTrue(card.HasCombat);
            }
        }
    }
}
