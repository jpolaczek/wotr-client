using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // MENU-26: the waiting room's chat: a fixed-height list that scrolls inside itself with the newest line at the
    // bottom, one line to write (200 characters) and Send. Messages are shown as plain text, never as markup.
    public sealed class RoomChatPanel
    {
        public const int LongestText = 200;
        public VisualElement Root { get; } = new VisualElement { name = "room-chat" };
        public TextField Input { get; } = new TextField { name = "room-chat-input", maxLength = LongestText };
        public Button Send { get; }
        public long Latest { get; private set; }
        public event Action<string> Sent;
        private readonly ScrollView log = new ScrollView(ScrollViewMode.Vertical) { name = "room-chat-log" };

        public RoomChatPanel()
        {
            Root.AddToClassList("room-chat");
            var title = new Label("Chat") { pickingMode = PickingMode.Ignore };
            title.AddToClassList("room-chat-title");
            Root.Add(title);
            log.AddToClassList("room-chat-log");
            log.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(log);
            var line = new VisualElement();
            line.AddToClassList("room-chat-line");
            Input.AddToClassList("room-chat-input");
            // Enter is caught on the way down, before the text field's own key handling can keep it.
            Root.RegisterCallback<KeyDownEvent>(EnterSends, TrickleDown.TrickleDown);
            line.Add(Input);
            Send = BoardScreen.Button("Send", Submit);
            Send.name = "room-chat-send";
            Send.AddToClassList("room-chat-send");
            line.Add(Send);
            Root.Add(line);
        }

        // New messages after the last one shown; a lower latest number means the server restarted, so the list starts over.
        public void Show(RoomChatView view)
        {
            if (view.Latest < Latest)
            {
                log.Clear();
                Latest = 0;
            }
            var added = false;
            foreach (var message in view.Messages)
            {
                if (message.Sequence <= Latest)
                    continue;
                log.Add(Line(message));
                Latest = message.Sequence;
                added = true;
            }
            if (added)
                log.schedule.Execute(() => log.scrollOffset = new Vector2(0, float.MaxValue));
        }

        public void Clear() => Input.SetValueWithoutNotify("");

        public void Busy(bool value) => Send.SetEnabled(!value);

        private static VisualElement Line(RoomChatMessage message)
        {
            var line = new VisualElement { name = "room-chat-message-" + message.Sequence, pickingMode = PickingMode.Ignore };
            line.AddToClassList("room-chat-message");
            // OBS-04: an observer writes under their name and role; a player in the colour of their side.
            var observer = message.Role == RoomRole.Observer;
            var author = new Label(message.Author + (observer ? " (observer):" : ":")) { enableRichText = false, pickingMode = PickingMode.Ignore };
            author.AddToClassList("room-chat-author");
            author.AddToClassList(observer ? "observer" : message.Side == Side.FreePeoples ? "side-free" : "side-shadow");
            var text = new Label(message.Text) { enableRichText = false, pickingMode = PickingMode.Ignore };
            text.AddToClassList("room-chat-text");
            line.Add(author);
            line.Add(text);
            return line;
        }

        private void EnterSends(KeyDownEvent e)
        {
            if (e.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter) || !(e.target is VisualElement target) ||
                target != Input && !Input.Contains(target))
                return;
            Submit();
            e.StopImmediatePropagation();
        }

        private void Submit()
        {
            var text = Input.value?.Trim() ?? "";
            if (text.Length > 0)
                Sent?.Invoke(text);
        }
    }
}
