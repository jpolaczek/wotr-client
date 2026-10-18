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
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // LOBBY-01 against the real server: two players on Open games read each other's chat and see each other online; a
    // right-click challenge reaches the other player, whose Accept seats both in a new private room hosted by the
    // challenger.
    public class LobbyNetworkTests
    {
        [UnityTest]
        public IEnumerator ChatOnlinePlayersAndAnAcceptedChallengeUseTheServer()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path)) Assert.Ignore("Real HTTP fixture required.");
            var task = Run(JObject.Parse(File.ReadAllText(path))["Address"].Value<string>());
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }

        private static async Task Run(string address)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var token = timeout.Token;
            var storage = Path.Combine(Path.GetTempPath(), "wotr-lobby-" + Guid.NewGuid().ToString("N"));
            var challengerApi = new GameApi(address);
            var challengedApi = new GameApi(address);
            var challenger = "lobby_a_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var challenged = "lobby_b_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            await TestAccounts.Register(challengerApi, challenger, token);
            await TestAccounts.Register(challengedApi, challenged, token);
            using var host = new MountedFlowClient(challengerApi, storage);
            using var guest = new MountedFlowClient(challengedApi, storage);

            var text = "Anyone for the Shadow? " + Guid.NewGuid().ToString("N").Substring(0, 6);
            await ReturnTestFixture.ClickReady(host.Root, "show-public-games", token);
            await ReturnTestFixture.ClickReady(guest.Root, "show-public-games", token);
            await ReturnTestFixture.Until(() => guest.Root.Q<TextField>("lobby-chat-input")?.worldBound.height > 0, token);
            guest.Root.Q<TextField>("lobby-chat-input").value = text;
            await ReturnTestFixture.ClickReady(guest.Root, "lobby-chat-send", token);
            await ReturnTestFixture.Until(() => host.Root.Query<Label>(className: "lobby-text").ToList().Any(l => l.text == text), token);
            await ReturnTestFixture.Until(() => host.Root.Q<Label>("lobby-player-" + challenged) != null, token);

            RightClick(host.Root.Q<Label>("lobby-player-" + challenged));
            await ReturnTestFixture.Until(() => host.Root.Q("lobby-context").resolvedStyle.display == DisplayStyle.Flex, token);
            await ReturnTestFixture.ClickReady(host.Root, "lobby-challenge-send", token);
            await ReturnTestFixture.Until(() => guest.Root.Q("challenge-tab").resolvedStyle.display == DisplayStyle.Flex, token);
            StringAssert.Contains(challenger, guest.Root.Q<Label>("challenge-text").text);
            await ReturnTestFixture.ClickReady(guest.Root, "challenge-accept", token);

            await ReturnTestFixture.Until(() => guest.Root.Q("waiting-room") != null && host.Root.Q("waiting-room") != null, token);
            var rooms = await challengerApi.Get<System.Collections.Generic.List<RoomView>>("api/rooms", token);
            var room = rooms.Single(r => r.FreePeoples.Length > 0 && r.Shadow.Length > 0 &&
                new[] { r.FreePeoples, r.Shadow }.OrderBy(n => n).SequenceEqual(new[] { challenger, challenged }.OrderBy(n => n)));
            Assert.AreEqual(challenger, room.Host, "The challenger hosts the new room.");
            Assert.IsTrue(room.IsHost);
            Assert.AreEqual(RoomVisibility.Private, room.Visibility);
        }

        private static void RightClick(VisualElement target)
        {
            Assert.IsNotNull(target);
            var point = target.worldBound.center;
            using var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 1, mousePosition = point });
            target.SendEvent(down);
        }
    }
}
