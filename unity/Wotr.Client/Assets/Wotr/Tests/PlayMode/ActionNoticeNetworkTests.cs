using System;
using System.Collections;
using System.IO;
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
    public sealed class ActionNoticeNetworkTests
    {
        [UnityTest]
        public IEnumerator SilentHoverPrivatePolicyExplicitReadAndRestartKeepIndependentReadState() => Wait(Lifecycle());
        [UnityTest]
        public IEnumerator NewStepDuringReadStaysUnreadAndFailedBattleReadRequiresRetry() => Wait(Delayed());

        private static async Task Lifecycle()
        {
            var root = Path.Combine(Path.GetTempPath(), "wotr-notice-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var room = await Connect("ui43-battle", ScenarioKind.BaseAcceptance, readRoot: root, openPanels: false);
                await Loaded(room);
                var fp = room.Fp.Board;
                var sh = room.Sh.Board;
                var pan = sh.Map.Camera.Pan;
                await room.Attack(room.Fp, "minas-tirith", "lossarnach");
                await room.Until(() => room.Sh.Activity.Model.StepCount > 0);
                Assert.IsTrue(fp.Battles.IsOpen, "UI-149: the new battle opens its window for both players.");
                Assert.IsTrue(sh.Battles.IsOpen);
                Assert.AreEqual(pan, sh.Map.Camera.Pan);
                StringAssert.Contains("Choose a Combat card", fp.Notice.Button.text);
                var count = room.Sh.Activity.Model.StepCount;
                var notice = Notice(room.Sh);
                Hover(notice);
                notice.Focus();
                await Task.Delay(300, room.Token);
                Assert.AreEqual(DisplayStyle.None, sh.Log.Popup.resolvedStyle.display);
                Assert.IsFalse(sh.Log.Pinned);
                Assert.AreEqual(count, room.Sh.Activity.Model.StepCount);
                Assert.AreEqual(pan, sh.Map.Camera.Pan);
                sh.Log.CloseCard();
                var opponentBefore = room.Fp.Activity.Model.StepCount;
                PointerGestureEvents.ClickAt(sh.BattlePolicy.Toggle);
                await room.Until(() => sh.View.AutoDecision.Version == 1);
                await room.Settle();
                Assert.AreEqual(opponentBefore, room.Fp.Activity.Model.StepCount, "Private preference is not an opponent action.");
                Assert.AreEqual(1, fp.View.Revision);
                PointerGestureEvents.ClickAt(sh.BattlePolicy.Toggle);
                await room.Until(() => sh.View.AutoDecision.Version == 2);
                await OpenLatest(room, room.Sh);
                Assert.IsFalse(sh.Log.IsOpen);
                sh.Log.CloseCard();
                PointerGestureEvents.ClickAt(fp.Notice.Button);
                await room.Settle();
                Assert.IsTrue(fp.Battles.IsOpen);
                fp.Battles.Close();
                await room.Settle();
                Assert.AreEqual(DisplayStyle.Flex, fp.Notice.Button.style.display.value);
                await NoCard(room, room.Fp);
                await room.Until(() => room.Sh.Activity.Model.StepCount > 0);
                await NoCard(room, room.Sh);
                await room.Until(() => room.Fp.Activity.Model.StepCount > 0);
                var revision = fp.View.Revision;
                var before = await room.Fp.Api.Get<MatchActivityView>(PathFor(fp, "/activity"), room.Token);
                await OpenLatest(room, room.Fp);
                await room.Fp.Activity.LocalWrites;
                var after = await room.Fp.Api.Get<MatchActivityView>(PathFor(fp, "/activity"), room.Token);
                Assert.AreEqual(before.LastSeenRevision, after.LastSeenRevision);
                Assert.AreEqual(revision, fp.View.Revision);
                var connected = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                await room.Settle();
                Assert.AreEqual(0, room.Fp.Activity.Model.StepCount);
                await room.RestartFreePeoples(root);
                Assert.AreEqual(0, room.Fp.Activity.Model.StepCount);
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                using var other = await room.AnotherDevice(room.Fp, Path.Combine(root, "other-device"));
                Assert.Greater(other.Activity.Model.StepCount, 0, "A different device has independent read markers.");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        private static async Task Delayed()
        {
            using var boundary = new ActionNoticeBoundary();
            using var room = await Connect("ui43-stale", ScenarioKind.BaseAcceptance, shHandler: boundary, openPanels: false);
            await Loaded(room);
            await room.Attack(room.Fp, "minas-tirith", "lossarnach");
            await room.Until(() => room.Sh.Activity.Model.StepCount > 0);
            var board = room.Sh.Board;
            var clicked = room.Sh.Activity.Model.Groups.Single().Latest.StepId;
            var clickedRevision = room.Sh.Activity.Model.Groups.Single().Latest.Revision;
            boundary.HoldAction = true;
            PointerGestureEvents.ClickAt(Notice(room.Sh));
            await room.Until(() => boundary.ActionHeld);
            await NoCard(room, room.Fp);
            await room.Until(() => room.Sh.Activity.Model.Groups.Single().Latest.StepId != clicked);
            var newest = room.Sh.Activity.Model.Groups.Single().Latest.StepId;
            var newestRevision = room.Sh.Activity.Model.Groups.Single().Latest.Revision;
            boundary.Release();
            await room.Until(() => board.ActionHistory.Context != null);
            await room.Settle();
            Assert.IsTrue(board.Log.Card.Displayed.Steps.Any(s => s.Id == clicked));
            Assert.IsTrue(board.Log.Card.Displayed.Steps.All(s => s.Revision <= clickedRevision));
            Assert.AreEqual(newest, room.Sh.Activity.Model.Groups.Single().Latest.StepId);
            Assert.Greater(room.Sh.Activity.Model.StepCount, 0);
            Assert.IsFalse(board.Log.IsOpen);
            board.Log.CloseCard();
            boundary.FailBattle = true;
            PointerGestureEvents.ClickAt(Notice(room.Sh));
            var retry = board.Log.Popup.Q<Button>("action-card-retry");
            await room.Until(() => retry.resolvedStyle.display == DisplayStyle.Flex);
            await room.Settle();
            Assert.Greater(room.Sh.Activity.Model.StepCount, 0, "Incomplete details cannot acknowledge the update.");
            var revision = board.View.Revision;
            boundary.FailBattle = false;
            PointerGestureEvents.ClickAt(retry);
            await room.Until(() => room.Sh.Activity.Model.StepCount == 0);
            Assert.IsTrue(board.Log.Card.Displayed.Steps.Any(s => s.Id == newest));
            Assert.IsTrue(board.Log.Card.Displayed.Steps.All(s => s.Revision <= newestRevision));
            Assert.AreEqual(revision, board.View.Revision);
        }

        internal static async Task Loaded(MatchTestRoom room) =>
            await room.Until(() => room.Fp.Activity.Loaded && room.Sh.Activity.Loaded);

        internal static Button Notice(MountedMatchClient client)
        {
            var id = client.Activity.Model.Groups.First().Latest.Id;
            return client.Board.Notifications.Root.Q<Button>("action-notice-" + id);
        }

        internal static async Task OpenLatest(MatchTestRoom room, MountedMatchClient client)
        {
            var group = client.Activity.Model.Groups.First();
            await room.Settle();
            PointerGestureEvents.ClickAt(Notice(client));
            await room.Until(() => !client.Activity.Model.Groups.Any(g => g.Latest.Id == group.Latest.Id));
            await room.Settle();
        }

        // UI-166: a notice opens the compact result; the steps, costs and archives are in its full record.
        internal static async Task OpenFullRecord(MatchTestRoom room, BoardScreen board)
        {
            var action = board.Log.SelectedId;
            PointerGestureEvents.ClickAt(board.Log.Popup.Q<Button>("action-result-full"));
            await room.Until(() => board.Log.Card.Displayed?.Action.Id == action && !board.Log.Card.Compact &&
                board.ActionHistory.Context != null);
            await room.Settle();
        }

        private static async Task NoCard(MatchTestRoom room, MountedMatchClient client)
        {
            var revision = client.Board.View.Revision;
            PointerGestureEvents.ClickAt(client.Board.Notice.Button);
            await room.Settle();
            PointerGestureEvents.ClickAt(client.Board.Tray.Q<Button>("battle-no-card"));
            await room.At(revision + 1);
        }

        internal static void Hover(VisualElement element)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = element.worldBound.center });
            element.panel.visualTree.SendEvent(move);
        }
        private static string PathFor(BoardScreen board, string suffix) => "api/matches/" + board.View.MatchId + suffix;
    }
}
