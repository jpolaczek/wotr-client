using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // TEST-15 (the owner's layout stress scenarios): console options that do not fit the console's line at their full text
    // — The Nazgûl Strike!'s three options beside its played card, or any options in a Lords or Kings game at 1366×768 and
    // 125 %, where the wider dice panel leaves the status a narrow column — stand in a strip just above the console at their
    // full width, with the played card's committed button when that does not fit the line either. The bottom row keeps its
    // height (UI-160), and wide layouts keep the options in the line.
    // The strip stands on the bottom row's fixed top edge (Console.uss); only its left edge follows the status column. Text
    // is measured and the strip placed outside layout callbacks (doing either inside one would start another layout pass),
    // only while options are shown and only when the line's width or the options change.
    internal sealed class ConsoleOptionsFit
    {
        private const float TrailShare = .45f;
        // Moving the options changes the console's width a little; they come back to the line only with this much room
        // to spare, so the choice never flips back and forth.
        private const float Hysteresis = 24;
        private readonly VisualElement line;
        private readonly VisualElement trail;
        private readonly VisualElement options;
        private readonly IVisualElementScheduledItem measure;

        public VisualElement Strip { get; } = new VisualElement { name = "console-options-raised" };
        public bool Raised { get; private set; }
        public bool TrailRaised { get; private set; }

        public ConsoleOptionsFit(VisualElement line, VisualElement trail, VisualElement options)
        {
            this.line = line;
            this.trail = trail;
            this.options = options;
            Strip.AddToClassList("console-options-strip");
            Strip.style.display = DisplayStyle.None;
            measure = line.schedule.Execute(Measure);
            measure.Pause();
            line.RegisterCallback<GeometryChangedEvent>(Changed);
            trail.RegisterCallback<GeometryChangedEvent>(Changed);
        }

        // After the options change: measured once their buttons are laid out.
        public void Refit()
        {
            if (Shown())
                measure.ExecuteLater(0);
            else
                Place(false, false);
        }

        private void Changed(GeometryChangedEvent evt)
        {
            if (evt.oldRect != evt.newRect && Shown())
                measure.ExecuteLater(0);
        }

        private void Measure()
        {
            if (!Shown())
            {
                Place(false, false);
                return;
            }
            var spare = Available() - Needed();
            var raise = Raised ? spare < Hysteresis : spare < 0;
            Place(raise, raise && Visible(trail) && Steps() > line.contentRect.width);
        }

        private void Place(bool raise, bool liftTrail)
        {
            if (raise != Raised)
            {
                Raised = raise;
                (raise ? Strip : line).Add(options);
                Strip.style.display = raise ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (liftTrail != TrailRaised)
            {
                TrailRaised = liftTrail;
                (liftTrail ? Strip : line).Insert(0, trail);
            }
            if (Raised)
                AlignLeft();
        }

        private bool Shown() => options.style.display == DisplayStyle.Flex && options.childCount > 0;

        // The options at their full text: in the line they share its width equally, so each needs the widest one's text,
        // padding, border and margin; then the row's own margins.
        private float Needed()
        {
            var buttons = options.Children().OfType<Button>().ToList();
            return buttons.Count * buttons.Max(Width) + Margins(options);
        }

        // The line beside the committed steps, which take at most their share of it.
        private float Available()
        {
            var steps = Visible(trail) ? Mathf.Min(line.contentRect.width * TrailShare, Steps()) : 0;
            return line.contentRect.width - steps;
        }

        // The committed steps at their full text.
        private float Steps() => trail.Query<Button>().ToList().Where(Visible).Sum(Width) + Margins(trail);

        private static float Width(Button button)
        {
            var style = button.resolvedStyle;
            var text = button.MeasureTextSize(button.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            return text + style.paddingLeft + style.paddingRight + style.borderLeftWidth + style.borderRightWidth + Margins(button);
        }

        private static float Margins(VisualElement element) => element.resolvedStyle.marginLeft + element.resolvedStyle.marginRight;

        private static bool Visible(VisualElement element) => element.resolvedStyle.display != DisplayStyle.None;

        // The strip's left edge on the status column's.
        private void AlignLeft()
        {
            var status = line.parent;
            if (Strip.parent == null || status?.panel == null)
                return;
            var left = Mathf.Round(Strip.parent.WorldToLocal(status.worldBound).xMin);
            if (Strip.style.left.value.value != left)
                Strip.style.left = left;
        }
    }
}
