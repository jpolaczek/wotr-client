using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Retained name for callers; visibility is exclusively explicit activation.
    internal sealed class DrawerHover
    {
        private readonly Action changed;
        public bool Pinned { get; private set; }
        public bool Visible => Pinned;

        public DrawerHover(VisualElement surface, VisualElement tab, VisualElement drawer,
            Func<VisualElement, bool> related, Action changed)
        {
            this.changed = changed;
            drawer.RegisterCallback<DetachFromPanelEvent>(e =>
            {
                if (e.target == drawer)
                    Close();
            });
        }

        public void Open()
        {
            Pinned = true;
            changed();
        }

        public void Close()
        {
            Pinned = false;
            changed();
        }

        public void CloseTransient() { }
    }
}
