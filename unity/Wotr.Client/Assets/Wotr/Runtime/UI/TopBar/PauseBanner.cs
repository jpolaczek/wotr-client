using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-500 (R35, docs/ui/RANKED_UX.md): a paused ranked game. A banner names who paused, with Resume for both players. One
    // veil over everything below the top bar dims the board and takes its clicks, whichever panel or column holds them; the
    // top bar stays free, so conceding is still possible. The chat works: while paused it stands above the veil, in a host
    // with the stage's rectangle, so it does not move.
    public sealed class PauseBanner
    {
        public VisualElement Layer { get; } = new VisualElement { name = "pause-layer", pickingMode = PickingMode.Ignore };
        public VisualElement Veil { get; } = new VisualElement { name = "pause-veil" };
        public Label Text { get; } = new Label { name = "pause-banner-text", pickingMode = PickingMode.Ignore };
        public Button Resume { get; }
        public bool Shown { get; private set; }
        private readonly VisualElement host = new VisualElement { name = "pause-host", pickingMode = PickingMode.Ignore };
        private readonly GameChatPanel chat;
        private readonly VisualElement stage;
        private int chatIndex;
        private int chatIconIndex;

        public PauseBanner(GameChatPanel chat, VisualElement stage, Action resume)
        {
            this.chat = chat;
            this.stage = stage;
            Resume = BoardScreen.Button("Resume", resume);
            Resume.name = "pause-resume";
            Resume.AddToClassList("pause-resume");
            Resume.tooltip = "Resume the game: the clock goes on from the time it stood at.";
            Layer.AddToClassList("pause-layer");
            Veil.AddToClassList("pause-veil");
            host.AddToClassList("pause-host");
            host.Add(BannerRow());
            Layer.Add(Veil);
            Layer.Add(host);
            Layer.style.display = DisplayStyle.None;
            stage.RegisterCallback<GeometryChangedEvent>(_ => FollowStage());
        }

        // UI-504: Resume cannot be sent without a connection, and says so.
        public void ShowConnected(bool connected)
        {
            Resume.SetEnabled(connected);
            Resume.tooltip = connected ? "Resume the game: the clock goes on from the time it stood at." : "The connection is lost: Resume cannot be sent now.";
        }

        public void Show(bool paused, string text)
        {
            if (paused)
                Text.text = text;
            if (paused == Shown)
                return;
            Shown = paused;
            Layer.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
            if (paused)
                RaiseTheChat();
            else
                ReturnTheChat();
        }

        private VisualElement BannerRow()
        {
            var row = new VisualElement { name = "pause-banner-row", pickingMode = PickingMode.Ignore };
            row.AddToClassList("pause-banner-row");
            var banner = new VisualElement { name = "pause-banner" };
            banner.AddToClassList("pause-banner");
            banner.tooltip = "A pause ends by itself after 10 minutes.";
            Text.AddToClassList("pause-banner-text");
            banner.Add(Text);
            banner.Add(Resume);
            row.Add(banner);
            return row;
        }

        private void RaiseTheChat()
        {
            chatIndex = stage.IndexOf(chat.Root);
            chatIconIndex = stage.IndexOf(chat.Icon);
            FollowStage();
            host.Insert(0, chat.Root);
            host.Insert(1, chat.Icon);
        }

        private void ReturnTheChat()
        {
            stage.Insert(chatIndex, chat.Root);
            stage.Insert(chatIconIndex, chat.Icon);
        }

        private void FollowStage()
        {
            if (!Shown || Layer.parent == null)
                return;
            var bounds = stage.worldBound;
            var corner = Layer.parent.WorldToLocal(bounds.position);
            host.style.left = corner.x;
            host.style.top = corner.y;
            host.style.width = stage.layout.width;
            host.style.height = stage.layout.height;
            Veil.style.top = corner.y;
        }
    }
}
