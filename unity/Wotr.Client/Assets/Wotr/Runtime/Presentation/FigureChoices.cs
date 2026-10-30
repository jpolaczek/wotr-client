using System;

namespace Wotr.Client
{
    // UI-402 (TABLETOP_FIGURES_UX): which figures a click may choose now, which are chosen and which already marched in
    // this action, by figure id. One model, read by the region panel's chips and by the map's character miniatures, so a
    // context that lights a chip lights the figure too. The contexts compose it on every refresh (the army context first,
    // then each context adds its own figures, PanelFigureHighlight); Refresh tells both readers.
    public sealed class FigureChoices
    {
        public Func<string, bool> Eligible { get; set; } = _ => false;
        public Func<string, bool> Selected { get; set; } = _ => false;
        public Func<string, bool> Moved { get; set; } = _ => false;
        public event Action Changed;

        public void Refresh() => Changed?.Invoke();

        // Nothing to choose: the state when no context holds the figures.
        public void Clear()
        {
            Eligible = _ => false;
            Selected = _ => false;
            Moved = _ => false;
            Refresh();
        }
    }
}
