using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // TEST-03: the server's test scenarios in their catalog groups. One click starts a scenario. TEST-09: the scenarios
    // stand in three tabs — dice and game, Free Peoples cards, Shadow cards (TestScenarioTabs); the tabs keep their
    // place and a tab without scenarios is disabled.
    public sealed class TestScenarioList
    {
        public VisualElement Root { get; } = new VisualElement { name = "test-scenarios" };
        public event Action<string> Start;
        public event Action Back;
        private readonly ScrollView scroll = new ScrollView { name = "test-scenario-scroll" };
        private readonly VisualElement tabs = new VisualElement { name = "test-scenario-tabs" };
        private TestScenarioCatalogView catalog = new TestScenarioCatalogView();
        public string Tab { get; private set; } = TestScenarioTabs.General;

        public TestScenarioList()
        {
            Root.AddToClassList("frontend-form");
            Root.AddToClassList("test-scenario-list");
            var header = BoardScreen.Row();
            header.Add(BoardScreen.Title("Test scenarios"));
            var back = IconButton.Create("back", "Back", () => Back?.Invoke());
            back.name = "close-test-scenarios";
            header.Add(back);
            Root.Add(header);
            Root.Add(Text("A prepared situation to test a die or an action. Play both sides with Switch side; nothing is saved.",
                "test-scenario-hint"));
            tabs.AddToClassList("test-scenario-tabs");
            Root.Add(tabs);
            scroll.AddToClassList("saved-games-scroll");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(scroll);
        }

        public void Show(TestScenarioCatalogView value)
        {
            catalog = value;
            var present = TestScenarioTabs.For(catalog);
            if (!present.Contains(Tab))
                Tab = present.FirstOrDefault() ?? TestScenarioTabs.General;
            RenderTabs();
            RenderScenarios();
        }

        public void Select(string tab)
        {
            Tab = tab;
            RenderTabs();
            RenderScenarios();
            scroll.scrollOffset = Vector2.zero;
        }

        private void RenderTabs()
        {
            tabs.Clear();
            var present = TestScenarioTabs.For(catalog);
            foreach (var tab in TestScenarioTabs.All)
            {
                var button = BoardScreen.Button(tab, () => Select(tab));
                button.name = TestScenarioTabs.Name(tab);
                button.AddToClassList("test-scenario-tab");
                button.EnableInClassList("selected", tab == Tab);
                button.EnableInClassList("test-scenario-tab-module", TestScenarioTabs.IsModule(tab));
                button.SetEnabled(present.Contains(tab));
                button.tooltip = present.Contains(tab) ? "" : TestScenarioTabs.IsModule(tab)
                    ? tab + " · Not yet available on this server." : "No scenarios here.";
                tabs.Add(button);
            }
        }

        private void RenderScenarios()
        {
            scroll.Clear();
            foreach (var group in catalog.Scenarios.Where(scenario => TestScenarioTabs.Of(scenario) == Tab).GroupBy(scenario => scenario.Group))
            {
                scroll.Add(Text(TestScenarioTabs.GroupTitle(Tab, group.Key), "test-scenario-group"));
                foreach (var scenario in group)
                    scroll.Add(Row(scenario));
            }
        }

        private Button Row(TestScenarioView scenario)
        {
            var button = BoardScreen.Button("", () => Start?.Invoke(scenario.Id));
            button.name = "test-scenario-" + scenario.Id;
            button.AddToClassList("test-scenario");
            button.Add(Text(scenario.Title, "test-scenario-title"));
            button.Add(Text(scenario.Description, "test-scenario-description"));
            return button;
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }
    }
}
