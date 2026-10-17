using System;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionNoticeController : IDisposable
    {
        private readonly GameApi api;
        private readonly string match;
        private readonly BoardScreen board;
        private readonly NotificationReadStore reads;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private string cursor = "";
        private long revision = -1;
        private bool online, busy, dirty, disposed;
        public bool Loaded { get; private set; }
        public ActionNoticeModel Model { get; }
        public Task LocalWrites => reads.Flushed;

        public ActionNoticeController(GameApi api, string match, BoardScreen board, NotificationReadStore reads = null)
        {
            this.api = api;
            this.match = match;
            this.board = board;
            this.reads = reads ?? new NotificationReadStore();
            Model = new ActionNoticeModel(this.reads, board.Presentation);
            board.Log.ExplicitlyDisplayed += Displayed;
            board.Notifications.Retry += Refresh;
            this.reads.Changed += ReadStatus;
            ReadStatus();
        }

        public void Update(PlayerView view)
        {
            if (disposed || view.MatchId != match || view.Revision == revision) return;
            Model.You = view.You;
            revision = view.Revision;
            Refresh();
        }

        public void SetConnection(string state)
        {
            online = state == "Connected";
            if (online) Refresh();
        }

        private void Refresh()
        {
            if (disposed || !online || revision < 0) return;
            reads.Retry();
            if (busy) { dirty = true; return; }
            _ = Fetch();
        }

        private async Task Fetch()
        {
            busy = true;
            dirty = false;
            try
            {
                var incremental = Loaded && cursor.Length > 0;
                var next = incremental ? cursor : "";
                string latest = null;
                do
                {
                    var path = "api/matches/" + Uri.EscapeDataString(match) + "/actions?filter=Opponent&limit=50";
                    if (next.Length > 0) path += (incremental ? "&after=" : "&before=") + Uri.EscapeDataString(next);
                    var page = await api.Get<ActionLogPage>(path, lifetime.Token);
                    if (disposed || page.MatchId != match) return;
                    latest ??= page.LatestCursor;
                    if (Model.Merge(page)) board.Notifications.Apply(Model.Groups);
                    next = page.NextCursor;
                } while (next.Length > 0);
                cursor = latest ?? cursor;
                Loaded = true;
                ReadStatus();
            }
            catch (OperationCanceledException) { }
            catch (ApiException error) when (error.Status == 400 && cursor.Length > 0)
            {
                // A server can discard an obsolete cursor; rebuilding by stable IDs does not duplicate notices.
                Loaded = false;
                cursor = "";
                dirty = true;
            }
            catch (Exception)
            {
                if (!disposed) board.Notifications.SetStatus("Opponent updates unavailable · Retry");
            }
            finally
            {
                busy = false;
                if (dirty) Refresh();
            }
        }

        private void Displayed(ActionLogView view)
        {
            if (disposed || view.MatchId != match) return;
            if (Model.Displayed(view)) board.Notifications.Apply(Model.Groups);
        }
        private void ReadStatus()
        {
            if (!disposed) board.Notifications.SetStatus(reads.Error);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            board.Log.ExplicitlyDisplayed -= Displayed;
            board.Notifications.Retry -= Refresh;
            reads.Changed -= ReadStatus;
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
