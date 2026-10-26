using System.Collections;
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
    public sealed class EdgeBoardLayoutTests
    {
        [UnityTest]
        public IEnumerator EdgePanelsStayOrderedAndLeftTabsAreExclusiveInFourLayouts() =>
            MatchTestRoom.Wait(Layouts());

        // UI-190 (user decision): a left panel opens level with its own tab and moves up only as far as it must to stay
        // whole above the bottom of the map.
        private static void BesideItsTab(VisualElement tab, VisualElement panel)
        {
            var map = panel.parent.worldBound;
            var label = panel.name + " at " + map.size;
            Assert.LessOrEqual(panel.worldBound.yMax, map.yMax - 7, "Whole above the bottom of the map · " + label);
            if (Mathf.Abs(panel.worldBound.yMin - tab.worldBound.yMin) > 1)
                Assert.That(panel.worldBound.yMax, Is.EqualTo(map.yMax - 8).Within(1), "Moved up only as far as it must · " + label);
        }

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
                            board.HandDrawer.Open();
                            await room.Settle();
                            CheckRight(board);
                            using var mouse = new EngineMouseInput(board.Root);
                            var tabs = new[] { board.Nations.Root, board.TableEffects.Tab, board.Characters.Tab };
                            var panels = new[] { board.Nations.Drawer, board.TableEffects.Root, board.Characters.Root };
                            for (var i = 0; i < tabs.Length; i++)
                            {
                                mouse.Click(tabs[i]);
                                // UI-98: a click unrolls the panel like hovering, so its 140 ms unroll runs first.
                                await Task.Delay(220);
                                for (var j = 0; j < panels.Length; j++)
                                    Assert.AreEqual(i == j, Visible(panels[j]), "Left panel " + panels[j].name);
                                Assert.That(panels[i].worldBound.xMin, Is.EqualTo(board.Edges.Left.worldBound.xMax).Within(1));
                                BesideItsTab(tabs[i], panels[i]);
                                CheckRight(board);
                            }
                            mouse.Click(board.Nations.Root);
                            await room.Settle();
                            board.Characters.Tab.Focus();
                            using (var submit = NavigationSubmitEvent.GetPooled())
                                board.Characters.Tab.SendEvent(submit);
                            await room.Settle();
                            Assert.IsTrue(Visible(board.Characters.Root));
                            Assert.IsFalse(Visible(board.Nations.Drawer));
                            Assert.IsFalse(Visible(board.TableEffects.Root));
                            mouse.Click(board.Nations.Root);
                            await room.Settle();
                            LayoutTests.Save(texture, $"ui85-board-{board.View.You}-{width}-{scale:0.00}.png");
                            board.Menu.Open();
                            await room.Settle();
                            mouse.Click(board.Menu.Root.Q<Button>("board-menu-victory"));
                            await room.Settle();
                            Assert.IsFalse(board.Menu.IsOpen);
                            Assert.IsTrue(board.Victory.IsOpen);
                            Assert.IsFalse(Visible(board.Victory.Tab), "Victory belongs to Menu.");
                            board.Victory.Close();
                            board.Fellowship.Close();
                            board.Hunt.Close();
                            board.Hover.Open("rivendell");
                            await room.Settle();
                            CheckRight(board);
                            board.Hover.Close();
                            Assert.AreEqual(snapshot, JsonConvert.SerializeObject(board.View));
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
            Assert.AreEqual(0, room.Boundary.Commands, "Board furniture cannot send game commands.");
        }

        // UI-98: the Fellowship is a plaque in the corner tab whose full panel unrolls over the hand; the right
        // column holds only the cards.
        private static void CheckRight(BoardScreen board)
        {
            var right = board.Edges.Right;
            var stage = board.Root.Q("board-stage");
            Assert.AreSame(board.Edges.Corner, board.Fellowship.Status.parent);
            // UI-160/UI-162: the Hunt plaque lives in the turn console and opens the Hunt window on the stage.
            Assert.AreSame(board.Console.Hunt.Well, board.Hunt.Tab.parent);
            Assert.AreSame(board.Edges.CornerPanels, board.Fellowship.Root.parent);
            Assert.AreSame(stage, board.Hunt.Root.parent);
            Assert.IsFalse(board.Fellowship.IsOpen || board.Hunt.IsOpen, "No decision holds the plaques' panels open.");
            Assert.That(right.worldBound.xMax, Is.EqualTo(stage.worldBound.xMax).Within(1));
            var cards = right.Q("edge-cards");
            Assert.That(cards.worldBound.yMax, Is.LessThanOrEqualTo(board.Root.worldBound.yMax + 1), Geometry(board));
            Assert.That(cards.worldBound.yMax, Is.EqualTo(board.Root.worldBound.yMax).Within(1), "Cards must meet the screen's lower edge.");
            Assert.That(cards.worldBound.width, Is.EqualTo(right.worldBound.width).Within(1), cards.name);
            foreach (var button in board.Hand.Query<Button>().ToList().Where(Visible))
            {
                Assert.That(button.worldBound.yMax, Is.LessThanOrEqualTo(cards.worldBound.yMax + 1), button.name);
                var picked = board.Root.panel.Pick(button.worldBound.center);
                Assert.IsTrue(picked == button || button.Contains(picked), "Card obscured: " + button.name + " by " + picked?.name);
            }
            LayoutTests.AssertNoDecisionScroll(board);
        }

        private static string Geometry(BoardScreen board) => string.Join("; ", new[]
        {
            board.Root.Q("board-stage"), board.Fellowship.Root, board.Hunt.Root, board.Hand.parent
        }.Select(e => e.name + "=" + e.worldBound));

        private static bool Visible(VisualElement element) => !Appearance.Closed(element) && element.resolvedStyle.display != DisplayStyle.None &&
            element.worldBound.width > 0 && element.worldBound.height > 0;
    }
}
