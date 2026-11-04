using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // What one action has already shown: the keys of its released cues and the dice that paid for it.
    internal sealed class ActionMemory
    {
        public HashSet<string> Keys { get; } = new HashSet<string>();
        public HashSet<string> CostDice { get; } = new HashSet<string>();
        // UI-488: the dice the player paid with themselves, whoever owns them.
        public HashSet<string> OwnCostDice { get; } = new HashSet<string>();
        public HashSet<string> Battles { get; } = new HashSet<string>();
    }

    // UI-303 (OPPONENT_ACTIONS_UX §8–§9): the cues of one revision of one action, in step order and, within a step, from
    // its cost to its consequences. Cues are made only from recorded facts — never from step text, today's board or the
    // command name — and every cue has a stable key, so a duplicate page, a producer step repeating the primary step's
    // facts or a later continuation never shows the same consequence twice. Every step of the revision is remembered, but
    // only the shown steps release cues: what the player did themselves is known to them and is never played back, not
    // even when an automatic step repeats it (the attacker's own attack is no attack scene for the attacker).
    internal static class PresentationCueExtraction
    {
        // `you` is the side whose own cards and dice make no cue (the player's), or null for an observer (UI-330), who sees
        // both sides' — the watched side's draws still as backs, their identities in its hand.
        public static IEnumerable<PresentationCue> Cues(IReadOnlyList<ActionLogStep> group, ISet<ActionLogStep> shown,
            ActionMemory memory, Side? you, BoardPresentation data)
        {
            var steps = group.OrderBy(step => step.Order).ToList();
            var figures = FigureCuesByStep(steps, shown, data);
            var revealed = RevealedTiles(steps);
            var corrupted = steps.Any(step => step.Facts.Sovereigns.Any(s => s.Status == SovereignStatus.Corrupted));
            var inactive = steps.Any(step => step.Kind == MordorInactivity);
            var released = new List<PresentationCue>();
            foreach (var step in steps)
            {
                var cues = Costs(step, memory, you)
                    .Concat(Cards(step, you))
                    .Concat(Movements(step))
                    .Concat(figures.TryGetValue(step, out var anchored) ? anchored : Enumerable.Empty<PresentationCue>())
                    .Concat(Losses(step))
                    .Concat(Battle(step, memory))
                    .Concat(Hunt(step))
                    .Concat(Fellowship(step))
                    .Concat(Politics(step))
                    .Concat(Sovereigns(step, revealed, corrupted))
                    .Concat(Control(step))
                    .Concat(Dice(step, memory, you))
                    .Concat(Resources(step))
                    .Concat(Outcome(step));
                foreach (var cue in cues)
                {
                    if (!memory.Keys.Add(cue.Id))
                        continue;
                    if (shown.Contains(step))
                    {
                        released.Add(cue);
                    }
                    else if (PresentationSelection.OpponentsChange(cue, step, you, memory, inactive))
                    {
                        cue.Actor = null;
                        released.Add(cue);
                    }
                }
            }
            return DiceLast(FinalBattleStatesLast(AfterTheirHunt(released)));
        }

        private const string MordorInactivity = "MordorInactivity";

        // UI-487/UI-488 (owner, 2026-11-04): the board's difference records a revision's dice on its first step, but they
        // move at its end. The action's die leaves after the action's last effect (a Fellowship move's die enters the Hunt
        // Box after its Hunt, as the rules place it), and a new turn's recovery follows it, before that turn's draws.
        private static List<PresentationCue> DiceLast(List<PresentationCue> cues)
        {
            var spent = cues.Where(cue => cue.Kind == PresentationCueKind.SpentDie).ToList();
            var recovered = cues.Where(Recovered).ToList();
            if (spent.Count == 0 && recovered.Count == 0)
                return cues;
            var draws = recovered.Count > 0 ? cues.Where(Drawn).ToList() : new List<PresentationCue>();
            return cues.Except(spent).Except(recovered).Except(draws).Concat(spent).Concat(recovered).Concat(draws).ToList();
        }

        private static bool Recovered(PresentationCue cue) =>
            cue.Kind == PresentationCueKind.Die && cue.Die.To is DieLocation.Unrolled or DieLocation.Removed;

        private static bool Drawn(PresentationCue cue) => cue.Kind == PresentationCueKind.Draw && cue.Card.Use == ActionCardUse.Draw;

        // UI-311: a decision step records its revision's final battle state, the automatic steps after it the states on the
        // way (the roll of each side, the rerolls, the losses). Its snapshot plays after theirs, so a round never steps back.
        // UI-337: after the revision's casualties of that battle too — the next round never shows before the losses.
        private static List<PresentationCue> FinalBattleStatesLast(List<PresentationCue> cues)
        {
            foreach (var final in cues.Where(cue => cue.Kind == PresentationCueKind.Battle && cue.Actor.HasValue).ToList())
            {
                var last = cues.FindLastIndex(cue => cue.Kind is PresentationCueKind.Battle or PresentationCueKind.Losses &&
                    cue.BattleId == final.BattleId);
                if (last <= cues.IndexOf(final))
                    continue;
                cues.Remove(final);
                cues.Insert(last, final);
            }
            return cues;
        }

        // UI-310: a revision's Fellowship change shows once its Hunt has shown its result. The engine records the move's
        // progress on the move itself, before the Hunt it causes, so a Fellowship cue of a Hunt chain follows that chain's
        // last Hunt cue of the revision: the tracks never move ahead of the roll and the tile.
        private static List<PresentationCue> AfterTheirHunt(List<PresentationCue> cues)
        {
            foreach (var fellowship in cues.Where(cue => cue.Kind == PresentationCueKind.Fellowship && cue.HuntChainId.Length > 0).ToList())
            {
                var last = cues.FindLastIndex(cue => cue.Kind == PresentationCueKind.Hunt && cue.HuntChainId == fellowship.HuntChainId);
                if (last <= cues.IndexOf(fellowship))
                    continue;
                cues.Remove(fellowship);
                cues.Insert(last, fellowship);
            }
            return cues;
        }

        // A card's cost is the card itself, shown by its own cue (played, entered or removed from the table); a free action
        // and a figure cost (a siege extension, shown as the figure change) have no cost cue.
        private static IEnumerable<PresentationCue> Costs(ActionLogStep step, ActionMemory memory, Side? you)
        {
            foreach (var cost in step.Costs.Where(cost => cost.Kind is not (ActionCostKind.Free or ActionCostKind.Figure or ActionCostKind.Card)))
            {
                if (cost.Kind == ActionCostKind.Die && cost.Id.Length > 0)
                {
                    memory.CostDice.Add(cost.Id);
                    if (you.HasValue && step.Actor == you)
                        memory.OwnCostDice.Add(cost.Id);
                }
                var payload = new CuePayload { Cost = cost };
                yield return new PresentationCue(step, PresentationCueKind.Cost,
                    cost.Kind + "/" + (cost.Id.Length > 0 ? cost.Id : cost.Source) + "/" + cost.Face, payload);
            }
        }

        // A face-up card only from a public Event or CombatReveal (or its table entry and removal); a commit and the
        // opponent's draws and discards show a back or a count, with the identity removed. The player's own cards never
        // make a cue.
        private static IEnumerable<PresentationCue> Cards(ActionLogStep step, Side? you)
        {
            for (var index = 0; index < step.Facts.Cards.Count; index++)
            {
                var card = step.Facts.Cards[index];
                if (card.Use is ActionCardUse.Event or ActionCardUse.CombatReveal or ActionCardUse.TableEntered or ActionCardUse.TableRemoved)
                {
                    if (card.Id.Length > 0)
                        yield return new PresentationCue(step, PresentationCueKind.Card, card.Use + "/" + card.Id, new CuePayload { Card = card });
                    continue;
                }
                if (card.Owner == you || card.Count == 0)
                    continue;
                var hidden = new ActionCardFact { Owner = card.Owner, Use = card.Use, Deck = card.Deck, Count = card.Count };
                var kind = card.Use == ActionCardUse.CombatCommit ? PresentationCueKind.Card : PresentationCueKind.Draw;
                var key = card.Use == ActionCardUse.CombatCommit
                    ? card.Use + "/" + card.Owner + "/" + step.Context.BattleId + "/" + (step.Facts.Battle?.Round ?? step.Context.BattleRound ?? 0)
                    : card.Use + "/" + card.Owner + "/" + step.Id + "/" + index;
                yield return new PresentationCue(step, kind, key, new CuePayload { Card = hidden });
            }
        }

        private static IEnumerable<PresentationCue> Movements(ActionLogStep step)
        {
            foreach (var move in step.Movements)
            {
                var payload = new CuePayload { Movement = move };
                payload.Regions.Add(move.From);
                payload.Regions.Add(move.To);
                var ids = string.Join(",", move.Participants.Select(p => p.Id).OrderBy(id => id, System.StringComparer.Ordinal));
                yield return new PresentationCue(step, MovementKind(move.Kind), move.From + ">" + move.To + "/" + ids, payload);
            }
        }

        private static PresentationCueKind MovementKind(ActionMovementKind kind) => kind switch
        {
            ActionMovementKind.Travel => PresentationCueKind.Travel,
            ActionMovementKind.Attack => PresentationCueKind.Attack,
            ActionMovementKind.Retreat => PresentationCueKind.Retreat,
            ActionMovementKind.Advance => PresentationCueKind.Advance,
            _ => PresentationCueKind.March
        };

        // The figure changes of the whole revision, minus those a movement or a battle loss describes and those the player's
        // own step records first, each cue anchored at the first step that records one of its changes.
        private static Dictionary<ActionLogStep, List<PresentationCue>> FigureCuesByStep(List<ActionLogStep> steps,
            ISet<ActionLogStep> shown, BoardPresentation data)
        {
            var covered = Covered(steps);
            var owner = new Dictionary<ActionFigureChange, ActionLogStep>();
            var records = steps.SelectMany(step => step.Facts.Figures.Select(change => (change, step)));
            foreach (var (change, step) in FigureCues.Once(records))
                if (!covered.Contains(FigureCues.FigureId(change)) && shown.Contains(step))
                    owner[change] = step;
            var anchored = new Dictionary<ActionLogStep, List<PresentationCue>>();
            foreach (var (kind, region, changes) in FigureCues.Classify(owner.Keys, data))
            {
                var anchor = changes.Select(change => owner[change]).OrderBy(step => step.Order).First();
                var payload = new CuePayload();
                payload.Figures.AddRange(changes);
                payload.Regions.Add(region);
                var key = region + "/" + string.Join(",", changes.Select(FigureCues.Key).OrderBy(id => id, System.StringComparer.Ordinal));
                if (!anchored.TryGetValue(anchor, out var list))
                    anchored[anchor] = list = new List<PresentationCue>();
                list.Add(new PresentationCue(anchor, kind, key, payload));
            }
            return anchored;
        }

        // UI-486 (owner, 2026-11-04): only a battle shows its losses itself (BattlePresenter, its window). An event's losses
        // flow as figure changes: a trace, a signed count and the staged stack (FigureChangePresenter).
        private static HashSet<string> Covered(List<ActionLogStep> steps)
        {
            var moved = steps.SelectMany(step => step.Movements).SelectMany(move => move.Participants).Select(p => p.Id);
            var lost = steps.Where(InBattle).SelectMany(step => step.Facts.LossDetails).SelectMany(loss => new[] { loss.Figure.Id, loss.ReplacementId });
            return moved.Concat(lost).Where(id => id.Length > 0).ToHashSet();
        }

        private static bool InBattle(ActionLogStep step) => step.Context.BattleId.Length > 0 || step.Facts.Battle != null;

        private static IEnumerable<PresentationCue> Losses(ActionLogStep step)
        {
            if (step.Facts.LossDetails.Count == 0 || !InBattle(step))
                yield break;
            var payload = new CuePayload();
            payload.Losses.AddRange(step.Facts.LossDetails);
            payload.Regions.AddRange(step.Facts.LossDetails.Select(loss => loss.RegionId));
            var key = string.Join(",", step.Facts.LossDetails.Select(loss => loss.Figure.Id + ":" + loss.Operation).OrderBy(id => id, System.StringComparer.Ordinal));
            yield return new PresentationCue(step, PresentationCueKind.Losses, key, payload);
        }

        // The first public snapshot of a battle is its attack (no attack movement is recorded) when it is the opening one;
        // each later snapshot whose public state changed is a battle beat for the existing battle window. A battle first
        // seen in progress (its start older than the baseline) is a beat, never a late attack scene.
        private static IEnumerable<PresentationCue> Battle(ActionLogStep step, ActionMemory memory)
        {
            var battle = step.Facts.Battle;
            if (battle == null || step.Context.BattleId.Length == 0)
                yield break;
            var payload = new CuePayload { Battle = battle };
            payload.Regions.Add(battle.Attacker.RegionId);
            payload.Regions.Add(battle.Defender.RegionId);
            if (memory.Battles.Add(battle.Id) && Opening(battle))
            {
                yield return new PresentationCue(step, PresentationCueKind.Attack, battle.Id, payload);
                yield break;
            }
            yield return new PresentationCue(step, PresentationCueKind.Battle, battle.Id + "/" + BattleFingerprint.Of(battle), payload);
        }

        private static bool Opening(BattleView battle) => battle.Round <= 1 &&
            !battle.Attacker.CardCommitted && !battle.Defender.CardCommitted && battle.Attacker.Dice.Count == 0 && battle.Defender.Dice.Count == 0;

        private static IEnumerable<PresentationCue> Hunt(ActionLogStep step)
        {
            var hunt = step.Facts.Hunt;
            if (hunt == null)
                yield break;
            var entry = hunt.Entry;
            var payload = new CuePayload { Hunt = entry };
            payload.Regions.Add(entry.Outcome.RegionId);
            yield return new PresentationCue(step, PresentationCueKind.Hunt,
                hunt.ChainId + "/" + entry.HuntId + "/" + entry.Revision + "/" + entry.Kind + "/" + entry.CharacterId + "/" + entry.Response, payload);
        }

        private static IEnumerable<PresentationCue> Fellowship(ActionLogStep step)
        {
            var before = step.Facts.FellowshipBefore;
            var after = step.Facts.FellowshipAfter;
            if (after == null || before != null && Same(before, after))
                yield break;
            var payload = new CuePayload { FellowshipBefore = before, FellowshipAfter = after };
            payload.Regions.AddRange(PublicRegions(before, after));
            yield return new PresentationCue(step, PresentationCueKind.Fellowship, State(after), payload);
        }

        // UI-310: where the camera may look — a revealed Fellowship's region, or a declared or revealed new position. A
        // hidden move stays on its track, and the Mordor track is no region: neither is framed.
        private static IEnumerable<string> PublicRegions(HuntOutcomeView before, HuntOutcomeView after)
        {
            if (!after.MordorStep.HasValue && (!after.Hidden || before != null && before.RegionId != after.RegionId))
                yield return after.RegionId;
        }

        private static bool Same(HuntOutcomeView a, HuntOutcomeView b) => State(a) == State(b);

        private static string State(HuntOutcomeView value) =>
            value.RegionId + "/" + value.Progress + "/" + value.MordorStep + "/" + value.Corruption + "/" + value.Hidden + "/" + value.GuideId;

        private static IEnumerable<PresentationCue> Politics(ActionLogStep step) => step.Facts.Nations.Select(nation =>
            new PresentationCue(step, PresentationCueKind.Politics, nation.Id + "/" + nation.Active + "/" + nation.StepsToWar,
                new CuePayload { Nation = nation }));

        private static IEnumerable<PresentationCue> Control(ActionLogStep step) => step.Facts.Control.Select(change =>
        {
            var payload = new CuePayload { Control = change };
            payload.Regions.Add(change.RegionId);
            return new PresentationCue(step, PresentationCueKind.Control, change.RegionId + "/" + change.After, payload);
        });

        // UI-344: a Kings sovereign's change, once per state (the producer step and the primary step record the same); the
        // tiles the action revealed (an attempt's returned draws) stand with it, with his corruption when it is one.
        private static IEnumerable<PresentationCue> Sovereigns(ActionLogStep step, IReadOnlyList<string> revealed, bool corruption) =>
            step.Facts.Sovereigns.Select(sovereign =>
            {
                var payload = new CuePayload { Sovereign = sovereign };
                payload.Regions.Add(sovereign.RegionId);
                if (!corruption || sovereign.Status == SovereignStatus.Corrupted)
                    payload.Tiles.AddRange(revealed);
                return new PresentationCue(step, PresentationCueKind.Sovereign,
                    sovereign.Id + "/" + sovereign.Status + "/" + sovereign.Tiles.Count + "/" + sovereign.Corruption, payload);
            });

        // The Corruption tiles the revision revealed, each recorded by its own automatic step.
        private static List<string> RevealedTiles(IEnumerable<ActionLogStep> steps) => steps
            .Where(step => step.Kind == RevealedTile).SelectMany(step => step.Facts.Targets).Distinct().ToList();

        private const string RevealedTile = "CorruptionTileRevealed";

        // The opponent's dice that changed face or place, except the die that is paying for the action (its cost cue shows
        // it). UI-344 (D4): and the player's own die the opponent rerolled in place (Dismay re-rolls a Ruler die); a reroll is
        // a beat even when it lands on the same face.
        // UI-487 (owner, 2026-11-04): the paying die's final move is its own cue when it leaves the pool (spent, out, into the
        // Hunt Box); once it returns to the pool (Vilya keeps it, a new turn recovers it) it is an ordinary change of a die.
        private static IEnumerable<PresentationCue> Dice(ActionLogStep step, ActionMemory memory, Side? you) => step.Facts.Dice
            .Where(die => (die.Owner != you || Rerolled(step, die, you)) && !Paying(die, memory) &&
                (die.Before != die.After || die.From != die.To || Rerolled(step, die, you)))
            .Select(die => new PresentationCue(step, Leaves(die, memory) ? PresentationCueKind.SpentDie : PresentationCueKind.Die,
                die.Id + "/" + die.From + ">" + die.To + "/" + die.After, new CuePayload { Die = die }));

        private static bool Paying(ActionDieChange die, ActionMemory memory) =>
            memory.CostDice.Contains(die.Id) && die.To == DieLocation.Resolving;

        // Only a paid die is ever Resolving, so one whose payment is older than the baseline still leaves as paid.
        private static bool Leaves(ActionDieChange die, ActionMemory memory) =>
            (memory.CostDice.Contains(die.Id) || die.From == DieLocation.Resolving) && !DiceOutsidePool.InPool(die.To);

        private static bool Rerolled(ActionLogStep step, ActionDieChange die, Side? you) =>
            step.Actor.HasValue && step.Actor != you && die.From == die.To && die.Before.HasValue && die.After.HasValue;

        private static IEnumerable<PresentationCue> Resources(ActionLogStep step) => step.Facts.Resources.Select(resource =>
            new PresentationCue(step, PresentationCueKind.Resource, resource.Kind + "/" + resource.Id + "/" + resource.From + ">" + resource.To,
                new CuePayload { Resource = resource }));

        private static IEnumerable<PresentationCue> Outcome(ActionLogStep step)
        {
            if (step.Facts.Outcome?.Winner != null)
                yield return new PresentationCue(step, PresentationCueKind.Outcome, "outcome", new CuePayload { Outcome = step.Facts.Outcome });
        }
    }

    // The public state of a battle snapshot a presentation can show: round, stage, cards, dice, hits and figures. History
    // snapshots are public (a committed card has no identity before the reveal, for its owner too), so both players give
    // the same snapshot the same key.
    // UI-337: a removed figure stays among the participants, so a force's figures are part of its state.
    internal static class BattleFingerprint
    {
        public static string Of(BattleView battle) =>
            battle.Round + "/" + battle.Stage + "/" + battle.CardsRevealed + "/" + Force(battle.Attacker) + "/" + Force(battle.Defender) +
            "/" + string.Join(",", battle.SpecialAttacks.SelectMany(attack => attack.Dice).Select(Die));

        private static string Force(BattleForceView force) =>
            force.CardCommitted + ":" + force.CardId + ":" + force.CardCancelled + ":" + force.Hits + ":" + force.SpentHits + ":" +
            string.Join(",", force.ParticipantIds.OrderBy(id => id, System.StringComparer.Ordinal)) + ":" +
            string.Join(",", force.Units.Select(unit => unit.Id).Concat(force.Characters.Select(character => character.Id))
                .OrderBy(id => id, System.StringComparer.Ordinal)) + ":" + string.Join(",", force.Dice.Select(Die));

        private static string Die(BattleDieView die) => die.Id + "=" + die.Result + ">" + die.Reroll + (die.Hit || die.RerollHit ? "h" : "");
    }
}
