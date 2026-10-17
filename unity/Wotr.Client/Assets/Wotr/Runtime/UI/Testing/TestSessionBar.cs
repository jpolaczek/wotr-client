using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // TEST-03: marks a test game under the notices and keeps its three controls one click away:
    // play the other side, restart the scenario, leave the test. Root is a lane centred over the map
    // beside the right column; only the bar inside it takes the pointer.
    public sealed class TestSessionBar
    {
        public VisualElement Root { get; } = new VisualElement { name = "test-session-lane", pickingMode = PickingMode.Ignore };
        public VisualElement Bar { get; } = new VisualElement { name = "test-session-bar" };
        public event Action SwitchSide;
        public event Action Restart;
        public event Action Exit;
        private readonly Label title = new Label { name = "test-session-title" };
        private readonly Button side;

        public TestSessionBar()
        {
            Root.AddToClassList("test-session-lane");
            Root.style.display = DisplayStyle.None;
            Bar.AddToClassList("test-session-bar");
            Bar.AddToClassList("map-edge-control");
            Root.Add(Bar);
            title.AddToClassList("test-session-title");
            Bar.Add(title);
            side = Control("", "test-switch-side", () => SwitchSide?.Invoke());
            Control("Restart", "test-restart", () => Restart?.Invoke()).tooltip = "Start this scenario again from the same situation";
            Control("Exit", "test-exit", () => Exit?.Invoke()).tooltip = "Leave the test; nothing is saved";
        }

        public void Show(TestScenarioView scenario, Side playing)
        {
            title.text = "TEST · " + scenario.Title;
            title.tooltip = scenario.Description;
            side.text = playing == Side.FreePeoples ? "Play Shadow" : "Play Free Peoples";
            side.tooltip = "You play the " + (playing == Side.FreePeoples ? "Free Peoples" : "Shadow") + ". Switch to the other side.";
            Root.style.display = DisplayStyle.Flex;
        }

        private Button Control(string text, string name, Action action)
        {
            var button = BoardScreen.Button(text, action);
            button.name = name;
            button.AddToClassList("test-session-control");
            Bar.Add(button);
            return button;
        }
    }
}
