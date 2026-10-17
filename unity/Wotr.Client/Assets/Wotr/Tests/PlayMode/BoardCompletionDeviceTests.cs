using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using static Wotr.PlayTests.BoardCompletionEntryChecks;

namespace Wotr.PlayTests
{
    public sealed partial class BoardCompletionContextNetworkTests
    {
        [UnityTest]
        public IEnumerator SecondDeviceEnteringTheChieftainInvalidatesTheOldCapturedDestinationAndExactCost() => Wait(EntryDevices());

        private static async Task EntryDevices()
        {
            using var boundary = new UI10HttpBoundary();
            using var room = await Connect("ui71-council-device", ScenarioKind.Interaction, shHandler: boundary, openPanels: false);
            using var second = await room.AnotherDevice(room.Sh, null);
            boundary.MatchPath = "/api/matches/" + room.Sh.Board.View.MatchId;
            var die = Choice(room.Sh.Board).Dice.Last();
            var revision = room.Sh.Board.View.Revision;
            var fpBefore = JsonConvert.SerializeObject(room.Fp.Board.View);
            await Prepare(room.Sh.Board, die, room.Token);
            await Prepare(second.Board, die, room.Token);
            Assert.AreEqual(fpBefore, JsonConvert.SerializeObject(room.Fp.Board.View));
            Assert.AreEqual(0, boundary.Commands);
            var staleTarget = await Target(room.Sh.Board, room.Token);
            MapContourProbe.Press(room.Sh.Board.Map, staleTarget);
            await room.Settle();
            Assert.AreEqual(0, boundary.Commands, "Pressing a terrain target does not submit before release.");
            Assert.IsTrue(room.Sh.Board.Map.HasPointerCapture(PointerId.mousePointerId));
            var target = await Target(second.Board, room.Token);
            MapContourProbe.Click(second.Board.Map, target);
            await room.At(revision + 1);
            await room.Until(() => second.Board.View.Revision == revision + 1);
            Assert.AreEqual(1, boundary.Commands);
            var command = boundary.LastCommand;
            Assert.AreEqual(die, command.Payload.DieId);
            Assert.AreEqual(Council, command.Payload.CouncilTokenId);
            Assert.IsEmpty(room.Sh.Board.ActionDie.DieId, "A spent cost must not be replaced after the other device commits.");
            Assert.IsFalse(room.Sh.Board.Map.HasPointerCapture(PointerId.mousePointerId));
            MapContourProbe.Release(room.Sh.Board.Map, staleTarget);
            await room.Settle();
            Assert.AreEqual(1, boundary.Commands, "The old release must not send another character entry.");
            Entered(room.Sh.Board.View, command);
            Entered(room.Fp.Board.View, command);
            Entered(second.Board.View, command);
            Assert.IsNull(room.Sh.Board.View.Decision);
            Assert.AreEqual(DisplayStyle.None, room.Sh.Board.Notice.Button.resolvedStyle.display);
        }
    }
}
