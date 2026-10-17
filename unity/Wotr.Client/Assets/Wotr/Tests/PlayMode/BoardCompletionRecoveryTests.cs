using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;
using static Wotr.PlayTests.BoardCompletionEntryChecks;

namespace Wotr.PlayTests
{
    public sealed partial class BoardCompletionContextNetworkTests
    {
        [UnityTest]
        public IEnumerator CouncilEntryDraftAndLostReceiptSurviveGamesAndBothProcessRestarts() => MatchTestRoom.Wait(EntryRecovery());

        private static async Task EntryRecovery()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-ui71-entry-" + Guid.NewGuid().ToString("N"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(150));
            using var server = new RestartTestServer();
            var token = timeout.Token;
            var match = fixture.ReturnMatches["ui71-council-entry"];
            try
            {
                await server.Start(token);
                var draft = await SaveEntry(fixture, server.Address, storage, match, token);
                server.Stop();
                await server.Start(token);
                var command = await SubmitEntry(fixture, server.Address, storage, match, draft, token);
                server.Stop();
                await server.Start(token);
                await RecoverEntry(fixture, server.Address, storage, match, command, token);
            }
            finally { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
        }

        private static async Task<string> SaveEntry(ReturnTestFixture fixture, string address, string storage,
            string match, CancellationToken token)
        {
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
            var api = await fixture.Login(Side.Shadow, token, boundary, address);
            using var opponent = await fixture.Login(Side.FreePeoples, token, address: address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            var before = JsonConvert.SerializeObject(await opponent.Get<PlayerView>("api/matches/" + match, token));
            var die = Choice(client.Board).Dice.Last();
            await Prepare(client.Board, die, token);
            await client.LocalWrites;
            var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.IsNotNull(saved.Draft);
            Assert.AreEqual(CommandKind.EnterCharacter, saved.Draft.Draft.Kind);
            Assert.AreEqual(Serpent, saved.Draft.Draft.Payload.CharacterId);
            Assert.AreEqual(Council, saved.Draft.Draft.Payload.CouncilTokenId);
            Assert.AreEqual(die, saved.Draft.Draft.Payload.DieId);
            Assert.IsEmpty(saved.Draft.Draft.Payload.RegionId, "The destination remains an explicit final choice.");
            Assert.IsNull(saved.Actions.Pending);
            Assert.AreEqual(0, boundary.Commands);
            var draft = JsonConvert.SerializeObject(saved.Draft);
            await Games(client, token);
            await Open(client, match, token);
            await client.LocalWrites;
            saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.AreEqual(draft, JsonConvert.SerializeObject(saved.Draft));
            Assert.AreEqual(die, client.Board.ActionDie.DieId);
            Assert.AreEqual(0, boundary.Commands);
            Assert.AreEqual(before, JsonConvert.SerializeObject(await opponent.Get<PlayerView>("api/matches/" + match, token)));
            client.Flow.Dispose();
            await client.LocalWrites;
            return draft;
        }

        private static async Task<GameCommand> SubmitEntry(ReturnTestFixture fixture, string address, string storage,
            string match, string draft, CancellationToken token)
        {
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
            var api = await fixture.Login(Side.Shadow, token, boundary, address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            await client.LocalWrites;
            var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.AreEqual(draft, JsonConvert.SerializeObject(saved.Draft));
            Assert.AreEqual(0, boundary.Commands);
            Assert.AreEqual(0, boundary.ReceiptLookups);
            PointerGestureEvents.ClickAt(client.Board.Notice.Button);
            await Task.Delay(100, token);
            // UI-108/UI-142: the restored entry keeps its decision in the dock, as when the row was clicked.
            Assert.AreNotSame(client.Board.Characters.DecisionHost, client.Board.Tray.parent);
            Assert.AreEqual(saved.Draft.Draft.Payload.DieId, client.Board.ActionDie.DieId);
            var destination = await Target(client.Board, token);
            boundary.HoldNextCommand = true;
            MapContourProbe.Click(client.Board.Map, destination);
            await Until(() => boundary.CommandHeld, token);
            var command = boundary.LastCommand;
            Assert.AreEqual(CommandKind.EnterCharacter, command.Kind);
            Assert.AreEqual(Council, command.Payload.CouncilTokenId);
            Assert.AreEqual(saved.Draft.Draft.Payload.DieId, command.Payload.DieId);
            Assert.AreEqual(Region, command.Payload.RegionId);
            Assert.AreEqual(1, boundary.Commands);
            await Games(client, token);
            saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.AreEqual(JsonConvert.SerializeObject(command), JsonConvert.SerializeObject(saved.Actions.Pending));
            client.Flow.Dispose();
            await client.LocalWrites;
            return command;
        }

        private static async Task RecoverEntry(ReturnTestFixture fixture, string address, string storage,
            string match, GameCommand command, CancellationToken token)
        {
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
            var api = await fixture.Login(Side.Shadow, token, boundary, address);
            using var opponent = await fixture.Login(Side.FreePeoples, token, address: address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            await ReturnRecoveryTests.EmptyJournal(storage, api, match, false, token);
            Assert.AreEqual(1, boundary.ReceiptLookups);
            Assert.AreEqual(0, boundary.Commands);
            var own = await api.Get<PlayerView>("api/matches/" + match, token);
            var other = await opponent.Get<PlayerView>("api/matches/" + match, token);
            Entered(own, command);
            Entered(other, command);
            Assert.IsEmpty(client.Board.ActionDie.DieId);
            var ownBefore = JsonConvert.SerializeObject(own);
            var otherBefore = JsonConvert.SerializeObject(other);
            var receipt = await api.Get<CommandReceipt>($"api/matches/{match}/receipts/{command.CommandId}", token);
            Assert.IsTrue(receipt.Accepted);
            var duplicate = await api.Post<CommandReceipt>($"api/matches/{match}/commands", command, token);
            Assert.AreEqual(receipt.Revision, duplicate.Revision);
            Assert.AreEqual(ownBefore, JsonConvert.SerializeObject(await api.Get<PlayerView>("api/matches/" + match, token)));
            Assert.AreEqual(otherBefore, JsonConvert.SerializeObject(await opponent.Get<PlayerView>("api/matches/" + match, token)));
            client.Flow.Dispose();
            await client.LocalWrites;
        }
    }
}
