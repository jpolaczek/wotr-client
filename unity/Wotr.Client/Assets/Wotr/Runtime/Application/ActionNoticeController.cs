using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-303: the authorized All feed serves both the pennants (which keep only the opponent's own steps, as the Opponent
    // feed did) and the opponent presentation model, which also needs the automatic consequences.
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
        public OpponentPresentationModel Presentation { get; private set; }
        // Cues that became presentable, in causal order; the playback coordinator (UI-304) consumes them.
        public event Action<IReadOnlyList<PresentationCue>> CuesReady;
        // UI-304: the history is complete up to this revision (published after the fetch's cues).
        public event Action<long> HistoryRead;
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
            Presentation ??= new OpponentPresentationModel(view.You, board.Presentation);
            revision = view.Revision;
            Publish(Presentation.Advance(revision));
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
            // A fetch started after a view arrived holds every step of that view's revision.
            var covers = revision;
            try
            {
                var fetched = Loaded && cursor.Length > 0 ? await FetchNewer() : await FetchFirst();
                if (!fetched) return;
                Loaded = true;
                ReadStatus();
                HistoryRead?.Invoke(covers);
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

        // The first load (or a reload after a discarded cursor). The newest All page is the presentation's baseline —
        // remembered, never played — and its cursor continues the All feed. The pennants read the opponent's whole history
        // backwards, as before UI-303, so the first load costs one page more instead of the whole All history.
        private async Task<bool> FetchFirst()
        {
            var newest = await Page("All", "");
            if (newest == null) return false;
            Notice(newest);
            var next = "";
            do
            {
                var page = await Page("Opponent", next.Length > 0 ? "&before=" + Uri.EscapeDataString(next) : "");
                if (page == null) return false;
                Notice(page);
                next = page.NextCursor;
            } while (next.Length > 0);
            Presentation?.Baseline(new[] { newest }, revision);
            cursor = newest.LatestCursor;
            return true;
        }

        // Every step of the All feed after the cursor: the pennants keep the opponent's, the presentation all it may show.
        private async Task<bool> FetchNewer()
        {
            string latest = null;
            var next = cursor;
            var pages = new List<ActionLogPage>();
            do
            {
                var page = await Page("All", "&after=" + Uri.EscapeDataString(next));
                if (page == null) return false;
                latest ??= page.LatestCursor;
                Notice(page);
                pages.Add(page);
                next = page.NextCursor;
            } while (next.Length > 0);
            if (Presentation != null)
                Publish(Presentation.Merge(pages));
            cursor = latest ?? cursor;
            return true;
        }

        // Null when the controller was disposed or the answer belongs to another match.
        private async Task<ActionLogPage> Page(string filter, string position)
        {
            var path = "api/matches/" + Uri.EscapeDataString(match) + "/actions?filter=" + filter + "&limit=50" + position;
            var page = await api.Get<ActionLogPage>(path, lifetime.Token);
            return disposed || page.MatchId != match ? null : page;
        }

        private void Notice(ActionLogPage page)
        {
            if (Model.Merge(page)) board.Notifications.Apply(Model.Groups);
        }

        private void Publish(IReadOnlyList<PresentationCue> cues)
        {
            if (cues.Count > 0)
                CuesReady?.Invoke(cues);
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
