using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class CharacterRosterLayoutTests
    {
        [UnityTest]
        public IEnumerator FullOwnRostersAndPublicInspectorsFitFourLayoutsWithoutDecisionScroll() => Wait(Layout());

        private static async Task Layout()
        {
            using var room = await Connect("ui66-full", ScenarioKind.Interaction, openPanels: false);
            foreach (var client in new[] { room.Fp, room.Sh })
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Interface.Preferences.SetScale(scale);
                        client.Interface.Windows.Clear();
                        var board = client.Board;
                        board.Characters.Close();
                        board.Characters.Open();
                        await room.Settle();
                        var expected = board.View.OwnCharacterRoster.Count;
                        Assert.AreEqual(expected, board.Characters.Root.Query<Button>().ToList()
                            .Count(b => b.name.StartsWith("character-roster-")));
                        var id = board.View.You == Side.FreePeoples ? "base.character.gandalf-grey" : "base.character.saruman";
                        board.Characters.Show(id, true);
                        await Task.Delay(160);
                        await room.Settle();
                        LayoutTests.Save(target, $"ui66-roster-{board.View.You}-{width}-{scale:0.00}.png");
                        Check(board.Characters.Root, board.Characters.Root.parent.worldBound);
                        Check(board.Characters.Inspector.Root, board.Characters.Inspector.Root.parent.worldBound);
                        Assert.IsFalse(board.Characters.Root.worldBound.Overlaps(board.Characters.Inspector.Root.worldBound),
                            "Automatic inspection must leave the complete roster readable: " +
                            board.Characters.Root.worldBound + " / " + board.Characters.Inspector.Root.worldBound);
                        foreach (var control in board.Characters.FixedOcclusions())
                            Assert.IsFalse(board.Characters.Inspector.Root.worldBound.Overlaps(control),
                                "Automatic inspection must leave fixed board controls visible: " +
                                board.Characters.Inspector.Root.worldBound + " / " + control);
                        foreach (var root in new[] { board.Characters.Root, board.Characters.Inspector.Root })
                        {
                            Assert.IsEmpty(root.Query<ScrollView>().ToList());
                            foreach (var button in root.Query<Button>().ToList().Where(Visible))
                            {
                                Check(button, root.worldBound);
                                var picked = root.panel.Pick(button.worldBound.center);
                                Assert.IsTrue(picked == button || button.Contains(picked), button.name + " covered by " + picked?.name);
                                CheckButtonEdges(button);
                                var title = button.Q<Label>("character-roster-title");
                                if (title != null)
                                    Check(title, button.worldBound);
                            }
                        }
                        var tab = board.Characters.Tab;
                        var tabPick = tab.panel.Pick(tab.worldBound.center);
                        Assert.IsTrue(tabPick == tab || tab.Contains(tabPick));
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
            room.Fp.Interface.Preferences.SetScale(1f);
            room.Sh.Interface.Preferences.SetScale(1f);
        }

        private static void Check(VisualElement element, Rect bounds)
        {
            Assert.GreaterOrEqual(element.worldBound.xMin, bounds.xMin - 1, element.name);
            Assert.GreaterOrEqual(element.worldBound.yMin, bounds.yMin - 1, element.name);
            Assert.LessOrEqual(element.worldBound.xMax, bounds.xMax + 1, element.name);
            Assert.LessOrEqual(element.worldBound.yMax, bounds.yMax + 1, element.name);
        }

        private static void CheckButtonEdges(Button button)
        {
            var bounds = button.worldBound;
            // Rounded corners lie outside the button's hit shape. Test every edge midpoint.
            foreach (var point in new[] { new Vector2(bounds.xMin + 2, bounds.center.y),
                new Vector2(bounds.xMax - 2, bounds.center.y), new Vector2(bounds.center.x, bounds.yMin + 2),
                new Vector2(bounds.center.x, bounds.yMax - 2) })
            {
                var picked = button.panel.Pick(point);
                Assert.IsTrue(picked == button || button.Contains(picked),
                    button.name + " edge covered by " + picked?.name);
            }
        }

        private static bool Visible(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return element.enabledInHierarchy && element.worldBound.width > 0;
        }
    }
}
