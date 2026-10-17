using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ActionFactText
    {
        // The full record of a step, as the history list and notices read it.
        public static IEnumerable<string> Lines(ActionLogStep step, BoardPresentation data) => Lines(step, data, true);

        // UI-106: only what a reader needs to understand what happened — no source, dice, remaining budget or the
        // acting side repeated on every line.
        public static IEnumerable<string> Effects(ActionLogStep step, BoardPresentation data) => Lines(step, data, false);

        private static IEnumerable<string> Lines(ActionLogStep step, BoardPresentation data, bool record)
        {
            foreach (var move in step.Movements)
            {
                yield return ActionCardText.Words(move.Kind.ToString()) + " · " + ActionCardText.Route(move, data);
                foreach (var group in ActionCardText.Participants(move.Participants))
                    yield return group;
            }
            var facts = step.Facts;
            if (record && facts.SourceId.Length > 0)
                yield return "Source · " + ActionCardText.Source(facts.SourceId);
            foreach (var line in CharacterActionFacts.Setup(step))
                yield return line;
            if (step.Movements.Count == 0)
                foreach (var change in facts.Figures)
                {
                    var line = CharacterActionFacts.Figure(change, data);
                    if (line.Length > 0)
                        yield return line;
                }
            foreach (var nation in facts.Nations)
                yield return FigureGrid.Name(nation.Id) + " · " + (nation.Active ? "Active" : "Inactive") + " · " +
                    (nation.PreviousStepsToWar.HasValue ? nation.PreviousStepsToWar + " → " : "") +
                    (nation.StepsToWar == 0 ? "At war" : nation.StepsToWar + " to war");
            foreach (var control in facts.Control)
                yield return ActionCardText.Region(control.RegionId, data) + " · Control: " +
                    (control.After.HasValue ? ActionCardText.Actor(control.After) : "Neutral");
            foreach (var card in facts.Cards)
                yield return (record || card.Owner != step.Actor ? ActionCardText.Actor(card.Owner) + " · " : "") +
                    ActionCardText.Words(card.Use.ToString()) + " · " +
                    (card.Id.Length > 0 ? CardLibrary.Title(card.Id) : card.Count + " " + card.Deck + " card" + (card.Count == 1 ? "" : "s"));
            foreach (var line in Resources(facts, record))
                yield return line;
            foreach (var sovereign in facts.Sovereigns)
                yield return sovereign.Title + " · " + ActionCardText.Words(sovereign.Status.ToString()) + " · " +
                    ActionCardText.Region(sovereign.RegionId, data) +
                    (sovereign.Corruption.HasValue ? " · Corruption " + sovereign.Corruption : "");
            foreach (var loss in facts.LossDetails)
                yield return ActionCardText.Participants(new[] { loss.Figure }).First() + " · " + loss.Operation + " · " + loss.Hits + " hit" +
                    (loss.Hits == 1 ? "" : "s") + (loss.ReplacementId.Length > 0 ? " · Regular from " + loss.ReplacementFrom : "");
            if (facts.FellowshipAfter is { } fellowship)
            {
                yield return "Fellowship · " + (fellowship.Hidden ? "Hidden" : "Revealed") + " · Progress " + fellowship.Progress +
                    " · Corruption " + fellowship.Corruption;
                var guide = CharacterActionFacts.Guide(facts);
                if (guide.Length > 0)
                    yield return guide;
            }
            if (facts.FellowshipRoute.Count > 0)
                yield return string.Join(" → ", facts.FellowshipRoute.Select(id => ActionCardText.Region(id, data)));
            if (facts.Hunt is { } hunt)
                yield return HuntReportText.Entry(hunt.Entry, data);
            if (facts.Battle is { } battle)
                yield return $"{battle.Mode} · Round {battle.Round} · {battle.Attacker.Hits} / {battle.Defender.Hits} hits";
            if (facts.Rolls.Count > 0)
                yield return "Rolled " + string.Join(" · ", facts.Rolls) + (facts.HitTarget.HasValue ? " · Hit on " + facts.HitTarget + "+" : "") +
                    (facts.Hits.HasValue ? " · Hits " + facts.Hits : "");
            if (record && facts.Budget is { } budget)
                yield return budget.Remaining > 0 ? budget.Remaining + " remaining" : "Action budget completed";
            if (facts.Outcome is { } outcome)
                yield return $"Victory points · Free Peoples {outcome.FreePeoplesPoints} · Shadow {outcome.ShadowPoints}" +
                    (outcome.Winner.HasValue ? " · " + ActionCardText.Actor(outcome.Winner) + " wins · " +
                        VictoryPresentation.Reason(outcome.Victory) : "");
        }

        private static IEnumerable<string> Resources(ActionFacts facts, bool record)
        {
            foreach (var die in facts.Dice.Where(d => record && (d.To != DieLocation.Spent || d.Before != d.After))
                .GroupBy(d => (d.Owner, d.Source, d.Before, d.After, d.From, d.To)))
            {
                var change = die.Key;
                yield return die.Count() + "× " + ActionCardText.Actor(change.Owner) + " · " +
                    InGameResourceText.Die(change.Source) +
                    (change.Before != change.After ? ActionCardText.Face(change.Before) + " → " : "") +
                    ActionCardText.Face(change.After) + " · " + ActionCardText.Words(change.To.ToString());
            }
            // Dice placed in the Hunt Box are an effect (the Hunt's strength), not the die that paid for an action.
            var hunted = facts.Dice.Count(d => d.To == DieLocation.Hunt && d.From != DieLocation.Hunt);
            if (!record && hunted > 0)
                yield return "Hunt Box +" + hunted;
            foreach (var resource in facts.Resources)
                yield return InGameResourceText.Change(resource);
        }
    }
}
