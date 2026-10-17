using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-103: the notices hang by themselves under the top bar, with no strip across the map, and a second click
    // on a region closes the panel the first click opened.
    public sealed class BoardChromeLayoutTests
    {
        [UnityTest]
        public IEnumerator NoticesHangUnderTheTopBarWithoutAStripInFourLayouts()
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var fixture = new BoardFixture();
                    using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    try
                    {
                        client.Panel.targetTexture = texture;
                        client.Panel.scale = scale;
                        client.Apply(fixture.View);
                        yield return Settle();
                        CheckStrip(client.Board, $"{width}-{scale:0.00}");
                        LayoutTests.Save(texture, $"ui103-notices-{width}-{scale:0.00}.png");
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        texture.Release();
                        Object.Destroy(texture);
                    }
                }
        }

        // UI-138: a button without words is an icon or artwork, and its tooltip says what it is or does.
        [UnityTest]
        public IEnumerator EveryButtonWithoutWordsExplainsItselfAndNoGlyphButtonRemains()
        {
            var fixture = new BoardFixture();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            client.SetOnline();
            yield return Settle();
            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("muster"));
            yield return Settle();
            client.Board.Hover.Open("minas-tirith");
            yield return Settle();
            // The edge plaques and their corner tab open their panel on hover (UI-92, UI-98); a tooltip would cover it.
            var silent = client.Board.Root.Query<Button>().ToList()
                .Where(b => !b.ClassListContains(EdgePlaque.ClassName) && !b.ClassListContains("edge-corner-tab"))
                .Where(b => b.resolvedStyle.display != DisplayStyle.None && b.worldBound.width > 0 &&
                    !System.Text.RegularExpressions.Regex.IsMatch(b.text ?? "", "[A-Za-z]{2,}|[0-9]"))
                .Where(b => string.IsNullOrWhiteSpace(b.tooltip)).Select(b => b.name).ToList();
            Assert.IsEmpty(silent, "Buttons without words and without a tooltip: " + string.Join(", ", silent));
            // Map figures and the cells of the political track are artwork with a state glyph, not action buttons.
            var glyphs = client.Board.Root.Query<Button>().ToList()
                .Where(b => !b.ClassListContains("fellowship-marker") && !b.ClassListContains("nation-slot"))
                .Where(b => b.text is "×" or "‹" or "›" or "✓" or "◇" or "◆" or "↻" or "⚔" or "?" or "+" or "−" or "⇥" or "☑" or "☐")
                .Select(b => b.name).ToList();
            Assert.IsEmpty(glyphs, "Glyph buttons left: " + string.Join(", ", glyphs));
            Assert.IsNotEmpty(client.Board.Root.Query<Button>(className: IconButton.ClassName).ToList());
            LayoutTests.Save(client.Viewport, "ui138-board.png");
        }

        private static void CheckStrip(BoardScreen board, string label)
        {
            var bar = board.Notifications.Root;
            var top = board.TopBar.Root.worldBound;
            var stage = board.Root.Q("board-stage").worldBound;
            Assert.AreEqual(DisplayStyle.Flex, bar.resolvedStyle.display, "The log icon keeps the notices visible · " + label);
            Assert.AreEqual(Position.Absolute, bar.resolvedStyle.position, "The notices take no room in the layout · " + label);
            Assert.AreEqual(top.yMax, stage.yMin, 1, "The stage starts at the top bar · " + label);
            Assert.AreEqual(top.yMax, board.TopBar.Shadow.worldBound.yMin, 1, "The bar's shadow falls right under it · " + label);
            Assert.AreEqual(top.yMax, bar.worldBound.yMin, 1, "The notices hang under the top bar · " + label);
            Assert.Less(bar.worldBound.width, board.Root.worldBound.width / 2, "The notices are only as wide as their items · " + label);
            var log = board.Log.Toggle;
            var picked = log.panel.Pick(log.worldBound.center);
            Assert.IsTrue(picked == log || log.Contains(picked), "The notices lie above the stage · " + label);
            // Right of the items the area under the top bar belongs to the map.
            var gap = new Vector2(bar.worldBound.xMax + 60, bar.worldBound.center.y);
            var under = board.Root.panel.Pick(gap);
            Assert.IsTrue(under == board.Map || board.Map.Contains(under), $"Map under the top bar, not {under?.name} · {label}");
        }

        [UnityTest]
        public IEnumerator SecondClickOnARegionOrItsArmyClosesItsPanel()
        {
            var fixture = new BoardFixture();
            fixture.View.You = Side.Shadow;
            fixture.View.Decision = null;
            fixture.View.Options.Clear();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            yield return Settle();
            var board = client.Board;
            var panel = board.Hover.Panel.Root;
            board.Map.Focus("pelargir");
            yield return Settle();
            var pelargir = RegionInspectionTests.Terrain(board, "pelargir");
            RegionInspectionTests.ClickTerrain(board, pelargir);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display);
            Assert.AreEqual("pelargir", board.Hover.Panel.Region);
            RegionInspectionTests.ClickTerrain(board, pelargir);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, panel.resolvedStyle.display, "A second click on the region closes its panel.");
            RegionInspectionTests.ClickTerrain(board, pelargir);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "A third click opens it again.");
            var other = MapContourProbe.ExposedTerrain(board.Map, out var region);
            if (region == "pelargir")
                Assert.Inconclusive("The fixture camera exposes no second region.");
            RegionInspectionTests.ClickTerrain(board, other);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "Another region switches the panel.");
            Assert.AreEqual(region, board.Hover.Panel.Region);
            board.Hover.Close();

            var marker = board.Map.Q<Button>("army-minas-tirith-FreePeoples");
            var reach = MapContourProbe.ReachMarker(board.Map, marker, "minas-tirith");
            while (!reach.IsCompleted)
                yield return null;
            Assert.IsNull(reach.Exception);
            PointerGestureEvents.ClickAt(marker);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display);
            Assert.AreEqual("minas-tirith", board.Hover.Panel.Region);
            PointerGestureEvents.ClickAt(marker);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, panel.resolvedStyle.display, "A second click on the army closes its region panel.");
            Assert.AreEqual(0, board.View.Revision, "Inspection sends no command.");
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }
    }
}
