using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.EditorTests.LocalRecoveryTests;

namespace Wotr.EditorTests
{
    public class CommandJournalTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task ANewClientRecoversThePersistedIdentityAndPayloadBeforeSendingAnythingElse(bool committed)
        {
            var directory = Path.Combine(Path.GetTempPath(), "wotr-journal-" + Guid.NewGuid().ToString("N"));
            try
            {
                var files = new LocalMatchFiles(directory, "https://server/", "account", "match");
                using var backend = new LostResponseBackend(files) { Committed = committed };
                using var api = new GameApi("https://server/", backend);
                var journal = await CommandJournal.Open(files);
                var sender = new CommandSender(api, journal);
                var command = Command("original");
                await Fails<HttpRequestException>(sender.Send(command, CancellationToken.None));
                command.Payload.DieId = "changed-after-send";
                var restarted = new CommandSender(api, await CommandJournal.Open(new LocalMatchFiles(directory, "https://server/", "account", "match")));
                Assert.AreEqual("original", restarted.Pending.CommandId);
                Assert.AreEqual("die", restarted.Pending.Payload.DieId);
                Assert.IsTrue((await restarted.Send(Command("must-not-send"), CancellationToken.None)).Accepted);
                Assert.AreEqual(1, backend.Lookups);
                Assert.AreEqual(committed ? 1 : 2, backend.Posts);
                Assert.IsNull((await CommandJournal.Open(files)).Pending);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public async Task FailedLocalWritePreventsHttpAndKeepsTheExactPendingCommand()
        {
            using var backend = new LostResponseBackend(null);
            using var api = new GameApi("https://server/", backend);
            var sender = new CommandSender(api, new FailingJournal());
            await Fails<LocalSaveException>(sender.Send(Command("unsent"), CancellationToken.None));
            Assert.AreEqual(0, backend.Posts);
            Assert.AreEqual(0, backend.Lookups);
            Assert.AreEqual("unsent", sender.Pending.CommandId);
        }

        private sealed class FailingJournal : ICommandJournal
        {
            public GameCommand Pending => null;
            public Task Save(GameCommand command) => Task.FromException(new LocalSaveException(new IOException("Disk unavailable.")));
            public Task Complete(string commandId) => Task.CompletedTask;
        }

        private sealed class LostResponseBackend : HttpMessageHandler
        {
            private readonly LocalMatchFiles files;
            public bool Committed;
            public int Lookups;
            public int Posts;
            public LostResponseBackend(LocalMatchFiles files) { this.files = files; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                if (request.Method == HttpMethod.Get)
                {
                    Lookups++;
                    StringAssert.EndsWith("/receipts/original", request.RequestUri.AbsolutePath);
                    if (!Committed)
                        return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("") };
                }
                else
                {
                    Posts++;
                    var sent = JsonConvert.DeserializeObject<GameCommand>(await request.Content.ReadAsStringAsync());
                    var saved = await files.Read<GameCommand>("action");
                    Assert.AreEqual(JsonConvert.SerializeObject(saved), JsonConvert.SerializeObject(sent), "Durability must precede HTTP.");
                    if (Posts == 1)
                        throw new HttpRequestException("Lost response.");
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(new CommandReceipt { CommandId = "original", Accepted = true, Revision = 2 }))
                };
            }
        }
    }
}
