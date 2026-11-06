using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-507: the sign-in screen can close the game. The button is the third in the row under the card's hairline, so the card
    // keeps its height, and a click closes the game at once.
    public class GatewayQuitTests
    {
        private static IEnumerator Settle()
        {
            for (var i = 0; i < 8; i++)
                yield return null;
        }

        private static void Inside(VisualElement element, VisualElement bounds)
        {
            Assert.Greater(element.worldBound.width, 0, element.name);
            Assert.GreaterOrEqual(element.worldBound.xMin, bounds.worldBound.xMin - 1, element.name);
            Assert.LessOrEqual(element.worldBound.xMax, bounds.worldBound.xMax + 1, element.name);
            Assert.GreaterOrEqual(element.worldBound.yMin, bounds.worldBound.yMin - 1, element.name);
            Assert.LessOrEqual(element.worldBound.yMax, bounds.worldBound.yMax + 1, element.name);
        }

        private static void TextFits(Button button)
        {
            var size = button.MeasureTextSize(button.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(size.x, button.contentRect.width + 1, button.name + " holds its words.");
        }

        [UnityTest]
        public IEnumerator TheButtonStandsInTheRowInEveryLayoutAndTheCardKeepsItsHeight()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var content = client.Interface.Content;
                foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    content.Clear();
                    var gateway = new GatewayScreen();
                    content.Add(gateway.Root);
                    yield return Settle();
                    var card = gateway.Root.Q(className: "login-card");
                    var row = gateway.Root.Q("auth-secondary");
                    var settings = gateway.Root.Q<Button>("open-interface");
                    var replays = gateway.Root.Q<Button>("auth-replays");
                    var quit = gateway.Root.Q<Button>("auth-quit");
                    foreach (var button in new[] { settings, replays, quit })
                    {
                        Inside(button, card);
                        TextFits(button);
                        Assert.AreEqual(settings.worldBound.y, button.worldBound.y, 1f, "One row.");
                    }
                    Assert.AreEqual(settings.worldBound.width, quit.worldBound.width, 1f, "Three equal buttons.");
                    Assert.AreEqual(settings.worldBound.height, quit.worldBound.height, 1f, "Three equal buttons.");
                    Assert.GreaterOrEqual(replays.worldBound.xMin, settings.worldBound.xMax, "A gap, not an overlap.");
                    Assert.GreaterOrEqual(quit.worldBound.xMin, replays.worldBound.xMax, "A gap, not an overlap.");
                    Inside(row, card);
                    LayoutTests.Save(target, $"ui507-entry-{width}-{scale:0.00}.png");
                    target.Release();
                }
            }
        }

        [UnityTest]
        public IEnumerator AClickClosesTheGameOnce()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                yield return Settle();
                var quit = client.Root.Q<Button>("auth-quit");
                Assert.IsNotNull(quit, "The flow opens on the sign-in screen.");
                Assert.AreEqual(0, client.Quits);
                ReturnTestFixture.Click(quit);
                Assert.AreEqual(1, client.Quits);
            }
        }
    }
}
