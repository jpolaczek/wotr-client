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
            // UI-239: only real choices are buttons; the chosen character is named in the summary instead.
            foreach (var choice in option.CharacterActions.Where(_ => current == null))
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
                // UI-271: an entry is chosen on its row in the Characters panel, where another lit row changes it.
                if (option.CharacterActions.Count > 1 && !compact && draft.Kind != CommandKind.EnterCharacter)
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
                    ? CharacterLibrary.Name(selected.CharacterId) + " · Choose a highlighted region"
                    : string.Join(" · ", targets.Where(data.Regions.ContainsKey).Select(id => data.Regions[id].Name));
                summary.tooltip = selected?.Detail ?? "";
            });
        }

        // UI-239: the console's hint while a chosen character's entry waits only for its region on the map.
        public static string RegionHint(PlayerView view, TurnDraft draft)
        {
            if (draft.Kind != CommandKind.EnterCharacter || draft.Payload.RegionId.Length > 0)
                return "";
            var selected = CharacterDraft.Selected(draft, DraftOptions.For(view, draft));
            return selected == null || selected.Recruitment.Count > 0 ? "" : CharacterLibrary.Name(selected.CharacterId) + " · Choose a highlighted region on the map";
        }

        // One die, or dice of one kind, are chosen for the player; only different dice are a choice worth a button.
        public static bool DiceChoice(PlayerView view, CharacterActionOption choice) => CharacterDraft.UniformDie(view, choice).Length == 0;

        private void AddCosts(VisualElement root, PlayerView view, TurnDraft draft, CharacterActionOption choice, Action<Action> edit)
        {
            // UI-248: different dice are chosen in the own dice pool, where they glow (CostDice); none is listed here.
            foreach (var id in DiceChoice(view, choice) && !CostDice.InPool(view, draft) ? choice.Dice : Enumerable.Empty<string>())
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
