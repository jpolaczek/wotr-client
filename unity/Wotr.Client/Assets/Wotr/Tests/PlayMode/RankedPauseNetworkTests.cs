using System;
using System.Collections;
using System.IO;
using System.Linq;
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
    // UI-500 (docs/ui/RANKED_UX.md): a pause between two real clients. scripts/test-client.sh runs the clocks at a tenth, so the
    // Long preset's move is 30 s: the first decision holds through the test, and the pause keeps it from running out.
    public class RankedPauseNetworkTests
    {
        [UnityTest]
        public IEnumerator PausesReachBothClientsEitherResumesAndTheBoardSendsNothing()
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
            var firstApi = new GameApi(address);
            var secondApi = new GameApi(address);
            await TestAccounts.Register(firstApi, "rp_a_" + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            await TestAccounts.Register(secondApi, "rp_b_" + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            var storage = Path.Combine(Path.GetTempPath(), "wotr-pause-" + Guid.NewGuid().ToString("N"));
            using var first = new MountedFlowClient(firstApi, storage);
            using var second = new MountedFlowClient(secondApi, storage);
            await Search(first, token);
            await Search(second, token);
            await ReturnTestFixture.Until(() => first.Board?.View?.Clock != null && second.Board?.View?.Clock != null, token);
            await ReturnTestFixture.Until(() => Live(first) && Live(second), token);

            Assert.AreEqual("Pause", Pause(first).text);
            StringAssert.Contains("You have 3 left, your opponent 3", Pause(first).tooltip);
            Assert.IsFalse(Paused(first));

            // Pausing on one client shows on the other within about a second.
            ReturnTestFixture.Click(Pause(first));
            var started = DateTime.UtcNow;
            await ReturnTestFixture.Until(() => Paused(first) && Paused(second), token);
            Assert.Less((DateTime.UtcNow - started).TotalSeconds, 1.5, "Both within about a second.");
            Assert.AreEqual("Paused by you", first.Root.Q<Label>("pause-banner-text").text);
            Assert.AreEqual("Paused by your opponent", second.Root.Q<Label>("pause-banner-text").text);

            // The clock stands still on both, at the same time.
            await Task.Delay(1200, token);
            var firstTime = Time(first);
            var secondTime = Time(second);
            Assert.AreEqual(firstTime, secondTime, "Both clients show the same time.");
            await Task.Delay(1500, token);
            Assert.AreEqual(firstTime, Time(first), "The clock is frozen.");

            // A click on the board sends nothing. Finish phase is the first decision's own button; it reaches the gate directly.
            var revision = first.Board.View.Revision;
            var owner = first.Board.View.Decision != null ? first : second;
            var finish = owner.Root.Query<Button>().ToList().FirstOrDefault(button => button.text == "Finish phase");
            Assert.IsNotNull(finish, "The first decision offers Finish phase.");
            ReturnTestFixture.Click(finish);
            await Task.Delay(600, token);
            Assert.AreEqual(revision, first.Board.View.Revision, "Nothing was sent.");
            Assert.AreEqual(revision, (await firstApi.Get<PlayerView>("api/matches/" + first.Board.View.MatchId, token)).Revision);

            // The chat stands above the veil and works.
            var chat = second.Board.GameChat;
            await ReturnTestFixture.Until(() => chat.Icon.resolvedStyle.display == DisplayStyle.Flex, token);
            Assert.AreSame(chat.Icon, second.Root.panel.Pick(chat.Icon.worldBound.center), "The chat is not covered.");
            ReturnTestFixture.Click(chat.Icon);
            await ReturnTestFixture.Until(() => chat.IsOpen, token);
            ReturnTestFixture.Click(chat.Icon);

            // Either player resumes, and both return to the same time.
            ReturnTestFixture.Click(second.Root.Q<Button>("pause-resume"));
            await ReturnTestFixture.Until(() => !Paused(first) && !Paused(second), token);
            AssertWithinASecond(Time(first), firstTime);
            AssertWithinASecond(Time(second), firstTime);
            Assert.IsTrue(Pause(first).enabledInHierarchy);
            StringAssert.Contains("You have 2 left, your opponent 3", Pause(first).tooltip);

            // With none left the button is disabled, not hidden, and the other's pause can still be resumed by this player.
            for (var spent = 0; spent < 2; spent++)
            {
                await ReturnTestFixture.Until(() => Pause(first).enabledInHierarchy, token);
                ReturnTestFixture.Click(Pause(first));
                await ReturnTestFixture.Until(() => Paused(first) && Paused(second), token);
                ReturnTestFixture.Click(first.Root.Q<Button>("pause-resume"));
                await ReturnTestFixture.Until(() => !Paused(first) && !Paused(second), token);
            }
            await ReturnTestFixture.Until(() => Pause(first).tooltip.StartsWith("You have no pauses left"), token);
            Assert.IsFalse(Pause(first).enabledInHierarchy);
            Assert.Greater(Pause(first).worldBound.height, 0, "Disabled, not hidden.");
            ReturnTestFixture.Click(Pause(second));
            await ReturnTestFixture.Until(() => Paused(first) && Paused(second), token);
            Assert.IsTrue(first.Root.Q<Button>("pause-resume").enabledInHierarchy, "Resume still works with no pauses left.");
            ReturnTestFixture.Click(first.Root.Q<Button>("pause-resume"));
            await ReturnTestFixture.Until(() => !Paused(first) && !Paused(second), token);

            await firstApi.Post<CommandReceipt>($"api/matches/{first.Board.View.MatchId}/concede",
                new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, token);
        }

        private static bool Live(MountedFlowClient client) => client.Board.Connection.text == "Live";

        private static Button Pause(MountedFlowClient client) => client.Root.Q<Button>("move-clock-pause");

        private static bool Paused(MountedFlowClient client) => client.Board.Pause.Shown;

        // The time a clock reads, without its owner's word.
        private static string Time(MountedFlowClient client) => client.Root.Q<Label>("move-clock").text.Split(' ')[1];

        private static void AssertWithinASecond(string shown, string before)
        {
            var difference = Math.Abs(Seconds(shown) - Seconds(before));
            Assert.LessOrEqual(difference, 1, $"{shown} against {before}: the same time comes back.");
        }

        private static int Seconds(string clock)
        {
            var parts = clock.Split(':');
            return int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
        }

        private static async Task Search(MountedFlowClient client, CancellationToken token)
        {
            await ReturnTestFixture.ClickReady(client.Root, "find-game", token);
            await ReturnTestFixture.Until(() => client.Root.Q("find-game-form").resolvedStyle.display == DisplayStyle.Flex, token);
            await Task.Delay(300, token);
            foreach (var module in new[] { "find-lords-off", "find-kings-off", "find-fate-off" })
                if (client.Root.Q<Button>(module).enabledInHierarchy)
                    ReturnTestFixture.Click(client.Root.Q<Button>(module));
            await ReturnTestFixture.ClickReady(client.Root, "find-long-enforce", token);
            await ReturnTestFixture.ClickReady(client.Root, "submit-find", token);
        }
    }
}
