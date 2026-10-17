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

namespace Wotr.PlayTests
{
    public class MainMenuNetworkTests
    {
        [UnityTest]
        public IEnumerator HostAndJoinPublicGameThroughMainMenu()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path)) Assert.Ignore("Real HTTP fixture required.");
            var task = Run(JObject.Parse(File.ReadAllText(path))["Address"].Value<string>());
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }
        [UnityTest]
        public IEnumerator PrivateInvitationLeaveAndCancelUseTheServer()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path)) Assert.Ignore("Real HTTP fixture required.");
            var task = Private(JObject.Parse(File.ReadAllText(path))["Address"].Value<string>());
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }
        private static async Task Private(string address)
        {
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
            {
                var token = timeout.Token;
                var storage = Path.Combine(Path.GetTempPath(), "wotr-menu-" + Guid.NewGuid().ToString("N"));
                var hostApi = new GameApi(address);
                var guestApi = new GameApi(address);
                await TestAccounts.Register(hostApi, "private_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
                await TestAccounts.Register(guestApi, "invite_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
                using (var host = new MountedFlowClient(hostApi, storage))
                using (var guest = new MountedFlowClient(guestApi, storage))
                {
                    await ReturnTestFixture.ClickReady(host.Root, "host-game", token);
                    await ReturnTestFixture.ClickReady(host.Root, "host-private", token);
                    await ReturnTestFixture.ClickReady(host.Root, "submit-host", token);
                    await ReturnTestFixture.ClickReady(host.Root, "generate-room-invitation", token);
                    var code = host.Root.Q<TextField>("room-invitation").value;
                    Assert.IsNotEmpty(code);
                    var own = await hostApi.Get<System.Collections.Generic.List<RoomView>>("api/rooms", token);
                    var id = own[0].Id;
                    var publicRooms = await guestApi.Get<PublicRoomListView>("api/rooms/public", token);
                    Assert.IsFalse(publicRooms.Rooms.Exists(room => room.Id == id));
                    guest.Root.Q<TextField>("table-invitation").value = code;
                    await ReturnTestFixture.ClickReady(guest.Root, "join-invitation", token);
                    await ReturnTestFixture.Until(() => guest.Root.Q("waiting-room") != null, token);
                    // MENU-25: the guest sees Start game in its place, greyed out.
                    Assert.IsFalse(guest.Root.Q<Button>("start-game").enabledSelf);
                    await ReturnTestFixture.ClickReady(guest.Root, "waiting-back", token);
                    await ReturnTestFixture.Until(() => guest.Root.Q("main-menu") != null, token);
                    await ReturnTestFixture.ClickReady(host.Root, "refresh-room", token);
                    await ReturnTestFixture.Until(() => host.Root.Q<TextField>("room-invitation").value.Length == 0, token);
                    await ReturnTestFixture.ClickReady(host.Root, "generate-room-invitation", token);
                    var newCode = host.Root.Q<TextField>("room-invitation").value;
                    Assert.AreNotEqual(code, newCode);
                    guest.Root.Q<TextField>("table-invitation").value = newCode;
                    await ReturnTestFixture.ClickReady(guest.Root, "join-invitation", token);
                    await ReturnTestFixture.Until(() => guest.Root.Q("waiting-room") != null, token);
                    await ReturnTestFixture.ClickReady(host.Root, "waiting-back", token);
                    await ReturnTestFixture.Until(() => guest.Root.Q("main-menu") != null && host.Root.Q("main-menu") != null, token);
                }
            }
        }

        [UnityTest]
        public IEnumerator LegacyWaitingRoomUsesTheDefaultMenuWithoutExpansionControls()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path)) Assert.Ignore("Real HTTP fixture required.");
            var task = Legacy(JObject.Parse(File.ReadAllText(path))["Address"].Value<string>());
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }
        private static async Task Legacy(string address)
        {
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
            {
                var token = timeout.Token;
                var storage = Path.Combine(Path.GetTempPath(), "wotr-menu-" + Guid.NewGuid().ToString("N"));
                var hostApi = new GameApi(address);
                var guestApi = new GameApi(address);
                await TestAccounts.Register(hostApi, "legacy_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
                await TestAccounts.Register(guestApi, "old_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
                var room = await hostApi.Post<RoomView>("api/rooms", new CreateRoomRequest { Side = Side.FreePeoples }, token);
                await guestApi.Post<RoomView>("api/rooms/join", new JoinRoomRequest { Code = room.JoinCode }, token);
                room = await hostApi.Put<RoomView>("api/rooms/" + room.Id + "/configuration",
                    new ConfigureRoomRequest { ExpectedVersion = room.Version, Configuration = new MatchConfiguration { Lords = true } }, token);
                Assert.IsFalse(room.Managed);
                using (var host = new MountedFlowClient(hostApi, storage))
                using (var guest = new MountedFlowClient(guestApi, storage))
                {
                    await ReturnTestFixture.ClickReady(host.Root, "open-room-" + room.Id, token);
                    await ReturnTestFixture.ClickReady(guest.Root, "open-room-" + room.Id, token);
                    await ReturnTestFixture.Until(() => host.Root.Q("waiting-room") != null && guest.Root.Q("waiting-room") != null, token);
                    Assert.IsEmpty(host.Root.Q("waiting-room").Query<Toggle>().ToList());
                    Assert.IsEmpty(guest.Root.Q("waiting-room").Query<Toggle>().ToList());
                    // MENU-25: the guest sees Start game in its place, greyed out.
                    Assert.IsFalse(guest.Root.Q<Button>("start-game").enabledSelf);
                    var saved = await hostApi.Get<RoomView>("api/rooms/" + room.Id, token);
                    Assert.IsTrue(saved.Configuration.Lords, "Opening the menu must preserve the saved configuration.");
                    await ReturnTestFixture.ClickReady(guest.Root, "waiting-back", token);
                    await ReturnTestFixture.ClickReady(host.Root, "waiting-back", token);
                }
            }
        }

        private static async Task Run(string address)
        {
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
            {
                var token = timeout.Token;
                var storage = Path.Combine(Path.GetTempPath(), "wotr-menu-" + Guid.NewGuid().ToString("N"));
                var hostApi = new GameApi(address);
                var guestApi = new GameApi(address);
                await TestAccounts.Register(hostApi, "host_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
                await TestAccounts.Register(guestApi, "guest_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
                using (var host = new MountedFlowClient(hostApi, storage))
                using (var guest = new MountedFlowClient(guestApi, storage))
                {
                    await ReturnTestFixture.ClickReady(host.Root, "host-game", token);
                    host.Root.Q<TextField>("room-name").value = "Menu integration game";
                    await ReturnTestFixture.ClickReady(host.Root, "submit-host", token);
                    await ReturnTestFixture.Until(() => host.Root.Q("main-menu") == null, token);
                    var rooms = await hostApi.Get<System.Collections.Generic.List<RoomView>>("api/rooms", token);
                    Assert.AreEqual(1, rooms.Count);
                    var id = rooms[0].Id;
                    await ReturnTestFixture.ClickReady(host.Root, "generate-room-invitation", token);
                    var firstCode = host.Root.Q<TextField>("room-invitation").value;
                    var clipboard = UnityEngine.GUIUtility.systemCopyBuffer;
                    try
                    {
                        await ReturnTestFixture.ClickReady(host.Root, "copy-room-invitation", token);
                        Assert.AreEqual(firstCode, UnityEngine.GUIUtility.systemCopyBuffer);
                    }
                    finally { UnityEngine.GUIUtility.systemCopyBuffer = clipboard; }
                    await ReturnTestFixture.ClickReady(guest.Root, "join-public-" + id, token);
                    await ReturnTestFixture.ClickReady(guest.Root, "waiting-back", token);
                    await ReturnTestFixture.Until(() => guest.Root.Q("main-menu") != null, token);
                    await ReturnTestFixture.ClickReady(host.Root, "refresh-room", token);
                    await ReturnTestFixture.ClickReady(host.Root, "generate-room-invitation", token);
                    var code = host.Root.Q<TextField>("room-invitation").value;
                    Assert.AreNotEqual(firstCode, code);
                    guest.Root.Q<TextField>("table-invitation").value = code;
                    await ReturnTestFixture.ClickReady(guest.Root, "join-invitation", token);
                    await ReturnTestFixture.Until(() => guest.Root.Q("main-menu") == null, token);
                    var joined = await guestApi.Get<RoomView>("api/rooms/" + id, token);
                    Assert.AreEqual(Side.Shadow, joined.You);
                    Assert.AreEqual("Menu integration game", joined.Name);
                    Assert.IsFalse(joined.Configuration.Lords);
                    await ReturnTestFixture.ClickReady(host.Root, "refresh-room", token);
                    await ReturnTestFixture.ClickReady(host.Root, "room-ready", token);
                    await ReturnTestFixture.ClickReady(host.Root, "room-ready", token);
                    Assert.AreEqual("Ready", host.Root.Q<Button>("room-ready").text);
                    await ReturnTestFixture.ClickReady(host.Root, "room-ready", token);
                    await ReturnTestFixture.ClickReady(guest.Root, "room-ready", token);
                    await ReturnTestFixture.ClickReady(host.Root, "start-game", token);
                    await ReturnTestFixture.Until(() => host.Board?.View != null && guest.Board?.View != null, token);
                    var matchId = host.Board.View.MatchId;
                    Assert.AreEqual(matchId, guest.Board.View.MatchId);
                    var revision = host.Board.View.Revision;
                    await ReturnTestFixture.ClickReady(host.Root, "back-to-games", token);
                    await ReturnTestFixture.ClickReady(guest.Root, "back-to-games", token);
                    await ReturnTestFixture.Until(() => host.Root.Q("main-menu") != null && guest.Root.Q("main-menu") != null, token);
                    await ReturnTestFixture.Open(host, matchId, token);
                    await ReturnTestFixture.Open(guest, matchId, token);
                    Assert.AreEqual(revision, host.Board.View.Revision);
                    Assert.AreEqual(revision, guest.Board.View.Revision);
                }
            }
        }
    }
}
