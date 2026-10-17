using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    public class LayoutTests
    {
        [UnityTest]
        public IEnumerator TwoResolutionsAndZoomRenderWithMapAndHudInBounds()
        {
            var bootstrap = Object.FindAnyObjectByType<ClientBootstrap>();
            if (bootstrap == null)
                bootstrap = new GameObject("Layout test client").AddComponent<ClientBootstrap>();
            bootstrap.ShowFixture();
            var document = bootstrap.GetComponent<UIDocument>();
            var screen = bootstrap.BoardScreen;
            foreach (var width in new[] { 1366, 1920 })
            {
                int height = width == 1366 ? 768 : 1080;
                var target = new RenderTexture(width, height, 24);
                target.Create();
                document.panelSettings.targetTexture = target;
                for (int i = 0; i < 12; i++)
                    yield return null;
                screen.Map.Fit();
                for (int i = 0; i < 6; i++)
                    yield return null;
                Assert.Greater(screen.Map.worldBound.height, screen.Tray.worldBound.height * 2);
                Assert.LessOrEqual(screen.Tray.worldBound.yMax, screen.Root.worldBound.yMax + 1);
                Assert.LessOrEqual(screen.Hand.worldBound.xMax, screen.Root.worldBound.xMax + 1);
                var markers = screen.Map.Query<Button>(className: "army").ToList();
                for (var a = 0; a < markers.Count; a++)
                    for (var b = a + 1; b < markers.Count; b++)
                        if (markers[a].resolvedStyle.display != DisplayStyle.None && markers[b].resolvedStyle.display != DisplayStyle.None)
                            Assert.IsFalse(markers[a].worldBound.Overlaps(markers[b].worldBound), markers[a].text + " / " + markers[b].text);
                Save(target, $"ui-{width}-overview.png");
                screen.Map.Focus("helms-deep");
                for (int i = 0; i < 6; i++)
                    yield return null;
                Assert.AreEqual("helms-deep", screen.Map.Selected);
                Save(target, $"ui-{width}-siege.png");
                var pan = screen.Map.Camera.Pan;
                var zoom = screen.Map.Camera.Zoom;
                screen.Map.UpdateData(screen.Presentation);
                Assert.AreEqual(pan, screen.Map.Camera.Pan);
                Assert.AreEqual(zoom, screen.Map.Camera.Zoom);
                // Fractional wheel deltas exercise the same path as a precision trackpad.
                using (var wheel = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, mousePosition = screen.Map.worldBound.center, delta = new Vector2(0, .25f) }))
                    screen.Map.SendEvent(wheel);
                Assert.Less(screen.Map.Camera.Zoom, zoom);
                var beforePan = screen.Map.Camera.Pan;
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = screen.Map.worldBound.center, button = 1 }))
                    screen.Map.SendEvent(down);
                using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, mousePosition = screen.Map.worldBound.center + new Vector2(35, 15), button = 1 }))
                    screen.Map.SendEvent(move);
                using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = screen.Map.worldBound.center + new Vector2(35, 15), button = 1 }))
                    screen.Map.SendEvent(up);
                Assert.AreNotEqual(beforePan, screen.Map.Camera.Pan);
                Assert.AreEqual("helms-deep", screen.Map.Selected, "Panning must not select/move armies.");
                document.panelSettings.targetTexture = null;
                target.Release();
                Object.Destroy(target);
            }
        }
        [UnityTest]
        public IEnumerator LoginAndAllConfigurationOptionsFitWithoutScrolling()
        {
            var bootstrap = Object.FindAnyObjectByType<ClientBootstrap>();
            if (bootstrap == null)
                bootstrap = new GameObject("Gateway test").AddComponent<ClientBootstrap>();
            bootstrap.ShowFixture();
            var document = bootstrap.GetComponent<UIDocument>();
            var target = new RenderTexture(1366, 768, 24);
            target.Create();
            document.panelSettings.targetTexture = target;
            var root = document.rootVisualElement;
            root.Clear();
            var auth = new GatewayScreen();
            root.Add(auth.Root);
            for (var frame = 0; frame < 12; frame++)
                yield return null;
            Assert.LessOrEqual(auth.Root.Q<Button>().worldBound.yMax, 768);
            Save(target, "login-1366.png");
            root.Clear();
            var room = new RoomScreen();
            room.Update(new Wotr.Contracts.RoomView
            {
                FreePeoples = "Alice",
                Shadow = "Bob",
                ExpansionsAvailable = true,
                Version = 1,
                Configuration = new Wotr.Contracts.MatchConfiguration
                {
                    Lords = true,
                    Kings = true,
                    Fate = true,
                    Council = true,
                    Dismay = true,
                    VisibleCorruption = true,
                    SequentialCorruption = true,
                    RemovingRulerTokens = true,
                    NewCities = true
                },
                StartUnavailableReason = "ContentNotImplemented"
            });
            root.Add(room.Root);
            for (var frame = 0; frame < 12; frame++)
                yield return null;
            Assert.AreEqual(9, room.Root.Query<Toggle>().ToList().Count);
            foreach (var button in room.Root.Query<Button>().ToList())
                Assert.LessOrEqual(button.worldBound.yMax, 768);
            Save(target, "room-1366.png");
            document.panelSettings.targetTexture = null;
            target.Release();
            Object.Destroy(target);
        }
        public static void AssertNoDecisionScroll(BoardScreen board)
        {
            var decisionScrolls = board.Root.Query<ScrollView>().ToList().Where(scroll =>
                !board.Log.Root.Contains(scroll) && !board.Log.Popup.Contains(scroll) &&
                !board.Notifications.Overflow.Contains(scroll) && !board.BattleReport.Root.Contains(scroll) &&
                !board.GameChat.Root.Contains(scroll));
            Assert.IsEmpty(decisionScrolls, "Only read-only history may scroll.");
            AssertDecisionWindowFits(board);
        }

        // UI-160: the open decision window sits between the top bar and the turn console and holds all of its content.
        public static void AssertDecisionWindowFits(BoardScreen board)
        {
            var window = board.DecisionWindow;
            if (!window.IsOpen)
                return;
            var bounds = window.Root.worldBound;
            Assert.GreaterOrEqual(bounds.yMin + .5f, board.TopBar.Root.worldBound.yMax, "Decision window below the top bar");
            Assert.LessOrEqual(bounds.yMax, board.Console.Root.worldBound.yMin + .5f, "Decision window above the turn console");
            Assert.LessOrEqual(window.Dock.worldBound.yMax, bounds.yMax + .5f, "The decision window holds its whole content");
        }

        public static void Save(RenderTexture target, string file)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.artifacts/screenshots"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, file), texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }
}
