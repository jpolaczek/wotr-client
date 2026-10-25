using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-337: a committed casualty's hit on its figure stack in the battle window. A small red ring bursts over the stack
    // while the stack flashes red; a stack losing its last figure fades out meanwhile. Under Reduced motion the flash and
    // the fade happen without movement. The hit lands once the window's content is laid out (content just staged has no
    // size yet). Nothing takes the pointer; Clear removes every mark, a pending one too.
    public sealed class BattleHits
    {
        public const string HitClass = "presented-hit";
        public const string LeavingClass = "presented-leaving";
        public const string BurstName = "presentation-battle-hit";
        public const string BurstClass = "presentation-battle-hit";
        public const string BurstingClass = "presentation-battle-hit-burst";
        private const float BurstSize = 44;
        private readonly PresentationLayer layer;
        private readonly List<VisualElement> stacks = new List<VisualElement>();
        private VisualElement burst;
        private int version;

        public BattleHits(PresentationLayer layer)
        {
            this.layer = layer;
        }

        public void Strike(Func<VisualElement> stack, bool leaving)
        {
            Clear();
            var current = version;
            layer.Root.schedule.Execute(() =>
            {
                if (current == version)
                    Land(stack(), leaving);
            });
        }

        public void Clear()
        {
            version++;
            burst?.RemoveFromHierarchy();
            burst = null;
            foreach (var stack in stacks)
            {
                stack.RemoveFromClassList(HitClass);
                stack.RemoveFromClassList(LeavingClass);
            }
            stacks.Clear();
        }

        private void Land(VisualElement stack, bool leaving)
        {
            if (!PresentationTargets.Shown(stack))
                return;
            stacks.Add(stack);
            stack.AddToClassList(HitClass);
            stack.EnableInClassList(LeavingClass, leaving);
            burst = Burst(layer.Local(stack.worldBound).center);
            layer.Root.Add(burst);
            // The ring bursts from the next frame, so its transition starts from the struck state.
            var shown = burst;
            shown.schedule.Execute(() => shown.AddToClassList(BurstingClass));
        }

        private static VisualElement Burst(UnityEngine.Vector2 centre)
        {
            var ring = new VisualElement { name = BurstName, pickingMode = PickingMode.Ignore };
            ring.AddToClassList(BurstClass);
            ring.style.position = Position.Absolute;
            ring.style.width = BurstSize;
            ring.style.height = BurstSize;
            ring.style.left = centre.x - BurstSize / 2;
            ring.style.top = centre.y - BurstSize / 2;
            return ring;
        }
    }
}
