using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Wotr.Client
{
    public sealed class NotificationReadRecord
    {
        public List<string> Steps { get; set; } = new List<string>();
    }

    // Only IDs that were explicitly displayed, scoped by LocalMatchFiles. No game snapshot.
    // Besides opponent step IDs it holds the read ⚔ rounds (BattleReport.RoundKeyPrefix, UI-112).
    public sealed class NotificationReadStore
    {
        private readonly ILocalMatchStorage files;
        private readonly HashSet<string> read = new HashSet<string>();
        private long generation;
        public IReadOnlyCollection<string> Steps => read;
        public string Error { get; private set; } = "";
        public Task Flushed { get; private set; } = Task.CompletedTask;
        public event Action Changed;

        public NotificationReadStore() { }
        private NotificationReadStore(ILocalMatchStorage files) { this.files = files; }

        public static async Task<NotificationReadStore> Open(ILocalMatchStorage files)
        {
            var store = new NotificationReadStore(files);
            try
            {
                var record = await files.Read<NotificationReadRecord>("notification-read");
                if (record != null)
                {
                    if (record.Steps == null || record.Steps.Any(id => string.IsNullOrEmpty(id) || id.Length > 256))
                        throw new LocalSaveException(new InvalidDataException("Invalid read marker."));
                    store.read.UnionWith(record.Steps);
                }
            }
            catch (LocalSaveException) { store.Error = "Read markers unavailable · Updates remain unread"; }
            return store;
        }

        public void Acknowledge(IEnumerable<string> ids)
        {
            var changed = false;
            foreach (var id in ids)
                if (!string.IsNullOrEmpty(id)) changed |= read.Add(id);
            if (!changed) return;
            generation++;
            if (files != null && Flushed.IsCompleted) Flushed = Save();
        }

        public void Retry()
        {
            if (files != null && read.Count > 0 && Error.Length > 0 && Flushed.IsCompleted)
                Flushed = Save();
        }

        private async Task Save()
        {
            long saved;
            do
            {
                saved = generation;
                try
                {
                    await files.MergeNotificationRead(new NotificationReadRecord { Steps = read.ToList() });
                    Error = "";
                }
                catch (LocalSaveException) { Error = "Read markers could not be saved · Updates may return after restart"; }
                Changed?.Invoke();
            } while (saved != generation);
        }
    }
}
