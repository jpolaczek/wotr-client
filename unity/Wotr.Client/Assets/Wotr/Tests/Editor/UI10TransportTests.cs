using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public class UI10TransportTests
    {
        [Test]
        public void VersionsRejectDuplicatesOtherViewersAndEveryRegressingCounter()
        {
            var version = new ViewVersion("match");
            Assert.IsTrue(version.Accept(View(4, 2, 3)));
            Assert.IsFalse(version.Accept(View(4, 2, 3)));
            Assert.IsFalse(version.Accept(View(3, 3, 4)));
            Assert.IsFalse(version.Accept(View(5, 1, 4)));
            Assert.IsFalse(version.Accept(View(5, 3, 2)));
            var opponent = View(5, 3, 4);
            opponent.You = Side.Shadow;
            Assert.IsFalse(version.Accept(opponent));
            Assert.IsTrue(version.Accept(View(4, 3, 3)));
            Assert.AreEqual(4, version.Revision);
            Assert.IsFalse(version.IsAhead(3, 5));
            Assert.IsTrue(version.IsAhead(5, 3));
        }

        [Test]
        public async Task DelayedGetCannotReplaceNewerProjectionOrPublishDuplicate()
        {
            using var backend = new QueueBackend();
            using var api = new GameApi("https://127.0.0.1:5088", backend);
            using var connection = new MatchConnection(api, "match");
            var received = new List<long>();
            connection.Updated += view => received.Add(view.Revision);
            var older = connection.Refresh();
            var newer = connection.Refresh();
            backend.Reply(1, View(2));
            await newer;
            backend.Reply(0, View(1));
            await older;
            var duplicate = connection.Refresh();
            backend.Reply(2, View(2));
            await duplicate;
            CollectionAssert.AreEqual(new long[] { 2 }, received);
            Assert.AreEqual(2, connection.Revision);
        }

        [Test]
        public async Task ConcurrentSendAndRecoverShareOneAttempt()
        {
            using var backend = new QueueBackend();
            using var api = new GameApi("https://127.0.0.1:5088", backend);
            var sender = new CommandSender(api);
            var first = sender.Send(Command("first"), CancellationToken.None);
            var second = sender.Send(Command("second"), CancellationToken.None);
            var recovery = sender.Recover(CancellationToken.None);
            Assert.AreSame(first, second);
            Assert.AreSame(first, recovery);
            Assert.AreEqual(1, backend.Requests.Count);
            backend.Reply(0, Receipt("first"));
            Assert.IsTrue((await first).Accepted);
            Assert.IsNull(sender.Pending);
        }

        [Test]
        public async Task RecoveryLooksUpReceiptThenResendsFrozenOriginalOnlyOnNotFound()
        {
            using var backend = new QueueBackend();
            using var api = new GameApi("https://127.0.0.1:5088", backend);
            var sender = new CommandSender(api);
            var command = Command("first");
            var attempt = sender.Send(command, CancellationToken.None);
            backend.Fail(0);
            await AssertFails<HttpRequestException>(attempt);
            command.Payload.DieId = "replacement";
            command.Payload.Ids.Clear();
            var recovery = sender.Send(Command("new-id-must-not-be-used"), CancellationToken.None);
            Assert.AreEqual("GET", backend.Requests[1].Method);
            Assert.That(backend.Requests[1].Path, Does.EndWith("/receipts/first"));
            backend.Reply(1, null, HttpStatusCode.NotFound);
            await backend.WaitForRequests(3);
            Assert.AreEqual(backend.Requests[0].Body, backend.Requests[2].Body);
            backend.Reply(2, Receipt("first"));
            await recovery;
            Assert.IsNull(sender.Pending);
        }

        [Test]
        public async Task SavedReceiptAvoidsResendingAndAcceptsItsOlderRevision()
        {
            using var backend = new QueueBackend();
            using var api = new GameApi("https://127.0.0.1:5088", backend);
            var sender = new CommandSender(api);
            var attempt = sender.Send(Command("saved"), CancellationToken.None);
            backend.Fail(0);
            await AssertFails<HttpRequestException>(attempt);
            var recovery = sender.Recover(CancellationToken.None);
            backend.Reply(1, Receipt("saved"));
            Assert.AreEqual(1, (await recovery).Revision);
            Assert.AreEqual(2, backend.Requests.Count);
            Assert.IsNull(sender.Pending);
        }

        [Test]
        public async Task FailedLookupAndUnrelatedReceiptKeepOriginalPending()
        {
            using var backend = new QueueBackend();
            using var api = new GameApi("https://127.0.0.1:5088", backend);
            var sender = new CommandSender(api);
            var attempt = sender.Send(Command("pending"), CancellationToken.None);
            backend.Fail(0);
            await AssertFails<HttpRequestException>(attempt);
            var unavailable = sender.Recover(CancellationToken.None);
            backend.Reply(1, null, HttpStatusCode.InternalServerError);
            await AssertFails<ApiException>(unavailable);
            Assert.AreEqual("pending", sender.Pending.CommandId);
            var mismatch = sender.Recover(CancellationToken.None);
            backend.Reply(2, Receipt("unrelated"));
            await AssertFails<InvalidOperationException>(mismatch);
            Assert.AreEqual("pending", sender.Pending.CommandId);
            Assert.AreEqual(3, backend.Requests.Count, "Neither failed lookup may repeat POST.");
        }

        private static async Task AssertFails<T>(Task task) where T : Exception
        {
            try
            {
                await task;
                Assert.Fail("Expected " + typeof(T).Name);
            }
            catch (T) { }
        }

        private static GameCommand Command(string id) => new GameCommand
        {
            MatchId = "match", CommandId = id, DecisionId = "action", Kind = CommandKind.DrawCard,
            Payload = new CommandPayload { DieId = "exact-die", Deck = "character", Ids = { "original" } }
        };

        private static CommandReceipt Receipt(string id) => new CommandReceipt { CommandId = id, Accepted = true, Revision = 1 };

        private static PlayerView View(long revision, long preference = 0, long hand = 0) => new PlayerView
        {
            MatchId = "match", Revision = revision, You = Side.FreePeoples,
            AutoDecision = new AutoDecisionView { Version = preference, HandVersion = hand }
        };

        private sealed class QueueBackend : HttpMessageHandler
        {
            public List<(string Method, string Path, string Body)> Requests { get; } = new List<(string, string, string)>();
            private readonly List<TaskCompletionSource<HttpResponseMessage>> responses = new List<TaskCompletionSource<HttpResponseMessage>>();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync();
                responses.Add(response);
                Requests.Add((request.Method.Method, request.RequestUri.AbsolutePath, body));
                return await response.Task;
            }

            public void Reply(int index, object value, HttpStatusCode status = HttpStatusCode.OK) =>
                responses[index].SetResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(value))
                });

            public void Fail(int index) => responses[index].SetException(new HttpRequestException("Response interrupted."));

            public async Task WaitForRequests(int count)
            {
                for (var attempt = 0; Requests.Count < count && attempt < 100; attempt++)
                    await Task.Delay(10);
                Assert.AreEqual(count, Requests.Count);
            }
        }
    }
}
