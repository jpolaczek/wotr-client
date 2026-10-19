using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal sealed class UI10TestRoom : IDisposable
    {
        public MountedMatchClient Fp { get; private set; }
        public MountedMatchClient Sh { get; private set; }
        public MountedMatchClient OtherFp { get; private set; }
        public UI10HttpBoundary Boundary { get; } = new UI10HttpBoundary();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(70));
        public CancellationToken Token => lifetime.Token;

        public static async Task<UI10TestRoom> Connect(string scenario, bool secondDevice = false, bool shadowBoundary = false)
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real HTTP/WebSocket and PostgreSQL integration.");
            var fixture = JsonConvert.DeserializeObject<Fixture>(File.ReadAllText(path));
            var room = new UI10TestRoom();
            var match = fixture.InteractionMatches[scenario];
            try
            {
                room.Boundary.MatchPath = "/api/matches/" + match;
                room.Fp = new MountedMatchClient(fixture.Address, match, shadowBoundary ? null : room.Boundary);
                room.Sh = new MountedMatchClient(fixture.Address, match, shadowBoundary ? room.Boundary : null);
                await room.Login(room.Fp, fixture.FreePeoplesUser, fixture.Password);
                await room.Login(room.Sh, fixture.ShadowUser, fixture.Password);
                if (secondDevice)
                {
                    room.OtherFp = new MountedMatchClient(fixture.Address, match);
                    await room.Login(room.OtherFp, fixture.FreePeoplesUser, fixture.Password);
                }
                await room.Until(() => room.Clients.All(c => c.Connected > 0));
                await room.Settle();
                return room;
            }
            catch
            {
                room.Dispose();
                throw;
            }
        }

        private IEnumerable<MountedMatchClient> Clients => OtherFp == null
            ? new[] { Fp, Sh } : new[] { Fp, Sh, OtherFp };

        private async Task Login(MountedMatchClient client, string user, string password)
        {
            await client.Api.Login(user, password, Token);
            client.Connection.Start();
        }

        public Task Settle() => Task.Delay(80, Token);

        public async Task Until(Func<bool> condition)
        {
            while (!condition())
                await Task.Delay(30, Token);
        }

        public async Task At(long revision)
        {
            await Until(() => Clients.All(c => c.Board.View?.Revision == revision));
            await Settle();
        }

        public static void Click(MountedMatchClient client, string name) =>
            CardNetworkTests.Click(client.Board.Tray.Q<Button>(name) ?? client.Board.Console.Status.Options.Q<Button>(name));

        public async Task SelectDrawCost(MountedMatchClient client, string die = null)
        {
            die ??= client.Board.View.Dice.First(d => d.Owner == client.Board.View.You && d.Location == DieLocation.Available).Id;
            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>(die));
            await CardGestures.Open(client);
            await Until(() => client.Board.Decks.Root.Q<Button>("deck-character")?.enabledInHierarchy == true);
            await Settle();
        }

        public void Draw(MountedMatchClient client) => CardNetworkTests.Click(client.Board.Decks.Root.Q<Button>("deck-character"));

        public async Task DiscardShadowDie()
        {
            var die = Sh.Board.View.Dice.First(d => d.Owner == Sh.Board.View.You && d.Location == DieLocation.Available);
            CardNetworkTests.Click(Sh.Board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(80, Token);
            await ComponentGestures.Select(Sh, "DiscardDie");
        }

        public static void Key(VisualElement root, bool down)
        {
            var evt = new Event { type = down ? EventType.KeyDown : EventType.KeyUp, keyCode = KeyCode.Return };
            if (down)
            {
                using (var key = KeyDownEvent.GetPooled(evt))
                    root.SendEvent(key);
            }
            else
            {
                using (var key = KeyUpEvent.GetPooled(evt))
                    root.SendEvent(key);
            }
        }

        public void Dispose()
        {
            Boundary.ReleaseCommand();
            Boundary.ReleasePreview();
            Boundary.ReleaseView();
            OtherFp?.Dispose();
            Sh?.Dispose();
            Fp?.Dispose();
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private sealed class Fixture
        {
            public string Address, FreePeoplesUser, ShadowUser, Password;
            public Dictionary<string, string> InteractionMatches;
        }
    }
}
