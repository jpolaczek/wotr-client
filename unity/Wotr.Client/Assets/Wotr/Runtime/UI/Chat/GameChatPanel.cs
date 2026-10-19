using UnityEngine.UIElements;

namespace Wotr.Client
{
    // CHAT-01: the board's chat. A small icon at the bottom left, beside the map legend, unrolls a compact panel just
    // above itself over the map's western sea, so it covers as little of the board as it can. Observers read every
    // message and players only the players' (the server filters). A message that arrives while the panel is rolled
    // up marks the icon with "?" until the panel opens. The icon shows only on a board whose server has the chat.
    public sealed class GameChatPanel
    {
        public const string UnreadMark = "?";
        private const string Tooltip = "Chat · Players read each other; observers read everyone";
        public Button Icon { get; }
        public Label Badge { get; } = new Label(UnreadMark) { name = "game-chat-unread", pickingMode = PickingMode.Ignore };
        public VisualElement Root { get; } = new VisualElement { name = "game-chat" };
        public RoomChatPanel Chat { get; } = new RoomChatPanel();
        public Label Status { get; } = new Label { name = "game-chat-status", pickingMode = PickingMode.Ignore };
        public bool IsOpen => Root.style.display.value == DisplayStyle.Flex;
        public bool Unread { get; private set; }

        public GameChatPanel()
        {
            Icon = IconButton.Create("chat", Tooltip, Toggle);
            Icon.name = "game-chat-icon";
            Icon.AddToClassList("game-chat-icon");
            Badge.AddToClassList("game-chat-unread");
            Icon.Add(Badge);
            Root.AddToClassList("game-chat");
            Root.Add(Chat.Root);
            Status.AddToClassList("game-chat-status");
            Root.Add(Status);
            Chat.Added += Received;
            SetUnread(false);
            Available(false);
            Close();
        }

        // The icon stands only where the server answers the board's chat.
        public void Available(bool value)
        {
            Icon.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (!value)
                Close();
        }

        // UI-281: the panel always opens on its newest message, with the line to write focused.
        public void Open()
        {
            Root.style.display = DisplayStyle.Flex;
            SetUnread(false);
            Chat.ShowLatest();
            Chat.Input.Focus();
        }

        public void Close() => Root.style.display = DisplayStyle.None;

        // UI-281: Enter and Escape typed in the chat belong to the chat, not to the board's confirm and back keys.
        public static bool ContainsInput(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.name == "game-chat")
                    return true;
            return false;
        }

        private void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        // The messages already there when the board opens are not news; later ones are while the panel is rolled up.
        private void Received(int count, bool first)
        {
            if (!first && !IsOpen)
                SetUnread(true);
        }

        private void SetUnread(bool value)
        {
            Unread = value;
            Badge.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            Icon.tooltip = value ? Tooltip + "\nNew message" : Tooltip;
        }
    }
}
