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
        public static IEnumerable<PresentationCue> Cues(IReadOnlyList<ActionLogStep> group, ISet<ActionLogStep> shown,
            ActionMemory memory, Side you, BoardPresentation data)
        {
            var steps = group.OrderBy(step => step.Order).ToList();
            var figures = FigureCuesByStep(steps, shown, data);
            var released = new List<PresentationCue>();
            foreach (var step in steps)
            {
                var cues = Costs(step, memory)
                    .Concat(Cards(step, you))
                    .Concat(Movements(step))
                    .Concat(figures.TryGetValue(step, out var anchored) ? anchored : Enumerable.Empty<PresentationCue>())
                    .Concat(Losses(step))
                    .Concat(Battle(step, memory))
                    .Concat(Hunt(step))
                    .Concat(Fellowship(step))
                    .Concat(Politics(step))
                    .Concat(Control(step))
                    .Concat(Dice(step, memory, you))
                    .Concat(Resources(step))
                    .Concat(Outcome(step));
                foreach (var cue in cues)
                    if (memory.Keys.Add(cue.Id) && shown.Contains(step))
                        released.Add(cue);
            }
            return AfterTheirHunt(released);
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
        private static IEnumerable<PresentationCue> Costs(ActionLogStep step, ActionMemory memory)
        {
            foreach (var cost in step.Costs.Where(cost => cost.Kind is not (ActionCostKind.Free or ActionCostKind.Figure or ActionCostKind.Card)))
            {
                if (cost.Kind == ActionCostKind.Die && cost.Id.Length > 0)
                    memory.CostDice.Add(cost.Id);
                var payload = new CuePayload { Cost = cost };
                yield return new PresentationCue(step, PresentationCueKind.Cost,
                    cost.Kind + "/" + (cost.Id.Length > 0 ? cost.Id : cost.Source) + "/" + cost.Face, payload);
            }
        }

        // A face-up card only from a public Event or CombatReveal (or its table entry and removal); a commit and the
        // opponent's draws and discards show a back or a count, with the identity removed. The player's own cards never
        // make a cue.
        private static IEnumerable<PresentationCue> Cards(ActionLogStep step, Side you)
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
            var seen = new HashSet<string>();
            foreach (var step in steps)
                foreach (var change in step.Facts.Figures)
                    if (!covered.Contains(FigureId(change)) && seen.Add(FigureCues.Key(change)) && shown.Contains(step))
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

        private static HashSet<string> Covered(List<ActionLogStep> steps)
        {
            var moved = steps.SelectMany(step => step.Movements).SelectMany(move => move.Participants).Select(p => p.Id);
            var lost = steps.SelectMany(step => step.Facts.LossDetails).SelectMany(loss => new[] { loss.Figure.Id, loss.ReplacementId });
            return moved.Concat(lost).Where(id => id.Length > 0).ToHashSet();
        }

        private static string FigureId(ActionFigureChange change) => change.After?.Id ?? change.Before?.Id ?? "";

        private static IEnumerable<PresentationCue> Losses(ActionLogStep step)
        {
            if (step.Facts.LossDetails.Count == 0)
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

        // The opponent's dice that changed face or place, except the die that paid for the action (its cost cue shows it).
        private static IEnumerable<PresentationCue> Dice(ActionLogStep step, ActionMemory memory, Side you) => step.Facts.Dice
            .Where(die => die.Owner != you && !memory.CostDice.Contains(die.Id) && (die.Before != die.After || die.From != die.To))
            .Select(die => new PresentationCue(step, PresentationCueKind.Die, die.Id + "/" + die.From + ">" + die.To + "/" + die.After,
                new CuePayload { Die = die }));

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
    internal static class BattleFingerprint
    {
        public static string Of(BattleView battle) =>
            battle.Round + "/" + battle.Stage + "/" + battle.CardsRevealed + "/" + Force(battle.Attacker) + "/" + Force(battle.Defender) +
            "/" + string.Join(",", battle.SpecialAttacks.SelectMany(attack => attack.Dice).Select(Die));

        private static string Force(BattleForceView force) =>
            force.CardCommitted + ":" + force.CardId + ":" + force.CardCancelled + ":" + force.Hits + ":" + force.SpentHits + ":" +
            string.Join(",", force.ParticipantIds.OrderBy(id => id, System.StringComparer.Ordinal)) + ":" + string.Join(",", force.Dice.Select(Die));

        private static string Die(BattleDieView die) => die.Id + "=" + die.Result + ">" + die.Reroll + (die.Hit || die.RerollHit ? "h" : "");
    }
}
