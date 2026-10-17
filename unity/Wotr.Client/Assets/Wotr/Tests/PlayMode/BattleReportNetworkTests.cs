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
            using var room = await Connect("ui46-report", ScenarioKind.BaseAcceptance, fpHandler: boundary);
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

            boundary.FailBattle = true;
            PointerGestureEvents.ClickAt(report.Open);
            var retry = report.Root.Q<Button>("retry-battle-report");
            await room.Until(() => retry.resolvedStyle.display == DisplayStyle.Flex);
            await room.Settle();
            Assert.IsNull(report.Displayed);
            boundary.FailBattle = false;
            PointerGestureEvents.ClickAt(retry);
            await room.Until(() => report.Displayed != null);
            Assert.AreEqual(2, report.Displayed.Rounds.Count);
            Assert.AreEqual(revision, board.View.Revision);
            Assert.AreEqual(revision, room.Sh.Board.View.Revision);
        }
    }
}
