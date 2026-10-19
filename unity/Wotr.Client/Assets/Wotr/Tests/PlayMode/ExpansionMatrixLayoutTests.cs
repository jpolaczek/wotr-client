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
    public class ExpansionMatrixLayoutTests
    {
        [UnityTest]
        public IEnumerator FullSpecialDiceAndTokenPoolsRemainInsideTheirPanels()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            var file = Path.Combine(Path.GetDirectoryName(config), "ui09-resources.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file));
            Assert.AreEqual(4, views.Count);
            Assert.AreEqual(5, views.First().Value.Sovereigns.Count);
            Assert.AreEqual(5, views.First().Value.RulerTokens.Count);
            Assert.IsTrue(views.First().Value.Dice.Any(d => d.Source == "lords.die.narya" && d.Location == DieLocation.Unrolled));
            using (var client = new MountedMatchClient("http://localhost", "expansion-resources"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        foreach (var entry in views)
                        {
                            client.Apply(entry.Value);
                            for (var frame = 0; frame < 6; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"ui09-resources-{entry.Key}-{width}-{scale:0.00}.png");
                            ResourcesFit(client.Board);
                            OwnResourcesReachable(client.Board);
                            var first = client.Board.OwnDice.Q<Button>();
                            client.Apply(entry.Value);
                            Assert.AreSame(first, client.Board.OwnDice.Q<Button>(), "Unchanged resources must retain their controls.");
                            client.Board.Root.AddToClassList("sovereign-context");
                            for (var frame = 0; frame < 6; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"ui59-resources-{entry.Key}-{width}-{scale:0.00}.png");
                            ResourcesFit(client.Board);
                            OwnResourcesReachable(client.Board);
                            client.Board.Root.RemoveFromClassList("sovereign-context");
                        }
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
        }

        [UnityTest]
        public IEnumerator BothPerspectivesOfAllConfigurationsFitWithoutScrolling()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            Assert.IsNotEmpty(config);
            var file = Path.Combine(Path.GetDirectoryName(config), "ui09-matrix.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file));
            Assert.AreEqual(186, views.Count);
            using (var client = new MountedMatchClient("http://localhost", "expansion-matrix"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        foreach (var entry in views)
                        {
                            client.Apply(entry.Value);
                            for (var frame = 0; frame < 4; frame++)
                                yield return null;
                            try
                            {
                                FellowshipLayoutTests.Check(client.Board);
                                ResourcesFit(client.Board);
                                ProjectedSovereigns(client.Board);
                            }
                            catch
                            {
                                LayoutTests.Save(target, $"ui09-matrix-{entry.Key}-{width}-{scale:0.00}.png");
                                throw;
                            }
                        }
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
        }

        internal static void ResourcesFit(BoardScreen board)
        {
            foreach (var pool in new[] { board.OwnDice, board.OpponentDice })
            {
                var buttons = pool.Query<Button>().ToList().Where(b => b.worldBound.width > 0 && b.resolvedStyle.display != DisplayStyle.None).ToList();
                foreach (var button in buttons)
                {
                    if (button.ClassListContains("die") && (pool == board.OwnDice || button.ClassListContains("special-die")))
                    {
                        MinimumTargetSize(button, button.resolvedStyle.width, button.resolvedStyle.minWidth.value, "width");
                        MinimumTargetSize(button, button.resolvedStyle.height, button.resolvedStyle.minHeight.value, "height");
                    }
                    Assert.GreaterOrEqual(button.worldBound.xMin, pool.worldBound.xMin, button.name);
                    Assert.LessOrEqual(button.worldBound.xMax, pool.worldBound.xMax + 1, button.name);
                    Assert.LessOrEqual(button.worldBound.yMax, pool.worldBound.yMax + 1, button.name);
                    foreach (var badge in button.Query<Label>().ToList())
                        Assert.LessOrEqual(badge.worldBound.yMax, button.worldBound.yMax + 1, badge.text);
                }
                for (var first = 0; first < buttons.Count; first++)
                    for (var second = first + 1; second < buttons.Count; second++)
                        Assert.IsFalse(buttons[first].worldBound.Overlaps(buttons[second].worldBound),
                            buttons[first].name + " overlaps " + buttons[second].name);
            }
        }

        private static void OwnResourcesReachable(BoardScreen board)
        {
            foreach (var button in board.OwnDice.Query<Button>().ToList())
            {
                if (!Visible(button))
                    continue;
                var picked = button.panel.Pick(button.worldBound.center);
                Assert.IsTrue(picked == button || button.Contains(picked),
                    button.name + " covered by " + picked?.name);
            }
        }

        private static bool Visible(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return element.worldBound.width > 0 && element.worldBound.height > 0;
        }

        private static void MinimumTargetSize(Button button, float actual, float minimum, string dimension)
        {
            if (actual >= 32)
                return;
            var scale = button.scaledPixelsPerPoint;
            var message = $"{button.name} target {dimension}: rendered={actual}, authored minimum={minimum}, pixels/point={scale}";
            Assert.GreaterOrEqual(minimum, 32, message);
            Assert.Greater(scale, 0, message);
            // Rounding the two edges can render a 32-point minimum as floor(32 * scale) pixels.
            // Keep both the authored minimum and its physical-pixel lower bound mandatory.
            Assert.GreaterOrEqual(actual * scale + .01f, Mathf.Floor(32 * scale), message);
        }

        private static void ProjectedSovereigns(BoardScreen board)
        {
            var portraits = board.Sovereigns.Root.Query<Button>(className: "sovereign-portrait").ToList();
            Assert.AreEqual(board.View.Sovereigns.Count, portraits.Count);
            if (board.View.Configuration.Fate)
                Assert.IsFalse(portraits.Any(b => b.name.EndsWith("dain") || b.name.EndsWith("brand")));
        }
    }
}
