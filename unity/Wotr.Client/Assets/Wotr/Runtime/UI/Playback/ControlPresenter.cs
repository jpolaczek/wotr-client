using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-306 (OPPONENT_ACTIONS_UX §8 retreat/siege/advance finish): a recorded control change. The settlement's gem takes the
    // new controller's colour on the staged board, with a trace; a settlement that scores shows its victory points beside
    // it — gained by the side that captures it from its original owner, lost by the side whose capture is retaken. A region
    // without a scoring settlement shows no point. UI-315 (owner review): the settlement is traced first, then its gem turns
    // and the points join it, held at the shared pace.
    public sealed class ControlPresenter : ICuePresenter
    {
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly MapObservedTrace trace;

        public ControlPresenter(BoardScreen board, StagedBoard staged, MapObservedTrace trace)
        {
            this.board = board;
            this.staged = staged;
            this.trace = trace;
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            var change = cue.Control;
            if (cue.Kind != PresentationCueKind.Control || change == null ||
                !board.Presentation.Regions.TryGetValue(change.RegionId, out var region))
                return null;
            var set = trace.Begin().Settlement(change.RegionId);
            var points = Points(region, change);
            return new StepsBeat(now, () => End(set), new List<(Action Start, double Seconds)>
            {
                (() => { }, PresentationPace.MarkSeconds),
                (() => Turn(set, change, points), PresentationPace.HoldSeconds)
            });
        }

        private void Turn(MapObservedTrace.TraceSet set, ActionControlChange change, (string Text, Side Side)? points)
        {
            staged.Control(change.RegionId, change.After);
            staged.Show();
            if (points.HasValue)
                set.Badge(change.RegionId, points.Value.Text, points.Value.Side);
        }

        private void End(MapObservedTrace.TraceSet set)
        {
            set.End();
            staged.Settle();
        }

        // The victory points this change moves, and for whom; none for a region whose settlement does not score.
        public static (string Text, Side Side)? Points(RegionDefinition region, ActionControlChange change)
        {
            if (region.VictoryPoints <= 0)
                return null;
            if (change.After.HasValue && change.After != region.OriginalOwner)
                return ("+" + region.VictoryPoints + " VP", change.After.Value);
            if (change.Before.HasValue && change.Before != region.OriginalOwner)
                return ("−" + region.VictoryPoints + " VP", change.Before.Value);
            return null;
        }
    }
}
