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

        // UI-226: a nation's town, city or stronghold asks for its nation's picture first, then the side's.
        [Test]
        public void SettlementsAskForTheirNationsPictureBeforeTheirSides()
        {
            var regions = BoardPresentation.Load().Regions;
            CollectionAssert.AreEqual(new[] { "stronghold-gondor-" + BattleSceneArt.NationVersion(regions["minas-tirith"], regions.Values),
                BattleSceneArt.Key(regions["minas-tirith"], regions.Values) }, BattleSceneArt.Keys(regions["minas-tirith"], regions.Values));
            Assert.IsNull(BattleSceneArt.NationGroup(regions["osgiliath"]), "a fortification keeps its UI-206 picture");
            Assert.AreEqual(1, BattleSceneArt.Keys(regions["fangorn"], regions.Values).Count, "open land keeps its UI-206 picture");
            var versions = regions.Values.Where(r => BattleSceneArt.NationGroup(r) == "stronghold-sauron")
                .Select(r => BattleSceneArt.NationVersion(r, regions.Values)).Distinct().OrderBy(v => v).ToList();
            CollectionAssert.AreEqual(new[] { 1, 2 }, versions, "a nation with several strongholds alternates two pictures");
            var single = regions.Values.Single(r => BattleSceneArt.NationGroup(r) == "stronghold-isengard");
            Assert.AreEqual(1, BattleSceneArt.NationVersion(single, regions.Values), "a single place has one picture");
        }

        [Test]
        public void TheVersionDoesNotDependOnTheBoardsOrder()
        {
            var regions = BoardPresentation.Load().Regions.Values.ToList();
            var reversed = Enumerable.Reverse(regions).ToList();
            foreach (var region in regions)
                Assert.AreEqual(BattleSceneArt.Key(region, regions), BattleSceneArt.Key(region, reversed), region.Id);
        }

        // UI-226: every nation's town, city and stronghold of the board, and Fate's dwarven City, has its picture.
        private static readonly string[] NationIllustrations =
        {
            "town-dwarves-1", "town-dwarves-2", "city-dwarves-1", "stronghold-dwarves-1",
            "stronghold-elves-1", "stronghold-elves-2",
            "city-gondor-1", "stronghold-gondor-1", "stronghold-gondor-2", "town-gondor-1", "town-gondor-2",
            "stronghold-isengard-1", "town-isengard-1", "town-isengard-2",
            "city-north-1", "city-north-2", "town-north-1", "town-north-2",
            "city-rohan-1", "stronghold-rohan-1", "town-rohan-1", "town-rohan-2",
            "city-sauron-1", "stronghold-sauron-1", "stronghold-sauron-2", "town-sauron-1",
            "city-southrons-1", "stronghold-southrons-1", "town-southrons-1", "town-southrons-2"
        };

        [Test]
        public void EveryNationSettlementOfTheBoardFindsItsNationsPicture()
        {
            var regions = BoardPresentation.Load().Regions.Values.ToList();
            var wanted = regions.Where(r => BattleSceneArt.NationGroup(r) != null)
                .Select(r => BattleSceneArt.Keys(r, regions)[0]).Distinct().OrderBy(k => k).ToList();
            CollectionAssert.IsSubsetOf(wanted, NationIllustrations);
            foreach (var key in wanted)
                Assert.IsNotNull(BattleSceneArtwork.Illustration(key), key);
        }

        [Test]
        public void EveryIllustrationIsAWideStripWithoutMipmapsThatDoesNotRepeat()
        {
            foreach (var key in Illustrations.Concat(NationIllustrations))
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
