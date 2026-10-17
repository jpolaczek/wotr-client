using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public class NetworkTests
    {
        [UnityTest]
        public IEnumerator TwoIndependentSessionsUseRealServerAndRecoverLobbyAndMap()
        {
            var fixture = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(fixture))
                Assert.Ignore("Run scripts/test-client.sh for real HTTP/WSS and PostgreSQL integration.");
            var config = JsonConvert.DeserializeObject<Fixture>(File.ReadAllText(fixture));
            var task = Run(config);
            var deadline = Time.realtimeSinceStartup + 90;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(task.IsCompleted, "Integration timed out.");
            task.GetAwaiter().GetResult();
        }

        private static async Task Run(Fixture fixture)
        {
            using (var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(80)))
            using (var alice = new GameApi(fixture.Address, new DropReceiptHandler()))
            using (var bob = new GameApi(fixture.Address))
            {
                var token = cancellation.Token;
                // AUTH-02: the seeder's two accounts instead of account invitations.
                await alice.Login(fixture.AccountA, fixture.AccountPassword, token);
                await bob.Login(fixture.AccountB, fixture.AccountPassword, token);
                Assert.AreNotEqual(alice.Token, bob.Token);
                var room = await alice.Post<RoomView>("api/rooms", new CreateRoomRequest { Side = Side.FreePeoples }, token);
                var joined = await bob.Post<RoomView>("api/rooms/join", new JoinRoomRequest { Code = room.JoinCode }, token);
                Assert.AreEqual(Side.Shadow, joined.You);
                await alice.Post<RoomView>($"api/rooms/{room.Id}/confirm", new RoomRevisionRequest { ExpectedVersion = 1 }, token);
                var configured = await bob.Put<RoomView>($"api/rooms/{room.Id}/configuration", new ConfigureRoomRequest { ExpectedVersion = 1 }, token);
                Assert.IsFalse(configured.FreePeoplesAccepted);
                using (var returned = new GameApi(fixture.Address))
                {
                    await returned.Login(fixture.AccountA, fixture.AccountPassword, token);
                    var recovered = await returned.Get<RoomView>($"api/rooms/{room.Id}", token);
                    Assert.AreEqual(2, recovered.Version);
                    Assert.IsFalse(recovered.FreePeoplesAccepted);
                }
                await alice.Post<RoomView>($"api/rooms/{room.Id}/confirm", new RoomRevisionRequest { ExpectedVersion = 2 }, token);
                await bob.Post<RoomView>($"api/rooms/{room.Id}/confirm", new RoomRevisionRequest { ExpectedVersion = 2 }, token);
                var started = await alice.Post<RoomView>($"api/rooms/{room.Id}/start", new RoomRevisionRequest { ExpectedVersion = 2 }, token);
                await CheckMatch(alice, bob, started.MatchId, token);
            }
        }

        private static async Task CheckMatch(GameApi alice, GameApi bob, string id, CancellationToken token)
        {
            var fp = await alice.Get<PlayerView>($"api/matches/{id}", token);
            var sh = await bob.Get<PlayerView>($"api/matches/{id}", token);
            Assert.AreEqual(Side.FreePeoples, fp.You);
            Assert.AreEqual(Side.Shadow, sh.You);
            Assert.AreEqual(JsonConvert.SerializeObject(fp.Units), JsonConvert.SerializeObject(sh.Units));
            var first = new BoardScreen();
            var second = new BoardScreen();
            first.Apply(fp);
            second.Apply(sh);
            Assert.AreEqual(fp.Units.Count(u => u.Location != "reserve"),
                first.Presentation.Armies.Sum(a => a.Regular + a.Elite + a.Leaders));
            first.Map.Camera.Pan = new Vector2(-310, 37);
            first.Map.Camera.Zoom = 1.4f;
            var connected = 0;
            var expired = false;
            using (var connection = new MatchConnection(alice, id))
            {
                connection.Updated += first.Apply;
                connection.StatusChanged += status => { if (status == "Connected") connected++; if (status == "SessionExpired") expired = true; };
                connection.Start();
                await Until(() => connected == 1, token);
                connection.Reconnect();
                await Until(() => connected >= 2, token);
                Assert.AreEqual(new Vector2(-310, 37), first.Map.Camera.Pan);
                Assert.AreEqual(1.4f, first.Map.Camera.Zoom);
                Assert.AreEqual(fp.Revision, connection.Revision);
                Assert.AreEqual(JsonConvert.SerializeObject(fp), JsonConvert.SerializeObject(first.View));
                await ExerciseTurnAndLostResponse(alice, bob, id, token);
                await alice.Logout(token);
                connection.Reconnect();
                await Until(() => expired, token);
                Assert.AreEqual(id, first.View.MatchId, "Expired session must leave the last board inspectable.");
            }
        }

        private static async Task ExerciseTurnAndLostResponse(GameApi alice, GameApi bob, string id, CancellationToken token)
        {
            var first = await alice.Get<PlayerView>($"api/matches/{id}", token);
            Assert.AreEqual(GamePhase.Fellowship, first.Phase);
            Assert.AreEqual(2, first.Hand.Count);
            var command = new GameCommand
            {
                MatchId = id,
                CommandId = Guid.NewGuid().ToString("N"),
                ExpectedRevision = first.Revision,
                DecisionId = first.Decision.Id,
                Kind = CommandKind.ContinueFellowship
            };
            var sender = new CommandSender(alice);
            try
            {
                await sender.Send(command, token);
                Assert.Fail("The HTTP boundary must drop the first committed command response.");
            }
            catch (HttpRequestException) { }
            Assert.AreEqual(command.CommandId, sender.Pending.CommandId);
            var receipt = await sender.Recover(token);
            Assert.IsTrue(receipt.Accepted);
            Assert.AreEqual(first.Revision + 1, receipt.Revision);
            Assert.IsNull(sender.Pending);
            var shadow = await bob.Get<PlayerView>($"api/matches/{id}", token);
            Assert.AreEqual(DecisionKind.AllocateHunt, shadow.Decision.Kind);
            var allocation = new GameCommand
            {
                MatchId = id,
                CommandId = Guid.NewGuid().ToString("N"),
                ExpectedRevision = shadow.Revision,
                DecisionId = shadow.Decision.Id,
                Kind = CommandKind.AllocateHunt,
                Payload = new CommandPayload { Ids = shadow.Options.Single().EligibleIds.Take(1).ToList() }
            };
            Assert.IsTrue((await bob.Post<CommandReceipt>($"api/matches/{id}/commands", allocation, token)).Accepted);
            var actions = await alice.Get<PlayerView>($"api/matches/{id}", token);
            Assert.AreEqual(GamePhase.ActionResolution, actions.Phase);
            Assert.AreEqual(Side.FreePeoples, actions.DecisionOwner);
            Assert.AreEqual(4, actions.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available));
        }

        private sealed class DropReceiptHandler : DelegatingHandler
        {
            private bool dropped;
            public DropReceiptHandler() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                var response = await base.SendAsync(request, cancellation);
                if (!dropped && request.RequestUri.AbsolutePath.EndsWith("/commands", StringComparison.Ordinal))
                {
                    dropped = true;
                    response.Dispose();
                    throw new HttpRequestException("Injected connection loss after the real server committed.");
                }
                return response;
            }
        }

        private static async Task Until(Func<bool> condition, CancellationToken cancellation)
        {
            while (!condition())
                await Task.Delay(50, cancellation);
        }

        private sealed class Fixture
        {
            public string Address;
            public string AccountA;
            public string AccountB;
            public string AccountPassword;
        }
    }
}
