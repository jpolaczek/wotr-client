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
            Assert.That(top.resolvedStyle.height, Is.EqualTo(44).Within(.5f), "UI-98 slims the bar from 56 points. " + label);
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
            // UI-98: the 44-point bar lies in the edge-pan band, so it is an edge control that pans only on the rim.
            Assert.IsTrue(top.ClassListContains("map-edge-control") && !top.ClassListContains("map-edge-surface"), label);
            Assert.That(bar.Frame.worldBound.height, Is.EqualTo(top.worldBound.height).Within(.5f), label);
            Assert.That(bar.Frame.worldBound.width, Is.EqualTo(top.worldBound.width).Within(.5f), label);
            var stage = board.Root.Q("board-stage");
            Assert.AreEqual(0, stage.IndexOf(bar.Shadow), "The shadow lies beneath every stage panel.");
            Assert.That(bar.Shadow.worldBound.yMin, Is.EqualTo(stage.worldBound.yMin).Within(.5f), label);
            Assert.That(bar.Shadow.resolvedStyle.height, Is.EqualTo(8).Within(.5f), label);
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
                Assert.AreEqual(entry.Value.ToString(), chip.Q<Label>(className: "summary-value").text, entry.Key + " · " + label);
                Assert.IsFalse(Visible(chip.Q<Label>(className: "summary-caption")), entry.Key + " · " + label);
                var picked = board.Root.panel.Pick(chip.worldBound.center);
                Assert.IsTrue(picked == chip || chip.Contains(picked), entry.Key + " tooltip target covered by " + picked?.name);
                StringAssert.EndsWith(": " + entry.Value, chip.tooltip);
            }
        }

        private static void CheckArtwork(BoardScreen board, string label)
        {
            var bar = board.TopBar;
            var decorated = new List<VisualElement> { bar.Root, bar.Frame, bar.Shadow, bar.Opponent.Root, board.Menu.Toggle, bar.Root.Q("topbar-divider") };
            decorated.AddRange(bar.Opponent.Root.Query(className: "summary-icon").ToList());
            decorated.AddRange(bar.Opponent.Root.Query(className: "summary-divider").ToList());
            foreach (var element in decorated)
                Assert.IsNotNull(element.resolvedStyle.backgroundImage.texture, (element.name ?? element.GetClasses().FirstOrDefault()) + " · " + label);
            Assert.That(board.Menu.Toggle.resolvedStyle.height, Is.EqualTo(32).Within(.5f), label);
            Inside(board.Menu.Toggle, bar.Root.worldBound, label);
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
