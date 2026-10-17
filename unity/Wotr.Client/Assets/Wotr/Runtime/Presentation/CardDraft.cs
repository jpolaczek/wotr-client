using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CardDraft
    {
        public static bool UsesCards(CommandKind? kind) => kind is CommandKind.PlayEvent or CommandKind.ChooseEffect or CommandKind.RespondEffect or CommandKind.DiscardCards or CommandKind.DiscardProtection;
        public static EventPlayOption Event(TurnDraft draft, ActionOption option) => option.Events.FirstOrDefault(e => e.CardId == draft.Payload.CardId);

        public static bool Select(TurnDraft draft, PlayerView view, string id)
        {
            var option = DraftOptions.For(view, draft, CommandKind.PlayEvent);
            var card = option?.Events.FirstOrDefault(e => e.CardId == id);
            if (card == null)
                return false;
            var next = draft.Fork();
            if (next.Kind != CommandKind.PlayEvent)
                DraftGesture.Choose(next, view, CommandKind.PlayEvent);
            next.Change(p => { p.CardId = id; p.NationId = ""; });
            MatchCost(next, view, card);
            draft.Adopt(next);
            return true;
        }

        // Auto-selection is limited to an equivalent ordinary die AND a single use of its result.
        private static void MatchCost(TurnDraft draft, PlayerView view, EventPlayOption card)
        {
            var dieId = card.Costs.Any(c => c.DieId == draft.Payload.DieId) ? draft.Payload.DieId
                : card.Costs.Any(c => c.AbilityId.Length > 0) ? ""
                : DraftGesture.EquivalentDie(view, new ActionOption { EligibleDice = card.Costs.Select(c => c.DieId).Distinct().ToList() });
            SelectCost(draft, card, dieId);
        }

        public static void SelectDie(TurnDraft draft, ActionOption option, string id)
        {
            var card = Event(draft, option);
            if (card?.Costs.Any(c => c.DieId == id) == true)
                SelectCost(draft, card, id);
        }

        private static void SelectCost(TurnDraft draft, EventPlayOption card, string dieId)
        {
            var costs = card.Costs.Where(c => c.DieId == dieId).ToList();
            draft.Change(p =>
            {
                p.DieId = dieId;
                p.Face = costs.Count == 1 ? costs[0].Face : (DieFace?)null;
                p.AbilityId = costs.Count == 1 ? costs[0].AbilityId : "";
                if (p.Face.HasValue)
                    p.CardCost = p.Face.Value;
            });
        }

        public static void Choose(TurnDraft draft, string id) => draft.Change(p =>
        {
            p.Ids.Clear();
            p.Ids.Add(id);
        });

        public static void Choose(TurnDraft draft, ActionOption option, string id) => EffectChoiceDraft.Choose(draft, option, id);

        public static bool Ready(TurnDraft draft, ActionOption option)
        {
            var p = draft.Payload;
            if (option.Kind == CommandKind.PlayEvent)
            {
                var card = Event(draft, option);
                return p.Face.HasValue && card?.Costs.Any(c => c.DieId == p.DieId && c.Face == p.CardCost && c.AbilityId == p.AbilityId) == true &&
                    (card.NationTargets.Count == 0 || card.NationTargets.Contains(p.NationId));
            }
            if (option.Kind == CommandKind.DiscardProtection)
                return option.Targets.Contains(p.CardId) && option.EligibleDice.Contains(p.DieId) && EffectChoiceDraft.Ready(draft, option);
            if (option.Kind == CommandKind.RespondEffect)
                return option.EligibleIds.Contains(p.ReactionId);
            return EffectChoiceDraft.Ready(draft, option);
        }

        public static void Reconcile(TurnDraft draft, ActionOption option)
        {
            var p = draft.Payload;
            if (option.Kind == CommandKind.PlayEvent)
            {
                var card = Event(draft, option);
                if (card == null)
                    p.CardId = "";
                if (card?.Costs.Any(c => c.DieId == p.DieId && c.Face == p.CardCost && c.AbilityId == p.AbilityId) != true)
                {
                    p.DieId = "";
                    p.Face = null;
                    p.AbilityId = "";
                }
                if (card?.NationTargets.Contains(p.NationId) != true)
                    p.NationId = "";
            }
            if (option.Kind == CommandKind.DiscardProtection)
            {
                if (!option.Targets.Contains(p.CardId))
                    p.CardId = "";
                if (!option.EligibleDice.Contains(p.DieId))
                    p.DieId = "";
            }
            if (!option.EligibleIds.Contains(p.ReactionId))
                p.ReactionId = "";
            if (option.Kind == CommandKind.ChooseEffect)
                EffectChoiceDraft.Reconcile(draft, option);
        }
    }
}
