using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-254: the decisions the game waits on before it can go on — excess units, battle and event losses, Hunt damage
    // and excess hand cards. They raise the required notice at the top; the window that already handles each one stays
    // its host (the region panel, the battle window, the Hunt panel, the hand).
    public static class RequiredDecisions
    {
        public static bool Is(DecisionKind kind) =>
            kind is DecisionKind.RemoveExcess or DecisionKind.Losses or DecisionKind.HuntDamage or DecisionKind.DiscardCards;

        public static bool Waits(PlayerView view) =>
            view?.Decision != null && view.DecisionOwner == view.You && !view.Winner.HasValue && Is(view.Decision.Kind);
    }
}
