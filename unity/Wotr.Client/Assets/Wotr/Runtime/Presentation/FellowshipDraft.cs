using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class FellowshipDraft
    {
        public static bool UsesFellowship(CommandKind? kind) => kind is CommandKind.DeclareFellowship or CommandKind.RevealFellowship
            or CommandKind.HideFellowship or CommandKind.SelectGuide or CommandKind.SeparateCompanions or CommandKind.MoveCompanions
            or CommandKind.AwakenSovereign or CommandKind.UseCouncilToken
            or CommandKind.MoveFellowship or CommandKind.RerollHunt or CommandKind.ResolveHuntDamage or CommandKind.EnterMordor or CommandKind.MoveShadowCharacters;

        public static bool UsesRoute(CommandKind? kind) => kind is CommandKind.DeclareFellowship or CommandKind.RevealFellowship
            or CommandKind.SeparateCompanions or CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters
            or CommandKind.AwakenSovereign or CommandKind.UseCouncilToken;

        public static bool LocationRoute(CommandKind? kind) => kind is CommandKind.DeclareFellowship or CommandKind.RevealFellowship;

        public static bool SingleRoute(CommandKind? kind) => LocationRoute(kind) || kind == CommandKind.SeparateCompanions;

        public static void UndoRoute(TurnDraft draft)
        {
            if (draft.Payload.Route.Count > 0)
                draft.Change(p => p.Route.RemoveAt(p.Route.Count - 1));
        }

        public static void CancelRoute(TurnDraft draft, PlayerView view = null)
        {
            if (!SingleRoute(draft.Kind))
                return;
            var kind = draft.Kind.Value;
            var mandatory = kind == CommandKind.RevealFellowship || view?.Decision?.Kind == DecisionKind.CompanionSeparation;
            draft.Clear();
            if (mandatory && view != null)
                DraftGesture.Choose(draft, view, kind);
            else if (mandatory)
                draft.Choose(kind);
        }

        public static void Respond(TurnDraft draft, HuntResponse response) => draft.Change(p =>
        {
            draft.HuntChoice = response;
            p.HuntResponse = response;
        });

        public static void ToggleReroll(TurnDraft draft, ActionOption option, string id)
        {
            if (option.EligibleIds.Contains(id) && (draft.Payload.Ids.Contains(id) || draft.Payload.Ids.Count < option.Maximum))
                draft.Toggle(id);
        }

        public static bool Ready(TurnDraft draft, ActionOption option)
        {
            if (option.EligibleDice.Count > 0 && !option.EligibleDice.Contains(draft.Payload.DieId))
                return false;
            switch (option.Kind)
            {
                case CommandKind.SelectGuide:
                    return option.EligibleIds.Contains(draft.Payload.GuideId);
                case CommandKind.ResolveHuntDamage:
                    return draft.HuntChoice.HasValue && option.HuntResponses.Contains(draft.HuntChoice.Value);
                case CommandKind.RerollHunt:
                    return draft.Payload.Ids.Count <= option.Maximum && draft.Payload.Ids.All(option.EligibleIds.Contains);
                case CommandKind.SeparateCompanions:
                    return draft.Payload.Ids.Count >= option.Minimum && draft.Payload.Ids.Count <= option.Maximum &&
                        draft.Payload.Ids.All(option.EligibleIds.Contains) &&
                        (option.Travel.Count == 0 ? draft.Payload.Route.Count == 0 : TravelDraft.EndAllowed(draft.Payload.Route, TravelDraft.Travel(draft, option)));
                case CommandKind.MoveCompanions:
                case CommandKind.MoveShadowCharacters:
                case CommandKind.UseCouncilToken:
                    return (draft.Payload.CompanionMoves.Count > 0 || option.AllowEmptySelection) && (option.MaximumGroups == 0 || draft.Payload.CompanionMoves.Count <= option.MaximumGroups) && draft.Payload.CompanionMoves.All(m =>
                        m.Ids.Count > 0 && m.Ids.All(option.EligibleIds.Contains) && TravelDraft.EndAllowed(m.Route, TravelDraft.GroupTravel(m.Ids, option)));
                case CommandKind.DeclareFellowship:
                case CommandKind.RevealFellowship:
                    return TravelDraft.EndAllowed(draft.Payload.Route, TravelDraft.Travel(draft, option));
                default:
                    return true;
            }
        }

        public static void Reconcile(TurnDraft draft, ActionOption option)
        {
            if (!UsesFellowship(draft.Kind))
                return;
            if (!option.EligibleIds.Contains(draft.Payload.GuideId))
                draft.Payload.GuideId = "";
            if (draft.HuntChoice.HasValue && !option.HuntResponses.Contains(draft.HuntChoice.Value))
                draft.HuntChoice = null;
            var focused = TravelDraft.Group(draft);
            foreach (var group in draft.Payload.CompanionMoves)
            {
                group.Ids.RemoveAll(id => !option.EligibleIds.Contains(id));
                KeepRoute(group.Route, TravelDraft.GroupTravel(group.Ids, option));
            }
            draft.Payload.CompanionMoves.RemoveAll(g => g.Ids.Count == 0);
            draft.CompanionFocus = focused == null ? -1 : draft.Payload.CompanionMoves.IndexOf(focused);
            if (UsesRoute(draft.Kind) && !TravelDraft.IsGrouped(draft.Kind))
                KeepRoute(draft.Payload.Route, TravelDraft.Travel(draft, option));
        }

        private static void KeepRoute(System.Collections.Generic.List<string> route, TravelOption travel)
        {
            if (!TravelDraft.PrefixAllowed(route, travel))
                route.Clear(); // Never replace an invalid route with a different strategic choice.
        }

        public static string Hint(TurnDraft draft, PlayerView view)
        {
            if (!UsesFellowship(draft.Kind))
                return "";
            if (draft.Kind == CommandKind.UseCouncilToken && draft.Payload.AbilityId != ExpansionDraft.CouncilTravel)
                return draft.Payload.AbilityId.Length == 0 ? "Choose a Council token." : "Choose a Shadow nation.";
            if (draft.Kind == CommandKind.ResolveHuntDamage && !draft.HuntChoice.HasValue)
                return "Click a response to apply it.";
            if (draft.Kind == CommandKind.SelectGuide)
                return "Choose a Guide from the highlighted portraits.";
            if (UsesRoute(draft.Kind))
            {
                if (view.Fellowship.MordorStep.HasValue && draft.Kind == CommandKind.SeparateCompanions)
                    return "Selected companions leave play.";
                var option = DraftOptions.For(view, draft);
                if (option.AllowEmptySelection && draft.Payload.CompanionMoves.Count == 0)
                    return draft.Kind == CommandKind.MoveShadowCharacters ? "Move any Nazgûl or confirm without moving."
                        : "Move any companions or confirm without moving.";
                var route = TravelDraft.Route(draft);
                var travel = TravelDraft.Travel(draft, option);
                if (draft.Kind == CommandKind.AwakenSovereign && SovereignPresentation.OwnsDecision(view, draft))
                    return ""; // The route row and its tooltip already describe this gesture.
                if (TravelDraft.TargetRoute(draft.Kind))
                    return travel == null ? "Select figures on the map, then click their destination."
                        : "Click a highlighted destination to move there.";
                if (route?.Count > 0 && travel != null && !TravelDraft.EndAllowed(route, travel))
                    return route.Count - 1 > travel.Range ? "Shorten the route or select a companion with enough range."
                        : "Continue the route · This region cannot be the destination.";
                return "Trace a route · Click its end again to finish.";
            }
            return draft.Payload.DieId.Length == 0 && view.Options.Any(o => o.Kind == draft.Kind && o.EligibleDice.Count > 0)
                ? "Choose an action die from your pool." : "";
        }
    }
}
