using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class TurnSummary
    {
        public static string Preview(PlayerView view, TurnDraft draft, ActionPreview preview, BoardPresentation board)
        {
            var token = ExpansionLabels.TokenCost(view, draft);
            if (draft.Payload.AbilityId == "kings.far-sighted" && draft.Kind is CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters)
                return (token.Length > 0 ? token : "Spend the selected die · ") + "Far-sighted · Move selected groups · Range +1.";
            return token + (draft.Payload.AbilityId == CardDrawDraft.Palantir ? "Palantír · Muster as Event · Once this turn. " :
                draft.Payload.AbilityId == "kings.far-sighted" ? "Far-sighted · " + (draft.Kind == CommandKind.MoveFellowship ? "One fewer Hunt die. " : "Range +1. ") :
                draft.Payload.AbilityId == "muster-as-army-once-per-turn" ? "Messenger · Once this turn. " : "") + ActionSummary(view, draft, preview, board);
        }

        private static string ActionSummary(PlayerView view, TurnDraft draft, ActionPreview preview, BoardPresentation board) => ExpansionDraft.Handles(draft.Kind) || draft.Kind is
            CommandKind.DeclineVilya or CommandKind.DeclineDismay or CommandKind.UseLoreBeforeActions or CommandKind.ChooseCorruptionTile
            ? ExpansionSummary.Preview(view, draft, preview) : BattleDraft.UsesBattle(draft.Kind) || draft.Kind == CommandKind.AttackArmy
            ? BattleSummary.Describe(draft, view, preview, board) : FellowshipDraft.UsesFellowship(draft.Kind)
            ? FellowshipSummary.Describe(draft, view, preview, board) : draft.Kind switch
            {
                CommandKind.DrawCard => "Spend the selected die · Draw a card from the " + draft.Payload.Deck + " deck.",
                CommandKind.DiscardDie => "Discard the selected die without effect.",
                CommandKind.DiscardProtection => "Spend the selected die and discard both selected cards face down · Remove " + CardLibrary.Title(draft.Payload.CardId) + ".",
                CommandKind.DiscardWelcome => "Spend this die · Remove Your Welcome Is Doubtful.",
                CommandKind.DrawCorruptionTile => "Draw the next Corruption tile.",
                CommandKind.ChooseCorruptionTile => "Place the selected tile face down; reveal and return the others.",
                CommandKind.DiscardGates => "Spend this die · Remove The Gates are Closed.",
                CommandKind.DiscardLore => RingCost(draft) + "Remove The Lore of the Elven Rings.",
                CommandKind.DiscardDenethor => "Spend this die · Remove Denethor’s Folly.",
                CommandKind.DiscardBaradPalantir => RingCost(draft) + "Remove the Palantír of Barad-dûr.",
                CommandKind.DiscardPalantir => RingCost(draft) + "Remove Palantír.",
                CommandKind.DiscardCards => "Discard selected cards face down.",
                CommandKind.AllocateHunt => HuntAllocationText.Confirm(draft) + ".",
                CommandKind.UseRing => (view.You == Side.FreePeoples ? "Give " + Ring(draft) + " to the Shadow"
                    : "Remove " + Ring(draft)) + " · Then choose your action.",
                CommandKind.MoveArmies => view.Decision?.SourceId == "base.sh.strategy.10" ? EventArmySummary.DescribeLanding(preview, board)
                    : DraftOptions.For(view, draft).ArmyEvent != null ? EventArmySummary.Describe(preview, board)
                    : preview.Excess.Count > 0 ? "After moving: choose " + preview.Excess.Sum(e => e.Count) + " excess units to return to reserve."
                    : MoveArmies(draft, board),
                CommandKind.Recruit => preview.RemovedIds.Count > 0 ? "This leader has no army and will be removed." : Recruit(view, draft, board),
                CommandKind.AdvancePolitics => "Advance " + TooltipText.NationPhrase(draft.Payload.NationId) + " one step on the Political Track.",
                CommandKind.RemoveExcess => "Return the selected units to reserve.",
                CommandKind.PlayEvent => (draft.Payload.DieId.Length == 0 ? "No action die · Play " : "Spend the selected die · Play ") + CardLibrary.Title(draft.Payload.CardId) + ".",
                CommandKind.ChooseEffect when view.Decision?.Presentation is "ReserveFigures" or "RecruitmentPlan" => Recruit(view, draft, board),
                CommandKind.ChooseEffect when view.Decision?.Presentation == "NazgulLeadership" => NazgulLeadershipSummary.Describe(draft, view),
                CommandKind.ChooseEffect when view.Decision?.Presentation is "DieAndResult" or "SmeagolCorruption" ||
                    view.Decision?.SourceId == "lords.sh.strategy.26" => LordsCardSummary.Describe(view, draft),
                CommandKind.ChooseEffect when view.Decision?.SourceId == "base.sh.character.10" => "Move the Fellowship figure · Progress and visibility stay unchanged.",
                CommandKind.ChooseEffect when view.Decision?.Presentation == "NazgulStrike" => DraftOptions.For(view, draft)
                    .EffectChoices.First(c => c.Id == draft.Payload.Ids.Single()).Detail,
                CommandKind.ChooseEffect => ConfirmSummary.Choices(view, draft) is { Length: > 0 } chosen ? chosen + "." : "Confirm the chosen options.",
                CommandKind.RespondEffect => "Use this response · Then resume the interrupted effect.",
                CommandKind.EnterCharacter => CharacterLibrary.Name(draft.Payload.CharacterId) + " enters play · Additional die next turn.",
                CommandKind.UseCharacterAbility => draft.Payload.Placements.Count > 0 ? Recruit(view, draft, board)
                    : "Spend this die · " + CharacterDraft.Selected(draft, DraftOptions.For(view, draft))?.Title + ".",
                _ => draft.Kind is { } kind ? DecisionTray.Label(kind) + "." : ""
            };

        // UI-420 (owner): the ✓ names the armies and routes it moves; one Army die may still add a second army.
        private static string MoveArmies(TurnDraft draft, BoardPresentation board)
        {
            var moves = ConfirmSummary.Moves(draft, board);
            var text = moves.Length > 0 ? "Move " + moves : "Move armies";
            return draft.Payload.Moves.Count == 1 && draft.Payload.MovementCost == DieFace.Army ? text + " · You may add a second army." : text + ".";
        }

        private static string Recruit(PlayerView view, TurnDraft draft, BoardPresentation board)
        {
            var recruits = ConfirmSummary.Recruits(view, draft, board);
            return recruits.Length > 0 ? "Recruit " + recruits + "." : "Recruit the marked figures.";
        }

        // A Will of the West pays alone; another die gives an Elven Ring to the Shadow with it.
        private static string RingCost(TurnDraft draft) => draft.Payload.RingId.Length == 0 ? "Spend this Will of the West · "
            : "Spend this die and give " + Ring(draft) + " to the Shadow · ";

        // A named Lords ring ("Narya"), otherwise "an Elven Ring".
        private static string Ring(TurnDraft draft) =>
            draft.Payload.RingId.StartsWith("lords.", System.StringComparison.Ordinal) ? ExpansionLabels.Ring(draft.Payload.RingId) : "an Elven Ring";

    }
}
