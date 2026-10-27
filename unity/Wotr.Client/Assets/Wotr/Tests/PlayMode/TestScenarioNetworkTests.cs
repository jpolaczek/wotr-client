using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    // TEST-03: a test scenario from the main menu against the real server: the tested action, both sides,
    // restart to the same situation and exit without a saved game. TEST-12–TEST-14: every expansion scenario of the
    // server stands in its expansion's tab, and the search finds an expansion card by its name.
    public sealed class TestScenarioNetworkTests
    {
        [UnityTest]
        public IEnumerator AScenarioPlaysBothSidesRestartsAndLeavesNoSavedGame() => MatchTestRoom.Wait(Scenario());

        [UnityTest]
        public IEnumerator EveryExpansionScenarioStandsInItsExpansionsTab() => MatchTestRoom.Wait(ExpansionTabs());

        private static async Task Scenario()
        {
            var fixture = Load();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-test03-" + Guid.NewGuid().ToString("N")));
            await ClickReady(client.Root, "open-test-scenarios", token);
            Assert.IsNotNull(client.Root.Q<Button>("test-scenario-sh-elven-ring"), "The menu lists the server catalog.");
            await Task.Delay(120, token);
            LayoutTests.Save(client.Panel.targetTexture, "test03-scenario-list.png");
            // UI-295: typing a name keeps only the matching scenarios; the × brings the tab back.
            client.Root.Q<TextField>("test-scenario-search").value = "elven ring";
            await Task.Delay(120, token);
            Assert.IsNotNull(client.Root.Q<Button>("test-scenario-sh-elven-ring"));
            Assert.IsNull(client.Root.Q<Button>("test-scenario-fp-pass"), "Only matching names stay.");
            LayoutTests.Save(client.Panel.targetTexture, "ui295-scenario-search.png");
            await ClickReady(client.Root, "clear-test-scenario-search", token);
            await Task.Delay(120, token);
            Assert.AreEqual("", client.Root.Q<TextField>("test-scenario-search").value);
            // UI-368: the scenario starts from a search, which the list keeps for the way back.
            client.Root.Q<TextField>("test-scenario-search").value = "pass";
            await Task.Delay(120, token);
            await ClickReady(client.Root, "test-scenario-fp-pass", token);
            await Live(client, Side.FreePeoples, token);
            var first = client.Board.View;
            Assert.AreEqual("TEST · Pass", client.Root.Q<Label>("test-session-title").text);
            Assert.AreEqual("Play Shadow", client.Root.Q<Button>("test-switch-side").text);
            LayoutTests.Save(client.Panel.targetTexture, "test03-scenario-free-peoples.png");

            await ClickReady(client.Root, "console-pass", token);
            await Until(() => client.Board.View.Revision > first.Revision, token);
            Assert.AreEqual(Side.Shadow, client.Board.View.DecisionOwner, "After the pass the Shadow decides.");

            await ClickReady(client.Root, "test-switch-side", token);
            await Live(client, Side.Shadow, token);
            Assert.AreEqual(first.MatchId, client.Board.View.MatchId);
            Assert.AreEqual(Side.Shadow, client.Board.View.DecisionOwner);
            Assert.IsTrue(client.Board.View.Options.Any(o => o.Enabled && o.Kind == CommandKind.MoveArmies), "The Shadow can act from the same client.");
            Assert.AreEqual("Play Free Peoples", client.Root.Q<Button>("test-switch-side").text);
            LayoutTests.Save(client.Panel.targetTexture, "test03-scenario-shadow.png");

            await ClickReady(client.Root, "test-restart", token);
            await Until(() => client.Board?.View != null && client.Board.View.MatchId != first.MatchId && client.Board.Connection.text == "Live", token);
            var restarted = client.Board.View;
            Assert.AreEqual(Side.FreePeoples, restarted.You, "A restart opens the starting side.");
            Assert.AreEqual(first.Revision, restarted.Revision);
            Assert.AreEqual(Side.FreePeoples, restarted.DecisionOwner);
            Assert.IsTrue(restarted.Options.Any(o => o.Enabled && o.Kind == CommandKind.Pass), "The same situation offers the pass again.");

            await ClickReady(client.Root, "test-exit", token);
            await Until(() => client.Board == null && client.Root.Q("test-scenarios")?.resolvedStyle.display == DisplayStyle.Flex, token);
            Assert.AreEqual("Test scenarios", client.Root.Q<Label>("menu-window-title").text, "Exit returns to the test list.");
            Assert.AreEqual("pass", client.Root.Q<TextField>("test-scenario-search").value, "The list keeps the search.");
            Assert.IsNotNull(client.Root.Q<Button>("test-scenario-fp-pass"));
            LayoutTests.Save(client.Panel.targetTexture, "ui368-back-to-test-scenarios.png");
            var games = await api.Get<MatchListView>("api/matches?status=All&limit=50", token);
            Assert.IsFalse(games.Matches.Any(m => m.MatchId == first.MatchId || m.MatchId == restarted.MatchId), "Test games are not saved games.");
            try
            {
                await api.Get<PlayerView>("api/matches/" + restarted.MatchId, token);
                Assert.Fail("Exit removes the test game.");
            }
            catch (ApiException error) when (error.Status == 404) { }
        }

        private static async Task ExpansionTabs()
        {
            var fixture = Load();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            using var api = await fixture.Login(Side.FreePeoples, token);
            var catalog = await api.Get<TestScenarioCatalogView>("api/test-scenarios", token);
            AssertTab(catalog, "fate-", TestScenarioTabs.Fate, 30);
            AssertTab(catalog, "lords-", TestScenarioTabs.Lords, 80);
            AssertTab(catalog, "kings-", TestScenarioTabs.Kings, 80);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-test12-" + Guid.NewGuid().ToString("N")));
            await ClickReady(client.Root, "open-test-scenarios", token);
            foreach (var tab in TestScenarioTabs.Modules)
            {
                await ClickReady(client.Root, TestScenarioTabs.Name(tab), token);
                await Task.Delay(120, token);
                var first = catalog.Scenarios.First(scenario => TestScenarioTabs.Of(scenario) == tab);
                Assert.IsNotNull(client.Root.Q<Button>("test-scenario-" + first.Id), tab + " lists its scenarios.");
                LayoutTests.Save(client.Panel.targetTexture, "test12-tab-" + tab.ToLowerInvariant() + ".png");
            }
            client.Root.Q<TextField>("test-scenario-search").value = "captain of despair";
            await Task.Delay(120, token);
            Assert.IsNotNull(client.Root.Q<Button>("test-scenario-lords-card-captain-of-despair-event"), "The search finds a Lords card.");
            Assert.IsNull(client.Root.Q<Button>("test-scenario-fp-pass"), "Only matching names stay.");
            LayoutTests.Save(client.Panel.targetTexture, "test12-search-captain-of-despair.png");
        }

        // Every scenario named for an expansion stands in that expansion's tab, and the tab holds at least the given number.
        private static void AssertTab(TestScenarioCatalogView catalog, string prefix, string tab, int least)
        {
            var scenarios = catalog.Scenarios.Where(scenario => scenario.Id.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            Assert.GreaterOrEqual(scenarios.Count, least, tab);
            foreach (var scenario in scenarios)
                Assert.AreEqual(tab, TestScenarioTabs.Of(scenario), scenario.Id);
        }

        private static Task Live(MountedFlowClient client, Side side, CancellationToken token) =>
            Until(() => client.Board?.View != null && client.Board.View.You == side && client.Board.Connection.text == "Live", token);
    }
}
