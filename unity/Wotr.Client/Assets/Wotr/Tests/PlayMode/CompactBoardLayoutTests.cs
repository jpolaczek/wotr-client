using System.Collections;
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
    public sealed class CompactBoardLayoutTests
    {
        [UnityTest]
        public IEnumerator SimplePanelsUseTheirContentSizeAndKeepControlsReachableInFourLayouts() =>
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
                            board.Windows.Clear();
                            await room.Settle();
                            CheckDock(board);
                            if (board.View.You == Side.FreePeoples)
                            {
                                ArmyGestures.Die(client, ArmyGestures.ArmyDie(client));
                                await room.Settle();
                                LayoutTests.Save(texture, $"ui78-army-{board.View.You}-{width}-{scale:0.00}.png");
                                CheckDock(board);
                            }
                            var contents = new RegionViewIndex(board.View);
                            var empty = board.Presentation.Regions.Keys.First(id => !contents.Occupied(id));
                            board.Hover.Open(empty);
                            await room.Settle();
                            var small = board.Hover.Panel.Root;
                            // UI-188: an empty region opens the small window of its name and state.
                            Assert.That(small.layout.width, Is.InRange(100f, 240f), "Empty regions should not reserve a full-army width.");
                            Assert.That(small.worldBound.yMax, Is.LessThanOrEqualTo(small.parent.worldBound.yMax + 1));
                            await CheckNotices(room, board);
                            LayoutTests.Save(texture, $"ui78-compact-{board.View.You}-{width}-{scale:0.00}.png");
                            var occupied = board.View.Units.Where(u => board.Presentation.Regions.ContainsKey(u.Location))
                                .GroupBy(u => u.Location).OrderByDescending(g => g.Count()).First().Key;
                            board.Hover.Open(occupied);
                            await room.Settle();
                            Assert.That(board.Hover.Panel.Root.layout.width, Is.InRange(279f, 521f));
                            foreach (var button in board.Hover.Panel.Root.Query<Button>().ToList().Where(Visible))
                                Reachable(button);
                            LayoutTests.AssertNoDecisionScroll(board);
                            board.Hover.Close();
                            Assert.AreEqual(snapshot, JsonConvert.SerializeObject(board.View));
                        }
                        finally
                        {
                            client.Panel.targetTexture = client.Viewport;
                            texture.Release();
                            Object.Destroy(texture);
                        }
                    }
            }
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        private static void CheckDock(BoardScreen board)
        {
            var dock = board.Root.Q(className: "bottom");
            Assert.That(dock.layout.height, Is.LessThan(180),
                "Simple state must not reserve the old 190–240 px spacer. " + DockGeometry(board, dock));
            foreach (var button in dock.Query<Button>().ToList().Where(Visible))
            {
                Assert.That(button.worldBound.yMax, Is.LessThanOrEqualTo(dock.worldBound.yMax + 1), button.name);
                Reachable(button);
            }
        }

        private static string DockGeometry(BoardScreen board, VisualElement dock)
        {
            var elements = dock.Children().Concat(board.OwnDice.Children())
                .Concat(board.OwnDice.Q<VisualElement>("own-resource-columns")?.Children() ?? Enumerable.Empty<VisualElement>());
            return board.View.You + " " + board.Root.layout.size + ": " + string.Join("; ",
                elements.Where(e => e.resolvedStyle.display != DisplayStyle.None)
                    .Select(e => (e.name.Length > 0 ? e.name : string.Join(" ", e.GetClasses())) + " " + e.layout));
        }

        private static async Task CheckNotices(UI10TestRoom room, BoardScreen board)
        {
            if (board.Notice.Button.style.display.value != DisplayStyle.None)
                return;
            // UI-100: the log icon keeps the strip visible once there is history; a status only adds its row.
            var idle = board.Notifications.Root.resolvedStyle.display;
            var camera = board.Map.Camera.Pan;
            board.Notifications.SetStatus("Retry opponent updates");
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Notifications.Root.resolvedStyle.display);
            Reachable(board.Notifications.Root.Q<Button>("notification-retry"));
            CheckInspectionBounds(board);
            board.Notifications.SetStatus("");
            await room.Settle();
            Assert.AreEqual(idle, board.Notifications.Root.resolvedStyle.display);
            CheckInspectionBounds(board);
            Assert.AreEqual(camera, board.Map.Camera.Pan, "The notification row must not navigate the fullscreen map.");
        }

        private static void CheckInspectionBounds(BoardScreen board)
        {
            var inspection = board.Hover.Panel.Root;
            var viewport = inspection.parent.worldBound;
            Assert.That(inspection.worldBound.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1));
            Assert.That(inspection.worldBound.yMax, Is.LessThanOrEqualTo(viewport.yMax + 1));
            Reachable(inspection);
        }

        private static void Reachable(VisualElement element)
        {
            var hit = element.panel.Pick(element.worldBound.center);
            Assert.IsTrue(hit == element || element.Contains(hit), element.name + " covered by " + hit?.name);
        }

        private static bool Visible(VisualElement element)
        {
            if (!element.enabledInHierarchy)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            return element.worldBound.width > 0 && element.worldBound.height > 0;
        }
    }
}
