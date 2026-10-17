using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-99/UI-100: unread opponent actions as small pennants stacked newest in front. A click opens the newest
    // pennant's action card and that pennant is gone at once, so repeated clicks read ever older notices.
    // The pennant carries no text; its tooltip names the newest action. The action card is shared,
    // so opening a notice replaces the card of the previous one.
    // Reading stays local: the card acknowledges its steps (NotificationReadStore); nothing is sent.
    public sealed class ActionNoticeStack
    {
        public const string ClassName = "notice-stack";
        // At most two pennant edges show behind the newest one; the seal tells the full count.
        private const int VisibleLayers = 2;
        public Button Root { get; }
        public int Count { get; private set; }
        private readonly BoardScreen board;
        private readonly Action opened;
        private readonly Label count = new Label { name = "notice-count", pickingMode = PickingMode.Ignore };
        private readonly VisualElement seal = new VisualElement { name = "notice-seal", pickingMode = PickingMode.Ignore };
        private readonly VisualElement[] layers = new VisualElement[VisibleLayers];
        // Pennants opened in this session vanish before the card acknowledges their steps. A step that
        // arrives later in the same action is a new pennant, so the key includes the newest step.
        private readonly HashSet<string> read = new HashSet<string>();
        private IReadOnlyList<ActionNoticeGroup> groups = Array.Empty<ActionNoticeGroup>();
        private ActionNoticeGroup front;
        private ActionNoticeGroup pressed;

        public ActionNoticeStack(BoardScreen board, Action opened)
        {
            this.board = board;
            this.opened = opened;
            Root = BoardScreen.Button("", Open);
            Root.AddToClassList(ClassName);
            // Older callers and tests find the newest notice by this class and its name.
            Root.AddToClassList("action-notice");
            for (var i = VisibleLayers - 1; i >= 0; i--)
            {
                layers[i] = new VisualElement { name = "notice-layer-" + (i + 1), pickingMode = PickingMode.Ignore };
                layers[i].AddToClassList("notice-layer");
                layers[i].AddToClassList("notice-layer-" + (i + 1));
                Root.Add(layers[i]);
            }
            var pennant = new VisualElement { name = "notice-pennant", pickingMode = PickingMode.Ignore };
            pennant.AddToClassList("notice-pennant");
            Root.Add(pennant);
            seal.AddToClassList("notice-seal");
            seal.Add(count);
            Root.Add(seal);
            // The press decides the notice even if a newer one arrives before the click completes.
            Root.RegisterCallback<PointerDownEvent>(_ => pressed = front);
            Root.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode is KeyCode.Return or KeyCode.Space)
                    pressed = front;
            });
            Root.RegisterCallback<PointerCancelEvent>(_ => pressed = null);
            Root.style.display = DisplayStyle.None;
        }

        public void Apply(IReadOnlyList<ActionNoticeGroup> next)
        {
            groups = next;
            read.IntersectWith(next.Select(Key));
            Render();
        }

        private void Render()
        {
            var unread = groups.Where(group => !read.Contains(Key(group))).ToList();
            Count = unread.Count;
            front = unread.FirstOrDefault();
            Root.style.display = front == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (front == null)
                return;
            Root.name = "action-notice-" + front.Latest.Id;
            Root.tooltip = Tooltip();
            for (var i = 0; i < VisibleLayers; i++)
                layers[i].style.display = Count > i + 1 ? DisplayStyle.Flex : DisplayStyle.None;
            seal.style.display = Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            count.text = Count.ToString();
            var opponent = board.View?.You == Side.Shadow ? Side.FreePeoples : Side.Shadow;
            Root.EnableInClassList("notice-free", opponent == Side.FreePeoples);
            Root.EnableInClassList("notice-shadow", opponent == Side.Shadow);
        }

        private static string Key(ActionNoticeGroup group) => group.Latest.Id + "|" + group.Latest.StepId;

        // UI-166: the result of the newest action — its heading and first line; the card tells the rest.
        private string Tooltip() => front.Result.Tooltip;

        private void Open()
        {
            var group = pressed ?? front;
            pressed = null;
            if (group == null)
                return;
            read.Add(Key(group));
            opened();
            board.ActionHistory.Open(group.Latest.Request);
            Render();
        }
    }
}
