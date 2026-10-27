using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class DieHints
    {
        // UI-97: a short description with the uses legal right now; full conditions stay behind "Why?".
        // UI-267: a Lords special die says what the special-die choice does with it, why it waits, or what its Remove
        // star means.
        public static string Describe(PlayerView view, DieView die)
        {
            var choice = SpecialDice.Tooltip(view, die);
            if (choice.Length > 0)
                return choice;
            // UI-276 (D20): a Ruler die Dismay may re-roll says which token its click spends.
            if (DismayChoice.Token(view, die.Id).Length > 0)
                return DieDescriptions.Tooltip(die) + "\n" + DismayChoice.Offer(view, die.Id);
            // UI-273: a die the Ruler discard may take says what its click does; a discarded one when it comes back.
            if (RulerDice.Candidate(view, die))
                return RulerDice.CandidateTooltip(die);
            if (RulerDice.Discarded(view, die))
                return RulerDice.DiscardedTooltip(die);
            var usable = die.Owner == view.You && die.Location == DieLocation.Available && view.Decision?.Kind == DecisionKind.Action;
            var text = DieDescriptions.Tooltip(die, usable ? Uses(view, die.Id).Distinct().ToArray() : null);
            return SpecialDice.IsLords(die) && die.RemoveSymbol ? text + "\n" + SpecialDice.RemoveLine(die, true) : text;
        }

        private static IEnumerable<string> Uses(PlayerView view, string die)
        {
            foreach (var option in view.Options.Where(o => o.Enabled))
            {
                if (option.Kind == CommandKind.PlayEvent)
                {
                    if (option.Events.Any(e => e.Costs.Any(c => c.DieId == die && c.AbilityId.Length == 0)))
                        yield return "Play a card";
                    continue;
                }
                if (option.CharacterActions.Any(c => c.Dice.Contains(die)))
                    yield return "Character abilities";
                if (!option.EligibleDice.Contains(die))
                    continue;
                var use = option.Kind switch
                {
                    CommandKind.MoveArmies or CommandKind.AttackArmy => "Armies",
                    CommandKind.Recruit => "Recruit",
                    CommandKind.AdvancePolitics => "Politics",
                    CommandKind.DrawCard => "Draw a card",
                    CommandKind.MoveFellowship or CommandKind.HideFellowship => "Fellowship",
                    CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters or CommandKind.SeparateCompanions => "Characters",
                    CommandKind.UseNarya => "Narya",
                    _ => ""
                };
                if (use.Length > 0)
                    yield return use;
            }
        }
    }
}
