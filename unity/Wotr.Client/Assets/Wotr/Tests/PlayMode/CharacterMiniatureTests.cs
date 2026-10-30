using System.Collections;
using System.Collections.Generic;
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
    // UI-402 (TABLETOP_FIGURES_UX, decision 5): every character is its own miniature on a small round base beside the
    // army in its region — Gandalf and Strider two distinct figures, a garrison's character at the gate, a separated
    // companion alone — standing on its region's character places, sized with the map like the armies. Its click chooses
    // it as its region panel's chip does and opens no panel; the army's click still takes every figure; more than six
    // stand as a stacked pair with "+n".
    public sealed class CharacterMiniatureTests
    {
        private const string Region = "minas-tirith";

        [UnityTest]
        public IEnumerator CharactersStandAsTheirOwnFiguresBesideTheirArmy()
        {
            var fixture = Fixture();
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Panel.scale = scale;
                    client.Apply(fixture.View);
                    yield return Settle();
                    client.Board.Hover.Close();
                    var map = client.Board.Map;
                    var label = $"{width} × {scale:0.00}";
                    map.FocusOn(map.Stands.Stands(Region).Field, map.FitZoom * 2.2f);
                    yield return Settle();
                    LayoutTests.Save(texture, $"ui402-characters-{width}-{scale:0.00}.png");
                    AssertTwoDistinctFigures(map, label);
                    foreach (var id in new[] { "gandalf", "strider", "boromir" })
                        AssertOnItsPlace(map, id, label);
                    Assert.AreEqual("3", map.Q<Button>("army-minas-tirith-FreePeoples").Q<Label>("army-count").text,
                        "The army counts its units, not the characters beside it · " + label);
                    AssertHitShape(map.Q<Button>("character-strider"), label);
                    var witchKing = map.Q<Button>("character-witch-king");
                    Assert.IsTrue(witchKing.ClassListContains("sh"), label);
                    Assert.IsNull(map.Q<Button>("army-lossarnach-FreePeoples"), "A separated companion is no army · " + label);
                    Assert.IsNotNull(map.Q<Button>("character-legolas"), label);
                    map.Fit();
                    yield return Settle();
                    var token = map.Q<Button>("character-strider");
                    if (token.resolvedStyle.display == DisplayStyle.Flex && !map.MarkerVisibility.Detail)
                        Assert.IsTrue(token.ClassListContains(MapCharacterArtwork.TokenClass), "Far out a character is a token · " + label);
                    LayoutTests.Save(texture, $"ui402-characters-fit-{width}-{scale:0.00}.png");
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        // The click on Strider's figure chooses him as his chip does, and no region panel opens; the army's click takes
        // its units and both characters.
        [UnityTest]
        public IEnumerator ACharacterClickChoosesItAsItsChipDoesAndTheArmyClickTakesThemAll()
        {
            var fixture = Fixture();
            HashSet<string> byFigure;
            HashSet<string> byChip;
            using (var client = Mounted(fixture))
            {
                yield return Armed(client);
                var strider = client.Board.Map.Q<Button>("character-strider");
                PointerGestureEvents.ClickAt(strider);
                yield return Settle();
                Assert.IsTrue(strider.ClassListContains(MapCharacterArtwork.SelectedClass), "His figure shows the selection.");
                Assert.AreEqual(DisplayStyle.None, client.Board.Hover.Panel.Root.resolvedStyle.display, "No region panel opens.");
                LayoutTests.Save(client.Viewport, "ui402-character-selected.png");
                byFigure = Chosen(client, fixture);
            }
            using (var client = Mounted(fixture))
            {
                yield return Armed(client);
                client.Board.Hover.Open(Region);
                yield return Settle();
                CardNetworkTests.Click(client.Board.Hover.Panel.Root.Q<Button>("hover-figure-strider"));
                yield return Settle();
                byChip = Chosen(client, fixture);
                Assert.IsTrue(client.Board.Map.Q<Button>("character-strider").ClassListContains(MapCharacterArtwork.SelectedClass),
                    "The chip's choice lights his figure too.");
            }
            CollectionAssert.IsNotEmpty(byFigure);
            CollectionAssert.AreEquivalent(byChip, byFigure, "The figure's click is the chip's.");
            using (var client = Mounted(fixture))
            {
                yield return Armed(client);
                PointerGestureEvents.ClickAt(client.Board.Map.Q<Button>("army-minas-tirith-FreePeoples"));
                yield return Settle();
                var chosen = Chosen(client, fixture);
                Assert.IsTrue(chosen.Contains("gandalf") && chosen.Contains("strider"), "The army's click takes its characters too.");
                foreach (var id in new[] { "gandalf", "strider" })
                    Assert.IsTrue(client.Board.Map.Q<Button>("character-" + id).ClassListContains(MapCharacterArtwork.SelectedClass), id);
            }
        }

        // The stress fixture's eight characters in Minas Tirith: five alone and a stacked pair with "+2" on the last place.
        [UnityTest]
        public IEnumerator MoreThanSixCharactersStandAsAStackedPair()
        {
            var fixture = new BoardFixture(true);
            using var client = Mounted(fixture);
            yield return Settle();
            client.Board.Hover.Close();
            var map = client.Board.Map;
            map.FocusOn(map.Stands.Stands(Region).Field, map.FitZoom * 2.6f);
            yield return Settle();
            LayoutTests.Save(client.Viewport, "ui402-stacked-pair.png");
            var shown = fixture.View.Characters.Select(c => map.Q<Button>("character-" + c.Id))
                .Where(button => button.resolvedStyle.display == DisplayStyle.Flex).ToList();
            Assert.AreEqual(RegionStands.CharacterPlaces, shown.Count, "Six places show six miniatures.");
            var stack = shown.Single(button => button.ClassListContains(MapCharacterArtwork.StackedClass));
            var more = stack.Q<Label>("character-more");
            Assert.AreEqual("+2", more.text);
            Assert.AreEqual(DisplayStyle.Flex, more.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, stack.Q("character-behind").resolvedStyle.display, "A second figure stands behind.");
        }

        private static BoardFixture Fixture()
        {
            var fixture = new BoardFixture();
            Character(fixture, "gandalf", "gandalf-grey", Side.FreePeoples, Region);
            Character(fixture, "strider", "strider", Side.FreePeoples, Region);
            Character(fixture, "boromir", "boromir", Side.FreePeoples, Region).Garrison = true;
            Character(fixture, "legolas", "legolas", Side.FreePeoples, "lossarnach");
            Character(fixture, "witch-king", "witch-king", Side.Shadow, "osgiliath");
            fixture.Refresh();
            return fixture;
        }

        private static CharacterView Character(BoardFixture fixture, string id, string name, Side side, string region)
        {
            var character = new CharacterView
            {
                Id = id, DefinitionId = "base.character." + name, Side = side, Location = region, Level = 2, Leadership = 1
            };
            fixture.View.Characters.Add(character);
            return character;
        }

        private static MountedMatchClient Mounted(BoardFixture fixture)
        {
            var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            return client;
        }

        // The Army die chosen and Minas Tirith in view at the figures' level.
        private static IEnumerator Armed(MountedMatchClient client)
        {
            yield return Settle();
            client.Board.Hover.Close();
            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("army"));
            yield return Settle();
            client.Board.Hover.Close();
            var map = client.Board.Map;
            map.FocusOn(map.Stands.Stands(Region).Field, map.FitZoom * 2.2f);
            yield return Settle();
        }

        // The figures of Minas Tirith the draft has chosen, as the shared choices say.
        private static HashSet<string> Chosen(MountedMatchClient client, BoardFixture fixture)
        {
            var choices = client.Board.Hover.Panel.Choices;
            return fixture.View.Units.Where(u => u.Location == Region).Select(u => u.Id)
                .Concat(fixture.View.Characters.Select(c => c.Id)).Where(choices.Selected).ToHashSet();
        }

        private static void AssertTwoDistinctFigures(MapCanvas map, string label)
        {
            var gandalf = map.Q<Button>("character-gandalf");
            var strider = map.Q<Button>("character-strider");
            foreach (var button in new[] { gandalf, strider })
            {
                Assert.AreEqual(DisplayStyle.Flex, button.resolvedStyle.display, button.name + " · " + label);
                Assert.IsTrue(button.ClassListContains(MapCharacterArtwork.FiguresClass), button.name + " · " + label);
            }
            var first = gandalf.Q<Image>("character-figure").sprite;
            var second = strider.Q<Image>("character-figure").sprite;
            StringAssert.EndsWith("gandalf-grey", first.name, label);
            StringAssert.EndsWith("strider", second.name, "Gandalf and Strider are two different figures · " + label);
            Assert.IsFalse(gandalf.worldBound.center == strider.worldBound.center, label);
        }

        // Its feet on one of its region's character places (a garrison's at the gate), inside the region, at the armies' size.
        private static void AssertOnItsPlace(MapCanvas map, string id, string label)
        {
            var button = map.Q<Button>("character-" + id);
            var stands = map.Stands.Stands(Region);
            var garrison = id == "boromir";
            var places = Enumerable.Range(0, garrison ? RegionStands.GarrisonCharacterPlaces : RegionStands.CharacterPlaces)
                .Select(i => garrison ? stands.GarrisonCharacter(i) : stands.Character(i));
            var feet = button.LocalToWorld(MapCharacterArtwork.Feet(true));
            var nearest = places.Min(place => Vector2.Distance(feet, map.LocalToWorld(map.Camera.ToScreen(place))));
            Assert.That(nearest, Is.LessThan(.5f), "Feet on a character place: " + id + " · " + label);
            Assert.AreEqual(Region, map.HitTest(map.WorldToLocal(feet)), "It stands inside its region: " + id + " · " + label);
            var drawn = button.worldBound.width / button.resolvedStyle.width;
            var wanted = Mathf.Clamp(map.Camera.Zoom, MapStandSlots.WorstCaseZoom(map.Data.Geometry), MapCharacterArtwork.DesignUnit) /
                MapCharacterArtwork.DesignUnit;
            Assert.That(drawn, Is.EqualTo(wanted).Within(.002f), "Sized like the armies: " + id + " · " + label);
        }

        // Only the stand and the figure's column take a press; the box's corner passes it on.
        private static void AssertHitShape(Button button, string label)
        {
            var corner = new Vector2(button.worldBound.xMin + 2, button.worldBound.yMin + 2);
            Assert.AreNotSame(button, button.panel.Pick(corner), "The headroom takes no press · " + label);
            var figure = button.Q<Image>("character-figure").worldBound.center;
            var picked = button.panel.Pick(figure);
            Assert.IsTrue(picked == button || button.Contains(picked), "The figure takes the press · " + label);
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
