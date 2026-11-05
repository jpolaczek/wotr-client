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
    // UI-501 (docs/ui/RANKED_UX.md): no control leaves a ranked game silently. Against the real server, whose clocks run at a
    // tenth (scripts/test-client.sh), with the Long preset's 30 s for a move so the first decision outlasts each test.
    public class RankedLeaveNetworkTests
    {
        private const string Queue = RankedSearchController.Route;

        [UnityTest]
        public IEnumerator ConcedingAsksFirstEndsTheGameForBothAndLeavesItFinished() => Real(async (address, token) =>
        {
            var (a, b, match) = await Paired(address, token);
            using var first = new MountedFlowClient(a, Storage());
            using var second = new MountedFlowClient(b, Storage());
            await OnTheBoard(first, match, token);
            await OnTheBoard(second, match, token);

            var exit = first.Root.Q<Button>("back-to-games");
            await ReturnTestFixture.Until(() => exit.text == "Concede", token);
            StringAssert.Contains("Concede", first.Board.Menu.Toggle.tooltip);
            first.Board.Menu.Open();
            ReturnTestFixture.Click(exit);
            await Frames(token);
            var question = first.Board.Menu.ConcedeQuestion;
            Assert.IsTrue(question.IsOpen, "It asks first.");
            Assert.AreSame(first.Root.Q<Button>("concede-cancel"), first.Root.panel.focusController.focusedElement, "Enter alone never concedes.");
            ReturnTestFixture.Click(first.Root.Q<Button>("concede-cancel"));
            await Frames(token);
            Assert.IsNull(first.Board.View.Winner, "Cancel changes nothing.");
            Assert.IsNull(first.Root.Q("main-menu"));

            ReturnTestFixture.Click(exit);
            await Frames(token);
            var sent = DateTime.UtcNow;
            ReturnTestFixture.Click(first.Root.Q<Button>("concede-confirm"));
            await ReturnTestFixture.Until(() => second.Board.View.Winner != null, token);
            Assert.Less((DateTime.UtcNow - sent).TotalSeconds, 2, "The opponent sees the ending at once.");
            await ReturnTestFixture.Until(() => first.Board.View.Winner != null, token);
            Assert.AreEqual(VictoryCondition.Conceded, first.Board.View.Victory);
            Assert.AreNotEqual(first.Board.View.You, first.Board.View.Winner);
            Assert.IsNull(first.Root.Q("main-menu"), "The board stays for the end screen; no path led to the menu with the game running.");

            await ReturnTestFixture.Until(() => exit.text == "Exit match", token);
            var finished = await a.Get<MatchListView>("api/matches?status=Finished", token);
            var row = finished.Matches.Single(game => game.MatchId == match);
            Assert.IsTrue(row.Ranked);
            Assert.AreEqual(RankedSearchStatus.Idle, (await a.Get<RankedSearchView>(Queue, token)).Status, "The seat is free: it cannot be resumed as a game.");
            var shownRow = new SavedMatchRow(row, _ => { });
            shownRow.Apply(row);
            Assert.IsTrue(shownRow.Root.Query<Label>().ToList().Any(label => label.text.StartsWith("Ranked · ")), "My games marks ranked rows.");
        });

        [UnityTest]
        public IEnumerator QuitGameWarnsThatItConcedesAndConcedesFirst() => Real(async (address, token) =>
        {
            var (a, b, match) = await Paired(address, token);
            using var first = new MountedFlowClient(a, Storage());
            await OnTheBoard(first, match, token);
            await ReturnTestFixture.Until(() => first.Flow.RankedMatchActive, token);
            first.Board.Menu.Open();
            await Frames(token);
            ReturnTestFixture.Click(first.Root.Q<Button>("board-menu-quit"));
            await Frames(token);
            Assert.IsFalse(first.Board.Menu.Quit.IsOpen, "Not the ordinary question.");
            Assert.IsTrue(first.Board.Menu.QuitAndConcedeQuestion.IsOpen);
            Assert.AreEqual(0, first.Quits);

            ReturnTestFixture.Click(first.Root.Q<Button>("quit-concede-confirm"));
            await ReturnTestFixture.Until(() => first.Quits == 1, token);
            var view = await b.Get<PlayerView>("api/matches/" + match, token);
            Assert.AreEqual(VictoryCondition.Conceded, view.Victory, "The concession went before anything else.");
        });

        // The window's close follows the same path, and passes once there is nothing left to lose.
        [UnityTest]
        public IEnumerator ClosingTheWindowAsksTheSameQuestion() => Real(async (address, token) =>
        {
            var (a, _, match) = await Paired(address, token);
            using var first = new MountedFlowClient(a, Storage());
            await OnTheBoard(first, match, token);
            await ReturnTestFixture.Until(() => first.Flow.RankedMatchActive && first.Board.Connection.text == "Live", token);

            Assert.IsFalse(first.Flow.AllowQuit(), "The close waits for an answer.");
            await Frames(token);
            Assert.IsTrue(first.Board.Menu.IsOpen);
            Assert.IsTrue(first.Board.Menu.QuitAndConcedeQuestion.IsOpen);
            ReturnTestFixture.Click(first.Root.Q<Button>("quit-concede-cancel"));
            await Frames(token);
            Assert.AreEqual(0, first.Quits);

            first.Board.Menu.Close();
            await a.Post<CommandReceipt>($"api/matches/{match}/concede", new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, token);
            await ReturnTestFixture.Until(() => !first.Flow.RankedMatchActive, token);
            Assert.IsTrue(first.Flow.AllowQuit(), "A finished game does not hold the window.");
        });

        [UnityTest]
        public IEnumerator SigningInWithAnActiveMatchOpensItWithItsClock() => Real(async (address, token) =>
        {
            var (a, _, match) = await Paired(address, token);
            var again = new GameApi(address);
            await again.Login(a.Username, TestAccounts.Password, token);
            using var relaunched = new MountedFlowClient(again, Storage());
            await ReturnTestFixture.Until(() => relaunched.Board?.View?.Clock != null, token);
            Assert.AreEqual(match, relaunched.Board.View.MatchId, "It opens directly.");
            Assert.IsNull(relaunched.Root.Q("main-menu"));
            var clock = relaunched.Root.Q<Label>("move-clock").text;
            var served = (await again.Get<PlayerView>("api/matches/" + match, token)).Clock;
            var shown = clock.Split(' ').Last().Split(':');
            var seconds = int.Parse(shown[0]) * 60 + int.Parse(shown[1]);
            Assert.That(seconds, Is.EqualTo(served.RemainingMilliseconds / 1000.0).Within(2), "The clock counts from the server's time.");
            await again.Post<CommandReceipt>($"api/matches/{match}/concede", new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, token);
        });

        // A server that cannot be reached: the concession says so and the player stays; a lost connection is not leaving.
        [UnityTest]
        public IEnumerator AConcessionThatCannotReachTheServerSaysSoAndKeepsThePlayerInTheGame()
        {
            var task = NoServer();
            while (!task.IsCompleted)
                yield return null;
            task.GetAwaiter().GetResult();
        }

        private static async Task NoServer()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            using var server = new RestartTestServer();
            await server.Start(token);
            var (a, _, match) = await Paired(server.Address, token);
            using var first = new MountedFlowClient(a, Storage());
            await OnTheBoard(first, match, token);
            await ReturnTestFixture.Until(() => first.Flow.RankedMatchActive && first.Board.Connection.text == "Live", token);

            first.Board.Menu.Open();
            await Frames(token);
            server.Stop();
            ReturnTestFixture.Click(first.Root.Q<Button>("back-to-games"));
            await Frames(token);
            ReturnTestFixture.Click(first.Root.Q<Button>("concede-confirm"));
            await ReturnTestFixture.Until(() => first.Root.Q<Label>("board-menu-status").text == BoardMenu.ConcedeFailed, token);
            Assert.IsNull(first.Root.Q("main-menu"), "The player stays in the game.");
            Assert.IsTrue(first.Root.Q<Button>("board-menu-retry").resolvedStyle.display == DisplayStyle.Flex, "And can try again.");
            first.Board.Menu.Close();

            // The socket has noticed by now: the window may close, sending nothing and asking nothing.
            await ReturnTestFixture.Until(() => first.Board.Connection.text != "Live", token);
            Assert.IsTrue(first.Flow.AllowQuit(), "A lost connection is never treated as leaving.");
            await Frames(token);
            Assert.IsFalse(first.Board.Menu.IsOpen, "No dialog appears.");

            await server.Start(token);
            var view = await a.Get<PlayerView>("api/matches/" + match, token);
            Assert.IsNull(view.Winner, "Nothing was sent: the game goes on.");
        }

        private static IEnumerator Real(Func<string, CancellationToken, Task> test)
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Real HTTP fixture required.");
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var task = test(JObject.Parse(File.ReadAllText(path))["Address"].Value<string>(), timeout.Token);
            while (!task.IsCompleted)
                yield return null;
            timeout.Dispose();
            task.GetAwaiter().GetResult();
        }

        // Two fresh accounts searching through the API until the server pairs them.
        private static async Task<(GameApi, GameApi, string)> Paired(string address, CancellationToken token)
        {
            var a = await Account(address, "rl_a_", token);
            var b = await Account(address, "rl_b_", token);
            var search = new RankedSearchRequest { Lords = RankedChoice.Off, Kings = RankedChoice.Off, Fate = RankedChoice.Off, Standard = RankedChoice.Off, Long = RankedChoice.Enforce };
            await a.Post<RankedSearchView>(Queue, search, token);
            await b.Post<RankedSearchView>(Queue, search, token);
            while (true)
            {
                var view = await a.Get<RankedSearchView>(Queue, token);
                await b.Get<RankedSearchView>(Queue, token);
                if (view.Status == RankedSearchStatus.Matched)
                    return (a, b, view.MatchId);
                await Task.Delay(200, token);
            }
        }

        private static async Task<GameApi> Account(string address, string prefix, CancellationToken token)
        {
            var api = new GameApi(address);
            await TestAccounts.Register(api, prefix + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            return api;
        }

        private static string Storage() => Path.Combine(Path.GetTempPath(), "wotr-leave-" + Guid.NewGuid().ToString("N"));

        private static Task OnTheBoard(MountedFlowClient client, string match, CancellationToken token) =>
            ReturnTestFixture.Until(() => client.Board?.View?.MatchId == match, token);

        private static Task Frames(CancellationToken token) => Task.Delay(120, token);
    }
}
