using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-265: a region's panel says who its settlement scores for: the side that captures it from its original owner.
    public sealed class RegionVictoryTextTests
    {
        [Test]
        public void ASettlementScoresForTheSideThatCapturesIt()
        {
            var erebor = new RegionDefinition { Id = "erebor", Settlement = "Stronghold", OriginalOwner = Side.Shadow, VictoryPoints = 2 };
            Assert.AreEqual("Worth 2 VP to the Free Peoples if captured", RegionVictoryText.For(erebor, Side.Shadow));
            Assert.AreEqual("Scores 2 VP for the Free Peoples", RegionVictoryText.For(erebor, Side.FreePeoples));
            var ironHills = new RegionDefinition { Id = "iron-hills", Settlement = "City", OriginalOwner = Side.FreePeoples, VictoryPoints = 1 };
            Assert.AreEqual("Worth 1 VP to the Shadow if captured", RegionVictoryText.For(ironHills, null));
            var dale = new RegionDefinition { Id = "dale", Settlement = "Fortification", OriginalOwner = null, VictoryPoints = 0 };
            Assert.AreEqual("", RegionVictoryText.For(dale, Side.FreePeoples));
            Assert.AreEqual("", RegionVictoryText.For(new RegionDefinition { Id = "bree", Settlement = "Town", OriginalOwner = Side.FreePeoples }, null));
        }
    }
}
