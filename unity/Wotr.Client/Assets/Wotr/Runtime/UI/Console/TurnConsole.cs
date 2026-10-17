using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-160: the turn console between the dice panel and the hand: the status, a column of the board's public
    // resources (the Hunt Box above the three Elven Rings) and the permanent actions. Every zone has a fixed size;
    // a state change updates a zone's contents, never its size.
    // Choices of a decision live at their objects or in the decision window, never here (TURN_CONSOLE_UX).
    public sealed class TurnConsole
    {
        public VisualElement Root { get; } = new VisualElement { name = "turn-console" };
        public ConsoleStatus Status { get; } = new ConsoleStatus();
        public ConsoleHunt Hunt { get; } = new ConsoleHunt();
        public ConsoleRings Rings { get; } = new ConsoleRings();
        public ConsoleActions Actions { get; } = new ConsoleActions();

        public TurnConsole()
        {
            Root.AddToClassList("turn-console");
            Root.Add(Status.Root);
            Root.Add(Divider());
            var resources = new VisualElement { name = "console-resources" };
            resources.AddToClassList("console-resources");
            resources.Add(Hunt.Root);
            resources.Add(Rings.Root);
            Root.Add(resources);
            Root.Add(Divider());
            Root.Add(Actions.Root);
        }

        private static VisualElement Divider()
        {
            var divider = new VisualElement { pickingMode = PickingMode.Ignore };
            divider.AddToClassList("console-divider");
            return divider;
        }
    }
}
