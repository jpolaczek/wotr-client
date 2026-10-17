using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // ART-01: every base unit has the figure of its own nation and kind; the UI-76 silhouette stays the fallback.
    public sealed class UnitFigureArtTests
    {
        [Test]
        public void EveryBaseUnitHasItsNationsFigureInTheUnitAtlas()
        {
            var board = BoardPresentation.Load().Board;
            var keys = board.Units.Select(u => FigureAppearances.UnitKey(u.Nation, u.Kind)).Distinct().ToList();
            Assert.AreEqual(22, keys.Count, "Five Free Peoples nations with three kinds, three Shadow nations with two, and the Nazgul.");
            CollectionAssert.AreEquivalent(keys, FigureArtwork.Units.Names);
            foreach (var key in keys)
            {
                var sprite = FigureArtwork.Units.Sprite(key);
                Assert.IsNotNull(sprite, key);
                Assert.Greater(sprite.rect.height, 100, key);
                StringAssert.EndsWith("-" + key, sprite.name);
            }
        }

        [Test]
        public void EliteAndRegularOfANationAreDifferentFiguresAndTheRegularStandsLower()
        {
            foreach (var nation in new[] { "gondor", "rohan", "elves", "dwarves", "north", "sauron", "isengard", "southrons" })
            {
                var regular = FigureArtwork.Units.Sprite(FigureAppearances.UnitKey(nation, UnitKind.Regular));
                var elite = FigureArtwork.Units.Sprite(FigureAppearances.UnitKey(nation, UnitKind.Elite));
                Assert.AreNotEqual(regular.rect, elite.rect, nation);
            }
        }

        [Test]
        public void ArtFollowsNationAndKindAndFallsBackToTheSidesSilhouette()
        {
            var unit = new UnitView { Id = "u", Nation = "southrons", Kind = UnitKind.Elite, Side = Side.Shadow, Location = "umbar" };
            var art = FigureAppearances.Art(unit);
            Assert.AreEqual("southrons-elite", art.Key);
            Assert.AreEqual(FigureAppearance.ShadowElite, art.Fallback);
            Assert.AreSame(FigureArtwork.Units.Sprite("southrons-elite"), FigureArtwork.Sprite(art));
            Assert.AreEqual("sauron-nazgul", FigureAppearances.UnitKey("sauron", UnitKind.Nazgul));
            var unknown = FigureAppearances.Unit("expansion-nation", UnitKind.Regular, Side.FreePeoples);
            Assert.AreSame(FigureArtwork.Sprite(FigureAppearance.FreeRegular), FigureArtwork.Sprite(unknown));
        }

        [Test]
        public void ArmyMarkerShowsTheLargestNationOfEachKind()
        {
            var army = ArmyMarker.From("helms-deep", Side.FreePeoples, true, new[]
            {
                ("gondor", UnitKind.Regular), ("rohan", UnitKind.Regular), ("rohan", UnitKind.Regular), ("rohan", UnitKind.Elite)
            });
            var button = new Button();
            var artwork = new MapArmyArtwork(button);
            artwork.Apply(army);
            var sprites = button.Query<Image>(className: "army-silhouette").ToList()
                .Where(image => !image.ClassListContains(MapArmyArtwork.UnusedClass)).Select(image => image.sprite).ToList();
            CollectionAssert.AreEqual(new[] { FigureArtwork.Units.Sprite("rohan-elite"), FigureArtwork.Units.Sprite("rohan-regular") }, sprites);
        }
    }
}
