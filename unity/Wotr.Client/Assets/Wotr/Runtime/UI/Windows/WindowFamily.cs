using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-89: a window opened from inside another board window closes together with that window.
    // The owner is the nearest registered window that contains the control used to open the child.
    public sealed class WindowFamily
    {
        private readonly VisualElement host;
        private readonly Func<IEnumerable<VisualElement>> windows;
        private readonly List<Link> links = new List<Link>();
        private readonly IVisualElementScheduledItem watch;

        public WindowFamily(VisualElement host, Func<IEnumerable<VisualElement>> windows)
        {
            this.host = host;
            this.windows = windows;
            // Runs once per frame only while some child has an owner; idle otherwise.
            watch = host.schedule.Execute(CloseOrphans).Every(0);
            watch.Pause();
        }

        public bool Watching => links.Count > 0;

        public void Opened(VisualElement child, VisualElement origin, Action close)
        {
            // Navigation inside the child keeps its current owner.
            if (origin != null && (origin == child || child.Contains(origin)))
                return;
            Forget(child);
            var owner = Owner(child, origin);
            if (owner == null)
                return;
            links.Add(new Link { Child = child, Owner = owner, Close = close });
            watch.Resume();
        }

        public VisualElement OwnerOf(VisualElement child) => links.FirstOrDefault(link => link.Child == child)?.Owner;

        // UI-98: an unrolled edge panel stays while a window opened from inside it is shown.
        public bool HasOpenChild(VisualElement owner) => links.Any(link => link.Owner == owner && Shown(link.Child));

        public void Forget(VisualElement child)
        {
            links.RemoveAll(link => link.Child == child);
            if (links.Count == 0)
                watch.Pause();
        }

        // A closed child may itself own a window, so repeat until nothing else closes.
        public void CloseOrphans()
        {
            var closed = true;
            while (closed)
            {
                closed = false;
                foreach (var link in links.ToList())
                {
                    if (Shown(link.Owner) && Shown(link.Child))
                        continue;
                    links.Remove(link);
                    if (!Shown(link.Child))
                        continue;
                    link.Close();
                    closed = true;
                }
            }
            if (links.Count == 0)
                watch.Pause();
        }

        private VisualElement Owner(VisualElement child, VisualElement origin)
        {
            var roots = new HashSet<VisualElement>(windows());
            for (var element = origin; element != null; element = element.parent)
                if (element != child && roots.Contains(element))
                    return element;
            return null;
        }

        private bool Shown(VisualElement element)
        {
            // A window removed from the live board counts as closed; detached test trees have no panel at all.
            if (element.panel == null && host.panel != null)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (Hidden(current))
                    return false;
            return true;
        }

        // An inline display is current immediately; the resolved style lags until the next style pass. A window fading out
        // is already closed (UI-322).
        private static bool Hidden(VisualElement element)
        {
            if (element.ClassListContains(Appearance.ClosingClass))
                return true;
            var inline = element.style.display;
            return inline.keyword == StyleKeyword.Undefined
                ? inline.value == DisplayStyle.None
                : element.resolvedStyle.display == DisplayStyle.None;
        }

        private sealed class Link
        {
            public VisualElement Child;
            public VisualElement Owner;
            public Action Close;
        }
    }
}
