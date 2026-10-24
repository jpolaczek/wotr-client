using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-306 (OPPONENT_ACTIONS_UX §8 army march, retreat, advance; storyboard A): a recorded movement on the presented
    // board. The source stack is traced first; then only the recorded participants fly to the destination while the rear
    // guard visibly stays (the staged source shows what remains, the destination settles with its true composition); the
    // destination is traced for a short dwell. Successive movements of one source accumulate on the same staged board, so
    // the destination is never counted twice and nothing replays at the release. An empty advance moves nothing.
    // UI-309: a group with characters or Nazgûl is named beside its destination (CharacterMarks.Group). Companions leaving
    // the Fellowship fly from its figure only while that position is known, else arrive in place (FellowshipSeparation);
    // the board's own figure difference decides which.
    public sealed class ArmyMovementPresenter : ICuePresenter
    {
        public const double SourceSeconds = .35;
        public const double DwellSeconds = .6;
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly MapObservedTrace trace;

        public ArmyMovementPresenter(BoardScreen board, StagedBoard staged, MapObservedTrace trace)
        {
            this.board = board;
            this.staged = staged;
            this.trace = trace;
        }

        public static bool Shows(PresentationCue cue) => cue.Movement != null &&
            cue.Kind is PresentationCueKind.March or PresentationCueKind.Travel or PresentationCueKind.Retreat or PresentationCueKind.Advance;

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            if (!Shows(cue) || !board.Map.Data.Shapes.ContainsKey(cue.Movement.From) || !board.Map.Data.Shapes.ContainsKey(cue.Movement.To))
                return null;
            var movers = staged.Present(cue.Movement.Participants).ToList();
            var leaving = FellowshipSeparation.Leaving(cue.Movement, staged.View);
            return movers.Count + leaving.Count == 0 ? null : new Beat(this, cue.Movement, movers, leaving, now);
        }

        private bool SourceGarrison(ActionParticipant mover) =>
            staged.View.Units.FirstOrDefault(u => u.Id == mover.Id)?.Garrison ??
            staged.View.Characters.FirstOrDefault(c => c.Id == mover.Id)?.Garrison ?? false;

        // The moving stack, or the Fellowship's figure a separation leaves from — untraced while its position is unknown.
        private MapObservedTrace.TraceSet Source(ActionMovement movement, List<ActionParticipant> movers, Side side)
        {
            if (movers.Count > 0)
                return trace.Begin().Force(movement.From, side, SourceGarrison(movers[0]));
            return BoardFigureSnapshot.PositionKnown(staged.View.Fellowship, board.Map.Data.Regions.ContainsKey)
                ? trace.Begin().Fellowship(movement.From) : null;
        }

        private MapObservedTrace.TraceSet Destination(ActionMovement movement, List<ActionParticipant> group, Side side)
        {
            var garrison = group[0].Garrison;
            var set = trace.Begin().Force(movement.To, side, garrison);
            var marks = CharacterMarks.Group(group);
            return marks.Count == 0 ? set : set.Identities(movement.To, side, garrison, marks);
        }

        // The staged board before and after, as the map's figure positions, so the flight is the board's own motion.
        private void Fly(ActionMovement movement, List<ActionParticipant> movers, List<ActionParticipant> leaving)
        {
            var before = new BoardFigureSnapshot(staged.View, board.Map.Data.Regions.ContainsKey);
            staged.Separate(leaving, movement.To);
            staged.Move(movers, movement.To);
            var after = new BoardFigureSnapshot(staged.View, board.Map.Data.Regions.ContainsKey);
            var keys = movers.Concat(leaving).Select(Key).ToHashSet();
            var moves = BoardFigureChanges.Moves(before, after).Where(move => keys.Contains(move.To.Key)).ToList();
            staged.Show();
            board.Map.Figures.Fly(moves);
        }

        private static string Key(ActionParticipant mover) => (mover.UnitKind.HasValue ? "unit:" : "character:") + mover.Id;

        private sealed class Beat : IPresentationBeat
        {
            private readonly ArmyMovementPresenter owner;
            private readonly ActionMovement movement;
            private readonly List<ActionParticipant> movers;
            private readonly List<ActionParticipant> leaving;
            private readonly Side side;
            private readonly double start;
            private MapObservedTrace.TraceSet source;
            private MapObservedTrace.TraceSet destination;
            private double landed = -1;
            private bool flying;

            public Beat(ArmyMovementPresenter owner, ActionMovement movement, List<ActionParticipant> movers,
                List<ActionParticipant> leaving, double now)
            {
                this.owner = owner;
                this.movement = movement;
                this.movers = movers;
                this.leaving = leaving;
                side = movers.Concat(leaving).First().Side;
                start = now;
                source = owner.Source(movement, movers, side);
            }

            public bool Advance(double now)
            {
                if (!flying && now - start < SourceSeconds)
                    return false;
                if (!flying)
                {
                    flying = true;
                    owner.Fly(movement, movers, leaving);
                    destination = owner.Destination(movement, movers.Concat(leaving).ToList(), side);
                    return false;
                }
                if (owner.board.Map.Figures.ActiveCount > 0)
                    return false;
                if (landed < 0)
                    landed = now;
                if (now - landed < DwellSeconds)
                    return false;
                End();
                return true;
            }

            // Skip or a rebase: the latest view follows at once, without the rest of the flight.
            public void Finish()
            {
                if (flying)
                    owner.board.Map.Figures.Land();
                End();
            }

            private void End()
            {
                source?.End();
                destination?.End();
                source = destination = null;
                owner.staged.Settle();
            }
        }
    }
}
