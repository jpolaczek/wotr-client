using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    public sealed partial class CompanionSetupPanelNetworkTests
    {
        [UnityTest]
        public IEnumerator PrivateVersionsAndOneReceiptSurviveClientAndServerRestarts() =>
            MatchTestRoom.Wait(Recovery());

        private static async Task Recovery()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-setup-" + Guid.NewGuid().ToString("N"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            using var server = new RestartTestServer();
            var token = timeout.Token;
            var match = fixture.ReturnMatches["council"];
            try
            {
                await server.Start(token);
                var draft = await SaveChoices(fixture, server.Address, storage, match, token);
                server.Stop();
                await server.Start(token);
                var command = await SubmitChoices(fixture, server.Address, storage, match, draft, token);
                server.Stop();
                await server.Start(token);
                await RecoverChoice(fixture, server.Address, storage, match, command, token);
            }
            finally { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
        }

        private static async Task<string> SaveChoices(ReturnTestFixture fixture, string address, string storage,
            string match, CancellationToken token)
        {
            var api = await fixture.Login(Side.FreePeoples, token, address: address);
            using var shadow = await fixture.Login(Side.Shadow, token, address: address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            var otherBefore = JsonConvert.SerializeObject(await shadow.Get<PlayerView>("api/matches/" + match, token));
            await ClickReady(client.Root, "fellowship-portrait-" + Grey, token);
            await ClickReady(client.Root, "version-lords.character.gandalf-grey", token);
            await ClickReady(client.Root, "fellowship-portrait-" + Gimli, token);
            await ClickReady(client.Root, "version-fate.character.gimli-dwarven-lord", token);
            await ClickReady(client.Root, "starting-guide-" + Strider, token);
            await Until(() => client.Root.Q<Button>("confirm-decision").enabledInHierarchy, token);
            await client.LocalWrites;
            var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.IsNotNull(saved.Draft);
            Assert.IsNull(saved.Actions.Pending);
            CollectionAssert.AreEquivalent(new[] { "lords.character.gandalf-grey", "fate.character.gimli-dwarven-lord" },
                saved.Draft.Draft.Payload.Ids);
            Assert.AreEqual(Strider, saved.Draft.Draft.Payload.GuideId);
            Assert.AreEqual(otherBefore, JsonConvert.SerializeObject(await shadow.Get<PlayerView>("api/matches/" + match, token)),
                "Private selection progress must not alter the opponent's projection.");
            var result = JsonConvert.SerializeObject(saved.Draft);
            client.Flow.Dispose();
            await client.LocalWrites;
            return result;
        }

        private static async Task<GameCommand> SubmitChoices(ReturnTestFixture fixture, string address, string storage,
            string match, string draft, CancellationToken token)
        {
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
            var api = await fixture.Login(Side.FreePeoples, token, boundary, address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.AreEqual(draft, JsonConvert.SerializeObject(saved.Draft));
            Assert.AreEqual(0, boundary.Commands);
            client.Board.Fellowship.Open();
            await Task.Delay(100, token);
            Assert.IsTrue(client.Root.Q<Button>("starting-guide-" + Strider).ClassListContains("selected"));
            boundary.HoldNextCommand = true;
            await ClickReady(client.Root, "confirm-decision", token);
            await Until(() => boundary.CommandHeld, token);
            saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.AreEqual(CommandKind.ChooseCompanionVersions, boundary.LastCommand.Kind);
            Assert.AreEqual(boundary.LastCommand.CommandId, saved.Actions.Pending.CommandId);
            Assert.AreEqual(1, boundary.Commands);
            client.Flow.Dispose();
            await client.LocalWrites;
            return boundary.LastCommand;
        }

        private static async Task RecoverChoice(ReturnTestFixture fixture, string address, string storage,
            string match, GameCommand command, CancellationToken token)
        {
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
            var api = await fixture.Login(Side.FreePeoples, token, boundary, address);
            using var shadow = await fixture.Login(Side.Shadow, token, address: address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            await ReturnRecoveryTests.EmptyJournal(storage, api, match, false, token);
            Assert.AreEqual(1, boundary.ReceiptLookups);
            Assert.AreEqual(0, boundary.Commands);
            Assert.AreEqual(command.ExpectedRevision + 1, client.Board.View.Revision);
            Assert.AreEqual(DecisionKind.CouncilHomelands, client.Board.View.Decision.Kind);
            Assert.IsTrue(client.Board.CompanionSetup.Active);
            Assert.AreSame(client.Board.CompanionSetup.DecisionHost, client.Board.Tray.parent);
            Assert.IsNull(client.Board.Tray.Q<VisualElement>("companion-setup-choices"));
            var other = await shadow.Get<PlayerView>("api/matches/" + match, token);
            Assert.IsNull(other.Decision);
            Assert.IsEmpty(other.OwnCompanionVersions);
            Assert.IsFalse(other.Fellowship.RevealedCompanionVersions.ContainsKey(Gimli));
            Assert.IsFalse(other.Fellowship.RevealedCompanionVersions.ContainsKey(Grey));
            Assert.AreEqual(Strider, other.Fellowship.Guide);
            var receipt = await api.Get<CommandReceipt>($"api/matches/{match}/receipts/{command.CommandId}", token);
            Assert.IsTrue(receipt.Accepted);
            var duplicate = await api.Post<CommandReceipt>($"api/matches/{match}/commands", command, token);
            Assert.AreEqual(receipt.Revision, duplicate.Revision);
            Assert.AreEqual(command.ExpectedRevision + 1, (await api.Get<PlayerView>("api/matches/" + match, token)).Revision);
            client.Flow.Dispose();
            await client.LocalWrites;
        }
    }
}
