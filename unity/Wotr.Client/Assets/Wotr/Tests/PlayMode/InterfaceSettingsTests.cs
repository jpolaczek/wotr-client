using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public class InterfaceSettingsTests
    {
        [UnityTest]
        public IEnumerator ChangingSizeAndMotionPreservesTheMoveAndEscapeOnlyClosesSettings()
        {
            using (var client = new MountedMatchClient("http://localhost", "settings"))
            {
                client.Apply(new BoardFixture().View);
                yield return Settle();
                ArmyGestures.Die(client, "army");
                yield return MatchTestRoom.Wait(ArmyGestures.All(client, "minas-tirith"));
                yield return Settle();
                var targets = client.Board.Map.DraftOverlay.Targets.ToArray();
                CollectionAssert.Contains(targets, "osgiliath");
                var pan = client.Board.Map.Camera.Pan;
                var zoom = client.Board.Map.Camera.Zoom;
                var open = client.Board.Menu.Toggle;
                open.Focus();
                PointerGestureEvents.ClickAt(open);
                yield return Settle();
                PointerGestureEvents.ClickAt(client.Board.Menu.Panel.Q<Button>("open-interface"));
                yield return Settle();
                Assert.IsFalse(client.Interface.Content.enabledInHierarchy);
                var settings = client.Interface.Settings.Root;
                CardNetworkTests.Click(settings.Q<Button>("scale-125"));
                yield return Settle();
                Assert.AreEqual(1.25f, client.Panel.scale);
                StringAssert.Contains("✓", settings.Q<Button>("scale-125").text);
                CardNetworkTests.Click(settings.Q<Toggle>("reduced-motion"));
                yield return Settle();
                Assert.IsTrue(client.Interface.Preferences.ReducedMotion);
                Assert.IsTrue(settings.Query<Button>().ToList().All(b => b.resolvedStyle.transitionDuration.All(t => t.value == 0)));
                // MENU-17: the game starts in Fullscreen, where no resolution can be picked.
                StringAssert.EndsWith("✓", settings.Q<Button>("window-mode-fullscreen").text);
                Assert.IsTrue(settings.Q("resolutions").Query<Button>().ToList().All(b => !b.enabledInHierarchy));
                // MENU-12: a window mode applies on its click; the editor keeps its game view, the choice is saved.
                CardNetworkTests.Click(settings.Q<Button>("window-mode-windowed"));
                yield return Settle();
                Assert.AreEqual(Wotr.Client.WindowMode.Windowed, client.Interface.Preferences.WindowMode);
                StringAssert.EndsWith("✓", settings.Q<Button>("window-mode-windowed").text);
                Assert.AreEqual(7, settings.Q("resolutions").childCount);
                Assert.IsTrue(settings.Q<Button>("resolution-1280x720").enabledInHierarchy, "A window picks its size.");
                using (var escape = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                    settings.Q<Button>("close-interface").SendEvent(escape);
                yield return Settle();
                Assert.IsFalse(client.Interface.Settings.IsOpen);
                Assert.IsTrue(client.Interface.Content.enabledInHierarchy);
                CollectionAssert.AreEquivalent(targets, client.Board.Map.DraftOverlay.Targets);
                var expected = new Wotr.Client.CameraState { Pan = pan, Zoom = zoom };
                expected.Constrain(client.Board.Map.CanvasBounds, client.Board.Map.contentRect);
                Assert.AreEqual(expected.Pan, client.Board.Map.Camera.Pan, "Resizing clamps only as needed to keep the raster covering the viewport.");
                Assert.AreEqual(zoom, client.Board.Map.Camera.Zoom);
                Assert.AreSame(open, open.panel.focusController.focusedElement);
                Assert.IsNull(client.Board.Tray.Q<Button>("confirm-decision"), "Offline settings never submit or enable a command.");
            }
        }

        [UnityTest]
        public IEnumerator SettingsFocusAndDenseHeaderFitAtBothSizesAndScales()
        {
            using (var client = new MountedMatchClient("http://localhost", "settings-layout"))
            {
                client.Apply(BattleScreenFixture.Create(Wotr.Contracts.CommandKind.TakeBattleLosses, true));
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        texture.Create();
                        client.Panel.targetTexture = texture;
                        client.Interface.Preferences.SetScale(scale);
                        yield return Settle();
                        var top = client.Board.Root.Q<VisualElement>(className: "topbar");
                        foreach (var child in top.Children())
                            Fits(child, top);
                        client.Interface.Open();
                        yield return Settle();
                        var settings = client.Interface.Settings.Root;
                        Assert.IsEmpty(settings.Query<ScrollView>().ToList());
                        Assert.IsEmpty(settings.Query<TextField>().ToList());
                        var focus = settings.Q<Button>("scale-125");
                        var before = focus.worldBound;
                        focus.Focus();
                        yield return Settle();
                        Assert.AreEqual(before, focus.worldBound, "Focus cannot move the layout.");
                        // UI-191: focus draws no outline of its own.
                        Assert.IsNull(settings.panel.visualTree.Q("keyboard-focus"));
                        foreach (var button in settings.Query<Button>().ToList())
                        {
                            Fits(button, settings);
                            Assert.GreaterOrEqual(button.resolvedStyle.height, 32);
                        }
                        foreach (var label in settings.Query<Label>().ToList())
                            Fits(label, settings);
                        LayoutTests.Save(texture, $"ui13-settings-{width}-{scale:0.00}.png");
                        client.Interface.Settings.Close();
                        client.Panel.targetTexture = client.Viewport;
                        texture.Release();
                        Object.Destroy(texture);
                    }
            }
        }

        private static void Fits(VisualElement element, VisualElement parent)
        {
            Assert.GreaterOrEqual(element.worldBound.xMin + 1, parent.worldBound.xMin, element.name);
            Assert.LessOrEqual(element.worldBound.xMax, parent.worldBound.xMax + 1, element.name);
            Assert.GreaterOrEqual(element.worldBound.yMin + 1, parent.worldBound.yMin, element.name);
            Assert.LessOrEqual(element.worldBound.yMax, parent.worldBound.yMax + 1, element.name);
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }
    }
}
