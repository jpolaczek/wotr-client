using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public class BoardMenuLayoutTests
    {
        [UnityTest]
        public IEnumerator MenuFitsFourLayoutsAndOutsideReleaseCannotReachTheMap()
        {
            using var client = new MountedMatchClient("http://localhost", "menu-layout");
            client.Apply(new BoardFixture().View);
            var selections = 0;
            client.Board.Map.RegionSelected += _ => selections++;
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Interface.Preferences.SetScale(scale);
                    yield return Settle();
                    PointerGestureEvents.ClickAt(client.Board.Menu.Toggle);
                    yield return Settle();
                    var menu = client.Board.Menu;
                    Assert.IsTrue(menu.IsOpen);
                    Check(client.Board.Menu.Panel, client.Board.Root);
                    Assert.IsEmpty(menu.Panel.Query<ScrollView>().ToList());
                    foreach (var button in menu.Panel.Query<Button>().ToList().Where(b => b.resolvedStyle.display != DisplayStyle.None))
                    {
                        Check(button, menu.Panel);
                        var picked = button.panel.Pick(button.worldBound.center);
                        Assert.IsTrue(picked == button || button.Contains(picked), button.name);
                        Assert.GreaterOrEqual(button.worldBound.height, 32);
                    }
                    LayoutTests.Save(texture, $"ui70-menu-{width}-{scale:0.00}.png");
                    var position = client.Board.Map.worldBound.center;
                    var outside = menu.Root.panel.Pick(position);
                    Assert.AreSame(menu.Root, outside);
                    using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
                        outside.SendEvent(down);
                    Assert.IsTrue(menu.IsOpen, "The shield owns the entire press, including release.");
                    using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
                        outside.SendEvent(up);
                    yield return Settle();
                    Assert.IsFalse(menu.IsOpen);
                    Assert.AreEqual(0, selections, "Outside click must not select a region through the menu.");
                    Assert.AreSame(menu.Toggle, menu.Toggle.panel.focusController.focusedElement);
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        // MENU-14: Quit game is the menu's last item; its question fits four layouts, starts on Cancel, and Escape,
        // Cancel or a click beside it keep the game, while Quit asks the flow to leave.
        [UnityTest]
        public IEnumerator QuitGameAsksFirstInFourLayoutsAndOnlyQuitLeaves()
        {
            var quits = 0;
            return AsksFirst("menu-quit", "board-menu-quit", "quit", menu => menu.Quit,
                menu => menu.QuitRequested += _ => quits++, () => quits);
        }

        private static IEnumerator AsksFirst(string name, string item, string prefix, System.Func<BoardMenu, ConfirmationDialog> question,
            System.Action<BoardMenu> observe, System.Func<int> requests)
        {
            using var client = new MountedMatchClient("http://localhost", name);
            client.Apply(new BoardFixture().View);
            var menu = client.Board.Menu;
            var dialog = question(menu);
            observe(menu);
            var answered = 0;
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Interface.Preferences.SetScale(scale);
                    yield return Settle();
                    PointerGestureEvents.ClickAt(menu.Toggle);
                    yield return Settle();
                    var items = menu.Panel.Query<Button>().ToList().Where(b => b.resolvedStyle.display != DisplayStyle.None).Select(b => b.name);
                    // MENU-32: no Delete game in the board menu.
                    CollectionAssert.AreEqual(new[] { "board-menu-resume", "board-menu-victory", "open-interface", "back-to-games",
                        "board-menu-quit" }, items);
                    yield return Ask(menu, item, dialog);
                    var window = dialog.Root.Q(prefix + "-window");
                    Check(window, client.Board.Root);
                    foreach (var button in window.Query<Button>().ToList())
                    {
                        Check(button, window);
                        var picked = button.panel.Pick(button.worldBound.center);
                        Assert.IsTrue(picked == button || button.Contains(picked), button.name);
                        Assert.GreaterOrEqual(button.worldBound.height, 32);
                    }
                    var cancel = window.Q<Button>(prefix + "-cancel");
                    Assert.AreSame(cancel, cancel.panel.focusController.focusedElement, "The question starts on Cancel.");
                    Key(cancel, true);
                    Key(cancel, false);
                    yield return Settle();
                    Assert.IsFalse(dialog.IsOpen, "Escape answers the question.");
                    Assert.IsTrue(menu.IsOpen, "Escape closes only the question, not the menu.");
                    yield return Ask(menu, item, dialog);
                    var beside = new Vector2(client.Board.Root.worldBound.xMin + 4, client.Board.Root.worldBound.yMin + 4);
                    Assert.AreSame(dialog.Root, dialog.Root.panel.Pick(beside));
                    using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = beside }))
                        dialog.Root.SendEvent(down);
                    using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = beside }))
                        dialog.Root.SendEvent(up);
                    yield return Settle();
                    Assert.IsFalse(dialog.IsOpen, "A click beside the question changes nothing.");
                    Assert.IsTrue(menu.IsOpen, "The click beside the question does not close the menu.");
                    yield return Ask(menu, item, dialog);
                    PointerGestureEvents.ClickAt(cancel);
                    yield return Settle();
                    Assert.IsFalse(dialog.IsOpen);
                    Assert.AreEqual(answered, requests(), "Nothing happens before the confirmation.");
                    yield return Ask(menu, item, dialog);
                    var request = menu.Request;
                    PointerGestureEvents.ClickAt(window.Q<Button>(prefix + "-confirm"));
                    yield return Settle();
                    Assert.AreEqual(++answered, requests());
                    Assert.Greater(menu.Request, request);
                    menu.Close();
                    yield return Settle();
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        private static IEnumerator Ask(BoardMenu menu, string item, ConfirmationDialog dialog)
        {
            PointerGestureEvents.ClickAt(menu.Panel.Q<Button>(item));
            yield return Settle();
            Assert.IsTrue(dialog.IsOpen);
        }

        [UnityTest]
        public IEnumerator OneEscapeClosesOneWindowThenOneSelectionThenOpensMenuAndPreservesFocus()
        {
            using var client = new MountedMatchClient("http://localhost", "menu-escape");
            client.Apply(new BoardFixture().View);
            yield return Settle();
            ArmyGestures.Die(client, "army");
            yield return Settle();
            var die = client.Board.OwnDice.Q<Button>("army");
            client.Board.Cards.Show("base.fp.strategy.01", true);
            yield return Settle();
            Assert.IsTrue(client.Board.Cards.Pinned);
            Assert.AreEqual("army", client.Board.ActionDie.DieId);
            Assert.IsFalse(client.Board.Menu.IsOpen);
            Key(client.Board.Cards.Root, true);
            yield return Settle();
            Assert.IsEmpty(client.Board.Cards.CardId, "One Escape closes one window.");
            Key(client.Board.Root, true);
            yield return Settle();
            Assert.AreEqual("army", client.Board.ActionDie.DieId, "A held key cannot clear the next selection.");
            Key(client.Board.Root, false);
            yield return Settle();
            Assert.AreEqual("army", client.Board.ActionDie.DieId);
            client.Board.HandDrawer.Close();
            Key(client.Board.Root, true);
            Key(client.Board.Root, false);
            yield return Settle();
            Assert.IsEmpty(client.Board.ActionDie.DieId);
            Assert.IsFalse(client.Board.Menu.IsOpen);
            die.Focus();
            Key(client.Board.Root, true);
            Key(client.Board.Root, false);
            yield return Settle();
            Assert.IsTrue(client.Board.Menu.IsOpen);
            PointerGestureEvents.ClickAt(client.Board.Menu.Panel.Q<Button>("board-menu-resume"));
            yield return Settle();
            Assert.AreSame(die, die.panel.focusController.focusedElement);
        }

        internal static void Key(VisualElement target, bool down)
        {
            var value = new Event { type = down ? EventType.KeyDown : EventType.KeyUp, keyCode = KeyCode.Escape };
            if (down)
            {
                using var e = KeyDownEvent.GetPooled(value);
                target.SendEvent(e);
            }
            else
            {
                using var e = KeyUpEvent.GetPooled(value);
                target.SendEvent(e);
            }
        }

        [UnityTest]
        public IEnumerator SettingsEscapeRemainsOwnedUntilReleaseAfterFocusReturnsToBoard()
        {
            using var client = new MountedMatchClient("http://localhost", "menu-settings-escape");
            client.Apply(new BoardFixture().View);
            yield return Settle();
            ArmyGestures.Die(client, "army");
            client.Board.Cards.Show("base.fp.strategy.01", true);
            yield return Settle();
            var menu = client.Board.Menu;
            menu.Toggle.Focus();
            PointerGestureEvents.ClickAt(menu.Toggle);
            yield return Settle();
            PointerGestureEvents.ClickAt(menu.Panel.Q<Button>("open-interface"));
            yield return Settle();
            var settings = client.Interface.Settings;
            Assert.IsTrue(settings.IsOpen);
            Assert.IsFalse(client.Interface.Content.enabledInHierarchy);
            Key(settings.Root.Q<Button>("close-interface"), true);
            yield return Settle();
            Assert.IsFalse(settings.IsOpen);
            Assert.IsTrue(client.Interface.Content.enabledInHierarchy);
            Assert.AreSame(menu.Toggle, menu.Toggle.panel.focusController.focusedElement);
            Key(menu.Toggle, true);
            yield return Settle();
            Assert.IsTrue(client.Board.Cards.Pinned, "A repeat of Settings' Escape must not close another window.");
            Assert.AreEqual("base.fp.strategy.01", client.Board.Cards.CardId);
            Assert.AreEqual("army", client.Board.ActionDie.DieId);
            Assert.IsFalse(menu.IsOpen, "The same physical key press cannot reopen Menu.");
            Key(menu.Toggle, false);
            Key(menu.Toggle, true);
            yield return Settle();
            Assert.IsEmpty(client.Board.Cards.CardId, "A new press after KeyUp may close the remaining inspection.");
            Assert.AreEqual("army", client.Board.ActionDie.DieId);
            Assert.IsFalse(menu.IsOpen);
            Key(menu.Toggle, false);
        }

        [UnityTest]
        public IEnumerator SettingsKeyboardObservationDoesNotConsumeOtherScreensAndDetachesWithBoard()
        {
            using var client = new MountedMatchClient("http://localhost", "menu-settings-scope");
            client.Apply(new BoardFixture().View);
            yield return Settle();
            var field = new TextField("Other screen") { name = "other-screen-input" };
            client.Interface.Content.Add(field);
            var received = 0;
            field.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) received++; }, TrickleDown.TrickleDown);
            field.Focus();
            yield return Settle();
            var inputFocus = field.panel.focusController.focusedElement as VisualElement;
            Assert.IsTrue(inputFocus == field || field.Contains(inputFocus));
            Key(inputFocus, true);
            Key(inputFocus, false);
            Assert.AreEqual(1, received, "Observing Settings must not take Escape from a sibling text field.");
            Assert.IsFalse(client.Board.Menu.IsOpen);
            client.Board.Root.RemoveFromHierarchy();
            client.Interface.Open();
            yield return Settle();
            Key(client.Interface.Settings.Root.Q<Button>("close-interface"), true);
            yield return Settle();
            Assert.AreSame(inputFocus, field.panel.focusController.focusedElement);
            Key(inputFocus, true);
            Key(inputFocus, false);
            Assert.AreEqual(2, received, "The detached board must not own keys on the next screen.");
            field.RemoveFromHierarchy();
            client.Interface.Content.Add(client.Board.Root);
            yield return Settle();
            var menu = client.Board.Menu;
            menu.Toggle.Focus();
            yield return Settle();
            Assert.AreSame(menu.Toggle, menu.Toggle.panel.focusController.focusedElement,
                "After removing the other screen, route the new key to a real focused board control.");
            var openWindows = client.Board.Root.Query<VisualElement>(className: "floating-window").ToList()
                .Where(window => window.resolvedStyle.display != DisplayStyle.None && window.worldBound.width > 0 && window.worldBound.height > 0)
                .Select(window => window.name).ToArray();
            Assert.IsFalse(menu.IsOpen);
            Key(menu.Toggle, true);
            yield return Settle();
            Assert.IsTrue(menu.IsOpen, "Reattaching starts with no stale held key. Earlier visible windows: " + string.Join(", ", openWindows));
            Key(menu.Toggle, false);
        }

        [UnityTest]
        public IEnumerator EscapeCancelsWindowDragBeforeClosingEitherPinnedWindowOrChangingTheDraft()
        {
            using var client = new MountedMatchClient("http://localhost", "menu-window-drag");
            client.Apply(new BoardFixture(true).View);
            yield return Settle();
            ArmyGestures.Die(client, "army");
            client.Board.Nations.Open();
            client.Board.HandDrawer.Open();
            client.Board.Cards.Show("base.fp.strategy.01", true);
            yield return Settle();
            var window = client.Board.Cards.Root;
            var handle = window.Q<VisualElement>(className: FloatingWindow.HandleClass).Q<Label>();
            var point = handle.worldBound.center;
            var picked = window.panel.Pick(point);
            Assert.IsTrue(picked == handle || handle.Contains(picked), "The card inspector remains a movable window beside the docked hand.");
            PointerGestureEvents.Press(picked);
            yield return null;
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = point + new Vector2(-50, -30) }))
                window.SendEvent(move);
            yield return Settle();
            Assert.IsTrue(window.ClassListContains("window-dragging"));
            Assert.IsTrue(window.HasPointerCapture(PointerId.mousePointerId));
            var position = window.layout.position;
            Key(window, true);
            yield return Settle();
            Assert.IsFalse(window.ClassListContains("window-dragging"));
            Assert.IsFalse(window.HasPointerCapture(PointerId.mousePointerId));
            Assert.IsTrue(client.Board.HandDrawer.IsOpen);
            Assert.IsTrue(client.Board.HandDrawer.Pinned);
            Assert.IsTrue(client.Board.Nations.Pinned);
            Assert.AreEqual(DisplayStyle.Flex, client.Board.Nations.Drawer.resolvedStyle.display);
            Assert.AreEqual("army", client.Board.ActionDie.DieId);
            Assert.IsFalse(client.Board.Menu.IsOpen);
            Key(window, true);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = point + new Vector2(-90, -80) }))
                window.SendEvent(move);
            yield return Settle();
            Assert.That(Vector2.Distance(position, window.layout.position), Is.LessThan(1));
            Assert.IsTrue(client.Board.HandDrawer.IsOpen, "Held Escape cannot close the window after cancelling its drag.");
            Key(window, false);
            PointerGestureEvents.Release(window);
        }

        [UnityTest]
        public IEnumerator EscapeCancelsMapPanAndCaptureWithoutClosingInspectionOrUndoingTheDie()
        {
            using var client = new MountedMatchClient("http://localhost", "menu-map-drag");
            client.Apply(new BoardFixture().View);
            yield return Settle();
            ArmyGestures.Die(client, "army");
            client.Board.Information.Rules();
            yield return Settle();
            var map = client.Board.Map;
            // UI-324: the rules window opens in the middle of the board, over the map's centre; the pan starts beside it.
            var point = MapContourProbe.ExposedMap(map);
            var picked = map.panel.Pick(point);
            Assert.IsTrue(picked == map || map.Contains(picked), "The actual map pan origin must be exposed.");
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 1, mousePosition = point }))
                picked.SendEvent(down);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 1, mousePosition = point + new Vector2(30, 20) }))
                map.SendEvent(move);
            yield return Settle();
            Assert.IsTrue(map.Panning);
            Assert.IsTrue(map.HasPointerCapture(PointerId.mousePointerId));
            var pan = map.Camera.Pan;
            Key(client.Board.Root, true);
            yield return Settle();
            Assert.IsFalse(map.Panning);
            Assert.IsFalse(map.HasPointerCapture(PointerId.mousePointerId));
            Assert.AreEqual(DisplayStyle.Flex, client.Board.Information.Root.resolvedStyle.display);
            Assert.AreEqual("army", client.Board.ActionDie.DieId);
            Assert.IsFalse(client.Board.Menu.IsOpen);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 1, mousePosition = point + new Vector2(90, 70) }))
                map.SendEvent(move);
            yield return Settle();
            Assert.AreEqual(pan, map.Camera.Pan, "Mouse movement after Escape cannot keep panning behind another UI.");
            Key(client.Board.Root, false);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 1, mousePosition = point }))
                map.SendEvent(up);
        }

        private static void Check(VisualElement element, VisualElement parent)
        {
            Assert.GreaterOrEqual(element.worldBound.xMin + 1, parent.worldBound.xMin, element.name);
            Assert.GreaterOrEqual(element.worldBound.yMin + 1, parent.worldBound.yMin, element.name);
            Assert.LessOrEqual(element.worldBound.xMax, parent.worldBound.xMax + 1, element.name);
            Assert.LessOrEqual(element.worldBound.yMax, parent.worldBound.yMax + 1, element.name);
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }
    }
}
