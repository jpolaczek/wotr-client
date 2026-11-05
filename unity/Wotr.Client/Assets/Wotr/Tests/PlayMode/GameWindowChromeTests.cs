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

        // UI-475: a rail tab is the thin medallion on a nearly opaque plate under one thin line; a press and a held panel have
        // their own pictures, and gold stays the sign of a usable tab, clearly apart from the warm line of a held one.
        [UnityTest]
        public IEnumerator RailTabsAreThinWithAUsableRimApartFromTheSelectedLine()
        {
            using var client = Mount();
            yield return Settle();
            var board = client.Board;
            var tab = board.Characters.Tab;
            Assert.AreEqual("edge-tab", tab.resolvedStyle.backgroundImage.texture?.name);
            Assert.GreaterOrEqual(tab.resolvedStyle.backgroundColor.a, 0.9f, "A plate that reads on the bright parchment");
            Assert.AreEqual(1f, tab.resolvedStyle.borderTopWidth, 0.01f, "One thin line");
            Assert.AreEqual(2f, tab.resolvedStyle.borderTopLeftRadius, 0.01f, "The menus' corner");
            tab.AddToClassList("selected");
            yield return Settle();
            Assert.AreEqual("edge-tab-selected", tab.resolvedStyle.backgroundImage.texture?.name);
            var selected = tab.resolvedStyle.borderTopColor;
            Assert.Greater(selected.g, 0.6f, "No red lacquer: a warm line");
            Assert.AreEqual(1f, tab.resolvedStyle.borderTopWidth, 0.01f);
            tab.RemoveFromClassList("selected");
            tab.AddToClassList("pressed");
            yield return Settle();
            Assert.AreEqual("edge-tab-pressed", tab.resolvedStyle.backgroundImage.texture?.name);
            Assert.GreaterOrEqual(tab.resolvedStyle.backgroundColor.a, 0.9f, "A press keeps the plate over the map");
            tab.RemoveFromClassList("pressed");
            tab.AddToClassList("legal-target");
            yield return Settle();
            Assert.AreEqual("edge-tab-available", tab.resolvedStyle.backgroundImage.texture?.name);
            Assert.AreEqual(3f, tab.resolvedStyle.borderTopWidth, 0.01f, "A usable tab's thick rim");
            Assert.AreNotEqual(selected, tab.resolvedStyle.borderTopColor, "A usable rim is not the selected line");
        }

        // UI-476: what keeps a side's colour wears that side's row or button, blue and red apart, on the kit's slices.
        [UnityTest]
        public IEnumerator SideSurfacesKeepTheirColourOnTheSideRows()
        {
            using var client = Mount();
            yield return Settle();
            var board = client.Board;
            var plaque = board.Console.Status.Plaque;
            var band = board.Root.Q("turn-ribbon-band");
            foreach (var side in new[] { "free", "shadow" })
            {
                plaque.RemoveFromClassList("side-free");
                plaque.RemoveFromClassList("side-shadow");
                plaque.AddToClassList("side-" + side);
                band.RemoveFromClassList("side-free");
                band.RemoveFromClassList("side-shadow");
                band.AddToClassList("side-" + side);
                yield return Settle();
                Assert.AreEqual("button-" + side, plaque.resolvedStyle.backgroundImage.texture?.name);
                Assert.AreEqual(12f, plaque.resolvedStyle.unitySliceLeft, 0.01f, "The button's 6-point slice");
                plaque.AddToClassList("pressed");
                yield return Settle();
                Assert.AreEqual("button-" + side + "-pressed", plaque.resolvedStyle.backgroundImage.texture?.name, "A press has its own picture (UI-113)");
                plaque.RemoveFromClassList("pressed");
                Assert.AreEqual("row-" + side, band.resolvedStyle.backgroundImage.texture?.name);
                Assert.AreEqual(16f, band.resolvedStyle.unitySliceLeft, 0.01f, "The row's 8-point slice");
            }
            var well = board.Console.Hunt.Well;
            Assert.AreEqual("row", well.resolvedStyle.backgroundImage.texture?.name);
            board.Console.Hunt.SetAllocating(true);
            yield return Settle();
            Assert.AreEqual("row-selected", well.resolvedStyle.backgroundImage.texture?.name, "An eligible Hunt Box stands out");
            Assert.AreEqual(16f, well.resolvedStyle.unitySliceLeft, 0.01f);
            var track = board.Nations.Drawer.Q(className: "nation-track");
            Assert.That(track.resolvedStyle.backgroundImage.texture?.name, Does.StartWith("row-"), "A Politics row keeps its side");
            Assert.AreEqual(0f, track.resolvedStyle.backgroundColor.a, 0.01f, track.name);
        }

        // UI-477: the battle's insides stand on the same rows; a card in a slot keeps its own picture.
        [UnityTest]
        public IEnumerator TheBattlesInsidesWearTheThinRows()
        {
            var view = BattleScreenFixture.Create(CommandKind.CommitCombatCard);
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Apply(view);
            yield return Settle();
            if (!client.Board.Battles.IsOpen)
                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
            yield return Settle();
            var tray = client.Board.Tray;
            Assert.AreEqual("row-free", tray.Q("battle-attacker").resolvedStyle.backgroundImage.texture?.name);
            Assert.AreEqual("row-shadow", tray.Q("battle-defender").resolvedStyle.backgroundImage.texture?.name);
            Assert.AreEqual(16f, tray.Q("battle-attacker").resolvedStyle.unitySliceLeft, 0.01f);
            Assert.AreEqual("row", client.Board.Battles.Root.Q(className: "battle-stages").resolvedStyle.backgroundImage.texture?.name);
            var slot = tray.Q<Button>("battle-card-defender");
            Assert.AreEqual("row", slot.resolvedStyle.backgroundImage.texture?.name, "An empty card slot is a thin row");
            Assert.AreEqual(16f, slot.resolvedStyle.unitySliceLeft, 0.01f);
            var back = tray.Q<Button>("battle-card-attacker");
            Assert.AreEqual("back", back.resolvedStyle.backgroundImage.texture?.name, "A committed card keeps its back");
            Assert.AreEqual(28f, back.resolvedStyle.unitySliceLeft, 0.01f, "On its own slices");
            var policy = client.Board.Battles.Root.Q(className: "battle-window-policy").Q<Button>();
            Assert.That(policy.resolvedStyle.backgroundImage.texture?.name, Does.StartWith("button"), "The policy control is a thin button");
        }

        // UI-478: a decision the game waits on stays clearly red; the pennants keep their art.
        [UnityTest]
        public IEnumerator ARequiredDecisionStaysRedOnAThinLine()
        {
            using var client = Mount();
            yield return Settle();
            var notice = client.Board.Notice.Button;
            notice.style.display = DisplayStyle.Flex;
            notice.AddToClassList(DecisionNotice.RequiredClass);
            // Under .motion-enabled its colours ease over the 750 ms of the pulse's rhythm.
            yield return new WaitForSecondsRealtime(0.9f);
            var style = notice.resolvedStyle;
            Assert.IsNull(style.backgroundImage.texture, "A flat red field");
            Assert.Greater(style.backgroundColor.r, 2.5f * style.backgroundColor.g, "Clearly red");
            Assert.Greater(style.backgroundColor.r, 2.5f * style.backgroundColor.b, "Clearly red");
            Assert.AreEqual(1f, style.borderTopWidth, 0.01f, "One thin line");
            Assert.AreEqual(2f, style.borderTopLeftRadius, 0.01f, "The menus' corner");
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
            board.Notifications.Overflow, board.Victory.Root, board.Map.Legend.Popup, board.Console.Root,
            board.Console.Status.RaisedOptions, board.RingPicker.Root
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
