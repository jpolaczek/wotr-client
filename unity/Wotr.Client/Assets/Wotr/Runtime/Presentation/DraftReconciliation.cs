using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal static class DraftReconciliation
    {
        public static bool Apply(TurnDraft draft, PlayerView view)
        {
            if (!draft.Kind.HasValue)
            {
                if (draft.Payload.CouncilTokenId.Length > 0 && !ResourcePresentation.CanSelectCouncil(view, draft.Payload.CouncilTokenId))
                {
                    draft.Clear();
                    return true;
                }
                if (draft.Payload.RulerTokenId.Length > 0 && !view.RulerTokenActions.Any(o => o.TokenId == draft.Payload.RulerTokenId))
                {
                    draft.Clear();
                    return true;
                }
                if (ResourcePresentation.CouncilPending(draft) && draft.Payload.DieId.Length > 0 &&
                    !ResourcePresentation.CouncilDice(view, draft).Contains(draft.Payload.DieId))
                {
                    draft.Payload.DieId = "";
                    return true;
                }
                if (PalantirConversion.Armed(draft) && PalantirConversion.Dice(view, draft).Count == 0)
                {
                    draft.Clear();
                    return true;
                }
                if (draft.Payload.DieId.Length == 0 || view.Dice.Any(die => die.Id == draft.Payload.DieId &&
                    die.Owner == view.You && die.Location == DieLocation.Available))
                    return false;
                draft.Payload.DieId = "";
                return true;
            }
            var before = draft.Fork();
            var option = DraftOptions.For(view, draft);
            if (option == null || !option.Enabled)
                draft.Clear();
            else
                KeepChoices(draft, view, option);
            return !Same(before, draft);
        }

        private static void KeepChoices(TurnDraft draft, PlayerView view, ActionOption option)
        {
            if (CouncilDraft.Handles(draft.Kind))
            {
                CouncilDraft.Reconcile(draft, option);
                return;
            }
            if (CharacterDraft.UsesCharacters(draft.Kind))
            {
                CharacterDraft.Reconcile(draft, option);
                return;
            }
            draft.Payload.Ids.RemoveAll(id => !option.EligibleIds.Contains(id));
            if (option.EventRecruitment != null)
            {
                EventRecruitDraft.Reconcile(draft, option);
                return;
            }
            if (CardDraft.UsesCards(draft.Kind))
            {
                CardDraft.Reconcile(draft, option);
                return;
            }
            KeepCost(draft.Payload, option);
            if (ExpansionDraft.Handles(draft.Kind))
                ExpansionDraft.Reconcile(draft, option);
            CardDrawDraft.MatchCost(draft, option);
            if (BoardDraft.UsesBoard(draft.Kind))
                BoardDraft.Reconcile(draft, option);
            if (FellowshipDraft.UsesFellowship(draft.Kind))
                FellowshipDraft.Reconcile(draft, option);
            if (BattleDraft.UsesBattle(draft.Kind))
                BattleDraft.Reconcile(draft, option, view);
        }

        private static void KeepCost(CommandPayload payload, ActionOption option)
        {
            if (!option.EligibleDice.Contains(payload.DieId))
                payload.DieId = "";
            if (option.MovementCosts.Count > 0 && !option.MovementCosts.Any(cost =>
                cost.DieId == payload.DieId && cost.Face == payload.MovementCost && cost.AbilityId == payload.AbilityId))
            {
                payload.DieId = "";
                payload.AbilityId = "";
            }
            if (!option.EligibleIds.Contains(payload.RingId))
                payload.RingId = "";
            if (!option.Targets.Contains(payload.Deck))
                payload.Deck = "";
            if (payload.Face.HasValue && !option.Faces.Contains(payload.Face.Value))
                payload.Face = null;
        }

        internal static bool Same(TurnDraft left, TurnDraft right) =>
            left.Kind == right.Kind && left.MoveFocus == right.MoveFocus && left.ReserveId == right.ReserveId &&
            left.CompanionFocus == right.CompanionFocus && left.HuntChoice == right.HuntChoice && left.ArmedSource == right.ArmedSource &&
            SamePayload(left.Payload, right.Payload);

        private static bool SamePayload(CommandPayload left, CommandPayload right) =>
            left.DieId == right.DieId && left.RingId == right.RingId && left.RulerTokenId == right.RulerTokenId &&
            left.CouncilTokenId == right.CouncilTokenId && left.Deck == right.Deck && left.Face == right.Face &&
            left.CardId == right.CardId && left.CardCost == right.CardCost && left.ReactionId == right.ReactionId &&
            left.RegionId == right.RegionId && left.CharacterId == right.CharacterId && left.AbilityId == right.AbilityId &&
            left.GuideId == right.GuideId && left.HuntResponse == right.HuntResponse && left.NationId == right.NationId &&
            left.MovementCost == right.MovementCost && left.Stepwise == right.Stepwise && left.Ids.SequenceEqual(right.Ids) && left.Route.SequenceEqual(right.Route) &&
            left.Losses.Select(loss => (loss.UnitId, loss.Kind)).SequenceEqual(right.Losses.Select(loss => (loss.UnitId, loss.Kind))) &&
            left.Placements.Select(place => (place.UnitId, place.RegionId)).SequenceEqual(right.Placements.Select(place => (place.UnitId, place.RegionId))) &&
            left.Moves.Count == right.Moves.Count && left.Moves.Zip(right.Moves, SameMove).All(same => same) &&
            left.CompanionMoves.Count == right.CompanionMoves.Count && left.CompanionMoves.Zip(right.CompanionMoves,
                (a, b) => a.Ids.SequenceEqual(b.Ids) && a.Route.SequenceEqual(b.Route)).All(same => same);

        private static bool SameMove(ArmyMoveChoice left, ArmyMoveChoice right) =>
            left.From == right.From && left.To == right.To && left.Route.SequenceEqual(right.Route) &&
            left.UnitIds.SequenceEqual(right.UnitIds) && left.CharacterIds.SequenceEqual(right.CharacterIds);
    }
}
