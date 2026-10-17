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
    // OBS-04 (OB03): through the real server — a public game is observed from its row and a private one by code;
    // an observer chats under their role, takes a free seat and goes back to the audience; when two observers reach
    // for the same seat, one gets it and the other is told; the host removes an observer, who returns to the menu.
    public sealed class ObserverRoomNetworkTests
    {
        [UnityTest]
        public IEnumerator ObserversJoinChatTakeSeatsAndAreRemovedThroughTheMenu()
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
            var storage = Path.Combine(Path.GetTempPath(), "wotr-observe-" + Guid.NewGuid().ToString("N"));
            var hostApi = await Account(address, "obsh", token);
            var firstApi = await Account(address, "obsa", token);
            var secondApi = await Account(address, "obsb", token);
            var room = await hostApi.Post<RoomView>("api/rooms", new CreateRoomRequest
                { Name = "Watched table", Visibility = RoomVisibility.Public, Side = Side.FreePeoples }, token);
            using (var host = new MountedFlowClient(hostApi, storage))
            using (var first = new MountedFlowClient(firstApi, storage))
            using (var second = new MountedFlowClient(secondApi, storage))
            {
                await WaitingRoomNetwork.Enter(host, room, token);
                await ClickReady(first.Root, "observe-public-" + room.Id, token);
                await Until(() => first.Root.Q("waiting-room") != null, token);
                Assert.IsFalse(first.Root.Q<Button>("room-ready").enabledSelf, "An observer does not get ready.");
                await Until(() => host.Root.Q("observer-" + firstApi.Username) != null, token);

                await Say(first, "Watching closely", token);
                await Until(() => ChatHas(host, firstApi.Username + " (observer):"), token);

                var code = (await hostApi.Post<RoomView>("api/rooms/" + room.Id + "/invitation", null, token)).JoinCode;
                second.Root.Q<TextField>("table-invitation").value = code;
                await ClickReady(second.Root, "observe-invitation", token);
                await Until(() => second.Root.Q("waiting-room") != null, token);
                await Until(() => host.Root.Q<Label>("waiting-observers-title")?.text == "Observers 2/16", token);

                // Both observers reach for the one free seat in the same frame: one sits, the other is told why.
                Click(first.Root.Q<Button>("seat-shadow-take"));
                Click(second.Root.Q<Button>("seat-shadow-take"));
                await Until(() => Seated(first, firstApi) || Seated(second, secondApi), token);
                var (winner, loser, winnerApi) = Seated(first, firstApi) ? (first, second, firstApi) : (second, first, secondApi);
                await Until(() => loser.Root.Q<Label>("waiting-status").text == "That seat was just taken.", token);
                await Until(() => loser.Root.Q<Label>("seat-shadow-player").text == winnerApi.Username, token);
                await Until(() => host.Root.Q<Label>("seat-shadow-player").text == winnerApi.Username, token);

                await ClickReady(winner.Root, "room-observe", token);
                await Until(() => winner.Root.Q<Label>("seat-shadow-player").text == "Waiting for a player…", token);
                Assert.IsFalse(winner.Root.Q<Button>("room-ready").enabledSelf);

                await Until(() => host.Root.Q<Button>("remove-observer-" + winnerApi.Username)?.enabledSelf == true, token);
                await ClickReady(host.Root, "remove-observer-" + winnerApi.Username, token);
                await Until(() => winner.Root.Q("main-menu") != null, token);
                Assert.AreEqual("The host removed you from this room.", winner.Root.Q<Label>("lobby-status").text);
            }
            if (Directory.Exists(storage))
                Directory.Delete(storage, true);
        }

        private static async Task<GameApi> Account(string address, string prefix, CancellationToken token)
        {
            var api = new GameApi(address);
            await TestAccounts.Register(api, prefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            return api;
        }

        private static async Task Say(MountedFlowClient client, string text, CancellationToken token)
        {
            client.Root.Q<TextField>("room-chat-input").value = text;
            await ClickReady(client.Root, "room-chat-send", token);
        }

        private static bool ChatHas(MountedFlowClient client, string author) =>
            client.Root.Query<Label>(className: "room-chat-author").ToList().Exists(label => label.text == author);

        private static bool Seated(MountedFlowClient client, GameApi api) =>
            client.Root.Q<Label>("seat-shadow-player")?.text == api.Username && client.Root.Q("seat-shadow").ClassListContains("yours");
    }
}
