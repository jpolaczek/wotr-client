using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    public class ReturnRecoveryTests
    {
        [UnityTest]
        public IEnumerator NewClientRecoversLostReplyAfterServerProcessRestartWithoutSpendingAgain() => MatchTestRoom.Wait(Restart());

        [UnityTest]
        public IEnumerator PrivateBattlePolicyRecoversAfterClientRecreationWithoutTouchingOpponent() => MatchTestRoom.Wait(Policy());

        private static async Task Restart()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-return-" + Guid.NewGuid().ToString("N"));
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100)))
            using (var server = new RestartTestServer())
            {
                var token = timeout.Token;
                var id = fixture.ReturnMatches["restart"];
                try
                {
                    await server.Start(token);
                    var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + id };
                    var api = await fixture.Login(Side.FreePeoples, token, boundary, server.Address);
                    string account = api.AccountId, commandId;
                    int hand;
                    long revision;
                    using (var first = new MountedFlowClient(api, storage))
                    {
                        await Open(first, id, token);
                        hand = first.Board.View.Hand.Count;
                        revision = first.Board.View.Revision;
                        var die = first.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
                        await ClickReady(first.Root, die.Id, token);
                        // UI-91: the hand is permanent; no Cards tab to open.
                        boundary.HoldNextCommand = true;
                        await ClickReady(first.Root, "deck-character", token);
                        await Until(() => boundary.CommandHeld, token);
                        commandId = boundary.LastCommand.CommandId;
                        var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, account, id);
                        Assert.AreEqual(commandId, saved.Actions.Pending.CommandId);
                        Assert.AreEqual(1, boundary.Commands);
                        first.Flow.Dispose();
                        await first.LocalWrites;
                    }
                    server.Stop();
                    await server.Start(token);
                    var recoveredBoundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + id };
                    var newApi = await fixture.Login(Side.FreePeoples, token, recoveredBoundary, server.Address);
                    using (var second = new MountedFlowClient(newApi, storage))
                    {
                        await Open(second, id, token);
                        await EmptyJournal(storage, newApi, id, false, token);
                        Assert.AreEqual(1, recoveredBoundary.ReceiptLookups);
                        Assert.AreEqual(0, recoveredBoundary.Commands);
                        Assert.AreEqual(revision + 1, second.Board.View.Revision);
                        Assert.AreEqual(hand + 1, second.Board.View.Hand.Count);
                        Assert.AreEqual(1, second.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
                        var receipt = await newApi.Get<CommandReceipt>($"api/matches/{id}/receipts/{commandId}", token);
                        Assert.IsTrue(receipt.Accepted);
                        Assert.AreEqual(revision + 1, receipt.Revision);
                        second.Flow.Dispose();
                        await second.LocalWrites;
                    }
                }
                finally { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
            }
        }

        private static async Task Policy()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-policy-return-" + Guid.NewGuid().ToString("N"));
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(70)))
            using (var opponent = await fixture.Login(Side.FreePeoples, timeout.Token))
            {
                var token = timeout.Token;
                var id = fixture.ReturnMatches["policy"];
                var before = await opponent.Get<PlayerView>($"api/matches/{id}", token);
                try
                {
                    var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + id };
                    var api = await fixture.Login(Side.Shadow, token, boundary);
                    using (var first = new MountedFlowClient(api, storage))
                    {
                        await Open(first, id, token);
                        boundary.HoldNextCommand = true;
                        await Until(() => first.Board.BattlePolicy.Toggle.enabledInHierarchy, token);
                        Click(first.Board.BattlePolicy.Toggle);
                        await Until(() => boundary.CommandHeld, token);
                        Assert.AreEqual(CommandKind.SetCombatCardPolicy, boundary.LastCommand.Kind);
                        first.Flow.Dispose();
                        await first.LocalWrites;
                    }
                    var nextBoundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + id };
                    var nextApi = await fixture.Login(Side.Shadow, token, nextBoundary);
                    using (var next = new MountedFlowClient(nextApi, storage))
                    {
                        await Open(next, id, token);
                        await EmptyJournal(storage, nextApi, id, true, token);
                        Assert.AreEqual(1, nextBoundary.ReceiptLookups);
                        Assert.AreEqual(0, nextBoundary.Commands);
                        Assert.NotNull(next.Board.View.AutoDecision.Policy);
                        Assert.AreEqual(before.Revision, next.Board.View.Revision);
                        Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(before),
                            JToken.FromObject(await opponent.Get<PlayerView>($"api/matches/{id}", token))));
                        next.Flow.Dispose();
                        await next.LocalWrites;
                    }
                }
                finally { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
            }
        }

        internal static async Task EmptyJournal(string root, GameApi api, string id, bool policy, CancellationToken token)
        {
            while (true)
            {
                var saved = await LocalMatchPersistence.Open(root, api.Address.AbsoluteUri, api.AccountId, id);
                if ((policy ? saved.Policy : saved.Actions).Pending == null)
                    return;
                await Task.Delay(25, token);
            }
        }
    }
}
