using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-183: the player's current decision takes dice from the own pool, so the pool's frame lights: the Hunt
    // allocation, an armed Elven Ring before its die is chosen, a card that waits for the die that pays it, and a
    // payment with alternative dice (UI-248).
    public static class DiceDecision
    {
        public const string ClassName = "dice-decision";

        public static bool Active(PlayerView view, TurnDraft draft)
        {
            if (view == null || draft == null || view.DecisionOwner != view.You)
                return false;
            if (draft.Kind == CommandKind.AllocateHunt && HuntActions.Allocation(view) != null)
                return true;
            return RingChoice.Armed(draft) || CardAwaitsDie(view, draft) || CostDice.InPool(view, draft);
        }

        private static bool CardAwaitsDie(PlayerView view, TurnDraft draft) =>
            draft.Kind == CommandKind.PlayEvent && draft.Payload.CardId.Length > 0 && draft.Payload.DieId.Length == 0 &&
            !DraftOptions.All(view, draft).Where(o => o.Kind == draft.Kind).SelectMany(o => o.Events)
                .Any(e => e.CardId == draft.Payload.CardId && e.Costs.Any(c => c.DieId.Length == 0));
    }
}
