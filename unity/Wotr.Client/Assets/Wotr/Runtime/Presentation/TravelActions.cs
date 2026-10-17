using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class TravelActions
    {
        public static bool IsStep(TurnDraft draft) => draft.Payload.Stepwise &&
            draft.Kind is CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters;

        public static ActionOption Option(PlayerView view, TurnDraft draft, ActionDieContext context)
        {
            if (view?.Decision?.Kind == DecisionKind.Action && view.DecisionOwner == view.You &&
                draft.Kind == CommandKind.UseCouncilToken && draft.Payload.AbilityId == ExpansionDraft.CouncilTravel)
            {
                var council = DraftOptions.For(view, draft);
                return council?.Enabled == true && council.Travel.Count > 0 ? council : null;
            }
            if (view?.Decision?.Kind == DecisionKind.CompanionSeparation && view.DecisionOwner == view.You)
                return view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.MoveCompanions);
            if (view?.Decision == null || view.DecisionOwner != view.You ||
                view.Decision.Kind is not (DecisionKind.Action or DecisionKind.ActionSequence) ||
                !ActionDieContext.DirectIntent(draft) ||
                draft.Payload.AbilityId != context.AbilityId)
                return null;
            var kind = view.You == Side.FreePeoples ? CommandKind.MoveCompanions : CommandKind.MoveShadowCharacters;
            if (view.ActionSequence != null && view.ActionSequence.Kind != kind)
                return null;
            var option = DraftOptions.For(view, draft, kind);
            if (option == null || !context.Allows(option))
                return null;
            var required = option.MovementCosts.FirstOrDefault(c => c.DieId == context.DieId && c.AbilityId == context.AbilityId)?.RequiredCharacterId;
            return string.IsNullOrEmpty(required) ? option : new ActionOption
            {
                Kind = option.Kind, EligibleDice = option.EligibleDice, MovementCosts = option.MovementCosts,
                EligibleIds = option.EligibleIds.Where(id => id == required).ToList(),
                Travel = option.Travel.Where(t => t.Id == required).ToList(), MaximumGroups = 1
            };
        }

        public static bool CanSelect(PlayerView view, TurnDraft draft, ActionDieContext context, string id)
        {
            if (draft.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy &&
                draft.Payload.Moves.Any(m => m.UnitIds.Any(unit => view.Units.Any(u => u.Id == unit && ArmyDraft.IsCombat(u)))))
                return false;
            var option = Option(view, draft, context);
            if (option?.EligibleIds.Contains(id) != true)
                return false;
            if (view.Decision.Kind != DecisionKind.CompanionSeparation && draft.Kind != CommandKind.UseCouncilToken || draft.Kind != option.Kind)
                return true;
            return !draft.Payload.CompanionMoves.Where((_, index) => index != draft.CompanionFocus).Any(g => g.Ids.Contains(id)) &&
                (TravelDraft.Group(draft) != null || option.MaximumGroups == 0 || draft.Payload.CompanionMoves.Count < option.MaximumGroups);
        }

        public static bool Select(PlayerView view, TurnDraft draft, ActionDieContext context, string id)
        {
            if (!CanSelect(view, draft, context, id))
                return false;
            var option = Option(view, draft, context);
            var journey = view.Decision.Kind == DecisionKind.CompanionSeparation;
            var council = draft.Kind == CommandKind.UseCouncilToken;
            if (draft.Kind != option.Kind || !TravelDraft.CanToggle(draft, view, option, id))
            {
                var token = draft.Payload.RulerTokenId;
                if ((journey || council) && draft.Kind == option.Kind)
                {
                    TravelDraft.RemoveGroup(draft);
                    TravelDraft.NewGroup(draft);
                }
                else
                    draft.Choose(option.Kind, journey ? "" : context.DieId);
                if (!council)
                    draft.Change(p =>
                    {
                        p.Stepwise = !journey;
                        p.AbilityId = journey ? "" : context.AbilityId;
                        p.RulerTokenId = journey ? "" : token;
                    });
            }
            TravelDraft.ToggleCompanion(draft, view, option, id);
            return true;
        }

        public static IEnumerable<string> Selected(TurnDraft draft) => draft.Kind is CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters
            or CommandKind.UseCouncilToken
            ? TravelDraft.Group(draft)?.Ids ?? Enumerable.Empty<string>() : Enumerable.Empty<string>();

        // UI-219: a drawn Fellowship route is complete when it is a legal end with no step left (all of the Progress
        // is used; with no Progress, the stay). A shorter legal route waits for ✓.
        public static bool Complete(TurnDraft draft, PlayerView view)
        {
            if (!FellowshipDraft.LocationRoute(draft.Kind))
                return false;
            var option = DraftOptions.For(view, draft);
            var travel = option == null ? null : TravelDraft.Travel(draft, option);
            var route = TravelDraft.Route(draft);
            return travel != null && route != null && TravelDraft.EndAllowed(route, travel) &&
                route.Count - 1 >= travel.Range && draft.Ready(option);
        }

        public static bool End(TurnDraft draft, PlayerView view, string region)
        {
            if (!FellowshipDraft.UsesRoute(draft.Kind))
                return false;
            var option = DraftOptions.For(view, draft);
            var travel = option == null ? null : TravelDraft.Travel(draft, option);
            var route = TravelDraft.Route(draft);
            if (travel == null || route == null)
                return false;
            if (route.Count == 0 && travel.Origin == region)
                TravelDraft.Stay(draft, option);
            return route.Count > 0 && route.Last() == region && draft.Ready(option);
        }
    }
}
