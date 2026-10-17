using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Fixtures
{
    // Faults only the local recovery boundary. All successful I/O uses the real atomic files.
    public sealed class ControlledRecoveryStorage : ILocalMatchStorage
    {
        private readonly ILocalMatchStorage files;
        private readonly Dictionary<string, WriteGate> gates = new Dictionary<string, WriteGate>();
        public string FailName { get; set; } = "";
        public string MatchId => files.MatchId;
        public int Writes { get; private set; }

        public ControlledRecoveryStorage(ILocalMatchStorage files) { this.files = files; }

        public WriteGate Pause(string name)
        {
            var gate = new WriteGate();
            gates[name] = gate;
            return gate;
        }

        public Task<T> Read<T>(string name) where T : class => files.Read<T>(name);
        public Task Write<T>(string name, T data) where T : class => Write(name, () => files.Write(name, data));
        public Task SaveCommand(string name, GameCommand command) => Write(name, () => files.SaveCommand(name, command));
        public Task CompleteCommand(string name, string id) => Write(name + "-complete", () => files.CompleteCommand(name, id));
        public Task MergeNotificationRead(NotificationReadRecord record) => files.MergeNotificationRead(record);

        private async Task Write(string name, Func<Task> operation)
        {
            Writes++;
            if (gates.TryGetValue(name, out var gate))
            {
                gates.Remove(name);
                gate.Start();
                await gate.Released;
            }
            if (FailName == name)
                throw new LocalSaveException(new IOException("Controlled local write failure."));
            await operation();
        }

        public sealed class WriteGate
        {
            private readonly TaskCompletionSource<bool> started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> released = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public Task Started => started.Task;
            public Task Released => released.Task;
            public void Start() => started.TrySetResult(true);
            public void Release() => released.TrySetResult(true);
        }
    }
}
