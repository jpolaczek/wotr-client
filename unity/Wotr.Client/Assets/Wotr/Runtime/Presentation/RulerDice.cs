using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-273 (TURN_CONSOLE_UX): the Kings Ruler die wears a crown; after the roll each side with a Ruler die discards one
    // of its dice in the pool: the eligible dice glow (never a Lords special die) and say what their click does, and one
    // click discards, even when a single die is eligible (UI-279). A die discarded in a Kings game keeps its slot,
    // dimmed, until the next Recover.
    public static class RulerDice
    {
        public const string Source = "ruler";

        public static bool IsRuler(DieView die) => die.Source == Source;

        public static bool Discarding(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.RulerDiscard && view.DecisionOwner == view.You && !view.Winner.HasValue;

        public static bool Candidate(PlayerView view, DieView die) => Discarding(view) && die.Owner == view.You &&
            die.Location is DieLocation.Available or DieLocation.AwaitingChoice &&
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.DiscardRulerDie && o.EligibleDice.Contains(die.Id));

        // A standard or Ruler die discarded this turn in a Kings game, shown dimmed in its slot.
        public static bool Discarded(PlayerView view, DieView die) => view?.Configuration?.Kings == true &&
            die.Location == DieLocation.Discarded && !SpecialDice.IsLords(die) && (die.Source == "standard" || IsRuler(die));

        public static string CandidateTooltip(DieView die) =>
            "Click to discard this die · It returns at the next Recover\n" + DieDescriptions.Tooltip(die);

        public static string DiscardedTooltip(DieView die) => DieDescriptions.Tooltip(die) + "\nDiscarded · Returns at the next Recover";

        public static string Hint(PlayerView view)
        {
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.DiscardRulerDie);
            return option?.EligibleDice.Count == 1 ? "Click your only eligible die to discard it · It returns at the next Recover"
                : "Click a glowing die to discard it · It returns at the next Recover";
        }
    }
}
