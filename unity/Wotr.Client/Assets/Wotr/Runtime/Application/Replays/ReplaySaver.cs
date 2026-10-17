using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wotr.Client
{
    // REPLAY-04: Save replay on the end screen of a player's or an observer's board. The finished game's replay is
    // downloaded straight into the library, which keeps it only once it checked the whole file; the button says
    // Saved only then. Leaving the board cancels the download and keeps everything saved before.
    public sealed class ReplaySaver : IDisposable
    {
        private readonly GameApi api;
        private readonly string route;
        private readonly ReplayLibrary library;
        private readonly GameEndScreen screen;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool busy;
        private bool disposed;

        public ReplaySaver(GameApi api, string route, ReplayLibrary library, GameEndScreen screen)
        {
            this.api = api;
            this.route = route;
            this.library = library;
            this.screen = screen;
            screen.SaveReplayRequested += Save;
            screen.SetReplay(ReplaySaveState.Ready, "Keep this game in Replays to watch it again, also offline.");
        }

        private async void Save()
        {
            if (busy || disposed)
                return;
            busy = true;
            screen.SetReplay(ReplaySaveState.Saving, "Downloading the replay…");
            var token = lifetime.Token;
            try
            {
                var added = await library.Add(target => api.Download(route, target, token), token);
                if (!disposed)
                    screen.SetReplay(added == ReplayAdded.Saved ? ReplaySaveState.Saved : ReplaySaveState.AlreadySaved);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error)
            {
                if (!disposed)
                    Show(error);
            }
            finally
            {
                busy = false;
            }
        }

        private void Show(Exception error)
        {
            if (error is ApiException refused && refused.Message == "ReplayUnavailable")
                screen.SetReplay(ReplaySaveState.Unavailable, "This game started before replays were recorded.");
            else
                screen.SetReplay(ReplaySaveState.Failed, Problem(error));
        }

        public static string Problem(Exception error) => error switch
        {
            ApiException refused when refused.Message == "MatchNotFinished" => "The game is not over yet.",
            ApiException refused when refused.Status == 404 => "This game can no longer be saved from here.",
            ApiException refused when refused.Status == 429 => "The server is busy. Try again in a moment.",
            ApiException _ => "The server did not give the replay. Try again.",
            ReplayFormatException format => format.Message,
            HttpRequestException _ => "Connection interrupted. Try again when the server is available.",
            IOException _ or UnauthorizedAccessException _ => "The replay could not be written to this computer: " + error.Message,
            _ => "The replay could not be saved. Try again."
        };

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            screen.SaveReplayRequested -= Save;
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
