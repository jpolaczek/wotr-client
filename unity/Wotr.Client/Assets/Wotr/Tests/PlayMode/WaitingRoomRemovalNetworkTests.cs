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
    // MENU-25 (WR04): the guest sees Start game and Remove greyed out in their places; the host removes the guest, who
    // returns to the main menu with the reason and cannot join that room again.
    public sealed class WaitingRoomRemovalNetworkTests
    {
        [UnityTest]
        public IEnumerator TheHostRemovesTheGuestWhoReturnsToTheMenuWithTheReason()
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
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var token = timeout.Token;
            var storage = Path.Combine(Path.GetTempPath(), "wotr-room-" + Guid.NewGuid().ToString("N"));
            var (hostApi, guestApi, room) = await WaitingRoomNetwork.Table(address, "remove", token);
            using (var host = new MountedFlowClient(hostApi, storage))
            using (var guest = new MountedFlowClient(guestApi, storage))
            {
                await WaitingRoomNetwork.Enter(host, room, token);
                await WaitingRoomNetwork.Enter(guest, room, token);
                Assert.IsFalse(guest.Root.Q<Button>("start-game").enabledSelf, "Only the host starts.");
                Assert.IsFalse(guest.Root.Q<Button>("seat-shadow-remove").enabledSelf, "Only the host removes.");
                Assert.AreEqual("Back", guest.Root.Q<Button>("waiting-back").text);
                Assert.IsTrue(host.Root.Q<Button>("seat-shadow-remove").enabledSelf);

                await ClickReady(host.Root, "seat-shadow-remove", token);

                await Until(() => host.Root.Q<Label>("seat-shadow-player")?.text == "Waiting for a player…", token);
                await Until(() => guest.Root.Q("main-menu") != null, token);
                Assert.AreEqual("The host removed you from this room.", guest.Root.Q<Label>("lobby-status").text);
                var code = (await hostApi.Post<RoomView>("api/rooms/" + room.Id + "/invitation", null, token)).JoinCode;
                var refused = 0;
                try { await guestApi.Post<RoomView>("api/rooms/join", new JoinRoomRequest { Code = code }, token); }
                catch (ApiException error) { refused = error.Status; }
                Assert.AreEqual(409, refused, "A removed player cannot join that room again.");
            }
            if (Directory.Exists(storage))
                Directory.Delete(storage, true);
        }
    }

    // Two signed-in players and a waiting room both sit in, opened through the main menu.
    internal static class WaitingRoomNetwork
    {
        public static async Task<(GameApi Host, GameApi Guest, RoomView Room)> Table(string address, string prefix, CancellationToken token)
        {
            var hostApi = new GameApi(address);
            var guestApi = new GameApi(address);
            await TestAccounts.Register(hostApi, prefix + "h_" + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            await TestAccounts.Register(guestApi, prefix + "g_" + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            var room = await hostApi.Post<RoomView>("api/rooms", new CreateRoomRequest { Side = Side.FreePeoples }, token);
            await guestApi.Post<RoomView>("api/rooms/join", new JoinRoomRequest { Code = room.JoinCode }, token);
            return (hostApi, guestApi, room);
        }

        public static async Task Enter(MountedFlowClient client, RoomView room, CancellationToken token)
        {
            await ClickReady(client.Root, "open-room-" + room.Id, token);
            await Until(() => client.Root.Q("waiting-room") != null, token);
        }
    }
}
