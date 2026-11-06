using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Debug = UnityEngine.Debug;

namespace Wotr.PlayTests
{
    // UI-498 (docs/ui/RANKED_UX.md): the search through the real ranked queue. Fresh accounts each time, and every test ends
    // its own search or match, so no ticket of one test can pair with another's.
    public class RankedSearchNetworkTests
    {
        private const string Queue = RankedSearchController.Route;

        [UnityTest]
        public IEnumerator TwoClientsFindEachOtherSeeTheOpponentAndOpenTheSameBoard() => Real(async (address, token) =>
        {
            var (firstApi, firstName) = await Account(address, "rs_a_", token);
            var (secondApi, secondName) = await Account(address, "rs_b_", token);
            await AvatarNetworkTests.Choose(address, firstName, "gondor", token);
            await AvatarNetworkTests.Choose(address, secondName, "sauron", token);
            var storage = Storage();
            using (var first = new MountedFlowClient(firstApi, storage))
            using (var second = new MountedFlowClient(secondApi, storage))
            {
                var alerted = false;
                second.Interface.Sounds.Policy.Played += cue => alerted |= cue == SoundCue.MatchFound && second.Board == null;
                await OpenFindGame(first, token);
                // UI-497's page reads the rating when it opens.
                await ReturnTestFixture.Until(() => first.Root.Q<Label>("find-rating").text == "Your rating: 1500", token);
                Assert.AreEqual("gondor", AvatarImage.Shown(first.Root.Q<Label>("find-rating").parent), "UI-506: one's own picture.");
                await ReturnTestFixture.ClickReady(first.Root, "submit-find", token);
                await ReturnTestFixture.Until(() => first.Root.Q<Label>("find-elapsed").text.StartsWith("Searching"), token);
                Assert.AreEqual(FindGameForm.CancelText, first.Root.Q<Button>("submit-find").text);
                Assert.IsTrue(first.Root.Q<Button>("submit-find").enabledInHierarchy, "Only Cancel search responds.");
                AssertGreyed(first.Root);

                await OpenFindGame(second, token);
                await ReturnTestFixture.ClickReady(second.Root, "submit-find", token);

                await ReturnTestFixture.Until(() => Named(first.Root, secondName) && Named(second.Root, firstName), token);
                Assert.AreEqual("Rating 1500", first.Root.Q<Label>("find-opponent-rating").text);
                Assert.AreEqual("Rating 1500", second.Root.Q<Label>("find-opponent-rating").text);
                Assert.AreEqual("sauron", first.Root.Q("find-opponent-avatar").userData, "UI-506: the opponent's picture.");
                Assert.AreEqual("gondor", second.Root.Q("find-opponent-avatar").userData, "UI-506: the opponent's picture.");
                var presented = Stopwatch.StartNew();

                await ReturnTestFixture.Until(() => first.Board?.View != null && second.Board?.View != null, token);
                Assert.IsTrue(alerted, "The soft sound plays before the board opens, since the window may be in the background.");
                Assert.Greater(presented.Elapsed.TotalSeconds, 2.5, "The opponent is presented for 3 seconds.");
                Assert.Less(presented.Elapsed.TotalSeconds, 6, "The board opens within 3 seconds of the presentation, plus its load.");
                Assert.AreEqual(first.Board.View.MatchId, second.Board.View.MatchId, "Both opened the same board with no further click.");
                await Concede(firstApi, first.Board.View.MatchId, token);
            }
        });

        [UnityTest]
        public IEnumerator ACancelReturnsEveryControlAsItWasAndLeavesNoTicket() => Real(async (address, token) =>
        {
            var (api, _) = await Account(address, "rs_c_", token);
            using (var client = new MountedFlowClient(api, Storage()))
            {
                await OpenFindGame(client, token);
                await ReturnTestFixture.ClickReady(client.Root, "find-shadow", token);
                await ReturnTestFixture.ClickReady(client.Root, "find-side-choice-enforce", token);
                await ReturnTestFixture.ClickReady(client.Root, "find-long-allow", token);
                await ReturnTestFixture.ClickReady(client.Root, "submit-find", token);
                await ReturnTestFixture.Until(() => client.Root.Q<Button>("submit-find").text == FindGameForm.CancelText, token);
                AssertGreyed(client.Root);
                Assert.AreEqual(RankedSearchStatus.Searching, (await api.Get<RankedSearchView>(Queue, token)).Status);

                ReturnTestFixture.Click(client.Root.Q<Button>("submit-find"));
                await ReturnTestFixture.Until(() => client.Root.Q<Button>("submit-find").text == FindGameForm.SearchText, token);

                Assert.AreEqual(RankedSearchStatus.Idle, (await api.Get<RankedSearchView>(Queue, token)).Status, "No ticket stays.");
                foreach (var name in new[] { "find-kings-enforce", "find-shadow", "find-fp", "cancel-find", "show-my-games", "refresh-games" })
                    Assert.IsTrue(client.Root.Q<Button>(name).enabledInHierarchy, name);
                Assert.IsTrue(client.Root.Q<Button>("find-long-off").enabledInHierarchy, "Two paces are on, so either may go off.");
                Assert.IsTrue(client.Root.Q<Button>("find-shadow").ClassListContains("selected"), "The side stays chosen.");
                Assert.IsTrue(client.Root.Q<Button>("find-side-choice-enforce").ClassListContains("selected"));
                Assert.IsTrue(client.Root.Q<Button>("find-long-allow").ClassListContains("selected"));
                Assert.AreEqual("", client.Root.Q<Label>("find-status").text, "A cancel the player asked for needs no notice.");
            }
        });

