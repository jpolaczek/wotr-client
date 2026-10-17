using System;
using System.Threading.Tasks;

namespace Wotr.Client
{
    public sealed class LocalMatchPersistence
    {
        private readonly ILocalMatchStorage files;
        private SavedTurnDraft queued;
        private long generation;
        private Task writing = Task.CompletedTask;
        public CommandJournal Actions { get; private set; }
        public CommandJournal Policy { get; private set; }
        public SavedTurnDraft Draft { get; private set; }
        public NotificationReadStore Notifications { get; private set; }
        public string Error { get; private set; } = "";
        public event Action Changed;
        public Task Flushed => Task.WhenAll(writing, Actions.Flushed, Policy.Flushed, Notifications.Flushed);
        public bool Settled => writing.IsCompleted && Actions.Flushed.IsCompleted && Policy.Flushed.IsCompleted && Notifications.Flushed.IsCompleted;
        public string RecoveryError => Error.Length > 0 ? "Draft · " + Error :
            Actions.Error.Length > 0 ? "Action · " + Actions.Error :
            Policy.Error.Length > 0 ? "Combat policy · " + Policy.Error : "";

        private LocalMatchPersistence(ILocalMatchStorage files) { this.files = files; }

        public static Task<LocalMatchPersistence> Open(string root, string server, string account, string match) =>
            Open(new LocalMatchFiles(root, server, account, match));

        public static async Task<LocalMatchPersistence> Open(ILocalMatchStorage files)
        {
            var result = new LocalMatchPersistence(files);
            result.Actions = await CommandJournal.Open(result.files);
            result.Policy = await CommandJournal.Open(result.files, true);
            result.Notifications = await NotificationReadStore.Open(result.files);
            result.Draft = await result.files.Read<SavedTurnDraft>("draft");
            if (result.Draft?.Context != null && result.Draft.Context.MatchId != files.MatchId)
                throw new LocalSaveException(new System.IO.InvalidDataException("Draft belongs to another match."));
            return result;
        }

        // A refresh or a receipt may enqueue another write while the first is in flight.
        public async Task FlushStable()
        {
            Task draft, action, policy, notifications;
            do
            {
                draft = writing;
                action = Actions.Flushed;
                policy = Policy.Flushed;
                notifications = Notifications.Flushed;
                await Task.WhenAll(draft, action, policy, notifications);
            } while (draft != writing || action != Actions.Flushed || policy != Policy.Flushed || notifications != Notifications.Flushed);
        }

        public void Retry()
        {
            if (Error.Length > 0 && writing.IsCompleted)
                writing = Drain();
            Actions.Retry();
            Policy.Retry();
        }

        public void SaveDraft(SavedTurnDraft draft)
        {
            queued = draft;
            generation++;
            if (writing.IsCompleted)
                writing = Drain();
        }

        private async Task Drain()
        {
            long saved;
            do
            {
                saved = generation;
                var draft = queued;
                var previousError = Error;
                try
                {
                    await files.Write("draft", draft);
                    Draft = draft;
                    Error = "";
                }
                catch (LocalSaveException error) { Error = error.Message; }
                if (previousError != Error)
                    Changed?.Invoke();
            } while (saved != generation);
        }
    }
}
