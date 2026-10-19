using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-270: the Council's homelands on the Fellowship panel. The companions are chosen on its portraits and their
    // homelands on the map; here the panel says what to do, keeps the selected companion with the Fellowship, and shows
    // which Council Action Tokens the Shadow will receive. The ✓ sends the choice, an empty one included.
    public sealed class CouncilHomelandChoices
    {
        private readonly TurnDraft draft;
        private readonly HashSet<string> eligible = new HashSet<string>();
        private readonly Label consequence = new Label { name = "homeland-token-effect" };
        private readonly Label politics = new Label("◇ Politics");
        private readonly Label travel = new Label("→ Travel");
        private readonly Label hint = new Label { name = "homeland-hint" };
        private readonly Button stay;

        public CouncilHomelandChoices(VisualElement root, PlayerView view, ActionOption option, TurnDraft draft,
            Action<Action> edit, BoardPresentation data)
        {
            this.draft = draft;
            if (!CouncilHomelandPresentation.Owns(view))
                return;
            foreach (var choice in option.CharacterActions)
                eligible.Add(choice.CharacterId);
            var choices = new VisualElement { name = "council-homeland-choices", focusable = true };
            root.Add(choices);
            hint.AddToClassList("homeland-hint");
            hint.AddToClassList("draft-status");
            choices.Add(hint);
            stay = BoardScreen.Button("Keep with the Fellowship", () => edit(() => CouncilDraft.Homeland(draft, draft.Payload.CharacterId, "")));
            stay.name = "homeland-stay";
            stay.tooltip = "Keep the selected companion with the Fellowship · Undo their placement";
            var tokens = BoardScreen.Row("homeland-tokens");
            politics.AddToClassList("homeland-token");
            travel.AddToClassList("homeland-token");
            tokens.Add(stay);
            tokens.Add(politics);
            tokens.Add(travel);
            tokens.Add(consequence);
            choices.Add(tokens);
        }

        public void Refresh()
        {
            if (stay == null)
                return;
            var selected = eligible.Contains(draft.Payload.CharacterId);
            stay.SetEnabled(selected);
            stay.EnableInClassList("selected", selected && draft.Payload.Placements.All(value => value.UnitId != draft.Payload.CharacterId));
            hint.text = eligible.Count == 0 ? "All companions stay with the Fellowship."
                : selected ? "Click a lit homeland on the map, or keep the companion with the Fellowship."
                : "Click a glowing companion, then a lit homeland on the map; dragging the portrait works too.";
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
