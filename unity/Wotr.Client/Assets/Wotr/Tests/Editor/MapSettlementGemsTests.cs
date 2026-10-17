using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-195: settlements are cut gemstones; the cut tells the kind and the colour tells who controls it.
    public sealed class MapSettlementGemsTests
    {
        [Test]
        public void EachKindHasItsOwnCut()
        {
            var town = SettlementGem.For("Town", Side.FreePeoples);
            var city = SettlementGem.For("City", Side.FreePeoples);
            var stronghold = SettlementGem.For("Stronghold", Side.FreePeoples);
            var ruins = SettlementGem.For("Fortification", Side.FreePeoples);

            Assert.AreEqual(8, town.Girdle.Length, "A Town is a round brilliant.");
            Assert.That(Width(town), Is.EqualTo(Height(town)).Within(.01f));
            Assert.Greater(Width(city), Height(city), "A City is an emerald cut, wider than tall.");
            Assert.IsTrue(stronghold.Setting, "A Stronghold sits in a gold setting.");
            Assert.IsFalse(town.Setting || city.Setting || ruins.Setting);
            Assert.AreEqual(6, ruins.Girdle.Length, "A Fortification is a trillion.");
            Assert.Less(ruins.Girdle.Min(p => p.y), -.9f, "The trillion points up.");
            Assert.Less(town.Extent, stronghold.Extent);
            Assert.Less(city.Extent, stronghold.Extent);
            Assert.Less(stronghold.Extent, 12, "Armies stand 12 points above a settlement's anchor.");
        }

        [Test]
        public void TheColourFollowsTheControllingSide()
        {
            var free = SettlementGem.For("City", Side.FreePeoples).Colours;
            var shadow = SettlementGem.For("City", Side.Shadow).Colours;
            var none = SettlementGem.For("City", null).Colours;

            Assert.Greater(free.Mid.b, free.Mid.r, "Sapphire for the Free Peoples.");
            Assert.Greater(shadow.Mid.r, shadow.Mid.b, "Ruby for the Shadow.");
            Assert.Less(Mathf.Abs(none.Mid.r - none.Mid.b), .15f, "Pale quartz without a controller.");
        }

        [Test]
        public void FacetsFacingTheLightAreBrighter()
        {
            var gem = SettlementGem.For("Stronghold", Side.Shadow);
            var colours = Enumerable.Range(0, gem.Girdle.Length).Select(edge => gem.Facet(edge, gem.Colours)).ToArray();
            var upperLeft = colours[0];
            var lowerRight = colours[4];
            Assert.Greater(upperLeft.grayscale, lowerRight.grayscale);
            Assert.Greater(gem.TableColour.grayscale, gem.Colours.Mid.grayscale, "The table catches the light.");
        }

        [Test]
        public void OutlinesAreScaledAroundTheAnchor()
        {
            var gem = SettlementGem.For("Town", Side.FreePeoples);
            var at = new Vector2(100, 50);
            var outline = gem.Outline(at);
            var table = gem.Table(at);
            Assert.That(outline.Average(p => p.x), Is.EqualTo(at.x).Within(.01f));
            Assert.That(outline.Average(p => p.y), Is.EqualTo(at.y).Within(.01f));
            Assert.Less(table.Max(p => p.x), outline.Max(p => p.x), "The table lies inside the girdle.");
        }

        private static float Width(SettlementGem gem) => gem.Girdle.Max(p => p.x) - gem.Girdle.Min(p => p.x);

        private static float Height(SettlementGem gem) => gem.Girdle.Max(p => p.y) - gem.Girdle.Min(p => p.y);
    }
}
