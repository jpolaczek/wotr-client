using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Figures removed from an overstacked region, chosen one by one. UI-135: recruitment left this tray palette for
    // the Reinforcements panel.
    public sealed class ExcessChoices
    {
        private readonly List<Action> refresh = new List<Action>();

        public ExcessChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            var option = view.Options.First(o => o.Kind == draft.Kind);
            var grid = new FigureGrid(view.Units.Where(u => option.EligibleIds.Contains(u.Id)),
                id => draft.Payload.Ids.Contains(id), _ => false, ids => edit(() => Toggle(draft, ids, option.Maximum)),
                allowGroupSelection: false);
            root.Add(grid.Root);
            refresh.Add(grid.Refresh);
        }

        private static void Toggle(TurnDraft draft, string[] ids, int maximum)
        {
            if (ids.Length > 1)
                return; // A group shortcut must not choose arbitrary casualties within a larger group.
            if (draft.Payload.Ids.Contains(ids[0]) || draft.Payload.Ids.Count < maximum)
                draft.Toggle(ids[0]);
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
