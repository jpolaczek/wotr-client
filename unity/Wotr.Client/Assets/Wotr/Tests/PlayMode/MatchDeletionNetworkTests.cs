using System;
using System.Collections;
using System.IO;
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
    // MENU-32: the board menu offers no Delete game, but a game deleted on the server (MENU-21) still reaches every
    // open board: each player sees "Game deleted" and returns to the main menu, which lists the game no more.
    public sealed class MatchDeletionNetworkTests
    {
        [UnityTest]
        public IEnumerator OnePlayerDeletesTheGameAndTheOtherIsSentBackToTheMenu()
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
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var token = timeout.Token;
            var storage = Path.Combine(Path.GetTempPath(), "wotr-delete-" + Guid.NewGuid().ToString("N"));
            var hostApi = new GameApi(address);
            var guestApi = new GameApi(address);
            await TestAccounts.Register(hostApi, "delete_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
            await TestAccounts.Register(guestApi, "deleted_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
            var match = await StartMatch(hostApi, guestApi, token);
            using (var host = new MountedFlowClient(hostApi, storage))
            using (var guest = new MountedFlowClient(guestApi, storage))
            {
                await Open(host, match, token);
                await Open(guest, match, token);
                await ClickReady(host.Root, "open-board-menu", token);
                Assert.IsNull(host.Root.Q<Button>("board-menu-delete"), "No Delete game in the board menu.");
                Assert.AreEqual("Exit match", host.Root.Q<Button>("back-to-games").text);
                host.Board.Menu.Close();
                await hostApi.Delete("api/matches/" + Uri.EscapeDataString(match), token);
                foreach (var client in new[] { host, guest })
                {
                    await Until(() => client.Board?.GameDeleted.IsOpen == true, token);
                    Assert.AreEqual("Game deleted", client.Board.Connection.text);
                    await ClickReady(client.Root, "game-deleted-menu", token);
                    await Until(() => client.Board == null && client.Root.Q("main-menu") != null, token);
                }
                foreach (var (client, api) in new[] { (host, hostApi), (guest, guestApi) })
                {
                    var list = await api.Get<MatchListView>("api/matches", token);
                    Assert.IsFalse(list.Matches.Exists(summary => summary.MatchId == match), "The server lists the game no more.");
                    await ShowList(client.Root, "resume-match-" + match, token);
                    // The menu loads its lists as it opens; give that first load time to finish.
                    await Task.Delay(1000, token);
                    Assert.IsNull(client.Root.Q<Button>("resume-match-" + match), "The deleted game is gone from My games.");
                }
                Assert.AreEqual(404, await Status(() => guestApi.Get<PlayerView>("api/matches/" + match, token)));
            }
            Directory.Delete(storage, true);
        }

        private static async Task<int> Status(Func<Task> request)
        {
            try
            {
                await request();
                return 200;
            }
            catch (ApiException error)
            {
                return error.Status;
            }
        }

        private static async Task<string> StartMatch(GameApi hostApi, GameApi guestApi, CancellationToken token)
        {
            var room = await hostApi.Post<RoomView>("api/rooms", new CreateRoomRequest { Side = Side.FreePeoples }, token);
            await guestApi.Post<RoomView>("api/rooms/join", new JoinRoomRequest { Code = room.JoinCode }, token);
            var version = new RoomRevisionRequest { ExpectedVersion = room.Version };
            await hostApi.Post<RoomView>("api/rooms/" + room.Id + "/confirm", version, token);
            await guestApi.Post<RoomView>("api/rooms/" + room.Id + "/confirm", version, token);
            var started = await hostApi.Post<RoomView>("api/rooms/" + room.Id + "/start", version, token);
            Assert.IsNotEmpty(started.MatchId);
            return started.MatchId;
        }
    }
}
