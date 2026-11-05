using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-503: reads the leaderboard when its page opens and on the window's Refresh while it is shown. A newer read replaces an
    // older one, and a failed read leaves what was shown.
    public sealed class LeaderboardController : IDisposable
    {
        private const string Route = "api/leaderboard";
        private readonly GameApi api;
        private readonly MainMenuScreen screen;
        private CancellationTokenSource reading;

        public LeaderboardController(GameApi api, MainMenuScreen screen)
        {
            this.api = api;
            this.screen = screen;
            screen.LeaderboardsOpened += Read;
            screen.Refresh += RefreshIfShown;
        }

        public void Dispose()
        {
            screen.LeaderboardsOpened -= Read;
            screen.Refresh -= RefreshIfShown;
            reading?.Cancel();
        }

        private void RefreshIfShown()
        {
            if (screen.LeaderboardsShown)
                Read();
        }

        private void Read() => _ = Load();

        private async Task Load()
        {
            reading?.Cancel();
            var current = new CancellationTokenSource();
            reading = current;
            try
            {
                var view = await api.Get<LeaderboardView>(Route, current.Token);
                if (reading == current)
                    screen.Leaderboards.Show(view);
            }
            catch (OperationCanceledException) when (current.IsCancellationRequested) { }
            catch (Exception error) when (error is ApiException or HttpRequestException or OperationCanceledException)
            {
                if (reading == current)
                    screen.Leaderboards.ShowFailed();
            }
        }
    }
}
