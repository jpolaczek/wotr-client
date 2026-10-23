using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-304 (OPPONENT_ACTIONS_UX §7): the one playback coordinator between received views and the board. The newest
    // authoritative view (Latest) is kept apart from the one the board presents. While the cues of a newer revision play,
    // or its history is still being read (at most HistoryWait), the board keeps presenting the earlier view and every
    // command path is held; then the latest view is applied once, with its revision, private version and decision. Skip
    // drains the beat and the backlog to the latest view on the next tick, so the skip click cannot reach a control the
    // release opens. Without a registered presenter, or when the session is not live (offline, reconnecting, an
    // unfocused window), a view is applied at once, exactly as before UI-304; cues of a presented revision are dropped,
    // so a late history page never plays an old move.
    public sealed class PresentationPlayback : IDisposable
    {
        public const double HistoryWait = 1.0;
        private const int ApplyAttempts = 3;
        private readonly Action<PlayerView> present;
        private readonly Action<bool> hold;
        private readonly Func<double> clock;
        private readonly List<ICuePresenter> presenters = new List<ICuePresenter>();
        private readonly Queue<PresentationCue> queue = new Queue<PresentationCue>();
        private IPresentationBeat beat;
        private double receivedAt;
        private long historyRevision = -1;
        private bool connected, focused = true, skipped, disposed;
        private int failures;
        public PlayerView Latest { get; private set; }
        public long PresentedRevision { get; private set; } = -1;
        public PresentationCue Current { get; private set; }
        public bool Holding { get; private set; }
        public bool Live => connected && focused;
        public int Queued => queue.Count;
        public ClientDiagnostics Diagnostics { get; set; } = ClientDiagnostics.Unity;

        public PresentationPlayback(Action<PlayerView> present, Action<bool> hold, Func<double> clock)
        {
            this.present = present;
            this.hold = hold;
            this.clock = clock;
        }

        public void Register(ICuePresenter presenter) => presenters.Add(presenter);

        // A view the connection accepted. Applied at once when nothing is to be presented, so a failure still reaches
        // MatchConnection's QUAL-09 retry; otherwise it waits for its history and its cues.
        public void Receive(PlayerView view)
        {
            if (disposed || view == null)
                return;
            if (Latest == null || view.Revision != Latest.Revision)
                receivedAt = clock();
            Latest = view;
            if (Holding)
                return;
            if (!Live || presenters.Count == 0 || view.Revision <= PresentedRevision)
            {
                Present(view);
                return;
            }
            SetHold(true);
        }

        public void Enqueue(IReadOnlyList<PresentationCue> cues)
        {
            if (disposed || !Live || presenters.Count == 0)
                return;
            foreach (var cue in cues)
                if (cue.Revision > PresentedRevision)
                    queue.Enqueue(cue);
        }

        // The history feed is complete up to this revision.
        public void HistoryRead(long revision)
        {
            if (revision > historyRevision)
                historyRevision = revision;
        }

        public void Tick()
        {
            if (disposed || !Holding)
                return;
            var now = clock();
            if (skipped)
            {
                Release();
                return;
            }
            if (beat != null && !Advance(now))
                return;
            EndBeat();
            if (StartNext(now))
                return;
            if (historyRevision >= Latest.Revision || now - receivedAt >= HistoryWait)
                Release();
        }

        // Skip (OPPONENT_ACTIONS_UX §6.3): the beat and the backlog end now and the latest view follows on the next tick.
        // It neither answers a decision nor marks a notice read; later events present again.
        public void Skip()
        {
            if (disposed || !Holding)
                return;
            FinishBeat();
            queue.Clear();
            skipped = true;
        }

        public void SetConnection(string status)
        {
            connected = status == "Connected";
            if (!Live)
                Rebase();
        }

        public void SetFocus(bool value)
        {
            focused = value;
            if (!Live)
                Rebase();
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            FinishBeat();
            queue.Clear();
        }

        // A disconnect or a lost focus shows the latest state without any catch-up.
        private void Rebase()
        {
            queue.Clear();
            if (disposed || !Holding)
                return;
            FinishBeat();
            skipped = false;
            try
            {
                Present(Latest);
            }
            catch (Exception error)
            {
                Report(error);
                PresentedRevision = Latest.Revision;
            }
            SetHold(false);
        }

        private bool StartNext(double now)
        {
            while (queue.Count > 0)
            {
                var cue = queue.Dequeue();
                if (cue.Revision <= PresentedRevision)
                    continue;
                beat = Begin(cue, now);
                if (beat == null)
                    continue;
                Current = cue;
                return true;
            }
            return false;
        }

        private IPresentationBeat Begin(PresentationCue cue, double now)
        {
            foreach (var presenter in presenters)
            {
                try
                {
                    var started = presenter.Begin(cue, now);
                    if (started != null)
                        return started;
                }
                catch (Exception error)
                {
                    Report(error);
                }
            }
            return null;
        }

        private bool Advance(double now)
        {
            try
            {
                return beat.Advance(now);
            }
            catch (Exception error)
            {
                Report(error);
                FinishBeat();
                return true;
            }
        }

        // QUAL-09 for a deferred view: a failure is reported and the same view is applied again on the next tick, up
        // to ApplyAttempts times; then the gate opens on whatever the board could show.
        private void Release()
        {
            FinishBeat();
            queue.Clear();
            skipped = false;
            try
            {
                Present(Latest);
            }
            catch (Exception error)
            {
                Report(error);
                if (++failures < ApplyAttempts)
                    return;
                PresentedRevision = Latest.Revision;
            }
            failures = 0;
            SetHold(false);
        }

        private void Present(PlayerView view)
        {
            present(view);
            PresentedRevision = view.Revision;
        }

        private void FinishBeat()
        {
            if (beat != null)
            {
                try
                {
                    beat.Finish();
                }
                catch (Exception error)
                {
                    Report(error);
                }
            }
            EndBeat();
        }

        private void EndBeat()
        {
            beat = null;
            Current = null;
        }

        private void SetHold(bool value)
        {
            if (Holding == value)
                return;
            Holding = value;
            hold(value);
        }

        private void Report(Exception error) =>
            Diagnostics.Report(DiagnosticCategory.Presentation, Latest?.MatchId ?? "", Latest?.Revision ?? -1, error);
    }
}
