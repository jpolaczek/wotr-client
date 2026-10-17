using System.Collections;
using System.Linq;
using System.Threading.Tasks;
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
    public class RuntimeTooltipTests
    {
        [UnityTest]
        public IEnumerator HoverAndFocusExplainWithoutActingAndActivationDismissesTheDescription()
        {
            using var client = Create();
            var clicks = 0;
            var button = Probe(client, "A precise explanation", () => clicks++);
            yield return Settle();
            var tip = Hint(client);
            Move(button);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(DisplayStyle.None, tip.style.display.value);
            yield return new WaitForSecondsRealtime(.16f);
            Visible(tip, button.tooltip);
            Assert.AreEqual(0, clicks);
            Assert.AreSame(button, button.panel.Pick(button.worldBound.center));
            Assert.AreNotSame(tip, button.panel.Pick(tip.worldBound.center));
            Move(client.Board.Phase);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, tip.style.display.value);
            button.Focus();
            yield return Settle();
            Visible(tip, button.tooltip);
            CardNetworkTests.Click(button);
            yield return Settle();
            Assert.AreEqual(1, clicks, "Inspection must not swallow or duplicate the action.");
            Assert.AreEqual(DisplayStyle.None, tip.style.display.value);
            button.Blur();
            button.Focus();
            yield return Settle();
            Visible(tip, button.tooltip);
            using (var submit = NavigationSubmitEvent.GetPooled())
                button.SendEvent(submit);
            yield return Settle();
            Assert.AreEqual(2, clicks);
            Assert.AreEqual(DisplayStyle.None, tip.style.display.value);
            Move(button);
            yield return new WaitForSecondsRealtime(.26f);
            Visible(tip, button.tooltip);
            client.Interface.Preferences.SetScale(1.25f);
            Assert.AreEqual(DisplayStyle.None, tip.style.display.value);
            yield return Settle();
            Move(button);
            yield return new WaitForSecondsRealtime(.26f);
            Visible(tip, button.tooltip);
            button.RemoveFromHierarchy();
            Assert.AreEqual(DisplayStyle.None, tip.style.display.value);
            Assert.IsEmpty(tip.text, "Detached views must not retain private text.");
        }

        [UnityTest]
        public IEnumerator DragCancelAndRemovedActionSourcesNeverLeaveDescriptions()
        {
            using var client = Create();
            var button = Probe(client, "Do not open after a drag");
            yield return Settle();
            var start = button.worldBound.center;
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = start }))
                button.SendEvent(down);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = start + Vector2.right * 20 }))
                button.SendEvent(move);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = start + Vector2.right * 20 }))
                button.SendEvent(up);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = start }))
                button.SendEvent(down);
            using (var cancel = PointerCancelEvent.GetPooled())
                button.SendEvent(cancel);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = start }))
                button.SendEvent(up);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
            Move(button);
            yield return new WaitForSecondsRealtime(.26f);
            Visible(Hint(client), button.tooltip);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = start }))
                button.SendEvent(down);
            button.CapturePointer(PointerId.mousePointerId);
            button.ReleasePointer(PointerId.mousePointerId);
            Move(button);
            yield return new WaitForSecondsRealtime(.26f);
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value,
                "Losing capture while pressed must not resume passive descriptions.");
            using (var cancel = PointerCancelEvent.GetPooled())
                button.SendEvent(cancel);
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = start }))
                button.SendEvent(up);
            Move(button);
            yield return new WaitForSecondsRealtime(.26f);
            Visible(Hint(client), button.tooltip);
            button.clicked += button.RemoveFromHierarchy;
            CardNetworkTests.Click(button);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
        }

        [UnityTest]
        public IEnumerator EmptyScreenClicksDoNotDisableLaterHints()
        {
            using var client = Create();
            var button = Probe(client, "Available after background clicks");
            yield return Settle();
            CardNetworkTests.Click(button.panel.visualTree);
            CardNetworkTests.Click(button.panel.visualTree);
            Move(button);
            yield return new WaitForSecondsRealtime(.26f);
            Visible(Hint(client), button.tooltip);
        }

        [UnityTest]
        public IEnumerator RichSourcesExposeMetadataWhileTerrainAndTextFieldsStaySilent()
        {
            using var client = Create();
            yield return Settle();
            client.Board.Map.Focus("pelargir");
            client.Board.Map.tooltip = "Terrain descriptions require explicit inspection";
            yield return Settle();
            MapContourProbe.ReachRegion(client.Board.Map, "pelargir");
            // Navigation may require a wheel gesture; finish it before testing idle hover.
            yield return new WaitForSecondsRealtime(.26f);
            var terrain = MapContourProbe.Terrain(client.Board.Map, "pelargir");
            MapContourProbe.Hover(client.Board.Root, terrain);
            client.Board.Map.Focus();
            yield return new WaitForSecondsRealtime(.26f);
            Assert.AreEqual("pelargir", client.Board.Map.Hovered);
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
            Assert.AreEqual(DisplayStyle.None, client.Board.Hover.Panel.Root.resolvedStyle.display);
            Assert.IsEmpty(client.Board.ActionDie.DieId);
            client.Board.Map.tooltip = "";
            yield return Settle();
            client.Board.Victory.Open();
            yield return Settle();
            var card = client.Board.Hand.Query<Button>(className: "hand-card").First();
            Move(card);
            yield return new WaitForSecondsRealtime(.25f);
            // UI-91: the unrolled card is the passive hint; no tooltip covers it.
            Assert.AreEqual(card.name, client.Board.ExpandedCard);
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
            Assert.IsEmpty(client.Board.Cards.CardId, "A passive card hint never opens its interactive inspector.");
            var field = new TextField { value = "private input", tooltip = "not game information" };
            field.style.position = Position.Absolute;
            field.style.left = 30;
            field.style.top = 260;
            client.Board.Root.Add(field);
            yield return Settle();
            Move(field);
            field.Focus();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
        }

        [UnityTest]
        public IEnumerator DisabledOwnActivationExplainsItsStateWithoutEnablingOrChoosingAnAction()
        {
            using var client = Create();
            var view = client.Board.View;
            var nation = view.Nations.Single(item => item.Id == "north");
            nation.Active = false;
            nation.StepsToWar = 1;
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.UseNarya, Enabled = true,
                EligibleDice = new System.Collections.Generic.List<string> { "muster" },
                Targets = new System.Collections.Generic.List<string> { "north" }
            });
            client.Apply(view);
            client.Board.Nations.Open();
            yield return Settle();
            PointerGestureEvents.ClickAt(client.Board.Nations.Drawer.Q<Button>("nation-status-north"));
            yield return Settle();
            var activation = client.Board.Nations.Drawer.Q<Button>("nation-activate-north");
            Assert.AreEqual(Visibility.Visible, activation.resolvedStyle.visibility, "UI-144: an offered activation shows without a die.");
            Assert.IsFalse(activation.enabledInHierarchy);
            StringAssert.Contains("Select an available action die", activation.tooltip);
            var picked = activation.panel.Pick(activation.worldBound.center);
            Assert.IsTrue(picked == activation || activation.Contains(picked));
            Move(activation);
            yield return new WaitForSecondsRealtime(.26f);
            Visible(Hint(client), activation.tooltip);
            Assert.IsNotEmpty(activation.tooltip);
            PointerGestureEvents.ClickAt(activation);
            yield return Settle();
            Assert.IsFalse(activation.enabledInHierarchy);
            Assert.IsFalse(nation.Active);
            Assert.IsEmpty(client.Board.ActionDie.DieId);
            Assert.AreEqual(0, view.Revision);
        }

        [UnityTest]
        public IEnumerator EveryMouseButtonAndWheelSuppressHintsUntilAFreshIdleHover()
        {
            using var client = Create();
            var button = Probe(client, "Read only while idle");
            yield return Settle();
            foreach (var mouseButton in new[] { 0, 1, 2 })
            {
                Move(button);
                yield return new WaitForSecondsRealtime(.26f);
                Visible(Hint(client), button.tooltip);
                var point = button.worldBound.center;
                using (var down = PointerDownEvent.GetPooled(new Event
                    { type = EventType.MouseDown, button = mouseButton, mousePosition = point }))
                    button.SendEvent(down);
                Move(button);
                button.Focus();
                yield return new WaitForSecondsRealtime(.26f);
                Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
                using (var up = PointerUpEvent.GetPooled(new Event
                    { type = EventType.MouseUp, button = mouseButton, mousePosition = point }))
                    button.SendEvent(up);
                yield return new WaitForSecondsRealtime(.26f);
                Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value, "Release alone must not reopen a hint.");
            }
            Move(button);
            yield return new WaitForSecondsRealtime(.26f);
            Visible(Hint(client), button.tooltip);
            using (var wheel = WheelEvent.GetPooled(new Event
                { type = EventType.ScrollWheel, delta = Vector2.up, mousePosition = button.worldBound.center }))
                button.SendEvent(wheel);
            Move(button);
            yield return new WaitForSecondsRealtime(.26f);
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
            Move(button);
            yield return new WaitForSecondsRealtime(.26f);
            Visible(Hint(client), button.tooltip);
        }

        [UnityTest]
        public IEnumerator EscapeClosesTheFocusedWindowWithoutConsumingAnExtraPressForItsHint()
        {
            using var client = Create();
            client.Board.HandDrawer.Open();
            yield return Settle();
            client.Board.Victory.Open();
            yield return Settle();
            var close = client.Board.Victory.Root.Q<Button>("close-victory");
            close.tooltip = "Close Victory";
            close.Focus();
            yield return Settle();
            Visible(Hint(client), close.tooltip);
            BoardMenuLayoutTests.Key(close, true);
            yield return Settle();
            Assert.IsFalse(client.Board.Victory.IsOpen);
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
            Assert.AreSame(client.Board.Menu.Toggle, client.Board.Root.panel.focusController.focusedElement);
            BoardMenuLayoutTests.Key(client.Board.Menu.Toggle, true);
            yield return Settle();
            Assert.IsFalse(client.Board.Menu.IsOpen, "Repeating held Esc cannot open the next owner.");
            Assert.AreEqual(DisplayStyle.None, Hint(client).style.display.value);
            BoardMenuLayoutTests.Key(client.Board.Menu.Toggle, false);
        }

        [UnityTest]
        public IEnumerator LongHintsFitEveryCornerAtBothSizesAndScales()
        {
            using var client = Create();
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Interface.Preferences.SetScale(scale);
                    yield return Settle();
                    foreach (var corner in new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one })
                    {
                        var button = Probe(client, string.Join(" ", Enumerable.Repeat("Projected unit details, legal cost and known consequences.", 5)));
                        button.style.left = corner.x == 0 ? 8 : client.Board.Root.contentRect.width - 68;
                        button.style.top = corner.y == 0 ? 8 : client.Board.Root.contentRect.height - 44;
                        yield return Settle();
                        Move(button);
                        yield return new WaitForSecondsRealtime(.26f);
                        var tip = Hint(client);
                        Visible(tip, button.tooltip);
                        var viewport = client.Interface.Content.parent.worldBound;
                        Assert.GreaterOrEqual(tip.worldBound.xMin, viewport.xMin);
                        Assert.GreaterOrEqual(tip.worldBound.yMin, viewport.yMin);
                        Assert.LessOrEqual(tip.worldBound.xMax, viewport.xMax);
                        Assert.LessOrEqual(tip.worldBound.yMax, viewport.yMax);
                        Assert.IsFalse(tip.worldBound.Overlaps(button.worldBound));
                        Assert.AreSame(button, button.panel.Pick(button.worldBound.center));
                        button.RemoveFromHierarchy();
                    }
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator CompanionHintsDisplayOnlyThePlayersServerProjection()
        {
            var task = CheckProjections();
            while (!task.IsCompleted)
                yield return null;
            task.GetAwaiter().GetResult();
        }

        private static async Task CheckProjections()
        {
            using var room = await MatchTestRoom.CreateNormalGame();
            var revision = room.Fp.Board.View.Revision;
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                client.Board.Fellowship.Open();
                await room.Settle();
                var portrait = client.Board.Fellowship.Root.Query<Button>(className: "companion-token").First();
                var pan = client.Board.Map.Camera.Pan;
                var zoom = client.Board.Map.Camera.Zoom;
                Move(portrait);
                await Task.Delay(270);
                var tip = Hint(client);
                Assert.AreEqual(DisplayStyle.Flex, tip.style.display.value,
                    $"{client.Board.View.You} portrait {portrait.name}, enabled={portrait.enabledInHierarchy}, panel={client.Board.Fellowship.IsOpen}");
                Visible(tip, portrait.tooltip);
                Assert.AreEqual(client == room.Fp, tip.text.Contains("Level"));
                Assert.AreEqual(pan, client.Board.Map.Camera.Pan);
                Assert.AreEqual(zoom, client.Board.Map.Camera.Zoom);
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                Assert.AreEqual(revision, room.Sh.Board.View.Revision);
            }
        }

        private static MountedMatchClient Create()
        {
            var client = new MountedMatchClient("http://localhost", "tooltip-layout");
            client.Apply(new BoardFixture(true).View);
            return client;
        }

        private static Button Probe(MountedMatchClient client, string text, System.Action clicked = null)
        {
            var button = new Button(clicked) { text = "Hint", tooltip = text };
            button.style.position = Position.Absolute;
            button.style.left = 30;
            button.style.top = 260;
            button.style.width = 60;
            button.style.height = 36;
            client.Board.Root.Add(button);
            return button;
        }

        private static Label Hint(MountedMatchClient client) => client.Interface.Content.parent.Q<Label>("runtime-tooltip");

        private static void Visible(Label tip, string expected)
        {
            Assert.AreEqual(DisplayStyle.Flex, tip.style.display.value);
            Assert.AreEqual(Visibility.Visible, tip.style.visibility.value);
            Assert.AreEqual(expected, tip.text);
            Assert.Greater(tip.worldBound.width, 0);
            Assert.Greater(tip.worldBound.height, 0);
        }

        private static void Move(VisualElement target)
        {
            var root = target.panel.visualTree;
            foreach (var point in new[] { root.worldBound.center, target.worldBound.center })
                using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point }))
                    root.SendEvent(move);
        }

        private static IEnumerator Settle()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(.08f);
        }
    }
}
