using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class ArmyMarkerTests
    {
        [Test]
        public void ForcesCountEveryNationLargestFirstAndKeepNazgulApartFromLeaders()
        {
            var army = ArmyMarker.From("osgiliath", Side.Shadow, false, new[]
            {
                ("sauron", UnitKind.Regular), ("sauron", UnitKind.Nazgul), ("sauron", UnitKind.Nazgul),
                ("southrons", UnitKind.Regular), ("southrons", UnitKind.Regular), ("southrons", UnitKind.Elite),
                ("southrons", UnitKind.Leader), ("isengard", UnitKind.Elite)
            });
            CollectionAssert.AreEqual(new[] { "southrons", "sauron", "isengard" }, army.Nations.Select(n => n.Nation));
            Assert.AreEqual(3, army.Regular);
            Assert.AreEqual(2, army.Elite);
            Assert.AreEqual(2, army.Nazgul);
            Assert.AreEqual(3, army.Leaders, "Leaders still include the Nazgûl for the existing figure rules.");
            Assert.AreEqual(1, army.Nations[0].Leaders);
            Assert.AreEqual(0, army.Nations[1].Leaders);
            Assert.AreEqual(2, army.Nations[1].Nazgul);
            Assert.AreEqual(8, army.Total);
        }

        // UI-402: an army's tooltip names its units; its characters stand beside it with tooltips of their own.
        [Test]
        public void TooltipNamesEveryNationAndTheNazgulAndACharacterHasItsOwn()
        {
            var army = ArmyMarker.From("helms-deep", Side.FreePeoples, true, new[]
            {
                ("rohan", UnitKind.Regular), ("rohan", UnitKind.Regular), ("rohan", UnitKind.Elite),
                ("rohan", UnitKind.Leader), ("gondor", UnitKind.Regular)
            });
            Assert.AreEqual("Helm's Deep · Garrison\nRohan · 2 Regular · 1 Elite · 1 Leader\nGondor · 1 Regular",
                ArmyMarkerText.Describe(army, "Helm's Deep"));
            var shadow = ArmyMarker.From("minas-morgul", Side.Shadow, false, new[] { ("sauron", UnitKind.Nazgul), ("sauron", UnitKind.Nazgul) });
            Assert.AreEqual("Minas Morgul · Field army\nSauron · 2 Nazgûl", ArmyMarkerText.Describe(shadow, "Minas Morgul"));
            var gandalf = CharacterFigure.From(new CharacterView
            {
                Id = "gandalf", DefinitionId = "base.character.gandalf-white", Location = "helms-deep", Garrison = true, Level = 3, Leadership = 1
            });
            Assert.AreEqual("Gandalf the White · Level 3 · Leadership 1\nHelm's Deep · Garrison", ArmyMarkerText.Describe(gandalf, "Helm's Deep"));
        }

        // UI-402: a force's characters leave its army marker: the marker keeps its units, counted alone, and every
        // character stands as its own figure, Gandalf and Strider as two.
        [Test]
        public void CharactersStandApartFromTheArmyAndItsCount()
        {
            var data = BoardPresentation.Load();
            data.Apply(new PlayerView
            {
                MatchId = "match", You = Side.FreePeoples,
                Units = { new UnitView { Id = "dwarves-1", Nation = "dwarves", Side = Side.FreePeoples, Kind = UnitKind.Regular, Location = "erebor" } },
                Characters =
                {
                    new CharacterView { Id = "gandalf", DefinitionId = "base.character.gandalf-grey", Side = Side.FreePeoples, Location = "erebor" },
                    new CharacterView { Id = "strider", DefinitionId = "base.character.strider", Side = Side.FreePeoples, Location = "erebor" },
                    new CharacterView { Id = "gimli", DefinitionId = "base.character.gimli", Side = Side.FreePeoples, Location = "dale" }
                }
            });
            var erebor = data.Armies.Single(a => a.RegionId == "erebor");
            Assert.AreEqual(1, erebor.Total, "The army counts its units only.");
            CollectionAssert.AreEqual(new[] { "dwarves-1" }, erebor.Units.Select(u => u.Id));
            Assert.IsEmpty(erebor.Carried);
            Assert.IsFalse(data.Armies.Any(a => a.RegionId == "dale"), "A character alone is no army.");
            CollectionAssert.AreEquivalent(new[] { "gandalf-grey", "strider", "gimli" }, data.Characters.Select(c => c.Art.Key));
            Assert.AreEqual(3, data.FigureCount("erebor", Side.FreePeoples, false));
            Assert.AreEqual(1, data.FigureCount("dale", Side.FreePeoples, false));
        }

        // UI-420: one composition form for a nation's figures, with the Southrons & Easterlings named in full.
        [Test]
        public void CompositionNamesTheNationAndCountsEachKindOnce()
        {
            Assert.AreEqual("Gondor · 3 Regular · 1 Elite · 1 Leader", ArmyMarkerText.Composition("gondor", 3, 1, 1, 0));
            Assert.AreEqual("Southrons & Easterlings · 2 Leaders", ArmyMarkerText.Composition("southrons", 0, 0, 2, 0));
            Assert.AreEqual("Sauron · 1 Regular · 2 Nazgûl",
                ArmyMarkerText.Composition("sauron", new[] { UnitKind.Nazgul, UnitKind.Regular, UnitKind.Nazgul }));
        }
    }
}
