using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-160: the default host of the decision tray when no object's panel holds it — a window of fixed width with a
    // fixed place, anchored above the console and growing upwards. It shows only while the tray offers choices; hints
    // and statuses stay in the console. A decision opens it once; closing keeps it closed until the next decision or
    // a click on the console status.
    public sealed class DecisionWindow
    {
        public VisualElement Root { get; } = new VisualElement { name = "decision-window" };
        public VisualElement Dock { get; } = new VisualElement { name = "decision-dock" };
        // Transient previews step aside when the window opens over the board.
        public event System.Action Opening;
        public bool IsOpen => Root.style.display.value != DisplayStyle.None;
        public bool HasChoices => choices;
        private readonly Label title;
        private string current = "";
        private string closedFor = "";
        private bool choices;

        public DecisionWindow()
        {
            Root.AddToClassList("decision-window");
            var header = BoardScreen.Row("window-header");
            title = BoardScreen.Title("Decision");
            header.Add(title);
            var close = IconButton.Create("close", "Close · Keep your choices", Close);
            close.name = "close-decision";
            header.Add(close);
            Root.Add(header);
            Dock.AddToClassList("decision-dock");
            Root.Add(Dock);
            Root.style.display = DisplayStyle.None;
        }

        // key names the decision and draft step. The window follows the choices: a step whose choice is on the
        // map hides it and the next step with choices shows it again, unless the player closed it for this key.
        public void Apply(bool hasChoices, string heading, string key)
        {
            choices = hasChoices;
            current = key;
            title.text = heading;
            if (!hasChoices)
                Root.style.display = DisplayStyle.None;
            else if (key != closedFor && !IsOpen)
                Show();
        }

        public void Open()
        {
            if (!choices)
                return;
            closedFor = "";
            Show();
        }

        public void Close()
        {
            closedFor = current;
            Root.style.display = DisplayStyle.None;
        }

        private void Show()
        {
            Opening?.Invoke();
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
        }
    }
}
