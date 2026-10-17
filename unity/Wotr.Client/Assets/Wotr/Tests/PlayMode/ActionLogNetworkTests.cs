using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class ActionLogNetworkTests
    {
        [UnityTest]
        public IEnumerator CompleteHistoryPagesAndFiltersWorkForBothPlayers() => Wait(Pages());

        [UnityTest]
        public IEnumerator NewStepPreservesAnchorAndPinnedVersionWithoutChangingDraft() => Wait(Increment());

        private static async Task Pages()
        {
            using var room = await Connect("ui41-history", ScenarioKind.BaseAcceptance);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var log = board.Log;
                var revision = board.View.Revision;
                var pan = board.Map.Camera.Pan;
                var zoom = board.Map.Camera.Zoom;
                var selection = board.Map.Selected;
                var displayed = 0;
                log.ExplicitlyDisplayed += _ => displayed++;
                PointerGestureEvents.ClickAt(log.Toggle);
                await room.Until(() => log.Model.Loaded);
                await room.Settle();
                while (log.Model.Older.Length > 0)
                {
                    var previous = log.Model.Older;
                    await room.Until(() => log.Root.Q<Button>("log-older").enabledSelf);
                    PointerGestureEvents.ClickAt(log.Root.Q<Button>("log-older"));
                    await room.Until(() => log.Model.Older != previous);
                    await room.Settle();
                }
                Assert.Greater(log.Model.StepCount, 120);
                Assert.Less(log.List.RenderedCount, 12);
                Assert.AreEqual(0, displayed);
                var first = log.Model.Rows[0].Group;
                var entry = log.Root.Q<Button>("log-open-" + first.Action.Id);
                Point(entry);
                await Task.Delay(300, room.Token);
                Assert.IsEmpty(log.SelectedId);
                Assert.AreEqual(DisplayStyle.None, log.Popup.resolvedStyle.display);
                Point(board.Phase);
                entry.Focus();
                await Task.Delay(300, room.Token);
                Assert.IsEmpty(log.SelectedId);
                Assert.AreEqual(0, displayed, "Focus and hover never load or acknowledge a version.");
                Assert.AreEqual(pan, board.Map.Camera.Pan, "Hover and keyboard focus cannot navigate the map.");
                Assert.AreEqual(zoom, board.Map.Camera.Zoom);
                PointerGestureEvents.ClickAt(entry);
                await room.Until(() => displayed == 1);
                await room.Settle();
                Assert.IsTrue(log.Pinned);
                Assert.AreEqual(first.Action.Version, log.Card.Displayed.Action.Version);
                Assert.AreEqual(revision, board.View.Revision);
                Assert.AreEqual(selection, board.Map.Selected, "Reading a recorded action preserves the active map selection.");
                if (board.ActionHistory.Context.Regions.Count == 0)
                {
                    Assert.AreEqual(pan, board.Map.Camera.Pan);
                    Assert.AreEqual(zoom, board.Map.Camera.Zoom);
                }
                log.CloseCard();
                foreach (var filter in new[] { ActionLogFilter.You, ActionLogFilter.Opponent, ActionLogFilter.All })
                {
                    PointerGestureEvents.ClickAt(log.Root.Q<Button>("log-filter-" + filter));
                    await room.Until(() => log.Model.Loaded);
                    await room.Settle();
                    if (filter != ActionLogFilter.All)
                        Assert.IsTrue(log.Model.Rows.Where(r => r.Step == null).SelectMany(r => r.Group.Steps)
                            .All(s => s.Actor == (filter == ActionLogFilter.You ? board.View.You : board.View.You == Side.FreePeoples ? Side.Shadow : Side.FreePeoples)));
                }
                log.List.Root.scrollOffset = new Vector2(0, 700);
                await room.Settle();
                var anchor = log.Model.Anchor;
                var offset = log.List.Root.scrollOffset.y;
                PointerGestureEvents.ClickAt(log.Root.Q<Button>("close-log"));
                PointerGestureEvents.ClickAt(log.Toggle);
                await room.Settle();
                Assert.AreEqual(anchor, log.Model.Anchor);
                Assert.That(log.List.Root.scrollOffset.y, Is.EqualTo(offset).Within(1));
                log.Close();
            }
        }

        private static async Task Increment()
        {
            using var room = await Connect("ui47-log-increment", ScenarioKind.BaseAcceptance);
            var board = room.Fp.Board;
            var die = ArmyGestures.ArmyDie(room.Fp);
            ArmyGestures.Die(room.Fp, die);
            await ArmyGestures.All(room.Fp, "minas-tirith");
            await ArmyGestures.Target(room, room.Fp, "osgiliath");
            var log = board.Log;
            PointerGestureEvents.ClickAt(log.Toggle);
            await room.Until(() => log.Model.Loaded);
            await room.Settle();
            var action = log.Model.Rows.First(r => r.Group.Action.Command == CommandKind.MoveArmies).Group;
            var displayed = 0;
            log.ExplicitlyDisplayed += _ => displayed++;
            PointerGestureEvents.ClickAt(log.Root.Q<Button>("log-open-" + action.Action.Id));
            await room.Until(() => displayed == 1);
            var version = log.Card.Displayed.Action.Version;
            var selectedDie = board.ActionDie.DieId;
            var anchor = log.Model.Anchor;
            var pan = board.Map.Camera.Pan;
            using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return }))
                log.Popup.Q<Button>("close-action-summary").SendEvent(key);
            await room.Settle();
            Assert.AreEqual(1, board.View.Revision, "Enter while reading must not confirm a draft.");
            Assert.AreEqual(selectedDie, board.ActionDie.DieId);
            var stepCount = log.Model.StepCount;
            await ArmyGestures.Finish(room, room.Fp);
            await room.Until(() => log.Model.StepCount > stepCount);
            await room.Settle();
            Assert.AreEqual(anchor, log.Model.Anchor);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(version, log.Card.Displayed.Action.Version, "An open historical card remains the version acknowledged.");
            Assert.AreEqual(1, displayed);
            Assert.IsTrue(log.Model.HasNew);
            Assert.AreEqual(DecisionKind.RemoveExcess, board.View.Decision.Kind);
            Assert.AreEqual(ActionLogStatus.InProgress, log.Model.Find(action.Action.Id).Action.Status,
                "Finish does not settle the die while the army still exceeds its limit.");
            await room.RemoveExcess(room.Fp);
            await room.Until(() => log.Model.Find(action.Action.Id).Action.Status == ActionLogStatus.Completed);
            var connected = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > connected);
            await room.Settle();
            Assert.AreEqual(version, log.Card.Displayed.Action.Version);
            log.CloseCard();
            PointerGestureEvents.ClickAt(log.Root.Q<Button>("log-new-entries"));
            await room.Settle();
            Assert.That(log.List.Root.scrollOffset.y, Is.EqualTo(0).Within(1));
            Assert.IsFalse(log.Model.HasNew);
        }

        private static void Point(VisualElement target)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = target.worldBound.center });
            target.SendEvent(move);
        }
    }
}
