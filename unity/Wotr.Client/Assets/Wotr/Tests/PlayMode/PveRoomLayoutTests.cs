using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed class PveRoomLayoutTests
    {
        [UnityTest]
        public IEnumerator BothSeatsKeepReadableCpuStatesInAllFourLayouts()
        {
            using var client = new MountedFlowClient(null, null);
            foreach (var width in new[] { 1366, 1920 })
            foreach (var scale in new[] { 1f, 1.25f })
            {
                var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                target.Create();
                client.Panel.targetTexture = target;
                client.Interface.Preferences.SetScale(scale);
                foreach (var human in new[] { Side.FreePeoples, Side.Shadow })
                {
                    client.Interface.Content.Clear();
                    var screen = new WaitingRoomScreen();
                    client.Interface.Content.Add(screen.Root);
                    var room = new RoomView { Id = "room", Name = "Play vs CPU", Managed = true, IsHost = true,
                        Host = "Human", Role = RoomRole.Player, You = human, FreePeoples = human == Side.FreePeoples ? "Human" : "",
                        Shadow = human == Side.Shadow ? "Human" : "", ObserverLimit = 16 };
                    screen.Update(room);
                    yield return Settle();
                    var key = human == Side.FreePeoples ? "shadow" : "free";
                    var button = screen.Root.Q<Button>("seat-" + key + "-remove");
                    var before = button.worldBound;
                    var start = screen.Root.Q<Button>("start-game").worldBound;
                    Assert.IsFalse(screen.Root.Q<Label>("waiting-title").isElided, "The default CPU room title is fully readable.");
                    Assert.AreEqual("Add CPU", button.text);
                    Assert.IsTrue(button.enabledInHierarchy);
                    var invitations = 0;
                    screen.AddCpu += () => invitations++;
                    button.Focus();
                    PointerGestureEvents.Press(button);
                    Assert.IsTrue(button.ClassListContains(PressFeedback.PressedClass));
                    Assert.AreEqual(0, invitations, "Mouse-down only gives feedback.");
                    LayoutTests.Save(target, $"bot13-room-{human}-AddCpu-{width}-{scale:0.00}.png");
                    PointerGestureEvents.Release(button);
                    Assert.AreEqual(1, invitations, "Release inside completes the invitation click.");
                    Assert.IsFalse(button.ClassListContains(PressFeedback.PressedClass));
                    foreach (var status in new[] { BotSeatStatus.Joining, BotSeatStatus.Ready, BotSeatStatus.Unavailable })
                    {
                        room.Bot = new BotSeatView { Side = human == Side.FreePeoples ? Side.Shadow : Side.FreePeoples, Status = status };
                        if (human == Side.FreePeoples) room.Shadow = "CPU";
                        else room.FreePeoples = "CPU";
                        room.FreePeoplesAccepted = room.ShadowAccepted = status == BotSeatStatus.Ready;
                        screen.Update(room);
                        yield return Settle();
                        Assert.AreEqual(before, button.worldBound);
                        Assert.AreEqual(start, screen.Root.Q<Button>("start-game").worldBound);
                        var seat = screen.Root.Q("seat-" + key);
                        var state = screen.Root.Q<Label>("seat-" + key + "-state");
                        foreach (var control in new VisualElement[] { state, button, screen.Root.Q<Button>("seat-" + key + "-take") })
                            Assert.IsTrue(seat.worldBound.Contains(control.worldBound.min) && seat.worldBound.Contains(control.worldBound.max), control.name);
                        // UI-415 (owner): the compact waiting room sets every seat state at 14 points; the CPU's is one of them.
                        Assert.GreaterOrEqual(state.resolvedStyle.fontSize, 14);
                        Assert.LessOrEqual(state.worldBound.xMin + state.MeasureTextSize(state.text, 0,
                            VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x * scale,
                            screen.Root.Q<Button>("seat-" + key + "-take").worldBound.xMin, "The status cannot overlap seat actions.");
                        Assert.LessOrEqual(state.MeasureTextSize(state.text, 0, VisualElement.MeasureMode.Undefined, 0,
                            VisualElement.MeasureMode.Undefined).x, state.contentRect.width + 1);
                        LayoutTests.Save(target, $"bot13-room-{human}-{status}-{width}-{scale:0.00}.png");
                    }
                }
                client.Panel.targetTexture = null;
                target.Release();
                Object.Destroy(target);
            }
        }
        private static IEnumerator Settle()
        {
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(.12f);
        }
    }
}
