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
    // Owner (2026-11-11): the pennants were easy to miss over the map and a new one came unnoticed. They stand larger on
    // the dark plate of the strip's other icons, the seal counts every unread notice (one included), and a pennant that
    // arrives while the game is open is signalled: it drops in from the top bar, a gold halo pulses three times in the
    // shared 750 ms rhythm and the seal swells once. Reduced motion keeps everything still: the halo shows steadily for
    // the same time. A history's first load, a reload and a read never signal; the pennants stay quiet (AUDIO_UX).
    public sealed class ActionNoticeStack
    {
        public const string ClassName = "notice-stack";
        public const string ArrivingClass = "notice-arriving";
        // Three pulses of the shared rhythm (MapTargetGlow.PulseMilliseconds).
        public const long ArrivalMilliseconds = 3 * MapTargetGlow.PulseMilliseconds;
        // At most two pennant edges show behind the newest one; the seal tells the full count.
        private const int VisibleLayers = 2;
        public Button Root { get; }
        public int Count { get; private set; }
        // How many arrivals were signalled, and whether one is showing now.
        public int Arrivals { get; private set; }
        public bool Arriving => Root.ClassListContains(ArrivingClass);
        // Whether the last arrival moved (the drop, the swell and the pulse) or stood still (reduced motion).
        public bool Moving { get; private set; }
        public VisualElement Halo { get; } = new VisualElement { name = "notice-halo", pickingMode = PickingMode.Ignore };
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
        // Every notice key this stack has shown: an unread key outside it is an arrival.
        private readonly HashSet<string> seen = new HashSet<string>();
        private IVisualElementScheduledItem pulse;
        private IVisualElementScheduledItem settle;

        public ActionNoticeStack(BoardScreen board, Action opened)
        {
            this.board = board;
            this.opened = opened;
            Root = BoardScreen.Button("", Open);
            Root.AddToClassList(ClassName);
            // Older callers and tests find the newest notice by this class and its name.
            Root.AddToClassList("action-notice");
            Halo.AddToClassList("notice-halo");
            Root.Add(Halo);
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

        // live: the notices came with the game running (a newer page of the feed), not from the history's first load.
        public void Apply(IReadOnlyList<ActionNoticeGroup> next, bool live = false)
        {
            groups = next;
            read.IntersectWith(next.Select(Key));
            var arrived = live && next.Any(group => !read.Contains(Key(group)) && !seen.Contains(Key(group)));
            seen.UnionWith(next.Select(Key));
            Render();
            if (arrived && front != null)
                Arrive();
        }

        private void Arrive()
        {
            Arrivals++;
            pulse?.Pause();
            settle?.Pause();
            Root.RemoveFromClassList("notice-halo-dim");
            Root.AddToClassList(ArrivingClass);
            Moving = !board.ReducedMotion();
            if (Moving)
            {
                // The drop and the swell start from their first position in one drawn frame.
                Root.AddToClassList("notice-drop-start");
                Root.schedule.Execute(() => Root.RemoveFromClassList("notice-drop-start"));
                pulse = Root.schedule.Execute(() => Root.ToggleInClassList("notice-halo-dim"))
                    .StartingIn(MapTargetGlow.PulseMilliseconds / 2).Every(MapTargetGlow.PulseMilliseconds);
            }
            settle = Root.schedule.Execute(Settle).StartingIn(ArrivalMilliseconds);
        }

        private void Settle()
        {
            pulse?.Pause();
            settle?.Pause();
            Root.RemoveFromClassList(ArrivingClass);
            Root.RemoveFromClassList("notice-halo-dim");
            Root.RemoveFromClassList("notice-drop-start");
        }

        private void Render()
        {
            var unread = groups.Where(group => !read.Contains(Key(group))).ToList();
            Count = unread.Count;
            front = unread.FirstOrDefault();
            Root.style.display = front == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (front == null)
            {
                Settle();
                return;
            }
            Root.name = "action-notice-" + front.Latest.Id;
            Root.tooltip = Tooltip();
            for (var i = 0; i < VisibleLayers; i++)
                layers[i].style.display = Count > i + 1 ? DisplayStyle.Flex : DisplayStyle.None;
            seal.style.display = DisplayStyle.Flex;
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
            // Opening a notice is seeing it: the arrival's signal ends.
            Settle();
            opened();
            board.ActionHistory.Open(group.Latest.Request);
            Render();
        }
    }
}
