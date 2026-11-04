using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-472, UI-473, UI-474: every panel and window of the board wears the menus' card and a quiet title over a hairline,
    // from the shared tokens of Styles/GameChrome.uss. These checks read what UI Toolkit resolved, so they also prove that
    // var() reaches each surface. The hairline is a picture, so a header gives up no layout to it, and a title without the
    // faux bold never takes more room than the bold one it replaced.
    public sealed class GameWindowChromeTests
    {
        private static readonly Color TitleGold = new Color32(232, 207, 146, 255);
        private static readonly Color Ink = new Color32(58, 42, 24, 255);
        private static readonly Color Hairline = new Color32(163, 132, 75, 255);
        // A battle title at its longest: every window's title is measured with it, so none is cut where it fitted.
        private const string LongTitle = "Dol Amroth · Relief · Round 12 · Report";

        [UnityTest]
        public IEnumerator EveryFrameWearsTheBoardCardWithoutABackgroundColour()
        {
            using var client = Mount();
            yield return Settle();
            foreach (var frame in Frames(client.Board))
            {
                var style = frame.resolvedStyle;
                Assert.AreEqual("card-board", style.backgroundImage.texture?.name, frame.name);
                Assert.AreEqual(24f, style.unitySliceTop, frame.name + ": the 12-point slice of the 2x art");
                Assert.AreEqual(24f, style.unitySliceRight, frame.name);
                Assert.AreEqual(24f, style.unitySliceBottom, frame.name);
                Assert.AreEqual(24f, style.unitySliceLeft, frame.name);
                Assert.AreEqual(0.5f, style.unitySliceScale, 0.001f, frame.name);
                Assert.AreEqual(0f, style.backgroundColor.a, 0.01f, frame.name + ": a colour would fill the art's cut corners");
            }
        }

        [UnityTest]
        public IEnumerator EveryHeaderIsAQuietTitleOverAHairlineThatTakesNoLayout()
        {
            using var client = Mount();
            yield return Settle();
            foreach (var header in Headers(client.Board))
                Reveal(header);
            yield return Settle();
            foreach (var header in Headers(client.Board))
            {
                var style = header.resolvedStyle;
                Assert.AreEqual("title-rule", style.backgroundImage.texture?.name, header.parent.name + " has a hairline, not a band");
                Assert.Greater(style.height, 0f, header.parent.name);
                Assert.AreEqual(0f, style.borderBottomWidth, 0.01f, header.parent.name + ": a border would take a point from the row");
                var title = header.Children().OfType<Label>().First(label => label.ClassListContains("title"));
                Assert.AreEqual(TitleGold, title.resolvedStyle.color, header.parent.name);
                Assert.AreEqual(FontStyle.Normal, title.resolvedStyle.unityFontStyleAndWeight, header.parent.name + " has no faux bold");
            }
        }

        [UnityTest]
        public IEnumerator HeadersOnParchmentStayInInk()
        {
            using var client = Mount();
            yield return Settle();
            var board = client.Board;
            foreach (var header in new[] { board.Log.Popup.Q(className: "window-header"), board.Cards.Root.Q(className: "card-header") })
            {
                Assert.AreEqual("title-rule", header.resolvedStyle.backgroundImage.texture?.name, header.parent.name);
                Assert.Less(header.resolvedStyle.unityBackgroundImageTintColor.r, 1f, header.parent.name + ": the gilt line is tinted dark for parchment");
                var title = header.Q<Label>(className: "title");
                Assert.AreEqual(Ink, title.resolvedStyle.color, header.parent.name);
                Assert.AreEqual(FontStyle.Normal, title.resolvedStyle.unityFontStyleAndWeight, header.parent.name);
            }
            Assert.AreEqual("parchment", board.Log.Popup.resolvedStyle.backgroundImage.texture?.name, "The parchment face stays.");
            Assert.AreEqual("parchment", board.Cards.Root.resolvedStyle.backgroundImage.texture?.name, "The parchment face stays.");
        }

        [UnityTest]
        public IEnumerator AQuietTitleTakesNoMoreRoomThanTheBoldOneItReplaced()
        {
            using var client = Mount();
            yield return Settle();
            var board = client.Board;
            // Each title and the letter-spacing it had under the band.
            var titles = new List<(Label Title, float Spacing)>
            {
                (Title(board.TableEffects.Root), 1.5f), (Title(board.Reinforcements.Root), 1.5f), (Title(board.Fellowship.Root), 1f),
                (Title(board.DecisionWindow.Root), 0f), (Title(board.Battles.Root), 0.6f), (Title(board.BattleReport.Root), 0.6f),
                (Title(board.Hunt.Root), 0.6f), (Title(board.Log.Popup), 0.8f), (Title(board.Victory.Root), 0f),
                (board.Characters.Inspector.Root.Q(className: "character-header").Q<Label>(className: "title"), 0.4f),
                (board.Cards.Root.Q(className: "card-header").Q<Label>(className: "title"), 0f),
            };
            foreach (var (title, spacing) in titles)
            {
                var quiet = Width(title);
                Assert.Greater(quiet, 0f, title.parent.name);
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.letterSpacing = spacing;
                yield return Settle();
                var bold = Width(title);
                title.style.unityFontStyleAndWeight = StyleKeyword.Null;
                title.style.letterSpacing = StyleKeyword.Null;
                yield return Settle();
                Assert.LessOrEqual(quiet, bold + 0.5f, title.parent.parent.name + " · the quiet title must fit where the bold one did");
            }
        }

        [UnityTest]
        public IEnumerator ScrollBarsUnderTheBoardAreTheMenusThinOnes()
        {
            using var client = Mount();
            yield return Settle();
            var board = client.Board;
            foreach (var view in board.Root.Query<ScrollView>().ToList())
            {
                var bar = view.verticalScroller.resolvedStyle;
                Assert.AreEqual(10f, bar.minWidth.value, 0.01f, view.name);
                Assert.AreEqual(0f, bar.backgroundColor.a, 0.01f, view.name + " has no plate");
            }
            Assert.Greater(board.Root.Query<ScrollView>().ToList().Count, 3, "The action log, the action card, the chronicle and others.");
            var log = board.Log.List.Root;
            log.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            Reveal(log);
            yield return Settle();
            var scroller = log.verticalScroller.worldBound;
            Assert.AreEqual(10f, scroller.width, 0.5f, "The bar is 10 points wide.");
            var thumb = log.verticalScroller.slider.Q(className: "unity-base-slider__dragger").worldBound;
            Assert.LessOrEqual(thumb.width, 6.5f, "A slim thumb");
            Assert.GreaterOrEqual(thumb.xMin + 0.5f, scroller.xMin, "The thumb stays inside its bar.");
            Assert.LessOrEqual(thumb.xMax - 0.5f, scroller.xMax, "The thumb stays inside its bar.");
            Assert.LessOrEqual(log.verticalScroller.lowButton.worldBound.width, 10.5f, "Its arrows too.");
            Assert.LessOrEqual(log.verticalScroller.highButton.worldBound.width, 10.5f, "Its arrows too.");
            Assert.LessOrEqual(log.verticalScroller.slider.worldBound.width, 10.5f, "Its track too.");
        }

        [UnityTest]
        public IEnumerator FlatSurfacesTakeTheCardsColours()
        {
            using var client = Mount();
            yield return Settle();
            var tooltip = client.Interface.Content.parent.Q<Label>("runtime-tooltip");
            Assert.Greater(tooltip.resolvedStyle.backgroundColor.a, 0.9f, "Readable over the bright map");
            Assert.AreEqual(Hairline, tooltip.resolvedStyle.borderTopColor);
            var legend = client.Board.Map.Legend.Popup.resolvedStyle;
            Assert.AreEqual(0f, legend.backgroundColor.a, 0.01f, "The legend's colour left its code (UI-474)");
        }

        private static MountedMatchClient Mount()
        {
            var fixture = new BoardFixture();
            var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            client.SetOnline();
            return client;
        }

        private static IEnumerable<VisualElement> Frames(BoardScreen board) => new[]
        {
            board.Nations.Drawer, board.Characters.Root, board.TableEffects.Root, board.Reinforcements.Root, board.Fellowship.Root,
            board.Edges.Corner, board.Root.Q("edge-cards"), board.OwnDice, board.DecisionWindow.Root, board.Battles.Root,
            board.BattleReport.Root, board.Hunt.Root, board.Characters.Inspector.Root, board.Sovereigns.Inspector,
            board.GameEnd.Root.Q(className: "game-end-window"), board.Hover.Panel.Root, board.Information.Root, board.Log.Root,
            board.Notifications.Overflow, board.Victory.Root, board.Map.Legend.Popup
        };

        private static IEnumerable<VisualElement> Headers(BoardScreen board) => new[]
        {
            board.TableEffects.Root.Q(className: "panel-header"), board.Reinforcements.Root.Q(className: "panel-header"),
            board.Fellowship.Root.Q(className: "panel-header"), board.DecisionWindow.Root.Q(className: "window-header"),
            board.Battles.Root.Q(className: "window-header"), board.BattleReport.Root.Q(className: "window-header"),
            board.Hunt.Root.Q(className: "window-header"), board.Log.Root.Q(className: "window-header"),
            board.Victory.Root.Q(className: "window-header"), board.Characters.Inspector.Root.Q(className: "character-header"),
            board.Sovereigns.Inspector.Q(className: "character-header")
        };

        // A hidden element resolves no layout, so a header's border reads 0 whatever the sheet says.
        private static void Reveal(VisualElement element)
        {
            for (var shown = element; shown != null; shown = shown.parent)
                shown.style.display = DisplayStyle.Flex;
        }

        private static Label Title(VisualElement window) => window.Q(className: "window-header").Q<Label>(className: "title");

        private static float Width(Label title) => title.MeasureTextSize(LongTitle, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;

        private static IEnumerator Settle()
        {
            yield return null;
            yield return null;
            yield return null;
        }
    }
}
