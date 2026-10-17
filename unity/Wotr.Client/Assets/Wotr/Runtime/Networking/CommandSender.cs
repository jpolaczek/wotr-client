using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CommandSender
    {
        private readonly GameApi api;
        private readonly ICommandJournal journal;
        private Task<CommandReceipt> inFlight;
        public GameCommand Pending
        {
            get; private set;
        }
        public CommandSender(GameApi api, ICommandJournal journal = null)
        {
            this.api = api;
            this.journal = journal;
            if (journal?.Pending != null)
                Pending = JsonConvert.DeserializeObject<GameCommand>(JsonConvert.SerializeObject(journal.Pending));
        }

        public Task<CommandReceipt> Send(GameCommand command, CancellationToken cancellation)
        {
            if (inFlight != null)
                return inFlight;
            if (Pending != null)
                return Recover(cancellation);
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            Pending = JsonConvert.DeserializeObject<GameCommand>(JsonConvert.SerializeObject(command));
            return Begin(false, cancellation);
        }

        public Task<CommandReceipt> Recover(CancellationToken cancellation)
        {
            if (inFlight != null)
                return inFlight;
            if (Pending == null)
                throw new InvalidOperationException("There is no pending command to recover.");
            return Begin(true, cancellation);
        }

        private Task<CommandReceipt> Begin(bool recover, CancellationToken cancellation)
        {
            var completion = new TaskCompletionSource<CommandReceipt>(TaskCreationOptions.RunContinuationsAsynchronously);
            inFlight = completion.Task;
            _ = Execute(recover, cancellation, completion);
            return completion.Task;
        }

        private async Task Execute(bool recover, CancellationToken cancellation, TaskCompletionSource<CommandReceipt> completion)
        {
            try
            {
                if (journal != null)
                    await journal.Save(Pending);
                // An uncertain response always requires lookup, even for the original object.
                var receipt = recover ? await FindReceipt(cancellation) : null;
                if (receipt == null)
                    receipt = await api.Post<CommandReceipt>($"api/matches/{Pending.MatchId}/commands", Pending, cancellation);
                if (receipt == null || receipt.CommandId != Pending.CommandId)
                    throw new InvalidOperationException("The receipt does not match the pending command.");
                if (journal != null)
                    await journal.Complete(Pending.CommandId);
                Pending = null;
                inFlight = null;
                completion.TrySetResult(receipt);
            }
            catch (OperationCanceledException)
            {
                inFlight = null;
                completion.TrySetCanceled();
            }
            catch (Exception error)
            {
                inFlight = null;
                completion.TrySetException(error);
            }
        }

        private async Task<CommandReceipt> FindReceipt(CancellationToken cancellation)
        {
            try
            {
                return await api.Get<CommandReceipt>($"api/matches/{Pending.MatchId}/receipts/{Pending.CommandId}", cancellation);
            }
            catch (ApiException error) when (error.Status == 404) { return null; }
        }
    }
}
