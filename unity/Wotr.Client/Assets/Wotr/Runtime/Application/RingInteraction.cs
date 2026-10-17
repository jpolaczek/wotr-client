using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-161: the Elven Ring gesture on the board. While a ring is armed the dice it can change glow and the others
    // dim; the chosen die gets the result popover, and a result completes the draft (UseRing is sent at once).
    // Esc, the ring again or a press on the map cancels without a command; reading other panels keeps the gesture.
    public sealed class RingInteraction : IDisposable
    {
        public const string CandidateClass = "ring-candidate";
        public const string MutedClass = "ring-muted";
        private readonly BoardScreen board;
        private readonly Action<DieFace> choose;
        private readonly Action cancel;
        private PlayerView view;
        private TurnDraft draft;
        private bool enabled;

        public RingInteraction(BoardScreen board, Action<DieFace> choose, Action cancel)
        {
            this.board = board;
            this.choose = choose;
            this.cancel = cancel;
            board.RingPicker.Chosen += Choose;
            board.Root.RegisterCallback<PointerDownEvent>(PressElsewhere, TrickleDown.TrickleDown);
            board.Root.RegisterCallback<GeometryChangedEvent>(Follow);
        }

        public void Refresh(PlayerView latest, TurnDraft current, bool canEdit)
        {
            view = latest;
            draft = current;
            enabled = canEdit;
            MarkDice();
            ShowPicker();
        }

        private void MarkDice()
        {
            var armed = RingChoice.Armed(draft);
            var dice = RingChoice.Dice(view, draft);
            foreach (var button in board.OwnDice.Query<Button>().ToList())
            {
                if (button.userData is not DraftToken token || token.Object != DraftObject.Die)
                    continue;
                button.EnableInClassList(CandidateClass, armed && dice.Contains(token.Id));
                button.EnableInClassList(MutedClass, armed && !dice.Contains(token.Id));
            }
        }

        private void ShowPicker()
        {
            var faces = RingChoice.Faces(view, draft);
            var die = faces.Count == 0 ? null : board.OwnDice.Q<Button>(draft.Payload.DieId);
            if (die == null || !enabled)
            {
                board.RingPicker.Hide();
                return;
            }
            var key = draft.Payload.DieId + "/" + string.Join(",", faces.Select(f => f.Face + ":" + f.Enabled));
            board.RingPicker.Show(die, board.OwnDice, view.You, faces, key);
        }

        private void Choose(DieFace face)
        {
            if (enabled && RingChoice.Step(draft) == RingStep.Face)
                choose(face);
        }

        private void PressElsewhere(PointerDownEvent evt)
        {
            if (view == null || !enabled || !RingChoice.Armed(draft) || evt.target is not VisualElement target)
                return;
            if (target == board.Map || board.Map.Contains(target))
                cancel();
        }

        private void Follow(GeometryChangedEvent _) => board.RingPicker.Position();

        public void Dispose()
        {
            board.RingPicker.Chosen -= Choose;
            board.Root.UnregisterCallback<PointerDownEvent>(PressElsewhere, TrickleDown.TrickleDown);
            board.Root.UnregisterCallback<GeometryChangedEvent>(Follow);
            board.RingPicker.Hide();
        }
    }
}
