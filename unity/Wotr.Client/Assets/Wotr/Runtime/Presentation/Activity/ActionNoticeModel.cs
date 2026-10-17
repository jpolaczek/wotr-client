using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionNoticeItem
    {
        public string Id { get; }
        public string ActionId { get; }
        public string StepId { get; }
        public string BattleId { get; }
        public string HuntChainId { get; }
        public long Revision { get; }
        public int Order { get; }
        public string Text { get; }
        public string Sentence { get; }
        public string Detail { get; }
        public ActionLogStep Step { get; }
        // UI-166: a notice opens the compact result card; its Full record opens the same action in the Log.
        public ActionOpenRequest Request => new ActionOpenRequest(ActionId, StepId,
            BattleId.Length > 0 || HuntChainId.Length > 0 ? StepId : "", includeConsequences: true, compact: true);

        public ActionNoticeItem(ActionLogStep step, BoardPresentation data)
        {
            Id = step.Context.BattleId.Length > 0 ? "battle/" + step.Context.BattleId : "action/" + step.ActionId;
            ActionId = step.ActionId;
            StepId = step.Id;
            BattleId = step.Context.BattleId;
            HuntChainId = step.Context.HuntChainId;
            Revision = step.Revision;
            Order = step.Order;
            Text = ActionNoticeText.Short(step, data);
            Sentence = ActionNoticeText.Sentence(step, data);
            Detail = string.Join("\n", ActionFactText.Lines(step, data).Distinct());
            Step = step;
        }
    }

    public sealed class ActionNoticeGroup
    {
        public ActionNoticeItem Latest { get; }
        public int Count { get; }
        // UI-166: the result of the unread steps of this action (or battle), for the pennant's tooltip.
        public ActionResult Result { get; }
        internal ActionNoticeGroup(IEnumerable<ActionNoticeItem> items, BoardPresentation data)
        {
            var ordered = items.OrderByDescending(s => s.Revision).ThenByDescending(s => s.Order).ToArray();
            Latest = ordered[0];
            Count = ordered.Length;
            Result = ActionResultText.For(ordered.Select(item => item.Step), data);
        }
    }

    public sealed class ActionNoticeModel
    {
        private readonly NotificationReadStore reads;
        private readonly BoardPresentation data;
        private readonly Dictionary<string, ActionNoticeItem> pending = new Dictionary<string, ActionNoticeItem>();
        private readonly HashSet<string> seen = new HashSet<string>();
        public IReadOnlyList<ActionNoticeGroup> Groups { get; private set; } = new ActionNoticeGroup[0];
        public Side You { get; set; }
        public int StepCount => pending.Count;

        public ActionNoticeModel(NotificationReadStore reads, BoardPresentation data)
        {
            this.reads = reads;
            this.data = data;
        }

        public bool Merge(ActionLogPage page)
        {
            var changed = false;
            foreach (var step in page.Steps)
            {
                if (!step.Actor.HasValue || step.Actor == You || !seen.Add(step.Id) || reads.Steps.Contains(step.Id)) continue;
                pending[step.Id] = new ActionNoticeItem(step, data);
                changed = true;
            }
            if (changed) Rebuild();
            return changed;
        }

        public bool Displayed(ActionLogView view)
        {
            var ids = view.Steps.Where(s => s.Actor.HasValue && s.Actor != You).Select(s => s.Id).ToArray();
            reads.Acknowledge(ids);
            var changed = false;
            foreach (var id in ids) changed |= pending.Remove(id);
            if (changed) Rebuild();
            return changed;
        }

        private void Rebuild() => Groups = pending.Values.GroupBy(s => s.Id).Select(g => new ActionNoticeGroup(g, data))
            .OrderByDescending(g => g.Latest.Revision).ThenByDescending(g => g.Latest.Order).ToArray();
    }
}
