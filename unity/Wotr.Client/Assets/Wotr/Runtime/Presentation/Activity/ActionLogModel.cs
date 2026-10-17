using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionLogRow
    {
        public string Id { get; }
        public ActionLogView Group { get; }
        public ActionLogStep Step { get; }
        public bool TurnHeading { get; }
        public ActionLogRow(ActionLogView group, ActionLogStep step, bool turnHeading = false)
        {
            Group = group;
            Step = step;
            Id = step?.Id ?? group.Action.Id;
            TurnHeading = turnHeading;
        }
    }

    // One filter's visible history. No PlayerView, game decisions or hidden state is cached here.
    public sealed class ActionLogModel
    {
        private readonly Dictionary<string, ActionLogView> groups = new Dictionary<string, ActionLogView>();
        private readonly HashSet<string> steps = new HashSet<string>();
        private readonly HashSet<string> expanded = new HashSet<string>();
        public IReadOnlyList<ActionLogRow> Rows { get; private set; } = Array.Empty<ActionLogRow>();
        public string Older { get; set; } = "";
        public string Latest { get; set; } = "";
        public bool Loaded { get; set; }
        public int StepCount => steps.Count;
        public string Anchor { get; set; } = "";
        public float AnchorOffset { get; set; }
        public bool HasNew { get; set; }
        public ActionLogView Find(string id) => groups.TryGetValue(id, out var group) ? group : null;
        public bool Expanded(string id) => expanded.Contains(id);

        public int Merge(ActionLogPage page)
        {
            var before = steps.Count;
            foreach (var header in page.Actions)
            {
                if (!groups.TryGetValue(header.Id, out var group))
                    groups[header.Id] = new ActionLogView { MatchId = page.MatchId, Action = header };
                else if (header.StepCount >= group.Action.StepCount)
                    group.Action = header;
            }
            var touched = new HashSet<string>();
            foreach (var step in page.Steps)
            {
                if (!groups.TryGetValue(step.ActionId, out var group) || !steps.Add(step.Id))
                    continue;
                group.Steps.Add(step);
                touched.Add(step.ActionId);
            }
            foreach (var id in touched)
                groups[id].Steps.Sort(Compare);
            RebuildRows();
            return steps.Count - before;
        }

        public void Details(ActionLogView view) => Merge(new ActionLogPage
        {
            MatchId = view.MatchId, Actions = new List<ActionLogSummary> { view.Action }, Steps = view.Steps
        });

        public void Toggle(string id)
        {
            if (!expanded.Add(id))
                expanded.Remove(id);
            RebuildRows();
        }

        private void RebuildRows()
        {
            var rows = new List<ActionLogRow>();
            int? turn = null;
            foreach (var group in groups.Values.Where(g => g.Steps.Count > 0)
                .OrderByDescending(g => g.Steps[g.Steps.Count - 1].Revision)
                .ThenByDescending(g => g.Steps[g.Steps.Count - 1].Order))
            {
                rows.Add(new ActionLogRow(group, null, rows.Count == 0 || turn != group.Action.Turn));
                turn = group.Action.Turn;
                if (expanded.Contains(group.Action.Id))
                    rows.AddRange(group.Steps.Select(s => new ActionLogRow(group, s)));
            }
            Rows = rows;
        }

        public int AnchorIndex()
        {
            for (var i = 0; i < Rows.Count; i++)
                if (Rows[i].Id == Anchor)
                    return i;
            return 0;
        }

        private static int Compare(ActionLogStep a, ActionLogStep b)
        {
            var revision = a.Revision.CompareTo(b.Revision);
            return revision != 0 ? revision : a.Order.CompareTo(b.Order);
        }
    }
}
