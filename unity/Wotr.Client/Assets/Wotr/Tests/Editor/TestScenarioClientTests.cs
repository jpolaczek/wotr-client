using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // TEST-03: the client parts of the test mode without a server.
    public sealed class TestScenarioClientTests
    {
        private static TestScenarioCatalogView Catalog() => new TestScenarioCatalogView
        {
            Scenarios = new List<TestScenarioView>
            {
                Scenario("fp-army-move", "Free Peoples · Army die", "Move armies", Side.FreePeoples),
                Scenario("fp-pass", "Free Peoples · Rings and passing", "Pass", Side.FreePeoples),
                Scenario("sh-army-move", "Shadow · Army die", "Move armies", Side.Shadow)
            }
        };

        private static TestScenarioView Scenario(string id, string group, string title, Side side) => new TestScenarioView
        {
            Id = id, Group = group, Title = title, Description = "Description of " + id, StartingSide = side
        };

        [Test]
        public void TheListShowsEveryScenarioUnderItsGroup()
        {
            var list = new TestScenarioList();
            list.Show(Catalog());
            CollectionAssert.AreEqual(new[] { "Free Peoples · Army die", "Free Peoples · Rings and passing", "Shadow · Army die" },
                list.Root.Query<Label>(className: "test-scenario-group").ToList().Select(l => l.text));
            var row = list.Root.Q<Button>("test-scenario-sh-army-move");
            Assert.AreEqual("Move armies", row.Q<Label>(className: "test-scenario-title").text);
            Assert.AreEqual("Description of sh-army-move", row.Q<Label>(className: "test-scenario-description").text);
            list.Show(new TestScenarioCatalogView());
            Assert.IsNull(list.Root.Q<Button>("test-scenario-sh-army-move"), "A new catalog replaces the rows.");
        }

        // TEST-09: card scenarios stand in their side's tab; the dice and game scenarios keep the first tab.
        [Test]
        public void CardScenariosStandInTheirSidesTab()
        {
            var catalog = Catalog();
            catalog.Scenarios.Add(Scenario("card-base.fp.character.01-event", "Free Peoples cards · Character", "Elven Cloaks · Event", Side.FreePeoples));
            catalog.Scenarios.Add(Scenario("card-base.fp.strategy.01-combat", "Free Peoples cards · Strategy", "Book of Mazarbul · Combat: Durin's Bane", Side.FreePeoples));
            var list = new TestScenarioList();
            list.Show(catalog);
            Assert.AreEqual(TestScenarioTabs.General, list.Tab);
            CollectionAssert.AreEqual(new[] { "test-tab-dice-game", "test-tab-free-peoples-cards", "test-tab-shadow-cards" },
                list.Root.Query<Button>(className: "test-scenario-tab").ToList().Select(b => b.name));
            Assert.IsNull(list.Root.Q<Button>("test-scenario-card-base.fp.character.01-event"), "Card scenarios wait in their tab.");
            Assert.IsFalse(list.Root.Q<Button>("test-tab-shadow-cards").enabledSelf, "A tab without scenarios keeps its place, disabled.");
            list.Select(TestScenarioTabs.FreePeoplesCards);
            CollectionAssert.AreEqual(new[] { "Character", "Strategy" },
                list.Root.Query<Label>(className: "test-scenario-group").ToList().Select(l => l.text));
            Assert.IsNotNull(list.Root.Q<Button>("test-scenario-card-base.fp.character.01-event"));
            Assert.IsNull(list.Root.Q<Button>("test-scenario-fp-army-move"));
            Assert.IsTrue(list.Root.Q<Button>("test-tab-free-peoples-cards").ClassListContains("selected"));
            list.Show(catalog);
            Assert.AreEqual(TestScenarioTabs.FreePeoplesCards, list.Tab, "A refreshed catalog keeps the open tab.");
            Assert.AreEqual(TestScenarioTabs.ShadowCards, TestScenarioTabs.Of("Shadow cards · Strategy"));
            Assert.AreEqual(TestScenarioTabs.General, TestScenarioTabs.Of("Shadow · Army die"));
        }

        [Test]
        public void TheBarNamesTheScenarioAndOffersTheOtherSide()
        {
            var bar = new TestSessionBar();
            Assert.AreEqual(DisplayStyle.None, bar.Root.style.display.value);
            var scenario = Catalog().Scenarios[0];
            bar.Show(scenario, Side.FreePeoples);
            Assert.AreEqual(DisplayStyle.Flex, bar.Root.style.display.value);
            Assert.AreEqual("TEST · Move armies", bar.Root.Q<Label>("test-session-title").text);
            Assert.AreEqual("Description of fp-army-move", bar.Root.Q<Label>("test-session-title").tooltip);
            Assert.AreEqual("Play Shadow", bar.Root.Q<Button>("test-switch-side").text);
            bar.Show(scenario, Side.Shadow);
            Assert.AreEqual("Play Free Peoples", bar.Root.Q<Button>("test-switch-side").text);
            Assert.IsNotNull(bar.Root.Q<Button>("test-restart"));
            Assert.IsNotNull(bar.Root.Q<Button>("test-exit"));
            Assert.IsTrue(bar.Bar.ClassListContains("map-edge-control"));
            Assert.AreEqual(PickingMode.Ignore, bar.Root.pickingMode, "Only the bar, not its lane, takes the pointer.");
        }

        [Test]
        public void TheSessionPlaysTheStartingSideAndSwitchesBetweenBothSessions()
        {
            using var owner = new GameApi("http://localhost");
            var view = new TestSessionView
            {
                MatchId = "test-match",
                Scenario = Catalog().Scenarios[2],
                OwnerSide = Side.FreePeoples,
                Seat = new SessionView { Token = "seat-token", AccountId = "testseat-owner", Username = "Test opponent" }
            };
            using var session = new TestSession(owner, view);
            Assert.AreEqual(Side.Shadow, session.Side);
            Assert.AreEqual("seat-token", session.Api.Token);
            Assert.AreEqual(owner.Address, session.Api.Address);
            session.SwitchSide();
            Assert.AreEqual(Side.FreePeoples, session.Side);
            Assert.AreSame(owner, session.Api);
        }

        [Test]
        public async Task AServerWithoutTheTestModeHidesItAndOtherErrorsStillReport()
        {
            using (var disabled = new GameApi("http://localhost", new Fixed(HttpStatusCode.NotFound, "")))
                Assert.IsNull(await TestScenarioApi.Catalog(disabled, CancellationToken.None));
            using (var enabled = new GameApi("http://localhost", new Fixed(HttpStatusCode.OK,
                       "{\"scenarios\":[{\"id\":\"fp-pass\",\"group\":\"G\",\"title\":\"Pass\",\"description\":\"D\",\"startingSide\":\"FreePeoples\"}]}")))
                Assert.AreEqual("fp-pass", (await TestScenarioApi.Catalog(enabled, CancellationToken.None)).Scenarios.Single().Id);
            using (var failing = new GameApi("http://localhost", new Fixed(HttpStatusCode.InternalServerError, "")))
                Assert.ThrowsAsync<ApiException>(() => TestScenarioApi.Catalog(failing, CancellationToken.None));
        }

        private sealed class Fixed : HttpMessageHandler
        {
            private readonly HttpStatusCode status;
            private readonly string body;
            public Fixed(HttpStatusCode status, string body) { this.status = status; this.body = body; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }

        [Test]
        public void TheMenuOffersTestScenariosOnlyWhenTheServerHasThem()
        {
            var menu = new MainMenuScreen("tester");
            var button = menu.Root.Q<Button>("open-test-scenarios");
            Assert.AreEqual(DisplayStyle.None, button.style.display.value);
            menu.ShowTestScenarios(Catalog());
            Assert.AreEqual(DisplayStyle.Flex, button.style.display.value);
            Assert.IsNotNull(menu.Root.Q<Button>("test-scenario-fp-pass"));
            menu.ShowTestScenarios(new TestScenarioCatalogView());
            Assert.AreEqual(DisplayStyle.None, button.style.display.value);
        }
    }
}
