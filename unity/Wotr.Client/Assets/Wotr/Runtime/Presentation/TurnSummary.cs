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
                return (token.Length > 0 ? token : "Spend Ruler · ") + "Move selected groups · Range +1.";
            return token + (draft.Payload.AbilityId == CardDrawDraft.Palantir ? "Palantír · Muster as Event · Once this turn. " :
                draft.Payload.AbilityId == "kings.far-sighted" ? "Far-sighted · " + (draft.Kind == CommandKind.MoveFellowship ? "One fewer Hunt die. " : "Range +1. ") :
                draft.Payload.AbilityId == "muster-as-army-once-per-turn" ? "Messenger · Once this turn. " : "") + ActionSummary(view, draft, preview, board);
        }

        private static string ActionSummary(PlayerView view, TurnDraft draft, ActionPreview preview, BoardPresentation board) => ExpansionDraft.Handles(draft.Kind) || draft.Kind is
            CommandKind.DeclineVilya or CommandKind.DeclineDismay or CommandKind.UseLoreBeforeActions or CommandKind.ChooseCorruptionTile
            ? ExpansionSummary.Preview(view, draft, preview) : BattleDraft.UsesBattle(draft.Kind) || draft.Kind == CommandKind.AttackArmy
            ? BattleSummary.Describe(draft, view, preview) : FellowshipDraft.UsesFellowship(draft.Kind)
            ? FellowshipSummary.Describe(draft, view, preview, board) : draft.Kind switch
            {
                CommandKind.DrawCard => "Spend the selected die · Draw a card from the " + draft.Payload.Deck + " deck.",
                CommandKind.DiscardDie => "Discard the selected die without effect.",
                CommandKind.DiscardProtection => "Spend the selected die and discard both selected cards face down · Remove " + CardLibrary.Title(draft.Payload.CardId) + ".",
                CommandKind.DiscardWelcome => "Spend this die · Remove Your Welcome Is Doubtful.",
                CommandKind.DrawCorruptionTile => "Draw the next Corruption tile.",
                CommandKind.ChooseCorruptionTile => "Place the selected tile face down; reveal and return the others.",
                CommandKind.DiscardGates => "Spend this die · Remove The Gates are Closed.",
                CommandKind.DiscardLore => draft.Payload.RingId.Length == 0 ? "Spend this Will of the West · Remove The Lore of the Elven Rings."
                    : "Spend this die and give this Ring to Shadow · Remove The Lore of the Elven Rings.",
                CommandKind.DiscardDenethor => "Spend this die · Remove Denethor’s Folly.",
                CommandKind.DiscardBaradPalantir => draft.Payload.RingId.Length == 0 ? "Spend this Will of the West · Remove the Palantír of Barad-dûr."
                    : "Spend this die and give this Ring to Shadow · Remove the Palantír of Barad-dûr.",
                CommandKind.DiscardPalantir => draft.Payload.RingId.Length == 0 ? "Spend this Will of the West · Remove Palantír."
                    : "Spend this die and give this Ring to Shadow · Remove Palantír.",
                CommandKind.DiscardCards => "Discard selected cards face down.",
                CommandKind.AllocateHunt => "Selected dice enter the Hunt · Remaining dice will be rolled.",
                CommandKind.UseRing => view.You == Side.FreePeoples ? "Give this Ring to Shadow · Then choose your action." : "Remove this Ring · Then choose your action.",
                CommandKind.MoveArmies => view.Decision?.SourceId == "base.sh.strategy.10" ? EventArmySummary.DescribeLanding(preview, board)
                    : DraftOptions.For(view, draft).ArmyEvent != null ? EventArmySummary.Describe(preview, board)
                    : preview.Excess.Count > 0 ? "After moving: choose " + preview.Excess.Sum(e => e.Count) + " excess units to return to reserve."
                    : draft.Payload.Moves.Count == 1 && draft.Payload.MovementCost == DieFace.Army ? "Ready · You may add a second army or confirm now." : "Ready · One die pays for this movement.",
                CommandKind.Recruit => preview.RemovedIds.Count > 0 ? "This leader has no army and will be removed." : "Ready · Reinforcements appear in the marked settlements.",
                CommandKind.AdvancePolitics => "Ready · Advance the selected nation one step.",
                CommandKind.RemoveExcess => "Return the selected units to reserve.",
                CommandKind.PlayEvent => (draft.Payload.DieId.Length == 0 ? "No action die · Play " : "Spend the selected die · Play ") + CardLibrary.Title(draft.Payload.CardId) + ".",
                CommandKind.ChooseEffect when view.Decision?.Presentation == "ReserveFigures" => "Recruit the marked figures.",
                CommandKind.ChooseEffect when view.Decision?.Presentation == "RecruitmentPlan" => "Ready · Recruit the marked figures together.",
                CommandKind.ChooseEffect when view.Decision?.Presentation == "NazgulLeadership" => NazgulLeadershipSummary.Describe(draft, view),
                CommandKind.ChooseEffect when view.Decision?.Presentation is "DieAndResult" or "SmeagolCorruption" ||
                    view.Decision?.SourceId == "lords.sh.strategy.26" => LordsCardSummary.Describe(view, draft),
                CommandKind.ChooseEffect when view.Decision?.SourceId == "base.sh.character.10" => "Move the Fellowship figure · Progress and visibility stay unchanged.",
                CommandKind.ChooseEffect when view.Decision?.Presentation == "NazgulStrike" => DraftOptions.For(view, draft)
                    .EffectChoices.First(c => c.Id == draft.Payload.Ids.Single()).Detail,
                CommandKind.ChooseEffect => "Confirm this step · Earlier committed choices stay in effect.",
                CommandKind.RespondEffect => "Use this response · Then resume the interrupted effect.",
                CommandKind.EnterCharacter => CharacterLibrary.Name(draft.Payload.CharacterId) + " enters play · Additional die next turn.",
                CommandKind.UseCharacterAbility => draft.Payload.Placements.Count > 0 ? "Recruit the marked figures."
                    : "Spend this die · " + CharacterDraft.Selected(draft, DraftOptions.For(view, draft))?.Title + ".",
                _ => "Ready."
            };

    }
}
