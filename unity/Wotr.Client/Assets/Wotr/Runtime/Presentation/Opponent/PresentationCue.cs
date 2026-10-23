using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-303 (OPPONENT_ACTIONS_UX §9): what a presentation can show of one recorded step. The kinds follow the families of
    // the specification; a playback coordinator (UI-304) and the object presenters (UI-305–UI-312) interpret them.
    public enum PresentationCueKind
    {
        Cost, Card, March, Travel, Attack, Retreat, Advance, Recruit, Upgrade, Removal, Entry, Transformation, Relocation,
        Politics, Control, Fellowship, Hunt, Battle, Losses, Die, Resource, Draw, Outcome
    }

    // UI-303: the immutable cue contract. One visible result of a recorded step, its place in the action and the authorized
    // facts that stage it. Payloads are copies owned by the cue, so a later page or view cannot change a cue already
    // queued; a cue never carries a private identity (an unrevealed card, a draw, a hidden route).
    public sealed class PresentationCue
    {
        public string Id { get; }
        public PresentationCueKind Kind { get; }
        public string ActionId { get; }
        public string StepId { get; }
        public long Revision { get; }
        public int Order { get; }
        public Side? Actor { get; }
        public string BattleId { get; }
        public string HuntChainId { get; }
        public IReadOnlyList<string> Regions { get; }
        public ActionCost Cost { get; }
        public ActionCardFact Card { get; }
        public ActionMovement Movement { get; }
        public IReadOnlyList<ActionFigureChange> Figures { get; }
        public ActionNationChange Nation { get; }
        public ActionControlChange Control { get; }
        public HuntOutcomeView FellowshipBefore { get; }
        public HuntOutcomeView FellowshipAfter { get; }
        public HuntReportEntryView Hunt { get; }
        public BattleView Battle { get; }
        public IReadOnlyList<ActionLossFact> Losses { get; }
        public ActionDieChange Die { get; }
        public ActionResourceChange Resource { get; }
        public ActionOutcome Outcome { get; }

        internal PresentationCue(ActionLogStep step, PresentationCueKind kind, string key, CuePayload payload)
        {
            Id = step.ActionId + "/" + kind + "/" + key;
            Kind = kind;
            ActionId = step.ActionId;
            StepId = step.Id;
            Revision = step.Revision;
            Order = step.Order;
            Actor = step.Actor;
            BattleId = step.Context.BattleId;
            HuntChainId = step.Context.HuntChainId;
            Regions = payload.Regions.Where(region => region.Length > 0).Distinct().ToArray();
            Cost = CueCopy.Of(payload.Cost);
            Card = CueCopy.Of(payload.Card);
            Movement = CueCopy.Of(payload.Movement);
            Figures = payload.Figures.Select(CueCopy.Of).ToArray();
            Nation = CueCopy.Of(payload.Nation);
            Control = CueCopy.Of(payload.Control);
            FellowshipBefore = CueCopy.Of(payload.FellowshipBefore);
            FellowshipAfter = CueCopy.Of(payload.FellowshipAfter);
            Hunt = CueCopy.Of(payload.Hunt);
            Battle = CueCopy.Of(payload.Battle);
            Losses = payload.Losses.Select(CueCopy.Of).ToArray();
            Die = CueCopy.Of(payload.Die);
            Resource = CueCopy.Of(payload.Resource);
            Outcome = CueCopy.Of(payload.Outcome);
        }

        public override string ToString() => Id;
    }

    // What one cue is made of while it is being built; never handed out.
    internal sealed class CuePayload
    {
        public List<string> Regions { get; } = new List<string>();
        public ActionCost Cost { get; set; }
        public ActionCardFact Card { get; set; }
        public ActionMovement Movement { get; set; }
        public List<ActionFigureChange> Figures { get; } = new List<ActionFigureChange>();
        public ActionNationChange Nation { get; set; }
        public ActionControlChange Control { get; set; }
        public HuntOutcomeView FellowshipBefore { get; set; }
        public HuntOutcomeView FellowshipAfter { get; set; }
        public HuntReportEntryView Hunt { get; set; }
        public BattleView Battle { get; set; }
        public List<ActionLossFact> Losses { get; } = new List<ActionLossFact>();
        public ActionDieChange Die { get; set; }
        public ActionResourceChange Resource { get; set; }
        public ActionOutcome Outcome { get; set; }
    }

    // A cue copies its contract objects once, when it is made; a handful of small objects per revision (a battle snapshot is
    // the largest, a few kilobytes), never per frame.
    internal static class CueCopy
    {
        public static T Of<T>(T value) where T : class =>
            value == null ? null : JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    }
}
