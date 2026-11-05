using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-303 (OPPONENT_ACTIONS_UX §1, §9): which recorded steps a player's presentation may show. The opponent's own steps
    // qualify, its reactions during the player's action included; so do the automatic consequences of the opponent's
    // action or of a revision in which the opponent acted, and every shared Battle or Hunt resolution, even when the
    // player's own click completed it. The player's own decisions and legacy steps without facts never qualify.
    internal static class PresentationSelection
    {
        public static IEnumerable<ActionLogStep> Select(IReadOnlyList<ActionLogStep> group, Side? initiator, Side you)
        {
            var opponentActed = initiator.HasValue && initiator != you || group.Any(step => Opponent(step, you));
            return group.Where(step => !step.Legacy && (Opponent(step, you) || !step.Actor.HasValue && (opponentActed || Shared(step))));
        }

        // UI-488 (owner, 2026-11-04): what the opponent gained or lost through the player's own click is no replay of it, so
        // these facts show even on a step that is not shown: the opponent's dice (rolled or recovered, its paying die leaving),
        // its automatic draws (as counts) and the Fellowship's Mordor inactivity. The player's own payment with a die the
        // opponent owns (a Keeper's Eye) stays theirs.
        public static bool OpponentsChange(PresentationCue cue, ActionLogStep step, Side? you, ActionMemory memory, bool mordorInactivity)
        {
            if (!you.HasValue || step.Legacy)
                return false;
            return cue.Kind switch
            {
                PresentationCueKind.Die or PresentationCueKind.SpentDie => cue.Die.Owner != you && !memory.OwnCostDice.Contains(cue.Die.Id),
                PresentationCueKind.Draw => cue.Card.Use == ActionCardUse.Draw && cue.Card.Owner != you && !step.Actor.HasValue,
                PresentationCueKind.Fellowship => mordorInactivity,
                _ => false
            };
        }

        // UI-330: an observer's presentation shows every side's recorded steps; only legacy steps without facts never qualify.
        public static IEnumerable<ActionLogStep> Every(IReadOnlyList<ActionLogStep> group) => group.Where(step => !step.Legacy);

        private static bool Opponent(ActionLogStep step, Side you) => step.Actor.HasValue && step.Actor != you;

        private static bool Shared(ActionLogStep step) => step.Context.BattleId.Length > 0 || step.Context.HuntChainId.Length > 0;
    }
}
