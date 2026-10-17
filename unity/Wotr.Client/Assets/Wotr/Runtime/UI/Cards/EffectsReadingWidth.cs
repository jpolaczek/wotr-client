using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-94: the Effects panel keeps the rail width. Only when a card's complete text would push it past
    // the bottom of the stage does it widen, like CardViewportLayout, so the text wraps into fewer lines
    // instead of being clipped or scrolled. Each new reading starts again at the rail width.
    internal sealed class EffectsReadingWidth
    {
        // A widened panel also gives its source entries one line each, since their titles now fit.
        public const string WideClass = "effects-wide-reading";
        private const float Margin = 8;
        private const float Step = 48;
        private const float WidestShare = .55f;
        private readonly VisualElement panel;
        private readonly IVisualElementScheduledItem refit;
        private bool reading;

        public EffectsReadingWidth(VisualElement panel)
        {
            this.panel = panel;
            refit = panel.schedule.Execute(Refit);
            refit.Pause();
            panel.RegisterCallback<GeometryChangedEvent>(_ => Request());
        }

        public void Read(bool card)
        {
            reading = card;
            panel.style.width = StyleKeyword.Null;
            panel.style.maxWidth = StyleKeyword.Null;
            panel.RemoveFromClassList(WideClass);
            Request();
        }

        private void Request()
        {
            if (reading)
                refit.ExecuteLater(0);
        }

        // Widening only grows, so the layout settles once the panel fits or reaches its widest.
        private void Refit()
        {
            var stage = panel.parent;
            if (!reading || stage == null || panel.resolvedStyle.display == DisplayStyle.None)
                return;
            var available = stage.contentRect.height - panel.layout.y - Margin;
            var widest = Mathf.Min(stage.contentRect.width * WidestShare, stage.contentRect.width - panel.layout.x - Margin);
            if (available <= 0 || panel.layout.height <= available || panel.layout.width >= widest - .5f)
                return;
            var estimated = panel.layout.width * panel.layout.height / available;
            panel.AddToClassList(WideClass);
            panel.style.maxWidth = widest;
            panel.style.width = Mathf.Min(Mathf.Max(panel.layout.width + Step, estimated), widest);
        }
    }
}
