using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // TEST-03: the server's test scenarios in their catalog groups. One click starts a scenario. TEST-09: the scenarios
    // stand in three tabs — dice and game, Free Peoples cards, Shadow cards (TestScenarioTabs); the tabs keep their
    // place and a tab without scenarios is disabled. UI-295: a search field finds scenarios by name in every tab; a tab's
    // click clears it.
    public sealed class TestScenarioList
    {
        public VisualElement Root { get; } = new VisualElement { name = "test-scenarios" };
        public event Action<string> Start;
        public event Action Back;
        private readonly ScrollView scroll = new ScrollView { name = "test-scenario-scroll" };
        private readonly VisualElement tabs = new VisualElement { name = "test-scenario-tabs" };
        public TextField Search { get; } = new TextField { name = "test-scenario-search" };
        private readonly Button clear;
        private string query = "";
        private TestScenarioCatalogView catalog = new TestScenarioCatalogView();
        public string Tab { get; private set; } = TestScenarioTabs.General;

        public TestScenarioList()
        {
            Root.AddToClassList("frontend-form");
            Root.AddToClassList("test-scenario-list");
            // UI-415 (owner): Back stands beside the hint; the menu window's header already names the page.
            var header = BoardScreen.Row("test-scenario-header");
            var back = IconButton.Create("back", "Back", () => Back?.Invoke());
            back.name = "close-test-scenarios";
            header.Add(back);
            header.Add(Text("A prepared situation to test a die or an action. Play both sides with Switch side; nothing is saved.",
                "test-scenario-hint"));
            Root.Add(header);
            tabs.AddToClassList("test-scenario-tabs");
            Root.Add(tabs);
            var search = BoardScreen.Row("test-scenario-search-row");
            Search.AddToClassList("test-scenario-search");
            Search.textEdition.placeholder = "Search scenarios by name";
            Search.RegisterValueChangedCallback(e => Find(e.newValue));
            search.Add(Search);
            clear = IconButton.Create("close", "Clear the search", () =>
            {
                Search.SetValueWithoutNotify("");
                Find("");
            });
            clear.name = "clear-test-scenario-search";
            search.Add(clear);
            Root.Add(search);
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
            Search.SetValueWithoutNotify("");
            query = "";
            RenderTabs();
            RenderScenarios();
            scroll.scrollOffset = Vector2.zero;
        }

        // UI-295: the scenarios whose name holds every word of the query, from all tabs.
        public void Find(string value)
        {
            query = value ?? "";
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
                button.EnableInClassList("selected", tab == Tab && !TestScenarioSearch.Active(query));
                button.SetEnabled(present.Contains(tab));
                tabs.Add(button);
            }
        }

        private void RenderScenarios()
        {
            scroll.Clear();
            var searching = TestScenarioSearch.Active(query);
            clear.SetEnabled(searching);
            var shown = catalog.Scenarios.Where(scenario => searching
                ? TestScenarioSearch.Matches(scenario, query)
                : TestScenarioTabs.Of(scenario.Group) == Tab).ToList();
            if (searching && shown.Count == 0)
                scroll.Add(Text("No scenario's name matches “" + query.Trim() + "”.", "test-scenario-none"));
            foreach (var group in shown.GroupBy(scenario => scenario.Group))
            {
                scroll.Add(Text(searching ? group.Key : TestScenarioTabs.GroupTitle(group.Key), "test-scenario-group"));
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
