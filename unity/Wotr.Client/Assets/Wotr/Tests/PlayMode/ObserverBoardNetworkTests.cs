using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
    // OBS-03/OBS-05 (OB05, OB06, OB08) through the real server: an observer opens a game in progress from My games.
    // The board shows the Free Peoples' hand under the viewing bar, a click on the map opens the region; a change to
    // the Shadow keeps the camera and shows the Shadow's hand; a player's move arrives live and a reconnect shows the
    // current state. The observer's client sends no command, preview, receipt or read marker, and Exit match frees
    // the place in the audience.
    public sealed class ObserverBoardNetworkTests
    {
        [UnityTest]
        public IEnumerator AnObserverWatchesBothSidesLiveWithoutSendingAnything()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Real HTTP fixture required.");
            var task = Run(JObject.Parse(File.ReadAllText(path))["Address"].Value<string>());
            while (!task.IsCompleted)
                yield return null;
            task.GetAwaiter().GetResult();
        }

        private static async Task Run(string address)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var storage = Path.Combine(Path.GetTempPath(), "wotr-observe-board-" + Guid.NewGuid().ToString("N"));
            var recorder = new Recorder();
            var host = await Account(new GameApi(address), "obvh", token);
            var guest = await Account(new GameApi(address), "obvg", token);
            var watcher = await Account(new GameApi(address, recorder), "obvw", token);
            var match = await StartWatched(host, guest, watcher, token);
            using (var client = new MountedFlowClient(watcher, storage))
            {
                await ClickReady(client.Root, "open-room-" + match.Room, token);
                await Until(() => client.Board?.View != null && client.Flow.Observer != null, token);
                var board = client.Board;
                Assert.IsTrue(board.Viewing.Visible);
                Assert.IsTrue(client.Root.Q<Button>("viewing-free").ClassListContains("selected"));
                Assert.AreEqual(Side.FreePeoples, board.View.You);
                CollectionAssert.AreEqual((await View(host, match.Id, token)).Hand, board.View.Hand);
                Assert.IsNull(board.View.Decision, "An observer's board holds no decision of its own.");
                Assert.IsEmpty(board.View.Options);
                await AssertPresenting(client, token);

                board.Map.Select("minas-tirith");
                await Until(() => board.Hover.Panel.Root.resolvedStyle.display == DisplayStyle.Flex, token);

                var camera = board.Map.Camera.Pan + "/" + board.Map.Camera.Zoom;
                await ClickReady(client.Root, "viewing-shadow", token);
                await Until(() => board.View.You == Side.Shadow, token);
                CollectionAssert.AreEqual((await View(guest, match.Id, token)).Hand, board.View.Hand);
                Assert.AreEqual(camera, board.Map.Camera.Pan + "/" + board.Map.Camera.Zoom, "A change of side keeps the camera.");
                await AssertPresenting(client, token);

                var before = board.View.Revision;
                await ContinueFellowship(host, match.Id, token);
                await Until(() => board.View.Revision > before, token);
                var live = board.View.Revision;
                client.Flow.Observer.Reconnect();
                await Until(() => board.Connection.text == "Live", token);
                Assert.AreEqual(live, board.View.Revision, "A reconnect shows the current state.");

                // CHAT-01: reading the board's chat is the observer's own traffic, not a player's.
                Assert.IsEmpty(recorder.Sent.Where(route => route.Contains("/commands") || route.Contains("/preview") ||
                    route.Contains("/receipts") || route.Contains("/seen") ||
                    route.StartsWith("/api/matches/" + match.Id) && !route.EndsWith("/chat")),
                    "Watching sends nothing that a player's board would.");

                await ClickReady(client.Root, "open-board-menu", token);
                await ClickReady(client.Root, "back-to-games", token);
                await Until(() => client.Board == null && client.Root.Q("main-menu") != null, token);
                var rooms = await watcher.Get<List<RoomView>>("api/rooms", token);
                Assert.IsFalse(rooms.Exists(room => room.Id == match.Room), "Exit match frees the place in the audience.");
            }
            if (Directory.Exists(storage))
                Directory.Delete(storage, true);
        }

        // UI-330: the watched side's presentation has loaded its history, shows every side's actions and plays live.
        private static async Task AssertPresenting(MountedFlowClient client, CancellationToken token)
        {
            await Until(() => client.Flow.Observer.PresentationLoaded && client.Flow.Observer.Playback?.Live == true, token);
            Assert.IsTrue(client.Flow.Observer.PresentsEverySide, "An observer is shown both sides' actions.");
        }

        private static async Task<GameApi> Account(GameApi api, string prefix, CancellationToken token)
        {
            await TestAccounts.Register(api, prefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            return api;
        }

        // A public room the guest joins and the watcher observes; both players get ready and the host starts it.
        private static async Task<(string Room, string Id)> StartWatched(GameApi host, GameApi guest, GameApi watcher, CancellationToken token)
        {
            var room = await host.Post<RoomView>("api/rooms", new CreateRoomRequest
                { Name = "Live watch", Visibility = RoomVisibility.Public, Side = Side.FreePeoples }, token);
            await guest.Post<RoomView>("api/rooms/" + room.Id + "/join", null, token);
            await watcher.Post<RoomView>("api/rooms/" + room.Id + "/join?role=Observer", null, token);
            var version = (await host.Get<RoomView>("api/rooms/" + room.Id, token)).Version;
            await host.Post<RoomView>("api/rooms/" + room.Id + "/confirm", new RoomRevisionRequest { ExpectedVersion = version }, token);
            await guest.Post<RoomView>("api/rooms/" + room.Id + "/confirm", new RoomRevisionRequest { ExpectedVersion = version }, token);
            var started = await host.Post<RoomView>("api/rooms/" + room.Id + "/start", new RoomRevisionRequest { ExpectedVersion = version }, token);
            return (room.Id, started.MatchId);
        }

        private static Task<PlayerView> View(GameApi player, string match, CancellationToken token) =>
            player.Get<PlayerView>("api/matches/" + match, token);

        private static async Task ContinueFellowship(GameApi player, string match, CancellationToken token)
        {
            var view = await View(player, match, token);
            var receipt = await player.Post<CommandReceipt>("api/matches/" + match + "/commands", new GameCommand
            {
                MatchId = match, ExpectedRevision = view.Revision, DecisionId = view.Decision.Id,
                CommandId = Guid.NewGuid().ToString("N"), Kind = CommandKind.ContinueFellowship
            }, token);
            Assert.IsTrue(receipt.Accepted, receipt.Error);
        }

        // Remembers every route the observer's client asks, without changing the answers.
        private sealed class Recorder : DelegatingHandler
        {
            private readonly List<string> sent = new List<string>();
            public Recorder() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }
            public IReadOnlyList<string> Sent
            {
                get { lock (sent) return sent.ToList(); }
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                lock (sent)
                    sent.Add(request.RequestUri.AbsolutePath);
                return base.SendAsync(request, token);
            }
        }
    }
}
