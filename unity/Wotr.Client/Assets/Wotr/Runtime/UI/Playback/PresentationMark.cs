using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-310: a beat's mark on one HUD object, placed once the object is laid out — a row or a panel the beat has just
    // shown has no size before the next layout pass. A newer mark or the beat's end replaces it. UI-314: the mark is the
    // object's own glow.
    public sealed class PresentationMark
    {
        private readonly PresentationLayer layer;
        private readonly ObjectGlow glow = new ObjectGlow();
        private int version;

        public PresentationMark(PresentationLayer layer)
        {
            this.layer = layer;
        }

        public VisualElement Target => glow.Target;

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
            glow.End();
        }

        private void Place(VisualElement target)
        {
            if (PresentationTargets.Shown(target))
                glow.Follow(target);
        }
    }
}
