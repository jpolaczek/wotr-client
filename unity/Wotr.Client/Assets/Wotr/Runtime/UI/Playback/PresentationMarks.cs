using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Where a plate stands by its target: inside its top right corner (a battle column has room there), under it, or
    // before it on the same line (the top bar's open middle).
    public enum PlateSide { Inside, Under, Before }

    // UI-311/UI-312: a beat's marks over HUD objects — observed rings and small plates — placed once the objects are laid
    // out (content a beat has just staged has no size before the next layout pass). A newer placement or the beat's end
    // removes them; none takes the pointer.
    public sealed class PresentationMarks
    {
        private readonly PresentationLayer layer;
        private readonly string ringName;
        private readonly string plateName;
        private readonly List<VisualElement> marks = new List<VisualElement>();
        private int version;

        public PresentationMarks(PresentationLayer layer, string ringName, string plateName)
        {
            this.layer = layer;
            this.ringName = ringName;
            this.plateName = plateName;
        }

        public void Place(Action place)
        {
            End();
            var current = version;
            layer.Root.schedule.Execute(() =>
            {
                if (current == version)
                    place();
            });
        }

        public void Ring(VisualElement target, bool round = true)
        {
            if (!PresentationTargets.Shown(target))
                return;
            var ring = new LayerRing(layer, ringName);
            ring.Follow(target, round);
            marks.Add(ring.Root);
        }

        public void Plate(VisualElement target, VisualElement plate, PlateSide side = PlateSide.Inside)
        {
            if (!PresentationTargets.Shown(target))
                return;
            plate.name = plateName;
            plate.pickingMode = PickingMode.Ignore;
            plate.style.position = Position.Absolute;
            var bounds = layer.Local(target.worldBound);
            switch (side)
            {
                case PlateSide.Under:
                    plate.style.left = bounds.xMin;
                    plate.style.top = bounds.yMax + 2;
                    break;
                case PlateSide.Before:
                    plate.style.right = layer.Root.contentRect.width - bounds.xMin + 6;
                    plate.style.top = bounds.yMin;
                    break;
                default:
                    plate.style.right = layer.Root.contentRect.width - bounds.xMax + 4;
                    plate.style.top = bounds.yMin;
                    break;
            }
            layer.Root.Add(plate);
            marks.Add(plate);
        }

        public void End()
        {
            version++;
            foreach (var mark in marks)
                mark.RemoveFromHierarchy();
            marks.Clear();
        }
    }
}
