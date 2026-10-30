using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-401 (TABLETOP_FIGURES_UX, "Scale"): a figure is sized in board units, the zoom clamped between the worst-case
    // Detail zoom (the footprints' scale 1) and the miniature's design size; a region shows figures while the footprint
    // fits its capacity, with the Detail level's hysteresis.
    public sealed class MapStandScaleTests
    {
        private BoardPresentation data;
        private MapStandSlots stands;
        private float minimum;

        [SetUp]
        public void Load()
        {
            data = BoardPresentation.Load();
            stands = MapStandSlots.Load(data);
            minimum = MapStandSlots.WorstCaseZoom(data.Geometry);
        }

        private MapStandScale At(float zoom) => new MapStandScale(zoom, minimum, MapArmyArtwork.DesignUnit);

        [Test]
        public void BetweenTheClampsFiguresGrowWithTheMapAndKeepTheirFootprint()
        {
            foreach (var zoom in new[] { minimum, 1f, 1.2f, MapArmyArtwork.DesignUnit })
            {
                var scale = At(zoom);
                Assert.AreEqual(1, scale.Footprint, 1e-5f, "zoom " + zoom);
                Assert.AreEqual(zoom / MapArmyArtwork.DesignUnit, scale.Of(MapArmyArtwork.DesignUnit), 1e-5f, "zoom " + zoom);
            }
        }

        // The readable minimum: an army's 40-unit base is never narrower than at the worst-case Detail zoom (32 points),
        // and its elite never shorter; the footprint grows instead (a focused region far out).
        [Test]
        public void BelowTheWorstCaseZoomFiguresKeepTheirReadableSize()
        {
            var scale = At(minimum / 2);
            Assert.AreEqual(minimum, scale.PointsPerUnit, 1e-6f);
            Assert.AreEqual(2, scale.Footprint, 1e-4f);
            Assert.AreEqual(32.14f, StandFootprint.Army.BaseWidth * scale.PointsPerUnit, .01f);
        }

        // Past the design size the figures stop growing, so the footprint shrinks and every tight region shows figures
        // before the closest zoom.
        [Test]
        public void ZoomedPastTheDesignSizeTheFootprintShrinksUntilEveryRegionHasRoom()
        {
            var closest = At(CameraState.ClosestZoom);
            Assert.AreEqual(MapArmyArtwork.DesignUnit, closest.PointsPerUnit, 1e-6f);
            Assert.Less(closest.Footprint, 1);
            foreach (var pair in stands.Regions)
                Assert.IsTrue(closest.Fits(pair.Value.Capacity, false), pair.Key + " " + pair.Value.Capacity);
            var tightest = stands.Regions.Values.Min(region => region.Capacity);
            Assert.IsFalse(At(MapArmyArtwork.DesignUnit).Fits(tightest, false), "The tightest region shows tokens at scale 1.");
        }

        [Test]
        public void ARegionKeepsItsFiguresUntilTheFootprintExceedsItsCapacityByTheHysteresis()
        {
            const float capacity = .8f;
            var entry = MapArmyArtwork.DesignUnit / capacity;
            Assert.IsTrue(At(entry).Fits(capacity, false));
            var between = At(entry / 1.05f);
            Assert.IsFalse(between.Fits(capacity, false), "Coming in, figures wait for room.");
            Assert.IsTrue(between.Fits(capacity, true), "Going out, they stay within the hysteresis.");
            Assert.IsFalse(At(entry / 1.1f).Fits(capacity, true));
            Assert.AreEqual(MapMarkerVisibility.FiguresFrom / MapMarkerVisibility.TokensBelow, MapStandScale.CapacityHysteresis);
        }

        // The token is about as wide as an army's base, between a readable 32 points and its 44-point design size.
        [Test]
        public void TheTokenScalesWithTheMapWithinItsReadableRange()
        {
            Assert.AreEqual(MapStandScale.TokenMinimum / MapStandScale.TokenDesign, At(.5f).Token, 1e-5f);
            Assert.AreEqual(40 * .9f / MapStandScale.TokenDesign, At(.9f).Token, 1e-5f);
            Assert.AreEqual(1, At(2).Token, 1e-5f);
        }

        // UI-406: a settlement gem is clamp(zoom / reference, .85, 1.4) of its UI-195 size, the reference being the
        // worst-case Detail zoom: there it has its UI-195 size, the size the stand places keep clear of, and between the
        // clamps it keeps one size on the board, so it never grows into a figure's place.
        [Test]
        public void TheGemScalesWithTheMapWithinItsClampsAndNeverOutgrowsTheStandPlacesMargin()
        {
            Assert.AreEqual(1, At(minimum).Gem, 1e-5f);
            Assert.AreEqual(1.2f, At(minimum * 1.2f).Gem, 1e-5f);
            Assert.AreEqual(MapStandScale.GemSmallest, At(minimum * .5f).Gem, 1e-5f);
            Assert.AreEqual(MapStandScale.GemLargest, At(CameraState.ClosestZoom).Gem, 1e-5f);
            for (var zoom = minimum; zoom <= CameraState.ClosestZoom; zoom += .05f)
                Assert.LessOrEqual(At(zoom).Gem / zoom, 1 / minimum + 1e-5f, "Board size at zoom " + zoom);
        }

        // The miniature's design: its stand is the army's base and an elite stands as tall as the base is wide, so a
        // figure drawn at the footprint's scale 1 keeps within its footprint's width.
        [Test]
        public void TheMiniatureIsDesignedOnTheArmyFootprint()
        {
            var visible = MapArmyArtwork.Visible(false, true);
            var feet = MapArmyArtwork.Feet(false, true);
            Assert.AreEqual(StandFootprint.Army.BaseWidth * MapArmyArtwork.DesignUnit, visible.width, .01f);
            Assert.AreEqual(feet.x, visible.center.x, .01f);
            Assert.LessOrEqual(feet.y - visible.yMin, (StandFootprint.Army.FigureHeight + 2) * MapArmyArtwork.DesignUnit);
            var holder = new Rect(Vector2.zero, MapArmyArtwork.HolderSize);
            Assert.IsTrue(holder.Contains(visible.min) && holder.Contains(visible.max));
            foreach (var garrison in new[] { false, true })
                Assert.AreEqual(MapArmyArtwork.Size(garrison, false) / 2, MapArmyArtwork.Feet(garrison, false));
        }
    }
}
