using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-401 (TABLETOP_FIGURES_UX, "Scale"): one object standing on the table — an army, the Fellowship, a planned ghost
    // or a corrupted king's marker. Its element keeps its design size inside a holder of a fixed size; the holder carries
    // the only per-frame writes, a translate and a scale (a dynamic transform), so a camera change never lays the map
    // out again. The element keeps its own style transforms (a hover or pressed scale).
    public sealed class MapStand
    {
        public VisualElement Root { get; }
        public VisualElement Content { get; }
        // The board point under its feet; the stand layer is sorted by it.
        public Vector2 Feet { get; set; }
        // Where its visible part stands, map-local, as last placed.
        public Rect Bounds { get; private set; }
        private Vector2 pivot;
        private Rect visible;
        private Vector2 shownTranslate = new Vector2(float.NaN, float.NaN);
        private float shownScale = float.NaN;

        public MapStand(VisualElement content, Vector2 room)
        {
            Content = content;
            Root = new VisualElement
            {
                name = content.name + "-stand",
                pickingMode = PickingMode.Ignore,
                usageHints = UsageHints.DynamicTransform,
                userData = this
            };
            Root.AddToClassList("map-stand");
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.top = 0;
            Root.style.width = room.x;
            Root.style.height = room.y;
            Root.style.transformOrigin = new TransformOrigin(0, 0, 0);
            content.style.left = 0;
            content.style.top = 0;
            Root.Add(content);
        }

        // The point under its feet and its visible part, in the element's own design points.
        public void Design(Vector2 feet, Rect visiblePart)
        {
            pivot = feet;
            visible = visiblePart;
        }

        // Stands its feet on a map-local point at a scale; the transform is written only when it changes.
        public void Place(Vector2 point, float scale)
        {
            var translate = point - pivot * scale;
            Bounds = new Rect(translate + visible.position * scale, visible.size * scale);
            if (translate != shownTranslate)
            {
                shownTranslate = translate;
                Root.style.translate = new Translate(translate.x, translate.y);
            }
            if (scale != shownScale)
            {
                shownScale = scale;
                Root.style.scale = new Scale(new Vector2(scale, scale));
            }
        }

        // Takes the stand off the map; its element leaves the panel with it.
        public void Remove()
        {
            Content.RemoveFromHierarchy();
            Root.RemoveFromHierarchy();
        }

        // Nearer stands (lower on the board) come later, so they are drawn in front.
        public static int CompareDepth(VisualElement a, VisualElement b)
        {
            var first = (a.userData as MapStand)?.Feet ?? Vector2.zero;
            var second = (b.userData as MapStand)?.Feet ?? Vector2.zero;
            var depth = first.y.CompareTo(second.y);
            return depth != 0 ? depth : first.x.CompareTo(second.x);
        }
    }
}
