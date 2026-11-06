using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-499 (R35, docs/ui/RANKED_UX.md): counts a ranked game's clock locally from the server's remaining time. A view
    // reaches the client some time after the server wrote it, so every estimate of the deadline is late by that time and
    // the earliest is the best: a re-sync moves the deadline down freely and never up by a sync error. Only a change
    // too large to be one moves it up, as after a server restart gave back its downtime (RANK-06).
    public sealed class MoveClockCountdown
    {
        // Larger than any view's delay on a connection that still delivers views, smaller than a useful restart.
        public const double LargestSyncErrorSeconds = 5;
        private double deadline;
        private double warningSeconds;
        private double frozen;

        public bool Active { get; private set; }
        public bool Paused { get; private set; }
        public string DecisionId { get; private set; } = "";
        public Side Owner { get; private set; }

        public void Sync(MoveClockView clock, double now)
        {
            if (clock == null)
            {
                Active = false;
                return;
            }
            var left = clock.RemainingMilliseconds / 1000.0;
            var estimate = now + left;
            var fresh = !Active || clock.DecisionId != DecisionId || Paused || estimate > deadline + LargestSyncErrorSeconds;
            deadline = fresh ? estimate : Math.Min(deadline, estimate);
            frozen = left;
            Paused = clock.Paused;
            Active = true;
            DecisionId = clock.DecisionId;
            Owner = clock.Owner;
            warningSeconds = clock.WarningMilliseconds / 1000.0;
        }

        public double Remaining(double now)
        {
            if (Paused)
                return frozen;
            return Math.Max(0, deadline - now);
        }

        public bool InWarning(double now) => Active && Remaining(now) <= warningSeconds;

        // Whole seconds rounded up, so the clock reads 0:00 only when no time is left at all.
        public static string Format(double seconds)
        {
            var whole = (int)Math.Ceiling(Math.Max(0, seconds));
            return whole / 60 + ":" + (whole % 60).ToString("00");
        }
    }
}
