using System;
using System.Collections;
using System.Collections.Generic;
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
    // MENU-31: Demo vs CPU in the main menu asks only for the side: one click on a side card opens a demo against the
    // CPU on that side, and no waiting room is left behind; a second demo replaces the first.
    public sealed class MainMenuDemoNetworkTests
    {
        [UnityTest]
        public IEnumerator ADemoFromTheMainMenuAsksOnlyForTheSide()
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
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var token = timeout.Token;
            var storage = Path.Combine(Path.GetTempPath(), "wotr-menu-demo-" + Guid.NewGuid().ToString("N"));
            var api = new GameApi(address);
            await TestAccounts.Register(api, "menudemo_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
            using (var client = new MountedFlowClient(api, storage))
            {
                var first = await Demo(client, "demo-shadow", Side.Shadow, token);
                await ClickReady(client.Root, "open-board-menu", token);
                await ClickReady(client.Root, "back-to-games", token);
                await Until(() => client.Board == null && client.Root.Q("main-menu") != null, token);
                var second = await Demo(client, "demo-fp", Side.FreePeoples, token);

                var matches = (await api.Get<MatchListView>("api/matches", token)).Matches;
                Assert.IsFalse(matches.Exists(m => m.MatchId == first), "The new demo replaced the previous one.");
                var listed = matches.Find(m => m.MatchId == second);
                Assert.AreEqual("CPU", listed.Opponent);
                Assert.AreEqual("Demo · Free Peoples vs CPU", listed.Name);
                var rooms = await api.Get<List<RoomView>>("api/rooms", token);
                Assert.IsFalse(rooms.Exists(r => r.Status == RoomStatus.Waiting), "No waiting room is left behind.");
            }
            Directory.Delete(storage, true);
        }

        private static async Task<string> Demo(MountedFlowClient client, string card, Side side, CancellationToken token)
        {
            await ClickReady(client.Root, "menu-demo", token);
            Assert.AreEqual("Demo vs CPU", client.Root.Q<Label>("menu-window-title").text);
            await ClickReady(client.Root, card, token);
            await Until(() => client.Board?.View != null, token);
            Assert.AreEqual(side, client.Board.View.You, "The demo is played on the chosen side.");
            return client.Board.View.MatchId;
        }
    }
}
