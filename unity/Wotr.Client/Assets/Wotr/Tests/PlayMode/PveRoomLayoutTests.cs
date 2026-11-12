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
                    Assert.AreEqual(DisplayStyle.None, screen.Root.Q<Button>("room-swap-sides").resolvedStyle.display,
                        "A room without the CPU has no side swap (the other seat is a player's).");
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
                        AssertThePaceStandsClear(screen, seat, key, scale);
                        AssertTheSwapStandsClear(screen, human);
                        LayoutTests.Save(target, $"bot13-room-{human}-{status}-{width}-{scale:0.00}.png");
                    }
                }
                client.Panel.targetTexture = null;
                target.Release();
                Object.Destroy(target);
            }
        }
        // Owner (2026-11-11): the CPU's pace covered its status and Take seat. It stays inside the seat and clear of the
        // side's name, the player's name and status (as far as their text reaches) and the seat's buttons.
        private static void AssertThePaceStandsClear(WaitingRoomScreen screen, VisualElement seat, string key, float scale)
        {
            var pace = screen.Root.Q<Button>("seat-" + key + "-pace");
            Assert.AreEqual(DisplayStyle.Flex, pace.resolvedStyle.display, "The CPU's seat shows its pace.");
            var bound = pace.worldBound;
            Assert.IsTrue(seat.worldBound.Contains(bound.min) && seat.worldBound.Contains(bound.max), "The pace stays in its seat.");
            foreach (var name in new[] { "-take", "-remove" })
                Assert.IsFalse(bound.Overlaps(screen.Root.Q<Button>("seat-" + key + name).worldBound), "The pace overlaps " + name);
            foreach (var label in seat.Query<Label>().ToList())
            {
                if (label.parent == pace || string.IsNullOrEmpty(label.text) || label.resolvedStyle.display == DisplayStyle.None)
                    continue;
                var text = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                var reach = new Rect(label.worldBound.xMin, label.worldBound.yMin,
                    Mathf.Min(label.worldBound.width, text.x * scale), label.worldBound.height);
                Assert.IsFalse(bound.Overlaps(reach), $"The pace covers '{label.text}'.");
            }
        }

        // UI-533 (owner, 2026-11-12): who plays which side is chosen in the CPU's waiting room: one button in the action column
        // names the other side, stays inside the column, overlaps no other action and asks for the swap.
        private static void AssertTheSwapStandsClear(WaitingRoomScreen screen, Side human)
        {
            var swap = screen.Root.Q<Button>("room-swap-sides");
            Assert.AreEqual(DisplayStyle.Flex, swap.resolvedStyle.display, "A room with the CPU offers the other side.");
            Assert.AreEqual(human == Side.FreePeoples ? "Play the Shadow" : "Play the Free Peoples", swap.text);
            Assert.IsTrue(swap.enabledInHierarchy);
            var actions = screen.Root.Q("waiting-actions");
            Assert.IsTrue(actions.worldBound.Contains(swap.worldBound.min) && actions.worldBound.Contains(swap.worldBound.max),
                "Swap sides stays in the action column.");
            foreach (var name in new[] { "room-ready", "start-game", "room-demo", "room-observe", "waiting-back" })
                Assert.IsFalse(swap.worldBound.Overlaps(screen.Root.Q<Button>(name).worldBound), "Swap sides overlaps " + name);
            Assert.LessOrEqual(swap.MeasureTextSize(swap.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x,
                swap.contentRect.width + 1, "The swap's label fits.");
            var swaps = 0;
            void Count() => swaps++;
            screen.SwapSides += Count;
            ReturnTestFixture.Click(swap);
            screen.SwapSides -= Count;
            Assert.AreEqual(1, swaps);
        }

        private static IEnumerator Settle()
        {
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(.12f);
        }
    }
}
