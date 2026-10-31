using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-432 (owner's rule, UI-430): an expansion step's die is its die in the own pool (Narya's unsent step, the kings'
    // actions) and a king is his portrait in the Characters panel (UI-274), so the tray lists neither: the cost-<die> and
    // sovereign-choice-<king> fallbacks are gone.
    public sealed class ExpansionChoices
    {
        private readonly List<Action> refresh = new List<Action>();

        public ExpansionChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit,
            BoardPresentation data, VersionComparison versions = null)
        {
            var option = DraftOptions.For(view, draft);
            if (CouncilDraft.Handles(draft.Kind))
            {
                var council = new CouncilChoices(root, view, draft, edit, data, versions);
                refresh.Add(council.Refresh);
                return;
            }
            // UI-376/UI-377 (owner): a Council token's printed use stands on its objects — its medallion in the pool, the
            // Politics panel it opens, the Nazgûl above the token — so the decision tray lists nothing for it.
            if (draft.Kind == CommandKind.UseCouncilToken)
                return;
            if (draft.Kind == CommandKind.AwakenSovereign && draft.Payload.CharacterId.Length > 0)
            {
                var travel = new FellowshipChoices(root, view, draft, edit, data);
                refresh.Add(travel.Refresh);
            }
            if (draft.Kind == CommandKind.UseNarya)
                root.Add(new Label("Narya → " + string.Join(" · ", option.Targets.Select(FigureGrid.Name)) + " at war"));
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
