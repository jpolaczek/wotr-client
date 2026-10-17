using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // The strip under the top bar: the decision notice, the action log and last battle icons (UI-100),
    // the stack of opponent action pennants (UI-99/UI-100) and the retry control of the notice feed.
    public sealed class ActionNoticeBar
    {
        public VisualElement Root { get; } = BoardScreen.Row("notification-bar");
        public ActionNoticeStack Stack { get; }
        // UI-99: the notice stack replaced the overflow list. These stay hidden so that callers written
        // for the list (the Esc registry, older tests) keep compiling and see a closed window.
        public VisualElement Overflow { get; } = new VisualElement { name = "notification-overflow" };
        public Button More { get; }
        public bool Pinned => false;
        public event Action Retry;
        private readonly BoardScreen board;
        private readonly Button status;
        private IReadOnlyList<ActionNoticeGroup> groups = Array.Empty<ActionNoticeGroup>();

        public ActionNoticeBar(BoardScreen board)
        {
            this.board = board;
            Root.name = "notification-bar";
            Root.AddToClassList("action-history");
            Root.Add(board.Notice.Button);
            // UI-100: with the map control row gone, the history entries sit as icons beside the stack.
            Root.Add(Adopt(board.Log.Toggle, "notice-log-icon"));
            Root.Add(Adopt(board.BattleReport.Open, "notice-battle-icon"));
            Stack = new ActionNoticeStack(board, () => { });
            Root.Add(Stack.Root);
            More = BoardScreen.Button("", () => { });
            More.name = "notification-more";
            More.style.display = DisplayStyle.None;
            Overflow.AddToClassList("notification-overflow");
            Overflow.style.display = DisplayStyle.None;
            status = IconButton.Create("refresh", "Retry", () => Retry?.Invoke());
            status.name = "notification-retry";
            Root.Add(status);
            SetStatus("");
        }

        private static Button Adopt(Button button, string iconClass)
        {
            button.text = "";
            button.AddToClassList("notice-icon-button");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("notice-icon");
            icon.AddToClassList(iconClass);
            button.Add(icon);
            return button;
        }

        public void Apply(IReadOnlyList<ActionNoticeGroup> value)
        {
            groups = value;
            Stack.Apply(groups);
            RefreshVisibility();
        }

        public void SetStatus(string text)
        {
            status.style.display = text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            status.tooltip = text;
            RefreshVisibility();
        }

        public void RefreshVisibility()
        {
            var visible = Stack.Count > 0 || board.Log.Toggle.enabledSelf ||
                status.style.display.value != DisplayStyle.None ||
                board.Notice.Button.style.display.value != DisplayStyle.None;
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Close() { }
        public void Pin() { }
        public void Position() { }
    }
}
