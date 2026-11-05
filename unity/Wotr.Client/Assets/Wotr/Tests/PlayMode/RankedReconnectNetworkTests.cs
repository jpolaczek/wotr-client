using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-504 (docs/ui/UI-12-RETURN.md, RANKED_UX): a lost connection in a ranked game costs only the time it lasts. One client
    // goes through SilentNetwork, which can stop passing bytes without closing anything, the way a pulled cable behaves. The
    // server's clocks run at a tenth (scripts/test-client.sh): the Long preset's move is 30 s, the Fast preset's 9 s.
    public class RankedReconnectNetworkTests
    {
        private const string Queue = RankedSearchController.Route;

        [UnityTest]
        public IEnumerator ASilentNetworkIsNoticedBothClientsSaySoAndTheReturnIsClean() => Real(async (address, token) =>
        {
            var pair = await Paired(address, longest: true, token);
            using var network = new SilentNetwork(address);
            var cutApi = await SignedIn(network.Address, pair.OwnerName, token);
            using var cut = new MountedFlowClient(cutApi, Storage());
            using var other = new MountedFlowClient(pair.Other, Storage());
            await BothLive(cut, other, token);

            network.Silent = true;
            var cutAt = DateTime.UtcNow;
            await ReturnTestFixture.Until(() => cut.Board.Connection.text != "Live", token);
            Assert.Less((DateTime.UtcNow - cutAt).TotalSeconds, 9.5, "The status appears within about 8 s.");
            Assert.AreEqual(TopBarClock.Reconnecting, cut.Board.TopBar.Clock.Status.text);
            Assert.IsFalse(cut.Board.TopBar.Clock.PauseButton.enabledInHierarchy, "A pause says the connection is lost.");
            Assert.AreEqual(TopBarClock.PauseNoConnection, cut.Board.TopBar.Clock.PauseButton.tooltip);
            var before = cut.Root.Q<Label>("move-clock").text;
            await Task.Delay(1200, token);
            Assert.AreNotEqual(before, cut.Root.Q<Label>("move-clock").text, "The clock keeps counting.");

            await ReturnTestFixture.Until(() => other.Board.TopBar.Clock.Status.text == TopBarClock.OpponentReconnecting, token);
            Assert.Less((DateTime.UtcNow - cutAt).TotalSeconds, 12, "The other client says so within about 10 s.");

            network.Silent = false;
            var back = DateTime.UtcNow;
            await ReturnTestFixture.Until(() => cut.Board.Connection.text == "Live", token);
            Assert.Less((DateTime.UtcNow - back).TotalSeconds, 3, "Within about 2 s of its return the board is live.");
            Assert.AreEqual("", cut.Board.TopBar.Clock.Status.text);
            await ReturnTestFixture.Until(() => other.Board.TopBar.Clock.Status.text == "", token);
            var served = (await pair.Other.Get<PlayerView>("api/matches/" + pair.Match, token)).Clock;
            Assert.That(Seconds(cut.Root.Q<Label>("move-clock").text), Is.EqualTo(served.RemainingMilliseconds / 1000.0).Within(1.5),
                "The clock is correct to about a second.");

            await pair.Other.Post<CommandReceipt>($"api/matches/{pair.Match}/concede", new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, token);
        });

        [UnityTest]
        public IEnumerator AClockThatRunsOutWhileCutOffWarnsAndEndsOnReturnWithTheRating() => Real(async (address, token) =>
        {
            var pair = await Paired(address, longest: false, token);
            using var network = new SilentNetwork(address);
            var cutApi = await SignedIn(network.Address, pair.OwnerName, token);
            using var cut = new MountedFlowClient(cutApi, Storage());
            using var other = new MountedFlowClient(pair.Other, Storage());
            var heard = new List<SoundCue>();
            cut.Interface.Sounds.Policy.Played += heard.Add;
            await ReturnTestFixture.Until(() => cut.Board?.View?.Clock != null, token);

            network.Silent = true;
            await ReturnTestFixture.Until(() => other.Board?.View?.Winner != null, token);
            Assert.IsNull(cut.Board.View.Winner, "Cut off, it does not know yet.");
            Assert.AreEqual(1, heard.Count(cue => cue == SoundCue.ClockWarning), "The warning sounded while cut off.");

            network.Silent = false;
            await ReturnTestFixture.Until(() => cut.Board.GameEnd.IsOpen, token);
            StringAssert.EndsWith("You ran out of time", cut.Root.Q<Label>("game-end-subtitle").text);
            await ReturnTestFixture.Until(() => cut.Root.Q<Label>("game-end-rating") != null, token);
            StringAssert.StartsWith("Rating ", cut.Root.Q<Label>("game-end-rating").text);
            Assert.IsNull(cut.Root.Q("move-clock"), "A finished game has no clock.");
        });

        // The answer is lost, not the command: the server applied it, and the client must learn that rather than send it again.
        [UnityTest]
        public IEnumerator ACommandWhoseAnswerIsLostAppliesExactlyOnce() => Real(async (address, token) =>
        {
            var pair = await Paired(address, longest: true, token);
            var losing = new LosingAnswers();
            var api = new GameApi(address, losing);
            await api.Login(pair.OwnerName, TestAccounts.Password, token);
            using var client = new MountedFlowClient(api, Storage());
            await ReturnTestFixture.Until(() => client.Board?.View?.Decision != null && client.Board.Connection.text == "Live", token);
            var revision = client.Board.View.Revision;

            losing.LoseNextCommand = true;
            var finish = client.Root.Query<Button>().ToList().First(button => button.text == "Finish phase");
            ReturnTestFixture.Click(finish);
            await ReturnTestFixture.Until(() => losing.Lost == 1, token);
            await ReturnTestFixture.Until(() => client.Board.View.Revision > revision, token);
            await Task.Delay(1000, token);

            var served = await pair.Other.Get<PlayerView>("api/matches/" + pair.Match, token);
            Assert.AreEqual(revision + 1, served.Revision, "Applied once, not twice.");
            Assert.AreEqual(1, losing.CommandsSent, "It was not sent again: its receipt told.");
            await pair.Other.Post<CommandReceipt>($"api/matches/{pair.Match}/concede", new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, token);
        });

        // The opponent moves while one client is cut off: on return it shows the new state with no replay of those steps.
        [UnityTest]
        public IEnumerator TheReturnPlaysNoPresentationOfTheMissedSteps() => Real(async (address, token) =>
        {
            var pair = await Paired(address, longest: true, token);
            using var network = new SilentNetwork(address);
            var cutApi = await SignedIn(network.Address, pair.OtherName, token);
            using var cut = new MountedFlowClient(cutApi, Storage());
            await ReturnTestFixture.Until(() => cut.Board?.View?.Clock != null && cut.Board.Connection.text == "Live", token);

            network.Silent = true;
            await ReturnTestFixture.Until(() => cut.Board.Connection.text != "Live", token);
            var ownerView = await pair.Owner.Get<PlayerView>("api/matches/" + pair.Match, token);
            var receipt = await pair.Owner.Post<CommandReceipt>($"api/matches/{pair.Match}/commands", new GameCommand
            {
                MatchId = pair.Match, CommandId = Guid.NewGuid().ToString("N"), DecisionId = ownerView.Decision.Id,
                ExpectedRevision = ownerView.Revision, Kind = CommandKind.ContinueFellowship
            }, token);
            Assert.IsTrue(receipt.Accepted, receipt.Error);

            network.Silent = false;
            await ReturnTestFixture.Until(() => cut.Board.View.Revision >= receipt.Revision, token);
            Assert.IsFalse(cut.Board.Presenting, "The view applies quietly.");
            await ReturnTestFixture.Until(() => cut.Board.Connection.text == "Live", token);
            await Task.Delay(300, token);
            Assert.IsFalse(cut.Board.Presenting, "And nothing starts afterwards.");
            await pair.Other.Post<CommandReceipt>($"api/matches/{pair.Match}/concede", new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, token);
        });

        private sealed class Pair
        {
            public GameApi Owner;
            public GameApi Other;
            public string OwnerName;
            public string OtherName;
            public string Match;
        }

        // Two fresh accounts paired through the API; the first decision's owner is told apart from the other.
        private static async Task<Pair> Paired(string address, bool longest, CancellationToken token)
        {
            var a = await Account(address, "rr_a_", token);
            var b = await Account(address, "rr_b_", token);
            var search = new RankedSearchRequest
            {
                Lords = RankedChoice.Off, Kings = RankedChoice.Off, Fate = RankedChoice.Off, Standard = RankedChoice.Off,
                Long = longest ? RankedChoice.Enforce : RankedChoice.Off, Fast = longest ? RankedChoice.Off : RankedChoice.Enforce
            };
            await a.Post<RankedSearchView>(Queue, search, token);
            await b.Post<RankedSearchView>(Queue, search, token);
            string match = null;
            while (match == null)
            {
                var view = await a.Get<RankedSearchView>(Queue, token);
                await b.Get<RankedSearchView>(Queue, token);
                if (view.Status == RankedSearchStatus.Matched)
                    match = view.MatchId;
                else
                    await Task.Delay(200, token);
            }
            var first = await a.Get<PlayerView>("api/matches/" + match, token);
            var aOwns = first.Clock.Owner == first.You;
            return new Pair
            {
                Owner = aOwns ? a : b, Other = aOwns ? b : a, OwnerName = aOwns ? a.Username : b.Username,
                OtherName = aOwns ? b.Username : a.Username, Match = match
            };
        }

        private static async Task<GameApi> Account(string address, string prefix, CancellationToken token)
        {
            var api = new GameApi(address);
            await TestAccounts.Register(api, prefix + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            return api;
        }

        private static async Task<GameApi> SignedIn(string address, string name, CancellationToken token)
        {
            var api = new GameApi(address);
            await api.Login(name, TestAccounts.Password, token);
            return api;
        }

        private static Task BothLive(MountedFlowClient first, MountedFlowClient second, CancellationToken token) => ReturnTestFixture.Until(() =>
            first.Board?.View?.Clock != null && second.Board?.View?.Clock != null &&
            first.Board.Connection.text == "Live" && second.Board.Connection.text == "Live", token);

        private static int Seconds(string clock)
        {
            var parts = clock.Split(' ').Last().Split(':');
            return int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
        }

        private static string Storage() => Path.Combine(Path.GetTempPath(), "wotr-return-" + Guid.NewGuid().ToString("N"));

        private static IEnumerator Real(Func<string, CancellationToken, Task> test)
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Real HTTP fixture required.");
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var task = test(JObject.Parse(File.ReadAllText(path))["Address"].Value<string>(), timeout.Token);
            while (!task.IsCompleted)
                yield return null;
            timeout.Dispose();
            task.GetAwaiter().GetResult();
        }

        // Passes the next command to the server and then loses its answer, as a network dropping at that moment does.
        private sealed class LosingAnswers : HttpClientHandler
        {
            public bool LoseNextCommand;
            public int Lost;
            public int CommandsSent;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var command = request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath.EndsWith("/commands");
                if (command)
                    CommandsSent++;
                var response = await base.SendAsync(request, cancellationToken);
                if (!command || !LoseNextCommand)
                    return response;
                LoseNextCommand = false;
                Lost++;
                response.Dispose();
                throw new HttpRequestException("The answer was lost.");
            }
        }
    }

    // A TCP relay that can go silent: it holds every byte without closing anything, so neither end is told, as with a pulled
    // cable. A new connection during the silence waits too.
    internal sealed class SilentNetwork : IDisposable
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly Uri target;
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly List<TcpClient> connections = new List<TcpClient>();

        public SilentNetwork(string address)
        {
            target = new Uri(address);
            listener.Start();
            _ = Accept();
        }

        public volatile bool Silent;

        public string Address => "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;

        public void Dispose()
        {
            stop.Cancel();
            listener.Stop();
            lock (connections)
                foreach (var connection in connections)
                    connection.Close();
        }

        private async Task Accept()
        {
            try
            {
                while (!stop.IsCancellationRequested)
                    _ = Relay(await listener.AcceptTcpClientAsync());
            }
            catch (Exception error) when (error is ObjectDisposedException or SocketException or InvalidOperationException) { }
        }

        private async Task Relay(TcpClient near)
        {
            try
            {
                await UntilAudible();
                var far = new TcpClient();
                lock (connections)
                {
                    connections.Add(near);
                    connections.Add(far);
                }
                await far.ConnectAsync(target.Host, target.Port);
                await Task.WhenAny(Pump(near.GetStream(), far.GetStream()), Pump(far.GetStream(), near.GetStream()));
                near.Close();
                far.Close();
            }
            catch (Exception error) when (error is ObjectDisposedException or SocketException or IOException or OperationCanceledException or InvalidOperationException) { }
        }

        private async Task Pump(Stream from, Stream to)
        {
            var buffer = new byte[16384];
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    var read = await from.ReadAsync(buffer, 0, buffer.Length, stop.Token);
                    if (read == 0)
                        return;
                    await UntilAudible();
                    await to.WriteAsync(buffer, 0, read, stop.Token);
                }
            }
            catch (Exception error) when (error is ObjectDisposedException or IOException or OperationCanceledException) { }
        }

        private async Task UntilAudible()
        {
            while (Silent)
                await Task.Delay(25, stop.Token);
        }
    }
}
