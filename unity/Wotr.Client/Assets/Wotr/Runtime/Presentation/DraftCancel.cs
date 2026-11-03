using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-455 (owner, 2026-11-03): "an additional small, default, always present Cancel among the console's other buttons,
    // disabled when there is nothing to cancel". It replaces UI-448's Cancel option, which stood in the console's line only
    // while a Fellowship route was drawn. Cancel drops everything chosen since the last sent step — the die, the action,
    // its figures, regions and route — as the panels' Cancel did for a route; Back undoes one step. A forced step starts
    // again (a reveal, a card's separation, the next step of an action sequence), so Cancel waits until it would change
    // something.
    public static class DraftCancel
    {
        public const string NothingTooltip = "Cancel · Nothing chosen to cancel";

        public static void Apply(TurnDraft draft, PlayerView view)
        {
            if (FellowshipDraft.SingleRoute(draft.Kind))
            {
                FellowshipDraft.CancelRoute(draft, view);
                return;
            }
            draft.Clear();
            MandatoryDraft.Ensure(view, draft);
        }

        // Whether the click would change the unsent draft of the player's own decision.
        public static bool Offered(PlayerView view, TurnDraft draft)
        {
            if (view?.Decision == null || view.DecisionOwner != view.You || view.Winner.HasValue || draft == null)
                return false;
            if (!FellowshipDraft.SingleRoute(draft.Kind) && !DraftCancellation.HasSelection(view, draft))
                return false;
            var cancelled = draft.Fork();
            Apply(cancelled, view);
            return !DraftReconciliation.Same(draft, cancelled);
        }

        public static string Tooltip(PlayerView view, TurnDraft draft)
        {
            if (!Offered(view, draft))
                return NothingTooltip;
            if (draft.Kind == CommandKind.SeparateCompanions)
                return "Cancel · Clear the chosen companions and their route";
            if (FellowshipDraft.LocationRoute(draft.Kind))
                return "Cancel · Clear the drawn route";
            return "Cancel · Drop everything chosen since your last sent step";
        }
    }
}
