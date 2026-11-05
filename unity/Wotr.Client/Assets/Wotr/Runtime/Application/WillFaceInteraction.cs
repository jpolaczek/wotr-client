using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-461 (owner, 2026-11-03): feeds the console's Will of the West face menu from the chosen die and routes its click:
    // the result is one remembered step of the draft (console Back undoes it) and sends nothing.
    public sealed class WillFaceInteraction : IDisposable
    {
        private static readonly WillFaceSlot[] None = new WillFaceSlot[0];
        private readonly BoardScreen board;
        private readonly Action<Action> edit;
        private readonly Func<bool> busy;
        private TurnDraft draft;

        public WillFaceInteraction(BoardScreen board, Action<Action> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            board.Console.Status.Faces.Chosen += Choose;
        }

        // After the action die's context, which tells whether the chosen die is a Will die and what it is used as.
        public void Refresh(PlayerView view, TurnDraft selection, bool enabled)
        {
            draft = selection;
            board.Console.Status.SetFaces(WillFaceChoices.For(view, draft, board.ActionDie), enabled);
        }

        private void Choose(DieFace face)
        {
            if (draft == null || busy())
                return;
            edit(() => WillFaceChoices.Choose(draft, face));
        }

        public void Dispose()
        {
            board.Console.Status.Faces.Chosen -= Choose;
            board.Console.Status.SetFaces(None, false);
        }
    }
}
