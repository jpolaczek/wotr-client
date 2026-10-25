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
    public sealed class HuntRecoveryNetworkTests
    {
        private const string Mithril = "base.fp.character.05";

        [UnityTest]
        public IEnumerator LostReplacementReceiptKeepsOneTileCardAndPaidContinuation() =>
            MatchTestRoom.Wait(Replacement());

        private static async Task Replacement()
        {
            using var boundary = new UI10HttpBoundary();
            using var room = await MatchTestRoom.Connect("ui54-hunt-protection", ScenarioKind.CardPack,
                fpHandler: boundary, openPanels: false);
            var board = room.Fp.Board;
            boundary.MatchPath = "/api/matches/" + board.View.MatchId;
            var revision = board.View.Revision;
            var cost = board.View.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
            var discard = board.View.Players.Single(p => p.Side == Side.FreePeoples).DiscardCount;
            var report = board.View.HuntReport.Id;
            Assert.AreEqual("hunt.replace", board.View.Decision.Window);
            Assert.IsTrue(board.View.Hunt.DamagePending);
            Assert.IsNull(board.View.Hunt.TileDie);
            await ClosedReconnect(room);
            Assert.IsTrue(board.Hunt.IsOpen, "The console's status reopens the Hunt.");
            await room.Settle();
            await CaptureRoles(room, "replacement");

            boundary.ArmCommandLoss();
            await Respond(room, "card.mithril-replace");
            await room.Until(() => boundary.CommandHeld);
            await room.At(revision + 1);
            Assert.AreEqual(CommandKind.RespondEffect, boundary.LastCommand.Kind);
            Assert.AreEqual("card.mithril-replace", boundary.LastCommand.Payload.ReactionId);
            AssertReplacement(room, cost, report, discard);
            var completedReport = JsonConvert.SerializeObject(board.View.HuntReport);
            var command = boundary.LastCommand.CommandId;
            boundary.ReleaseCommand();
            await room.Until(() => Recovery(board) != null);
            await room.Settle();
            PointerGestureEvents.ClickAt(Recovery(board));
            await room.Until(() => Recovery(board) == null && board.Tray.enabledInHierarchy);
            await room.Settle();
            Assert.Greater(boundary.ReceiptLookups, 0);
            Assert.AreEqual(1, boundary.Commands);
            Assert.AreEqual(completedReport, JsonConvert.SerializeObject(board.View.HuntReport));
            var receipt = await room.Fp.Api.Get<CommandReceipt>(
                $"api/matches/{board.View.MatchId}/receipts/{command}", room.Token);
            Assert.IsTrue(receipt.Accepted);
            Assert.AreEqual(revision + 1, receipt.Revision);
            await ClosedReconnect(room);
            AssertReplacement(room, cost, report, discard);
            await ActionNoticeNetworkTests.Loaded(room);
            await room.Until(() => room.Sh.Activity.Model.Groups.Any(group => group.Latest.Text.Contains("Hunt tile · Die")));

            Assert.IsTrue(board.Hunt.IsOpen, "The console's status reopens the Hunt.");
            await room.Settle();
            await CaptureRoles(room, "protection");
            await Respond(room, "card.axe-and-bow");
            await room.At(revision + 2);
            // UI-142: the response's click applies it.
            PointerGestureEvents.ClickAt(board.Hunt.Root.Q<Button>("hunt-response-Ring"));
            await room.At(revision + 3);
            Assert.IsNull(board.View.Hunt);
            Assert.AreEqual(2, board.View.Fellowship.Corruption);
            Assert.AreEqual(1, board.View.Fellowship.MordorStep);
            Assert.AreEqual(DieLocation.Hunt, board.View.Dice.Single(d => d.Id == cost).Location);
            Assert.AreEqual(discard + 2, board.View.Players.Single(p => p.Side == Side.FreePeoples).DiscardCount);
            Assert.IsTrue(board.View.HuntReport.Completed);
            Assert.AreEqual(1, board.View.HuntReport.Entries.Count(e => e.Kind == HuntReportKind.TileReplaced));
            Assert.AreEqual(3, boundary.Commands);
            AssertPublicReport(room);
        }

        private static void AssertReplacement(MatchTestRoom room, string cost, string report, int discard)
        {
            var view = room.Fp.Board.View;
            Assert.AreEqual("hunt.table", view.Decision.Window);
            Assert.AreEqual("base.hunt.standard.3.0.1", view.Hunt.TileId);
            Assert.AreEqual(3, view.Hunt.Damage);
            Assert.IsFalse(view.Hunt.DamagePending);
            Assert.IsNull(view.Hunt.TileDie, "Shelob was replaced before its die was rolled.");
            Assert.AreEqual(DieLocation.Resolving, view.Dice.Single(d => d.Id == cost).Location);
            Assert.AreEqual(report, view.HuntReport.Id);
            Assert.AreEqual(discard + 1, view.Players.Single(p => p.Side == Side.FreePeoples).DiscardCount);
            Assert.IsFalse(view.Players.Single(p => p.Side == Side.FreePeoples).TableCards.Contains(Mithril));
            Assert.AreEqual(1, view.HuntReport.Entries.Count(e => e.Kind == HuntReportKind.TileReplaced));
            Assert.IsNull(room.Sh.Board.View.Decision);
            Assert.IsEmpty(room.Sh.Board.View.Options);
            AssertPublicReport(room);
        }

        private static async Task ClosedReconnect(MatchTestRoom room)
        {
            var board = room.Fp.Board;
            board.Hunt.Close();
            await room.Fp.Connection.Refresh();
            Assert.IsFalse(board.Hunt.Pinned);
            var connected = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > connected);
            await room.Settle();
            Assert.IsFalse(board.Hunt.Pinned, "The same decision cannot undo Close.");
            // UI-162: a closed Hunt window stays closed within its Hunt; the console's status brings it back.
            Assert.IsFalse(board.Hunt.IsOpen, "A closed Hunt window stays closed within its Hunt.");
            PointerGestureEvents.ClickAt(board.Console.Status.Plaque);
            await room.Settle();
        }

        private static async Task Respond(MatchTestRoom room, string reaction)
        {
            // UI-285: a table card's response is a card object in the Hunt window's response row.
            Button Find() => MatchTestRoom.HuntCard(room.Fp, reaction) ?? room.Fp.Board.Tray.Q<Button>("effect-choice-" + reaction);
            await room.Until(() => Find()?.enabledInHierarchy == true);
            await room.Settle();
            var button = Find();
            var picked = button.panel.Pick(button.worldBound.center);
            Assert.IsTrue(picked == button || button.Contains(picked), "The reaction must be reachable by a real click.");
            PointerGestureEvents.ClickAt(button);
        }

        private static async Task CaptureRoles(MatchTestRoom room, string stage)
        {
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                client.Board.Hunt.Open();
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        try
                        {
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            await room.Settle();
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            var panel = client.Board.Hunt.Root.worldBound;
                            Assert.LessOrEqual(panel.yMax, client.Board.Map.worldBound.yMax + 1);
                            Assert.LessOrEqual(panel.xMax, client.Board.Root.worldBound.xMax + 1);
                            Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                            LayoutTests.Save(target, $"ui54-{stage}-{client.Board.View.You}-{width}-{scale:0.00}.png");
                        }
                        finally
                        {
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
                    }
                client.Panel.scale = 1f;
            }
            await room.Settle();
        }

        private static Button Recovery(BoardScreen board) =>
            board.Tray.Query<Button>().ToList().FirstOrDefault(button => button.text == "Check result");

        private static void AssertPublicReport(MatchTestRoom room) => Assert.AreEqual(
            JsonConvert.SerializeObject(room.Fp.Board.View.HuntReport),
            JsonConvert.SerializeObject(room.Sh.Board.View.HuntReport));
    }
}
