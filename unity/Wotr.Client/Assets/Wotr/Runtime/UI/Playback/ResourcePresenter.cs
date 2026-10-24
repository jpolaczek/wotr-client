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
    //   its old → new face under it, dice gone to the Hunt Box ring the Hunt plaque as its count rises; a roll sounds once.
    // - Consecutive draws or discards are one beat at the opponent's hand count: card backs of their decks and the signed
    //   count, never an identity.
    // Passes, declines and "No card" record nothing to show and play no scene; a forfeited die keeps its cost copy
    // (CostEmphasis). None of this reads or marks a notice.
    public sealed class ResourcePresenter : ICuePresenter
    {
        public const double RingSeconds = .6;
        public const double DiceSeconds = 1;
        public const double CardsSeconds = .8;
        public const int MostBacks = 4;
        public const string RingName = "presentation-resource-ring";
        public const string PlateName = "presentation-resource-plate";
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly PresentationPlayback playback;
        private readonly PresentationSoundSink sounds;
        private readonly PresentationMarks marks;
        private readonly HashSet<string> grouped = new HashSet<string>();

        public ResourcePresenter(BoardScreen board, StagedBoard staged, PresentationPlayback playback, PresentationLayer layer,
            PresentationSoundSink sounds)
        {
            this.board = board;
            this.staged = staged;
            this.playback = playback;
            this.sounds = sounds;
            marks = new PresentationMarks(layer, RingName, PlateName);
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
            marks.Place(() => marks.Ring(socket));
            return new TimedBeat(now, RingSeconds);
        }

        private IPresentationBeat Dice(PresentationCue cue, double now)
        {
            var changes = new List<ActionDieChange> { cue.Die };
            foreach (var next in playback.Upcoming.TakeWhile(next => next.Kind == PresentationCueKind.Die && next.StepId == cue.StepId))
            {
                changes.Add(next.Die);
                grouped.Add(next.Id);
            }
            foreach (var change in changes)
                staged.Die(change);
            board.StageOpponentDice(staged.View);
            var hunted = changes.Any(change => change.To == DieLocation.Hunt && change.From != DieLocation.Hunt);
            if (hunted)
                board.StageHunt(staged.View);
            if (changes.Any(change => !change.Before.HasValue && change.After.HasValue))
                sounds.Play(SoundCue.DiceRoll, "presented/" + cue.Id);
            marks.Place(() =>
            {
                foreach (var change in changes)
                    MarkDie(change);
                if (hunted)
                    marks.Ring(board.Hunt.Tab, round: false);
            });
            return new TimedBeat(now, DiceSeconds);
        }

        private void MarkDie(ActionDieChange change)
        {
            var die = board.OpponentDice.Q<Button>(change.Id);
            marks.Ring(die);
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
            staged.Hand(cue.Card.Owner, count);
            board.StageOpponentSummary(staged.View);
            var chip = board.TopBar.Opponent.Root.Q("opponent-cards");
            marks.Place(() =>
            {
                marks.Ring(chip, round: false);
                marks.Plate(chip, Backs(cards, count), PlateSide.Before);
            });
            return new TimedBeat(now, CardsSeconds);
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
