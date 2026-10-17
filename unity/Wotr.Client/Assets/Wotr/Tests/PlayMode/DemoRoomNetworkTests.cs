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
    // DEMO-04 (DM09): the waiting room's fixed "Play demo vs CPU" starts a demo on the player's side and opens its board;
    // the CPU makes its first move by itself; a second demo from the same room replaces the first in the player's games.
    public sealed class DemoRoomNetworkTests
    {
        [UnityTest]
        public IEnumerator ADemoFromTheWaitingRoomOpensAgainstTheCpuAndReplacesThePreviousOne()
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
            var storage = Path.Combine(Path.GetTempPath(), "wotr-demo-" + Guid.NewGuid().ToString("N"));
            var api = new GameApi(address);
            await TestAccounts.Register(api, "demo_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
            var room = await api.Post<RoomView>("api/rooms", new CreateRoomRequest { Side = Side.Shadow }, token);
            using (var client = new MountedFlowClient(api, storage))
            {
                var first = await Demo(client, room.Id, token);
                // The CPU plays the Free Peoples' Fellowship phase; then the Shadow allocates the Hunt.
                await Until(() => client.Board.View?.DecisionOwner == Side.Shadow, token);
                Assert.AreEqual(DecisionKind.AllocateHunt, client.Board.View.Decision.Kind);
                var listed = (await api.Get<MatchListView>("api/matches", token)).Matches.Find(m => m.MatchId == first);
                Assert.AreEqual("CPU", listed.Opponent);
                Assert.AreEqual("Demo · Shadow vs CPU", listed.Name);
                Assert.AreEqual(RoomStatus.Waiting, (await api.Get<RoomView>("api/rooms/" + room.Id, token)).Status, "The room keeps waiting.");

                await ClickReady(client.Root, "open-board-menu", token);
                await ClickReady(client.Root, "back-to-games", token);
                await Until(() => client.Board == null && client.Root.Q("main-menu") != null, token);
                var second = await Demo(client, room.Id, token);

                Assert.AreNotEqual(first, second);
                var matches = (await api.Get<MatchListView>("api/matches", token)).Matches;
                Assert.IsFalse(matches.Exists(m => m.MatchId == first), "The new demo replaced the previous one.");
                Assert.IsTrue(matches.Exists(m => m.MatchId == second));
            }
            Directory.Delete(storage, true);
        }

        private static async Task<string> Demo(MountedFlowClient client, string room, CancellationToken token)
        {
            await ClickReady(client.Root, "open-room-" + room, token);
            await Until(() => client.Root.Q("waiting-room") != null, token);
            var demo = client.Root.Q<Button>("room-demo");
            StringAssert.Contains("replaces your previous demo", demo.tooltip);
            await ClickReady(client.Root, "room-demo", token);
            await Until(() => client.Board?.View != null, token);
            return client.Board.View.MatchId;
        }
    }
}
