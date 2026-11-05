using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class BattleReportNetworkTests
    {
        [UnityTest]
        public IEnumerator ClosedReportRejectsLateHttpAndFailedReadCanRetry() => Wait(Read());

        private static async Task Read()
        {
            using var boundary = new ActionNoticeBoundary();
            using var shadowBoundary = new ActionNoticeBoundary();
            using var room = await Connect("ui46-report", ScenarioKind.BaseAcceptance, fpHandler: boundary, shHandler: shadowBoundary);
            var board = room.Fp.Board;
            var report = board.BattleReport;
            var revision = board.View.Revision;
            boundary.HoldBattle = true;
            PointerGestureEvents.ClickAt(report.Open);
            await room.Until(() => boundary.BattleHeld);
            await room.Settle();
            PointerGestureEvents.ClickAt(report.Root.Q<Button>("close-battle-report"));
            boundary.Release();
            await room.Until(() => boundary.BattleDelivered);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.None, report.Root.resolvedStyle.display);
            Assert.IsNull(report.Displayed, "The late response cannot replace a closed report.");

            // UI-112: the ⚔ hides once its round has been shown, so the failed read is the other side's own unread ⚔.
            var shadowReport = room.Sh.Board.BattleReport;
            shadowBoundary.FailBattle = true;
            PointerGestureEvents.ClickAt(shadowReport.Open);
            var retry = shadowReport.Root.Q<Button>("retry-battle-report");
            await room.Until(() => retry.resolvedStyle.display == DisplayStyle.Flex);
            await room.Settle();
            Assert.IsNull(shadowReport.Displayed);
            shadowBoundary.FailBattle = false;
            PointerGestureEvents.ClickAt(retry);
            await room.Until(() => shadowReport.Displayed != null);
            Assert.AreEqual(2, shadowReport.Displayed.Rounds.Count);
            Assert.AreEqual(revision, board.View.Revision);
            Assert.AreEqual(revision, room.Sh.Board.View.Revision);
        }
    }
}
