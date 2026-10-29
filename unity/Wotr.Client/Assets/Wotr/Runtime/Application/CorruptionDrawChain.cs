using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-399 (owner): a standard corruption attempt (T28) draws a fixed number of tiles with no choice between the draws,
    // so its Draw tiles is one click: after it the client sends the next draw by itself, a short pause after each tile
    // shows, until the limit or a Kings tile; then the choice waits for the player. Each draw stays the server's own
    // command. The Sequential variant keeps a click per draw, as each is a choice.
    public sealed class CorruptionDrawChain
    {
        public const long PauseMilliseconds = 450;
        private string attempt = "";
        private long due = -1;

        // The player's Draw tiles click starts the chain for this attempt.
        public void Start(PlayerView view) => attempt = view?.CorruptionAttempt?.Sequential == false ? Key(view) : "";

        // The next draw is due on this view, once per revision: the same standard attempt still has a draw as its only step.
        public bool Due(PlayerView view)
        {
            if (attempt.Length == 0 || !SovereignAttempt.MustDraw(view) || Key(view) != attempt)
            {
                if (!SovereignAttempt.OwnAttempt(view))
                    attempt = "";
                return false;
            }
            if (due == view.Revision)
                return false;
            due = view.Revision;
            return true;
        }

        public bool Running(PlayerView view) => attempt.Length > 0 && SovereignAttempt.MustDraw(view) && Key(view) == attempt;

        private static string Key(PlayerView view) => view.MatchId + "/" + view.Turn + "/" + view.CorruptionAttempt?.SovereignId;
    }
}
