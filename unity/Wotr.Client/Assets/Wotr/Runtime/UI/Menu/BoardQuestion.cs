using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // A question the board asks before it sends something (UI-571's unused moves, UI-572's entry into Mordor). It holds
    // the send the question was asked for: the confirm button sends it; its other button, Esc or a click beside the window
    // drop it and the game goes on. Focus starts on that other button, so Enter never answers by itself.
    public sealed class BoardQuestion
    {
        private readonly ConfirmationDialog dialog;
        private Action pending;

        public BoardQuestion(ConfirmationDialog dialog)
        {
            this.dialog = dialog;
            dialog.Confirmed += Send;
        }

        public VisualElement Root => dialog.Root;
        public bool IsOpen => dialog.IsOpen;

        public void Ask(string note, Action send)
        {
            pending = send;
            dialog.SetNote(note);
            dialog.Open();
        }

        public void Close()
        {
            pending = null;
            dialog.Close();
        }

        private void Send()
        {
            var send = pending;
            pending = null;
            send?.Invoke();
        }
    }
}
