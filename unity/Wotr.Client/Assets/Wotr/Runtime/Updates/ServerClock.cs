using System;

namespace Wotr.Client.Updates
{
    // Deadlines come from the server (D13): the local clock may be wrong, so the updater keeps the offset between
    // the catalog's serverTime and the local time of its answer.
    public sealed class ServerClock
    {
        private readonly Func<DateTimeOffset> local;
        private TimeSpan offset;

        public ServerClock(Func<DateTimeOffset> local = null)
        {
            this.local = local ?? (() => DateTimeOffset.UtcNow);
        }

        public DateTimeOffset Now => local() + offset;

        public void Observe(DateTimeOffset serverTime)
        {
            if (serverTime != default)
                offset = serverTime - local();
        }
    }
}
