using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // REPLAY-05: the Log of a replay holds only what was recorded up to the step shown, from the side shown. It reads
    // the history chunk holding that point and the one before it (Load older adds earlier chunks), so nothing from
    // later in the game appears and only a few chunks are kept.
    public sealed class ReplayHistory
    {
        private const int ChunksKept = 4;
        private readonly ReplayArchive archive;
        private readonly ActionLogPanel panel;
        private readonly Dictionary<(Side, int), ActionLogPage> chunks = new Dictionary<(Side, int), ActionLogPage>();
        private readonly Queue<(Side, int)> order = new Queue<(Side, int)>();
        private readonly Dictionary<(Side, int), long> firstRevision = new Dictionary<(Side, int), long>();
        private Side side;
        private long cutoff = -1;
        private int oldest;

        public ReplayHistory(ReplayArchive archive, ActionLogPanel panel)
        {
            this.archive = archive;
            this.panel = panel;
            panel.LoadOlder += Older;
            panel.FilterChanged += _ => Rebuild();
        }

        public void Show(Side viewer, long revision)
        {
            if (viewer == side && revision == cutoff)
                return;
            side = viewer;
            cutoff = revision;
            oldest = System.Math.Max(0, Last() - 1);
            Rebuild();
        }

        // The last chunk recorded at or before the cutoff.
        private int Last()
        {
            var count = archive.HistoryChunks(side);
            var found = 0;
            for (var chunk = 0; chunk < count; chunk++)
            {
                if (First(chunk) > cutoff)
                    break;
                found = chunk;
            }
            return found;
        }

        private long First(int chunk)
        {
            if (!firstRevision.TryGetValue((side, chunk), out var revision))
            {
                var steps = Page(chunk).Steps;
                firstRevision[(side, chunk)] = revision = steps.Count == 0 ? long.MaxValue : steps.Min(s => s.Revision);
            }
            return revision;
        }

        private void Older()
        {
            if (oldest == 0)
                return;
            oldest--;
            Rebuild();
        }

        // The page of the shown chunks cut at the step shown, filtered by the Log's choice.
        public ActionLogPage Visible() => Visible(true);

        private ActionLogPage Visible(bool filtered)
        {
            var steps = new List<ActionLogStep>();
            var actions = new Dictionary<string, ActionLogSummary>();
            if (archive.HistoryChunks(side) == 0)
                return new ActionLogPage { MatchId = archive.Manifest.MatchId };
            for (var chunk = oldest; chunk <= Last(); chunk++)
            {
                var page = Page(chunk);
                steps.AddRange(page.Steps.Where(s => s.Revision <= cutoff && (!filtered || Matches(s))));
                foreach (var action in page.Actions)
                    actions[action.Id] = action;
            }
            var shown = steps.Select(s => s.ActionId).Distinct().Where(actions.ContainsKey)
                .Select(id => Cut(actions[id], steps.Count(s => s.ActionId == id))).ToList();
            return new ActionLogPage { MatchId = archive.Manifest.MatchId, Steps = steps, Actions = shown, NextCursor = oldest > 0 ? "older" : "" };
        }

        // The last entries up to the step shown, for the view's own short history.
        public List<HistoryEntry> Recent(int count)
        {
            var steps = Visible(false).Steps.OrderBy(s => s.Revision).ThenBy(s => s.Order).ToList();
            return steps.Skip(System.Math.Max(0, steps.Count - count))
                .Select(s => new HistoryEntry { Revision = s.Revision, Kind = s.Kind, Text = s.Text }).ToList();
        }

        private bool Matches(ActionLogStep step) => panel.Filter switch
        {
            ActionLogFilter.You => step.Actor == side,
            ActionLogFilter.Opponent => step.Actor.HasValue && step.Actor != side,
            _ => true
        };

        // An action seen part-way shows the steps recorded so far as the whole of it.
        private static ActionLogSummary Cut(ActionLogSummary action, int visible) => new ActionLogSummary
        {
            Id = action.Id, Version = action.Version, Initiator = action.Initiator, Command = action.Command, Turn = action.Turn,
            PartialHistory = action.PartialHistory, Legacy = action.Legacy, Context = action.Context, Costs = action.Costs,
            Status = action.Status, StepCount = System.Math.Min(action.StepCount, visible)
        };

        private void Rebuild()
        {
            if (cutoff < 0)
                return;
            var page = Visible();
            var model = new ActionLogModel();
            model.Merge(page);
            model.Older = page.NextCursor;
            model.Loaded = true;
            panel.Bind(model);
            panel.Changed();
            panel.SetStatus(model.StepCount == 0 ? "No recorded actions yet." : "", false, false);
        }

        private ActionLogPage Page(int chunk)
        {
            if (chunks.TryGetValue((side, chunk), out var page))
                return page;
            page = archive.History(side, chunk);
            chunks[(side, chunk)] = page;
            order.Enqueue((side, chunk));
            while (order.Count > ChunksKept)
                chunks.Remove(order.Dequeue());
            return page;
        }
    }
}
