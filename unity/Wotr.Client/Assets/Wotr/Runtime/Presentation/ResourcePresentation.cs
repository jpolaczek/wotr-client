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
        Use,
        // UI-388: a second click on the marked token releases it.
        Release
    }

    // UI-264 to UI-276: every resource decision is made on its objects — the special-die pick and the Ruler discard on
    // the pool's dice (UI-264), Vilya on the paid die and its socket (UI-268), the Council's tokens on their medallions
    // (UI-270, UI-347), the Lore on the pool's dice and a result popover (UI-272), Dismay on the glowing Ruler dice
    // (UI-276) — so the Dice & tokens panel is gone.
    public static class ResourcePresentation
    {
        public static bool CouncilPending(TurnDraft draft) => draft != null && draft.Kind == null && draft.Payload.CouncilTokenId.Length > 0;

        // UI-388: this token is marked: its Chieftain's entry and its printed use are both lit.
        public static bool CouncilMarked(TurnDraft draft, string token) => CouncilPending(draft) && draft.Payload.CouncilTokenId == token;

        public static bool CanSelectCouncil(PlayerView view, string token) => view?.Decision?.Kind == DecisionKind.Action &&
            !view.Winner.HasValue && view.DecisionOwner == view.You && view.Options.Any(o => o.Enabled &&
                (o.Kind == CommandKind.UseCouncilToken && o.EligibleIds.Contains(token) ||
                o.Kind == CommandKind.EnterCharacter && o.CharacterActions.Any(c => c.CouncilTokens.Contains(token))));

        // UI-347 (D13): a Kings Chieftain can enter with this Council token now, so its first click marks the token and
        // lights his Characters row.
        public static bool CouncilEntry(PlayerView view, string token) => view.Options.Any(o => o.Enabled &&
            o.Kind == CommandKind.EnterCharacter && o.CharacterActions.Any(c => c.CouncilTokens.Contains(token) && c.Dice.Count > 0));

        // UI-434: the Council medallion a hint asks for glows in gold — the token the Council's choice offers ("Click the
        // glowing Council token you want"), or one a chosen Chieftain's entry may pay with while it waits for its region
        // (CharacterChoices.RegionHint: "Click a glowing Council token to pay the entry").
        public static bool CouncilAsked(PlayerView view, TurnDraft draft, string token) =>
            CouncilTokenSlots.Offered(view, token) || EntryAsksCouncil(view, draft, token);

        private static bool EntryAsksCouncil(PlayerView view, TurnDraft draft, string token)
        {
            if (draft?.Kind != CommandKind.EnterCharacter || draft.Payload.RegionId.Length > 0 || draft.Payload.CouncilTokenId.Length > 0)
                return false;
            var option = DraftOptions.For(view, draft);
            var selected = option == null ? null : CharacterDraft.Selected(draft, option);
            return selected != null && selected.Recruitment.Count == 0 && selected.CouncilTokens.Contains(token);
        }

        public static CouncilClick Clicked(PlayerView view, TurnDraft draft, string token)
        {
            if (!CanSelectCouncil(view, token))
                return CouncilClick.None;
            if (draft.Kind == CommandKind.EnterCharacter &&
                CharacterDraft.Selected(draft, DraftOptions.For(view, draft))?.CouncilTokens.Contains(token) == true)
                return CouncilClick.EntryCost;
            // UI-388 (owner): a token with several uses lights them all at its first click (its Chieftain's row, its
            // printed use's objects) and opens no panel; a second click releases it.
            if (CouncilMarked(draft, token))
                return CouncilClick.Release;
            if (CouncilEntry(view, token))
                return CouncilClick.Mark;
            return CouncilUse(view, draft, token) != null ? CouncilClick.Use : CouncilClick.None;
        }

        // The token's printed use (Politics or Travel), or null.
        public static ComponentAction CouncilUse(PlayerView view, TurnDraft draft, string token) => ComponentActions.For(view, draft)
            .FirstOrDefault(c => c.Kind == CommandKind.UseCouncilToken && c.AbilityId == token && c.Enabled);

        // The console's hint while a marked token waits for one of its lit uses (UI-388).
        public static string CouncilHint(PlayerView view, TurnDraft draft)
        {
            if (!CouncilPending(draft) || view?.Decision?.Kind != DecisionKind.Action)
                return "";
            var token = draft.Payload.CouncilTokenId;
            // UI-423 (owner's playtest pattern, like UI-374): the hint names what glows — a Shadow nation's token and its
            // arrow (no station glows, UI-410) — and the figures the travel offers, Minions among them (UI-416). UI-434: both
            // places fit the console's two lines (the longer hint lost "in Politics" to its ellipsis at 1366 × 768).
            var use = CouncilUse(view, draft, token) == null ? ""
                : token == ExpansionDraft.CouncilPolitics ? ", or a glowing Shadow nation's token or → in Politics"
                : ", or the " + CouncilTravel.Subject(view, draft) + " above the token, then their destination";
            return use.Length > 0 ? "Click a lit Chieftain's row in Characters to enter him" + use + "."
                : "Click a lit Chieftain's row in Characters to enter him with this token; the entry also spends a die.";
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
            DecisionKind.Dismay => DismayHint(view),
            DecisionKind.CouncilToken => "Click the glowing Council token you want; you use it later in an action, without a die.",
            // UI-434: Vilya is a socket in the console (Lords also has a Vilya die), so the hint says where.
            DecisionKind.KeepActionDie => "Click the glowing Vilya in the console to keep the die that paid, or Pass to spend the die and keep Vilya.",
            _ => "Choose the exact die and its new face. Changing the face does not use the die."
        };

        // UI-433, UI-434: the hint names where the Ruler die Dismay rerolls stands — the Free Peoples' in the top bar, the
        // Shadow's own in its pool beside the console.
        private static string DismayHint(PlayerView view)
        {
            var offered = DismayChoice.Option(view)?.EligibleDice.ToHashSet() ?? new HashSet<string>();
            var dice = view.Dice.Where(d => offered.Contains(d.Id)).ToList();
            var own = dice.Any(d => d.Owner == view.You);
            var opponent = dice.Any(d => d.Owner != view.You);
            var die = own && opponent ? "a glowing Ruler die, yours or the top bar's,"
                : own ? "your glowing Ruler die" : "the glowing Ruler die in the top bar";
            return "Click " + die + " to reroll it with Dismay.";
        }

        // UI-420 (owner): each explanation starts with its title, so they follow one another without a blank line.
        public static string Explain(PlayerView view, string subject)
        {
            var descriptions = view.Availability.Where(a => a.SubjectId == subject).Select(Describe);
            return TooltipText.Lines(descriptions.ToArray());
        }

        private static string Describe(ActionAvailabilityView availability)
        {
            var cost = availability.Cost.Length > 0 ? "Cost: " + availability.Cost : "";
            var lines = new List<string> { availability.Title, cost };
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
