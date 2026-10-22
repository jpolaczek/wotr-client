using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public class DraftInputTests
    {
        [UnityTest]
        public IEnumerator HeldEnterCannotSubmitNextSetWhileDelayedPreviewIsPending()
        {
            var backend = new DelayedBackend();
            using (var client = new MountedMatchClient("http://127.0.0.1:5088", "input-fixture", backend))
            {
                client.Apply(backend.View);
                client.SetOnline();
                yield return null;
                yield return null;
                Assert.IsFalse(AllocationReady(client));
                yield return new WaitForSeconds(.65f);
                Assert.IsTrue(AllocationReady(client));
                client.Board.Root.Focus();
                yield return null;
                Key(client.Board.Root, KeyCode.Return, true);
                Key(client.Board.Root, KeyCode.Return, true);
                yield return new WaitForSeconds(.1f);
                Assert.AreEqual(1, backend.Commands);
                Assert.AreEqual(CommandKind.AllocateHunt, backend.LastCommand.Kind);
                Assert.IsFalse(AllocationReady(client));
                yield return new WaitForSeconds(.65f);
                Key(client.Board.Root, KeyCode.Return, true);
                Assert.AreEqual(1, backend.Commands);
                Key(client.Board.Root, KeyCode.Return, false);
                Key(client.Board.Root, KeyCode.Return, true);
                Key(client.Board.Root, KeyCode.Return, false);
                yield return new WaitForSeconds(.1f);
                Assert.AreEqual(2, backend.Commands);
            }
        }

        // UI-281: Enter typed in the board's open chat sends the message and is not the board's confirmation, even
        // while a choice (here the Hunt allocation) is ready; Enter outside the chat still confirms it.
        [UnityTest]
        public IEnumerator EnterInTheOpenChatSendsTheMessageAndConfirmsNothing()
        {
            var backend = new DelayedBackend();
            using (var client = new MountedMatchClient("http://127.0.0.1:5088", "input-fixture", backend))
            {
                client.Apply(backend.View);
                client.SetOnline();
                yield return UntilReady(client);
                Assert.IsTrue(AllocationReady(client));
                var chat = client.Board.GameChat;
                var sent = new List<string>();
                chat.Chat.Sent += sent.Add;
                chat.Available(true);
                chat.Open();
                chat.Chat.Input.value = "Ready when you are";
                yield return null;
                var line = chat.Chat.Input.Q(className: "unity-base-text-field__input") ?? chat.Chat.Input;
                Key(line, KeyCode.Return, true);
                Key(line, KeyCode.Return, false);
                yield return new WaitForSeconds(.1f);
                CollectionAssert.AreEqual(new[] { "Ready when you are" }, sent);
                Assert.AreEqual(0, backend.Commands, "The chat's Enter is not the board's confirmation.");
                client.Board.Root.Focus();
                yield return UntilReady(client);
                Assert.IsTrue(AllocationReady(client), "The allocation still waits for its confirmation.");
                Key(client.Board.Root, KeyCode.Return, true);
                Key(client.Board.Root, KeyCode.Return, false);
                yield return new WaitForSeconds(.1f);
                Assert.AreEqual(1, backend.Commands);
                Assert.AreEqual(CommandKind.AllocateHunt, backend.LastCommand.Kind);
            }
        }

        // The delayed preview makes the allocation ready after about .6 s; a loaded machine may need longer.
        private static IEnumerator UntilReady(MountedMatchClient client)
        {
            for (var waited = 0f; !AllocationReady(client) && waited < 5f; waited += Time.unscaledDeltaTime)
                yield return null;
        }

        [UnityTest]
        public IEnumerator CapturedButtonPressRequiresTheOriginalScopeAndReleasesCleanly()
        {
            using (var client = new MountedMatchClient("http://127.0.0.1:5088", "input-fixture"))
            {
                var surface = new VisualElement();
                surface.style.position = Position.Absolute;
                surface.style.left = 100;
                surface.style.top = 100;
                surface.style.width = 180;
                client.Board.Root.parent.Add(surface);
                var clicks = 0;
                var revision = "first";
                var button = new Button(() => clicks++) { text = "Decision" };
                surface.Add(button);
                using (var input = new DraftInput(surface, () => { }, () => { }, (_, _) => false, () => revision))
                {
                    yield return null;
                    foreach (var token in new[] { null, new DraftToken(DraftObject.Deck, "character"), new DraftToken(DraftObject.Die, "die") })
                    {
                        button.userData = token;
                        PointerGestureEvents.Press(button);
                        Assert.IsTrue(button.HasPointerCapture(PointerId.mousePointerId));
                        revision += "-next";
                        PointerGestureEvents.Release(button);
                        yield return null;
                        Assert.AreEqual(0, clicks, "A captured release cannot cross a decision boundary.");
                        Assert.IsFalse(button.HasPointerCapture(PointerId.mousePointerId));
                        CardNetworkTests.Click(button);
                        Assert.AreEqual(1, clicks, "A fresh click still works after cancellation.");
                        clicks = 0;
                    }
                    PointerGestureEvents.Press(button);
                    input.Dispose();
                    yield return null;
                    Assert.IsFalse(button.HasPointerCapture(PointerId.mousePointerId));
                    using (var next = new DraftInput(surface, () => { }, () => { }, (_, _) => false, () => "other-match"))
                    {
                        PointerGestureEvents.Release(button);
                        Assert.AreEqual(0, clicks, "A new match cannot inherit a pressed button.");
                        CardNetworkTests.Click(button);
                        Assert.AreEqual(1, clicks);
                    }
                }
                surface.RemoveFromHierarchy();
            }
        }

        [UnityTest]
        public IEnumerator EscapeCancelsCapturedDragAndAFastClickCannotCrossScope()
        {
            using (var client = new MountedMatchClient("http://127.0.0.1:5088", "input-fixture"))
            {
                var surface = new VisualElement();
                surface.style.position = Position.Absolute;
                surface.style.left = 100;
                surface.style.top = 100;
                surface.style.width = 180;
                client.Board.Root.parent.Add(surface);
                // UI-105: figures and armies are never dragged, so a reserve stands for the draggable object.
                var clicks = 0;
                var figure = new Button(() => clicks++) { text = "Reserve", userData = new DraftToken(DraftObject.Reserve, "reserve") };
                var target = new Button { text = "Region", userData = new DraftToken(DraftObject.Region, "region") };
                surface.Add(figure);
                surface.Add(target);
                var drops = 0;
                var scope = "a";
                using (var input = new DraftInput(surface, () => { }, () => { }, (_, _) => { drops++; return true; }, () => scope))
                {
                    yield return null;
                    PointerGestureEvents.Press(figure);
                    using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = target.worldBound.center }))
                        figure.SendEvent(move);
                    Assert.IsTrue(surface.HasPointerCapture(PointerId.mousePointerId));
                    Key(surface, KeyCode.Escape, true);
                    Key(surface, KeyCode.Escape, false);
                    PointerGestureEvents.Release(target);
                    yield return null;
                    Assert.AreEqual(0, drops);
                    Assert.IsFalse(surface.HasPointerCapture(PointerId.mousePointerId));
                    // UI-142: a fast second click is an ordinary click unless the decision changed in between.
                    CardNetworkTests.Click(figure);
                    scope = "b";
                    Click(figure, 2);
                    Assert.AreEqual(1, clicks, "A fast click after the decision changed is suppressed.");
                    CardNetworkTests.Click(figure);
                    Click(figure, 2);
                    Assert.AreEqual(3, clicks, "Within one decision both clicks reach the control.");
                }
                surface.RemoveFromHierarchy();
            }
        }

        private static void Click(VisualElement target, int count = 1)
        {
            Assert.NotNull(target);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = target.worldBound.center, clickCount = count }))
                target.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = target.worldBound.center, clickCount = count }))
                target.SendEvent(up);
        }

        private static void Key(VisualElement target, KeyCode code, bool down)
        {
            var evt = new Event { type = down ? EventType.KeyDown : EventType.KeyUp, keyCode = code };
            if (down)
            {
                using (var key = KeyDownEvent.GetPooled(evt))
                    target.SendEvent(key);
            }
            else
            {
                using (var key = KeyUpEvent.GetPooled(evt))
                    target.SendEvent(key);
            }
        }

        // This test isolates input timing. NetworkTests separately runs against real Kestrel/PostgreSQL.
        // QUAL-19: since UI-172 the Hunt allocation is confirmed at the Hunt plaque, whose ready state follows the legal
        // preview; Enter still reaches the same confirmation.
        private static bool AllocationReady(MountedMatchClient client) => client.Board.Hunt.Tab.ClassListContains("hunt-confirm-ready");

        private sealed class DelayedBackend : HttpMessageHandler
        {
            public int Commands { get; private set; }
            public GameCommand LastCommand { get; private set; }
            public PlayerView View { get; } = new PlayerView
            {
                MatchId = "input-fixture",
                You = Side.Shadow,
                DecisionOwner = Side.Shadow,
                Phase = GamePhase.HuntAllocation,
                Decision = new DecisionView { Id = "initial", Kind = DecisionKind.AllocateHunt },
                Options =
                {
                    new ActionOption { Kind = CommandKind.AllocateHunt, EligibleIds = { "event-a", "event-b" }, Maximum = 2, AllowEmptySelection = true }
                },
                Dice =
                {
                    new DieView { Id = "event-a", Owner = Side.Shadow, Source = "standard", Face = DieFace.Event, Location = DieLocation.Available },
                    new DieView { Id = "event-b", Owner = Side.Shadow, Source = "standard", Face = DieFace.Event, Location = DieLocation.Available }
                }
            };

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                if (request.RequestUri.AbsolutePath.EndsWith("/preview"))
                {
                    var revision = View.Revision;
                    await Task.Delay(500, cancellation);
                    return Reply(new ActionPreview { Legal = true, Revision = revision, HasUnknownResult = true });
                }
                if (request.RequestUri.AbsolutePath.EndsWith("/commands"))
                {
                    LastCommand = JsonConvert.DeserializeObject<GameCommand>(await request.Content.ReadAsStringAsync());
                    Commands++;
                    View.Revision++;
                    View.Decision.Id = "decision-" + View.Revision;
                    return Reply(new CommandReceipt { Accepted = true, CommandId = LastCommand.CommandId, Revision = View.Revision });
                }
                return Reply(View);
            }

            private static HttpResponseMessage Reply(object value) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(value))
            };
        }
    }
}
