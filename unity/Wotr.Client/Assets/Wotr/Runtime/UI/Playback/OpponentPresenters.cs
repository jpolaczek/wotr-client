using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-305: the production presenters of the opponent's actions on one mounted board, in beat order — the cost die,
    // then the camera — with the Skip target, Esc (only when nothing else owns it) and the pause while the player reads a
    // card opened during the presentation. The family presenters of UI-306–UI-312 register after these.
    public sealed class OpponentPresenters : IDisposable
    {
        private readonly BoardScreen board;
        private readonly PlaybackBinding binding;
        private readonly PresentationLayer layer;
        private readonly CostEmphasis cost;
        private readonly PresentationCamera camera;
        public SkipControl Skip { get; }
        public PresentationCamera Camera => camera;
        private PresentationPlayback Playback => binding.Playback;
        private bool reading;

        public OpponentPresenters(BoardScreen board, PlaybackBinding binding)
        {
            this.board = board;
            this.binding = binding;
            layer = new PresentationLayer(board);
            cost = new CostEmphasis(board, layer);
            camera = new PresentationCamera(board);
            Skip = new SkipControl(board, layer, Playback);
            Playback.Register(cost);
            Playback.Register(camera);
            Playback.Paused = Reading;
            Playback.Released += Released;
            binding.Ticked += Skip.Refresh;
            board.Escape.SkipPresentation = Escape;
            board.Cards.Opening += CardOpening;
        }

        public void Dispose()
        {
            board.Cards.Opening -= CardOpening;
            board.Escape.SkipPresentation = null;
            binding.Ticked -= Skip.Refresh;
            Playback.Released -= Released;
            Playback.Paused = null;
            camera.Dispose();
            layer.Root.RemoveFromHierarchy();
        }

        // OPPONENT_ACTIONS_UX §6.2: a card the player opens while a presentation plays holds it until the card closes.
        private void CardOpening()
        {
            if (Playback.Presenting)
                reading = true;
        }

        private bool Reading()
        {
            reading &= board.Cards.Pinned && board.Cards.Root.resolvedStyle.display != DisplayStyle.None;
            return reading;
        }

        private bool Escape()
        {
            if (!Playback.Presenting)
                return false;
            Playback.Skip();
            return true;
        }

        private void Released()
        {
            cost.Clear();
            camera.Released();
            reading = false;
        }
    }
}
