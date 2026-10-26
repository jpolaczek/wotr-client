using System.Linq;

namespace Wotr.Client
{
    // UI-343 (owner 2026-10-26: "once the Hunt reaches its end and the next actions start to animate, the Hunt window is to
    // close"): registered first, so the window showing a finished Hunt goes as the next action's first scene begins. The
    // Fellowship's tracks moving after the Hunt belong to its own action, so the result stays while they move.
    public sealed class HuntWindowRetirement : ICuePresenter
    {
        private readonly BoardScreen board;
        private readonly PresentationPlayback playback;
        // The action whose Hunt the window showed last.
        private string action = "";

        public HuntWindowRetirement(BoardScreen board, PresentationPlayback playback)
        {
            this.board = board;
            this.playback = playback;
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            var window = board.Hunt;
            if (cue.Kind == PresentationCueKind.Hunt)
                action = cue.ActionId;
            else if (window.ShowsFinishedHunt && cue.ActionId != action && !Queued(window.ShownChain))
                window.Close();
            return null;
        }

        private bool Queued(string chain) =>
            playback.Upcoming.Any(next => next.Kind == PresentationCueKind.Hunt && next.HuntChainId == chain);
    }
}
