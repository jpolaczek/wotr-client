using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Where a plate stands by its target: inside its top right corner (a battle column has room there), under it, or
    // before it on the same line (the top bar's open middle).
    public enum PlateSide { Inside, Under, Before }

    // UI-311/UI-312: a beat's marks on HUD objects — the objects' own glow (UI-314) and small plates — placed once the
    // objects are laid out (content a beat has just staged has no size before the next layout pass). A newer placement or
    // the beat's end removes them; none takes the pointer.
    public sealed class PresentationMarks
    {
        private readonly PresentationLayer layer;
        private readonly string plateName;
        private readonly List<VisualElement> plates = new List<VisualElement>();
        private readonly List<ObjectGlow> glows = new List<ObjectGlow>();
        private int version;

        public PresentationMarks(PresentationLayer layer, string plateName)
        {
            this.layer = layer;
            this.plateName = plateName;
        }

        public IEnumerable<VisualElement> Lit => glows.Select(glow => glow.Target);

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

        // UI-315: more marks beside the current ones (a plate joining a lit object), placed after the next layout pass.
        public void Add(Action place)
        {
            var current = version;
            layer.Root.schedule.Execute(() =>
            {
                if (current == version)
                    place();
            });
        }

        public void Light(VisualElement target)
        {
            if (!PresentationTargets.Shown(target))
                return;
            var glow = new ObjectGlow();
            glow.Follow(target);
            glows.Add(glow);
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
            Appearance.Arrive(plate);
            plates.Add(plate);
        }

        public void End()
        {
            version++;
            foreach (var plate in plates)
                plate.RemoveFromHierarchy();
            plates.Clear();
            foreach (var glow in glows)
                glow.End();
            glows.Clear();
        }
    }
}
