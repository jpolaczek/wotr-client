using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-230: a die outside the pool (in the Hunt box, spent, out or removed) that the player's decision may choose
    // stands in the pool's row for that decision; the resource inspector that listed those places is gone.
    public static class DiceOutsidePool
    {
        public static bool Offered(PlayerView view, DieView die) =>
            !InPool(die) && view.DecisionOwner == view.You && view.Options.Any(o => o.Enabled && Eligible(view, o, die.Id));

        public static bool InPool(DieView die) =>
            die.Location is DieLocation.Available or DieLocation.Unrolled or DieLocation.Resolving or DieLocation.AwaitingChoice;

        private static bool Eligible(PlayerView view, ActionOption option, string id) => option.EligibleDice.Contains(id) ||
            option.Kind == CommandKind.ChooseEffect && view.Decision?.Presentation is "ActionDie" or "DieAndResult" && option.EligibleIds.Contains(id);
    }
}
