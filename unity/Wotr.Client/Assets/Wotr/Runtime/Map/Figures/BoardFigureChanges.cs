using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BoardFigureMove
    {
        public BoardFigurePosition From { get; }
        public BoardFigurePosition To { get; }
        public bool IsArrival => From.Region == To.Region && From.Garrison == To.Garrison;
        public BoardFigureMove(BoardFigurePosition from, BoardFigurePosition to)
        {
            From = from;
            To = to;
        }
    }

    public sealed class BoardFigureChanges
    {
        public BoardFigureSnapshot Current { get; private set; }
        public bool Connected { get; private set; }
        private static readonly IReadOnlyList<BoardFigureMove> none = Array.Empty<BoardFigureMove>();

        public void SetConnected(bool connected)
        {
            Connected = connected;
            if (!connected)
                Current = null;
        }

        public void Reset() => Current = null;

        public IReadOnlyList<BoardFigureMove> Apply(PlayerView view, Func<string, bool> isRegion)
        {
            var next = new BoardFigureSnapshot(view, isRegion);
            var previous = Current;
            var sameGame = previous?.MatchId == next.MatchId && previous.Viewer == next.Viewer;
            if (sameGame && next.Revision < previous.Revision)
                return none;
            Current = next;
            if (!Connected)
                return none;
            return Difference(previous, next);
        }

        public static IReadOnlyList<BoardFigureMove> Difference(BoardFigureSnapshot previous, BoardFigureSnapshot next)
        {
            if (previous == null || previous.MatchId != next.MatchId || previous.Viewer != next.Viewer ||
                next.Revision != previous.Revision + 1)
                return none;
            var moves = new List<BoardFigureMove>();
            foreach (var position in next.Figures.Values)
            {
                var known = previous.Figures.TryGetValue(position.Key, out var before);
                // UI-130: a figure new to the board (a recruit from the reserve, a character entering play) arrives
                // where it now stands; only a separating companion travels from the Fellowship's public region.
                if (!known)
                    before = SeparationSource(previous, next, position) ?? position;
                if (before != null && (!known || before.Region != position.Region || before.Garrison != position.Garrison))
                    moves.Add(new BoardFigureMove(before, position));
            }
            return moves;
        }

        private static BoardFigurePosition SeparationSource(BoardFigureSnapshot before,
            BoardFigureSnapshot after, BoardFigurePosition figure)
        {
            if (figure.Side != Side.FreePeoples || figure.Identity.Length == 0 || before.FellowshipRegion.Length == 0 ||
                !before.ContainedCompanion(figure.Identity) || after.ContainedCompanion(figure.Identity))
                return null;
            // Both the old public fellowship identity and the newly public figure are required.
            return before.FellowshipPositionKnown
                ? new BoardFigurePosition(figure.Key, before.FellowshipRegion, figure.Side, false, figure.Appearance, figure.Identity)
                : figure;
        }
    }
}
