using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-381 (owner, 2026-10-28: "in this and every analogous case the button in the bottom panel confirms, not an extra
    // button in the window"): a choice planned on the board's objects — a card's or ability's recruitment plan in
    // Reinforcements, a Council token's Nazgûl, an ability's decision in the Characters window — is confirmed with the
    // console's ✓ once the server's preview allows; its window keeps no confirmation of its own. A card's board decision
    // already was (UI-211).
    public static class ConsoleConfirm
    {
        public static bool Owns(PlayerView view, TurnDraft draft, bool planningReinforcements) =>
            view?.Decision != null && view.DecisionOwner == view.You &&
            (planningReinforcements || CouncilTravel.Active(view, draft) || CharacterRosterActions.OwnsDecision(view, draft));
    }
}
