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
    // restart to the same situation and exit without a saved game.
    public sealed class TestScenarioNetworkTests
    {
        [UnityTest]
        public IEnumerator AScenarioPlaysBothSidesRestartsAndLeavesNoSavedGame() => MatchTestRoom.Wait(Scenario());

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
            await Until(() => client.Board == null && client.Root.Q("main-menu") != null, token);
            var games = await api.Get<MatchListView>("api/matches?status=All&limit=50", token);
            Assert.IsFalse(games.Matches.Any(m => m.MatchId == first.MatchId || m.MatchId == restarted.MatchId), "Test games are not saved games.");
            try
            {
                await api.Get<PlayerView>("api/matches/" + restarted.MatchId, token);
                Assert.Fail("Exit removes the test game.");
            }
            catch (ApiException error) when (error.Status == 404) { }
        }

        private static Task Live(MountedFlowClient client, Side side, CancellationToken token) =>
            Until(() => client.Board?.View != null && client.Board.View.You == side && client.Board.Connection.text == "Live", token);
    }
}
