using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-272 (CARD_CHOICES_UX): The Lore of the Elven Rings on its objects. The pool's lit dice are chosen by the pool's
    // own click (TurnInteraction); this context shows the chosen die's new results over it and sends the result clicked.
    public sealed class LoreInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;

        public LoreInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            board.LorePicker.OptionChosen += Choose;
            board.Root.RegisterCallback<GeometryChangedEvent>(Follow);
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            var faces = LoreChoice.Faces(view, draft);
            var die = faces.Count == 0 ? null : board.OwnDice.Q<Button>(LoreChoice.ChosenDie(view, draft));
            if (die == null || busy())
            {
                board.LorePicker.Hide();
                return;
            }
            var key = die.name + "/" + string.Join(",", faces.Select(f => f.Id));
            board.LorePicker.Show(die, board.OwnDice, view.You, faces, key);
        }

        private void Choose(RingFace face)
        {
            var option = LoreChoice.Option(view);
            if (option == null || busy() || face.Id.Length == 0)
                return;
            edit(() => CardDraft.Choose(draft, option, face.Id), true);
        }

        private void Follow(GeometryChangedEvent _) => board.LorePicker.Position();

        public void Dispose()
        {
            board.LorePicker.OptionChosen -= Choose;
            board.Root.UnregisterCallback<GeometryChangedEvent>(Follow);
            board.LorePicker.Hide();
        }
    }
}
