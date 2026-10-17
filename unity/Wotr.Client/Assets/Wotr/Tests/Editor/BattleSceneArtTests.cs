using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // UI-206: the battle strip's painting follows the place's settlement and side, in two versions per group.
    public class BattleSceneArtTests
    {
        private static readonly string[] Illustrations =
        {
            "open-free-1", "open-free-2", "open-shadow-1", "open-shadow-2", "open-wild-1", "open-wild-2",
            "fortification-free-1", "fortification-free-2", "town-free-1", "town-free-2", "town-shadow-1", "town-shadow-2",
            "city-free-1", "city-free-2", "city-shadow-1", "city-shadow-2", "stronghold-free-1", "stronghold-free-2",
            "stronghold-shadow-1", "stronghold-shadow-2"
        };

        [TestCase(null, null, "open-wild")]
        [TestCase(null, Side.FreePeoples, "open-free")]
        [TestCase(null, Side.Shadow, "open-shadow")]
        [TestCase("Town", Side.FreePeoples, "town-free")]
        [TestCase("Town", Side.Shadow, "town-shadow")]
        [TestCase("City", Side.Shadow, "city-shadow")]
        [TestCase("Stronghold", Side.FreePeoples, "stronghold-free")]
        [TestCase("Fortification", null, "fortification-free")]
        public void TheGroupFollowsTheSettlementAndTheSideOfThePlace(string settlement, Side? owner, string group)
        {
            Assert.AreEqual(group, BattleSceneArt.Group(new RegionDefinition { Id = "x", Settlement = settlement, OriginalOwner = owner }));
        }

        [TestCase("fords-of-isen", "fortification-free-1")]
        [TestCase("osgiliath", "fortification-free-2")]
        [TestCase("edoras", "city-free-2")]
        [TestCase("helms-deep", "stronghold-free-2")]
        [TestCase("dol-guldur", "stronghold-shadow-2")]
        [TestCase("dagorlad", "open-wild-1")]
        public void NamedPlacesShowTheirIllustration(string region, string illustration)
        {
            var regions = BoardPresentation.Load().Regions;
            Assert.AreEqual(illustration, BattleSceneArt.Key(regions[region], regions.Values));
        }

        [Test]
        public void EveryRegionHasAnIllustrationAndEveryIllustrationAPlace()
        {
            var regions = BoardPresentation.Load().Regions.Values.ToList();
            var keys = regions.Select(region => BattleSceneArt.Key(region, regions)).Distinct().OrderBy(key => key).ToList();
            CollectionAssert.AreEqual(Illustrations.OrderBy(key => key), keys);
        }

        [Test]
        public void TheVersionDoesNotDependOnTheBoardsOrder()
        {
            var regions = BoardPresentation.Load().Regions.Values.ToList();
            var reversed = Enumerable.Reverse(regions).ToList();
            foreach (var region in regions)
                Assert.AreEqual(BattleSceneArt.Key(region, regions), BattleSceneArt.Key(region, reversed), region.Id);
        }

        [Test]
        public void EveryIllustrationIsAWideStripWithoutMipmapsThatDoesNotRepeat()
        {
            foreach (var key in Illustrations)
            {
                var texture = BattleSceneArtwork.Illustration(key);
                Assert.IsNotNull(texture, key);
                Assert.AreEqual(1536, texture.width, key);
                Assert.AreEqual(320, texture.height, key);
                Assert.AreEqual(1, texture.mipmapCount, key);
                Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapModeU, key);
            }
        }
    }
}
