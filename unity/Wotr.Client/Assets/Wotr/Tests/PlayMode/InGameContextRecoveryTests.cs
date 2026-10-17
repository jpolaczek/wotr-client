using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    public sealed partial class InGameContextNetworkTests
    {
        [UnityTest]
        public IEnumerator RingReceiptSurvivesClientAndServerRestartWithoutChangingThePhysicalCost() =>
            MatchTestRoom.Wait(Recover("ring", Side.FreePeoples));

        [UnityTest]
        public IEnumerator PrivateCorruptionTileReceiptSurvivesRestartWithTheSameParentAndPublicBack() =>
            MatchTestRoom.Wait(Recover("tile", Side.Shadow));

        [UnityTest]
        public IEnumerator VictoryReceiptSurvivesRestartWithoutReplayingTheFinalAction() =>
            MatchTestRoom.Wait(Recover("victory", Side.Shadow));

        private static async Task Recover(string name, Side side)
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-ui62-" + Guid.NewGuid().ToString("N"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(110));
            using var server = new RestartTestServer();
            var token = timeout.Token;
            var id = fixture.ReturnMatches["ui62-" + name];
            try
            {
                await server.Start(token);
                var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + id };
                var api = await fixture.Login(side, token, boundary, server.Address);
                GameCommand command;
                PlayerView before;
                string parent = "";
                using (var first = new MountedFlowClient(api, storage))
                {
                    await Open(first, id, token);
                    before = first.Board.View;
                    if (name == "tile")
                    {
                        var page = await api.Get<ActionLogPage>($"api/matches/{id}/actions?filter=All&limit=50", token);
                        parent = page.Steps.Last(step => step.Command == CommandKind.AttemptCorruption).ActionId;
                    }
                    await PrepareAndCommit(first, name, boundary, token);
                    await Until(() => boundary.CommandHeld, token);
                    command = boundary.LastCommand;
                    var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, id);
                    Assert.AreEqual(command.CommandId, saved.Actions.Pending.CommandId);
                    Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(command.Payload), JToken.FromObject(saved.Actions.Pending.Payload)));
                    Assert.AreEqual(1, boundary.Commands);
                    first.Flow.Dispose();
                    await first.LocalWrites;
                }
                server.Stop();
                await server.Start(token);
                var recovered = new UI10HttpBoundary { MatchPath = "/api/matches/" + id };
                var nextApi = await fixture.Login(side, token, recovered, server.Address);
                using (var second = new MountedFlowClient(nextApi, storage))
                {
                    await Open(second, id, token);
                    await ReturnRecoveryTests.EmptyJournal(storage, nextApi, id, false, token);
                    Assert.AreEqual(1, recovered.ReceiptLookups);
                    Assert.AreEqual(0, recovered.Commands, "Recovery looks up the receipt; it cannot execute the restored draft.");
                    Assert.AreEqual(before.Revision + 1, second.Board.View.Revision);
                    VerifyCommitted(name, before, second.Board.View, command);
                    var page = await nextApi.Get<ActionLogPage>($"api/matches/{id}/actions?filter=All&limit=50", token);
                    var step = page.Steps.Single(item => item.Command == command.Kind && item.Revision == before.Revision + 1);
                    if (parent.Length > 0)
                        Assert.AreEqual(parent, step.ActionId, "The private tile belongs to the original corruption attempt.");
                    var group = page.Actions.Single(item => item.Id == step.ActionId);
                    second.Board.ActionHistory.Open(new ActionOpenRequest(group.Id, group.Version, step.Id));
                    await Until(() => second.Board.ActionHistory.Context != null, token);
                    Assert.AreEqual(name == "ring" ? InGameActionHost.Resources : name == "tile"
                        ? InGameActionHost.Sovereign : InGameActionHost.Victory, second.Board.ActionHistory.Context.InGame.Host);
                    var snapshot = await nextApi.Get<PlayerView>($"api/matches/{id}", token);
                    var duplicate = await nextApi.Post<CommandReceipt>($"api/matches/{id}/commands", command, token);
                    Assert.IsTrue(duplicate.Accepted);
                    Assert.AreEqual(snapshot.Revision, duplicate.Revision);
                    var after = await nextApi.Get<PlayerView>($"api/matches/{id}", token);
                    Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(snapshot), JToken.FromObject(after)),
                        "Duplicate delivery cannot spend again, draw again or change the ending.");
                    using var enemy = await fixture.Login(side == Side.Shadow ? Side.FreePeoples : Side.Shadow, token, address: server.Address);
                    var publicView = await enemy.Get<PlayerView>($"api/matches/{id}", token);
                    VerifyPublic(name, before, publicView);
                    second.Flow.Dispose();
                    await second.LocalWrites;
                }
            }
            finally
            {
                if (Directory.Exists(storage))
                    Directory.Delete(storage, true);
            }
        }

        private static async Task PrepareAndCommit(MountedFlowClient client, string name, UI10HttpBoundary boundary, CancellationToken token)
        {
            var view = client.Board.View;
            if (name == "ring")
            {
                var die = view.Dice.First(value => value.Owner == view.You && value.Location == DieLocation.Available);
                var ring = view.Rings.First(value => value.Owner == view.You).Id;
                // UI-161: ring at its console socket, then the die, then the result in the popover above it.
                Click(client.Board.Console.Rings.For(ring));
                await ClickReady(client.Root, die.Id, token);
                boundary.HoldNextCommand = true;
                await Until(() => client.Board.RingPicker.IsOpen, token);
                var target = client.Board.RingPicker.Root.Q<Button>("ring-face-muster");
                await Until(() => target.enabledInHierarchy, token);
                PointerGestureEvents.ClickAt(target);
            }
            else if (name == "tile")
            {
                var tile = SelectableCorruptionTile(view);
                client.Board.Notice.Open();
                boundary.HoldNextCommand = true;
                await ClickReady(client.Root, "corruption-choice-" + tile, token);
            }
            else
            {
                var die = view.Dice.Single(value => value.Owner == view.You && value.Location == DieLocation.Available);
                await ClickReady(client.Root, die.Id, token);
                boundary.HoldNextCommand = true;
                await ClickReady(client.Root, "console-discard", token);
            }
        }

        private static void VerifyCommitted(string name, PlayerView before, PlayerView after, GameCommand command)
        {
            if (name == "ring")
            {
                Assert.AreEqual(CommandKind.UseRing, command.Kind);
                Assert.AreEqual(Side.Shadow, after.Rings.Single(ring => ring.Id == command.Payload.RingId).Owner);
                var die = after.Dice.Single(value => value.Id == command.Payload.DieId);
                Assert.AreEqual(DieFace.Muster, die.Face);
                Assert.AreEqual(DieLocation.Available, die.Location);
            }
            else if (name == "tile")
            {
                Assert.AreEqual(CommandKind.ChooseCorruptionTile, command.Kind);
                CollectionAssert.AreEqual(new[] { SelectableCorruptionTile(before) }, command.Payload.Ids);
                Assert.IsNull(after.CorruptionAttempt);
                var sovereign = after.Sovereigns.Single(value => value.Id == before.CorruptionAttempt.SovereignId);
                Assert.AreEqual(1, sovereign.Tiles.Count);
                Assert.AreEqual(DieLocation.Spent, after.Dice.Single(value => value.Id == before.CorruptionAttempt.DieId).Location);
            }
            else
            {
                Assert.AreEqual(Side.FreePeoples, after.Winner);
                Assert.AreEqual(VictoryCondition.FreePeoplesMilitary, after.Victory);
                Assert.IsNull(after.Decision);
                Assert.IsEmpty(after.Options);
            }
        }

        private static void VerifyPublic(string name, PlayerView before, PlayerView after)
        {
            if (name == "tile")
            {
                var sovereign = after.Sovereigns.Single(value => value.Id == before.CorruptionAttempt.SovereignId);
                Assert.IsNull(sovereign.Corruption);
                Assert.IsNull(sovereign.Tiles.Single().Value);
                Assert.AreEqual("", sovereign.Tiles.Single().TileId);
            }
            if (name == "victory")
            {
                Assert.AreEqual(Side.FreePeoples, after.Winner);
                Assert.AreEqual(VictoryCondition.FreePeoplesMilitary, after.Victory);
            }
        }
    }
}
