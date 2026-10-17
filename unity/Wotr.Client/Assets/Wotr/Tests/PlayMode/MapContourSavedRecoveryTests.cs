using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    public sealed partial class MapContourRecoveryTests
    {
        [UnityTest]
        public IEnumerator SwitchingSavedGamesRestoresBaseMetadataForBothRoles() => MatchTestRoom.Wait(SwitchGames());

        [UnityTest]
        public IEnumerator HistoricalPaidStepKeepsItsDraftAndRecoversUncertainCompletionAfterServerRestart() =>
            MatchTestRoom.Wait(Historical());

        private static async Task SwitchGames()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-map-switch-" + Guid.NewGuid().ToString("N"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
            try
            {
                foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                {
                    var api = await fixture.Login(side, timeout.Token);
                    using var client = new MountedFlowClient(api, storage);
                    object geometry = null;
                    foreach (var name in new[] { "newcities", "base", "whitehand", "base" })
                    {
                        await Open(client, fixture.ReturnMatches["map16-" + name], timeout.Token);
                        AssertRegions(client.Board, name);
                        geometry ??= client.Board.Presentation.Geometry;
                        Assert.AreSame(geometry, client.Board.Presentation.Geometry,
                            "Shared contours remain reusable while each match has independent metadata.");
                        var revision = client.Board.View.Revision;
                        client.Board.Map.Fit();
                        await Task.Delay(100, timeout.Token);
                        LayoutTests.Save(client.Panel.targetTexture, $"map16-switch-{side}-{name}.png");
                        Assert.AreEqual(revision, (await api.Get<PlayerView>(
                            "api/matches/" + client.Board.View.MatchId, timeout.Token)).Revision);
                        await ClickReady(client.Root, "back-to-games", timeout.Token);
                        await Until(() => client.Board == null, timeout.Token);
                    }
                    client.Flow.Dispose();
                    await client.LocalWrites;
                }
            }
            finally { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
        }

        private static async Task Historical()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-map-history-" + Guid.NewGuid().ToString("N"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            using var server = new RestartTestServer();
            var token = timeout.Token;
            var match = fixture.ReturnMatches["map16-paid"];
            try
            {
                await server.Start(token);
                var history = await SaveHistoricalDraft(fixture, server.Address, storage, match, token);
                var uncertain = await CompleteHistoricalStep(fixture, server.Address, storage, match, history, token);
                server.Stop();
                await server.Start(token);
                await RecoverHistoricalCompletion(fixture, server.Address, storage, match, history, uncertain, token);
            }
            finally { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
        }

        private static async Task<HistoricalState> SaveHistoricalDraft(ReturnTestFixture fixture, string address,
            string storage, string match, CancellationToken token)
        {
            var api = await fixture.Login(Side.FreePeoples, token, address: address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            var view = client.Board.View;
            Assert.AreEqual(1, view.Revision, "The committed historical state starts after its first paid movement step.");
            AssertPaid(view);
            var army = view.Options.Single(o => o.Kind == CommandKind.MoveArmies).Armies
                .First(a => a.UnitIds.Count > 0 && a.Targets.Count > 0);
            client.Board.Hover.Open(army.RegionId);
            await Task.Delay(100, token);
            await ClickReady(client.Root, "hover-figure-" + army.UnitIds[0], token);
            client.Board.Map.Camera.Zoom = 1.1f;
            client.Board.Map.Camera.Pan = new Vector2(-190, 30);
            client.Board.Map.Refresh();
            await client.LocalWrites;
            var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.NotNull(saved.Draft);
            Assert.AreEqual(army.RegionId, saved.Draft.Draft.Payload.Moves.Single().From);
            Assert.Contains(army.UnitIds[0], saved.Draft.Draft.Payload.Moves.Single().UnitIds);
            Assert.IsEmpty(saved.Draft.Draft.Payload.Moves.Single().To);
            Assert.IsNull(saved.Actions.Pending);
            var result = new HistoricalState
            {
                Draft = JsonConvert.SerializeObject(saved.Draft),
                Units = JsonConvert.SerializeObject(view.Units.OrderBy(u => u.Id)),
                Source = army.RegionId, Unit = army.UnitIds[0],
                Dice = view.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent)
            };
            Assert.AreEqual(1, (await api.Get<PlayerView>("api/matches/" + match, token)).Revision);
            LayoutTests.Save(client.Panel.targetTexture, "map16-historical-paid-draft.png");
            client.Flow.Dispose();
            await client.LocalWrites;
            return result;
        }

        private static async Task<GameCommand> CompleteHistoricalStep(ReturnTestFixture fixture, string address,
            string storage, string match, HistoricalState history, CancellationToken token)
        {
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
            var api = await fixture.Login(Side.FreePeoples, token, boundary, address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            AssertPaid(client.Board.View);
            Assert.AreEqual("FreePeoples.standard.1", client.Board.ActionDie.DieId);
            var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.AreEqual(history.Draft, JsonConvert.SerializeObject(saved.Draft),
                "Loading contours-2 cannot discard still-legal IDs, paid cost or an unsent choice.");
            Assert.AreEqual(history.Units, JsonConvert.SerializeObject(client.Board.View.Units.OrderBy(u => u.Id)));
            Assert.AreEqual(0, boundary.Commands);
            client.Board.Map.Focus(history.Source);
            Assert.Contains(history.Unit, saved.Draft.Draft.Payload.Moves.Single().UnitIds);
            boundary.HoldNextCommand = true;
            await ClickReady(client.Root, "console-confirm", token);
            await Until(() => boundary.CommandHeld, token);
            saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.AreEqual(boundary.LastCommand.CommandId, saved.Actions.Pending.CommandId);
            Assert.AreEqual(CommandKind.FinishActionSequence, boundary.LastCommand.Kind);
            Assert.AreEqual(1, boundary.Commands);
            client.Flow.Dispose();
            await client.LocalWrites;
            return boundary.LastCommand;
        }

        private static async Task RecoverHistoricalCompletion(ReturnTestFixture fixture, string address,
            string storage, string match, HistoricalState history, GameCommand uncertain, CancellationToken token)
        {
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
            var api = await fixture.Login(Side.FreePeoples, token, boundary, address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            await ReturnRecoveryTests.EmptyJournal(storage, api, match, false, token);
            Assert.AreEqual(1, boundary.ReceiptLookups);
            Assert.AreEqual(0, boundary.Commands, "A committed command is recovered from its receipt, never replayed by the map.");
            Assert.AreEqual(2, client.Board.View.Revision);
            Assert.IsNull(client.Board.View.ActionSequence);
            Assert.AreEqual(history.Dice + 1, client.Board.View.Dice.Count(d =>
                d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
            Assert.AreEqual(history.Units, JsonConvert.SerializeObject(client.Board.View.Units.OrderBy(u => u.Id)));
            AssertRegions(client.Board, "base");
            var receipt = await api.Get<CommandReceipt>($"api/matches/{match}/receipts/{uncertain.CommandId}", token);
            Assert.IsTrue(receipt.Accepted);
            Assert.AreEqual(2, receipt.Revision);
            var duplicate = await api.Post<CommandReceipt>($"api/matches/{match}/commands", uncertain, token);
            Assert.IsTrue(duplicate.Accepted);
            Assert.AreEqual(receipt.Revision, duplicate.Revision);
            Assert.AreEqual(2, (await api.Get<PlayerView>("api/matches/" + match, token)).Revision);
            LayoutTests.Save(client.Panel.targetTexture, "map16-historical-recovered.png");
            client.Flow.Dispose();
            await client.LocalWrites;
        }

        private static void AssertPaid(PlayerView view)
        {
            Assert.AreEqual(DecisionKind.ActionSequence, view.Decision.Kind);
            Assert.AreEqual(1, view.ActionSequence.CompletedSteps);
            Assert.AreEqual(1, view.ActionSequence.Remaining);
            Assert.AreEqual("FreePeoples.standard.1", view.ActionSequence.DieId);
            Assert.AreEqual(DieLocation.Resolving, view.Dice.Single(d => d.Id == view.ActionSequence.DieId).Location);
            Assert.AreEqual("osgiliath", view.Units.Single(u => u.Id == "base.gondor.regular.04").Location);
        }

        private sealed class HistoricalState
        {
            public string Draft, Units, Source, Unit;
            public int Dice;
        }
    }
}
