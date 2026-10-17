using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    public sealed class RegionInspectionTests
    {
        [TestCase("empty", false)]
        [TestCase("effect", false)]
        [TestCase("regular", true)]
        [TestCase("nazgul", true)]
        [TestCase("garrison", true)]
        [TestCase("character", true)]
        [TestCase("fellowship", true)]
        [TestCase("mordor", false)]
        public void OnlyVisibleForcesOrTheRegionalFellowshipQualifyForHover(string contents, bool expected)
        {
            var view = new PlayerView();
            const string region = "pelargir";
            view.Control[region] = Side.FreePeoples;
            if (contents == "effect")
                view.Effects.Add(new ActiveEffectView
                {
                    Id = "effect", Targets = { new EffectTargetView { Kind = "Region", Id = region } }
                });
            if (contents is "regular" or "nazgul" or "garrison")
                view.Units.Add(new UnitView
                {
                    Id = "unit", Location = region, Side = Side.Shadow,
                    Kind = contents == "nazgul" ? UnitKind.Nazgul : UnitKind.Regular,
                    Garrison = contents == "garrison"
                });
            if (contents == "character")
                view.Characters.Add(new CharacterView { Id = "character", Location = region, Side = Side.Shadow });
            if (contents is "fellowship" or "mordor")
                view.Fellowship = new FellowshipView { RegionId = region, Hidden = true, MordorStep = contents == "mordor" ? 0 : (int?)null };

            var index = new RegionViewIndex(view);
            Assert.AreEqual(expected, index.Occupied(region));
            if (contents == "effect")
                Assert.AreEqual(1, index.Get(region).Effects.Count, "The effect is present without making the region hoverable.");
            Assert.IsFalse(index.Occupied("unknown"));
            Assert.AreEqual(contents == "fellowship", index.Get(region)?.Fellowship != null);
        }

        [Test]
        public void NewProjectionRemovesDepartedForcesWithoutGuessingTheFellowshipsRoute()
        {
            var view = new PlayerView { Fellowship = new FellowshipView { RegionId = "rivendell", Progress = 3, Hidden = true } };
            view.Characters.Add(new CharacterView { Id = "character", Location = "hollin" });
            var before = new RegionViewIndex(view);
            view.Characters[0].Location = "moria";
            var after = new RegionViewIndex(view);
            Assert.IsTrue(before.Occupied("hollin"));
            Assert.IsFalse(after.Occupied("hollin"));
            Assert.IsTrue(after.Occupied("moria"));
            Assert.IsTrue(after.Occupied("rivendell"));
            Assert.IsFalse(after.Occupied("trollshaws"));
        }
    }
}
