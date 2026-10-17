using System;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MatchConnection : IDisposable
    {
        private readonly GameApi api;
        private readonly string matchId;
        private readonly MatchReadRoutes routes;
        // OBS-05: raised by a change of perspective; an answer for an older generation is dropped.
        private int generation;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private ClientWebSocket socket;
        private Task running;
        private readonly ViewVersion version;
        private bool disposed;
        private string status = "";
        private string failedView = "";
        private int failures;
        private readonly Random jitter = new Random();
        public const string DisplayError = "DisplayError";
        // MENU-22: the match is gone from the server (deleted by a player); reconnecting cannot bring it back.
        public const string Deleted = "MatchDeleted";
        public const int ApplyAttempts = 3;
        public event Action<PlayerView> Updated;
        public event Action<string> StatusChanged;
        public long Revision => version.Revision;
        public ClientDiagnostics Diagnostics { get; set; } = ClientDiagnostics.Unity;

        public MatchConnection(GameApi api, string matchId, MatchReadRoutes routes = null)
        {
            this.api = api;
            this.matchId = matchId;
            this.routes = routes ?? MatchReadRoutes.Player(matchId);
            version = new ViewVersion(matchId);
        }
        public void Start()
        {
            if (running == null)
                running = Run();
        }

        public async Task Refresh()
        {
            var ticket = generation;
            var view = await api.Get<PlayerView>(routes.View, lifetime.Token);
            if (!disposed && ticket == generation)
                Apply(view);
        }

        // OBS-05: another perspective of the same match. The current revision applies again from the other side, and
        // an answer still on its way for the previous side is dropped.
        public void Rebase()
        {
            if (disposed)
                return;
            generation++;
            version.Reset();
            failedView = "";
            failures = 0;
            _ = RefreshQuietly();
        }

        private async Task RefreshQuietly()
        {
            try
            {
                await Refresh();
            }
            catch (OperationCanceledException) { }
            catch (Exception error) when (!disposed)
            {
                // The update loop keeps running; its next notice or reconnect reads the view again.
                Diagnostics.Report(DiagnosticCategory.Transport, matchId, Revision, error);
            }
        }

        // QUAL-09: a view counts as applied only when every observer applied it. Each observer runs on its own, so
        // one failure neither hides the view from the others nor turns into a network status; the version stays
        // where it was and the next update notice (at least every few seconds) applies the same view again, up to
        // ApplyAttempts times per exact view. Observers must therefore apply a repeated view idempotently.
        public bool Apply(PlayerView view)
        {
            if (view == null || !version.IsNewer(view) || GaveUp(view))
                return false;
            var failed = false;
            foreach (var observer in Observers())
                failed |= !Notify(observer, view);
            if (failed)
            {
                Failed(view);
                return false;
            }
            version.Commit(view);
            Recovered();
            return true;
        }

        private Action<PlayerView>[] Observers() =>
            Updated?.GetInvocationList().Cast<Action<PlayerView>>().ToArray() ?? Array.Empty<Action<PlayerView>>();

        private bool Notify(Action<PlayerView> observer, PlayerView view)
        {
            try
            {
                observer(view);
                return true;
            }
            catch (Exception error)
            {
                Diagnostics.Report(DiagnosticCategory.Presentation, matchId, view.Revision, error);
                return false;
            }
        }

        private bool GaveUp(PlayerView view) => failedView == ViewVersion.Key(view) && failures >= ApplyAttempts;

        private void Failed(PlayerView view)
        {
            var key = ViewVersion.Key(view);
            failures = failedView == key ? failures + 1 : 1;
            failedView = key;
            SetStatus(DisplayError);
        }

        private void Recovered()
        {
            failedView = "";
            failures = 0;
            if (status == DisplayError)
                SetStatus("Connected");
        }

        private void SetStatus(string next)
        {
            status = next;
            StatusChanged?.Invoke(next);
        }

        // Used by reconnect controls and integration tests; it follows the same recovery loop as a network loss.
        public void Reconnect()
        {
            if (disposed)
                return;
            // A manual reconnect gives a view that failed to apply a fresh set of attempts.
            failures = 0;
            if (running?.IsCompleted == true)
            {
                running = Run();
                return;
            }
            try
            {
                socket?.Abort();
            }
            catch (ObjectDisposedException) { }
        }

        private async Task Run()
        {
            var delay = 1;
            while (!lifetime.IsCancellationRequested)
            {
                try
                {
                    SetStatus(Revision < 0 ? "Connecting" : "Reconnecting");
                    await Refresh();
                    using (socket = new ClientWebSocket())
                    {
                        socket.Options.SetRequestHeader("Authorization", "Bearer " + api.Token);
                        socket.Options.SetRequestHeader(ClientCompatibility.ClientVersionHeader, api.Identity.Version);
                        socket.Options.SetRequestHeader(ClientCompatibility.ProtocolVersionHeader,
                            api.Identity.ProtocolVersion.ToString(CultureInfo.InvariantCulture));
                        var address = new UriBuilder(new Uri(api.Address, routes.Updates));
                        address.Scheme = address.Scheme == "https" ? "wss" : "ws";
                        await socket.ConnectAsync(address.Uri, lifetime.Token);
                        SetStatus(failedView.Length > 0 ? DisplayError : "Connected");
                        delay = 1;
                        await Receive(socket);
                    }
                }
                catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { return; }
                catch (ApiException error) when (error.Status == 401)
                {
                    Diagnostics.Report(DiagnosticCategory.Authentication, matchId, Revision, error);
                    SetStatus("SessionExpired");
                    return;
                }
                catch (ApiException error) when (error.Status == 404)
                {
                    SetStatus(Deleted);
                    return;
                }
                catch (ApiException error) when (IsPermanent(error.Message))
                {
                    Diagnostics.Report(DiagnosticCategory.Compatibility, matchId, Revision, error);
                    SetStatus(error.Message);
                    return;
                }
                catch (ApiException error) when (error.RetryAfter.HasValue)
                {
                    // QUAL-02/05: 429 and 503 name their own wait; it replaces the backoff for this round.
                    Diagnostics.Report(DiagnosticCategory.Transport, matchId, Revision, error);
                    SetStatus("Reconnecting");
                    delay = Math.Max(delay, (int)Math.Ceiling(Math.Min(60, error.RetryAfter.Value.TotalSeconds)));
                }
                catch (Exception error)
                {
                    if (lifetime.IsCancellationRequested)
                        return;
                    Diagnostics.Report(DiagnosticCategory.Transport, matchId, Revision, error);
                    SetStatus("Reconnecting");
                }
                if (lifetime.IsCancellationRequested)
                    return;
                try
                {
                    // Bounded backoff with jitter, so many clients do not reconnect in step.
                    await Task.Delay(TimeSpan.FromSeconds(delay * (0.8 + 0.4 * jitter.NextDouble())), lifetime.Token);
                }
                catch (OperationCanceledException) { return; }
                delay = Math.Min(15, delay * 2);
            }
        }

        // DIST-05: an unopenable save or an incompatible client version will not heal by retrying.
        private static bool IsPermanent(string code) => code is "CorruptSave" or "UnsupportedSaveVersion"
            or ClientCompatibility.ClientUpdateRequired or ClientCompatibility.ServerUpdateRequired;

        private async Task Receive(ClientWebSocket activeSocket)
        {
            var buffer = new byte[1024];
            while (!lifetime.IsCancellationRequested && activeSocket.State == WebSocketState.Open)
            {
                var message = await activeSocket.ReceiveAsync(new ArraySegment<byte>(buffer), lifetime.Token);
                if (message.MessageType == WebSocketMessageType.Close)
                    return;
                if (!message.EndOfMessage || message.MessageType != WebSocketMessageType.Text)
                    throw new InvalidOperationException("Invalid revision notification.");
                var notice = JsonConvert.DeserializeObject<RevisionNotice>(Encoding.UTF8.GetString(buffer, 0, message.Count));
                if (notice != null && notice.MatchId == matchId && version.IsAhead(notice.Revision, notice.PrivateVersion))
                    await Refresh();
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            lifetime.Cancel();
            try { socket?.Abort(); }
            catch (ObjectDisposedException) { }
            if (running == null)
                lifetime.Dispose();
            else
                _ = running.ContinueWith(_ => lifetime.Dispose(), TaskScheduler.Default);
            // The loop owns the socket and disposes it after pending receives unwind.
        }

        private sealed class RevisionNotice
        {
            public string MatchId { get; set; }
            public long Revision { get; set; }
            public long PrivateVersion { get; set; }
        }
    }
}
