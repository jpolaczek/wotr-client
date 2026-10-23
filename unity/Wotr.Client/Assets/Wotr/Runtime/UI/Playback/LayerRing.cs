using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-307/UI-308: the observed ring around a HUD object (a political token, the Effects tab or a table card's tile), in
    // the presentation layer above the HUD. It follows its object while the beat lasts and ignores the pointer.
    public sealed class LayerRing
    {
        private const float Gap = 3;
        private readonly PresentationLayer layer;
        public VisualElement Root { get; }

        public LayerRing(PresentationLayer layer, string name)
        {
            this.layer = layer;
            Root = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            Root.AddToClassList("observed-ring");
            layer.Root.Add(Root);
        }

        public void Follow(VisualElement target, bool round = true)
        {
            var bounds = layer.Local(target.worldBound);
            var radius = round ? Mathf.Min(bounds.width, bounds.height) / 2 + Gap : 4;
            Root.style.left = bounds.x - Gap;
            Root.style.top = bounds.y - Gap;
            Root.style.width = bounds.width + 2 * Gap;
            Root.style.height = bounds.height + 2 * Gap;
            Root.style.borderTopLeftRadius = Root.style.borderTopRightRadius = radius;
            Root.style.borderBottomLeftRadius = Root.style.borderBottomRightRadius = radius;
        }

        public void End() => Root.RemoveFromHierarchy();
    }
}
