using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ResourcePresentation
    {
        public static bool OwnsDecision(PlayerView view, TurnDraft draft)
        {
            if (view?.Decision == null || view.DecisionOwner != view.You || view.Winner.HasValue)
                return false;
            return view.Decision.Kind is DecisionKind.SpecialDieChoice or DecisionKind.RulerDiscard
                or DecisionKind.CouncilToken or DecisionKind.Dismay or DecisionKind.KeepActionDie ||
                view.Decision.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == "DieAndResult" ||
                view.Decision.Kind == DecisionKind.Action && CouncilPending(draft);
        }

        public static bool CouncilPending(TurnDraft draft) => draft != null && draft.Kind == null && draft.Payload.CouncilTokenId.Length > 0;

        public static bool CanSelectCouncil(PlayerView view, string token) => view?.Decision?.Kind == DecisionKind.Action &&
            !view.Winner.HasValue && view.DecisionOwner == view.You && view.Options.Any(o => o.Enabled &&
                (o.Kind == CommandKind.UseCouncilToken && o.EligibleIds.Contains(token) ||
                o.Kind == CommandKind.EnterCharacter && o.CharacterActions.Any(c => c.CouncilTokens.Contains(token))));

        public static IEnumerable<ComponentAction> CouncilChoices(PlayerView view, TurnDraft draft) =>
            CouncilPending(draft) ? ComponentActions.For(view, draft)
                : Enumerable.Empty<ComponentAction>();

        public static IEnumerable<string> CouncilDice(PlayerView view, TurnDraft draft)
        {
            if (draft == null || !CanSelectCouncil(view, draft.Payload.CouncilTokenId))
                return Enumerable.Empty<string>();
            var entries = view.Options.Where(o => o.Enabled && o.Kind == CommandKind.EnterCharacter)
                .SelectMany(o => o.CharacterActions).Where(c => c.CouncilTokens.Contains(draft.Payload.CouncilTokenId));
            if (draft.Kind == CommandKind.EnterCharacter)
                entries = entries.Where(c => c.CharacterId == draft.Payload.CharacterId && c.AbilityId == draft.Payload.AbilityId);
            else if (!CouncilPending(draft))
                return Enumerable.Empty<string>();
            return entries.SelectMany(c => c.Dice).Distinct();
        }

        public static string Consequence(PlayerView view, TurnDraft draft) => CouncilPending(draft)
            ? "Choose a highlighted use. Chieftain entry also spends the exact die you select."
            : view.Decision?.Kind switch
        {
            DecisionKind.SpecialDieChoice => "Click a highlighted die to keep it. Other special dice are set aside.",
            DecisionKind.RulerDiscard => "Click a highlighted die to set it aside until Recover. It cannot be used this turn.",
            DecisionKind.Dismay => "Choose a token, then either highlighted Ruler. The server rolls its new result.",
            DecisionKind.CouncilToken => "Choose one token. Its printed use can be taken later in an action.",
            DecisionKind.KeepActionDie => "Vilya keeps the die that paid for the completed action. Keeping the Ring spends that die.",
            _ => "Choose the exact die and its new face. Changing the face does not use the die."
        };

        public static string Explain(PlayerView view, string subject)
        {
            var descriptions = view.Availability.Where(a => a.SubjectId == subject).Select(Describe);
            return string.Join("\n\n", descriptions);
        }

        private static string Describe(ActionAvailabilityView availability)
        {
            var lines = new List<string> { availability.Title, "Cost · " + availability.Cost };
            lines.AddRange(availability.Conditions.Where(c => !c.Met).Select(c => c.Description));
            if (!availability.WindowOpen)
                lines.Add(availability.Timing);
            if (availability.Source.Length > 0)
                lines.Add(availability.Source);
            return string.Join("\n", lines.Where(s => s.Length > 0).Distinct());
        }

        public static string WithExplanation(PlayerView view, string subject, string detail)
        {
            var explanation = Explain(view, subject);
            return explanation.Length == 0 ? detail : detail + "\n" + explanation;
        }
    }
}
