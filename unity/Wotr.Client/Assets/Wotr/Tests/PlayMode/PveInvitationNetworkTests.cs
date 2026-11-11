using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;
using static Wotr.PlayTests.PveTestFixture;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class PveInvitationNetworkTests
    {
        [UnityTest]
        public IEnumerator BothSidesUseReadyStartAndKeepEverySavedPveAndLegacyDemo() => Wait(Saves());
        [UnityTest]
        public IEnumerator LostResponseDoubleClickAndRemovalDoNotDuplicateOrResurrectCpu() => Wait(Retry());
        [UnityTest]
        public IEnumerator AHumanAdmissionRaceHasOnlyOneWinner() => Wait(Admission());
        [UnityTest]
        public IEnumerator ConfigurationChangesRequireNewCpuAcceptanceAndRejectUnsupportedModules() => Wait(Configuration());
        [UnityTest]
        public IEnumerator ConfigurationAndInvitationRacesCannotRestoreARemovedSeat() => Wait(ConfigurationRace());
        [UnityTest]
        public IEnumerator AnUnavailableRuntimeBlocksStartWithoutReplacingTheRoom() => Wait(Unavailable());

        private static async Task Saves()
        {
            var fixture = PveTestFixture.Load();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
            var token = timeout.Token;
            var api = await fixture.Human(token);
            var legacyRoom = await api.Post<RoomView>("api/rooms", new CreateRoomRequest { Side = Side.FreePeoples }, token);
            var legacy = await api.Post<DemoMatchView>(Path(legacyRoom) + "/demo", null, token);
            await api.Post<object>(Path(legacyRoom) + "/leave", null, token);
            var storage = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pve-menu-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var client = new MountedFlowClient(api, storage);
                var first = await OpenCpu(client, "cpu-shadow", Side.Shadow, token);
                var revision = client.Board.View.Revision;
                await Task.Delay(350, token);
                Assert.AreEqual(revision, client.Board.View.Revision, "The human Hunt allocation is never automated.");
                await BackToMenu(client, token);
                var second = await OpenCpu(client, "cpu-fp", Side.FreePeoples, token);
                await BackToMenu(client, token);
                var matches = (await api.Get<MatchListView>("api/matches", token)).Matches;
                foreach (var id in new[] { first, second, legacy.MatchId }) Assert.IsTrue(matches.Exists(m => m.MatchId == id), id);
                foreach (var id in new[] { first, second })
                {
                    var saved = matches.Find(m => m.MatchId == id);
                    Assert.IsTrue(saved.IsPve);
                    Assert.AreEqual("CPU", saved.Opponent);
                    Assert.IsNotNull(saved.Bot);
                    await Open(client, id, token);
                    Assert.AreEqual(id, client.Board.View.MatchId);
                    await BackToMenu(client, token);
                }
                Assert.IsFalse(matches.Find(m => m.MatchId == legacy.MatchId).IsPve, "The old demo remains a demo.");
            }
            finally { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
        }

        private static async Task<string> OpenCpu(MountedFlowClient client, string card, Side side, CancellationToken token)
        {
            await ClickReady(client.Root, "menu-cpu", token);
            await ClickReady(client.Root, card, token);
            await Until(() => client.Root.Q("waiting-room") != null, token);
            Assert.IsNull(client.Board, "Choosing a side opens a room; it does not start a match.");
            Assert.IsFalse(client.Root.Q<Button>("start-game").enabledSelf);
            await ClickReady(client.Root, "room-ready", token);
            await ClickReady(client.Root, "start-game", token);
            await Until(() => client.Board?.View?.DecisionOwner == side && client.Board.Connection.text == "Live", token);
            Assert.AreEqual(side, client.Board.View.You);
            return client.Board.View.MatchId;
        }
        private static async Task BackToMenu(MountedFlowClient client, CancellationToken token)
        {
            await ClickReady(client.Root, "back-to-games", token);
            await Until(() => client.Board == null && client.Root.Q("main-menu") != null, token);
        }

        private static async Task Retry()
        {
            var fixture = PveTestFixture.Load();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var token = timeout.Token;
            var loss = new LoseFirstBotResponse();
            using var api = await fixture.Human(token, loss);
            var room = await api.Post<RoomView>("api/rooms", new CreateRoomRequest { Side = Side.FreePeoples }, token);
            var screen = new WaitingRoomScreen();
            using var controller = new WaitingRoomController(api, screen, room, _ => Task.CompletedTask,
                () => Task.CompletedTask, () => Assert.Fail("Expired"), TimeSpan.FromMinutes(5));
            controller.InviteCpu();
            controller.InviteCpu();
            await Until(() => loss.Calls == 1 && screen.Root.Q<Button>("seat-shadow-remove").enabledSelf, token);
            controller.InviteCpu();
            await Until(() => screen.Room.Bot != null, token);
            Assert.AreEqual(2, loss.Calls);
            Assert.AreEqual(loss.First, loss.Second);
            room = await api.Post<RoomView>(Path(room) + "/remove", new RemoveParticipantRequest { Name = "CPU" }, token);
            var original = Newtonsoft.Json.JsonConvert.DeserializeObject<AddRoomBotRequest>(loss.First);
            await Refused(() => api.Put<RoomView>(Path(room) + "/bot", original, token), "BotReservationCancelled");
            await Task.Delay(400, token);
            room = await api.Get<RoomView>(Path(room), token);
            Assert.IsNull(room.Bot);
            Assert.IsEmpty(room.Shadow);
        }

        private static async Task Admission()
        {
            var fixture = PveTestFixture.Load();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var token = timeout.Token;
            using var host = await fixture.Human(token);
            using var guest = await fixture.Human(token);
            var room = await host.Post<RoomView>("api/rooms", new CreateRoomRequest { Side = Side.Shadow }, token);
            var cpu = Outcome(() => host.Put<RoomView>(Path(room) + "/bot", Request(room), token));
            var human = Outcome(() => guest.Post<RoomView>("api/rooms/join", new JoinRoomRequest { Code = room.JoinCode }, token));
            var outcomes = await Task.WhenAll(cpu, human);
            Assert.AreEqual(1, Array.FindAll(outcomes, ok => ok).Length, "Exactly one occupant wins the transaction.");
            room = await host.Get<RoomView>(Path(room), token);
            Assert.AreEqual(outcomes[0] ? "CPU" : guest.Username, room.FreePeoples);
            Assert.AreEqual(outcomes[0], room.Bot != null);
        }

        private static async Task Configuration()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var token = timeout.Token;
            using var api = await PveTestFixture.Load().Human(token);
            var room = await CpuReady(api, await Create(api, Side.FreePeoples, token), token);
            var oldVersion = room.Version;
            room = await api.Put<RoomView>(Path(room) + "/configuration", new ConfigureRoomRequest
                { ExpectedVersion = oldVersion, Configuration = room.Configuration }, token);
            Assert.Greater(room.Version, oldVersion);
            Assert.IsFalse(room.FreePeoplesAccepted);
            await Refused(() => api.Post<RoomView>(Path(room) + "/start", new RoomRevisionRequest { ExpectedVersion = oldVersion }, token), "StaleConfiguration");
            await Refused(() => api.Put<RoomView>(Path(room) + "/configuration", new ConfigureRoomRequest
                { ExpectedVersion = room.Version, Configuration = new MatchConfiguration { Lords = true } }, token), "BotConfigurationUnsupported");
            room = await CpuReady(api, room, token);
            Assert.IsFalse(room.FreePeoplesAccepted);
            Assert.IsTrue(room.ShadowAccepted);
        }

        private static async Task Unavailable()
        {
            var fixture = PveTestFixture.Load();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var token = timeout.Token;
            using var api = await fixture.Human(token);
            var room = await CpuReady(api, await Create(api, Side.FreePeoples, token), token);
            room = await api.Put<RoomView>(Path(room) + "/ready", new RoomReadyRequest { ExpectedVersion = room.Version, Ready = true }, token);
            using var offline = new RestartTestServer();
            await offline.Start(token, new Dictionary<string, string> { ["Wotr__Bots__RuntimeEnabled"] = "false" });
            using var other = new GameApi(offline.Address);
            await other.Login(api.Username, TestAccounts.Password, token);
            var unavailable = await other.Get<RoomView>(Path(room), token);
            Assert.AreEqual(BotSeatStatus.Unavailable, unavailable.Bot.Status);
            var screen = new WaitingRoomScreen();
            screen.Update(unavailable);
            Assert.IsFalse(screen.Root.Q<Button>("start-game").enabledSelf);
            await Refused(() => other.Post<RoomView>(Path(room) + "/start", new RoomRevisionRequest { ExpectedVersion = room.Version }, token), "BotUnavailable");
            Assert.IsEmpty((await api.Get<RoomView>(Path(room), token)).MatchId);
            var empty = await other.Post<RoomView>("api/rooms", new CreateRoomRequest { Name = "Unavailable CPU", Side = Side.Shadow }, token);
            await Refused(() => other.Put<RoomView>(Path(empty) + "/bot", Request(empty), token), "BotUnavailable");
            Assert.IsNull((await other.Get<RoomView>(Path(empty), token)).Bot);
        }

        private static async Task ConfigurationRace()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var token = timeout.Token;
            using var api = await PveTestFixture.Load().Human(token);
            var room = await api.Post<RoomView>("api/rooms", new CreateRoomRequest { Name = "CPU configuration race", Side = Side.FreePeoples }, token);
            var invitation = Request(room);
            var outcomes = await Task.WhenAll(
                Outcome(() => api.Put<RoomView>(Path(room) + "/bot", invitation, token)),
                Outcome(() => api.Put<RoomView>(Path(room) + "/configuration", new ConfigureRoomRequest
                    { ExpectedVersion = room.Version, Configuration = room.Configuration }, token)));
            Assert.AreEqual(1, Array.FindAll(outcomes, ok => ok).Length);
            room = await api.Get<RoomView>(Path(room), token);
            if (!outcomes[0])
            {
                await Refused(() => api.Put<RoomView>(Path(room) + "/bot", invitation, token), "StaleConfiguration");
                invitation = Request(room);
                room = await api.Put<RoomView>(Path(room) + "/bot", invitation, token);
            }
            var stale = room;
            room = await api.Post<RoomView>(Path(room) + "/remove", new RemoveParticipantRequest { Name = "CPU" }, token);
            var screen = new WaitingRoomScreen();
            screen.Update(room);
            screen.Update(stale);
            Assert.AreEqual(room.Version, screen.Room.Version, "A late response cannot restore the removed seat in the UI.");
            Assert.AreEqual("Add CPU", screen.Root.Q<Button>("seat-shadow-remove").text);
            await Task.Delay(300, token);
            await Refused(() => api.Put<RoomView>(Path(room) + "/bot", invitation, token), "BotReservationCancelled");
            Assert.IsNull((await api.Get<RoomView>(Path(room), token)).Bot);
        }

        private static async Task Refused(Func<Task<RoomView>> action, string error)
        {
            try { await action(); Assert.Fail("Expected " + error); }
            catch (ApiException refused) { Assert.AreEqual(error, refused.Message); }
        }
        private static async Task<bool> Outcome(Func<Task<RoomView>> action)
        {
            try { await action(); return true; }
            catch (ApiException error) when (error.Status == 409) { return false; }
        }
        private sealed class LoseFirstBotResponse : DelegatingHandler
        {
            public int Calls;
            public string First, Second;
            public LoseFirstBotResponse() : base(new HttpClientHandler()) { }
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                if (!request.RequestUri.AbsolutePath.EndsWith("/bot")) return await base.SendAsync(request, token);
                var body = await request.Content.ReadAsStringAsync();
                var response = await base.SendAsync(request, token);
                Calls++;
                if (Calls == 1)
                {
                    First = body;
                    response.Dispose();
                    throw new HttpRequestException("Lost successful invitation response");
                }
                Second = body;
                return response;
            }
        }
    }
}
