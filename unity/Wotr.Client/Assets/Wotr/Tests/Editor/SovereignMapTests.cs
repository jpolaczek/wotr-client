using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-274: a king stands on the map as his force's crowned leader (Initial), as a character figure (Awakened) or as a
    // dark marker by his capital (Corrupted); a lit king's force and marker glow and say what their click does.
    public sealed class SovereignMapTests
    {
        [Test]
        public void EachKingStandsOnTheMapByHisState()
        {
            var data = Data();
            var erebor = data.Armies.Single(a => a.RegionId == "erebor");
            CollectionAssert.AreEqual(new[] { "dain" }, erebor.SovereignIds);
            CollectionAssert.AreEqual(new[] { "Dáin" }, erebor.CrownedTitles);
            StringAssert.EndsWith("Led by Dáin (not awakened)", ArmyMarkerText.Describe(erebor, "Erebor"));
            var edoras = data.Armies.Single(a => a.RegionId == "edoras");
            CollectionAssert.AreEqual(new[] { "theoden" }, edoras.SovereignIds);
            Assert.IsEmpty(edoras.CrownedTitles, "An Awakened king is a character figure, not a crowned leader.");
            Assert.AreEqual("denethor", data.CorruptedSovereigns.Single().Id);
            var map = new MapCanvas(data);
            Assert.IsFalse(Crown(map, "erebor").ClassListContains(MapArmyArtwork.UnusedClass));
            Assert.IsTrue(Crown(map, "edoras").ClassListContains(MapArmyArtwork.UnusedClass));
            var marker = map.SovereignMarkers.Find("denethor");
            Assert.AreEqual("Denethor · Corrupted · Minas Tirith", marker.tooltip);
            Assert.IsFalse(marker.ClassListContains(MapSovereignMarkers.LitClass));
            Assert.IsInstanceOf<DraftToken>(marker.userData);
        }

        [Test]
        public void ALitKingsForceAndMarkerGlowAndSayWhatTheClickDoes()
        {
            var map = new MapCanvas(Data());
            map.LightSovereigns(id => id switch
            {
                "dain" => "Click to awaken Dáin",
                "denethor" => "Click to remove the corrupted Denethor",
                _ => null
            });
            var army = map.Q<Button>("army-erebor-FreePeoples");
            Assert.IsTrue(army.ClassListContains(MapCanvas.SovereignTargetClass));
            StringAssert.StartsWith("Click to awaken Dáin\nErebor", army.tooltip);
            Assert.AreEqual("dain", map.LitSovereign(Token("erebor")));
            Assert.IsNull(map.LitSovereign(Token("edoras")));
            Assert.IsFalse(map.Q<Button>("army-edoras-FreePeoples").ClassListContains(MapCanvas.SovereignTargetClass));
            var marker = map.SovereignMarkers.Find("denethor");
            Assert.IsTrue(marker.ClassListContains(MapSovereignMarkers.LitClass));
            StringAssert.StartsWith("Click to remove the corrupted Denethor", marker.tooltip);
            // A force the selected die can move stays a force: the king is chosen by his portrait.
            map.HighlightSources(new[] { new ArmyOption { RegionId = "erebor" } }, Side.FreePeoples);
            Assert.IsNull(map.LitSovereign(Token("erebor")));
            Assert.IsFalse(army.ClassListContains(MapCanvas.SovereignTargetClass));
            StringAssert.StartsWith("Erebor", army.tooltip);
        }

        private static VisualElement Crown(MapCanvas map, string region) => map.Q<Button>("army-" + region + "-FreePeoples").Q("army-crown");

        private static DraftToken Token(string region) => new DraftToken(DraftObject.Army, region, side: Side.FreePeoples);

        private static BoardPresentation Data()
        {
            var data = BoardPresentation.Load();
            data.Apply(new PlayerView
            {
                MatchId = "match", You = Side.FreePeoples,
                Units =
                {
                    new UnitView { Id = "dain", Nation = "dwarves", Side = Side.FreePeoples, Kind = UnitKind.Leader, Location = "erebor" },
                    new UnitView { Id = "dwarves-1", Nation = "dwarves", Side = Side.FreePeoples, Kind = UnitKind.Regular, Location = "erebor" }
                },
                Characters =
                {
                    new CharacterView { Id = "theoden", DefinitionId = "kings.character.theoden.awakened", Side = Side.FreePeoples, Location = "edoras" }
                },
                Sovereigns =
                {
                    new SovereignView { Id = "dain", Title = "Dáin", RegionId = "erebor", Status = SovereignStatus.Initial, Resistance = 4 },
                    new SovereignView { Id = "theoden", Title = "Théoden", RegionId = "edoras", Status = SovereignStatus.Awakened, Resistance = 3 },
                    new SovereignView { Id = "denethor", Title = "Denethor", RegionId = "minas-tirith", Status = SovereignStatus.Corrupted, Resistance = 3 }
                }
            });
            return data;
        }
    }
}
