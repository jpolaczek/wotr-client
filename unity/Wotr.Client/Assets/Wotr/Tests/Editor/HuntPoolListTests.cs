using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-141: the Hunt pool list and the printed faces of Hunt tiles.
    public sealed class HuntPoolListTests
    {
        private static HuntTileFaceView Face(string pool, string value, int count = 1, bool reveal = false, bool stop = false,
            string source = "base") => new HuntTileFaceView { Pool = pool, Value = value, Count = count, Reveal = reveal, Stop = stop, Source = source };

        [Test]
        public void EveryFaceExplainsItsValueAndMarks()
        {
            StringAssert.Contains("successful Hunt dice", HuntTileText.Tooltip(Face("standard", "eye")));
            StringAssert.Contains("Heals 2 Corruption", HuntTileText.Tooltip(Face("special-fp", "-2")));
            StringAssert.Contains("3 Hunt damage", HuntTileText.Tooltip(Face("standard", "3")));
            var red = HuntTileText.Tooltip(Face("special-sh", "die", reveal: true, stop: true));
            StringAssert.Contains("die roll", red);
            StringAssert.Contains("Reveal ·", red);
            StringAssert.Contains("Stop ·", red);
            StringAssert.Contains("Shadow special tile", red);
            StringAssert.Contains("Sméagol joins", HuntTileText.Tooltip(Face(HuntTileText.Smeagol, "0", source: "lords")));
            StringAssert.Contains("Kings of Middle-earth", HuntTileText.Tooltip(Face("standard", "2", source: "kings")));
        }

        // UI-420: the value stands once, the kind leads, the edition ends the first line and no line is blank.
        [Test]
        public void ATileNamesItsValueOnceAfterItsKind()
        {
            Assert.AreEqual("Standard tile · 3 Hunt damage", HuntTileText.Tooltip(Face("standard", "3")));
            Assert.AreEqual("Shadow special tile · Die · Damage equals a die roll\n" +
                "Reveal · The Fellowship is revealed, apart from the damage\nStop · Stops the Fellowship's move on the Mordor track",
                HuntTileText.Tooltip(Face("special-sh", "die", reveal: true, stop: true)));
            Assert.AreEqual("Standard tile · 2 Hunt damage · Kings of Middle-earth", HuntTileText.Tooltip(Face("standard", "2", source: "kings")));
        }

        // UI-420: a drawn tile repeats its value only when the resolved damage differs, and explains Reveal and Stop
        // together only when it has both.
        [Test]
        public void ADrawnTileShowsTheResolvedValueOnlyWhenItDiffers()
        {
            var printed = new HuntView { TileId = "base.hunt.standard.3.0.1", Damage = 3, Face = Face("standard", "3") };
            Assert.AreEqual("Drawn tile · Standard tile · 3 Hunt damage", HuntTileView.Tooltip(printed, "Damage 3"));
            var changed = new HuntView { TileId = "base.hunt.standard.3.0.1", Damage = 2, Face = Face("standard", "3") };
            Assert.AreEqual("Drawn tile · Standard tile · 3 Hunt damage\nDamage 2", HuntTileView.Tooltip(changed, "Damage 2"));
            var both = new HuntView { TileId = "base.hunt.special-sh.die", Damage = 4, Reveal = true, Stop = true,
                Face = Face("special-sh", "die", reveal: true, stop: true) };
            StringAssert.Contains("\nReveal and Stop are independent effects.", HuntTileView.Tooltip(both, "Damage 4"));
            StringAssert.DoesNotContain("\n\n", HuntTileView.Tooltip(both, "Damage 4"));
            StringAssert.DoesNotContain("independent", HuntTileView.Tooltip(printed, "Damage 3"));
        }

        [Test]
        public void ListShowsOneEntryPerFaceAndRebuildsOnlyWhenTheCompositionChanges()
        {
            var list = new HuntPoolList();
            var pool = new HuntPoolView { Count = 5, Tiles = { Face("standard", "eye", 4), Face("standard", "3", 1) } };
            list.Apply(pool);
            var entries = list.Root.Q("hunt-pool-entries").Children().ToList();
            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("×4", entries[0].Q<Label>(className: "hunt-pool-count").text);
            Assert.IsTrue(entries.All(e => e.tooltip.Length > 0));
            Assert.IsTrue(entries[0].Q(className: HuntTileFace.ClassName).ClassListContains("hunt-face-symbol-eye"));
            Assert.AreEqual("In the pool · 5", list.Root.Q<Label>("hunt-pool-heading").text);
            Assert.AreEqual(DisplayStyle.None, list.Root.Q("hunt-pool-prepared").style.display.value);
            list.Apply(new HuntPoolView { Count = 5, Tiles = { Face("standard", "eye", 4), Face("standard", "3", 1) } });
            Assert.AreSame(entries[0], list.Root.Q("hunt-pool-entries").Children().First(), "An unchanged composition keeps the entries.");
            list.Apply(new HuntPoolView { Count = 3, Unseen = 1, Tiles = { Face("standard", "eye", 4) }, Prepared = { Face("special-sh", "die", stop: true) } });
            StringAssert.Contains("1 face down elsewhere", list.Root.Q<Label>("hunt-pool-heading").text);
            Assert.AreEqual(DisplayStyle.Flex, list.Root.Q("hunt-pool-prepared").style.display.value);
            Assert.AreEqual(1, list.Root.Q("hunt-pool-prepared").childCount);
            Assert.AreEqual(DisplayStyle.Flex, list.Root.Q<Label>("hunt-pool-prepared-heading").style.display.value);
        }

        [Test]
        public void DrawnTileShowsItsArtworkAndKeepsTheNameOnlyWithoutAFace()
        {
            var tile = new HuntTileView(BoardPresentation.Load());
            tile.Apply(new HuntView { TileId = "base.hunt.standard.eye.1.1", Face = Face("standard", "eye", reveal: true) });
            Assert.AreEqual(DisplayStyle.Flex, tile.Root.Q("hunt-tile-art").style.display.value);
            Assert.AreEqual(DisplayStyle.None, tile.Root.Q<Label>("hunt-tile-face").style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, tile.Root.Q("hunt-face-reveal").style.display.value);
            StringAssert.StartsWith("Drawn tile · Standard tile · Eye ·", tile.Root.tooltip);
            tile.Apply(new HuntView { TileId = "base.hunt.standard.eye.1.1" });
            Assert.AreEqual(DisplayStyle.None, tile.Root.Q("hunt-tile-art").style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, tile.Root.Q<Label>("hunt-tile-face").style.display.value);
        }
    }
}
