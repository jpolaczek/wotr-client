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
    // UI-505 against the real server: two accounts with different avatars see each other's, and a change made through the
    // browser's routes reaches the other client on its next refresh and one's own signed-in line.
    public class AvatarNetworkTests
    {
        [UnityTest]
        public IEnumerator TwoAccountsSeeEachOthersAvatarsAndAChangeInTheBrowserReachesThem()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Real HTTP fixture required.");
            var address = JObject.Parse(File.ReadAllText(path))["Address"].Value<string>();
            ServerConfiguration.Use(address);
            try
            {
                var task = Run(address);
                while (!task.IsCompleted)
                    yield return null;
                task.GetAwaiter().GetResult();
            }
            finally
            {
                ServerConfiguration.Use(null);
            }
        }

        // What the browser's account page does.
        internal static async Task Choose(string address, string username, string avatar, CancellationToken token)
        {
            var browser = new GameApi(address);
            var session = await browser.Post<JObject>("api/account/session",
                new JObject { ["username"] = username, ["password"] = TestAccounts.Password }, token);
            var signedIn = browser.WithSession(new SessionView { Token = session["token"].Value<string>() });
            await signedIn.Put<JObject>("api/account/avatar", new JObject { ["avatar"] = avatar }, token);
        }

        private static async Task Run(string address)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var token = timeout.Token;
            var storage = Path.Combine(Path.GetTempPath(), "wotr-avatar-" + Guid.NewGuid().ToString("N"));
            var firstApi = new GameApi(address);
            var secondApi = new GameApi(address);
            var first = "avatar_a_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var second = "avatar_b_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            await TestAccounts.Register(firstApi, first, token);
            await TestAccounts.Register(secondApi, second, token);
            Assert.That(firstApi.Avatar, Is.EqualTo(AvatarIds.Eye).Or.EqualTo(AvatarIds.Ring), "A new account has a default.");
            await Choose(address, first, "gondor", token);
            await Choose(address, second, "sauron", token);

            using var one = new MountedFlowClient(firstApi, storage);
            using var two = new MountedFlowClient(secondApi, storage);
            await ReturnTestFixture.Until(() => AvatarImage.Shown(one.Root.Q("menu-player").parent) == "gondor", token);
            await ReturnTestFixture.ClickReady(one.Root, "show-public-games", token);
            await ReturnTestFixture.ClickReady(two.Root, "show-public-games", token);
            await ReturnTestFixture.Until(() => Online(one, second) == "sauron", token);
            await ReturnTestFixture.Until(() => Online(two, first) == "gondor", token);

            await Choose(address, second, "elves", token);

            await ReturnTestFixture.Until(() => Online(one, second) == "elves", token);
            await ReturnTestFixture.Until(() => AvatarImage.Shown(two.Root.Q("menu-player").parent) == "elves", token);
            Assert.AreEqual("elves", secondApi.Avatar);
        }

        private static string Online(MountedFlowClient client, string name)
        {
            var row = client.Root.Q<Label>("lobby-player-" + name);
            return row == null ? null : AvatarImage.Shown(row.parent);
        }
    }
}
