using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class PoliticalContextInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;
        public bool Active { get; private set; }

        public PoliticalContextInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            board.Nations.Advanced += OnAdvanced;
            board.Nations.Activated += OnActivated;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            var option = PoliticalActions.AdvanceOption(view, draft, board.ActionDie);
            Active = option != null || PoliticalActions.UsesPanel(draft);
            var selected = draft.Kind is CommandKind.AdvancePolitics or CommandKind.UseCouncilToken ? draft.Payload.NationId : "";
            // UI-366: a character's political ability lights its nation; it does not take over the console's hint.
            board.Nations.Abilities = draft.Kind == null ? CharacterPolitics.For(view, board.ActionDie)
                : new System.Collections.Generic.Dictionary<string, CharacterPolitics.Offer>();
            board.Nations.RefreshActions(option, selected, !busy(), draft);
        }

        private void OnAdvanced(string nation) => Commit(nation, false);
        private void OnActivated(string nation) => Commit(nation, true);

        // UI-134: the Reinforcements panel moves or activates a nation with the same step as the Politics panel.
        public bool Advance(string nation) => Commit(nation, false);
        public bool Activate(string nation) => Commit(nation, true);

        private bool Commit(string nation, bool activation)
        {
            if (view == null || busy())
                return false;
            var next = draft.Fork();
            var valid = activation ? PoliticalActions.Activate(next, view, nation) : Step(next, nation);
            if (!valid)
                return false;
            edit(() => draft.Adopt(next), true);
            board.Nations.PresentOwnStep();
            return true;
        }

        // The die's own step first; a character's ability when the die cannot step this nation or the ability does more.
        private bool Step(TurnDraft next, string nation)
        {
            var die = board.ActionDie;
            if (CharacterPolitics.Preferred(view, die, nation))
                return CharacterPolitics.Use(next, view, die, nation) || PoliticalActions.Advance(next, view, die, nation);
            return PoliticalActions.Advance(next, view, die, nation) || draft.Kind == null && CharacterPolitics.Use(next, view, die, nation);
        }

        public bool Drop(DraftToken source, DraftToken target) =>
            source?.Object == DraftObject.Nation && target?.Object == DraftObject.PoliticalSlot && source.Id == target.Id && Commit(source.Id, false);

        public void Dispose()
        {
            board.Nations.Advanced -= OnAdvanced;
            board.Nations.Activated -= OnActivated;
            board.Nations.Close();
        }
    }
}