        // The poller is slowed so that a cancel can arrive after the server has already paired.
        [UnityTest]
        public IEnumerator ACancelThatLosesTheRaceToAPairingOpensTheMatch() => Real(async (address, token) =>
        {
            var (api, _) = await Account(address, "rs_d_", token);
            var (rivalApi, rivalName) = await Account(address, "rs_e_", token);
            using (var client = new MountedFlowClient(new GameApi(address), Storage()))
            {
                var menu = new MainMenuScreen("Player");
                client.Interface.Content.Clear();
                client.Interface.Content.Add(menu.Root);
                string opened = null;
                using (new RankedSearchController(api, menu, id => opened = id, error => error.Message,
                    pollInterval: TimeSpan.FromSeconds(30), presentation: TimeSpan.FromMilliseconds(200)))
                {
                    await Settled(token);
                    menu.FindGame.Offer(new[] { ExpansionModules.Lords });
                    await ReturnTestFixture.ClickReady(menu.Root, "find-game", token);
                    await ReturnTestFixture.ClickReady(menu.Root, "submit-find", token);
                    await ReturnTestFixture.Until(() => menu.FindGame.IsSearching, token);
                    await Task.Delay(200, token);

                    var rival = await rivalApi.Post<RankedSearchView>(Queue, new RankedSearchRequest { Lords = RankedChoice.Off, Kings = RankedChoice.Off, Fate = RankedChoice.Off }, token);
                    var match = await PairedMatch(rivalApi, rival, token);

                    ReturnTestFixture.Click(menu.Root.Q<Button>("submit-find"));
                    await ReturnTestFixture.Until(() => opened != null, token);
                    Assert.AreEqual(match, opened, "The match opens rather than running unseen.");
                    Assert.AreEqual(rivalName, menu.Root.Q<Label>("find-opponent-name").text);
                    await Concede(api, match, token);
                }
            }
        });

        // A return after the ticket lapsed: the next poll comes later than the server's 6 s of liveness.
        [UnityTest]
        public IEnumerator AReturnAfterTheTicketLapsedSaysTheSearchStoppedAndRestoresTheControls() => Real(async (address, token) =>
        {
            var (api, _) = await Account(address, "rs_f_", token);
            using (var client = new MountedFlowClient(new GameApi(address), Storage()))
            {
                var menu = new MainMenuScreen("Player");
                client.Interface.Content.Clear();
                client.Interface.Content.Add(menu.Root);
                using (new RankedSearchController(api, menu, _ => Assert.Fail("Nothing was paired."), error => error.Message,
                    pollInterval: TimeSpan.FromSeconds(7)))
                {
                    await Settled(token);
                    await ReturnTestFixture.ClickReady(menu.Root, "find-game", token);
                    await ReturnTestFixture.ClickReady(menu.Root, "submit-find", token);
                    await ReturnTestFixture.Until(() => menu.FindGame.IsSearching, token);
                    await ReturnTestFixture.Until(() => !menu.FindGame.IsSearching, token);
                    Assert.AreEqual(FindGameForm.SearchStopped, menu.Root.Q<Label>("find-status").text);
                    Assert.AreEqual(FindGameForm.SearchText, menu.Root.Q<Button>("submit-find").text);
                    Assert.IsTrue(menu.Root.Q<Button>("cancel-find").enabledInHierarchy);
                    Assert.IsTrue(menu.Root.Q("menu-actions").enabledInHierarchy);
                }
            }
        });

