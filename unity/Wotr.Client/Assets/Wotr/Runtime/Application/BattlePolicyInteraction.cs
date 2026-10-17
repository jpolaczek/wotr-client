using System;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BattlePolicyInteraction : IDisposable
    {
        private readonly BattlePolicyControl control;
        private readonly MatchConnection connection;
        private readonly CommandSender sender;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private PlayerView view;
        private bool online, blocked, sending;
        private string notice = "";
        public bool Busy => sending || sender.Pending != null;
        public event Action Changed;

        public BattlePolicyInteraction(GameApi api, MatchConnection connection, BattlePolicyControl control, ICommandJournal journal = null)
        {
            this.control = control;
            this.connection = connection;
            sender = new CommandSender(api, journal);
            control.Chosen += Choose;
        }

        public void Render(PlayerView latest, bool connected, bool commandBusy)
        {
            if (view?.MatchId != latest.MatchId || view?.Battle?.Id != latest.Battle?.Id || view?.AutoDecision.Version != latest.AutoDecision.Version)
                notice = "";
            view = latest;
            online = connected;
            blocked = commandBusy;
            control.Apply(view, online && !blocked, sending, sender.Pending != null && !sending, notice);
        }

        private void Choose()
        {
            if (!online || blocked || sending || view == null || view.Battle == null && sender.Pending == null)
                return;
            _ = Submit();
        }

        public void Recover()
        {
            if (online && !sending && view != null && sender.Pending != null)
                _ = Submit();
        }

        private async Task Submit()
        {
            sending = true;
            Changed?.Invoke();
            try
            {
                var receipt = sender.Pending != null ? await sender.Recover(lifetime.Token) :
                    await sender.Send(Command(view), lifetime.Token);
                if (lifetime.IsCancellationRequested)
                    return;
                await connection.Refresh();
                notice = !receipt.Accepted ? "Setting changed · Retry" :
                    receipt.AutoDecision?.Version == view.AutoDecision.Version ? Outcome(receipt.AutoDecision, view.Battle?.Id) : "";
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (Exception) { notice = "Check the saved result"; }
            finally
            {
                sending = false;
                if (!lifetime.IsCancellationRequested)
                    Changed?.Invoke();
            }
        }

        public static GameCommand Command(PlayerView view) => new GameCommand
        {
            MatchId = view.MatchId,
            CommandId = Guid.NewGuid().ToString("N"),
            ExpectedRevision = view.Revision,
            Kind = CommandKind.SetCombatCardPolicy,
            Payload = new CommandPayload
            {
                AutoDecision = new AutoDecisionChange
                {
                    BattleId = view.Battle.Id,
                    ExpectedVersion = view.AutoDecision.Version,
                    HandVersion = view.AutoDecision.HandVersion,
                    Enabled = view.AutoDecision.Policy == null
                }
            }
        };

        private static string Outcome(AutoDecisionView result, string battleId)
        {
            if (result?.Policy != null)
                return "";
            return result?.LastSkippedRound > 0 && result.LastSkippedBattleId == battleId ? "Ask next · Round " + result.LastSkippedRound + " kept" : "Card choices restored";
        }

        public void Dispose()
        {
            control.Chosen -= Choose;
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
