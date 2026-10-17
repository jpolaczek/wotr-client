using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // MENU-22: the other player deleted this game. The board behind is gone from the server, so the notice stays
    // until Main menu: Escape and a click beside it change nothing.
    public sealed class GameDeletedNotice
    {
        public VisualElement Root { get; } = new VisualElement { name = "game-deleted" };
        public bool IsOpen => Root.style.display.value == DisplayStyle.Flex;
        public event Action MenuRequested;
        private readonly Button menu;
        private readonly Label heading = BoardScreen.Title(DeletedTitle);
        private readonly Label text = new Label(DeletedNote) { name = "game-deleted-note" };
        private const string DeletedTitle = "Game deleted";
        private const string DeletedNote = "The other player deleted this game. It is no longer on the server.";

        public GameDeletedNotice()
        {
            Root.AddToClassList("settings-scrim");
            Root.AddToClassList("confirmation");
            Root.style.display = DisplayStyle.None;
            var window = new VisualElement { name = "game-deleted-window" };
            window.AddToClassList("confirm-window");
            window.Add(heading);
            text.AddToClassList("confirm-note");
            window.Add(text);
            menu = BoardScreen.Button("Main menu", () => MenuRequested?.Invoke());
            menu.name = "game-deleted-menu";
            var actions = BoardScreen.Row("confirm-actions");
            actions.Add(menu);
            window.Add(actions);
            Root.Add(window);
        }

        public void Open() => Open(DeletedTitle, DeletedNote);

        // OBS-05: the same notice ends watching a game that an observer can no longer read.
        public void Open(string title, string note)
        {
            heading.text = title;
            text.text = note;
            if (IsOpen)
                return;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            menu.Focus();
        }
    }
}
