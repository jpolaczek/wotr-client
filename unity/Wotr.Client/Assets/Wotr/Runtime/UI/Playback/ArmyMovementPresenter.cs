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
            return movers.Count == 0 ? null : new Beat(this, cue.Movement, movers, now);
        }

        private bool SourceGarrison(ActionParticipant mover) =>
            staged.View.Units.FirstOrDefault(u => u.Id == mover.Id)?.Garrison ??
            staged.View.Characters.FirstOrDefault(c => c.Id == mover.Id)?.Garrison ?? false;

        // The staged board before and after, as the map's figure positions, so the flight is the board's own motion.
        private List<BoardFigureMove> Fly(ActionMovement movement, List<ActionParticipant> movers)
        {
            var before = new BoardFigureSnapshot(staged.View, board.Map.Data.Regions.ContainsKey);
            staged.Move(movers, movement.To);
            var after = new BoardFigureSnapshot(staged.View, board.Map.Data.Regions.ContainsKey);
            var moves = movers.Select(Key)
                .Where(key => before.Figures.ContainsKey(key) && after.Figures.ContainsKey(key))
                .Select(key => new BoardFigureMove(before.Figures[key], after.Figures[key]))
                .ToList();
            staged.Show();
            board.Map.Figures.Fly(moves);
            return moves;
        }

        private static string Key(ActionParticipant mover) => (mover.UnitKind.HasValue ? "unit:" : "character:") + mover.Id;

        private sealed class Beat : IPresentationBeat
        {
            private readonly ArmyMovementPresenter owner;
            private readonly ActionMovement movement;
            private readonly List<ActionParticipant> movers;
            private readonly Side side;
            private readonly bool sourceGarrison;
            private readonly double start;
            private MapObservedTrace.TraceSet source;
            private MapObservedTrace.TraceSet destination;
            private double landed = -1;
            private bool flying;

            public Beat(ArmyMovementPresenter owner, ActionMovement movement, List<ActionParticipant> movers, double now)
            {
                this.owner = owner;
                this.movement = movement;
                this.movers = movers;
                side = movers[0].Side;
                sourceGarrison = owner.SourceGarrison(movers[0]);
                start = now;
                source = owner.trace.Begin().Force(movement.From, side, sourceGarrison);
            }

            public bool Advance(double now)
            {
                if (!flying && now - start < SourceSeconds)
                    return false;
                if (!flying)
                {
                    flying = true;
                    owner.Fly(movement, movers);
                    destination = owner.trace.Begin().Force(movement.To, side, movers[0].Garrison);
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
