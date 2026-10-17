using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.Client
{
    public interface ICommandJournal
    {
        GameCommand Pending { get; }
        Task Save(GameCommand command);
        Task Complete(string commandId);
    }

    public sealed class CommandJournal : ICommandJournal
    {
        private readonly ILocalMatchStorage files;
        private readonly string name;
        private Func<Task> failedWrite;
        public GameCommand Pending { get; private set; }
        public Task Flushed { get; private set; } = Task.CompletedTask;
        public string Error { get; private set; } = "";

        private CommandJournal(ILocalMatchStorage files, string name, GameCommand pending)
        {
            this.files = files;
            this.name = name;
            Pending = pending;
        }

        public static async Task<CommandJournal> Open(ILocalMatchStorage files, bool policy = false)
        {
            var name = policy ? "policy" : "action";
            var pending = await files.Read<GameCommand>(name);
            if (pending != null && (pending.MatchId != files.MatchId || string.IsNullOrEmpty(pending.CommandId) ||
                (pending.Kind == CommandKind.SetCombatCardPolicy) != policy))
                throw new LocalSaveException(new System.IO.InvalidDataException("Invalid pending command."));
            return new CommandJournal(files, name, pending);
        }

        public Task Save(GameCommand command)
        {
            var copy = JsonConvert.DeserializeObject<GameCommand>(JsonConvert.SerializeObject(command));
            return Track(() => SaveRecord(copy));
        }

        public Task Complete(string commandId) => Track(() => CompleteRecord(commandId));

        public void Retry()
        {
            if (failedWrite != null && Flushed.IsCompleted)
                _ = Track(failedWrite);
        }

        private async Task SaveRecord(GameCommand command)
        {
            await files.SaveCommand(name, command);
            Pending = JsonConvert.DeserializeObject<GameCommand>(JsonConvert.SerializeObject(command));
        }

        private async Task CompleteRecord(string commandId)
        {
            await files.CompleteCommand(name, commandId);
            if (Pending?.CommandId == commandId)
                Pending = null;
        }

        private Task Track(Func<Task> write)
        {
            var operation = WriteAfter(Flushed, write);
            Flushed = Settle(operation);
            return operation;
        }

        private async Task WriteAfter(Task previous, Func<Task> write)
        {
            await previous;
            try
            {
                await write();
                failedWrite = null;
                Error = "";
            }
            catch (LocalSaveException error)
            {
                failedWrite = write;
                Error = error.Message;
                throw;
            }
        }

        // Senders still receive the exception; navigation also checks Error after settling.
        private static async Task Settle(Task operation)
        {
            try { await operation; }
            catch (LocalSaveException) { }
        }
    }
}
