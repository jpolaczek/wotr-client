using System.Collections;
using System.Linq;
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
    public sealed class HuntNetworkTests
    {
        [UnityTest]
        public IEnumerator SeveralDiceAllocateByClicksAndDragWithOneRecoverableCommand() => MatchTestRoom.Wait(Allocation(false));

        [UnityTest]
        public IEnumerator LegalZeroAllocationRollsWithoutChoosingAnyDie() => MatchTestRoom.Wait(Allocation(true));

        [UnityTest]
        public IEnumerator RequiredAllocationKeepsTheExactDieAtItsLimit() => MatchTestRoom.Wait(RequiredAllocation());

        // UI-162: the Shadow allocates in the turn console: clicks on the dice panel build the set, the Hunt plaque
        // counts it and a dropped die joins it. UI-172: the plaque, once ready, submits it once.
        private static async Task RequiredAllocation()
        {
            using var room = await UI10TestRoom.Connect("ui50-hunt-required", shadowBoundary: true);
            var board = room.Sh.Board;
            var option = board.View.Options.Single(o => o.Kind == CommandKind.AllocateHunt);
            var revision = board.View.Revision;
            Assert.AreEqual(1, option.Minimum);
            Assert.AreEqual(1, option.Maximum);
            Assert.IsTrue(board.Hunt.Tab.ClassListContains("hunt-confirm-waiting"), "One die is required.");
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            await room.Settle();
            Assert.AreEqual(0, room.Boundary.Commands, "A plaque that is not ready sends nothing.");
            Assert.IsFalse(board.Hunt.IsOpen, "During the allocation the plaque opens no Hunt window.");
            Assert.AreEqual(DisplayStyle.None, board.Hunt.Pool.Root.style.display.value, "UI-209: nor its pool list.");
            var first = option.EligibleIds[0];
            var second = option.EligibleIds[1];
            Assert.IsTrue(board.OwnDice.Q<Button>(second).ClassListContains("hunt-candidate"));
            // UI-204/UI-205: the dice the Hunt can take pulse together and, with none chosen yet, call loudest; the
            // plaque has no glow and its count badge is still plain.
            var beat = board.OwnDice.ClassListContains(HuntContextInteraction.CandidatePulseClass);
            await Task.Delay(900);
            Assert.AreNotEqual(beat, board.OwnDice.ClassListContains(HuntContextInteraction.CandidatePulseClass), "The candidates pulse.");
            Assert.IsTrue(board.OwnDice.ClassListContains(HuntContextInteraction.ChooseClass), "No die yet: the dice call first.");
            Assert.IsTrue(board.Hunt.Tab.ClassListContains(HuntPanel.EmptyClass));
            Assert.IsFalse(board.Hunt.AllocationCount.ClassListContains(HuntPanel.CountFilledClass));
            Assert.AreEqual("0/1", board.Hunt.AllocationCount.text);
            Assert.IsNull(board.Hunt.Tab.Q("hunt-confirm-glow"), "UI-205: the plaque has no glow of its own.");
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(first));
            await PlaqueReady(room, board);
            // Ready: only the count lights up — an opaque gilt badge.
            Assert.IsFalse(board.OwnDice.ClassListContains(HuntContextInteraction.ChooseClass), "A die is in: the loud call ends.");
            Assert.IsFalse(board.Hunt.Tab.ClassListContains(HuntPanel.EmptyClass));
            Assert.IsTrue(board.Hunt.AllocationCount.ClassListContains(HuntPanel.CountFilledClass));
            Assert.Greater(board.Hunt.AllocationCount.resolvedStyle.backgroundColor.a, 0.9f, "The badge is filled.");
            StringAssert.Contains("Click the plaque", board.Hunt.Tab.tooltip);
            Assert.AreEqual("Allocate 1/1", board.Hunt.Caption);
            Assert.IsFalse(board.Console.Actions.Confirm.enabledSelf, "UI-172: the console's ✓ does not allocate.");
            StringAssert.Contains("In your Hunt allocation", board.OwnDice.Q<Button>(first).tooltip);
            StringAssert.DoesNotContain(first, board.OwnDice.Q<Button>(first).tooltip, "UI-97: the short description carries no die id.");
            Assert.IsFalse(board.OwnDice.Q<Button>(second).ClassListContains("hunt-candidate"), "At the limit no other die is offered.");
            StringAssert.Contains("limit", board.OwnDice.Q<Button>(second).tooltip);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(second));
            await room.Settle();
            Assert.IsTrue(board.OwnDice.Q<Button>(first).ClassListContains("hunt-allocated"));
            Assert.IsFalse(board.OwnDice.Q<Button>(second).ClassListContains("hunt-allocated"));
            Assert.AreEqual(0, room.Boundary.Commands);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(first));
            await room.Settle();
            Assert.AreEqual("Allocate 0/1", board.Hunt.Caption, "A second click on the die returns it.");
            Assert.IsTrue(board.Hunt.Tab.ClassListContains("hunt-confirm-waiting"));
            // UI-237: a die is chosen by a click only; dragging it to the plaque does nothing.
            await Drop(room, board.OwnDice.Q<Button>(second), board.Hunt.Tab);
            await room.Settle();
            Assert.AreEqual("Allocate 0/1", board.Hunt.Caption, "A dragged die is not allocated.");
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(second));
            await PlaqueReady(room, board);
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            await room.At(revision + 1);
            CollectionAssert.AreEqual(new[] { second }, room.Boundary.LastCommand.Payload.Ids);
            Assert.AreEqual(1, room.Boundary.Commands);
        }

        private static async Task Allocation(bool empty)
        {
            using var room = await UI10TestRoom.Connect(empty ? "ui34-hunt-zero" : "ui34-hunt-allocation", shadowBoundary: true);
            var client = room.Sh;
            var board = client.Board;
            var option = board.View.Options.Single(o => o.Kind == CommandKind.AllocateHunt);
            var before = board.View.Revision;
            var pan = board.Map.Camera.Pan;
            Assert.IsFalse(board.Hunt.IsOpen, "UI-162: the allocation opens no Hunt window.");
            Assert.IsFalse(room.Fp.Board.Hunt.IsOpen);
            Assert.AreSame(board.DecisionWindow.Dock, board.Tray.parent);
            Assert.IsFalse(board.DecisionWindow.IsOpen, "The allocation's choices are the dice and the Hunt Box.");
            Assert.IsTrue(board.Console.Hunt.Root.ClassListContains("hunt-allocating"));
            Assert.IsFalse(room.Fp.Board.Console.Hunt.Root.ClassListContains("hunt-allocating"));
            StringAssert.StartsWith("Allocate 0/", board.Hunt.Caption);
            Assert.AreEqual("Hunt", room.Fp.Board.Hunt.Tab.text, "Only the Shadow allocates.");
            Assert.AreEqual(0, option.Minimum);
            var chosen = option.EligibleIds.Take(2).ToArray();
            if (!empty)
            {
                // UI-88: each click adds a die to one set; nothing reaches the server before the plaque (UI-172).
                PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(chosen[0]));
                await room.Settle();
                StringAssert.StartsWith("Allocate 1/", board.Hunt.Caption);
                PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(chosen[1]));
                await room.Settle();
                StringAssert.StartsWith("Allocate 2/", board.Hunt.Caption);
                Assert.IsTrue(board.OwnDice.Q<Button>(chosen[1]).ClassListContains("hunt-allocated"));
                PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(chosen[0]));
                await room.Settle();
                StringAssert.StartsWith("Allocate 1/", board.Hunt.Caption, "A second click returns the die.");
                PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(chosen[1]));
                await room.Settle();
                StringAssert.StartsWith("Allocate 0/", board.Hunt.Caption, "The last die returns too.");
                PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(chosen[0]));
                await room.Settle();
                PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(chosen[1]));
                await room.Settle();
                StringAssert.StartsWith("Allocate 2/", board.Hunt.Caption);
                Assert.IsFalse(board.Hunt.IsOpen, "Choosing dice opens nothing.");
                Assert.AreEqual(0, room.Boundary.Commands);
                Assert.AreEqual(before, room.Fp.Board.View.Revision);
                Assert.IsTrue(room.Fp.Board.View.Dice.Where(d => chosen.Contains(d.Id)).All(d => d.Location == DieLocation.Unrolled));
                await client.Connection.Refresh();
                await room.Settle();
                StringAssert.StartsWith("Allocate 2/", board.Hunt.Caption);
                var connected = client.Connected;
                client.Connection.Reconnect();
                await room.Until(() => client.Connected > connected);
                await room.Settle();
                StringAssert.StartsWith("Allocate 2/", board.Hunt.Caption);
                CollectionAssert.AreEquivalent(chosen, board.OwnDice.Query<Button>(className: "hunt-allocated").ToList().Select(b => b.name));
            }
            await PlaqueReady(room, board);
            StringAssert.StartsWith("Allocate & roll", board.Hunt.Tab.tooltip);
            Assert.IsFalse(board.Console.Actions.Confirm.enabledSelf, "UI-172: the console's ✓ does not allocate.");
            if (!empty)
                await HuntRoleScreenshots.Capture(room, "allocation");
            room.Boundary.HoldNextCommand = true;
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            // UI-142: a quick second click on the confirmed plaque is ignored.
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            await room.Until(() => room.Boundary.CommandHeld);
            await room.At(before + 1);
            CollectionAssert.AreEquivalent(empty ? new string[0] : chosen, room.Boundary.LastCommand.Payload.Ids);
            Assert.IsEmpty(room.Boundary.LastCommand.Payload.DieId);
            Assert.AreEqual(CommandKind.AllocateHunt, room.Boundary.LastCommand.Kind);
            if (!empty)
                Assert.IsTrue(board.View.Dice.Where(d => chosen.Contains(d.Id)).All(d => d.Location == DieLocation.Hunt));
            await Recover(room, board);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(1, room.Boundary.Commands);
        }

        [UnityTest]
        public IEnumerator FailedSubsetStaysPrivateUntilRerollAndDamageRemainsPlayable() => MatchTestRoom.Wait(Reroll());

        private static async Task Reroll()
        {
            using var room = await UI10TestRoom.Connect("ui34-hunt-reroll", shadowBoundary: true);
            var board = room.Sh.Board;
            var before = board.View.Revision;
            var id = board.View.Options.Single(o => o.Kind == CommandKind.RerollHunt).EligibleIds.First();
            var die = board.Hunt.Root.Q<Button>("hunt-die-" + id);
            var publicRoll = JsonConvert.SerializeObject(room.Fp.Board.View.Hunt);
            room.Fp.Board.Hunt.Open();
            await room.Settle();
            Assert.AreEqual("Waiting for Shadow", room.Fp.Board.Hunt.Root.Q<Label>("hunt-owner").text);
            Assert.AreEqual("Hunt roll", room.Fp.Board.Hunt.Root.Q<Label>("hunt-step").text);
            Assert.AreEqual(board.Hunt.Root.Q<Label>("hunt-reroll-sources").text,
                room.Fp.Board.Hunt.Root.Q<Label>("hunt-reroll-sources").text);
            StringAssert.Contains("Shadow stronghold", board.Hunt.Root.Q<Label>("hunt-reroll-sources").text);
            Assert.AreEqual(DisplayStyle.None, room.Fp.Board.Hunt.Responses.Root.style.display.value);
            PointerGestureEvents.ClickAt(die);
            Assert.IsTrue(die.ClassListContains("selected"));
            PointerGestureEvents.ClickAt(die);
            Assert.IsFalse(die.ClassListContains("selected"));
            PointerGestureEvents.ClickAt(die);
            await room.Sh.Connection.Refresh();
            await room.Settle();
            Assert.AreSame(die, board.Hunt.Root.Q<Button>(die.name));
            Assert.IsTrue(die.ClassListContains("selected"));
            Assert.AreEqual(publicRoll, JsonConvert.SerializeObject(room.Fp.Board.View.Hunt));
            Assert.AreEqual(0, room.Boundary.Commands);
            Assert.IsNull(board.Tray.Q<Button>(die.name), "One set of reroll controls.");
            await Ready(room, board);
            Assert.AreEqual("Reroll selected", board.Tray.Q<Button>("confirm-decision").text);
            await HuntRoleScreenshots.Capture(room, "reroll");
            room.Boundary.HoldNextCommand = true;
            PointerGestureEvents.ClickAt(board.Tray.Q<Button>("confirm-decision"));
            await room.Until(() => room.Boundary.CommandHeld);
            await room.At(before + 1);
            Assert.IsTrue(board.View.Hunt.Dice.Single(d => d.Id == id).Rerolled);
            Assert.AreEqual(1, board.View.Hunt.Dice.Single(d => d.Id == id).First);
            CollectionAssert.AreEqual(new[] { id }, room.Boundary.LastCommand.Payload.Ids);
            await Recover(room, board);
            var fp = room.Fp.Board;
            Assert.IsTrue(fp.Hunt.IsOpen);
            Assert.AreEqual("Your decision · Free Peoples", fp.Hunt.Root.Q<Label>("hunt-owner").text);
            Assert.AreEqual("Waiting for Free Peoples", board.Hunt.Root.Q<Label>("hunt-owner").text);
            Assert.AreSame(fp.Hunt.DecisionHost, fp.Tray.parent);
            // UI-141: the drawn tile shows its artwork, and the pool list follows the server after the draw.
            Assert.IsNotNull(fp.View.Hunt.Face);
            Assert.AreEqual(DisplayStyle.Flex, fp.Hunt.Root.Q("hunt-tile-art").resolvedStyle.display);
            Assert.AreEqual(fp.View.Hunt.PoolCount, fp.View.HuntPool.Count);
            Assert.AreEqual(JsonConvert.SerializeObject(fp.View.HuntPool), JsonConvert.SerializeObject(board.View.HuntPool));
            PointerGestureEvents.ClickAt(fp.Hunt.PoolToggle);
            await room.Settle();
            StringAssert.StartsWith("In the pool · " + fp.View.HuntPool.Count, fp.Hunt.Pool.Root.Q<Label>("hunt-pool-heading").text);
            Assert.AreEqual(fp.View.HuntPool.Tiles.Count, fp.Hunt.Pool.Root.Q("hunt-pool-entries").childCount);
            PointerGestureEvents.ClickAt(fp.Hunt.PoolToggle);
            await room.Settle();
            // UI-142: the response's click applies it.
            PointerGestureEvents.ClickAt(fp.Hunt.Root.Q<Button>("hunt-response-Ring"));
            await room.At(before + 2);
            Assert.AreEqual(DecisionKind.FellowshipRoute, fp.View.Decision.Kind);
            Assert.AreSame(fp.Fellowship.Root, fp.Tray.parent);
            Assert.IsNotNull(fp.Hunt.Root.Q<Button>("hunt-tile"));
            Assert.AreEqual(1, room.Boundary.Commands);
        }

        [UnityTest]
        public IEnumerator KeepRollUsesNoRerollsAndRetainsTheCompletedMissReport() => MatchTestRoom.Wait(Keep());

        private static async Task Keep()
        {
            using var room = await UI10TestRoom.Connect("ui34-hunt-keep", shadowBoundary: true);
            var board = room.Sh.Board;
            var before = board.View.Revision;
            await Ready(room, board);
            Assert.AreEqual("Keep roll", board.Tray.Q<Button>("confirm-decision").text);
            PointerGestureEvents.ClickAt(board.Tray.Q<Button>("confirm-decision"));
            await room.At(before + 1);
            Assert.IsEmpty(room.Boundary.LastCommand.Payload.Ids);
            Assert.IsNull(board.View.Hunt);
            Assert.IsTrue(board.View.HuntReport.Completed);
            var reroll = board.View.HuntReport.Entries.Single(e => e.Kind == HuntReportKind.Rerolled).Hunt;
            Assert.IsFalse(reroll.Dice.Single().Rerolled);
            Assert.AreEqual(1, reroll.Dice.Single().Result);
            Assert.IsFalse(board.View.HuntReport.Entries.Any(e => e.Kind == HuntReportKind.TileDrawn));
            await Report(room);
        }

        [UnityTest]
        public IEnumerator AutomaticMissIsInspectableAfterReconnectWithoutAnyPendingHunt() => MatchTestRoom.Wait(Miss());

        [UnityTest]
        public IEnumerator ZeroDiceRetainsAnExplicitResultWithoutOpeningHunt() => MatchTestRoom.Wait(NoDice());

        private static async Task NoDice()
        {
            using var room = await UI10TestRoom.Connect("ui50-hunt-no-dice");
            Assert.IsNull(room.Fp.Board.View.Hunt);
            Assert.IsFalse(room.Fp.Board.Hunt.Pinned);
            Assert.IsFalse(room.Sh.Board.Hunt.Pinned);
            Assert.IsEmpty(room.Fp.Board.View.HuntReport.Entries.Single(e => e.Kind == HuntReportKind.Rolled).Hunt.Dice);
            // UI-109: the explicit zero-dice result stays in the shared report (above); the panel at rest is the
            // Hunt Box only, without the last roll.
            await Report(room);
            Assert.AreEqual(DisplayStyle.None, room.Fp.Board.Hunt.Root.Q("hunt-main").resolvedStyle.display);
        }

        private static async Task Miss()
        {
            using var room = await UI10TestRoom.Connect("ui34-hunt-miss");
            Assert.IsNull(room.Fp.Board.View.Hunt);
            Assert.IsFalse(room.Fp.Board.Hunt.Pinned);
            await Report(room);
        }

        private static async Task Report(UI10TestRoom room)
        {
            var board = room.Fp.Board;
            // UI-162: a window opened for a Hunt keeps its result, at the last stage, until it is closed.
            if (board.Hunt.IsOpen)
            {
                Assert.IsTrue(board.Hunt.Root.Q("hunt-stage-result").ClassListContains("current-stage"), "The result stays in the window.");
                Assert.IsFalse(board.Hunt.Root.ClassListContains("hunt-idle"));
                PointerGestureEvents.ClickAt(board.Hunt.Root.Q<Button>("close-hunt"));
                await room.Settle();
            }
            // UI-160: the Hunt plaque sits in the turn console and a click opens the window. UI-109: once the Hunt
            // is complete it is only the Hunt Box; the completed report stays in the history and in both projections.
            Assert.IsFalse(board.Hunt.IsOpen, "A completed Hunt waits under its plaque.");
            Assert.IsTrue(board.Console.Hunt.Root.Contains(board.Hunt.Tab), "The Hunt plaque lives in the turn console.");
            var projection = JsonConvert.SerializeObject(board.View);
            var commands = room.Boundary.Commands;
            var pan = board.Map.Camera.Pan;
            await ComponentGestures.Hover(board.Hunt.Tab);
            await room.Settle();
            Assert.IsFalse(board.Hunt.IsOpen, "Hovering the plaque only shows its tooltip.");
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            await room.Settle();
            Assert.IsTrue(board.Hunt.IsOpen, "A click on the plaque opens the Hunt panel.");
            AssertCompact(board);
            // UI-209: opened from its plaque, the window shows the tiles left in the pool; the header icon still folds it,
            // and the plaque unfolds it again in the open window.
            Assert.AreEqual(DisplayStyle.Flex, board.Hunt.Pool.Root.resolvedStyle.display, "The plaque opens the pool list.");
            Assert.IsTrue(board.Hunt.PoolToggle.ClassListContains("selected"));
            PointerGestureEvents.ClickAt(board.Hunt.PoolToggle);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.None, board.Hunt.Pool.Root.resolvedStyle.display, "The header icon folds the list.");
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Hunt.Pool.Root.resolvedStyle.display, "The plaque unfolds it in the open window.");
            Assert.IsTrue(board.View.HuntReport.Completed);
            Assert.AreEqual(JsonConvert.SerializeObject(board.View.HuntReport), JsonConvert.SerializeObject(room.Sh.Board.View.HuntReport));
            var saved = JsonConvert.SerializeObject(board.View.HuntReport);
            var connected = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > connected);
            await room.Settle();
            Assert.AreEqual(saved, JsonConvert.SerializeObject(board.View.HuntReport));
            PointerGestureEvents.ClickAt(board.Hunt.Root.Q<Button>("close-hunt"));
            await room.Settle();
            Assert.IsFalse(board.Hunt.IsOpen, "The close button rolls the panel back.");
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            await room.Settle();
            Assert.IsTrue(board.Hunt.IsOpen, "The panel opens again after reconnect.");
            AssertCompact(board);
            Assert.AreEqual(projection, JsonConvert.SerializeObject(board.View));
            Assert.AreEqual(commands, room.Boundary.Commands, "Reading a report never sends a command.");
            Assert.AreEqual(pan, board.Map.Camera.Pan);
        }

        private static void AssertCompact(BoardScreen board)
        {
            Assert.IsTrue(board.Hunt.Root.ClassListContains("hunt-idle"), "A completed Hunt leaves a compact panel.");
            Assert.AreEqual(DisplayStyle.Flex, board.Hunt.Box.Root.style.display.value);
            Assert.AreEqual(DisplayStyle.None, board.Hunt.Root.Q(className: "hunt-report-strip").resolvedStyle.display);
        }

        private static async Task PlaqueReady(UI10TestRoom room, BoardScreen board)
        {
            await room.Until(() => board.Hunt.Tab.ClassListContains("hunt-confirm-ready"));
            await room.Settle();
        }

        private static async Task Ready(UI10TestRoom room, BoardScreen board)
        {
            await room.Until(() => board.Tray.Q<Button>("confirm-decision")?.enabledInHierarchy == true);
            await room.Settle();
        }

        private static async Task Drop(UI10TestRoom room, VisualElement source, VisualElement target)
        {
            PointerGestureEvents.Press(source);
            await room.Settle();
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = target.worldBound.center }))
                source.SendEvent(move);
            await room.Settle();
            var picked = target.panel.Pick(target.worldBound.center);
            Assert.IsTrue(picked == target || target.Contains(picked), "The Hunt plaque must be a reachable drop target.");
            PointerGestureEvents.Release(target);
        }

        private static async Task Recover(UI10TestRoom room, BoardScreen board)
        {
            room.Boundary.ReleaseCommand();
            await room.Until(() => Recovery(board) != null);
            await room.Settle();
            PointerGestureEvents.ClickAt(Recovery(board));
            await room.Until(() => Recovery(board) == null);
            Assert.Greater(room.Boundary.ReceiptLookups, 0);
        }

        private static Button Recovery(BoardScreen board) => board.Tray.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result");
    }
}
