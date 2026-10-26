using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-270: the Council's homelands on the Fellowship panel. The companions are chosen on its portraits and their
    // homelands on the map; here the panel says what to do and shows which Council Action Tokens the Shadow will receive.
    // The ✓ sends the choice, an empty one included. UI-347 (D14): a second click on the placed companion keeps it with
    // the Fellowship, so no button does.
    public sealed class CouncilHomelandChoices
    {
        private readonly PlayerView view;
        private readonly TurnDraft draft;
        private readonly Label consequence = new Label { name = "homeland-token-effect" };
        private readonly Label politics = new Label("◇ Politics");
        private readonly Label travel = new Label("→ Travel");
        private readonly Label hint = new Label { name = "homeland-hint" };
        private readonly bool shown;

        public CouncilHomelandChoices(VisualElement root, PlayerView view, TurnDraft draft)
        {
            this.view = view;
            this.draft = draft;
            if (!CouncilHomelandPresentation.Owns(view))
                return;
            shown = true;
            var choices = new VisualElement { name = "council-homeland-choices", focusable = true };
            root.Add(choices);
            hint.AddToClassList("homeland-hint");
            hint.AddToClassList("draft-status");
            choices.Add(hint);
            var tokens = BoardScreen.Row("homeland-tokens");
            politics.AddToClassList("homeland-token");
            travel.AddToClassList("homeland-token");
            tokens.Add(politics);
            tokens.Add(travel);
            tokens.Add(consequence);
            choices.Add(tokens);
        }

        public void Refresh()
        {
            if (!shown)
                return;
            hint.text = CouncilHomelandPresentation.Hint(view, draft);
            var count = draft.Payload.Placements.Count;
            consequence.text = CouncilHomelandPresentation.TokenEffect(count);
            consequence.tooltip = CouncilHomelandPresentation.TokenDetail(count);
            politics.tooltip = "Council Action Token: advance a Shadow nation without a die.\n" + consequence.tooltip;
            travel.tooltip = "Council Action Token: move Nazgûl and Minions without a die.\n" + consequence.tooltip;
            foreach (var token in new[] { politics, travel })
            {
                token.EnableInClassList("homeland-token-none", count == 0);
                token.EnableInClassList("homeland-token-choice", count == 1);
                token.EnableInClassList("homeland-token-granted", count > 1);
            }
        }
    }
}
