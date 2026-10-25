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

        // UI-330: an observer's presentation shows every side's recorded steps; only legacy steps without facts never qualify.
        public static IEnumerable<ActionLogStep> Every(IReadOnlyList<ActionLogStep> group) => group.Where(step => !step.Legacy);

        private static bool Opponent(ActionLogStep step, Side you) => step.Actor.HasValue && step.Actor != you;

        private static bool Shared(ActionLogStep step) => step.Context.BattleId.Length > 0 || step.Context.HuntChainId.Length > 0;
    }
}
