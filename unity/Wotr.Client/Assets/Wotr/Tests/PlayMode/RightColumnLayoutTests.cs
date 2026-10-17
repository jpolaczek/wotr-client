using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-90 art of the Fellowship and Hunt panels; since UI-98 they unroll from their top-bar plaques.
    public sealed class RightColumnLayoutTests
    {
        private static readonly (int Width, float Scale)[] Layouts = { (1366, 1f), (1366, 1.25f), (1920, 1f), (1920, 1.25f) };

        [UnityTest]
        public System.Collections.IEnumerator IllustratedFellowshipAndHuntWasteNoRowsInFourLayouts() =>
            MatchTestRoom.Wait(Illustrated());

        [UnityTest]
        public System.Collections.IEnumerator ActiveHuntKeepsItsBodyBelowTheIllustratedHeader() =>
            MatchTestRoom.Wait(ActiveHunt());

        // Uses only names that predate UI-90, so the same test measures the old column for comparison.
        [UnityTest]
        public System.Collections.IEnumerator ReportsPanelHeightsForComparison() => MatchTestRoom.Wait(Heights());

        private static async Task Illustrated()
        {
            using var room = await UI10TestRoom.Connect("map15-navigation");
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var revision = board.View.Revision;
                await EachLayout(room, client, async label =>
                {
                    await Unroll(board.Fellowship.Status);
                    LayoutTests.Save(client.Panel.targetTexture, $"ui98-fellowship-{label.Replace(' ', '-')}.png");
                    CheckPanel(board.Fellowship.Root, "fellowship", label);
                    CheckFellowship(board, label);
                    await OpenHunt(board);
                    LayoutTests.Save(client.Panel.targetTexture, $"ui98-hunt-{label.Replace(' ', '-')}.png");
                    CheckHuntWindow(board, label);
                    CheckIdleHunt(board, label);
                    await RollBack(board);
                });
                // The viewport was just restored; let the layout settle before hovering the corner tab.
                await room.Settle();
                await room.Settle();
                await Unroll(board.Fellowship.Status);
                Assert.IsTrue(board.Fellowship.IsOpen);
                var locate = board.Fellowship.Root.Q<Button>("focus-fellowship");
                PointerGestureEvents.ClickAt(locate);
                await room.Settle();
                Assert.AreEqual(revision, board.View.Revision, "Locating never sends a game command.");
            }
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        private static async Task ActiveHunt()
        {
            using var room = await UI10TestRoom.Connect("ui34-hunt-allocation", shadowBoundary: true);
            var board = room.Sh.Board;
            // UI-162: the allocation happens in the turn console; no Hunt window or corner panel opens for it.
            await EachLayout(room, room.Sh, label =>
            {
                Assert.IsFalse(board.Hunt.IsOpen, "The allocation opens no Hunt window · " + label);
                Assert.IsTrue(board.Console.Hunt.Root.ClassListContains("hunt-allocating"), label);
                StringAssert.StartsWith("Allocate 0/", board.Hunt.Caption, label);
                var well = board.Console.Hunt.Well.worldBound;
                var plaque = board.Hunt.Tab.worldBound;
                Assert.IsTrue(plaque.xMin + 1 >= well.xMin && plaque.xMax <= well.xMax + 1 && plaque.yMin + 1 >= well.yMin &&
                    plaque.yMax <= well.yMax + 1, "The plaque stays in its well · " + label);
                Assert.IsFalse(Visible(board.Edges.CornerPanels), "The corner keeps only the Fellowship · " + label);
                return Task.CompletedTask;
            });
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        private static async Task Heights()
        {
            using var room = await UI10TestRoom.Connect("map15-navigation");
            var report = new Dictionary<string, float>();
            foreach (var client in new[] { room.Fp, room.Sh })
                await EachLayout(room, client, async label =>
                {
                    await Unroll(client.Board.Fellowship.Status);
                    report["fellowship " + label] = client.Board.Fellowship.Root.worldBound.height;
                    await OpenHunt(client.Board);
                    report["hunt " + label] = client.Board.Hunt.Root.worldBound.height;
                    await RollBack(client.Board);
                });
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.artifacts/screenshots"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "ui90-right-heights.json"), JsonConvert.SerializeObject(report, Formatting.Indented));
        }

        // Hovering a plaque unrolls its panel; the map rolls it back (UI-98).
        private static async Task Unroll(VisualElement plaque)
        {
            CardGestures.Move(plaque);
            await Task.Delay(300);
        }

        // UI-160: the console's Hunt plaque opens the Hunt panel on a click. UI-209: opened from its plaque the panel
        // shows the pool list at once; the header icon folds it, leaving the compact panel at rest.
        private static async Task OpenHunt(BoardScreen board)
        {
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            await Task.Delay(120);
            Assert.AreEqual(DisplayStyle.Flex, board.Hunt.Pool.Root.style.display.value, "The plaque unfolds the pool list.");
            PointerGestureEvents.ClickAt(board.Hunt.PoolToggle);
            await Task.Delay(120);
        }

        private static async Task RollBack(BoardScreen board)
        {
            board.Hunt.Close();
            CardGestures.Move(board.Map);
            await Task.Delay(420);
        }

        private static async Task EachLayout(UI10TestRoom room, MountedMatchClient client, System.Func<string, Task> check)
        {
            foreach (var (width, scale) in Layouts)
            {
                var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                texture.Create();
                try
                {
                    client.Panel.targetTexture = texture;
                    client.Interface.Preferences.SetScale(scale);
                    await room.Settle();
                    await room.Settle();
                    await check($"{client.Board.View.You} {width} {scale:0.00}");
                }
                finally
                {
                    client.Panel.targetTexture = client.Viewport;
                    client.Interface.Preferences.SetScale(1);
                    texture.Release();
                    Object.Destroy(texture);
                }
            }
        }

        private static void CheckPanel(VisualElement panel, string key, string label)
        {
            Assert.AreEqual("panel", Texture(panel), key + " frame · " + label);
            var header = panel.Q(className: "panel-header");
            Assert.AreEqual("panel-header", Texture(header), key + " header · " + label);
            Assert.LessOrEqual(header.worldBound.height, 28.5f, key + " header stays thinner than the left panels · " + label);
            var icon = header.Q("panel-header-icon-" + key);
            Assert.AreEqual(0, header.IndexOf(icon));
            Assert.AreEqual(key, Texture(icon), label);
            Assert.AreEqual(PickingMode.Ignore, icon.pickingMode);
            Inside(icon.worldBound, header.worldBound, key + " icon · " + label);
            foreach (var button in header.Query<Button>().ToList().Where(Visible))
            {
                Assert.That(Texture(button), Does.StartWith("button-square"), button.name + " · " + label);
                Assert.GreaterOrEqual(button.worldBound.height + 1, 24, button.name + " keeps the 24-point click target · " + label);
                Inside(button.worldBound, header.worldBound, button.name + " · " + label);
            }
            var title = header.Q<Label>(className: "title");
            Assert.AreEqual(title.text.ToUpperInvariant(), title.text, "Titles use capitals as in the left rail.");
            Assert.LessOrEqual(Width(title), title.contentRect.width + 1, key + " title fits · " + label);
        }

        private static void CheckFellowship(BoardScreen board, string label)
        {
            var panel = board.Fellowship.Root;
            var locate = panel.Q<Button>("focus-fellowship");
            var header = panel.Q(className: "panel-header");
            Assert.AreSame(header, locate.parent, "The state row is the header's locate button · " + label);
            Assert.AreSame(locate, panel.Q(className: "fellowship-state-row").parent);
            var condition = panel.Q<Label>("fellowship-condition");
            Assert.LessOrEqual(Width(condition), condition.contentRect.width + 1, "Hidden/Revealed stays readable · " + label);
            Assert.Greater(panel.Q<Label>("fellowship-location").contentRect.width, 24, "The position keeps room in the header · " + label);
            Assert.AreEqual(PickingMode.Ignore, condition.pickingMode);
            var tokens = panel.Query<Button>(className: "companion-token").ToList().Where(Visible).ToList();
            Assert.IsTrue(tokens.All(token => Mathf.Abs(token.worldBound.yMin - tokens[0].worldBound.yMin) < 1),
                "Portraits stay on one row · " + label);
            var guide = panel.Q(className: "fellowship-guide-row");
            var badge = panel.Query<Label>("guide-badge").ToList().FirstOrDefault(Visible);
            Assert.AreEqual(badge == null, Visible(guide), "The Guide row appears only without a Guide portrait · " + label);
        }

        // UI-98: an idle Hunt lists the Hunt Box under its header; the plaque already carries the count.
        // UI-162: the Hunt is a window in the battle window's frame and header, not a corner panel.
        private static void CheckHuntWindow(BoardScreen board, string label)
        {
            var window = board.Hunt.Root;
            Assert.AreEqual("panel", Texture(window), "hunt frame · " + label);
            var header = window.Q(className: "window-header");
            Assert.AreEqual("panel-header", Texture(header), "hunt header · " + label);
            Assert.LessOrEqual(header.worldBound.height, 28.5f, "hunt header · " + label);
            foreach (var button in header.Query<Button>().ToList().Where(Visible))
            {
                Assert.GreaterOrEqual(button.worldBound.height + 1, 24, button.name + " keeps the 24-point click target · " + label);
                Inside(button.worldBound, header.worldBound, button.name + " · " + label);
            }
            var title = header.Q<Label>(className: "title");
            Assert.AreEqual(title.text.ToUpperInvariant(), title.text, "Titles use capitals.");
            Assert.LessOrEqual(Width(title), title.contentRect.width + 1, "hunt title fits · " + label);
            Assert.LessOrEqual(window.worldBound.yMax, board.Console.Root.worldBound.yMin + 1, "The window stays above the console · " + label);
        }

        private static void CheckIdleHunt(BoardScreen board, string label)
        {
            var panel = board.Hunt.Root;
            if (!panel.ClassListContains("hunt-idle"))
                return;
            var header = panel.Q(className: "window-header");
            Assert.IsFalse(Visible(header.Q<Label>("hunt-idle-count")), label);
            Assert.IsFalse(Visible(panel.Q("hunt-main")), "No empty body under the header · " + label);
            var box = panel.Q("hunt-box");
            Assert.IsTrue(Visible(box), "The Hunt Box is listed · " + label);
            foreach (var entry in box.Query<Label>().ToList().Where(Visible))
                Assert.LessOrEqual(Width(entry), entry.contentRect.width + 1, entry.name + " · " + label);
            Assert.AreEqual("Eyes · " + HuntBoxView.Eyes(board.View), box.Q<Label>("hunt-box-eyes").text, label);
            var free = HuntBoxView.FreeDice(board.View);
            Assert.AreEqual(free > 0, Visible(box.Q<Label>("hunt-box-free")), "Free Peoples dice show only when present · " + label);
            Inside(box.worldBound, panel.worldBound, label);
            // UI-109: no last report and no empty space — the header and one row of the Hunt Box.
            Assert.IsFalse(panel.Query(className: "hunt-report-strip").ToList().Any(Visible), "No last report at rest · " + label);
            Assert.LessOrEqual(panel.layout.height, 84, "A compact panel at rest · " + label);
        }

        private static float Width(TextElement text) =>
            text.MeasureTextSize(text.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;

        private static string Texture(VisualElement element) => element.resolvedStyle.backgroundImage.texture?.name;

        private static void Inside(Rect inner, Rect outer, string label)
        {
            Assert.GreaterOrEqual(inner.xMin + 1, outer.xMin, label);
            Assert.LessOrEqual(inner.xMax, outer.xMax + 1, label);
            Assert.GreaterOrEqual(inner.yMin + 1, outer.yMin, label);
            Assert.LessOrEqual(inner.yMax, outer.yMax + 1, label);
        }

        private static bool Visible(VisualElement element)
        {
            if (element == null || element.panel == null || element.worldBound.width <= 0 || element.worldBound.height <= 0)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return true;
        }
    }
}
