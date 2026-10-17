using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class HuntActivityNetworkTests
    {
        [UnityTest]
        public IEnumerator AutomaticNoDiceMissAndCancelledTileRemainInLogWithoutOpeningHunt() => Wait(AutomaticResults());

        [UnityTest]
        public IEnumerator ReactionCostsReadFailureAndNextIncrementKeepTheirActualActorsAndVersions() => Wait(Reaction());

        [UnityTest]
        public IEnumerator EarlierHuntStaysSeparateFromCurrentDecisionAfterReconnectAndReadRestart() => Wait(History());

        private static async Task AutomaticResults()
        {
            foreach (var scenario in new[] { "ui53-none", "ui53-miss", "ui53-cancelled" })
            {
                using var room = await Connect(scenario, ScenarioKind.Interaction, openPanels: false);
                await ActionNoticeNetworkTests.Loaded(room);
                var report = room.Sh.Board.View.HuntReport;
                Assert.IsTrue(report.Completed);
                Assert.IsNull(room.Sh.Board.View.Hunt);
                Assert.AreEqual(0, room.Fp.Activity.Model.StepCount, "An automatic own result is not an opponent action.");
                foreach (var client in new[] { room.Fp, room.Sh })
                {
                    var pan = client.Board.Map.Camera.Pan;
                    Assert.IsFalse(client.Board.Hunt.Pinned);
                    Assert.AreEqual("Hunt", client.Board.Hunt.Tab.text);
                    await client.Connection.Refresh();
                    Assert.IsFalse(client.Board.Hunt.Pinned);
                    Assert.AreEqual(pan, client.Board.Map.Camera.Pan);
                }
                await ActionNoticeNetworkTests.OpenLatest(room, room.Sh);
                var board = room.Sh.Board;
                Assert.IsFalse(board.Log.IsOpen);
                Assert.IsFalse(board.Hunt.Pinned);
                await ActionNoticeNetworkTests.OpenFullRecord(room, board);
                AssertArchive(board, report.Id, true);
                Assert.AreEqual("No tile drawn", board.Log.Card.HuntArchive.Root.Q<Label>("hunt-archive-tile").text);
                var rolled = board.Log.Card.HuntArchive.Displayed.Roll;
                Assert.IsNotNull(rolled);
                Assert.AreEqual(scenario == "ui53-none" ? 0 : 1, rolled.Dice.Count);
                Assert.AreEqual(scenario == "ui53-cancelled" ? 1 : 0, rolled.Dice.Count(d => d.Success));
                Assert.AreEqual(1, board.Log.Card.Displayed.Steps.SelectMany(s => s.Costs).Count(c => c.Kind == ActionCostKind.Die));
                if (scenario == "ui53-cancelled")
                    Assert.IsTrue(board.Log.Card.Displayed.Steps.SelectMany(s => s.Costs)
                        .Any(c => c.Kind == ActionCostKind.Card && c.Id == "base.fp.character.08"));
                await KingsCardNetworkChecks.Layout(room, room.Sh, scenario, "ui53");
                board.Log.CloseCard();
                board.Log.Close();
                Assert.IsFalse(board.Hunt.IsOpen, "An automatic completed Hunt must not unfold its panel.");
                await room.Settle();
                Assert.IsFalse(board.Hunt.IsOpen);
                Assert.AreEqual(report.Id, board.View.HuntReport.Id);
            }
        }

        private static async Task Reaction()
        {
            using var boundary = new HuntActivityBoundary();
            using var room = await Connect("ui53-reaction", ScenarioKind.Interaction, shHandler: boundary, openPanels: false);
            await ActionNoticeNetworkTests.Loaded(room);
            var fp = room.Fp.Board;
            var sh = room.Sh.Board;
            var chain = fp.View.HuntReport.Id;
            await ActionNoticeNetworkTests.OpenLatest(room, room.Fp);
            await ActionNoticeNetworkTests.OpenFullRecord(room, fp);
            AssertArchive(fp, chain, false);
            await HuntRoleScreenshots.CaptureCurrent(room.Fp, room.Settle, "history-reaction");
            var displayed = fp.Log.Card.Displayed;
            Assert.AreEqual(Side.FreePeoples, displayed.Action.Initiator);
            Assert.AreEqual(1, displayed.Action.Costs.Count(c => c.Kind == ActionCostKind.Die));
            var reaction = displayed.Steps.Single(s => s.Command == CommandKind.RespondEffect);
            Assert.AreEqual(Side.Shadow, reaction.Actor);
            Assert.IsTrue(reaction.Costs.Any(c => c.Kind == ActionCostKind.Card && c.Id == "base.sh.character.16"));
            var json = JsonConvert.SerializeObject(displayed);
            Assert.IsTrue(room.Sh.Board.View.Hand.All(id => !json.Contains(id)), "The other player's hand is absent from public facts.");
            Assert.IsTrue(fp.Log.Card.HuntArchive.Displayed.Roll.Dice.Single().Success);
            fp.Log.CloseCard();
            fp.Log.Close();
            var clicked = room.Sh.Activity.Model.Groups.Single().Latest;
            boundary.Fail = true;
            PointerGestureEvents.ClickAt(ActionNoticeNetworkTests.Notice(room.Sh));
            var retry = sh.Log.Popup.Q<Button>("action-card-retry");
            await room.Until(() => retry.resolvedStyle.display == DisplayStyle.Flex);
            Assert.AreEqual(clicked.StepId, room.Sh.Activity.Model.Groups.Single().Latest.StepId);
            boundary.Fail = false;
            boundary.Hold = true;
            await room.Settle();
            PointerGestureEvents.ClickAt(retry);
            await room.Until(() => boundary.Held);
            var revision = fp.View.Revision;
            PointerGestureEvents.ClickAt(fp.Notice.Button);
            await room.Settle();
            var ring = fp.Hunt.Responses.Root.Q<Button>("hunt-response-Ring");
            await room.Until(() => ring.enabledInHierarchy);
            // UI-142: the response's click applies it.
            PointerGestureEvents.ClickAt(ring);
            await room.At(revision + 1);
            await room.Until(() => room.Sh.Activity.Model.Groups.Single().Latest.StepId != clicked.StepId);
            var latest = room.Sh.Activity.Model.Groups.Single().Latest;
            boundary.Release();
            await room.Until(() => sh.ActionHistory.Context != null);
            await room.Settle();
            Assert.IsTrue(sh.Log.Card.Displayed.Steps.All(s => s.Revision <= clicked.Revision));
            Assert.AreEqual(latest.StepId, room.Sh.Activity.Model.Groups.Single().Latest.StepId);
            Assert.AreEqual(0, room.Fp.Activity.Model.StepCount, "Finishing our own Hunt does not create a red alert for us.");
            sh.Log.CloseCard();
            await ActionNoticeNetworkTests.OpenLatest(room, room.Sh);
            await ActionNoticeNetworkTests.OpenFullRecord(room, sh);
            AssertArchive(sh, chain, true);
            Assert.AreEqual(1, sh.Log.Card.HuntArchive.Displayed.Outcome.Corruption);
            Assert.AreEqual(1, sh.Log.Card.Displayed.Steps.SelectMany(s => s.Costs).Count(c => c.Kind == ActionCostKind.Die));
        }

        private static async Task History()
        {
            var root = Path.Combine(Path.GetTempPath(), "wotr-hunt-activity-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var room = await Connect("ui53-history", ScenarioKind.Interaction, readRoot: root, openPanels: false);
                await ActionNoticeNetworkTests.Loaded(room);
                var board = room.Fp.Board;
                var current = board.View.HuntReport.Id;
                var decision = board.View.Decision.Id;
                await ActionContextNetworkTests.OpenLog(room, board);
                var old = board.Log.Model.Rows.Where(r => r.Step == null && r.Group.Action.Command == CommandKind.MoveFellowship)
                    .Select(r => r.Group).Last();
                Assert.AreNotEqual(current, old.Action.Context.HuntChainId);
                await ActionContextNetworkTests.OpenEntry(room, board, old.Action.Id);
                AssertArchive(board, old.Action.Context.HuntChainId, true);
                Assert.AreEqual(current, board.View.HuntReport.Id);
                Assert.AreEqual(decision, board.View.Decision.Id);
                Assert.IsNotNull(board.Hunt.Responses.Root.Q<Button>("hunt-response-Ring"));
                board.Log.Close();
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                AssertArchive(board, old.Action.Context.HuntChainId, true);
                board.Log.CloseCard();
                board.Hunt.Close();
                PointerGestureEvents.ClickAt(board.Notice.Button);
                await room.Settle();
                Assert.IsTrue(board.Hunt.IsOpen);
                Assert.AreEqual(decision, board.View.Decision.Id);
                await ActionNoticeNetworkTests.OpenLatest(room, room.Fp);
                await room.Fp.Activity.LocalWrites;
                await room.RestartFreePeoples(root);
                Assert.AreEqual(0, room.Fp.Activity.Model.StepCount);
                Assert.AreEqual(decision, room.Fp.Board.View.Decision.Id);
                Assert.IsTrue(room.Fp.Board.Hunt.IsOpen, "The current own decision remains independently available after restart.");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        private static void AssertArchive(BoardScreen board, string chain, bool complete)
        {
            Assert.AreEqual(chain, board.ActionHistory.Context.HuntChainId);
            Assert.AreEqual(chain, board.Log.Card.HuntArchive.Displayed.ChainId);
            Assert.AreEqual(complete, board.Log.Card.HuntArchive.Displayed.Completed);
            Assert.AreEqual(DisplayStyle.Flex, board.Log.Card.HuntArchive.Root.style.display.value);
            Assert.IsEmpty(board.Log.Card.HuntArchive.Root.Query<Button>().ToList());
        }
    }
}
