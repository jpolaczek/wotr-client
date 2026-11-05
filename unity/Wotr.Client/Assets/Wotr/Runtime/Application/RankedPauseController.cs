using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-500 (R35, docs/ui/RANKED_UX.md): pausing and resuming a ranked game. Each player has three pauses, and either may
    // resume. A pause is no rules event, so it reaches both clients as a new clock on the view, without a revision. The
    // server decides; a refused request only reads the clock again.
    public sealed class RankedPauseController : IDisposable
    {
        private readonly GameApi api;
        private readonly BoardScreen board;
        private readonly MatchConnection connection;
        private readonly string route;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool busy;

        public RankedPauseController(GameApi api, string matchId, BoardScreen board, MatchConnection connection)
        {
            this.api = api;
            this.board = board;
            this.connection = connection;
            route = "api/matches/" + Uri.EscapeDataString(matchId);
            board.TopBar.Clock.PauseRequested += Pause;
            board.ResumeRequested += Resume;
        }

        public void Dispose()
        {
            board.TopBar.Clock.PauseRequested -= Pause;
            board.ResumeRequested -= Resume;
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private void Pause() => _ = Send(route + "/pause");

        private void Resume() => _ = Send(route + "/unpause");

        private async Task Send(string path)
        {
            if (busy)
                return;
            busy = true;
            var token = lifetime.Token;
            try
            {
                await Answered(path, token);
                await connection.RefreshClock();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            // The update loop keeps running; its next notice or reconnect reads the clock again.
            catch (Exception error) when (error is ApiException or HttpRequestException or OperationCanceledException) { }
            finally
            {
                busy = false;
            }
        }

        // A refusal, such as a pause the opponent has just started, still needs the clock read again.
        private async Task Answered(string path, CancellationToken token)
        {
            try
            {
                await api.Post<object>(path, null, token);
            }
            catch (ApiException refused) when (refused.Status == 409) { }
        }
    }
}
