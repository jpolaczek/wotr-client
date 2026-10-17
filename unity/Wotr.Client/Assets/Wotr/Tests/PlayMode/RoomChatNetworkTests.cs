using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    // MENU-26 (WR05): the two players of a waiting room exchange messages through the server both ways; Send and Enter
    // send, an empty line sends nothing, and markup is shown as typed.
    public sealed class RoomChatNetworkTests
    {
        [UnityTest]
        public IEnumerator BothPlayersChatInTheWaitingRoom()
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
            var storage = Path.Combine(Path.GetTempPath(), "wotr-chat-" + Guid.NewGuid().ToString("N"));
            var (hostApi, guestApi, room) = await WaitingRoomNetwork.Table(address, "chat", token);
            using (var host = new MountedFlowClient(hostApi, storage))
            using (var guest = new MountedFlowClient(guestApi, storage))
            {
                await WaitingRoomNetwork.Enter(host, room, token);
                await WaitingRoomNetwork.Enter(guest, room, token);

                host.Root.Q<TextField>("room-chat-input").value = "   ";
                await ClickReady(host.Root, "room-chat-send", token);
                await Task.Delay(1500, token);
                Assert.IsEmpty(Lines(guest), "An empty line sends nothing.");

                const string greeting = "Hello <b>there</b>";
                host.Root.Q<TextField>("room-chat-input").value = greeting;
                await ClickReady(host.Root, "room-chat-send", token);
                await Until(() => Lines(guest).Contains(greeting), token);
                Assert.AreEqual("", host.Root.Q<TextField>("room-chat-input").value, "A sent line leaves the field.");

                var input = guest.Root.Q<TextField>("room-chat-input");
                input.value = "Ready when you are";
                // A real key goes to the focused text input inside the field: the player is typing in it.
                input.Focus();
                var focused = (VisualElement)input.panel.focusController.focusedElement ?? input;
                using (var enter = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return }))
                    focused.SendEvent(enter);
                var arrived = await Within(() => Lines(host).Contains("Ready when you are"), 10, token);
                Assert.IsTrue(arrived, $"Enter: field '{input.value}', status '{guest.Root.Q<Label>("waiting-status").text}', " +
                    $"guest lines [{string.Join(" | ", Lines(guest))}], host lines [{string.Join(" | ", Lines(host))}]");
                CollectionAssert.AreEqual(new[] { greeting, "Ready when you are" }, Lines(host));
            }
            if (Directory.Exists(storage))
                Directory.Delete(storage, true);
        }

        private static async Task<bool> Within(Func<bool> condition, float seconds, CancellationToken token)
        {
            var end = DateTime.UtcNow.AddSeconds(seconds);
            while (!condition())
            {
                if (DateTime.UtcNow > end)
                    return false;
                await Task.Delay(100, token);
            }
            return true;
        }

        private static string[] Lines(MountedFlowClient client) =>
            client.Root.Query<Label>(className: "room-chat-text").ToList().Select(l => l.text).ToArray();
    }
}