        // A server that cannot be reached: the search shows it and keeps trying; a cancel gives up once the ticket has lapsed.
        [UnityTest]
        public IEnumerator ALostConnectionIsShownAndACancelGivesUpInTime() => Real(async (address, token) =>
        {
            using (var client = new MountedFlowClient(new GameApi(address), Storage()))
            using (var unreachable = new GameApi("http://127.0.0.1:9"))
            {
                var menu = new MainMenuScreen("Player");
                client.Interface.Content.Clear();
                client.Interface.Content.Add(menu.Root);
                using (new RankedSearchController(unreachable, menu, _ => Assert.Fail("Nothing was paired."), error => error.Message,
                    pollInterval: TimeSpan.FromMilliseconds(300)))
                {
                    await Settled(token);
                    await ReturnTestFixture.ClickReady(menu.Root, "find-game", token);
                    await ReturnTestFixture.ClickReady(menu.Root, "submit-find", token);
                    await ReturnTestFixture.Until(() => menu.Root.Q<Label>("find-elapsed").text.StartsWith("Connection lost"), token);
                    Assert.IsTrue(menu.FindGame.IsSearching, "It keeps trying.");
                    Assert.AreEqual(FindGameForm.CancelText, menu.Root.Q<Button>("submit-find").text);

                    ReturnTestFixture.Click(menu.Root.Q<Button>("submit-find"));
                    await ReturnTestFixture.Until(() => !menu.FindGame.IsSearching, token);
                    Assert.AreEqual(FindGameForm.CancelUnconfirmed, menu.Root.Q<Label>("find-status").text);
                }
            }
        });

        // Closing the client takes the ticket with it, without waiting for it to lapse.
        [UnityTest]
        public IEnumerator ClosingTheClientDuringASearchLeavesNoTicket() => Real(async (address, token) =>
        {
            var (api, _) = await Account(address, "rs_g_", token);
            using (var client = new MountedFlowClient(new GameApi(address), Storage()))
            {
                var menu = new MainMenuScreen("Player");
                client.Interface.Content.Clear();
                client.Interface.Content.Add(menu.Root);
                var search = new RankedSearchController(api, menu, _ => Assert.Fail("Nothing can be paired."), error => error.Message);
                await Settled(token);
                await ReturnTestFixture.ClickReady(menu.Root, "find-game", token);
                await ReturnTestFixture.ClickReady(menu.Root, "submit-find", token);
                await ReturnTestFixture.Until(() => menu.FindGame.IsSearching, token);
                await Task.Delay(300, token);
                Assert.AreEqual(RankedSearchStatus.Searching, (await api.Get<RankedSearchView>(Queue, token)).Status);
                search.Dispose();
                await Task.Delay(500, token);
                Assert.AreEqual(RankedSearchStatus.Idle, (await api.Get<RankedSearchView>(Queue, token)).Status, "The ticket went with the client.");
            }
        });

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

        private static async Task<(GameApi, string)> Account(string address, string prefix, CancellationToken token)
        {
            var api = new GameApi(address);
            var name = prefix + Guid.NewGuid().ToString("N").Substring(0, 14);
            await TestAccounts.Register(api, name, token);
            return (api, name);
        }

        private static string Storage() => Path.Combine(Path.GetTempPath(), "wotr-ranked-" + Guid.NewGuid().ToString("N"));

        private static Task Settled(CancellationToken token) => Task.Delay(200, token);

        private static async Task OpenFindGame(MountedFlowClient client, CancellationToken token)
        {
            await ReturnTestFixture.ClickReady(client.Root, "find-game", token);
            await ReturnTestFixture.Until(() => client.Root.Q("find-game-form").resolvedStyle.display == DisplayStyle.Flex, token);
        }

        private static bool Named(VisualElement root, string name) => root.Q<Label>("find-opponent-name")?.text == name;

        private static void AssertGreyed(VisualElement root)
        {
            foreach (var name in new[] { "find-kings-enforce", "find-shadow", "find-standard-allow", "cancel-find", "show-my-games", "host-game", "quit-game", "refresh-games" })
            {
                var button = root.Q<Button>(name);
                Assert.IsFalse(button.enabledInHierarchy, name + " must not respond during a search.");
                Assert.Greater(button.worldBound.height, 0, name + " is disabled, not hidden.");
            }
        }

        private static async Task<string> PairedMatch(GameApi api, RankedSearchView view, CancellationToken token)
        {
            while (view.Status != RankedSearchStatus.Matched)
            {
                await Task.Delay(250, token);
                view = await api.Get<RankedSearchView>(Queue, token);
            }
            return view.MatchId;
        }

        private static Task Concede(GameApi api, string match, CancellationToken token) =>
            api.Post<CommandReceipt>($"api/matches/{match}/concede", new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, token);
    }
}
