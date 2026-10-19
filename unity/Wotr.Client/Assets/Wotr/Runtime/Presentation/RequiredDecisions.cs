using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-254: the decisions the game waits on before it can go on — excess units, battle and event losses, Hunt damage
    // and excess hand cards. They raise the required notice at the top; the window that already handles each one stays
    // its host (the region panel, the battle window, the Hunt panel, the hand).
    // UI-264: the expansions' mandatory steps wait the same way — the special-die pick, the Ruler discard, the Council
    // setup and token, and the tile a corruption attempt attaches.
    public static class RequiredDecisions
    {
        public static bool Is(DecisionKind kind) =>
            kind is DecisionKind.RemoveExcess or DecisionKind.Losses or DecisionKind.HuntDamage or DecisionKind.DiscardCards
                or DecisionKind.SpecialDieChoice or DecisionKind.RulerDiscard or DecisionKind.CouncilVersions
                or DecisionKind.CouncilHomelands or DecisionKind.CouncilToken or DecisionKind.CorruptionTile;

        public static bool Waits(PlayerView view) =>
            view?.Decision != null && view.DecisionOwner == view.You && !view.Winner.HasValue && Is(view.Decision.Kind);
    }
}
