using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-92: hovering or focusing an edge tab unrolls its panel, like a hand card. UI-98: the panel is
    // interactive at once and nothing is pinned by a click; it rolls back once the pointer has left the tab
    // and the panel and keyboard focus is elsewhere. The left rail and the top bar's Fellowship and Hunt
    // plaques form separate groups: a preview restores the panel of its own group that it replaced.
    // A panel that comes to host the current decision stays, as if its context had opened it.
    public sealed class LeftRailPreview
    {
        public const string PreviewClass = "edge-panel-preview";
        public const string EnteringClass = "edge-panel-entering";
        public const string LeftGroup = "left";
        // Grace between the tab and its panel, and the roll-back time that matches the USS transition.
        private const long LeaveDelay = 140;
        private const long RollBack = 130;
        private readonly List<Entry> entries = new List<Entry>();
        private Entry previewing;
        private Entry restore;
        private bool suspended;

        public string Previewing => previewing?.Key ?? "";

        // A pointer that crosses a tab while the map pans, or with a button held, is not hovering it.
        public Func<bool> Blocked { get; set; } = () => false;

        // A window opened from inside the panel (a card, a character) keeps it until that window closes.
        public Func<VisualElement, bool> Busy { get; set; } = _ => false;

        public void Add(string key, Button tab, VisualElement panel, Action open, Action close) =>
            Add(key, tab, panel, open, close, LeftGroup, null, null);

        // holds: the panel now hosts the current decision. kept: what keeping such a panel releases.
        public void Add(string key, Button tab, VisualElement panel, Action open, Action close,
            string group, Func<bool> holds, Action kept)
        {
            var entry = new Entry
            {
                Key = key, Group = group, Tab = tab, Panel = panel, Open = open, Close = close,
                Holds = holds ?? (() => false), Kept = kept
            };
            // A click or Enter only unrolls the panel, as hovering does.
            tab.clickable = new Clickable(() => Begin(entry));
            tab.RegisterCallback<PointerEnterEvent>(e =>
            {
                if (RememberPointer(entry, e.position) && !Blocked() && e.pressedButtons == 0)
                    Enter(entry, true);
            });
            tab.RegisterCallback<PointerMoveEvent>(e =>
            {
                var suppressed = entry.SuppressedPointer.HasValue;
                if (RememberPointer(entry, e.position) && suppressed && !Blocked() && e.pressedButtons == 0)
                    Enter(entry, true);
            });
            tab.RegisterCallback<PointerLeaveEvent>(e =>
            {
                RememberPointer(entry, e.position);
                Leave(entry, () => entry.OverTab = false);
            });
            panel.RegisterCallback<PointerEnterEvent>(e =>
            {
                // UI-362: a panel rolling back rises under a still pointer; that is its own motion, not a hover.
                if (entry.Closing && Still(entry, e.position))
                    return;
                if (RememberPointer(entry, e.position))
                    Enter(entry, false);
            });
            panel.RegisterCallback<PointerMoveEvent>(e =>
            {
                var moved = !Still(entry, e.position);
                if (RememberPointer(entry, e.position) && moved && entry.Closing && !entry.OverPanel)
                    Enter(entry, false);
            });
            panel.RegisterCallback<PointerLeaveEvent>(e =>
            {
                RememberPointer(entry, e.position);
                Leave(entry, () => entry.OverPanel = false);
            });
            ObserveFocus(entry, tab, true);
            ObserveFocus(entry, panel, false);
            entries.Add(entry);
        }

        // A control inside a tab (e.g. a plaque in the corner tab) unrolls the tab's panel as a click on the tab does.
        public void Show(string key)
        {
            var entry = entries.FirstOrDefault(item => item.Key == key);
            if (entry != null)
                Begin(entry);
        }

        // Every edge panel unrolls from its tab, whether it opens as a preview or by its context.
        public static void Unroll(VisualElement panel)
        {
            panel.AddToClassList(EnteringClass);
            panel.schedule.Execute(() => panel.RemoveFromClassList(EnteringClass)).ExecuteLater(16);
        }

        public void Suspend(bool dragging, VisualElement origin)
        {
            suspended = dragging;
            var entry = previewing;
            if (entry == null)
                return;
            if (!dragging)
            {
                entry.Panel.schedule.Execute(() => Check(entry)).ExecuteLater(LeaveDelay);
                return;
            }
            if (origin == null || origin != entry.Panel && !entry.Panel.Contains(origin))
            {
                Finish(entry);
                return;
            }
            // A dragged control must keep its own panel and drop targets alive, even
            // while pointer capture temporarily prevents hover events inside the panel.
            entry.Closing = false;
            entry.Panel.RemoveFromClassList(EnteringClass);
        }

        // Esc may restore focus to a plaque inside the corner tab as well as a left tab.
        public void SuppressRestoredFocus(VisualElement tab)
        {
            var entry = entries.FirstOrDefault(item => item.Tab == tab || tab != null && item.Tab.Contains(tab));
            if (entry == null)
                return;
            entry.Focused = false;
            entry.SuppressedFocus = tab;
            entry.SuppressedPointer = entry.Pointer;
        }

        // The pointer stands where it was last seen over the tab or the panel.
        private static bool Still(Entry entry, Vector2 position) => (position - entry.Pointer).sqrMagnitude < .01f;

        // Closing a panel can uncover its tab under a stationary pointer. That layout-generated
        // enter is not a fresh hover; a real move or leave makes the tab available again.
        private static bool RememberPointer(Entry entry, Vector2 position)
        {
            entry.Pointer = position;
            if (!entry.SuppressedPointer.HasValue)
                return true;
            if ((position - entry.SuppressedPointer.Value).sqrMagnitude < .01f)
                return false;
            entry.SuppressedPointer = null;
            return true;
        }

        // Focus from the keyboard holds the panel; the focus a click gives does not.
        private void ObserveFocus(Entry entry, VisualElement element, bool tab)
        {
            element.RegisterCallback<PointerDownEvent>(_ => entry.Pressing = true, TrickleDown.TrickleDown);
            element.RegisterCallback<PointerUpEvent>(_ => entry.Pressing = false);
            element.RegisterCallback<FocusInEvent>(e =>
            {
                var keyboard = !entry.Pressing;
                entry.Pressing = false;
                if (!keyboard || entry.SuppressedFocus != null && entry.SuppressedFocus == e.target)
                    return;
                entry.SuppressedFocus = null;
                entry.Focused = true;
                if (tab)
                    Begin(entry);
            });
            element.RegisterCallback<FocusOutEvent>(e =>
            {
                if (e.relatedTarget is VisualElement next && (next == entry.Tab || entry.Tab.Contains(next) || entry.Panel.Contains(next)))
                    return;
                Leave(entry, () =>
                {
                    entry.Focused = false;
                    entry.SuppressedFocus = null;
                });
            });
        }

        private void Enter(Entry entry, bool tab)
        {
            if (tab)
            {
                entry.OverTab = true;
                entry.SuppressedFocus = null;
            }
            else
                entry.OverPanel = true;
            if (tab || previewing == entry)
                Begin(entry);
        }

        private void Leave(Entry entry, Action update)
        {
            update();
            entry.Panel.schedule.Execute(() => Check(entry)).ExecuteLater(LeaveDelay);
        }

        private void Check(Entry entry)
        {
            if (suspended || previewing != entry || entry.OverTab || entry.OverPanel || entry.Focused)
                return;
            if (!Visible(entry.Panel))
                Forget(entry);
            else if (Busy(entry.Panel))
                entry.Panel.schedule.Execute(() => Check(entry)).ExecuteLater(LeaveDelay);
            else
                End(entry);
        }

        private void Begin(Entry entry)
        {
            if (suspended)
                return;
            if (previewing == entry)
            {
                if (Visible(entry.Panel))
                {
                    entry.Closing = false;
                    entry.Panel.RemoveFromClassList(EnteringClass);
                    return;
                }
                // Closed meanwhile by its × or by code: unroll it again.
                Forget(entry);
            }
            // A panel opened by its context or by code is not replaced by its own preview.
            if (Visible(entry.Panel))
                return;
            if (previewing != null)
            {
                if (previewing.Group == entry.Group)
                    Clear(previewing, true);
                else
                    Finish(previewing);
            }
            restore ??= entries.FirstOrDefault(other => other != entry && other.Group == entry.Group && Visible(other.Panel));
            previewing = entry;
            entry.Open();
            // The red lacquer of a selected tab marks a panel held open; a preview keeps the hover frame.
            entry.Tab.RemoveFromClassList("selected");
            entry.Panel.AddToClassList(PreviewClass);
        }

        private void End(Entry entry)
        {
            if (entry.Holds())
            {
                Keep(entry);
                return;
            }
            entry.Closing = true;
            if (ReducedMotion(entry.Panel))
            {
                Finish(entry);
                return;
            }
            entry.Panel.AddToClassList(EnteringClass);
            entry.Panel.schedule.Execute(() =>
            {
                if (previewing == entry && entry.Closing)
                    Finish(entry);
            }).ExecuteLater(RollBack);
        }

        // The decision now hosted by the panel keeps it open; the panel it replaced is forgotten.
        private void Keep(Entry entry)
        {
            Clear(entry, false);
            restore = null;
            entry.Kept?.Invoke();
            if (Visible(entry.Panel))
                entry.Tab.AddToClassList("selected");
        }

        // The panel was closed by its × or by code during the preview; nothing is restored for it.
        private void Forget(Entry entry)
        {
            Clear(entry, false);
            restore = null;
        }

        private void Finish(Entry entry)
        {
            Clear(entry, true);
            var previous = restore;
            restore = null;
            if (previous != null && !Visible(previous.Panel))
                previous.Open();
        }

        private void Clear(Entry entry, bool close)
        {
            entry.Closing = false;
            entry.Panel.RemoveFromClassList(PreviewClass);
            entry.Panel.RemoveFromClassList(EnteringClass);
            if (previewing == entry)
                previewing = null;
            if (close)
                entry.Close();
        }

        // UI-322: a panel fading out is already closed, so its tab unrolls it again.
        private static bool Visible(VisualElement panel) => !Appearance.Closed(panel) && panel.resolvedStyle.display != DisplayStyle.None;

        private static bool ReducedMotion(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.ClassListContains("reduced-motion"))
                    return true;
            return false;
        }

        private sealed class Entry
        {
            public string Key;
            public string Group;
            public Button Tab;
            public VisualElement Panel;
            public Action Open;
            public Action Close;
            public Func<bool> Holds;
            public Action Kept;
            public bool OverTab;
            public bool OverPanel;
            public bool Focused;
            public bool Pressing;
            public VisualElement SuppressedFocus;
            public Vector2 Pointer;
            public Vector2? SuppressedPointer;
            public bool Closing;
        }
    }
}
