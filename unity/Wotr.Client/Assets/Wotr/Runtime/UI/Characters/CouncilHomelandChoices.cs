using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CouncilHomelandChoices
    {
        private readonly TurnDraft draft;
        private readonly BoardPresentation data;
        private readonly Dictionary<string, CouncilHomelandPortrait> portraits = new Dictionary<string, CouncilHomelandPortrait>();
        private readonly Label consequence = new Label { name = "homeland-token-effect" };
        private readonly Label politics = new Label("◇ Politics");
        private readonly Label travel = new Label("→ Travel");
        private readonly Label hint = new Label { name = "homeland-hint" };
        private readonly Button stay;

        public CouncilHomelandChoices(VisualElement root, PlayerView view, ActionOption option, TurnDraft draft,
            Action<Action> edit, BoardPresentation data)
        {
            this.draft = draft;
            this.data = data;
            if (!CouncilHomelandPresentation.Owns(view))
                return;
            var choices = new VisualElement { name = "council-homeland-choices" };
            root.Add(choices);
            hint.AddToClassList("homeland-hint");
            choices.Add(hint);
            var row = BoardScreen.Row("homeland-portraits");
            choices.Add(row);
            var own = view.OwnCompanions.ToDictionary(character => character.Id);
            foreach (var optionCharacter in option.CharacterActions)
            {
                var id = optionCharacter.CharacterId;
                var portrait = new CouncilHomelandPortrait(optionCharacter, own.TryGetValue(id, out var character) ? character : null,
                    () => Select(id, edit));
                portraits.Add(id, portrait);
                row.Add(portrait.Root);
            }
            stay = BoardScreen.Button("Fellowship", () => edit(() => CouncilDraft.Homeland(draft, draft.Payload.CharacterId, "")));
            stay.name = "homeland-stay";
            stay.tooltip = "Keep the selected companion with the Fellowship · Undo their placement";
            choices.Add(stay);
            var tokens = BoardScreen.Row("homeland-tokens");
            politics.AddToClassList("homeland-token");
            travel.AddToClassList("homeland-token");
            tokens.Add(politics);
            tokens.Add(travel);
            tokens.Add(consequence);
            choices.Add(tokens);
        }

        private void Select(string id, Action<Action> edit)
        {
            if (draft.Payload.CharacterId != id)
                edit(() => draft.Change(payload => payload.CharacterId = id));
        }

        public void Refresh()
        {
            if (stay == null)
                return;
            foreach (var pair in portraits)
            {
                var placement = draft.Payload.Placements.FirstOrDefault(value => value.UnitId == pair.Key);
                var region = placement != null && data.Regions.TryGetValue(placement.RegionId, out var definition)
                    ? definition.Name : "";
                pair.Value.Refresh(pair.Key == draft.Payload.CharacterId, region);
            }
            var selected = portraits.ContainsKey(draft.Payload.CharacterId);
            stay.SetEnabled(selected);
            stay.EnableInClassList("selected", selected && draft.Payload.Placements.All(value => value.UnitId != draft.Payload.CharacterId));
            hint.text = portraits.Count == 0 ? "All companions stay with the Fellowship."
                : selected ? "Choose a highlighted homeland, or stay with the Fellowship."
                : "Select a companion, then choose a highlighted homeland.";
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
