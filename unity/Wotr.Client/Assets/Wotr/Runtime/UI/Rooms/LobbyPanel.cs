using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // LOBBY-01: the Open games screen's chat under the game list, with a compact column of the players online beside it.
    // LOBBY-02: the column folds into a grey strip at the right edge; the tab at the strip's bottom folds and unfolds it
    // and never moves. Unfolded, the column lies over the chat's right side and its input instead of narrowing them.
    // LOBBY-04 (owner 2026-10-26: "the chat in the main menu is to be unfolded by default"): it starts unfolded. A
    // right-click on a player's name (in the column or on a message) opens a small tab with Send challenge. The panel
    // keeps a fixed height; only its own log scrolls.
    public sealed class LobbyPanel
    {
        public VisualElement Root { get; } = new VisualElement { name = "lobby-panel" };
        public TextField Input { get; } = new TextField { name = "lobby-chat-input", maxLength = LobbyLimits.LongestMessage };
        public Label Status { get; } = new Label { name = "lobby-status" };
        public VisualElement ContextTab { get; } = new VisualElement { name = "lobby-context" };
        public bool OnlineShown { get; private set; } = true;
        private const float ContextWidth = 170;
        private const float ContextHeight = 80;
        public event Action<string> Send;
        public event Action<string> Challenge;
        private readonly ScrollView log = new ScrollView(ScrollViewMode.Vertical) { name = "lobby-chat-log" };
        private readonly VisualElement online = new VisualElement { name = "lobby-online" };
        private readonly ScrollView onlineList = new ScrollView(ScrollViewMode.Vertical) { name = "lobby-online-list" };
        private readonly Label onlineTitle = new Label("Online") { name = "lobby-online-title", pickingMode = PickingMode.Ignore };
        private Button tab;
        private readonly Label contextName = new Label { name = "lobby-context-name", pickingMode = PickingMode.Ignore };
        private string onlineKey = "";
        private long shownLatest;
        private string contextTarget = "";

        public LobbyPanel()
        {
            Root.AddToClassList("lobby-panel");
            var chat = new VisualElement { name = "lobby-chat" };
            chat.AddToClassList("lobby-chat");
            chat.Add(Header());
            log.AddToClassList("lobby-chat-log");
            log.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            chat.Add(log);
            chat.Add(InputRow());
            Status.AddToClassList("lobby-status");
            chat.Add(Status);
            Root.Add(chat);
            online.AddToClassList("lobby-online");
            onlineTitle.AddToClassList("lobby-online-title");
            online.Add(onlineTitle);
            onlineList.AddToClassList("lobby-online-list");
            onlineList.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            online.Add(onlineList);
            Root.Add(online);
            Root.Add(Strip());
            ShowOnlineColumn();
            BuildContextTab();
            Root.RegisterCallback<PointerDownEvent>(CloseContextOutside, TrickleDown.TrickleDown);
            Root.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) CloseContext(); }, TrickleDown.TrickleDown);
            RenderOnlineCount(0);
        }

        private VisualElement Header()
        {
            var header = new VisualElement { name = "lobby-chat-header" };
            header.AddToClassList("lobby-chat-header");
            var title = new Label("Lobby chat") { pickingMode = PickingMode.Ignore };
            title.AddToClassList("lobby-chat-title");
            header.Add(title);
            var hint = new Label("Right-click a name to challenge") { pickingMode = PickingMode.Ignore };
            hint.AddToClassList("lobby-chat-hint");
            header.Add(hint);
            return header;
        }

        private VisualElement InputRow()
        {
            var row = new VisualElement { name = "lobby-chat-line" };
            row.AddToClassList("lobby-chat-line");
            Input.AddToClassList("lobby-chat-input");
            // LOBBY-02: Enter sends; there is no Send button.
            Input.textEdition.placeholder = "Write to everyone on Open games and press Enter";
            // Caught on the way down, before the field's own text input can take the key.
            Input.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter))
                    return;
                Submit();
                e.StopPropagation();
            }, TrickleDown.TrickleDown);
            row.Add(Input);
            return row;
        }

        // LOBBY-02: the grey strip at the right edge with the tab at its bottom; the tab folds the column beside it.
        private VisualElement Strip()
        {
            var strip = new VisualElement { name = "lobby-online-strip" };
            strip.AddToClassList("lobby-online-strip");
            tab = BoardScreen.Button("", ToggleOnline);
            tab.name = "lobby-online-tab";
            tab.AddToClassList("lobby-online-tab");
            var label = new Label("Online") { name = "lobby-online-tab-label", pickingMode = PickingMode.Ignore };
            label.AddToClassList("lobby-online-tab-label");
            tab.Add(label);
            strip.Add(tab);
            return strip;
        }

        private void BuildContextTab()
        {
            ContextTab.AddToClassList("lobby-context");
            contextName.AddToClassList("lobby-context-name");
            ContextTab.Add(contextName);
            var challenge = BoardScreen.Button("Send challenge", SendChallenge);
            challenge.name = "lobby-challenge-send";
            challenge.AddToClassList("lobby-context-action");
            ContextTab.Add(challenge);
            ContextTab.style.display = DisplayStyle.None;
            Root.Add(ContextTab);
        }

        public void Submit()
        {
            var text = Input.value?.Trim() ?? "";
            if (text.Length == 0)
                return;
            if (text.Length > LobbyLimits.LongestMessage)
            {
                Status.text = LobbyState.Refusal("InvalidMessage", "");
                return;
            }
            Input.value = "";
            Send?.Invoke(text);
        }

        public void Apply(LobbyState state)
        {
            AppendMessages(state.Messages);
            ShowOnline(state.Online);
        }

        private void AppendMessages(IReadOnlyList<LobbyMessage> messages)
        {
            var added = messages.Where(message => message.Sequence > shownLatest).ToList();
            if (added.Count == 0)
                return;
            var atBottom = log.verticalScroller.value >= log.verticalScroller.highValue - 4;
            foreach (var message in added)
                log.Add(MessageRow(message));
            while (log.contentContainer.childCount > LobbyLimits.MessagesKept)
                log.contentContainer.RemoveAt(0);
            shownLatest = added[added.Count - 1].Sequence;
            if (atBottom)
                log.schedule.Execute(() => log.scrollOffset = new Vector2(0, float.MaxValue));
        }

        private VisualElement MessageRow(LobbyMessage message)
        {
            var row = new VisualElement { name = "lobby-message-" + message.Sequence };
            row.AddToClassList("lobby-message");
            var author = new Label(message.Author) { name = "lobby-author" };
            author.AddToClassList("lobby-author");
            OfferChallenge(author, message.Author);
            row.Add(author);
            var text = new Label(message.Text) { pickingMode = PickingMode.Ignore };
            text.AddToClassList("lobby-text");
            row.Add(text);
            return row;
        }

        private void ShowOnline(IReadOnlyList<OnlinePlayer> players)
        {
            var key = string.Join("|", players.Select(p => p.Name + (p.You ? "*" : "")));
            if (key != onlineKey)
            {
                onlineKey = key;
                onlineList.Clear();
                foreach (var player in players)
                    onlineList.Add(PlayerRow(player));
            }
            RenderOnlineCount(players.Count);
        }

        private VisualElement PlayerRow(OnlinePlayer player)
        {
            var name = new Label(player.You ? player.Name + " (you)" : player.Name) { name = "lobby-player-" + player.Name };
            name.AddToClassList("lobby-player");
            name.EnableInClassList("lobby-player-you", player.You);
            name.tooltip = player.You ? "You" : "Right-click to challenge " + player.Name;
            if (!player.You)
                OfferChallenge(name, player.Name);
            return name;
        }

        private void OfferChallenge(VisualElement element, string name)
        {
            element.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 1)
                    return;
                OpenContext(name, Root.WorldToLocal(e.position));
                e.StopPropagation();
            });
        }

        // The tab opens beside the pointer and stays inside the panel.
        public void OpenContext(string name, Vector2 local)
        {
            contextTarget = name;
            contextName.text = name;
            ContextTab.style.display = DisplayStyle.Flex;
            var width = Mathf.Max(Root.layout.width, 1);
            var height = Mathf.Max(Root.layout.height, 1);
            // The tab is 170 points wide and about 80 high (ContextWidth, ContextHeight); it never leaves the panel.
            ContextTab.style.left = Mathf.Clamp(local.x, 0, Mathf.Max(0, width - ContextWidth - 2));
            ContextTab.style.top = Mathf.Clamp(local.y, 0, Mathf.Max(0, height - ContextHeight - 2));
            ContextTab.BringToFront();
        }

        public void CloseContext()
        {
            ContextTab.style.display = DisplayStyle.None;
            contextTarget = "";
        }

        public string ContextTarget => contextTarget;

        private void CloseContextOutside(PointerDownEvent e)
        {
            if (ContextTab.style.display.value == DisplayStyle.None)
                return;
            var target = e.target as VisualElement;
            if (target != null && (target == ContextTab || ContextTab.Contains(target)))
                return;
            CloseContext();
        }

        private void SendChallenge()
        {
            var name = contextTarget;
            CloseContext();
            if (name.Length > 0)
                Challenge?.Invoke(name);
        }

        public void ToggleOnline()
        {
            OnlineShown = !OnlineShown;
            ShowOnlineColumn();
            RenderOnlineCount(onlineCount);
        }

        private void ShowOnlineColumn()
        {
            online.style.display = OnlineShown ? DisplayStyle.Flex : DisplayStyle.None;
            tab.EnableInClassList("lobby-online-tab-folded", !OnlineShown);
            if (OnlineShown)
                online.BringToFront();
        }

        private int onlineCount;

        private void RenderOnlineCount(int count)
        {
            onlineCount = count;
            onlineTitle.text = "Online · " + count;
            tab.tooltip = OnlineShown ? "Hide the players online" : $"Show the players online ({count})";
        }
    }
}
