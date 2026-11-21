using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-304 (OPPONENT_ACTIONS_UX §7): the one playback coordinator between received views and the board. The newest
    // authoritative view (Latest) is kept apart from the one the board presents. While the cues of a newer revision play,
    // or its history is still being read (at most HistoryWait), the board keeps presenting the earlier view and every
    // command path is held; then the latest view is applied once, with its revision, private version and decision. Skip
    // drains the beat and the backlog to the latest view on the next tick, so the skip click cannot reach a control the
    // release opens. Without a registered presenter, or when the session is not live (offline, reconnecting), a view is
    // applied at once, exactly as before UI-304; cues of a presented revision are dropped, so a late history page never
    // plays an old move. Every presenter that shows a cue plays its beat in registration order (UI-305: the camera, then
    // the family's effect), on a presentation clock that stops while Paused (the player reads a card).
    // UI-490 (owner, 2026-11-04): the clock also stops while the window has no focus, and the presentation resumes where it
    // was on the player's return, with what arrived meanwhile behind it. Only a backlog over MostCuesAway is given up: the
    // latest state, as a lost focus always showed before, until the player returns.
    // UI-557 (owner, 2026-11-20: "I played The Red Arrow, it moves Rohan on the Political Track, but the animation started
    // only later, when the computer made its move"): the view of the player's own accepted command (Own, from its receipt)
    // is not coalesced into the opponent's next one. A quick answer arrived while the own view still waited for its history,
    // so the own result showed only at the end of the opponent's presentation. Now the own view is applied as soon as its
    // history is read (or waited for) and its own cues played, before any cue of a later revision begins. The opponent's
    // views still coalesce: only the newest of those is applied (UI-304).
    public sealed class PresentationPlayback : IDisposable
    {
        public const double HistoryWait = 1.0;
        // UI-490: a cue plays for 1.5–2 s at the shipped pace, so this is about half a minute of catching up on return:
        // three or four of the opponent's actions. Beyond it the player gets the latest state and the pennants.
        public const int MostCuesAway = 20;
        private const int ApplyAttempts = 3;
        private readonly Action<PlayerView> present;
        private readonly Action<bool> hold;
        private readonly Func<double> clock;
        private readonly List<ICuePresenter> presenters = new List<ICuePresenter>();
        private readonly Queue<PresentationCue> queue = new Queue<PresentationCue>();
        // UI-557: the revisions of the player's own accepted commands, and the held views that may be one of them.
        private readonly HashSet<long> own = new HashSet<long>();
        private readonly List<Held> held = new List<Held>();
        private IPresentationBeat beat;
        private int next;
        private double elapsed;
        private double? lastTick;
        private double receivedAt;
        private long historyRevision = -1;
        private bool connected, focused = true, skipped, disposed;
        // UI-490: the backlog of this absence went over the cap; everything shows at once until the player returns.
        private bool givenUp;
        private int failures;
        public PlayerView Latest { get; private set; }
        public long PresentedRevision { get; private set; } = -1;
        public PresentationCue Current { get; private set; }
        public bool Holding { get; private set; }
        // A cue is shown or waits: the Skip target exists only then, not while a quiet revision's history is read.
        public bool Presenting => Holding && (Current != null || queue.Count > 0);
        public bool Live => connected;
        public Func<bool> Paused { get; set; }
        // UI-490: a view that waits for the player's return, as it arrives or as the focus goes.
        public event Action<PlayerView> HeldAway;
        // The latest view was applied: per-presentation state (a source symbol, a camera takeover) ends here.
        public event Action Released;
        public int Queued => queue.Count;
        // UI-308: the cues still waiting behind the current one (a card's reveal looks ahead to its first effect).
        public IReadOnlyCollection<PresentationCue> Upcoming => queue;
        public ClientDiagnostics Diagnostics { get; set; } = ClientDiagnostics.Unity;

        public PresentationPlayback(Action<PlayerView> present, Action<bool> hold, Func<double> clock)
        {
            this.present = present;
            this.hold = hold;
            this.clock = clock;
        }

        public void Register(ICuePresenter presenter) => presenters.Add(presenter);

        // UI-557: the revision an accepted command of the player's own produced (its receipt's).
        public void Own(long revision)
        {
            if (!disposed && revision > PresentedRevision)
                own.Add(revision);
        }

        // A view the connection accepted. Applied at once when nothing is to be presented, so a failure still reaches
        // MatchConnection's QUAL-09 retry; otherwise it waits for its history and its cues.
        public void Receive(PlayerView view)
        {
            if (disposed || view == null)
                return;
            if (Latest == null || view.Revision != Latest.Revision)
                receivedAt = clock();
            Latest = view;
            if (!Holding && (!Live || givenUp || presenters.Count == 0 || view.Revision <= PresentedRevision))
            {
                Present(view);
                return;
            }
            Keep(view);
            SetHold(true);
            if (!focused)
                HeldAway?.Invoke(view);
        }

        public void Enqueue(IReadOnlyList<PresentationCue> cues)
        {
            if (disposed || !Live || givenUp || presenters.Count == 0)
                return;
            foreach (var cue in cues)
                if (cue.Revision > PresentedRevision)
                    queue.Enqueue(cue);
            CapBacklog();
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
            {
                lastTick = null;
                return;
            }
            var now = clock();
            if (skipped)
            {
                Release();
                return;
            }
            var reading = Paused?.Invoke() == true;
            var paused = reading || !focused;
            if (lastTick.HasValue && !paused)
                elapsed += now - lastTick.Value;
            lastTick = now;
            if (paused)
            {
                // UI-490: a quiet revision has nothing to wait for, so its decision and its alert come when they are due.
                if (!reading && !Presenting)
                    ReleaseOnceRead(now);
                return;
            }
            if (beat != null && !Advance(elapsed))
                return;
            beat = null;
            if (StartNext(elapsed))
                return;
            ReleaseOnceRead(now);
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

        // UI-490: the presentation waits for the player, and neither the time away nor a clock jump on return counts. The
        // history gets its second again on return, so a page that was late meanwhile still plays.
        public void SetFocus(bool value)
        {
            if (focused == value)
                return;
            focused = value;
            lastTick = null;
            if (value)
            {
                givenUp = false;
                receivedAt = clock();
                return;
            }
            CapBacklog();
            if (Holding)
                HeldAway?.Invoke(Latest);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            FinishBeat();
            queue.Clear();
            held.Clear();
            own.Clear();
        }

        // UI-490: a long absence does not replay minutes. Over the cap, this absence shows the latest state.
        private void CapBacklog()
        {
            if (focused || queue.Count <= MostCuesAway)
                return;
            givenUp = true;
            Rebase();
        }

        private void ReleaseOnceRead(double now)
        {
            if (historyRevision >= Latest.Revision || now - receivedAt >= HistoryWait)
            {
                Release();
                return;
            }
            // UI-557: the own result whose history is read (or waited its second) does not wait for the newer view's.
            var result = held.LastOrDefault(entry => own.Contains(entry.View.Revision) && entry.View.Revision > PresentedRevision &&
                entry.View.Revision < Latest.Revision && (entry.View.Revision <= historyRevision || now - entry.At >= HistoryWait));
            if (result != null)
                PresentOwn(result.View);
        }

        // A disconnect, or a backlog over the cap of an absence, shows the latest state without any catch-up.
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
                Forget();
            }
            SetHold(false);
            NotifyReleased();
        }

        // The next beat: of the current cue's next presenter, else of the next queued cue.
        private bool StartNext(double now)
        {
            while (Current != null || Dequeue())
            {
                while (next < presenters.Count)
                {
                    var started = Begin(presenters[next++], Current, now);
                    if (started == null)
                        continue;
                    beat = started;
                    return true;
                }
                Current = null;
            }
            return false;
        }

        private bool Dequeue()
        {
            while (queue.Count > 0)
            {
                var cue = queue.Dequeue();
                if (cue.Revision <= PresentedRevision)
                    continue;
                // UI-557: the player's own result shows before the opponent's later move begins.
                var result = OwnResult(cue.Revision - 1);
                if (result != null)
                    PresentOwn(result);
                Current = cue;
                next = 0;
                return true;
            }
            return false;
        }

        // UI-557: a held view of the player's own command, received and older than the latest, up to this revision.
        private PlayerView OwnResult(long through) => held
            .Where(entry => own.Contains(entry.View.Revision) && entry.View.Revision <= through &&
                entry.View.Revision > PresentedRevision && entry.View.Revision < Latest.Revision)
            .Select(entry => entry.View)
            .LastOrDefault();

        // UI-557: the own view is applied as if the opponent's answer had not arrived yet: its presentation's state ends
        // (Released), and the gate stays held for the newer view.
        private void PresentOwn(PlayerView view)
        {
            try
            {
                Present(view);
            }
            catch (Exception error)
            {
                Report(error);
                PresentedRevision = view.Revision;
            }
            Forget();
            NotifyReleased();
        }

        private void Keep(PlayerView view)
        {
            held.RemoveAll(entry => entry.View.Revision == view.Revision);
            held.Add(new Held(view, clock()));
        }

        // Everything up to the presented revision is behind the board now.
        private void Forget()
        {
            held.RemoveAll(entry => entry.View.Revision <= PresentedRevision);
            own.RemoveWhere(revision => revision <= PresentedRevision);
        }

        private sealed class Held
        {
            public PlayerView View { get; }
            public double At { get; }

            public Held(PlayerView view, double at)
            {
                View = view;
                At = at;
            }
        }

        private IPresentationBeat Begin(ICuePresenter presenter, PresentationCue cue, double now)
        {
            try
            {
                return presenter.Begin(cue, now);
            }
            catch (Exception error)
            {
                Report(error);
                return null;
            }
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
                Forget();
            }
            failures = 0;
            SetHold(false);
            NotifyReleased();
        }

        private void NotifyReleased()
        {
            try
            {
                Released?.Invoke();
            }
            catch (Exception error)
            {
                Report(error);
            }
        }

        private void Present(PlayerView view)
        {
            present(view);
            PresentedRevision = view.Revision;
            Forget();
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
            beat = null;
            Current = null;
            next = 0;
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
