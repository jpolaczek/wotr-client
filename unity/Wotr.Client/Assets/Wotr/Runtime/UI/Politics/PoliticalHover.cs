using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    internal sealed class PoliticalHover
    {
        public bool Visible { get; private set; }
        public bool Pinned => Visible;
        public event Action Changed;

        public PoliticalHover(VisualElement surface, VisualElement drawer)
        {
            surface.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape)
                    Close();
            }, TrickleDown.TrickleDown);
            drawer.RegisterCallback<DetachFromPanelEvent>(e =>
            {
                if (e.target == drawer)
                    Close();
            });
        }

        public void Open()
        {
            Visible = true;
            Changed?.Invoke();
        }

        public void Close()
        {
            Visible = false;
            Changed?.Invoke();
        }
    }
}
