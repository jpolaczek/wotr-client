using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.PlayTests
{
    internal static class SovereignPanelLayoutChecks
    {
        public static async Task Layout(MatchTestRoom room, MountedMatchClient client, string name)
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    client.Interface.Windows.Clear();
                    await room.Settle();
                    LayoutTests.Save(target, $"ui59-{name}-{width}-{scale:0.00}.png");
                    Check(client);
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
            client.Interface.Preferences.SetScale(1f);
            await room.Settle();
        }

        // UI-274: a king's decision is made on his slot in the Characters panel: every slot, its attempt button and its
        // tiles stand inside the panel at four layouts, and no decision window opens.
        public static async Task Slots(MatchTestRoom room, MountedMatchClient client, string name)
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    client.Board.Characters.Open();
                    await room.Settle();
                    LayoutTests.Save(target, $"ui274-{name}-{width}-{scale:0.00}.png");
                    CheckSlots(client);
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
            client.Interface.Preferences.SetScale(1f);
            await room.Settle();
        }

        private static void CheckSlots(MountedMatchClient client)
        {
            var board = client.Board;
            var panel = board.Characters.Root;
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display);
            Assert.IsFalse(board.DecisionWindow.IsOpen, "A king's decision opens no window.");
            foreach (var slot in board.Sovereigns.Root.Query(className: "sovereign-slot").ToList())
                foreach (var element in new[] { slot }.Concat(slot.Query<Button>().ToList()).Where(Visible))
                {
                    Assert.GreaterOrEqual(element.worldBound.xMin, panel.worldBound.xMin - 1, element.name);
                    Assert.LessOrEqual(element.worldBound.xMax, panel.worldBound.xMax + 1, element.name);
                    Assert.LessOrEqual(element.worldBound.yMax, panel.worldBound.yMax + 1, element.name);
                }
            Assert.IsEmpty(panel.Query<ScrollView>().ToList());
        }

        private static void Check(MountedMatchClient client)
        {
            var board = client.Board;
            var sharedCharacter = !board.Sovereigns.IsOpen && board.Characters.Inspector.Root.style.display.value != DisplayStyle.None;
            var panel = sharedCharacter ? board.Characters.Inspector.Root : board.Sovereigns.Inspector;
            var viewport = board.Root.worldBound;
            Assert.IsTrue(board.Sovereigns.IsOpen || sharedCharacter);
            Assert.GreaterOrEqual(panel.worldBound.xMin, viewport.xMin - 1);
            Assert.GreaterOrEqual(panel.worldBound.yMin, viewport.yMin - 1);
            Assert.LessOrEqual(panel.worldBound.xMax, viewport.xMax + 1);
            Assert.LessOrEqual(panel.worldBound.yMax, viewport.yMax + 1);
            Assert.LessOrEqual(panel.worldBound.yMax, panel.parent.worldBound.yMax + 1, "The panel must leave the dice dock visible.");
            // UI-98: the corner tab's plaques are soft occlusions, like the hand (BoardScreen fixedControls).
            var fixedControls = new VisualElement[] { board.Map.Legend.Toggle, board.Map.Legend.PoliticalToggle, board.Nations.Root,
                board.TableEffects.Tab,
                board.HandDrawer.Tab, board.ResourceDecision.Tab, board.Victory.Tab, board.Victory.Banner };
            foreach (var control in fixedControls.Where(c => c != null && Visible(c) && (c.childCount > 0 || c is TextElement)))
                Assert.IsFalse(panel.worldBound.Overlaps(control.worldBound), "Fixed control is covered: " + control.name);
            Assert.IsEmpty(panel.Query<ScrollView>().ToList());
            Assert.IsEmpty(panel.Query<TextField>().ToList());
            foreach (var button in panel.Query<Button>().ToList().Where(b => b.enabledInHierarchy && Visible(b)))
            {
                Assert.GreaterOrEqual(button.worldBound.xMin, panel.worldBound.xMin - 1, button.name);
                Assert.GreaterOrEqual(button.worldBound.yMin, panel.worldBound.yMin - 1, button.name);
                Assert.LessOrEqual(button.worldBound.xMax, panel.worldBound.xMax + 1, button.name);
                Assert.LessOrEqual(button.worldBound.yMax, panel.worldBound.yMax + 1, button.name);
                var picked = panel.panel.Pick(button.worldBound.center);
                Assert.IsTrue(picked == button || button.Contains(picked), button.name + " covered by " + picked?.name);
            }
            foreach (var button in fixedControls.Where(c => c != null && Visible(c))
                .SelectMany(c => c is Button b ? new[] { b }.AsEnumerable() : c.Query<Button>().ToList())
                .Where(b => b.enabledInHierarchy && Visible(b)))
            {
                var picked = panel.panel.Pick(button.worldBound.center);
                Assert.IsTrue(picked == button || button.Contains(picked), button.name + " covered by " + picked?.name);
            }
            ExpansionMatrixLayoutTests.ResourcesFit(board);
        }

        private static bool Visible(VisualElement element)
        {
            if (element.worldBound.width <= 0 || element.worldBound.height <= 0)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            return true;
        }
    }
}
