using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-347 (D13): what a click on a Council medallion does now.
    public enum CouncilClick
    {
        None,
        // During a Chieftain's entry the token is its cost.
        EntryCost,
        // A Chieftain can enter with the token: it is marked and his Characters row lights.
        Mark,
        // The token's printed use starts.
        Use
    }

    // UI-264 to UI-276: every resource decision is made on its objects — the special-die pick and the Ruler discard on
    // the pool's dice (UI-264), Vilya on the paid die and its socket (UI-268), the Council's tokens on their medallions
    // (UI-270, UI-347), the Lore on the pool's dice and a result popover (UI-272), Dismay on the glowing Ruler dice
    // (UI-276) — so the Dice & tokens panel is gone.
    public static class ResourcePresentation
    {
        public static bool CouncilPending(TurnDraft draft) => draft != null && draft.Kind == null && draft.Payload.CouncilTokenId.Length > 0;

        public static bool CanSelectCouncil(PlayerView view, string token) => view?.Decision?.Kind == DecisionKind.Action &&
            !view.Winner.HasValue && view.DecisionOwner == view.You && view.Options.Any(o => o.Enabled &&
                (o.Kind == CommandKind.UseCouncilToken && o.EligibleIds.Contains(token) ||
                o.Kind == CommandKind.EnterCharacter && o.CharacterActions.Any(c => c.CouncilTokens.Contains(token))));

        // UI-347 (D13): a Kings Chieftain can enter with this Council token now, so its first click marks the token and
        // lights his Characters row.
        public static bool CouncilEntry(PlayerView view, string token) => view.Options.Any(o => o.Enabled &&
            o.Kind == CommandKind.EnterCharacter && o.CharacterActions.Any(c => c.CouncilTokens.Contains(token) && c.Dice.Count > 0));

        public static CouncilClick Clicked(PlayerView view, TurnDraft draft, string token)
        {
            if (!CanSelectCouncil(view, token))
                return CouncilClick.None;
            if (draft.Kind == CommandKind.EnterCharacter &&
                CharacterDraft.Selected(draft, DraftOptions.For(view, draft))?.CouncilTokens.Contains(token) == true)
                return CouncilClick.EntryCost;
            var marked = CouncilPending(draft) && draft.Payload.CouncilTokenId == token;
            if (CouncilEntry(view, token) && !marked)
                return CouncilClick.Mark;
            return CouncilUse(view, draft, token) != null ? CouncilClick.Use : CouncilClick.None;
        }

        // The token's printed use (Politics or Travel), or null.
        public static ComponentAction CouncilUse(PlayerView view, TurnDraft draft, string token) => ComponentActions.For(view, draft)
            .FirstOrDefault(c => c.Kind == CommandKind.UseCouncilToken && c.AbilityId == token && c.Enabled);

        // The console's hint while a marked token waits for a Chieftain's entry or its second click.
        public static string CouncilHint(PlayerView view, TurnDraft draft)
        {
            if (!CouncilPending(draft) || view?.Decision?.Kind != DecisionKind.Action)
                return "";
            var token = draft.Payload.CouncilTokenId;
            return "Click a lit Chieftain's row in the Characters panel to enter him with this token" +
                (CouncilUse(view, draft, token) != null ? ", or click the token again for its printed use (" + ExpansionLabels.CouncilToken(token) + ")" : "") +
                ". An entry also spends the die you select.";
        }

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

        // UI-264: the special-die pick and the Ruler discard are made on the lit dice in the pool.
        public static bool PoolDecision(PlayerView view) =>
            view?.Decision?.Kind is DecisionKind.SpecialDieChoice or DecisionKind.RulerDiscard or DecisionKind.KeepActionDie or
                DecisionKind.CouncilToken or DecisionKind.Dismay &&
            view.DecisionOwner == view.You && !view.Winner.HasValue;

        // The console's hint says what a click on a lit die does.
        public static string PoolHint(PlayerView view) => !PoolDecision(view) ? ""
            : SpecialDice.Choosing(view) ? SpecialDice.Hint(view) : RulerDice.Discarding(view) ? RulerDice.Hint(view) : Consequence(view, null);

        public static string Consequence(PlayerView view, TurnDraft draft) => view.Decision?.Kind switch
        {
            DecisionKind.SpecialDieChoice => "Click a highlighted die to keep it. Your other special dice wait for the next turn.",
            DecisionKind.RulerDiscard => "Click a highlighted die to set it aside until Recover. It cannot be used this turn.",
            DecisionKind.Dismay => "Click a glowing Ruler die to spend a Dismay token and re-roll it.",
            DecisionKind.CouncilToken => "Click the glowing Council token you want; you use it later in an action, without a die.",
            DecisionKind.KeepActionDie => "Click the glowing Vilya to keep the die that paid for this action, or Pass to spend the die and keep Vilya.",
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
