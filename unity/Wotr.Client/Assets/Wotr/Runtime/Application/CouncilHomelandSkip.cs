using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-482 (owner, 2026-11-04): "I have to press confirm twice and one of them does not really confirm anything". The
    // server always opens the Council's homelands, even with no companion to place, so that the step's existence reveals no
    // version; that stays. With no candidate the client answers it itself, with the empty list and through the normal send,
    // after a short random pause: a quick click's timing, which tells the Shadow nothing. One send per decision: a refused
    // one leaves the console's ✓ to the player, and only a reconnection or a restored match arms the same decision again.
    public sealed class CouncilHomelandSkip : IDisposable
    {
        public const int ShortestPause = 1000;
        public const int LongestPause = 2500;

        private readonly Action<Action, long> later;
        private readonly Action send;
        private readonly Func<long> pause;
        private string waiting = "";
        private string sent = "";
        private int generation;

        // later: runs the action after the pause, on a fresh scheduled item (one rescheduled from its own callback is
        // dropped). pause: the milliseconds to wait, random by default; this is not game randomness.
        public CouncilHomelandSkip(Action<Action, long> later, Action send, Func<long> pause = null)
        {
            this.later = later;
            this.send = send;
            this.pause = pause ?? RandomPause;
        }

        private static long RandomPause() => UnityEngine.Random.Range(ShortestPause, LongestPause + 1);

        // The projection and the draft alone: an opponent's, an observer's or a finished view never owns this step.
        public static bool Applies(PlayerView view, TurnDraft draft)
        {
            if (!CouncilHomelandPresentation.Owns(view) || draft?.Kind != CommandKind.ChooseCouncilHomelands ||
                !CouncilHomelandPresentation.NothingToPlace(view))
                return false;
            return draft.Payload.Placements.Count == 0 && draft.Ready(CompanionSetupPresentation.HomelandOption(view));
        }

        // free: online and no command, presentation or newer revision under way. Every change of these reaches here (the
        // interaction renders), so a pause under way ends the moment its decision closes or the client stops being free.
        public void Refresh(PlayerView view, TurnDraft draft, bool free)
        {
            var decision = free && Applies(view, draft) ? view.Decision.Id : "";
            if (decision == waiting)
                return;
            Cancel();
            if (decision.Length == 0 || decision == sent)
                return;
            waiting = decision;
            var scheduled = generation;
            later(() => Fire(scheduled), pause());
        }

        // The connection came back, or the same still-open decision deserves one more answer.
        public void Rearm() => sent = "";

        private void Fire(int scheduled)
        {
            if (scheduled != generation)
                return;
            sent = waiting;
            waiting = "";
            send();
        }

        private void Cancel()
        {
            generation++;
            waiting = "";
        }

        public void Dispose() => Cancel();
    }
}
