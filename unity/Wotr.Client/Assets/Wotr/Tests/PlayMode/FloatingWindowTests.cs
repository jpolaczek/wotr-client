using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class FloatingWindowTests
    {
        [UnityTest]
        public IEnumerator PersistentWindowsMoveClampAndKeepTheirPositionAcrossProjectionsAndCameraChanges()
        {
            using var client = new MountedMatchClient("http://localhost", "windows");
            var view = new BoardFixture().View;
            view.Hand.Add("base.fp.strategy.01");
            client.Apply(view);
            var board = client.Board;
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Panel.scale = scale;
                    client.Interface.Windows.Clear();
                    yield return Settle();
                    var card = view.Hand[0];
                    board.Cards.Show(card, true);
                    yield return Settle();
                    var initial = board.Cards.Root.layout.position;
                    yield return Drag(board.Cards.Root, new Vector2(-140, 65));
                    var moved = board.Cards.Root.layout.position;
                    Assert.That(Vector2.Distance(initial, moved), Is.GreaterThan(40));
                    board.Map.Camera.ZoomAt(board.Map.contentRect.center, 1.25f);
                    board.Map.Camera.Pan += new Vector2(20, 10);
                    board.Map.Refresh();
                    board.Apply(view);
                    yield return Settle();
                    Near(moved, board.Cards.Root.layout.position);
                    board.Cards.Close();
                    board.Cards.Show(card, true);
                    yield return Settle();
                    Near(moved, board.Cards.Root.layout.position);
                    yield return Drag(board.Cards.Root, new Vector2(3000, 3000));
                    Inside(board.Cards.Root, board.Map);
                    yield return Drag(board.Cards.Root, new Vector2(-3000, -3000));
                    Inside(board.Cards.Root, board.Map);
                    PointerGestureEvents.ClickAt(board.Cards.Root.Q<Button>("close-card"));
                    Assert.AreEqual(DisplayStyle.None, board.Cards.Root.style.display.value);
                    Assert.AreEqual(view.Revision, board.View.Revision);
                    board.Information.Rules();
                    yield return Settle();
                    yield return Drag(board.Information.Root, new Vector2(100, 40));
                    Inside(board.Information.Root, board.Map);
                    board.Information.Close();
                    var fellowship = board.Fellowship.Root.worldBound;
                    var hunt = board.Hunt.Root.worldBound;
                    Assert.IsNull(board.Fellowship.Root.Q(className: FloatingWindow.HandleClass));
                    Assert.IsNotNull(board.Hunt.Root.Q(className: FloatingWindow.HandleClass), "UI-162: the Hunt window moves like the battle window.");
                    board.Map.Camera.Pan += new Vector2(-100, -40);
                    board.Map.Refresh();
                    board.Apply(view);
                    yield return Settle();
                    Assert.AreEqual(fellowship, board.Fellowship.Root.worldBound);
                    Assert.AreEqual(hunt, board.Hunt.Root.worldBound);
                    board.Resources.Show(DieLocation.Spent);
                    yield return Settle();
                    yield return Drag(board.Resources.Root, new Vector2(150, 45));
                    var resources = board.Resources.Root.layout.position;
                    board.Resources.Apply(view); // Rebuilds the content, not the window controller.
                    yield return Settle();
                    Near(resources, board.Resources.Root.layout.position);
                    board.Resources.Close();
                    board.Nations.Open();
                    yield return Settle();
                    Assert.IsNull(board.Nations.Drawer.Q(className: FloatingWindow.HandleClass));
                    var political = board.Nations.Drawer.layout.position;
                    PointerGestureEvents.ClickAt(board.Nations.ShowOpponent);
                    yield return Settle();
                    Assert.IsFalse(board.Nations.ShowOpponent.value);
                    Near(political, board.Nations.Drawer.layout.position);
                    board.Nations.ShowOpponent.value = true;
                    board.Nations.Close();
                    board.HandDrawer.Open();
                    yield return Settle();
                    Assert.IsNull(board.Hand.Q(className: FloatingWindow.HandleClass));
                    Inside(board.Hand, board.Map);
                    LayoutTests.Save(texture, $"ui32-windows-{width}-{scale:0.00}.png");
                    board.HandDrawer.Close();
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        [UnityTest]
        public IEnumerator HeaderCaptureCancelsSafelyAndResetAndNewBoardUseTheSessionLayout()
        {
            using var client = new MountedMatchClient("http://localhost", "window-session");
            var view = new BoardFixture().View;
            view.Hand.Add("base.fp.strategy.01");
            client.Apply(view);
            var board = client.Board;
            board.Cards.Show(view.Hand[0], true);
            yield return Settle();
            var initial = board.Cards.Root.layout.position;
            yield return Drag(board.Cards.Root, new Vector2(-120, 50));
            var saved = board.Cards.Root.layout.position;
            var header = board.Cards.Root.Q(className: FloatingWindow.HandleClass);
            PointerGestureEvents.Press(header.Q<Label>());
            yield return null;
            Assert.IsTrue(board.Cards.Root.ClassListContains("window-dragging"));
            using (var escape = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                board.Cards.Root.SendEvent(escape);
            yield return Settle();
            Assert.IsFalse(board.Cards.Root.ClassListContains("window-dragging"));
            Assert.IsFalse(board.Cards.Root.HasPointerCapture(PointerId.mousePointerId));
            Near(saved, board.Cards.Root.layout.position);
            PointerGestureEvents.Press(header.Q<Label>());
            yield return null;
            board.Map.Focus();
            yield return Settle();
            Assert.IsFalse(board.Cards.Root.HasPointerCapture(PointerId.mousePointerId));
            client.Interface.Content.Clear();
            var next = new BoardScreen(client.Interface.Windows);
            client.Interface.Content.Add(next.Root);
            next.Apply(view);
            next.Cards.Show(view.Hand[0], true);
            yield return Settle();
            Near(saved, next.Cards.Root.layout.position);
            client.Interface.Open();
            yield return Settle();
            PointerGestureEvents.ClickAt(client.Interface.Settings.Root.Q<Button>("reset-window-layout"));
            client.Interface.Settings.Close();
            yield return Settle();
            Near(initial, next.Cards.Root.layout.position);
            Assert.IsFalse(new WindowLayout().TryGet("card-inspector", out _));
        }

        [UnityTest]
        public IEnumerator BattleHeaderAndReportMoveWhileControlsKeepTheirCommands()
        {
            using var client = new MountedMatchClient("http://localhost", "battle-windows");
            var view = BattleScreenFixture.Create(CommandKind.TakeBattleLosses, true);
            view.LastBattleRound = view.Battle;
            client.Apply(view);
            client.Board.Battles.Open();
            yield return Settle();
            yield return Drag(client.Board.Battles.Root, new Vector2(-80, 35));
            var position = client.Board.Battles.Root.layout.position;
            client.Board.Map.Camera.Pan += Vector2.one * 80;
            client.Board.Map.Refresh();
            yield return Settle();
            Near(position, client.Board.Battles.Root.layout.position);
            Assert.IsNotNull(client.Board.Battles.Root.Q<Button>("confirm-decision"));
            PointerGestureEvents.ClickAt(client.Board.Battles.Root.Q<Button>("close-battle"));
            Assert.IsFalse(client.Board.Battles.IsOpen);
            CardNetworkTests.Click(client.Board.BattleReport.Open);
            yield return Settle();
            yield return Drag(client.Board.BattleReport.Root, new Vector2(-50, 35));
            Inside(client.Board.BattleReport.Root, client.Board.Map);
        }

        [UnityTest]
        public IEnumerator TransientPreviewStaysAnchoredAndDraggingPinsItWhileSovereignContentMayRefresh()
        {
            using var client = new MountedMatchClient("http://localhost", "transient-windows");
            var view = new BoardFixture().View;
            view.Hand.Add("base.fp.strategy.01");
            view.Sovereigns.Add(new SovereignView { Id = "denethor", Title = "Denethor", Nation = "gondor",
                RegionId = "minas-tirith", Resistance = 3, Status = SovereignStatus.Initial });
            client.Apply(view);
            var board = client.Board;
            board.Cards.Show(view.Hand[0]);
            yield return Settle();
            LayoutTests.Save(client.Viewport, "ui61-card-transient-anchored.png");
            Assert.AreEqual(view.Hand[0], board.Cards.CardId);
            Assert.AreEqual(DisplayStyle.Flex, board.Cards.Root.resolvedStyle.display);
            var anchored = board.Cards.Root.layout.position;
            Assert.IsFalse(board.Cards.Pinned);
            yield return Drag(board.Cards.Root, new Vector2(-140, 45));
            Assert.IsTrue(board.Cards.Pinned);
            board.Cards.Close();
            board.Cards.Show(view.Hand[0]);
            yield return Settle();
            LayoutTests.Save(client.Viewport, "ui61-card-transient-reopened.png");
            Assert.AreEqual(view.Hand[0], board.Cards.CardId);
            Assert.AreEqual(DisplayStyle.Flex, board.Cards.Root.resolvedStyle.display);
            Assert.IsFalse(board.Cards.Pinned);
            Near(anchored, board.Cards.Root.layout.position);
            board.Cards.Close();
            board.Sovereigns.Show("denethor");
            yield return Settle();
            yield return Drag(board.Sovereigns.Inspector, new Vector2(180, 40));
            var moved = board.Sovereigns.Inspector.layout.position;
            board.Sovereigns.Apply(view);
            yield return Settle();
            Near(moved, board.Sovereigns.Inspector.layout.position);
            Assert.IsTrue(board.Sovereigns.Pinned);
        }

        private static IEnumerator Drag(VisualElement window, Vector2 delta)
        {
            var header = window.Q(className: FloatingWindow.HandleClass);
            var handle = header.Q<Label>();
            var point = handle.worldBound.center;
            var picked = window.panel.Pick(point);
            Assert.IsTrue(picked == handle || handle.Contains(picked), $"Window handle is covered by {picked?.name}.");
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point, clickCount = 1 }))
                picked.SendEvent(down);
            yield return null;
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = point + delta }))
                window.SendEvent(move);
            yield return null;
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point + delta, clickCount = 1 }))
                window.SendEvent(up);
            yield return Settle();
            Assert.IsFalse(window.ClassListContains("window-dragging"));
            Assert.IsFalse(window.HasPointerCapture(PointerId.mousePointerId));
        }

        private static void Near(Vector2 expected, Vector2 actual) => Assert.That(Vector2.Distance(expected, actual), Is.LessThan(1));
        private static void Inside(VisualElement window, VisualElement area)
        {
            Assert.That(window.worldBound.xMin, Is.GreaterThanOrEqualTo(area.worldBound.xMin - 1));
            Assert.That(window.worldBound.yMin, Is.GreaterThanOrEqualTo(area.worldBound.yMin - 1));
            Assert.That(window.worldBound.xMax, Is.LessThanOrEqualTo(area.worldBound.xMax + 1));
            Assert.That(window.worldBound.yMax, Is.LessThanOrEqualTo(area.worldBound.yMax + 1));
        }
        private static IEnumerator Settle() { for (var frame = 0; frame < 12; frame++) yield return null; }
    }
}
