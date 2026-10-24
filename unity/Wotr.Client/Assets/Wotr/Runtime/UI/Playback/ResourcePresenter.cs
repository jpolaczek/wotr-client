using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-312 (OPPONENT_ACTIONS_UX §8 rings/dice/drawing and pass/discard/decline): the opponent's resources at their own
    // HUD objects — never as a control, a popup or a sentence.
    // - A Ring's cost rings its socket in the console; the Ring's new holder shows at the release (its own pulse).
    // - The opponent's die changes of one step are one beat in its pool: each changed die is ringed, a face change shows
    //   its old → new face under it, dice gone to the Hunt Box ring the Hunt plaque as its count rises. The beat is
    //   silent: the released view sounds the roll, so a decision passed with it keeps one tone, the more important one
    //   (AUDIO-05).
    // - Consecutive draws or discards are one beat at the opponent's hand count: card backs of their decks and the signed
    //   count, never an identity.
    // Passes, declines and "No card" record nothing to show and play no scene; a forfeited die keeps its cost copy
    // (CostEmphasis). None of this reads or marks a notice.
    // UI-315 (owner review): at the shared pace — the Ring's socket glows before its die's beat; the dice and the hand
    // count glow first with their old values, then change while lit and hold, so a draw shows the counter lighting up
    // before its number changes.
    public sealed class ResourcePresenter : ICuePresenter
    {
        public const int MostBacks = 4;

        public const string PlateName = "presentation-resource-plate";
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly PresentationPlayback playback;
        private readonly PresentationMarks marks;
        private readonly HashSet<string> grouped = new HashSet<string>();

        public ResourcePresenter(BoardScreen board, StagedBoard staged, PresentationPlayback playback, PresentationLayer layer)
        {
            this.board = board;
            this.staged = staged;
            this.playback = playback;
            marks = new PresentationMarks(layer, PlateName);
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            marks.End();
            if (grouped.Remove(cue.Id))
                return null;
            return cue.Kind switch
            {
                PresentationCueKind.Cost when cue.Cost?.Kind == ActionCostKind.Ring => Ring(cue, now),
                PresentationCueKind.Die when cue.Die != null => Dice(cue, now),
                PresentationCueKind.Draw when cue.Card != null => Cards(cue, now),
                _ => null
            };
        }

        public void Released()
        {
            marks.End();
            grouped.Clear();
        }

        private IPresentationBeat Ring(PresentationCue cue, double now)
        {
            var socket = board.Console.Rings.For(cue.Cost.Id);
            if (socket == null)
                return null;
            marks.Place(() => marks.Light(socket));
            return new TimedBeat(now, PresentationPace.MarkSeconds);
        }

        private IPresentationBeat Dice(PresentationCue cue, double now)
        {
            var changes = new List<ActionDieChange> { cue.Die };
            foreach (var next in playback.Upcoming.TakeWhile(next => next.Kind == PresentationCueKind.Die && next.StepId == cue.StepId))
            {
                changes.Add(next.Die);
                grouped.Add(next.Id);
            }
            var hunted = changes.Any(change => change.To == DieLocation.Hunt && change.From != DieLocation.Hunt);
            return new StepsBeat(now, marks.End, new List<(Action Start, double Seconds)>
            {
                (() => marks.Place(() => LightBefore(changes, hunted)), PresentationPace.MarkSeconds),
                (() => ChangeDice(changes, hunted), PresentationPace.HoldSeconds)
            });
        }

        // The dice that will change, where they stand now, and the Hunt plaque an allocation will raise.
        private void LightBefore(IEnumerable<ActionDieChange> changes, bool hunted)
        {
            foreach (var change in changes)
                marks.Light(board.OpponentDice.Q<Button>(change.Id));
            if (hunted)
                marks.Light(board.Hunt.Tab);
        }

        private void ChangeDice(IReadOnlyList<ActionDieChange> changes, bool hunted)
        {
            foreach (var change in changes)
                staged.Die(change);
            board.StageOpponentDice(staged.View);
            if (hunted)
                board.StageHunt(staged.View);
            marks.Place(() =>
            {
                foreach (var change in changes)
                    MarkDie(change);
                if (hunted)
                    marks.Light(board.Hunt.Tab);
            });
        }

        private void MarkDie(ActionDieChange change)
        {
            var die = board.OpponentDice.Q<Button>(change.Id);
            marks.Light(die);
            if (change.Before.HasValue && change.After.HasValue && change.Before != change.After)
                marks.Plate(die, Faces(change.Before.Value, change.After.Value), PlateSide.Under);
        }

        private IPresentationBeat Cards(PresentationCue cue, double now)
        {
            var cards = new List<ActionCardFact> { cue.Card };
            foreach (var next in playback.Upcoming.TakeWhile(next => next.Kind == PresentationCueKind.Draw && next.Revision == cue.Revision &&
                         next.Card?.Owner == cue.Card.Owner && next.Card.Use == cue.Card.Use))
            {
                cards.Add(next.Card);
                grouped.Add(next.Id);
            }
            var count = cards.Sum(card => card.Count) * (cue.Card.Use == ActionCardUse.Draw ? 1 : -1);
            var owner = cue.Card.Owner;
            return new StepsBeat(now, marks.End, new List<(Action Start, double Seconds)>
            {
                (() => marks.Place(() => marks.Light(HandCount())), PresentationPace.MarkSeconds),
                (() => ChangeHand(owner, cards, count), PresentationPace.HoldSeconds)
            });
        }

        private VisualElement HandCount() => board.TopBar.Opponent.Root.Q("opponent-cards");

        // The count changes while the counter is lit; the card backs and the signed count join it.
        private void ChangeHand(Side owner, IReadOnlyList<ActionCardFact> cards, int count)
        {
            staged.Hand(owner, count);
            board.StageOpponentSummary(staged.View);
            marks.Add(() =>
            {
                var chip = HandCount();
                if (!PresentationGlow.IsLit(chip))
                    marks.Light(chip);
                marks.Plate(chip, Backs(cards, count), PlateSide.Before);
            });
        }

        // The face the die had, an arrow and the face it has now (an Elven Ring's change).
        private static VisualElement Faces(DieFace before, DieFace after)
        {
            var plate = new VisualElement();
            plate.AddToClassList("presentation-used-face");
            plate.Add(new DieFaceIcon(before));
            plate.Add(new Label("→") { pickingMode = PickingMode.Ignore });
            plate.Add(new DieFaceIcon(after));
            return plate;
        }

        // Card backs of the decks drawn from (or discarded to, without a deck), and the signed count.
        private static VisualElement Backs(IReadOnlyList<ActionCardFact> cards, int count)
        {
            var plate = new VisualElement();
            plate.AddToClassList("presentation-card-backs");
            foreach (var card in cards.Take(MostBacks))
            {
                var back = new VisualElement { pickingMode = PickingMode.Ignore, tooltip = "" };
                back.AddToClassList("presentation-card-back");
                plate.Add(back);
            }
            var label = new Label(count > 0 ? "+" + count : "−" + -count) { name = "presentation-card-count", pickingMode = PickingMode.Ignore };
            plate.Add(label);
            return plate;
        }
    }
}
