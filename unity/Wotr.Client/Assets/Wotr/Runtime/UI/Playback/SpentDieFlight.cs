using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-487 (owner, 2026-11-04: "it must be clear, not slow"): the action's die leaves. Its last move is the action's last
    // beat: a copy of the die, wearing the face it left with, flies from its place in the row (or the Hunt plaque, for an Eye
    // paid from the Box) into the count where it now lies — the Spent or Out chip, or a Hunt Box count — and that count
    // steps as it lands. The ring ended with the cue (CostEmphasis) and the row closes only at the landing, so no other die
    // ever stands in the ring. A count the viewer cannot see (a die in the opponent's Fellowship Box) takes no time. Under
    // Skip animations nothing flies: the count steps with its own pulse, lit for a moment. Skip ends it at the latest state.
    public sealed class SpentDieFlight
    {
        public const string PieceClass = "presentation-die-flight";
        public const double StillSeconds = .35;
        private static readonly Vector2 Size = new Vector2(29, 29);
        // About 0.7 s from the row to the count: an extra beat on every action of the game.
        private static readonly FlightPace Pace = new FlightPace(0, .08, .42, .2, 0);
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly PresentationMarks marks;
        private readonly PresentationLayer layer;

        public SpentDieFlight(BoardScreen board, StagedBoard staged, PresentationMarks marks, PresentationLayer layer)
        {
            this.board = board;
            this.staged = staged;
            this.marks = marks;
            this.layer = layer;
        }

        public IPresentationBeat Begin(ActionDieChange die, double now)
        {
            if (!PresentationTargets.Shown(Count(die)))
            {
                Step(die);
                return null;
            }
            return board.ReducedMotion() ? Still(die, now) : Flight(die, now);
        }

        private IPresentationBeat Flight(ActionDieChange die, double now)
        {
            var row = Row(die);
            return new DrawFlight(now, board.Dissolve.Root, () => Count(die), new[] { Origin(die, row) },
                () => marks.Place(() => marks.Light(Count(die))),
                _ => Land(die),
                () => { },
                () => End(row),
                PieceClass, Size, piece => Leave(piece, die, row), Pace);
        }

        private IPresentationBeat Still(ActionDieChange die, double now) =>
            new StepsBeat(now, marks.End, new List<(Action Start, double Seconds)> { (() => Land(die), StillSeconds) });

        private VisualElement Row(ActionDieChange die)
        {
            var row = board.DicePool(die.Owner).Q<Button>(die.Id);
            return PresentationTargets.Shown(row) ? row : null;
        }

        // Where the die stands: its row, the Hunt plaque it was paid from, or its cost copy.
        private VisualElement Origin(ActionDieChange die, VisualElement row) => PresentationTargets.First(row,
            die.From == DieLocation.Hunt ? board.Hunt.Box.Count(die.Owner) : null, layer.Root.Q(CostEmphasis.MarkName));

        // The piece leaves: it wears the die's face, and the row keeps the die's place empty until the landing.
        private static void Leave(VisualElement piece, ActionDieChange die, VisualElement row)
        {
            piece.AddToClassList(die.Owner == Side.FreePeoples ? "die-free" : "die-shadow");
            piece.Add(new DieFaceIcon(die.After ?? die.Before));
            if (row != null)
                row.style.visibility = Visibility.Hidden;
        }

        private void Land(ActionDieChange die)
        {
            Step(die);
            marks.Add(() =>
            {
                var count = Count(die);
                if (!PresentationGlow.IsLit(count))
                    marks.Light(count);
            });
        }

        private void Step(ActionDieChange die)
        {
            staged.Die(die);
            board.StageDice(die.Owner, staged.View);
            board.StageOpponentSummary(staged.View);
            if (die.To == DieLocation.Hunt || die.From == DieLocation.Hunt)
                board.StageHunt(staged.View);
        }

        // Skip or the beat's end: a die that did not land stands again until the release redraws the row.
        private void End(VisualElement row)
        {
            if (row != null)
                row.style.visibility = StyleKeyword.Null;
            marks.End();
        }

        // The count the top bar shows for the opponent, the pool's own for the side an observer watches from.
        private VisualElement Count(ActionDieChange die) => die.To switch
        {
            DieLocation.Hunt => board.Hunt.Box.Count(die.Owner),
            DieLocation.Spent => Chip(die.Owner, "opponent-spent", "pool-Spent"),
            DieLocation.Discarded or DieLocation.Removed => Chip(die.Owner, "opponent-out", "pool-Discarded"),
            DieLocation.FellowshipBox => board.Shown(die.Owner) ? board.OwnDice.Q("pool-FellowshipBox") : null,
            _ => null
        };

        private VisualElement Chip(Side owner, string opponent, string own) =>
            board.Shown(owner) ? board.OwnDice.Q(own) : board.TopBar.Opponent.Root.Q(opponent);
    }
}
