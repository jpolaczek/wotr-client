using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-195: settlements are cut gemstones; the cut tells the kind and the colour tells who controls it. UI-406: they lie
    // on the table with a side band and a soft shadow.
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

        // UI-406: the stone lies on the table — a band of side facets below its girdle (below the setting of a Stronghold)
        // gives it thickness, darker than its crown and keeping its colour, and its whole body fits the gem's element with
        // its shadow at every kind.
        [Test]
        public void TheStoneHasASideBandBelowItsGirdleInItsOwnColour()
        {
            foreach (var kind in new[] { "Town", "City", "Stronghold", "Fortification" })
            {
                var gem = SettlementGem.For(kind, Side.FreePeoples);
                var at = new Vector2(16, 16);
                var rim = gem.Rim(at);
                var silhouette = gem.Silhouette(at);
                Assert.Greater(gem.Thickness, 1f, kind);
                Assert.LessOrEqual(gem.Thickness, 1.6f + 1e-4f,
                    "The band stays within the stand places' 2-unit margin, 1.6 points at the worst-case zoom · " + kind);
                Assert.AreEqual(rim.Max(p => p.y) + gem.Thickness, silhouette.Max(p => p.y), 1e-4f, "The band hangs below · " + kind);
                Assert.AreEqual(rim.Min(p => p.y), silhouette.Min(p => p.y), 1e-4f, "The crown is the top · " + kind);
                Assert.AreEqual(rim.Min(p => p.x), silhouette.Min(p => p.x), 1e-4f, kind);
                var down = Enumerable.Range(0, rim.Length).Where(edge => SettlementGem.FacesDown(rim, edge, at)).ToList();
                Assert.That(down.Count, Is.InRange(2, rim.Length / 2 + 1), "Only the lower edges show their side · " + kind);
                var palette = gem.Setting ? SettlementGem.Gold : gem.Colours;
                foreach (var edge in down)
                {
                    var side = SettlementGem.SideFacet(palette, rim, edge, at);
                    Assert.Less(side.grayscale, palette.Mid.grayscale, "The side is darker than the crown · " + kind);
                    Assert.AreEqual(1, side.a, 1e-5f);
                }
                // The shadow's outer fringe, as MapSettlementVisual spreads it, stays inside the gem's element.
                var middle = at + new Vector2(0, gem.Thickness / 2);
                var element = new Rect(Vector2.zero, MapSettlementGems.Box);
                foreach (var point in silhouette)
                    Assert.IsTrue(element.Contains(middle + SettlementGem.ShadowOffset + (point - middle) * SettlementGem.ShadowSpread),
                        "The stone and its shadow fit its element · " + kind);
            }
            var free = SettlementGem.For("City", Side.FreePeoples);
            var band = SettlementGem.SideFacet(free.Colours, free.Outline(Vector2.zero), 4, Vector2.zero);
            Assert.Greater(band.b, band.r, "A sapphire's side is blue too.");
            var shadow = SettlementGem.For("City", Side.Shadow);
            band = SettlementGem.SideFacet(shadow.Colours, shadow.Outline(Vector2.zero), 4, Vector2.zero);
            Assert.Greater(band.r, band.b, "A ruby's side is red too.");
        }

        private static float Width(SettlementGem gem) => gem.Girdle.Max(p => p.x) - gem.Girdle.Min(p => p.x);

        private static float Height(SettlementGem gem) => gem.Girdle.Max(p => p.y) - gem.Girdle.Min(p => p.y);
    }
}
