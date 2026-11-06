using System;
using System.Threading;

namespace Wotr.Client
{
    // UI-504 (R35, docs/ui/UI-12-RETURN.md, RANKED_UX): how hard a board's connection works. A correspondence game can wait;
    // in a ranked game a clock is running, so a dead socket is noticed in seconds and every attempt is bounded and soon
    // repeated. MatchConnection follows the ranked policy from the first view that shows a clock.
    public sealed class ConnectionPolicy
    {
        public static readonly ConnectionPolicy Standard = new ConnectionPolicy(null, null, 15, 60);
        public static readonly ConnectionPolicy Ranked = new ConnectionPolicy(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(5), 1, 5);

        private ConnectionPolicy(TimeSpan? silence, TimeSpan? attempt, int longestDelaySeconds, int retryAfterCapSeconds)
        {
            Silence = silence;
            AttemptTimeout = attempt;
            LongestDelaySeconds = longestDelaySeconds;
            RetryAfterCapSeconds = retryAfterCapSeconds;
        }

        // No message for this long ends the socket; the server sends one at least every 5 s.
        public TimeSpan? Silence { get; }
        public TimeSpan? AttemptTimeout { get; }
        public int LongestDelaySeconds { get; }
        public int RetryAfterCapSeconds { get; }

        // One attempt's token: bounded by this policy, and by the whole connection's lifetime.
        public CancellationTokenSource Attempt(CancellationToken lifetime) => Bounded(lifetime, AttemptTimeout);

        public CancellationTokenSource Listening(CancellationToken lifetime) => Bounded(lifetime, Silence);

        private static CancellationTokenSource Bounded(CancellationToken lifetime, TimeSpan? limit)
        {
            var source = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
            if (limit.HasValue)
                source.CancelAfter(limit.Value);
            return source;
        }
    }
}
