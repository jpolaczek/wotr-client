using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class BattleSummary
    {
        // The board names the battle's region; without it the region reads from its id.
        public static string Describe(TurnDraft draft, PlayerView view, ActionPreview preview, BoardPresentation board = null)
        {
            if (draft.Kind == CommandKind.AttackArmy)
                return AttackDraft.Mode(draft, view) + (view.Options.First(o => o.Kind == draft.Kind).ArmyEvent != null
                    ? " · No extra die. " : " · One die pays for the battle. ") + "Attackers remain in their region until an advance.";
            if (draft.Kind == CommandKind.TakeBattleLosses && view.Battle.VoluntaryLosses)
                return Sacrifice(draft, view, preview);
            if (draft.Kind == CommandKind.ExtendSiege)
                return SiegeExtension(draft, view, preview);
            if (draft.Kind is CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses)
                return Losses(draft, view, preview, board);
            if (draft.Kind == CommandKind.AdvanceBattle)
                return preview.Excess.Count > 0 ? "Enter the region · Then resolve the highlighted stacking excess." : "Enter the region with the selected survivors.";
            if (draft.Kind == CommandKind.RerollBattle)
                return Rerolls(draft, view);
            return BattleDraft.Hint(draft, view);
        }

        // UI-420 (owner): Apply losses names the region, the chosen losses and the hits they take, then what the
        // server's preview adds: a missing regular, a replacement, commanders lost with the army.
        private static string Losses(TurnDraft draft, PlayerView view, ActionPreview preview, BoardPresentation board)
        {
            var chosen = ChosenLosses(draft, view);
            var hits = preview.Losses?.AppliedHits ?? BattleDraft.Damage(draft, view);
            return TooltipText.Parts(
                "Apply losses" + LossRegion(draft, view, board),
                chosen.Length == 0 ? "" : chosen + " (" + TooltipText.Count(hits, "hit") + ")",
                Replacement(draft, preview),
                LostCommanders(view, preview));
        }

        private static string LossRegion(TurnDraft draft, PlayerView view, BoardPresentation board)
        {
            var id = draft.Kind == CommandKind.TakeEventLosses ? view.EventLoss?.RegionId : view.Battle?.RegionId;
            if (string.IsNullOrEmpty(id))
                return "";
            return " in " + (board == null ? FigureGrid.Name(id) : ActionCardText.Region(id, board));
        }

        // "Remove 1 Rohan Regular, downgrade 1 Rohan Elite", in the order of the choices.
        private static string ChosenLosses(TurnDraft draft, PlayerView view)
        {
            var groups = draft.Payload.Losses
                .Select(loss => (Loss: loss, Unit: view.Units.FirstOrDefault(u => u.Id == loss.UnitId)))
                .Where(pair => pair.Unit != null)
                .GroupBy(pair => (Operation: pair.Loss.Kind, Nation: pair.Unit.Nation, Figure: pair.Unit.Kind))
                .Select(group => Operation(group.Key.Operation) + " " + group.Count() + " " + TooltipText.Unit(group.Key.Nation, group.Key.Figure))
                .ToList();
            var text = string.Join(", ", groups);
            return text.Length == 0 ? "" : char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        private static string Operation(BattleLossKind kind) => kind == BattleLossKind.Downgrade ? "downgrade" : "remove";

        private static string Replacement(TurnDraft draft, ActionPreview preview)
        {
            var reductions = draft.Payload.Losses.Count(l => l.Kind == BattleLossKind.Downgrade);
            var missing = preview.Losses?.Operations.Count(l => l.Operation == BattleLossKind.Downgrade && l.ReplacementId.Length == 0)
                ?? reductions - preview.Placements.Count;
            var replacements = preview.Losses?.Operations.Count(l => l.ReplacementId.Length > 0) ?? preview.Placements.Count;
            return missing > 0 ? "No regular available: elite removed" : replacements > 0 ? "Elite replaced by a regular" : "";
        }

        private static string LostCommanders(PlayerView view, ActionPreview preview)
        {
            var lost = view.Characters.Where(c => preview.RemovedIds.Contains(c.Id)).Select(c => CharacterLibrary.Name(c.DefinitionId))
                .Concat(view.Units.Where(u => preview.RemovedIds.Contains(u.Id) && !ArmyDraft.IsCombat(u)).Select(u => TooltipText.Unit(u.Nation, u.Kind)))
                .ToList();
            if (lost.Count > 1)
                return "All " + lost.Count + " commanders also lost";
            return lost.Count == 1 ? lost[0] + " also lost" : "";
        }

        // UI-420 (owner): Keep roll and Reroll selected name the roll they settle and the rerolls they use.
        private static string Rerolls(TurnDraft draft, PlayerView view)
        {
            var allowed = view.Options.First(o => o.Kind == draft.Kind).Maximum;
            var chosen = draft.Payload.Ids.Count;
            if (chosen == 0)
                return "Keep roll · Accept your Combat roll (" + TooltipText.Count(OwnForce(view).Hits, "hit") + "); " + Unused(allowed);
            var dice = chosen == 1 ? "1 failed die" : chosen + " failed dice";
            return "Reroll selected · Roll " + dice + " again · " + chosen + " of " + allowed + " rerolls";
        }

        private static string Unused(int allowed) => allowed == 1 ? "do not use your reroll" : "use none of your " + allowed + " rerolls";

        private static BattleForceView OwnForce(PlayerView view) =>
            view.Battle.Attacker.Side == view.You ? view.Battle.Attacker : view.Battle.Defender;

        private static string SiegeExtension(TurnDraft draft, PlayerView view, ActionPreview preview)
        {
            var unit = view.Units.First(u => draft.Payload.Ids.Contains(u.Id));
            var cost = unit.Kind == UnitKind.Regular ? "Remove regular · Sovereign weakness." :
                preview.Placements.Count > 0 ? "Elite → Regular." : "No regular available · Elite removed.";
            var survivors = view.Battle.Attacker.Units.Any(u => ArmyDraft.IsCombat(u) && !preview.RemovedIds.Contains(u.Id)) || preview.Placements.Count > 0;
            return cost + (survivors ? " Continue the siege." : " Last fighting unit lost · Battle ends.");
        }

        private static string Sacrifice(TurnDraft draft, PlayerView view, ActionPreview preview)
        {
            if (draft.Payload.Losses.Count == 0)
                return "Keep all units · No sacrifice bonus.";
            var hits = BattleDraft.Damage(draft, view);
            var figureChange = preview.Placements.Count > 0 ? "Elite → Regular" : "Remove chosen units";
            var combat = CardLibrary.Cards.TryGetValue(view.Battle.LossSource, out var card) ? card.CombatTitle : "";
            if (combat == "Onslaught")
                return figureChange + " · Roll " + hits + (hits == 1 ? " die" : " dice") + " · Hits on 4+.";
            var force = OwnForce(view);
            if (preview.Placements.Count == 0 && force.Units.Where(ArmyDraft.IsCombat).All(u => preview.RemovedIds.Contains(u.Id)))
                return "Army eliminated · No Combat roll.";
            return combat == "Relentless Assault" ? figureChange + " · Combat +" + hits + " · Reroll unchanged."
                : figureChange + " · Confirm sacrifice.";
        }

        // UI-420 (owner): the preview's operations, then each commander eliminated with the army, without blank lines.
        public static string LossDetails(PlayerView view, ActionPreview preview) => TooltipText.Lines(
            BattleLossText.Details(preview.Losses),
            string.Join("\n", EliminatedCommanders(view, preview)));

        private static IEnumerable<string> EliminatedCommanders(PlayerView view, ActionPreview preview) =>
            view.Characters.Where(c => preview.RemovedIds.Contains(c.Id)).Select(c => CharacterLibrary.Name(c.DefinitionId) + " eliminated")
                .Concat(view.Units.Where(u => preview.RemovedIds.Contains(u.Id) && !ArmyDraft.IsCombat(u))
                    .GroupBy(u => (u.Nation, u.Kind)).Select(g => g.Count() + " " + TooltipText.Unit(g.Key.Nation, g.Key.Kind) + " eliminated"));
    }
}
