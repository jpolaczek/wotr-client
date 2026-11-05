using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-403 (TABLETOP_FIGURES_UX, "Landing mark"): where the lifted figures would land while the pointer rests on a legal
    // target — a baked ring on the table (`landing-mark-landing-move` green for a move, `-attack` red for an attack, 112 ×
    // 36 points) on the prospective place, in a layer under the stands, so a figure standing near it is never covered — and
    // the caption of that click (ArmyGhost), hanging above the lifted figure; while the lifted figure is out of view the
    // caption hangs above the ring instead. It replaces UI-127's translucent army ghost: nothing is drawn faintly at the
    // target. Neither takes the pointer; per frame only their translate and scale change.
    internal sealed class MapLandingMark
    {
        public const string RingClass = "landing-mark";
        public const string MoveClass = "landing-move";
        public const string AttackClass = "landing-attack";
        // The ring's art drawn round an army's base at its design unit: its ellipse is as wide as the 54-point base.
        public static readonly Vector2 Size = new Vector2(58, 18.6f);
        // A character's base (14 units) beside an army's (40).
        public const float CharacterShare = .35f;
        // The caption's width in Styles/Armies.uss (.landing-caption) and its gap above what it hangs over.
        private const float CaptionWidth = 120;
        private const float CaptionHeight = 16;
        private const float CaptionGap = 4;
        // The ring lies round the base's footprint, a little below the middle of its top face.
        private static readonly Vector2 Pivot = new Vector2(Size.x / 2, Size.y / 2 - 1.5f);

        public VisualElement Layer { get; }
        public VisualElement Ring { get; }
        public Label Caption { get; }
        private readonly MapStand stand;
        private Vector2 captionAt = new Vector2(float.NaN, float.NaN);

        public MapLandingMark(MapCanvas map, VisualElement below, VisualElement above)
        {
            Layer = new VisualElement { name = "map-landing", pickingMode = PickingMode.Ignore };
            Layer.AddToClassList("map-landing");
            Layer.StretchToParentSize();
            map.Insert(map.IndexOf(below), Layer);
            Ring = new VisualElement { name = "landing-mark", pickingMode = PickingMode.Ignore };
            Ring.AddToClassList(RingClass);
            stand = new MapStand(Ring, Size);
            stand.Design(Pivot, new Rect(Vector2.zero, Size));
            Layer.Add(stand.Root);
            Caption = new Label { name = "landing-caption", pickingMode = PickingMode.Ignore };
            Caption.AddToClassList("landing-caption");
            Caption.style.left = 0;
            Caption.style.top = 0;
            map.Insert(map.IndexOf(above) + 1, Caption);
            Hide();
        }

        public bool Shown => Ring.style.display == DisplayStyle.Flex;

        // The ring on a map-local point at an element scale, and the caption over `raised` (the lifted figure) when it is
        // in `view`, otherwise over the ring.
        public void Show(Vector2 point, float scale, bool attack, string caption, Rect? raised, Rect view)
        {
            stand.Place(point, scale);
            Ring.EnableInClassList(MoveClass, !attack);
            Ring.EnableInClassList(AttackClass, attack);
            if (Caption.text != caption)
                Caption.text = caption;
            var over = raised is Rect lifted && lifted.Overlaps(view) ? lifted : stand.Bounds;
            HangCaption(new Vector2(over.center.x - CaptionWidth / 2, over.yMin - CaptionHeight - CaptionGap));
            if (Shown)
                return;
            Ring.style.display = DisplayStyle.Flex;
            Caption.style.display = DisplayStyle.Flex;
        }

        private void HangCaption(Vector2 at)
        {
            if (at == captionAt)
                return;
            captionAt = at;
            Caption.style.translate = new Translate(at.x, at.y);
        }

        public void Hide()
        {
            Ring.style.display = DisplayStyle.None;
            Caption.style.display = DisplayStyle.None;
        }
    }
}
