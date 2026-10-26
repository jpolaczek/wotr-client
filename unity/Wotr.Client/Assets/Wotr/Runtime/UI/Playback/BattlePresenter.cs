using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-311 (OPPONENT_ACTIONS_UX §8 battle round; storyboard C5–C9): a battle's recorded rounds in its existing window,
    // from the public snapshots (a committed card stays a back; no unsubmitted choice is ever shown).
    // - After the attack's trace the new battle's window opens at its normal place, as a new battle opens it today; a
    //   window the player closed during this battle stays closed and the beats wait for nothing.
    // - Both played cards turn face up together for 3 s, ringed, with their initiative; a click opens the card read-only
    //   and holds the presentation. A card cancelled later is ringed with its "Cancelled".
    // - A roll or reroll rings the dice it changed in their side's row (lifted for a moment, still under Reduced motion),
    //   with one dice sound; the hits show on the faces and the totals.
    // - UI-337: committed casualties play one at a time, in their recorded order: a small hit lands on the figure's stack
    //   (StrikeSeconds), then the state steps (SettleSeconds) — the count drops, a stack losing its last figure has faded
    //   out, an Elite downgraded becomes a Regular whose stack lights. A leader or character lost with its army is
    //   ringed on its portrait; a siege extension's figure change is a delta plate.
    // - Later rounds stay in the same window; a retreat, an advance and a capture return to the map (their presenters).
    public sealed class BattlePresenter : ICuePresenter
    {
        // UI-315: the window the presentation opened settles before its round plays.
        public const double OpenSeconds = PresentationPace.PanelSettleSeconds + .6;
        public const double RevealSeconds = 3;
        public const double RollSeconds = 1;
        public const double StateSeconds = .6;
        public const double LossSeconds = 1;
        public const double StrikeSeconds = .35;
        public const double SettleSeconds = .45;
        public const double LiftMilliseconds = 250;
        public const string RollClass = "presented-roll";
        public const string LiftClass = "presented-roll-lift";
        public const string CardMarkClass = "presented-card-mark";

        public const string PlateName = "presentation-battle-plate";
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly PresentationPlayback playback;
        private readonly PresentationLayer layer;
        private readonly PresentationSoundSink sounds;
        private readonly HashSet<string> revealed = new HashSet<string>();
        private readonly PresentationMarks marks;
        private readonly BattleHits hits;
        private BattleView shown;

        public BattlePresenter(BoardScreen board, StagedBoard staged, PresentationPlayback playback, PresentationLayer layer,
            PresentationSoundSink sounds)
        {
            this.board = board;
            this.staged = staged;
            this.playback = playback;
            this.layer = layer;
            this.sounds = sounds;
            marks = new PresentationMarks(layer, PlateName);
            hits = new BattleHits(layer);
        }

        private RegionalBattlePanel Window => board.Battles;

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            EndMarks();
            if (cue.BattleId.Length == 0)
                return null;
            // A presentation continues from the battle state the board presents.
            if (shown == null && staged.View.Battle?.Id == cue.BattleId)
                shown = Copy(staged.View.Battle);
            if (shown != null && !Window.Presenting)
                Show(shown);
            return cue.Kind switch
            {
                PresentationCueKind.Attack when cue.Battle != null => Opened(cue.Battle, now),
                PresentationCueKind.Battle when cue.Battle != null => State(cue, now),
                PresentationCueKind.Card when cue.Card?.Use == ActionCardUse.CombatReveal => Reveal(cue, now),
                PresentationCueKind.Losses => Losses(cue, now),
                PresentationCueKind.Removal or PresentationCueKind.Recruit or PresentationCueKind.Upgrade => Figures(cue, now),
                _ => null
            };
        }

        // The presentation ended: the window gives its controls back to the released view.
        public void Released()
        {
            EndMarks();
            board.EndBattlePresentation();
            shown = null;
            revealed.Clear();
        }

        private IPresentationBeat Opened(BattleView battle, double now)
        {
            Show(battle);
            return Window.IsOpen ? new TimedBeat(now, OpenSeconds) : null;
        }

        private IPresentationBeat State(PresentationCue cue, double now)
        {
            var previous = shown;
            var battle = Copy(cue.Battle);
            KeepReveals(previous, battle);
            Show(battle);
            if (!Window.IsOpen || previous == null)
                return null;
            var rolled = Dice(battle).Where(die => Changed(previous, die)).Select(die => die.Id).ToList();
            var cards = Cards(previous, battle).ToList();
            if (rolled.Count > 0)
            {
                sounds.Play(SoundCue.DiceRoll, "presented/" + cue.Id);
                Mark(() => Rolled(rolled));
                return new TimedBeat(now, RollSeconds);
            }
            if (cards.Count > 0)
            {
                Mark(() => MarkCards(cards));
                return new TimedBeat(now, StateSeconds);
            }
            return BattleFingerprint.Of(previous) == BattleFingerprint.Of(battle) ? null : new TimedBeat(now, StateSeconds);
        }

        // Both cards of a round turn face up together: the next reveal of this battle is applied with this one.
        private IPresentationBeat Reveal(PresentationCue cue, double now)
        {
            if (shown == null || !revealed.Add(cue.Card.Id))
                return null;
            var battle = Copy(shown);
            Face(battle, cue.Card);
            foreach (var other in playback.Upcoming.TakeWhile(next => next.Kind == PresentationCueKind.Card).Where(next =>
                         next.Card?.Use == ActionCardUse.CombatReveal && next.BattleId == cue.BattleId && revealed.Add(next.Card.Id)))
                Face(battle, other.Card);
            Show(battle);
            if (!Window.IsOpen)
                return null;
            Mark(() => MarkCards(new[] { "attacker", "defender" }.Where(role => Force(battle, role).CardId?.Length > 0)));
            return new TimedBeat(now, RevealSeconds);
        }

        // UI-337 (owner 2026-10-25: "small animated hits that either turn a unit from Elite to Regular or kill a Regular,
        // each server-committed decision shown separately"): every recorded loss is its own hit, then its own step.
        private IPresentationBeat Losses(PresentationCue cue, double now)
        {
            var losses = cue.Losses.Where(loss => loss.Figure.UnitKind.HasValue).ToList();
            if (shown == null || !Window.IsOpen || losses.Count == 0)
                return null;
            var steps = new List<(Action Start, double Seconds)>();
            foreach (var loss in losses)
            {
                steps.Add((() => Strike(loss), StrikeSeconds));
                steps.Add((() => Settle(loss), SettleSeconds));
            }
            return new StepsBeat(now, EndMarks, steps);
        }

        private void Strike(ActionLossFact loss)
        {
            marks.End();
            var force = Force(shown, loss.Figure.Side);
            var count = force.Units.Count(unit => SameStack(unit, loss.Figure));
            var battle = shown;
            hits.Strike(() => Stack(battle, loss.Figure.Side, loss.Figure.NationId, loss.Figure.UnitKind.Value), count <= 1);
        }

        // The loss applied to the shown state: the figure leaves its column, a downgraded Elite's replacement joins it.
        private void Settle(ActionLossFact loss)
        {
            hits.Clear();
            var battle = Copy(shown);
            var force = Force(battle, loss.Figure.Side);
            var unit = force.Units.FirstOrDefault(candidate => candidate.Id == loss.Figure.Id);
            if (unit == null)
                return;
            force.Units.Remove(unit);
            var replaced = loss.Operation == BattleLossKind.Downgrade && loss.ReplacementId.Length > 0;
            if (replaced)
            {
                force.Units.Add(new UnitView
                {
                    Id = loss.ReplacementId, Nation = unit.Nation, Kind = UnitKind.Regular, Side = unit.Side, Location = unit.Location,
                    Garrison = unit.Garrison
                });
                force.ParticipantIds.Add(loss.ReplacementId);
            }
            Show(battle);
            if (replaced)
                Mark(() => marks.Light(Stack(battle, unit.Side, unit.Nation, UnitKind.Regular)));
        }

        private static bool SameStack(UnitView unit, ActionParticipant figure) =>
            unit.Nation == figure.NationId && unit.Kind == figure.UnitKind;

        private VisualElement Stack(BattleView battle, Side side, string nation, UnitKind kind) =>
            Column(battle, side)?.Q("battle-figure-" + nation + "-" + kind);

        // A battle's own figure change outside its losses: a leader or character lost with its army, a siege extension.
        private IPresentationBeat Figures(PresentationCue cue, double now)
        {
            if (shown == null || !Window.IsOpen || cue.Figures.Count == 0)
                return null;
            var battle = shown;
            var figure = cue.Figures[0].Before ?? cue.Figures[0].After;
            Mark(() =>
            {
                var column = Column(battle, figure.Side);
                foreach (var character in cue.Figures.Select(change => change.Before ?? change.After).Where(CharacterMarks.Named))
                    marks.Light(column?.Q("portrait-" + character.Id));
                Plate(column?.Q("battle-composition"), FigureChangePresenter.Deltas(cue));
            });
            return new TimedBeat(now, LossSeconds);
        }

        private void Show(BattleView battle)
        {
            shown = battle;
            staged.View.Battle = battle;
            board.StageBattle(staged.View);
        }

        // A later snapshot still shows a card this presentation turned face up (the reveal step records no snapshot).
        private static void KeepReveals(BattleView previous, BattleView battle)
        {
            if (previous == null || previous.Id != battle.Id || previous.Round != battle.Round)
                return;
            foreach (var role in new[] { "attacker", "defender" })
                if (string.IsNullOrEmpty(Force(battle, role).CardId) && !string.IsNullOrEmpty(Force(previous, role).CardId))
                    Force(battle, role).CardId = Force(previous, role).CardId;
        }

        private static void Face(BattleView battle, ActionCardFact card)
        {
            var force = battle.Attacker.Side == card.Owner ? battle.Attacker : battle.Defender;
            force.CardId = card.Id;
            force.CardCommitted = true;
            battle.CardsRevealed = true;
        }

        private static BattleForceView Force(BattleView battle, string role) => role == "attacker" ? battle.Attacker : battle.Defender;

        private static BattleForceView Force(BattleView battle, Side side) => battle.Attacker.Side == side ? battle.Attacker : battle.Defender;

        private static IEnumerable<BattleDieView> Dice(BattleView battle) =>
            battle.Attacker.Dice.Concat(battle.Defender.Dice).Concat(battle.SpecialAttacks.SelectMany(attack => attack.Dice));

        private static bool Changed(BattleView previous, BattleDieView die)
        {
            var before = Dice(previous).FirstOrDefault(known => known.Id == die.Id);
            return before == null || before.Result != die.Result || before.Reroll != die.Reroll;
        }

        // The cards newly committed or cancelled since the last state.
        private static IEnumerable<string> Cards(BattleView previous, BattleView battle) => new[] { "attacker", "defender" }.Where(role =>
            Force(battle, role).CardCancelled && !Force(previous, role).CardCancelled ||
            Force(battle, role).CardCommitted && !Force(previous, role).CardCommitted);

        private VisualElement Column(BattleView battle, Side side) =>
            Window.Presented.Q(battle.Attacker.Side == side ? "battle-attacker" : "battle-defender");

        // The beat's marks — dice and card rings, delta plates — once the window's new content is laid out.
        private void Mark(Action place) => marks.Place(place);

        private void Rolled(IEnumerable<string> ids)
        {
            var dice = ids.Select(id => Window.Presented.Q("battle-die-" + id)).Where(die => die != null).ToList();
            foreach (var die in dice)
                die.AddToClassList(RollClass);
            foreach (var die in dice)
                die.AddToClassList(LiftClass);
            layer.Root.schedule.Execute(() => dice.ForEach(die => die.RemoveFromClassList(LiftClass))).StartingIn((long)LiftMilliseconds);
        }

        private void MarkCards(IEnumerable<string> roles)
        {
            foreach (var role in roles)
                Window.Presented.Q("battle-card-" + role)?.AddToClassList(CardMarkClass);
        }

        private void Plate(VisualElement target, IEnumerable<FigureDelta> entries)
        {
            var list = entries.ToList();
            if (list.Count > 0)
                marks.Plate(target, ObservedDelta.Plate(list));
        }

        private void EndMarks()
        {
            hits.Clear();
            marks.End();
        }

        private static BattleView Copy(BattleView battle) => JsonConvert.DeserializeObject<BattleView>(JsonConvert.SerializeObject(battle));
    }
}
