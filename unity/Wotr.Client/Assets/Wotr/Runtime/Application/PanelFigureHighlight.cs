using System;
using System.Collections.Generic;

namespace Wotr.Client
{
    // UI-212, UI-215, UI-216: a context adds its eligible and selected figures to the region panel's, after the army
    // context has set them for this refresh.
    internal static class PanelFigureHighlight
    {
        public static void Add(RegionHoverPanel panel, ISet<string> eligible, ISet<string> selected, Func<bool> busy)
        {
            var previousEligible = panel.Eligible;
            var previousSelected = panel.Selected;
            panel.Eligible = id => previousEligible(id) || !busy() && eligible.Contains(id);
            panel.Selected = id => previousSelected(id) || selected.Contains(id);
            panel.RefreshSelection();
        }
    }
}
