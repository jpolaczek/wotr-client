using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class PveRoomTests
    {
        [TestCase(Side.FreePeoples)]
        [TestCase(Side.Shadow)]
        public void CpuUsesTheExistingRemoveSlotAndOnlyASeatedHostCanInvite(Side side)
        {
            var room = Room(side);
            var screen = new WaitingRoomScreen();
            screen.Update(room);
            var key = side == Side.FreePeoples ? "shadow" : "free";
            var action = screen.Root.Q<Button>("seat-" + key + "-remove");
            Assert.AreEqual("Add CPU", action.text);
            Assert.IsTrue(action.enabledSelf);
            var buttons = screen.Root.Query<Button>().ToList().Count;
            foreach (var status in new[] { BotSeatStatus.Joining, BotSeatStatus.Ready, BotSeatStatus.Unavailable })
            {
                room.Bot = new BotSeatView { Side = Other(side), Status = status };
                if (side == Side.FreePeoples) room.Shadow = "CPU";
                else room.FreePeoples = "CPU";
                room.FreePeoplesAccepted = room.ShadowAccepted = true;
                screen.Update(room);
                Assert.AreEqual("Remove", action.text);
                Assert.AreEqual(status.ToString(), screen.Root.Q<Label>("seat-" + key + "-state").text);
                Assert.AreEqual(status == BotSeatStatus.Ready, screen.Root.Q<Button>("start-game").enabledSelf);
                Assert.AreEqual(buttons, screen.Root.Query<Button>().ToList().Count);
            }
            room = Room(side);
            room.IsHost = false;
            screen.Update(room);
            Assert.IsFalse(action.enabledSelf);
            room.IsHost = true;
            room.Role = RoomRole.Observer;
            screen.Update(room);
            Assert.IsFalse(action.enabledSelf);
            room.Role = RoomRole.Player;
            room.Status = RoomStatus.Started;
            screen.Update(room);
            Assert.IsFalse(action.enabledSelf);
        }

        // BOT-37: the CPU seat shows its pace; only the host switches it, only while the room waits.
        [Test]
        public void TheCpuSeatShowsItsPaceAndOnlyTheWaitingHostSwitchesIt()
        {
            var room = Room(Side.FreePeoples);
            var screen = new WaitingRoomScreen();
            screen.Update(room);
            var pace = screen.Root.Q<Button>("seat-shadow-pace");
            Assert.AreEqual(DisplayStyle.None, pace.style.display.value, "No CPU, no pace.");
            Assert.AreEqual(DisplayStyle.None, screen.Root.Q<Button>("seat-free-pace").style.display.value);
            room.Shadow = "CPU";
            room.Bot = new BotSeatView { Side = Side.Shadow, Status = BotSeatStatus.Ready, ThinkingMode = BotThinkingMode.Thorough };
            screen.Update(room);
            Assert.AreEqual(DisplayStyle.Flex, pace.style.display.value);
            Assert.AreEqual("Pace: Thorough", pace.text);
            Assert.IsTrue(pace.enabledSelf);
            StringAssert.Contains("Press for Fast", pace.tooltip);
            room.Bot.ThinkingMode = BotThinkingMode.Fast;
            room.Version++;
            screen.Update(room);
            Assert.AreEqual("Pace: Fast", pace.text);
            room.IsHost = false;
            screen.Update(room);
            Assert.IsFalse(pace.enabledSelf);
            room.IsHost = true;
            room.Status = RoomStatus.Started;
            screen.Update(room);
            Assert.IsFalse(pace.enabledSelf);
            StringAssert.Contains("saved with the game", pace.tooltip);
        }

        [Test]
        public async Task SwitchingThePaceChangesTheRoomThroughTheServer()
        {
            var room = Room(Side.FreePeoples);
            room.Shadow = "CPU";
            room.Bot = new BotSeatView { Side = Side.Shadow, Status = BotSeatStatus.Ready };
            var handler = new Pace(room);
            using var api = new GameApi("http://127.0.0.1:5088", handler);
            var screen = new WaitingRoomScreen();
            using var controller = new WaitingRoomController(api, screen, room, _ => Task.CompletedTask,
                () => Task.CompletedTask, () => Assert.Fail("Unexpected expiry"), TimeSpan.FromHours(1));
            controller.ChangeCpuPace(BotThinkingMode.Thorough);
            await Until(() => screen.Room.Bot.ThinkingMode == BotThinkingMode.Thorough);
            Assert.AreEqual(1, handler.Calls);
            StringAssert.Contains("\"ExpectedVersion\":1", handler.Body);
            StringAssert.Contains("Thorough", handler.Body);
            Assert.AreEqual(2, screen.Room.Version);
            Assert.AreEqual("Pace: Thorough", screen.Root.Q<Button>("seat-shadow-pace").text);
        }

        private sealed class Pace : HttpMessageHandler
        {
            private readonly RoomView room;
            public int Calls;
            public string Body;
            public Pace(RoomView room) => this.room = room;
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                if (request.RequestUri.AbsolutePath.EndsWith("/bot/thinking") && request.Method == HttpMethod.Put)
                {
                    Calls++;
                    Body = await request.Content.ReadAsStringAsync();
                    var copy = JsonConvert.DeserializeObject<RoomView>(JsonConvert.SerializeObject(room));
                    copy.Version++;
                    copy.Bot.ThinkingMode = BotThinkingMode.Thorough;
                    copy.FreePeoplesAccepted = copy.ShadowAccepted = false;
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(copy)) };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(
                    request.RequestUri.AbsolutePath.EndsWith("/chat") ? (object)new RoomChatView() : room)) };
            }
        }

        [Test]
        public void SavedPveIdentityDoesNotLeakPolicyAndKeepsHumanDecisionsAboveAvailability()
        {
            var match = new MatchSummary { MatchId = "pve", IsPve = true, Opponent = "private-profile",
                Bot = new BotSeatView { ProfileId = "private-policy", Status = BotSeatStatus.Unavailable }, Status = MatchStatus.Waiting };
            var row = new SavedMatchRow(match, _ => { });
            var text = string.Join(" ", row.Root.Query<Label>().ToList().Select(label => label.text));
            StringAssert.Contains("CPU", text);
            StringAssert.DoesNotContain("private", text);
            Assert.AreEqual("CPU unavailable", SavedMatchRow.Status(match));
            match.Status = MatchStatus.YourDecision;
            match.YourDecision = DecisionKind.HuntDamage;
            Assert.AreEqual("Your decision · Hunt", SavedMatchRow.Status(match));
            match.IsPve = false;
            match.Status = MatchStatus.Waiting;
            Assert.AreEqual("Waiting for opponent", SavedMatchRow.Status(match));
        }

        [Test]
        public async Task DuplicateClicksAndLostResponseRetryTheSameInvitation()
        {
            var room = Room(Side.FreePeoples);
            var handler = new LostResponse(room);
            using var api = new GameApi("http://127.0.0.1:5088", handler);
            var screen = new WaitingRoomScreen();
            using var controller = new WaitingRoomController(api, screen, room, _ => Task.CompletedTask,
                () => Task.CompletedTask, () => Assert.Fail("Unexpected expiry"), TimeSpan.FromHours(1));
            controller.InviteCpu();
            controller.InviteCpu();
            Assert.AreEqual(1, handler.Calls);
            handler.Response.SetResult(true);
            await Until(() => screen.Root.Q<Button>("seat-shadow-remove").enabledSelf);
            Assert.AreEqual("Unavailable", screen.Root.Q<Label>("seat-shadow-state").text);
            controller.InviteCpu();
            await Until(() => screen.Room.Bot != null);
            Assert.AreEqual(2, handler.Calls);
            Assert.AreEqual(handler.First, handler.Second, "Both retries carry exactly the original request ID and version.");
            Assert.AreEqual("CPU", screen.Room.Shadow);
        }

        private static async Task Until(Func<bool> condition)
        {
            var end = DateTime.UtcNow.AddSeconds(5);
            while (!condition() && DateTime.UtcNow < end) await Task.Delay(10);
            Assert.IsTrue(condition());
        }

        private sealed class LostResponse : HttpMessageHandler
        {
            private readonly RoomView room;
            public readonly TaskCompletionSource<bool> Response = new TaskCompletionSource<bool>();
            public int Calls;
            public string First, Second;
            public LostResponse(RoomView room) => this.room = room;
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                if (request.RequestUri.AbsolutePath.EndsWith("/bot"))
                {
                    Calls++;
                    var body = await request.Content.ReadAsStringAsync();
                    if (Calls == 1)
                    {
                        First = body;
                        await Response.Task;
                        throw new HttpRequestException("Response lost after reservation");
                    }
                    Second = body;
                    room.Version++;
                    room.Shadow = "CPU";
                    room.Bot = new BotSeatView { Side = Side.Shadow, Status = BotSeatStatus.Joining };
                    return Json(room);
                }
                return Json(new RoomChatView());
            }
            private static HttpResponseMessage Json(object value) => new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(JsonConvert.SerializeObject(value)) };
        }

        private static Side Other(Side side) => side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
        internal static RoomView Room(Side side) => new RoomView
        {
            Id = "room", Name = "Game", Managed = true, IsHost = true, Host = "Human", You = side,
            Role = RoomRole.Player, FreePeoples = side == Side.FreePeoples ? "Human" : "",
            Shadow = side == Side.Shadow ? "Human" : "", Version = 1
        };
    }
}
