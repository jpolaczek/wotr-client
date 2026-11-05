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
    // UI-406 (TABLETOP_FIGURES_UX, decisions 3 and 4): far out an army is a puck lying on the table, every settlement's
    // gem a stone with a side band and a shadow, a Corrupted king a small puck, and the Fellowship's figure stands on a
    // round Free Peoples base — on its region and on the Mordor track. The gems are moved and scaled with the map by a
    // transform, so a camera move repaints neither the map's vector layers nor any gem. UI-427: the Fellowship's figure
    // shows ◇ Hidden or ◆ Revealed on a plaque on its base's front, on its region and on the track, at both levels.
    public sealed class TabletopObjectsTests
    {
        private const string Region = "minas-tirith";
        // UI-427: the plaque's least height on the screen (map-world points): its 16 design points, never drawn smaller than
        // on a figure at 0.62 of its design size (FellowshipStateBadge.SmallestScale).
        private const float ReadablePlaque = 9.5f;

        [UnityTest]
        public IEnumerator TokensGemsKingsAndTheFellowshipLieOnTheTableAtBothLevels()
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
                    map.Fit();
                    yield return Settle();
                    LayoutTests.Save(texture, $"ui406-far-{width}-{scale:0.00}.png");
                    AssertTokens(map, label);
                    AssertReadable(map, "far " + label);
                    var far = AssertGem(map, "osgiliath", label);
                    yield return FellowshipStates(client, fixture, texture, true, "far " + label, $"ui427-far-{width}-{scale:0.00}");
                    map.FocusOn(map.Stands.Stands(Region).Field, map.FitZoom * 2.2f);
                    yield return Settle();
                    LayoutTests.Save(texture, $"ui406-near-{width}-{scale:0.00}.png");
                    AssertReadable(map, "near " + label);
                    Assert.IsTrue(map.Q<Button>("army-minas-tirith-FreePeoples").ClassListContains(MapArmyArtwork.FiguresClass), label);
                    var near = AssertGem(map, "osgiliath", label);
                    Assert.Greater(near, far, "A gem grows with the map · " + label);
                    AssertNameClearOfItsGem(map, Region, label);
                    AssertKing(map, label);
                    AssertFellowship(map.FellowshipMarker.Root, label);
                    yield return FellowshipStates(client, fixture, texture, false, "near " + label, $"ui427-near-{width}-{scale:0.00}");
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        // The Fellowship's figure stands on its base on the Mordor track's step too, at its UI-283 size.
        [UnityTest]
        public IEnumerator OnTheMordorTrackTheFellowshipStandsOnItsBase()
        {
            var fixture = Fixture();
            fixture.View.Fellowship.MordorStep = 2;
            fixture.View.Fellowship.RegionId = "mordor-track";
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            var texture = new RenderTexture(1366, 768, 24);
            texture.Create();
            client.Panel.targetTexture = texture;
            client.Apply(fixture.View);
            yield return Settle();
            client.Board.Hover.Close();
            var map = client.Board.Map;
            map.Camera.Zoom = map.FitZoom * 1.8f;
            map.Camera.Pan = map.contentRect.center - map.Data.MordorTrack.Bounds.center * map.Camera.Zoom;
            map.Refresh();
            yield return Settle();
            LayoutTests.Save(texture, "ui406-mordor-track.png");
            var figure = map.FellowshipMarker.Root;
            AssertFellowship(figure, "Mordor");
            var step = map.Q<Button>("mordor-track-step-2").worldBound;
            var stand = figure.Q("fellowship-stand").worldBound;
            Assert.That(stand.yMin + stand.height * .357f, Is.EqualTo(step.center.y).Within(3f), "The base stands on its step.");
            var art = figure.Q("figure-art").worldBound;
            Assert.That(art.height, Is.EqualTo(80 * 62 / 72f).Within(2f), "UI-283: the figure keeps its size.");
            yield return FellowshipStates(client, fixture, texture, false, "Mordor", "ui427-track-near");
            map.Fit();
            yield return Settle();
            Assert.IsFalse(map.MarkerVisibility.Detail, "The whole map: the track's token level.");
            yield return FellowshipStates(client, fixture, texture, false, "Mordor, the whole map", "ui427-track-far");
            client.Panel.targetTexture = client.Viewport;
            texture.Release();
            Object.Destroy(texture);
        }

        // The map legend draws the army entries with the same pucks and rims as the map.
        [UnityTest]
        public IEnumerator TheLegendShowsThePucksWithTheirStateRims()
        {
            var fixture = Fixture();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            yield return Settle();
            client.Board.Hover.Close();
            var map = client.Board.Map;
            PointerGestureEvents.ClickAt(map.Legend.Toggle);
            yield return Settle();
            LayoutTests.Save(client.Viewport, "ui406-legend.png");
            foreach (var (entry, puck, rim) in new (string Entry, string Puck, string Rim)[]
            {
                ("token-puck-free", "token-puck-free", null), ("token-puck-garrison-shadow", "token-puck-garrison-shadow", null),
                ("rim-token-rim-eligible", "token-puck-free", "rim-token-rim-eligible"),
                ("rim-token-rim-selected", "token-puck-free", "rim-token-rim-selected"),
                ("rim-token-rim-moved", "token-puck-free", "rim-token-rim-moved")
            })
            {
                var row = map.Legend.Popup.Q("map-legend-army-" + entry);
                Assert.IsNotNull(row, entry);
                var icon = row[0];
                Assert.AreEqual(puck, icon.resolvedStyle.backgroundImage.texture?.name, entry);
                Assert.AreEqual(rim == null ? 0 : 1, icon.childCount, entry);
                if (rim != null)
                    Assert.AreEqual(rim, icon[0].resolvedStyle.backgroundImage.texture?.name, entry);
            }
        }

        // The constraint of UI-406: panning, zooming and gliding move the gems by their transform, scaled clamp(zoom /
        // reference, .85, 1.4), and repaint neither a gem nor the map's vector layers; a capture repaints its gem alone.
        [UnityTest]
        public IEnumerator CameraMovesRepaintNoVectorLayerAndNoGemAndACaptureRepaintsItsGemOnly()
        {
            var fixture = Fixture();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            yield return Settle();
            client.Board.Hover.Close();
            var map = client.Board.Map;
            map.Fit();
            yield return Settle();
            Assert.Less(map.IndexOf(map.Settlements.Layer), map.IndexOf(map.Stands.Layer), "The gems lie under the stands.");
            var layers = map.Renderer.Repaints;
            var gems = map.Settlements.Repaints;
            var settlements = map.Data.Regions.Values.Count(region => region.Settlement != null);
            Assert.GreaterOrEqual(gems, settlements, "Every gem was painted once.");
            for (var frame = 0; frame < 40; frame++)
            {
                if (frame % 2 == 0)
                    map.Camera.ZoomAt(map.contentRect.center, frame < 24 ? 1.06f : .95f);
                else
                    map.Camera.Pan += new Vector2(frame < 20 ? -9 : 7, frame % 3 == 0 ? 5 : -4);
                map.Refresh();
                yield return null;
                AssertGemScale(map, "osgiliath");
            }
            map.FocusOn(map.Stands.Stands(Region).Field, map.FitZoom * 2.4f);
            for (var frame = 0; frame < 30; frame++)
                yield return null;
            AssertGemScale(map, "osgiliath");
            Assert.AreEqual(layers, map.Renderer.Repaints, "A camera move repaints no vector layer.");
            Assert.AreEqual(gems, map.Settlements.Repaints, "A camera move repaints no gem.");
            var gem = map.Settlements.Find("osgiliath");
            fixture.View.Control["osgiliath"] = Side.Shadow;
            fixture.View.Revision++;
            client.Apply(fixture.View);
            yield return Settle();
            Assert.AreSame(gem, map.Settlements.Find("osgiliath"));
            Assert.AreEqual(gems + 1, map.Settlements.Repaints, "A capture repaints the captured settlement's gem alone.");
        }

        private static void AssertTokens(MapCanvas map, string label)
        {
            Assert.IsFalse(map.MarkerVisibility.Detail, label);
            foreach (var (name, puck) in new[]
            {
                ("army-minas-tirith-FreePeoples", "token-puck-free"), ("army-dale-FreePeoples", "token-puck-free"),
                ("army-minas-tirith-FreePeoples-garrison", "token-puck-garrison-free"),
                ("army-minas-morgul-Shadow-garrison", "token-puck-garrison-shadow"), ("army-gorgoroth-Shadow", "token-puck-shadow")
            })
            {
                var token = map.Q<Button>(name);
                Assert.IsNotNull(token, name);
                if (token.resolvedStyle.display == DisplayStyle.None)
                    continue;
                Assert.IsTrue(token.ClassListContains(MapArmyArtwork.TokenClass), name + " · " + label);
                var picture = token.Q("army-puck");
                Assert.AreEqual(puck, picture.resolvedStyle.backgroundImage.texture?.name, name + " · " + label);
                var shadow = token.Q("army-contact-shadow").worldBound;
                Assert.Greater(shadow.yMax, picture.worldBound.yMax, "Its shadow lies under it · " + name + " · " + label);
            }
        }

        // UI-425, UI-426: the king's marker, the medallions and the tokens cover no force's count or plaque and no
        // settlement's stone, and no name lies on its own stone.
        private static void AssertReadable(MapCanvas map, string label)
        {
            TabletopFrameChecks.AssertReadable(map, label);
        }

        // The gem stands on its anchor at the gem scale; returns its drawn width.
        private static float AssertGem(MapCanvas map, string region, string label)
        {
            var gem = map.Settlements.Find(region);
            Assert.IsNotNull(gem, region);
            AssertGemScale(map, region);
            var anchor = map.Data.Shapes[region].Anchor;
            var point = map.LocalToWorld(map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])));
            Assert.That(Vector2.Distance(gem.worldBound.center, point), Is.LessThan(1f), "The gem lies on its anchor · " + label);
            return gem.worldBound.width;
        }

        // A name its own settlement's gem would reach into steps clear of the stone.
        private static void AssertNameClearOfItsGem(MapCanvas map, string region, string label)
        {
            var name = map.Q<Label>("map-name-" + region);
            Assert.AreEqual(Visibility.Visible, name.resolvedStyle.visibility, "The region keeps its name · " + label);
            var gem = map.Settlements.Bounds(region).Value;
            var stone = Rect.MinMaxRect(map.LocalToWorld(gem.min).x, map.LocalToWorld(gem.min).y,
                map.LocalToWorld(gem.max).x, map.LocalToWorld(gem.max).y);
            Assert.IsFalse(stone.Overlaps(name.worldBound), "The name lies clear of its gem · " + label);
        }

        private static void AssertGemScale(MapCanvas map, string region)
        {
            var holder = map.Settlements.Find(region).parent;
            var expected = map.Stands.Scale.Gem;
            Assert.That(holder.resolvedStyle.scale.value.x, Is.EqualTo(expected).Within(1e-3f));
            // UI-425: past the whole map of the worst case the gem keeps its board size, below its smallest scale.
            var smallest = MapStandScale.GemSmallest * map.Stands.Scale.Distant;
            Assert.That(expected, Is.InRange(smallest - 1e-5f, MapStandScale.GemLargest));
        }

        // The Corrupted king's marker is a small puck over its contact shadow, the crown and the Eye on its top face.
        private static void AssertKing(MapCanvas map, string label)
        {
            var marker = map.SovereignMarkers.Find("denethor");
            Assert.AreEqual(DisplayStyle.Flex, marker.resolvedStyle.display, label);
            var puck = marker.Q("sovereign-puck");
            Assert.AreEqual("puck-corrupted", puck.resolvedStyle.backgroundImage.texture?.name, label);
            var shadow = marker.Q("sovereign-shadow").worldBound;
            Assert.Greater(shadow.yMax, puck.worldBound.yMax, "Its shadow lies under it · " + label);
            Assert.Greater(shadow.center.x, puck.worldBound.center.x, "Light from the upper left · " + label);
            var emblem = marker.Q("sovereign-emblem").worldBound;
            Assert.IsTrue(puck.worldBound.Contains(emblem.center), "The emblem lies on the puck · " + label);
            Assert.That(marker.worldBound.width, Is.EqualTo(30).Within(1f), "The marker keeps its 30 points · " + label);
        }

        // The Fellowship's figure stands on its round base: its feet on the middle of the base's top face, the base's
        // shadow a little below and right, and no blue underline.
        private static void AssertFellowship(VisualElement figure, string label)
        {
            Assert.AreEqual(DisplayStyle.Flex, figure.resolvedStyle.display, label);
            var stand = figure.Q("fellowship-stand");
            Assert.AreEqual("stand-character-free", stand.resolvedStyle.backgroundImage.texture?.name, label);
            var art = figure.Q("figure-art").worldBound;
            var top = stand.worldBound.yMin + stand.worldBound.height * .357f;
            Assert.That(art.yMax, Is.EqualTo(top).Within(1.5f), "The figure's feet on its base · " + label);
            Assert.That(art.center.x, Is.EqualTo(stand.worldBound.center.x).Within(1f), label);
            var shadow = figure.Q("fellowship-contact-shadow").worldBound;
            Assert.Greater(shadow.yMax, stand.worldBound.yMax, "The base's shadow lies under it · " + label);
            Assert.Greater(shadow.center.x, stand.worldBound.center.x, label);
            Assert.AreEqual(0, figure.resolvedStyle.borderBottomWidth, "No underline · " + label);
        }

        // UI-427: the figure's plaque shows ◇ while hidden, ◆ once the view reveals it, and ◇ again. Far out on a region the
        // figure stands only while it offers a click (UI-280, UI-282): the move while hidden, the hide while revealed; the
        // whole map's zoom is centred on it there, since at 125 % Minas Tirith lies under the console.
        private static IEnumerator FellowshipStates(MountedMatchClient client, BoardFixture fixture, RenderTexture texture,
            bool far, string label, string picture)
        {
            var map = client.Board.Map;
            var fellowship = map.FellowshipMarker;
            if (far)
                map.FocusOn(map.Stands.Stands(Region).Fellowship, map.FitZoom);
            foreach (var (hidden, name) in new[] { (true, "hidden"), (false, "revealed"), (true, "") })
            {
                fixture.View.Fellowship.Hidden = hidden;
                fixture.View.Revision++;
                client.Apply(fixture.View);
                fellowship.OfferMove(far && hidden);
                fellowship.OfferHide(far && !hidden);
                yield return Settle();
                if (name.Length > 0)
                    LayoutTests.Save(texture, $"{picture}-{name}.png");
                Assert.IsTrue(AssertFellowshipState(fellowship, hidden, label), "The plaque is in view · " + label);
                AssertDiamondPainted(fellowship, texture, hidden, label);
            }
            fellowship.OfferMove(false);
            fellowship.OfferHide(false);
        }

        // UI-427: the plaque hangs on the base's front below the figure's feet, shown and readable, ◇ while hidden, ◆ while
        // revealed, and no map object covers it: a click there is the figure's own. Returns false when a window or the HUD
        // lies over it (then it is out of view, not covered by the map).
        internal static bool AssertFellowshipState(MapFellowshipMarker fellowship, bool hidden, string label)
        {
            var figure = fellowship.Root;
            var plaque = figure.Q(FellowshipStateBadge.ClassName);
            var state = (hidden ? "◇ Hidden" : "◆ Revealed") + " · " + label;
            Assert.IsNotNull(plaque, "The figure carries its plaque · " + label);
            Assert.AreEqual(DisplayStyle.Flex, figure.resolvedStyle.display, "The figure shows · " + state);
            Assert.AreEqual(1, figure.resolvedStyle.opacity, .001f, state);
            Assert.AreEqual(DisplayStyle.Flex, plaque.resolvedStyle.display, state);
            Assert.AreEqual(Visibility.Visible, plaque.resolvedStyle.visibility, state);
            Assert.AreEqual(1, plaque.resolvedStyle.opacity, .001f, state);
            Assert.AreEqual(hidden, plaque.ClassListContains(FellowshipStateBadge.HiddenClass), state);
            Assert.AreEqual(!hidden, plaque.ClassListContains(FellowshipStateBadge.RevealedClass), state);
            Assert.AreEqual(hidden ? "◇" : "◆", fellowship.Symbol, state);
            var stand = figure.Q("fellowship-stand").worldBound;
            var art = figure.Q("figure-art").worldBound;
            var box = plaque.worldBound;
            Assert.That(box.center.x, Is.EqualTo(stand.center.x).Within(1f), "On the base's front · " + state);
            Assert.GreaterOrEqual(box.yMin, art.yMax - 1, "Below the figure's feet · " + state);
            Assert.Less(box.yMin, stand.yMax, "Over the base · " + state);
            Assert.GreaterOrEqual(box.height, ReadablePlaque, "Readable · " + state);
            var picked = figure.panel.Pick(box.center);
            var map = figure.GetFirstAncestorOfType<MapCanvas>();
            if (picked == null || picked != map && !map.Contains(picked))
                return false;
            Assert.IsTrue(picked == figure || figure.Contains(picked),
                "No map object covers it, and a click there is the figure's · " + state + " (picked " + picked.name + ")");
            return true;
        }

        // UI-427: the diamond is painted in the frame: an ivory ◆ fills the plaque's middle; a ◇ leaves its middle the
        // plaque's blue inside an ivory outline.
        internal static void AssertDiamondPainted(MapFellowshipMarker fellowship, RenderTexture target, bool hidden, string label)
        {
            var pixels = MapContourRenderingTests.Capture(target);
            var box = fellowship.State.worldBound;
            var scale = target.width / fellowship.Root.panel.visualTree.worldBound.width;
            float Brightness(Vector2 world)
            {
                var x = Mathf.Clamp(Mathf.RoundToInt(world.x * scale), 0, target.width - 1);
                var y = Mathf.Clamp(target.height - 1 - Mathf.RoundToInt(world.y * scale), 0, target.height - 1);
                var pixel = (Color)pixels[y * target.width + x];
                return pixel.grayscale;
            }
            var brightest = 0f;
            for (var row = 2; row <= 8; row++)
                for (var column = 2; column <= 8; column++)
                    brightest = Mathf.Max(brightest, Brightness(box.min + Vector2.Scale(box.size, new Vector2(column, row) / 10f)));
            var middle = Brightness(box.center);
            var state = (hidden ? "◇ Hidden" : "◆ Revealed") + " · " + label;
            Assert.Greater(brightest, .6f, "An ivory diamond on the plaque · " + state);
            if (hidden)
                Assert.Less(middle, .4f, "Its middle is the plaque's blue · " + state);
            else
                Assert.Greater(middle, .6f, "Its middle is filled · " + state);
        }

        private static BoardFixture Fixture()
        {
            var fixture = new BoardFixture();
            fixture.View.Units.First(u => u.Location == Region && u.Kind == UnitKind.Leader).Garrison = true;
            fixture.Put("sauron", UnitKind.Regular, "minas-morgul", 2);
            foreach (var unit in fixture.View.Units.Where(u => u.Location == "minas-morgul"))
                unit.Garrison = true;
            fixture.Put("sauron", UnitKind.Regular, "gorgoroth", 3);
            fixture.Put("sauron", UnitKind.Elite, "gorgoroth", 1);
            fixture.View.Characters.Add(new CharacterView
            {
                Id = "strider", DefinitionId = "base.character.strider", Side = Side.FreePeoples, Location = Region, Level = 3,
                Leadership = 1
            });
            fixture.View.Fellowship.RegionId = Region;
            fixture.View.Sovereigns.Add(new SovereignView
            {
                Id = "denethor", Title = "Denethor", Nation = "gondor", RegionId = Region, Resistance = 3,
                Status = SovereignStatus.Corrupted
            });
            fixture.Refresh();
            return fixture;
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 8; i++)
                yield return null;
        }
    }
}
