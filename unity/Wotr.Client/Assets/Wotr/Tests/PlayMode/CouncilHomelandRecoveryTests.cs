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
    public sealed partial class CouncilHomelandPanelNetworkTests
    {
        [UnityTest]
        public IEnumerator HomelandDraftAndBothReceiptsSurviveRestartsWithoutSkippingTheShadowChoice() =>
            MatchTestRoom.Wait(HomelandRecovery());

        private static async Task HomelandRecovery()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-homelands-" + Guid.NewGuid().ToString("N"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(180));
            using var server = new RestartTestServer();
            var token = timeout.Token;
            var match = fixture.ReturnMatches["ui69-recovery"];
            try
            {
                await server.Start(token);
                var draft = await SaveHomelands(fixture, server.Address, storage, match, token);
                server.Stop();
                await server.Start(token);
                var homeland = await SubmitHomelands(fixture, server.Address, storage, match, draft, token);
                server.Stop();
                await server.Start(token);
                await RecoverHomelands(fixture, server.Address, storage, match, homeland, token);
                server.Stop();
                await server.Start(token);
                var councilToken = await SubmitCouncilToken(fixture, server.Address, storage, match, token);
                server.Stop();
                await server.Start(token);
                await RecoverCouncilToken(fixture, server.Address, storage, match, councilToken, token);
            }
            finally
            {
                if (Directory.Exists(storage))
                    Directory.Delete(storage, true);
            }
        }

        private static async Task<string> SaveHomelands(ReturnTestFixture fixture, string address, string storage,
            string match, CancellationToken token)
        {
            var api = await fixture.Login(Side.FreePeoples, token, address: address);
            using var shadow = await fixture.Login(Side.Shadow, token, address: address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            await StrictPress(client.Root, "fellowship-portrait-base.character.gandalf-grey", token);
            await StrictPress(client.Root, "version-base.character.gandalf-grey", token);
            await StrictPress(client.Root, "fellowship-portrait-" + Gimli, token);
            await StrictPress(client.Root, "version-fate.character.gimli-dwarven-lord", token);
            await StrictPress(client.Root, "starting-guide-base.character.strider", token);
            await StrictPress(client.Root, "confirm-decision", token);
            await Until(() => client.Board.View.Decision?.Kind == DecisionKind.CouncilHomelands, token);
            await Task.Delay(100, token);
            var other = JsonConvert.SerializeObject(await shadow.Get<PlayerView>("api/matches/" + match, token));
            await Place(client.Board, () => Task.Delay(100, token), Gimli, "iron-hills", false);
            await Place(client.Board, () => Task.Delay(100, token), Gimli, "ered-luin", true);
            await client.LocalWrites;
            var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.IsNotNull(saved.Draft);
            Assert.IsNull(saved.Actions.Pending);
            Assert.AreEqual(Gimli, saved.Draft.Draft.Payload.Placements.Single().UnitId);
            Assert.AreEqual("ered-luin", saved.Draft.Draft.Payload.Placements.Single().RegionId);
            Assert.AreEqual(other, JsonConvert.SerializeObject(await shadow.Get<PlayerView>("api/matches/" + match, token)));
            var result = JsonConvert.SerializeObject(saved.Draft);
            client.Flow.Dispose();
            await client.LocalWrites;
            return result;
        }

        private static async Task<GameCommand> SubmitHomelands(ReturnTestFixture fixture, string address, string storage,
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
            Assert.AreEqual("→ Ered Luin", Portrait(client.Board, Gimli).Q<Label>("homeland-location").text);
            return await LoseResponse(client, api, boundary, storage, match, "confirm-decision",
                CommandKind.ChooseCouncilHomelands, token);
        }

        private static async Task RecoverHomelands(ReturnTestFixture fixture, string address, string storage,
            string match, GameCommand command, CancellationToken token)
        {
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
            var api = await fixture.Login(Side.FreePeoples, token, boundary, address);
            using var shadow = await fixture.Login(Side.Shadow, token, address: address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            await ReturnRecoveryTests.EmptyJournal(storage, api, match, false, token);
            AssertRecovered(boundary, client.Board, command);
            Assert.IsFalse(CompanionSetupPresentation.Setup(client.Board.View));
            var other = await shadow.Get<PlayerView>("api/matches/" + match, token);
            Assert.AreEqual(DecisionKind.CouncilToken, other.Decision.Kind);
            Assert.IsEmpty(other.CouncilTokens);
            Assert.AreEqual("ered-luin", other.Characters.Single(c => c.Id == Gimli).Location);
            Assert.AreEqual("fate.character.gimli-dwarven-lord", other.Fellowship.RevealedCompanionVersions[Gimli]);
            var duplicate = await api.Post<CommandReceipt>($"api/matches/{match}/commands", command, token);
            Assert.IsTrue(duplicate.Accepted);
            Assert.AreEqual(command.ExpectedRevision + 1, duplicate.Revision);
            Assert.AreEqual(JsonConvert.SerializeObject(other), JsonConvert.SerializeObject(
                await shadow.Get<PlayerView>("api/matches/" + match, token)));
            client.Flow.Dispose();
            await client.LocalWrites;
        }

        private static async Task<GameCommand> SubmitCouncilToken(ReturnTestFixture fixture, string address, string storage,
            string match, CancellationToken token)
        {
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
            var api = await fixture.Login(Side.Shadow, token, boundary, address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            Assert.AreEqual(DecisionKind.CouncilToken, client.Board.View.Decision.Kind);
            Assert.IsEmpty(client.Board.View.CouncilTokens);
            // UI-270: the token is taken on its lit medallion in the Shadow's pool; no panel opens.
            Assert.IsFalse(client.Board.ResourceDecision.IsOpen);
            foreach (var id in CouncilTokenSlots.Ids)
                Assert.IsTrue(client.Board.OwnDice.Q<Button>("pool-" + id).ClassListContains("legal-target"));
            Assert.AreEqual(0, boundary.Commands);
            return await LoseResponse(client, api, boundary, storage, match,
                "pool-" + ExpansionDraft.CouncilTravel, CommandKind.ChooseCouncilToken, token);
        }

        private static async Task RecoverCouncilToken(ReturnTestFixture fixture, string address, string storage,
            string match, GameCommand command, CancellationToken token)
        {
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
            var api = await fixture.Login(Side.Shadow, token, boundary, address);
            using var freePeoples = await fixture.Login(Side.FreePeoples, token, address: address);
            using var client = new MountedFlowClient(api, storage);
            await Open(client, match, token);
            await ReturnRecoveryTests.EmptyJournal(storage, api, match, false, token);
            AssertRecovered(boundary, client.Board, command);
            Assert.AreEqual(ExpansionDraft.CouncilTravel, client.Board.View.CouncilTokens.Single().Id);
            Assert.IsFalse(client.Board.View.CouncilTokens.Single().Spent);
            var own = JsonConvert.SerializeObject(client.Board.View);
            var other = await freePeoples.Get<PlayerView>("api/matches/" + match, token);
            Assert.AreEqual(DecisionKind.FellowshipPhase, other.Decision.Kind);
            var duplicate = await api.Post<CommandReceipt>($"api/matches/{match}/commands", command, token);
            Assert.IsTrue(duplicate.Accepted);
            Assert.AreEqual(command.ExpectedRevision + 1, duplicate.Revision);
            Assert.AreEqual(own, JsonConvert.SerializeObject(await api.Get<PlayerView>("api/matches/" + match, token)));
            Assert.AreEqual(JsonConvert.SerializeObject(other), JsonConvert.SerializeObject(
                await freePeoples.Get<PlayerView>("api/matches/" + match, token)));
            client.Flow.Dispose();
            await client.LocalWrites;
        }

        private static async Task<GameCommand> LoseResponse(MountedFlowClient client, GameApi api, UI10HttpBoundary boundary,
            string storage, string match, string button, CommandKind kind, CancellationToken token)
        {
            boundary.HoldNextCommand = true;
            await StrictPress(client.Root, button, token);
            await Until(() => boundary.CommandHeld, token);
            var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
            Assert.AreEqual(kind, boundary.LastCommand.Kind);
            Assert.AreEqual(boundary.LastCommand.CommandId, saved.Actions.Pending.CommandId);
            Assert.AreEqual(JsonConvert.SerializeObject(boundary.LastCommand), JsonConvert.SerializeObject(saved.Actions.Pending));
            Assert.AreEqual(1, boundary.Commands);
            client.Flow.Dispose();
            await client.LocalWrites;
            return boundary.LastCommand;
        }

        private static void AssertRecovered(UI10HttpBoundary boundary, BoardScreen board, GameCommand command)
        {
            Assert.AreEqual(1, boundary.ReceiptLookups);
            Assert.AreEqual(0, boundary.Commands);
            Assert.AreEqual(command.ExpectedRevision + 1, board.View.Revision);
        }

        private static async Task StrictPress(VisualElement root, string name, CancellationToken token)
        {
            await Until(() => root.Q<Button>(name)?.enabledInHierarchy == true && root.Q<Button>(name).worldBound.height > 0, token);
            PointerGestureEvents.ClickAt(root.Q<Button>(name));
            await Task.Delay(100, token);
        }
    }
}
