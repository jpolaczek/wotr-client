using Wotr.Contracts;

namespace Wotr.Client
{
    public static class DraftCancellation
    {
        public static bool HasSelection(PlayerView view, TurnDraft draft)
        {
            if (view?.Decision == null)
                return false;
            var p = draft.Payload;
            if (view.Decision.Kind == DecisionKind.Action)
                return draft.Kind.HasValue || p.DieId.Length > 0 || p.RulerTokenId.Length > 0 || p.CouncilTokenId.Length > 0;
            // A sequence's die/ability is an already committed cost, not an undoable choice.
            var sequence = view.Decision.Kind == DecisionKind.ActionSequence;
            return (!sequence && (p.DieId.Length > 0 || p.AbilityId.Length > 0)) || p.Face.HasValue ||
                p.RingId.Length > 0 || p.RulerTokenId.Length > 0 || p.CouncilTokenId.Length > 0 ||
                p.Deck.Length > 0 || p.CardId.Length > 0 || p.ReactionId.Length > 0 ||
                p.GuideId.Length > 0 || p.RegionId.Length > 0 || p.NationId.Length > 0 || p.CharacterId.Length > 0 ||
                p.Ids.Count > 0 || p.Moves.Count > 0 || p.CompanionMoves.Count > 0 || p.Route.Count > 0 ||
                p.Placements.Count > 0 || p.Losses.Count > 0 || draft.ReserveId.Length > 0 ||
                draft.HuntChoice.HasValue;
        }
    }
}
