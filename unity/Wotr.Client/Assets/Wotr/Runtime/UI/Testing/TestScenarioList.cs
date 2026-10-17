using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // TEST-03: the server's test scenarios in their catalog groups. One click starts a scenario.
    public sealed class TestScenarioList
    {
        public VisualElement Root { get; } = new VisualElement { name = "test-scenarios" };
        public event Action<string> Start;
        public event Action Back;
        private readonly ScrollView scroll = new ScrollView { name = "test-scenario-scroll" };

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
            scroll.AddToClassList("saved-games-scroll");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(scroll);
        }

        public void Show(TestScenarioCatalogView catalog)
        {
            scroll.Clear();
            foreach (var group in catalog.Scenarios.GroupBy(scenario => scenario.Group))
            {
                scroll.Add(Text(group.Key, "test-scenario-group"));
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
