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
            var address = JObject.Parse(File.ReadAllText(path))["Address"].Value<string>();
            // Signing in again (LOBBY-02) goes to the build's configured server, here the test server.
            ServerConfiguration.Use(address);
            try
            {
                var task = Run(address);
                while (!task.IsCompleted) yield return null;
                task.GetAwaiter().GetResult();
            }
            finally
            {
                ServerConfiguration.Use(null);
            }
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
            // Both menus have polled once (each sees the other online), so the message below is news to both of them.
            await ReturnTestFixture.Until(() => host.Root.Q<Label>("lobby-player-" + challenged) != null, token);
            await ReturnTestFixture.Until(() => guest.Root.Q<Label>("lobby-player-" + challenger) != null, token);
            // LOBBY-02: Enter sends; there is no Send button.
            Assert.IsNull(guest.Root.Q("lobby-chat-send"));
            await PressEnter(guest.Root.Q<TextField>("lobby-chat-input"), text, token);
            await Expect(() => Shows(guest.Root, text), () => "the sender's own line: status '" +
                guest.Root.Q<Label>("lobby-status").text + "', input '" + guest.Root.Q<TextField>("lobby-chat-input").value + "'", token);
            await ReturnTestFixture.Until(() => Shows(host.Root, text), token);

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

            // LOBBY-02: back in the menu the chat of this sign-in is still there; after signing out and in it is gone.
            await ReturnTestFixture.ClickReady(host.Root, "waiting-back", token);
            await ReturnTestFixture.Until(() => host.Root.Q("main-menu") != null, token);
            await ReturnTestFixture.ClickReady(host.Root, "show-public-games", token);
            Assert.IsTrue(Shows(host.Root, text), "The sign-in's chat comes back with the menu.");
            await Task.Delay(TimeSpan.FromSeconds(LobbyLimits.PollSeconds + 1), token);
            Assert.IsNull(host.Root.Q("waiting-room"), "The accepted challenge does not open its room again.");
            await ReturnTestFixture.ClickReady(host.Root, "sign-out", token);
            await ReturnTestFixture.Until(() => host.Root.Q<TextField>("auth-username") != null, token);
            host.Root.Q<TextField>("auth-username").value = challenger;
            host.Root.Q<TextField>("auth-password").value = TestAccounts.Password;
            await ReturnTestFixture.ClickReady(host.Root, "auth-submit", token);
            await Expect(() => host.Root.Q("main-menu") != null, () => "the menu after signing in again: status '" +
                host.Root.Q<Label>(className: "status-message")?.text + "'", token);
            await ReturnTestFixture.ClickReady(host.Root, "show-public-games", token);
            await ReturnTestFixture.Until(() => host.Root.Q<Label>("lobby-player-" + challenger) != null, token);
            Assert.IsFalse(Shows(host.Root, text), "A new sign-in starts the chat from now.");
            Assert.IsNull(host.Root.Q("waiting-room"));
        }

        private static async Task Expect(Func<bool> condition, Func<string> state, CancellationToken token)
        {
            var until = DateTime.UtcNow.AddSeconds(20);
            while (!condition() && DateTime.UtcNow < until)
                await Task.Delay(50, token);
            if (!condition())
                Assert.Fail("Timed out waiting for " + state());
        }

        private static bool Shows(VisualElement root, string text) =>
            root.Query<Label>(className: "lobby-text").ToList().Any(label => label.text == text);

        // As a player does: focus the field (keys go to the focused element), type, press Enter.
        private static async Task PressEnter(TextField input, string text, CancellationToken token)
        {
            input.Focus();
            await Task.Delay(100, token);
            input.value = text;
            var target = input.Q(TextField.textInputUssName) ?? input;
            using var enter = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return });
            target.SendEvent(enter);
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
