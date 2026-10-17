using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CharacterChoices
    {
        private readonly List<Action> refresh = new List<Action>();

        public CharacterChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit, BoardPresentation data, bool compact = false,
            Func<VisualElement> reinforcements = null)
        {
            var option = DraftOptions.For(view, draft);
            var current = CharacterDraft.Selected(draft, option);
            var portraits = BoardScreen.Row("companion-portraits");
            root.Add(portraits);
            foreach (var choice in option.CharacterActions.Where(c => current == null || c == current && !compact))
            {
                var label = draft.Kind == CommandKind.EnterCharacter ? CharacterLibrary.ShortName(choice.CharacterId) : choice.Title;
                var button = BoardScreen.Button(label, () =>
                {
                    if (CharacterDraft.Selected(draft, option) != choice)
                        edit(() => CharacterDraft.Select(draft, view, choice));
                });
                button.name = "character-action-" + choice.CharacterId + "-" + choice.AbilityId;
                button.tooltip = choice.Title + "\n" + choice.Detail;
                portraits.Add(button);
                refresh.Add(() => button.EnableInClassList("selected", CharacterDraft.Selected(draft, option) == choice));
            }
            if (current != null)
            {
                if (option.CharacterActions.Count > 1 && !compact)
                {
                    var change = BoardScreen.Button("Change", () => edit(() => CharacterDraft.ClearSelection(draft)));
                    change.name = "change-character-action";
                    portraits.Add(change);
                }
                AddCosts(portraits, view, draft, current, edit);
                // UI-131: the figures of a ruler's muster or the Balrog's companion are chosen in Reinforcements.
                if (current.Recruitment.Count > 0)
                {
                    if (reinforcements != null)
                        root.Add(reinforcements());
                    return;
                }
            }
            var summary = new Label();
            root.Add(summary);
            refresh.Add(() =>
            {
                var selected = CharacterDraft.Selected(draft, option);
                var targets = draft.Kind == CommandKind.EnterCharacter ? new[] { draft.Payload.RegionId } : draft.Payload.Ids.ToArray();
                summary.text = selected == null ? "Choose a character" : targets.All(string.IsNullOrEmpty)
                    ? "Choose a highlighted region" : string.Join(" · ", targets.Where(data.Regions.ContainsKey).Select(id => data.Regions[id].Name));
                summary.tooltip = selected?.Detail ?? "";
            });
        }

        private void AddCosts(VisualElement root, PlayerView view, TurnDraft draft, CharacterActionOption choice, Action<Action> edit)
        {
            foreach (var id in choice.Dice)
            {
                var die = view.Dice.Single(d => d.Id == id);
                var button = BoardScreen.Button(ExpansionLabels.DieCost(view, draft, die) + (ExpansionLabels.Special(die) ? " · " + ExpansionLabels.DieSource(die) : ""),
                    () => edit(() => draft.Change(p => p.DieId = id)));
                button.name = "cost-" + id;
                button.tooltip = ExpansionLabels.DieDetail(die);
                root.Add(button);
                refresh.Add(() => button.EnableInClassList("selected", draft.Payload.DieId == id));
            }
            if (choice.CouncilTokens.Count == 0)
                return;
            if (!choice.RequiresCouncilToken)
                CouncilCost(root, draft, edit, "", "No token");
            foreach (var id in choice.CouncilTokens)
                CouncilCost(root, draft, edit, id, ExpansionLabels.CouncilToken(id));
        }

        private void CouncilCost(VisualElement row, TurnDraft draft, Action<Action> edit, string id, string label)
        {
            var button = BoardScreen.Button(label, () => edit(() => draft.Change(p => p.CouncilTokenId = id)));
            button.name = "entry-token-" + id;
            button.tooltip = "Entry cost · Spent with the selected die when this choice is complete";
            row.Add(button);
            refresh.Add(() => button.EnableInClassList("selected", draft.Payload.CouncilTokenId == id));
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
