using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-436 (owner: "the window opens in a different place for each ruler; always show it beside the rulers' tab, in
    // the same place"): every ruler's window, and every character's window opened from the Characters tab, its panel or a
    // ruler, stands at one place: just right of where the Characters panel unrolls (its Slot, laid out even while the
    // panel is rolled up, so the panel never covers the window and the window never moves when the panel opens or
    // closes), its top level with the Characters tab, pulled up only as far as it must to stay whole above the bottom of
    // the board. A window the player dragged keeps its own place (FloatingWindow.Restore).
    public sealed class CharacterWindowPlace
    {
        public const float Gap = 6;
        public const float BottomMargin = 8;
        public const string SlotClass = "characters-panel-slot";
        private readonly VisualElement slot;
        private readonly VisualElement tab;

        public CharacterWindowPlace(VisualElement slot, VisualElement tab)
        {
            this.slot = slot;
            this.tab = tab;
        }

        // The Characters panel's size and place without its content: the same classes, nothing shown, nothing picked.
        public static VisualElement Slot()
        {
            var slot = new VisualElement { name = "characters-panel-slot", pickingMode = PickingMode.Ignore };
            slot.AddToClassList("characters-panel");
            slot.AddToClassList("edge-left-panel");
            slot.AddToClassList(SlotClass);
            return slot;
        }

        public bool Ready => slot.panel != null && tab.panel != null && slot.worldBound.width > 0 && tab.worldBound.height > 0;

        // Writes the window's place; false while the slot or the tab are not laid out yet.
        public bool Apply(VisualElement window, Vector2 fallbackSize)
        {
            var viewport = window.parent;
            if (viewport == null || !Ready || viewport.contentRect.width <= 0)
                return false;
            var size = window.layout.width > 0 && !float.IsNaN(window.layout.width) ? window.layout.size : fallbackSize;
            var point = Point(viewport.contentRect, Local(viewport, slot.worldBound), Local(viewport, tab.worldBound), size);
            if (window.style.left.keyword != StyleKeyword.Undefined || Mathf.Abs(window.style.left.value.value - point.x) > .25f)
                window.style.left = point.x;
            if (window.style.top.keyword != StyleKeyword.Undefined || Mathf.Abs(window.style.top.value.value - point.y) > .25f)
                window.style.top = point.y;
            return true;
        }

        // The top-left of a window of this size: beside the slot, level with the tab, whole inside the viewport.
        public static Vector2 Point(Rect viewport, Rect slot, Rect tab, Vector2 size)
        {
            var left = Mathf.Min(slot.xMax + Gap, Mathf.Max(viewport.xMin, viewport.xMax - size.x - Gap));
            var lowest = viewport.yMax - size.y - BottomMargin;
            var top = Mathf.Max(viewport.yMin, Mathf.Min(tab.yMin, lowest));
            return new Vector2(Mathf.Round(left), Mathf.Round(top));
        }

        private static Rect Local(VisualElement viewport, Rect world) =>
            new Rect(viewport.WorldToLocal(world.position), world.size);
    }
}
