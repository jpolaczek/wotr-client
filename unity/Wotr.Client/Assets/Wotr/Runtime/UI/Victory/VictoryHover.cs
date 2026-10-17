using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    internal sealed class VictoryHover
    {
        private readonly DrawerHover drawer;
        public bool Pinned => drawer.Pinned;
        public bool Visible => drawer.Visible;

        public VictoryHover(VisualElement surface, VisualElement tab, VisualElement panel, Action changed)
        {
            drawer = new DrawerHover(surface, tab, panel, _ => false, changed);
        }

        public void Open() => drawer.Open();
        public void Close() => drawer.Close();
        public void CloseTransient() => drawer.CloseTransient();
    }
}
