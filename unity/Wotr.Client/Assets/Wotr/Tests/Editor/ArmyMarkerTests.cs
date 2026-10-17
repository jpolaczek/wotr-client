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

        [Test]
        public void TooltipNamesEveryNationTheNazgulAndTheCharacters()
        {
            var army = ArmyMarker.From("helms-deep", Side.FreePeoples, true, new[]
            {
                ("rohan", UnitKind.Regular), ("rohan", UnitKind.Regular), ("rohan", UnitKind.Elite),
                ("rohan", UnitKind.Leader), ("gondor", UnitKind.Regular)
            });
            army.Characters = 1;
            army.CharacterNames.Add("Gandalf the White");
            Assert.AreEqual("Helm's Deep · Garrison\nRohan: 2 regular, 1 elite, 1 leader\nGondor: 1 regular\n" +
                "Characters: Gandalf the White", ArmyMarkerText.Describe(army, "Helm's Deep"));
            var shadow = ArmyMarker.From("minas-morgul", Side.Shadow, false, new[] { ("sauron", UnitKind.Nazgul), ("sauron", UnitKind.Nazgul) });
            Assert.AreEqual("Minas Morgul · Field army\nSauron: 2 Nazgûl", ArmyMarkerText.Describe(shadow, "Minas Morgul"));
        }
    }
}
