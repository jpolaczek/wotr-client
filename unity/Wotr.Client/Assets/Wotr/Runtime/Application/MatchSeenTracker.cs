using System;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MatchSeenTracker : IDisposable
    {
        private readonly GameApi api;
        private readonly string matchId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private Task sending = Task.CompletedTask;
        private long displayed = -1;
        private long confirmed = -1;

        public MatchSeenTracker(GameApi api, string matchId)
        {
            this.api = api;
            this.matchId = matchId;
        }

        public void Displayed(long revision)
        {
            if (lifetime.IsCancellationRequested || revision < 0)
                return;
            displayed = Math.Max(displayed, revision);
            if (displayed > confirmed && sending.IsCompleted)
                sending = Save();
        }

        private async Task Save()
        {
            var delay = 2;
            while (!lifetime.IsCancellationRequested && displayed > confirmed)
            {
                try
                {
                    var result = await api.Put<SeenRevisionView>($"api/matches/{matchId}/seen",
                        new SeenRevisionRequest { Revision = displayed }, lifetime.Token);
                    confirmed = Math.Max(confirmed, result.LastSeenRevision);
                    delay = 2;
                }
                catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { return; }
                // DIST-07: a refused version (409) waits for the update instead of retrying.
                catch (ApiException error) when (error.Status == 401 || error.Status == 404 || error.Status == 409) { return; }
                catch (Exception)
                {
                    try { await Task.Delay(TimeSpan.FromSeconds(delay), lifetime.Token); }
                    catch (OperationCanceledException) { return; }
                    delay = Math.Min(15, delay * 2);
                }
            }
        }

        public void Dispose()
        {
            lifetime.Cancel();
            _ = sending.ContinueWith(_ => lifetime.Dispose(), TaskScheduler.Default);
        }
    }
}
