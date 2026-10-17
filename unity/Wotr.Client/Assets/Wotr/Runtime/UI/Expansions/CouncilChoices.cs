using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CouncilChoices
    {
        private readonly List<Action> refresh = new List<Action>();
        private readonly TurnDraft draft;
        private readonly Action<Action> edit;

        public CouncilChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit,
            BoardPresentation data, CompanionSetupPanel setup = null)
        {
            this.draft = draft;
            this.edit = edit;
            var option = DraftOptions.For(view, draft);
            if (draft.Kind == CommandKind.ChooseCompanionVersions)
            {
                var choices = new CompanionSetupChoices(root, option, draft, edit, setup);
                refresh.Add(choices.Refresh);
            }
            else if (draft.Kind == CommandKind.ChooseCouncilHomelands)
            {
                var choices = new CouncilHomelandChoices(root, view, option, draft, edit, data);
                refresh.Add(choices.Refresh);
            }
            else
                Tokens(root, option);
        }

        private void Tokens(VisualElement root, ActionOption option)
        {
            var row = BoardScreen.Row();
            root.Add(row);
            foreach (var id in option.EligibleIds)
            {
                var button = Choice(ExpansionLabels.CouncilToken(id), "council-token-" + id,
                    () => draft.Payload.Ids.Contains(id), () => CardDraft.Choose(draft, id));
                button.tooltip = id == ExpansionDraft.CouncilPolitics ? "One action to advance a Shadow nation, without a die."
                    : "One action to move Nazgûl and Minions, without a die.";
                row.Add(button);
            }
        }

        private Button Choice(string label, string name, Func<bool> selected, Action change)
        {
            var button = BoardScreen.Button(label, () => edit(change));
            button.name = name;
            refresh.Add(() => button.EnableInClassList("selected", selected()));
            return button;
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
