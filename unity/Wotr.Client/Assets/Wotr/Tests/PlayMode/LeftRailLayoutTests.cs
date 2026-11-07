using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class LeftRailLayoutTests
    {
        // UI-107: the rail order from the top.
        private static readonly string[] Keys = { "politics", "characters", "effects" };

        [UnityTest]
        public System.Collections.IEnumerator IllustratedRailAndPanelsKeepTheirTargetsInFourLayouts() =>
            MatchTestRoom.Wait(Layouts());

        private static async Task Layouts()
        {
            using var room = await UI10TestRoom.Connect("map15-navigation");
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var snapshot = JsonConvert.SerializeObject(board.View);
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        texture.Create();
                        try
                        {
                            client.Panel.targetTexture = texture;
                            client.Interface.Preferences.SetScale(scale);
                            await room.Settle();
                            using var mouse = new EngineMouseInput(board.Root);
                            var label = $"{board.View.You} {width} {scale:0.00}";
                            for (var i = 0; i < Keys.Length; i++)
                            {
                                mouse.Click(Tabs(board)[i]);
                                // UI-98: a click unrolls the panel like hovering, so its 140 ms unroll runs first.
                                await Task.Delay(220);
                                CheckTabs(board, i, label);
                                CheckPanel(board, Panels(board)[i], Keys[i], label);
                                LayoutTests.Save(texture, $"ui87-left-{board.View.You}-{Keys[i]}-{width}-{scale:0.00}.png");
                            }
                            mouse.Click(board.Nations.Root);
                            await room.Settle();
                            CheckPolitics(board, label);
                            CheckEntries(board, label);
                        }
                        finally
                        {
                            client.Panel.targetTexture = client.Viewport;
                            client.Interface.Preferences.SetScale(1);
                            texture.Release();
                            Object.Destroy(texture);
                        }
                    }
                Assert.AreEqual(snapshot, JsonConvert.SerializeObject(board.View));
            }
            Assert.AreEqual(0, room.Boundary.Commands, "Rail artwork cannot send game commands.");
        }

        // UI-518 (owner): Victory under Effects in the rail, with its own icon and both sides' points, clear of the map's
        // corner buttons in the four standard layouts; a click opens the Victory panel and a second click closes it.
        [UnityTest]
        public System.Collections.IEnumerator VictoryTabStandsUnderEffectsAndOpensThePanel()
        {
            var fixture = new Wotr.Fixtures.BoardFixture(false);
            fixture.View.MatchId = "victory-rail";
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Panel.scale = scale;
                    client.Apply(fixture.View);
                    yield return Frames();
                    var board = client.Board;
                    var label = $"{width} {scale:0.00}";
                    var tab = board.Victory.RailTab;
                    var rail = board.Edges.Left;
                    Assert.AreSame(rail, tab.parent, label);
                    Assert.AreEqual(rail.IndexOf(board.TableEffects.Tab) + 1, rail.IndexOf(tab), "Victory stands right under Effects · " + label);
                    Assert.Greater(tab.worldBound.yMin, board.TableEffects.Tab.worldBound.yMax - 1, label);
                    Assert.AreEqual("victory", Texture(tab.Q("edge-tab-icon-victory")), label);
                    StringAssert.StartsWith("VP ", tab.text, label);
                    StringAssert.Contains("Free Peoples", tab.tooltip, label);
                    var text = tab.MeasureTextSize(tab.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                    Assert.LessOrEqual(text.x, tab.contentRect.width + .5f, "The points fit on one line · " + label);
                    var corners = new[] { board.Root.Q("map-legend-toggle"), board.Root.Q("map-political-toggle"), board.Root.Q(className: "game-chat-icon") };
                    foreach (var corner in corners.Where(corner => corner != null && Visible(corner)))
                        Assert.IsFalse(tab.worldBound.Overlaps(corner.worldBound), $"Victory {tab.worldBound} clear of {corner.name} {corner.worldBound} · {label}");
                    LayoutTests.Save(texture, $"ui518-victory-tab-{width}-{scale:0.00}.png");
                    CardNetworkTests.Click(tab);
                    yield return Frames();
                    Assert.IsTrue(board.Victory.IsOpen, "A click opens the Victory panel · " + label);
                    LayoutTests.Save(texture, $"ui518-victory-open-{width}-{scale:0.00}.png");
                    CardNetworkTests.Click(tab);
                    yield return Frames();
                    Assert.IsFalse(board.Victory.IsOpen, "A second click closes it · " + label);
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        // Owner (2026-11-08): closing the Victory window with a second click on its rail icon flashed it in the top left
        // corner for a moment. The window fades out where it stood, on both layouts.
        [UnityTest]
        public System.Collections.IEnumerator VictoryClosedFromTheRailFadesOutWhereItStood()
        {
            var fixture = new Wotr.Fixtures.BoardFixture(false);
            fixture.View.MatchId = "victory-close";
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            foreach (var (width, scale) in new[] { (1920, 1f), (1366, 1.25f) })
            {
                var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                texture.Create();
                client.Panel.targetTexture = texture;
                client.Panel.scale = scale;
                client.Apply(fixture.View);
                yield return Frames();
                var board = client.Board;
                var panel = board.Victory.Root;
                CardNetworkTests.Click(board.Victory.RailTab);
                yield return Frames();
                var stood = panel.worldBound.position;
                CardNetworkTests.Click(board.Victory.RailTab);
                for (var frame = 0; frame < 30 && panel.resolvedStyle.display != DisplayStyle.None; frame++)
                {
                    Assert.That(Vector2.Distance(panel.worldBound.position, stood), Is.LessThan(1f),
                        $"The closing window stays where it stood ({stood}), frame {frame} · {width} {scale:0.00}");
                    yield return null;
                }
                Assert.IsFalse(board.Victory.IsOpen);
                client.Panel.targetTexture = client.Viewport;
                texture.Release();
                Object.Destroy(texture);
            }
        }

        private static System.Collections.IEnumerator Frames()
        {
            for (var frame = 0; frame < 4; frame++)
                yield return null;
        }

        private static Button[] Tabs(BoardScreen board) => new[] { board.Nations.Root, board.Characters.Tab, board.TableEffects.Tab };

        private static VisualElement[] Panels(BoardScreen board) =>
            new[] { board.Nations.Drawer, board.Characters.Root, board.TableEffects.Root };

        private static void CheckTabs(BoardScreen board, int open, string label)
        {
            var tabs = Tabs(board);
            for (var i = 1; i < tabs.Length; i++)
                Assert.Greater(tabs[i].worldBound.yMin, tabs[i - 1].worldBound.yMax - 1, Keys[i] + " below " + Keys[i - 1] + " · " + label);
            for (var i = 0; i < tabs.Length; i++)
            {
                // UI-98: a clicked tab only unrolls its panel; the tab keeps the frame of the hovering pointer.
                // UI-181: the tabs have their own art (rail/edge-tab).
                var expected = i == open ? "edge-tab-hover" : "edge-tab";
                Assert.AreEqual(expected, Texture(tabs[i]), Keys[i] + " tab · " + label);
                // UI-182: a plain label on one line, centred under the medallion and inside the tab.
                Assert.AreEqual(WhiteSpace.NoWrap, tabs[i].resolvedStyle.whiteSpace, Keys[i] + " · " + label);
                Assert.AreEqual(TextAnchor.LowerCenter, tabs[i].resolvedStyle.unityTextAlign, Keys[i] + " · " + label);
                Assert.AreEqual(FontStyle.Normal, tabs[i].resolvedStyle.unityFontStyleAndWeight, Keys[i] + " · " + label);
                var text = tabs[i].MeasureTextSize(tabs[i].text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(text.x, tabs[i].contentRect.width + .5f, Keys[i] + " label fits on one line · " + label);
                var icon = tabs[i].Q("edge-tab-icon-" + Keys[i]);
                Assert.IsNotNull(icon, Keys[i]);
                Assert.AreEqual(Keys[i], Texture(icon), label);
                Assert.AreEqual(PickingMode.Ignore, icon.pickingMode);
                Inside(icon.worldBound, tabs[i].worldBound, Keys[i] + " icon · " + label);
                var picked = board.Root.panel.Pick(icon.worldBound.center);
                Assert.IsTrue(picked == tabs[i] || tabs[i].Contains(picked) && picked != icon,
                    Keys[i] + " tab target taken by " + picked?.name);
                Assert.AreEqual(i == open, Visible(Panels(board)[i]), Keys[i] + " panel · " + label);
            }
        }

        private static void CheckPanel(BoardScreen board, VisualElement panel, string key, string label)
        {
            Assert.AreEqual("card-board", Texture(panel), key + " wears the menus' card (UI-472) · " + label);
            Assert.That(panel.worldBound.xMin, Is.EqualTo(board.Edges.Left.worldBound.xMax).Within(1), label);
            Inside(panel.worldBound, board.Map.worldBound, key + " panel · " + label);
            var header = panel.Q(className: "panel-header");
            Assert.AreEqual("title-rule", Texture(header), key + " has a hairline, not a band (UI-472) · " + label);
            var icon = header.Q("panel-header-icon-" + key);
            Assert.AreSame(header, icon.parent);
            Assert.AreEqual(0, header.IndexOf(icon));
            Assert.AreEqual(key, Texture(icon), label);
            Assert.AreEqual(PickingMode.Ignore, icon.pickingMode);
            // UI-472: the title in the menus' type, Politics' plain label included, is never cut.
            var title = header.Children().OfType<Label>().First();
            var width = title.MeasureTextSize(title.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            Assert.LessOrEqual(width, title.contentRect.width + 1, key + " title fits · " + label);
            foreach (var button in header.Query<Button>().ToList().Where(Visible))
            {
                Assert.That(Texture(button), Does.StartWith("button"), button.name + " wears the shared button art (UI-471) · " + label);
                Inside(button.worldBound, header.worldBound, button.name + " · " + label);
            }
        }

        private static void CheckPolitics(BoardScreen board, string label)
        {
            var rows = board.Nations.Drawer.Query(className: "nation-track").ToList().Where(Visible).ToList();
            Assert.AreEqual(board.View.Nations.Count, rows.Count, "Show opponent is on · " + label);
            foreach (var nation in board.View.Nations)
            {
                var row = board.Nations.Drawer.Q("nation-track-" + nation.Id);
                // UI-197: the crest is the face of the nation's medallion on the track, over a soft glow.
                var emblem = row.Q("nation-emblem-" + nation.Id);
                var shadow = row.ClassListContains("shadow-nation");
                var token = row.Q<Button>("nation-status-" + nation.Id);
                Assert.AreSame(token, emblem.parent, nation.Id + " crest rides its medallion · " + label);
                Assert.AreEqual(nation.Id, Texture(emblem), label);
                Assert.AreEqual(PickingMode.Ignore, emblem.pickingMode);
                Inside(emblem.worldBound, token.worldBound, nation.Id + " emblem · " + label);
                var glow = row.Q("nation-glow-" + nation.Id);
                Assert.AreEqual(PickingMode.Ignore, glow.pickingMode);
                Assert.That(glow.worldBound.center.x, Is.EqualTo(token.worldBound.center.x).Within(1), nation.Id + " glow · " + label);
                Assert.Greater(glow.worldBound.width, token.worldBound.width);
                Assert.AreEqual(nation.Active, glow.ClassListContains("active-nation-glow"));
                Assert.AreEqual(shadow ? "politics-token-shadow" : "politics-token-free", Texture(token), nation.Id + " · " + label);
            }
            var legend = board.Nations.Drawer.Q(className: "politics-axis").Q(className: "nation-name");
            // A window scaled to fit snaps each edge to a device pixel, so equal columns may differ by up to one pixel
            // (UI-197: 63.75 and 64.46 points at 1920 × 1.00, with the same row width).
            var name = rows[0].Q(className: "nation-name");
            Assert.That(legend.worldBound.xMin, Is.EqualTo(name.worldBound.xMin).Within(1f), "Axis legend and names start together · " + label);
            Assert.That(legend.worldBound.width, Is.EqualTo(name.worldBound.width).Within(1f),
                "Axis legend and nation names share one column · " + label);
        }

        private static void CheckEntries(BoardScreen board, string label)
        {
            foreach (var entry in board.TableEffects.Root.Query(className: "effect-entry").ToList())
                Assert.That(Texture(entry), Does.StartWith(entry.ClassListContains("shadow-effect") ? "row-shadow" : "row"), entry.name + " · " + label);
            // UI-391: a row stands in its cell, above its ability icons.
            foreach (var entry in board.Characters.Root.Query<Button>(className: "character-roster-row").ToList())
                Assert.That(Texture(entry), Does.StartWith("row"), entry.name + " · " + label);
        }

        private static string Texture(VisualElement element) => element.resolvedStyle.backgroundImage.texture?.name;

        private static void Inside(Rect inner, Rect outer, string label)
        {
            Assert.GreaterOrEqual(inner.xMin + 1, outer.xMin, label);
            Assert.LessOrEqual(inner.xMax, outer.xMax + 1, label);
            Assert.GreaterOrEqual(inner.yMin + 1, outer.yMin, label);
            Assert.LessOrEqual(inner.yMax, outer.yMax + 1, label);
        }

        private static bool Visible(VisualElement element) => element.resolvedStyle.display != DisplayStyle.None &&
            element.worldBound.width > 0 && element.worldBound.height > 0;
    }
}
