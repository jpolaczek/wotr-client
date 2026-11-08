using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-522 (owner's picture: Barad-dûr's field army, 4 Regulars, 1 Elite and 1 Nazgûl, its figures on one another): on
    // the map the army stands its three kinds side by side on its region's row stand — as wide as the stand places give
    // Barad-dûr — none covering another, each count readable at its figure's feet, with the Witch-king beside it.
    public sealed class MapArmyRowTests
    {
        private static readonly float[] Zooms = { 1.4f, 2.2f, 4f };

        // UI-531 (owner: "a second layout without a siege"; Erebor's Dwarves stood beside the gem at the Iron Hills'
        // border): without a garrison Erebor takes its open places round its middle; a garrison brings the siege places
        // back.
        [UnityTest]
        public IEnumerator EreborWithoutAGarrisonStandsItsArmyInItsOpenMiddle()
        {
            var fixture = new BoardFixture();
            foreach (var unit in fixture.View.Units.Where(u => u.Location == "erebor"))
                unit.Location = "reserve";
            fixture.Put("dwarves", UnitKind.Elite, "erebor", 2);
            fixture.Put("dwarves", UnitKind.Regular, "erebor", 1);
            fixture.Put("dwarves", UnitKind.Leader, "erebor", 1);
            fixture.Refresh();
            var slots = MapStandSlots.Load(BoardPresentation.Load()).Regions["erebor"];
            Assert.IsNotNull(slots.Open, "Erebor has open places");
            using (var client = new MountedMatchClient("http://localhost", fixture.View.MatchId))
            {
                client.Apply(fixture.View);
                yield return Settle();
                client.Board.Hover.Close();
                var map = client.Board.Map;
                Assert.AreEqual(slots.Open.Field, map.Stands.Stands("erebor").Field, "No garrison: the open field");
                map.FocusOn(slots.Open.Field, map.FitZoom * 2.2f);
                yield return Settle();
                LayoutTests.Save(client.Viewport, "ui531-erebor-open.png");
                client.Interface.Preferences.SetFigureSize(InterfacePreferences.DefaultFigureSize);
                yield return Settle();
                LayoutTests.Save(client.Viewport, "ui531-erebor-open-150.png");
            }
            foreach (var unit in fixture.View.Units.Where(u => u.Location == "erebor"))
                unit.Garrison = true;
            fixture.Put("sauron", UnitKind.Regular, "erebor", 2);
            fixture.Refresh();
            using (var client = new MountedMatchClient("http://localhost", fixture.View.MatchId))
            {
                client.Apply(fixture.View);
                yield return Settle();
                var stands = client.Board.Map.Stands.Stands("erebor");
                Assert.AreEqual(slots.Garrison, stands.Garrison, "A siege: the siege places");
                Assert.AreEqual(slots.Field, stands.Field);
            }
        }

        [UnityTest]
        public IEnumerator BaradDursArmyStandsItsKindsSideBySideOnItsRegionsRowStand()
        {
            var fixture = new BoardFixture();
            foreach (var unit in fixture.View.Units.Where(u => u.Location == "barad-dur"))
                unit.Location = "reserve";
            fixture.Put("sauron", UnitKind.Regular, "barad-dur", 4);
            fixture.Put("sauron", UnitKind.Elite, "barad-dur", 1);
            fixture.Put("sauron", UnitKind.Nazgul, "barad-dur", 1);
            fixture.View.Characters.Add(new CharacterView
            {
                Id = "witch-king", DefinitionId = "base.character.witch-king", Side = Side.Shadow, Location = "barad-dur"
            });
            fixture.Refresh();
            foreach (var (width, height, scale) in new[] { (1366, 768, 1.25f), (1920, 1080, 1f) })
            {
                using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
                var texture = new RenderTexture(width, height, 24);
                texture.Create();
                client.Panel.targetTexture = texture;
                client.Panel.scale = scale;
                client.Apply(fixture.View);
                yield return Settle();
                client.Board.Hover.Close();
                var map = client.Board.Map;
                var stands = map.Stands.Stands("barad-dur");
                foreach (var relative in Zooms)
                {
                    var label = $"{width}×{height}×{scale:0.00} at {relative:0.0} × fit";
                    map.FocusOn(stands.Field, Mathf.Min(map.FitZoom * relative, map.Camera.MaximumZoom));
                    yield return Settle();
                    LayoutTests.Save(texture, $"ui522-barad-dur-{width}-{scale:0.00}-{relative:0.0}.png");
                    var button = map.Q<Button>("army-barad-dur-Shadow");
                    Assert.IsNotNull(button, label);
                    if (!button.ClassListContains(MapArmyArtwork.FiguresClass))
                        continue;
                    Assert.IsTrue(button.ClassListContains(MapArmyArtwork.RowClass), "A row: " + label);
                    // UI-523: the middle figure is drawn in front, so figures and counts pair from left to right.
                    var figures = button.Query<Image>(className: "army-silhouette").ToList()
                        .Where(image => image.resolvedStyle.display != DisplayStyle.None).OrderBy(image => image.worldBound.center.x).ToList();
                    var counts = button.Query<Label>(className: MapArmyArtwork.FigureCountClass).ToList()
                        .Where(count => count.resolvedStyle.display != DisplayStyle.None).OrderBy(count => count.worldBound.center.x).ToList();
                    Assert.AreEqual(3, figures.Count, label);
                    CollectionAssert.AreEqual(new[] { "×1", "×4", "×1" }, counts.Select(c => c.text).ToArray(), label);
                    for (var i = 0; i < figures.Count; i++)
                    {
                        Assert.That(counts[i].worldBound.center.x, Is.EqualTo(figures[i].worldBound.center.x).Within(1f), label);
                        for (var j = 0; j < i; j++)
                        {
                            var share = Mathf.Min(figures[i].worldBound.width, figures[j].worldBound.width) * MapArmyRow.OverlapShare;
                            Assert.LessOrEqual(Overlap(figures[i].worldBound, figures[j].worldBound), share + .5f, $"Figures {j} and {i} overlap at most their share: {label}");
                            Assert.LessOrEqual(Overlap(counts[i].worldBound, counts[j].worldBound), .01f, $"Counts {j} and {i} apart: {label}");
                        }
                    }
                    // UI-526: the figures stand nearly as tall as a single army's, on a stand wider than a round one; the
                    // Witch-king beside them keeps the row from reaching past him, so it may shrink a little.
                    var unit = button.worldBound.width / MapArmyArtwork.HolderSize.x;
                    var stand = button.Q("army-stand").worldBound.width / unit;
                    foreach (var figure in figures)
                        Assert.GreaterOrEqual(figure.layout.height, .7f * 54, "Nearly full size beside the Witch-king: " + label);
                    // The Witch-king's column (StandFootprint.Character, 16 units round his figure's middle) stays clear.
                    var king = map.Q<Button>("character-witch-king").Q("character-figure").worldBound;
                    var half = StandFootprint.Character.FigureWidth / 2 * MapArmyArtwork.DesignUnit * unit;
                    var column = Rect.MinMaxRect(king.center.x - half, king.yMin, king.center.x + half, king.yMax);
                    foreach (var figure in figures)
                        Assert.AreEqual(0, Overlap(figure.worldBound, column), .5f, "The row stops short of the Witch-king: " + label);
                    Assert.Greater(stand, StandFootprint.Army.BaseWidth * MapArmyArtwork.DesignUnit, "Wider than a round stand: " + label);
                }
                client.Panel.targetTexture = client.Viewport;
                texture.Release();
                Object.Destroy(texture);
            }
        }

        private static float Overlap(Rect a, Rect b)
        {
            var across = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            var down = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return across > 0 && down > 0 ? across : 0;
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 4; frame++)
                yield return null;
        }
    }
}
