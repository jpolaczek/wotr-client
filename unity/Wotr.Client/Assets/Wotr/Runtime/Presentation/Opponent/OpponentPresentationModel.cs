using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-303 (OPPONENT_ACTIONS_UX §7, §9): the opponent's recorded actions as presentation cues, built incrementally from the
    // authorized All history feed. The history already on the board at first load is remembered but never played; a step
    // newer than the shown view waits for it; a repeated step is ignored; the steps of one fetch are ordered by revision and
    // order before they become cues, so page boundaries and arrival order cannot change the cues. The model never reads or
    // marks notifications and never sends anything.
    public sealed class OpponentPresentationModel
    {
        private readonly BoardPresentation data;
        private readonly HashSet<string> seen = new HashSet<string>();
        private readonly List<ActionLogStep> held = new List<ActionLogStep>();
        private readonly Dictionary<string, Side?> initiators = new Dictionary<string, Side?>();
        private readonly Dictionary<string, ActionMemory> memories = new Dictionary<string, ActionMemory>();
        private readonly Dictionary<string, ActionPresentation> actions = new Dictionary<string, ActionPresentation>();
        public Side You { get; }
        // UI-330: an observer's model — every side's steps are presented, none is the viewer's own.
        public bool Everyone { get; }
        public long ViewRevision { get; private set; } = -1;
        public IReadOnlyDictionary<string, ActionPresentation> Actions => actions;
        public int Held => held.Count;

        public OpponentPresentationModel(Side you, BoardPresentation data, bool everyone = false)
        {
            You = you;
            Everyone = everyone;
            this.data = data;
        }

        // First load, reconnect or a jump: the state is shown as it is (no catch-up), but continuations of these actions
        // keep their source and do not show the same consequence again.
        public void Baseline(IEnumerable<ActionLogPage> pages, long viewRevision)
        {
            var steps = Unseen(pages).ToList();
            held.AddRange(steps.Where(step => step.Revision > viewRevision));
            Build(steps.Where(step => step.Revision <= viewRevision).ToList());
            ViewRevision = System.Math.Max(ViewRevision, viewRevision);
        }

        // The pages of one fetch; returns the cues they made presentable, in causal order.
        public IReadOnlyList<PresentationCue> Merge(IEnumerable<ActionLogPage> pages)
        {
            held.AddRange(Unseen(pages));
            return Release();
        }

        // The board now shows this revision: the facts up to it become presentable.
        public IReadOnlyList<PresentationCue> Advance(long viewRevision)
        {
            if (viewRevision > ViewRevision)
                ViewRevision = viewRevision;
            return Release();
        }

        private IEnumerable<ActionLogStep> Unseen(IEnumerable<ActionLogPage> pages)
        {
            var list = pages.ToList();
            foreach (var summary in list.SelectMany(page => page.Actions))
                if (summary.Initiator.HasValue || !initiators.ContainsKey(summary.Id))
                    initiators[summary.Id] = summary.Initiator;
            return list.SelectMany(page => page.Steps).Where(step => seen.Add(step.Id)).ToList();
        }

        private IReadOnlyList<PresentationCue> Release()
        {
            var ready = held.Where(step => step.Revision <= ViewRevision).ToList();
            held.RemoveAll(step => step.Revision <= ViewRevision);
            return Build(ready);
        }

        private List<PresentationCue> Build(List<ActionLogStep> steps)
        {
            var cues = new List<PresentationCue>();
            var own = steps.Where(step => !Everyone && step.Actor == You).Select(step => step.Revision).ToHashSet();
            var groups = steps.GroupBy(step => (step.Revision, step.ActionId))
                .OrderBy(group => group.Key.Revision).ThenBy(group => group.Min(step => step.Order));
            foreach (var group in groups)
            {
                var all = group.ToList();
                var shown = (Everyone ? PresentationSelection.Every(all)
                    : PresentationSelection.Select(all, Initiator(group.Key.ActionId, all), You)).ToHashSet();
                var released = PresentationCueExtraction.Cues(all, shown, Memory(group.Key.ActionId), Everyone ? null : You, data).ToList();
                if (released.Count == 0)
                    continue;
                var presentation = Presentation(group.Key.ActionId);
                foreach (var cue in released)
                {
                    cue.OwnCommand = own.Contains(cue.Revision);
                    presentation.Add(cue);
                }
                cues.AddRange(released);
            }
            return cues;
        }

        // The action's summary names who started it; without one, the earliest acting step does.
        private Side? Initiator(string actionId, List<ActionLogStep> group)
        {
            if (initiators.TryGetValue(actionId, out var known) && known.HasValue)
                return known;
            var first = group.OrderBy(step => step.Order).FirstOrDefault(step => step.Actor.HasValue)?.Actor;
            if (first.HasValue)
                initiators[actionId] = first;
            return first;
        }

        private ActionMemory Memory(string actionId)
        {
            if (!memories.TryGetValue(actionId, out var memory))
                memories[actionId] = memory = new ActionMemory();
            return memory;
        }

        private ActionPresentation Presentation(string actionId)
        {
            if (!actions.TryGetValue(actionId, out var presentation))
                actions[actionId] = presentation = new ActionPresentation(actionId);
            presentation.Initiator ??= initiators.GetValueOrDefault(actionId);
            return presentation;
        }
    }
}
