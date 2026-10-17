using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Object gestures preserve the exact cost chosen by the player. No fallback die.
    public static class CardActions
    {
        public static EventCostOption Cost(PlayerView view, TurnDraft draft, ActionDieContext context, string id, DieFace? chosenFace = null) =>
            Costs(view, draft, context, id).FirstOrDefault(c => !chosenFace.HasValue || c.Face == chosenFace);

        public static IEnumerable<EventCostOption> Costs(PlayerView view, TurnDraft draft, ActionDieContext context, string id)
        {
            var option = DraftOptions.For(view, draft, CommandKind.PlayEvent);
            var card = option?.Enabled == true ? option.Events.FirstOrDefault(e => e.CardId == id) : null;
            if (card == null)
                return Enumerable.Empty<EventCostOption>();
            if (view.Decision?.Kind != DecisionKind.Action)
                return card.Costs.Where(c => c.DieId.Length == 0 && c.AbilityId.Length == 0);
            var ability = draft.Payload.AbilityId;
            return card.Costs.Where(c => c.DieId == context.DieId && c.AbilityId == ability &&
                    Supports(view, draft, context, c.Face))
                .OrderBy(c => c.Face == DieFace.Event ? 0 : 1);
        }

        public static bool CanPlay(PlayerView view, TurnDraft draft, ActionDieContext context, string id) =>
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.CommitCombatCard && o.EligibleIds.Contains(id)) ||
            Cost(view, draft, context, id) != null;

        public static bool Start(TurnDraft draft, PlayerView view, ActionDieContext context, string id, DieFace? chosenFace = null)
        {
            if (view.Options.Any(o => o.Enabled && o.Kind == CommandKind.CommitCombatCard && o.EligibleIds.Contains(id)))
                return BattleDraft.SelectCard(draft, view, id);
            var cost = Cost(view, draft, context, id, chosenFace);
            if (cost == null)
                return false;
            var token = draft.Payload.RulerTokenId;
            draft.Choose(CommandKind.PlayEvent, cost.DieId);
            draft.Change(p =>
            {
                p.RulerTokenId = token;
                p.CardId = id;
                p.CardCost = cost.Face;
                p.Face = cost.Face;
                p.AbilityId = cost.AbilityId;
            });
            return true;
        }

        public static EventCostOption DrawCost(PlayerView view, TurnDraft draft, ActionDieContext context)
        {
            var option = DraftOptions.For(view, draft, CommandKind.DrawCard);
            if (option?.Enabled != true)
                return null;
            // A special conversion is available only after its own explicit selection.
            if (draft.Payload.AbilityId.Length > 0)
                return option.DrawCosts.FirstOrDefault(c => c.DieId == draft.Payload.DieId && c.AbilityId == draft.Payload.AbilityId);
            var face = Supports(view, draft, context, DieFace.Event) ? DieFace.Event : DieFace.CardDraw;
            if (!Supports(view, draft, context, face) || !option.EligibleDice.Contains(context.DieId))
                return null;
            return option.DrawCosts.Count == 0 ? new EventCostOption { DieId = context.DieId, Face = face }
                : option.DrawCosts.FirstOrDefault(c => c.DieId == context.DieId && c.AbilityId.Length == 0);
        }

        public static bool CanDraw(PlayerView view, TurnDraft draft, ActionDieContext context, string deck) =>
            view.Decision?.Presentation == "Deck"
                ? view.Options.Any(o => o.Enabled && o.Kind == CommandKind.ChooseEffect && o.EligibleIds.Contains(deck))
                : DrawCost(view, draft, context) != null && DraftOptions.For(view, draft, CommandKind.DrawCard).Targets.Contains(deck);

        public static bool Draw(TurnDraft draft, PlayerView view, ActionDieContext context, string deck)
        {
            if (!CanDraw(view, draft, context, deck))
                return false;
            if (view.Decision?.Presentation == "Deck")
            {
                CardDraft.Choose(draft, DraftOptions.For(view, draft), deck);
                return true;
            }
            var cost = DrawCost(view, draft, context);
            var token = draft.Payload.RulerTokenId;
            draft.Choose(CommandKind.DrawCard, cost.DieId);
            draft.Change(p => { p.Deck = deck; p.AbilityId = cost.AbilityId; p.RulerTokenId = token; });
            return true;
        }

        private static bool Supports(PlayerView view, TurnDraft draft, ActionDieContext context, DieFace face)
        {
            if (draft.Payload.RulerTokenId.Length > 0)
                return view.RulerTokens.FirstOrDefault(t => t.Id == draft.Payload.RulerTokenId)?.Face == face;
            return draft.Payload.AbilityId.Length > 0 ? draft.Payload.Face == face : context.Supports(face);
        }

        public static bool SingleEffect(PlayerView view, TurnDraft draft)
        {
            var option = DraftOptions.For(view, draft);
            if (option == null || !draft.Ready(option))
                return false;
            if (draft.Kind == CommandKind.RespondEffect)
                return true;
            return draft.Kind == CommandKind.ChooseEffect && option.Minimum == 1 && option.Maximum == 1 &&
                option.EventRecruitment == null;
        }
    }
}
