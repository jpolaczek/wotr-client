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
            BoardPresentation data, VersionComparison versions = null)
        {
            this.draft = draft;
            this.edit = edit;
            var option = DraftOptions.For(view, draft);
            if (draft.Kind == CommandKind.ChooseCompanionVersions)
            {
                var choices = new CompanionSetupChoices(root, option, draft, edit, versions);
                refresh.Add(choices.Refresh);
            }
            // UI-270: the homelands are chosen on the Fellowship panel's portraits and the map (UI-482 (owner, 2026-11-04): the
            // console holds their instruction and tokens), the Council's token on the tokens in the Shadow's pool
            // (CouncilTokenSlots).
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
