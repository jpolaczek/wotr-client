using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-328 (owner 2026-10-25: "we need the same animations that show who does what for replay playback too, when the user
    // clicks Play"): the presentation of a replay's Play. Like an observer's (UI-330) it shows every side's actions from the
    // side shown, with only that side's hidden facts: its cues come from that side's recorded history in the archive. A quiet
    // show (the first view, a manual move, a change of side) starts it over at that step; Play hands it each next step's
    // view with the steps recorded since the step before, and the presenters play them before the view is applied.
    public sealed class ReplayPresentation : IDisposable
    {
        // How far back a new start remembers the recorded steps, so an action under way keeps its source and is not shown
        // again.
        private const long BaselineRevisions = 200;
        private const int ChunksKept = 4;
        private readonly ReplayArchive archive;
        private readonly BoardScreen board;
        private readonly Action<PlayerView> apply;
        private readonly Dictionary<(Side, int), ActionLogPage> chunks = new Dictionary<(Side, int), ActionLogPage>();
        private readonly Queue<(Side, int)> order = new Queue<(Side, int)>();
        private readonly Dictionary<(Side, int), (long First, long Last)> spans = new Dictionary<(Side, int), (long, long)>();
        private OpponentPresentationModel model;
        private PlaybackBinding binding;
        private OpponentPresenters presenters;
        private Side side;
        private long revision = -1;
        private IPresentationSounds sounds;

        public ReplayPresentation(ReplayArchive archive, BoardScreen board, Action<PlayerView> apply)
        {
            this.archive = archive;
            this.board = board;
            this.apply = apply;
        }

        public PresentationPlayback Playback => binding?.Playback;
        public bool Holding => binding?.Playback.Holding == true;

        // A new point of reference: the view at this revision is shown as it is (by the session); later steps are presented.
        public void Restart(Side shown, long at, IPresentationSounds sounds)
        {
            Stop();
            side = shown;
            revision = at;
            this.sounds = sounds;
        }

        // The pipeline starts with the first step Play presents, from the last point of reference; a board on no panel has
        // nothing to animate, so its steps are applied at once.
        private bool Start()
        {
            if (binding != null)
                return true;
            if (board.Root.panel == null || revision < 0)
                return false;
            model = new OpponentPresentationModel(side, board.Presentation, everyone: true);
            model.Baseline(new[] { Recorded(side, revision - BaselineRevisions, revision) }, revision);
            binding = new PlaybackBinding(board, null, apply, _ => { });
            presenters = new OpponentPresenters(board, binding);
            presenters.Sounds.Target = sounds;
            binding.Playback.SetConnection("Connected");
            return true;
        }

        // Play: the next step's view, presented with what the side shown recorded since the step before.
        public void Present(PlayerView view)
        {
            if (!Start())
            {
                revision = view.Revision;
                apply(view);
                return;
            }
            var cues = new List<PresentationCue>();
            cues.AddRange(model.Merge(new[] { Recorded(side, revision, view.Revision) }));
            cues.AddRange(model.Advance(view.Revision));
            revision = view.Revision;
            binding.Receive(view);
            binding.Playback.Enqueue(cues);
            binding.Playback.HistoryRead(view.Revision);
        }

        // Pause: the beat that plays ends where it would end, and the step's view follows.
        public void Skip() => binding?.Playback.Skip();

        public void Dispose() => Stop();

        private void Stop()
        {
            presenters?.Dispose();
            binding?.Dispose();
            presenters = null;
            binding = null;
            model = null;
        }

        // The steps the side shown recorded after one revision, up to another, with their actions.
        private ActionLogPage Recorded(Side shown, long after, long upTo)
        {
            var steps = new List<ActionLogStep>();
            var actions = new Dictionary<string, ActionLogSummary>();
            for (var chunk = 0; chunk < archive.HistoryChunks(shown); chunk++)
            {
                var span = Span(shown, chunk);
                if (span.Last <= after || span.First > upTo)
                    continue;
                var page = Page(shown, chunk);
                steps.AddRange(page.Steps.Where(step => step.Revision > after && step.Revision <= upTo));
                foreach (var action in page.Actions)
                    actions[action.Id] = action;
            }
            var used = steps.Select(step => step.ActionId).ToHashSet();
            return new ActionLogPage
            {
                MatchId = archive.Manifest.MatchId, Steps = steps,
                Actions = actions.Values.Where(action => used.Contains(action.Id)).ToList()
            };
        }

        private (long First, long Last) Span(Side shown, int chunk)
        {
            if (spans.TryGetValue((shown, chunk), out var span))
                return span;
            var steps = Page(shown, chunk).Steps;
            span = steps.Count == 0 ? (long.MaxValue, long.MinValue) : (steps.Min(step => step.Revision), steps.Max(step => step.Revision));
            spans[(shown, chunk)] = span;
            return span;
        }

        private ActionLogPage Page(Side shown, int chunk)
        {
            if (chunks.TryGetValue((shown, chunk), out var page))
                return page;
            page = archive.History(shown, chunk);
            chunks[(shown, chunk)] = page;
            order.Enqueue((shown, chunk));
            while (order.Count > ChunksKept)
                chunks.Remove(order.Dequeue());
            return page;
        }
    }
}
