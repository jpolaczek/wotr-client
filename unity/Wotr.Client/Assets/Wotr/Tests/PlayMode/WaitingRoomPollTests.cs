using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // MENU-19: the waiting room refreshes in the background without disabling its controls or clearing the status the
    // player is reading; a failed refresh says so and the next successful one clears only that message.
    public sealed class WaitingRoomPollTests
    {
        private static readonly string[] Controls = { "waiting-back", "room-ready", "generate-room-invitation" };

        [UnityTest]
        public IEnumerator TheBackgroundRefreshKeepsTheControlsAndTheStatusStill()
        {
            var room = Room();
            var handler = new RoomHandler(room);
            using var api = new GameApi("http://localhost", handler);
            var screen = new WaitingRoomScreen();
            using var controller = new WaitingRoomController(api, screen, room, _ => Task.CompletedTask,
                () => Task.CompletedTask, () => { }, TimeSpan.FromMilliseconds(100));
            screen.Status.text = "Code copied. Paste it in Join by code in the main menu.";
            var end = Time.realtimeSinceStartup + 1.5f;
            while (Time.realtimeSinceStartup < end)
            {
                foreach (var name in Controls)
                    Assert.IsTrue(screen.Root.Q<Button>(name).enabledSelf, name + " stays enabled while the room refreshes");
                yield return null;
            }
            Assert.GreaterOrEqual(handler.Requests, 3, "The room was refreshed in the background.");
            StringAssert.StartsWith("Code copied", screen.Status.text, "The player's status stays.");

            handler.Fail = true;
            yield return Until(() => screen.Status.text.StartsWith("Connection interrupted"));
            foreach (var name in Controls)
                Assert.IsTrue(screen.Root.Q<Button>(name).enabledSelf, name + " after a failed refresh");
            handler.Fail = false;
            yield return Until(() => screen.Status.text.Length == 0);
        }

        private static IEnumerator Until(Func<bool> condition)
        {
            var end = Time.realtimeSinceStartup + 5;
            while (!condition())
            {
                Assert.Less(Time.realtimeSinceStartup, end, "Timed out");
                yield return null;
            }
        }

        private static RoomView Room() => new RoomView
        {
            Id = "room", Name = "Evening game", Status = RoomStatus.Waiting, Managed = true, IsHost = true,
            Visibility = RoomVisibility.Private, You = Side.FreePeoples, FreePeoples = "Host", Version = 1
        };

        // Answers every GET of the room after a short delay, or fails the connection while Fail is set.
        private sealed class RoomHandler : HttpMessageHandler
        {
            private readonly string json;
            private int requests;
            public volatile bool Fail;
            public int Requests => requests;

            public RoomHandler(RoomView room) => json = JsonConvert.SerializeObject(room);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                Interlocked.Increment(ref requests);
                await Task.Delay(60, cancellation);
                if (Fail)
                    throw new HttpRequestException("Connection refused");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }
        }
    }
}
