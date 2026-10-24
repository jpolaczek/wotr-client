using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-310: a beat's observed ring around one HUD object, placed once the object is laid out — a row or a panel the
    // beat has just shown has no size before the next layout pass. A newer mark or the beat's end replaces it.
    public sealed class PresentationMark
    {
        private readonly PresentationLayer layer;
        private readonly string name;
        private LayerRing ring;
        private int version;

        public PresentationMark(PresentationLayer layer, string name)
        {
            this.layer = layer;
            this.name = name;
        }

        public void Show(Func<VisualElement> target)
        {
            End();
            var current = version;
            layer.Root.schedule.Execute(() =>
            {
                if (current == version)
                    Place(target());
            });
        }

        public void End()
        {
            version++;
            ring?.End();
            ring = null;
        }

        private void Place(VisualElement target)
        {
            if (!PresentationTargets.Shown(target))
                return;
            ring = new LayerRing(layer, name);
            ring.Follow(target, round: false);
        }
    }
}
