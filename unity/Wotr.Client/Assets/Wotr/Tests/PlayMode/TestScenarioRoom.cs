using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-169/UI-168: a TEST-03 test session from PlayMode — the owner's account plays its side and the seat session
    // the other, both boards live — for situations that the test scenario catalog builds on demand.
    internal sealed class TestScenarioRoom
    {
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        private readonly GameApi owner;
        private readonly GameApi seat;
        private readonly TestSessionView view;
        private readonly string address;
        private readonly MountedMatchClient free;
        private readonly MountedMatchClient shadow;
        private CancellationToken Token => lifetime.Token;

        private TestScenarioRoom(string address, GameApi owner, TestSessionView view)
        {
            this.address = address;
            this.owner = owner;
            this.view = view;
            seat = owner.WithSession(view.Seat);
            free = new MountedMatchClient(address, view.MatchId, api: Api(Side.FreePeoples));
            shadow = new MountedMatchClient(address, view.MatchId, api: Api(Side.Shadow));
        }

        public static async Task<TestScenarioRoom> Start(string scenario)
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real server integration.");
            var fixture = JObject.Parse(File.ReadAllText(path));
            var address = fixture["Address"].Value<string>();
            using var start = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            // The shared session of the fixture account: the server allows 20 logins a minute from one address.
            var api = await BattleTestSessions.Login(address, fixture["FreePeoplesUser"].Value<string>(), fixture["Password"].Value<string>(), start.Token);
            var view = await TestScenarioApi.Start(api, scenario, start.Token);
            var session = new TestScenarioRoom(address, api, view);
            await session.Connect(session.free);
            await session.Connect(session.shadow);
            return session;
        }

        public MountedMatchClient Client(Side side) => side == Side.FreePeoples ? free : shadow;

        public async Task<MountedMatchClient> Reopen(Side side)
        {
            var client = new MountedMatchClient(address, view.MatchId, api: Api(side));
            await Connect(client);
            return client;
        }

        public async Task Send(Side side, CommandKind kind, CommandPayload payload)
        {
            var current = Client(side).Board.View;
            var receipt = await Api(side).Post<CommandReceipt>($"api/matches/{current.MatchId}/commands", new GameCommand
            {
                MatchId = current.MatchId, ExpectedRevision = current.Revision, DecisionId = current.Decision.Id,
                CommandId = Guid.NewGuid().ToString("N"), Kind = kind, Payload = payload
            }, Token);
            Assert.IsTrue(receipt.Accepted, kind + ": " + receipt.Error);
        }

        public Task Settle() => Task.Delay(120, Token);

        public async Task Until(Func<bool> condition)
        {
            while (!condition())
                await Task.Delay(25, Token);
        }

        private GameApi Api(Side side) => side == view.OwnerSide ? owner : seat;

        private async Task Connect(MountedMatchClient client)
        {
            client.Connection.Start();
            await Until(() => client.Connected > 0 && client.Board.View != null);
            await Settle();
        }

        // Ends the test session on the server (the test match is removed) and closes the boards; the owner's
        // session stays shared with the other tests.
        public async Task End()
        {
            free.Dispose();
            shadow.Dispose();
            try
            {
                using var end = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await TestScenarioApi.End(owner, end.Token);
            }
            finally
            {
                seat.Dispose();
                lifetime.Dispose();
            }
        }
    }
}
