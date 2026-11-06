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
    // RANK-09: the whole mode between two real clients against the real server, clicked as a player clicks it. scripts/test-client.sh
    // runs the clocks at a tenth, so the Long preset's move is 30 s, enough for what this game does.
    public class RankedEndToEndNetworkTests
    {
        [UnityTest]
        public IEnumerator TwoClientsPlayFromTheSearchToTheLeaderboard() => Real(async (address, token) =>
        {
            var firstApi = await Account(address, "e2e_a_", token);
            var secondApi = await Account(address, "e2e_b_", token);
            using var first = new MountedFlowClient(firstApi, Storage());
            using var second = new MountedFlowClient(secondApi, Storage());

            await Search(first, token);
            await Search(second, token);
            await ReturnTestFixture.Until(() => first.Board?.View?.Clock != null && second.Board?.View?.Clock != null, token);
            await ReturnTestFixture.Until(() => first.Board.Connection.text == "Live" && second.Board.Connection.text == "Live", token);
            var match = first.Board.View.MatchId;
            Assert.AreEqual(match, second.Board.View.MatchId, "Paired with each other.");
            var owner = first.Board.View.Clock.Owner == first.Board.View.You ? first : second;
            var other = owner == first ? second : first;
            StringAssert.StartsWith("You ", Time(owner));
            StringAssert.StartsWith("Opponent ", Time(other));

            // A real move: the clock passes to the other player.
            var revision = owner.Board.View.Revision;
            ReturnTestFixture.Click(owner.Root.Query<Button>().ToList().First(button => button.text == "Finish phase"));
            await ReturnTestFixture.Until(() => owner.Board.View.Revision > revision && other.Board.View.Revision > revision, token);
            await ReturnTestFixture.Until(() => Time(other).StartsWith("You ") && Time(owner).StartsWith("Opponent "), token);

            // One pauses, the other resumes.
            ReturnTestFixture.Click(other.Root.Q<Button>("move-clock-pause"));
            await ReturnTestFixture.Until(() => owner.Board.Pause.Shown && other.Board.Pause.Shown, token);
            ReturnTestFixture.Click(owner.Root.Q<Button>("pause-resume"));
            await ReturnTestFixture.Until(() => !owner.Board.Pause.Shown && !other.Board.Pause.Shown, token);

            // One concedes through the menu.
            other.Board.Menu.Open();
            await Task.Delay(120, token);
            ReturnTestFixture.Click(other.Root.Q<Button>("back-to-games"));
            await Task.Delay(120, token);
            ReturnTestFixture.Click(other.Root.Q<Button>("concede-confirm"));
            await ReturnTestFixture.Until(() => owner.Board.GameEnd.IsOpen && other.Board.GameEnd.IsOpen, token);

            StringAssert.EndsWith("You conceded", other.Root.Q<Label>("game-end-subtitle").text);
            StringAssert.EndsWith("Your opponent conceded", owner.Root.Q<Label>("game-end-subtitle").text);
            await ReturnTestFixture.Until(() => owner.Root.Q<Label>("game-end-rating") != null && other.Root.Q<Label>("game-end-rating") != null, token);
            var won = await (owner == first ? firstApi : secondApi).Get<RankedResultView>($"api/matches/{match}/rating", token);
            var lost = await (other == first ? firstApi : secondApi).Get<RankedResultView>($"api/matches/{match}/rating", token);
            Assert.AreEqual(GameEndState.RatingLine(won), owner.Root.Q<Label>("game-end-rating").text);
            Assert.AreEqual(GameEndState.RatingLine(lost), other.Root.Q<Label>("game-end-rating").text);
            Assert.Greater(won.Change, 0);
            Assert.Less(lost.Change, 0);

            // Back in the menu, the game stands under Finished and the leaderboard knows the new rating.
            await ReturnTestFixture.ClickReady(owner.Root, "back-to-games", token);
            await ReturnTestFixture.Until(() => owner.Root.Q("main-menu") != null, token);
            await ReturnTestFixture.ClickReady(owner.Root, "show-my-games", token);
            await ReturnTestFixture.ClickReady(owner.Root, "my-games-finished", token);
            await ReturnTestFixture.Until(() => owner.Root.Query<Label>().ToList().Any(label => label.text.StartsWith("Ranked · Victory")), token);
            await ReturnTestFixture.ClickReady(owner.Root, "show-leaderboards", token);
            await ReturnTestFixture.Until(() => owner.Root.Q<Label>("leaderboard-you").text.EndsWith("Rating " + won.RatingAfter), token);
            StringAssert.StartsWith("Your place: ", owner.Root.Q<Label>("leaderboard-you").text);
        });

        // D14: the owner's way to try the clock alone. It has a clock and pauses, and no rating moves.
        [UnityTest]
        public IEnumerator TheTestScenarioHasAClockAndAPauseAndChangesNoRating() => Real(async (address, token) =>
        {
            var api = await Account(address, "e2e_t_", token);
            using var client = new MountedFlowClient(api, Storage());
            await ReturnTestFixture.OpenScenario(client, "ranked-clock", token);
            await ReturnTestFixture.Until(() => client.Board.View?.Clock != null, token);
            var clock = client.Board.View.Clock;
            Assert.AreEqual(30000, clock.LimitMilliseconds, "The scenario's limits are its own, whatever the server's scale.");
            StringAssert.StartsWith("You ", Time(client));
            Assert.AreEqual("", client.Board.TopBar.Clock.Status.text, "The test seat is not reconnecting.");

            ReturnTestFixture.Click(client.Root.Q<Button>("move-clock-pause"));
            await ReturnTestFixture.Until(() => client.Board.Pause.Shown, token);
            Assert.AreEqual("Paused by you", client.Root.Q<Label>("pause-banner-text").text);
            ReturnTestFixture.Click(client.Root.Q<Button>("pause-resume"));
            await ReturnTestFixture.Until(() => !client.Board.Pause.Shown, token);

            client.Board.Menu.Open();
            await Task.Delay(120, token);
            ReturnTestFixture.Click(client.Root.Q<Button>("back-to-games"));
            await Task.Delay(120, token);
            ReturnTestFixture.Click(client.Root.Q<Button>("concede-confirm"));
            await ReturnTestFixture.Until(() => client.Board.GameEnd.IsOpen, token);
            await Task.Delay(800, token);
            Assert.IsNull(client.Root.Q("game-end-rating"), "A test game has no rating line.");
            var board = await api.Get<LeaderboardView>("api/leaderboard", token);
            Assert.IsNull(board.You, "And no rating.");
        });

        private static string Time(MountedFlowClient client) => client.Root.Q<Label>("move-clock").text;

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

        private static async Task<GameApi> Account(string address, string prefix, CancellationToken token)
        {
            var api = new GameApi(address);
            await TestAccounts.Register(api, prefix + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            return api;
        }

        private static string Storage() => Path.Combine(Path.GetTempPath(), "wotr-e2e-" + Guid.NewGuid().ToString("N"));

        private static IEnumerator Real(Func<string, CancellationToken, Task> test)
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Real HTTP fixture required.");
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var task = test(JObject.Parse(File.ReadAllText(path))["Address"].Value<string>(), timeout.Token);
            while (!task.IsCompleted)
                yield return null;
            timeout.Dispose();
            task.GetAwaiter().GetResult();
        }
    }

    // RANK-09: a server restart during an active ranked match, on two real clients. Both return, and nobody lost the time it took.
    public class RankedRestartNetworkTests
    {
        [UnityTest]
        public IEnumerator AServerRestartLosesNobodyTheGame()
        {
            var task = Run();
            while (!task.IsCompleted)
                yield return null;
            task.GetAwaiter().GetResult();
        }

        private static async Task Run()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            using var server = new RestartTestServer();
            await server.Start(token);
            var a = new GameApi(server.Address);
            var b = new GameApi(server.Address);
            await TestAccounts.Register(a, "rs_a_" + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            await TestAccounts.Register(b, "rs_b_" + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            var search = new RankedSearchRequest { Lords = RankedChoice.Off, Kings = RankedChoice.Off, Fate = RankedChoice.Off, Standard = RankedChoice.Off, Long = RankedChoice.Enforce };
            await a.Post<RankedSearchView>(RankedSearchController.Route, search, token);
            await b.Post<RankedSearchView>(RankedSearchController.Route, search, token);
            string match = null;
            while (match == null)
            {
                var view = await a.Get<RankedSearchView>(RankedSearchController.Route, token);
                await b.Get<RankedSearchView>(RankedSearchController.Route, token);
                if (view.Status == RankedSearchStatus.Matched)
                    match = view.MatchId;
                else
                    await Task.Delay(200, token);
            }
            var storage = Path.Combine(Path.GetTempPath(), "wotr-restart-" + Guid.NewGuid().ToString("N"));
            using var first = new MountedFlowClient(a, storage);
            using var second = new MountedFlowClient(b, storage);
            await ReturnTestFixture.Until(() => first.Board?.View?.Clock != null && second.Board?.View?.Clock != null &&
                first.Board.Connection.text == "Live" && second.Board.Connection.text == "Live", token);
            var before = (await a.Get<PlayerView>("api/matches/" + match, token)).Clock.RemainingMilliseconds;

            server.Stop();
            await ReturnTestFixture.Until(() => first.Board.Connection.text != "Live" && second.Board.Connection.text != "Live", token);
            await Task.Delay(4000, token);
            await server.Start(token);
            await ReturnTestFixture.Until(() => first.Board.Connection.text == "Live" && second.Board.Connection.text == "Live", token);

            var after = await a.Get<PlayerView>("api/matches/" + match, token);
            Assert.IsNull(after.Winner, "Nobody lost the game.");
            Assert.That(after.Clock.RemainingMilliseconds, Is.GreaterThan(before - 6000), "The downtime was given back: only the time with the server up was spent.");
            Assert.AreEqual("", first.Board.TopBar.Clock.Status.text.Replace(TopBarClock.OpponentReconnecting, ""), "Nobody reads as reconnecting after the return.");
            await a.Post<CommandReceipt>($"api/matches/{match}/concede", new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, token);
        }
    }
}
