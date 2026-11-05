using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-427 (FELLOWSHIP_UX "Fellowship on track", TABLETOP_FIGURES_UX "As built (UI-427)"): the Fellowship's ◇ Hidden or
    // ◆ Revealed on its figure — a code-drawn plaque on the front of its base, as an army's plaque hangs on its stand, in
    // the Free Peoples' blue enamel with a gilt rim (Styles/Armies.uss), its diamond outlined while hidden and filled while
    // revealed. It lies in the figure's design box, so the map moves and scales it with the figure (on a region, on the
    // Mordor track and in its flight); it never takes the pointer, so the figure's click stays the hide target (UI-280). The
    // diamond is painted once per state change, never per frame. Far out, where a region's figure is drawn small, the
    // plaque keeps a readable size: it grows round the middle of its top edge (a scale, written only when the zoom changes
    // it), so it never reaches up over the figure's feet.
    public static class FellowshipStateBadge
    {
        public const string ClassName = "fellowship-state";
        public const string SymbolClass = "fellowship-state-symbol";
        public const string HiddenClass = "fellowship-hidden";
        public const string RevealedClass = "fellowship-revealed";
        // In the figure's design points (MapFellowshipMarker: a 64 × 80 box, the feet at (32, 64), the base's front at 77):
        // the plaque hangs from the feet line over the front of the base, inside the box.
        public static readonly Rect Box = new Rect(20, 64, 24, 16);
        // The diamond, as tall as a ◆ is wide and a half, and the outline's width.
        private static readonly Vector2 SymbolSize = new Vector2(9.5f, 13);
        private const float Outline = 2.2f;
        // Never drawn smaller on the screen than at this figure scale: a region's figure is drawn at 0.446–0.75 of its
        // design size (MapFellowshipMarker), the track's at 0.89 or 1.11.
        public const float SmallestScale = .62f;

        public static VisualElement Create(bool hidden)
        {
            var plaque = new VisualElement
            {
                name = ClassName,
                pickingMode = PickingMode.Ignore,
                usageHints = UsageHints.DynamicTransform
            };
            plaque.AddToClassList(ClassName);
            Place(plaque);
            var symbol = new VisualElement { name = SymbolClass, pickingMode = PickingMode.Ignore };
            symbol.AddToClassList(SymbolClass);
            symbol.generateVisualContent += Draw;
            plaque.Add(symbol);
            Show(plaque, hidden);
            return plaque;
        }

        // The state the plaque shows; the diamond repaints only when it changes.
        public static void Show(VisualElement plaque, bool hidden)
        {
            if (plaque.ClassListContains(HiddenClass) == hidden && plaque.ClassListContains(RevealedClass) != hidden)
                return;
            plaque.EnableInClassList(HiddenClass, hidden);
            plaque.EnableInClassList(RevealedClass, !hidden);
            plaque.Q(SymbolClass)?.MarkDirtyRepaint();
        }

        public static bool ShowsHidden(VisualElement plaque) => plaque.ClassListContains(HiddenClass);

        // How much larger than its design size the plaque is drawn on a figure drawn at `figureScale`.
        public static float Growth(float figureScale) => Mathf.Max(1, SmallestScale / Mathf.Max(.01f, figureScale));

        // The lower edge of the grown plaque, in the figure's design points.
        public static float Bottom(float figureScale) => Box.y + Box.height * Growth(figureScale);

        // Grows the plaque for the figure's scale; the scale is written only when it changes.
        public static void Fit(VisualElement plaque, float figureScale)
        {
            var growth = Growth(figureScale);
            var shown = plaque.style.scale.keyword == StyleKeyword.Undefined ? plaque.style.scale.value.value.x : 1;
            if (Mathf.Approximately(shown, growth))
                return;
            plaque.style.scale = new Scale(new Vector2(growth, growth));
        }

        private static void Place(VisualElement plaque)
        {
            plaque.style.position = Position.Absolute;
            plaque.style.left = Box.x;
            plaque.style.top = Box.y;
            plaque.style.width = Box.width;
            plaque.style.height = Box.height;
            plaque.style.transformOrigin = new TransformOrigin(Length.Percent(50), 0, 0);
        }

        // ◇ an outline, ◆ filled, in the symbol's colour (Styles/Armies.uss), centred in the plaque.
        private static void Draw(MeshGenerationContext context)
        {
            var symbol = context.visualElement;
            var hidden = symbol.parent != null && ShowsHidden(symbol.parent);
            var painter = context.painter2D;
            var inset = hidden ? Outline / 2 : 0;
            Diamond(painter, symbol.contentRect.center, SymbolSize / 2 - new Vector2(inset, inset * 1.4f));
            if (hidden)
            {
                painter.strokeColor = symbol.resolvedStyle.color;
                painter.lineWidth = Outline;
                painter.lineJoin = LineJoin.Miter;
                painter.Stroke();
            }
            else
            {
                painter.fillColor = symbol.resolvedStyle.color;
                painter.Fill();
            }
        }

        private static void Diamond(Painter2D painter, Vector2 centre, Vector2 half)
        {
            painter.BeginPath();
            painter.MoveTo(centre + new Vector2(0, -half.y));
            painter.LineTo(centre + new Vector2(half.x, 0));
            painter.LineTo(centre + new Vector2(0, half.y));
            painter.LineTo(centre + new Vector2(-half.x, 0));
            painter.ClosePath();
        }
    }
}
