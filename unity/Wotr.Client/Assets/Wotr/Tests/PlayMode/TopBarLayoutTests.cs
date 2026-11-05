using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed class TopBarLayoutTests
    {
        // At a fractional panel scale an element's edges round to whole pixels: a 1-point hairline shows as one pixel or two.
        private const float PixelRounding = 1.2f;

        // The Free Peoples view faces 10 Shadow dice and 2 special dice: the width budget of TopBarView,
        // which since UI-98 also holds the Fellowship and Hunt plaques (EdgePlaqueTests).
        [UnityTest]
        public IEnumerator IllustratedTopBarFitsBothSidesWithFullDicePoolsInFourLayouts()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            var file = Path.Combine(Path.GetDirectoryName(config), "ui09-resources.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file));
            using var client = new MountedMatchClient("http://localhost", "topbar-layout");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    try
                    {
                        client.Panel.targetTexture = target;
                        client.Interface.Preferences.SetScale(scale);
                        foreach (var key in new[] { "unrolled-FreePeoples", "unrolled-Shadow" })
                        {
                            client.Apply(views[key]);
                            for (var frame = 0; frame < 6; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"ui86-topbar-{key}-{width}-{scale:0.00}.png");
                            var label = $"{key} {width} {scale:0.00}";
                            CheckBar(client.Board, label);
                            CheckDice(client.Board, label);
                            CheckCounters(client.Board, label);
                            CheckArtwork(client.Board, label);
                        }
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        client.Interface.Preferences.SetScale(1);
                        target.Release();
                        Object.Destroy(target);
                    }
                }
        }

        private static void CheckBar(BoardScreen board, string label)
        {
            var bar = board.TopBar;
            var top = bar.Root;
            Assert.That(top.resolvedStyle.height, Is.EqualTo(36).Within(.5f), "UI-479 slims the bar from 44 points. " + label);
            Assert.AreEqual(top.layout.width < TopBarView.CompactBelow, bar.Compact, label);
            var flowing = top.Children().Where(child => child != bar.Frame && Visible(child)).ToList();
            foreach (var child in flowing)
                Inside(child, top.worldBound, label);
            for (var first = 0; first < flowing.Count; first++)
                for (var second = first + 1; second < flowing.Count; second++)
                    Assert.IsFalse(Overlap(flowing[first].worldBound, flowing[second].worldBound),
                        $"{flowing[first].name} overlaps {flowing[second].name} · {label}");
            var phase = board.Phase;
            var needed = phase.MeasureTextSize(phase.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            Assert.GreaterOrEqual(phase.contentRect.width + 1, needed, "Phase text is not elided. " + label);
            // UI-98: the slim bar lies in the edge-pan band, so it is an edge control that pans only on the rim.
            Assert.IsTrue(top.ClassListContains("map-edge-control") && !top.ClassListContains("map-edge-surface"), label);
            Assert.That(bar.Frame.worldBound.height, Is.EqualTo(top.worldBound.height).Within(.5f), label);
            Assert.That(bar.Frame.worldBound.width, Is.EqualTo(top.worldBound.width).Within(.5f), label);
            var stage = board.Root.Q("board-stage");
            Assert.AreEqual(0, stage.IndexOf(bar.Shadow), "The shadow lies beneath every stage panel.");
            Assert.That(bar.Shadow.worldBound.yMin, Is.EqualTo(stage.worldBound.yMin).Within(.5f), label);
            Assert.That(bar.Shadow.resolvedStyle.height, Is.EqualTo(4).Within(.5f), label);
            var cap = new Vector2(top.worldBound.xMin + 8, top.worldBound.yMin + 16);
            Assert.AreNotSame(bar.Frame, board.Root.panel.Pick(cap), "Ornaments never take the pointer.");
            var shade = new Vector2(stage.worldBound.center.x, stage.worldBound.yMin + 3);
            Assert.AreNotSame(bar.Shadow, board.Root.panel.Pick(shade), label);
        }

        private static void CheckDice(BoardScreen board, string label)
        {
            var opponent = Opponent(board);
            var visible = board.View.Dice.Count(d => d.Owner == opponent &&
                d.Location is DieLocation.Available or DieLocation.Unrolled or DieLocation.Resolving or DieLocation.AwaitingChoice);
            var dice = board.OpponentDice.Query<Button>().ToList();
            Assert.AreEqual(visible, dice.Count, label);
            foreach (var die in dice)
            {
                Inside(die, board.TopBar.Root.worldBound, label);
                Assert.IsNotNull(die.resolvedStyle.backgroundImage.texture, die.name + " artwork · " + label);
            }
            // UI-479: a die's crown, star and emblem stand out of the die, but not out of the bar.
            foreach (var mark in new[] { "ruler-crown", "die-remove-star", "die-origin-emblem" })
                foreach (var part in board.OpponentDice.Query(className: mark).ToList())
                    Inside(part, board.TopBar.Root.worldBound, mark + " · " + label);
            ExpansionMatrixLayoutTests.ResourcesFit(board);
            var owner = board.OpponentDice.Q<Label>(className: "opponent-side");
            Assert.AreEqual(BoardScreen.SideName(opponent).ToUpperInvariant(), owner.text);
            Assert.AreEqual(!board.TopBar.Compact, Visible(owner), label);
        }

        private static void CheckCounters(BoardScreen board, string label)
        {
            var summary = board.TopBar.Opponent.Root;
            var counts = SideResourceCounts.From(board.View, Opponent(board));
            var expected = new Dictionary<string, int>
            {
                ["cards"] = counts.Cards, ["rings"] = counts.Rings, ["spent"] = counts.Spent, ["out"] = counts.Out
            };
            // UI-98: the Hunt Box has its own plaque, and every counter is its icon and number.
            Assert.IsNull(summary.Q("opponent-hunt"), label);
            foreach (var entry in expected)
            {
                var chip = summary.Q("opponent-" + entry.Key);
                Inside(chip, summary.worldBound, label);
                Inside(chip, board.TopBar.Root.worldBound, label);
                // UI-479: a counter stands 24 points high, with an 18-point icon and a 15-point value.
                Assert.That(chip.resolvedStyle.height, Is.EqualTo(24).Within(PixelRounding), entry.Key + " · " + label);
                Assert.That(chip.Q(className: "summary-icon").resolvedStyle.width, Is.EqualTo(18).Within(PixelRounding), entry.Key + " · " + label);
                Assert.AreEqual(15f, chip.Q<Label>(className: "summary-value").resolvedStyle.fontSize, .01f, entry.Key + " · " + label);
                Assert.AreEqual(entry.Value.ToString(), chip.Q<Label>(className: "summary-value").text, entry.Key + " · " + label);
                Assert.IsFalse(Visible(chip.Q<Label>(className: "summary-caption")), entry.Key + " · " + label);
                var picked = board.Root.panel.Pick(chip.worldBound.center);
                Assert.IsTrue(picked == chip || chip.Contains(picked), entry.Key + " tooltip target covered by " + picked?.name);
                // UI-361: the card counter's tooltip opens with the hand's size, then what else is public about the cards.
                if (entry.Key == "cards")
                    StringAssert.Contains(" · " + entry.Value + (entry.Value == 1 ? " card" : " cards") + " in hand", chip.tooltip);
                else
                    StringAssert.EndsWith(": " + entry.Value, chip.tooltip);
            }
        }

        private static void CheckArtwork(BoardScreen board, string label)
        {
            var bar = board.TopBar;
            var pictures = new List<VisualElement> { bar.Shadow, board.Menu.Toggle };
            pictures.AddRange(bar.Opponent.Root.Query(className: "summary-icon").ToList());
            foreach (var element in pictures)
                Assert.IsNotNull(element.resolvedStyle.backgroundImage.texture, (element.name ?? element.GetClasses().FirstOrDefault()) + " · " + label);
            CheckFlatChrome(bar, label);
            CheckMenu(board.Menu.Toggle, bar, label);
            foreach (var crest in bar.Crests.Query(className: "expansion-crest").ToList().Where(Visible))
            {
                Assert.That(crest.resolvedStyle.height, Is.EqualTo(16).Within(PixelRounding), "UI-479 · " + label);
                Inside(crest, bar.Root.worldBound, label);
            }
        }

        // UI-479: no plate, frame, cartouche or ornament: the bar, its rule and its hairlines are USS.
        private static void CheckFlatChrome(TopBarView bar, string label)
        {
            var hairlines = bar.Opponent.Root.Query(className: "summary-divider").ToList().Where(Visible).ToList();
            hairlines.Add(bar.Root.Q("topbar-divider"));
            foreach (var flat in hairlines.Concat(new[] { bar.Root, bar.Frame, bar.Opponent.Root }))
                Assert.IsNull(flat.resolvedStyle.backgroundImage.texture, (flat.name ?? flat.GetClasses().FirstOrDefault()) + " · " + label);
            Assert.GreaterOrEqual(bar.Root.resolvedStyle.backgroundColor.a, .92f, "Readable over the bright map · " + label);
            // UI-480 (owner): the gilt rule runs round the whole bar.
            Assert.That(bar.Frame.resolvedStyle.borderBottomWidth, Is.EqualTo(1).Within(.5f), "The rule along the foot · " + label);
            Assert.That(bar.Frame.resolvedStyle.borderTopWidth, Is.EqualTo(1).Within(.5f), "The rule along the top · " + label);
            Assert.That(bar.Frame.resolvedStyle.borderLeftWidth, Is.EqualTo(1).Within(.5f), "The rule at the left · " + label);
            Assert.That(bar.Frame.resolvedStyle.borderRightWidth, Is.EqualTo(1).Within(.5f), "The rule at the right · " + label);
            Assert.Greater(bar.Frame.resolvedStyle.borderBottomColor.a, .5f, label);
            Assert.AreEqual(bar.Frame.resolvedStyle.borderBottomColor, bar.Frame.resolvedStyle.borderTopColor, label);
            foreach (var hairline in hairlines)
            {
                Assert.That(hairline.resolvedStyle.width, Is.EqualTo(1).Within(PixelRounding), label);
                Assert.That(hairline.resolvedStyle.height, Is.EqualTo(18).Within(PixelRounding), label);
                Assert.Greater(hairline.resolvedStyle.backgroundColor.a, 0f, label);
            }
        }

        // UI-479: a 28-point thin button by its picture, on a 32-point click target.
        private static void CheckMenu(Button menu, TopBarView bar, string label)
        {
            Assert.That(menu.resolvedStyle.height, Is.EqualTo(32).Within(.5f), "The click target · " + label);
            Assert.AreEqual(28f, menu.resolvedStyle.backgroundSize.y.value, .01f, "The picture · " + label);
            Assert.AreEqual("button", menu.resolvedStyle.backgroundImage.texture.name, label);
            Inside(menu, bar.Root.worldBound, label);
        }

        // UI-479: every state of the slim bar stays inside it, and what a click can reach stays at 32 points or more.
        [UnityTest]
        public IEnumerator EveryTargetAndStateStaysInsideTheSlimBar()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            var file = Path.Combine(Path.GetDirectoryName(config), "ui09-resources.json");
            var view = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file))["dismay-FreePeoples"];
            view.Dice.Add(new DieView { Id = "tile", Owner = Side.Shadow, Source = "base.character.witch-king", Face = DieFace.Character, Location = DieLocation.Available });
            using var client = new MountedMatchClient("http://localhost", "topbar-targets");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    try
                    {
                        client.Panel.targetTexture = target;
                        client.Interface.Preferences.SetScale(scale);
                        client.Apply(view);
                        yield return Frames();
                        var label = $"{width} {scale:0.00}";
                        CheckTile(client.Board, label);
                        yield return CheckOfferedCounter(client.Board, label);
                        yield return CheckDieRims(client.Board, label);
                        yield return CheckLongStatus(client.Board, label);
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        client.Interface.Preferences.SetScale(1);
                        target.Release();
                        Object.Destroy(target);
                    }
                }
        }

        private static void CheckTile(BoardScreen board, string label)
        {
            var tile = board.OpponentDice.Q<Button>("tile");
            Assert.IsTrue(tile.ClassListContains("special-die"), label);
            AtLeastATarget(tile, label);
            Inside(tile, board.TopBar.Root.worldBound, label);
        }

        // UI-432: the counter a click can reach stands 32 points high; nothing around it moves.
        private static IEnumerator CheckOfferedCounter(BoardScreen board, string label)
        {
            var summary = board.TopBar.Opponent;
            var chips = summary.Root.Query(className: "summary-chip").ToList().Where(Visible).ToList();
            var before = chips.Select(chip => chip.worldBound).ToList();
            summary.OfferCards("Click to discard a random card");
            yield return Frames();
            AtLeastATarget(summary.Cards, label);
            Assert.GreaterOrEqual(summary.Cards.worldBound.width + .5f, 32, label);
            Inside(summary.Cards, board.TopBar.Root.worldBound, label);
            Assert.That(board.TopBar.Root.resolvedStyle.height, Is.EqualTo(36).Within(.5f), label);
            for (var index = 0; index < chips.Count; index++)
            {
                Assert.That(chips[index].worldBound.xMin, Is.EqualTo(before[index].xMin).Within(PixelRounding), chips[index].name + " · " + label);
                Assert.That(chips[index].worldBound.center.y, Is.EqualTo(before[index].center.y).Within(PixelRounding), chips[index].name + " · " + label);
            }
            var picked = board.Root.panel.Pick(summary.Cards.worldBound.center);
            Assert.IsTrue(picked == summary.Cards || summary.Cards.Contains(picked), "Covered by " + picked?.name + " · " + label);
            summary.OfferCards("");
            yield return Frames();
            Assert.That(summary.Cards.resolvedStyle.height, Is.EqualTo(24).Within(PixelRounding), label);
        }

        // A die's rims stay in its own box: its size and place do not change, and the rim shows.
        private static IEnumerator CheckDieRims(BoardScreen board, string label)
        {
            var die = board.OpponentDice.Query<Button>(className: "die").First();
            var bounds = die.worldBound;
            foreach (var state in new[] { "selected", "legal-target", "resource-candidate" })
            {
                die.AddToClassList(state);
                yield return Frames();
                Assert.Greater(die.resolvedStyle.borderTopColor.a, .5f, state + " · " + label);
                Assert.That(die.resolvedStyle.borderTopWidth, Is.EqualTo(2).Within(.5f), state + " · " + label);
                Assert.That(die.worldBound.width, Is.EqualTo(bounds.width).Within(.5f), state + " · " + label);
                Assert.That(die.worldBound.height, Is.EqualTo(bounds.height).Within(.5f), state + " · " + label);
                die.RemoveFromClassList(state);
            }
            yield return Frames();
        }

        // The longest status takes two lines, which fit the bar and leave its neighbours alone.
        private static IEnumerator CheckLongStatus(BoardScreen board, string label)
        {
            board.Connection.text = "Session expired · Sign in again";
            yield return Frames();
            Inside(board.Connection, board.TopBar.Root.worldBound, label);
            Assert.IsFalse(Overlap(board.Connection.worldBound, board.Menu.Toggle.worldBound), label);
            Assert.IsFalse(Overlap(board.Connection.worldBound, board.TopBar.Opponent.Root.worldBound), label);
            board.Connection.text = "";
            yield return Frames();
        }

        private static void AtLeastATarget(VisualElement element, string label) =>
            Assert.GreaterOrEqual(element.worldBound.height + .5f, 32, element.name + " click target · " + label);

        private static IEnumerator Frames()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }

        private static Side Opponent(BoardScreen board) => board.View.You == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;

        private static void Inside(VisualElement element, Rect bounds, string label)
        {
            var rect = element.worldBound;
            Assert.GreaterOrEqual(rect.xMin + 1, bounds.xMin, element.name + " · " + label);
            Assert.LessOrEqual(rect.xMax, bounds.xMax + 1, element.name + " · " + label);
            Assert.GreaterOrEqual(rect.yMin + 1, bounds.yMin, element.name + " · " + label);
            Assert.LessOrEqual(rect.yMax, bounds.yMax + 1, element.name + " · " + label);
        }

        // Scaled layouts can place touching edges a fraction of a point apart.
        private static bool Overlap(Rect first, Rect second) =>
            first.xMax - .5f > second.xMin && second.xMax - .5f > first.xMin && first.yMax - .5f > second.yMin && second.yMax - .5f > first.yMin;

        private static bool Visible(VisualElement element) => element.resolvedStyle.display != DisplayStyle.None &&
            element.worldBound.width > 0 && element.worldBound.height > 0;
    }
}
